// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using System.Reflection.Emit;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class TypeBuilderCreateType
    {
        private const int MinAsmName = 1;
        private const int MaxAsmName = 260;
        private const int MinModName = 1;
        private const int MaxModName = 260;
        private const int MinTypName = 1;
        private const int MaxTypName = 1024;
        private const int NumLoops = 5;
        private TypeAttributes[] _typesPos = new TypeAttributes[18] {
                                                       TypeAttributes.Abstract,
                                                       TypeAttributes.AnsiClass,
                                                       TypeAttributes.AutoClass,
                                                       TypeAttributes.AutoLayout,
                                                       TypeAttributes.BeforeFieldInit,
                                                       TypeAttributes.Class,
                                                       TypeAttributes.ClassSemanticsMask | TypeAttributes.Abstract,
                                                       TypeAttributes.ExplicitLayout,
                                                       TypeAttributes.Import,
                                                       TypeAttributes.Interface | TypeAttributes.Abstract,
                                                       TypeAttributes.NotPublic,
                                                       TypeAttributes.Public,
                                                       TypeAttributes.Sealed,
                                                       TypeAttributes.SequentialLayout,
                                                       TypeAttributes.Serializable,
                                                       TypeAttributes.SpecialName,
                                                       TypeAttributes.StringFormatMask,
                                                       TypeAttributes.UnicodeClass,
                                                       };
        private TypeAttributes[] _typesNeg = new TypeAttributes[11] {
                                                       TypeAttributes.ClassSemanticsMask,
                                                       TypeAttributes.HasSecurity,  // Bad type attributes. Reserved bits set on the type.
                                                       TypeAttributes.LayoutMask,  // Bad type attributes. Invalid layout attribute specified.
                                                       TypeAttributes.NestedAssembly,
                                                       TypeAttributes.NestedFamANDAssem,
                                                       TypeAttributes.NestedFamily,
                                                       TypeAttributes.NestedFamORAssem,
                                                       TypeAttributes.NestedPrivate,
                                                       TypeAttributes.NestedPublic,
                                                       TypeAttributes.RTSpecialName,  // Bad type attributes. Reserved bits set on the type.
                                                       TypeAttributes.VisibilityMask // Nested visibility flag set on a non-nested type
                                                       };

        [Fact]
        public void PosTest1()
        {
            ModuleBuilder modBuilder;
            TypeBuilder typeBuilder;
            Type newType;
            string typeName = "";

            for (int i = 0; i < NumLoops; i++)
            {
                modBuilder = CreateModule(
                              TestLibrary.Generator.GetString(true, MinAsmName, MaxAsmName),
                              TestLibrary.Generator.GetString(false, MinModName, MaxModName));

                typeName = TestLibrary.Generator.GetString(true, MinTypName, MaxTypName);  // name can not contain embedded nulls

                typeBuilder = modBuilder.DefineType(typeName);

                newType = typeBuilder.CreateTypeInfo().AsType();

                Assert.True(newType.Name.Equals(typeName));
            }
        }

        [Fact]
        public void PosTest2()
        {
            ModuleBuilder modBuilder;
            TypeBuilder typeBuilder;
            Type newType;
            string typeName = "";
            TypeAttributes typeAttrib = (TypeAttributes)0;
            int i = 0;

            for (i = 0; i < _typesPos.Length; i++)
            {
                modBuilder = CreateModule(
                              TestLibrary.Generator.GetString(true, MinAsmName, MaxAsmName),
                              TestLibrary.Generator.GetString(false, MinModName, MaxModName));

                typeName = TestLibrary.Generator.GetString(true, MinTypName, MaxTypName);  // name can not contain embedded nulls
                typeAttrib = _typesPos[i];

                typeBuilder = modBuilder.DefineType(typeName, typeAttrib);

                newType = typeBuilder.CreateTypeInfo().AsType();

                Assert.True(newType.Name.Equals(typeName));
            }
        }

        [Fact]
        public void PosTest3()
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
                nestedTypeName = TestLibrary.Generator.GetString(true, MinTypName, MaxTypName);  // name can not contain embedded nulls

                typeBuilder = modBuilder.DefineType(typeName);

                // create nested type
                typeBuilder.DefineNestedType(nestedTypeName);

                newType = typeBuilder.CreateTypeInfo().AsType();

                Assert.True(newType.Name.Equals(typeName));
            }
        }

        [Fact]
        public void PosTest4()
        {
            ModuleBuilder modBuilder;
            TypeBuilder typeBuilder;
            Type newType;
            GenericTypeParameterBuilder[] gTypeBuilders;
            string typeName = "";
            string nestedTypeName = "";

            for (int i = 0; i < NumLoops; i++)
            {
                modBuilder = CreateModule(
                                 TestLibrary.Generator.GetString(true, MinAsmName, MaxAsmName),
                                 TestLibrary.Generator.GetString(false, MinModName, MaxModName));

                typeName = TestLibrary.Generator.GetString(true, MinTypName, MaxTypName);  // name can not contain embedded nulls
                nestedTypeName = TestLibrary.Generator.GetString(true, MinTypName, MaxTypName);  // name can not contain embedded nulls

                typeBuilder = modBuilder.DefineType(typeName);

                // create generic parameters
                gTypeBuilders = typeBuilder.DefineGenericParameters(TestLibrary.Generator.GetStrings(true, MinTypName, MaxTypName));

                newType = typeBuilder.CreateTypeInfo().AsType();

                Assert.True(newType.Name.Equals(typeName));
            }
        }

        [Fact]
        public void NegTest1()
        {
            ModuleBuilder modBuilder;
            TypeBuilder typeBuilder;
            Type newType;
            string typeName = "";
            TypeAttributes typeAttrib = (TypeAttributes)0;
            int i = 0;

            for (i = 0; i < _typesNeg.Length; i++)
            {
                try
                {
                    modBuilder = CreateModule(
                                  TestLibrary.Generator.GetString(true, MinAsmName, MaxAsmName),
                                  TestLibrary.Generator.GetString(false, MinModName, MaxModName));

                    typeName = TestLibrary.Generator.GetString(true, MinTypName, MaxTypName);  // name can not contain embedded nulls
                    typeAttrib = _typesNeg[i];

                    typeBuilder = modBuilder.DefineType(typeName, typeAttrib);

                    newType = typeBuilder.CreateTypeInfo().AsType();
                    Assert.Throws<ArgumentException>(() => { });
                }
                catch (InvalidOperationException)
                {
                    Assert.Equal(TypeAttributes.ClassSemanticsMask, typeAttrib);
                }
                catch (ArgumentException)
                {
                    // all the others fail with this exception
                }
            }
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
