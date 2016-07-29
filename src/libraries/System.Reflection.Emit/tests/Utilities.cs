// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection.Emit;

namespace System.Reflection.Emit.Tests
{
    public class EmptyAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
    public class IntAllAttribute : Attribute
    {
        public int _i;
        public IntAllAttribute(int i) { _i = i; }
    }

    public static class Helpers
    {
        public static AssemblyBuilder DynamicAssembly(string name = "TestAssembly")
        {
            AssemblyName assemblyName = new AssemblyName(name);
            return AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
        }

        public static ModuleBuilder DynamicModule(string assemblyName = "TestAssembly", string moduleName = "TestModule")
        {
            return DynamicAssembly(assemblyName).DefineDynamicModule(moduleName);
        }

        public static TypeBuilder DynamicType(TypeAttributes attributes, string assemblyName = "TestAssembly", string moduleName = "TestModule", string typeName = "TestType")
        {
            return DynamicModule(assemblyName, moduleName).DefineType(typeName, attributes);
        }

        public static EnumBuilder DynamicEnum(TypeAttributes visibility, Type underlyingType, string enumName = "TestEnum", string assemblyName = "TestAssembly", string moduleName = "TestModule")
        {
            return DynamicModule(assemblyName, moduleName).DefineEnum(enumName, visibility, underlyingType);
        }
    }
}
