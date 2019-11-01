// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class SetCustomAttributeTests
    {
        [Fact]
        public void SetCustomAttribute_CustomAttributeBuilder()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.NotPublic);
            MethodBuilder method = type.DefineMethod("method1", MethodAttributes.Public | MethodAttributes.Static, typeof(void), new Type[] { typeof(int) });
            ParameterBuilder parameter = method.DefineParameter(1, ParameterAttributes.HasDefault, "testParam");
            ILGenerator ilGenerator = method.GetILGenerator();
            ilGenerator.Emit(OpCodes.Ret);

            Type attributeType = typeof(ParameterBuilderCustomAttribute);
            ConstructorInfo constructor = attributeType.GetConstructors()[0];
            FieldInfo field = attributeType.GetField("Field12345");


            CustomAttributeBuilder attribute = new CustomAttributeBuilder(constructor, new object[] { 4 }, new FieldInfo[] { field }, new object[] { "hello" });
            parameter.SetCustomAttribute(attribute);
            Type createdType = type.CreateTypeInfo().AsType();
            MethodInfo createdMethod = createdType.GetMethod("method1");
            ParameterInfo createdParameter = createdMethod.GetParameters()[0];

            object[] attributes = createdParameter.GetCustomAttributes(false).ToArray();
            Assert.Equal(1, attributes.Length);

            ParameterBuilderCustomAttribute obj = (ParameterBuilderCustomAttribute)attributes[0];
            Assert.Equal("hello", obj.Field12345);
            Assert.Equal(4, obj.m_ctorType2);
        }

        [Fact]
        public void SetCustomAttribute_ConstructorInfo_ByteArray()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.NotPublic);
            MethodBuilder method = type.DefineMethod("method1", MethodAttributes.Public | MethodAttributes.Static, typeof(void), new Type[] { typeof(int) });
            ParameterBuilder parameter = method.DefineParameter(1, ParameterAttributes.HasDefault, "testParam");
            ILGenerator ilGenerator = method.GetILGenerator();
            ilGenerator.Emit(OpCodes.Ret);

            parameter.SetCustomAttribute(typeof(ParameterBuilderCustomAttribute).GetConstructor(new Type[] { typeof(bool)}), new byte[] { 1, 0, 1, 0, 0});
            Type createdType = type.CreateTypeInfo().AsType();
            MethodInfo createdMethod = createdType.GetMethod("method1");
            ParameterInfo createdParameter = createdMethod.GetParameters()[0];

            object[] attributes = createdParameter.GetCustomAttributes(false).Select(a => (object)a).ToArray();
            Assert.Equal(1, attributes.Length);

            ParameterBuilderCustomAttribute obj = (ParameterBuilderCustomAttribute)attributes[0];
            Assert.True(obj.booleanValue);
        }

        [Fact]
        public void SetCustomAttribute_NullArgument_ThrowsArgumentNullException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.NotPublic);
            MethodBuilder method = type.DefineMethod("method1", MethodAttributes.Public | MethodAttributes.Static, typeof(void), new Type[] { typeof(int) });
            ParameterBuilder parameter = method.DefineParameter(1, ParameterAttributes.HasDefault, "testParam");

            AssertExtensions.Throws<ArgumentNullException>("con", () => parameter.SetCustomAttribute(null, new byte[0]));
            AssertExtensions.Throws<ArgumentNullException>("binaryAttribute", () => parameter.SetCustomAttribute(typeof(ParameterBuilderCustomAttribute).GetConstructor(new Type[] { typeof(bool) }), null));
            AssertExtensions.Throws<ArgumentNullException>("customBuilder", () => parameter.SetCustomAttribute(null));
        }
    }

    public class ParameterBuilderCustomAttribute : Attribute
    {
        public ParameterBuilderCustomAttribute(int mc)
        {
            m_ctorType2 = mc;
        }

        public ParameterBuilderCustomAttribute(bool b)
        {
            booleanValue = b;
        }

        public bool booleanValue;
        public string Field12345;
        public int m_ctorType2;
    }
}
