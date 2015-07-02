// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using System.Reflection;
using System.Reflection.Emit;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class TypeBuilderCreateType3
    {
        private const int MinAsmName = 1;
        private const int MaxAsmName = 260;
        private const int MinModName = 1;
        private const int MaxModName = 260;
        private const int MinTypName = 1;
        private const int MaxTypName = 1024;
        private const int NumLoops = 5;
        private TypeAttributes[] _typesPos = new TypeAttributes[17] {
                                                       TypeAttributes.Abstract | TypeAttributes.NestedPublic,
                                                       TypeAttributes.AnsiClass | TypeAttributes.NestedPublic,
                                                       TypeAttributes.AutoClass | TypeAttributes.NestedPublic,
                                                       TypeAttributes.AutoLayout | TypeAttributes.NestedPublic,
                                                       TypeAttributes.BeforeFieldInit | TypeAttributes.NestedPublic,
                                                       TypeAttributes.Class | TypeAttributes.NestedPublic,
                                                       TypeAttributes.ClassSemanticsMask | TypeAttributes.Abstract | TypeAttributes.NestedPublic,
                                                       TypeAttributes.ExplicitLayout | TypeAttributes.NestedPublic,
                                                       TypeAttributes.Import | TypeAttributes.NestedPublic,
                                                       TypeAttributes.Interface | TypeAttributes.Abstract | TypeAttributes.NestedPublic,
                                                       TypeAttributes.Sealed | TypeAttributes.NestedPublic,
                                                       TypeAttributes.SequentialLayout | TypeAttributes.NestedPublic,
                                                       TypeAttributes.Serializable | TypeAttributes.NestedPublic,
                                                       TypeAttributes.SpecialName | TypeAttributes.NestedPublic,
                                                       TypeAttributes.StringFormatMask | TypeAttributes.NestedPublic,
                                                       TypeAttributes.UnicodeClass | TypeAttributes.NestedPublic,
                                                       TypeAttributes.VisibilityMask,
                                                       };
        [Fact]
        public void TestDefineNestedType()
        {
            ModuleBuilder modBuilder;
            TypeBuilder typeBuilder;
            TypeBuilder nestedType = null;
            Type newType;
            string typeName = "";
            string nestedTypeName = "";

            modBuilder = CreateModule(
                             TestLibrary.Generator.GetString(true, MinAsmName, MaxAsmName),
                             TestLibrary.Generator.GetString(false, MinModName, MaxModName));

            typeName = TestLibrary.Generator.GetString(true, MinTypName, MaxTypName);  // name can not contain embedded nulls

            typeBuilder = modBuilder.DefineType(typeName);

            for (int i = 0; i < NumLoops; i++)
            {
                nestedTypeName = TestLibrary.Generator.GetString(true, MinTypName, MaxTypName);  // name can not contain embedded nulls

                // create nested type
                if (null != nestedType && 0 == (TestLibrary.Generator.GetInt32() % 2))
                {
                    nestedType.DefineNestedType(nestedTypeName, TypeAttributes.Abstract | TypeAttributes.NestedPublic, typeBuilder.GetType());
                }
                else
                {
                    nestedType = typeBuilder.DefineNestedType(nestedTypeName, TypeAttributes.Abstract | TypeAttributes.NestedPublic, typeBuilder.GetType());
                }
            }

            newType = typeBuilder.CreateTypeInfo().AsType();

            Assert.True(newType.Name.Equals(typeName));
        }

        [Fact]
        public void TestWithEmbeddedNullsInName()
        {
            ModuleBuilder modBuilder;
            TypeBuilder typeBuilder;
            Type newType;
            string typeName = "";
            string nestedTypeName = "";

            for (int i = 0; i < NumLoops; i++)
            {
                modBuilder = CreateModule(
                                 TestLibrary.Generator.GetString(true, MinAsmName, MaxAsmName),
                                 TestLibrary.Generator.GetString(false, MinModName, MaxModName));

                typeName = TestLibrary.Generator.GetString(true, MinTypName, MaxTypName);  // name can not contain embedded nulls
                nestedTypeName = TestLibrary.Generator.GetString(true, MinTypName, MaxTypName / 4)
                                 + '\0'
                                 + TestLibrary.Generator.GetString(true, MinTypName, MaxTypName / 4)
                                 + '\0'
                                 + TestLibrary.Generator.GetString(true, MinTypName, MaxTypName / 4);

                typeBuilder = modBuilder.DefineType(typeName);

                // create nested type
                typeBuilder.DefineNestedType(nestedTypeName, TypeAttributes.Abstract | TypeAttributes.NestedPublic, typeBuilder.GetType());

                newType = typeBuilder.CreateTypeInfo().AsType();

                Assert.True(newType.Name.Equals(typeName));
            }
        }

        [Fact]
        public void TestWithTypeAttributesSet()
        {
            ModuleBuilder modBuilder;
            TypeBuilder typeBuilder;
            TypeBuilder nestedType = null;
            Type newType;
            string typeName = "";
            string nestedTypeName = "";
            TypeAttributes typeAttrib = (TypeAttributes)0;
            int i = 0;

            modBuilder = CreateModule(
                              TestLibrary.Generator.GetString(true, MinAsmName, MaxAsmName),
                              TestLibrary.Generator.GetString(false, MinModName, MaxModName));

            typeName = TestLibrary.Generator.GetString(true, MinTypName, MaxTypName);  // name can not contain embedded nulls

            typeBuilder = modBuilder.DefineType(typeName, typeAttrib);

            for (i = 0; i < _typesPos.Length; i++)
            {
                typeAttrib = _typesPos[i];
                nestedTypeName = TestLibrary.Generator.GetString(true, MinTypName, MaxTypName);  // name can not contain embedded nulls

                // create nested type
                if (null != nestedType && 0 == (TestLibrary.Generator.GetInt32() % 2))
                {
                    nestedType.DefineNestedType(nestedTypeName, _typesPos[i], typeBuilder.GetType());
                }
                else
                {
                    nestedType = typeBuilder.DefineNestedType(nestedTypeName, _typesPos[i], typeBuilder.GetType());
                }
            }

            newType = typeBuilder.CreateTypeInfo().AsType();

            Assert.True(newType.Name.Equals(typeName));
        }

        [Fact]
        public void TestWithNullParentType()
        {
            ModuleBuilder modBuilder;
            TypeBuilder typeBuilder;
            TypeBuilder nestedType = null;
            Type newType;
            string typeName = "";
            string nestedTypeName = "";

            modBuilder = CreateModule(
                             TestLibrary.Generator.GetString(true, MinAsmName, MaxAsmName),
                             TestLibrary.Generator.GetString(false, MinModName, MaxModName));

            typeName = TestLibrary.Generator.GetString(true, MinTypName, MaxTypName);  // name can not contain embedded nulls

            typeBuilder = modBuilder.DefineType(typeName);

            nestedTypeName = TestLibrary.Generator.GetString(true, MinTypName, MaxTypName);  // name can not contain embedded nulls

            // create nested type
            nestedType = typeBuilder.DefineNestedType(nestedTypeName, TypeAttributes.Abstract | TypeAttributes.NestedPublic, null);

            newType = typeBuilder.CreateTypeInfo().AsType();

            Assert.True(newType.Name.Equals(typeName));
        }


        [Fact]
        public void TestThrowsExceptionForNullname()
        {
            ModuleBuilder modBuilder;
            TypeBuilder typeBuilder;
            Type newType;
            string typeName = "";
            string nestedTypeName = "";

            modBuilder = CreateModule(
                                 TestLibrary.Generator.GetString(true, MinAsmName, MaxAsmName),
                                 TestLibrary.Generator.GetString(false, MinModName, MaxModName));

            typeName = TestLibrary.Generator.GetString(true, MinTypName, MaxTypName);  // name can not contain embedded nulls
            nestedTypeName = null;

            typeBuilder = modBuilder.DefineType(typeName);

            Assert.Throws<ArgumentNullException>(() =>
            {
                // create nested type
                typeBuilder.DefineNestedType(nestedTypeName, TypeAttributes.NestedPublic | TypeAttributes.Class, typeBuilder.GetType());
                newType = typeBuilder.CreateTypeInfo().AsType();
            });
        }

        [Fact]
        public void TestThrowsExcetpionForEmptyName()
        {
            ModuleBuilder modBuilder;
            TypeBuilder typeBuilder;
            Type newType;
            string typeName = "";
            string nestedTypeName = "";

            modBuilder = CreateModule(
                                 TestLibrary.Generator.GetString(true, MinAsmName, MaxAsmName),
                                 TestLibrary.Generator.GetString(false, MinModName, MaxModName));

            typeName = TestLibrary.Generator.GetString(true, MinTypName, MaxTypName);  // name can not contain embedded nulls
            nestedTypeName = string.Empty;

            typeBuilder = modBuilder.DefineType(typeName);

            Assert.Throws<ArgumentException>(() =>
            {
                // create nested type
                typeBuilder.DefineNestedType(nestedTypeName, TypeAttributes.NestedPublic | TypeAttributes.Class, typeBuilder.GetType());
                newType = typeBuilder.CreateTypeInfo().AsType();
            });
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
