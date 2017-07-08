﻿using System;
using Microsoft.Owin;
using Microsoft.Owin.Testing;
using Moq;
using Owin;
using Xunit;

namespace Autofac.Integration.Owin.Test
{
    public class OwinContextExtensionsFixture
    {
        [Fact]
        public void GetAutofacLifetimeScopeReturnsInstanceFromContext()
        {
            var context = new Mock<IOwinContext>();
            context.Setup(mock => mock.Get<ILifetimeScope>(Constants.OwinLifetimeScopeKey));
            context.Object.GetAutofacLifetimeScope();
            context.VerifyAll();
        }

        [Fact]
        public void GetAutofacLifetimeScopeThrowsWhenProvidedNullInstance()
        {
            var exception = Assert.Throws<ArgumentNullException>(() => OwinContextExtensions.GetAutofacLifetimeScope(null));
            Assert.Equal("context", exception.ParamName);
        }

        [Fact]
        public void SetAutofacLifetimeScopeSetsInstanceToContext()
        {
            var instance = new Mock<ILifetimeScope>();

            var context = new Mock<IOwinContext>();
            context.Setup(mock => mock.Set(Constants.OwinLifetimeScopeKey, instance.Object));
            context.Object.SetAutofacLifetimeScope(instance.Object);
            context.VerifyAll();
        }

        [Fact]
        public void SetAutofacLifetimeScopeThrowsWhenProvidedNullContextInstance()
        {
            var exception = Assert.Throws<ArgumentNullException>(() => OwinContextExtensions.SetAutofacLifetimeScope(null, new Mock<ILifetimeScope>().Object));
            Assert.Equal("context", exception.ParamName);
        }

        [Fact]
        public void SetAutofacLifetimeScopeThrowsWhenProvidedNullScopeInstance()
        {
            var exception = Assert.Throws<ArgumentNullException>(() => OwinContextExtensions.SetAutofacLifetimeScope(new OwinContext(), null));
            Assert.Equal("scope", exception.ParamName);
        }

        [Fact]
        public void GetAutofacLifetimeScopeReturnsTheInstanceFromSetLifetimeScope()
        {
            var instance = new Mock<ILifetimeScope>().Object;

            var context = new OwinContext();
            context.SetAutofacLifetimeScope(instance);
            Assert.Same(instance, context.GetAutofacLifetimeScope());
        }

        [Fact]
        public async void ScopeSetBySetAutofacLifetimeScopeIsntDisposed()
        {
            var lifetimeScope = new Mock<ILifetimeScope>();
            using (var server = TestServer.Create(app =>
            {
                app.Use((ctx, next) =>
                {
                    ctx.SetAutofacLifetimeScope(lifetimeScope.Object);
                    return next();
                });
                app.Run(context => context.Response.WriteAsync("Hello, world!"));
            }))
            {
                await server.HttpClient.GetAsync("/");
            }
            lifetimeScope.Verify(s => s.Dispose(), Times.Never);
        }
    }
}
