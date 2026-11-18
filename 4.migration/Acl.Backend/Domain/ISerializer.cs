namespace Acl.Backend.Domain;

public interface ISerializer
{
    string Serialize<OrderDomain>(OrderDomain value);
    OrderDomain Deserialize<OrderDomain>(string json);
}