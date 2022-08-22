// Copyright (c) Autofac Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace Autofac.Integration.Owin.Test;

public class AutofacAppBuilderRunExtensionsFixture
{
    [Fact]
    public void RunFromContainerRequiresInjectorRegistrationFirst()
    {
        var app = new Mock<IAppBuilder>();
        app.Setup(mock => mock.Properties).Returns(new Dictionary<string, object>());
        app.SetReturnsDefault(app.Object);

        Assert.Throws<InvalidOperationException>(() => app.Object.RunFromContainer<ITestComponent>((component, owinContext) => component.InvokeAsync(owinContext)));
    }

    [Fact]
    public async Task RunFromContainerResolvesFromLifetimeScope()
    {
        var instance = new Mock<ITestComponent>();
        instance.Setup(c => c.InvokeAsync(It.IsAny<IOwinContext>())).Returns(Task.FromResult(0)).Verifiable();

        var builder = new ContainerBuilder();
        builder.RegisterInstance(instance.Object);
        using (var server = TestServer.Create(app =>
        {
            app
                .UseAutofacLifetimeScopeInjector(builder.Build())
                .RunFromContainer<ITestComponent>((component, owinContext) => component.InvokeAsync(owinContext));
        }))
        {
            await server.HttpClient.GetAsync("/");
        }

        instance.Verify();
    }

    private interface ITestComponent
    {
        Task InvokeAsync(IOwinContext owinContext);
    }
}
