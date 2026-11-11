using backend.application;
using Newtonsoft.Json;

var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

var order = new Order
{
    Id = 1,
    Amount = 100.50m,
    CustomerName = "John Doe",
    Note = "Please deliver between 9 AM and 5 PM.",
    Status = OrderStatus.Pending,
    CreatedAt = DateTimeOffset.UtcNow
};

 var json = JsonConvert.SerializeObject(order, Formatting.Indented);

Console.WriteLine(json);
