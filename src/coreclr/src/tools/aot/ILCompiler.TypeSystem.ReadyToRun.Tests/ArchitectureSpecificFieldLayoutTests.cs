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
            Assert.Equal(0x11, tARM.InstanceByteCountUnaligned.AsInt);
            Assert.Equal(0xD, tX86.InstanceByteCountUnaligned.AsInt);

            Assert.Equal(0x18, tX64.InstanceByteCount.AsInt);
            Assert.Equal(0x18, tARM.InstanceByteCount.AsInt);
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
            Assert.Equal(0x11, tARM.InstanceByteCountUnaligned.AsInt);
            Assert.Equal(0x15, tX86.InstanceByteCountUnaligned.AsInt);

            Assert.Equal(0x20, tX64.InstanceByteCount.AsInt);
            Assert.Equal(0x18, tARM.InstanceByteCount.AsInt);
            Assert.Equal(0x18, tX86.InstanceByteCount.AsInt);
        }
    }
}
