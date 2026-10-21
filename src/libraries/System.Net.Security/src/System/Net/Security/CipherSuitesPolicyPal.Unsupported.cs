// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Net.Security
{
    internal sealed class CipherSuitesPolicyPal
    {
        internal CipherSuitesPolicyPal(IEnumerable<TlsCipherSuite> allowedCipherSuites) =>
            throw new PlatformNotSupportedException(SR.SystemNetSecurity_PlatformNotSupported);

#pragma warning disable CA1822 // Must match the instance signature used by the other PALs.
        internal IEnumerable<TlsCipherSuite> GetCipherSuites() =>
            throw new PlatformNotSupportedException(SR.SystemNetSecurity_PlatformNotSupported);
#pragma warning restore CA1822
    }
}
