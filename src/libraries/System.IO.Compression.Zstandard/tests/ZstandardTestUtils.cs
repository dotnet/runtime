// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.IO.Compression
{
    public static class ZstandardTestUtils
    {
        public static byte[] CreateSampleDictionary()
        {
            // Create a simple dictionary with some sample data
            return "a;owijfawoiefjawfafajzlf zfijf slifljeifa flejf;waiefjwaf"u8.ToArray();
        }

        public static byte[] CreateTestData(int size = 1000)
        {
            // Create test data of specified size
            byte[] data = new byte[size];
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = (byte)(i % 256); // Varying pattern
            }
            return data;
        }
    }
}