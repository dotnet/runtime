// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
using System;
using System.IO;
using System.Collections.Generic;
using System.Security.Cryptography;

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

        public static IEnumerable<object[]> GetArraysVariedBySize()
        {
            yield return new object[] { RandomNumberGenerator.GetBytes(0) };
            yield return new object[] { RandomNumberGenerator.GetBytes(1) };
            yield return new object[] { RandomNumberGenerator.GetBytes(2) };
            yield return new object[] { RandomNumberGenerator.GetBytes(256) };
            yield return new object[] { RandomNumberGenerator.GetBytes(512) };
            yield return new object[] { RandomNumberGenerator.GetBytes(1024) };
            yield return new object[] { RandomNumberGenerator.GetBytes(2047) };
            yield return new object[] { RandomNumberGenerator.GetBytes(2048) };
            yield return new object[] { RandomNumberGenerator.GetBytes(2049) };
            yield return new object[] { RandomNumberGenerator.GetBytes(2100) };
        }
    }
}
