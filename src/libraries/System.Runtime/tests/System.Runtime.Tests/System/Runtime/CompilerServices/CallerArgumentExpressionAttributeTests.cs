// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Runtime.CompilerServices.Tests
{
    public static class CallerArgumentExpressionAttributeTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("paramName")]
        public static void Ctor_ParameterName_Roundtrip(string value)
        {
            var caea = new CallerArgumentExpressionAttribute(value);
            Assert.Equal(value, caea.ParameterName);
        }

        [Fact]
        public static void BasicTest()
        {
            // Just a quick test to validate basic behavior. Compiler tests validate it fully.
            Assert.Equal("\"hello\"", GetValue("hello"));
            Assert.Equal("3 + 2", GetValue(3 + 2));
            Assert.Equal("new object()", GetValue(new object()));
        }

        private static string GetValue(object argument, [CallerArgumentExpression(nameof(argument))] string expr = null) => expr;
    }
}
