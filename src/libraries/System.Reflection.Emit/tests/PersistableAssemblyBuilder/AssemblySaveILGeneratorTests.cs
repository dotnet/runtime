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

        [Fact]
        public void Label_ConditionalBranching()
        {
            using (TempFile file = TempFile.Create())
            {
                AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilderTypeBuilderAndSaveMethod(out TypeBuilder type, out MethodInfo saveMethod);
                MethodBuilder methodBuilder = type.DefineMethod("Method1", MethodAttributes.Public, typeof(int), new[] { typeof(int), typeof(int) });
                ILGenerator il = methodBuilder.GetILGenerator();
                Label failed = il.DefineLabel();
                Label endOfMethod = il.DefineLabel();

                // public int Method1(int P_0, int P_1) => (P_0 > 100 || P_1 > 100) ? (-1) : (P_0 + P_1);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Ldc_I4_S, 100);
                il.Emit(OpCodes.Bgt_S, failed);

                il.Emit(OpCodes.Ldarg_2);
                il.Emit(OpCodes.Ldc_I4_S, 100);
                il.Emit(OpCodes.Bgt_S, failed);

                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Ldarg_2);
                il.Emit(OpCodes.Add);
                il.Emit(OpCodes.Br_S, endOfMethod);

                il.MarkLabel(failed);
                il.Emit(OpCodes.Ldc_I4_M1);
                il.MarkLabel(endOfMethod);
                il.Emit(OpCodes.Ret);

                saveMethod.Invoke(ab, new object[] { file.Path });

                MethodInfo getMaxStackSizeMethod = LoadILGenerator_GetMaxStackSizeMethod();
                Assert.Equal(2, getMaxStackSizeMethod.Invoke(il, new object[0]));

                Assembly assemblyFromDisk = AssemblySaveTools.LoadAssemblyFromPath(file.Path);
                Type typeFromDisk = assemblyFromDisk.Modules.First().GetType("MyType");
                byte[]? bodyBytes = typeFromDisk.GetMethod("Method1").GetMethodBody().GetILAsByteArray();
                Assert.Equal(
                [
                    (byte)OpCodes.Ldarg_1.Value, (byte)OpCodes.Ldc_I4_S.Value, 100, 0, 0, 0,
                    (byte)OpCodes.Bgt_S.Value, 13,
                    (byte)OpCodes.Ldarg_2.Value, (byte)OpCodes.Ldc_I4_S.Value, 100, 0, 0, 0,
                    (byte)OpCodes.Bgt_S.Value, 5,
                    (byte)OpCodes.Ldarg_1.Value, (byte)OpCodes.Ldarg_2.Value, (byte)OpCodes.Add.Value,
                    (byte)OpCodes.Br_S.Value, (byte)OpCodes.Break.Value,
                    (byte)OpCodes.Ldc_I4_M1.Value, (byte)OpCodes.Ret.Value
                ], bodyBytes);
            }
        }

        [Fact]
        public void Label_SwitchCase()
        {
            using (TempFile file = TempFile.Create())
            {
                AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilderTypeBuilderAndSaveMethod(out TypeBuilder type, out MethodInfo saveMethod);
                MethodBuilder methodBuilder = type.DefineMethod("Method1", MethodAttributes.Public, typeof(string), new[] { typeof(int) });
                ILGenerator il = methodBuilder.GetILGenerator();
                Label defaultCase = il.DefineLabel();
                Label endOfMethod = il.DefineLabel();
                Label[] jumpTable = [il.DefineLabel(), il.DefineLabel(), il.DefineLabel(), il.DefineLabel(), il.DefineLabel()];

                // public string Method1(int P_0) => P_0 switch ...
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Switch, jumpTable);

                // Branch on default case
                il.Emit(OpCodes.Br_S, defaultCase);

                // Case P_0 = 0
                il.MarkLabel(jumpTable[0]);
                il.Emit(OpCodes.Ldstr, "no bananas");
                il.Emit(OpCodes.Br_S, endOfMethod);

                // Case P_0 = 1
                il.MarkLabel(jumpTable[1]);
                il.Emit(OpCodes.Ldstr, "one banana");
                il.Emit(OpCodes.Br_S, endOfMethod);

                // Case P_0 = 2
                il.MarkLabel(jumpTable[2]);
                il.Emit(OpCodes.Ldstr, "two bananas");
                il.Emit(OpCodes.Br_S, endOfMethod);

                // Case P_0 = 3
                il.MarkLabel(jumpTable[3]);
                il.Emit(OpCodes.Ldstr, "three bananas");
                il.Emit(OpCodes.Br_S, endOfMethod);

                // Case P_0 = 4
                il.MarkLabel(jumpTable[4]);
                il.Emit(OpCodes.Ldstr, "four bananas");
                il.Emit(OpCodes.Br_S, endOfMethod);

                // Default case
                il.MarkLabel(defaultCase);
                il.Emit(OpCodes.Ldstr, "many bananas");
                il.MarkLabel(endOfMethod);
                il.Emit(OpCodes.Ret);

                saveMethod.Invoke(ab, new object[] { file.Path });

                MethodInfo getMaxStackSizeMethod = LoadILGenerator_GetMaxStackSizeMethod();
                Assert.Equal(6, getMaxStackSizeMethod.Invoke(il, new object[0]));

                Assembly assemblyFromDisk = AssemblySaveTools.LoadAssemblyFromPath(file.Path);
                Type typeFromDisk = assemblyFromDisk.Modules.First().GetType("MyType");
                byte[]? bodyBytes = typeFromDisk.GetMethod("Method1").GetMethodBody().GetILAsByteArray();
                Assert.Equal((byte)OpCodes.Ldarg_1.Value, bodyBytes[0]);
                Assert.Equal((byte)OpCodes.Switch.Value, bodyBytes[1]);
                Assert.Equal(5, bodyBytes[2]); // case count
                Assert.Equal(69, bodyBytes.Length);
            }
        }

        [Fact]
        public void LocalBuilderMultipleLocalsUsage()
        {
            using (TempFile file = TempFile.Create())
            {
                AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilderTypeBuilderAndSaveMethod(out TypeBuilder type, out MethodInfo saveMethod);
                MethodBuilder methodBuilder = type.DefineMethod("Method1", MethodAttributes.Public | MethodAttributes.Static, typeof(int), new[] { typeof(int), typeof(string) });
                ILGenerator il = methodBuilder.GetILGenerator();
                LocalBuilder intLocal = il.DeclareLocal(typeof(int));
                LocalBuilder stringLocal = il.DeclareLocal(typeof(string));
                LocalBuilder shortLocal = il.DeclareLocal(typeof(short), pinned: true); ;
                LocalBuilder int2Local = il.DeclareLocal(typeof(int), pinned: false);
                il.Emit(OpCodes.Ldarg, 1);
                il.Emit(OpCodes.Stloc_1);
                il.Emit(OpCodes.Ldstr, "string value");
                il.Emit(OpCodes.Stloc, stringLocal);
                il.Emit(OpCodes.Ldloc, stringLocal);
                il.Emit(OpCodes.Starg, 1);
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Stloc_0);
                il.Emit(OpCodes.Ldc_I4_S, 120);
                il.Emit(OpCodes.Stloc, 2);
                il.Emit(OpCodes.Ldloc, shortLocal);
                il.Emit(OpCodes.Ldloc, 0);
                il.Emit(OpCodes.Add);
                il.Emit(OpCodes.Stloc, intLocal);
                il.Emit(OpCodes.Ldloca, intLocal);
                il.Emit(OpCodes.Ldind_I);
                il.Emit(OpCodes.Stloc, int2Local);
                il.Emit(OpCodes.Ldloc_3);
                il.Emit(OpCodes.Ret);

                MethodInfo getMaxStackSizeMethod = LoadILGenerator_GetMaxStackSizeMethod();
                Assert.Equal(2, getMaxStackSizeMethod.Invoke(il, new object[0]));
                saveMethod.Invoke(ab, new object[] { file.Path });

                Assembly assemblyFromDisk = AssemblySaveTools.LoadAssemblyFromPath(file.Path);
                Type typeFromDisk = assemblyFromDisk.Modules.First().GetType("MyType");
                MethodBody body = typeFromDisk.GetMethod("Method1").GetMethodBody();
                Assert.Equal(4, body.LocalVariables.Count);
                Assert.Equal(intLocal.LocalIndex, body.LocalVariables[0].LocalIndex);
                Assert.Equal(intLocal.LocalType.FullName, body.LocalVariables[0].LocalType.FullName);
                Assert.Equal(intLocal.IsPinned, body.LocalVariables[0].IsPinned);
                Assert.Equal(stringLocal.LocalIndex, body.LocalVariables[1].LocalIndex);
                Assert.Equal(stringLocal.LocalType.FullName, body.LocalVariables[1].LocalType.FullName);
                Assert.Equal(stringLocal.IsPinned, body.LocalVariables[1].IsPinned);
                Assert.Equal(shortLocal.LocalIndex, body.LocalVariables[2].LocalIndex);
                Assert.Equal(shortLocal.LocalType.FullName, body.LocalVariables[2].LocalType.FullName);
                Assert.Equal(shortLocal.IsPinned, body.LocalVariables[2].IsPinned);
                Assert.Equal(int2Local.LocalIndex, body.LocalVariables[3].LocalIndex);
                Assert.Equal(int2Local.LocalType.FullName, body.LocalVariables[3].LocalType.FullName);
                Assert.Equal(int2Local.IsPinned, body.LocalVariables[3].IsPinned);
                byte[]? bodyBytes = body.GetILAsByteArray();
                Assert.Equal((byte)OpCodes.Ldarg_1.Value, bodyBytes[0]);
                Assert.Equal((byte)OpCodes.Stloc_1.Value, bodyBytes[1]);
                Assert.Equal((byte)OpCodes.Ldstr.Value, bodyBytes[2]);
                Assert.Equal((byte)OpCodes.Stloc_1.Value, bodyBytes[7]);
                Assert.Equal((byte)OpCodes.Ldloc_1.Value, bodyBytes[8]);
                Assert.Equal((byte)OpCodes.Starg_S.Value, bodyBytes[9]);
                Assert.Equal((byte)OpCodes.Ldarg_0.Value, bodyBytes[11]);
                Assert.Equal((byte)OpCodes.Stloc_0.Value, bodyBytes[12]);
                Assert.Equal((byte)OpCodes.Ldc_I4_S.Value, bodyBytes[13]);
                Assert.Equal(120, BitConverter.ToInt32(bodyBytes.AsSpan().Slice(14, 4)));
                Assert.Equal(0xFE, bodyBytes[18]); // Stloc instruction occupies 2 bytes 0xfe0e
                Assert.Equal(0x0E, bodyBytes[19]);
                Assert.Equal(2, BitConverter.ToInt32(bodyBytes.AsSpan().Slice(20, 4))); // index 2 of 'il.Emit(OpCodes.Stloc, 2);' instruction
                Assert.Equal((byte)OpCodes.Ldloc_2.Value, bodyBytes[24]);
                Assert.Equal(0xFE, bodyBytes[25]); // Ldloc = 0xfe0c
                Assert.Equal(0x0C, bodyBytes[26]);
                Assert.Equal(0, BitConverter.ToInt32(bodyBytes.AsSpan().Slice(27, 4))); // index 0 of 'il.Emit(OpCodes.Ldloc, 0);' instruction
                Assert.Equal((byte)OpCodes.Add.Value, bodyBytes[31]);
                Assert.Equal((byte)OpCodes.Stloc_0.Value, bodyBytes[32]);
                Assert.Equal((byte)OpCodes.Ldloca_S.Value, bodyBytes[33]);
                Assert.Equal(0, bodyBytes[34]); // intLocal index is 0 for 'il.Emit(OpCodes.Ldloca, intLocal);' instruction
                Assert.Equal((byte)OpCodes.Ldind_I.Value, bodyBytes[35]);
                Assert.Equal((byte)OpCodes.Stloc_3.Value, bodyBytes[36]);
                Assert.Equal((byte)OpCodes.Ldloc_3.Value, bodyBytes[37]);
                Assert.Equal(OpCodes.Ret.Value, bodyBytes[38]);
            }
        }

        [Fact]
        public void LocalBuilderMultipleTypesWithMultipleMethodsWithLocals()
        {
            using (TempFile file = TempFile.Create())
            {
                AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilderTypeBuilderAndSaveMethod(out TypeBuilder type, out MethodInfo saveMethod);
                MethodBuilder methodBuilder = type.DefineMethod("Method1", MethodAttributes.Public | MethodAttributes.Static, typeof(string), new[] { typeof(int), typeof(string) });
                ILGenerator il = methodBuilder.GetILGenerator();
                LocalBuilder intLocal = il.DeclareLocal(typeof(int));
                LocalBuilder stringLocal = il.DeclareLocal(typeof(string));
                LocalBuilder shortLocal = il.DeclareLocal(typeof(short));
                il.Emit(OpCodes.Ldstr, "string value");
                il.Emit(OpCodes.Stloc, stringLocal);
                il.Emit(OpCodes.Ldloc, stringLocal);
                il.Emit(OpCodes.Starg, 1);
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Stloc_0);
                il.Emit(OpCodes.Ldc_I4_S, 120);
                il.Emit(OpCodes.Stloc, 2);
                il.Emit(OpCodes.Ldloc, shortLocal);
                il.Emit(OpCodes.Ldloc, 0);
                il.Emit(OpCodes.Add);
                il.Emit(OpCodes.Stloc, intLocal);
                il.Emit(OpCodes.Ldloc, stringLocal);
                il.Emit(OpCodes.Ret);
                MethodBuilder multiplyMethod = type.DefineMethod("MultiplyMethod", MethodAttributes.Public, typeof(int), new[] { typeof(int) });
                ILGenerator multiplyMethodIL = multiplyMethod.GetILGenerator();
                LocalBuilder iLocal = multiplyMethodIL.DeclareLocal(typeof(int));
                LocalBuilder shLocal = multiplyMethodIL.DeclareLocal(typeof(short));
                multiplyMethodIL.Emit(OpCodes.Ldarg, 1);
                multiplyMethodIL.Emit(OpCodes.Stloc, iLocal);
                multiplyMethodIL.Emit(OpCodes.Ldloc, iLocal);
                multiplyMethodIL.Emit(OpCodes.Ldc_I4_S, 11);
                multiplyMethodIL.Emit(OpCodes.Stloc, shLocal);
                multiplyMethodIL.Emit(OpCodes.Ldloc, shLocal);
                multiplyMethodIL.Emit(OpCodes.Mul);
                multiplyMethodIL.Emit(OpCodes.Stloc, iLocal);
                multiplyMethodIL.Emit(OpCodes.Ldloc, iLocal);
                multiplyMethodIL.Emit(OpCodes.Ret);

                TypeBuilder anotherType = ab.GetDynamicModule("MyModule").DefineType("AnotherType", TypeAttributes.NotPublic, type);
                MethodBuilder stringMethod = anotherType.DefineMethod("StringMethod", MethodAttributes.FamORAssem, typeof(string), Type.EmptyTypes);
                ILGenerator stringMethodIL = stringMethod.GetILGenerator();
                LocalBuilder strLocal = stringMethodIL.DeclareLocal(typeof(string));
                stringMethodIL.Emit(OpCodes.Ldstr, "Hello world!");
                stringMethodIL.Emit(OpCodes.Stloc, strLocal);
                stringMethodIL.Emit(OpCodes.Ldloc, strLocal);
                stringMethodIL.Emit(OpCodes.Ret);
                MethodBuilder typeMethod = anotherType.DefineMethod("TypeMethod", MethodAttributes.Family, type, new[] { anotherType, type });
                ILGenerator typeMethodIL = typeMethod.GetILGenerator();
                typeMethodIL.Emit(OpCodes.Ldarg, 1);
                LocalBuilder typeLocal = typeMethodIL.DeclareLocal(type);
                LocalBuilder anotherTypeLocal = typeMethodIL.DeclareLocal(anotherType);
                typeMethodIL.Emit(OpCodes.Stloc, anotherTypeLocal);
                typeMethodIL.Emit(OpCodes.Ldloc, anotherTypeLocal);
                typeMethodIL.Emit(OpCodes.Stloc, typeLocal);
                typeMethodIL.Emit(OpCodes.Ldloc, typeLocal);
                typeMethodIL.Emit(OpCodes.Ret);
                MethodBuilder longMethod = anotherType.DefineMethod("LongMethod", MethodAttributes.Static, typeof(long), Type.EmptyTypes);
                ILGenerator longMethodIL = longMethod.GetILGenerator();
                longMethodIL.Emit(OpCodes.Ldc_I8, 1234567L);
                LocalBuilder longLocal = longMethodIL.DeclareLocal(typeof(long));
                LocalBuilder shiftLocal = longMethodIL.DeclareLocal(typeof(int));
                longMethodIL.Emit(OpCodes.Stloc, longLocal);
                longMethodIL.Emit(OpCodes.Ldc_I4_5);
                longMethodIL.Emit(OpCodes.Stloc, shiftLocal);
                longMethodIL.Emit(OpCodes.Ldloc, longLocal);
                longMethodIL.Emit(OpCodes.Ldloc, shiftLocal);
                longMethodIL.Emit(OpCodes.Shl);
                longMethodIL.Emit(OpCodes.Stloc, longLocal);
                longMethodIL.Emit(OpCodes.Ldloc, longLocal);
                longMethodIL.Emit(OpCodes.Ret);

                saveMethod.Invoke(ab, new object[] { file.Path });

                Assembly assemblyFromDisk = AssemblySaveTools.LoadAssemblyFromPath(file.Path);
                Module moduleFromFile = assemblyFromDisk.Modules.First();
                Type typeFromDisk = moduleFromFile.GetType("MyType");
                Assert.Equal(2, typeFromDisk.GetMethod("MultiplyMethod").GetMethodBody().LocalVariables.Count);
                Assert.Equal(3, typeFromDisk.GetMethod("Method1").GetMethodBody().LocalVariables.Count);
                Type anotherTypeFromDisk = moduleFromFile.GetType("AnotherType");
                Assert.Equal(1, anotherTypeFromDisk.GetMethod("StringMethod", BindingFlags.NonPublic | BindingFlags.Instance).GetMethodBody().LocalVariables.Count);
                Assert.Equal(2, anotherTypeFromDisk.GetMethod("TypeMethod", BindingFlags.NonPublic | BindingFlags.Instance).GetMethodBody().LocalVariables.Count);
                Assert.Equal(2, anotherTypeFromDisk.GetMethod("LongMethod", BindingFlags.NonPublic | BindingFlags.Static).GetMethodBody().LocalVariables.Count);
            }
        }

        [Fact]
        public void LocalBuilderExceptions()
        {
            AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilderTypeBuilderAndSaveMethod(out TypeBuilder type, out MethodInfo saveMethod);
            ILGenerator il = type.DefineMethod("Method1", MethodAttributes.Public).GetILGenerator();
            ILGenerator anotherIL = type.DefineMethod("AnotherMethod", MethodAttributes.Public).GetILGenerator();
            LocalBuilder stringLocal = il.DeclareLocal(typeof(string));
            LocalBuilder nullBuilder = null;

            Assert.Throws<ArgumentNullException>(() => il.DeclareLocal(null!));
            Assert.Throws<ArgumentNullException>(() => il.Emit(OpCodes.Ldloc, nullBuilder));
            Assert.Throws<ArgumentException>(() => anotherIL.Emit(OpCodes.Ldloc, stringLocal));
        }
    }
}
