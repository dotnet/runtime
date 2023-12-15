// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class GenericTypeParameterBuilderSetCustomAttribute
    {
        [Fact]
        public void SetCustomAttribute_ConstructorInfo_ByteArray()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            string[] typeParamNames = new string[] { "TFirst" };
            GenericTypeParameterBuilder[] typeParams = type.DefineGenericParameters(typeParamNames);
            ConstructorInfo attributeConstructor = typeof(HelperAttribute).GetConstructors()[0];
            byte[] binaryAttribute = new byte[128];

            typeParams[0].SetCustomAttribute(attributeConstructor, binaryAttribute);
        }

        [Fact]
        public void SetCustomAttribute_ConstructorInfo_ByteArray_NullAttributeConstructor_ThrowsArgumentNullException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            string[] typeParamNames = new string[] { "TFirst" };
            GenericTypeParameterBuilder[] typeParams = type.DefineGenericParameters(typeParamNames);

            AssertExtensions.Throws<ArgumentNullException>("con", () => typeParams[0].SetCustomAttribute(null, new byte[128]));
        }

        [Fact]
        public void SetCustomAttribute_CustomAttributeBuilder()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            string[] typeParamNames = new string[] { "TFirst" };
            GenericTypeParameterBuilder[] typeParams = type.DefineGenericParameters(typeParamNames);
            ConstructorInfo constructorinfo = typeof(HelperAttribute).GetConstructor(new Type[] { typeof(string) });
            CustomAttributeBuilder attributeBuilder = new CustomAttributeBuilder(constructorinfo, new object[] { "TestString" });

            typeParams[0].SetCustomAttribute(attributeBuilder);
        }

        [Fact]
        public void SetCustomAttribute_CustomAttributeBuilder_NullAttributeBuilder_ThrowsArgumentNullException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            string[] typeParamNames = new string[] { "TFirst" };
            GenericTypeParameterBuilder[] typeParams = type.DefineGenericParameters(typeParamNames);

            AssertExtensions.Throws<ArgumentNullException>("customBuilder", () => typeParams[0].SetCustomAttribute(null));
        }
    }

    public class HelperAttribute
    {
        public HelperAttribute() { }
        public HelperAttribute(string str) { }
    }
}
