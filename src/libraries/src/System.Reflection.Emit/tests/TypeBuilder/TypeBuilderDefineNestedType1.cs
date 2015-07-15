// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using System.Reflection;
using System.Reflection.Emit;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class TypeBuilderCreateType1
    {
        private const int MinAsmName = 1;
        private const int MaxAsmName = 260;
        private const int MinModName = 1;
        private const int MaxModName = 260;
        private const int MinTypName = 1;
        private const int MaxTypName = 1024;
        private const int NumLoops = 5;
        private readonly RandomDataGenerator _generator = new RandomDataGenerator();

        [Fact]
        public void TestWithString()
        {
            ModuleBuilder modBuilder;
            TypeBuilder typeBuilder;
            TypeBuilder nestedType = null;
            Type newType;
            string typeName = "";
            string nestedTypeName = "";

            modBuilder = CreateModule(
                             _generator.GetString(true, MinAsmName, MaxAsmName),
                             _generator.GetString(false, MinModName, MaxModName));

            typeName = _generator.GetString(true, MinTypName, MaxTypName);  // name can not contain embedded nulls

            typeBuilder = modBuilder.DefineType(typeName);

            for (int i = 0; i < NumLoops; i++)
            {
                nestedTypeName = _generator.GetString(true, MinTypName, MaxTypName);  // name can not contain embedded nulls

                // create nested type
                if (null != nestedType && 0 == (_generator.GetInt32() % 2))
                {
                    nestedType.DefineNestedType(nestedTypeName);
                }
                else
                {
                    nestedType = typeBuilder.DefineNestedType(nestedTypeName);
                }
            }

            newType = typeBuilder.CreateTypeInfo().AsType();

            Assert.True(newType.Name.Equals(typeName));
        }

        [Fact]
        public void TestWithEmbeddedNulls()
        {
            ModuleBuilder modBuilder;
            TypeBuilder typeBuilder;
            Type newType;
            string typeName = "";
            string nestedTypeName = "";

            for (int i = 0; i < NumLoops; i++)
            {
                modBuilder = CreateModule(
                                 _generator.GetString(true, MinAsmName, MaxAsmName),
                                 _generator.GetString(false, MinModName, MaxModName));

                typeName = _generator.GetString(true, MinTypName, MaxTypName);  // name can not contain embedded nulls
                nestedTypeName = _generator.GetString(true, MinTypName, MaxTypName / 4)
                                 + '\0'
                                 + _generator.GetString(true, MinTypName, MaxTypName / 4)
                                 + '\0'
                                 + _generator.GetString(true, MinTypName, MaxTypName / 4);

                typeBuilder = modBuilder.DefineType(typeName);

                // create nested type
                typeBuilder.DefineNestedType(nestedTypeName);

                newType = typeBuilder.CreateTypeInfo().AsType();

                Assert.True(newType.Name.Equals(typeName));
            }
        }

        [Fact]
        public void TestThrowsExceptionForNullName()
        {
            ModuleBuilder modBuilder;
            TypeBuilder typeBuilder;
            string typeName = "";
            string nestedTypeName = "";

            modBuilder = CreateModule(
                                 _generator.GetString(true, MinAsmName, MaxAsmName),
                                 _generator.GetString(false, MinModName, MaxModName));

            typeName = _generator.GetString(true, MinTypName, MaxTypName);  // name can not contain embedded nulls
            nestedTypeName = null;

            typeBuilder = modBuilder.DefineType(typeName);

            // create nested type
            Assert.Throws<ArgumentNullException>(() => typeBuilder.DefineNestedType(nestedTypeName));
        }

        [Fact]
        public void TestThrowsExceptionForEmptyName()
        {

            ModuleBuilder modBuilder;
            TypeBuilder typeBuilder;
            string typeName = "";
            string nestedTypeName = "";

            modBuilder = CreateModule(
                                 _generator.GetString(true, MinAsmName, MaxAsmName),
                                 _generator.GetString(false, MinModName, MaxModName));

            typeName = _generator.GetString(true, MinTypName, MaxTypName);  // name can not contain embedded nulls
            nestedTypeName = string.Empty;

            typeBuilder = modBuilder.DefineType(typeName);

            // create nested type
            Assert.Throws<ArgumentException>(() => { typeBuilder.DefineNestedType(nestedTypeName); });
        }

        public ModuleBuilder CreateModule(string assemblyName, string modName)
        {
            AssemblyName asmName;
            AssemblyBuilder asmBuilder;
            ModuleBuilder modBuilder;

            // create the dynamic module
            asmName = new AssemblyName(assemblyName);
            asmBuilder = AssemblyBuilder.DefineDynamicAssembly(asmName, AssemblyBuilderAccess.Run);
            modBuilder = TestLibrary.Utilities.GetModuleBuilder(asmBuilder, "Module1");

            return modBuilder;
        }
    }
}
