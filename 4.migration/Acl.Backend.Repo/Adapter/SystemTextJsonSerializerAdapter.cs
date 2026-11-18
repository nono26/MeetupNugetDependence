using System.Text.Json;
using System.Text.Json.Serialization;
using Acl.Backend.Domain;

namespace Acl.Backend.Repo.Adapter;
    public class SystemTextJsonSerializerAdapter : ISerializer
    {
            private readonly JsonSerializerOptions _options = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        PropertyNamingPolicy = null,
        Converters = { new JsonStringEnumConverter() }
    };

    public string Serialize<OrderDomain>(OrderDomain value)
        => JsonSerializer.Serialize(value, _options);

    public OrderDomain Deserialize<OrderDomain>(string json){
        try
        {
            return JsonSerializer.Deserialize<OrderDomain>(json, _options);
        }
        catch (JsonException ex)
        {
            throw new ExceptionDomain
            {
                Id = 0,
                Code = 500,
                Message = ex.Message
            };
        }
        catch (Exception ex)
        {
            throw new ExceptionDomain
            {
                Id = 0,
                Code = 500,
                Message = ex.Message
            };
        }
    }
}
