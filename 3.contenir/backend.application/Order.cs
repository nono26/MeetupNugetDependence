namespace backend.application;

public class Order
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public string? CustomerName { get; set; }
    public string? Note { get; set; }
    public OrderStatus Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public List<OrderLine> Lines { get; set; } = [];

}
public class OrderLine
{
    public string ProductCode { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
public enum OrderStatus { Pending, Confirmed, Shipped, Delivered, Cancelled }

