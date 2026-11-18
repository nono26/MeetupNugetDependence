namespace backend.application.Abstraction.Serialization;

/// <summary>
/// STEP 1 of the wrap-then-replace strategy: define the abstraction.
/// This interface expresses what the application needs from a serializer
/// in domain terms — no Newtonsoft or STJ types appear anywhere in this signature.
/// </summary>
public interface ISerializer
{
    string Serialize<T>(T value);
    T Deserialize<T>(string json);
}
