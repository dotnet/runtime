// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Internal.TypeSystem;

using Xunit;

namespace TypeSystemTests
{
    public class ArchitectureSpecificFieldLayoutTests
    {
        private TestTypeSystemContext _contextX86;
        private ModuleDesc _testModuleX86;
        private TestTypeSystemContext _contextX64;
        private ModuleDesc _testModuleX64;
        private TestTypeSystemContext _contextX64Windows;
        private ModuleDesc _testModuleX64Windows;
        private TestTypeSystemContext _contextX64Linux;
        private ModuleDesc _testModuleX64Linux;
        private TestTypeSystemContext _contextARM;
        private ModuleDesc _testModuleARM;
        private TestTypeSystemContext _contextARM64;
        private ModuleDesc _testModuleARM64;

        public ArchitectureSpecificFieldLayoutTests()
        {
            _contextX64 = new TestTypeSystemContext(TargetArchitecture.X64);
            var systemModuleX64 = _contextX64.CreateModuleForSimpleName("CoreTestAssembly");
            _contextX64.SetSystemModule(systemModuleX64);

            _testModuleX64 = systemModuleX64;

            _contextX64Linux = new TestTypeSystemContext(TargetArchitecture.X64, TargetOS.Linux);
            var systemModuleX64Linux = _contextX64Linux.CreateModuleForSimpleName("CoreTestAssembly");
            _contextX64Linux.SetSystemModule(systemModuleX64Linux);

            _testModuleX64Linux = systemModuleX64Linux;

            _contextX64Windows = new TestTypeSystemContext(TargetArchitecture.X64, TargetOS.Windows);
            var systemModuleX64Windows = _contextX64Windows.CreateModuleForSimpleName("CoreTestAssembly");
            _contextX64Windows.SetSystemModule(systemModuleX64Windows);

            _testModuleX64Windows = systemModuleX64Windows;

            _contextARM = new TestTypeSystemContext(TargetArchitecture.ARM);
            var systemModuleARM = _contextARM.CreateModuleForSimpleName("CoreTestAssembly");
            _contextARM.SetSystemModule(systemModuleARM);

            _testModuleARM = systemModuleARM;

            _contextX86 = new TestTypeSystemContext(TargetArchitecture.X86);
            var systemModuleX86 = _contextX86.CreateModuleForSimpleName("CoreTestAssembly");
            _contextX86.SetSystemModule(systemModuleX86);

            _testModuleX86 = systemModuleX86;

            _contextARM64 = new TestTypeSystemContext(TargetArchitecture.ARM64);
            var systemModuleARM64 = _contextARM64.CreateModuleForSimpleName("CoreTestAssembly");
            _contextARM64.SetSystemModule(systemModuleARM64);

            _testModuleARM64 = systemModuleARM64;
        }

        [Fact]
        public void TestInstanceLayoutDoubleBool()
        {
            MetadataType tX64 = _testModuleX64.GetType("Sequential", "ClassDoubleBool");
            MetadataType tX86 = _testModuleX86.GetType("Sequential", "ClassDoubleBool");
            MetadataType tARM = _testModuleARM.GetType("Sequential", "ClassDoubleBool");

            Assert.Equal(0x8, tX64.InstanceByteAlignment.AsInt);
            Assert.Equal(0x8, tARM.InstanceByteAlignment.AsInt);
            Assert.Equal(0x4, tX86.InstanceByteAlignment.AsInt);

            Assert.Equal(0x11, tX64.InstanceByteCountUnaligned.AsInt);
            Assert.Equal(0xD, tARM.InstanceByteCountUnaligned.AsInt);
            Assert.Equal(0xD, tX86.InstanceByteCountUnaligned.AsInt);

            Assert.Equal(0x18, tX64.InstanceByteCount.AsInt);
            Assert.Equal(0x10, tARM.InstanceByteCount.AsInt);
            Assert.Equal(0x10, tX86.InstanceByteCount.AsInt);
        }

