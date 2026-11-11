using backend.application.Abstraction.Serialization;
using backend.Infrasctructure.SerializationLegacy;
using Microsoft.Extensions.DependencyInjection;


/// <summary>
/// STEP 3: The replacement is a single-line DI change.
///
/// Because all callers depend on ISerializer (not on a concrete class),
/// swapping the implementation requires changing exactly one line.
/// This is the payoff of the wrap-then-replace strategy.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the LEGACY Newtonsoft.Json serializer.
    /// This is the "wrap" registration — behavior is unchanged.
    /// </summary>
    public static IServiceCollection AddLegacySerializer(this IServiceCollection services)
        => services.AddSingleton<ISerializer, LegacyNewtonsoftSerializer>();
}