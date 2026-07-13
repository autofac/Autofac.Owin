// Copyright (c) Autofac Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace Autofac.Integration.Owin.Test;

public class OwinContextExtensionsFixture
{
    [Fact]
    public void GetAutofacLifetimeScopeReturnsInstanceFromContext()
    {
        var context = Substitute.For<IOwinContext>();
        context.GetAutofacLifetimeScope();
        context.Received().Get<ILifetimeScope>(Constants.OwinLifetimeScopeKey);
    }

    [Fact]
    public void GetAutofacLifetimeScopeThrowsWhenProvidedNullInstance()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => OwinContextExtensions.GetAutofacLifetimeScope(null!));
        Assert.Equal("context", exception.ParamName);
    }

    [Fact]
    public void RemoveAutofacLifetimeScopeThrowsWhenProvidedNullInstance()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => OwinContextExtensions.RemoveAutofacLifetimeScope(null!));
        Assert.Equal("context", exception.ParamName);
    }

    [Fact]
    public void SetAutofacLifetimeScopeSetsInstanceToContext()
    {
        var instance = Substitute.For<ILifetimeScope>();

        var context = Substitute.For<IOwinContext>();
        context.SetAutofacLifetimeScope(instance);
        context.Received().Set(Constants.OwinLifetimeScopeKey, instance);
    }

    [Fact]
    public void SetAutofacLifetimeScopeThrowsWhenProvidedNullContextInstance()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => OwinContextExtensions.SetAutofacLifetimeScope(null!, Substitute.For<ILifetimeScope>()));
        Assert.Equal("context", exception.ParamName);
    }

    [Fact]
    public void SetAutofacLifetimeScopeThrowsWhenProvidedNullScopeInstance()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => OwinContextExtensions.SetAutofacLifetimeScope(new OwinContext(), null!));
        Assert.Equal("scope", exception.ParamName);
    }

    [Fact]
    public void GetAutofacLifetimeScopeReturnsTheInstanceFromSetLifetimeScope()
    {
        var instance = Substitute.For<ILifetimeScope>();
        var context = new OwinContext();
        context.SetAutofacLifetimeScope(instance);
        Assert.Same(instance, context.GetAutofacLifetimeScope());
    }

    [Fact]
    public void RemoveAutofacLifetimeScopeRemovesScopeFromContext()
    {
        var scope = Substitute.For<ILifetimeScope>();
        var context = Substitute.For<IOwinContext>();
        var environment = Substitute.For<IDictionary<string, object>>();

        context.Environment.Returns(environment);
        context.SetAutofacLifetimeScope(scope);
        context.RemoveAutofacLifetimeScope();

        context.Received().Set(Constants.OwinLifetimeScopeKey, scope);
        environment.Received().Remove(Constants.OwinLifetimeScopeKey);
    }

    [Fact]
    public void RemoveAutofacLifetimeScopeDoesNotThrowIfScopeNotPresent()
    {
        var context = new OwinContext();
        var exception = Record.Exception(() => context.RemoveAutofacLifetimeScope());
        Assert.Null(exception);
    }

    [Fact]
    public async Task ScopeSetBySetAutofacLifetimeScopeIsNotDisposed()
    {
        var lifetimeScope = Substitute.For<ILifetimeScope>();
        using (var server = TestServer.Create(app =>
        {
            app.Use((ctx, next) =>
            {
                ctx.SetAutofacLifetimeScope(lifetimeScope);
                return next();
            });
            app.Run(context => context.Response.WriteAsync("Hello, world!"));
        }))
        {
            await server.HttpClient.GetAsync("/");
        }

        lifetimeScope.DidNotReceive().Dispose();
    }
}
