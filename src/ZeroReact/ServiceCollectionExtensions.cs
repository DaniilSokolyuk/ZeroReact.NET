using System;
using Microsoft.Extensions.DependencyInjection;
using ZeroReact.JsPool;

namespace ZeroReact
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddZeroReactCore(this IServiceCollection services, Action<ReactConfiguration> configuration = null)
        {
            var config = new ReactConfiguration();
            configuration?.Invoke(config);

            services.AddSingleton(config);

            services.AddScoped<IReactIdGenerator, ReactIdGenerator>();

            services.AddSingleton<ICache, NullCache>();
            services.AddSingleton<IFileSystem, PhysicalFileSystem>();

            services.AddScoped<IReactScopedContext, ReactScopedContext>();
            services.AddSingleton<IJavaScriptEngineFactory, JavaScriptEngineFactory>();

            return services;
        }
    }
}
