// Copyright (c) Autofac Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Globalization;
using Autofac.Core;
using Autofac.Integration.Owin.Properties;

namespace Autofac.Integration.Owin
{
    /// <summary>
    /// Wrapper around an <see cref="OwinMiddleware"/> that handles Autofac resolution for that middleware.
    /// </summary>
    /// <typeparam name="T">The type of middleware the wrapper handles.</typeparam>
    internal class AutofacMiddleware<T> : OwinMiddleware
        where T : OwinMiddleware
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AutofacMiddleware{T}"/> class.
        /// </summary>
        /// <param name="next">The next middleware to invoke when this one is complete.</param>
        public AutofacMiddleware(OwinMiddleware next)
            : base(next)
        {
        }

        /// <inheritdoc />
        public override Task Invoke(IOwinContext context)
        {
            var lifetimeScope = context.GetAutofacLifetimeScope();
            if (lifetimeScope == null)
            {
                // We pretty well protect against this, but just in case
                // someone's trying to pull a fast one...
                throw new InvalidOperationException(String.Format(CultureInfo.InvariantCulture, Resources.LifetimeScopeNotFoundWhileInjectingMiddleware, typeof(T)));
            }

            T middleware;
            try
            {
                middleware = lifetimeScope.Resolve<T>(TypedParameter.From(this.Next));
            }
            catch (DependencyResolutionException ex)
            {
                throw new InvalidOperationException(String.Format(CultureInfo.InvariantCulture, Resources.MiddlewareNotRegistered, typeof(T)), ex);
            }

            return middleware.Invoke(context);
        }
    }
}
