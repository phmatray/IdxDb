using Microsoft.Extensions.DependencyInjection;

namespace IdxDb;

public static class DependencyInjections
{
    public static IServiceCollection AddIndexedDb(this IServiceCollection services)
    {
        // Extension point for future services
        return services;
    }
    
}