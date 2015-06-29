// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using System.Reflection;
using System.Reflection.Emit;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class ModuleBuilderDefineUninitializedData
    {
        private const string DefaultAssemblyName = "ModuleBuilderDefineUninitializedData";
        private const AssemblyBuilderAccess DefaultAssemblyBuilderAccess = AssemblyBuilderAccess.Run;
        private const string DefaultModuleName = "DynamicModule";
        private const int MinStringLength = 8;
        private const int MaxStringLength = 256;
        private const int ReservedMaskFieldAttribute = 0x9500; // This constant maps to FieldAttributes.ReservedMask that is not available in the contract.

        private ModuleBuilder GetModuleBuilder()
        {
            AssemblyName name = new AssemblyName(DefaultAssemblyName);
            AssemblyBuilder asmBuilder = AssemblyBuilder.DefineDynamicAssembly(name, DefaultAssemblyBuilderAccess);
            return TestLibrary.Utilities.GetModuleBuilder(asmBuilder, DefaultModuleName);
        }

        [Fact]
        public void PosTest1()
        {
            ModuleBuilder builder = GetModuleBuilder();
            string fieldName = "PosTest1_";
            int size = TestLibrary.Generator.GetByte();
            if (size == 0)
                size++;

            FieldAttributes[] attributes = new FieldAttributes[] {
                FieldAttributes.Assembly,
                FieldAttributes.FamANDAssem,
                FieldAttributes.Family,
                FieldAttributes.FamORAssem,
                FieldAttributes.FieldAccessMask,
                FieldAttributes.HasDefault,
                FieldAttributes.HasFieldMarshal,
                FieldAttributes.HasFieldRVA,
                FieldAttributes.InitOnly,
                FieldAttributes.Literal,
                FieldAttributes.NotSerialized,
                FieldAttributes.PinvokeImpl,
                FieldAttributes.Private,
                FieldAttributes.PrivateScope,
                FieldAttributes.Public,
                FieldAttributes.RTSpecialName,
                FieldAttributes.SpecialName,
                FieldAttributes.Static
            };

            for (int i = 0; i < attributes.Length; ++i)
            {
                FieldAttributes attribute = attributes[i];
                string desiredFieldName = fieldName + i.ToString();
                FieldBuilder fb = builder.DefineUninitializedData(desiredFieldName, size, attribute);
                int desiredAttribute = ((int)attribute | (int)FieldAttributes.Static) & ~ReservedMaskFieldAttribute;
                VerificationHelper(fb, desiredFieldName, (FieldAttributes)desiredAttribute);
            }
        }

        [Fact]
        public void PosTest2()
        {
            ModuleBuilder builder = GetModuleBuilder();
            string fieldName = "PosTest2_";
            int[] sizeValues = new int[] {
                1,
                0x003f0000 - 1
            };
            FieldAttributes[] attributes = new FieldAttributes[] {
                FieldAttributes.Assembly,
                FieldAttributes.FamANDAssem,
                FieldAttributes.Family,
                FieldAttributes.FamORAssem,
                FieldAttributes.FieldAccessMask,
                FieldAttributes.HasDefault,
                FieldAttributes.HasFieldMarshal,
                FieldAttributes.HasFieldRVA,
                FieldAttributes.InitOnly,
                FieldAttributes.Literal,
                FieldAttributes.NotSerialized,
                FieldAttributes.PinvokeImpl,
                FieldAttributes.Private,
                FieldAttributes.PrivateScope,
                FieldAttributes.Public,
                FieldAttributes.RTSpecialName,
                FieldAttributes.SpecialName,
                FieldAttributes.Static
            };

            for (int i = 0; i < sizeValues.Length; ++i)
            {
                for (int j = 0; j < attributes.Length; ++j)
                {
                    FieldAttributes attribute = attributes[j];
                    string desiredFieldName = fieldName + i.ToString() + "_" + j.ToString();
                    FieldBuilder fb = builder.DefineUninitializedData(desiredFieldName, sizeValues[i], attribute);

                    int desiredAttribute = ((int)attribute | (int)FieldAttributes.Static) & (~ReservedMaskFieldAttribute);
                    VerificationHelper(fb, desiredFieldName, (FieldAttributes)desiredAttribute);
                }
            }
        }

        [Fact]
        public void NegTest1()
        {
            ModuleBuilder builder = GetModuleBuilder();
            FieldAttributes[] attributes = new FieldAttributes[] {
                FieldAttributes.Assembly,
                FieldAttributes.FamANDAssem,
                FieldAttributes.Family,
                FieldAttributes.FamORAssem,
                FieldAttributes.FieldAccessMask,
                FieldAttributes.HasDefault,
                FieldAttributes.HasFieldMarshal,
                FieldAttributes.HasFieldRVA,
                FieldAttributes.InitOnly,
                FieldAttributes.Literal,
                FieldAttributes.NotSerialized,
                FieldAttributes.PinvokeImpl,
                FieldAttributes.Private,
                FieldAttributes.PrivateScope,
                FieldAttributes.Public,
                FieldAttributes.RTSpecialName,
                FieldAttributes.SpecialName,
                FieldAttributes.Static
            };
            int size = TestLibrary.Generator.GetByte();

            for (int i = 0; i < attributes.Length; ++i)
            {
                VerificationHelper(builder, "", size, attributes[i], typeof(ArgumentException));
            }
        }

        [Fact]
        public void NegTest2()
        {
            int[] sizeValues = new int[] {
            0,
            -1,
            0x003f0000,
            0x003f0000 + 1
            };

            ModuleBuilder builder = GetModuleBuilder();

            FieldAttributes[] attributes = new FieldAttributes[] {
                FieldAttributes.Assembly,
                FieldAttributes.FamANDAssem,
                FieldAttributes.Family,
                FieldAttributes.FamORAssem,
                FieldAttributes.FieldAccessMask,
                FieldAttributes.HasDefault,
                FieldAttributes.HasFieldMarshal,
                FieldAttributes.HasFieldRVA,
                FieldAttributes.InitOnly,
                FieldAttributes.Literal,
                FieldAttributes.NotSerialized,
                FieldAttributes.PinvokeImpl,
                FieldAttributes.Private,
                FieldAttributes.PrivateScope,
                FieldAttributes.Public,
                FieldAttributes.RTSpecialName,
                FieldAttributes.SpecialName,
                FieldAttributes.Static
            };

            for (int i = 0; i < sizeValues.Length; ++i)
            {
                for (int j = 0; j < attributes.Length; ++j)
                {
                    FieldAttributes attribute = attributes[j];
                    VerificationHelper(builder, "", sizeValues[i], attribute, typeof(ArgumentException));
                }
            }
        }

        [Fact]
        public void NegTest3()
        {
            ModuleBuilder builder = GetModuleBuilder();
            FieldAttributes[] attributes = new FieldAttributes[] {
                FieldAttributes.Assembly,
                FieldAttributes.FamANDAssem,
                FieldAttributes.Family,
                FieldAttributes.FamORAssem,
                FieldAttributes.FieldAccessMask,
                FieldAttributes.HasDefault,
                FieldAttributes.HasFieldMarshal,
                FieldAttributes.HasFieldRVA,
                FieldAttributes.InitOnly,
                FieldAttributes.Literal,
                FieldAttributes.NotSerialized,
                FieldAttributes.PinvokeImpl,
                FieldAttributes.Private,
                FieldAttributes.PrivateScope,
                FieldAttributes.Public,
                FieldAttributes.RTSpecialName,
                FieldAttributes.SpecialName,
                FieldAttributes.Static
            };
            int size = TestLibrary.Generator.GetByte();

            for (int i = 0; i < attributes.Length; ++i)
            {
                VerificationHelper(builder, null, size, attributes[i], typeof(ArgumentNullException));
            }
        }

        [Fact]
        public void NegTest4()
        {
            FieldAttributes[] attributes = new FieldAttributes[] {
                FieldAttributes.Assembly,
                FieldAttributes.FamANDAssem,
                FieldAttributes.Family,
                FieldAttributes.FamORAssem,
                FieldAttributes.FieldAccessMask,
                FieldAttributes.HasDefault,
                FieldAttributes.HasFieldMarshal,
                FieldAttributes.HasFieldRVA,
                FieldAttributes.InitOnly,
                FieldAttributes.Literal,
                FieldAttributes.NotSerialized,
                FieldAttributes.PinvokeImpl,
                FieldAttributes.Private,
                FieldAttributes.PrivateScope,
                FieldAttributes.Public,
                FieldAttributes.RTSpecialName,
                FieldAttributes.SpecialName,
                FieldAttributes.Static
            };
            int size = TestLibrary.Generator.GetByte();
            ModuleBuilder testModuleBuilder = GetModuleBuilder();

            testModuleBuilder.CreateGlobalFunctions();
            string fieldName = "NegTest4_";

            for (int i = 0; i < attributes.Length; ++i)
            {
                VerificationHelper(testModuleBuilder, fieldName + i.ToString(), size, attributes[i], typeof(InvalidOperationException));
            }
        }


        private void VerificationHelper(FieldBuilder fb, string desiredName, FieldAttributes desiredAttribute)
        {
            Assert.Equal(desiredName, fb.Name);
            Assert.Equal(desiredAttribute, fb.Attributes);
        }

        private void VerificationHelper(ModuleBuilder builder, string name, int size, FieldAttributes attribute, Type desiredException)
        {
            Assert.Throws(desiredException, () => { FieldBuilder fieldBuilder = builder.DefineUninitializedData(name, size, attribute); });
        }
    }
}
