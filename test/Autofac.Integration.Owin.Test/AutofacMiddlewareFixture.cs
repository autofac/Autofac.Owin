using System;
using Microsoft.Owin.Testing;
using Owin;
using Xunit;

namespace Autofac.Integration.Owin.Test
{
    public class AutofacMiddlewareFixture
    {
        [Fact]
        public async void MiddlewareMustBeRegistered()
        {
            var builder = new ContainerBuilder();
            var container = builder.Build();

            using (var server = TestServer.Create(app =>
                {
                    app.UseAutofacLifetimeScopeInjector(container);
                    app.UseMiddlewareFromContainer<TestMiddleware>();
                    app.Run(context => context.Response.WriteAsync("Hello, world!"));
                }))
            {
                await Assert.ThrowsAsync<InvalidOperationException>(() => server.HttpClient.GetAsync("/"));
            }
        }
    }
}
