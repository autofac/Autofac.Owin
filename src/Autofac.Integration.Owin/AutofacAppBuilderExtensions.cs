// Copyright (c) Autofac Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel;
using System.Globalization;
using Autofac;
using Autofac.Core;
using Autofac.Core.Lifetime;
using Autofac.Integration.Owin;
using Autofac.Integration.Owin.Properties;

namespace Owin;

/// <summary>
/// Extension methods for configuring Autofac within the OWIN pipeline.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class AutofacAppBuilderExtensions
{
    /// <summary>
    /// Unique key used to indicate the middleware for injecting the request lifetime scope has been registered with the application.
    /// </summary>
    private static readonly string InjectorRegisteredKey = "AutofacLifetimeScopeInjectorRegistered:" + Constants.AutofacMiddlewareBoundary;

    /// <summary>
    /// Registers a callback to dispose an Autofac <see cref="ILifetimeScope"/>
    /// when the OWIN <c>host.OnAppDisposing</c> event is triggered. This is a
    /// convenience method that will dispose an Autofac container or child scope
    /// when an OWIN application is shutting down.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <param name="lifetimeScope">The Autofac lifetime scope that should be disposed.</param>
    /// <returns>The application builder for continued configuration.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="app" /> or <paramref name="lifetimeScope" /> is <see langword="null" />.
    /// </exception>
    public static IAppBuilder DisposeScopeOnAppDisposing(this IAppBuilder app, ILifetimeScope lifetimeScope)
    {
        if (app == null)
        {
            throw new ArgumentNullException(nameof(app));
        }

        if (lifetimeScope == null)
        {
            throw new ArgumentNullException(nameof(lifetimeScope));
        }

        var context = new OwinContext(app.Properties);
        var token = context.Get<CancellationToken>("host.OnAppDisposing");

        if (token.CanBeCanceled)
        {
            token.Register(lifetimeScope.Dispose);
        }

        return app;
    }

    /// <summary>
    /// Determines if the Autofac lifetime scope injector middleware is
    /// registered with the application pipeline.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <returns>
    /// <see langword="true"/> if the Autofac lifetime scope injector has been registered
    /// with the <paramref name="app"/>; <see langword="false"/> if not.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="app"/> is <see langword="null"/>.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method is useful when composing an application where you may
    /// accidentally register more than one Autofac lifetime scope injector
    /// with the pipeline - for example, accidentally calling both
    /// <see cref="UseAutofacMiddleware(IAppBuilder, ILifetimeScope)"/>
    /// and <see cref="UseAutofacLifetimeScopeInjector(IAppBuilder, ILifetimeScope)"/>
    /// on the same <see cref="IAppBuilder"/>. This allows you to check
    /// an <see cref="IAppBuilder"/> and only add Autofac to the pipeline
    /// if it hasn't already been registered.
    /// </para>
    /// </remarks>
    public static bool IsAutofacLifetimeScopeInjectorRegistered(this IAppBuilder app)
    {
        if (app == null)
        {
            throw new ArgumentNullException(nameof(app));
        }

        return app.Properties.ContainsKey(InjectorRegisteredKey);
    }

    /// <summary>
    /// Adds middleware to inject a request-scoped Autofac lifetime scope into the OWIN pipeline.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <param name="container">The root Autofac application lifetime scope/container.</param>
    /// <returns>The application builder for continued configuration.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="app"/> or <paramref name="container"/> is <see langword="null"/>.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This extension is used when separating the notions of injecting the
    /// lifetime scope and adding middleware to the pipeline from the container.
    /// </para>
    /// <para>
    /// Since middleware registration order matters, generally you want the
    /// Autofac request lifetime scope registered early in the pipeline, but
    /// you may not want the middleware registered with Autofac added to the
    /// pipeline until later.
    /// </para>
    /// <para>
    /// This method gets used in conjunction with <see cref="UseMiddlewareFromContainer{T}(IAppBuilder)"/>.
    /// Do not use this with <see cref="UseAutofacMiddleware(IAppBuilder, ILifetimeScope)"/>
    /// or you'll get unexpected results.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code lang="C#">
    /// app
    ///   .UseAutofacLifetimeScopeInjector(container)
    ///   .UseBasicAuthentication()
    ///   .Use((c, next) =&gt;
    ///   {
    ///     // authorization
    ///     return next();
    ///   })
    ///   .UseMiddlewareFromContainer&lt;PathRewriter&gt;()
    ///   .UseSendFileFallback()
    ///   .UseStaticFiles();
    /// </code>
    /// </example>
    /// <seealso cref="UseMiddlewareFromContainer{T}(IAppBuilder)"/>
    public static IAppBuilder UseAutofacLifetimeScopeInjector(this IAppBuilder app, ILifetimeScope container)
    {
        if (app == null)
        {
            throw new ArgumentNullException(nameof(app));
        }

        if (container == null)
        {
            throw new ArgumentNullException(nameof(container));
        }

        return app.RegisterAutofacLifetimeScopeInjector(container);
    }

    /// <summary>
    /// Adds middleware to inject an externally-defined Autofac lifetime scope into the OWIN pipeline.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <param name="scopeProvider">The delegate to get the scope (either from passed OWIN context or anywhere else).</param>
    /// <returns>The application builder for continued configuration.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="app"/> or <paramref name="scopeProvider"/> is <see langword="null"/>.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This extension is used when separating the notions of injecting the
    /// lifetime scope and adding middleware to the pipeline from the container.
    /// </para>
    /// <para>
    /// Since middleware registration order matters, generally you want the
    /// Autofac request lifetime scope registered early in the pipeline, but
    /// you may not want the middleware registered with Autofac added to the
    /// pipeline until later.
    /// </para>
    /// <para>
    /// This method won't add any disposal of passed lifetime scope;
    /// the caller is responsible for disposing it at the appropriate moment.
    /// </para>
    /// <para>
    /// This method gets used in conjunction with <see cref="UseMiddlewareFromContainer{T}(IAppBuilder)"/>.
    /// Do not use this with <see cref="UseAutofacMiddleware(IAppBuilder, ILifetimeScope)"/>
    /// or you'll get unexpected results.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code lang="C#">
    /// app
    ///   .UseAutofacLifetimeScopeInjector(c => GetSomeExternallyDefinedLifetimeScope(c))
    ///   .UseBasicAuthentication()
    ///   .Use((c, next) =&gt;
    ///   {
    ///     //authorization
    ///     return next();
    ///   })
    ///   .UseMiddlewareFromContainer&lt;PathRewriter&gt;()
    ///   .UseSendFileFallback()
    ///   .UseStaticFiles();
    /// </code>
    /// </example>
    /// <seealso cref="UseMiddlewareFromContainer{T}(IAppBuilder)"/>
    public static IAppBuilder UseAutofacLifetimeScopeInjector(this IAppBuilder app, Func<IOwinContext, ILifetimeScope> scopeProvider)
    {
        if (app == null)
        {
            throw new ArgumentNullException(nameof(app));
        }

        if (scopeProvider == null)
        {
            throw new ArgumentNullException(nameof(scopeProvider));
        }

        return app.RegisterAutofacLifetimeScopeInjector(scopeProvider, false);
    }

    /// <summary>
    /// Adds middleware to both inject a request-scoped Autofac lifetime scope into the OWIN pipeline
    /// as well as add all middleware components registered with Autofac.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <param name="container">The root Autofac application lifetime scope/container.</param>
    /// <returns>The application builder for continued configuration.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="app"/> or <paramref name="container"/> is <see langword="null"/>.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This extension registers the Autofac lifetime scope and all Autofac-registered
    /// middleware into the application at the same time. This is the simplest
    /// way to integrate Autofac into OWIN but has the least control over
    /// pipeline construction.
    /// </para>
    /// <para>
    /// Do not use this with <see cref="UseAutofacLifetimeScopeInjector(IAppBuilder, ILifetimeScope)"/>
    /// or <see cref="UseMiddlewareFromContainer{T}(IAppBuilder)"/>
    /// or you'll get unexpected results.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code lang="C#">
    /// app
    ///   .UseAutofacMiddleware(container)
    ///   .UseBasicAuthentication()
    ///   .Use((c, next) =&gt;
    ///   {
    ///     //authorization
    ///     return next();
    ///   })
    ///   .UseSendFileFallback()
    ///   .UseStaticFiles();
    /// </code>
    /// </example>
    public static IAppBuilder UseAutofacMiddleware(this IAppBuilder app, ILifetimeScope container)
    {
        if (app == null)
        {
            throw new ArgumentNullException(nameof(app));
        }

        if (container == null)
        {
            throw new ArgumentNullException(nameof(container));
        }

        return app
            .RegisterAutofacLifetimeScopeInjector(container)
            .UseAllMiddlewareRegisteredInContainer(container);
    }

    /// <summary>
    /// Adds a middleware to the OWIN pipeline that will be constructed using Autofac.
    /// </summary>
    /// <typeparam name="T">The type of middleware to inject.</typeparam>
    /// <param name="app">The application builder.</param>
    /// <returns>The application builder for continued configuration.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="app"/> is <see langword="null"/>.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This extension is used when separating the notions of injecting the
    /// lifetime scope and adding middleware to the pipeline from the container.
    /// </para>
    /// <para>
    /// Since middleware registration order matters, generally you want the
    /// Autofac request lifetime scope registered early in the pipeline, but
    /// you may not want the middleware registered with Autofac added to the
    /// pipeline until later.
    /// </para>
    /// <para>
    /// This method gets used in conjunction with <see cref="UseAutofacLifetimeScopeInjector(IAppBuilder, ILifetimeScope)"/>.
    /// Do not use this with <see cref="UseAutofacMiddleware(IAppBuilder, ILifetimeScope)"/>
    /// or you'll get unexpected results.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code lang="C#">
    /// app
    ///   .UseAutofacLifetimeScopeInjector(container)
    ///   .UseBasicAuthentication()
    ///   .Use((c, next) =&gt;
    ///   {
    ///     //authorization
    ///     return next();
    ///   })
    ///   .UseMiddlewareFromContainer&lt;PathRewriter&gt;()
    ///   .UseSendFileFallback()
    ///   .UseStaticFiles();
    /// </code>
    /// </example>
    /// <seealso cref="UseAutofacLifetimeScopeInjector(IAppBuilder, ILifetimeScope)"/>
    public static IAppBuilder UseMiddlewareFromContainer<T>(this IAppBuilder app)
        where T : OwinMiddleware
    {
        if (app == null)
        {
            throw new ArgumentNullException(nameof(app));
        }

        if (!app.IsAutofacLifetimeScopeInjectorRegistered())
        {
            throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, Resources.LifetimeScopeInjectorNotFoundWhileRegisteringMiddleware, typeof(T)));
        }

        return app.Use<AutofacMiddleware<T>>();
    }

    /// <summary>
    /// Locates all explicitly registered middleware instances and generates the list of
    /// corresponding <see cref="AutofacMiddleware{T}"/> types that should be inserted
    /// into the application pipeline.
    /// </summary>
    /// <param name="container">
    /// The <see cref="IComponentContext"/> containing registrations to convert to middleware.
    /// </param>
    /// <returns>
    /// An <see cref="IEnumerable{T}"/> of <see cref="AutofacMiddleware{T}"/> wrapped around
    /// registered middleware types.
    /// </returns>
    internal static IEnumerable<Type> GenerateAllAutofacMiddleware(IComponentContext container)
    {
        return container.ComponentRegistry.Registrations.SelectMany(r => r.Services)
            .OfType<TypedService>()
            .Where(s => IsMiddlewareButNotAutofac(s.ServiceType))
            .Select(service => typeof(AutofacMiddleware<>).MakeGenericType(service.ServiceType))
            .ToArray();
    }

    /// <summary>
    /// Determines whether a type is middleware we should wrap.
    /// </summary>
    /// <param name="typeToCheck">The type to check.</param>
    /// <returns>
    /// <see langword="true" /> if the type is middleware, not abstract, and not
    /// already wrapped; otherwise <see langword="false" />.
    /// </returns>
    private static bool IsMiddlewareButNotAutofac(Type typeToCheck)
    {
        return typeToCheck.IsAssignableTo<OwinMiddleware>() &&
            !typeToCheck.IsAbstract &&
            !(typeToCheck.IsGenericType && typeToCheck.GetGenericTypeDefinition() == typeof(AutofacMiddleware<>));
    }

    private static IAppBuilder RegisterAutofacLifetimeScopeInjector(this IAppBuilder app, ILifetimeScope container)
    {
        return app
            .RegisterAutofacLifetimeScopeInjector(
                context => container.BeginLifetimeScope(MatchingScopeLifetimeTags.RequestLifetimeScopeTag, b => b.RegisterInstance(context).As<IOwinContext>()),
                true);
    }

    private static IAppBuilder RegisterAutofacLifetimeScopeInjector(this IAppBuilder app, Func<IOwinContext, ILifetimeScope> scopeProvider, bool dispose)
    {
        app.Use(async (context, next) =>
        {
            if (context.GetAutofacLifetimeScope() != null)
            {
                await next();
                return;
            }

            var lifetimeScope = scopeProvider(context);
            try
            {
                context.SetAutofacLifetimeScope(lifetimeScope);
                await next();
            }
            finally
            {
                if (dispose && lifetimeScope != null)
                {
                    await lifetimeScope.DisposeAsync();
                }

                context.RemoveAutofacLifetimeScope();
            }
        });

        app.Properties[InjectorRegisteredKey] = true;
        return app;
    }

    private static IAppBuilder UseAllMiddlewareRegisteredInContainer(this IAppBuilder app, IComponentContext container)
    {
        var typedServices = GenerateAllAutofacMiddleware(container);
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