        [Fact]
        public void TestInstanceLayoutBoolDoubleBool()
        {
            MetadataType tX64 = _testModuleX64.GetType("Sequential", "ClassBoolDoubleBool");
            MetadataType tX86 = _testModuleX86.GetType("Sequential", "ClassBoolDoubleBool");
            MetadataType tARM = _testModuleARM.GetType("Sequential", "ClassBoolDoubleBool");

            Assert.Equal(0x8, tX64.InstanceByteAlignment.AsInt);
            Assert.Equal(0x8, tARM.InstanceByteAlignment.AsInt);
            Assert.Equal(0x4, tX86.InstanceByteAlignment.AsInt);

            Assert.Equal(0x19, tX64.InstanceByteCountUnaligned.AsInt);
            Assert.Equal(0x15, tARM.InstanceByteCountUnaligned.AsInt);
            Assert.Equal(0x15, tX86.InstanceByteCountUnaligned.AsInt);

            Assert.Equal(0x20, tX64.InstanceByteCount.AsInt);
            Assert.Equal(0x18, tARM.InstanceByteCount.AsInt);
            Assert.Equal(0x18, tX86.InstanceByteCount.AsInt);
        }

        [Fact]
        public void TestAlignmentBehavior_LongIntEnumStruct()
        {
            string _namespace = "EnumAlignment";
            string _type = "LongIntEnumStruct";

            MetadataType tX64 = _testModuleX64.GetType(_namespace, _type);
            MetadataType tX86 = _testModuleX86.GetType(_namespace, _type);
            MetadataType tARM = _testModuleARM.GetType(_namespace, _type);

            Assert.Equal(0x8, tX64.InstanceByteAlignment.AsInt);
            Assert.Equal(0x8, tARM.InstanceByteAlignment.AsInt);
            Assert.Equal(0x4, tX86.InstanceByteAlignment.AsInt);

            Assert.Equal(0x20, tX64.InstanceByteCountUnaligned.AsInt);
            Assert.Equal(0x20, tARM.InstanceByteCountUnaligned.AsInt);
            Assert.Equal(0x20, tX86.InstanceByteCountUnaligned.AsInt);

            Assert.Equal(0x20, tX64.InstanceByteCount.AsInt);
            Assert.Equal(0x20, tARM.InstanceByteCount.AsInt);
            Assert.Equal(0x20, tX86.InstanceByteCount.AsInt);

            Assert.Equal(0x8, tX64.InstanceFieldAlignment.AsInt);
            Assert.Equal(0x8, tARM.InstanceFieldAlignment.AsInt);
            Assert.Equal(0x8, tX86.InstanceFieldAlignment.AsInt);

            Assert.Equal(0x20, tX64.InstanceFieldSize.AsInt);
            Assert.Equal(0x20, tARM.InstanceFieldSize.AsInt);
            Assert.Equal(0x20, tX86.InstanceFieldSize.AsInt);

            Assert.Equal(0x0, tX64.GetField("_1").Offset.AsInt);
            Assert.Equal(0x0, tARM.GetField("_1").Offset.AsInt);
            Assert.Equal(0x0, tX86.GetField("_1").Offset.AsInt);

            Assert.Equal(0x8, tX64.GetField("_2").Offset.AsInt);
            Assert.Equal(0x8, tARM.GetField("_2").Offset.AsInt);
            Assert.Equal(0x8, tX86.GetField("_2").Offset.AsInt);

            Assert.Equal(0x10, tX64.GetField("_3").Offset.AsInt);
            Assert.Equal(0x10, tARM.GetField("_3").Offset.AsInt);
            Assert.Equal(0x10, tX86.GetField("_3").Offset.AsInt);

            Assert.Equal(0x18, tX64.GetField("_4").Offset.AsInt);
            Assert.Equal(0x18, tARM.GetField("_4").Offset.AsInt);
            Assert.Equal(0x18, tX86.GetField("_4").Offset.AsInt);

            MetadataType tX64FieldStruct = _testModuleX64.GetType(_namespace, _type + "FieldStruct");
            MetadataType tX86FieldStruct = _testModuleX86.GetType(_namespace, _type + "FieldStruct");
            MetadataType tARMFieldStruct = _testModuleARM.GetType(_namespace, _type + "FieldStruct");

            Assert.Equal(0x8, tX64FieldStruct.GetField("_struct").Offset.AsInt);
            Assert.Equal(0x8, tX86FieldStruct.GetField("_struct").Offset.AsInt);
            Assert.Equal(0x8, tARMFieldStruct.GetField("_struct").Offset.AsInt);
        }

