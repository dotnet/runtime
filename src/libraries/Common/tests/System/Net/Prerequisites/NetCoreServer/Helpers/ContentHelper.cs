// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace NetCoreServer
{
    public class ContentHelper
    {
        public static byte[] GetDeflateBytes(string data)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(data);
            var compressedStream = new MemoryStream();

            using (var compressor = new DeflateStream(compressedStream, CompressionMode.Compress, true))
            {
                compressor.Write(bytes, 0, bytes.Length);
            }

            return compressedStream.ToArray();
        }

        public static byte[] GetGZipBytes(string data)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(data);
            var compressedStream = new MemoryStream();

            using (var compressor = new GZipStream(compressedStream, CompressionMode.Compress, true))
            {
                compressor.Write(bytes, 0, bytes.Length);
            }

            return compressedStream.ToArray();

        }

        public static byte[] ComputeMD5Hash(string data)
        {
            return ComputeMD5Hash(Encoding.UTF8.GetBytes(data));
        }

        public static byte[] ComputeMD5Hash(byte[] data)
        {
            using (MD5 md5 = MD5.Create())
            {
                return md5.ComputeHash(data);
            }
        }
    }
}
