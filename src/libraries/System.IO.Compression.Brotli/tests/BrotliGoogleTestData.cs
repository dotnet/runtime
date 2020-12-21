// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.IO.Compression.Tests
{
    public class BrotliGoogleTestData
    {
        public static IEnumerable<object[]> GoogleTestData()
        {
            yield return new object[] { Path.Combine("UncompressedTestFiles", "GoogleTestData", "10x10y") };
            yield return new object[] { Path.Combine("UncompressedTestFiles", "GoogleTestData", "64x") };
            yield return new object[] { Path.Combine("UncompressedTestFiles", "GoogleTestData", "backward65536") };
            yield return new object[] { Path.Combine("UncompressedTestFiles", "GoogleTestData", "compressed_file") };
            yield return new object[] { Path.Combine("UncompressedTestFiles", "GoogleTestData", "compressed_repeated") };
            yield return new object[] { Path.Combine("UncompressedTestFiles", "GoogleTestData", "empty") };
            yield return new object[] { Path.Combine("UncompressedTestFiles", "GoogleTestData", "mapsdatazrh") };
            yield return new object[] { Path.Combine("UncompressedTestFiles", "GoogleTestData", "monkey") };
            yield return new object[] { Path.Combine("UncompressedTestFiles", "GoogleTestData", "quickfox") };
            yield return new object[] { Path.Combine("UncompressedTestFiles", "GoogleTestData", "quickfox_repeated") };
            yield return new object[] { Path.Combine("UncompressedTestFiles", "GoogleTestData", "random_org_10k.bin") };
            yield return new object[] { Path.Combine("UncompressedTestFiles", "GoogleTestData", "x") };
            yield return new object[] { Path.Combine("UncompressedTestFiles", "GoogleTestData", "ukkonooa") };
            yield return new object[] { Path.Combine("UncompressedTestFiles", "GoogleTestData", "xyzzy") };
            yield return new object[] { Path.Combine("UncompressedTestFiles", "GoogleTestData", "zeros") };
        }

        private string CompressedTestFile(string uncompressedPath) => Path.Combine("BrotliTestData", "GoogleTestData", Path.GetFileName(uncompressedPath) + ".br");


        [Theory]
        [MemberData(nameof(GoogleTestData))]
        public void DecompressFile(string fileName)
        {
            byte[] bytes = File.ReadAllBytes(CompressedTestFile(fileName));
            byte[] expected = File.ReadAllBytes(fileName);

            ValidateCompressedData(bytes, expected);
        }

        [Theory]
        [MemberData(nameof(GoogleTestData))]
        public void RoundtripCompressDecompressFile(string fileName)
        {
            byte[] bytes = File.ReadAllBytes(fileName);
            MemoryStream memoryStream = new MemoryStream();
            using (BrotliStream brotliStream = new BrotliStream(memoryStream, CompressionMode.Compress, true))
            {
                brotliStream.Write(bytes, 0, bytes.Length);
            }
            memoryStream.Position = 0;
            ValidateCompressedData(memoryStream.ToArray(), bytes);
            memoryStream.Dispose();
        }

        private void ValidateCompressedData(byte[] compressedData, byte[] expected)
        {
            MemoryStream compressed = new MemoryStream(compressedData);
            using (MemoryStream decompressed = new MemoryStream())
            using (var decompressor = new BrotliStream(compressed, CompressionMode.Decompress, true))
            {
                decompressor.CopyTo(decompressed);
                Assert.Equal(expected.Length, decompressed.ToArray().Length);
                Assert.Equal<byte>(expected, decompressed.ToArray());
            }
        }
    }

}
