// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using System.Reflection.Emit;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class FieldBuilderSetOffset
    {
        [Fact]
        public void TestWithOffsetZero()
        {
            AssemblyBuilder assembly = AssemblyBuilder.DefineDynamicAssembly(
                new AssemblyName("FieldBuilderSetOffset_Assembly_PosTest1"), AssemblyBuilderAccess.Run);
            ModuleBuilder module = TestLibrary.Utilities.GetModuleBuilder(assembly, "FieldBuilderSetOffset_Module_PosTest1");
            TypeBuilder type = module.DefineType("FieldBuilderSetOffset_Type_PosTest1", TypeAttributes.Abstract);

            FieldBuilder field = type.DefineField("Field_PosTest1", typeof(int), FieldAttributes.Public);
            field.SetOffset(0);
        }

        [Fact]
        public void TestWithOffsetSmallPositiveValue1()
        {
            AssemblyBuilder assembly = AssemblyBuilder.DefineDynamicAssembly(
                new AssemblyName("FieldBuilderSetOffset_Assembly_PosTest2"), AssemblyBuilderAccess.Run);
            ModuleBuilder module = TestLibrary.Utilities.GetModuleBuilder(assembly, "FieldBuilderSetOffset_Module_PosTest2");
            TypeBuilder type = module.DefineType("FieldBuilderSetOffset_Type_PosTest2", TypeAttributes.Abstract);

            FieldBuilder field = type.DefineField("Field_PosTest2", typeof(int), FieldAttributes.Public);
            field.SetOffset(1);
        }

        [Fact]
        public void TestWithOffsetSmallPositiveValue2()
        {
            AssemblyBuilder assembly = AssemblyBuilder.DefineDynamicAssembly(
                new AssemblyName("FieldBuilderSetOffset_Assembly_PosTest3"), AssemblyBuilderAccess.Run);
            ModuleBuilder module = TestLibrary.Utilities.GetModuleBuilder(assembly, "FieldBuilderSetOffset_Module_PosTest3");
            TypeBuilder type = module.DefineType("FieldBuilderSetOffset_Type_PosTest3", TypeAttributes.Abstract);

            FieldBuilder field = type.DefineField("Field_PosTest3", typeof(int), FieldAttributes.Public);
            field.SetOffset(8);
        }

        [Fact]
        public void TestWithOffsetDifferentForTwoProperties()
        {
            AssemblyBuilder assembly = AssemblyBuilder.DefineDynamicAssembly(
                new AssemblyName("FieldBuilderSetOffset_Assembly_PosTest4"), AssemblyBuilderAccess.Run);
            ModuleBuilder module = TestLibrary.Utilities.GetModuleBuilder(assembly, "FieldBuilderSetOffset_Module_PosTest4");
            TypeBuilder type = module.DefineType("FieldBuilderSetOffset_Type_PosTest4", TypeAttributes.Abstract);

            FieldBuilder field1 = type.DefineField("Field1_PosTest4", typeof(int), FieldAttributes.Public);
            field1.SetOffset(0);

            FieldBuilder field2 = type.DefineField("Field2_PosTest4", typeof(int), FieldAttributes.Public);
            field2.SetOffset(4);

            FieldBuilder field3 = type.DefineField("Field3_PosTest4", typeof(int), FieldAttributes.Public);
            field3.SetOffset(4);
        }

        [Fact]
        public void TestWithOffsetSameForTwoProperties()
        {
            AssemblyBuilder assembly = AssemblyBuilder.DefineDynamicAssembly(
                new AssemblyName("FieldBuilderSetOffset_Assembly_PosTest5"), AssemblyBuilderAccess.Run);
            ModuleBuilder module = TestLibrary.Utilities.GetModuleBuilder(assembly, "FieldBuilderSetOffset_Module_PosTest5");
            TypeBuilder type = module.DefineType("FieldBuilderSetOffset_Type_PosTest5", TypeAttributes.Abstract);

            FieldBuilder field1 = type.DefineField("Field1_PosTest5", typeof(int), FieldAttributes.Public);
            field1.SetOffset(0);

            FieldBuilder field2 = type.DefineField("Field2_PosTest5", typeof(int), FieldAttributes.Public);
            field2.SetOffset(0);
        }

        [Fact]
        public void TestThrowsExceptionForCreateTypeCalled()
        {
            AssemblyBuilder assembly = AssemblyBuilder.DefineDynamicAssembly(
                new AssemblyName("FieldBuilderSetOffset_Assembly_NegTest1"), AssemblyBuilderAccess.Run);
            ModuleBuilder module = TestLibrary.Utilities.GetModuleBuilder(assembly, "FieldBuilderSetOffset_Module_NegTest1");
            TypeBuilder type = module.DefineType("FieldBuilderSetOffset_Type_NegTest1", TypeAttributes.Abstract);
            FieldBuilder field = type.DefineField("Field1_NegTest1", typeof(int), FieldAttributes.Public);

            type.CreateTypeInfo().AsType();

            Assert.Throws<InvalidOperationException>(() => { field.SetOffset(0); });
        }

        [Fact]
        public void TestThrowsExceptionForNegativeOffset()
        {
            AssemblyBuilder assembly = AssemblyBuilder.DefineDynamicAssembly(
                new AssemblyName("FieldBuilderSetOffset_Assembly_NegTest2"), AssemblyBuilderAccess.Run);
            ModuleBuilder module = TestLibrary.Utilities.GetModuleBuilder(assembly, "FieldBuilderSetOffset_Module_NegTest2");
            TypeBuilder type = module.DefineType("FieldBuilderSetOffset_Type_NegTest2", TypeAttributes.Abstract);

            FieldBuilder field = type.DefineField("Field_NegTest2", typeof(int), FieldAttributes.Public);
            Assert.Throws<ArgumentException>(() => { field.SetOffset(-1); });
        }
    }
}
