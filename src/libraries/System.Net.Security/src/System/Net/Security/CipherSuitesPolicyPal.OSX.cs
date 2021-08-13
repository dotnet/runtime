// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;

namespace System.Net.Security
{
    internal sealed class CipherSuitesPolicyPal
    {
        internal uint[] TlsCipherSuites { get; private set; }

        internal CipherSuitesPolicyPal(IEnumerable<TlsCipherSuite> allowedCipherSuites)
        {
            TlsCipherSuites = allowedCipherSuites.Select((cs) => (uint)cs).ToArray();
        }

        internal IEnumerable<TlsCipherSuite> GetCipherSuites()
        {
            return TlsCipherSuites.Select((cs) => (TlsCipherSuite)cs);
        }
    }
}
