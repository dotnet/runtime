// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Internal.Runtime.Augments
{
    /// <summary>For internal use only.  Exposes runtime functionality to the Environments implementation in corefx.</summary>
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
        // TODO Perf: Once CoreCLR gets EnumerateEnvironmentVariables(), get rid of GetEnvironmentVariables() and have 
        // corefx call EnumerateEnvironmentVariables() instead so we don't have to create a dictionary just to copy it into
        // another dictionary.
        public static IDictionary GetEnvironmentVariables() => new Dictionary<string, string>(EnumerateEnvironmentVariables());
        public static IDictionary GetEnvironmentVariables(EnvironmentVariableTarget target) => new Dictionary<string, string>(EnumerateEnvironmentVariables(target));
        public static IEnumerable<KeyValuePair<string, string>> EnumerateEnvironmentVariables() => Environment.EnumerateEnvironmentVariables();
        public static IEnumerable<KeyValuePair<string, string>> EnumerateEnvironmentVariables(EnvironmentVariableTarget target) => Environment.EnumerateEnvironmentVariables(target);

        public static void SetEnvironmentVariable(string variable, string value) => Environment.SetEnvironmentVariable(variable, value);
        public static void SetEnvironmentVariable(string variable, string value, EnvironmentVariableTarget target) => Environment.SetEnvironmentVariable(variable, value, target);

        public static string StackTrace
        {
            [MethodImpl(MethodImplOptions.NoInlining)] // Prevent inlining from affecting where the stacktrace starts
            get
            {
                return new StackTrace(1 /* skip this one frame */, true).ToString(System.Diagnostics.StackTrace.TraceFormat.Normal);
            }
        }
    }
}
