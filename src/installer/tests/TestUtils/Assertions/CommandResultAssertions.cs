// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
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
                .FailWith("Expected command to exit with {0} but it did not.{1}", expectedExitCode, GetDiagnosticsInfo());
            return new AndConstraint<CommandResultAssertions>(this);
        }

        public AndConstraint<CommandResultAssertions> Pass()
        {
            Execute.Assertion.ForCondition(Result.ExitCode == 0)
                .FailWith("Expected command to pass but it did not.{0}", GetDiagnosticsInfo());
            return new AndConstraint<CommandResultAssertions>(this);
        }

        public AndConstraint<CommandResultAssertions> Fail()
        {
            Execute.Assertion.ForCondition(Result.ExitCode != 0)
                .FailWith("Expected command to fail but it did not.{0}", GetDiagnosticsInfo());
            return new AndConstraint<CommandResultAssertions>(this);
        }

        public AndConstraint<CommandResultAssertions> HaveStdOut()
        {
            Execute.Assertion.ForCondition(!string.IsNullOrEmpty(Result.StdOut))
                .FailWith("Command did not output anything to stdout{0}", GetDiagnosticsInfo());
            return new AndConstraint<CommandResultAssertions>(this);
        }

        public AndConstraint<CommandResultAssertions> HaveStdOut(string expectedOutput)
        {
            Execute.Assertion.ForCondition(Result.StdOut.Equals(expectedOutput, StringComparison.Ordinal))
                .FailWith("Command did not output with Expected Output. Expected: {0}{1}", expectedOutput, GetDiagnosticsInfo());
            return new AndConstraint<CommandResultAssertions>(this);
        }

        public AndConstraint<CommandResultAssertions> HaveStdOutContaining(string pattern)
        {
            Execute.Assertion.ForCondition(Result.StdOut.Contains(pattern))
                .FailWith("The command output did not contain expected result: {0}{1}", pattern, GetDiagnosticsInfo());
            return new AndConstraint<CommandResultAssertions>(this);
        }

        public AndConstraint<CommandResultAssertions> NotHaveStdOutContaining(string pattern)
        {
            Execute.Assertion.ForCondition(!Result.StdOut.Contains(pattern))
                .FailWith("The command output contained a result it should not have contained: {0}{1}", pattern, GetDiagnosticsInfo());
            return new AndConstraint<CommandResultAssertions>(this);
        }

        public AndConstraint<CommandResultAssertions> HaveStdOutMatching(string pattern, RegexOptions options = RegexOptions.None)
        {
            Execute.Assertion.ForCondition(Regex.Match(Result.StdOut, pattern, options).Success)
                .FailWith("Matching the command output failed. Pattern: {0}{1}", pattern, GetDiagnosticsInfo());
            return new AndConstraint<CommandResultAssertions>(this);
        }

        public AndConstraint<CommandResultAssertions> HaveStdErr()
        {
            Execute.Assertion.ForCondition(!string.IsNullOrEmpty(Result.StdErr))
                .FailWith("Command did not output anything to stderr.{0}", GetDiagnosticsInfo());
            return new AndConstraint<CommandResultAssertions>(this);
        }

        public AndConstraint<CommandResultAssertions> HaveStdErrContaining(string pattern)
        {
            Execute.Assertion.ForCondition(Result.StdErr.Contains(pattern))
                .FailWith("The command error output did not contain expected result: {0}{1}", pattern, GetDiagnosticsInfo());
            return new AndConstraint<CommandResultAssertions>(this);
        }

        public AndConstraint<CommandResultAssertions> NotHaveStdErrContaining(string pattern)
        {
            Execute.Assertion.ForCondition(!Result.StdErr.Contains(pattern))
                .FailWith("The command error output contained a result it should not have contained: {0}{1}", pattern, GetDiagnosticsInfo());
            return new AndConstraint<CommandResultAssertions>(this);
        }

        public AndConstraint<CommandResultAssertions> HaveStdErrMatching(string pattern, RegexOptions options = RegexOptions.None)
        {
            Execute.Assertion.ForCondition(Regex.Match(Result.StdErr, pattern, options).Success)
                .FailWith("Matching the command error output failed. Pattern: {0}{1}", pattern, GetDiagnosticsInfo());
            return new AndConstraint<CommandResultAssertions>(this);
        }

        public AndConstraint<CommandResultAssertions> NotHaveStdOut()
        {
            Execute.Assertion.ForCondition(string.IsNullOrEmpty(Result.StdOut))
                .FailWith("Expected command to not output to stdout but it was not:{0}", GetDiagnosticsInfo());
            return new AndConstraint<CommandResultAssertions>(this);
        }

        public AndConstraint<CommandResultAssertions> NotHaveStdErr()
        {
            Execute.Assertion.ForCondition(string.IsNullOrEmpty(Result.StdErr))
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
            string fileContent = System.IO.File.ReadAllText(path);
            Execute.Assertion.ForCondition(fileContent.Contains(pattern))
                .FailWith("The command did not write the expected result '{0}' to the file: {1}{2}\nfile content: {3}", pattern, path, GetDiagnosticsInfo(), fileContent);
            return new AndConstraint<CommandResultAssertions>(this);
        }

        public AndConstraint<CommandResultAssertions> NotFileContains(string path, string pattern)
        {
            string fileContent = System.IO.File.ReadAllText(path);
            Execute.Assertion.ForCondition(!fileContent.Contains(pattern))
                .FailWith("The command did not write the expected result '{1}' to the file: {1}{2}\nfile content: {3}", pattern, path, GetDiagnosticsInfo(), fileContent);
            return new AndConstraint<CommandResultAssertions>(this);
        }

        public string GetDiagnosticsInfo()
        {
            return $"{Environment.NewLine}" +
                $"File Name: {Result.StartInfo.FileName}{Environment.NewLine}" +
                $"Arguments: {Result.StartInfo.Arguments}{Environment.NewLine}" +
                $"Exit Code: {Result.ExitCode}{Environment.NewLine}" +
                $"StdOut:{Environment.NewLine}{Result.StdOut}{Environment.NewLine}" +
                $"StdErr:{Environment.NewLine}{Result.StdErr}{Environment.NewLine}";
        }

        public AndConstraint<CommandResultAssertions> HaveSkippedProjectCompilation(string skippedProject, string frameworkFullName)
        {
            Result.StdOut.Should().Contain("Project {0} ({1}) was previously compiled. Skipping compilation.", skippedProject, frameworkFullName);

            return new AndConstraint<CommandResultAssertions>(this);
        }

        public AndConstraint<CommandResultAssertions> HaveCompiledProject(string compiledProject, string frameworkFullName)
        {
            Result.StdOut.Should().Contain($"Project {0} ({1}) will be compiled", compiledProject, frameworkFullName);

            return new AndConstraint<CommandResultAssertions>(this);
        }
    }
}
