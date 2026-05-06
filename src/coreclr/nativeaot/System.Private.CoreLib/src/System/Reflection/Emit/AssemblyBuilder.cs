// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace System.Reflection.Emit
{
    public partial class AssemblyBuilder : Assembly
    {
        [RequiresDynamicCode("Defining a dynamic assembly requires dynamic code.")]
        public static AssemblyBuilder DefineDynamicAssembly(AssemblyName name, AssemblyBuilderAccess access)
        {
            ReflectionEmitThrower.ThrowPlatformNotSupportedException();
            return default;
        }

        [RequiresDynamicCode("Defining a dynamic assembly requires dynamic code.")]
        public static AssemblyBuilder DefineDynamicAssembly(AssemblyName name, AssemblyBuilderAccess access, IEnumerable<CustomAttributeBuilder> assemblyAttributes)
        {
            ReflectionEmitThrower.ThrowPlatformNotSupportedException();
            return default;
        }
    }
}
