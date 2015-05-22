// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Reflection.Emit.Lightweight.Tests
{
    internal class Utilities
    {
        public static ModuleBuilder GetModuleBuilder(AssemblyBuilder asmBuild, string moduleName)
        {
            return asmBuild.DefineDynamicModule(moduleName);
        }
    }
}
