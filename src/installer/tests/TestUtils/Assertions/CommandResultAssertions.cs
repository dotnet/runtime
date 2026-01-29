// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text.RegularExpressions;
using Microsoft.DotNet.Cli.Build.Framework;
using Xunit;

namespace Microsoft.DotNet.CoreSetup.Test
{
    public class CommandResultAssertions
    {
        public CommandResult Result { get; }

        public CommandResultAssertions(CommandResult commandResult)
        {
            Result = commandResult;
        }

        public CommandResultAssertions ExitWith(int expectedExitCode)
        {
            // Some Unix systems will have 8 bit exit codes
            if (!OperatingSystem.IsWindows())
                expectedExitCode = expectedExitCode & 0xFF;

            Assert.True(Result.ExitCode == expectedExitCode, $"Expected command to exit with {expectedExitCode} but it did not.{GetDiagnosticsInfo()}");
            return this;
        }

        public CommandResultAssertions Pass()
        {
            Assert.True(Result.ExitCode == 0, $"Expected command to pass but it did not.{GetDiagnosticsInfo()}");
            return this;
        }

        public CommandResultAssertions Fail()
        {
            Assert.True(Result.ExitCode != 0, $"Expected command to fail but it did not.{GetDiagnosticsInfo()}");
            return this;
        }

        public CommandResultAssertions HaveStdOut()
        {
            Assert.True(!string.IsNullOrEmpty(Result.StdOut), $"Command did not output anything to stdout{GetDiagnosticsInfo()}");
            return this;
        }

        public CommandResultAssertions HaveStdOut(string expectedOutput)
        {
            Assert.True(Result.StdOut.Equals(expectedOutput, StringComparison.Ordinal), $"Command did not output with Expected Output. Expected: '{expectedOutput}'{GetDiagnosticsInfo()}");
            return this;
        }

        public CommandResultAssertions HaveStdOutContaining(string pattern)
        {
            Assert.True(!string.IsNullOrEmpty(Result.StdOut) && Result.StdOut.Contains(pattern), $"The command output did not contain expected result: '{pattern}'{GetDiagnosticsInfo()}");
            return this;
        }

        public CommandResultAssertions NotHaveStdOutContaining(string pattern)
        {
            Assert.True(!Result.StdOut.Contains(pattern), $"The command output contained a result it should not have contained: '{pattern}'{GetDiagnosticsInfo()}");
            return this;
        }

        public CommandResultAssertions HaveStdOutMatching(string pattern, RegexOptions options = RegexOptions.None)
        {
            Assert.True(Regex.IsMatch(Result.StdOut, pattern, options), $"Matching the command output failed. Pattern: '{pattern}'{GetDiagnosticsInfo()}");
            return this;
        }

        public CommandResultAssertions NotHaveStdOutMatching(string pattern, RegexOptions options = RegexOptions.None)
        {
            Assert.True(!Regex.IsMatch(Result.StdOut, pattern, options), $"The command output matched a pattern is should not have matched. Pattern: '{pattern}'{GetDiagnosticsInfo()}");
            return this;
        }

        public CommandResultAssertions HaveStdErr()
        {
            Assert.True(!string.IsNullOrEmpty(Result.StdErr), $"Command did not output anything to stderr.{GetDiagnosticsInfo()}");
            return this;
        }

        public CommandResultAssertions HaveStdErrContaining(string pattern)
        {
            Assert.True(!string.IsNullOrEmpty(Result.StdErr) && Result.StdErr.Contains(pattern), $"The command error output did not contain expected result: '{pattern}'{GetDiagnosticsInfo()}");
            return this;
        }

        public CommandResultAssertions NotHaveStdErrContaining(string pattern)
        {
            Assert.True(!Result.StdErr.Contains(pattern), $"The command error output contained a result it should not have contained: '{pattern}'{GetDiagnosticsInfo()}");
            return this;
        }

        public CommandResultAssertions HaveStdErrMatching(string pattern, RegexOptions options = RegexOptions.None)
        {
            Assert.True(Regex.IsMatch(Result.StdErr, pattern, options), $"Matching the command error output failed. Pattern: '{pattern}'{GetDiagnosticsInfo()}");
            return this;
        }

        public CommandResultAssertions NotHaveStdOut()
        {
            Assert.True(string.IsNullOrEmpty(Result.StdOut), $"Expected command to not output to stdout but it did:{GetDiagnosticsInfo()}");
            return this;
        }

        public CommandResultAssertions NotHaveStdErr()
        {
            Assert.True(string.IsNullOrEmpty(Result.StdErr), $"Expected command to not output to stderr but it did:{GetDiagnosticsInfo()}");
            return this;
        }

        public CommandResultAssertions FileExists(string path)
        {
            Assert.True(System.IO.File.Exists(path), $"The command did not write the expected file: '{path}'{GetDiagnosticsInfo()}");
            return this;
        }

        public CommandResultAssertions FileContains(string path, string pattern)
        {
            string fileContent = System.IO.File.ReadAllText(path);
            Assert.True(fileContent.Contains(pattern), $"The command did not write the expected result '{pattern}' to the file: '{path}'{GetDiagnosticsInfo()}{Environment.NewLine}file content: >>{fileContent}<<");
            return this;
        }

        public CommandResultAssertions NotFileContains(string path, string pattern)
        {
            string fileContent = System.IO.File.ReadAllText(path);
            Assert.True(!fileContent.Contains(pattern), $"The command did not write the expected result '{pattern}' to the file: '{path}'{GetDiagnosticsInfo()}{Environment.NewLine}file content: >>{fileContent}<<");
            return this;
        }

        public string GetDiagnosticsInfo() => Result.GetDiagnosticsInfo();
    }
}
