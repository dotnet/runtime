// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class PropertyBuilderTest15
    {
        [Theory]
        [InlineData(MethodAttributes.Private | MethodAttributes.SpecialName | MethodAttributes.HideBySig)]
        [InlineData(MethodAttributes.Family | MethodAttributes.SpecialName | MethodAttributes.HideBySig)]
        [InlineData(MethodAttributes.FamORAssem | MethodAttributes.SpecialName | MethodAttributes.HideBySig)]
        [InlineData(MethodAttributes.Static | MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig)]
        [InlineData(MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig)]
        public void SetSetMethod(MethodAttributes methodAttributes)
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Class | TypeAttributes.Public);
            FieldBuilder field = type.DefineField("TestField", typeof(int), FieldAttributes.Private);
            PropertyBuilder property = type.DefineProperty("TestProperty", PropertyAttributes.None, typeof(int), null);
            MethodBuilder method = ImplementMethod( type, "TestMethod", methodAttributes, null, new Type[] { typeof(int) }, field);

            property.SetSetMethod(method);
            MethodInfo actualMethod = property.GetSetMethod(true);
            Assert.Equal(method.Name, actualMethod.Name);
        }

        [Fact]
        public void SetSetMethod_NullMethodBuilder_ThrowsArgumentNullException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Class | TypeAttributes.Public);
            PropertyBuilder property = type.DefineProperty("TestProperty", PropertyAttributes.None, typeof(int), null);
            AssertExtensions.Throws<ArgumentNullException>("mdBuilder", () => property.SetSetMethod(null));
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/2389", TestRuntimes.Mono)]
        public void TestThrowsExceptionForCreateTypeCalled()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Class | TypeAttributes.Public);
            FieldBuilder field = type.DefineField("TestField", typeof(int), FieldAttributes.Private);
            PropertyBuilder property = type.DefineProperty("TestProperty", PropertyAttributes.None, typeof(int), null);

            MethodAttributes setMethodAttributes = MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig;
            MethodBuilder method = ImplementMethod(type, "TestMethod", setMethodAttributes, null, new Type[] { typeof(int) }, field);

            type.CreateType();
            Assert.Throws<InvalidOperationException>(() => { property.SetSetMethod(method); });
        }

        private MethodBuilder ImplementMethod(TypeBuilder type, string methodName, MethodAttributes methodAttr, Type returnType, Type[] paramTypes, FieldBuilder field)
        {
            MethodBuilder method = type.DefineMethod(methodName, methodAttr, returnType, paramTypes);

            ILGenerator methodILGenerator = method.GetILGenerator();
            methodILGenerator.Emit(OpCodes.Ldarg_0);
            methodILGenerator.Emit(OpCodes.Ldarg_1);
            methodILGenerator.Emit(OpCodes.Stfld, field);
            methodILGenerator.Emit(OpCodes.Ret);

            return method;
        }
    }
}
