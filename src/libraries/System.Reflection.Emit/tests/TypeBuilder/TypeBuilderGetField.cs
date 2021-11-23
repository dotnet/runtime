// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class TypeBuilderGetField
    {
        [Fact]
        public void GetField_DeclaringTypeOfFieldGeneric()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Class | TypeAttributes.Public);
            GenericTypeParameterBuilder[] typeParams = type.DefineGenericParameters("T");

            FieldBuilder field = type.DefineField("Field", typeParams[0].AsType(), FieldAttributes.Public);
            Assert.Equal("Field", TypeBuilder.GetField(type.AsType(), field).Name);
        }

        [Fact]
        public void GetField()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Class | TypeAttributes.Public);
            GenericTypeParameterBuilder[] typeParams =
                type.DefineGenericParameters("T");

            FieldBuilder field = type.DefineField("Field", typeParams[0].AsType(), FieldAttributes.Public);

            Type genericIntType = type.MakeGenericType(typeof(int));
            FieldInfo resultField = TypeBuilder.GetField(genericIntType, field);
            Assert.Equal("Field", resultField.Name);
        }

        [Fact]
        public void GetField_TypeNotTypeBuilder_ThrowsArgumentException()
        {
            AssertExtensions.Throws<ArgumentException>("type", () => TypeBuilder.GetField(typeof(int), typeof(int).GetField("MaxValue")));
        }

        [Fact]
        public void GetField_DeclaringTypeOfFieldNotGenericTypeDefinitionOfType_ThrowsArgumentException()
        {
            ModuleBuilder module = Helpers.DynamicModule();
            TypeBuilder type1 = module.DefineType("Sample", TypeAttributes.Class | TypeAttributes.Public);
            GenericTypeParameterBuilder[] typeParams = type1.DefineGenericParameters("T");

            TypeBuilder type2 = module.DefineType("Sample2", TypeAttributes.Class | TypeAttributes.Public);
            GenericTypeParameterBuilder[] typeParams2 = type2.DefineGenericParameters("T");

            FieldBuilder field1 = type1.DefineField("Field", typeParams[0].AsType(), FieldAttributes.Public);
            FieldBuilder field2 = type2.DefineField("Field", typeParams[0].AsType(), FieldAttributes.Public);

            Type genericInt = type1.MakeGenericType(typeof(int));
            AssertExtensions.Throws<ArgumentException>("type", () => TypeBuilder.GetField(genericInt, field2));
        }

        [Fact]
        public void GetField_TypeNotGeneric_ThrowsArgumentException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Class | TypeAttributes.Public);
            FieldBuilder field = type.DefineField("Field", typeof(int), FieldAttributes.Public);

            AssertExtensions.Throws<ArgumentException>("field", () => TypeBuilder.GetField(type.AsType(), field));
        }
    }
}
