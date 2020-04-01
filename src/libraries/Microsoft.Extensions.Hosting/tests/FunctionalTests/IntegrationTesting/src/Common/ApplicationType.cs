// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.Extensions.Hosting.IntegrationTesting
{
    public enum ApplicationType
    {
        /// <summary>
        /// Does not target a specific platform. Requires the matching runtime to be installed.
        /// </summary>
        Portable,

        /// <summary>
        /// All dlls are published with the app for x-copy deploy. Net461 requires this because ASP.NET Core is not in the GAC.
        /// </summary>
        Standalone
    }
}
