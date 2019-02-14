using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using ZeroReact.Components;

namespace ZeroReact
{
    public interface IReactScopedContext
    {
        T CreateComponent<T>(string componentName) where T: ReactBaseComponent;

        void GetInitJavaScript(TextWriter writer);
    }

    public class ReactScopedContext : IDisposable, IReactScopedContext
    {
        protected readonly List<ReactBaseComponent> _components = new List<ReactBaseComponent>();

        private readonly IServiceProvider _serviceProvider;

        public ReactScopedContext(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public virtual T CreateComponent<T>(string componentName) where T: ReactBaseComponent
        {
            var component = _serviceProvider.GetRequiredService<T>();

            component.ComponentName = componentName;

            _components.Add(component);

            return component;
        }

        public virtual void GetInitJavaScript(TextWriter writer)
        {
            foreach (var component in _components)
            {
                if (!component.ServerOnly)
                {
                    component.RenderJavaScript(writer);
                    writer.Write(';');
                }
            }
        }

        public void Dispose()
        {
            foreach (var component in _components)
            {
                component.Dispose();
            }
        }
    }
}
