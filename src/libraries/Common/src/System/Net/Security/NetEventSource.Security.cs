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
        private const int EnumerateSecurityPackagesId = NextAvailableEventId;
        private const int SspiPackageNotFoundId = EnumerateSecurityPackagesId + 1;
        private const int AcquireDefaultCredentialId = SspiPackageNotFoundId + 1;
        private const int AcquireCredentialsHandleId = AcquireDefaultCredentialId + 1;
        private const int InitializeSecurityContextId = AcquireCredentialsHandleId + 1;
        private const int SecurityContextInputBufferId = InitializeSecurityContextId + 1;
        private const int SecurityContextInputBuffersId = SecurityContextInputBufferId + 1;
        private const int AcceptSecuritContextId = SecurityContextInputBuffersId + 1;
        private const int OperationReturnedSomethingId = AcceptSecuritContextId + 1;

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
