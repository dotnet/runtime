// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.Tracing;
using System.Globalization;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace System.Net
{
    //TODO: If localization resources are not found, logging does not work. Issue #5126.
    internal sealed partial class NetEventSource
    {
        // Event ids are defined in NetEventSource.Common.cs.

        [Event(EnumerateSecurityPackagesId, Keywords = Keywords.Default, Level = EventLevel.Informational)]
        public void EnumerateSecurityPackages(string securityPackage)
        {
            if (IsEnabled())
            {
                WriteEvent(EnumerateSecurityPackagesId, securityPackage ?? "");
            }
        }

        [Event(SspiPackageNotFoundId, Keywords = Keywords.Default, Level = EventLevel.Informational)]
        public void SspiPackageNotFound(string packageName)
        {
            if (IsEnabled())
            {
                WriteEvent(SspiPackageNotFoundId, packageName ?? "");
            }
        }
    }
}
