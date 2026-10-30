// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Xunit;

namespace System.DirectoryServices.AccountManagement.Tests
{
    public class ADUtilsTests
    {
        private static string EscapeRFC2254SpecialChars(string input)
        {
            Type adUtilsType = typeof(PrincipalContext).Assembly.GetType("System.DirectoryServices.AccountManagement.ADUtils", throwOnError: true);
            MethodInfo method = adUtilsType.GetMethod("EscapeRFC2254SpecialChars", BindingFlags.Static | BindingFlags.NonPublic);
            return (string)method.Invoke(null, new object[] { input });
        }

        [Theory]
        [InlineData("simple", "simple")]
        [InlineData("has(paren", @"has\28paren")]
        [InlineData("has)paren", @"has\29paren")]
        [InlineData("has*star", @"has\2astar")]
        [InlineData(@"has\backslash", @"has\5cbackslash")]
        [InlineData("(all*special)\\chars", @"\28all\2aspecial\29\5cchars")]
        [InlineData("", "")]
        [InlineData("nothingspecial123", "nothingspecial123")]
        [InlineData("CN=user,OU=test (lab),DC=contoso,DC=com", @"CN=user,OU=test \28lab\29,DC=contoso,DC=com")]
        [InlineData("membername))", @"membername\29\29")]
        public void EscapeRFC2254SpecialChars_EscapesCorrectly(string input, string expected)
        {
            string result = EscapeRFC2254SpecialChars(input);

            Assert.Equal(expected, result);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotNetFramework))]
        public void EscapeRFC2254SpecialChars_EscapesNulCharacter()
        {
            string result = EscapeRFC2254SpecialChars("\0");

            Assert.Equal(@"\00", result);
        }
    }
}
