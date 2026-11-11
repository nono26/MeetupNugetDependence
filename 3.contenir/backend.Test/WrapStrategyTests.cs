using backend.application;
using backend.application.Abstraction.Serialization;
using backend.Infrasctructure.SerializationLegacy;
using backend.Infrasctructure.SerializationModern;
using Xunit;

namespace backend.Test;

/// <summary>
/// Demonstrates the wrap strategy: callers depend on ISerializer,
/// so the underlying implementation can be swapped transparently.
/// </summary>
public class WrapStrategyTests()
{
    // Provides both implementations to every [Theory] test.
    public static IEnumerable<object[]> BothSerializers() =>
    [
        [new LegacyNewtonsoftSerializer()],
        [new SystemTextJsonSerializer()]
    ];

    [Theory]
    [MemberData(nameof(BothSerializers))]
    public void Serialize_BasicOrder_ProducesNonEmptyJson(ISerializer serializer)
    {
        var order = new Order { Id = 1, Amount = 99.99m, Status = OrderStatus.Confirmed };
        var json = serializer.Serialize(order);

        Assert.NotEmpty(json);
        Assert.Contains("99.99", json);
    }

    [Theory]
    [MemberData(nameof(BothSerializers))]
    public void RoundTrip_PreservesAllScalarProperties(ISerializer serializer)
    {
        var order = new Order
        {
            Id = 42,
            Amount = 1234.56m,
            CustomerName = "Acme Corp",
            Note = "Urgent",
            Status = OrderStatus.Shipped,
            CreatedAt = new DateTimeOffset(2024, 6, 1, 12, 0, 0, TimeSpan.Zero)
        };

        var json = serializer.Serialize(order);

        var result = serializer.Deserialize<Order>(json);

        Assert.Equal(order.Id, result.Id);
        Assert.Equal(order.Amount, result.Amount);
        Assert.Equal(order.CustomerName, result.CustomerName);
        Assert.Equal(order.Note, result.Note);
        Assert.Equal(order.Status, result.Status);
    }
}

