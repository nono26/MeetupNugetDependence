using backend.application.Abstraction.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace backend.Infrasctructure.SerializationLegacy;

/// <summary>
/// STEP 2 of wrap-then-replace: wrap the existing dependency.
///
/// This class wraps Newtonsoft.Json behind ISerializer.
/// Nothing else changes externally — callers that were using Newtonsoft.Json
/// directly are migrated to use ISerializer instead, but the underlying
/// behavior is identical. This is the "wrap" move.
///
/// At this stage, no behavioral risk is taken. The dependency hasn't changed.
/// You've gained isolation — the ability to swap the implementation later.
/// </summary>
public class LegacyNewtonsoftSerializer : ISerializer
{
    private readonly JsonSerializerSettings _settings = new()
    {
        NullValueHandling = NullValueHandling.Include,
        Formatting = Formatting.Indented,
        Converters = { new StringEnumConverter() }
    };

    public string Serialize<T>(T value)
        => JsonConvert.SerializeObject(value, _settings);

    public T Deserialize<T>(string json)
        => JsonConvert.DeserializeObject<T>(json, _settings)
           ?? throw new InvalidOperationException("Deserialization returned null.");
}
