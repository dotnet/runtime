// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace System.Buffers.Text.Tests
{
    public static class Base64TestHelper
    {
        public static string s_characters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/";

        // Pre-computing this table using a custom string(s_characters) and GenerateEncodingMapAndVerify (found in tests)
        public static readonly byte[] s_encodingMap = {
            65, 66, 67, 68, 69, 70, 71, 72,         //A..H
            73, 74, 75, 76, 77, 78, 79, 80,         //I..P
            81, 82, 83, 84, 85, 86, 87, 88,         //Q..X
            89, 90, 97, 98, 99, 100, 101, 102,      //Y..Z, a..f
            103, 104, 105, 106, 107, 108, 109, 110, //g..n
            111, 112, 113, 114, 115, 116, 117, 118, //o..v
            119, 120, 121, 122, 48, 49, 50, 51,     //w..z, 0..3
            52, 53, 54, 55, 56, 57, 43, 47          //4..9, +, /
        };

        // Pre-computing this table using a custom string(s_characters) and GenerateDecodingMapAndVerify (found in tests)
        public static readonly sbyte[] s_decodingMap = {
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 62, -1, -1, -1, 63,         //62 is placed at index 43 (for +), 63 at index 47 (for /)
            52, 53, 54, 55, 56, 57, 58, 59, 60, 61, -1, -1, -1, -1, -1, -1,         //52-61 are placed at index 48-57 (for 0-9), 64 at index 61 (for =)
            -1, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14,
            15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, -1, -1, -1, -1, -1,         //0-25 are placed at index 65-90 (for A-Z)
            -1, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40,
            41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51, -1, -1, -1, -1, -1,         //26-51 are placed at index 97-122 (for a-z)
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,         // Bytes over 122 ('z') are invalid and cannot be decoded
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,         // Hence, padding the map with 255, which indicates invalid input
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        };

        public static bool IsByteToBeIgnored(byte charByte) => charByte is (byte)' ' or (byte)'\t' or (byte)'\r' or (byte)'\n';

        public const byte EncodingPad = (byte)'=';      // '=', for padding
        public const sbyte InvalidByte = -1;            // Designating -1 for invalid bytes in the decoding map

        public static byte[] InvalidBytes
        {
            get
            {
                int[] indices = s_decodingMap.FindAllIndexOf(InvalidByte);
                // Workaround for indices.Cast<byte>().ToArray() since it throws
                // InvalidCastException: Unable to cast object of type 'System.Int32' to type 'System.Byte'
                return indices.Select(i => (byte)i).ToArray();
            }
        }

        internal static void InitializeBytes(Span<byte> bytes, int seed = 100)
        {
            var rnd = new Random(seed);
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = (byte)rnd.Next(0, byte.MaxValue + 1);
            }
        }

        internal static void InitializeDecodableBytes(Span<byte> bytes, int seed = 100)
        {
            var rnd = new Random(seed);
            for (int i = 0; i < bytes.Length; i++)
            {
                int index = (byte)rnd.Next(0, s_encodingMap.Length);
                bytes[i] = s_encodingMap[index];
            }
        }

        [Fact]
        public static void GenerateEncodingMapAndVerify()
        {
            byte[] data = new byte[64]; // Base64
            for (int i = 0; i < s_characters.Length; i++)
            {
                data[i] = (byte)s_characters[i];
            }
            Assert.True(s_encodingMap.AsSpan().SequenceEqual(data));
        }

        [Fact]
        public static void GenerateDecodingMapAndVerify()
        {
            sbyte[] data = new sbyte[256]; // 0 to byte.MaxValue (255)
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = InvalidByte;
            }
            for (int i = 0; i < s_characters.Length; i++)
            {
                data[s_characters[i]] = (sbyte)i;
            }
            Assert.True(s_decodingMap.AsSpan().SequenceEqual(data));
        }

        public static int[] FindAllIndexOf<T>(this IEnumerable<T> values, T valueToFind)
        {
            return values.Select((element, index) => Equals(element, valueToFind) ? index : -1).Where(index => index != -1).ToArray();
        }

        public static bool VerifyEncodingCorrectness(int expectedConsumed, int expectedWritten, Span<byte> source, Span<byte> encodedBytes)
        {
            string expectedText = Convert.ToBase64String(source.Slice(0, expectedConsumed).ToArray());
            string encodedText = Encoding.ASCII.GetString(encodedBytes.Slice(0, expectedWritten).ToArray());
            return expectedText.Equals(encodedText);
        }

        public static bool VerifyDecodingCorrectness(int expectedConsumed, int expectedWritten, Span<byte> source, Span<byte> decodedBytes)
        {
            string sourceString = Encoding.ASCII.GetString(source.Slice(0, expectedConsumed).ToArray());
            byte[] expectedBytes = Convert.FromBase64String(sourceString);
            return expectedBytes.AsSpan().SequenceEqual(decodedBytes.Slice(0, expectedWritten));
        }
    }
}
