// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Linq;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    public class AssemblySaveILGeneratorTests
    {
        [Fact]
        public void MethodWithEmptyBody()
        {
            using (TempFile file = TempFile.Create())
            {
                AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilderTypeBuilderAndSaveMethod(out TypeBuilder type, out MethodInfo saveMethod);
                MethodBuilder methodBuilder = type.DefineMethod("EmptyMethod", MethodAttributes.Public, typeof(void), new[] { typeof(Version) });
                ILGenerator il = methodBuilder.GetILGenerator();
                il.Emit(OpCodes.Ret);
                saveMethod.Invoke(ab, new object[] { file.Path });

                Assembly assemblyFromDisk = AssemblySaveTools.LoadAssemblyFromPath(file.Path);
                Type typeFromDisk = assemblyFromDisk.Modules.First().GetType("MyType");
                MethodInfo method = typeFromDisk.GetMethod("EmptyMethod");
                MethodBody body = method.GetMethodBody();
                Assert.Empty(body.LocalVariables);
                Assert.Empty(body.ExceptionHandlingClauses);
                byte[]? bodyBytes = body.GetILAsByteArray();
                Assert.NotNull(bodyBytes);
                Assert.Equal(OpCodes.Ret.Value, bodyBytes[0]);

            }
        }

        [Theory]
        [InlineData(20)]
        [InlineData(-10)] // For compat, runtime implementation doesn't throw for negative value.
        public void MethodReturning_Int(int size)
        {
            using (TempFile file = TempFile.Create())
            {
                AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilderTypeBuilderAndSaveMethod(out TypeBuilder type, out MethodInfo saveMethod);
                MethodBuilder method = type.DefineMethod("TestMethod", MethodAttributes.Public | MethodAttributes.Static, typeof(int), Type.EmptyTypes);

                ILGenerator ilGenerator = method.GetILGenerator(size);
                int expectedReturn = 5;
                ilGenerator.Emit(OpCodes.Ldc_I4, expectedReturn);
                ilGenerator.Emit(OpCodes.Ret);
                saveMethod.Invoke(ab, new object[] { file.Path });

                Assembly assemblyFromDisk = AssemblySaveTools.LoadAssemblyFromPath(file.Path);
                Type typeFromDisk = assemblyFromDisk.Modules.First().GetType("MyType");
                MethodInfo methodFromFile = typeFromDisk.GetMethod("TestMethod");
                MethodBody body = methodFromFile.GetMethodBody();
                byte[]? bodyBytes = body.GetILAsByteArray();
                Assert.NotNull(bodyBytes);
                Assert.Equal(OpCodes.Ldc_I4_5.Value, bodyBytes[0]);
                Assert.Equal(OpCodes.Ret.Value, bodyBytes[1]);
            }
        }

        [Theory]
        [InlineData(20)]
        [InlineData(11)]
        public void TypeWithTwoMethod_ReferenceMethodArguments(int multiplier)
        {
            using (TempFile file = TempFile.Create())
            {
                AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilderTypeBuilderAndSaveMethod(out TypeBuilder type, out MethodInfo saveMethod);
                MethodBuilder multiplyMethod = type.DefineMethod("MultiplyMethod", MethodAttributes.Public | MethodAttributes.Static, typeof(int), new Type[] { typeof(int) });
                multiplyMethod.DefineParameter(1, ParameterAttributes.None, "myParam");
                MethodBuilder addMethod = type.DefineMethod("AddMethod", MethodAttributes.Public | MethodAttributes.Static, typeof(int), new Type[] { typeof(int), typeof(int) });
                addMethod.DefineParameter(1, ParameterAttributes.None, "firsParam");
                addMethod.DefineParameter(2, ParameterAttributes.None, "secondParam");

                ILGenerator multiplyMethodIL = multiplyMethod.GetILGenerator();
                multiplyMethodIL.Emit(OpCodes.Ldarg_0);
                multiplyMethodIL.Emit(OpCodes.Ldc_I4, multiplier);
                multiplyMethodIL.Emit(OpCodes.Mul);
                multiplyMethodIL.Emit(OpCodes.Ret);

                ILGenerator addMethodIL = addMethod.GetILGenerator();
                addMethodIL.Emit(OpCodes.Ldarg_0);
                addMethodIL.Emit(OpCodes.Ldarg_1);
                addMethodIL.Emit(OpCodes.Add);
                addMethodIL.Emit(OpCodes.Ret);

                saveMethod.Invoke(ab, new object[] { file.Path });

                Assembly assemblyFromDisk = AssemblySaveTools.LoadAssemblyFromPath(file.Path);
                Type typeFromDisk = assemblyFromDisk.Modules.First().GetType("MyType");
                byte[]? multiplyBody = typeFromDisk.GetMethod("MultiplyMethod").GetMethodBody().GetILAsByteArray();
                byte[]? addBody = typeFromDisk.GetMethod("AddMethod").GetMethodBody().GetILAsByteArray();

                Assert.NotNull(multiplyBody);
                Assert.Equal(OpCodes.Ldarg_0.Value, multiplyBody[0]);
                Assert.Equal(OpCodes.Ldc_I4_S.Value, multiplyBody[1]);
                Assert.Equal(multiplier, multiplyBody[2]);
                Assert.Equal(OpCodes.Mul.Value, multiplyBody[3]);
                Assert.NotNull(addBody);
                Assert.Equal(OpCodes.Ldarg_0.Value, addBody[0]);
                Assert.Equal(OpCodes.Ldarg_1.Value, addBody[1]);
                Assert.Equal(OpCodes.Add.Value, addBody[2]);
            }
        }

        [Fact]
        public void MultipleTypesWithMultipleMethods()
        {
            using (TempFile file = TempFile.Create())
            {
                AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilderTypeBuilderAndSaveMethod(out TypeBuilder type, out MethodInfo saveMethod);
                MethodBuilder multiplyMethod = type.DefineMethod("MultiplyMethod", MethodAttributes.Public, typeof(short), new Type[] { typeof(short) });
                MethodBuilder addMethod = type.DefineMethod("AddMethod", MethodAttributes.Public | MethodAttributes.Static, typeof(double), new Type[] { typeof(double) });

                ILGenerator multiplyMethodIL = multiplyMethod.GetILGenerator();
                multiplyMethodIL.Emit(OpCodes.Ldarg, 1);
                multiplyMethodIL.Emit(OpCodes.Ldc_I4_S, (short)11);
                multiplyMethodIL.Emit(OpCodes.Mul);
                multiplyMethodIL.Emit(OpCodes.Ret);
                ILGenerator addMethodIL = addMethod.GetILGenerator();
                addMethodIL.Emit(OpCodes.Ldarg_0);
                addMethodIL.Emit(OpCodes.Ldc_R8, (double)123456.123);
                addMethodIL.Emit(OpCodes.Add);
                addMethodIL.Emit(OpCodes.Ret);

                TypeBuilder anotherType = ab.GetDynamicModule("MyModule").DefineType("AnotherType", TypeAttributes.NotPublic);
                MethodBuilder stringMethod = anotherType.DefineMethod("StringMethod", MethodAttributes.FamORAssem, typeof(string), Type.EmptyTypes);
                MethodBuilder floatMethod = anotherType.DefineMethod("FloatMethod", MethodAttributes.Family, typeof(float), Type.EmptyTypes);
                MethodBuilder longMethod = anotherType.DefineMethod("LongMethod", MethodAttributes.Static, typeof(long), Type.EmptyTypes);

                ILGenerator stringMethodIL = stringMethod.GetILGenerator();
                stringMethodIL.Emit(OpCodes.Ldstr, "Hello world!");
                stringMethodIL.Emit(OpCodes.Ret);
                ILGenerator floatMethodIL = floatMethod.GetILGenerator();
                floatMethodIL.Emit(OpCodes.Ldc_R4, (float)123456.123);
                floatMethodIL.Emit(OpCodes.Ret);
                ILGenerator longMethodIL = longMethod.GetILGenerator();
                longMethodIL.Emit(OpCodes.Ldc_I8, (long)1234567);
                longMethodIL.Emit(OpCodes.Ret);

                saveMethod.Invoke(ab, new object[] { file.Path });

                Assembly assemblyFromDisk = AssemblySaveTools.LoadAssemblyFromPath(file.Path);
                Module moduleFromFile = assemblyFromDisk.Modules.First();
                Type typeFromDisk = moduleFromFile.GetType("MyType");
                Type anotherTypeFromDisk = moduleFromFile.GetType("AnotherType");
                byte[]? multiplyBody = typeFromDisk.GetMethod("MultiplyMethod").GetMethodBody().GetILAsByteArray();
                byte[]? addBody = typeFromDisk.GetMethod("AddMethod").GetMethodBody().GetILAsByteArray();
                byte[]? stringBody = anotherTypeFromDisk.GetMethod("StringMethod", BindingFlags.NonPublic | BindingFlags.Instance).GetMethodBody().GetILAsByteArray();
                byte[]? floatBody = anotherTypeFromDisk.GetMethod("FloatMethod", BindingFlags.NonPublic | BindingFlags.Instance).GetMethodBody().GetILAsByteArray();
                byte[]? longBody = anotherTypeFromDisk.GetMethod("LongMethod", BindingFlags.NonPublic | BindingFlags.Static).GetMethodBody().GetILAsByteArray();

                Assert.NotNull(multiplyBody);
                Assert.Equal(OpCodes.Ldarg_1.Value, multiplyBody[0]);
                Assert.Equal(OpCodes.Ldc_I4_S.Value, multiplyBody[1]);
                Assert.Equal(11, multiplyBody[2]);
                Assert.NotNull(addBody);
                Assert.Equal(OpCodes.Ldarg_0.Value, addBody[0]);
                Assert.Equal(OpCodes.Ldc_R8.Value, addBody[1]);
                Assert.NotNull(stringBody);
                Assert.Equal(OpCodes.Ldstr.Value, stringBody[0]);
                Assert.NotNull(floatBody);
                Assert.Equal(OpCodes.Ldc_R4.Value, floatBody[0]);
                Assert.NotNull(longBody);
                Assert.Equal(OpCodes.Ldc_I8.Value, longBody[0]);
            }
        }

        [Fact]
        public void ILOffset_Test()
        {
            AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilderTypeBuilderAndSaveMethod(out TypeBuilder type, out MethodInfo saveMethod);
            MethodBuilder method = type.DefineMethod("Method1", MethodAttributes.Public | MethodAttributes.Static, typeof(Type), new Type[0]);
            ILGenerator ilGenerator = method.GetILGenerator();

            Assert.Equal(0, ilGenerator.ILOffset);
            ilGenerator.Emit(OpCodes.Ret);
            Assert.Equal(1, ilGenerator.ILOffset);
        }

        [Fact]
        public void ILMaxStack_Test()
        {
            using (TempFile file = TempFile.Create())
            {
                AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilderTypeBuilderAndSaveMethod(out TypeBuilder type, out MethodInfo saveMethod);
                MethodBuilder method1 = type.DefineMethod("Method1", MethodAttributes.Public, typeof(long), new Type[] { typeof(int), typeof(long), typeof(short), typeof(byte) });
                ILGenerator il1 = method1.GetILGenerator();

                // public int Method1(int x, int y, short z, byte r) =>
                //  x + (z + 2 * (r + (8 * y + 3 * (y - (5 + x))))
                il1.Emit(OpCodes.Ldarg_1);     // push 1 MaxStack 1
                il1.Emit(OpCodes.Ldarg_3);     // push 1 MaxStack 2
                il1.Emit(OpCodes.Ldc_I4_2);    // push 1 MaxStack 3
                il1.Emit(OpCodes.Ldarg_S, 4);  // push 1 MaxStack 4
                il1.Emit(OpCodes.Ldc_I4_8);    // push 1 MaxStack 5
                il1.Emit(OpCodes.Ldarg_2);     // push 1 MaxStack 6
                il1.Emit(OpCodes.Mul);         // pop 2 push 1 MaxStack 5
                il1.Emit(OpCodes.Ldc_I4_3);    // push 1 MaxStack 6
                il1.Emit(OpCodes.Ldarg_2);     // push 1 MaxStack 7
                il1.Emit(OpCodes.Ldc_I4_5);    // push 1 MaxStack 8
                il1.Emit(OpCodes.Ldarg_1);     // push 1 MaxStack 9
                il1.Emit(OpCodes.Add);         // pop 2 push 1 stack size 8 
                il1.Emit(OpCodes.Sub);         // pop 2 push 1 stack size 7
                il1.Emit(OpCodes.Mul);         // pop 2 push 1 stack size 6
                il1.Emit(OpCodes.Add);         // pop 2 push 1 stack size 5
                il1.Emit(OpCodes.Add);         // pop 2 push 1 stack size 4
                il1.Emit(OpCodes.Mul);         // pop 2 push 1 stack size 3
                il1.Emit(OpCodes.Add);         // pop 2 push 1 stack size 2
                il1.Emit(OpCodes.Add);         // pop 2 push 1 stack size 1
                il1.Emit(OpCodes.Ret);         // pop 1 stack size 0

                MethodBuilder method2 = type.DefineMethod("Method2", MethodAttributes.Public, typeof(int), new Type[] { typeof(int), typeof(byte) });
                ILGenerator il2 = method2.GetILGenerator();

                // int Method2(int x, int y) =>  x + (y + 18);
                il2.Emit(OpCodes.Ldarg_1);     // push 1 MaxStack 1
                il2.Emit(OpCodes.Ldarg_2);     // push 1 MaxStack 2
                il2.Emit(OpCodes.Ldc_I4_S, 8); // push 1 MaxStack 3
                il2.Emit(OpCodes.Add);         // pop 2 push 1 stack size 2
                il2.Emit(OpCodes.Add);         // pop 2 push 1 stack size 1
                il2.Emit(OpCodes.Ret);         // pop 1 stack size 0

                saveMethod.Invoke(ab, new object[] { file.Path });

                MethodInfo getMaxStackSizeMethod = LoadILGenerator_GetMaxStackSizeMethod();
                Assert.Equal(9, getMaxStackSizeMethod.Invoke(il1, new object[0]));
                Assert.Equal(3, getMaxStackSizeMethod.Invoke(il2, new object[0]));

                Assembly assemblyFromDisk = AssemblySaveTools.LoadAssemblyFromPath(file.Path);
                Type typeFromDisk = assemblyFromDisk.Modules.First().GetType("MyType");
                MethodBody body1 = typeFromDisk.GetMethod("Method1").GetMethodBody();
                MethodBody body2 = typeFromDisk.GetMethod("Method2").GetMethodBody();
                Assert.Equal(9, body1.MaxStackSize);
                Assert.Equal(8, body2.MaxStackSize); // apparently doesn't write lower than 8 
            }
        }

        private MethodInfo LoadILGenerator_GetMaxStackSizeMethod()
        {
            Type ilgType = Type.GetType("System.Reflection.Emit.ILGeneratorImpl, System.Reflection.Emit", throwOnError: true)!;
            return ilgType.GetMethod("GetMaxStackSize", BindingFlags.NonPublic | BindingFlags.Instance, Type.EmptyTypes);
        }
    }
}
