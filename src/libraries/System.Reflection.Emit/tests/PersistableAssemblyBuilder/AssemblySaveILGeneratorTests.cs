// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
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
                AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilderAndTypeBuilder(out TypeBuilder type);
                MethodBuilder methodBuilder = type.DefineMethod("EmptyMethod", MethodAttributes.Public, typeof(void), [typeof(Version)]);
                ILGenerator il = methodBuilder.GetILGenerator();
                il.Emit(OpCodes.Ret);
                type.CreateType();
                ab.Save(file.Path);

                using (MetadataLoadContext mlc = new MetadataLoadContext(new CoreMetadataAssemblyResolver()))
                {
                    Assembly assemblyFromDisk = mlc.LoadFromAssemblyPath(file.Path);
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
        }

        [Theory]
        [InlineData(20)]
        [InlineData(-10)] // For compat, runtime implementation doesn't throw for negative value.
        public void MethodReturning_Int(int size)
        {
            using (TempFile file = TempFile.Create())
            {
                AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilderAndTypeBuilder(out TypeBuilder type);
                MethodBuilder method = type.DefineMethod("TestMethod", MethodAttributes.Public | MethodAttributes.Static, typeof(int), Type.EmptyTypes);

                ILGenerator ilGenerator = method.GetILGenerator(size);
                int expectedReturn = 5;
                ilGenerator.Emit(OpCodes.Ldc_I4, expectedReturn);
                ilGenerator.Emit(OpCodes.Ret);
                type.CreateType();
                ab.Save(file.Path);

                using (MetadataLoadContext mlc = new MetadataLoadContext(new CoreMetadataAssemblyResolver()))
                {
                    Assembly assemblyFromDisk = mlc.LoadFromAssemblyPath(file.Path);
                    Type typeFromDisk = assemblyFromDisk.Modules.First().GetType("MyType");
                    MethodInfo methodFromFile = typeFromDisk.GetMethod("TestMethod");
                    MethodBody body = methodFromFile.GetMethodBody();
                    byte[]? bodyBytes = body.GetILAsByteArray();
                    Assert.NotNull(bodyBytes);
                    Assert.Equal(OpCodes.Ldc_I4_5.Value, bodyBytes[0]);
                    Assert.Equal(OpCodes.Ret.Value, bodyBytes[1]);
                }
            }
        }

        [Theory]
        [InlineData(20)]
        [InlineData(11)]
        public void TypeWithTwoMethod_ReferenceMethodArguments(int multiplier)
        {
            using (TempFile file = TempFile.Create())
            {
                AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilderAndTypeBuilder(out TypeBuilder type);
                MethodBuilder multiplyMethod = type.DefineMethod("MultiplyMethod", MethodAttributes.Public | MethodAttributes.Static, typeof(int), [typeof(int)]);
                multiplyMethod.DefineParameter(1, ParameterAttributes.None, "myParam");
                MethodBuilder addMethod = type.DefineMethod("AddMethod", MethodAttributes.Public | MethodAttributes.Static, typeof(int), [typeof(int), typeof(int)]);
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
                ab.Save(file.Path);

                using (MetadataLoadContext mlc = new MetadataLoadContext(new CoreMetadataAssemblyResolver()))
                {
                    Assembly assemblyFromDisk = mlc.LoadFromAssemblyPath(file.Path);
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
        }

        [Fact]
        public void MultipleTypesWithMultipleMethods()
        {
            using (TempFile file = TempFile.Create())
            {
                AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilderAndTypeBuilder(out TypeBuilder type);
                MethodBuilder multiplyMethod = type.DefineMethod("MultiplyMethod", MethodAttributes.Public, typeof(short), [typeof(short)]);
                MethodBuilder addMethod = type.DefineMethod("AddMethod", MethodAttributes.Public | MethodAttributes.Static, typeof(double), [typeof(double)]);

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
                ab.Save(file.Path);

                using (MetadataLoadContext mlc = new MetadataLoadContext(new CoreMetadataAssemblyResolver()))
                {
                    Assembly assemblyFromDisk = mlc.LoadFromAssemblyPath(file.Path);
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
        }

        [Fact]
        public void ILOffset_Test()
        {
            AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilderAndTypeBuilder(out TypeBuilder type);
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
                AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilderAndTypeBuilder(out TypeBuilder type);
                MethodBuilder method1 = type.DefineMethod("Method1", MethodAttributes.Public, typeof(long), [typeof(int), typeof(long), typeof(short), typeof(byte)]);
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

                MethodBuilder method2 = type.DefineMethod("Method2", MethodAttributes.Public, typeof(int), [typeof(int), typeof(byte)]);
                ILGenerator il2 = method2.GetILGenerator();

                // int Method2(int x, int y) =>  x + (y + 18);
                il2.Emit(OpCodes.Ldarg_1);     // push 1 MaxStack 1
                il2.Emit(OpCodes.Ldarg_2);     // push 1 MaxStack 2
                il2.Emit(OpCodes.Ldc_I4_S, 8); // push 1 MaxStack 3
                il2.Emit(OpCodes.Add);         // pop 2 push 1 stack size 2
                il2.Emit(OpCodes.Add);         // pop 2 push 1 stack size 1
                il2.Emit(OpCodes.Ret);         // pop 1 stack size 0
                type.CreateType();
                ab.Save(file.Path);

                MethodInfo getMaxStackMethod = GetMaxStackMethod();
                Assert.Equal(9, getMaxStackMethod.Invoke(il1, null));
                Assert.Equal(3, getMaxStackMethod.Invoke(il2, null));

                using (MetadataLoadContext mlc = new MetadataLoadContext(new CoreMetadataAssemblyResolver()))
                {
                    Assembly assemblyFromDisk = mlc.LoadFromAssemblyPath(file.Path);
                    Type typeFromDisk = assemblyFromDisk.Modules.First().GetType("MyType");
                    MethodBody body1 = typeFromDisk.GetMethod("Method1").GetMethodBody();
                    MethodBody body2 = typeFromDisk.GetMethod("Method2").GetMethodBody();
                    Assert.Equal(9, body1.MaxStackSize);
                    Assert.Equal(8, body2.MaxStackSize); // apparently doesn't write lower than 8
                }
            }
        }

        private static MethodInfo GetMaxStackMethod()
        {
            Type ilgType = Type.GetType("System.Reflection.Emit.ILGeneratorImpl, System.Reflection.Emit", throwOnError: true)!;
            return ilgType.GetMethod("GetMaxStack", BindingFlags.NonPublic | BindingFlags.Instance, Type.EmptyTypes);
        }

        private static FieldInfo GetMaxStackDepthAndCurrentStackDepthField(out FieldInfo currentStack)
        {
            Type ilgType = Type.GetType("System.Reflection.Emit.ILGeneratorImpl, System.Reflection.Emit", throwOnError: true)!;
            currentStack = ilgType.GetField("_currentStackDepth", BindingFlags.NonPublic | BindingFlags.Instance)!;
            return ilgType.GetField("_maxStackDepth", BindingFlags.NonPublic | BindingFlags.Instance);
        }

        [Fact]
        public void Label_ConditionalBranching()
        {
            using (TempFile file = TempFile.Create())
            {
                AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilderAndTypeBuilder(out TypeBuilder type);
                MethodBuilder methodBuilder = type.DefineMethod("Method1", MethodAttributes.Public, typeof(int), [typeof(int), typeof(int)]);
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
                ab.Save(file.Path);

                MethodInfo getMaxStackMethod = GetMaxStackMethod();
                Assert.Equal(2, getMaxStackMethod.Invoke(il, null));

                using (MetadataLoadContext mlc = new MetadataLoadContext(new CoreMetadataAssemblyResolver()))
                {
                    Assembly assemblyFromDisk = mlc.LoadFromAssemblyPath(file.Path);
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
        }

        [Fact]
        public void Label_SwitchCase()
        {
            using (TempFile file = TempFile.Create())
            {
                AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilderAndTypeBuilder(out TypeBuilder type);
                MethodBuilder methodBuilder = type.DefineMethod("Method1", MethodAttributes.Public, typeof(string), [typeof(int)]);
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
                ab.Save(file.Path);

                MethodInfo getMaxStackMethod = GetMaxStackMethod();
                Assert.Equal(1, getMaxStackMethod.Invoke(il, null));

                using (MetadataLoadContext mlc = new MetadataLoadContext(new CoreMetadataAssemblyResolver()))
                {
                    Assembly assemblyFromDisk = mlc.LoadFromAssemblyPath(file.Path);
                    Type typeFromDisk = assemblyFromDisk.Modules.First().GetType("MyType");
                    byte[]? bodyBytes = typeFromDisk.GetMethod("Method1").GetMethodBody().GetILAsByteArray();
                    Assert.Equal((byte)OpCodes.Ldarg_1.Value, bodyBytes[0]);
                    Assert.Equal((byte)OpCodes.Switch.Value, bodyBytes[1]);
                    Assert.Equal(5, bodyBytes[2]); // case count
                    Assert.Equal(69, bodyBytes.Length);
                }
            }
        }

        [Fact]
        public void LocalBuilderMultipleLocalsUsage()
        {
            using (TempFile file = TempFile.Create())
            {
                AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilderAndTypeBuilder(out TypeBuilder type);
                MethodBuilder methodBuilder = type.DefineMethod("Method1", MethodAttributes.Public | MethodAttributes.Static, typeof(int), [typeof(int), typeof(string)]);
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
                MethodInfo getMaxStackMethod = GetMaxStackMethod();
                Assert.Equal(2, getMaxStackMethod.Invoke(il, null));
                ab.Save(file.Path);

                using (MetadataLoadContext mlc = new MetadataLoadContext(new CoreMetadataAssemblyResolver()))
                {
                    Assembly assemblyFromDisk = mlc.LoadFromAssemblyPath(file.Path);
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
                    Assert.Equal(2, BinaryPrimitives.ReadInt32LittleEndian(bodyBytes.AsSpan().Slice(20, 4))); // index 2 of 'il.Emit(OpCodes.Stloc, 2);' instruction
                    Assert.Equal((byte)OpCodes.Ldloc_2.Value, bodyBytes[24]);
                    Assert.Equal(0xFE, bodyBytes[25]); // Ldloc = 0xfe0c
                    Assert.Equal(0x0C, bodyBytes[26]);
                    Assert.Equal(0, BinaryPrimitives.ReadInt32LittleEndian(bodyBytes.AsSpan().Slice(27, 4))); // index 0 of 'il.Emit(OpCodes.Ldloc, 0);' instruction
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
        }

        [Fact]
        public void LocalBuilderMultipleTypesWithMultipleMethodsWithLocals()
        {
            using (TempFile file = TempFile.Create())
            {
                AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilderAndTypeBuilder(out TypeBuilder type);
                MethodBuilder methodBuilder = type.DefineMethod("Method1", MethodAttributes.Public | MethodAttributes.Static, typeof(string), [typeof(int), typeof(string)]);
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
                MethodBuilder multiplyMethod = type.DefineMethod("MultiplyMethod", MethodAttributes.Public, typeof(int), [typeof(int)]);
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
                ab.Save(file.Path);

                using (MetadataLoadContext mlc = new MetadataLoadContext(new CoreMetadataAssemblyResolver()))
                {
                    Assembly assemblyFromDisk = mlc.LoadFromAssemblyPath(file.Path);
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
        }

        [Fact]
        public void LocalBuilderExceptions()
        {
            AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilderAndTypeBuilder(out TypeBuilder type);
            ILGenerator il = type.DefineMethod("Method1", MethodAttributes.Public).GetILGenerator();
            ILGenerator anotherIL = type.DefineMethod("AnotherMethod", MethodAttributes.Public).GetILGenerator();
            LocalBuilder stringLocal = il.DeclareLocal(typeof(string));
            LocalBuilder nullBuilder = null;

            Assert.Throws<ArgumentNullException>("localType", () => il.DeclareLocal(null!));
            Assert.Throws<ArgumentNullException>("local", () => il.Emit(OpCodes.Ldloc, nullBuilder));
            Assert.Throws<ArgumentException>("local", () => anotherIL.Emit(OpCodes.Ldloc, stringLocal));
        }

        [Fact]
        public void ReferenceFieldInIL()
        {
            using (TempFile file = TempFile.Create())
            {
                AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilderAndTypeBuilder(out TypeBuilder tb);
                MethodBuilder methodBuilder = tb.DefineMethod("Method1", MethodAttributes.Public, typeof(int), [typeof(int)]);
                FieldBuilder fbNumber = tb.DefineField("_number", typeof(int), FieldAttributes.Private);
                Assert.Equal(0, fbNumber.MetadataToken);

                ILGenerator il = methodBuilder.GetILGenerator();
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldfld, fbNumber);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Mul);
                il.Emit(OpCodes.Ret);
                tb.CreateType();
                ab.Save(file.Path);

                using (MetadataLoadContext mlc = new MetadataLoadContext(new CoreMetadataAssemblyResolver()))
                {
                    Assembly assemblyFromDisk = mlc.LoadFromAssemblyPath(file.Path);
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
        }

        [Fact]
        public void ReferenceFieldAndMethodsInIL()
        {
            using (TempFile file = TempFile.Create())
            {
                AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilderAndTypeBuilder(out TypeBuilder tb);
                MethodBuilder methodMain = tb.DefineMethod("Main", MethodAttributes.Public, typeof(void), [typeof(int)]);
                FieldBuilder field = tb.DefineField("_field", typeof(int), FieldAttributes.Private);
                MethodInfo writeLineString = typeof(Console).GetMethod("WriteLine", [typeof(string)]);
                MethodInfo writeLineObj = typeof(Console).GetMethod("WriteLine", [typeof(string), typeof(object), typeof(object), typeof(object)]);
                MethodBuilder methodMultiply = tb.DefineMethod("Multiply", MethodAttributes.Public, typeof(int), [typeof(int)]);
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
                ab.Save(file.Path);

                using (MetadataLoadContext mlc = new MetadataLoadContext(new CoreMetadataAssemblyResolver()))
                {
                    Assembly assemblyFromDisk = mlc.LoadFromAssemblyPath(file.Path);
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
                    Assert.Equal(intTypeToken, BinaryPrimitives.ReadInt32LittleEndian(bodyBytes.AsSpan().Slice(28, 4)));
                    Assert.Equal(OpCodes.Ldarg_0.Value, bodyBytes[32]);
                    Assert.Equal(OpCodes.Ldarg_1.Value, bodyBytes[33]);
                    Assert.Equal(OpCodes.Call.Value, bodyBytes[34]);
                    Assert.Equal(methodMultiply.MetadataToken, BinaryPrimitives.ReadInt32LittleEndian(bodyBytes.AsSpan().Slice(35, 4)));
                    Assert.Equal(OpCodes.Box.Value, bodyBytes[39]);
                    Assert.Equal(intTypeToken, BinaryPrimitives.ReadInt32LittleEndian(bodyBytes.AsSpan().Slice(40, 4)));
                    Assert.Equal(OpCodes.Call.Value, bodyBytes[44]);
                    // Bytes 24, 46, 47, 48 are token for writeLineObj, but it is not same as the value before save
                    Assert.Equal(OpCodes.Ret.Value, bodyBytes[49]);
                }
            }
        }

        [Fact]
        public void ReferenceConstructedGenericMethod()
        {
            using (TempFile file = TempFile.Create())
            {
                AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilderAndTypeBuilder(out TypeBuilder type);
                ConstructorBuilder ctor = type.DefineDefaultConstructor(MethodAttributes.Public);
                MethodBuilder genericMethod = type.DefineMethod("GM", MethodAttributes.Public | MethodAttributes.Static);
                GenericTypeParameterBuilder[] methodParams = genericMethod.DefineGenericParameters("U");
                genericMethod.SetSignature(null, null, null, new[] { methodParams[0] }, null, null);
                ILGenerator ilg = genericMethod.GetILGenerator();
                MethodInfo writeLineObj = typeof(Console).GetMethod("WriteLine", [typeof(object)]);
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
                ab.Save(file.Path);

                using (MetadataLoadContext mlc = new MetadataLoadContext(new CoreMetadataAssemblyResolver()))
                {
                    Assembly assemblyFromDisk = mlc.LoadFromAssemblyPath(file.Path);
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
        }

        [Fact]
        public void ReferenceConstructedGenericMethodFieldOfConstructedType()
        {
            using (TempFile file = TempFile.Create())
            {
                AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilderAndTypeBuilder(out TypeBuilder type);
                GenericTypeParameterBuilder[] typeParams = type.DefineGenericParameters(["T"]);
                ConstructorBuilder ctor = type.DefineDefaultConstructor(MethodAttributes.PrivateScope | MethodAttributes.Public |
                    MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);
                FieldBuilder myField = type.DefineField("Field", typeParams[0], FieldAttributes.Public);
                MethodBuilder genericMethod = type.DefineMethod("GM", MethodAttributes.Public | MethodAttributes.Static);
                GenericTypeParameterBuilder[] methodParams = genericMethod.DefineGenericParameters("U");
                genericMethod.SetSignature(null, null, null, new[] { methodParams[0] }, null, null);
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
                MethodInfo writeLineObj = typeof(Console).GetMethod("WriteLine", [typeof(object)]);
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
                ab.Save(file.Path);

                using (MetadataLoadContext mlc = new MetadataLoadContext(new CoreMetadataAssemblyResolver()))
                {
                    Assembly assemblyFromDisk = mlc.LoadFromAssemblyPath(file.Path);
                    Module module = assemblyFromDisk.Modules.First();
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
        }

        [Fact]
        public void EmitWriteLineMacroTest()
        {
            using (TempFile file = TempFile.Create())
            {
                AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilderAndTypeBuilder(out TypeBuilder type1);
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
                ab.Save(file.Path);

                using (MetadataLoadContext mlc = new MetadataLoadContext(new CoreMetadataAssemblyResolver()))
                {
                    Assembly assemblyFromDisk = mlc.LoadFromAssemblyPath(file.Path);
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
        }

        [Fact]
        public void ReferenceStaticFieldAndMethodsInIL()
        {
            using (TempFile file = TempFile.Create())
            {
                AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilderAndTypeBuilder(out TypeBuilder tb);
                MethodBuilder methodMain = tb.DefineMethod("Main", MethodAttributes.Public, typeof(int), [typeof(int)]);
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
                ab.Save(file.Path);

                using (MetadataLoadContext mlc = new MetadataLoadContext(new CoreMetadataAssemblyResolver()))
                {
                    Assembly assemblyFromDisk = mlc.LoadFromAssemblyPath(file.Path);
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
        }

        [Fact]
        public void ReferenceConstructorInIL()
        {
            using (TempFile file = TempFile.Create())
            {
                AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilderAndTypeBuilder(out TypeBuilder tb);
                MethodBuilder methodBuilder = tb.DefineMethod("Method1", MethodAttributes.Public, typeof(Version), [typeof(int), typeof(int)]);
                ConstructorInfo ctor = typeof(Version).GetConstructor([typeof(int), typeof(int)]);

                ILGenerator il = methodBuilder.GetILGenerator();
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Ldarg_2);
                il.Emit(OpCodes.Newobj, ctor);
                il.Emit(OpCodes.Ret);
                tb.CreateType();
                ab.Save(file.Path);

                using (MetadataLoadContext mlc = new MetadataLoadContext(new CoreMetadataAssemblyResolver()))
                {
                    Assembly assemblyFromDisk = mlc.LoadFromAssemblyPath(file.Path);
                    Type typeFromDisk = assemblyFromDisk.Modules.First().GetType("MyType");
                    byte[]? bodyBytes = typeFromDisk.GetMethod("Method1").GetMethodBody().GetILAsByteArray();
                    Assert.Equal(OpCodes.Ldarg_1.Value, bodyBytes[0]);
                    Assert.Equal(OpCodes.Ldarg_2.Value, bodyBytes[1]);
                    Assert.Equal(OpCodes.Newobj.Value, bodyBytes[2]);
                    Assert.Equal(OpCodes.Ret.Value, bodyBytes[7]);
                }
            }
        }

        [Fact]
        public void ReferenceAType()
        {
            using (TempFile file = TempFile.Create())
            {
                AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilderAndTypeBuilder(out TypeBuilder tb);
                MethodBuilder method = tb.DefineMethod("meth1", MethodAttributes.Public | MethodAttributes.Static, typeof(bool), Type.EmptyTypes);
                ILGenerator ilGenerator = method.GetILGenerator();
                LocalBuilder lb0 = ilGenerator.DeclareLocal(typeof(ValueTuple));
                ilGenerator.Emit(OpCodes.Ldloca, lb0);
                ilGenerator.Emit(OpCodes.Initobj, typeof(ValueTuple));
                ilGenerator.Emit(OpCodes.Ldc_I4, 1);
                ilGenerator.Emit(OpCodes.Ret);
                tb.CreateType();
                ab.Save(file.Path);

                using (MetadataLoadContext mlc = new MetadataLoadContext(new CoreMetadataAssemblyResolver()))
                {
                    Assembly assemblyFromDisk = mlc.LoadFromAssemblyPath(file.Path);
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
        }

        [Fact]
        public void MemberReferenceExceptions()
        {
            AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilderAndTypeBuilder(out TypeBuilder type);
            MethodBuilder method = type.DefineMethod("Method1", MethodAttributes.Public);
            ILGenerator il = method.GetILGenerator();
            MethodInfo nullMethod = null;
            ConstructorInfo nullConstructor = null;
            FieldInfo nullField = null;
            Label[] nullArray = null;
            Type nullType = null;
            SignatureHelper signature = null;

            Assert.Throws<ArgumentNullException>("meth", () => il.Emit(OpCodes.Call, nullMethod));
            Assert.Throws<ArgumentNullException>("con", () => il.Emit(OpCodes.Callvirt, nullConstructor));
            Assert.Throws<ArgumentNullException>("field", () => il.Emit(OpCodes.Ldfld, nullField));
            Assert.Throws<ArgumentNullException>("labels", () => il.Emit(OpCodes.Switch, nullArray));
            Assert.Throws<ArgumentNullException>("cls", () => il.Emit(OpCodes.Switch, nullType));
            Assert.Throws<ArgumentNullException>("methodInfo", () => il.EmitCall(OpCodes.Call, nullMethod, null));
            // only OpCodes.Switch expected
            Assert.Throws<ArgumentException>("opcode", () => il.Emit(OpCodes.Call, new Label[0]));
            // only OpCodes.Call or .OpCodes.Callvirt or OpCodes.Newob expected
            Assert.Throws<ArgumentException>("opcode", () => il.Emit(OpCodes.Switch, typeof(object).GetConstructor(Type.EmptyTypes)));
            // Undefined label
            Assert.Throws<ArgumentException>(() => il.MarkLabel(new Label()));
            // only OpCodes.Call or OpCodes.Callvirt or OpCodes.Newob expected
            Assert.Throws<ArgumentException>("opcode", () => il.EmitCall(OpCodes.Ldfld, method, null));
            Assert.Throws<ArgumentNullException>("signature", () => il.Emit(OpCodes.Calli, signature));
            Assert.Throws<InvalidOperationException>(() => il.EmitCalli(OpCodes.Calli, CallingConventions.Standard, null, null, [typeof(string)]));
        }

        [Fact]
        public void SimpleTryCatchBlock()
        {
            using (TempFile file = TempFile.Create())
            {
                AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilderAndTypeBuilder(out TypeBuilder tb);
                MethodBuilder method = tb.DefineMethod("Method", MethodAttributes.Public | MethodAttributes.Static, typeof(float), [typeof(int), typeof(int)]);
                Type dBZException = typeof(DivideByZeroException);
                ILGenerator ilGenerator = method.GetILGenerator();
                LocalBuilder local = ilGenerator.DeclareLocal(typeof(float));
                MethodInfo getMaxStackMethod = GetMaxStackMethod();
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

                Assert.Equal(3, getMaxStackMethod.Invoke(ilGenerator, null));
                ab.Save(file.Path);

                using (MetadataLoadContext mlc = new MetadataLoadContext(new CoreMetadataAssemblyResolver()))
                {
                    Assembly assemblyFromDisk = mlc.LoadFromAssemblyPath(file.Path);
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
        }

        [Fact]
        public void TryMultipleCatchBlocks()
        {
            using (TempFile file = TempFile.Create())
            {
                AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilderAndTypeBuilder(out TypeBuilder tb);
                MethodBuilder method = tb.DefineMethod("Method", MethodAttributes.Public | MethodAttributes.Static, typeof(float), [typeof(int), typeof(int)]);
                Type dBZException = typeof(DivideByZeroException);
                Type exception = typeof(Exception);
                ILGenerator ilGenerator = method.GetILGenerator();
                FieldInfo maxStackField = GetMaxStackDepthAndCurrentStackDepthField(out FieldInfo currentStack);
                LocalBuilder local = ilGenerator.DeclareLocal(typeof(float));
                Label exBlock = ilGenerator.BeginExceptionBlock();
                ilGenerator.Emit(OpCodes.Ldarg_0);
                ilGenerator.Emit(OpCodes.Ldarg_1);
                ilGenerator.Emit(OpCodes.Div);
                ilGenerator.Emit(OpCodes.Stloc_0);
                Assert.Equal(2, maxStackField.GetValue(ilGenerator));
                ilGenerator.BeginCatchBlock(dBZException);
                Assert.Equal(1, currentStack.GetValue(ilGenerator));
                ilGenerator.EmitWriteLine("Error: division by zero");
                ilGenerator.Emit(OpCodes.Ldc_R4, 0.0f);
                ilGenerator.Emit(OpCodes.Stloc_0);
                Assert.Equal(2, maxStackField.GetValue(ilGenerator));
                ilGenerator.Emit(OpCodes.Pop); // pop the exception in the stack, else its gonna added to the _depthAdjustment
                ilGenerator.BeginCatchBlock(exception);
                Assert.Equal(1, currentStack.GetValue(ilGenerator));
                ilGenerator.EmitWriteLine("Error: generic Exception");
                ilGenerator.Emit(OpCodes.Ldc_R4, 0.0f);
                ilGenerator.Emit(OpCodes.Stloc_0);
                Assert.Equal(2, maxStackField.GetValue(ilGenerator));
                ilGenerator.Emit(OpCodes.Pop);
                ilGenerator.EndExceptionBlock();
                ilGenerator.Emit(OpCodes.Ldloc_0);
                ilGenerator.Emit(OpCodes.Ret);
                tb.CreateType();
                ab.Save(file.Path);

                Assert.Equal(2, maxStackField.GetValue(ilGenerator));
                MethodInfo getMaxStackMethod = GetMaxStackMethod();
                Assert.Equal(2, getMaxStackMethod.Invoke(ilGenerator, null));

                using (MetadataLoadContext mlc = new MetadataLoadContext(new CoreMetadataAssemblyResolver()))
                {
                    Assembly assemblyFromDisk = mlc.LoadFromAssemblyPath(file.Path);
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
                    Assert.Equal(OpCodes.Pop.Value, bodyBytes[25]);
                    Assert.Equal(OpCodes.Leave.Value, bodyBytes[26]);
                    Assert.Equal(OpCodes.Ldstr.Value, bodyBytes[31]); // "Error: division by zero"
                    Assert.Equal(OpCodes.Call.Value, bodyBytes[36]); // Calls Console.WriteLine
                    Assert.Equal(OpCodes.Ldc_R4.Value, bodyBytes[41]);
                    Assert.Equal(OpCodes.Stloc_0.Value, bodyBytes[46]);
                    Assert.Equal(OpCodes.Pop.Value, bodyBytes[47]);
                    Assert.Equal(OpCodes.Leave.Value, bodyBytes[48]);
                    Assert.Equal(OpCodes.Ldloc_0.Value, bodyBytes[53]);
                    Assert.Equal(OpCodes.Ret.Value, bodyBytes[54]);
                }
            }
        }

        [Fact]
        public void TryFilterCatchBlock()
        {
            using (TempFile file = TempFile.Create())
            {
                AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilderAndTypeBuilder(out TypeBuilder tb);
                MethodBuilder method = tb.DefineMethod("Method", MethodAttributes.Public | MethodAttributes.Static, typeof(float), [typeof(int), typeof(int)]);
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
                ab.Save(file.Path);

                MethodInfo getMaxStackMethod = GetMaxStackMethod();
                Assert.Equal(2, getMaxStackMethod.Invoke(ilGenerator, null));

                using (MetadataLoadContext mlc = new MetadataLoadContext(new CoreMetadataAssemblyResolver()))
                {
                    Assembly assemblyFromDisk = mlc.LoadFromAssemblyPath(file.Path);
                    Type typeFromDisk = assemblyFromDisk.Modules.First().GetType("MyType");
                    MethodBody body = typeFromDisk.GetMethod("Method").GetMethodBody();
                    Assert.Equal(2, body.ExceptionHandlingClauses.Count);
                    Assert.Equal(ExceptionHandlingClauseOptions.Filter, body.ExceptionHandlingClauses[0].Flags);
                    Assert.Equal(ExceptionHandlingClauseOptions.Clause, body.ExceptionHandlingClauses[1].Flags);
                    Assert.Equal(exception.FullName, body.ExceptionHandlingClauses[1].CatchType.FullName);
                }
            }
        }

        [Fact]
        public void TryCatchFilterCatchBlock()
        {
            using (TempFile file = TempFile.Create())
            {
                AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilderAndTypeBuilder(out TypeBuilder tb);
                MethodBuilder method = tb.DefineMethod("Method", MethodAttributes.Public | MethodAttributes.Static, typeof(float), [typeof(int), typeof(int)]);
                Type dBZException = typeof(DivideByZeroException);
                Type overflowException = typeof(OverflowException);
                Type exception = typeof(Exception);
                ILGenerator ilGenerator = method.GetILGenerator();
                FieldInfo maxStackField = GetMaxStackDepthAndCurrentStackDepthField(out FieldInfo _);
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
                Assert.Equal(2, maxStackField.GetValue(ilGenerator));
                ilGenerator.BeginCatchBlock(overflowException);
                ilGenerator.EmitWriteLine("Overflow Exception!");
                ilGenerator.ThrowException(overflowException);
                Assert.Equal(2, maxStackField.GetValue(ilGenerator));
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
                Assert.Equal(2, maxStackField.GetValue(ilGenerator));
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
                ab.Save(file.Path);

                MethodInfo getMaxStackMethod = GetMaxStackMethod();
                Assert.Equal(2, getMaxStackMethod.Invoke(ilGenerator, null));

                using (MetadataLoadContext mlc = new MetadataLoadContext(new CoreMetadataAssemblyResolver()))
                {
                    Assembly assemblyFromDisk = mlc.LoadFromAssemblyPath(file.Path);
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
        }

        [Fact]
        public void TryFinallyBlock()
        {
            using (TempFile file = TempFile.Create())
            {
                AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilderAndTypeBuilder(out TypeBuilder tb);
                MethodBuilder method = tb.DefineMethod("Method", MethodAttributes.Public | MethodAttributes.Static, typeof(float), [typeof(int), typeof(int)]);
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
                ab.Save(file.Path);

                MethodInfo getMaxStackMethod = GetMaxStackMethod();
                Assert.Equal(2, getMaxStackMethod.Invoke(ilGenerator, null));

                using (MetadataLoadContext mlc = new MetadataLoadContext(new CoreMetadataAssemblyResolver()))
                {
                    Assembly assemblyFromDisk = mlc.LoadFromAssemblyPath(file.Path);
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
        }

        [Fact]
        public void TryCatchFinallyBlock()
        {
            using (TempFile file = TempFile.Create())
            {
                AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilderAndTypeBuilder(out TypeBuilder tb);
                MethodBuilder method = tb.DefineMethod("Method", MethodAttributes.Public | MethodAttributes.Static, typeof(void), [typeof(int), typeof(int)]);
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
                ab.Save(file.Path);

                MethodInfo getMaxStackMethod = GetMaxStackMethod();
                Assert.Equal(2, getMaxStackMethod.Invoke(ilGenerator, null));

                using (MetadataLoadContext mlc = new MetadataLoadContext(new CoreMetadataAssemblyResolver()))
                {
                    Assembly assemblyFromDisk = mlc.LoadFromAssemblyPath(file.Path);
                    Type typeFromDisk = assemblyFromDisk.Modules.First().GetType("MyType");
                    MethodBody body = typeFromDisk.GetMethod("Method").GetMethodBody();
                    Assert.Equal(2, body.ExceptionHandlingClauses.Count);
                    Assert.Equal(ExceptionHandlingClauseOptions.Clause, body.ExceptionHandlingClauses[0].Flags);
                    Assert.Equal(ExceptionHandlingClauseOptions.Finally, body.ExceptionHandlingClauses[1].Flags);
                    Assert.Equal(exception.FullName, body.ExceptionHandlingClauses[0].CatchType.FullName);
                }
            }
        }

        [Fact]
        public void TryFilterCatchFinallyBlock()
        {
            using (TempFile file = TempFile.Create())
            {
                AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilderAndTypeBuilder(out TypeBuilder tb);
                MethodBuilder method = tb.DefineMethod("Method", MethodAttributes.Public | MethodAttributes.Static, typeof(int), [typeof(int), typeof(int)]);
                Type overflowEType = typeof(OverflowException);
                ConstructorInfo myConstructorInfo = overflowEType.GetConstructor([typeof(string)]);
                MethodInfo myExToStrMI = overflowEType.GetMethod("ToString");
                MethodInfo myWriteLineMI = typeof(Console).GetMethod("WriteLine", [typeof(string), typeof(object)]);
                ILGenerator ilGenerator = method.GetILGenerator();
                LocalBuilder myLocalBuilder1 = ilGenerator.DeclareLocal(typeof(int));
                LocalBuilder myLocalBuilder2 = ilGenerator.DeclareLocal(overflowEType);

                Label myFailedLabel = ilGenerator.DefineLabel();
                Label myEndOfMethodLabel = ilGenerator.DefineLabel();
                Label myLabel = ilGenerator.BeginExceptionBlock();
                ilGenerator.Emit(OpCodes.Ldarg_0);
                ilGenerator.Emit(OpCodes.Ldc_I4_S, 10);
                ilGenerator.Emit(OpCodes.Bgt_S, myFailedLabel);
                ilGenerator.Emit(OpCodes.Ldarg_1);
                ilGenerator.Emit(OpCodes.Ldc_I4_S, 10);
                ilGenerator.Emit(OpCodes.Bgt_S, myFailedLabel);
                ilGenerator.Emit(OpCodes.Ldarg_0);
                ilGenerator.Emit(OpCodes.Ldarg_1);
                ilGenerator.Emit(OpCodes.Add_Ovf_Un);
                ilGenerator.Emit(OpCodes.Stloc_S, myLocalBuilder1);
                ilGenerator.Emit(OpCodes.Br_S, myEndOfMethodLabel);
                ilGenerator.MarkLabel(myFailedLabel);
                ilGenerator.Emit(OpCodes.Ldstr, "Cannot accept values over 10 for add.");
                ilGenerator.Emit(OpCodes.Newobj, myConstructorInfo);
                ilGenerator.Emit(OpCodes.Stloc_S, myLocalBuilder2);
                ilGenerator.Emit(OpCodes.Ldloc_S, myLocalBuilder2);
                ilGenerator.Emit(OpCodes.Throw);
                ilGenerator.BeginExceptFilterBlock();
                ilGenerator.BeginCatchBlock(null);
                ilGenerator.Emit(OpCodes.Pop);
                ilGenerator.EmitWriteLine("Except filter block handled.");
                ilGenerator.BeginCatchBlock(overflowEType);
                ilGenerator.Emit(OpCodes.Stloc_S, myLocalBuilder2);
                ilGenerator.Emit(OpCodes.Ldstr, "{0}");
                ilGenerator.Emit(OpCodes.Ldloc_S, myLocalBuilder2);
                ilGenerator.EmitCall(OpCodes.Callvirt, myExToStrMI, null);
                ilGenerator.EmitCall(OpCodes.Call, myWriteLineMI, null);
                ilGenerator.Emit(OpCodes.Ldc_I4_M1);
                ilGenerator.Emit(OpCodes.Stloc_S, myLocalBuilder1);
                ilGenerator.BeginFinallyBlock();
                ilGenerator.EmitWriteLine("Finally block handled.");
                ilGenerator.EndExceptionBlock();
                ilGenerator.MarkLabel(myEndOfMethodLabel);
                ilGenerator.Emit(OpCodes.Ldloc_S, myLocalBuilder1);
                ilGenerator.Emit(OpCodes.Ret);
                tb.CreateType();
                ab.Save(file.Path);

                MethodInfo getMaxStackMethod = GetMaxStackMethod();
                Assert.Equal(2, getMaxStackMethod.Invoke(ilGenerator, null));

                using (MetadataLoadContext mlc = new MetadataLoadContext(new CoreMetadataAssemblyResolver()))
                {
                    Assembly assemblyFromDisk = mlc.LoadFromAssemblyPath(file.Path);
                    Type typeFromDisk = assemblyFromDisk.Modules.First().GetType("MyType");
                    MethodBody body = typeFromDisk.GetMethod("Method").GetMethodBody();
                    Assert.Equal(3, body.ExceptionHandlingClauses.Count);
                    Assert.Equal(ExceptionHandlingClauseOptions.Filter, body.ExceptionHandlingClauses[0].Flags);
                    Assert.Equal(ExceptionHandlingClauseOptions.Clause, body.ExceptionHandlingClauses[1].Flags);
                    Assert.Equal(ExceptionHandlingClauseOptions.Finally, body.ExceptionHandlingClauses[2].Flags);
                    Assert.Equal(overflowEType.FullName, body.ExceptionHandlingClauses[1].CatchType.FullName);
                }
            }
        }

        [Fact]
        public void TryFaultBlock()
        {
            using (TempFile file = TempFile.Create())
            {
                AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilderAndTypeBuilder(out TypeBuilder tb);
                MethodBuilder method = tb.DefineMethod("Method", MethodAttributes.Public | MethodAttributes.Static, typeof(float), [typeof(int), typeof(int)]);
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
                ab.Save(file.Path);

                MethodInfo getMaxStackMethod = GetMaxStackMethod();
                Assert.Equal(2, getMaxStackMethod.Invoke(ilGenerator, null));

                using (MetadataLoadContext mlc = new MetadataLoadContext(new CoreMetadataAssemblyResolver()))
                {
                    Assembly assemblyFromDisk = mlc.LoadFromAssemblyPath(file.Path);
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
        }

        [Fact]
        public void NestedTryCatchBlocks()
        {
            using (TempFile file = TempFile.Create())
            {
                AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilderAndTypeBuilder(out TypeBuilder tb);
                MethodBuilder method = tb.DefineMethod("Method", MethodAttributes.Public | MethodAttributes.Static, typeof(void), [typeof(int), typeof(int)]);
                Type exception = typeof(Exception);
                ILGenerator ilGenerator = method.GetILGenerator();
                MethodInfo getMaxStackMethod = GetMaxStackMethod();
                ilGenerator.BeginExceptionBlock();
                ilGenerator.Emit(OpCodes.Ldarg_0);
                ilGenerator.Emit(OpCodes.Ldarg_1);
                ilGenerator.Emit(OpCodes.Div);
                Assert.Equal(2, getMaxStackMethod.Invoke(ilGenerator, null));
                ilGenerator.BeginExceptionBlock();
                ilGenerator.EmitWriteLine("Try block nested in try");
                ilGenerator.Emit(OpCodes.Ldarg_0);
                ilGenerator.Emit(OpCodes.Ldarg_1);
                ilGenerator.Emit(OpCodes.Ldc_I4_4);
                ilGenerator.Emit(OpCodes.Add);
                ilGenerator.Emit(OpCodes.Add);
                Assert.Equal(3, getMaxStackMethod.Invoke(ilGenerator, null));
                ilGenerator.BeginCatchBlock(exception);
                ilGenerator.Emit(OpCodes.Pop);
                ilGenerator.EmitWriteLine("Catch block nested in try");
                ilGenerator.EndExceptionBlock();
                ilGenerator.EmitWriteLine("Outer try block ends");
                ilGenerator.BeginCatchBlock(exception);
                ilGenerator.Emit(OpCodes.Pop);
                ilGenerator.EmitWriteLine("Outer catch block starts");
                ilGenerator.BeginExceptionBlock();
                ilGenerator.EmitWriteLine("Try block nested in catch");
                ilGenerator.BeginCatchBlock(exception);
                ilGenerator.Emit(OpCodes.Ldarg_0);
                ilGenerator.Emit(OpCodes.Ldarg_1);
                ilGenerator.Emit(OpCodes.Ldc_I4_4);
                ilGenerator.Emit(OpCodes.Ldc_I4_4);
                ilGenerator.Emit(OpCodes.Add);
                ilGenerator.Emit(OpCodes.Add);
                ilGenerator.Emit(OpCodes.Add);
                ilGenerator.Emit(OpCodes.Pop);
                ilGenerator.Emit(OpCodes.Pop);
                ilGenerator.EmitWriteLine("Catch block nested in catch");
                Assert.Equal(5, getMaxStackMethod.Invoke(ilGenerator, null)); // 5 including the exception object
                ilGenerator.EndExceptionBlock();
                ilGenerator.EmitWriteLine("Outer catch block ends");
                ilGenerator.EndExceptionBlock();
                ilGenerator.Emit(OpCodes.Ret);
                tb.CreateType();
                ab.Save(file.Path);

                Assert.Equal(5, getMaxStackMethod.Invoke(ilGenerator, null));

                using (MetadataLoadContext mlc = new MetadataLoadContext(new CoreMetadataAssemblyResolver()))
                {
                    Assembly assemblyFromDisk = mlc.LoadFromAssemblyPath(file.Path);
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
        }

        [Fact]
        public void DeeperNestedTryCatchFilterFinallyBlocks()
        {
            using (TempFile file = TempFile.Create())
            {
                AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilderAndTypeBuilder(out TypeBuilder tb);
                MethodBuilder method = tb.DefineMethod("Method", MethodAttributes.Public | MethodAttributes.Static, typeof(int), [typeof(int), typeof(int)]);
                Type exception = typeof(Exception);
                ILGenerator ilGenerator = method.GetILGenerator();
                MethodInfo getMaxStackMethod = GetMaxStackMethod();
                LocalBuilder local = ilGenerator.DeclareLocal(typeof(int));
                ilGenerator.Emit(OpCodes.Ldc_I4_0);

                ilGenerator.BeginExceptionBlock();
                ilGenerator.Emit(OpCodes.Ldarg_0);
                ilGenerator.Emit(OpCodes.Ldarg_1);
                ilGenerator.Emit(OpCodes.Div);
                ilGenerator.Emit(OpCodes.Stloc_0);
                Assert.Equal(2, getMaxStackMethod.Invoke(ilGenerator, null));
                ilGenerator.BeginExceptionBlock();
                ilGenerator.EmitWriteLine("1st nested try block nested in try");
                Label myLabel = ilGenerator.BeginExceptionBlock();
                ilGenerator.EmitWriteLine("2nd nested try block starts");
                ilGenerator.Emit(OpCodes.Ldc_I4_3);
                ilGenerator.Emit(OpCodes.Stloc_0);
                Assert.Equal(2, getMaxStackMethod.Invoke(ilGenerator, null));
                ilGenerator.BeginExceptionBlock();
                ilGenerator.Emit(OpCodes.Ldc_I4_3);
                ilGenerator.Emit(OpCodes.Ldc_I4_3);
                ilGenerator.Emit(OpCodes.Ldc_I4_3);
                ilGenerator.Emit(OpCodes.Ldarg_0);
                ilGenerator.Emit(OpCodes.Add);
                ilGenerator.Emit(OpCodes.Add);
                ilGenerator.Emit(OpCodes.Add);
                ilGenerator.Emit(OpCodes.Stloc_0);
                ilGenerator.EmitWriteLine("3rd nested try block");
                Assert.Equal(4, getMaxStackMethod.Invoke(ilGenerator, null));
                ilGenerator.BeginFinallyBlock();
                ilGenerator.EmitWriteLine("3rd nested finally block");
                ilGenerator.EndExceptionBlock();

                ilGenerator.EmitWriteLine("2nd nested try block ends");
                ilGenerator.BeginExceptFilterBlock();
                ilGenerator.EmitWriteLine("2nd nested filter block starts.");
                Assert.Equal(4, getMaxStackMethod.Invoke(ilGenerator, null));
                ilGenerator.BeginCatchBlock(null);
                ilGenerator.Emit(OpCodes.Pop);
                ilGenerator.EmitWriteLine("2nd nested filter block handled.");
                ilGenerator.BeginCatchBlock(exception);
                ilGenerator.EmitWriteLine("2nd nested catch block handled.");
                ilGenerator.Emit(OpCodes.Ldc_I4_3);
                ilGenerator.Emit(OpCodes.Ldc_I4_3);
                ilGenerator.Emit(OpCodes.Ldc_I4_3);
                ilGenerator.Emit(OpCodes.Ldarg_0);
                ilGenerator.Emit(OpCodes.Add);
                ilGenerator.Emit(OpCodes.Add);
                ilGenerator.Emit(OpCodes.Add);
                ilGenerator.Emit(OpCodes.Stloc_0);
                Assert.Equal(5, getMaxStackMethod.Invoke(ilGenerator, null)); // including the exception object
                ilGenerator.Emit(OpCodes.Pop);
                ilGenerator.BeginFinallyBlock();
                ilGenerator.EmitWriteLine("2nd nested finally block handled.");
                ilGenerator.EndExceptionBlock();

                ilGenerator.BeginCatchBlock(exception);
                ilGenerator.Emit(OpCodes.Pop);
                ilGenerator.EmitWriteLine("Catch block nested in try");
                ilGenerator.EndExceptionBlock();

                ilGenerator.EmitWriteLine("Outer try block ends");
                ilGenerator.BeginCatchBlock(exception);
                ilGenerator.EmitWriteLine("Outer catch block starts");
                ilGenerator.Emit(OpCodes.Pop);
                ilGenerator.EmitWriteLine("Outer catch block ends");
                ilGenerator.EndExceptionBlock();

                ilGenerator.Emit(OpCodes.Stloc_0);
                ilGenerator.Emit(OpCodes.Ldloc_0);
                ilGenerator.Emit(OpCodes.Ret);
                tb.CreateType();
                ab.Save(file.Path);

                Assert.Equal(5, getMaxStackMethod.Invoke(ilGenerator, null));

                using (MetadataLoadContext mlc = new MetadataLoadContext(new CoreMetadataAssemblyResolver()))
                {
                    Assembly assemblyFromDisk = mlc.LoadFromAssemblyPath(file.Path);
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
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int Int32SumStdCall(int a, int b);

        private static int Int32Sum(int a, int b) => a + b;

        [Fact]
        public void EmitCalliBlittable()
        {
            int a = 1, b = 1, result = 2;
            using (TempFile file = TempFile.Create())
            {
                AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilder(new AssemblyName("EmitCalliBlittable"));
                TypeBuilder tb = ab.DefineDynamicModule("MyModule").DefineType("MyType", TypeAttributes.Public | TypeAttributes.Class);
                Type returnType = typeof(int);
                MethodBuilder methodBuilder = tb.DefineMethod("F", MethodAttributes.Public | MethodAttributes.Static, returnType, [typeof(IntPtr), typeof(int), typeof(int)]);
                methodBuilder.SetImplementationFlags(MethodImplAttributes.NoInlining);
                ILGenerator il = methodBuilder.GetILGenerator();
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Ldarg_2);
                il.Emit(OpCodes.Ldarg_0);
                il.EmitCalli(OpCodes.Calli, CallingConvention.StdCall, returnType, [typeof(int), typeof(int)]);
                il.Emit(OpCodes.Ret);
                tb.CreateType();
                ab.Save(file.Path);

                TestAssemblyLoadContext tlc = new TestAssemblyLoadContext();
                Assembly assemblyFromDisk = tlc.LoadFromAssemblyPath(file.Path);
                Type typeFromDisk = assemblyFromDisk.GetType("MyType");
                var del = new Int32SumStdCall(Int32Sum);
                IntPtr funcPtr = Marshal.GetFunctionPointerForDelegate(del);
                object resultValue = typeFromDisk.GetMethod("F", BindingFlags.Public | BindingFlags.Static).Invoke(null, [funcPtr, a, b]);
                GC.KeepAlive(del);

                Assert.IsType(returnType, resultValue);
                Assert.Equal(result, resultValue);
                tlc.Unload();
            }
        }

        [Fact]
        public void EmitCalliManagedBlittable()
        {
            int a = 1, b = 1, result = 2;
            using (TempFile file = TempFile.Create())
            {
                AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilder(new AssemblyName("EmitCalliManagedBlittable"));
                TypeBuilder tb = ab.DefineDynamicModule("MyModule").DefineType("MyType", TypeAttributes.Public | TypeAttributes.Class);
                Type returnType = typeof(int);
                MethodBuilder methodBuilder = tb.DefineMethod("F", MethodAttributes.Public | MethodAttributes.Static, returnType, [typeof(IntPtr), typeof(int), typeof(int)]);
                methodBuilder.SetImplementationFlags(MethodImplAttributes.NoInlining);
                MethodInfo method = typeof(AssemblySaveILGeneratorTests).GetMethod(nameof(Int32Sum), BindingFlags.NonPublic | BindingFlags.Static)!;
                IntPtr funcPtr = method.MethodHandle.GetFunctionPointer();
                ILGenerator il = methodBuilder.GetILGenerator();
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Ldarg_2);
                il.Emit(OpCodes.Ldarg_0);
                il.EmitCalli(OpCodes.Calli, CallingConventions.Standard, returnType, [typeof(int), typeof(int)], null);
                il.Emit(OpCodes.Ret);
                tb.CreateType();
                ab.Save(file.Path);

                TestAssemblyLoadContext tlc = new TestAssemblyLoadContext();
                Assembly assemblyFromDisk = tlc.LoadFromAssemblyPath(file.Path);
                Type typeFromDisk = assemblyFromDisk.GetType("MyType");
                object resultValue = typeFromDisk.GetMethod("F", BindingFlags.Public | BindingFlags.Static).Invoke(null, [funcPtr, a, b]);

                Assert.IsType(returnType, resultValue);
                Assert.Equal(result, resultValue);
                tlc.Unload();
            }
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate string StringReverseCdecl(string a);

        private static string StringReverse(string a) => string.Join("", a.Reverse());

        [Fact]
        public void EmitCalliNonBlittable()
        {
                string input = "Test string!", result = "!gnirts tseT";
            using (TempFile file = TempFile.Create())
            {
                AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilder(new AssemblyName("EmitCalliNonBlittable"));
                TypeBuilder tb = ab.DefineDynamicModule("MyModule").DefineType("MyType", TypeAttributes.Public | TypeAttributes.Class);
                Type returnType = typeof(string);
                MethodBuilder methodBuilder = tb.DefineMethod("F", MethodAttributes.Public | MethodAttributes.Static, returnType, [typeof(IntPtr), typeof(string)]);
                methodBuilder.SetImplementationFlags(MethodImplAttributes.NoInlining);
                ILGenerator il = methodBuilder.GetILGenerator();
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Ldarg_0);
                il.EmitCalli(OpCodes.Calli, CallingConvention.Cdecl, returnType, [typeof(string)]);
                il.Emit(OpCodes.Ret);
                tb.CreateType();
                ab.Save(file.Path);

                TestAssemblyLoadContext tlc = new TestAssemblyLoadContext();
                Assembly assemblyFromDisk = tlc.LoadFromAssemblyPath(file.Path);
                Type typeFromDisk = assemblyFromDisk.GetType("MyType");
                var del = new StringReverseCdecl(StringReverse);
                IntPtr funcPtr = Marshal.GetFunctionPointerForDelegate(del);
                object resultValue = typeFromDisk.GetMethod("F", BindingFlags.Public | BindingFlags.Static).Invoke(null, [funcPtr, input]);
                GC.KeepAlive(del);

                Assert.IsType(returnType, resultValue);
                Assert.Equal(result, resultValue);
                tlc.Unload();
            }
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/96389", TestRuntimes.Mono)]
        public void EmitCall_VarArgsMethodInIL()
        {
            using (TempFile file = TempFile.Create())
            {
                AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilderAndTypeBuilder(out TypeBuilder tb);
                MethodBuilder mb1 = tb.DefineMethod("VarArgMethod", MethodAttributes.Public | MethodAttributes.Static, CallingConventions.VarArgs, null, [typeof(string)]);
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
                MethodBuilder mb2 = tb.DefineMethod("CallVarArgMethod", MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard);
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
                ab.Save(file.Path);

                using (MetadataLoadContext mlc = new MetadataLoadContext(new CoreMetadataAssemblyResolver()))
                {
                    Assembly assemblyFromDisk = mlc.LoadFromAssemblyPath(file.Path);
                    Type typeFromDisk = assemblyFromDisk.Modules.First().GetType("MyType");
                    MethodInfo varArgMethodFromDisk = typeFromDisk.GetMethod("VarArgMethod");
                    Assert.Equal(CallingConventions.VarArgs, varArgMethodFromDisk.CallingConvention);
                    ParameterInfo[] parameters = varArgMethodFromDisk.GetParameters();
                    Assert.Equal(1, parameters.Length); // TODO: how to get the vararg parameter?
                    IList<LocalVariableInfo> locals = varArgMethodFromDisk.GetMethodBody().LocalVariables;
                    Assert.Equal(2, locals.Count);
                    Assert.Equal(typeof(ArgIterator).FullName, locals[0].LocalType.FullName);
                    Assert.Equal(typeof(bool).FullName, locals[1].LocalType.FullName);

                    byte[] callingMethodBody = typeFromDisk.GetMethod("CallVarArgMethod").GetMethodBody().GetILAsByteArray();
                    Assert.Equal(OpCodes.Ldstr.Value, callingMethodBody[0]);
                    Assert.Equal(OpCodes.Ldstr.Value, callingMethodBody[5]);
                    Assert.Equal(OpCodes.Ldc_I4.Value, callingMethodBody[10]);
                    Assert.Equal(OpCodes.Call.Value, callingMethodBody[15]);
                }
            }
        }

        private static FieldInfo GetDepthAdjustmentField()
        {
            Type ilgType = Type.GetType("System.Reflection.Emit.ILGeneratorImpl, System.Reflection.Emit", throwOnError: true)!;
            return ilgType.GetField("_depthAdjustment", BindingFlags.NonPublic | BindingFlags.Instance);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/96389", TestRuntimes.Mono)]
        public void Emit_CallBySignature()
        {
            using (TempFile file = TempFile.Create())
            {
                AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilderAndTypeBuilder(out TypeBuilder tb);
                MethodBuilder mb1 = tb.DefineMethod("VarArgMethod", MethodAttributes.Public | MethodAttributes.Static, CallingConventions.VarArgs, null, [typeof(string)]);
                ILGenerator il1 = mb1.GetILGenerator();
                FieldInfo maxStack = GetMaxStackDepthAndCurrentStackDepthField(out FieldInfo currentStack);
                FieldInfo depthAdjustment = GetDepthAdjustmentField();
                LocalBuilder locAi = il1.DeclareLocal(typeof(ArgIterator));
                LocalBuilder locNext = il1.DeclareLocal(typeof(bool));
                Label labelCheckCondition = il1.DefineLabel();
                Label labelNext = il1.DefineLabel();
                il1.Emit(OpCodes.Ldarg_0);
                Assert.Equal(1, currentStack.GetValue(il1));
                il1.Emit(OpCodes.Call, typeof(Console).GetMethod("Write", [typeof(string)]));
                Assert.Equal(0, currentStack.GetValue(il1));
                il1.Emit(OpCodes.Ldloca_S, locAi);
                il1.Emit(OpCodes.Arglist);
                Assert.Equal(2, currentStack.GetValue(il1));
                il1.Emit(OpCodes.Call, typeof(ArgIterator).GetConstructor([typeof(RuntimeArgumentHandle)]));
                Assert.Equal(2, currentStack.GetValue(il1));
                Assert.Equal(2, maxStack.GetValue(il1));
                il1.Emit(OpCodes.Br_S, labelCheckCondition); // uncleared currentStack value 2 will be kept in the LabelInfo._startDepth
                il1.MarkLabel(labelNext);
                Assert.Equal(0, currentStack.GetValue(il1));
                il1.Emit(OpCodes.Ldloca_S, locAi);
                il1.Emit(OpCodes.Call, typeof(ArgIterator).GetMethod("GetNextArg", Type.EmptyTypes));
                il1.Emit(OpCodes.Call, typeof(TypedReference).GetMethod("ToObject"));
                il1.Emit(OpCodes.Call, typeof(Console).GetMethod("Write", [typeof(object)]));
                Assert.Equal(0, currentStack.GetValue(il1));
                il1.MarkLabel(labelCheckCondition);
                Assert.Equal(2, currentStack.GetValue(il1)); // LabelInfo._startDepth sets the currentStack
                Assert.Equal(2, maxStack.GetValue(il1));
                il1.Emit(OpCodes.Ldloca_S, locAi);
                Assert.Equal(3, currentStack.GetValue(il1));
                il1.Emit(OpCodes.Call, typeof(ArgIterator).GetMethod("GetRemainingCount"));
                Assert.Equal(3, currentStack.GetValue(il1));
                il1.Emit(OpCodes.Ldc_I4_0);
                Assert.Equal(4, currentStack.GetValue(il1));
                il1.Emit(OpCodes.Cgt);
                Assert.Equal(3, currentStack.GetValue(il1));
                il1.Emit(OpCodes.Stloc_1);
                Assert.Equal(2, currentStack.GetValue(il1));
                il1.Emit(OpCodes.Ldloc_1);
                Assert.Equal(3, currentStack.GetValue(il1));
                Assert.Equal(4, maxStack.GetValue(il1));
                Assert.Equal(0, depthAdjustment.GetValue(il1));
                il1.Emit(OpCodes.Brtrue_S, labelNext); // Backward branching, sets the adjustment to 2
                Assert.Equal(2, depthAdjustment.GetValue(il1));
                il1.Emit(OpCodes.Ret);

                Assert.Equal(0, currentStack.GetValue(il1));
                Assert.Equal(4, maxStack.GetValue(il1));
                MethodInfo getMaxStackMethod = GetMaxStackMethod();
                Assert.Equal(4 + 2, getMaxStackMethod.Invoke(il1, null));

                MethodBuilder mb2 = tb.DefineMethod("CallingMethod", MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard);
                ILGenerator il2 = mb2.GetILGenerator();
                il2.Emit(OpCodes.Ldstr, "Hello ");
                il2.Emit(OpCodes.Ldstr, "world ");
                il2.Emit(OpCodes.Ldc_I4, 2024);
                il2.Emit(OpCodes.Ldftn, mb1);
                Assert.Equal(4, currentStack.GetValue(il2));
                Assert.Equal(4, maxStack.GetValue(il2));
                Assert.Equal(0, depthAdjustment.GetValue(il2));
                SignatureHelper signature = SignatureHelper.GetMethodSigHelper(CallingConventions.VarArgs, typeof(void));
                signature.AddArgument(typeof(string));
                signature.AddSentinel();
                signature.AddArgument(typeof(string));
                signature.AddArgument(typeof(int));
                il2.Emit(OpCodes.Calli, signature);
                Assert.Equal(0, currentStack.GetValue(il2));
                Assert.Equal(4, maxStack.GetValue(il2));
                il2.Emit(OpCodes.Ret);
                Assert.Equal(0, depthAdjustment.GetValue(il2));
                Assert.Equal(4, getMaxStackMethod.Invoke(il2, null));
                Type type = tb.CreateType();
                ab.Save(file.Path);

                using (MetadataLoadContext mlc = new MetadataLoadContext(new CoreMetadataAssemblyResolver()))
                {
                    Assembly assemblyFromDisk = mlc.LoadFromAssemblyPath(file.Path);
                    Type typeFromDisk = assemblyFromDisk.Modules.First().GetType("MyType");
                    Assert.Equal(CallingConventions.VarArgs, typeFromDisk.GetMethod("VarArgMethod").CallingConvention);

                    byte[] callingMethodBody = typeFromDisk.GetMethod("CallingMethod").GetMethodBody().GetILAsByteArray();
                    Assert.Equal(OpCodes.Ldstr.Value, callingMethodBody[0]);
                    Assert.Equal(OpCodes.Ldstr.Value, callingMethodBody[5]);
                    Assert.Equal(OpCodes.Ldc_I4.Value, callingMethodBody[10]);
                    Assert.Equal(0xFE, callingMethodBody[15]); // Ldftn = 0xfe06
                    Assert.Equal(0x06, callingMethodBody[16]);
                    Assert.Equal(OpCodes.Calli.Value, callingMethodBody[21]);
                }
            }
        }

        [Fact]
        public void MaxStackOverflowTest()
        {
            GetCode(1 << 5);

            // Previously this threw because the computed stack depth was 2^16 + 1, which is 1 mod 2^16
            // and 1 is too small.
            GetCode(1 << 14);

            /// <summary>
            /// The <paramref name="num"/> parameter is the number of basic blocks. Each has a max stack
            /// depth of four. There is one final basic block with max stack of one. The ILGenerator
            /// erroneously adds these, so the final value can overflow 2^16. When that result mod 2^16
            /// is less than required, the CLR throws an <see cref="InvalidProgramException"/>.
            /// </summary>
            static void GetCode(int num)
            {
                AssemblySaveTools.PopulateAssemblyBuilderAndTypeBuilder(out TypeBuilder type);
                MethodBuilder method = type.DefineMethod("meth1", MethodAttributes.Public | MethodAttributes.Static, typeof(int), Type.EmptyTypes);
                var ilg = method.GetILGenerator();

                var loc = ilg.DeclareLocal(typeof(int));
                ilg.Emit(OpCodes.Ldc_I4_0);
                ilg.Emit(OpCodes.Stloc, loc);

                for (int i = 0; i < num; i++)
                {
                    ilg.Emit(OpCodes.Ldloc, loc);
                    ilg.Emit(OpCodes.Ldc_I4_1);
                    ilg.Emit(OpCodes.Ldc_I4_1);
                    ilg.Emit(OpCodes.Ldc_I4_2);
                    ilg.Emit(OpCodes.Add);
                    ilg.Emit(OpCodes.Add);
                    ilg.Emit(OpCodes.Add);
                    ilg.Emit(OpCodes.Stloc, loc);

                    // Unconditional jump to next block.
                    var labNext = ilg.DefineLabel();
                    ilg.Emit(OpCodes.Br, labNext);
                    ilg.MarkLabel(labNext);
                }

                ilg.Emit(OpCodes.Ldloc, loc);
                ilg.Emit(OpCodes.Ret);
                type.CreateTypeInfo();

                MethodInfo getMaxStackMethod = GetMaxStackMethod();
                Assert.Equal(4, getMaxStackMethod.Invoke(ilg, null));
            }
        }

        [Fact]
        public void MaxStackNonEmptyForward()
        {
            // This test uses forward branches to "new" basic blocks where the stack depth
            // at the branch location is non-empty.

            GetCode(1 << 0);
            GetCode(1 << 1);
            GetCode(1 << 5);

            static void GetCode(int num)
            {
                AssemblySaveTools.PopulateAssemblyBuilderAndTypeBuilder(out TypeBuilder type);
                MethodBuilder method = type.DefineMethod("meth1", MethodAttributes.Public | MethodAttributes.Static, typeof(int), null);
                var ilg = method.GetILGenerator();

                ilg.Emit(OpCodes.Ldc_I4_0);
                for (int i = 0; i < num; i++)
                {
                    ilg.Emit(OpCodes.Ldc_I4_1);
                    ilg.Emit(OpCodes.Ldc_I4_1);
                    ilg.Emit(OpCodes.Ldc_I4_1);
                    ilg.Emit(OpCodes.Ldc_I4_1);
                    ilg.Emit(OpCodes.Add);
                    ilg.Emit(OpCodes.Add);

                    // Unconditional jump to next block.
                    var labNext = ilg.DefineLabel();
                    ilg.Emit(OpCodes.Br, labNext);
                    ilg.MarkLabel(labNext);
                }

                // Each block leaves two values on the stack. Add them into the previous value.
                for (int i = 0; i < num; i++)
                {
                    ilg.Emit(OpCodes.Add);
                    ilg.Emit(OpCodes.Add);
                }

                ilg.Emit(OpCodes.Ret);
                type.CreateTypeInfo();

                MethodInfo getMaxStackMethod = GetMaxStackMethod();
                Assert.Equal(2 * num + 3, getMaxStackMethod.Invoke(ilg, null));
            }
        }

        [Fact]
        public void MaxStackNonEmptyBackward()
        {
            // This test uses backward branches to "new" basic blocks where the stack depth
            // at the branch location is non-empty.

            GetCode(1 << 1);
            GetCode(1 << 2); // n = 4 expected 16 was 12
            GetCode(1 << 3); // n = 8 exp 32 was 20
            GetCode(1 << 4); // n = 16 exp 64 was 36
            GetCode(1 << 5); // n = 32 exp 128 was 68

            static void GetCode(int num)
            {
                AssemblySaveTools.PopulateAssemblyBuilderAndTypeBuilder(out TypeBuilder type);
                MethodBuilder method = type.DefineMethod("meth1", MethodAttributes.Public | MethodAttributes.Static, typeof(int), Type.EmptyTypes);
                var ilg = method.GetILGenerator();

                var labels = new Label[num + 1];
                for (int i = 0; i <= num; i++)
                    labels[i] = ilg.DefineLabel();

                ilg.Emit(OpCodes.Ldc_I4_0);
                ilg.Emit(OpCodes.Br, labels[0]);

                for (int i = num; --i >= 0;)
                {
                    ilg.MarkLabel(labels[i]);

                    ilg.Emit(OpCodes.Ldc_I4_1);
                    ilg.Emit(OpCodes.Ldc_I4_1);
                    ilg.Emit(OpCodes.Ldc_I4_1);
                    ilg.Emit(OpCodes.Ldc_I4_1);
                    ilg.Emit(OpCodes.Add);
                    ilg.Emit(OpCodes.Add);

                    // Unconditional jump to "next" block (which is really before this code).
                    ilg.Emit(OpCodes.Br, labels[i + 1]);
                }

                ilg.MarkLabel(labels[num]);

                // Each block leaves two values on the stack. Add them into the previous value.
                for (int i = 0; i < num; i++)
                {
                    ilg.Emit(OpCodes.Add);
                    ilg.Emit(OpCodes.Add);
                }

                ilg.Emit(OpCodes.Ret);

                type.CreateTypeInfo();
                MethodInfo getMaxStackMethod = GetMaxStackMethod();
                // The expected formula comes from (num - 1) * 2 + 1 + 5,
                // ILs within the loop produces 2 adjustments for each, except 1st one
                // last loop produce 2 + 1 adjustment because of the 1st instruction that loads 0
                // the max stack for 1st loop is 5, 4 for the all other loops
                Assert.Equal(num * 2 + 4, getMaxStackMethod.Invoke(ilg, null));
            }
        }

        [Fact]
        public void AmbiguousDepth()
        {
            GetCode();

            static void GetCode()
            {
                AssemblySaveTools.PopulateAssemblyBuilderAndTypeBuilder(out TypeBuilder type);
                MethodBuilder method = type.DefineMethod("meth1", MethodAttributes.Public | MethodAttributes.Static, typeof(int), [typeof(bool)]);
                var ilg = method.GetILGenerator();

                // The label is targeted with stack depth zero.
                var lab = ilg.DefineLabel();
                ilg.Emit(OpCodes.Ldarg_0);
                ilg.Emit(OpCodes.Brfalse, lab);

                // The label is marked with a larger stack depth, one. This IL is invalid.
                ilg.Emit(OpCodes.Ldc_I4_1);
                ilg.MarkLabel(lab);

                ilg.Emit(OpCodes.Ldc_I4_1);
                ilg.Emit(OpCodes.Add);
                ilg.Emit(OpCodes.Ret);
                type.CreateTypeInfo();

                MethodInfo getMaxStackMethod = GetMaxStackMethod();
                // Observed depth of 2, with "adjustment" of 1.
                Assert.Equal(2 + 1, getMaxStackMethod.Invoke(ilg, null));
            }
        }

        [Fact]
        public void UnreachableDepth()
        {
            GetCode();

            static void GetCode()
            {
                AssemblySaveTools.PopulateAssemblyBuilderAndTypeBuilder(out TypeBuilder type);
                MethodBuilder method = type.DefineMethod("meth1", MethodAttributes.Public | MethodAttributes.Static, typeof(int), Type.EmptyTypes);
                var ilg = method.GetILGenerator();

                var lab = ilg.DefineLabel();

                ilg.Emit(OpCodes.Ldc_I4_1);
                ilg.Emit(OpCodes.Ldc_I4_1);
                ilg.Emit(OpCodes.Br, lab);

                // Unreachable.
                ilg.Emit(OpCodes.Ldarg_0);

                // Depth 
                ilg.MarkLabel(lab);
                ilg.Emit(OpCodes.Add);
                ilg.Emit(OpCodes.Ret);

                type.CreateTypeInfo();
                MethodInfo getMaxStackMethod = GetMaxStackMethod();
                Assert.Equal(2, getMaxStackMethod.Invoke(ilg, null));
            }
        }

        [Fact]
        public void SimpleForLoopTest()
        {
            using (TempFile file = TempFile.Create())
            {
                AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilderAndTypeBuilder(out TypeBuilder tb);
                MethodBuilder mb2 = tb.DefineMethod("SumMethod", MethodAttributes.Public | MethodAttributes.Static, typeof(int), [typeof(int)]);
                ILGenerator il = mb2.GetILGenerator();
                LocalBuilder sum = il.DeclareLocal(typeof(int));
                LocalBuilder i = il.DeclareLocal(typeof(int));
                Label loopEnd = il.DefineLabel();
                Label loopStart = il.DefineLabel();
                il.Emit(OpCodes.Ldc_I4_0);
                il.Emit(OpCodes.Stloc_0);
                il.Emit(OpCodes.Ldc_I4_1);
                il.Emit(OpCodes.Stloc_1);
                il.MarkLabel(loopStart);
                il.Emit(OpCodes.Ldloc_1);
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Bgt, loopEnd);
                il.Emit(OpCodes.Ldloc_0);
                il.Emit(OpCodes.Ldloc_1);
                il.Emit(OpCodes.Add);
                il.Emit(OpCodes.Stloc_0);
                il.Emit(OpCodes.Ldloc_1);
                il.Emit(OpCodes.Ldc_I4_1);
                il.Emit(OpCodes.Add);
                il.Emit(OpCodes.Stloc_1);
                il.Emit(OpCodes.Br, loopStart);
                il.MarkLabel(loopEnd);
                il.Emit(OpCodes.Ldloc_0);
                il.Emit(OpCodes.Ret);
                tb.CreateType();
                ab.Save(file.Path);

                MethodInfo getMaxStackMethod = GetMaxStackMethod();
                Assert.Equal(2, getMaxStackMethod.Invoke(il, null));

                TestAssemblyLoadContext tlc = new TestAssemblyLoadContext();
                Assembly assemblyFromDisk = tlc.LoadFromAssemblyPath(file.Path);
                Type typeFromDisk = assemblyFromDisk.GetType("MyType");
                MethodInfo sumMethodFromDisk = typeFromDisk.GetMethod("SumMethod");
                Assert.Equal(55, sumMethodFromDisk.Invoke(null, [10]));
                tlc.Unload();
            }
        }

        [Fact]
        public void RecursiveSumTest()
        {
            using (TempFile file = TempFile.Create())
            {
                AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilder(new AssemblyName("RecursiveSumTest"));
                TypeBuilder tb = ab.DefineDynamicModule("MyModule").DefineType("MyType", TypeAttributes.Public | TypeAttributes.Class);
                MethodBuilder mb2 = tb.DefineMethod("RecursiveMethod", MethodAttributes.Public | MethodAttributes.Static, typeof(int), [typeof(int)]);
                ILGenerator il = mb2.GetILGenerator();
                Label loopEnd = il.DefineLabel();
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldc_I4_0);
                il.Emit(OpCodes.Ble, loopEnd); // if (value1 <= value2)
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldc_I4_1);
                il.Emit(OpCodes.Sub);
                il.Emit(OpCodes.Call, mb2);
                il.Emit(OpCodes.Add);
                il.Emit(OpCodes.Ret);
                il.MarkLabel(loopEnd);
                il.Emit(OpCodes.Ldc_I4_0);
                il.Emit(OpCodes.Ret);
                tb.CreateType();
                ab.Save(file.Path);

                MethodInfo getMaxStackMethod = GetMaxStackMethod();
                Assert.Equal(3, getMaxStackMethod.Invoke(il, null));

                TestAssemblyLoadContext tlc = new TestAssemblyLoadContext();
                Assembly assemblyFromDisk = tlc.LoadFromAssemblyPath(file.Path);
                Type typeFromDisk = assemblyFromDisk.GetType("MyType");
                MethodInfo recursiveMethodFromDisk = typeFromDisk.GetMethod("RecursiveMethod");
                Assert.NotNull(recursiveMethodFromDisk);
                Assert.Equal(55, recursiveMethodFromDisk.Invoke(null, [10]));
                tlc.Unload();
            }
        }

        [Fact]
        public void CallOpenGenericMembersFromConstructedGenericType()
        {
            using (TempFile file = TempFile.Create())
            {
                AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilderAndTypeBuilder(out TypeBuilder type);
                MethodBuilder method = type.DefineMethod("M1", MethodAttributes.Public, typeof(string), null);

                ILGenerator ilGenerator = method.GetILGenerator();
                LocalBuilder span = ilGenerator.DeclareLocal(typeof(ReadOnlySpan<char>));
                LocalBuilder str = ilGenerator.DeclareLocal(typeof(string));

                ilGenerator.Emit(OpCodes.Ldstr, "hello");
                ilGenerator.Emit(OpCodes.Call, typeof(MemoryExtensions).GetMethod("AsSpan", [typeof(string)])!);
                ilGenerator.Emit(OpCodes.Stloc, span);
                ilGenerator.Emit(OpCodes.Ldloca_S, span);
                ilGenerator.Emit(OpCodes.Ldc_I4_1);
                ilGenerator.Emit(OpCodes.Call, typeof(ReadOnlySpan<char>).GetMethod("Slice", [typeof(int)])!);
                ilGenerator.Emit(OpCodes.Stloc, span);
                ilGenerator.Emit(OpCodes.Ldloca_S, span);
                ilGenerator.Emit(OpCodes.Constrained, typeof(ReadOnlySpan<char>));
                ilGenerator.Emit(OpCodes.Callvirt, typeof(object).GetMethod("ToString"));
                ilGenerator.Emit(OpCodes.Ret);

                type.CreateType();
                ab.Save(file.Path);

                TestAssemblyLoadContext tlc = new TestAssemblyLoadContext();
                Type typeFromDisk = tlc.LoadFromAssemblyPath(file.Path).GetType("MyType");
                string result = (string)typeFromDisk.GetMethod("M1").Invoke(Activator.CreateInstance(typeFromDisk), null);
                Assert.Equal("ello", result);
                tlc.Unload();
            }
        }

        [Fact]
        public void ReferenceMethodsOfDictionaryFieldInGenericTypeWorks()
        {
            using (TempFile file = TempFile.Create())
            {
                AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilderAndTypeBuilder(out TypeBuilder type);
                TypeBuilder tb = ab.GetDynamicModule("MyModule").DefineType("EnumNameCache", TypeAttributes.NotPublic);
                GenericTypeParameterBuilder[] param = tb.DefineGenericParameters(["TEnum"]);
                Type fieldType = typeof(Dictionary<,>).MakeGenericType(param[0], typeof(string));
                FieldBuilder field = tb.DefineField("Cache", fieldType, FieldAttributes.Public | FieldAttributes.Static);
                ILGenerator staticCtorIL = tb.DefineTypeInitializer().GetILGenerator();
                staticCtorIL.Emit(OpCodes.Newobj, TypeBuilder.GetConstructor(
                    typeof(Dictionary<,>).MakeGenericType(param[0], typeof(string)), typeof(Dictionary<,>).GetConstructor(Type.EmptyTypes)));
                staticCtorIL.Emit(OpCodes.Stsfld, field);
                staticCtorIL.Emit(OpCodes.Ret);

                MethodBuilder method = type.DefineMethod("Append", MethodAttributes.Public, typeof(string), null);
                GenericTypeParameterBuilder[] methParam = method.DefineGenericParameters(["T"]);
                method.SetParameters(methParam[0], typeof(string));
                Type typeOfT = tb.MakeGenericType(methParam);
                FieldInfo fieldOfT = TypeBuilder.GetField(typeOfT, field);
                ILGenerator ilGenerator = method.GetILGenerator();
                LocalBuilder str = ilGenerator.DeclareLocal(typeof(string));
                Label labelFalse = ilGenerator.DefineLabel();
                ilGenerator.Emit(OpCodes.Ldsfld, fieldOfT);
                ilGenerator.Emit(OpCodes.Ldarg_1);
                ilGenerator.Emit(OpCodes.Ldloca_S, 0);
                ilGenerator.Emit(OpCodes.Callvirt, TypeBuilder.GetMethod(
                    typeof(Dictionary<,>).MakeGenericType(methParam[0], typeof(string)), typeof(Dictionary<,>).GetMethod("TryGetValue")));
                ilGenerator.Emit(OpCodes.Brfalse_S, labelFalse);
                ilGenerator.Emit(OpCodes.Ldloc_0);
                ilGenerator.Emit(OpCodes.Ret);
                ilGenerator.MarkLabel(labelFalse);
                ilGenerator.Emit(OpCodes.Ldsfld, fieldOfT);
                ilGenerator.Emit(OpCodes.Ldarg_1);
                ilGenerator.Emit(OpCodes.Ldarg_2);
                ilGenerator.Emit(OpCodes.Callvirt, TypeBuilder.GetMethod(
                    typeof(Dictionary<,>).MakeGenericType(methParam[0], typeof(string)), typeof(Dictionary<,>).GetMethod("Add")));
                ilGenerator.Emit(OpCodes.Ldstr, "Added");
                ilGenerator.Emit(OpCodes.Ret);

                type.CreateType();
                tb.CreateType();
                ab.Save(file.Path);

                TestAssemblyLoadContext tlc = new TestAssemblyLoadContext();
                Type typeFromDisk = tlc.LoadFromAssemblyPath(file.Path).GetType("MyType");
                MethodInfo methodFromDisk = typeFromDisk.GetMethod("Append");
                MethodInfo genericMethod = methodFromDisk.MakeGenericMethod(typeof(int));
                object obj = Activator.CreateInstance(typeFromDisk);
                Assert.Equal("Added", genericMethod.Invoke(obj, [1, "hello"]));
                Assert.Equal("hello", genericMethod.Invoke(obj, [1, "next"]));
                tlc.Unload();
            }
        }

        [Fact]
        public void ANestedTypeUsedAsGenericArgumentWorks()
        {
            using (TempFile file = TempFile.Create())
            {
                AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilderAndTypeBuilder(out TypeBuilder type);
                TypeBuilder nested = type.DefineNestedType("Nested", TypeAttributes.NestedPrivate);
                Type nestedFType = typeof(Dictionary<,>).MakeGenericType(typeof(Type), nested);
                FieldBuilder nestedField = nested.DefineField("Helpers", nestedFType, FieldAttributes.Static | FieldAttributes.Private);
                MethodBuilder nestedMethod = nested.DefineMethod("TryGet", MethodAttributes.Public | MethodAttributes.Static,
                    typeof(bool), [typeof(Type)]);
                ParameterBuilder param1 = nestedMethod.DefineParameter(1, ParameterAttributes.None, "type");
                ConstructorBuilder constructor = nested.DefineDefaultConstructor(MethodAttributes.Public);
                ILGenerator nestedMILGen = nestedMethod.GetILGenerator();
                nestedMILGen.DeclareLocal(nested);
                Label label = nestedMILGen.DefineLabel();
                nestedMILGen.Emit(OpCodes.Ldsfld, nestedField);
                nestedMILGen.Emit(OpCodes.Ldarg_0);
                nestedMILGen.Emit(OpCodes.Ldloca_S, 0);
                nestedMILGen.Emit(OpCodes.Callvirt, TypeBuilder.GetMethod(nestedFType, typeof(Dictionary<,>).GetMethod("TryGetValue")));
                nestedMILGen.Emit(OpCodes.Brfalse_S, label);
                nestedMILGen.Emit(OpCodes.Ldc_I4_1);
                nestedMILGen.Emit(OpCodes.Ret);
                nestedMILGen.MarkLabel(label);
                nestedMILGen.Emit(OpCodes.Ldsfld, nestedField);
                nestedMILGen.Emit(OpCodes.Ldarg_0);
                nestedMILGen.Emit(OpCodes.Newobj, constructor);
                nestedMILGen.Emit(OpCodes.Callvirt, TypeBuilder.GetMethod(nestedFType, typeof(Dictionary<,>).GetMethod("Add")));
                nestedMILGen.Emit(OpCodes.Ldc_I4_0);
                nestedMILGen.Emit(OpCodes.Ret);

                ILGenerator nestedStaticCtorIL = nested.DefineTypeInitializer().GetILGenerator();
                nestedStaticCtorIL.Emit(OpCodes.Newobj, TypeBuilder.GetConstructor(
                    typeof(Dictionary<,>).MakeGenericType(typeof(Type), nested), typeof(Dictionary<,>).GetConstructor(Type.EmptyTypes)));
                nestedStaticCtorIL.Emit(OpCodes.Stsfld, nestedField);
                nestedStaticCtorIL.Emit(OpCodes.Ret);

                MethodBuilder test = type.DefineMethod("TestNested", MethodAttributes.Public | MethodAttributes.Static, typeof(bool), [typeof(Type)]);
                test.DefineParameter(1, ParameterAttributes.None, "type");
                ILGenerator testIl = test.GetILGenerator();
                testIl.Emit(OpCodes.Ldarg_0);
                testIl.Emit(OpCodes.Call, nestedMethod);
                testIl.Emit(OpCodes.Ret);
                nested.CreateType();
                type.CreateType();
                ab.Save(file.Path);

                TestAssemblyLoadContext tlc = new TestAssemblyLoadContext();
                Type typeFromDisk = tlc.LoadFromAssemblyPath(file.Path).GetType("MyType");
                MethodInfo methodFromDisk = typeFromDisk.GetMethod("TestNested");
                object obj = Activator.CreateInstance(typeFromDisk);
                Assert.Equal(false, methodFromDisk.Invoke(null, [typeof(int)]));
                Assert.Equal(true, methodFromDisk.Invoke(null, [typeof(int)]));
                tlc.Unload();
            }
        }

        [Fact]
        public void ReferenceNestedGenericCollectionsWithTypeBuilderParameterInIL()
        {
            using (TempFile file = TempFile.Create())
            {
                AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilderAndTypeBuilder(out TypeBuilder type);
                TypeBuilder nestedType = type.DefineNestedType("NestedType", TypeAttributes.NestedPublic);

                Type returnType = typeof(List<>).MakeGenericType(typeof(Dictionary<,>).MakeGenericType(nestedType, typeof(bool)));
                MethodBuilder nestedMethod = nestedType.DefineMethod("M1", MethodAttributes.Public, returnType, null);
                ILGenerator nestedIL = nestedMethod.GetILGenerator();
                nestedIL.Emit(OpCodes.Ldc_I4_4);
                nestedIL.Emit(OpCodes.Newobj, TypeBuilder.GetConstructor(returnType, typeof(List<>).GetConstructor([typeof(int)])));
                nestedIL.Emit(OpCodes.Ret);

                nestedType.CreateType();
                type.CreateType();
                ab.Save(file.Path);

                TestAssemblyLoadContext tlc = new TestAssemblyLoadContext();
                Type typeFromDisk = tlc.LoadFromAssemblyPath(file.Path).GetType("MyType");
                Type nestedFromDisk = typeFromDisk.GetNestedType("NestedType");
                MethodInfo methodFromDisk = nestedFromDisk.GetMethod("M1");
                object obj = Activator.CreateInstance(nestedFromDisk);
                object result = methodFromDisk.Invoke(obj, null);
                Assert.NotNull(result);
                Type listType = result.GetType();
                Type expectedType = typeof(List<>).MakeGenericType(typeof(Dictionary<,>).MakeGenericType(nestedFromDisk, typeof(bool)));
                Assert.Equal(expectedType, listType);
                tlc.Unload();
            }
        }
    }
}
