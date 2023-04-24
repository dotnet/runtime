// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.Versioning;

namespace System.Net.Security
{
    /// <summary>
    /// Specifies allowed cipher suites.
    /// </summary>
    [UnsupportedOSPlatform("windows")]
    [UnsupportedOSPlatform("android")]
    public sealed partial class CipherSuitesPolicy
    {
        internal CipherSuitesPolicyPal Pal { get; private set; }

        [CLSCompliant(false)]
        public CipherSuitesPolicy(IEnumerable<TlsCipherSuite> allowedCipherSuites)
        {
            ArgumentNullException.ThrowIfNull(allowedCipherSuites);

            Pal = new CipherSuitesPolicyPal(allowedCipherSuites);
        }

        [CLSCompliant(false)]
        public IEnumerable<TlsCipherSuite> AllowedCipherSuites
        {
            get
            {
                // This method is only useful only for diagnostic purposes so perf is not important
                // We do not want users to be able to cast result to something they can modify
                foreach (TlsCipherSuite cs in Pal.GetCipherSuites())
                {
                    yield return cs;
                }
            }
        }
    }
}
