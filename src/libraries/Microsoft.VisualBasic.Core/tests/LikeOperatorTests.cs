// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.VisualBasic.CompilerServices.Tests
{
    public class LikeOperatorTests
    {
        [Theory]
        [MemberData(nameof(LikeObject_TestData))]
        [MemberData(nameof(LikeString_TestData))]
        public void LikeObject(object source, object pattern, object expectedBinaryCompare, object expectedTextCompare)
        {
            Assert.Equal(expectedBinaryCompare, LikeOperator.LikeObject(source, pattern, CompareMethod.Binary));
            Assert.Equal(expectedTextCompare, LikeOperator.LikeObject(source, pattern, CompareMethod.Text));
        }

        [Theory]
        [MemberData(nameof(LikeString_TestData))]
        public void LikeString(string source, string pattern, bool expectedBinaryCompare, bool expectedTextCompare)
        {
            Assert.Equal(expectedBinaryCompare, LikeOperator.LikeString(source, pattern, CompareMethod.Binary));
            Assert.Equal(expectedTextCompare, LikeOperator.LikeString(source, pattern, CompareMethod.Text));
        }

        private static IEnumerable<object[]> LikeObject_TestData()
        {
            yield return new object[] { null, new[] { '*' }, true, true };
            yield return new object[] { new char[0], null, true, true };
            yield return new object[] { "a3", new[] { 'A', '#' }, false, true };
            yield return new object[] { new[] { 'A', '3' }, "a#", false, true };
        }

        private static IEnumerable<object[]> LikeString_TestData()
        {
            yield return new object[] { null, null, true, true };
            yield return new object[] { null, "*", true, true };
            yield return new object[] { "", null, true, true };
            yield return new object[] { "", "*", true, true };
            yield return new object[] { "", "?", false, false };
            yield return new object[] { "a", "?", true, true };
            yield return new object[] { "a3", "[A-Z]#", false, true };
            yield return new object[] { "A3", "[a-a]#", false, true };
        }
    }
}
