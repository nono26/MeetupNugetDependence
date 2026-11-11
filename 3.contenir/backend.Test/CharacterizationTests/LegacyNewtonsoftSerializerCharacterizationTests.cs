using backend.application;
using backend.Infrasctructure.SerializationLegacy;
using Xunit;

namespace backend.Test.CharacterizationTests
{
    public class LegacyNewtonsoftSerializerCharacterizationTests
    {
    // Fixed offset that is intentionally NOT UTC, to characterize how the ISO
    // date format renders a non-zero offset.
    private static readonly DateTimeOffset FixedCreatedAt =
        new(2024, 1, 15, 9, 30, 0, TimeSpan.FromHours(-5));

    // Typical, fully-populated order. Every test starts from this and mutates
    // the one field under characterization.
    private static Order TypicalOrder() => new()
    {
        Id = 2,
        Amount = 199.95m,
        Note = "Leave at front desk",
        CustomerName = "Acme Corp",
        CreatedAt = FixedCreatedAt,
        Status = OrderStatus.Confirmed,
        Lines =
        {
            new OrderLine { ProductCode = "A1", Quantity = 2, UnitPrice = 49.99m },
            new OrderLine { ProductCode = "B7", Quantity = 1, UnitPrice = 100.00m },
        },
    };
     // ---- Serialize ---------------------------------------------------------

    [Fact]
    public Task Serialize_TypicalOrder_ProducesGoldenJson()
        => Verify(new LegacyNewtonsoftSerializer().Serialize(TypicalOrder()));
    

    // NullValueHandling.Include means null optionals are WRITTEN (not omitted).
    // This is exactly the kind of setting a dependency swap can silently change.
    [Fact]
    public Task Serialize_NullOptionalFields_IncludesNulls()
    {
        var order = TypicalOrder();
        order.Note = null;
        order.CustomerName = null;
        return Verify(new LegacyNewtonsoftSerializer().Serialize(order));
    }

    [Fact]
    public Task Serialize_EmptyLines_ProducesEmptyArray()
    {
        var order = TypicalOrder();
        order.Lines.Clear();
        return Verify(new LegacyNewtonsoftSerializer().Serialize(order));
    }

    // StringEnumConverter renders the enum NAME, not its numeric value. Each
    // status gets its own snapshot so a converter change is visible per-value.
    [Theory]
    [InlineData(OrderStatus.Pending)]
    [InlineData(OrderStatus.Confirmed)]
    [InlineData(OrderStatus.Shipped)]
    [InlineData(OrderStatus.Delivered)]
    [InlineData(OrderStatus.Cancelled)]
    public Task Serialize_EachStatus_RendersEnumName(OrderStatus status)
    {
        var order = TypicalOrder();
        order.Status = status;
        return Verify(new LegacyNewtonsoftSerializer().Serialize(order)).UseParameters(status);
    }

    // Decimal rendering (trailing zeros, scale) is a classic seam difference
    // between JSON libraries.
    [Fact]
    public Task Serialize_DecimalPrecision_PreservesScale()
    {
        var order = TypicalOrder();
        order.Amount = 10.50m;                 // trailing zero
        order.Lines.Clear();
        order.Lines.Add(new OrderLine { ProductCode = "C", Quantity = 3, UnitPrice = 0.1m });
        return Verify(new LegacyNewtonsoftSerializer().Serialize(order));
    }

    // How the ISO date format emits a non-zero offset.
    [Fact]
    public Task Serialize_DateTimeOffset_UsesIsoWithOffset()
            => Verify(new LegacyNewtonsoftSerializer().Serialize(TypicalOrder()));
    // (Same input as Typical, but kept as a named test so the date contract has
    //  its own dedicated golden master to watch.)

    [Fact]
    public Task Serialize_UnicodeStrings_ArePreserved()
    {
        var order = TypicalOrder();
        order.CustomerName = "Ñandú café — 日本語 🚀";
        order.Note = "quote \" and backslash \\ and newline \n";
        return Verify(new LegacyNewtonsoftSerializer().Serialize(order));
    }

    // ---- Deserialize -------------------------------------------------------

    // Round-trips through the same settings; freezes the materialized object
    // graph (enum parsed back, decimals, offset, lines).
    [Fact]
    public Task Deserialize_RoundTrip_ProducesEquivalentGraph()
    {
        var serializer = new LegacyNewtonsoftSerializer();
        var json = serializer.Serialize(TypicalOrder());
        var order = serializer.Deserialize<Order>(json);
        return Verify(order);
    }

    // JSON literal "null" makes Newtonsoft return null, which the wrapper
    // converts into an InvalidOperationException. Pin the type AND message.
    [Fact]
    public void Deserialize_NullLiteral_ThrowsInvalidOperation()
    {
        var ex = Assert.Throws<InvalidOperationException>(
            () => new LegacyNewtonsoftSerializer().Deserialize<Order>("null"));
        Assert.Equal("Deserialization returned null.", ex.Message);
    }

    // Characterizes what malformed JSON does today: it surfaces Newtonsoft's
    // own exception type (not wrapped). This pins the current contract — if a
    // dependency swap throws a different type, this test breaks on purpose.
    [Fact]
    public void Deserialize_MalformedJson_ThrowsNewtonsoftException()
    {
        Assert.ThrowsAny<Newtonsoft.Json.JsonException>(
            () => new LegacyNewtonsoftSerializer().Deserialize<Order>("{ not valid json"));
    }
    }
}