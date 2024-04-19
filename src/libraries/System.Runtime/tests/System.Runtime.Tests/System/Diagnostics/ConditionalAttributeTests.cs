// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Xunit;

namespace System.Tests
{
    public static class ConditionalAttributeTests
    {
        [Fact]
        public static void Ctor()
        {
            var attribute0 = new ConditionalAttribute(null);
            Assert.Null(attribute0.ConditionString);

            var attribute = new ConditionalAttribute("CONDITION");
            Assert.Equal("CONDITION", attribute.ConditionString);
        }
    }
}
