// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class TypeBuilderGetNullableUnderlyingType
    {
        [Fact]
        public void TypeBuilder_ReturnsNull()
        {
            TypeBuilder tb = Helpers.DynamicType(TypeAttributes.Public);
            Assert.Null(tb.GetNullableUnderlyingType());
        }

        [Fact]
        public void EnumBuilder_ReturnsNull()
        {
            EnumBuilder eb = Helpers.DynamicEnum(TypeAttributes.Public, typeof(int));
            Assert.Null(eb.GetNullableUnderlyingType());
        }

        [Fact]
        public void GenericTypeParameterBuilder_ReturnsNull()
        {
            TypeBuilder tb = Helpers.DynamicType(TypeAttributes.Public);
            GenericTypeParameterBuilder[] gps = tb.DefineGenericParameters("T");
            Assert.Null(gps[0].GetNullableUnderlyingType());
        }

        [Fact]
        public void TypeBuilderInstantiation_ReturnsNull()
        {
            // A TypeBuilderInstantiation is produced when MakeGenericType is called on a
            // generic TypeBuilder. The open generic definition is a TypeBuilder (never
            // typeof(Nullable<>)), so the override always returns null in practice.
            TypeBuilder tb = Helpers.DynamicType(TypeAttributes.Public);
            tb.DefineGenericParameters("T");
            Type instantiation = tb.MakeGenericType(typeof(int));
            Assert.Null(instantiation.GetNullableUnderlyingType());
        }

        [Fact]
        public void SymbolType_Array_ReturnsNull()
        {
            TypeBuilder tb = Helpers.DynamicType(TypeAttributes.Public);
            Type arrayType = tb.MakeArrayType();
            Assert.Null(arrayType.GetNullableUnderlyingType());
            Assert.Null(Nullable.GetUnderlyingType(arrayType));
        }

        [Fact]
        public void SymbolType_MultiDimArray_ReturnsNull()
        {
            TypeBuilder tb = Helpers.DynamicType(TypeAttributes.Public);
            Type arrayType = tb.MakeArrayType(2);
            Assert.Null(arrayType.GetNullableUnderlyingType());
            Assert.Null(Nullable.GetUnderlyingType(arrayType));
        }

        [Fact]
        public void SymbolType_Pointer_ReturnsNull()
        {
            TypeBuilder tb = Helpers.DynamicType(TypeAttributes.Public);
            Type pointerType = tb.MakePointerType();
            Assert.Null(pointerType.GetNullableUnderlyingType());
            Assert.Null(Nullable.GetUnderlyingType(pointerType));
        }

        [Fact]
        public void SymbolType_ByRef_ReturnsNull()
        {
            TypeBuilder tb = Helpers.DynamicType(TypeAttributes.Public);
            Type byRefType = tb.MakeByRefType();
            Assert.Null(byRefType.GetNullableUnderlyingType());
            Assert.Null(Nullable.GetUnderlyingType(byRefType));
        }
    }
}
