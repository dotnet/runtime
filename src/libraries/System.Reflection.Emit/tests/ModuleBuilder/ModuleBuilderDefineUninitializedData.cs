// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class ModuleBuilderDefineUninitializedData
    {
        private const int ReservedMaskFieldAttribute = 0x9500; // This constant maps to FieldAttributes.ReservedMask that is not available in the contract.

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
        public void DefineUnitializedData(FieldAttributes attributes)
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
        public void DefineUnitializedData_EmptyName_ThrowsArgumentException(FieldAttributes attributes)
        {
            ModuleBuilder module = Helpers.DynamicModule();
            AssertExtensions.Throws<ArgumentException>("name", () => module.DefineUninitializedData("", 1, attributes));
        }

        [Theory]
        [MemberData(nameof(Attributes_TestData))]
        public void DefineUnitializedData_InvalidSize_ThrowsArgumentException(FieldAttributes attributes)
        {
            ModuleBuilder module = Helpers.DynamicModule();
            foreach (int size in new int[] { -1, 0, 0x003f0000, 0x003f0000 + 1 })
            {
                AssertExtensions.Throws<ArgumentException>(null, () => module.DefineUninitializedData("TestField", size, attributes));
            }
        }

        [Theory]
        [MemberData(nameof(Attributes_TestData))]
        public void DefineUnitializedData_NullName_ThrowsArgumentNullException(FieldAttributes attributes)
        {
            ModuleBuilder module = Helpers.DynamicModule();
            AssertExtensions.Throws<ArgumentNullException>("name", () => module.DefineUninitializedData(null, 1, attributes));
        }

        [Theory]
        [MemberData(nameof(Attributes_TestData))]
        public void DefineUninitalizedData_CreateGlobalFunctionsAlreadyCalled_ThrowsInvalidOperationException(FieldAttributes attributes)
        {
            ModuleBuilder module = Helpers.DynamicModule();
            module.CreateGlobalFunctions();

            Assert.Throws<InvalidOperationException>(() => module.DefineUninitializedData("TestField", 1, attributes));
        }
    }
}
