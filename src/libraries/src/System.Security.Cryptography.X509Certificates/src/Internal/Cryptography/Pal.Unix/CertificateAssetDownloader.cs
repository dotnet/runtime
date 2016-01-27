// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Win32.SafeHandles;

namespace Internal.Cryptography.Pal
{
    internal static class CertificateAssetDownloader
    {
        private static readonly Interop.Http.ReadWriteCallback s_writeCallback = CurlWriteCallback;

        internal static X509Certificate2 DownloadCertificate(string uri, ref TimeSpan remainingDownloadTime)
        {
            byte[] data = DownloadAsset(uri, ref remainingDownloadTime);

            if (data == null)
            {
                return null;
            }

            try
            {
                return new X509Certificate2(data);
            }
            catch (CryptographicException)
            {
                return null;
            }
        }

        internal static SafeX509CrlHandle DownloadCrl(string uri, ref TimeSpan remainingDownloadTime)
        {
            byte[] data = DownloadAsset(uri, ref remainingDownloadTime);

            if (data == null)
            {
                return null;
            }

            // DER-encoded CRL seems to be the most common off of some random spot-checking, so try DER first.
            SafeX509CrlHandle handle = Interop.Crypto.DecodeX509Crl(data, data.Length);

            if (!handle.IsInvalid)
            {
                return handle;
            }

            using (SafeBioHandle bio = Interop.Crypto.CreateMemoryBio())
            {
                Interop.Crypto.BioWrite(bio, data, data.Length);

                handle = Interop.Crypto.PemReadBioX509Crl(bio);

                if (!handle.IsInvalid)
                {
                    return handle;
                }
            }

            return null;
        }

        private static byte[] DownloadAsset(string uri, ref TimeSpan remainingDownloadTime)
        {
            if (remainingDownloadTime <= TimeSpan.Zero)
            {
                return null;
            }

            List<byte[]> dataPieces = new List<byte[]>();

            using (Interop.Http.SafeCurlHandle curlHandle = Interop.Http.EasyCreate())
            {
                GCHandle gcHandle = GCHandle.Alloc(dataPieces);
                Interop.Http.SafeCallbackHandle callbackHandle = new Interop.Http.SafeCallbackHandle();

                try
                {
                    Interop.Http.EasySetOptionString(curlHandle, Interop.Http.CURLoption.CURLOPT_URL, uri);
                    Interop.Http.EasySetOptionLong(curlHandle, Interop.Http.CURLoption.CURLOPT_FOLLOWLOCATION, 1L);

                    IntPtr dataHandlePtr = GCHandle.ToIntPtr(gcHandle);
                    Interop.Http.RegisterReadWriteCallback(
                        curlHandle,
                        Interop.Http.ReadWriteFunction.Write,
                        s_writeCallback,
                        dataHandlePtr,
                        ref callbackHandle);

                    Stopwatch stopwatch = Stopwatch.StartNew();
                    Interop.Http.CURLcode res = Interop.Http.EasyPerform(curlHandle);
                    stopwatch.Stop();

                    // TimeSpan.Zero isn't a worrisome value on the subtraction, it only
                    // means "no limit" on the original input.
                    remainingDownloadTime -= stopwatch.Elapsed;

                    if (res != Interop.Http.CURLcode.CURLE_OK)
                    {
                        return null;
                    }
                }
                finally
                {
                    gcHandle.Free();
                    callbackHandle.Dispose();
                }
            }

            if (dataPieces.Count == 0)
            {
                return null;
            }

            if (dataPieces.Count == 1)
            {
                return dataPieces[0];
            }

            int dataLen = 0;

            for (int i = 0; i < dataPieces.Count; i++)
            {
                dataLen += dataPieces[i].Length;
            }

            byte[] data = new byte[dataLen];
            int offset = 0;

            for (int i = 0; i < dataPieces.Count; i++)
            {
                byte[] piece = dataPieces[i];

                Buffer.BlockCopy(piece, 0, data, offset, piece.Length);
                offset += piece.Length;
            }

            return data;
        }

        private static ulong CurlWriteCallback(IntPtr buffer, ulong size, ulong nitems, IntPtr context)
        {
            ulong totalSize = size * nitems;

            if (totalSize == 0)
            {
                return 0;
            }

            GCHandle gcHandle = GCHandle.FromIntPtr(context);
            List<byte[]> dataPieces = (List<byte[]>)gcHandle.Target;
            byte[] piece = new byte[totalSize];

            Marshal.Copy(buffer, piece, 0, (int)totalSize);
            dataPieces.Add(piece);

            return totalSize;
        }
    }
}
