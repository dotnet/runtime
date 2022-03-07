// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Collections.Generic;
using System.IO;

namespace System.Reflection.Emit
{
    public sealed partial class AssemblyBuilder : Assembly
    {
        internal AssemblyBuilder()
        {
            // Prevent generating a default constructor
        }

        public override string FullName
        {
            get
            {
                return default;
            }
        }

        public override Module ManifestModule
        {
            get
            {
                return default;
            }
        }

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

        public ModuleBuilder DefineDynamicModule(string name)
        {
            return default;
        }

        public override bool Equals(object? obj)
        {
            return default;
        }

        public ModuleBuilder GetDynamicModule(string name)
        {
            return default;
        }

        public override int GetHashCode()
        {
            return default;
        }

        public void SetCustomAttribute(ConstructorInfo con, byte[] binaryAttribute)
        {
        }

        public void SetCustomAttribute(CustomAttributeBuilder customBuilder)
        {
        }
    }
}
