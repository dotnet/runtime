// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class MethodBuilderGetILGenerator
    {
        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsReflectionEmitSupported))]
        [InlineData(AssemblyBuilderAccess.Run, 0)]
        [InlineData(AssemblyBuilderAccess.Run, 512)]
        [InlineData(AssemblyBuilderAccess.RunAndCollect, 0)]
        [InlineData(AssemblyBuilderAccess.RunAndCollect, 512)]
        public void GetILGenerator_BodiesSurviveFurtherEmission(AssemblyBuilderAccess access, int padding)
        {
            ModuleBuilder module = Helpers.DynamicAssembly(access: access).DefineDynamicModule("MethodModule");
            TypeBuilder type = module.DefineType("Methods", TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Sealed);
            MethodBuilder constant = type.DefineMethod("Constant", MethodAttributes.Public | MethodAttributes.Static, typeof(int), Type.EmptyTypes);
            constant.InitLocals = false;
            ILGenerator constantIL = constant.GetILGenerator();
            constantIL.Emit(OpCodes.Ldc_I4, 37);
            constantIL.Emit(OpCodes.Ret);

            MethodBuilder method = type.DefineMethod("WithExceptions", MethodAttributes.Public | MethodAttributes.Static, typeof(int), new[] { typeof(int) });
            ILGenerator il = method.GetILGenerator();
            LocalBuilder result = il.DeclareLocal(typeof(int));
            Label nonNegative = il.DefineLabel();
            il.BeginExceptionBlock();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Bge_S, nonNegative);
            il.Emit(OpCodes.Newobj, typeof(InvalidOperationException).GetConstructor(Type.EmptyTypes));
            il.Emit(OpCodes.Throw);
            il.MarkLabel(nonNegative);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Call, typeof(Math).GetMethod(nameof(Math.Abs), new[] { typeof(int) }));
            il.Emit(OpCodes.Ldstr, "abc");
            il.Emit(OpCodes.Callvirt, typeof(string).GetProperty(nameof(string.Length)).GetMethod);
            il.Emit(OpCodes.Add);
            il.Emit(OpCodes.Stloc, result);
            il.Emit(OpCodes.Ldtoken, typeof(int));
            il.Emit(OpCodes.Call, typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle)));
            il.Emit(OpCodes.Pop);
            for (int i = 0; i < padding; i++)
            {
                il.Emit(OpCodes.Nop);
            }
            il.BeginCatchBlock(typeof(InvalidOperationException));
            il.Emit(OpCodes.Pop);
            il.Emit(OpCodes.Ldc_I4, -7);
            il.Emit(OpCodes.Stloc, result);
            il.BeginFinallyBlock();
            il.Emit(OpCodes.Ldloc, result);
            il.Emit(OpCodes.Ldc_I4_1);
            il.Emit(OpCodes.Add);
            il.Emit(OpCodes.Stloc, result);
            il.EndExceptionBlock();
            il.Emit(OpCodes.Ldloc, result);
            il.Emit(OpCodes.Ret);

            Type createdType = type.CreateType();
            MethodInfo createdMethod = createdType.GetMethod(method.Name);
            MethodInfo createdConstant = createdType.GetMethod(constant.Name);
            Assert.Equal(37, createdConstant.Invoke(null, null));
            Assert.Equal(6, createdMethod.Invoke(null, new object[] { 2 }));
            Assert.Equal(-6, createdMethod.Invoke(null, new object[] { -2 }));
            Assert.Equal(method.MetadataToken, createdMethod.MetadataToken);

            MethodBody body = createdMethod.GetMethodBody();
            Assert.True(body.InitLocals);
            Assert.Equal(typeof(int), Assert.Single(body.LocalVariables).LocalType);
            Assert.Equal(2, body.ExceptionHandlingClauses.Count);
            Assert.Contains(body.ExceptionHandlingClauses, clause => clause.Flags == ExceptionHandlingClauseOptions.Clause && clause.CatchType == typeof(InvalidOperationException));
            Assert.Contains(body.ExceptionHandlingClauses, clause => clause.Flags == ExceptionHandlingClauseOptions.Finally);
            byte[] bytes = body.GetILAsByteArray();
            Assert.Equal(il.ILOffset, bytes.Length);
            Assert.Empty(createdConstant.GetMethodBody().ExceptionHandlingClauses);
            Assert.Empty(createdConstant.GetMethodBody().LocalVariables);
            Assert.False(createdConstant.GetMethodBody().InitLocals);

            TypeBuilder moreMethods = module.DefineType("MoreMethods", TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Sealed);
            for (int i = 0; i < 64; i++)
            {
                MethodBuilder extra = moreMethods.DefineMethod($"Extra{i}", MethodAttributes.Public | MethodAttributes.Static, typeof(int), Type.EmptyTypes);
                ILGenerator extraIL = extra.GetILGenerator();
                extraIL.Emit(OpCodes.Ldc_I4, i);
                extraIL.Emit(OpCodes.Ret);
            }
            Type extraType = moreMethods.CreateType();
            Assert.Equal(63, extraType.GetMethod("Extra63").Invoke(null, null));

            Assert.Equal(bytes, createdMethod.GetMethodBody().GetILAsByteArray());
            Assert.Equal(37, createdConstant.Invoke(null, null));
            Assert.Equal(6, createdMethod.Invoke(null, new object[] { 2 }));
            Assert.Equal(-6, createdMethod.Invoke(null, new object[] { -2 }));
            Assert.Equal(createdMethod, module.ResolveMethod(method.MetadataToken));
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsReflectionEmitSupported))]
        [InlineData(20)]
        [InlineData(-10)]
        public void GetILGenerator_Int(int size)
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            MethodBuilder method = type.DefineMethod("TestMethod", MethodAttributes.Public | MethodAttributes.Static, typeof(int), new Type[0]);

            ILGenerator ilGenerator = method.GetILGenerator(size);
            int expectedReturn = 5;
            ilGenerator.Emit(OpCodes.Ldc_I4, expectedReturn);
            ilGenerator.Emit(OpCodes.Ret);

            Type createdType = type.CreateType();
            MethodInfo createdMethod = createdType.GetMethod("TestMethod");
            Assert.Equal(expectedReturn, createdMethod.Invoke(null, null));

            // Verify MetadataToken
            Assert.Equal(method.MetadataToken, createdMethod.MetadataToken);
            MethodInfo methodFromToken = (MethodInfo)type.Module.ResolveMethod(method.MetadataToken);
            Assert.Equal(createdMethod, methodFromToken);

            MemberInfo memberInfoFromToken = (MemberInfo)type.Module.ResolveMember(method.MetadataToken);
            Assert.Equal(methodFromToken, memberInfoFromToken);
        }

        [Theory]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/2389", TestRuntimes.Mono)]
        [InlineData(TypeAttributes.Public, MethodAttributes.Public | MethodAttributes.PinvokeImpl)]
        [InlineData(TypeAttributes.Abstract, MethodAttributes.PinvokeImpl)]
        [InlineData(TypeAttributes.Abstract, MethodAttributes.Abstract | MethodAttributes.PinvokeImpl)]
        public void GetILGenerator_NoMethodBody_ThrowsInvalidOperationException(TypeAttributes typeAttributes, MethodAttributes methodAttributes)
        {
            TypeBuilder type = Helpers.DynamicType(typeAttributes);
            MethodBuilder method = type.DefineMethod("TestMethod", methodAttributes);

            Assert.Throws<InvalidOperationException>(() => method.GetILGenerator());
            Assert.Throws<InvalidOperationException>(() => method.GetILGenerator(10));
        }

        [Theory]
        [InlineData(MethodAttributes.Abstract)]
        [InlineData(MethodAttributes.Assembly)]
        [InlineData(MethodAttributes.CheckAccessOnOverride)]
        [InlineData(MethodAttributes.FamANDAssem)]
        [InlineData(MethodAttributes.Family)]
        [InlineData(MethodAttributes.FamORAssem)]
        [InlineData(MethodAttributes.Final)]
        [InlineData(MethodAttributes.HasSecurity)]
        [InlineData(MethodAttributes.HideBySig)]
        [InlineData(MethodAttributes.MemberAccessMask)]
        [InlineData(MethodAttributes.NewSlot)]
        [InlineData(MethodAttributes.Private)]
        [InlineData(MethodAttributes.Public)]
        [InlineData(MethodAttributes.RequireSecObject)]
        [InlineData(MethodAttributes.ReuseSlot)]
        [InlineData(MethodAttributes.RTSpecialName)]
        [InlineData(MethodAttributes.SpecialName)]
        [InlineData(MethodAttributes.Static)]
        [InlineData(MethodAttributes.UnmanagedExport)]
        [InlineData(MethodAttributes.Virtual)]
        [InlineData(MethodAttributes.Assembly | MethodAttributes.CheckAccessOnOverride |
                MethodAttributes.FamORAssem | MethodAttributes.Final |
                MethodAttributes.HasSecurity | MethodAttributes.HideBySig | MethodAttributes.MemberAccessMask |
                MethodAttributes.NewSlot | MethodAttributes.Private |
                MethodAttributes.PrivateScope | MethodAttributes.RequireSecObject |
                MethodAttributes.RTSpecialName | MethodAttributes.SpecialName |
                MethodAttributes.Static | MethodAttributes.UnmanagedExport)]
        public void GetILGenerator_DifferentAttributes(MethodAttributes attributes)
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Abstract);
            MethodBuilder method = type.DefineMethod(attributes.ToString(), attributes);
            Assert.NotNull(method.GetILGenerator());
        }

        [Fact]
        public void LoadPointerTypeInILGeneratedMethod()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            Type pointerType = type.MakePointerType();

            MethodBuilder method = type.DefineMethod("TestMethod", MethodAttributes.Public | MethodAttributes.Static, typeof(string), Type.EmptyTypes);
            ILGenerator ilGenerator = method.GetILGenerator();

            ilGenerator.Emit(OpCodes.Ldtoken, pointerType);
            ilGenerator.Emit(OpCodes.Call, typeof(Type).GetMethod("GetTypeFromHandle", BindingFlags.Static | BindingFlags.Public));
            ilGenerator.Emit(OpCodes.Callvirt, typeof(Type).GetMethod("get_FullName"));
            ilGenerator.Emit(OpCodes.Ret);

            Type createdType = type.CreateType();
            MethodInfo createdMethod = createdType.GetMethod("TestMethod");
            Assert.Equal("TestType*", createdMethod.Invoke(null, null));
        }

        [Fact]
        public void LoadArrayTypeInILGeneratedMethod()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            Type arrayType = type.MakeArrayType();

            MethodBuilder method = type.DefineMethod("TestMethod", MethodAttributes.Public | MethodAttributes.Static, typeof(string), Type.EmptyTypes);
            ILGenerator ilGenerator = method.GetILGenerator();

            ilGenerator.Emit(OpCodes.Ldtoken, arrayType);
            ilGenerator.Emit(OpCodes.Call, typeof(Type).GetMethod("GetTypeFromHandle", BindingFlags.Static | BindingFlags.Public));
            ilGenerator.Emit(OpCodes.Callvirt, typeof(Type).GetMethod("get_FullName"));
            ilGenerator.Emit(OpCodes.Ret);

            Type createdType = type.CreateType();
            MethodInfo createdMethod = createdType.GetMethod("TestMethod");
            Assert.Equal("TestType[]", createdMethod.Invoke(null, null));
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/82257", TestRuntimes.Mono)]
        public void LoadByRefTypeInILGeneratedMethod()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            Type byrefType = type.MakeByRefType();

            MethodBuilder method = type.DefineMethod("TestMethod", MethodAttributes.Public | MethodAttributes.Static, typeof(string), Type.EmptyTypes);
            ILGenerator ilGenerator = method.GetILGenerator();

            ilGenerator.Emit(OpCodes.Ldtoken, byrefType);
            ilGenerator.Emit(OpCodes.Call, typeof(Type).GetMethod("GetTypeFromHandle", BindingFlags.Static | BindingFlags.Public));
            ilGenerator.Emit(OpCodes.Callvirt, typeof(Type).GetMethod("get_FullName"));
            ilGenerator.Emit(OpCodes.Ret);

            Type createdType = type.CreateType();
            MethodInfo createdMethod = createdType.GetMethod("TestMethod");
            Assert.Equal("TestType&", createdMethod.Invoke(null, null));
        }

        [Fact]
        public void HasDefaultValueShouldBeFalseWhenParameterDoNotDefineDefaultValue()
        {
            var builder = Helpers.DynamicModule();
            var type = builder.DefineType("MyProxy", TypeAttributes.Public);

            var methodBuilder = type.DefineMethod("DoSomething", MethodAttributes.Public, CallingConventions.Standard, typeof(void), new[] { typeof(Version) });
            var il = methodBuilder.GetILGenerator();
            il.Emit(OpCodes.Ret);

            var typeInfo = type.CreateTypeInfo();
            var method = typeInfo.GetMethod("DoSomething", new[] { typeof(Version) });
            var parameters = method.GetParameters();
            Assert.False(parameters[0].HasDefaultValue);
        }

        [Fact]
        public void HasDefaultValueShouldBeTrueWhenParameterDoDefineDefaultValue()
        {
            var builder = Helpers.DynamicModule();
            var type = builder.DefineType("MyProxy", TypeAttributes.Public);

            var methodBuilder = type.DefineMethod("DoSomething", MethodAttributes.Public, CallingConventions.Standard, typeof(void), new[] { typeof(Version) });
            ParameterBuilder parameter = methodBuilder.DefineParameter(1, ParameterAttributes.Optional | ParameterAttributes.HasDefault, "param1");
            parameter.SetConstant(default(Version));
            var il = methodBuilder.GetILGenerator();
            il.Emit(OpCodes.Ret);

            var typeInfo = type.CreateTypeInfo();
            var method = typeInfo.GetMethod("DoSomething", new[] { typeof(Version) });
            var parameters = method.GetParameters();
            Assert.True(parameters[0].HasDefaultValue);
            Assert.Null(parameters[0].DefaultValue);
        }
    }
}
