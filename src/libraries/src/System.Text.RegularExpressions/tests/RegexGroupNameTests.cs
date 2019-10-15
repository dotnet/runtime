// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using System;
using System.Text.RegularExpressions;
using System.Collections;

namespace System.Text.RegularExpressionsTests
{
    /// <summary>
    /// Tests the Name property on the Group class.
    /// </summary>
    public class RegexGroupNameTests
    {
        [Fact]
        public static void NameTests()
        {
            string pattern = @"\b(?<FirstWord>\w+)\s?((\w+)\s)*(?<LastWord>\w+)?(?<Punctuation>\p{Po})";
            string input = "The cow jumped over the moon.";
            Regex regex = new Regex(pattern);
            Match match = regex.Match(input);
            Assert.True(match.Success);

            string[] names = regex.GetGroupNames();
            for (int i = 0; i < names.Length; i++)
            {
                Assert.Equal(names[i], match.Groups[i].Name);
            }
        }
    }
}