        [Fact]
        public void TestAlignmentBehavior_IntShortEnumStruct()
        {
            string _namespace = "EnumAlignment";
            string _type = "IntShortEnumStruct";

            MetadataType tX64 = _testModuleX64.GetType(_namespace, _type);
            MetadataType tX86 = _testModuleX86.GetType(_namespace, _type);
            MetadataType tARM = _testModuleARM.GetType(_namespace, _type);

            Assert.Equal(0x8, tX64.InstanceByteAlignment.AsInt);
            Assert.Equal(0x4, tARM.InstanceByteAlignment.AsInt);
            Assert.Equal(0x4, tX86.InstanceByteAlignment.AsInt);

            Assert.Equal(0x10, tX64.InstanceByteCountUnaligned.AsInt);
            Assert.Equal(0x10, tARM.InstanceByteCountUnaligned.AsInt);
            Assert.Equal(0x10, tX86.InstanceByteCountUnaligned.AsInt);

            Assert.Equal(0x10, tX64.InstanceByteCount.AsInt);
            Assert.Equal(0x10, tARM.InstanceByteCount.AsInt);
            Assert.Equal(0x10, tX86.InstanceByteCount.AsInt);

            Assert.Equal(0x4, tX64.InstanceFieldAlignment.AsInt);
            Assert.Equal(0x4, tARM.InstanceFieldAlignment.AsInt);
            Assert.Equal(0x4, tX86.InstanceFieldAlignment.AsInt);

            Assert.Equal(0x10, tX64.InstanceFieldSize.AsInt);
            Assert.Equal(0x10, tARM.InstanceFieldSize.AsInt);
            Assert.Equal(0x10, tX86.InstanceFieldSize.AsInt);

            Assert.Equal(0x0, tX64.GetField("_1").Offset.AsInt);
            Assert.Equal(0x0, tARM.GetField("_1").Offset.AsInt);
            Assert.Equal(0x0, tX86.GetField("_1").Offset.AsInt);

            Assert.Equal(0x4, tX64.GetField("_2").Offset.AsInt);
            Assert.Equal(0x4, tARM.GetField("_2").Offset.AsInt);
            Assert.Equal(0x4, tX86.GetField("_2").Offset.AsInt);

            Assert.Equal(0x8, tX64.GetField("_3").Offset.AsInt);
            Assert.Equal(0x8, tARM.GetField("_3").Offset.AsInt);
            Assert.Equal(0x8, tX86.GetField("_3").Offset.AsInt);

            Assert.Equal(0xC, tX64.GetField("_4").Offset.AsInt);
            Assert.Equal(0xC, tARM.GetField("_4").Offset.AsInt);
            Assert.Equal(0xC, tX86.GetField("_4").Offset.AsInt);

            MetadataType tX64FieldStruct = _testModuleX64.GetType(_namespace, _type + "FieldStruct");
            MetadataType tX86FieldStruct = _testModuleX86.GetType(_namespace, _type + "FieldStruct");
            MetadataType tARMFieldStruct = _testModuleARM.GetType(_namespace, _type + "FieldStruct");

            Assert.Equal(0x4, tX64FieldStruct.GetField("_struct").Offset.AsInt);
            Assert.Equal(0x4, tX86FieldStruct.GetField("_struct").Offset.AsInt);
            Assert.Equal(0x4, tARMFieldStruct.GetField("_struct").Offset.AsInt);

        }

