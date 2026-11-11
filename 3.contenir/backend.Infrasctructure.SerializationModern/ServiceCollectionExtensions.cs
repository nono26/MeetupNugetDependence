using backend.application.Abstraction.Serialization;
using backend.Infrasctructure.SerializationModern;
using Microsoft.Extensions.DependencyInjection;


/// <summary>
/// STEP 5: The replacement is a single-line DI change.
///
/// Because all callers depend on ISerializer (not on a concrete class),
/// swapping the implementation requires changing exactly one line.
/// This is the payoff of the wrap-then-replace strategy.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the REPLACEMENT System.Text.Json serializer.
    /// Switch to this once contract tests pass and the team has signed off.
    /// </summary>
    public static IServiceCollection AddModernSerializer(this IServiceCollection services)
        => services.AddSingleton<ISerializer, SystemTextJsonSerializer>();
}
