// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class PropertyBuilderTest14
    {
        [Theory]
        [InlineData(MethodAttributes.Private | MethodAttributes.SpecialName | MethodAttributes.HideBySig)]
        [InlineData(MethodAttributes.Family | MethodAttributes.SpecialName | MethodAttributes.HideBySig)]
        [InlineData(MethodAttributes.FamORAssem | MethodAttributes.SpecialName | MethodAttributes.HideBySig)]
        [InlineData(MethodAttributes.Static | MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig)]
        [InlineData(MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig)]
        public void SetGetMethod(MethodAttributes methodAttributes)
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Class | TypeAttributes.Public);
            FieldBuilder field = type.DefineField("TestField", typeof(int), FieldAttributes.Private);

            PropertyBuilder property = type.DefineProperty("TestProperty", PropertyAttributes.None, typeof(int), null);
            MethodBuilder method = type.DefineMethod("TestMethod", methodAttributes, typeof(int), null);

            ILGenerator methodILGenerator = method.GetILGenerator();
            methodILGenerator.Emit(OpCodes.Ldarg_0);
            methodILGenerator.Emit(OpCodes.Ldfld, field);
            methodILGenerator.Emit(OpCodes.Ret);

            property.SetGetMethod(method);
            MethodInfo actualMethod = property.GetGetMethod(true);
            Assert.Equal(method.Name, actualMethod.Name);
        }

        [Fact]
        public void SetGetMethod_NullMethodBuilder_ThrowsArgumentNullException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Class | TypeAttributes.Public);
            PropertyBuilder property = type.DefineProperty("TestProperty", PropertyAttributes.None, typeof(int), null);
            AssertExtensions.Throws<ArgumentNullException>("mdBuilder", () => property.SetGetMethod(null));
        }

        [Fact]
        public void SetGetMethod_TypeAlreadyCreated_ThrowsInvalidOperationException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Class | TypeAttributes.Public);
            FieldBuilder field = type.DefineField("TestField", typeof(int), FieldAttributes.Private);
            PropertyBuilder property = type.DefineProperty("TestProperty", PropertyAttributes.None, typeof(int), null);

            MethodAttributes getMethodAttributes = MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig;
            MethodBuilder method = type.DefineMethod("TestMethod", getMethodAttributes, typeof(int), null);

            ILGenerator methodILGenerator = method.GetILGenerator();
            methodILGenerator.Emit(OpCodes.Ldfld, field);
            methodILGenerator.Emit(OpCodes.Ret);

            type.CreateType();
            Assert.Throws<InvalidOperationException>(() => property.SetGetMethod(method));
        }
    }
}