        [Fact]
        public void TestAlignmentBehavior_ShortByteEnumStruct()
        {
            string _namespace = "EnumAlignment";
            string _type = "ShortByteEnumStruct";

            MetadataType tX64 = _testModuleX64.GetType(_namespace, _type);
            MetadataType tX86 = _testModuleX86.GetType(_namespace, _type);
            MetadataType tARM = _testModuleARM.GetType(_namespace, _type);

            Assert.Equal(0x8, tX64.InstanceByteAlignment.AsInt);
            Assert.Equal(0x4, tARM.InstanceByteAlignment.AsInt);
            Assert.Equal(0x4, tX86.InstanceByteAlignment.AsInt);

            Assert.Equal(0x8, tX64.InstanceByteCountUnaligned.AsInt);
            Assert.Equal(0x8, tARM.InstanceByteCountUnaligned.AsInt);
            Assert.Equal(0x8, tX86.InstanceByteCountUnaligned.AsInt);

            Assert.Equal(0x8, tX64.InstanceByteCount.AsInt);
            Assert.Equal(0x8, tARM.InstanceByteCount.AsInt);
            Assert.Equal(0x8, tX86.InstanceByteCount.AsInt);

            Assert.Equal(0x2, tX64.InstanceFieldAlignment.AsInt);
            Assert.Equal(0x2, tARM.InstanceFieldAlignment.AsInt);
            Assert.Equal(0x2, tX86.InstanceFieldAlignment.AsInt);

            Assert.Equal(0x8, tX64.InstanceFieldSize.AsInt);
            Assert.Equal(0x8, tARM.InstanceFieldSize.AsInt);
            Assert.Equal(0x8, tX86.InstanceFieldSize.AsInt);

            Assert.Equal(0x0, tX64.GetField("_1").Offset.AsInt);
            Assert.Equal(0x0, tARM.GetField("_1").Offset.AsInt);
            Assert.Equal(0x0, tX86.GetField("_1").Offset.AsInt);

            Assert.Equal(0x2, tX64.GetField("_2").Offset.AsInt);
            Assert.Equal(0x2, tARM.GetField("_2").Offset.AsInt);
            Assert.Equal(0x2, tX86.GetField("_2").Offset.AsInt);

            Assert.Equal(0x4, tX64.GetField("_3").Offset.AsInt);
            Assert.Equal(0x4, tARM.GetField("_3").Offset.AsInt);
            Assert.Equal(0x4, tX86.GetField("_3").Offset.AsInt);

            Assert.Equal(0x6, tX64.GetField("_4").Offset.AsInt);
            Assert.Equal(0x6, tARM.GetField("_4").Offset.AsInt);
            Assert.Equal(0x6, tX86.GetField("_4").Offset.AsInt);

            MetadataType tX64FieldStruct = _testModuleX64.GetType(_namespace, _type + "FieldStruct");
            MetadataType tX86FieldStruct = _testModuleX86.GetType(_namespace, _type + "FieldStruct");
            MetadataType tARMFieldStruct = _testModuleARM.GetType(_namespace, _type + "FieldStruct");

            Assert.Equal(0x2, tX64FieldStruct.GetField("_struct").Offset.AsInt);
            Assert.Equal(0x2, tX86FieldStruct.GetField("_struct").Offset.AsInt);
            Assert.Equal(0x2, tARMFieldStruct.GetField("_struct").Offset.AsInt);
        }

