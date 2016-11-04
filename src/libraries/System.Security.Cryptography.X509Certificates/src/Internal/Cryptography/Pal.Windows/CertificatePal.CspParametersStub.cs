// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// .NET Native (UWP) does not have access to the CspParameters type because it is in a non-UWP-compatible
// assembly.  So these type definitions allow the code to continue to use the same data transport structure
// that it has for quite a while.

#if NETNATIVE
using System;

namespace Internal.Cryptography.Pal
{
    internal sealed partial class CertificatePal : IDisposable, ICertificatePal
    {
        [Flags]
        private enum CspProviderFlags
        {
            // Note: UWP doesn't use any flags other than these, so they have been omitted.
            NoFlags = 0x0000,
            UseMachineKeyStore = 0x0001,
        }

        private sealed class CspParameters
        {
            public int ProviderType;
            public string ProviderName;
            public string KeyContainerName;
            public int KeyNumber;
            private int _flags;

            /// <summary>
            /// Flag property
            /// </summary>
            public CspProviderFlags Flags
            {
                get { return (CspProviderFlags)_flags; }
                set
                {
                    const int allFlags = 0x00FF; // this should change if more values are added to CspProviderFlags
                    int flags = (int)value;
                    if ((flags & ~allFlags) != 0)
                    {
                        throw new ArgumentException(SR.Format(SR.Arg_EnumIllegalVal, nameof(value)));
                    }
                    _flags = flags;
                }
            }

            public CspParameters() : this(-1, null, null)
            {
            }

            private CspParameters(int dwTypeIn, string strProviderNameIn, string strContainerNameIn) :
                this(dwTypeIn, strProviderNameIn, strContainerNameIn, CspProviderFlags.NoFlags)
            {
            }

            private CspParameters(int providerType, string providerName, string keyContainerName, CspProviderFlags flags)
            {
                ProviderType = providerType;
                ProviderName = providerName;
                KeyContainerName = keyContainerName;
                KeyNumber = -1;
                Flags = flags;
            }
        }
    }
}

#endif // #if NETNATIVE
