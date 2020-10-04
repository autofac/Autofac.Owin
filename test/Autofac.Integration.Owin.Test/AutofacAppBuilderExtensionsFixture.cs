using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Autofac.Core;
using Autofac.Core.Lifetime;
using Autofac.Core.Registration;
using Autofac.Core.Resolving;
using Autofac.Features.ResolveAnything;
using Autofac.Util;
using Microsoft.Owin;
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
            var lifetimeScope = new TestableLifetimeScope();
            using (var server = TestServer.Create(app =>
            {
                app.Use((ctx, next) =>
                {
                    ctx.SetAutofacLifetimeScope(lifetimeScope);
                    return next();
                });
                //we don't expect anything to be called on this one, so we want it to fail
                app.UseAutofacLifetimeScopeInjector(new Mock<ILifetimeScope>(MockBehavior.Strict).Object);
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
            var lifetimeScope = new TestableLifetimeScope();
            using (var server = TestServer.Create(app =>
            {
                app.Use((ctx, next) =>
                {
                    ctx.SetAutofacLifetimeScope(lifetimeScope);
                    return next();
                });
                app.UseAutofacLifetimeScopeInjector(new Mock<ILifetimeScope>(MockBehavior.Strict).Object);
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
                //we don't expect anything to be called on this one, so we want it to fail
                app.UseAutofacLifetimeScopeInjector(new Mock<ILifetimeScope>(MockBehavior.Strict).Object);
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

            var disposable = new Mock<IDisposable>();

            using (var server = TestServer.Create(app =>
            {
                app.UseAutofacLifetimeScopeInjector(container);
                app.Use((ctx, next) =>
                {
                    ctx.GetAutofacLifetimeScope().Disposer.AddInstanceForDisposal(disposable.Object);
                    return next();
                });
                app.Run(context => context.Response.WriteAsync("Hello, world!"));
            }))
            {
                await server.HttpClient.GetAsync("/");
            }
            disposable.Verify(d => d.Dispose());
        }

        [Fact]
        public async void UseAutofacLifetimeScopeInjectorWithExternalScopeAddsItToOwinContext()
        {
            var lifetimeScope = new TestableLifetimeScope();
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
                    return new Mock<ILifetimeScope>().Object;
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
            var lifetimeScope = new TestableLifetimeScope();
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

        [Fact]
        public async void UseAutofacLifetimeScopeInjectorWithContainerRegistersOwinContextInTheScope()
        {
            using (var server = TestServer.Create(app =>
            {
                app.UseAutofacLifetimeScopeInjector(new ContainerBuilder().Build());
                app.Run(context =>
                {
                    //we can't directly compare contexts because they are recreated at each step in UseHandlerMidleware
                    Assert.Same(
                        context.Environment,
                        context
                            .GetAutofacLifetimeScope()
                            .Resolve<IOwinContext>()
                            .Environment
                            );
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
                            .ResolveOptional<IOwinContext>()
                    );
                    return Task.FromResult(0);
                });
            }))
            {
                await server.HttpClient.GetAsync("/");
            }
        }

        public class TestableLifetimeScope : Disposable, ILifetimeScope
        {
            public bool ScopeIsDisposed { get; set; }

            public IDisposer Disposer => throw new NotImplementedException();

            public object Tag => throw new NotImplementedException();

            public IComponentRegistry ComponentRegistry => throw new NotImplementedException();

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
                this.ScopeIsDisposed = true;
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
}
