using System;
using Microsoft.Owin;
using Moq;
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
    }
}
