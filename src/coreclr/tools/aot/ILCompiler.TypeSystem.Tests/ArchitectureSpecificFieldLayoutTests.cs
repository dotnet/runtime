// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Internal.TypeSystem.Ecma;
using Internal.TypeSystem;

using Xunit;

namespace TypeSystemTests
{
    public class ArchitectureSpecificFieldLayoutTests
    {
        TestTypeSystemContext _contextX86;
        ModuleDesc _testModuleX86;
        TestTypeSystemContext _contextX64;
        ModuleDesc _testModuleX64;
        TestTypeSystemContext _contextARM;
        ModuleDesc _testModuleARM;

        public ArchitectureSpecificFieldLayoutTests()
        {
            _contextX64 = new TestTypeSystemContext(TargetArchitecture.X64);
            var systemModuleX64 = _contextX64.CreateModuleForSimpleName("CoreTestAssembly");
            _contextX64.SetSystemModule(systemModuleX64);

            _testModuleX64 = systemModuleX64;

            _contextARM = new TestTypeSystemContext(TargetArchitecture.ARM);
            var systemModuleARM = _contextARM.CreateModuleForSimpleName("CoreTestAssembly");
            _contextARM.SetSystemModule(systemModuleARM);

            _testModuleARM = systemModuleARM;

            _contextX86 = new TestTypeSystemContext(TargetArchitecture.X86);
            var systemModuleX86 = _contextX86.CreateModuleForSimpleName("CoreTestAssembly");
            _contextX86.SetSystemModule(systemModuleX86);

            _testModuleX86 = systemModuleX86;
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
            Assert.Equal(0xC,  tARM.InstanceByteCountUnaligned.AsInt);
            Assert.Equal(0xC,  tX86.InstanceByteCountUnaligned.AsInt);

            Assert.Equal(0x10, tX64.InstanceByteCount.AsInt);
            Assert.Equal(0xC,  tARM.InstanceByteCount.AsInt);
            Assert.Equal(0xC,  tX86.InstanceByteCount.AsInt);

            Assert.Equal(0x8, tX64.InstanceFieldAlignment.AsInt);
            Assert.Equal(0x4, tARM.InstanceFieldAlignment.AsInt);
            Assert.Equal(0x4, tX86.InstanceFieldAlignment.AsInt);

            Assert.Equal(0x10, tX64.InstanceFieldSize.AsInt);
            Assert.Equal(0xC,  tARM.InstanceFieldSize.AsInt);
            Assert.Equal(0xC,  tX86.InstanceFieldSize.AsInt);

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
    }
}
