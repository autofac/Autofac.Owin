// Copyright (c) Autofac Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace Autofac.Integration.Owin.Test;

public class AutofacAppBuilderRunExtensionsFixture
{
    [Fact]
    public void RunFromContainerRequiresInjectorRegistrationFirst()
    {
        var app = Substitute.For<IAppBuilder>();
        app.Properties.Returns(new Dictionary<string, object>());
        app.Use(Arg.Any<object>(), Arg.Any<object[]>()).Returns(app);

        Assert.Throws<InvalidOperationException>(() => app.RunFromContainer<ITestComponent>((component, owinContext) => component.InvokeAsync(owinContext)));
    }

    [Fact]
    public async Task RunFromContainerResolvesFromLifetimeScope()
    {
        var instance = Substitute.For<ITestComponent>();
        instance.InvokeAsync(Arg.Any<IOwinContext>()).Returns(Task.FromResult(0));

        var builder = new ContainerBuilder();
        builder.RegisterInstance(instance);
        using (var server = TestServer.Create(app =>
        {
            app
                .UseAutofacLifetimeScopeInjector(builder.Build())
                .RunFromContainer<ITestComponent>((component, owinContext) => component.InvokeAsync(owinContext));
        }))
        {
            await server.HttpClient.GetAsync("/");
        }

        await instance.Received(1).InvokeAsync(Arg.Any<IOwinContext>());
    }

    internal interface ITestComponent
    {
        Task InvokeAsync(IOwinContext owinContext);
    }
}
