// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.Extensions.Hosting
{
    /// <summary>
    /// Provides information about the hosting environment an application is running in.
    /// <para>
    ///  This type is obsolete and will be removed in a future version.
    ///  The recommended alternative is Microsoft.Extensions.Hosting.IHostEnvironment.
    /// </para>
    /// </summary>
    [Obsolete("IHostingEnvironment has been deprecated. Use Microsoft.Extensions.Hosting.IHostEnvironment instead.")]
    public interface IHostingEnvironment : IHostEnvironment
    { }
}
