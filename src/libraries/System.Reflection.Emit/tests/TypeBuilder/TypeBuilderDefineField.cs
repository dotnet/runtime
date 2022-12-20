// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class TypeBuilderDefineFieldTests
    {
        public static IEnumerable<object[]> TestData()
        {
            yield return new object[] { "TestName", typeof(object), FieldAttributes.Public, FieldAttributes.Public };
            yield return new object[] { "A!?123C", typeof(int), FieldAttributes.Assembly, FieldAttributes.Assembly };
            yield return new object[] { "a\0b\0c", typeof(string), FieldAttributes.FamANDAssem, FieldAttributes.FamANDAssem };
            yield return new object[] { "\uD800\uDC00", Helpers.DynamicType(TypeAttributes.Public).AsType(), FieldAttributes.Family, FieldAttributes.Family };
            yield return new object[] { "\u043F\u0440\u0438\u0432\u0435\u0442", typeof(EmptyNonGenericInterface1), FieldAttributes.FamORAssem, FieldAttributes.FamORAssem };
            yield return new object[] { "Test Name With Spaces", typeof(EmptyEnum), FieldAttributes.Public, FieldAttributes.Public };
            yield return new object[] { "TestName", typeof(EmptyNonGenericClass), FieldAttributes.HasDefault, FieldAttributes.PrivateScope };
            yield return new object[] { "TestName", typeof(EmptyNonGenericStruct), FieldAttributes.HasFieldMarshal, FieldAttributes.PrivateScope };
            yield return new object[] { "TestName", typeof(EmptyGenericClass<int>), FieldAttributes.HasFieldRVA, FieldAttributes.PrivateScope };
            yield return new object[] { "TestName", typeof(EmptyGenericStruct<int>), FieldAttributes.Literal, FieldAttributes.Literal };
            yield return new object[] { "TestName", typeof(int), FieldAttributes.NotSerialized, FieldAttributes.NotSerialized };
            yield return new object[] { "TestName", typeof(int[]), FieldAttributes.PinvokeImpl, FieldAttributes.PinvokeImpl };
            yield return new object[] { "TestName", typeof(int).MakePointerType(), FieldAttributes.Private, FieldAttributes.Private };
            yield return new object[] { "TestName", typeof(EmptyGenericClass<>), FieldAttributes.PrivateScope, FieldAttributes.PrivateScope };
            yield return new object[] { "TestName", typeof(int), FieldAttributes.Public, FieldAttributes.Public };
            yield return new object[] { "TestName", typeof(int), FieldAttributes.RTSpecialName, FieldAttributes.PrivateScope };
            yield return new object[] { "TestName", typeof(int), FieldAttributes.SpecialName, FieldAttributes.SpecialName };
            yield return new object[] { "TestName", typeof(int), FieldAttributes.Public | FieldAttributes.Static, FieldAttributes.Public | FieldAttributes.Static };
        }

        [Theory]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/2389", TestRuntimes.Mono)]
        [MemberData(nameof(TestData))]
        public void DefineField(string name, Type fieldType, FieldAttributes attributes, FieldAttributes expectedAttributes)
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            FieldBuilder field = type.DefineField(name, fieldType, attributes);
            Assert.Equal(name, field.Name);
            Assert.Equal(fieldType, field.FieldType);
            Assert.Equal(expectedAttributes, field.Attributes);
            Assert.Equal(type.AsType(), field.DeclaringType);
            Assert.Equal(field.Module, field.Module);

            Type createdType = type.CreateType();
            Assert.Equal(type.AsType().GetFields(Helpers.AllFlags), createdType.GetFields(Helpers.AllFlags));

            FieldInfo fieldInfo = createdType.GetField(name, Helpers.AllFlags);
            Assert.Equal(type.AsType().GetField(name, Helpers.AllFlags), fieldInfo);

            if (fieldInfo != null)
            {
                // Verify MetadataToken
                Assert.Equal(field.MetadataToken, fieldInfo.MetadataToken);
                FieldInfo fieldFromToken = (FieldInfo)fieldInfo.Module.ResolveField(fieldInfo.MetadataToken);
                Assert.Equal(fieldInfo, fieldFromToken);
            }
        }

        [Fact]
        public void DefineField_CalledTwice_Works()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            type.DefineField("Name", typeof(int), FieldAttributes.Public);
            type.DefineField("Name", typeof(int), FieldAttributes.Public);

            Type createdType = type.CreateType();
            FieldInfo[] fields = createdType.GetFields();
            Assert.Equal(2, fields.Length);
            Assert.Equal(fields[0].Name, fields[1].Name);
        }

        [Fact]
        public void DefineField_NullFieldName_ThrowsArgumentNullException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            AssertExtensions.Throws<ArgumentNullException>("fieldName", () => type.DefineField(null, typeof(int), FieldAttributes.Public));
        }

        [Fact]
        public void DefineField_TypeAlreadyCreated_ThrowsInvalidOperationException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            type.CreateTypeInfo();

            Assert.Throws<InvalidOperationException>(() => type.DefineField("Name", typeof(int), FieldAttributes.Public));
        }

        [Theory]
        [InlineData("")]
        [InlineData("\0")]
        [InlineData("\0TestName")]
        public void DefineField_InvalidFieldName_ThrowsArgumentException(string fieldName)
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            AssertExtensions.Throws<ArgumentException>("fieldName", () => type.DefineField(fieldName, typeof(int), FieldAttributes.Public));
        }

        [Fact]
        public void DefineField_NullFieldType_ThrowsArgumentNullException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            AssertExtensions.Throws<ArgumentNullException>("type", () => type.DefineField("Name", null, FieldAttributes.Public));
        }

        [Fact]
        public void DefineField_VoidFieldType_ThrowsArgumentException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            AssertExtensions.Throws<ArgumentException>(null, () => type.DefineField("Name", typeof(void), FieldAttributes.Public));
        }

        [Theory]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/2389", TestRuntimes.Mono)]
        [InlineData((FieldAttributes)(-1), (FieldAttributes)(-38145))]
        [InlineData(FieldAttributes.FieldAccessMask, FieldAttributes.FieldAccessMask)]
        [InlineData((FieldAttributes)int.MaxValue, (FieldAttributes)2147445503)]
        public void DefineField_InvalidFieldAttributes_ThrowsTypeLoadExceptionOnCreation(FieldAttributes attributes, FieldAttributes expected)
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            FieldBuilder field = type.DefineField("Name", typeof(int), attributes);
            Assert.Equal(expected, field.Attributes);

            Assert.Throws<TypeLoadException>(() => type.CreateTypeInfo());
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/2389", TestRuntimes.Mono)]
        public void DefineField_DynamicFieldTypeNotCreated_ThrowsTypeLoadException()
        {
            ModuleBuilder module = Helpers.DynamicModule();
            TypeBuilder type = module.DefineType("Name", TypeAttributes.Public);
            TypeBuilder fieldType = module.DefineType("FieldType", TypeAttributes.Public);
            type.DefineField("Name", fieldType.AsType(), FieldAttributes.Public);

            Type createdType = type.CreateType();
            FieldInfo field = createdType.GetField("Name");
            Assert.Throws<TypeLoadException>(() => field.FieldType);

            Type createdFieldType = fieldType.CreateType();
            Assert.Equal(createdFieldType, field.FieldType);
        }

        [Fact]
        public void DefineByRefField_Class_ThrowsTypeLoadExceptionOnCreation()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            type.DefineField("Name", typeof(int).MakeByRefType(), FieldAttributes.Public);

            Assert.Throws<TypeLoadException>(() => type.CreateTypeInfo());
        }

        [Fact]
        public void DefineByRefField_ValueType_NonByRefLike_ThrowsTypeLoadExceptionOnCreation()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public, baseType: typeof(ValueType));
            type.DefineField("Name", typeof(int).MakeByRefType(), FieldAttributes.Public);

            Assert.Throws<TypeLoadException>(() => type.CreateTypeInfo());
        }

        [Fact]
        public void DefineByRefField_ValueType_ByRefLike()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public, baseType: typeof(ValueType));

            // Define type to be ByRefLike
            CustomAttributeBuilder ca = new(typeof(IsByRefLikeAttribute).GetConstructors()[0], new object[] { });
            type.SetCustomAttribute(ca);

            type.DefineField("Name", typeof(int).MakeByRefType(), FieldAttributes.Public);

            Type createdType = type.CreateType();
            FieldInfo[] fields = createdType.GetFields();
            Assert.Equal(1, fields.Length);
            Assert.True(fields[0].FieldType.IsByRef);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/45152")]
        public void Instantiate_ValueType_With_ByRefField()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public, baseType: typeof(ValueType));

            // Define type to be ByRefLike
            CustomAttributeBuilder ca = new(typeof(IsByRefLikeAttribute).GetConstructors()[0], new object[] { });
            type.SetCustomAttribute(ca);

            var field = type.DefineField("Name", typeof(int).MakeByRefType(), FieldAttributes.Public);

            var ctor = type.DefineConstructor(MethodAttributes.Public, CallingConventions.HasThis, new Type[] { typeof(string) });
            {
                ILGenerator il = ctor.GetILGenerator();
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldarga_S, 1);
                il.Emit(OpCodes.Stfld, field);
                il.Emit(OpCodes.Ret);
            }

            Type createdType = type.CreateType();

            var ctorToCall = createdType.GetConstructor(BindingFlags.Public | BindingFlags.Instance, new[] { typeof(string) });
            var str = "12345";
            ctorToCall.Invoke(new[] { str });
        }

        [Fact]
        public void GetField_TypeNotCreated_ThrowsNotSupportedException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            Assert.Throws<NotSupportedException>(() => type.AsType().GetField("Any"));
        }

        [Fact]
        public void GetFields_TypeNotCreated_ThrowsNotSupportedException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            Assert.Throws<NotSupportedException>(() => type.AsType().GetFields());
        }
    }
}
