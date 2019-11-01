// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Security.Cryptography
{
    public partial struct ECCurve
    {
        /// <summary>
        /// Factory class for creating named curves.
        /// </summary>
        public static class NamedCurves
        {
            private const string ECDSA_P256_OID_VALUE = "1.2.840.10045.3.1.7"; // nistP256 or secP256r1
            private const string ECDSA_P384_OID_VALUE = "1.3.132.0.34"; // nistP384 or secP384r1
            private const string ECDSA_P521_OID_VALUE = "1.3.132.0.35"; // nistP521 or secP521r1

            public static ECCurve brainpoolP160r1
            {
                get
                {
                    return ECCurve.CreateFromFriendlyName(nameof(brainpoolP160r1));
                }
            }

            public static ECCurve brainpoolP160t1
            {
                get
                {
                    return ECCurve.CreateFromFriendlyName(nameof(brainpoolP160t1));
                }
            }

            public static ECCurve brainpoolP192r1
            {
                get
                {
                    return ECCurve.CreateFromFriendlyName(nameof(brainpoolP192r1));
                }
            }

            public static ECCurve brainpoolP192t1
            {
                get
                {
                    return ECCurve.CreateFromFriendlyName(nameof(brainpoolP192t1));
                }
            }

            public static ECCurve brainpoolP224r1
            {
                get
                {
                    return ECCurve.CreateFromFriendlyName(nameof(brainpoolP224r1));
                }
            }

            public static ECCurve brainpoolP224t1
            {
                get
                {
                    return ECCurve.CreateFromFriendlyName(nameof(brainpoolP224t1));
                }
            }

            public static ECCurve brainpoolP256r1
            {
                get
                {
                    return ECCurve.CreateFromFriendlyName(nameof(brainpoolP256r1));
                }
            }

            public static ECCurve brainpoolP256t1
            {
                get
                {
                    return ECCurve.CreateFromFriendlyName(nameof(brainpoolP256t1));
                }
            }

            public static ECCurve brainpoolP320r1
            {
                get
                {
                    return ECCurve.CreateFromFriendlyName(nameof(brainpoolP320r1));
                }
            }

            public static ECCurve brainpoolP320t1
            {
                get
                {
                    return ECCurve.CreateFromFriendlyName(nameof(brainpoolP320t1));
                }
            }

            public static ECCurve brainpoolP384r1
            {
                get
                {
                    return ECCurve.CreateFromFriendlyName(nameof(brainpoolP384r1));
                }
            }

            public static ECCurve brainpoolP384t1
            {
                get
                {
                    return ECCurve.CreateFromFriendlyName(nameof(brainpoolP384t1));
                }
            }

            public static ECCurve brainpoolP512r1
            {
                get
                {
                    return ECCurve.CreateFromFriendlyName(nameof(brainpoolP512r1));
                }
            }

            public static ECCurve brainpoolP512t1
            {
                get
                {
                    return ECCurve.CreateFromFriendlyName(nameof(brainpoolP512t1));
                }
            }

            public static ECCurve nistP256
            {
                get
                {
                    // Hard-code nist curve as friendly name is not consistent with algorithm name
                    return ECCurve.CreateFromValueAndName(ECDSA_P256_OID_VALUE, nameof(nistP256));
                }
            }

            public static ECCurve nistP384
            {
                get
                {
                    // Hard-code nist curve as friendly name is not consistent with algorithm name
                    return ECCurve.CreateFromValueAndName(ECDSA_P384_OID_VALUE, nameof(nistP384));
                }
            }

            public static ECCurve nistP521
            {
                get
                {
                    // Hard-code nist curve as friendly name is not consistent with algorithm name
                    return ECCurve.CreateFromValueAndName(ECDSA_P521_OID_VALUE, nameof(nistP521));
                }
            }
        }
    }
}
