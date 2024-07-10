// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Text.RegularExpressions;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.DotNet.Cli.Build.Framework;

namespace Microsoft.DotNet.CoreSetup.Test
{
    public class CommandResultAssertions
    {
        public CommandResult Result { get; }

        public CommandResultAssertions(CommandResult commandResult)
        {
            Result = commandResult;
        }

        public AndConstraint<CommandResultAssertions> ExitWith(int expectedExitCode)
        {
            Execute.Assertion.ForCondition(Result.ExitCode == expectedExitCode)
                .FailWith($"Expected command to exit with {expectedExitCode} but it did not.{GetDiagnosticsInfo()}");
            return new AndConstraint<CommandResultAssertions>(this);
        }

        public AndConstraint<CommandResultAssertions> Pass()
        {
            Execute.Assertion.ForCondition(Result.ExitCode == 0)
                .FailWith($"Expected command to pass but it did not.{GetDiagnosticsInfo()}");
            return new AndConstraint<CommandResultAssertions>(this);
        }

        public AndConstraint<CommandResultAssertions> Fail()
        {
            Execute.Assertion.ForCondition(Result.ExitCode != 0)
                .FailWith($"Expected command to fail but it did not.{GetDiagnosticsInfo()}");
            return new AndConstraint<CommandResultAssertions>(this);
        }

        public AndConstraint<CommandResultAssertions> HaveStdOut()
        {
            Execute.Assertion.ForCondition(!string.IsNullOrEmpty(Result.StdOut))
                .FailWith($"Command did not output anything to stdout{GetDiagnosticsInfo()}");
            return new AndConstraint<CommandResultAssertions>(this);
        }

        public AndConstraint<CommandResultAssertions> HaveStdOut(string expectedOutput)
        {
            Execute.Assertion.ForCondition(Result.StdOut.Equals(expectedOutput, StringComparison.Ordinal))
                .FailWith($"Command did not output with Expected Output. Expected: '{expectedOutput}'{GetDiagnosticsInfo()}");
            return new AndConstraint<CommandResultAssertions>(this);
        }

        public AndConstraint<CommandResultAssertions> HaveStdOutContaining(string pattern)
        {
            Execute.Assertion.ForCondition(Result.StdOut.Contains(pattern))
                .FailWith($"The command output did not contain expected result: '{pattern}'{GetDiagnosticsInfo()}");
            return new AndConstraint<CommandResultAssertions>(this);
        }

        public AndConstraint<CommandResultAssertions> NotHaveStdOutContaining(string pattern)
        {
            Execute.Assertion.ForCondition(!Result.StdOut.Contains(pattern))
                .FailWith($"The command output contained a result it should not have contained: '{pattern}'{GetDiagnosticsInfo()}");
            return new AndConstraint<CommandResultAssertions>(this);
        }

        public AndConstraint<CommandResultAssertions> HaveStdOutMatching(string pattern, RegexOptions options = RegexOptions.None)
        {
            Execute.Assertion.ForCondition(Regex.IsMatch(Result.StdOut, pattern, options))
                .FailWith($"Matching the command output failed. Pattern: '{pattern}'{GetDiagnosticsInfo()}");
            return new AndConstraint<CommandResultAssertions>(this);
        }

        public AndConstraint<CommandResultAssertions> NotHaveStdOutMatching(string pattern, RegexOptions options = RegexOptions.None)
        {
            Execute.Assertion.ForCondition(!Regex.IsMatch(Result.StdOut, pattern, options))
                .FailWith($"The command output matched a pattern is should not have matched. Pattern: '{pattern}'{GetDiagnosticsInfo()}");
            return new AndConstraint<CommandResultAssertions>(this);
        }

        public AndConstraint<CommandResultAssertions> HaveStdErr()
        {
            Execute.Assertion.ForCondition(!string.IsNullOrEmpty(Result.StdErr))
                .FailWith($"Command did not output anything to stderr.{GetDiagnosticsInfo()}");
            return new AndConstraint<CommandResultAssertions>(this);
        }

