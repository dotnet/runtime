// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Xunit;

namespace Tests.System
{
    public class StringExtensionTests
    {
        [Theory]
        [InlineData("", 0, 0, "")]
        [InlineData("  ", 0, 0, "")]
        [InlineData("  ", 0, 2, "")]
        [InlineData("  ", 1, 1, "")]
        [InlineData("hello", 1, 0, "")]
        [InlineData("hello", 0, 5, "hello")]
        [InlineData("hello", 1, 4, "ello")]
        [InlineData("hello", 1, 3, "ell")]
        [InlineData("hello", 2, 1, "l")]
        [InlineData("hello", 4, 1, "o")]
        [InlineData(" hello\t\t ", 0, 9, "hello")]
        [InlineData(" hello\r\t ", 1, 5, "hello")]
        [InlineData(" hello\t\n ", 6, 3, "")]
        [InlineData(" hello\t\n ", 8, 1, "")]
        [InlineData(" hello\t\n ", 9, 0, "")]
        public void SubstringTrim_VariousInputsOutputs(string source, int startIndex, int length, string expectedResult)
        {
            Action<string> validate = result =>
            {
                Assert.Equal(expectedResult, result);
                if (result.Length == 0)
                {
                    Assert.Same(string.Empty, result);
                }
                else if (result.Length == source.Length)
                {
                    Assert.Same(source, result);
                }
            };

            validate(source.SubstringTrim(startIndex, length));

            if (length == source.Length - startIndex)
            {
                validate(source.SubstringTrim(startIndex));
            }
        }
    }
}
