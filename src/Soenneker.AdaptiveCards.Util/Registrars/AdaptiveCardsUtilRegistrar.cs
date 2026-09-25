using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soenneker.AdaptiveCards.Util.Abstract;

namespace Soenneker.AdaptiveCards.Util.Registrars;

/// <summary>
/// Utilities for creating and serializing Adaptive Cards
/// </summary>
public static class AdaptiveCardsUtilRegistrar
{
    /// <summary>
    /// Adds <see cref="IAdaptiveCardsUtil"/> as a singleton service. <para/>
    /// </summary>
    public static IServiceCollection AddAdaptiveCardsUtilAsSingleton(this IServiceCollection services)
    {
        services.TryAddSingleton<IAdaptiveCardsUtil, AdaptiveCardsUtil>();

        return services;
    }

    /// <summary>
    /// Adds <see cref="IAdaptiveCardsUtil"/> as a scoped service. <para/>
    /// </summary>
    public static IServiceCollection AddAdaptiveCardsUtilAsScoped(this IServiceCollection services)
    {
        services.TryAddScoped<IAdaptiveCardsUtil, AdaptiveCardsUtil>();

        return services;
    }
}
