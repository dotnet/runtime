// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using System.Reflection.Emit;

using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class FBTestAttribute1 : Attribute
    {
    }

    public class FieldBuilderSetCustomAttribute1
    {
        private TypeBuilder TypeBuilder
        {
            get
            {
                if (null == _typeBuilder)
                {
                    AssemblyBuilder assembly = AssemblyBuilder.DefineDynamicAssembly(
                        new AssemblyName("FieldBuilderSetCustomAttribute1_Assembly"), AssemblyBuilderAccess.Run);
                    ModuleBuilder module = TestLibrary.Utilities.GetModuleBuilder(assembly, "FieldBuilderSetCustomAttribute1_Module");
                    _typeBuilder = module.DefineType("FieldBuilderSetCustomAttribute1_Type", TypeAttributes.Abstract);
                }

                return _typeBuilder;
            }
        }

        private TypeBuilder _typeBuilder;
        private const int ArraySize = 256;
        private readonly RandomDataGenerator _generator = new RandomDataGenerator();

        [Fact]
        public void TestSetCustomAttribute()
        {
            FieldBuilder field = TypeBuilder.DefineField("Field_PosTest1", typeof(object), FieldAttributes.Public);
            Type type = typeof(FBTestAttribute1);
            ConstructorInfo con = type.GetConstructor(new Type[] { });
            byte[] bytes = new byte[ArraySize];
            _generator.GetBytes(bytes);

            field.SetCustomAttribute(con, bytes);
        }

        [Fact]
        public void TestThrowsExceptionForNullConstructorInfo()
        {
            FieldBuilder field = TypeBuilder.DefineField("Field_NegTest1", typeof(object), FieldAttributes.Public);
            byte[] bytes = new byte[ArraySize];
            Assert.Throws<ArgumentNullException>(() => { field.SetCustomAttribute(null, bytes); });
        }

        [Fact]
        public void TestThrowsExceptionForNullByteArray()
        {
            FieldBuilder field = TypeBuilder.DefineField("Field_NegTest2", typeof(object), FieldAttributes.Public);
            Type type = typeof(FBTestAttribute1);
            ConstructorInfo con = type.GetConstructor(new Type[] { });
            Assert.Throws<ArgumentNullException>(() => { field.SetCustomAttribute(con, null); });
        }

        [Fact]
        public void TestThrowsExceptionForCreateTypeCalled()
        {
            FieldBuilder field = TypeBuilder.DefineField("Field_NegTest3", typeof(object), FieldAttributes.Public);
            Type type = typeof(FBTestAttribute1);
            ConstructorInfo con = type.GetConstructor(new Type[] { });
            byte[] bytes = new byte[ArraySize];
            _generator.GetBytes(bytes);
            TypeBuilder.CreateTypeInfo().AsType();

            Assert.Throws<InvalidOperationException>(() => { field.SetCustomAttribute(con, bytes); });
        }
    }
}
