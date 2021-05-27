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
        TestTypeSystemContext _contextX86Windows;
        ModuleDesc _testModuleX86Windows;
        TestTypeSystemContext _contextX86Unix;
        ModuleDesc _testModuleX86Unix;
        TestTypeSystemContext _contextX64;
        ModuleDesc _testModuleX64;
        TestTypeSystemContext _contextARM;
        ModuleDesc _testModuleARM;

        public ArchitectureSpecificFieldLayoutTests()
        {
            _contextX64 = new TestTypeSystemContext(TargetArchitecture.X64, TargetOS.Unknown);
            var systemModuleX64 = _contextX64.CreateModuleForSimpleName("CoreTestAssembly");
            _contextX64.SetSystemModule(systemModuleX64);

            _testModuleX64 = systemModuleX64;

            _contextARM = new TestTypeSystemContext(TargetArchitecture.ARM, TargetOS.Unknown);
            var systemModuleARM = _contextARM.CreateModuleForSimpleName("CoreTestAssembly");
            _contextARM.SetSystemModule(systemModuleARM);

            _testModuleARM = systemModuleARM;

            _contextX86Windows = new TestTypeSystemContext(TargetArchitecture.X86, TargetOS.Windows);
            var systemModuleX86Windows = _contextX86Windows.CreateModuleForSimpleName("CoreTestAssembly");
            _contextX86Windows.SetSystemModule(systemModuleX86Windows);

            _testModuleX86Windows = systemModuleX86Windows;

            _contextX86Unix = new TestTypeSystemContext(TargetArchitecture.X86, TargetOS.Linux);
            var systemModuleX86Unix = _contextX86Unix.CreateModuleForSimpleName("CoreTestAssembly");
            _contextX86Unix.SetSystemModule(systemModuleX86Unix);

            _testModuleX86Unix = systemModuleX86Unix;
        }

        [Fact]
        public void TestInstanceLayoutDoubleBool()
        {
            MetadataType tX64 = _testModuleX64.GetType("Sequential", "ClassDoubleBool");
            MetadataType tX86Windows = _testModuleX86Windows.GetType("Sequential", "ClassDoubleBool");
            MetadataType tX86Unix = _testModuleX86Unix.GetType("Sequential", "ClassDoubleBool");
            MetadataType tARM = _testModuleARM.GetType("Sequential", "ClassDoubleBool");

            Assert.Equal(0x8, tX64.InstanceByteAlignment.AsInt);
            Assert.Equal(0x8, tARM.InstanceByteAlignment.AsInt);
            Assert.Equal(0x4, tX86Windows.InstanceByteAlignment.AsInt);
            Assert.Equal(0x4, tX86Unix.InstanceByteAlignment.AsInt);

            Assert.Equal(0x11, tX64.InstanceByteCountUnaligned.AsInt);
            Assert.Equal(0xD, tARM.InstanceByteCountUnaligned.AsInt);
            Assert.Equal(0xD, tX86Windows.InstanceByteCountUnaligned.AsInt);
            Assert.Equal(0xD, tX86Unix.InstanceByteCountUnaligned.AsInt);

            Assert.Equal(0x18, tX64.InstanceByteCount.AsInt);
            Assert.Equal(0x10, tARM.InstanceByteCount.AsInt);
            Assert.Equal(0x10, tX86Windows.InstanceByteCount.AsInt);
            Assert.Equal(0x10, tX86Unix.InstanceByteCount.AsInt);
        }

        [Fact]
        public void TestInstanceLayoutBoolDoubleBool()
        {
            MetadataType tX64 = _testModuleX64.GetType("Sequential", "ClassBoolDoubleBool");
            MetadataType tX86Windows = _testModuleX86Windows.GetType("Sequential", "ClassBoolDoubleBool");
            MetadataType tX86Unix = _testModuleX86Unix.GetType("Sequential", "ClassBoolDoubleBool");
            MetadataType tARM = _testModuleARM.GetType("Sequential", "ClassBoolDoubleBool");

            Assert.Equal(0x8, tX64.InstanceByteAlignment.AsInt);
            Assert.Equal(0x8, tARM.InstanceByteAlignment.AsInt);
            Assert.Equal(0x4, tX86Windows.InstanceByteAlignment.AsInt);
            Assert.Equal(0x4, tX86Unix.InstanceByteAlignment.AsInt);

            Assert.Equal(0x19, tX64.InstanceByteCountUnaligned.AsInt);
            Assert.Equal(0x15, tARM.InstanceByteCountUnaligned.AsInt);
            Assert.Equal(0x15, tX86Windows.InstanceByteCountUnaligned.AsInt);
            Assert.Equal(0x11, tX86Unix.InstanceByteCountUnaligned.AsInt);

            Assert.Equal(0x20, tX64.InstanceByteCount.AsInt);
            Assert.Equal(0x18, tARM.InstanceByteCount.AsInt);
            Assert.Equal(0x18, tX86Windows.InstanceByteCount.AsInt);
            Assert.Equal(0x14, tX86Unix.InstanceByteCount.AsInt);
        }

        [Fact]
        public void TestAlignmentBehavior_LongIntEnumStruct()
        {
            string _namespace = "EnumAlignment";
            string _type = "LongIntEnumStruct";

            MetadataType tX64 = _testModuleX64.GetType(_namespace, _type);
            MetadataType tX86Windows = _testModuleX86Windows.GetType(_namespace, _type);
            MetadataType tX86Unix = _testModuleX86Unix.GetType(_namespace, _type);
            MetadataType tARM = _testModuleARM.GetType(_namespace, _type);

            Assert.Equal(0x8, tX64.InstanceByteAlignment.AsInt);
            Assert.Equal(0x8, tARM.InstanceByteAlignment.AsInt);
            Assert.Equal(0x4, tX86Windows.InstanceByteAlignment.AsInt);
            Assert.Equal(0x4, tX86Unix.InstanceByteAlignment.AsInt);

            Assert.Equal(0x20, tX64.InstanceByteCountUnaligned.AsInt);
            Assert.Equal(0x20, tARM.InstanceByteCountUnaligned.AsInt);
            Assert.Equal(0x1C, tX86Windows.InstanceByteCountUnaligned.AsInt);
            Assert.Equal(0x18, tX86Unix.InstanceByteCountUnaligned.AsInt);

            Assert.Equal(0x20, tX64.InstanceByteCount.AsInt);
            Assert.Equal(0x20, tARM.InstanceByteCount.AsInt);
            Assert.Equal(0x1C, tX86Windows.InstanceByteCount.AsInt);
            Assert.Equal(0x18, tX86Unix.InstanceByteCount.AsInt);

            Assert.Equal(0x8, tX64.InstanceFieldAlignment.AsInt);
            Assert.Equal(0x8, tARM.InstanceFieldAlignment.AsInt);
            Assert.Equal(0x8, tX86Windows.InstanceFieldAlignment.AsInt);
            Assert.Equal(0x4, tX86Unix.InstanceFieldAlignment.AsInt);

            Assert.Equal(0x20, tX64.InstanceFieldSize.AsInt);
            Assert.Equal(0x20, tARM.InstanceFieldSize.AsInt);
            Assert.Equal(0x1C, tX86Windows.InstanceFieldSize.AsInt);
            Assert.Equal(0x18, tX86Unix.InstanceFieldSize.AsInt);

            Assert.Equal(0x0, tX64.GetField("_1").Offset.AsInt);
            Assert.Equal(0x0, tARM.GetField("_1").Offset.AsInt);
            Assert.Equal(0x0, tX86Windows.GetField("_1").Offset.AsInt);
            Assert.Equal(0x0, tX86Unix.GetField("_1").Offset.AsInt);

            Assert.Equal(0x8, tX64.GetField("_2").Offset.AsInt);
            Assert.Equal(0x8, tARM.GetField("_2").Offset.AsInt);
            Assert.Equal(0x8, tX86Windows.GetField("_2").Offset.AsInt);
            Assert.Equal(0x8, tX86Unix.GetField("_2").Offset.AsInt);

            Assert.Equal(0x10, tX64.GetField("_3").Offset.AsInt);
            Assert.Equal(0x10, tARM.GetField("_3").Offset.AsInt);
            Assert.Equal(0x10, tX86Windows.GetField("_3").Offset.AsInt);
            Assert.Equal(0x0C, tX86Unix.GetField("_3").Offset.AsInt);

            Assert.Equal(0x18, tX64.GetField("_4").Offset.AsInt);
            Assert.Equal(0x18, tARM.GetField("_4").Offset.AsInt);
            Assert.Equal(0x18, tX86Windows.GetField("_4").Offset.AsInt);
            Assert.Equal(0x14, tX86Unix.GetField("_4").Offset.AsInt);

            MetadataType tX64FieldStruct = _testModuleX64.GetType(_namespace, _type + "FieldStruct");
            MetadataType tX86FieldStructWindows = _testModuleX86Windows.GetType(_namespace, _type + "FieldStruct");
            MetadataType tX86FieldStructUnix = _testModuleX86Unix.GetType(_namespace, _type + "FieldStruct");
            MetadataType tARMFieldStruct = _testModuleARM.GetType(_namespace, _type + "FieldStruct");

            Assert.Equal(0x8, tX64FieldStruct.GetField("_struct").Offset.AsInt);
            Assert.Equal(0x4, tX86FieldStructWindows.GetField("_struct").Offset.AsInt);
            Assert.Equal(0x4, tX86FieldStructUnix.GetField("_struct").Offset.AsInt);
            Assert.Equal(0x8, tARMFieldStruct.GetField("_struct").Offset.AsInt);
        }

        [Fact]
        public void TestAlignmentBehavior_IntShortEnumStruct()
        {
            string _namespace = "EnumAlignment";
            string _type = "IntShortEnumStruct";

            MetadataType tX64 = _testModuleX64.GetType(_namespace, _type);
            MetadataType tX86Windows = _testModuleX86Windows.GetType(_namespace, _type);
            MetadataType tX86Unix = _testModuleX86Unix.GetType(_namespace, _type);
            MetadataType tARM = _testModuleARM.GetType(_namespace, _type);

            Assert.Equal(0x8, tX64.InstanceByteAlignment.AsInt);
            Assert.Equal(0x4, tARM.InstanceByteAlignment.AsInt);
            Assert.Equal(0x4, tX86Windows.InstanceByteAlignment.AsInt);
            Assert.Equal(0x4, tX86Unix.InstanceByteAlignment.AsInt);

            Assert.Equal(0x10, tX64.InstanceByteCountUnaligned.AsInt);
            Assert.Equal(0x10, tARM.InstanceByteCountUnaligned.AsInt);
            Assert.Equal(0x10, tX86Windows.InstanceByteCountUnaligned.AsInt);
            Assert.Equal(0x10, tX86Unix.InstanceByteCountUnaligned.AsInt);

            Assert.Equal(0x10, tX64.InstanceByteCount.AsInt);
            Assert.Equal(0x10, tARM.InstanceByteCount.AsInt);
            Assert.Equal(0x10, tX86Windows.InstanceByteCount.AsInt);
            Assert.Equal(0x10, tX86Unix.InstanceByteCount.AsInt);

            Assert.Equal(0x4, tX64.InstanceFieldAlignment.AsInt);
            Assert.Equal(0x4, tARM.InstanceFieldAlignment.AsInt);
            Assert.Equal(0x4, tX86Windows.InstanceFieldAlignment.AsInt);
            Assert.Equal(0x4, tX86Unix.InstanceFieldAlignment.AsInt);

            Assert.Equal(0x10, tX64.InstanceFieldSize.AsInt);
            Assert.Equal(0x10, tARM.InstanceFieldSize.AsInt);
            Assert.Equal(0x10, tX86Windows.InstanceFieldSize.AsInt);
            Assert.Equal(0x10, tX86Unix.InstanceFieldSize.AsInt);

            Assert.Equal(0x0, tX64.GetField("_1").Offset.AsInt);
            Assert.Equal(0x0, tARM.GetField("_1").Offset.AsInt);
            Assert.Equal(0x0, tX86Windows.GetField("_1").Offset.AsInt);
            Assert.Equal(0x0, tX86Unix.GetField("_1").Offset.AsInt);

            Assert.Equal(0x4, tX64.GetField("_2").Offset.AsInt);
            Assert.Equal(0x4, tARM.GetField("_2").Offset.AsInt);
            Assert.Equal(0x4, tX86Windows.GetField("_2").Offset.AsInt);
            Assert.Equal(0x4, tX86Unix.GetField("_2").Offset.AsInt);

            Assert.Equal(0x8, tX64.GetField("_3").Offset.AsInt);
            Assert.Equal(0x8, tARM.GetField("_3").Offset.AsInt);
            Assert.Equal(0x8, tX86Windows.GetField("_3").Offset.AsInt);
            Assert.Equal(0x8, tX86Unix.GetField("_3").Offset.AsInt);

            Assert.Equal(0xC, tX64.GetField("_4").Offset.AsInt);
            Assert.Equal(0xC, tARM.GetField("_4").Offset.AsInt);
            Assert.Equal(0xC, tX86Windows.GetField("_4").Offset.AsInt);
            Assert.Equal(0xC, tX86Unix.GetField("_4").Offset.AsInt);

            MetadataType tX64FieldStruct = _testModuleX64.GetType(_namespace, _type + "FieldStruct");
            MetadataType tX86FieldStructWindows = _testModuleX86Windows.GetType(_namespace, _type + "FieldStruct");
            MetadataType tX86FieldStructUnix = _testModuleX86Unix.GetType(_namespace, _type + "FieldStruct");
            MetadataType tARMFieldStruct = _testModuleARM.GetType(_namespace, _type + "FieldStruct");

            Assert.Equal(0x4, tX64FieldStruct.GetField("_struct").Offset.AsInt);
            Assert.Equal(0x4, tX86FieldStructWindows.GetField("_struct").Offset.AsInt);
            Assert.Equal(0x4, tX86FieldStructUnix.GetField("_struct").Offset.AsInt);
            Assert.Equal(0x4, tARMFieldStruct.GetField("_struct").Offset.AsInt);

        }

        [Fact]
        public void TestAlignmentBehavior_ShortByteEnumStruct()
        {
            string _namespace = "EnumAlignment";
            string _type = "ShortByteEnumStruct";

            MetadataType tX64 = _testModuleX64.GetType(_namespace, _type);
            MetadataType tX86Windows = _testModuleX86Windows.GetType(_namespace, _type);
            MetadataType tX86Unix = _testModuleX86Unix.GetType(_namespace, _type);
            MetadataType tARM = _testModuleARM.GetType(_namespace, _type);

            Assert.Equal(0x8, tX64.InstanceByteAlignment.AsInt);
            Assert.Equal(0x4, tARM.InstanceByteAlignment.AsInt);
            Assert.Equal(0x4, tX86Windows.InstanceByteAlignment.AsInt);
            Assert.Equal(0x4, tX86Unix.InstanceByteAlignment.AsInt);

            Assert.Equal(0x8, tX64.InstanceByteCountUnaligned.AsInt);
            Assert.Equal(0x8, tARM.InstanceByteCountUnaligned.AsInt);
            Assert.Equal(0x8, tX86Windows.InstanceByteCountUnaligned.AsInt);
            Assert.Equal(0x8, tX86Unix.InstanceByteCountUnaligned.AsInt);

            Assert.Equal(0x8, tX64.InstanceByteCount.AsInt);
            Assert.Equal(0x8, tARM.InstanceByteCount.AsInt);
            Assert.Equal(0x8, tX86Windows.InstanceByteCount.AsInt);
            Assert.Equal(0x8, tX86Unix.InstanceByteCount.AsInt);

            Assert.Equal(0x2, tX64.InstanceFieldAlignment.AsInt);
            Assert.Equal(0x2, tARM.InstanceFieldAlignment.AsInt);
            Assert.Equal(0x2, tX86Windows.InstanceFieldAlignment.AsInt);
            Assert.Equal(0x2, tX86Unix.InstanceFieldAlignment.AsInt);

            Assert.Equal(0x8, tX64.InstanceFieldSize.AsInt);
            Assert.Equal(0x8, tARM.InstanceFieldSize.AsInt);
            Assert.Equal(0x8, tX86Windows.InstanceFieldSize.AsInt);
            Assert.Equal(0x8, tX86Unix.InstanceFieldSize.AsInt);

            Assert.Equal(0x0, tX64.GetField("_1").Offset.AsInt);
            Assert.Equal(0x0, tARM.GetField("_1").Offset.AsInt);
            Assert.Equal(0x0, tX86Windows.GetField("_1").Offset.AsInt);
            Assert.Equal(0x0, tX86Unix.GetField("_1").Offset.AsInt);

            Assert.Equal(0x2, tX64.GetField("_2").Offset.AsInt);
            Assert.Equal(0x2, tARM.GetField("_2").Offset.AsInt);
            Assert.Equal(0x2, tX86Windows.GetField("_2").Offset.AsInt);
            Assert.Equal(0x2, tX86Unix.GetField("_2").Offset.AsInt);

            Assert.Equal(0x4, tX64.GetField("_3").Offset.AsInt);
            Assert.Equal(0x4, tARM.GetField("_3").Offset.AsInt);
            Assert.Equal(0x4, tX86Windows.GetField("_3").Offset.AsInt);
            Assert.Equal(0x4, tX86Unix.GetField("_3").Offset.AsInt);

            Assert.Equal(0x6, tX64.GetField("_4").Offset.AsInt);
            Assert.Equal(0x6, tARM.GetField("_4").Offset.AsInt);
            Assert.Equal(0x6, tX86Windows.GetField("_4").Offset.AsInt);
            Assert.Equal(0x6, tX86Unix.GetField("_4").Offset.AsInt);

            MetadataType tX64FieldStruct = _testModuleX64.GetType(_namespace, _type + "FieldStruct");
            MetadataType tX86FieldStructWindows = _testModuleX86Windows.GetType(_namespace, _type + "FieldStruct");
            MetadataType tX86FieldStructUnix = _testModuleX86Unix.GetType(_namespace, _type + "FieldStruct");
            MetadataType tARMFieldStruct = _testModuleARM.GetType(_namespace, _type + "FieldStruct");

            Assert.Equal(0x2, tX64FieldStruct.GetField("_struct").Offset.AsInt);
            Assert.Equal(0x2, tX86FieldStructWindows.GetField("_struct").Offset.AsInt);
            Assert.Equal(0x2, tX86FieldStructUnix.GetField("_struct").Offset.AsInt);
            Assert.Equal(0x2, tARMFieldStruct.GetField("_struct").Offset.AsInt);
        }

        [Fact]
        public void TestAlignmentBehavior_LongIntEnumStructAuto()
        {
            string _namespace = "EnumAlignment";
            string _type = "LongIntEnumStructAuto";

            MetadataType tX64 = _testModuleX64.GetType(_namespace, _type);
            MetadataType tX86Windows = _testModuleX86Windows.GetType(_namespace, _type);
            MetadataType tX86Unix = _testModuleX86Unix.GetType(_namespace, _type);
            MetadataType tARM = _testModuleARM.GetType(_namespace, _type);

            Assert.Equal(0x8, tX64.InstanceByteAlignment.AsInt);
            Assert.Equal(0x8, tARM.InstanceByteAlignment.AsInt);
            Assert.Equal(0x4, tX86Windows.InstanceByteAlignment.AsInt);
            Assert.Equal(0x4, tX86Unix.InstanceByteAlignment.AsInt);

            Assert.Equal(0x18, tX64.InstanceByteCountUnaligned.AsInt);
            Assert.Equal(0x18, tARM.InstanceByteCountUnaligned.AsInt);
            Assert.Equal(0x18, tX86Windows.InstanceByteCountUnaligned.AsInt);
            Assert.Equal(0x18, tX86Unix.InstanceByteCountUnaligned.AsInt);

            Assert.Equal(0x18, tX64.InstanceByteCount.AsInt);
            Assert.Equal(0x18, tARM.InstanceByteCount.AsInt);
            Assert.Equal(0x18, tX86Windows.InstanceByteCount.AsInt);
            Assert.Equal(0x18, tX86Unix.InstanceByteCount.AsInt);

            Assert.Equal(0x8, tX64.InstanceFieldAlignment.AsInt);
            Assert.Equal(0x8, tARM.InstanceFieldAlignment.AsInt);
            Assert.Equal(0x4, tX86Windows.InstanceFieldAlignment.AsInt);
            Assert.Equal(0x4, tX86Unix.InstanceFieldAlignment.AsInt);

            Assert.Equal(0x18, tX64.InstanceFieldSize.AsInt);
            Assert.Equal(0x18, tARM.InstanceFieldSize.AsInt);
            Assert.Equal(0x18, tX86Windows.InstanceFieldSize.AsInt);
            Assert.Equal(0x18, tX86Unix.InstanceFieldSize.AsInt);

            Assert.Equal(0x0, tX64.GetField("_1").Offset.AsInt);
            Assert.Equal(0x0, tARM.GetField("_1").Offset.AsInt);
            Assert.Equal(0x0, tX86Windows.GetField("_1").Offset.AsInt);
            Assert.Equal(0x0, tX86Unix.GetField("_1").Offset.AsInt);

            Assert.Equal(0x10, tX64.GetField("_2").Offset.AsInt);
            Assert.Equal(0x10, tARM.GetField("_2").Offset.AsInt);
            Assert.Equal(0x10, tX86Windows.GetField("_2").Offset.AsInt);
            Assert.Equal(0x10, tX86Unix.GetField("_2").Offset.AsInt);

            Assert.Equal(0x8, tX64.GetField("_3").Offset.AsInt);
            Assert.Equal(0x8, tARM.GetField("_3").Offset.AsInt);
            Assert.Equal(0x8, tX86Windows.GetField("_3").Offset.AsInt);
            Assert.Equal(0x8, tX86Unix.GetField("_3").Offset.AsInt);

            Assert.Equal(0x14, tX64.GetField("_4").Offset.AsInt);
            Assert.Equal(0x14, tARM.GetField("_4").Offset.AsInt);
            Assert.Equal(0x14, tX86Windows.GetField("_4").Offset.AsInt);
            Assert.Equal(0x14, tX86Unix.GetField("_4").Offset.AsInt);

            MetadataType tX64FieldStruct = _testModuleX64.GetType(_namespace, _type + "FieldStruct");
            MetadataType tX86WindowsFieldStruct = _testModuleX86Windows.GetType(_namespace, _type + "FieldStruct");
            MetadataType tX86UnixFieldStruct = _testModuleX86Unix.GetType(_namespace, _type + "FieldStruct");
            MetadataType tARMFieldStruct = _testModuleARM.GetType(_namespace, _type + "FieldStruct");

            Assert.Equal(0x8, tX64FieldStruct.GetField("_struct").Offset.AsInt);
            Assert.Equal(0x4, tX86WindowsFieldStruct.GetField("_struct").Offset.AsInt);
            Assert.Equal(0x4, tX86UnixFieldStruct.GetField("_struct").Offset.AsInt);
            Assert.Equal(0x8, tARMFieldStruct.GetField("_struct").Offset.AsInt);
        }

        [Fact]
        public void TestAlignmentBehavior_IntShortEnumStructAuto()
        {
            string _namespace = "EnumAlignment";
            string _type = "IntShortEnumStructAuto";

            MetadataType tX64 = _testModuleX64.GetType(_namespace, _type);
            MetadataType tX86Windows = _testModuleX86Windows.GetType(_namespace, _type);
            MetadataType tX86Unix = _testModuleX86Unix.GetType(_namespace, _type);
            MetadataType tARM = _testModuleARM.GetType(_namespace, _type);

            Assert.Equal(0x8, tX64.InstanceByteAlignment.AsInt);
            Assert.Equal(0x4, tARM.InstanceByteAlignment.AsInt);
            Assert.Equal(0x4, tX86Windows.InstanceByteAlignment.AsInt);
            Assert.Equal(0x4, tX86Unix.InstanceByteAlignment.AsInt);

            Assert.Equal(0x10, tX64.InstanceByteCountUnaligned.AsInt);
            Assert.Equal(0xC,  tARM.InstanceByteCountUnaligned.AsInt);
            Assert.Equal(0xC,  tX86Windows.InstanceByteCountUnaligned.AsInt);
            Assert.Equal(0xC,  tX86Unix.InstanceByteCountUnaligned.AsInt);

            Assert.Equal(0x10, tX64.InstanceByteCount.AsInt);
            Assert.Equal(0xC,  tARM.InstanceByteCount.AsInt);
            Assert.Equal(0xC,  tX86Windows.InstanceByteCount.AsInt);
            Assert.Equal(0xC,  tX86Unix.InstanceByteCount.AsInt);

            Assert.Equal(0x8, tX64.InstanceFieldAlignment.AsInt);
            Assert.Equal(0x4, tARM.InstanceFieldAlignment.AsInt);
            Assert.Equal(0x4, tX86Windows.InstanceFieldAlignment.AsInt);
            Assert.Equal(0x4, tX86Unix.InstanceFieldAlignment.AsInt);

            Assert.Equal(0x10, tX64.InstanceFieldSize.AsInt);
            Assert.Equal(0xC,  tARM.InstanceFieldSize.AsInt);
            Assert.Equal(0xC,  tX86Windows.InstanceFieldSize.AsInt);
            Assert.Equal(0xC,  tX86Unix.InstanceFieldSize.AsInt);

            Assert.Equal(0x0, tX64.GetField("_1").Offset.AsInt);
            Assert.Equal(0x0, tARM.GetField("_1").Offset.AsInt);
            Assert.Equal(0x0, tX86Windows.GetField("_1").Offset.AsInt);
            Assert.Equal(0x0, tX86Unix.GetField("_1").Offset.AsInt);

            Assert.Equal(0x8, tX64.GetField("_2").Offset.AsInt);
            Assert.Equal(0x8, tARM.GetField("_2").Offset.AsInt);
            Assert.Equal(0x8, tX86Windows.GetField("_2").Offset.AsInt);
            Assert.Equal(0x8, tX86Unix.GetField("_2").Offset.AsInt);

            Assert.Equal(0x4, tX64.GetField("_3").Offset.AsInt);
            Assert.Equal(0x4, tARM.GetField("_3").Offset.AsInt);
            Assert.Equal(0x4, tX86Windows.GetField("_3").Offset.AsInt);
            Assert.Equal(0x4, tX86Unix.GetField("_3").Offset.AsInt);

            Assert.Equal(0xA, tX64.GetField("_4").Offset.AsInt);
            Assert.Equal(0xA, tARM.GetField("_4").Offset.AsInt);
            Assert.Equal(0xA, tX86Windows.GetField("_4").Offset.AsInt);
            Assert.Equal(0xA, tX86Unix.GetField("_4").Offset.AsInt);

            MetadataType tX64FieldStruct = _testModuleX64.GetType(_namespace, _type + "FieldStruct");
            MetadataType tX86FieldStructWindows = _testModuleX86Windows.GetType(_namespace, _type + "FieldStruct");
            MetadataType tX86FieldStructUnix = _testModuleX86Unix.GetType(_namespace, _type + "FieldStruct");
            MetadataType tARMFieldStruct = _testModuleARM.GetType(_namespace, _type + "FieldStruct");

            Assert.Equal(0x8, tX64FieldStruct.GetField("_struct").Offset.AsInt);
            Assert.Equal(0x4, tX86FieldStructWindows.GetField("_struct").Offset.AsInt);
            Assert.Equal(0x4, tX86FieldStructUnix.GetField("_struct").Offset.AsInt);
            Assert.Equal(0x4, tARMFieldStruct.GetField("_struct").Offset.AsInt);

        }

        [Fact]
        public void TestAlignmentBehavior_ShortByteEnumStructAuto()
        {
            string _namespace = "EnumAlignment";
            string _type = "ShortByteEnumStructAuto";

            MetadataType tX64 = _testModuleX64.GetType(_namespace, _type);
            MetadataType tX86Windows = _testModuleX86Windows.GetType(_namespace, _type);
            MetadataType tX86Unix = _testModuleX86Unix.GetType(_namespace, _type);
            MetadataType tARM = _testModuleARM.GetType(_namespace, _type);

            Assert.Equal(0x8, tX64.InstanceByteAlignment.AsInt);
            Assert.Equal(0x4, tARM.InstanceByteAlignment.AsInt);
            Assert.Equal(0x4, tX86Windows.InstanceByteAlignment.AsInt);
            Assert.Equal(0x4, tX86Unix.InstanceByteAlignment.AsInt);

            Assert.Equal(0x8, tX64.InstanceByteCountUnaligned.AsInt);
            Assert.Equal(0x8, tARM.InstanceByteCountUnaligned.AsInt);
            Assert.Equal(0x8, tX86Windows.InstanceByteCountUnaligned.AsInt);
            Assert.Equal(0x8, tX86Unix.InstanceByteCountUnaligned.AsInt);

            Assert.Equal(0x8, tX64.InstanceByteCount.AsInt);
            Assert.Equal(0x8, tARM.InstanceByteCount.AsInt);
            Assert.Equal(0x8, tX86Windows.InstanceByteCount.AsInt);
            Assert.Equal(0x8, tX86Unix.InstanceByteCount.AsInt);

            Assert.Equal(0x8, tX64.InstanceFieldAlignment.AsInt);
            Assert.Equal(0x4, tARM.InstanceFieldAlignment.AsInt);
            Assert.Equal(0x4, tX86Windows.InstanceFieldAlignment.AsInt);
            Assert.Equal(0x4, tX86Unix.InstanceFieldAlignment.AsInt);

            Assert.Equal(0x8, tX64.InstanceFieldSize.AsInt);
            Assert.Equal(0x8, tARM.InstanceFieldSize.AsInt);
            Assert.Equal(0x8, tX86Windows.InstanceFieldSize.AsInt);
            Assert.Equal(0x8, tX86Unix.InstanceFieldSize.AsInt);

            Assert.Equal(0x0, tX64.GetField("_1").Offset.AsInt);
            Assert.Equal(0x0, tARM.GetField("_1").Offset.AsInt);
            Assert.Equal(0x0, tX86Windows.GetField("_1").Offset.AsInt);
            Assert.Equal(0x0, tX86Unix.GetField("_1").Offset.AsInt);

            Assert.Equal(0x4, tX64.GetField("_2").Offset.AsInt);
            Assert.Equal(0x4, tARM.GetField("_2").Offset.AsInt);
            Assert.Equal(0x4, tX86Windows.GetField("_2").Offset.AsInt);
            Assert.Equal(0x4, tX86Unix.GetField("_2").Offset.AsInt);

            Assert.Equal(0x2, tX64.GetField("_3").Offset.AsInt);
            Assert.Equal(0x2, tARM.GetField("_3").Offset.AsInt);
            Assert.Equal(0x2, tX86Windows.GetField("_3").Offset.AsInt);
            Assert.Equal(0x2, tX86Unix.GetField("_3").Offset.AsInt);

            Assert.Equal(0x5, tX64.GetField("_4").Offset.AsInt);
            Assert.Equal(0x5, tARM.GetField("_4").Offset.AsInt);
            Assert.Equal(0x5, tX86Windows.GetField("_4").Offset.AsInt);
            Assert.Equal(0x5, tX86Unix.GetField("_4").Offset.AsInt);

            MetadataType tX64FieldStruct = _testModuleX64.GetType(_namespace, _type + "FieldStruct");
            MetadataType tX86FieldStructWindows = _testModuleX86Windows.GetType(_namespace, _type + "FieldStruct");
            MetadataType tX86FieldStructUnix = _testModuleX86Unix.GetType(_namespace, _type + "FieldStruct");
            MetadataType tARMFieldStruct = _testModuleARM.GetType(_namespace, _type + "FieldStruct");

            Assert.Equal(0x8, tX64FieldStruct.GetField("_struct").Offset.AsInt);
            Assert.Equal(0x4, tX86FieldStructWindows.GetField("_struct").Offset.AsInt);
            Assert.Equal(0x4, tX86FieldStructUnix.GetField("_struct").Offset.AsInt);
            Assert.Equal(0x4, tARMFieldStruct.GetField("_struct").Offset.AsInt);
        }
    }
}
