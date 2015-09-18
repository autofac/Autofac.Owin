using System;
using Microsoft.Owin;
using Moq;
using NUnit.Framework;

namespace Autofac.Integration.Owin.Test
{
    [TestFixture]
    public class OwinContextExtensionsFixture
    {
        [Test]
        public void GetAutofacLifetimeScopeReturnsInstanceFromContext()
        {
            var context = new Mock<IOwinContext>();
            context.Setup(mock => mock.Get<ILifetimeScope>(Constants.OwinLifetimeScopeKey));

            context.Object.GetAutofacLifetimeScope();

            context.VerifyAll();
        }

        [Test]
        public void GetAutofacLifetimeScopeThrowsWhenProvidedNullInstance()
        {
            var exception = Assert.Throws<ArgumentNullException>(() => OwinContextExtensions.GetAutofacLifetimeScope(null));
            Assert.That(exception.ParamName, Is.EqualTo("context"));
        }
    }
}
