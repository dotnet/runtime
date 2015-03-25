// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
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
