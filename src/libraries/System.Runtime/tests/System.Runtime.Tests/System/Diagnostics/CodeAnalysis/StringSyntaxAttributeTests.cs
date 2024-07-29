// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Diagnostics.CodeAnalysis.Tests
{
    public sealed class StringSyntaxAttributeTests
    {
        [Theory]
        [InlineData(StringSyntaxAttribute.DateTimeFormat)]
        [InlineData(StringSyntaxAttribute.Json)]
        [InlineData(StringSyntaxAttribute.Regex)]
        public void Ctor_Roundtrips(string syntax)
        {
            var attribute = new StringSyntaxAttribute(syntax);
            Assert.Equal(syntax, attribute.Syntax);
            Assert.Empty(attribute.Arguments);

            attribute = new StringSyntaxAttribute(syntax, "a", DayOfWeek.Monday);
            Assert.Equal(syntax, attribute.Syntax);
            Assert.Equal(new object[] { "a", DayOfWeek.Monday }, attribute.Arguments);
        }
    }
}
