using System;
using Microsoft.Extensions.DependencyInjection;
using ZeroReact.Components;
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

            services.AddSingleton<IComponentNameInvalidator, ComponentNameInvalidator>();
            services.AddSingleton<IReactIdGenerator, ReactIdGenerator>();
            services.AddSingleton<ICache, NullCache>();
            services.AddSingleton<IFileSystem, PhysicalFileSystem>();

            services.AddSingleton<IJavaScriptEngineFactory, JavaScriptEngineFactory>();

            services.AddScoped<IReactScopedContext, ReactScopedContext>();

            services.AddTransient<ReactComponent>();
            services.AddTransient<ReactRouterComponent>();

            return services;
        }
    }
}
