// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

    namespace System.Security.Cryptography
{
    public sealed partial class CspKeyContainerInfo
    {
        public CspKeyContainerInfo(CspParameters parameters)
        {
            throw new PlatformNotSupportedException();
        }

        public bool Accessible { get { return default(bool); } }
        public bool Exportable { get { return default(bool); } }
        public bool HardwareDevice { get { return default(bool); } }
        public string KeyContainerName { get { return default(string); } }
        public KeyNumber KeyNumber { get { return default(KeyNumber); } }
        public bool MachineKeyStore { get { return default(bool); } }
        public bool Protected { get { return default(bool); } }
        public string ProviderName { get { return default(string); } }
        public int ProviderType { get { return default(int); } }
        public bool RandomlyGenerated { get { return default(bool); } }
        public bool Removable { get { return default(bool); } }
        public string UniqueKeyContainerName { get { return default(string); } }
    }

    public sealed partial class CspParameters
    {
        public string KeyContainerName;
        public int KeyNumber;
        public string ProviderName;
        public int ProviderType;

        public CspParameters()
        {
            throw new PlatformNotSupportedException();
        }

        public CspParameters(int dwTypeIn)
        {
            throw new PlatformNotSupportedException();
        }

        public CspParameters(int dwTypeIn, string strProviderNameIn)
        {
            throw new PlatformNotSupportedException();
        }

        public CspParameters(int dwTypeIn, string strProviderNameIn, string strContainerNameIn)
        {
            throw new PlatformNotSupportedException();
        }

        public CspProviderFlags Flags { get { return default(CspProviderFlags); } set { } }
        public IntPtr ParentWindowHandle { get { return default(IntPtr); } set { } }
    }
}