        public AndConstraint<CommandResultAssertions> HaveStdErrContaining(string pattern)
        {
            Execute.Assertion.ForCondition(Result.StdErr.Contains(pattern))
                .FailWith($"The command error output did not contain expected result: '{pattern}'{GetDiagnosticsInfo()}");
            return new AndConstraint<CommandResultAssertions>(this);
        }

        public AndConstraint<CommandResultAssertions> NotHaveStdErrContaining(string pattern)
        {
            Execute.Assertion.ForCondition(!Result.StdErr.Contains(pattern))
                .FailWith($"The command error output contained a result it should not have contained: '{pattern}'{GetDiagnosticsInfo()}");
            return new AndConstraint<CommandResultAssertions>(this);
        }

        public AndConstraint<CommandResultAssertions> HaveStdErrMatching(string pattern, RegexOptions options = RegexOptions.None)
        {
            Execute.Assertion.ForCondition(Regex.IsMatch(Result.StdErr, pattern, options))
                .FailWith($"Matching the command error output failed. Pattern: '{pattern}'{GetDiagnosticsInfo()}");
            return new AndConstraint<CommandResultAssertions>(this);
        }

        public AndConstraint<CommandResultAssertions> NotHaveStdOut()
        {
            Execute.Assertion.ForCondition(string.IsNullOrEmpty(Result.StdOut))
                .FailWith($"Expected command to not output to stdout but it did:{GetDiagnosticsInfo()}");
            return new AndConstraint<CommandResultAssertions>(this);
        }

        public AndConstraint<CommandResultAssertions> NotHaveStdErr()
        {
            Execute.Assertion.ForCondition(string.IsNullOrEmpty(Result.StdErr))
                .FailWith($"Expected command to not output to stderr but it did:{GetDiagnosticsInfo()}");
            return new AndConstraint<CommandResultAssertions>(this);
        }

        public AndConstraint<CommandResultAssertions> FileExists(string path)
        {
            Execute.Assertion.ForCondition(System.IO.File.Exists(path))
                .FailWith($"The command did not write the expected file: '{path}'{GetDiagnosticsInfo()}");
            return new AndConstraint<CommandResultAssertions>(this);
        }

        public AndConstraint<CommandResultAssertions> FileContains(string path, string pattern)
        {
            string fileContent = System.IO.File.ReadAllText(path);
            Execute.Assertion.ForCondition(fileContent.Contains(pattern))
                .FailWith($"The command did not write the expected result '{pattern}' to the file: '{path}'{GetDiagnosticsInfo()}{Environment.NewLine}file content: >>{fileContent}<<");
            return new AndConstraint<CommandResultAssertions>(this);
        }

        public AndConstraint<CommandResultAssertions> NotFileContains(string path, string pattern)
        {
            string fileContent = System.IO.File.ReadAllText(path);
            Execute.Assertion.ForCondition(!fileContent.Contains(pattern))
                .FailWith($"The command did not write the expected result '{pattern}' to the file: '{path}'{GetDiagnosticsInfo()}{Environment.NewLine}file content: >>{fileContent}<<");
            return new AndConstraint<CommandResultAssertions>(this);
        }

        public string GetDiagnosticsInfo()
            => $"""

                File Name: {Result.StartInfo.FileName}
                Arguments: {Result.StartInfo.Arguments}
                Environment:
                {string.Join(Environment.NewLine, Result.StartInfo.Environment.Where(i => i.Key.StartsWith(Constants.DotnetRoot.EnvironmentVariable)).Select(i => $"  {i.Key} = {i.Value}"))}
                Exit Code: 0x{Result.ExitCode:x}
                StdOut:
                {Result.StdOut}
                StdErr:
                {Result.StdErr}
                """;
    }
}
