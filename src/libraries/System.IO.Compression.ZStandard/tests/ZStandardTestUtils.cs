// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace System.IO.Compression
{
    public static class ZstandardTestUtils
    {
        public static byte[] CreateSampleDictionary()
        {
            // Create a simple dictionary with some sample data
            return "a;owijfawoiefjawfafajzlf zfijf slifljeifa flejf;waiefjwaf"u8.ToArray();
            // return new byte[]
            // {
            //     0x37, 0xA4, 0x30, 0xEC, // Zstandard magic number for dictionary
            //     0x01, 0x00, 0x00, 0x00, // Version and flags
            //     0x48, 0x65, 0x6C, 0x6C, 0x6F, 0x20, 0x57, 0x6F, 0x72, 0x6C, 0x64, // "Hello World"
            //     0x54, 0x68, 0x69, 0x73, 0x20, 0x69, 0x73, 0x20, 0x61, 0x20, 0x74, 0x65, 0x73, 0x74, // "This is a test"
            // };
        }
    }
}