// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Diagnostics.Contracts;

using Microsoft.Win32.SafeHandles;

using Internal.Cryptography;

using ErrorCode = Interop.NCrypt.ErrorCode;

namespace System.Security.Cryptography
{
    /// <summary>
    ///     Managed representation of an NCrypt key
    /// </summary>
    public sealed partial class CngKey : IDisposable
    {
        //
        // Open factory methods
        //

        public static CngKey Open(string keyName)
        {
            return Open(keyName, provider: CngProvider.MicrosoftSoftwareKeyStorageProvider);
        }

        public static CngKey Open(string keyName, CngProvider provider)
        {
            return Open(keyName, provider, openOptions: CngKeyOpenOptions.None);
        }

        public static CngKey Open(string keyName, CngProvider provider, CngKeyOpenOptions openOptions)
        {
            if (keyName == null)
                throw new ArgumentNullException("keyName");
            if (provider == null)
                throw new ArgumentNullException("provider");

            SafeNCryptProviderHandle providerHandle = provider.OpenStorageProvider();
            SafeNCryptKeyHandle keyHandle;
            ErrorCode errorCode = Interop.NCrypt.NCryptOpenKey(providerHandle, out keyHandle, keyName, 0, openOptions);
            if (errorCode != ErrorCode.ERROR_SUCCESS)
                throw errorCode.ToCryptographicException();

            return new CngKey(providerHandle, keyHandle);
        }
    }
}

