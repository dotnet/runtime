// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;

namespace Microsoft.Extensions.Hosting
{
    /// <summary>
    /// Allows consumers to perform cleanup during a graceful shutdown.
    /// <para>
    ///  This type is obsolete and will be removed in a future version.
    ///  The recommended alternative is Microsoft.Extensions.Hosting.IHostApplicationLifetime.
    /// </para>
    /// </summary>
    [Obsolete("IApplicationLifetime has been deprecated. Use Microsoft.Extensions.Hosting.IHostApplicationLifetime instead.")]
    public interface IApplicationLifetime : IHostApplicationLifetime
    { }
}
