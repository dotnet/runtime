// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.FileProviders;

namespace Microsoft.Extensions.Hosting.Internal
{
#pragma warning disable CS0618 // Type or member is obsolete
    /// <summary>
    /// This API supports infrastructure and is not intended to be used
    /// directly from your code. This API may change or be removed in future releases.
    /// </summary>
    public class HostingEnvironment : IHostingEnvironment, IHostEnvironment
#pragma warning restore CS0618 // Type or member is obsolete
    {
        public string EnvironmentName { get; set; } = null!;

        public string? ApplicationName { get; set; }

        public string ContentRootPath { get; set; } = null!;

        public IFileProvider ContentRootFileProvider { get; set; } = null!;
    }
}
