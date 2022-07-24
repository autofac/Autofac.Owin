// Copyright (c) Autofac Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel;
using System.Globalization;
using Autofac;
using Autofac.Integration.Owin;
using Autofac.Integration.Owin.Properties;

namespace Owin;

/// <summary>
/// Extension methods for running (terminal) delegates with Autofac DI within the OWIN pipeline.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class AutofacAppBuilderRunExtensions
{
    /// <summary>
    /// Adds a middleware to the OWIN pipeline that will be constructed using Autofac and that does not have next middleware.
    /// </summary>
    /// <typeparam name="T">The type of middleware to inject.</typeparam>
    /// <param name="app">The application builder.</param>
    /// <param name="handler">The handler to invoke. The constructed middleware will be available as a parameter.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="app"/> or <paramref name="handler"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown if lifetime scope injector not registered in pipeline.
    /// </exception>
    /// <example>
    /// <code lang="C#">
    /// app
    ///   .UseAutofacLifetimeScopeInjector(container)
    ///   .RunFromContainer&lt;Uploader&gt;((uploader, owinContext) =&gt; uploader.InvokeAsync(owinContext));
    /// </code>
    /// </example>
    /// <seealso cref="AutofacAppBuilderExtensions.UseAutofacLifetimeScopeInjector(IAppBuilder, ILifetimeScope)"/>
    public static void RunFromContainer<T>(this IAppBuilder app, Func<T, IOwinContext, Task> handler)
        where T : notnull
    {
        if (app == null)
        {
            throw new ArgumentNullException(nameof(app));
        }

        if (handler == null)
        {
            throw new ArgumentNullException(nameof(handler));
        }

        if (!app.IsAutofacLifetimeScopeInjectorRegistered())
        {
            throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, Resources.LifetimeScopeInjectorNotFoundWhileRegisteringMiddleware, typeof(T)));
        }

        app
            .Run(context =>
            {
                if (context == null)
                {
                    throw new ArgumentNullException(nameof(context));
                }

                return handler(context.GetAutofacLifetimeScope().Resolve<T>(), context);
            });
    }
}
