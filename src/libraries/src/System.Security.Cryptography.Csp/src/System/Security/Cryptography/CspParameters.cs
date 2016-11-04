// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Security.Cryptography;
using Internal.NativeCrypto;

namespace System.Security.Cryptography
{
    public sealed class CspParameters
    {
        public int ProviderType;
        public string ProviderName;
        public string KeyContainerName;
        public int KeyNumber;
        private int _flags;
        private IntPtr _parentWindowHandle;

        /// <summary>
        /// Flag property
        /// </summary>
        public CspProviderFlags Flags
        {
            get
            {
                return (CspProviderFlags)_flags;
            }
            set
            {
                int allFlags = 0x00FF; // this should change if more values are added to CspProviderFlags
                Debug.Assert((CspProviderFlags.UseMachineKeyStore |
                              CspProviderFlags.UseDefaultKeyContainer |
                              CspProviderFlags.UseNonExportableKey |
                              CspProviderFlags.UseExistingKey |
                              CspProviderFlags.UseArchivableKey |
                              CspProviderFlags.UseUserProtectedKey |
                              CspProviderFlags.NoPrompt |
                              CspProviderFlags.CreateEphemeralKey) == (CspProviderFlags)allFlags, "allFlags does not match all CspProviderFlags");

                int flags = (int)value;
                if ((flags & ~allFlags) != 0)
                {
                    throw new ArgumentException(SR.Format(SR.Arg_EnumIllegalVal, nameof(value)));
                }
                _flags = flags;
            }
        }

        public CspParameters() : this(CapiHelper.DefaultRsaProviderType, null, null) { }

        public CspParameters(int dwTypeIn) : this(dwTypeIn, null, null) { }
        public CspParameters(int dwTypeIn, string strProviderNameIn) : this(dwTypeIn, strProviderNameIn, null) { }
        public CspParameters(int dwTypeIn, string strProviderNameIn, string strContainerNameIn) :
            this(dwTypeIn, strProviderNameIn, strContainerNameIn, CspProviderFlags.NoFlags)
        {
        }

        internal CspParameters(int providerType, string providerName, string keyContainerName, CspProviderFlags flags)
        {
            ProviderType = providerType;
            ProviderName = providerName;
            KeyContainerName = keyContainerName;
            KeyNumber = -1;
            Flags = flags;
        }

        //Copy constructor
        internal CspParameters(CspParameters parameters)
        {
            ProviderType = parameters.ProviderType;
            ProviderName = parameters.ProviderName;
            KeyContainerName = parameters.KeyContainerName;
            KeyNumber = parameters.KeyNumber;
            Flags = parameters.Flags;
            _parentWindowHandle = parameters._parentWindowHandle;
        }

        public IntPtr ParentWindowHandle
        {
            get
            {
                return _parentWindowHandle;
            }
            set
            {
                _parentWindowHandle = value;
            }
        }
    }
}
