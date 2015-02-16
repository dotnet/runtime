// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace CoreFXTestLibrary
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
