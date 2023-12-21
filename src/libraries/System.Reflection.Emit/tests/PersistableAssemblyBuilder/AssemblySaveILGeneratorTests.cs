// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Buffers.Binary;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestPlatform.TestHost;
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
                type.CreateType();
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
                type.CreateType();
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
                type.CreateType();
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
                type.CreateType();
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
                anotherType.CreateType();
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
            MethodBuilder method = type.DefineMethod("Method1", MethodAttributes.Public | MethodAttributes.Static, typeof(Type), Type.EmptyTypes);
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
                MethodBuilder method1 = type.DefineMethod("Method1", MethodAttributes.Public, typeof(long), new [] { typeof(int), typeof(long), typeof(short), typeof(byte) });
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
                type.CreateType();
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
                type.CreateType();
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
                type.CreateType();
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
                type.CreateType();
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
                Assert.Equal(120, BinaryPrimitives.ReadInt32LittleEndian(bodyBytes.AsSpan().Slice(14, 4)));
                Assert.Equal(0xFE, bodyBytes[18]); // Stloc instruction occupies 2 bytes 0xfe0e
                Assert.Equal(0x0E, bodyBytes[19]);
                Assert.Equal(2, BinaryPrimitives.ReadInt32LittleEndian(bodyBytes.AsSpan().Slice(20, 4))); // index 2 of 'il2.Emit(OpCodes.Stloc, 2);' instruction
                Assert.Equal((byte)OpCodes.Ldloc_2.Value, bodyBytes[24]);
                Assert.Equal(0xFE, bodyBytes[25]); // Ldloc = 0xfe0c
                Assert.Equal(0x0C, bodyBytes[26]);
                Assert.Equal(0, BitConverter.ToInt32(bodyBytes.AsSpan().Slice(27, 4))); // index 0 of 'il2.Emit(OpCodes.Ldloc, 0);' instruction
                Assert.Equal((byte)OpCodes.Add.Value, bodyBytes[31]);
                Assert.Equal((byte)OpCodes.Stloc_0.Value, bodyBytes[32]);
                Assert.Equal((byte)OpCodes.Ldloca_S.Value, bodyBytes[33]);
                Assert.Equal(0, bodyBytes[34]); // intLocal index is 0 for 'il2.Emit(OpCodes.Ldloca, intLocal);' instruction
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
                type.CreateType();
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
                anotherType.CreateType();
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

        [Fact]
        public void ReferenceFieldInIL()
        {
            using (TempFile file = TempFile.Create())
            {
                AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilderTypeBuilderAndSaveMethod(out TypeBuilder tb, out MethodInfo saveMethod);
                MethodBuilder methodBuilder = tb.DefineMethod("Method1", MethodAttributes.Public, typeof(int), new[] { typeof(int) });
                FieldBuilder fbNumber = tb.DefineField("_number", typeof(int), FieldAttributes.Private);
                Assert.Equal(0, fbNumber.MetadataToken);

                ILGenerator il = methodBuilder.GetILGenerator();
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldfld, fbNumber);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Mul);
                il.Emit(OpCodes.Ret);
                tb.CreateType();
                saveMethod.Invoke(ab, new object[] { file.Path });

                Assembly assemblyFromDisk = AssemblySaveTools.LoadAssemblyFromPath(file.Path);
                Type typeFromDisk = assemblyFromDisk.Modules.First().GetType("MyType");
                byte[]? bodyBytes = typeFromDisk.GetMethod("Method1").GetMethodBody().GetILAsByteArray();
                Assert.Equal(9, bodyBytes.Length);
                Assert.NotEqual(0, fbNumber.MetadataToken);
                Assert.Equal(OpCodes.Ldarg_0.Value, bodyBytes[0]);
                Assert.Equal(OpCodes.Ldfld.Value, bodyBytes[1]);
                Assert.Equal(fbNumber.MetadataToken, BinaryPrimitives.ReadInt32LittleEndian(bodyBytes.AsSpan().Slice(2, 4)));
                Assert.Equal(OpCodes.Ldarg_1.Value, bodyBytes[6]);
                Assert.Equal(OpCodes.Mul.Value, bodyBytes[7]);
                Assert.Equal(OpCodes.Ret.Value, bodyBytes[8]);
            }
        }

        [Fact]
        public void ReferenceFieldAndMethodsInIL()
        {
            using (TempFile file = TempFile.Create())
            {
                AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilderTypeBuilderAndSaveMethod(out TypeBuilder tb, out MethodInfo saveMethod);
                MethodBuilder methodMain = tb.DefineMethod("Main", MethodAttributes.Public, typeof(void), new[] { typeof(int) });
                FieldBuilder field = tb.DefineField("_field", typeof(int), FieldAttributes.Private);
                MethodInfo writeLineString = typeof(Console).GetMethod("WriteLine", new[] { typeof(string) });
                MethodInfo writeLineObj = typeof(Console).GetMethod("WriteLine", new[] { typeof(string), typeof(object), typeof(object), typeof(object) });
                MethodBuilder methodMultiply = tb.DefineMethod("Multiply", MethodAttributes.Public, typeof(int), new[] { typeof(int) });
                /*
                class MyType
                { 
                    private int _field;
                    int Multiply(int value) => _field * value;
                    void Main(int a)
                    {
                        Console.WriteLine("Displaying the expression:");
                        Console.WriteLine("{0} * {1} = {2}", _field, a, Multiply(a));
                    }
                }
                */
                ILGenerator il = methodMultiply.GetILGenerator();
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldfld, field);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Mul);
                il.Emit(OpCodes.Ret);

                ILGenerator ilMain = methodMain.GetILGenerator();
                ilMain.Emit(OpCodes.Ldstr, "Displaying the expression:");
                ilMain.Emit(OpCodes.Call, writeLineString);
                ilMain.Emit(OpCodes.Ldstr, "{0} * {1} = {2}");
                ilMain.Emit(OpCodes.Ldarg_0);
                ilMain.Emit(OpCodes.Ldfld, field);
                ilMain.Emit(OpCodes.Box, typeof(int));
                ilMain.Emit(OpCodes.Ldarg_1);
                ilMain.Emit(OpCodes.Box, typeof(int));
                ilMain.Emit(OpCodes.Ldarg_0);
                ilMain.Emit(OpCodes.Ldarg_1);
                ilMain.Emit(OpCodes.Call, methodMultiply);
                ilMain.Emit(OpCodes.Box, typeof(int));
                ilMain.Emit(OpCodes.Call, writeLineObj);
                ilMain.Emit(OpCodes.Ret);
                tb.CreateType();
                saveMethod.Invoke(ab, new object[] { file.Path });

                Assembly assemblyFromDisk = AssemblySaveTools.LoadAssemblyFromPath(file.Path);
                Type typeFromDisk = assemblyFromDisk.Modules.First().GetType("MyType");
                byte[]? bodyBytes = typeFromDisk.GetMethod("Main").GetMethodBody().GetILAsByteArray();
                Assert.Equal(OpCodes.Ldstr.Value, bodyBytes[0]);
                Assert.Equal(OpCodes.Call.Value, bodyBytes[5]);
                // Bytes 6, 7, 8, 9 are token for writeLineString, but it is not same as the value before save
                Assert.Equal(OpCodes.Ldstr.Value, bodyBytes[10]);
                Assert.Equal(OpCodes.Ldarg_0.Value, bodyBytes[15]);
                Assert.Equal(OpCodes.Ldfld.Value, bodyBytes[16]);
                Assert.Equal(field.MetadataToken, BinaryPrimitives.ReadInt32LittleEndian(bodyBytes.AsSpan().Slice(17, 4)));
                Assert.Equal(OpCodes.Box.Value, bodyBytes[21]);
                int intTypeToken = BinaryPrimitives.ReadInt32LittleEndian(bodyBytes.AsSpan().Slice(22, 4));
                Assert.Equal(OpCodes.Ldarg_1.Value, bodyBytes[26]);
                Assert.Equal(OpCodes.Box.Value, bodyBytes[27]);
                Assert.Equal(intTypeToken, BitConverter.ToInt32(bodyBytes.AsSpan().Slice(28, 4)));
                Assert.Equal(OpCodes.Ldarg_0.Value, bodyBytes[32]);
                Assert.Equal(OpCodes.Ldarg_1.Value, bodyBytes[33]);
                Assert.Equal(OpCodes.Call.Value, bodyBytes[34]);
                Assert.Equal(methodMultiply.MetadataToken, BinaryPrimitives.ReadInt32LittleEndian(bodyBytes.AsSpan().Slice(35, 4)));
                Assert.Equal(OpCodes.Box.Value, bodyBytes[39]);
                Assert.Equal(intTypeToken, BitConverter.ToInt32(bodyBytes.AsSpan().Slice(40, 4)));
                Assert.Equal(OpCodes.Call.Value, bodyBytes[44]);
                // Bytes 24, 46, 47, 48 are token for writeLineObj, but it is not same as the value before save
                Assert.Equal(OpCodes.Ret.Value, bodyBytes[49]);
            }
        }

        [Fact]
        public void ReferenceConstructedGenericMethod()
        {
            using (TempFile file = TempFile.Create())
            {
                AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilderTypeBuilderAndSaveMethod(out TypeBuilder type, out MethodInfo saveMethod);
                ConstructorBuilder ctor = type.DefineDefaultConstructor(MethodAttributes.Public);
                MethodBuilder genericMethod = type.DefineMethod("GM", MethodAttributes.Public | MethodAttributes.Static);
                GenericTypeParameterBuilder[] methodParams = genericMethod.DefineGenericParameters("U");
                genericMethod.SetSignature(null, null, null, new[] { methodParams[0] }, null, null);
                ILGenerator ilg = genericMethod.GetILGenerator();
                MethodInfo writeLineObj = typeof(Console).GetMethod("WriteLine", new[] { typeof(object) });
                ilg.Emit(OpCodes.Ldarg_0);
                ilg.EmitCall(OpCodes.Call, writeLineObj, null);
                ilg.Emit(OpCodes.Ret);
                MethodBuilder mainMethod = type.DefineMethod("Main", MethodAttributes.Public | MethodAttributes.Static);
                ilg = mainMethod.GetILGenerator();
                MethodInfo GMOfString = genericMethod.MakeGenericMethod(typeof(string));
                ilg.Emit(OpCodes.Ldstr, "Hello, world!");
                ilg.EmitCall(OpCodes.Call, GMOfString, null);
                ilg.Emit(OpCodes.Ret);
                type.CreateType();
                saveMethod.Invoke(ab, new[] { file.Path });

                Assembly assemblyFromDisk = AssemblySaveTools.LoadAssemblyFromPath(file.Path);
                Type typeFromDisk = assemblyFromDisk.Modules.First().GetType("MyType");
                MethodInfo genericMethodFromDisk = typeFromDisk.GetMethod("GM");
                Assert.True(genericMethodFromDisk.IsGenericMethod);
                Assert.True(genericMethodFromDisk.IsGenericMethodDefinition);
                byte[] ilBytes = typeFromDisk.GetMethod("Main").GetMethodBody().GetILAsByteArray();
                Assert.Equal(OpCodes.Ldstr.Value, ilBytes[0]);
                Assert.Equal(OpCodes.Call.Value, ilBytes[5]);
                Assert.Equal(OpCodes.Ret.Value, ilBytes[10]);
            }
        }

        [Fact]
        public void ReferenceConstructedGenericMethodFieldOfConstructedType()
        {
            using (TempFile file = TempFile.Create())
            {
                AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilderTypeBuilderAndSaveMethod(out TypeBuilder type, out MethodInfo saveMethod);
                GenericTypeParameterBuilder[] typeParams = type.DefineGenericParameters(new[] { "T" });
                ConstructorBuilder ctor = type.DefineDefaultConstructor(MethodAttributes.PrivateScope | MethodAttributes.Public |
                    MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);
                FieldBuilder myField = type.DefineField("Field", typeParams[0], FieldAttributes.Public);
                MethodBuilder genericMethod = type.DefineMethod("GM", MethodAttributes.Public | MethodAttributes.Static);
                GenericTypeParameterBuilder[] methodParams = genericMethod.DefineGenericParameters("U");
                genericMethod.SetSignature(null, null, null, new [] { methodParams[0] }, null, null);
                ILGenerator ilg = genericMethod.GetILGenerator();
                Type SampleOfU = type.MakeGenericType(methodParams[0]);
                ilg.DeclareLocal(SampleOfU);
                ConstructorInfo ctorOfU = TypeBuilder.GetConstructor(SampleOfU, ctor);
                ilg.Emit(OpCodes.Newobj, ctorOfU);
                ilg.Emit(OpCodes.Stloc_0);
                ilg.Emit(OpCodes.Ldloc_0);
                ilg.Emit(OpCodes.Ldarg_0);
                FieldInfo FieldOfU = TypeBuilder.GetField(SampleOfU, myField);
                ilg.Emit(OpCodes.Stfld, FieldOfU);
                ilg.Emit(OpCodes.Ldloc_0);
                ilg.Emit(OpCodes.Ldfld, FieldOfU);
                ilg.Emit(OpCodes.Box, methodParams[0]);
                MethodInfo writeLineObj = typeof(Console).GetMethod("WriteLine", new[] { typeof(object) });
                ilg.EmitCall(OpCodes.Call, writeLineObj, null);
                ilg.Emit(OpCodes.Ret);
                type.CreateType();
                TypeBuilder dummy = ab.GetDynamicModule("MyModule").DefineType("Dummy", TypeAttributes.Class | TypeAttributes.NotPublic);
                MethodBuilder mainMethod = dummy.DefineMethod("Main", MethodAttributes.Public | MethodAttributes.Static);
                ilg = mainMethod.GetILGenerator();
                Type SampleOfInt = type.MakeGenericType(typeof(string));
                MethodInfo SampleOfIntGM = TypeBuilder.GetMethod(SampleOfInt, genericMethod);
                MethodInfo GMOfString = SampleOfIntGM.MakeGenericMethod(typeof(string));
                ilg.Emit(OpCodes.Ldstr, "Hello, world!");
                ilg.EmitCall(OpCodes.Call, GMOfString, null);
                ilg.Emit(OpCodes.Ret);
                dummy.CreateType();
/* Generated IL would like this in C#:
public class MyType<T>
{
	public T Field;

	public static void GM<U>(U P_0)
	{
		MyType<U> myType = new MyType<U>();
		myType.Field = P_0;
		Console.WriteLine(myType.Field);
	}
}

internal class Dummy
{
	public static void Main()
	{
		MyType<string>.GM("HelloWorld");
	}
}               */
                saveMethod.Invoke(ab, new[] { file.Path });

                Module module = AssemblySaveTools.LoadAssemblyFromPath(file.Path).Modules.First();
                Type myTypeFromDisk = module.GetType("MyType");
                Assert.True(myTypeFromDisk.IsGenericType);
                Assert.True(myTypeFromDisk.IsGenericTypeDefinition);
                Assert.Equal("T", myTypeFromDisk.GetGenericArguments()[0].Name);
                Assert.Equal("T", myTypeFromDisk.GetField("Field").FieldType.Name);
                MethodInfo genericMethodFromDisk = myTypeFromDisk.GetMethod("GM");
                Assert.True(genericMethodFromDisk.IsGenericMethod);
                Assert.True(genericMethodFromDisk.IsGenericMethodDefinition);
                Assert.Equal(1, genericMethodFromDisk.GetMethodBody().LocalVariables.Count);
                Assert.Equal("MyType[U]", genericMethodFromDisk.GetMethodBody().LocalVariables[0].LocalType.ToString());
                byte[] gmIlBytes = genericMethodFromDisk.GetMethodBody().GetILAsByteArray();
                Assert.Equal(OpCodes.Newobj.Value, gmIlBytes[0]);
                Assert.Equal(OpCodes.Stloc_0.Value, gmIlBytes[5]);
                Assert.Equal(OpCodes.Ldloc_0.Value, gmIlBytes[6]);
                Assert.Equal(OpCodes.Ldarg_0.Value, gmIlBytes[7]);
                Assert.Equal(OpCodes.Stfld.Value, gmIlBytes[8]);
                Assert.Equal(OpCodes.Ldloc_0.Value, gmIlBytes[13]);
                Assert.Equal(OpCodes.Ldfld.Value, gmIlBytes[14]);
                Assert.Equal(OpCodes.Box.Value, gmIlBytes[19]);
                Assert.Equal(OpCodes.Call.Value, gmIlBytes[24]);
                Assert.Equal(OpCodes.Ret.Value, gmIlBytes[29]);
                byte[] ilBytes = module.GetType("Dummy").GetMethod("Main").GetMethodBody().GetILAsByteArray();
                Assert.Equal(OpCodes.Ldstr.Value, ilBytes[0]);
                Assert.Equal(OpCodes.Call.Value, ilBytes[5]);
                Assert.Equal(OpCodes.Ret.Value, ilBytes[10]);
            }
        }

        [Fact]
        public void EmitWriteLineMacroTest()
        {
            using (TempFile file = TempFile.Create())
            {
                AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilderTypeBuilderAndSaveMethod(out TypeBuilder type1, out MethodInfo saveMethod);
                MethodBuilder method = type1.DefineMethod("meth", MethodAttributes.Public, typeof(int), Type.EmptyTypes);
                FieldBuilder field = type1.DefineField("field", typeof(int), FieldAttributes.Public | FieldAttributes.Static);
                ILGenerator ilGenerator = method.GetILGenerator();
                LocalBuilder local = ilGenerator.DeclareLocal(typeof(int));
                ilGenerator.Emit(OpCodes.Ldc_I4_1);
                ilGenerator.Emit(OpCodes.Stloc_0);
                ilGenerator.Emit(OpCodes.Ldloc_0);
                ilGenerator.Emit(OpCodes.Stsfld, field);
                ilGenerator.EmitWriteLine(field);
                ilGenerator.EmitWriteLine("Emit WriteLine");
                ilGenerator.EmitWriteLine(local);
                ilGenerator.Emit(OpCodes.Ldsfld, field);
                ilGenerator.Emit(OpCodes.Ret);
                type1.CreateType();
                saveMethod.Invoke(ab, new object[] { file.Path });

                Assembly assemblyFromDisk = AssemblySaveTools.LoadAssemblyFromPath(file.Path);
                Type typeFromDisk = assemblyFromDisk.Modules.First().GetType("MyType");
                byte[]? bodyBytes = typeFromDisk.GetMethod("meth").GetMethodBody().GetILAsByteArray();
                Assert.Equal(OpCodes.Ldc_I4_1.Value, bodyBytes[0]);
                Assert.Equal(OpCodes.Stloc_0.Value, bodyBytes[1]);
                Assert.Equal(OpCodes.Ldloc_0.Value, bodyBytes[2]);
                Assert.Equal(OpCodes.Stsfld.Value, bodyBytes[3]);
                Assert.Equal(field.MetadataToken, BinaryPrimitives.ReadInt32LittleEndian(bodyBytes.AsSpan().Slice(4, 4)));
                Assert.Equal(OpCodes.Call.Value, bodyBytes[8]);
                Assert.Equal(OpCodes.Ldsfld.Value, bodyBytes[13]);
                Assert.Equal(field.MetadataToken, BinaryPrimitives.ReadInt32LittleEndian(bodyBytes.AsSpan().Slice(14, 4)));
                Assert.Equal(OpCodes.Callvirt.Value, bodyBytes[18]);
                Assert.Equal(OpCodes.Ldstr.Value, bodyBytes[23]);
                Assert.Equal(OpCodes.Call.Value, bodyBytes[28]);
                Assert.Equal(OpCodes.Call.Value, bodyBytes[33]);
                Assert.Equal(OpCodes.Ldloc_0.Value, bodyBytes[38]);
                Assert.Equal(OpCodes.Callvirt.Value, bodyBytes[39]);
                Assert.Equal(OpCodes.Ldsfld.Value, bodyBytes[44]);
                Assert.Equal(field.MetadataToken, BinaryPrimitives.ReadInt32LittleEndian(bodyBytes.AsSpan().Slice(45, 4)));
                Assert.Equal(OpCodes.Ret.Value, bodyBytes[49]);
            }
        }

        [Fact]
        public void ReferenceStaticFieldAndMethodsInIL()
        {
            using (TempFile file = TempFile.Create())
            {
                AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilderTypeBuilderAndSaveMethod(out TypeBuilder tb, out MethodInfo saveMethod);
                MethodBuilder methodMain = tb.DefineMethod("Main", MethodAttributes.Public, typeof(int), new[] { typeof(int) });
                TypeBuilder anotherType = ab.GetDynamicModule("MyModule").DefineType("AnotherType", TypeAttributes.Public);
                FieldBuilder field = anotherType.DefineField("StaticField", typeof(int), FieldAttributes.Public | FieldAttributes.Static);
                MethodBuilder staticMethod = anotherType.DefineMethod("StaticMethod", MethodAttributes.Public | MethodAttributes.Static, typeof(void), Type.EmptyTypes);
                /*class MyType
                  { 
                      int Main(int a)
                      {
                          AnotherType.StaticField = a;
                          AnotherType.StaticMethod();
                          return AnotherType.StaticField;
                      }
                  }
                  public class AnotherType
                  {
                      public static int StaticField;
                      void static StaticMethod() { }
                  }*/
                ILGenerator ilMain = methodMain.GetILGenerator();
                ilMain.Emit(OpCodes.Call, staticMethod);
                ilMain.Emit(OpCodes.Ldarg_1);
                ilMain.Emit(OpCodes.Stsfld, field);
                ilMain.Emit(OpCodes.Ldsfld, field);
                ilMain.Emit(OpCodes.Ret);
                ILGenerator il = staticMethod.GetILGenerator();
                il.Emit(OpCodes.Ret);
                tb.CreateType();
                anotherType.CreateType();
                saveMethod.Invoke(ab, new object[] { file.Path });

                Assembly assemblyFromDisk = AssemblySaveTools.LoadAssemblyFromPath(file.Path);
                Type typeFromDisk = assemblyFromDisk.Modules.First().GetType("MyType");
                byte[]? bodyBytes = typeFromDisk.GetMethod("Main").GetMethodBody().GetILAsByteArray();
                Assert.Equal(OpCodes.Call.Value, bodyBytes[0]);
                Assert.Equal(staticMethod.MetadataToken, BinaryPrimitives.ReadInt32LittleEndian(bodyBytes.AsSpan().Slice(1, 4)));
                Assert.Equal(OpCodes.Ldarg_1.Value, bodyBytes[5]);
                Assert.Equal(OpCodes.Stsfld.Value, bodyBytes[6]);
                Assert.Equal(field.MetadataToken, BinaryPrimitives.ReadInt32LittleEndian(bodyBytes.AsSpan().Slice(7, 4)));
                Assert.Equal(OpCodes.Ldsfld.Value, bodyBytes[11]);
                Assert.Equal(field.MetadataToken, BinaryPrimitives.ReadInt32LittleEndian(bodyBytes.AsSpan().Slice(12, 4)));
                Assert.Equal(OpCodes.Ret.Value, bodyBytes[16]);
            }
        }

        [Fact]
        public void ReferenceConstructorInIL()
        {
            using (TempFile file = TempFile.Create())
            {
                AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilderTypeBuilderAndSaveMethod(out TypeBuilder tb, out MethodInfo saveMethod);
                MethodBuilder methodBuilder = tb.DefineMethod("Method1", MethodAttributes.Public, typeof(Version), new[] { typeof(int), typeof(int) });
                ConstructorInfo ctor = typeof(Version).GetConstructor(new[] { typeof(int), typeof(int) });

                ILGenerator il = methodBuilder.GetILGenerator();
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Ldarg_2);
                il.Emit(OpCodes.Newobj, ctor);
                il.Emit(OpCodes.Ret);
                tb.CreateType();
                saveMethod.Invoke(ab, new object[] { file.Path });

                Assembly assemblyFromDisk = AssemblySaveTools.LoadAssemblyFromPath(file.Path);
                Type typeFromDisk = assemblyFromDisk.Modules.First().GetType("MyType");
                byte[]? bodyBytes = typeFromDisk.GetMethod("Method1").GetMethodBody().GetILAsByteArray();
                Assert.Equal(OpCodes.Ldarg_1.Value, bodyBytes[0]);
                Assert.Equal(OpCodes.Ldarg_2.Value, bodyBytes[1]);
                Assert.Equal(OpCodes.Newobj.Value, bodyBytes[2]);
                Assert.Equal(OpCodes.Ret.Value, bodyBytes[7]);
            }
        }

        [Fact]
        public void ReferenceAType()
        {
            using (TempFile file = TempFile.Create())
            {
                AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilderTypeBuilderAndSaveMethod(out TypeBuilder tb, out MethodInfo saveMethod);
                MethodBuilder method = tb.DefineMethod("meth1", MethodAttributes.Public | MethodAttributes.Static, typeof(bool), Type.EmptyTypes);
                ILGenerator ilGenerator = method.GetILGenerator();
                LocalBuilder lb0 = ilGenerator.DeclareLocal(typeof(ValueTuple));
                ilGenerator.Emit(OpCodes.Ldloca, lb0);
                ilGenerator.Emit(OpCodes.Initobj, typeof(ValueTuple));
                ilGenerator.Emit(OpCodes.Ldc_I4, 1);
                ilGenerator.Emit(OpCodes.Ret);
                tb.CreateType();
                saveMethod.Invoke(ab, new object[] { file.Path });

                Assembly assemblyFromDisk = AssemblySaveTools.LoadAssemblyFromPath(file.Path);
                Type typeFromDisk = assemblyFromDisk.Modules.First().GetType("MyType");
                byte[]? bodyBytes = typeFromDisk.GetMethod("meth1").GetMethodBody().GetILAsByteArray();
                Assert.Equal(OpCodes.Ldloca_S.Value, bodyBytes[0]); // short form of Ldloca
                Assert.Equal(0, bodyBytes[1]);
                Assert.Equal(0xFE, bodyBytes[2]); // Initobj = 0xfe15
                Assert.Equal(0x15, bodyBytes[3]);
                Assert.Equal(OpCodes.Ldc_I4_1.Value, bodyBytes[8]);
                Assert.Equal(OpCodes.Ret.Value, bodyBytes[9]);
            }
        }

        [Fact]
        public void MemberReferenceExceptions()
        {
            AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilderTypeBuilderAndSaveMethod(out TypeBuilder type, out MethodInfo saveMethod);
            MethodBuilder method = type.DefineMethod("Method1", MethodAttributes.Public);
            ILGenerator il = method.GetILGenerator();
            MethodInfo nullMethod = null;
            ConstructorInfo nullConstructor = null;
            FieldInfo nullField = null;
            Label[] nullArray = null;
            Type nullType = null;

            Assert.Throws<ArgumentNullException>(() => il.Emit(OpCodes.Call, nullMethod));
            Assert.Throws<ArgumentNullException>(() => il.Emit(OpCodes.Callvirt, nullConstructor));
            Assert.Throws<ArgumentNullException>(() => il.Emit(OpCodes.Ldfld, nullField));
            Assert.Throws<ArgumentNullException>(() => il.Emit(OpCodes.Switch, nullArray));
            Assert.Throws<ArgumentNullException>(() => il.Emit(OpCodes.Switch, nullType));
            Assert.Throws<ArgumentNullException>(() => il.EmitCall(OpCodes.Call, nullMethod, null));
            // only OpCodes.Switch expected
            Assert.Throws<ArgumentException>(() => il.Emit(OpCodes.Call, new Label[0])); 
            // only OpCodes.Call or .OpCodes.Callvirt or OpCodes.Newob expected
            Assert.Throws<ArgumentException>(() => il.Emit(OpCodes.Switch, typeof(object).GetConstructor(Type.EmptyTypes)));
            // Undefined label
            Assert.Throws<ArgumentException>(() => il.MarkLabel(new Label()));
            // only OpCodes.Call or OpCodes.Callvirt or OpCodes.Newob expected
            Assert.Throws<ArgumentException>(() => il.EmitCall(OpCodes.Ldfld, method, null));
        }

        [Fact]
        public void SimpleTryCatchBlock()
        {
            using (TempFile file = TempFile.Create())
            {
                AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilderTypeBuilderAndSaveMethod(out TypeBuilder tb, out MethodInfo saveMethod);
                MethodBuilder method = tb.DefineMethod("Method", MethodAttributes.Public | MethodAttributes.Static, typeof(float), new[] { typeof(int), typeof(int) });
                Type dBZException = typeof(DivideByZeroException);
                ILGenerator ilGenerator = method.GetILGenerator();
                LocalBuilder local = ilGenerator.DeclareLocal(typeof(float));
                Label exBlock = ilGenerator.BeginExceptionBlock();
                ilGenerator.Emit(OpCodes.Ldarg_0);
                ilGenerator.Emit(OpCodes.Ldarg_1);
                ilGenerator.Emit(OpCodes.Div);
                ilGenerator.Emit(OpCodes.Stloc_0);
                ilGenerator.BeginCatchBlock(dBZException);
                ilGenerator.EmitWriteLine("Error: division by zero");
                ilGenerator.Emit(OpCodes.Ldc_R4, 0.0f);
                ilGenerator.Emit(OpCodes.Stloc_0);
                ilGenerator.EndExceptionBlock();
                ilGenerator.Emit(OpCodes.Ldloc_0);
                ilGenerator.Emit(OpCodes.Ret);
                tb.CreateType();
                saveMethod.Invoke(ab, new object[] { file.Path });

                Assembly assemblyFromDisk = AssemblySaveTools.LoadAssemblyFromPath(file.Path);
                Type typeFromDisk = assemblyFromDisk.Modules.First().GetType("MyType");
                MethodBody body = typeFromDisk.GetMethod("Method").GetMethodBody();
                Assert.Equal(1, body.ExceptionHandlingClauses.Count);
                Assert.Equal(dBZException.FullName, body.ExceptionHandlingClauses[0].CatchType.FullName);
                Assert.Equal(ExceptionHandlingClauseOptions.Clause, body.ExceptionHandlingClauses[0].Flags);
                byte[] bodyBytes = body.GetILAsByteArray();
                Assert.Equal(OpCodes.Ldarg_0.Value, bodyBytes[0]);
                Assert.Equal(OpCodes.Ldarg_1.Value, bodyBytes[1]);
                Assert.Equal(OpCodes.Div.Value, bodyBytes[2]);
                Assert.Equal(OpCodes.Stloc_0.Value, bodyBytes[3]);
                Assert.Equal(OpCodes.Leave.Value, bodyBytes[4]);
                // Next 4 bytes 'exBlock' label location
                Assert.Equal(OpCodes.Ldstr.Value, bodyBytes[9]); // "Error: division by zero"
                Assert.Equal(OpCodes.Call.Value, bodyBytes[14]); // Calls Console.WriteLine
                Assert.Equal(OpCodes.Ldc_R4.Value, bodyBytes[19]);
                Assert.Equal(OpCodes.Stloc_0.Value, bodyBytes[24]);
                Assert.Equal(OpCodes.Leave.Value, bodyBytes[25]);
                Assert.Equal(OpCodes.Ldloc_0.Value, bodyBytes[30]);
                Assert.Equal(OpCodes.Ret.Value, bodyBytes[31]);
            }
        }

        [Fact]
        public void TryMultipleCatchBlocks()
        {
            using (TempFile file = TempFile.Create())
            {
                AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilderTypeBuilderAndSaveMethod(out TypeBuilder tb, out MethodInfo saveMethod);
                MethodBuilder method = tb.DefineMethod("Method", MethodAttributes.Public | MethodAttributes.Static, typeof(float), new[] { typeof(int), typeof(int) });
                Type dBZException = typeof(DivideByZeroException);
                Type exception = typeof(Exception);
                ILGenerator ilGenerator = method.GetILGenerator();
                LocalBuilder local = ilGenerator.DeclareLocal(typeof(float));
                Label exBlock = ilGenerator.BeginExceptionBlock();
                ilGenerator.Emit(OpCodes.Ldarg_0);
                ilGenerator.Emit(OpCodes.Ldarg_1);
                ilGenerator.Emit(OpCodes.Div);
                ilGenerator.Emit(OpCodes.Stloc_0);
                ilGenerator.BeginCatchBlock(dBZException);
                ilGenerator.EmitWriteLine("Error: division by zero");
                ilGenerator.Emit(OpCodes.Ldc_R4, 0.0f);
                ilGenerator.Emit(OpCodes.Stloc_0);
                ilGenerator.BeginCatchBlock(exception);
                ilGenerator.EmitWriteLine("Error: generic Exception");
                ilGenerator.Emit(OpCodes.Ldc_R4, 0.0f);
                ilGenerator.Emit(OpCodes.Stloc_0);
                ilGenerator.EndExceptionBlock();
                ilGenerator.Emit(OpCodes.Ldloc_0);
                ilGenerator.Emit(OpCodes.Ret);
                tb.CreateType();
                saveMethod.Invoke(ab, new object[] { file.Path });

                Assembly assemblyFromDisk = AssemblySaveTools.LoadAssemblyFromPath(file.Path);
                Type typeFromDisk = assemblyFromDisk.Modules.First().GetType("MyType");
                MethodBody body = typeFromDisk.GetMethod("Method").GetMethodBody();
                Assert.Equal(2, body.ExceptionHandlingClauses.Count);
                Assert.Equal(ExceptionHandlingClauseOptions.Clause, body.ExceptionHandlingClauses[0].Flags);
                Assert.Equal(ExceptionHandlingClauseOptions.Clause, body.ExceptionHandlingClauses[1].Flags);
                Assert.Equal(dBZException.FullName, body.ExceptionHandlingClauses[0].CatchType.FullName);
                Assert.Equal(exception.FullName, body.ExceptionHandlingClauses[1].CatchType.FullName);
                byte[] bodyBytes = body.GetILAsByteArray();
                Assert.Equal(OpCodes.Ldarg_0.Value, bodyBytes[0]);
                Assert.Equal(OpCodes.Ldarg_1.Value, bodyBytes[1]);
                Assert.Equal(OpCodes.Div.Value, bodyBytes[2]);
                Assert.Equal(OpCodes.Stloc_0.Value, bodyBytes[3]);
                Assert.Equal(OpCodes.Leave.Value, bodyBytes[4]);
                Assert.Equal(OpCodes.Ldstr.Value, bodyBytes[9]); // "Error: division by zero"
                Assert.Equal(OpCodes.Call.Value, bodyBytes[14]); // Calls Console.WriteLine
                Assert.Equal(OpCodes.Ldc_R4.Value, bodyBytes[19]);
                Assert.Equal(OpCodes.Stloc_0.Value, bodyBytes[24]);
                Assert.Equal(OpCodes.Leave.Value, bodyBytes[25]);
                Assert.Equal(OpCodes.Ldstr.Value, bodyBytes[30]); // "Error: division by zero"
                Assert.Equal(OpCodes.Call.Value, bodyBytes[35]); // Calls Console.WriteLine
                Assert.Equal(OpCodes.Ldc_R4.Value, bodyBytes[40]);
                Assert.Equal(OpCodes.Stloc_0.Value, bodyBytes[45]);
                Assert.Equal(OpCodes.Leave.Value, bodyBytes[46]);
                Assert.Equal(OpCodes.Ldloc_0.Value, bodyBytes[51]);
                Assert.Equal(OpCodes.Ret.Value, bodyBytes[52]);
            }
        }

        [Fact]
        public void TryFilterCatchBlock()
        {
            using (TempFile file = TempFile.Create())
            {
                AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilderTypeBuilderAndSaveMethod(out TypeBuilder tb, out MethodInfo saveMethod);
                MethodBuilder method = tb.DefineMethod("Method", MethodAttributes.Public | MethodAttributes.Static, typeof(float), new[] { typeof(int), typeof(int) });
                Type dBZException = typeof(DivideByZeroException);
                Type exception = typeof(Exception);
                ILGenerator ilGenerator = method.GetILGenerator();
                LocalBuilder local = ilGenerator.DeclareLocal(typeof(float));
                Label filterEnd = ilGenerator.DefineLabel();
                Label filterCheck = ilGenerator.DefineLabel();
                Label exBlock = ilGenerator.BeginExceptionBlock();
                ilGenerator.Emit(OpCodes.Ldarg_0);
                ilGenerator.Emit(OpCodes.Ldarg_1);
                ilGenerator.Emit(OpCodes.Div);
                ilGenerator.Emit(OpCodes.Stloc_0);
                ilGenerator.BeginExceptFilterBlock();
                ilGenerator.Emit(OpCodes.Isinst, dBZException);
                ilGenerator.Emit(OpCodes.Dup);
                ilGenerator.Emit(OpCodes.Brtrue_S, filterCheck);
                ilGenerator.Emit(OpCodes.Pop);
                ilGenerator.Emit(OpCodes.Ldc_I4_0);
                ilGenerator.Emit(OpCodes.Br_S, filterEnd);
                ilGenerator.MarkLabel(filterCheck);
                ilGenerator.Emit(OpCodes.Pop);
                ilGenerator.Emit(OpCodes.Ldarg_0);
                ilGenerator.Emit(OpCodes.Ldc_I4_0);
                ilGenerator.Emit(OpCodes.Cgt_Un);
                ilGenerator.MarkLabel(filterEnd);
                ilGenerator.BeginCatchBlock(null);
                ilGenerator.EmitWriteLine("Filtered division by zero");
                ilGenerator.Emit(OpCodes.Ldc_R4, 0.0f);
                ilGenerator.Emit(OpCodes.Stloc_0);
                ilGenerator.Emit(OpCodes.Pop);
                ilGenerator.BeginCatchBlock(exception);
                ilGenerator.EmitWriteLine("Generic Exception");
                ilGenerator.Emit(OpCodes.Ldc_R4, 0.0f);
                ilGenerator.Emit(OpCodes.Stloc_0);
                ilGenerator.Emit(OpCodes.Pop);
                ilGenerator.EndExceptionBlock();
                ilGenerator.Emit(OpCodes.Ldloc_0);
                ilGenerator.Emit(OpCodes.Ret);
                tb.CreateType();
                saveMethod.Invoke(ab, new object[] { file.Path });

                Assembly assemblyFromDisk = AssemblySaveTools.LoadAssemblyFromPath(file.Path);
                Type typeFromDisk = assemblyFromDisk.Modules.First().GetType("MyType");
                MethodBody body = typeFromDisk.GetMethod("Method").GetMethodBody();
                Assert.Equal(2, body.ExceptionHandlingClauses.Count);
                Assert.Equal(ExceptionHandlingClauseOptions.Filter, body.ExceptionHandlingClauses[0].Flags);
                Assert.Equal(ExceptionHandlingClauseOptions.Clause, body.ExceptionHandlingClauses[1].Flags);
                Assert.Equal(exception.FullName, body.ExceptionHandlingClauses[1].CatchType.FullName);
            }
        }

        [Fact]
        public void TryCatchFilterCatchBlock()
        {
            using (TempFile file = TempFile.Create())
            {
                AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilderTypeBuilderAndSaveMethod(out TypeBuilder tb, out MethodInfo saveMethod);
                MethodBuilder method = tb.DefineMethod("Method", MethodAttributes.Public | MethodAttributes.Static, typeof(float), new[] { typeof(int), typeof(int) });
                Type dBZException = typeof(DivideByZeroException);
                Type overflowException = typeof(OverflowException);
                Type exception = typeof(Exception);
                ILGenerator ilGenerator = method.GetILGenerator();
                LocalBuilder local = ilGenerator.DeclareLocal(typeof(float));
                Label filterEnd = ilGenerator.DefineLabel();
                Label filterCheck = ilGenerator.DefineLabel();
                Label exBlock = ilGenerator.BeginExceptionBlock();
                ilGenerator.Emit(OpCodes.Ldarg_0);
                ilGenerator.Emit(OpCodes.Ldarg_0);
                ilGenerator.Emit(OpCodes.Mul);
                ilGenerator.Emit(OpCodes.Ldarg_1);
                ilGenerator.Emit(OpCodes.Div);
                ilGenerator.Emit(OpCodes.Stloc_0);
                ilGenerator.BeginCatchBlock(overflowException);
                ilGenerator.EmitWriteLine("Overflow Exception!");
                ilGenerator.ThrowException(overflowException);
                ilGenerator.BeginExceptFilterBlock();
                ilGenerator.Emit(OpCodes.Isinst, dBZException);
                ilGenerator.Emit(OpCodes.Dup);
                ilGenerator.Emit(OpCodes.Brtrue_S, filterCheck);
                ilGenerator.Emit(OpCodes.Pop);
                ilGenerator.Emit(OpCodes.Ldc_I4_0);
                ilGenerator.Emit(OpCodes.Br_S, filterEnd);
                ilGenerator.MarkLabel(filterCheck);
                ilGenerator.Emit(OpCodes.Pop);
                ilGenerator.Emit(OpCodes.Ldarg_0);
                ilGenerator.Emit(OpCodes.Ldc_I4_0);
                ilGenerator.Emit(OpCodes.Cgt_Un);
                ilGenerator.MarkLabel(filterEnd);
                ilGenerator.BeginCatchBlock(null);
                ilGenerator.EmitWriteLine("Filtered division by zero");
                ilGenerator.Emit(OpCodes.Ldc_R4, 0.0f);
                ilGenerator.Emit(OpCodes.Stloc_0);
                ilGenerator.Emit(OpCodes.Pop);
                ilGenerator.BeginCatchBlock(exception);
                ilGenerator.EmitWriteLine("Generic Exception");
                ilGenerator.Emit(OpCodes.Ldc_R4, 0.0f);
                ilGenerator.Emit(OpCodes.Stloc_0);
                ilGenerator.Emit(OpCodes.Pop);
                ilGenerator.EndExceptionBlock();
                ilGenerator.Emit(OpCodes.Ldloc_0);
                ilGenerator.Emit(OpCodes.Ret);
                tb.CreateType();
                saveMethod.Invoke(ab, new object[] { file.Path });

                Assembly assemblyFromDisk = AssemblySaveTools.LoadAssemblyFromPath(file.Path);
                Type typeFromDisk = assemblyFromDisk.Modules.First().GetType("MyType");
                MethodBody body = typeFromDisk.GetMethod("Method").GetMethodBody();
                Assert.Equal(3, body.ExceptionHandlingClauses.Count);
                Assert.Equal(ExceptionHandlingClauseOptions.Clause, body.ExceptionHandlingClauses[0].Flags);
                Assert.Equal(ExceptionHandlingClauseOptions.Filter, body.ExceptionHandlingClauses[1].Flags);
                Assert.Equal(ExceptionHandlingClauseOptions.Clause, body.ExceptionHandlingClauses[2].Flags);
                Assert.Equal(overflowException.FullName, body.ExceptionHandlingClauses[0].CatchType.FullName);
                Assert.Equal(exception.FullName, body.ExceptionHandlingClauses[2].CatchType.FullName);
            }
        }

        [Fact]
        public void TryFinallyBlock()
        {
            using (TempFile file = TempFile.Create())
            {
                AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilderTypeBuilderAndSaveMethod(out TypeBuilder tb, out MethodInfo saveMethod);
                MethodBuilder method = tb.DefineMethod("Method", MethodAttributes.Public | MethodAttributes.Static, typeof(float), new[] { typeof(int), typeof(int) });
                ILGenerator ilGenerator = method.GetILGenerator();
                LocalBuilder local = ilGenerator.DeclareLocal(typeof(float));
                Label exBlock = ilGenerator.BeginExceptionBlock();
                ilGenerator.Emit(OpCodes.Ldarg_0);
                ilGenerator.Emit(OpCodes.Ldarg_1);
                ilGenerator.Emit(OpCodes.Div);
                ilGenerator.Emit(OpCodes.Stloc_0);
                ilGenerator.BeginFinallyBlock();
                ilGenerator.EmitWriteLine("Finally handler");
                ilGenerator.EndExceptionBlock();
                ilGenerator.Emit(OpCodes.Ldloc_0);
                ilGenerator.Emit(OpCodes.Ret);
                tb.CreateType();
                saveMethod.Invoke(ab, new object[] { file.Path });

                Assembly assemblyFromDisk = AssemblySaveTools.LoadAssemblyFromPath(file.Path);
                Type typeFromDisk = assemblyFromDisk.Modules.First().GetType("MyType");
                MethodBody body = typeFromDisk.GetMethod("Method").GetMethodBody();
                Assert.Equal(1, body.ExceptionHandlingClauses.Count);
                Assert.Equal(ExceptionHandlingClauseOptions.Finally, body.ExceptionHandlingClauses[0].Flags);
                byte[] bodyBytes = body.GetILAsByteArray();
                Assert.Equal(OpCodes.Ldarg_0.Value, bodyBytes[0]);
                Assert.Equal(OpCodes.Ldarg_1.Value, bodyBytes[1]);
                Assert.Equal(OpCodes.Div.Value, bodyBytes[2]);
                Assert.Equal(OpCodes.Stloc_0.Value, bodyBytes[3]);
                Assert.Equal(OpCodes.Leave.Value, bodyBytes[4]);
                Assert.Equal(OpCodes.Ldstr.Value, bodyBytes[9]);
                Assert.Equal(OpCodes.Call.Value, bodyBytes[14]);
                Assert.Equal(OpCodes.Endfinally.Value, bodyBytes[19]);
                Assert.Equal(OpCodes.Ldloc_0.Value, bodyBytes[20]);
                Assert.Equal(OpCodes.Ret.Value, bodyBytes[21]);
            }
        }

        [Fact]
        public void TryCatchFinallyBlock()
        {
            using (TempFile file = TempFile.Create())
            {
                AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilderTypeBuilderAndSaveMethod(out TypeBuilder tb, out MethodInfo saveMethod);
                MethodBuilder method = tb.DefineMethod("Method", MethodAttributes.Public | MethodAttributes.Static, typeof(void), new[] { typeof(int), typeof(int) });
                Type exception = typeof(Exception);
                ILGenerator ilGenerator = method.GetILGenerator();
                ilGenerator.BeginExceptionBlock();
                ilGenerator.Emit(OpCodes.Ldarg_0);
                ilGenerator.Emit(OpCodes.Ldarg_1);
                ilGenerator.Emit(OpCodes.Div);
                ilGenerator.BeginCatchBlock(exception);
                ilGenerator.EmitWriteLine("Generic Exception");
                ilGenerator.BeginFinallyBlock();
                ilGenerator.EmitWriteLine("Finally handler");
                ilGenerator.EndExceptionBlock();
                ilGenerator.Emit(OpCodes.Ret);
                tb.CreateType();
                saveMethod.Invoke(ab, new object[] { file.Path });

                Assembly assemblyFromDisk = AssemblySaveTools.LoadAssemblyFromPath(file.Path);
                Type typeFromDisk = assemblyFromDisk.Modules.First().GetType("MyType");
                MethodBody body = typeFromDisk.GetMethod("Method").GetMethodBody();
                Assert.Equal(2, body.ExceptionHandlingClauses.Count);
                Assert.Equal(ExceptionHandlingClauseOptions.Clause, body.ExceptionHandlingClauses[0].Flags);
                Assert.Equal(ExceptionHandlingClauseOptions.Finally, body.ExceptionHandlingClauses[1].Flags);
                Assert.Equal(exception.FullName, body.ExceptionHandlingClauses[0].CatchType.FullName);
            }
        }

        [Fact]
        public void TryFilterCatchFinallyBlock()
        {
            using (TempFile file = TempFile.Create())
            {
                AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilderTypeBuilderAndSaveMethod(out TypeBuilder tb, out MethodInfo saveMethod);
                MethodBuilder method = tb.DefineMethod("Method", MethodAttributes.Public | MethodAttributes.Static, typeof(int), new[] { typeof(int), typeof(int) });
                Type overflowEType = typeof(OverflowException);
                ConstructorInfo myConstructorInfo = overflowEType.GetConstructor(new [] { typeof(string) });
                MethodInfo myExToStrMI = overflowEType.GetMethod("ToString");
                MethodInfo myWriteLineMI = typeof(Console).GetMethod("WriteLine", new [] {typeof(string),typeof(object) });
                ILGenerator myAdderIL = method.GetILGenerator();
                LocalBuilder myLocalBuilder1 = myAdderIL.DeclareLocal(typeof(int));
                LocalBuilder myLocalBuilder2 = myAdderIL.DeclareLocal(overflowEType);

                Label myFailedLabel = myAdderIL.DefineLabel();
                Label myEndOfMethodLabel = myAdderIL.DefineLabel();
                Label myLabel = myAdderIL.BeginExceptionBlock();
                myAdderIL.Emit(OpCodes.Ldarg_0);
                myAdderIL.Emit(OpCodes.Ldc_I4_S, 10);
                myAdderIL.Emit(OpCodes.Bgt_S, myFailedLabel);
                myAdderIL.Emit(OpCodes.Ldarg_1);
                myAdderIL.Emit(OpCodes.Ldc_I4_S, 10);
                myAdderIL.Emit(OpCodes.Bgt_S, myFailedLabel);
                myAdderIL.Emit(OpCodes.Ldarg_0);
                myAdderIL.Emit(OpCodes.Ldarg_1);
                myAdderIL.Emit(OpCodes.Add_Ovf_Un);
                myAdderIL.Emit(OpCodes.Stloc_S, myLocalBuilder1);
                myAdderIL.Emit(OpCodes.Br_S, myEndOfMethodLabel);
                myAdderIL.MarkLabel(myFailedLabel);
                myAdderIL.Emit(OpCodes.Ldstr, "Cannot accept values over 10 for add.");
                myAdderIL.Emit(OpCodes.Newobj, myConstructorInfo);
                myAdderIL.Emit(OpCodes.Stloc_S, myLocalBuilder2);
                myAdderIL.Emit(OpCodes.Ldloc_S, myLocalBuilder2);
                myAdderIL.Emit(OpCodes.Throw);
                myAdderIL.BeginExceptFilterBlock();
                myAdderIL.BeginCatchBlock(null);
                myAdderIL.EmitWriteLine("Except filter block handled.");
                myAdderIL.BeginCatchBlock(overflowEType);
                myAdderIL.Emit(OpCodes.Ldstr, "{0}");
                myAdderIL.Emit(OpCodes.Ldloc_S, myLocalBuilder2);
                myAdderIL.EmitCall(OpCodes.Callvirt, myExToStrMI, null);
                myAdderIL.EmitCall(OpCodes.Call, myWriteLineMI, null);
                myAdderIL.Emit(OpCodes.Ldc_I4_M1);
                myAdderIL.Emit(OpCodes.Stloc_S, myLocalBuilder1);
                myAdderIL.BeginFinallyBlock();
                myAdderIL.EmitWriteLine("Finally block handled.");
                myAdderIL.EndExceptionBlock();
                myAdderIL.MarkLabel(myEndOfMethodLabel);
                myAdderIL.Emit(OpCodes.Ldloc_S, myLocalBuilder1);
                myAdderIL.Emit(OpCodes.Ret);
                tb.CreateType();
                saveMethod.Invoke(ab, new object[] { file.Path });

                Assembly assemblyFromDisk = AssemblySaveTools.LoadAssemblyFromPath(file.Path);
                Type typeFromDisk = assemblyFromDisk.Modules.First().GetType("MyType");
                MethodBody body = typeFromDisk.GetMethod("Method").GetMethodBody();
                Assert.Equal(3, body.ExceptionHandlingClauses.Count);
                Assert.Equal(ExceptionHandlingClauseOptions.Filter, body.ExceptionHandlingClauses[0].Flags);
                Assert.Equal(ExceptionHandlingClauseOptions.Clause, body.ExceptionHandlingClauses[1].Flags);
                Assert.Equal(ExceptionHandlingClauseOptions.Finally, body.ExceptionHandlingClauses[2].Flags);
                Assert.Equal(overflowEType.FullName, body.ExceptionHandlingClauses[1].CatchType.FullName);
            }
        }

        [Fact]
        public void TryFaultBlock()
        {
            using (TempFile file = TempFile.Create())
            {
                AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilderTypeBuilderAndSaveMethod(out TypeBuilder tb, out MethodInfo saveMethod);
                MethodBuilder method = tb.DefineMethod("Method", MethodAttributes.Public | MethodAttributes.Static, typeof(float), new[] { typeof(int), typeof(int) });
                ILGenerator ilGenerator = method.GetILGenerator();
                Label exBlock = ilGenerator.BeginExceptionBlock();
                ilGenerator.Emit(OpCodes.Ldarg_0);
                ilGenerator.Emit(OpCodes.Ldarg_1);
                ilGenerator.Emit(OpCodes.Div);
                ilGenerator.Emit(OpCodes.Stloc_0);
                ilGenerator.BeginFaultBlock();
                ilGenerator.EmitWriteLine("Fault handling");
                ilGenerator.Emit(OpCodes.Ldc_R4, 0.0f);
                ilGenerator.Emit(OpCodes.Stloc_0);
                ilGenerator.EndExceptionBlock();
                ilGenerator.Emit(OpCodes.Ldloc_0);
                ilGenerator.Emit(OpCodes.Ret);
                tb.CreateType();
                saveMethod.Invoke(ab, new object[] { file.Path });

                Assembly assemblyFromDisk = AssemblySaveTools.LoadAssemblyFromPath(file.Path);
                Type typeFromDisk = assemblyFromDisk.Modules.First().GetType("MyType");
                MethodBody body = typeFromDisk.GetMethod("Method").GetMethodBody();
                Assert.Equal(1, body.ExceptionHandlingClauses.Count);
                Assert.Equal(ExceptionHandlingClauseOptions.Fault, body.ExceptionHandlingClauses[0].Flags);
                byte[] bodyBytes = body.GetILAsByteArray();
                Assert.Equal(OpCodes.Ldarg_0.Value, bodyBytes[0]);
                Assert.Equal(OpCodes.Ldarg_1.Value, bodyBytes[1]);
                Assert.Equal(OpCodes.Div.Value, bodyBytes[2]);
                Assert.Equal(OpCodes.Stloc_0.Value, bodyBytes[3]);
                Assert.Equal(OpCodes.Leave.Value, bodyBytes[4]);
                Assert.Equal(OpCodes.Ldstr.Value, bodyBytes[9]);
                Assert.Equal(OpCodes.Call.Value, bodyBytes[14]);
                Assert.Equal(OpCodes.Ldc_R4.Value, bodyBytes[19]);
                Assert.Equal(OpCodes.Stloc_0.Value, bodyBytes[24]);
                Assert.Equal(OpCodes.Endfinally.Value, bodyBytes[25]);
                Assert.Equal(OpCodes.Ldloc_0.Value, bodyBytes[26]);
                Assert.Equal(OpCodes.Ret.Value, bodyBytes[27]);
            }
        }

        [Fact]
        public void NestedTryCatchBlocks()
        {
            using (TempFile file = TempFile.Create())
            {
                AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilderTypeBuilderAndSaveMethod(out TypeBuilder tb, out MethodInfo saveMethod);
                MethodBuilder method = tb.DefineMethod("Method", MethodAttributes.Public | MethodAttributes.Static, typeof(void), new[] { typeof(int), typeof(int) });
                Type exception = typeof(Exception);
                ILGenerator ilGenerator = method.GetILGenerator();
                ilGenerator.BeginExceptionBlock();
                ilGenerator.Emit(OpCodes.Ldarg_0);
                ilGenerator.Emit(OpCodes.Ldarg_1);
                ilGenerator.Emit(OpCodes.Div);
                ilGenerator.BeginExceptionBlock();
                ilGenerator.EmitWriteLine("Try block nested in try");
                ilGenerator.BeginCatchBlock(exception);
                ilGenerator.EmitWriteLine("Catch block nested in try");
                ilGenerator.EndExceptionBlock();
                ilGenerator.EmitWriteLine("Outer try block ends");
                ilGenerator.BeginCatchBlock(exception);
                ilGenerator.EmitWriteLine("Outer catch block starts");
                ilGenerator.BeginExceptionBlock();
                ilGenerator.EmitWriteLine("Try block nested in catch");
                ilGenerator.BeginCatchBlock(exception);
                ilGenerator.EmitWriteLine("Catch block nested in catch");
                ilGenerator.EndExceptionBlock();
                ilGenerator.EmitWriteLine("Outer catch block ends");
                ilGenerator.EndExceptionBlock();
                ilGenerator.Emit(OpCodes.Ret);
                tb.CreateType();
                saveMethod.Invoke(ab, new object[] { file.Path });

                Assembly assemblyFromDisk = AssemblySaveTools.LoadAssemblyFromPath(file.Path);
                Type typeFromDisk = assemblyFromDisk.Modules.First().GetType("MyType");
                MethodBody body = typeFromDisk.GetMethod("Method").GetMethodBody();
                Assert.Equal(3, body.ExceptionHandlingClauses.Count);
                Assert.Equal(ExceptionHandlingClauseOptions.Clause, body.ExceptionHandlingClauses[0].Flags);
                Assert.Equal(ExceptionHandlingClauseOptions.Clause, body.ExceptionHandlingClauses[1].Flags);
                Assert.Equal(ExceptionHandlingClauseOptions.Clause, body.ExceptionHandlingClauses[2].Flags);
                Assert.Equal(exception.FullName, body.ExceptionHandlingClauses[0].CatchType.FullName);
                Assert.Equal(exception.FullName, body.ExceptionHandlingClauses[1].CatchType.FullName);
                Assert.Equal(exception.FullName, body.ExceptionHandlingClauses[2].CatchType.FullName);
            }
        }

        [Fact]
        public void DeeperNestedTryCatchFilterFinallyBlocks()
        {
            using (TempFile file = TempFile.Create())
            {
                AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilderTypeBuilderAndSaveMethod(out TypeBuilder tb, out MethodInfo saveMethod);
                MethodBuilder method = tb.DefineMethod("Method", MethodAttributes.Public | MethodAttributes.Static, typeof(int), new[] { typeof(int), typeof(int) });
                Type exception = typeof(Exception);
                ILGenerator ilGenerator = method.GetILGenerator();
                LocalBuilder local = ilGenerator.DeclareLocal(typeof(int));
                ilGenerator.Emit(OpCodes.Ldc_I4_0);
                ilGenerator.Emit(OpCodes.Stloc_0);

                ilGenerator.BeginExceptionBlock();
                ilGenerator.Emit(OpCodes.Ldarg_0);
                ilGenerator.Emit(OpCodes.Ldarg_1);
                ilGenerator.Emit(OpCodes.Div);
                ilGenerator.Emit(OpCodes.Stloc_0);

                ilGenerator.BeginExceptionBlock();
                ilGenerator.EmitWriteLine("1st nested try block nested in try");
                Label myLabel = ilGenerator.BeginExceptionBlock();
                ilGenerator.EmitWriteLine("2nd nested try block starts");
                ilGenerator.Emit(OpCodes.Ldc_I4_3);
                ilGenerator.Emit(OpCodes.Ldarg_0);
                ilGenerator.Emit(OpCodes.Add);
                ilGenerator.Emit(OpCodes.Stloc_0);

                ilGenerator.BeginExceptionBlock();
                ilGenerator.EmitWriteLine("3rd nested try block");
                ilGenerator.BeginFinallyBlock();
                ilGenerator.EmitWriteLine("3rd nested finally block");
                ilGenerator.EndExceptionBlock();

                ilGenerator.EmitWriteLine("2nd nested try block ends");
                ilGenerator.BeginExceptFilterBlock();
                ilGenerator.EmitWriteLine("2nd nested filter block starts.");
                ilGenerator.BeginCatchBlock(null);
                ilGenerator.EmitWriteLine("2nd nested filter block handled.");
                ilGenerator.BeginCatchBlock(exception);
                ilGenerator.EmitWriteLine("2nd nested catch block handled.");
                ilGenerator.BeginFinallyBlock();
                ilGenerator.EmitWriteLine("2nd nested finally block handled.");
                ilGenerator.EndExceptionBlock();

                ilGenerator.BeginCatchBlock(exception);
                ilGenerator.EmitWriteLine("Catch block nested in try");
                ilGenerator.EndExceptionBlock();

                ilGenerator.EmitWriteLine("Outer try block ends");
                ilGenerator.BeginCatchBlock(exception);
                ilGenerator.EmitWriteLine("Outer catch block starts");
                ilGenerator.EmitWriteLine("Outer catch block ends");
                ilGenerator.EndExceptionBlock();

                ilGenerator.Emit(OpCodes.Ldloc_0);
                ilGenerator.Emit(OpCodes.Ret);
                tb.CreateType();
                saveMethod.Invoke(ab, new object[] { file.Path });

                Assembly assemblyFromDisk = AssemblySaveTools.LoadAssemblyFromPath(file.Path);
                Type typeFromDisk = assemblyFromDisk.Modules.First().GetType("MyType");
                MethodBody body = typeFromDisk.GetMethod("Method").GetMethodBody();
                Assert.Equal(6, body.ExceptionHandlingClauses.Count);
                Assert.Equal(ExceptionHandlingClauseOptions.Finally, body.ExceptionHandlingClauses[0].Flags);
                Assert.Equal(ExceptionHandlingClauseOptions.Filter, body.ExceptionHandlingClauses[1].Flags);
                Assert.Equal(ExceptionHandlingClauseOptions.Clause, body.ExceptionHandlingClauses[2].Flags);
                Assert.Equal(ExceptionHandlingClauseOptions.Finally, body.ExceptionHandlingClauses[3].Flags);
                Assert.Equal(ExceptionHandlingClauseOptions.Clause, body.ExceptionHandlingClauses[4].Flags);
                Assert.Equal(ExceptionHandlingClauseOptions.Clause, body.ExceptionHandlingClauses[5].Flags);
            }
        }

        [Fact]
        public void EmitCall_VarArgsMethodInIL()
        {
            using (TempFile file = TempFile.Create())
            {
                AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilderTypeBuilderAndSaveMethod(out TypeBuilder tb, out MethodInfo saveMethod);
                MethodBuilder mb1 = tb.DefineMethod("VarargMethod", MethodAttributes.Public | MethodAttributes.Static, CallingConventions.VarArgs, null, [typeof(string)]);
                ILGenerator il1 = mb1.GetILGenerator();
                LocalBuilder locAi = il1.DeclareLocal(typeof(ArgIterator));
                LocalBuilder locNext = il1.DeclareLocal(typeof(bool));
                Label labelCheckCondition = il1.DefineLabel();
                Label labelNext = il1.DefineLabel();
                // Load the fixed argument and print it.
                il1.Emit(OpCodes.Ldarg_0);
                il1.Emit(OpCodes.Call, typeof(Console).GetMethod("Write", [typeof(string)]));
                // Load the address of the local variable represented by
                // locAi, which will hold the ArgIterator.
                il1.Emit(OpCodes.Ldloca_S, locAi);
                // Load the address of the argument list, and call the ArgIterator
                // constructor that takes an array of runtime argument handles.
                il1.Emit(OpCodes.Arglist);
                il1.Emit(OpCodes.Call, typeof(ArgIterator).GetConstructor([typeof(RuntimeArgumentHandle)]));
                // Enter the loop at the point where the remaining argument
                // count is tested.
                il1.Emit(OpCodes.Br_S, labelCheckCondition);
                // At the top of the loop, call GetNextArg to get the next
                // argument from the ArgIterator. Convert the typed reference
                // to an object reference and write the object to the console.
                il1.MarkLabel(labelNext);
                il1.Emit(OpCodes.Ldloca_S, locAi);
                il1.Emit(OpCodes.Call, typeof(ArgIterator).GetMethod("GetNextArg", Type.EmptyTypes));
                il1.Emit(OpCodes.Call, typeof(TypedReference).GetMethod("ToObject"));
                il1.Emit(OpCodes.Call, typeof(Console).GetMethod("Write", [typeof(object)]));
                il1.MarkLabel(labelCheckCondition);
                il1.Emit(OpCodes.Ldloca_S, locAi);
                il1.Emit(OpCodes.Call, typeof(ArgIterator).GetMethod("GetRemainingCount"));
                // If the remaining count is greater than zero, go to
                // the top of the loop.
                il1.Emit(OpCodes.Ldc_I4_0);
                il1.Emit(OpCodes.Cgt);
                il1.Emit(OpCodes.Stloc_1);
                il1.Emit(OpCodes.Ldloc_1);
                il1.Emit(OpCodes.Brtrue_S, labelNext);
                il1.Emit(OpCodes.Ret);

                // Create a method that contains a call to the vararg method.
                MethodBuilder mb2 = tb.DefineMethod("CallVarargMethod", MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard);
                ILGenerator il2 = mb2.GetILGenerator();
                // Push arguments on the stack: one for the fixed string
                // parameter, and two for the list.
                il2.Emit(OpCodes.Ldstr, "Hello ");
                il2.Emit(OpCodes.Ldstr, "world ");
                il2.Emit(OpCodes.Ldc_I4, 2006);
                // Call the vararg method, specifying the types of the
                // arguments in the list.
                il2.EmitCall(OpCodes.Call, mb1, [typeof(string), typeof(int)]);
                il2.Emit(OpCodes.Ret);
                Type type = tb.CreateType();
                saveMethod.Invoke(ab, [file.Path]);

                Assembly assemblyFromDisk = AssemblySaveTools.LoadAssemblyFromPath(file.Path);
                Type typeFromDisk = assemblyFromDisk.Modules.First().GetType("MyType");
                MethodInfo varargMethodFromDisk = typeFromDisk.GetMethod("VarargMethod");
                Assert.Equal(CallingConventions.VarArgs, varargMethodFromDisk.CallingConvention);
                ParameterInfo[] parameters = varargMethodFromDisk.GetParameters();
                Assert.Equal(1, parameters.Length); // TODO: how to get the vararg parameter?
            }
        }

        [Fact]
        public void EmitCalli_CallFixedAndVarargMethodsInIL()
        {
            using (TempFile file = TempFile.Create())
            {
                AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilderTypeBuilderAndSaveMethod(out TypeBuilder tb, out MethodInfo saveMethod);
                MethodInfo getpid = typeof(AssemblySaveILGeneratorTests).GetMethod("getpid", BindingFlags.Static | BindingFlags.NonPublic);
                MethodInfo print = typeof(AssemblySaveILGeneratorTests).GetMethod("Print", BindingFlags.Static | BindingFlags.NonPublic);
                MethodBuilder mb2 = tb.DefineMethod("CallingMethod", MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard);
                ILGenerator il2 = mb2.GetILGenerator();
                LocalBuilder local = il2.DeclareLocal(typeof(int));
                il2.EmitWriteLine("Calling native functions");
                il2.Emit(OpCodes.Ldftn, getpid);
                il2.EmitCalli(OpCodes.Calli, CallingConvention.StdCall, typeof(int), Type.EmptyTypes);
                il2.Emit(OpCodes.Stloc_0);
                il2.Emit(OpCodes.Ldstr, "Hello ");
                il2.Emit(OpCodes.Ldstr, "world ");
                il2.Emit(OpCodes.Ldloc_0);
                il2.Emit(OpCodes.Ldftn, print);
                il2.EmitCalli(OpCodes.Calli, CallingConventions.VarArgs, typeof(void), [typeof(string)], [typeof(string), typeof(int)]);
                il2.Emit(OpCodes.Ret);
                Type type = tb.CreateType();
                saveMethod.Invoke(ab, [file.Path]);

                Assembly assemblyFromDisk = AssemblySaveTools.LoadAssemblyFromPath(file.Path);
                Type typeFromDisk = assemblyFromDisk.Modules.First().GetType("MyType");
                MethodInfo varargMethodFromDisk = typeFromDisk.GetMethod("VarargMethod");
            } 
        }

        [DllImport("libSystem.dylib")]
        private static extern int getpid();

        internal static void Print(string format, params object[] args)
        {
            Console.WriteLine(format, args);
        }

        [Fact]
        public void Emit_CallBySignature()
        {
            using (TempFile file = TempFile.Create())
            {
                AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilderTypeBuilderAndSaveMethod(out TypeBuilder tb, out MethodInfo saveMethod);
                MethodBuilder mb1 = tb.DefineMethod("VarargMethod", MethodAttributes.Public | MethodAttributes.Static, CallingConventions.VarArgs, null, [typeof(string)]);
                ILGenerator il1 = mb1.GetILGenerator();
                LocalBuilder locAi = il1.DeclareLocal(typeof(ArgIterator));
                LocalBuilder locNext = il1.DeclareLocal(typeof(bool));
                Label labelCheckCondition = il1.DefineLabel();
                Label labelNext = il1.DefineLabel();
                il1.Emit(OpCodes.Ldarg_0);
                il1.Emit(OpCodes.Call, typeof(Console).GetMethod("Write", [typeof(string)]));
                il1.Emit(OpCodes.Ldloca_S, locAi);
                il1.Emit(OpCodes.Arglist);
                il1.Emit(OpCodes.Call, typeof(ArgIterator).GetConstructor([typeof(RuntimeArgumentHandle)]));
                il1.Emit(OpCodes.Br_S, labelCheckCondition);
                il1.MarkLabel(labelNext);
                il1.Emit(OpCodes.Ldloca_S, locAi);
                il1.Emit(OpCodes.Call, typeof(ArgIterator).GetMethod("GetNextArg", Type.EmptyTypes));
                il1.Emit(OpCodes.Call, typeof(TypedReference).GetMethod("ToObject"));
                il1.Emit(OpCodes.Call, typeof(Console).GetMethod("Write", [typeof(object)]));
                il1.MarkLabel(labelCheckCondition);
                il1.Emit(OpCodes.Ldloca_S, locAi);
                il1.Emit(OpCodes.Call, typeof(ArgIterator).GetMethod("GetRemainingCount"));
                il1.Emit(OpCodes.Ldc_I4_0);
                il1.Emit(OpCodes.Cgt);
                il1.Emit(OpCodes.Stloc_1);
                il1.Emit(OpCodes.Ldloc_1);
                il1.Emit(OpCodes.Brtrue_S, labelNext);
                il1.Emit(OpCodes.Ret);

                MethodBuilder mb2 = tb.DefineMethod("CallingMethod", MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard);
                ILGenerator il2 = mb2.GetILGenerator();
                il2.Emit(OpCodes.Ldstr, "Hello ");
                il2.Emit(OpCodes.Ldstr, "world ");
                il2.Emit(OpCodes.Ldc_I4, 2024);
                il2.Emit(OpCodes.Ldftn, mb1);
                SignatureHelper signature = SignatureHelper.GetMethodSigHelper(CallingConventions.VarArgs, typeof(void));
                signature.AddArgument(typeof(string));
                signature.AddSentinel();
                signature.AddArgument(typeof(string));
                signature.AddArgument(typeof(int));
                il2.Emit(OpCodes.Calli, signature);
                il2.Emit(OpCodes.Ret);
                Type type = tb.CreateType();
                saveMethod.Invoke(ab, [file.Path]);

                Assembly assemblyFromDisk = AssemblySaveTools.LoadAssemblyFromPath(file.Path);
                Type typeFromDisk = assemblyFromDisk.Modules.First().GetType("MyType");
                MethodInfo varargMethodFromDisk = typeFromDisk.GetMethod("VarargMethod");
            }
        }
    }
}
