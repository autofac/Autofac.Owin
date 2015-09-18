using System.Collections.Generic;
using Autofac.Core.Lifetime;
using Microsoft.Owin.Testing;
using Moq;
using NUnit.Framework;
using Owin;

namespace Autofac.Integration.Owin.Test
{
    [TestFixture]
    public class AutofacAppBuilderExtensionsFixture
    {
        [Test]
        public void UseAutofacLifetimeScopeInjectorAddsChildLifetimeScopeToOwinContext()
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
                server.HttpClient.GetAsync("/").Wait();
                Assert.That(TestMiddleware.LifetimeScope.Tag, Is.EqualTo(MatchingScopeLifetimeTags.RequestLifetimeScopeTag));
            }
        }

        [Test]
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

        [Test]
        public void UseAutofacMiddlewareAddsChildLifetimeScopeToOwinContext()
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
                server.HttpClient.GetAsync("/").Wait();
                Assert.That(TestMiddleware.LifetimeScope.Tag, Is.EqualTo(MatchingScopeLifetimeTags.RequestLifetimeScopeTag));
            }
        }

        [Test]
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

        [Test]
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
