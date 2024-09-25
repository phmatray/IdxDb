using Microsoft.Extensions.DependencyInjection;

namespace IdxDb;

public static class DependencyInjections
{
    public static IServiceCollection AddIndexedDb(this IServiceCollection services)
    {
        services.AddScoped<IndexedDb>();
        return services;
    }
    
}