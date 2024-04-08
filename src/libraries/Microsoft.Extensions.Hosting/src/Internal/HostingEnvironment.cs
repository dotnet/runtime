// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.Extensions.Hosting.Internal
{
    /// <summary>
    /// This API supports infrastructure and is not intended to be used
    /// directly from your code. This API may change or be removed in future releases.
    /// </summary>
    [DebuggerDisplay("ApplicationName = {ApplicationName}, EnvironmentName = {EnvironmentName}")]
#pragma warning disable CS0618 // Type or member is obsolete
    public class HostingEnvironment : IHostingEnvironment, IHostEnvironment
#pragma warning restore CS0618 // Type or member is obsolete
    {
        /// <summary>
        /// This API supports infrastructure and is not intended to be used directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public string EnvironmentName { get; set; } = string.Empty;

        /// <summary>
        /// This API supports infrastructure and is not intended to be used directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public string ApplicationName { get; set; } = string.Empty;

        /// <summary>
        /// This API supports infrastructure and is not intended to be used directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public string ContentRootPath { get; set; } = string.Empty;

        /// <summary>
        /// This API supports infrastructure and is not intended to be used directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public IFileProvider ContentRootFileProvider { get; set; } = null!;
    }
}
