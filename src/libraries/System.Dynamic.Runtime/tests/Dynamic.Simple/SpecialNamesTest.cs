// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace SampleDynamicTests
{
    public class SpecialNamesTest
    {
        public static string operator !(SpecialNamesTest s)
        {
            return "Inverted!";
        }

        public static string op_LogicalNot(dynamic d)
        {
            return "Should not have been called.";
        }

        [Fact]
        public static void SpecialNamesTest_RunTest()
        {
            dynamic d = new SpecialNamesTest();

            Assert.Equal("Inverted!", (string)!d);
        }
    }
}
