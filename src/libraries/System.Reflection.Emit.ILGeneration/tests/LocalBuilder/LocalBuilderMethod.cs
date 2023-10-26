// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Reflection.Emit.Tests
{
    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.IsNotMonoRuntime))]
    public class LocalBuilderMethod
    {
        [Fact]
        public void LocalBuilder_MethodReturnsCorrectMethod()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.NotPublic);
            MethodBuilder method1 = type.DefineMethod("Method1", MethodAttributes.Public);
            MethodBuilder method2 = type.DefineMethod("Method2", MethodAttributes.Public);
            ILGenerator ilGenerator1 = method1.GetILGenerator();
            ILGenerator ilGenerator2 = method2.GetILGenerator();
            LocalBuilder local1 = ilGenerator1.DeclareLocal(typeof(int));
            LocalBuilder local2 = ilGenerator2.DeclareLocal(typeof(string), true);

            Assert.Equal(method1, local1.Method);
            Assert.Equal(method2, local2.Method);
        }

        [Fact]
        public void LocalBuilder_UseLocalsThatBelongToTheMethod()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.NotPublic);
            MethodBuilder method1 = type.DefineMethod("Method1", MethodAttributes.Public);
            MethodBuilder method2 = type.DefineMethod("Method2", MethodAttributes.Public);
            ILGenerator ilGenerator1 = method1.GetILGenerator();
            ILGenerator ilGenerator2 = method2.GetILGenerator();
            LocalBuilder local1 = ilGenerator1.DeclareLocal(typeof(short));
            LocalBuilder local2 = ilGenerator2.DeclareLocal(typeof(long), true);

            ilGenerator1.Emit(OpCodes.Ldloc, local1);
            ilGenerator2.Emit(OpCodes.Ldloc, local2);

            Assert.Throws<ArgumentException>(() => ilGenerator1.Emit(OpCodes.Ldloc, local2));
            Assert.Throws<ArgumentException>(() => ilGenerator2.Emit(OpCodes.Ldloc, local1));
        }
    }
}
