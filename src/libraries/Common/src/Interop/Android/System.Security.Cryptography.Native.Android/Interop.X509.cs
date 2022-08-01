// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

internal static partial class Interop
{
    internal static partial class AndroidCrypto
    {
        private const int INSUFFICIENT_BUFFER = -1;
        private const int SUCCESS = 1;

        [LibraryImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_X509Decode")]
        internal static partial SafeX509Handle X509Decode(ref byte buf, int len);

        [LibraryImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_X509Encode")]
        private static partial int X509Encode(SafeX509Handle x, byte[]? buf, ref int len);
        internal static byte[] X509Encode(SafeX509Handle x)
        {
            int len = 0;
            int ret = X509Encode(x, null, ref len);
            if (ret != INSUFFICIENT_BUFFER)
                throw new CryptographicException();

            byte[] encoded = new byte[len];
            ret = X509Encode(x, encoded, ref len);
            if (ret != SUCCESS)
                throw new CryptographicException();

            return encoded;
        }

        [LibraryImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_X509DecodeCollection")]
        private static partial int X509DecodeCollection(ref byte buf, int bufLen, IntPtr[]? ptrs, ref int handlesLen);
        internal static SafeX509Handle[] X509DecodeCollection(ReadOnlySpan<byte> data)
        {
            ref byte buf = ref MemoryMarshal.GetReference(data);
            int size = 0;
            int ret = X509DecodeCollection(ref buf, data.Length, null, ref size);
            if (ret == SUCCESS && size == 0)
                return Array.Empty<SafeX509Handle>();

            if (ret != INSUFFICIENT_BUFFER)
                throw new CryptographicException();

            IntPtr[] ptrs = new IntPtr[size];
            SafeX509Handle[] handles = new SafeX509Handle[ptrs.Length];
            for (var i = 0; i < handles.Length; i++)
            {
                handles[i] = new SafeX509Handle();
            }

            ret = X509DecodeCollection(ref buf, data.Length, ptrs, ref size);
            if (ret != SUCCESS)
            {
                foreach (SafeX509Handle handle in handles)
                {
                    handle.Dispose();
                }

                throw new CryptographicException();
            }

            for (var i = 0; i < handles.Length; i++)
            {
                Marshal.InitHandle(handles[i], ptrs[i]);
            }

            return handles;
        }

        [LibraryImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_X509ExportPkcs7")]
        private static partial int X509ExportPkcs7(IntPtr[] certs, int certsLen, byte[]? buf, ref int len);
        internal static byte[] X509ExportPkcs7(IntPtr[] certHandles)
        {
            int len = 0;
            int ret = X509ExportPkcs7(certHandles, certHandles.Length, null, ref len);
            if (ret != INSUFFICIENT_BUFFER)
                throw new CryptographicException();

            byte[] encoded = new byte[len];
            ret = X509ExportPkcs7(certHandles, certHandles.Length, encoded, ref len);
            if (ret != SUCCESS)
                throw new CryptographicException();

            return encoded;
        }

        [LibraryImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_X509GetContentType")]
        private static partial X509ContentType X509GetContentType(ref byte buf, int len);
        internal static X509ContentType X509GetContentType(ReadOnlySpan<byte> data)
        {
            return X509GetContentType(ref MemoryMarshal.GetReference(data), data.Length);
        }

        internal enum PAL_KeyAlgorithm
        {
            DSA,
            EC,
            RSA,
            UnknownAlgorithm = -1,
        }

        [LibraryImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_X509PublicKey")]
        internal static partial IntPtr X509GetPublicKey(SafeX509Handle x, PAL_KeyAlgorithm algorithm);
    }
}

namespace System.Security.Cryptography.X509Certificates
{
    internal sealed class SafeX509Handle : Interop.JObjectLifetime.SafeJObjectHandle
    {
        public SafeX509Handle()
        {
        }

        internal SafeX509Handle(IntPtr ptr)
            : base(ptr)
        {
        }
    }
}
