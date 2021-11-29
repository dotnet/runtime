// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Hosting
{
    /// <summary>
    /// Commonly used environment names.
    /// <para>
    ///  This type is obsolete and will be removed in a future version.
    ///  The recommended alternative is Microsoft.Extensions.Hosting.Environments.
    /// </para>
    /// </summary>
    [System.Obsolete("EnvironmentName has been deprecated. Use Microsoft.Extensions.Hosting.Environments instead.")]
    public static class EnvironmentName
    {
        public static readonly string Development = "Development";
        public static readonly string Staging = "Staging";
        public static readonly string Production = "Production";
    }
}
