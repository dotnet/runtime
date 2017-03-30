// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32.SafeHandles;

using Internal.Cryptography;
using System.Runtime.InteropServices;
using System.Diagnostics;

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

        internal static CngKey Import(byte[] keyBlob, string curveName, CngKeyBlobFormat format)
        {
            return Import(keyBlob, curveName, format, provider: CngProvider.MicrosoftSoftwareKeyStorageProvider);
        }

        public static CngKey Import(byte[] keyBlob, CngKeyBlobFormat format, CngProvider provider)
        {
            return Import(keyBlob, null, format, provider);
        }

        internal static CngKey Import(byte[] keyBlob, string curveName, CngKeyBlobFormat format, CngProvider provider)
        {
            if (keyBlob == null)
                throw new ArgumentNullException(nameof(keyBlob));
            if (format == null)
                throw new ArgumentNullException(nameof(format));
            if (provider == null)
                throw new ArgumentNullException(nameof(provider));

            SafeNCryptProviderHandle providerHandle = provider.OpenStorageProvider();
            SafeNCryptKeyHandle keyHandle = null;
            ErrorCode errorCode;
            
            if (curveName == null)
            {
                errorCode = Interop.NCrypt.NCryptImportKey(providerHandle, IntPtr.Zero, format.Format, IntPtr.Zero, out keyHandle, keyBlob, keyBlob.Length, 0);
                if (errorCode != ErrorCode.ERROR_SUCCESS)
                {
                    throw errorCode.ToCryptographicException();
                }
            }
            else
            {
                keyHandle = ECCng.ImportKeyBlob(format.Format, keyBlob, curveName, providerHandle);
            }

            CngKey key = new CngKey(providerHandle, keyHandle);

            // We can't tell directly if an OpaqueTransport blob imported as an ephemeral key or not
            key.IsEphemeral = format != CngKeyBlobFormat.OpaqueTransportBlob;

            return key;
        }
    }
}
