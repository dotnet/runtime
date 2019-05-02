// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
                .FailWith("Expected command to exit with {0} but it did not.{1}", expectedExitCode, GetDiagnosticsInfo());
            return new AndConstraint<CommandResultAssertions>(this);
        }

        public AndConstraint<CommandResultAssertions> Pass()
        {
            Execute.Assertion.ForCondition(_commandResult.ExitCode == 0)
                .FailWith("Expected command to pass but it did not.{0}", GetDiagnosticsInfo());
            return new AndConstraint<CommandResultAssertions>(this);
        }

        public AndConstraint<CommandResultAssertions> Fail()
        {
            Execute.Assertion.ForCondition(_commandResult.ExitCode != 0)
                .FailWith("Expected command to fail but it did not.{0}", GetDiagnosticsInfo());
            return new AndConstraint<CommandResultAssertions>(this);
        }

        public AndConstraint<CommandResultAssertions> HaveStdOut()
        {
            Execute.Assertion.ForCondition(!string.IsNullOrEmpty(_commandResult.StdOut))
                .FailWith("Command did not output anything to stdout{0}", GetDiagnosticsInfo());
            return new AndConstraint<CommandResultAssertions>(this);
        }

        public AndConstraint<CommandResultAssertions> HaveStdOut(string expectedOutput)
        {
            Execute.Assertion.ForCondition(_commandResult.StdOut.Equals(expectedOutput, StringComparison.Ordinal))
                .FailWith("Command did not output with Expected Output. Expected: {0}{1}", expectedOutput, GetDiagnosticsInfo());
            return new AndConstraint<CommandResultAssertions>(this);
        }

        public AndConstraint<CommandResultAssertions> HaveStdOutContaining(string pattern)
        {
            Execute.Assertion.ForCondition(_commandResult.StdOut.Contains(pattern))
                .FailWith("The command output did not contain expected result: {0}{1}", pattern, GetDiagnosticsInfo());
            return new AndConstraint<CommandResultAssertions>(this);
        }

        public AndConstraint<CommandResultAssertions> NotHaveStdOutContaining(string pattern)
        {
            Execute.Assertion.ForCondition(!_commandResult.StdOut.Contains(pattern))
                .FailWith("The command output contained a result it should not have contained: {0}{1}", pattern, GetDiagnosticsInfo());
            return new AndConstraint<CommandResultAssertions>(this);
        }

        public AndConstraint<CommandResultAssertions> HaveStdOutMatching(string pattern, RegexOptions options = RegexOptions.None)
        {
            Execute.Assertion.ForCondition(Regex.Match(_commandResult.StdOut, pattern, options).Success)
                .FailWith("Matching the command output failed. Pattern: {0}{1}", pattern, GetDiagnosticsInfo());
            return new AndConstraint<CommandResultAssertions>(this);
        }

        public AndConstraint<CommandResultAssertions> HaveStdErr()
        {
            Execute.Assertion.ForCondition(!string.IsNullOrEmpty(_commandResult.StdErr))
                .FailWith("Command did not output anything to stderr.{0}", GetDiagnosticsInfo());
            return new AndConstraint<CommandResultAssertions>(this);
        }

        public AndConstraint<CommandResultAssertions> HaveStdErrContaining(string pattern)
        {
            Execute.Assertion.ForCondition(_commandResult.StdErr.Contains(pattern))
                .FailWith("The command error output did not contain expected result: {0}{1}", pattern, GetDiagnosticsInfo());
            return new AndConstraint<CommandResultAssertions>(this);
        }

        public AndConstraint<CommandResultAssertions> NotHaveStdErrContaining(string pattern)
        {
            Execute.Assertion.ForCondition(!_commandResult.StdErr.Contains(pattern))
                .FailWith("The command error output contained a result it should not have contained: {0}{1}", pattern, GetDiagnosticsInfo());
            return new AndConstraint<CommandResultAssertions>(this);
        }

        public AndConstraint<CommandResultAssertions> HaveStdErrMatching(string pattern, RegexOptions options = RegexOptions.None)
        {
            Execute.Assertion.ForCondition(Regex.Match(_commandResult.StdErr, pattern, options).Success)
                .FailWith("Matching the command error output failed. Pattern: {0}{1}", pattern, GetDiagnosticsInfo());
            return new AndConstraint<CommandResultAssertions>(this);
        }

        public AndConstraint<CommandResultAssertions> NotHaveStdOut()
        {
            Execute.Assertion.ForCondition(string.IsNullOrEmpty(_commandResult.StdOut))
                .FailWith("Expected command to not output to stdout but it was not:{0}", GetDiagnosticsInfo());
            return new AndConstraint<CommandResultAssertions>(this);
        }

        public AndConstraint<CommandResultAssertions> NotHaveStdErr()
        {
            Execute.Assertion.ForCondition(string.IsNullOrEmpty(_commandResult.StdErr))
                .FailWith("Expected command to not output to stderr but it was not:{0}", GetDiagnosticsInfo());
            return new AndConstraint<CommandResultAssertions>(this);
        }

        public AndConstraint<CommandResultAssertions> FileExists(string path)
        {
            Execute.Assertion.ForCondition(System.IO.File.Exists(path))
                .FailWith("The command did not write the expected file: {0}{1}", path, GetDiagnosticsInfo());
            return new AndConstraint<CommandResultAssertions>(this);
        }

        public AndConstraint<CommandResultAssertions> FileContains(string path, string pattern)
        {
            Execute.Assertion.ForCondition(System.IO.File.ReadAllText(path).Contains(pattern))
                .FailWith("The command did not write the expected result '{0}' to the file: {1}{2}", pattern, path, GetDiagnosticsInfo());
            return new AndConstraint<CommandResultAssertions>(this);
        }

        public AndConstraint<CommandResultAssertions> NotFileContains(string path, string pattern)
        {
            Execute.Assertion.ForCondition(!System.IO.File.ReadAllText(path).Contains(pattern))
                .FailWith("The command did not write the expected result '{1}' to the file: {1}{2}", pattern, path, GetDiagnosticsInfo());
            return new AndConstraint<CommandResultAssertions>(this);
        }

        private string GetDiagnosticsInfo()
        {
            return $"{Environment.NewLine}" +
                $"File Name: {_commandResult.StartInfo.FileName}{Environment.NewLine}" +
                $"Arguments: {_commandResult.StartInfo.Arguments}{Environment.NewLine}" +
                $"Exit Code: {_commandResult.ExitCode}{Environment.NewLine}" +
                $"StdOut:{Environment.NewLine}{_commandResult.StdOut}{Environment.NewLine}" +
                $"StdErr:{Environment.NewLine}{_commandResult.StdErr}{Environment.NewLine}"; ;
        }

        public AndConstraint<CommandResultAssertions> HaveSkippedProjectCompilation(string skippedProject, string frameworkFullName)
        {
            _commandResult.StdOut.Should().Contain("Project {0} ({1}) was previously compiled. Skipping compilation.", skippedProject, frameworkFullName);

            return new AndConstraint<CommandResultAssertions>(this);
        }

        public AndConstraint<CommandResultAssertions> HaveCompiledProject(string compiledProject, string frameworkFullName)
        {
            _commandResult.StdOut.Should().Contain($"Project {0} ({1}) will be compiled", compiledProject, frameworkFullName);

            return new AndConstraint<CommandResultAssertions>(this);
        }
    }
}
