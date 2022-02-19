// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;

namespace System
{
    public static partial class Environment
    {
        // Systems without the Windows registry pretend that it's always empty.

#pragma warning disable IDE0060
        private static string? GetEnvironmentVariableFromRegistry(string variable, bool fromMachine) => null;
#pragma warning restore IDE0060

        static partial void SetEnvironmentVariableFromRegistry(string variable, string? value, bool fromMachine);

#pragma warning disable IDE0060
        private static IDictionary GetEnvironmentVariablesFromRegistry(bool fromMachine) => new Hashtable();
#pragma warning restore IDE0060
    }
}