        [Fact]
        public void TestAlignmentBehavior_LongIntEnumStructAuto()
        {
            string _namespace = "EnumAlignment";
            string _type = "LongIntEnumStructAuto";

            MetadataType tX64 = _testModuleX64.GetType(_namespace, _type);
            MetadataType tX86 = _testModuleX86.GetType(_namespace, _type);
            MetadataType tARM = _testModuleARM.GetType(_namespace, _type);

            Assert.Equal(0x8, tX64.InstanceByteAlignment.AsInt);
            Assert.Equal(0x8, tARM.InstanceByteAlignment.AsInt);
            Assert.Equal(0x4, tX86.InstanceByteAlignment.AsInt);

            Assert.Equal(0x18, tX64.InstanceByteCountUnaligned.AsInt);
            Assert.Equal(0x18, tARM.InstanceByteCountUnaligned.AsInt);
            Assert.Equal(0x18, tX86.InstanceByteCountUnaligned.AsInt);

            Assert.Equal(0x18, tX64.InstanceByteCount.AsInt);
            Assert.Equal(0x18, tARM.InstanceByteCount.AsInt);
            Assert.Equal(0x18, tX86.InstanceByteCount.AsInt);

            Assert.Equal(0x8, tX64.InstanceFieldAlignment.AsInt);
            Assert.Equal(0x8, tARM.InstanceFieldAlignment.AsInt);
            Assert.Equal(0x4, tX86.InstanceFieldAlignment.AsInt);

            Assert.Equal(0x18, tX64.InstanceFieldSize.AsInt);
            Assert.Equal(0x18, tARM.InstanceFieldSize.AsInt);
            Assert.Equal(0x18, tX86.InstanceFieldSize.AsInt);

            Assert.Equal(0x0, tX64.GetField("_1").Offset.AsInt);
            Assert.Equal(0x0, tARM.GetField("_1").Offset.AsInt);
            Assert.Equal(0x0, tX86.GetField("_1").Offset.AsInt);

            Assert.Equal(0x10, tX64.GetField("_2").Offset.AsInt);
            Assert.Equal(0x10, tARM.GetField("_2").Offset.AsInt);
            Assert.Equal(0x10, tX86.GetField("_2").Offset.AsInt);

            Assert.Equal(0x8, tX64.GetField("_3").Offset.AsInt);
            Assert.Equal(0x8, tARM.GetField("_3").Offset.AsInt);
            Assert.Equal(0x8, tX86.GetField("_3").Offset.AsInt);

            Assert.Equal(0x14, tX64.GetField("_4").Offset.AsInt);
            Assert.Equal(0x14, tARM.GetField("_4").Offset.AsInt);
            Assert.Equal(0x14, tX86.GetField("_4").Offset.AsInt);

            MetadataType tX64FieldStruct = _testModuleX64.GetType(_namespace, _type + "FieldStruct");
            MetadataType tX86FieldStruct = _testModuleX86.GetType(_namespace, _type + "FieldStruct");
            MetadataType tARMFieldStruct = _testModuleARM.GetType(_namespace, _type + "FieldStruct");

            Assert.Equal(0x8, tX64FieldStruct.GetField("_struct").Offset.AsInt);
            Assert.Equal(0x4, tX86FieldStruct.GetField("_struct").Offset.AsInt);
            Assert.Equal(0x8, tARMFieldStruct.GetField("_struct").Offset.AsInt);
        }

