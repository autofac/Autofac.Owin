// Copyright (c) Autofac Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Diagnostics.CodeAnalysis;
using Autofac.Core;
using Autofac.Core.Lifetime;
using Autofac.Core.Resolving;
using Autofac.Features.ResolveAnything;
using Autofac.Util;
using Microsoft.Owin.Builder;

namespace Autofac.Integration.Owin.Test;

public class AutofacAppBuilderExtensionsFixture
{
    [Fact]
    public void DisposeScopeOnAppDisposing()
    {
        var app = new AppBuilder();
        var tcs = new CancellationTokenSource();
        using var scope = new TestableLifetimeScope();
        app.Properties.Add("host.OnAppDisposing", tcs.Token);

        app.DisposeScopeOnAppDisposing(scope);

        tcs.Cancel();

        Assert.True(scope.ScopeIsDisposed);
    }

    [Fact]
    public void DisposeScopeOnAppDisposingDoesNothingWhenNoTokenPresent()
    {
        var app = new AppBuilder();
        using var scope = new TestableLifetimeScope();

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
    public void GenerateAllAutofacMiddleware_CreatesOnlyRegisteredMiddlewareWithACTNARS()
    {
        // Issue #9: ACTNARS causes the list of registered middleware to fail during generation.
        var builder = new ContainerBuilder();
        builder.RegisterSource(new AnyConcreteTypeNotAlreadyRegisteredSource());
        builder.RegisterType<TestMiddleware>();
        var container = builder.Build();

        var middlewareTypes = AutofacAppBuilderExtensions.GenerateAllAutofacMiddleware(container).ToArray();
        Assert.Single(middlewareTypes);
        Assert.Contains(typeof(AutofacMiddleware<TestMiddleware>), middlewareTypes);
    }

    [Fact]
    public void GenerateAllAutofacMiddleware_CreatesRegisteredMiddleware()
    {
        var builder = new ContainerBuilder();
        builder.RegisterType<TestMiddleware>();
        var container = builder.Build();

        var middlewareTypes = AutofacAppBuilderExtensions.GenerateAllAutofacMiddleware(container).ToArray();
        Assert.Single(middlewareTypes);
        Assert.Contains(typeof(AutofacMiddleware<TestMiddleware>), middlewareTypes);
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
    public async void UseAutofacLifetimeScopeInjectorDoesntOverrideScopeSetBySetAutofacLifetimeScope()
    {
        using var lifetimeScope = new TestableLifetimeScope();
        using (var server = TestServer.Create(app =>
        {
            app.Use((ctx, next) =>
            {
                ctx.SetAutofacLifetimeScope(lifetimeScope);
                return next();
            });

            // We don't expect anything to be called on this one, so we want it to fail.
            var strictScope = Substitute.For<ILifetimeScope>();
            app.UseAutofacLifetimeScopeInjector(strictScope);
            app.Use<TestMiddleware>();
            app.Run(context => context.Response.WriteAsync("Hello, world!"));
        }))
        {
            await server.HttpClient.GetAsync("/");
            Assert.Same(lifetimeScope, TestMiddleware.LifetimeScope);
        }
    }

    [Fact]
    public async void UseAutofacLifetimeScopeInjectorDoesntDisposeScopeSetBySetAutofacLifetimeScope()
    {
        using var lifetimeScope = new TestableLifetimeScope();
        using (var server = TestServer.Create(app =>
        {
            app.Use((ctx, next) =>
            {
                ctx.SetAutofacLifetimeScope(lifetimeScope);
                return next();
            });
            var strictScope = Substitute.For<ILifetimeScope>();
            app.UseAutofacLifetimeScopeInjector(strictScope);
            app.Use<TestMiddleware>();
            app.Run(context => context.Response.WriteAsync("Hello, world!"));
        }))
        {
            await server.HttpClient.GetAsync("/");
        }

        Assert.False(lifetimeScope.ScopeIsDisposed);
    }

    [Fact]
    public async void UseAutofacLifetimeScopeInjectorDoesntAddLifetimeScopeToOwinContextIfAlreadyPresent()
    {
        var container = new ContainerBuilder().Build();

        using (var server = TestServer.Create(app =>
        {
            app.UseAutofacLifetimeScopeInjector(container);

            // We don't expect anything to be called on this one, so we want it to fail.
            var strictScope = Substitute.For<ILifetimeScope>();
            app.UseAutofacLifetimeScopeInjector(strictScope);
            app.Run(context => context.Response.WriteAsync("Hello, world!"));
        }))
        {
            await server.HttpClient.GetAsync("/");
        }
    }

    [Fact]
    public async void UseAutofacLifetimeScopeInjectorDisposesIt()
    {
        var container = new ContainerBuilder().Build();

        var disposable = Substitute.For<IDisposable>();
        var asyncDisposable = Substitute.For<IAsyncDisposable>();

        using (var server = TestServer.Create(app =>
        {
            app.UseAutofacLifetimeScopeInjector(container);
            app.Use((ctx, next) =>
            {
                var disposer = ctx.GetAutofacLifetimeScope().Disposer;

                disposer.AddInstanceForDisposal(disposable);
                disposer.AddInstanceForAsyncDisposal(asyncDisposable);

                return next();
            });
            app.Run(context => context.Response.WriteAsync("Hello, world!"));
        }))
        {
            await server.HttpClient.GetAsync("/");
        }

        disposable.Received().Dispose();
        await asyncDisposable.Received().DisposeAsync();
    }

    [Fact]
    public async void RemoveAutofacLifetimeScopeAfterUse()
    {
        var builder = new ContainerBuilder();
        builder.RegisterType<TestMiddleware>();
        var container = builder.Build();

        using (var server = TestServer.Create(app =>
        {
            app.Use((ctx, next) =>
            {
                try
                {
                    return next();
                }
                catch
                {
                    Assert.False(ctx.Environment.ContainsKey(Constants.OwinLifetimeScopeKey));
                    throw;
                }
            });
            app.UseAutofacLifetimeScopeInjector(container);
            app.Use<TestMiddleware>();
            app.Use((ctx, next) =>
            {
                Assert.True(ctx.Environment.ContainsKey(Constants.OwinLifetimeScopeKey));
                return next();
            });
        }))
        {
            await server.HttpClient.GetAsync("/");
        }
    }

    [Fact]
    public async void RemoveAutofacLifetimeScopeAfterUseWhenExceptionThrown()
    {
        var builder = new ContainerBuilder();
        builder.RegisterType<TestMiddleware>();
        var container = builder.Build();

        using (var server = TestServer.Create(app =>
        {
            app.Use((ctx, next) =>
            {
                var nextInvoke = next();
                Assert.False(ctx.Environment.ContainsKey(Constants.OwinLifetimeScopeKey));
                return nextInvoke;
            });
            app.UseAutofacLifetimeScopeInjector(container);
            app.Use<TestMiddleware>();
            app.Use((ctx, next) =>
            {
                Assert.True(ctx.Environment.ContainsKey(Constants.OwinLifetimeScopeKey));
                throw new InvalidOperationException("Test Exception");
            });
        }))
        {
            try
            {
                await server.HttpClient.GetAsync("/");
            }
            catch (InvalidOperationException ex)
            {
                Assert.Equal("Test Exception", ex.Message);
            }
        }
    }

    [Fact]
    public async void UseAutofacLifetimeScopeInjectorWithExternalScopeAddsItToOwinContext()
    {
        using var lifetimeScope = new TestableLifetimeScope();
        using (var server = TestServer.Create(app =>
        {
            app.UseAutofacLifetimeScopeInjector(ctx => lifetimeScope);
            app.Use<TestMiddleware>();
            app.Run(context => context.Response.WriteAsync("Hello, world!"));
        }))
        {
            await server.HttpClient.GetAsync("/");
            Assert.Same(lifetimeScope, TestMiddleware.LifetimeScope);
        }
    }

    [Fact]
    public async void UseAutofacLifetimeScopeInjectorWithExternalScopePassesOwinContextToTheProvider()
    {
        using (var server = TestServer.Create(app =>
        {
            app.UseAutofacLifetimeScopeInjector(ctx =>
            {
                Assert.IsAssignableFrom<IOwinContext>(ctx);
                return Substitute.For<ILifetimeScope>();
            });
            app.Run(context => context.Response.WriteAsync("Hello, world!"));
        }))
        {
            await server.HttpClient.GetAsync("/");
        }
    }

    [Fact]
    public async void UseAutofacLifetimeScopeInjectorWithExternalScopeDoesntDisposeIt()
    {
        using var lifetimeScope = new TestableLifetimeScope();
        using (var server = TestServer.Create(app =>
        {
            app.UseAutofacLifetimeScopeInjector(ctx => lifetimeScope);
            app.Run(context => context.Response.WriteAsync("Hello, world!"));
        }))
        {
            await server.HttpClient.GetAsync("/");
        }

        Assert.False(lifetimeScope.ScopeIsDisposed);
    }

    [Fact]
    public void UseAutofacLifetimeScopeInjectorDoesntAddWrappedMiddlewareInstancesToAppBuilder()
    {
        var builder = new ContainerBuilder();
        builder.RegisterType<TestMiddleware>();
        var container = builder.Build();
        var app = Substitute.For<IAppBuilder>();
        app.Properties.Returns(new Dictionary<string, object>());
        app.Use(Arg.Any<object>(), Arg.Any<object[]>()).Returns(app);

        app.UseAutofacLifetimeScopeInjector(container);

        app.DidNotReceive().Use(Arg.Is<object>(o => o is AutofacMiddleware<TestMiddleware>), Arg.Any<object[]>());
    }

    [Fact]
    public void UseAutofacLifetimeScopeInjectorShowsInjectorRegistered()
    {
        var app = Substitute.For<IAppBuilder>();
        app.Properties.Returns(new Dictionary<string, object>());
        app.Use(Arg.Any<object>(), Arg.Any<object[]>()).Returns(app);

        var container = new ContainerBuilder().Build();
        app.UseAutofacLifetimeScopeInjector(container);
        Assert.True(app.IsAutofacLifetimeScopeInjectorRegistered());
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
        var app = Substitute.For<IAppBuilder>();
        app.Properties.Returns(new Dictionary<string, object>());
        app.Use(Arg.Any<object>(), Arg.Any<object[]>()).Returns(app);

        app.UseAutofacMiddleware(container);

        app.Received().Use(typeof(AutofacMiddleware<TestMiddleware>));
    }

    [Fact]
    public void UseAutofacMiddlewareShowsInjectorRegistered()
    {
        var app = Substitute.For<IAppBuilder>();
        app.Properties.Returns(new Dictionary<string, object>());
        app.Use(Arg.Any<object>(), Arg.Any<object[]>()).Returns(app);

        var container = new ContainerBuilder().Build();
        app.UseAutofacMiddleware(container);
        Assert.True(app.IsAutofacLifetimeScopeInjectorRegistered());
    }

    [Fact]
    public void UseMiddlewareFromContainerAddsSingleWrappedMiddlewareInstanceToAppBuilder()
    {
        var app = Substitute.For<IAppBuilder>();
        app.Properties.Returns(new Dictionary<string, object>());
        app.Use(Arg.Any<object>(), Arg.Any<object[]>()).Returns(app);

        var container = new ContainerBuilder().Build();
        app.UseAutofacLifetimeScopeInjector(container);
        app.UseMiddlewareFromContainer<TestMiddleware>();

        app.Received(1).Use(typeof(AutofacMiddleware<TestMiddleware>));
    }

    [Fact]
    public void UseMiddlewareFromContainerRequiresInjectorRegistrationFirst()
    {
        var app = Substitute.For<IAppBuilder>();
        app.Properties.Returns(new Dictionary<string, object>());
        app.Use(Arg.Any<object>(), Arg.Any<object[]>()).Returns(app);

        Assert.Throws<InvalidOperationException>(() => app.UseMiddlewareFromContainer<TestMiddleware>());
    }

    [Fact]
    public async void UseAutofacLifetimeScopeInjectorWithContainerRegistersOwinContextInTheScope()
    {
        using (var server = TestServer.Create(app =>
        {
            app.UseAutofacLifetimeScopeInjector(new ContainerBuilder().Build());
            app.Run(context =>
            {
                // We can't directly compare contexts because they are recreated at each step in UseHandlerMiddleware.
                Assert.Same(
                    context.Environment,
                    context.GetAutofacLifetimeScope().Resolve<IOwinContext>().Environment);
                return Task.FromResult(0);
            });
        }))
        {
            await server.HttpClient.GetAsync("/");
        }
    }

    [Fact]
    public async void UseAutofacLifetimeScopeInjectorWithExternalScopeDoesNotRegisterOwinContextInTheScope()
    {
        using (var server = TestServer.Create(app =>
        {
            app.UseAutofacLifetimeScopeInjector(context => new ContainerBuilder().Build());
            app.Run(context =>
            {
                Assert.Null(
                    context
                        .GetAutofacLifetimeScope()
                        .ResolveOptional<IOwinContext>());
                return Task.FromResult(0);
            });
        }))
        {
            await server.HttpClient.GetAsync("/");
        }
    }

    [Fact]
    public void UseAutofacMiddlewareAddsMiddlewareInTheCorrectOrder()
    {
        var builder = new ContainerBuilder();

        var traceSet = new List<Type>();

        builder.RegisterType<TracingTestMiddleware<int>>();
        builder.RegisterType<TracingTestMiddleware<bool>>();
        builder.RegisterType<TracingTestMiddleware<string>>();

        var container = builder.Build();
        var app = Substitute.For<IAppBuilder>();
        app.Properties.Returns(new Dictionary<string, object>());
        app.Use(Arg.Any<object>(), Arg.Any<object[]>()).Returns(callInfo =>
        {
            if (callInfo[0] is Type t)
            {
                traceSet.Add(t);
            }

            return app;
        });

        app.UseAutofacMiddleware(container);

        var autofacMiddleware = traceSet
            .Where(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(AutofacMiddleware<>))
            .ToList();

        Assert.Collection(
            autofacMiddleware,
            item => Assert.Equal(typeof(AutofacMiddleware<TracingTestMiddleware<int>>), item),
            item => Assert.Equal(typeof(AutofacMiddleware<TracingTestMiddleware<bool>>), item),
            item => Assert.Equal(typeof(AutofacMiddleware<TracingTestMiddleware<string>>), item));
    }

    [SuppressMessage("CA1812", "CA1812", Justification = "Middleware is instantiated based on being a type parameter.")]
    private class TracingTestMiddleware<T> : OwinMiddleware
    {
        public TracingTestMiddleware(OwinMiddleware next)
            : base(next)
        {
        }

        public override Task Invoke(IOwinContext context)
        {
            throw new NotImplementedException();
        }
    }

    private class TestableLifetimeScope : Disposable, ILifetimeScope
    {
        public bool ScopeIsDisposed { get; set; }

        public IDisposer Disposer => null;

        public object Tag => null;

        public IComponentRegistry ComponentRegistry => null;

        public TestableLifetimeScope()
        {
        }

        public event EventHandler<LifetimeScopeBeginningEventArgs> ChildLifetimeScopeBeginning;

        public event EventHandler<LifetimeScopeEndingEventArgs> CurrentScopeEnding;

        public event EventHandler<ResolveOperationBeginningEventArgs> ResolveOperationBeginning;

        protected override void Dispose(bool disposing)
        {
            CurrentScopeEnding?.Invoke(this, null);
            base.Dispose(disposing);
            ScopeIsDisposed = true;
        }

        public ILifetimeScope BeginLifetimeScope()
        {
            ChildLifetimeScopeBeginning(this, null);
            throw new NotImplementedException();
        }

        public ILifetimeScope BeginLifetimeScope(object tag)
        {
            throw new NotImplementedException();
        }

        public ILifetimeScope BeginLifetimeScope(Action<ContainerBuilder> configurationAction)
        {
            throw new NotImplementedException();
        }

        public ILifetimeScope BeginLifetimeScope(object tag, Action<ContainerBuilder> configurationAction)
        {
            throw new NotImplementedException();
        }

        public object ResolveComponent(ResolveRequest request)
        {
            ResolveOperationBeginning(this, null);
            throw new NotImplementedException();
        }
    }
}
