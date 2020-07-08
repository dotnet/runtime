// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Microsoft.Extensions.CommandLineUtils
{
    public class ArgumentEscaperTests
    {
        [Theory]
        [InlineData(new[] { "one", "two", "three" }, "one two three")]
        [InlineData(new[] { "line1\nline2", "word1\tword2" }, "\"line1\nline2\" \"word1\tword2\"")]
        [InlineData(new[] { "with spaces" }, "\"with spaces\"")]
        [InlineData(new[] { @"with\backslash" }, @"with\backslash")]
        [InlineData(new[] { @"""quotedwith\backslash""" }, @"\""quotedwith\backslash\""")]
        [InlineData(new[] { @"C:\Users\" }, @"C:\Users\")]
        [InlineData(new[] { @"C:\Program Files\dotnet\" }, @"""C:\Program Files\dotnet\\""")]
        [InlineData(new[] { @"backslash\""preceedingquote" }, @"backslash\\\""preceedingquote")]
        public void EscapesArguments(string[] args, string expected)
        {
            Assert.Equal(expected, ArgumentEscaper.EscapeAndConcatenate(args));
        }
    }
}