        [Fact]
        public void TestAlignmentBehavior_IntShortEnumStructAuto()
        {
            string _namespace = "EnumAlignment";
            string _type = "IntShortEnumStructAuto";

            MetadataType tX64 = _testModuleX64.GetType(_namespace, _type);
            MetadataType tX86 = _testModuleX86.GetType(_namespace, _type);
            MetadataType tARM = _testModuleARM.GetType(_namespace, _type);

            Assert.Equal(0x8, tX64.InstanceByteAlignment.AsInt);
            Assert.Equal(0x4, tARM.InstanceByteAlignment.AsInt);
            Assert.Equal(0x4, tX86.InstanceByteAlignment.AsInt);

            Assert.Equal(0x10, tX64.InstanceByteCountUnaligned.AsInt);
            Assert.Equal(0xC, tARM.InstanceByteCountUnaligned.AsInt);
            Assert.Equal(0xC, tX86.InstanceByteCountUnaligned.AsInt);

            Assert.Equal(0x10, tX64.InstanceByteCount.AsInt);
            Assert.Equal(0xC, tARM.InstanceByteCount.AsInt);
            Assert.Equal(0xC, tX86.InstanceByteCount.AsInt);

            Assert.Equal(0x8, tX64.InstanceFieldAlignment.AsInt);
            Assert.Equal(0x4, tARM.InstanceFieldAlignment.AsInt);
            Assert.Equal(0x4, tX86.InstanceFieldAlignment.AsInt);

            Assert.Equal(0x10, tX64.InstanceFieldSize.AsInt);
            Assert.Equal(0xC, tARM.InstanceFieldSize.AsInt);
            Assert.Equal(0xC, tX86.InstanceFieldSize.AsInt);

            Assert.Equal(0x0, tX64.GetField("_1").Offset.AsInt);
            Assert.Equal(0x0, tARM.GetField("_1").Offset.AsInt);
            Assert.Equal(0x0, tX86.GetField("_1").Offset.AsInt);

            Assert.Equal(0x8, tX64.GetField("_2").Offset.AsInt);
            Assert.Equal(0x8, tARM.GetField("_2").Offset.AsInt);
            Assert.Equal(0x8, tX86.GetField("_2").Offset.AsInt);

            Assert.Equal(0x4, tX64.GetField("_3").Offset.AsInt);
            Assert.Equal(0x4, tARM.GetField("_3").Offset.AsInt);
            Assert.Equal(0x4, tX86.GetField("_3").Offset.AsInt);

            Assert.Equal(0xA, tX64.GetField("_4").Offset.AsInt);
            Assert.Equal(0xA, tARM.GetField("_4").Offset.AsInt);
            Assert.Equal(0xA, tX86.GetField("_4").Offset.AsInt);

            MetadataType tX64FieldStruct = _testModuleX64.GetType(_namespace, _type + "FieldStruct");
            MetadataType tX86FieldStruct = _testModuleX86.GetType(_namespace, _type + "FieldStruct");
            MetadataType tARMFieldStruct = _testModuleARM.GetType(_namespace, _type + "FieldStruct");

            Assert.Equal(0x8, tX64FieldStruct.GetField("_struct").Offset.AsInt);
            Assert.Equal(0x4, tX86FieldStruct.GetField("_struct").Offset.AsInt);
            Assert.Equal(0x4, tARMFieldStruct.GetField("_struct").Offset.AsInt);

        }

        [Fact]
        public void TestAlignmentBehavior_ShortByteEnumStructAuto()
        {
            string _namespace = "EnumAlignment";
            string _type = "ShortByteEnumStructAuto";

            MetadataType tX64 = _testModuleX64.GetType(_namespace, _type);
            MetadataType tX86 = _testModuleX86.GetType(_namespace, _type);
            MetadataType tARM = _testModuleARM.GetType(_namespace, _type);

            Assert.Equal(0x8, tX64.InstanceByteAlignment.AsInt);
            Assert.Equal(0x4, tARM.InstanceByteAlignment.AsInt);
            Assert.Equal(0x4, tX86.InstanceByteAlignment.AsInt);

            Assert.Equal(0x8, tX64.InstanceByteCountUnaligned.AsInt);
            Assert.Equal(0x8, tARM.InstanceByteCountUnaligned.AsInt);
            Assert.Equal(0x8, tX86.InstanceByteCountUnaligned.AsInt);

            Assert.Equal(0x8, tX64.InstanceByteCount.AsInt);
            Assert.Equal(0x8, tARM.InstanceByteCount.AsInt);
            Assert.Equal(0x8, tX86.InstanceByteCount.AsInt);

            Assert.Equal(0x8, tX64.InstanceFieldAlignment.AsInt);
            Assert.Equal(0x4, tARM.InstanceFieldAlignment.AsInt);
            Assert.Equal(0x4, tX86.InstanceFieldAlignment.AsInt);

            Assert.Equal(0x8, tX64.InstanceFieldSize.AsInt);
            Assert.Equal(0x8, tARM.InstanceFieldSize.AsInt);
            Assert.Equal(0x8, tX86.InstanceFieldSize.AsInt);

            Assert.Equal(0x0, tX64.GetField("_1").Offset.AsInt);
            Assert.Equal(0x0, tARM.GetField("_1").Offset.AsInt);
            Assert.Equal(0x0, tX86.GetField("_1").Offset.AsInt);

            Assert.Equal(0x4, tX64.GetField("_2").Offset.AsInt);
            Assert.Equal(0x4, tARM.GetField("_2").Offset.AsInt);
            Assert.Equal(0x4, tX86.GetField("_2").Offset.AsInt);

            Assert.Equal(0x2, tX64.GetField("_3").Offset.AsInt);
            Assert.Equal(0x2, tARM.GetField("_3").Offset.AsInt);
            Assert.Equal(0x2, tX86.GetField("_3").Offset.AsInt);

            Assert.Equal(0x5, tX64.GetField("_4").Offset.AsInt);
            Assert.Equal(0x5, tARM.GetField("_4").Offset.AsInt);
            Assert.Equal(0x5, tX86.GetField("_4").Offset.AsInt);

            MetadataType tX64FieldStruct = _testModuleX64.GetType(_namespace, _type + "FieldStruct");
            MetadataType tX86FieldStruct = _testModuleX86.GetType(_namespace, _type + "FieldStruct");
            MetadataType tARMFieldStruct = _testModuleARM.GetType(_namespace, _type + "FieldStruct");

            Assert.Equal(0x8, tX64FieldStruct.GetField("_struct").Offset.AsInt);
            Assert.Equal(0x4, tX86FieldStruct.GetField("_struct").Offset.AsInt);
            Assert.Equal(0x4, tARMFieldStruct.GetField("_struct").Offset.AsInt);
        }

