// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;

namespace Internal.Runtime.Augments
{
    // TODO: Delete this file once corefx has consumed https://github.com/dotnet/coreclr/pull/22106
    // and its corresponding mirrored build from corert, and then the resulting corefx builds
    // have been consumed back here, such that the CI tests which currently expect to find
    // EnvironmentAugments have been updated to no longer need it.

    public static class EnvironmentAugments
    {
        public static int CurrentManagedThreadId => Environment.CurrentManagedThreadId;
        public static void Exit(int exitCode) => Environment.Exit(exitCode);
        public static int ExitCode { get { return Environment.ExitCode; } set { Environment.ExitCode = value; } }
        public static void FailFast(string message, Exception error) => Environment.FailFast(message, error);
        public static string[] GetCommandLineArgs() => Environment.GetCommandLineArgs();
        public static bool HasShutdownStarted => Environment.HasShutdownStarted;
        public static int TickCount => Environment.TickCount;
        public static string GetEnvironmentVariable(string variable) => Environment.GetEnvironmentVariable(variable);
        public static string GetEnvironmentVariable(string variable, EnvironmentVariableTarget target) => Environment.GetEnvironmentVariable(variable, target);
        public static IEnumerable<KeyValuePair<string, string>> EnumerateEnvironmentVariables()
        {
            IDictionaryEnumerator de = Environment.GetEnvironmentVariables().GetEnumerator();
            while (de.MoveNext())
            {
                yield return new KeyValuePair<string, string>((string)de.Key, (string)de.Value);
            }
        }
        public static IEnumerable<KeyValuePair<string, string>> EnumerateEnvironmentVariables(EnvironmentVariableTarget target)
        {
            IDictionaryEnumerator de = Environment.GetEnvironmentVariables(target).GetEnumerator();
            while (de.MoveNext())
            {
                yield return new KeyValuePair<string, string>((string)de.Key, (string)de.Value);
            }
        }
        public static int ProcessorCount => Environment.ProcessorCount;
        public static void SetEnvironmentVariable(string variable, string value) => Environment.SetEnvironmentVariable(variable, value);
        public static void SetEnvironmentVariable(string variable, string value, EnvironmentVariableTarget target) => Environment.SetEnvironmentVariable(variable, value, target);
        public static string StackTrace => Environment.StackTrace; // this will temporarily result in an extra frame in Environment.StackTrace calls
    }
}
