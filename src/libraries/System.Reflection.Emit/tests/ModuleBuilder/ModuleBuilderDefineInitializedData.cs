// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class ModuleBuilderDefineInitializedData
    {
        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsReflectionEmitSupported), nameof(PlatformDetection.IsCoreCLR))]
        [InlineData(AssemblyBuilderAccess.Run, false)]
        [InlineData(AssemblyBuilderAccess.Run, true)]
        [InlineData(AssemblyBuilderAccess.RunAndCollect, false)]
        [InlineData(AssemblyBuilderAccess.RunAndCollect, true)]
        public void DefineInitializedData_ContentsAndAddressesSurviveFurtherEmission(AssemblyBuilderAccess access, bool global)
        {
            ModuleBuilder module = Helpers.DynamicAssembly(access: access).DefineDynamicModule("DataModule");
            TypeBuilder type = module.DefineType("DataAccessors", TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Sealed);
            int[] sizes = { 1, 4, 8, 13, 16, 4097, 8192 };
            byte[][] expected = new byte[sizes.Length][];
            FieldBuilder[] fields = new FieldBuilder[sizes.Length];

            for (int i = 0; i < sizes.Length; i++)
            {
                expected[i] = new byte[sizes[i]];
                Array.Fill(expected[i], (byte)(i + 1));
                string name = $"Data{i}";
                fields[i] = global
                    ? module.DefineInitializedData(name, expected[i], FieldAttributes.Public)
                    : type.DefineInitializedData(name, expected[i], FieldAttributes.Public);

                MethodBuilder method = type.DefineMethod($"GetAddress{i}", MethodAttributes.Public | MethodAttributes.Static, typeof(IntPtr), Type.EmptyTypes);
                ILGenerator il = method.GetILGenerator();
                il.Emit(OpCodes.Ldsflda, fields[i]);
                il.Emit(OpCodes.Conv_I);
                il.Emit(OpCodes.Ret);
            }

            if (global)
            {
                module.CreateGlobalFunctions();
            }
            Type createdType = type.CreateType();
            IntPtr[] addresses = new IntPtr[sizes.Length];
            for (int i = 0; i < addresses.Length; i++)
            {
                addresses[i] = (IntPtr)createdType.GetMethod($"GetAddress{i}").Invoke(null, null);
            }

            TypeBuilder moreData = module.DefineType("MoreData", TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Sealed);
            for (int i = 0; i < 64; i++)
            {
                moreData.DefineInitializedData($"Extra{i}", new byte[513], FieldAttributes.Public);
            }
            moreData.CreateType();

            for (int i = 0; i < fields.Length; i++)
            {
                FieldInfo field = global ? module.GetField(fields[i].Name) : createdType.GetField(fields[i].Name);
                Assert.Equal(fields[i].MetadataToken, field.MetadataToken);
                Assert.True((field.Attributes & FieldAttributes.HasFieldRVA) != 0);
                Assert.Equal(addresses[i], (IntPtr)createdType.GetMethod($"GetAddress{i}").Invoke(null, null));
                Assert.Equal(0L, addresses[i].ToInt64() % (sizes[i] % 8 == 0 ? 8 : 4));
                Assert.Equal(expected[i], Helpers.GetFieldValueBytes(field, sizes[i]));

                byte[] initialized = new byte[sizes[i]];
                RuntimeHelpers.InitializeArray(initialized, field.FieldHandle);
                Assert.Equal(expected[i], initialized);

                if (BitConverter.IsLittleEndian)
                {
                    Assert.Equal(expected[i], RuntimeHelpers.CreateSpan<byte>(field.FieldHandle).ToArray());
                    if (sizes[i] % 8 == 0)
                    {
                        Assert.Equal(expected[i], MemoryMarshal.AsBytes(RuntimeHelpers.CreateSpan<long>(field.FieldHandle)).ToArray());
                    }
                }
                else
                {
                    Assert.Throws<PlatformNotSupportedException>(() => RuntimeHelpers.CreateSpan<byte>(field.FieldHandle));
                }

                field.SetValue(null, Activator.CreateInstance(field.FieldType));
                Assert.Equal(new byte[sizes[i]], Helpers.GetFieldValueBytes(field, sizes[i]));
                RuntimeHelpers.InitializeArray(initialized, field.FieldHandle);
                Assert.Equal(new byte[sizes[i]], initialized);
            }
        }

        [Theory]
        [InlineData(FieldAttributes.Static | FieldAttributes.Public)]
        [InlineData(FieldAttributes.Static | FieldAttributes.Private)]
        [InlineData( FieldAttributes.Private)]
        public void TestWithStaticAndPublic(FieldAttributes attributes)
        {
            ModuleBuilder module = Helpers.DynamicModule();
            FieldBuilder field = module.DefineInitializedData("MyField", new byte[] { 01, 00, 01 }, attributes);
            Assert.True(field.IsStatic);
            Assert.Equal((attributes & FieldAttributes.Public) != 0 , field.IsPublic);
            Assert.Equal((attributes & FieldAttributes.Private) != 0, field.IsPrivate);
            Assert.Equal("MyField", field.Name);
        }

        [Fact]
        public void DefineInitializedData_EmptyName_ThrowsArgumentException()
        {
            ModuleBuilder module = Helpers.DynamicModule();
            AssertExtensions.Throws<ArgumentException>("name", () => module.DefineInitializedData("", new byte[] { 1, 0, 1 }, FieldAttributes.Private));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(0x3f0000)]
        public void DefineInitializedData_InvalidDataLength_ThrowsArgumentException(int length)
        {
            ModuleBuilder module = Helpers.DynamicModule();
            AssertExtensions.Throws<ArgumentException>(null, () => module.DefineInitializedData("MyField", new byte[length], FieldAttributes.Public));
        }

        [Fact]
        public void DefineInitializedData_NullName_ThrowsArgumentNullException()
        {
            ModuleBuilder module = Helpers.DynamicModule();
            AssertExtensions.Throws<ArgumentNullException>("name", () => module.DefineInitializedData(null, new byte[] { 1, 0, 1 }, FieldAttributes.Public));
        }

        [Fact]
        public void DefineInitializedData_NullData_ThrowsArgumentNullException()
        {
            ModuleBuilder module = Helpers.DynamicModule();
            AssertExtensions.Throws<ArgumentNullException>("data", () => module.DefineInitializedData("MyField", null, FieldAttributes.Public));
        }

        [Fact]
        public void DefineInitializedData_CreateGlobalFunctionsCalled_ThrowsInvalidOperationException()
        {
            ModuleBuilder module = Helpers.DynamicModule();
            FieldBuilder field = module.DefineInitializedData("MyField", new byte[] { 1, 0, 1 }, FieldAttributes.Public);
            module.CreateGlobalFunctions();

            Assert.Null(field.DeclaringType);
            Assert.Throws<InvalidOperationException>(() => module.DefineInitializedData("MyField2", new byte[] { 1, 0, 1 }, FieldAttributes.Public));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsReflectionEmitSupported))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/65558", typeof(PlatformDetection), nameof(PlatformDetection.IsAndroid), nameof(PlatformDetection.Is32BitProcess))]

        public void DefineInitializedData_EnsureAlignmentIsMinimumNeededForUseOfCreateSpan()
        {
            ModuleBuilder module = Helpers.DynamicModule();

            // Create static field data in a variety of orders that requires the runtime to actively apply alignment
            // RuntimeHelpers.CreateSpan requires data to be naturally aligned within the "PE" file. At this time CreateSpan only
            // requires alignments up to 8 bytes.
            FieldBuilder field1Byte = module.DefineInitializedData("Field1Byte", new byte[] { 1 }, FieldAttributes.Public);
            byte[] field4Byte_1_data = new byte[] { 1, 2, 3, 4 };
            byte[] field8Byte_1_data = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
            byte[] field4Byte_2_data = new byte[] { 5, 6, 7, 8 };
            byte[] field8Byte_2_data = new byte[] { 9, 10, 11, 12, 13, 14, 15, 16 };
            FieldBuilder field4Byte_1 = module.DefineInitializedData("Field4Bytes_1", field4Byte_1_data, FieldAttributes.Public);
            FieldBuilder field8Byte_1 = module.DefineInitializedData("Field8Bytes_1", field8Byte_1_data, FieldAttributes.Public);
            FieldBuilder field4Byte_2 = module.DefineInitializedData("Field4Bytes_2", field4Byte_2_data, FieldAttributes.Public);
            FieldBuilder field8Byte_2 = module.DefineInitializedData("Field8Bytes_2", field8Byte_2_data, FieldAttributes.Public);
            module.CreateGlobalFunctions();

            Assert.Null(field4Byte_1.DeclaringType);
            Assert.Null(field8Byte_1.DeclaringType);
            Assert.Null(field4Byte_2.DeclaringType);
            Assert.Null(field8Byte_2.DeclaringType);

            var checkTypeBuilder = module.DefineType("CheckType", TypeAttributes.Public);
            CreateLoadAddressMethod("LoadAddress1", field1Byte);
            CreateLoadAddressMethod("LoadAddress4_1", field4Byte_1);
            CreateLoadAddressMethod("LoadAddress4_2", field4Byte_2);
            CreateLoadAddressMethod("LoadAddress8_1", field8Byte_1);
            CreateLoadAddressMethod("LoadAddress8_2", field8Byte_2);

            var checkType = checkTypeBuilder.CreateType();

            CheckMethod("LoadAddress4_1", 4, field4Byte_1_data);
            CheckMethod("LoadAddress4_2", 4, field4Byte_2_data);
            CheckMethod("LoadAddress8_1", 8, field8Byte_1_data);
            CheckMethod("LoadAddress8_2", 8, field8Byte_2_data);

            void CreateLoadAddressMethod(string name, FieldBuilder fieldBuilder)
            {
                var loadAddressMethod = checkTypeBuilder.DefineMethod(name, MethodAttributes.Public | MethodAttributes.Static, typeof(IntPtr), null);
                var methodIL = loadAddressMethod.GetILGenerator();
                methodIL.Emit(OpCodes.Ldsflda, fieldBuilder);
                methodIL.Emit(OpCodes.Ret);
            }

            void CheckMethod(string name, int minAlignmentRequired, byte[] dataToVerify)
            {
                var methodToCall = checkType.GetMethod(name);
                nint address = (nint)methodToCall.Invoke(null, null);

                for (int i = 0; i < dataToVerify.Length; i++)
                {
                    Assert.Equal(dataToVerify[i], Marshal.ReadByte(address + (nint)i));
                }
                Assert.Equal(name + "_0" + "_" + address.ToString(), name + "_" + (address % minAlignmentRequired).ToString() + "_" + address.ToString());
            }
        }
    }
}
