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
        // Check to see if a key already exists
        //

        public static bool Exists(string keyName)
        {
            return Exists(keyName, provider: CngProvider.MicrosoftSoftwareKeyStorageProvider);
        }

        public static bool Exists(string keyName, CngProvider provider)
        {
            return Exists(keyName, provider, options: CngKeyOpenOptions.None);
        }

        public static bool Exists(string keyName, CngProvider provider, CngKeyOpenOptions options)
        {
            if (keyName == null)
                throw new ArgumentNullException("keyName");

            if (provider == null)
                throw new ArgumentNullException("provider");

            using (SafeNCryptProviderHandle providerHandle = provider.OpenStorageProvider())
            {
                SafeNCryptKeyHandle keyHandle = null;
                try
                {
                    ErrorCode errorCode = Interop.NCrypt.NCryptOpenKey(providerHandle, out keyHandle, keyName, 0, options);
                    if (errorCode == ErrorCode.NTE_BAD_KEYSET)
                        return false;
                    if (errorCode != ErrorCode.ERROR_SUCCESS)
                        throw errorCode.ToCryptographicException();
                    return true;
                }
                finally
                {
                    if (keyHandle != null)
                        keyHandle.Dispose();
                }
            }
        }
    }
}

