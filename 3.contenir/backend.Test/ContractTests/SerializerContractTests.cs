using backend.application;
using backend.application.Abstraction.Serialization;
using backend.Infrasctructure.SerializationLegacy;
using backend.Infrasctructure.SerializationModern;
using Xunit;

namespace backend.Test
{
    public abstract class SerializerContractTests
    {
        /// The only implementation-specific seam. Returns a fresh instance every call so
    /// no test can depend on state left behind by another.
    protected abstract ISerializer CreateSut();

    private static Order SampleOrder() => new()
    {
        Id = 42,
        Amount = 1234.5678m,
        CustomerName = "Ada Lovelace",
        Note = "Handle with care",
        Status = OrderStatus.Confirmed,
        CreatedAt = new DateTimeOffset(2026, 6, 28, 10, 30, 0, TimeSpan.FromHours(-5)),
        Lines =
        [
            new OrderLine { ProductCode = "ABC-1", Quantity = 2, UnitPrice = 9.99m },
            new OrderLine { ProductCode = "XYZ-9", Quantity = 1, UnitPrice = 199.50m },
        ],
    };

    // ---- Round-trip: scalars -------------------------------------------------

    [Theory]
    [InlineData(0)]
    [InlineData(42)]
    [InlineData(-7)]
    [InlineData(int.MaxValue)]
    [InlineData(int.MinValue)]
    public void RoundTrip_Int_ReturnsSameValue(int value)
    {
        var sut = CreateSut();
        Assert.Equal(value, sut.Deserialize<int>(sut.Serialize(value)));
    }

    [Theory]
    [InlineData("")]
    [InlineData("hello")]
    [InlineData("Café ☕ 日本語 — naïve")]
    [InlineData("line1\nline2\t\"quoted\"")]
    public void RoundTrip_String_PreservesValueIncludingUnicodeAndEscapes(string value)
    {
        var sut = CreateSut();
        Assert.Equal(value, sut.Deserialize<string>(sut.Serialize(value)));
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(1234.5678)]
    [InlineData(-0.0001)]
    public void RoundTrip_Decimal_PreservesPrecision(double raw)
    {
        var sut = CreateSut();
        var value = (decimal)raw;
        Assert.Equal(value, sut.Deserialize<decimal>(sut.Serialize(value)));
    }

    [Theory]
    [InlineData(OrderStatus.Pending)]
    [InlineData(OrderStatus.Cancelled)]
    public void RoundTrip_Enum_ReturnsSameValue(OrderStatus status)
    {
        var sut = CreateSut();
        Assert.Equal(status, sut.Deserialize<OrderStatus>(sut.Serialize(status)));
    }

    [Fact]
    public void RoundTrip_DateTimeOffset_PreservesInstant()
    {
        var sut = CreateSut();
        var value = new DateTimeOffset(2026, 6, 28, 10, 30, 0, TimeSpan.FromHours(-5));
        Assert.Equal(value, sut.Deserialize<DateTimeOffset>(sut.Serialize(value)));
    }

    // ---- Round-trip: nested objects & collections ----------------------------

    [Fact]
    public void RoundTrip_ComplexObject_PreservesAllScalarFields()
    {
        var sut = CreateSut();
        var original = SampleOrder();

        var result = sut.Deserialize<Order>(sut.Serialize(original));

        Assert.Equal(original.Id, result.Id);
        Assert.Equal(original.Amount, result.Amount);
        Assert.Equal(original.CustomerName, result.CustomerName);
        Assert.Equal(original.Note, result.Note);
        Assert.Equal(original.Status, result.Status);
        Assert.Equal(original.CreatedAt, result.CreatedAt);
    }

    [Fact]
    public void RoundTrip_ComplexObject_PreservesNestedCollection()
    {
        var sut = CreateSut();
        var original = SampleOrder();

        var result = sut.Deserialize<Order>(sut.Serialize(original));

        Assert.Equal(original.Lines.Count, result.Lines.Count);
        for (var i = 0; i < original.Lines.Count; i++)
        {
            Assert.Equal(original.Lines[i].ProductCode, result.Lines[i].ProductCode);
            Assert.Equal(original.Lines[i].Quantity, result.Lines[i].Quantity);
            Assert.Equal(original.Lines[i].UnitPrice, result.Lines[i].UnitPrice);
        }
    }

    [Fact]
    public void RoundTrip_ListOfPrimitives_PreservesOrderAndValues()
    {
        var sut = CreateSut();
        var original = new List<int> { 3, 1, 4, 1, 5, 9, 2, 6 };
        Assert.Equal(original, sut.Deserialize<List<int>>(sut.Serialize(original)));
    }

    // ---- Null / empty handling (the #1 source of silent divergence) ----------

    [Fact]
    public void RoundTrip_NullStringProperties_ArePreserved()
    {
        var sut = CreateSut();
        var original = SampleOrder();
        original.CustomerName = null;
        original.Note = null;

        var result = sut.Deserialize<Order>(sut.Serialize(original));

        Assert.Null(result.CustomerName);
        Assert.Null(result.Note);
    }

    [Fact]
    public void RoundTrip_EmptyCollection_StaysEmptyAndNotNull()
    {
        var sut = CreateSut();
        var original = SampleOrder();
        original.Lines = [];

        var result = sut.Deserialize<Order>(sut.Serialize(original));

        Assert.NotNull(result.Lines);
        Assert.Empty(result.Lines);
    }

    // ---- Wire format: enums are written as their string name ------------------

    [Fact]
    public void Serialize_Enum_WritesStringNameNotNumber()
    {
        var sut = CreateSut();
        var json = sut.Serialize(SampleOrder());

        // Both implementations are configured for string enums; callers and any
        // persisted JSON depend on this format being stable across a migration.
        Assert.Contains("Confirmed", json);
        Assert.DoesNotContain("\"Status\": 1", json);
        Assert.DoesNotContain("\"Status\":1", json);
    }

    // ---- Determinism ---------------------------------------------------------

    [Fact]
    public void Serialize_SameValueTwice_ProducesIdenticalOutput()
    {
        var sut = CreateSut();
        var order = SampleOrder();
        Assert.Equal(sut.Serialize(order), sut.Serialize(order));
    }

    // ---- Error handling (assert only the shared guarantee: it throws) ---------

    [Theory]
    [InlineData("not json at all")]
    [InlineData("{ \"Id\": ")]
    [InlineData("")]
    public void Deserialize_InvalidJson_Throws(string json)
    {
        var sut = CreateSut();
        Assert.ThrowsAny<Exception>(() => sut.Deserialize<Order>(json));
    }

    [Fact]
    public void Deserialize_NullLiteral_Throws()
    {
        var sut = CreateSut();
        Assert.ThrowsAny<Exception>(() => sut.Deserialize<Order>("null"));
    }
}

// One subclass per implementation — no test bodies, just the factory. xUnit
// discovers and runs the full contract inside each subclass automatically.

public class LegacyNewtonsoftSerializerContractTests : SerializerContractTests
{
    protected override ISerializer CreateSut() => new LegacyNewtonsoftSerializer();
}

public class SystemTextJsonSerializerContractTests : SerializerContractTests
{
    protected override ISerializer CreateSut() => new SystemTextJsonSerializer();
}

}