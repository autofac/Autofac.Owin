using System.Threading.Tasks;
using Microsoft.Owin;

namespace Autofac.Integration.Owin.Test
{
    public class TestMiddleware : OwinMiddleware
    {
        public TestMiddleware(OwinMiddleware next)
            : base(next)
        {
            LifetimeScope = null;
        }

        public static ILifetimeScope LifetimeScope { get; set; }

        public override Task Invoke(IOwinContext context)
        {
            LifetimeScope = context.GetAutofacLifetimeScope();
            return Next.Invoke(context);
        }
    }
}
