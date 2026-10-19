// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
using System;
using System.IO;
using System.Collections.Generic;

namespace System.IO.Tests
{
    public class MemoryStream_ToArrayTests
    {
        [Theory]
        [MemberData(nameof(GetArraysVariedBySize))]
        public static void ToArray_ZeroOffset(byte[] array)
        {
            var stream = new MemoryStream();
            stream.Write(array);
            var newArray = stream.ToArray();

            Assert.Equal(array.Length, newArray.Length);
            Assert.Equal(array, newArray);
        }

        [Theory]
        [MemberData(nameof(GetArraysVariedBySize))]
        public static void ToArray_Offset(byte[] array)
        {
            int index = 0;
            int count = array.Length;

            if (count > 3)
            {
                // Trim some off each end
                index = 1;
                count -= 3;
            }

            var stream = new MemoryStream(array, index, count);
            var newArray = stream.ToArray();

            Assert.Equal(count, newArray.Length);
            Assert.True(array.AsSpan(index, count).SequenceEqual(newArray));
        }

        private static byte[] GetRandomBytes(int count)
        {
            byte[] bytes = new byte[count];
            Random.Shared.NextBytes(bytes);
            return bytes;
        }

        public static IEnumerable<object[]> GetArraysVariedBySize()
        {
            yield return new object[] { GetRandomBytes(0) };
            yield return new object[] { GetRandomBytes(1) };
            yield return new object[] { GetRandomBytes(2) };
            yield return new object[] { GetRandomBytes(256) };
            yield return new object[] { GetRandomBytes(512) };
            yield return new object[] { GetRandomBytes(1024) };
            yield return new object[] { GetRandomBytes(2047) };
            yield return new object[] { GetRandomBytes(2048) };
            yield return new object[] { GetRandomBytes(2049) };
            yield return new object[] { GetRandomBytes(2100) };
        }
    }
}
