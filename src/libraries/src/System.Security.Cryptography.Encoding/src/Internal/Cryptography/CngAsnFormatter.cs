// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Buffers;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

using Internal.NativeCrypto;

namespace Internal.Cryptography
{
    internal sealed class CngAsnFormatter : AsnFormatter
    {
        protected override string FormatNative(Oid oid, byte[] rawData, bool multiLine)
        {
            // If OID is not present, then we can force CryptFormatObject 
            // to use hex formatting by providing an empty OID string.
            string oidValue = string.Empty;
            if (oid != null && oid.Value != null)
            {
                oidValue = oid.Value;
            }

            int dwFormatStrType = multiLine ? Interop.Crypt32.CRYPT_FORMAT_STR_MULTI_LINE : Interop.Crypt32.CRYPT_FORMAT_STR_NONE;
            int cbFormat = 0;
            const int X509_ASN_ENCODING = 0x00000001;
            unsafe
            {
                IntPtr oidValuePtr = Marshal.StringToHGlobalAnsi(oidValue);
                char[] pooledarray = null;
                try
                {
                    if (Interop.Crypt32.CryptFormatObject(X509_ASN_ENCODING, 0, dwFormatStrType, IntPtr.Zero, (byte*)oidValuePtr, rawData, rawData.Length, null, ref cbFormat))
                    {
                        int charLength = (cbFormat + 1) / 2;
                        Span<char> buffer = charLength <= 256 ?
                            stackalloc char[256] :
                            (pooledarray = ArrayPool<char>.Shared.Rent(charLength));
                        fixed (char* bufferPtr = buffer)
                        {
                            if (Interop.Crypt32.CryptFormatObject(X509_ASN_ENCODING, 0, dwFormatStrType, IntPtr.Zero, (byte*)oidValuePtr, rawData, rawData.Length, bufferPtr, ref cbFormat))
                            {
                                return new string(bufferPtr);
                            }
                        }
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(oidValuePtr);
                    if (pooledarray != null)
                    {
                        ArrayPool<char>.Shared.Return(pooledarray);
                    }
                }
            }

            return null;
        }
    }
}
