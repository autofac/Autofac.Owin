// This software is part of the Autofac IoC container
// Copyright © 2014 Autofac Contributors
// http://autofac.org
//
// Permission is hereby granted, free of charge, to any person
// obtaining a copy of this software and associated documentation
// files (the "Software"), to deal in the Software without
// restriction, including without limitation the rights to use,
// copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following
// conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.
using System;
using System.ComponentModel;
using System.Linq;
using Autofac;
using Autofac.Core;
using Autofac.Core.Lifetime;
using Autofac.Integration.Owin;
using Microsoft.Owin;

namespace Owin
{
    /// <summary>
    /// Extension methods for configuring Autofac within the OWIN pipeline.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class OwinExtensions
    {
        /// <summary>
        /// Unique key used to indicate the middleware has been registered with the application.
        /// </summary>
        private static readonly string MiddlewareRegisteredKey = "AutofacMiddelwareRegistered:" + Constants.AutofacMiddlewareBoundary;

        /// <summary>
        /// Adds a middleware to inject request-scoped Autofac lifetime scope into the OWIN pipeline
        /// </summary>
        /// <param name="app">The application builder.</param>
        /// <param name="container">The Autofac application lifetime scope/container.</param>
        /// <returns>The application builder.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// Thrown if <paramref name="app" /> or <paramref name="container" /> is <see langword="null" />.
        /// </exception>
        public static IAppBuilder UseAutofacLifetimeScopeInjector(this IAppBuilder app, ILifetimeScope container)
        {
            if (app == null)
            {
                throw new ArgumentNullException("app");
            }

            if (container == null)
            {
                throw new ArgumentNullException("container");
            }

            if (app.Properties.ContainsKey(MiddlewareRegisteredKey))
            {
                return app;
            }

            app.RegisterAutofacLifetimeScopeInjector(container);
            app.Properties.Add(MiddlewareRegisteredKey, true);

            return app;
        }

        /// <summary>
        /// Adds a component to the OWIN pipeline for using Autofac dependency injection with middleware.
        /// </summary>
        /// <param name="app">The application builder.</param>
        /// <param name="container">The Autofac application lifetime scope/container.</param>
        /// <returns>The application builder.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// Thrown if <paramref name="app" /> or <paramref name="container" /> is <see langword="null" />.
        /// </exception>
        public static IAppBuilder UseAutofacMiddleware(this IAppBuilder app, ILifetimeScope container)
        {
            if (app == null)
            {
                throw new ArgumentNullException("app");
            }

            if (container == null)
            {
                throw new ArgumentNullException("container");
            }

            if (app.Properties.ContainsKey(MiddlewareRegisteredKey))
            {
                return app;
            }

            app
                .RegisterAutofacLifetimeScopeInjector(container)
                .UseMiddlewareFromContainer(container);

            app.Properties.Add(MiddlewareRegisteredKey, true);

            return app;
        }

        /// <summary>
        /// Adds a middleware to the OWIN pipeline that will be constructed using Autofac
        /// </summary>
        /// <param name="app">The application builder.</param>
        /// <returns>The application builder.</returns>
        public static IAppBuilder UseMiddlewareFromContainer<T>(this IAppBuilder app)
            where T : OwinMiddleware
        {
            if (app == null)
            {
                throw new ArgumentNullException("app");
            }

            return app.Use<AutofacMiddleware<T>>();
        }

        private static IAppBuilder RegisterAutofacLifetimeScopeInjector(this IAppBuilder app, ILifetimeScope container)
        {
            return app.Use(async (context, next) =>
                {
                    using (var lifetimeScope = container.BeginLifetimeScope(MatchingScopeLifetimeTags.RequestLifetimeScopeTag,
                    b => b.RegisterInstance(context).As<IOwinContext>()))
                    {
                        context.Set(Constants.OwinLifetimeScopeKey, lifetimeScope);
                        await next();
                    }
                });
        }

        private static IAppBuilder UseMiddlewareFromContainer(this IAppBuilder app, IComponentContext container)
        {
            var services = container.ComponentRegistry.Registrations.SelectMany(r => r.Services)
                .OfType<TypedService>()
                .Where(s => s.ServiceType.IsAssignableTo<OwinMiddleware>() && !s.ServiceType.IsAbstract)
                .Select(service => typeof(AutofacMiddleware<>).MakeGenericType(service.ServiceType))
                .Where(serviceType => !container.IsRegistered(serviceType));

            var typedServices = services.ToArray();
            if (!typedServices.Any())
            {
                return app;
            }

            foreach (var typedService in typedServices)
            {
                app.Use(typedService);
            }

            return app;
        }
    }
}
