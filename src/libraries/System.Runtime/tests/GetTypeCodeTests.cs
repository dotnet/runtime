// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Xunit;

namespace System.Tests
{
    public static partial class GetTypeCodeTests
    {
        [Theory]
        [InlineData(TypeCode.Boolean, false)]
        [InlineData(TypeCode.Char, 'x')]
        [InlineData(TypeCode.Double, 42.0)]
        [InlineData(TypeCode.Int32, 42)]
        [InlineData(TypeCode.Int64, 42L)]
        [InlineData(TypeCode.Single, 42f)]
        [InlineData(TypeCode.UInt32, 42u)]
        [InlineData(TypeCode.UInt64, 42ul)]
        [InlineData(TypeCode.Byte, (byte)1)]
        [InlineData(TypeCode.Int16, (short)1)]
        [InlineData(TypeCode.SByte, (sbyte)1)]
        [InlineData(TypeCode.UInt16, (ushort)1)]
        public static void GetTypeCode_Primitives(TypeCode expected, IConvertible convertible)
        {
            Assert.Equal(expected, convertible.GetTypeCode());
        }

        [Fact]
        public static void FromDecimal()
        {
            Assert.Equal(TypeCode.Decimal, decimal.MinValue.GetTypeCode());
        }
    }
}
