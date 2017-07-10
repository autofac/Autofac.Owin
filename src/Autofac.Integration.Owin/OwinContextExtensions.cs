using System;
using Microsoft.Owin;

namespace Autofac.Integration.Owin
{
    /// <summary>
    /// Extension methods for using Autofac within an OWIN context.
    /// </summary>
    public static class OwinContextExtensions
    {
        /// <summary>
        /// Gets the current Autofac lifetime scope from the OWIN context.
        /// </summary>
        /// <param name="context">The OWIN context.</param>
        /// <returns>The current lifetime scope.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// Thrown if <paramref name="context" /> is <see langword="null" />.
        /// </exception>
        public static ILifetimeScope GetAutofacLifetimeScope(this IOwinContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            return context.Get<ILifetimeScope>(Constants.OwinLifetimeScopeKey);
        }

        /// <summary>
        /// Sets the current Autofac lifetime scope to the OWIN context.
        /// </summary>
        /// <param name="context">The OWIN context.</param>
        /// <param name="scope">The current lifetime scope.</param>
        /// <exception cref="System.ArgumentNullException">
        /// Thrown if <paramref name="context" /> or <paramref name="scope" /> is <see langword="null" />.
        /// </exception>
        /// <remarks>The caller is responsible for the appropriate disposal of the passed <see cref="ILifetimeScope"/>.</remarks>
        public static void SetAutofacLifetimeScope(this IOwinContext context, ILifetimeScope scope)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (scope == null)
            {
                throw new ArgumentNullException(nameof(scope));
            }

            context.Set(Constants.OwinLifetimeScopeKey, scope);
        }

    }
}
