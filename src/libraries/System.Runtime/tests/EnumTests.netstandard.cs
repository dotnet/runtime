// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using Xunit;

namespace System.Tests
{
    public static partial class EnumTests
    {
        [Theory]
        [InlineData(SimpleEnum.Blue, TypeCode.Int32)]
        [InlineData(ByteEnum.Max, TypeCode.Byte)]
        [InlineData(SByteEnum.Min, TypeCode.SByte)]
        [InlineData(UInt16Enum.Max, TypeCode.UInt16)]
        [InlineData(Int16Enum.Min, TypeCode.Int16)]
        [InlineData(UInt32Enum.Max, TypeCode.UInt32)]
        [InlineData(Int32Enum.Min, TypeCode.Int32)]
        [InlineData(UInt64Enum.Max, TypeCode.UInt64)]
        [InlineData(Int64Enum.Min, TypeCode.Int64)]
        public static void GetTypeCode(Enum e, TypeCode expected)
        {
            Assert.Equal(expected, e.GetTypeCode());
        }

#pragma warning disable 618
        [Fact]
        public static void ToString_FormatProvider()
        {
            // FormatProvider parameter is ignored
            Assert.Equal("Blue", SimpleEnum.Blue.ToString(CultureInfo.CurrentCulture));
        }

        [Theory]
        [MemberData(nameof(ToString_Format_TestData))]
        public static void ToString_FormatProvider2(Enum e, string format, string expected)
        {
            // FormatProvider parameter is ignored
            Assert.Equal(expected, e.ToString(format, CultureInfo.CurrentCulture));
        }
#pragma warning restore 618        
    }
}
