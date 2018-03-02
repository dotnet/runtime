// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.VisualBasic.CompilerServices;
using Xunit;

namespace Microsoft.VisualBasic.Tests
{
    public class ConversionsTests
    {
        public static IEnumerable<object[]> ToBoolean_String_ReturnsExpected_TestData()
        {
            yield return new object[] { 5.5.ToString(CultureInfo.CurrentCulture), true };
            yield return new object[] { 0.0.ToString(CultureInfo.CurrentCulture), false };
        }

        [Theory]
        [InlineData("true", true)]
        [InlineData("false", false)]
        [InlineData("5", true)]
        [InlineData("0", false)]
        [InlineData("&h5", true)]
        [InlineData("&h0", false)]
        [InlineData("&o5", true)]
        [InlineData("&o0", false)]
        [MemberData(nameof(ToBoolean_String_ReturnsExpected_TestData))]
        public void ToBoolean_String_ReturnsExpected(string str, bool expected)
        {
            Assert.Equal(expected, Conversions.ToBoolean(str));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("yes")]
        [InlineData("contoso")]
        public void ToBoolean_String_ThrowsOnInvalidFormat(string str)
        {
            Assert.Throws<InvalidCastException>(() => Conversions.ToBoolean(str));
        }

        public static IEnumerable<object[]> ToBoolean_Object_ReturnsExpected_TestData()
        {
            yield return new object[] { null, false };
            yield return new object[] { false, false };
            yield return new object[] { true, true };
            yield return new object[] { (sbyte)0, false };
            yield return new object[] { (sbyte)42, true };
            yield return new object[] { (byte)0, false };
            yield return new object[] { (byte)42, true };
            yield return new object[] { (System.Int16)0, false };
            yield return new object[] { (System.Int16)42, true };
            yield return new object[] { (System.UInt16)0, false };
            yield return new object[] { (System.UInt16)42, true };
            yield return new object[] { (System.Int32)0, false };
            yield return new object[] { (System.Int32)42, true };
            yield return new object[] { (System.UInt32)0, false };
            yield return new object[] { (System.UInt32)42, true };
            yield return new object[] { (System.Int64)0, false };
            yield return new object[] { (System.Int64)42, true };
            yield return new object[] { (System.UInt64)0, false };
            yield return new object[] { (System.UInt64)42, true };
            yield return new object[] { 0.0m, false };
            yield return new object[] { 0.42m, true };
            yield return new object[] { (float)0.0, false };
            yield return new object[] { (float)0.42, true };
            yield return new object[] { (double)0.0, false };
            yield return new object[] { (double)0.42, true };
        }

        [Theory]
        [MemberData(nameof(ToBoolean_Object_ReturnsExpected_TestData))]
        public void ToBoolean_Object_ReturnsExpected(object obj, bool expected)
        {
            Assert.Equal(expected, Conversions.ToBoolean(obj));
        }
        
        [Fact]
        public void ToBoolean_Object_ThrowsOn_List()
        {
            Assert.Throws<InvalidCastException>(() => Conversions.ToBoolean(new List<string>()));
        }
    }
}
