// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Text.RegularExpressions.Tests
{
    public class RegexCompilationInfoTests
    {
        private RegexCompilationInfo Instance
        {
            get => new RegexCompilationInfo("pattern", RegexOptions.None, "name", "fullnamespace", true, TimeSpan.FromSeconds(60));
        }

        public static IEnumerable<object[]> Ctor_MemberData()
        {
            yield return new object[] { string.Empty, RegexOptions.None, "name", string.Empty, false, TimeSpan.FromSeconds(60) };
            yield return new object[] { "pattern", RegexOptions.Compiled, "name", "fullnamespace", true, TimeSpan.FromSeconds(60) };
        }

        [Theory]
        [MemberData(nameof(Ctor_MemberData))]
        public void Ctor_ValidArguments_CheckProperties(string pattern, RegexOptions options, string name, string fullnamespace, bool ispublic, TimeSpan matchTimeout)
        {
            var regexCompilationInfo = new RegexCompilationInfo(pattern, options, name, fullnamespace, ispublic, matchTimeout);
            Assert.Equal(pattern, regexCompilationInfo.Pattern);
            Assert.Equal(options, regexCompilationInfo.Options);
            Assert.Equal(name, regexCompilationInfo.Name);
            Assert.Equal(fullnamespace, regexCompilationInfo.Namespace);
            Assert.Equal(ispublic, regexCompilationInfo.IsPublic);
            Assert.Equal(matchTimeout, regexCompilationInfo.MatchTimeout);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void IsPublic_GetSet(bool isPublic)
        {
            RegexCompilationInfo regexCompilationInfo = Instance;
            regexCompilationInfo.IsPublic = isPublic;
            Assert.Equal(isPublic, regexCompilationInfo.IsPublic);
        }

        public static IEnumerable<object[]> MatchTimeout_GetSet_Throws_MemberData()
        {
            yield return new object[] { TimeSpan.Zero };
            yield return new object[] { TimeSpan.FromMilliseconds(int.MaxValue) };
            yield return new object[] { TimeSpan.MinValue };
        }

        public static IEnumerable<object[]> MatchTimeout_GetSet_Success_MemberData()
        {
            yield return new object[] { TimeSpan.Zero.Add(TimeSpan.FromMilliseconds(1)) };
            yield return new object[] { TimeSpan.FromMilliseconds(int.MaxValue - 1) };
        }

        [Theory]
        [MemberData(nameof(MatchTimeout_GetSet_Throws_MemberData))]
        public void MatchTimeout_GetSet_Throws(TimeSpan matchTimeout)
        {
            RegexCompilationInfo regexCompilationInfo = Instance;
            AssertExtensions.Throws<ArgumentOutOfRangeException>("matchTimeout", () => 
                regexCompilationInfo.MatchTimeout = matchTimeout);            
        }

        [Theory]
        [MemberData(nameof(MatchTimeout_GetSet_Success_MemberData))]
        public void MatchTimeout_GetSet_Success(TimeSpan matchTimeout)
        {
            RegexCompilationInfo regexCompilationInfo = Instance;
            regexCompilationInfo.MatchTimeout = matchTimeout;
            Assert.Equal(matchTimeout, regexCompilationInfo.MatchTimeout);
        }

        [Fact]
        public void Name_GetSet()
        {
            RegexCompilationInfo regexCompilationInfo = Instance;
            AssertExtensions.Throws<ArgumentNullException>("Name", "value", () => regexCompilationInfo.Name = null);
            AssertExtensions.Throws<ArgumentException>("Name", "value", () => regexCompilationInfo.Name = string.Empty);
            regexCompilationInfo.Name = "Name";
            Assert.Equal("Name", regexCompilationInfo.Name);
        }

        [Fact]
        public void Namespace_GetSet()
        {
            RegexCompilationInfo regexCompilationInfo = Instance;
            AssertExtensions.Throws<ArgumentNullException>("Namespace", "value", () => regexCompilationInfo.Namespace = null);
            regexCompilationInfo.Namespace = string.Empty;
            Assert.Equal(string.Empty, regexCompilationInfo.Namespace);
            regexCompilationInfo.Namespace = "Namespace";
            Assert.Equal("Namespace", regexCompilationInfo.Namespace);
        }

        [Fact]
        public void Options_GetSet()
        {
            RegexCompilationInfo regexCompilationInfo = Instance;
            regexCompilationInfo.Options = RegexOptions.None;
            Assert.Equal(RegexOptions.None, regexCompilationInfo.Options);
            regexCompilationInfo.Options = RegexOptions.Compiled | RegexOptions.CultureInvariant;
            Assert.Equal(RegexOptions.Compiled | RegexOptions.CultureInvariant, regexCompilationInfo.Options);
        }

        [Fact]
        public void Pattern_GetSet()
        {
            RegexCompilationInfo regexCompilationInfo = Instance;
            AssertExtensions.Throws<ArgumentNullException>("Pattern", "value", () => regexCompilationInfo.Pattern = null);
            regexCompilationInfo.Pattern = string.Empty;
            Assert.Equal(string.Empty, regexCompilationInfo.Pattern);
            regexCompilationInfo.Pattern = "Pattern";
            Assert.Equal("Pattern", regexCompilationInfo.Pattern);
        }
    }
}
