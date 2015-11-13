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
using System.Globalization;
using System.Threading.Tasks;
using Autofac;
using Autofac.Integration.Owin;
using Autofac.Integration.Owin.Properties;
using Microsoft.Owin;

namespace Owin
{
    /// <summary>
    /// Extension methods for running (terminal) delegates with Autofac DI within the OWIN pipeline.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class AutofacAppBuilderRunExtensions
    {
        /// <summary>
        /// Adds a middleware to the OWIN pipeline that will be constructed using Autofac and that does not have next middleware
        /// </summary>
        /// <param name="app">The application builder.</param>
        /// <param name="handler">The handler to invoke. The constructed middleware will be available as a parameter.</param>
        /// <exception cref="System.ArgumentNullException">
        /// Thrown if <paramref name="app"/> is <see langword="null"/>.
        /// </exception>
        /// <example>
        /// <code lang="C#">
        /// app
        ///   .UseAutofacLifetimeScopeInjector(container)
        ///   .Run&lt;Uploader&gt;((uploader, owinContext) =&gt; uploader.InvokeAsync(owinContext));
        /// </code>
        /// </example>
        /// <seealso cref="AutofacAppBuilderExtensions.UseAutofacLifetimeScopeInjector(IAppBuilder, ILifetimeScope)"/>
        public static void RunFromContainer<T>(this IAppBuilder app, Func<T,IOwinContext,Task> handler)
        {
            if (app == null)
            {
                throw new ArgumentNullException("app");
            }

            if (!app.IsAutofacLifetimeScopeInjectorRegistered())
            {
                throw new InvalidOperationException(String.Format(CultureInfo.InvariantCulture, Resources.LifetimeScopeInjectorNotFoundWhileRegisteringMiddleware, typeof(T)));
            }

            app
                .Run(context =>
                {
                    if (context == null)
                        throw new ArgumentNullException("context");

                    return handler(context.GetAutofacLifetimeScope().Resolve<T>(), context);
                });
        }
    }
}
