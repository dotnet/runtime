// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

using Assembly = System.Reflection.Assembly;

namespace Internal.Runtime.CompilerHelpers
{
    internal partial class StartupCodeHelpers
    {
        private static RuntimeTypeHandle s_entryAssemblyType;

        internal static void InitializeEntryAssembly(RuntimeTypeHandle entryAssemblyType)
        {
            s_entryAssemblyType = entryAssemblyType;
        }

        internal static Assembly? GetEntryAssembly()
        {
            return Type.GetTypeFromHandle(s_entryAssemblyType)?.Assembly;
        }
    }
}
