// Copyright (c) Autofac Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace Autofac.Integration.Owin;

/// <summary>
/// Constants used in lifetime scope handling.
/// </summary>
internal static class Constants
{
    /// <summary>
    /// The AutofacMiddleware boundary.
    /// </summary>
    /// <remarks>
    /// This boundary is used to isolate this AutofacMiddleware from other copies
    /// on the same OWIN pipeline.
    /// </remarks>
    internal static readonly string AutofacMiddlewareBoundary = Guid.NewGuid().ToString();

    /// <summary>
    /// The OWIN key for the current lifetime scope.
    /// </summary>
    internal static readonly string OwinLifetimeScopeKey = "autofac:OwinLifetimeScope:" + AutofacMiddlewareBoundary;
}
