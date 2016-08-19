// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Runtime.InteropServices;
using Xunit;

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
        public static AssemblyBuilder DynamicAssembly(string name = "TestAssembly", AssemblyBuilderAccess access = AssemblyBuilderAccess.Run)
        {
            AssemblyName assemblyName = new AssemblyName(name);
            return AssemblyBuilder.DefineDynamicAssembly(assemblyName, access);
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

        public static void VerifyType(TypeBuilder type, Module module, TypeBuilder declaringType, string name, TypeAttributes attributes, Type baseType, int size, PackingSize packingSize, Type[] implementedInterfaces)
        {
            Assert.Same(module, type.Module);
            Assert.Same(module.Assembly, type.Assembly);

            Assert.Equal(name, type.Name);
            if (declaringType == null)
            {
                Assert.Equal(GetFullName(name), type.FullName);
            }
            else
            {
                Assert.Equal(GetFullName(declaringType.Name) + "+" + GetFullName(type.Name), type.FullName);
            }

            Assert.Equal(attributes, type.Attributes);

            Assert.Equal(declaringType?.AsType(), type.DeclaringType);
            Assert.Equal(baseType, type.BaseType);

            Assert.Equal(size, type.Size);
            Assert.Equal(packingSize, type.PackingSize);

            Assert.Equal(implementedInterfaces ?? new Type[0], type.ImplementedInterfaces);

            if (declaringType == null && !type.IsInterface && (implementedInterfaces == null || implementedInterfaces.Length == 0))
            {
                Type createdType = type.CreateTypeInfo().AsType();
                Assert.Equal(createdType, module.GetType(name, false, false));
                Assert.Equal(createdType, module.GetType(name, true, false));

                // [ActiveIssue(10989, PlatformID.AnyUnix)]
                // Assert.Equal(createdType, module.GetType(name, true, true));
                // Assert.Equal(createdType, module.GetType(name.ToLowerInvariant(), true, true));
                // Assert.Equal(createdType, module.GetType(name.ToUpperInvariant(), true, true));
            }
        }

        public static string GetFullName(string name)
        {
            int nullTerminatorIndex = name.IndexOf('\0');
            if (nullTerminatorIndex >= 0)
            {
                return name.Substring(0, nullTerminatorIndex);
            }
            return name;
        }
    }
}
