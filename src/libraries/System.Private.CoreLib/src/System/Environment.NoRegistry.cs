// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;

namespace System
{
    public static partial class Environment
    {
        // Systems without the Windows registry pretend that it's always empty.

        private static string? GetEnvironmentVariableFromRegistry(string _, bool _1) => null;

        static partial void SetEnvironmentVariableFromRegistry(string variable, string? value, bool fromMachine);

        private static IDictionary GetEnvironmentVariablesFromRegistry(bool _) => new Hashtable();
    }
}
