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
        // Import factory methods
        //

        public static CngKey Import(byte[] keyBlob, CngKeyBlobFormat format)
        {
            return Import(keyBlob, format, provider: CngProvider.MicrosoftSoftwareKeyStorageProvider);
        }

        public static CngKey Import(byte[] keyBlob, CngKeyBlobFormat format, CngProvider provider)
        {
            if (keyBlob == null)
                throw new ArgumentNullException("keyBlob");
            if (format == null)
                throw new ArgumentNullException("format");
            if (provider == null)
                throw new ArgumentNullException("provider");

            SafeNCryptProviderHandle providerHandle = provider.OpenStorageProvider();
            SafeNCryptKeyHandle keyHandle;
            ErrorCode errorCode = Interop.NCrypt.NCryptImportKey(providerHandle, IntPtr.Zero, format.Format, IntPtr.Zero, out keyHandle, keyBlob, keyBlob.Length, 0);
            if (errorCode != ErrorCode.ERROR_SUCCESS)
                throw errorCode.ToCryptographicException();

            CngKey key = new CngKey(providerHandle, keyHandle);

            // We can't tell directly if an OpaqueTransport blob imported as an ephemeral key or not
            key.IsEphemeral = format != CngKeyBlobFormat.OpaqueTransportBlob;

            return key;
        }
    }
}

