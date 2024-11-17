// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Text.Encodings.Web.Tests
{
    public class SpanUtilityTests
    {
        public static IEnumerable<object[]> IsValidIndexTestData()
        {
            yield return new object[] { "", -1, false };
            yield return new object[] { "", 0, false };
            yield return new object[] { "", 1, false };
            yield return new object[] { "x", -1, false };
            yield return new object[] { "x", 0, true };
            yield return new object[] { "x", 1, false };
            yield return new object[] { "Hello", -1, false };
            yield return new object[] { "Hello", 0, true };
            yield return new object[] { "Hello", 4, true };
            yield return new object[] { "Hello", 5, false };
        }

        [Theory]
        [MemberData(nameof(IsValidIndexTestData))]
        public void IsValidIndex_ReadOnlySpan(string inputData, int index, bool expectedValue)
        {
            ReadOnlySpan<char> span = inputData.AsSpan();
            Assert.Equal(expectedValue, SpanUtility.IsValidIndex(span, index));
        }

        [Theory]
        [MemberData(nameof(IsValidIndexTestData))]
        public void IsValidIndex_Span(string inputData, int index, bool expectedValue)
        {
            Span<char> span = inputData.ToCharArray();
            Assert.Equal(expectedValue, SpanUtility.IsValidIndex(span, index));
        }
    }
}