        [Fact]
        public void TestAlignmentBehavior_StructStructByte_StructByteAuto()
        {
            string _namespace = "Sequential";
            string _type = "StructStructByte_StructByteAuto";

            MetadataType tX64 = _testModuleX64.GetType(_namespace, _type);
            MetadataType tX86 = _testModuleX86.GetType(_namespace, _type);
            MetadataType tARM = _testModuleARM.GetType(_namespace, _type);

            Assert.Equal(0x1, tX64.InstanceFieldAlignment.AsInt);
            Assert.Equal(0x1, tARM.InstanceFieldAlignment.AsInt);
            Assert.Equal(0x1, tX86.InstanceFieldAlignment.AsInt);

            Assert.Equal(0x2, tX64.InstanceFieldSize.AsInt);
            Assert.Equal(0x2, tARM.InstanceFieldSize.AsInt);
            Assert.Equal(0x2, tX86.InstanceFieldSize.AsInt);

            Assert.Equal(0x0, tX64.GetField("fld1").Offset.AsInt);
            Assert.Equal(0x0, tARM.GetField("fld1").Offset.AsInt);
            Assert.Equal(0x0, tX86.GetField("fld1").Offset.AsInt);

            Assert.Equal(0x1, tX64.GetField("fld2").Offset.AsInt);
            Assert.Equal(0x1, tARM.GetField("fld2").Offset.AsInt);
            Assert.Equal(0x1, tX86.GetField("fld2").Offset.AsInt);
        }

