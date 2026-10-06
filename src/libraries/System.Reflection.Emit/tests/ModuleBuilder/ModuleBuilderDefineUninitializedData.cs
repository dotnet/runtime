// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class ModuleBuilderDefineUninitializedData
    {
        private const int ReservedMaskFieldAttribute = 0x9500; // This constant maps to FieldAttributes.ReservedMask that is not available in the contract.

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsReflectionEmitSupported), nameof(PlatformDetection.IsCoreCLR))]
        [InlineData(AssemblyBuilderAccess.Run, false)]
        [InlineData(AssemblyBuilderAccess.Run, true)]
        [InlineData(AssemblyBuilderAccess.RunAndCollect, false)]
        [InlineData(AssemblyBuilderAccess.RunAndCollect, true)]
        public void DefineUninitializedData_ZeroFilledAndWritable(AssemblyBuilderAccess access, bool global)
        {
            ModuleBuilder module = Helpers.DynamicAssembly(access: access).DefineDynamicModule("DataModule");
            TypeBuilder type = module.DefineType("DataOwner", TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Sealed);
            int[] sizes = { 1, 4, 8, 13, 16, 4097 };
            byte[][] expected = new byte[sizes.Length][];
            FieldBuilder[] fields = new FieldBuilder[sizes.Length];
            FieldBuilder[] sources = new FieldBuilder[sizes.Length];
            for (int i = 0; i < sizes.Length; i++)
            {
                expected[i] = new byte[sizes[i]];
                Array.Fill(expected[i], (byte)(i + 1));
                fields[i] = global
                    ? module.DefineUninitializedData($"Data{i}", sizes[i], FieldAttributes.Public)
                    : type.DefineUninitializedData($"Data{i}", sizes[i], FieldAttributes.Public);
                sources[i] = global
                    ? module.DefineInitializedData($"Source{i}", expected[i], FieldAttributes.Public)
                    : type.DefineInitializedData($"Source{i}", expected[i], FieldAttributes.Public);
            }

            if (global)
            {
                module.CreateGlobalFunctions();
            }
            Type createdType = type.CreateType();

            for (int i = 0; i < fields.Length; i++)
            {
                FieldInfo field = global ? module.GetField(fields[i].Name) : createdType.GetField(fields[i].Name);
                FieldInfo source = global ? module.GetField(sources[i].Name) : createdType.GetField(sources[i].Name);
                Assert.Equal(new byte[sizes[i]], Helpers.GetFieldValueBytes(field, sizes[i]));

                byte[] initialized = new byte[sizes[i]];
                Array.Fill(initialized, byte.MaxValue);
                RuntimeHelpers.InitializeArray(initialized, field.FieldHandle);
                Assert.Equal(new byte[sizes[i]], initialized);

                field.SetValue(null, source.GetValue(null));
                Assert.Equal(expected[i], Helpers.GetFieldValueBytes(field, sizes[i]));
                RuntimeHelpers.InitializeArray(initialized, field.FieldHandle);
                Assert.Equal(expected[i], initialized);
                Assert.Equal(expected[i], Helpers.GetFieldValueBytes(source, sizes[i]));
            }
        }

        public static IEnumerable<object[]> Attributes_TestData()
        {
            yield return new object[] { FieldAttributes.Assembly };
            yield return new object[] { FieldAttributes.FamANDAssem };
            yield return new object[] { FieldAttributes.Family };
            yield return new object[] { FieldAttributes.FamORAssem };
            yield return new object[] { FieldAttributes.FieldAccessMask };
            yield return new object[] { FieldAttributes.HasDefault };
            yield return new object[] { FieldAttributes.HasFieldMarshal };
            yield return new object[] { FieldAttributes.HasFieldRVA };
            yield return new object[] { FieldAttributes.InitOnly };
            yield return new object[] { FieldAttributes.Literal };
            yield return new object[] { FieldAttributes.NotSerialized };
            yield return new object[] { FieldAttributes.PinvokeImpl };
            yield return new object[] { FieldAttributes.Private };
            yield return new object[] { FieldAttributes.PrivateScope };
            yield return new object[] { FieldAttributes.Public };
            yield return new object[] { FieldAttributes.RTSpecialName };
            yield return new object[] { FieldAttributes.SpecialName };
            yield return new object[] { FieldAttributes.Static };
        }

        [Theory]
        [MemberData(nameof(Attributes_TestData))]
        public void DefineUninitializedData(FieldAttributes attributes)
        {
            ModuleBuilder module = Helpers.DynamicModule();
            foreach (int size in new int[] { 1, 2, 0x003f0000 - 1 })
            {
                FieldBuilder field = module.DefineUninitializedData(size.ToString(), size, attributes);

                int expectedAttributes = ((int)attributes | (int)FieldAttributes.Static) & ~ReservedMaskFieldAttribute;
                Assert.Equal(size.ToString(), field.Name);
                Assert.Equal((FieldAttributes)expectedAttributes, field.Attributes);
            }
        }

        [Theory]
        [MemberData(nameof(Attributes_TestData))]
        public void DefineUninitializedData_EmptyName_ThrowsArgumentException(FieldAttributes attributes)
        {
            ModuleBuilder module = Helpers.DynamicModule();
            AssertExtensions.Throws<ArgumentException>("name", () => module.DefineUninitializedData("", 1, attributes));
        }

        [Theory]
        [MemberData(nameof(Attributes_TestData))]
        public void DefineUninitializedData_InvalidSize_ThrowsArgumentException(FieldAttributes attributes)
        {
            ModuleBuilder module = Helpers.DynamicModule();
            foreach (int size in new int[] { -1, 0, 0x003f0000, 0x003f0000 + 1 })
            {
                AssertExtensions.Throws<ArgumentException>(null, () => module.DefineUninitializedData("TestField", size, attributes));
            }
        }

        [Theory]
        [MemberData(nameof(Attributes_TestData))]
        public void DefineUninitializedData_NullName_ThrowsArgumentNullException(FieldAttributes attributes)
        {
            ModuleBuilder module = Helpers.DynamicModule();
            AssertExtensions.Throws<ArgumentNullException>("name", () => module.DefineUninitializedData(null, 1, attributes));
        }

        [Theory]
        [MemberData(nameof(Attributes_TestData))]
        public void DefineUninitializedData_CreateGlobalFunctionsAlreadyCalled_ThrowsInvalidOperationException(FieldAttributes attributes)
        {
            ModuleBuilder module = Helpers.DynamicModule();
            module.CreateGlobalFunctions();

            Assert.Throws<InvalidOperationException>(() => module.DefineUninitializedData("TestField", 1, attributes));
        }
    }
}
