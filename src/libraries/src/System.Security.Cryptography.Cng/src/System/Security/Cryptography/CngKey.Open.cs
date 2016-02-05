// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
                throw new ArgumentNullException(nameof(keyName));
            if (provider == null)
                throw new ArgumentNullException(nameof(provider));

            SafeNCryptProviderHandle providerHandle = provider.OpenStorageProvider();
            SafeNCryptKeyHandle keyHandle;
            ErrorCode errorCode = Interop.NCrypt.NCryptOpenKey(providerHandle, out keyHandle, keyName, 0, openOptions);
            if (errorCode != ErrorCode.ERROR_SUCCESS)
                throw errorCode.ToCryptographicException();

            return new CngKey(providerHandle, keyHandle);
        }
    }
}

