using Acl.Backend.Domain;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Acl.Backend.Repo.Adapter;

public class LegacyNewtonsoftSerializer : ISerializer
{
    private readonly JsonSerializerSettings _settings = new()
    {
        NullValueHandling = NullValueHandling.Include,
        Formatting = Formatting.Indented,
        Converters = { new StringEnumConverter() }
    };

    public string Serialize<OrderDomain>(OrderDomain value)
        => JsonConvert.SerializeObject(value, _settings);

    public OrderDomain Deserialize<OrderDomain>(string json)
    {
        try
        {
            return JsonConvert.DeserializeObject<OrderDomain>(json, _settings);
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