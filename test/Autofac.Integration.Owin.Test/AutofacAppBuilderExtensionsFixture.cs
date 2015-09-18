using System.Collections.Generic;
using Autofac.Core.Lifetime;
using Microsoft.Owin.Testing;
using Moq;
using Owin;
using Xunit;

namespace Autofac.Integration.Owin.Test
{
    public class AutofacAppBuilderExtensionsFixture
    {
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

            app.Object.UseMiddlewareFromContainer<TestMiddleware>();

            app.Verify(mock => mock.Use(typeof(AutofacMiddleware<TestMiddleware>)), Times.Once);
        }
    }
}
