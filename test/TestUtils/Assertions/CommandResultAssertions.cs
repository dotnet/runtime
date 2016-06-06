// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text.RegularExpressions;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.DotNet.Cli.Build.Framework;

namespace Microsoft.DotNet.CoreSetup.Test
{
    public class CommandResultAssertions
    {
        private CommandResult _commandResult;

        public CommandResultAssertions(CommandResult commandResult)
        {
            _commandResult = commandResult;
        }

        public AndConstraint<CommandResultAssertions> ExitWith(int expectedExitCode)
        {
            Execute.Assertion.ForCondition(_commandResult.ExitCode == expectedExitCode)
                .FailWith(AppendDiagnosticsTo($"Expected command to exit with {expectedExitCode} but it did not."));
            return new AndConstraint<CommandResultAssertions>(this);
        }

        public AndConstraint<CommandResultAssertions> Pass()
        {
            Execute.Assertion.ForCondition(_commandResult.ExitCode == 0)
                .FailWith(AppendDiagnosticsTo($"Expected command to pass but it did not."));
            return new AndConstraint<CommandResultAssertions>(this);
        }

        public AndConstraint<CommandResultAssertions> Fail()
        {
            Execute.Assertion.ForCondition(_commandResult.ExitCode != 0)
                .FailWith(AppendDiagnosticsTo($"Expected command to fail but it did not."));
            return new AndConstraint<CommandResultAssertions>(this);
        }

        public AndConstraint<CommandResultAssertions> HaveStdOut()
        {
            Execute.Assertion.ForCondition(!string.IsNullOrEmpty(_commandResult.StdOut))
                .FailWith(AppendDiagnosticsTo("Command did not output anything to stdout"));
            return new AndConstraint<CommandResultAssertions>(this);
        }

        public AndConstraint<CommandResultAssertions> HaveStdOut(string expectedOutput)
        {
            Execute.Assertion.ForCondition(_commandResult.StdOut.Equals(expectedOutput, StringComparison.Ordinal))
                .FailWith(AppendDiagnosticsTo($"Command did not output with Expected Output. Expected: {expectedOutput}"));
            return new AndConstraint<CommandResultAssertions>(this);
        }

        public AndConstraint<CommandResultAssertions> HaveStdOutContaining(string pattern)
        {
            Execute.Assertion.ForCondition(_commandResult.StdOut.Contains(pattern))
                .FailWith(AppendDiagnosticsTo($"The command output did not contain expected result: {pattern}{Environment.NewLine}"));
            return new AndConstraint<CommandResultAssertions>(this);
        }

        public AndConstraint<CommandResultAssertions> HaveStdOutMatching(string pattern, RegexOptions options = RegexOptions.None)
        {
            Execute.Assertion.ForCondition(Regex.Match(_commandResult.StdOut, pattern, options).Success)
                .FailWith(AppendDiagnosticsTo($"Matching the command output failed. Pattern: {pattern}{Environment.NewLine}"));
            return new AndConstraint<CommandResultAssertions>(this);
        }

        public AndConstraint<CommandResultAssertions> HaveStdErr()
        {
            Execute.Assertion.ForCondition(!string.IsNullOrEmpty(_commandResult.StdErr))
                .FailWith(AppendDiagnosticsTo("Command did not output anything to stderr."));
            return new AndConstraint<CommandResultAssertions>(this);
        }

        public AndConstraint<CommandResultAssertions> HaveStdErrContaining(string pattern)
        {
            Execute.Assertion.ForCondition(_commandResult.StdErr.Contains(pattern))
                .FailWith(AppendDiagnosticsTo($"The command error output did not contain expected result: {pattern}{Environment.NewLine}"));
            return new AndConstraint<CommandResultAssertions>(this);
        }

        public AndConstraint<CommandResultAssertions> NotHaveStdErrContaining(string pattern)
        {
            Execute.Assertion.ForCondition(!_commandResult.StdErr.Contains(pattern))
                .FailWith(AppendDiagnosticsTo($"The command error output contained a result it should not have contained: {pattern}{Environment.NewLine}"));
            return new AndConstraint<CommandResultAssertions>(this);
        }

        public AndConstraint<CommandResultAssertions> HaveStdErrMatching(string pattern, RegexOptions options = RegexOptions.None)
        {
            Execute.Assertion.ForCondition(Regex.Match(_commandResult.StdErr, pattern, options).Success)
                .FailWith(AppendDiagnosticsTo($"Matching the command error output failed. Pattern: {pattern}{Environment.NewLine}"));
            return new AndConstraint<CommandResultAssertions>(this);
        }

        public AndConstraint<CommandResultAssertions> NotHaveStdOut()
        {
            Execute.Assertion.ForCondition(string.IsNullOrEmpty(_commandResult.StdOut))
                .FailWith(AppendDiagnosticsTo($"Expected command to not output to stdout but it was not:"));
            return new AndConstraint<CommandResultAssertions>(this);
        }

        public AndConstraint<CommandResultAssertions> NotHaveStdErr()
        {
            Execute.Assertion.ForCondition(string.IsNullOrEmpty(_commandResult.StdErr))
                .FailWith(AppendDiagnosticsTo("Expected command to not output to stderr but it was not:"));
            return new AndConstraint<CommandResultAssertions>(this);
        }

        private string AppendDiagnosticsTo(string s)
        {
            return s + $"{Environment.NewLine}" +
                       $"File Name: {_commandResult.StartInfo.FileName}{Environment.NewLine}" +
                       $"Arguments: {_commandResult.StartInfo.Arguments}{Environment.NewLine}" +
                       $"Exit Code: {_commandResult.ExitCode}{Environment.NewLine}" +
                       $"StdOut:{Environment.NewLine}{_commandResult.StdOut}{Environment.NewLine}" +
                       $"StdErr:{Environment.NewLine}{_commandResult.StdErr}{Environment.NewLine}"; ;
        }
		
		public AndConstraint<CommandResultAssertions> HaveSkippedProjectCompilation(string skippedProject, string frameworkFullName)
        {
            _commandResult.StdOut.Should().Contain($"Project {skippedProject} ({frameworkFullName}) was previously compiled. Skipping compilation.");

            return new AndConstraint<CommandResultAssertions>(this);
        }

        public AndConstraint<CommandResultAssertions> HaveCompiledProject(string compiledProject, string frameworkFullName)
        {
            _commandResult.StdOut.Should().Contain($"Project {compiledProject} ({frameworkFullName}) will be compiled");

            return new AndConstraint<CommandResultAssertions>(this);
        }
    }
}
