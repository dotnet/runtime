// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using System.Reflection.Emit;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class FBTestAttribute2 : Attribute
    {
    }

    public class FieldBuilderSetCustomAttribute2
    {
        private TypeBuilder TypeBuilder
        {
            get
            {
                if (null == _typeBuilder)
                {
                    AssemblyBuilder assembly = AssemblyBuilder.DefineDynamicAssembly(
                        new AssemblyName("EventBuilderAddOtherMethod_Assembly"), AssemblyBuilderAccess.Run);
                    ModuleBuilder module = TestLibrary.Utilities.GetModuleBuilder(assembly, "EventBuilderAddOtherMethod_Module");
                    _typeBuilder = module.DefineType("EventBuilderAddOtherMethod_Type", TypeAttributes.Abstract);
                }

                return _typeBuilder;
            }
        }

        private TypeBuilder _typeBuilder;

        [Fact]
        public void TestSetCustomAttribute()
        {
            FieldBuilder field = TypeBuilder.DefineField("Field_PosTest1", typeof(object), FieldAttributes.Public);
            ConstructorInfo con = typeof(FBTestAttribute2).GetConstructor(new Type[] { });
            CustomAttributeBuilder attribute = new CustomAttributeBuilder(con, new object[] { });

            field.SetCustomAttribute(attribute);
        }

        [Fact]
        public void TestThrowsExceptionForNullBuilder()
        {
            FieldBuilder field = TypeBuilder.DefineField("Field_NegTest1", typeof(object), FieldAttributes.Public);
            Assert.Throws<ArgumentNullException>(() => { field.SetCustomAttribute(null); });
        }

        [Fact]
        public void TestThrowsExceptionForCreateTypeCalled()
        {
            FieldBuilder field = TypeBuilder.DefineField("Field_NegTest2", typeof(object), FieldAttributes.Public);
            ConstructorInfo con = typeof(FBTestAttribute2).GetConstructor(new Type[] { });
            CustomAttributeBuilder attribute = new CustomAttributeBuilder(con, new object[] { });
            TypeBuilder.CreateTypeInfo().AsType();

            Assert.Throws<InvalidOperationException>(() => { field.SetCustomAttribute(attribute); });
        }
    }
}
