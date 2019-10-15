// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Cryptography
{
    public sealed class CspKeyContainerInfo
    {
        public CspKeyContainerInfo(CspParameters parameters) { throw GetPlatformNotSupported(); }
        public bool Accessible { get { throw GetPlatformNotSupported(); } }
        public bool Exportable { get { throw GetPlatformNotSupported(); } }
        public bool HardwareDevice { get { throw GetPlatformNotSupported(); } }
        public string KeyContainerName { get { throw GetPlatformNotSupported(); } }
        public KeyNumber KeyNumber { get { throw GetPlatformNotSupported(); } }
        public bool MachineKeyStore { get { throw GetPlatformNotSupported(); } }
        public bool Protected { get { throw GetPlatformNotSupported(); } }
        public string ProviderName { get { throw GetPlatformNotSupported(); } }
        public int ProviderType { get { throw GetPlatformNotSupported(); } }
        public bool RandomlyGenerated { get { throw GetPlatformNotSupported(); } }
        public bool Removable { get { throw GetPlatformNotSupported(); } }
        public string UniqueKeyContainerName { get { throw GetPlatformNotSupported(); } }

        private static Exception GetPlatformNotSupported()
        {
            return new PlatformNotSupportedException(SR.Format(SR.Cryptography_CAPI_Required, nameof(CspKeyContainerInfo)));
        }
    }
}
