// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

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
        public static string StackTrace => Environment.StackTrace;
        public static int TickCount => Environment.TickCount;
    }
}
