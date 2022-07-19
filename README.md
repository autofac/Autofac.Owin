# Autofac.Owin

ASP.NET OWIN integration for [Autofac](https://autofac.org).

[![Build status](https://ci.appveyor.com/api/projects/status/t5084r3ur38w31ho?svg=true)](https://ci.appveyor.com/project/Autofac/autofac-owin)

Please file issues and pull requests for this package [in this repository](https://github.com/autofac/Autofac.Owin/issues) rather than in the Autofac core repo.

- [Documentation](https://autofac.readthedocs.io/en/latest/integration/owin.html)
- [NuGet](https://www.nuget.org/packages/Autofac.Owin)
- [Contributing](https://autofac.readthedocs.io/en/latest/contributors.html)
- [Open in Visual Studio Code](https://open.vscode.dev/autofac/Autofac.Owin)

## Quick Start

To take advantage of Autofac in your OWIN pipeline:

- Reference the `Autofac.Owin` package from NuGet.
- Build your Autofac container.
- Register the Autofac middleware with OWIN and pass it the container.

```c#
public class Startup
{
  public void Configuration(IAppBuilder app)
  {
    var builder = new ContainerBuilder();
    // Register dependencies, then...
    var container = builder.Build();

    // Register the Autofac middleware FIRST. This also adds
    // Autofac-injected middleware registered with the container.
    app.UseAutofacMiddleware(container);

    // ...then register your other middleware not registered
    // with Autofac.
  }
}
```

Check out the [Autofac OWIN integration documentation](https://autofac.readthedocs.io/en/latest/integration/owin.html) for more information.

## Get Help

**Need help with Autofac?** We have [a documentation site](https://autofac.readthedocs.io/) as well as [API documentation](https://autofac.org/apidoc/). We're ready to answer your questions on [Stack Overflow](https://stackoverflow.com/questions/tagged/autofac) or check out the [discussion forum](https://groups.google.com/forum/#forum/autofac).
