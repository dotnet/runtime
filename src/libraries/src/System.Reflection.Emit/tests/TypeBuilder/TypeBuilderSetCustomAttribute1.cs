// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class TypeBuilderSetCustomAttribute1
    {
        public const string ModuleName = "ModuleName";
        public const string TypeName = "TypeName";
        private TypeBuilder GetTypeBuilder()
        {
            AssemblyName assemblyname = new AssemblyName("assemblyname");

            AssemblyBuilder assemblybuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyname, AssemblyBuilderAccess.Run);
            ModuleBuilder modulebuilder = TestLibrary.Utilities.GetModuleBuilder(assemblybuilder, "Module1");
            return modulebuilder.DefineType(TypeName);
        }

        [Fact]
        public void PosTest1()
        {
            TypeBuilder typebuilder = GetTypeBuilder();
            ConstructorInfo constructorinfo = typeof(ClassCreator).GetConstructor(new Type[] { typeof(string) });
            CustomAttributeBuilder cuatbu = new CustomAttributeBuilder(constructorinfo, new object[] { "hello" });
            typebuilder.SetCustomAttribute(cuatbu);
        }

        [Fact]
        public void NegTest1()
        {
            TypeBuilder typebuilder = GetTypeBuilder();
            Assert.Throws<ArgumentNullException>(() => { typebuilder.SetCustomAttribute(null); });
        }
    }

    public class ClassCreator : Attribute
    {
        private string _creator;
        public string Creator
        {
            get
            {
                return _creator;
            }
        }

        public ClassCreator(string name)
        {
            _creator = name;
        }
    }
}
