// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class PropertyBuilderTest16
    {
        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/2389", TestRuntimes.Mono)]
        public void SetValue_ThrowsNotSupportedException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Class | TypeAttributes.Public);
            FieldBuilder field = type.DefineField("TestField", typeof(int), FieldAttributes.Private);
            PropertyBuilder property = type.DefineProperty("TestProperty", PropertyAttributes.None, typeof(int), null);

            MethodAttributes getMethodAttributes = MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig;
            MethodBuilder method = type.DefineMethod("TestMethod", getMethodAttributes, typeof(int), null);

            ILGenerator methodILGenerator = method.GetILGenerator();
            methodILGenerator.Emit(OpCodes.Ldarg_0);
            methodILGenerator.Emit(OpCodes.Ldfld, field);
            methodILGenerator.Emit(OpCodes.Ret);
            property.SetGetMethod(method);

            Type createdType = type.CreateType();
            object obj = createdType.GetConstructor(new Type[0]).Invoke(null);
            Assert.Throws<NotSupportedException>(() => property.SetValue(obj, 99, null));
        }
    }
}
