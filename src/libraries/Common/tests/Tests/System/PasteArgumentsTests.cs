// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

using Xunit;

namespace Tests.System
{
    public class PasteArgumentsTests
    {
        [Theory]
        [InlineData(@"app.exe arg1 arg2", new[] {"app.exe", "arg1", "arg2"})]
        [InlineData(@"""app name.exe"" arg1 arg2", new[] {"app name.exe", "arg1", "arg2"})]
        [InlineData(@"app.exe \\ arg2", new[] {"app.exe", @"\\", "arg2"})]
        [InlineData(@"app.exe ""\"""" arg2", new[] {"app.exe", @"""", "arg2"})]  // literal double quotation mark character
        [InlineData(@"app.exe ""\\\"""" arg2", new[] {"app.exe", @"\""", "arg2"})]    // 2N+1 backslashes before quote rule
        [InlineData(@"app.exe ""\\\\\"""" arg2", new[] {"app.exe", @"\\""", "arg2"})]  // 2N backslashes before quote rule
        public void Pastes(string pasteExpected, string[] arguments)
        {
            Assert.Equal(pasteExpected, PasteArguments.Paste(arguments, pasteFirstArgumentUsingArgV0Rules: true));
        }

        [Theory]
        [InlineData(@"""dir/app\""name.exe""", new[] {@"dir/app""name.exe"})]  // no throwing on quotes, escaping quotes
        [InlineData(@"""dir/app\\\""name.exe""", new[] {@"dir/app\""name.exe"})]  // escaping a backslash
        [InlineData(@"""dir/app\\\\\""name.exe""", new[] {@"dir/app\\""name.exe"})]  // escaping backslashes
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        public void Paste_Argv0Rules_Ignored_onUnix(string pasteExpected, string[] arguments)
        {
            Assert.Equal(pasteExpected, PasteArguments.Paste(arguments, pasteFirstArgumentUsingArgV0Rules: true));
        }

        [Theory]
        [InlineData(@"dir/app""name.exe")]  // throws
        [InlineData(@"dir/app\""name.exe")]  // throws and ignores a backslash
        [InlineData(@"dir/app\\""name.exe")]  // throws and ignores backslashes
        [PlatformSpecific(TestPlatforms.Windows)]
        public void Paste_Argv0Rules_ThrowsIfQuotes_OnWindows(string argv0)
        {
            Assert.Throws<ApplicationException>(() => PasteArguments.Paste(new []{argv0}, pasteFirstArgumentUsingArgV0Rules: true));
        }
    }
}
