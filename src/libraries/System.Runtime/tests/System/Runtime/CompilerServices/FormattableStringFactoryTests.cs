// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Runtime.CompilerServices.Tests
{
    public class FormattableStringFactoryTests
    {
        [Fact]
        public void Create_InvalidArguments_Throws()
        {
            AssertExtensions.Throws<ArgumentNullException>("format", () => FormattableStringFactory.Create(null));
            AssertExtensions.Throws<ArgumentNullException>("arguments", () => FormattableStringFactory.Create("{0}", null));
        }

        [Theory]
        [InlineData("")]
        [InlineData("hello")]
        [InlineData("hel{0}o")]
        public void Create_FormatMatchesExpected(string format)
        {
            FormattableString fs = FormattableStringFactory.Create(format);
            Assert.Equal(format, fs.Format);
        }

        public static IEnumerable<object[]> Create_ArgumentsMatchExpected_MemberData()
        {
            yield return new object[] { new object[0] };
            yield return new object[] { new object[1] { new object() } };
            yield return new object[] { new object[2] { new object(), new object() } };
        }

        [Theory]
        [MemberData(nameof(Create_ArgumentsMatchExpected_MemberData))]
        public void Create_ArgumentsMatchExpected(object[] arguments)
        {
            FormattableString fs = FormattableStringFactory.Create("", arguments);
            Assert.Same(arguments, fs.GetArguments());
            Assert.Equal(arguments.Length, fs.ArgumentCount);
            for (int i = 0; i < arguments.Length; i++)
            {
                Assert.Same(arguments[i], fs.GetArgument(i));
            }
        }

        [Fact]
        public void Create_ToString_MatchesExpected()
        {
            Assert.Equal("hello, world", FormattableStringFactory.Create("hello{0} world", ',').ToString(null));
        }
    }
}
