// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Threading.Tasks.Tests
{
    // A dummy class for passing through System.Console calls until the System.Console contract is available.
    // To-do: Remove this workaround when possible.
    internal static class Logger
    {
        internal static void LogInformation(string message, params object[] args)
        {
            System.Diagnostics.Debug.WriteLine(message, args);
        }
    }
}