        [Theory]
        [InlineData("StructStructByte_StructByteAuto", new int[] { 1, 1, 1 }, new int[] { 2, 2, 2 })]
        [InlineData("StructStructByte_Struct2BytesAuto", new int[] { 2, 2, 2 }, new int[] { 4, 4, 4 })]
        [InlineData("StructStructByte_Struct3BytesAuto", new int[] { 4, 4, 4 }, new int[] { 8, 8, 8 })]
        [InlineData("StructStructByte_Struct4BytesAuto", new int[] { 4, 4, 4 }, new int[] { 8, 8, 8 })]
        [InlineData("StructStructByte_Struct5BytesAuto", new int[] { 8, 4, 4 }, new int[] { 16, 12, 12 })]
        [InlineData("StructStructByte_Struct8BytesAuto", new int[] { 8, 4, 4 }, new int[] { 16, 12, 12 })]
        [InlineData("StructStructByte_Struct9BytesAuto", new int[] { 8, 4, 4 }, new int[] { 24, 16, 16 })]
        public void TestAlignmentBehavior_AutoAlignmentRules(string wrapperType, int[] alignment, int[] size)
        {
            string _namespace = "Sequential";
            string _type = wrapperType;

            MetadataType tX64 = _testModuleX64.GetType(_namespace, _type);
            MetadataType tX86 = _testModuleX86.GetType(_namespace, _type);
            MetadataType tARM = _testModuleARM.GetType(_namespace, _type);

            Assert.Equal(alignment[0], tX64.InstanceFieldAlignment.AsInt);
            Assert.Equal(alignment[1], tARM.InstanceFieldAlignment.AsInt);
            Assert.Equal(alignment[2], tX86.InstanceFieldAlignment.AsInt);

            Assert.Equal(size[0], tX64.InstanceFieldSize.AsInt);
            Assert.Equal(size[1], tARM.InstanceFieldSize.AsInt);
            Assert.Equal(size[2], tX86.InstanceFieldSize.AsInt);

            Assert.Equal(0x0, tX64.GetField("fld1").Offset.AsInt);
            Assert.Equal(0x0, tARM.GetField("fld1").Offset.AsInt);
            Assert.Equal(0x0, tX86.GetField("fld1").Offset.AsInt);

            Assert.Equal(alignment[0], tX64.GetField("fld2").Offset.AsInt);
            Assert.Equal(alignment[1], tARM.GetField("fld2").Offset.AsInt);
            Assert.Equal(alignment[2], tX86.GetField("fld2").Offset.AsInt);
        }

        [Theory]
        [InlineData("StructStructByte_Int128StructAuto", "ARM64", 16, 32)]
        [InlineData("StructStructByte_Int128StructAuto", "ARM", 8, 24)]
        [InlineData("StructStructByte_Int128StructAuto", "X86", 16, 32)]
        [InlineData("StructStructByte_Int128StructAuto", "X64Linux", 16, 32)]
        [InlineData("StructStructByte_Int128StructAuto", "X64Windows", 16, 32)]
        [InlineData("StructStructByte_UInt128StructAuto", "ARM64", 16, 32)]
        [InlineData("StructStructByte_UInt128StructAuto", "ARM", 8, 24)]
        [InlineData("StructStructByte_UInt128StructAuto", "X86", 16, 32)]
        [InlineData("StructStructByte_UInt128StructAuto", "X64Linux", 16, 32)]
        [InlineData("StructStructByte_UInt128StructAuto", "X64Windows", 16, 32)]
        // Variation of TestAlignmentBehavior_AutoAlignmentRules above that is able to deal with os specific behavior
        public void TestAlignmentBehavior_AutoAlignmentRulesWithOSDependence(string wrapperType, string osArch, int alignment, int size)
        {
            ModuleDesc testModule;
            switch (osArch)
            {
                case "ARM64":
                    testModule = _testModuleARM64;
                    break;
                case "ARM":
                    testModule = _testModuleARM;
                    break;
                case "X64":
                    testModule = _testModuleX64;
                    break;
                case "X64Linux":
                    testModule = _testModuleX64Linux;
                    break;
                case "X64Windows":
                    testModule = _testModuleX64Windows;
                    break;
                case "X86":
                    testModule = _testModuleX86;
                    break;
                default:
                    throw new Exception();
            }

            string _namespace = "Sequential";
            string _type = wrapperType;

            MetadataType type = testModule.GetType(_namespace, _type);

            Assert.Equal(alignment, type.InstanceFieldAlignment.AsInt);
            Assert.Equal(size, type.InstanceFieldSize.AsInt);
            Assert.Equal(0x0, type.GetField("fld1").Offset.AsInt);
            Assert.Equal(alignment, type.GetField("fld2").Offset.AsInt);
        }
    }
}
