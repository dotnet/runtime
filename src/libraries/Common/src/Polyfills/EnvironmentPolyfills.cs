// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System;

/// <summary>Provides downlevel polyfills for static members on <see cref="Environment"/>.</summary>
internal static class EnvironmentPolyfills
{
    private static int s_processId;

    extension(Environment)
    {
        public static int ProcessId
        {
            get
            {
                int processId = s_processId;
                if (processId == 0)
                {
                    using Process currentProcess = Process.GetCurrentProcess();
                    s_processId = processId = currentProcess.Id;
                }
                return processId;
            }
        }
    }
}
