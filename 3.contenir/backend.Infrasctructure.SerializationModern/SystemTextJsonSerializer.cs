using System.Text.Json;
using System.Text.Json.Serialization;
using backend.application.Abstraction.Serialization;

namespace backend.Infrasctructure.SerializationModern;

/// <summary>
/// STEP 4 of wrap-then-replace: implement the replacement behind the same interface.
///
/// System.Text.Json is the modern, built-in .NET serializer. It is faster and
/// has lower allocations than Newtonsoft.Json.
/// </summary>
public class SystemTextJsonSerializer : ISerializer
{
    private readonly JsonSerializerOptions _options = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        PropertyNamingPolicy = null,
        Converters = { new JsonStringEnumConverter() }
    };

    public string Serialize<T>(T value)
        => JsonSerializer.Serialize(value, _options);

    public T Deserialize<T>(string json)
        => JsonSerializer.Deserialize<T>(json, _options)
           ?? throw new InvalidOperationException("Deserialization returned null.");
}