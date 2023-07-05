// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class PropertyBuilderTest12
    {
        [Fact]
        public void SetCustomAttribute()
        {
            Type returnType = typeof(int);
            Type[] ctorParamTypes = new Type[] { typeof(int) };

            int expectedValue = 10;
            object[] ctorParamValues = new object[] { expectedValue };
            CustomAttributeBuilder customAttrBuilder = new CustomAttributeBuilder(typeof(IntPropertyAttribute).GetConstructor(ctorParamTypes), ctorParamValues);

            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Class | TypeAttributes.Public);

            PropertyBuilder property = type.DefineProperty("TestProperty", PropertyAttributes.HasDefault, returnType, null);
            property.SetCustomAttribute(customAttrBuilder);

            MethodAttributes getMethodAttributes = MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig;
            MethodBuilder method = type.DefineMethod("TestMethod", getMethodAttributes, returnType, new Type[0]);

            ILGenerator methodILGenerator = method.GetILGenerator();
            methodILGenerator.Emit(OpCodes.Ldarg_0);
            methodILGenerator.Emit(OpCodes.Ret);

            property.SetGetMethod(method);

            BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static;
            Type createdType = type.CreateType();
            PropertyInfo createdProperty = createdType.GetProperty("TestProperty", bindingFlags);
            object[] attributes = createdProperty.GetCustomAttributes(false).ToArray();

            Assert.Equal(1, attributes.Length);
            Assert.True(attributes[0] is IntPropertyAttribute);
            Assert.Equal(expectedValue, (attributes[0] as IntPropertyAttribute).Value);
        }

        [Fact]
        public void SetCustomAttribute_CustomAttributeBuilder_NullBuilder_ThrowsArgumentNullException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Class | TypeAttributes.Public);
            PropertyBuilder property = type.DefineProperty("TestProperty", PropertyAttributes.HasDefault, typeof(int), null);

            AssertExtensions.Throws<ArgumentNullException>("customBuilder", () => property.SetCustomAttribute(null));
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/2389", TestRuntimes.Mono)]
        public void SetCustomAttribute_CustomAttributeBuilder_TypeNotCreated_ThrowsInvalidOperationException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Class | TypeAttributes.Public);
            PropertyBuilder property = type.DefineProperty("TestProperty", PropertyAttributes.HasDefault, typeof(int), null);

            Type[] ctorParamTypes = new Type[] { typeof(int) };
            object[] ctorParamValues = new object[] { 10 };
            CustomAttributeBuilder customAttrBuilder = new CustomAttributeBuilder(typeof(IntPropertyAttribute).GetConstructor(ctorParamTypes), ctorParamValues);

            type.CreateType();
            Assert.Throws<InvalidOperationException>(() => property.SetCustomAttribute(customAttrBuilder));
        }

        [Fact]
        public void SetCustomAttribute_ConstructorInfo_ByteArray()
        {
            Type returnType = typeof(int);
            Type[] ctorParamTypes = new Type[] { typeof(int) };

            int expectedValue = 10;
            byte[] binaryAttribute = new byte[6];
            ConstructorInfo con = typeof(IntPropertyAttribute).GetConstructor(ctorParamTypes);
            binaryAttribute[0] = 01;
            binaryAttribute[1] = 00;
            for (int i = 0; i < binaryAttribute.Length - 2; ++i)
            {
                binaryAttribute[i + 2] = (byte)(expectedValue >> (8 * i) & 0xff);
            }

            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Class | TypeAttributes.Public);

            PropertyBuilder property = type.DefineProperty("TestProperty", PropertyAttributes.HasDefault, returnType, null);
            property.SetCustomAttribute(con, binaryAttribute);

            MethodAttributes getMethodAttributes = MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig;
            MethodBuilder method = type.DefineMethod("TestMethod", getMethodAttributes, returnType, new Type[0]);
            ILGenerator methodILGenerator = method.GetILGenerator();
            methodILGenerator.Emit(OpCodes.Ldarg_0);
            methodILGenerator.Emit(OpCodes.Ret);

            property.SetGetMethod(method);

            BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static;
            Type createdType = type.CreateType();
            PropertyInfo createdProperty = createdType.GetProperty("TestProperty", bindingFlags);
            object[] attributes = createdProperty.GetCustomAttributes(false).ToArray();

            Assert.Equal(1, attributes.Length);
            Assert.True(attributes[0] is IntPropertyAttribute);
            Assert.Equal(expectedValue, (attributes[0] as IntPropertyAttribute).Value);
        }

        [Fact]
        public void SetCustomAttribute_ConstructorInfo_ByteArray_NullConstructorInfo_ThrowsArgumentNullException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Class | TypeAttributes.Public);
            PropertyBuilder property = type.DefineProperty("TestProperty", PropertyAttributes.HasDefault, typeof(int), null);

            AssertExtensions.Throws<ArgumentNullException>("con", () => property.SetCustomAttribute(null, new byte[6]));
        }


        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/2389", TestRuntimes.Mono)]
        public void SetCustomAttribute_ConstructorInfo_ByteArray_TypeAlreadyCreated_ThrowsInvalidOperationException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Class | TypeAttributes.Public);
            PropertyBuilder property = type.DefineProperty("TestProperty", PropertyAttributes.HasDefault, typeof(int), null);

            ConstructorInfo con = typeof(IntPropertyAttribute).GetConstructor(new Type[] { typeof(int) });

            type.CreateType();
            Assert.Throws<InvalidOperationException>(() => property.SetCustomAttribute(con, new byte[6]));
        }
    }

    [AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
    public class IntPropertyAttribute : Attribute
    {
        private int _value;
        public int Value { get { return _value; } }

        public IntPropertyAttribute(int value)
        {
            _value = value;
        }

        public IntPropertyAttribute() : this(0) { }
    }
}
