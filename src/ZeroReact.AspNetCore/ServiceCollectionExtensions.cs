using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ZeroReact.AspNetCore
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddZeroReactAsp(this IServiceCollection services, Action<ReactConfiguration> configuration = null)
        {
            services.AddZeroReactCore(configuration);

            var descriptor = new ServiceDescriptor(typeof(ICache), typeof(MemoryFileCacheCore), ServiceLifetime.Singleton);
            services.Replace(descriptor);

            var descriptor2 = new ServiceDescriptor(typeof(IFileSystem), typeof(AspNetFileSystem), ServiceLifetime.Singleton);
            services.Replace(descriptor2);

            return services;
        }
    }
}
