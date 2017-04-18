// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class PropertyBuilderTest2
    {
        [Theory]
        [InlineData(MethodAttributes.Public, CallingConventions.HasThis, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)]
        [InlineData(MethodAttributes.Private, CallingConventions.HasThis, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)]
        [InlineData(MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Any, BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Instance)]
        public void AddOtherMethod(MethodAttributes attributes, CallingConventions callingConventions, BindingFlags bindingFlags)
        {
            Type[] paramTypes = new Type[] { typeof(int) };

            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Class | TypeAttributes.Public);
            PropertyBuilder property = type.DefineProperty("TestProperty", PropertyAttributes.None, typeof(int), new Type[0]);
            MethodBuilder method = type.DefineMethod("TestMethod", attributes, callingConventions, typeof(int), paramTypes);

            ILGenerator methodILGenerator = method.GetILGenerator();
            methodILGenerator.Emit(OpCodes.Ldarg_0);
            methodILGenerator.Emit(OpCodes.Ldarg_1);
            methodILGenerator.Emit(OpCodes.Ret);

            property.AddOtherMethod(method);
            Type createdType = type.CreateTypeInfo().AsType();
            PropertyInfo createdProperty = createdType.GetProperty("TestProperty", bindingFlags);

            MethodInfo[] actualMethods = createdProperty.GetAccessors(true);
            Assert.Equal(1, actualMethods.Length);
            Assert.Equal(method.Name, actualMethods[0].Name);
        }

        [Fact]
        public void AddOtherMethod_NullMethodBuilder_ThrowsArgumentNullExceptio()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Class | TypeAttributes.Public);
            PropertyBuilder property = type.DefineProperty("TestProperty", PropertyAttributes.None, typeof(int), new Type[0]);

            AssertExtensions.Throws<ArgumentNullException>("mdBuilder", () => property.AddOtherMethod(null));
        }

        [Fact]
        public void AddOtherMethod_TypeAlreadyCreated_ThrowsInvalidOperationException()
        {
            Type[] paramTypes = new Type[] { typeof(int) };

            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Class | TypeAttributes.Public);
            PropertyBuilder property = type.DefineProperty("TestProperty", PropertyAttributes.None, typeof(int), new Type[0]);
            MethodBuilder method = type.DefineMethod("TestMethod", MethodAttributes.Public, CallingConventions.HasThis, typeof(int), paramTypes);
            ILGenerator methodILGenerator = method.GetILGenerator();

            methodILGenerator.Emit(OpCodes.Ldarg_0);
            methodILGenerator.Emit(OpCodes.Ldarg_1);
            methodILGenerator.Emit(OpCodes.Ret);

            type.CreateTypeInfo().AsType();
            Assert.Throws<InvalidOperationException>(() => property.AddOtherMethod(method));
        }
    }
}

