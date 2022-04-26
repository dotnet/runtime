// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.TypeSystem;

using Xunit;

namespace TypeSystemTests
{
    public partial class GCPointerMapTests
    {
        TestTypeSystemContext _context;
        ModuleDesc _testModule;

        public GCPointerMapTests()
        {
            _context = new TestTypeSystemContext(TargetArchitecture.X86);
            var systemModule = _context.CreateModuleForSimpleName("CoreTestAssembly");
            _context.SetSystemModule(systemModule);

            _testModule = systemModule;
        }

        [Fact]
        public void TestInstanceMap()
        {
            MetadataType classWithArrayFields = _testModule.GetType("GCPointerMap", "ClassWithArrayFields");
            MetadataType classWithStringField = _testModule.GetType("GCPointerMap", "ClassWithStringField");
            MetadataType mixedStruct = _testModule.GetType("GCPointerMap", "MixedStruct");
            MetadataType structWithSameGCLayoutAsMixedStruct = _testModule.GetType("GCPointerMap", "StructWithSameGCLayoutAsMixedStruct");
            MetadataType doubleMixedStructLayout = _testModule.GetType("GCPointerMap", "DoubleMixedStructLayout");
            MetadataType explicitlyFarPointer = _testModule.GetType("GCPointerMap", "ExplicitlyFarPointer");
            MetadataType struct32GcPointers = _testModule.GetType("GCPointerMap", "Struct32GcPointers");

            {
                var map = GCPointerMap.FromInstanceLayout(classWithArrayFields);
                Assert.Equal(3, map.Size);
                Assert.Equal("011", map.ToString());
            }

            {
                var map = GCPointerMap.FromInstanceLayout(classWithStringField);
                Assert.Equal(4, map.Size);
                Assert.Equal("0010", map.ToString());
            }

            {
                var map = GCPointerMap.FromInstanceLayout(mixedStruct);
                Assert.Equal(5, map.Size);
                Assert.Equal("01001", map.ToString());
            }

            {
                var map1 = GCPointerMap.FromInstanceLayout(mixedStruct);
                var map2 = GCPointerMap.FromInstanceLayout(structWithSameGCLayoutAsMixedStruct);
                Assert.Equal(map1.Size, map2.Size);
                Assert.Equal(map1.ToString(), map2.ToString());
            }

            {
                var map = GCPointerMap.FromInstanceLayout(doubleMixedStructLayout);
                Assert.Equal(10, map.Size);
                Assert.Equal("0100101001", map.ToString());
            }

            {
                var map = GCPointerMap.FromInstanceLayout(explicitlyFarPointer);
                Assert.Equal(117, map.Size);
                Assert.Equal("100000000000000000000000000000000000000000000000000000000000000010000000000000001000000000000000000000000000000001001", map.ToString());
            }

            {
                var map = GCPointerMap.FromInstanceLayout(struct32GcPointers);
                Assert.Equal(32, map.Size);
                Assert.Equal("11111111111111111111111111111111", map.ToString());
            }
        }
    }
}
