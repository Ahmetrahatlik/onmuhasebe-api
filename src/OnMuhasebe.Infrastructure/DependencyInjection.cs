using Microsoft.Extensions.DependencyInjection;
using OnMuhasebe.Application;

namespace OnMuhasebe.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddOnMuhasebe(this IServiceCollection services)
    {
        services.AddSingleton<ICariRepository, InMemoryCariRepository>();
        services.AddSingleton<IFaturaRepository, InMemoryFaturaRepository>();
        services.AddSingleton<ITahsilatRepository, InMemoryTahsilatRepository>();
        services.AddSingleton<IFaturaNumaratoru, InMemoryFaturaNumaratoru>();
        services.AddSingleton<ISaat, SistemSaati>();

        services.AddScoped<CariService>();
        services.AddScoped<FaturaService>();
        services.AddScoped<TahsilatService>();
        services.AddScoped<EkstreService>();
        return services;
    }
}
