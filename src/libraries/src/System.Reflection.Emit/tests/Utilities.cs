// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection.Emit;

namespace TestLibrary
{
    public static class Utilities
    {
        public static ModuleBuilder GetModuleBuilder(AssemblyBuilder asmBuild, string moduleName)
        {
            return asmBuild.DefineDynamicModule(moduleName);
        }
    }
}
