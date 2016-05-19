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

#if !NETNATIVE
        internal static CngKey Import(byte[] keyBlob, ECCurve curve, CngKeyBlobFormat format)
        {
            return Import(keyBlob, curve, format, provider: CngProvider.MicrosoftSoftwareKeyStorageProvider);
        }
#endif //!NETNATIVE

        public static CngKey Import(byte[] keyBlob, CngKeyBlobFormat format, CngProvider provider)
        {
#if !NETNATIVE
            return Import(keyBlob, null, format, provider);
        }

        internal static CngKey Import(byte[] keyBlob, ECCurve? curve, CngKeyBlobFormat format, CngProvider provider)
        {
#endif //!NETNATIVE
            if (keyBlob == null)
                throw new ArgumentNullException(nameof(keyBlob));
            if (format == null)
                throw new ArgumentNullException(nameof(format));
            if (provider == null)
                throw new ArgumentNullException(nameof(provider));

            SafeNCryptProviderHandle providerHandle = provider.OpenStorageProvider();
            SafeNCryptKeyHandle keyHandle;
            ErrorCode errorCode;
            
#if !NETNATIVE
            if (curve == null)
#endif //!NETNATIVE
            {
                errorCode = Interop.NCrypt.NCryptImportKey(providerHandle, IntPtr.Zero, format.Format, IntPtr.Zero, out keyHandle, keyBlob, keyBlob.Length, 0);
                if (errorCode != ErrorCode.ERROR_SUCCESS)
                {
                    throw errorCode.ToCryptographicException();
                }
            }
#if !NETNATIVE
            else
            {
                // Call with Oid.FriendlyName because .Value will result in an invalid parameter error
                Debug.Assert(curve.Value.IsNamed);
                string curveName = curve.Value.Oid.FriendlyName; 
                using (SafeUnicodeStringHandle safeCurveName = new SafeUnicodeStringHandle(curveName))
                {
                    var desc = new Interop.BCrypt.BCryptBufferDesc();
                    var buff = new Interop.BCrypt.BCryptBuffer();

                    IntPtr descPtr = IntPtr.Zero;
                    IntPtr buffPtr = IntPtr.Zero;
                    try
                    {
                        descPtr = Marshal.AllocHGlobal(Marshal.SizeOf(desc));
                        buffPtr = Marshal.AllocHGlobal(Marshal.SizeOf(buff));
                        buff.cbBuffer = (curveName.Length + 1) * 2; // Add 1 for null terminator
                        buff.BufferType = Interop.BCrypt.NCryptBufferDescriptors.NCRYPTBUFFER_ECC_CURVE_NAME;
                        buff.pvBuffer = safeCurveName.DangerousGetHandle();
                        Marshal.StructureToPtr(buff, buffPtr, false);

                        desc.cBuffers = 1;
                        desc.pBuffers = buffPtr;
                        desc.ulVersion = Interop.BCrypt.BCRYPTBUFFER_VERSION;
                        Marshal.StructureToPtr(desc, descPtr, false);

                        errorCode = Interop.NCrypt.NCryptImportKey(providerHandle, IntPtr.Zero, format.Format, descPtr, out keyHandle, keyBlob, keyBlob.Length, 0);
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(descPtr);
                        Marshal.FreeHGlobal(buffPtr);
                    }
                }

                if (errorCode != ErrorCode.ERROR_SUCCESS)
                {
                    Exception e = errorCode.ToCryptographicException();
                    if (errorCode == ErrorCode.NTE_INVALID_PARAMETER)
                    {
                        throw new PlatformNotSupportedException(string.Format(SR.Cryptography_CurveNotSupported, curveName), e);
                    }
                    throw e;
                }
            }
#endif //!NETNATIVE

            CngKey key = new CngKey(providerHandle, keyHandle);

            // We can't tell directly if an OpaqueTransport blob imported as an ephemeral key or not
            key.IsEphemeral = format != CngKeyBlobFormat.OpaqueTransportBlob;

            return key;
        }
    }
}
