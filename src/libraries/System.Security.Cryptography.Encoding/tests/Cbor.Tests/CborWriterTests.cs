// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Encoding.Tests.Cbor
{
    public partial class CborWriterTests
    {
        [Fact]
        public static void IsWriteCompleted_OnWrittenPrimitive_ShouldBeTrue()
        {
            using var writer = new CborWriter();
            Assert.False(writer.IsWriteCompleted);
            writer.WriteInt64(42);
            Assert.True(writer.IsWriteCompleted);
        }

        [Fact]
        public static void ToArray_OnInCompleteValue_ShouldThrowInvalidOperationExceptoin()
        {
            using var writer = new CborWriter();
            Assert.Throws<InvalidOperationException>(() => writer.ToArray());
        }

        [Fact]
        public static void CborWriter_WritingTwoPrimitiveValues_ShouldThrowInvalidOperationException()
        {
            using var writer = new CborWriter();
            writer.WriteInt64(42);
            Assert.Throws<InvalidOperationException>(() => writer.WriteTextString("lorem ipsum"));
        }
    }
}
