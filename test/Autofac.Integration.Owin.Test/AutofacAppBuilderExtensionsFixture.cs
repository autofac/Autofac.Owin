using System;
using System.Collections.Generic;
using System.Threading;
using Autofac.Core.Lifetime;
using Autofac.Core.Registration;
using Microsoft.Owin.Builder;
using Microsoft.Owin.Testing;
using Moq;
using Owin;
using Xunit;

namespace Autofac.Integration.Owin.Test
{
    public class AutofacAppBuilderExtensionsFixture
    {
        [Fact]
        public void DisposeScopeOnAppDisposing()
        {
            var app = new AppBuilder();
            var tcs = new CancellationTokenSource();
            var scope = new TestableLifetimeScope();
            app.Properties.Add("host.OnAppDisposing", tcs.Token);

            app.DisposeScopeOnAppDisposing(scope);

            tcs.Cancel();

            Assert.True(scope.ScopeIsDisposed);
        }

        [Fact]
        public void DisposeScopeOnAppDisposingDoesNothingWhenNoTokenPresent()
        {
            var app = new AppBuilder();
            var scope = new TestableLifetimeScope();

            // XUnit doesn't have Assert.DoesNotThrow
            app.DisposeScopeOnAppDisposing(scope);
        }

        [Fact]
        public void DisposeScopeOnAppDisposingLifetimeScopeRequired()
        {
            var app = new AppBuilder();
            Assert.Throws<ArgumentNullException>(() => app.DisposeScopeOnAppDisposing(null));
        }

        [Fact]
        public void DisposeScopeOnAppDisposingAppBuildRequired()
        {
            var app = (IAppBuilder)null;
            Assert.Throws<ArgumentNullException>(() => app.DisposeScopeOnAppDisposing(new TestableLifetimeScope()));
        }

        [Fact]
        public async void UseAutofacLifetimeScopeInjectorAddsChildLifetimeScopeToOwinContext()
        {
            var builder = new ContainerBuilder();
            builder.RegisterType<TestMiddleware>();
            var container = builder.Build();

            using (var server = TestServer.Create(app =>
                {
                    app.UseAutofacLifetimeScopeInjector(container);
                    app.Use<TestMiddleware>();
                    app.Run(context => context.Response.WriteAsync("Hello, world!"));
                }))
            {
                await server.HttpClient.GetAsync("/");
                Assert.Equal(MatchingScopeLifetimeTags.RequestLifetimeScopeTag, TestMiddleware.LifetimeScope.Tag);
            }
        }

        [Fact]
        public void UseAutofacLifetimeScopeInjectorDoesntAddWrappedMiddlewareInstancesToAppBuilder()
        {
            var builder = new ContainerBuilder();
            builder.RegisterType<TestMiddleware>();
            var container = builder.Build();
            var app = new Mock<IAppBuilder>();
            app.Setup(mock => mock.Properties).Returns(new Dictionary<string, object>());
            app.SetReturnsDefault(app.Object);

            app.Object.UseAutofacLifetimeScopeInjector(container);

            app.Verify(mock => mock.Use(It.IsAny<AutofacMiddleware<TestMiddleware>>(), It.IsAny<object[]>()), Times.Never);
        }

        [Fact]
        public void UseAutofacLifetimeScopeInjectorShowsInjectorRegistered()
        {
            var app = new Mock<IAppBuilder>();
            app.Setup(mock => mock.Properties).Returns(new Dictionary<string, object>());
            app.SetReturnsDefault(app.Object);

            var container = new ContainerBuilder().Build();
            app.Object.UseAutofacLifetimeScopeInjector(container);
            Assert.True(app.Object.IsAutofacLifetimeScopeInjectorRegistered());
        }

        [Fact]
        public async void UseAutofacMiddlewareAddsChildLifetimeScopeToOwinContext()
        {
            var builder = new ContainerBuilder();
            builder.RegisterType<TestMiddleware>();
            var container = builder.Build();

            using (var server = TestServer.Create(app =>
                {
                    app.UseAutofacMiddleware(container);
                    app.Run(context => context.Response.WriteAsync("Hello, world!"));
                }))
            {
                await server.HttpClient.GetAsync("/");
                Assert.Equal(MatchingScopeLifetimeTags.RequestLifetimeScopeTag, TestMiddleware.LifetimeScope.Tag);
            }
        }

        [Fact]
        public void UseAutofacMiddlewareAddsWrappedMiddlewareInstancesToAppBuilder()
        {
            var builder = new ContainerBuilder();
            builder.RegisterType<TestMiddleware>();
            var container = builder.Build();
            var app = new Mock<IAppBuilder>();
            app.Setup(mock => mock.Properties).Returns(new Dictionary<string, object>());
            app.Setup(mock => mock.Use(typeof(AutofacMiddleware<TestMiddleware>)));
            app.SetReturnsDefault(app.Object);

            app.Object.UseAutofacMiddleware(container);

            app.VerifyAll();
        }

        [Fact]
        public void UseAutofacMiddlewareShowsInjectorRegistered()
        {
            var app = new Mock<IAppBuilder>();
            app.Setup(mock => mock.Properties).Returns(new Dictionary<string, object>());
            app.SetReturnsDefault(app.Object);

            var container = new ContainerBuilder().Build();
            app.Object.UseAutofacMiddleware(container);
            Assert.True(app.Object.IsAutofacLifetimeScopeInjectorRegistered());
        }

        [Fact]
        public void UseMiddlewareFromContainerAddsSingleWrappedMiddlewareInstanceToAppBuilder()
        {
            var app = new Mock<IAppBuilder>();
            app.Setup(mock => mock.Properties).Returns(new Dictionary<string, object>());
            app.SetReturnsDefault(app.Object);

            var container = new ContainerBuilder().Build();
            app.Object.UseAutofacLifetimeScopeInjector(container);
            app.Object.UseMiddlewareFromContainer<TestMiddleware>();

            app.Verify(mock => mock.Use(typeof(AutofacMiddleware<TestMiddleware>)), Times.Once);
        }

        [Fact]
        public void UseMiddlewareFromContainerRequiresInjectorRegistrationFirst()
        {
            var app = new Mock<IAppBuilder>();
            app.Setup(mock => mock.Properties).Returns(new Dictionary<string, object>());
            app.SetReturnsDefault(app.Object);

            Assert.Throws<InvalidOperationException>(() => app.Object.UseMiddlewareFromContainer<TestMiddleware>());
        }

        class TestableLifetimeScope : LifetimeScope
        {
            public bool ScopeIsDisposed { get; set; }

            public TestableLifetimeScope()
                : base(new ComponentRegistry())
            {
            }

            protected override void Dispose(bool disposing)
            {
                base.Dispose(disposing);
                this.ScopeIsDisposed = true;
            }
        }
    }
}
