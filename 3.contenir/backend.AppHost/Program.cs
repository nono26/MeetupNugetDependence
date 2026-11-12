using backend.application;
using backend.application.Abstraction.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddLegacySerializer();
//builder.Services.AddModernSerializer();

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

// don't want to use Newtonsoft.Json or System.Text.Json directly 
// var json = JsonConvert.SerializeObject(order, Formatting.Indented);

var serializer = app.Services.GetService<ISerializer>()!;
var json = serializer.Serialize(order);
Console.WriteLine(json);

