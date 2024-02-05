// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    public class AssemblySaveModuleBuilderTests
    {
        [Fact]
        public void DefineGlobalMethodAndCreateGlobalFunctionsTest()
        {
            using (TempFile file = TempFile.Create())
            {
                AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilder(new AssemblyName("MyAssembly"));
                ModuleBuilder module = ab.DefineDynamicModule("MyModule");
                MethodBuilder method = module.DefineGlobalMethod("TestMethod", MethodAttributes.Static | MethodAttributes.Public, null, null);
                ILGenerator ilGenerator = method.GetILGenerator();
                ilGenerator.EmitWriteLine("Hello World from global method.");
                ilGenerator.Emit(OpCodes.Ret);

                MethodBuilder method2 = module.DefineGlobalMethod("MyMethod", MethodAttributes.Static | MethodAttributes.Public, CallingConventions.Standard, typeof(int), [typeof(string), typeof(int)]);
                ilGenerator = method2.GetILGenerator();
                ilGenerator.Emit(OpCodes.Ldarg_0);
                ilGenerator.Emit(OpCodes.Call, typeof(Console).GetMethod("WriteLine", [typeof(string)]));
                ilGenerator.Emit(OpCodes.Ldarg_1);
                ilGenerator.Emit(OpCodes.Ret);

                module.CreateGlobalFunctions();
                Assert.Null(method.DeclaringType);
                Assert.Equal(typeof(void), method.ReturnType);
                Assert.Equal(method, module.GetMethod("TestMethod"));
                Assert.Equal(method2, module.GetMethod("MyMethod", [typeof(string), typeof(int)]));
                Assert.Equal(2, module.GetMethods().Length);

                ab.Save(file.Path);

                using (MetadataLoadContext mlc = new MetadataLoadContext(new CoreMetadataAssemblyResolver()))
                {
                    Assembly assemblyFromDisk = mlc.LoadFromAssemblyPath(file.Path);
                    MethodInfo globalMethod1 = assemblyFromDisk.ManifestModule.GetMethod("TestMethod");
                    Assert.NotNull(globalMethod1);
                    Assert.True(globalMethod1.IsStatic);
                    Assert.True(globalMethod1.IsPublic);
                    Assert.Equal(0, globalMethod1.GetParameters().Length);
                    Assert.Equal(method.ReturnType.FullName, globalMethod1.ReturnType.FullName);
                    Assert.NotNull(globalMethod1.DeclaringType);

                    MethodInfo globalMethod2 = assemblyFromDisk.ManifestModule.GetMethod("MyMethod");
                    Assert.NotNull(globalMethod2);
                    Assert.True(globalMethod2.IsStatic);
                    Assert.True(globalMethod2.IsPublic);
                    Assert.Equal(2, globalMethod2.GetParameters().Length);
                    Assert.Equal(method.ReturnType.FullName, globalMethod1.ReturnType.FullName);
                    Assert.Equal("<Module>", globalMethod2.DeclaringType.Name);
                }
            }
        }

        [Fact]
        public void DefineGlobalMethodAndCreateGlobalFunctions_Validations()
        {
            AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilder(new AssemblyName("MyAssembly"));
            ModuleBuilder module = ab.DefineDynamicModule("MyModule");
            Assert.Throws<ArgumentException>(() => module.DefineGlobalMethod("TestMethod", MethodAttributes.Public, null, null)); // must be static
            MethodBuilder method = module.DefineGlobalMethod("TestMethod", MethodAttributes.Static | MethodAttributes.Public, null, null);
            ILGenerator ilGenerator = method.GetILGenerator();
            ilGenerator.EmitWriteLine("Hello World from global method.");
            ilGenerator.Emit(OpCodes.Ret);

            Assert.Throws<NotSupportedException>(() => module.GetMethod("TestMethod")); // not supported when not created
            Assert.Throws<NotSupportedException>(() => module.GetMethods());
            module.CreateGlobalFunctions();

            Assert.Null(method.DeclaringType);
            Assert.Throws<InvalidOperationException>(() => module.CreateGlobalFunctions()); // already created
            Assert.Throws<InvalidOperationException>(() => module.DefineGlobalMethod("TestMethod2", MethodAttributes.Static | MethodAttributes.Public, null, null));
        }

        [Fact]
        public static void DefinePInvokeMethodTest()
        {
            using (TempFile file = TempFile.Create())
            {
                AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilder(new AssemblyName("MyAssembly"));
                DpmParams p = new DpmParams() { MethodName = "A2", LibName = "Foo2.dll", EntrypointName = "Wha2", ReturnType = typeof(int),
                    ParameterTypes = [typeof(int)], NativeCallConv = CallingConvention.Cdecl };

                ModuleBuilder modb = ab.DefineDynamicModule("MyModule");
                MethodBuilder mb = modb.DefinePInvokeMethod(p.MethodName, p.LibName, p.EntrypointName, p.Attributes, p.ManagedCallConv, p.ReturnType,
                    p.ParameterTypes, p.NativeCallConv, p.Charset);
                mb.SetImplementationFlags(mb.GetMethodImplementationFlags() | MethodImplAttributes.PreserveSig);

                modb.CreateGlobalFunctions();
                ab.Save(file.Path);
                MethodInfo m = modb.GetMethod(p.MethodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static, null, CallingConventions.Any, p.ParameterTypes, null);
                Assert.NotNull(m);

                using (MetadataLoadContext mlc = new MetadataLoadContext(new CoreMetadataAssemblyResolver()))
                {
                    Module moduleFromDisk = mlc.LoadFromAssemblyPath(file.Path).GetModule("MyModule");
                    Assert.Equal(1, moduleFromDisk.GetMethods().Length);
                    MethodInfo method = moduleFromDisk.GetMethod(p.MethodName);
                    Assert.NotNull(method);
                    AssemblySaveTypeBuilderAPIsTests.VerifyPInvokeMethod(method.DeclaringType, method, p, mlc.CoreAssembly);
                }
            }
        }

        [Theory]
        [InlineData(FieldAttributes.Static | FieldAttributes.Public)]
        [InlineData(FieldAttributes.Private)]
        [InlineData(FieldAttributes.FamANDAssem)]
        [InlineData(FieldAttributes.Assembly | FieldAttributes.SpecialName)]
        public void DefineUninitializedDataTest(FieldAttributes attributes)
        {
            AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilder(new AssemblyName("MyAssembly"));
            ModuleBuilder module = ab.DefineDynamicModule("MyModule");
            foreach (int size in new int[] { 1, 2, 0x003f0000 - 1 })
            {
                FieldBuilder field = module.DefineUninitializedData(size.ToString(), size, attributes);

                Assert.Equal(size.ToString(), field.Name);
                Assert.True(field.IsStatic);
                Assert.True((field.Attributes & FieldAttributes.HasFieldRVA) != 0);
                Assert.Equal(attributes | FieldAttributes.Static | FieldAttributes.HasFieldRVA, field.Attributes);
            }
        }

        [Fact]
        public void DefineUninitializedData_Validations()
        {
            AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilder(new AssemblyName("MyAssembly"));
            ModuleBuilder module = ab.DefineDynamicModule("MyModule");

            AssertExtensions.Throws<ArgumentNullException>("name", () => module.DefineUninitializedData(null, 1, FieldAttributes.Family));
            AssertExtensions.Throws<ArgumentException>("name", () => module.DefineUninitializedData("", 1, FieldAttributes.Public));

            foreach (int size in new int[] { -1, 0, 0x003f0000, 0x003f0000 + 1 })
            {
                AssertExtensions.Throws<ArgumentException>("size", () => module.DefineUninitializedData("TestField", size, FieldAttributes.Private));
            }

            module.CreateGlobalFunctions();
            Assert.Throws<InvalidOperationException>(() => module.DefineUninitializedData("TestField", 1, FieldAttributes.HasDefault));
        }

        [Theory]
        [InlineData(FieldAttributes.Static | FieldAttributes.Public)]
        [InlineData(FieldAttributes.Static | FieldAttributes.Private)]
        [InlineData(FieldAttributes.Private)]
        public void DefineInitializedDataTest(FieldAttributes attributes)
        {
            AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilder(new AssemblyName("MyAssembly"));
            ModuleBuilder module = ab.DefineDynamicModule("MyModule");
            FieldBuilder field = module.DefineInitializedData("MyField", [01, 00, 01], attributes);

            Assert.True(field.IsStatic);
            Assert.Equal((attributes & FieldAttributes.Public) != 0, field.IsPublic);
            Assert.Equal((attributes & FieldAttributes.Private) != 0, field.IsPrivate);
            Assert.Equal("MyField", field.Name);
        }

        [Fact]
        public void DefineInitializedData_Validations()
        {
            AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilder(new AssemblyName("MyAssembly"));
            ModuleBuilder module = ab.DefineDynamicModule("MyModule");

            AssertExtensions.Throws<ArgumentNullException>("name", () => module.DefineInitializedData(null, [1, 0, 1], FieldAttributes.Public));
            AssertExtensions.Throws<ArgumentException>("name", () => module.DefineInitializedData("", [1, 0, 1], FieldAttributes.Private));
            AssertExtensions.Throws<ArgumentNullException>("data", () => module.DefineInitializedData("MyField", null, FieldAttributes.Public));
            AssertExtensions.Throws<ArgumentException>("Length", () => module.DefineInitializedData("MyField", [], FieldAttributes.Public));
            AssertExtensions.Throws<ArgumentException>("Length", () => module.DefineInitializedData("MyField", new byte[0x3f0000], FieldAttributes.Public));

            FieldBuilder field = module.DefineInitializedData("MyField", [1, 0, 1], FieldAttributes.Public);
            module.CreateGlobalFunctions();

            Assert.Null(field.DeclaringType);
            Assert.Throws<InvalidOperationException>(() => module.DefineInitializedData("MyField2", new byte[] { 1, 0, 1 }, FieldAttributes.Public));
        }

        [Fact]
        // Field RVA alignment is not working on mono
        [ActiveIssue("https://github.com/dotnet/runtime/issues/97172", TestRuntimes.Mono)]
        public void DefineInitializedData_EnsureAlignmentIsMinimumNeededForUseOfCreateSpan()
        {
            using (TempFile file = TempFile.Create())
            {
                AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilder(new AssemblyName("MyAssembly"));
                ModuleBuilder module = ab.DefineDynamicModule("MyModule");
                TypeBuilder tb = module.DefineType("MyType", TypeAttributes.Public);
                // Create static field data in a variety of orders that requires the runtime to actively apply alignment
                // RuntimeHelpers.CreateSpan requires data to be naturally aligned within the "PE" file. At this time CreateSpan only
                // requires alignments up to 8 bytes.
                FieldBuilder field1Byte = module.DefineInitializedData("Field1Byte", new byte[] { 1 }, FieldAttributes.Public);
                byte[] field4Byte_1_data = new byte[] { 1, 2, 3, 4 };
                byte[] field8Byte_1_data = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
                byte[] field4Byte_2_data = new byte[] { 5, 6, 7, 8 };
                byte[] field8Byte_2_data = new byte[] { 9, 10, 11, 12, 13, 14, 15, 16 };
                FieldBuilder field4Byte_1 = module.DefineInitializedData("Field4Bytes_1", field4Byte_1_data, FieldAttributes.Public);
                FieldBuilder tbField4Byte_1 = tb.DefineInitializedData("Field4Bytes_1", field4Byte_1_data, FieldAttributes.Public);
                FieldBuilder field8Byte_1 = module.DefineInitializedData("Field8Bytes_1", field8Byte_1_data, FieldAttributes.Public);
                FieldBuilder field4Byte_2 = module.DefineInitializedData("Field4Bytes_2", field4Byte_2_data, FieldAttributes.Public);
                FieldBuilder field8Byte_2 = module.DefineInitializedData("Field8Bytes_2", field8Byte_2_data, FieldAttributes.Public);
                FieldBuilder tbField8Byte_2 = tb.DefineInitializedData("Field8Bytes_2", field8Byte_2_data, FieldAttributes.Public);
                module.CreateGlobalFunctions();
                tb.CreateType();
                Assert.Null(field4Byte_1.DeclaringType);
                Assert.Null(field8Byte_1.DeclaringType);
                Assert.Null(field4Byte_2.DeclaringType);
                Assert.Null(field8Byte_2.DeclaringType);

                TypeBuilder checkTypeBuilder = module.DefineType("CheckType", TypeAttributes.Public);
                CreateLoadAddressMethod("LoadAddress1", field1Byte);
                CreateLoadAddressMethod("LoadAddress4_1", field4Byte_1);
                CreateLoadAddressMethod("LoadAddress4_3", tbField4Byte_1);
                CreateLoadAddressMethod("LoadAddress4_2", field4Byte_2);
                CreateLoadAddressMethod("LoadAddress8_1", field8Byte_1);
                CreateLoadAddressMethod("LoadAddress8_2", field8Byte_2);
                CreateLoadAddressMethod("LoadAddress8_3", tbField8Byte_2);

                void CreateLoadAddressMethod(string name, FieldBuilder fieldBuilder)
                {
                    MethodBuilder loadAddressMethod = checkTypeBuilder.DefineMethod(name, MethodAttributes.Public | MethodAttributes.Static, typeof(IntPtr), null);
                    ILGenerator methodIL = loadAddressMethod.GetILGenerator();
                    methodIL.Emit(OpCodes.Ldsflda, fieldBuilder);
                    methodIL.Emit(OpCodes.Ret);
                }

                checkTypeBuilder.CreateType();
                ab.Save(file.Path);

                TestAssemblyLoadContext tlc = new TestAssemblyLoadContext();
                Assembly assemblyFromDisk = tlc.LoadFromAssemblyPath(file.Path);
                Type checkType = assemblyFromDisk.GetType("CheckType");

                CheckMethod("LoadAddress4_1", 4, field4Byte_1_data);
                CheckMethod("LoadAddress4_3", 4, field4Byte_1_data);
                CheckMethod("LoadAddress4_2", 4, field4Byte_2_data);
                CheckMethod("LoadAddress8_1", 8, field8Byte_1_data);
                CheckMethod("LoadAddress8_2", 8, field8Byte_2_data);
                CheckMethod("LoadAddress8_3", 8, field8Byte_2_data);
                tlc.Unload();

                void CheckMethod(string name, int minAlignmentRequired, byte[] dataToVerify)
                {
                    MethodInfo methodToCall = checkType.GetMethod(name);
                    nint address = (nint)methodToCall.Invoke(null, null);

                    for (int i = 0; i < dataToVerify.Length; i++)
                    {
                        Assert.Equal(dataToVerify[i], Marshal.ReadByte(address + (nint)i));
                    }
                    Assert.Equal(name + "_0" + "_" + address.ToString(), name + "_" + (address % minAlignmentRequired).ToString() + "_" + address.ToString());
                }
            }
        }

        [Fact]
        // Standalone signature is not supported on mono
        [ActiveIssue("https://github.com/dotnet/runtime/issues/96389", TestRuntimes.Mono)]
        public void GetABCMetadataToken_Validations()
        {
            AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilder(new AssemblyName("MyAssembly"));
            ModuleBuilder module = ab.DefineDynamicModule("MyModule");
            TypeBuilder type = module.DefineType("MyType", TypeAttributes.Public);
            MethodBuilder method = type.DefineMethod("TestMethod", MethodAttributes.Static | MethodAttributes.Public);
            FieldBuilder field = type.DefineField("MyField", typeof(int), FieldAttributes.Public);
            ConstructorBuilder constructor = type.DefineDefaultConstructor(MethodAttributes.Public);

            Assert.Throws<InvalidOperationException>(() => module.GetMethodMetadataToken(method));
            Assert.Throws<InvalidOperationException>(() => module.GetMethodMetadataToken(constructor));
            Assert.Throws<InvalidOperationException>(() => module.GetFieldMetadataToken(field));
            Assert.Throws<InvalidOperationException>(() => module.GetTypeMetadataToken(type));

            SignatureHelper signature = SignatureHelper.GetMethodSigHelper(CallingConventions.HasThis, typeof(void));
            signature.AddArgument(typeof(string));
            signature.AddArgument(typeof(int));

            int signatureToken = module.GetSignatureMetadataToken(signature);
            int stringToken = module.GetStringMetadataToken("Hello");

            Assert.True(signatureToken > 0);
            Assert.True(stringToken > 0);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotMonoRuntime))]
        public static void GetArrayMethodTest()
        {
            using (TempFile file = TempFile.Create())
            {
                AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilder(new AssemblyName("MyAssembly"));

                ModuleBuilder mb = ab.DefineDynamicModule("MyModule");
                TypeBuilder tb = mb.DefineType("TestClass", TypeAttributes.Public);
                Type tbArray = tb.MakeArrayType(2);
                Type[] paramArray = { tbArray, typeof(int), typeof(int) };
                MethodBuilder getMethod = tb.DefineMethod("GetArray", MethodAttributes.Public | MethodAttributes.Static, tb, paramArray);

                MethodInfo arrayGetMethod = mb.GetArrayMethod(tbArray, "Get", CallingConventions.HasThis, tb, [typeof(int), typeof(int)]);
                Assert.Equal(tbArray, arrayGetMethod.DeclaringType);
                Assert.Equal("Get", arrayGetMethod.Name);
                Assert.Equal(CallingConventions.HasThis, arrayGetMethod.CallingConvention);
                Assert.Equal(tb, arrayGetMethod.ReturnType);

                ILGenerator getIL = getMethod.GetILGenerator();
                getIL.Emit(OpCodes.Ldarg_0);
                getIL.Emit(OpCodes.Ldarg_1);
                getIL.Emit(OpCodes.Ldarg_2);
                getIL.Emit(OpCodes.Call, arrayGetMethod);
                getIL.Emit(OpCodes.Ret);

                MethodInfo arraySetMethod = mb.GetArrayMethod(tbArray, "Set", CallingConventions.HasThis, typeof(void), [typeof(int), typeof(int), tb]);
                MethodBuilder setMethod = tb.DefineMethod("SetArray", MethodAttributes.Public | MethodAttributes.Static, typeof(void), [tbArray, typeof(int), typeof(int), tb]);
                ILGenerator setIL = setMethod.GetILGenerator();
                setIL.Emit(OpCodes.Ldarg_0);
                setIL.Emit(OpCodes.Ldarg_1);
                setIL.Emit(OpCodes.Ldarg_2);
                setIL.Emit(OpCodes.Ldarg_3);
                setIL.Emit(OpCodes.Call, arraySetMethod);
                setIL.Emit(OpCodes.Ret);

                tb.CreateType();
                ab.Save(file.Path);

                TestAssemblyLoadContext tlc = new TestAssemblyLoadContext();
                Assembly assemblyFromDisk = tlc.LoadFromAssemblyPath(file.Path);
                Type typeFromDisk = assemblyFromDisk.GetType("TestClass");
                object instance = Activator.CreateInstance(typeFromDisk);
                Array a = Array.CreateInstance(typeFromDisk, 2, 2);
                MethodInfo setArray = typeFromDisk.GetMethod("SetArray");
                setArray.Invoke(null, [a, 0, 0, instance]);
                MethodInfo getArray = typeFromDisk.GetMethod("GetArray");
                object obj = getArray.Invoke(null, [a, 0, 0]);
                Assert.NotNull(obj);
                Assert.Equal(instance, obj);
                tlc.Unload();
            }
        }

        [Fact]
        public void GetArrayMethod_InvalidArgument_ThrowsArgumentException()
        {
            ModuleBuilder module = AssemblySaveTools.PopulateAssemblyBuilder(new AssemblyName("MyAssembly")).DefineDynamicModule("MyModule");

            AssertExtensions.Throws<ArgumentNullException>("arrayClass", () => module.GetArrayMethod(null, "TestMethod", CallingConventions.Standard, null, null));
            AssertExtensions.Throws<ArgumentNullException>("methodName", () => module.GetArrayMethod(typeof(string[]), null, CallingConventions.Standard, typeof(void), null));
            AssertExtensions.Throws<ArgumentException>("methodName", () => module.GetArrayMethod(typeof(string[]), "", CallingConventions.Standard, null, null));
            AssertExtensions.Throws<ArgumentNullException>("parameterTypes", () => module.GetArrayMethod(typeof(string[]), "TestMethod", CallingConventions.Standard, null, [null]));
            AssertExtensions.Throws<ArgumentException>(null, () => module.GetArrayMethod(typeof(Array), "TestMethod", CallingConventions.Standard, null, null));
        }
    }
}
