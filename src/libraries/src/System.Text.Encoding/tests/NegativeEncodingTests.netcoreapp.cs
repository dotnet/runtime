// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Xunit;

namespace System.Text.Tests
{
    public static partial class NegativeEncodingTests
    {
        [Theory]
        [MemberData(nameof(Encodings_TestData))]
        public static unsafe void GetByteCount_Invalid_NetCoreApp(Encoding encoding)
        {
            // Chars is null
            AssertExtensions.Throws<ArgumentNullException>("s", () => encoding.GetByteCount((string)null, 0, 0));

            // Index < 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => encoding.GetByteCount("abc", -1, 0));

            // Count < 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => encoding.GetByteCount("abc", 0, -1));

            // Index + count > chars.Length
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => encoding.GetByteCount("abc", 0, 4));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => encoding.GetByteCount("abc", 1, 3));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => encoding.GetByteCount("abc", 2, 2));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => encoding.GetByteCount("abc", 3, 1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => encoding.GetByteCount("abc", 4, 0));
        }

        [Theory]
        [MemberData(nameof(Encodings_TestData))]
        public static unsafe void GetBytes_Invalid_NetCoreApp(Encoding encoding)
        {
            // Source is null
            AssertExtensions.Throws<ArgumentNullException>("s", () => encoding.GetBytes((string)null, 0, 0));

            // CharIndex < 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => encoding.GetBytes("a", -1, 0));

            // CharCount < 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => encoding.GetBytes("a", 0, -1));

            // CharIndex + charCount > source.Length
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => encoding.GetBytes("a", 2, 0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => encoding.GetBytes("a", 1, 1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => encoding.GetBytes("a", 0, 2));
        }
    }
}
