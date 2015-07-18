// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;

namespace System.Net.Http.WinHttpHandlerUnitTests
{
    public static class TestServer
    {
        private static MemoryStream requestBody = null;
        private static MemoryStream responseBody = null;
        private static string responseHeaders = null;

        public static byte[] RequestBody
        {
            get
            {
                return requestBody.ToArray();
            }
        }

        public static byte[] ResponseBody
        {
            set
            {
                responseBody.Write(value, 0, value.Length);
                responseBody.Position = 0;
            }
        }

        public static string ResponseHeaders
        {
            get
            {
                return responseHeaders;
            }

            set
            {
                responseHeaders = value;
            }
        }

        public static void WriteToRequestBody(IntPtr buffer, uint bufferSize)
        {
            var bytes = new byte[bufferSize];

            Marshal.Copy(buffer, bytes, 0, (int)bufferSize);
            requestBody.Write(bytes, 0, (int)bufferSize);
        }

        public static void ReadFromResponseBody(IntPtr buffer, uint bufferSize, out uint bytesRead)
        {
            bytesRead = 0;

            if (responseBody == null)
            {
                return;
            }

            for (var i = 0; i < bufferSize; i++)
            {
                int data = responseBody.ReadByte();
                if (data == -1)
                {
                    return;
                }

                System.Runtime.InteropServices.Marshal.WriteByte(IntPtr.Add(buffer, i), (byte)data);
                bytesRead++;
            }
        }

        public static void SetResponse(DecompressionMethods compressionKind, string responseBody)
        {
            byte[] responseBodyBytes = Encoding.UTF8.GetBytes(responseBody);
            byte[] compressedBytes;
            string responseHeadersFormat =
                "HTTP/1.1 200 OK\r\n" +
                "{0}" +
                "Content-Length: {1}\r\n" +
                "Content-Type: text/plain; charset=utf-8\r\n" +
                "Date: Tue, 08 Jul 2014 20:45:02 GMT\r\n";

            if (compressionKind == DecompressionMethods.None)
            {
                compressedBytes = responseBodyBytes;
            }
            else
            {
                using (var memoryStream = new MemoryStream())
                {
                    Stream compressedStream = null;
                    if (compressionKind == DecompressionMethods.Deflate)
                    {
                        compressedStream = new DeflateStream(memoryStream, CompressionMode.Compress);
                    }
                    else
                    {
                        compressedStream = new GZipStream(memoryStream, CompressionMode.Compress);
                    }

                    compressedStream.Write(responseBodyBytes, 0, responseBodyBytes.Length);
                    compressedStream.Dispose();

                    compressedBytes = memoryStream.ToArray();
                }
            }

            ResponseBody = compressedBytes;

            string contentEncodingHeader = string.Empty;
            if (compressionKind != DecompressionMethods.None)
            {
                contentEncodingHeader = string.Format("Content-Encoding: {0}\r\n", compressionKind.ToString().ToLower());
            }

            ResponseHeaders = string.Format(responseHeadersFormat, contentEncodingHeader, compressedBytes.Length);
        }

        public static void Reset()
        {
            requestBody = new MemoryStream();
            responseBody = new MemoryStream();
            responseHeaders = null;
        }
    }
}
