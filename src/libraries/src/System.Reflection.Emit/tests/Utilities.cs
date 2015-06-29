// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
