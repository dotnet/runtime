// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Internal.TypeSystem;

using Xunit;

namespace TypeSystemTests
{
    public class UniversalGenericFieldLayoutTests
    {
        TestTypeSystemContext _contextX86;
        ModuleDesc _testModuleX86;
        TestTypeSystemContext _contextX64;
        ModuleDesc _testModuleX64;
        TestTypeSystemContext _contextARM;
        ModuleDesc _testModuleARM;

        public UniversalGenericFieldLayoutTests()
        {
            // Architecture specific tests may use these contexts
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
        public void LayoutIntTests()
        {
            Assert.Throws<ArgumentException>(() => { return new LayoutInt(int.MinValue); });
            Assert.Throws<ArgumentException>(() => { return new LayoutInt(-1); });

            Assert.Equal(LayoutInt.Zero, new LayoutInt(0));
            Assert.Equal(LayoutInt.One, new LayoutInt(1));

            Assert.True(LayoutInt.Zero == new LayoutInt(0));
            Assert.True(LayoutInt.One == new LayoutInt(1));
            Assert.False(LayoutInt.Zero == new LayoutInt(1));
            Assert.False(LayoutInt.One == new LayoutInt(0));
#pragma warning disable 1718 // Allow comparison to same variable
            Assert.True(LayoutInt.Indeterminate == LayoutInt.Indeterminate);
#pragma warning restore 1718

            Assert.False(LayoutInt.Zero != new LayoutInt(0));
            Assert.False(LayoutInt.One != new LayoutInt(1));
            Assert.True(LayoutInt.Zero != new LayoutInt(1));
            Assert.True(LayoutInt.One != new LayoutInt(0));
#pragma warning disable 1718 // Allow comparison to same variable
            Assert.False(LayoutInt.Indeterminate != LayoutInt.Indeterminate);
#pragma warning restore 1718

            Assert.Equal(0, new LayoutInt(0).AsInt);
            Assert.Equal(1, new LayoutInt(1).AsInt);
            Assert.Equal(Int32.MaxValue, new LayoutInt(Int32.MaxValue).AsInt);
            Assert.Throws<InvalidOperationException>(() => { return LayoutInt.Indeterminate.AsInt; });

            Assert.Equal(LayoutInt.Indeterminate, LayoutInt.Indeterminate + LayoutInt.Indeterminate);
            Assert.Equal(LayoutInt.Indeterminate, LayoutInt.One + LayoutInt.Indeterminate);
            Assert.Equal(LayoutInt.Indeterminate, LayoutInt.Indeterminate + LayoutInt.One);
            Assert.Equal(new LayoutInt(2), LayoutInt.One + LayoutInt.One);
            Assert.Throws<OverflowException>(() => { return new LayoutInt(int.MaxValue) + LayoutInt.One; });
            Assert.Throws<OverflowException>(() => { return new LayoutInt(int.MaxValue) + LayoutInt.One; });

            Assert.Equal(LayoutInt.One, LayoutInt.Max(LayoutInt.One, LayoutInt.Zero));
            Assert.Equal(LayoutInt.One, LayoutInt.Max(LayoutInt.Zero, LayoutInt.One));
            Assert.Equal(LayoutInt.Indeterminate, LayoutInt.Max(LayoutInt.Indeterminate, LayoutInt.Zero));
            Assert.Equal(LayoutInt.Indeterminate, LayoutInt.Max(LayoutInt.Zero, LayoutInt.Indeterminate));
            Assert.Equal(LayoutInt.Indeterminate, LayoutInt.Max(LayoutInt.Indeterminate, LayoutInt.Indeterminate));

            Assert.Equal(LayoutInt.Zero, LayoutInt.Min(LayoutInt.One, LayoutInt.Zero));
            Assert.Equal(LayoutInt.Zero, LayoutInt.Min(LayoutInt.Zero, LayoutInt.One));
            Assert.Equal(LayoutInt.Indeterminate, LayoutInt.Min(LayoutInt.Indeterminate, LayoutInt.Zero));
            Assert.Equal(LayoutInt.Indeterminate, LayoutInt.Min(LayoutInt.Zero, LayoutInt.Indeterminate));
            Assert.Equal(LayoutInt.Indeterminate, LayoutInt.Min(LayoutInt.Indeterminate, LayoutInt.Indeterminate));
        }

        public static IEnumerable<object[]> GetTargetDetails()
        {
            yield return new object[] { new TargetDetails(TargetArchitecture.ARM, TargetOS.Unknown, TargetAbi.NativeAot) };
            yield return new object[] { new TargetDetails(TargetArchitecture.ARM64, TargetOS.Unknown, TargetAbi.NativeAot) };
            yield return new object[] { new TargetDetails(TargetArchitecture.X64, TargetOS.Unknown, TargetAbi.NativeAot) };
            yield return new object[] { new TargetDetails(TargetArchitecture.X86, TargetOS.Unknown, TargetAbi.NativeAot) };
            yield return new object[] { new TargetDetails(TargetArchitecture.Wasm32, TargetOS.Unknown, TargetAbi.NativeAot) };
        }

        [Theory]
        [MemberData(nameof(GetTargetDetails))]
        public void TestLayoutIntAlignUp(TargetDetails target)
        {
            // AlignUp testing
            Assert.Equal(new LayoutInt(0), LayoutInt.AlignUp(new LayoutInt(0), new LayoutInt(1), target));
            Assert.Equal(new LayoutInt(0), LayoutInt.AlignUp(new LayoutInt(0), new LayoutInt(2), target));
            Assert.Equal(new LayoutInt(0), LayoutInt.AlignUp(new LayoutInt(0), new LayoutInt(4), target));
            Assert.Equal(new LayoutInt(0), LayoutInt.AlignUp(new LayoutInt(0), new LayoutInt(8), target));

            Assert.Equal(new LayoutInt(1), LayoutInt.AlignUp(new LayoutInt(1), new LayoutInt(1), target));
            Assert.Equal(new LayoutInt(2), LayoutInt.AlignUp(new LayoutInt(2), new LayoutInt(1), target));
            Assert.Equal(new LayoutInt(3), LayoutInt.AlignUp(new LayoutInt(3), new LayoutInt(1), target));
            Assert.Equal(new LayoutInt(4), LayoutInt.AlignUp(new LayoutInt(4), new LayoutInt(1), target));
            Assert.Equal(new LayoutInt(5), LayoutInt.AlignUp(new LayoutInt(5), new LayoutInt(1), target));
            Assert.Equal(new LayoutInt(6), LayoutInt.AlignUp(new LayoutInt(6), new LayoutInt(1), target));
            Assert.Equal(new LayoutInt(7), LayoutInt.AlignUp(new LayoutInt(7), new LayoutInt(1), target));
            Assert.Equal(new LayoutInt(8), LayoutInt.AlignUp(new LayoutInt(8), new LayoutInt(1), target));
            Assert.Equal(new LayoutInt(9), LayoutInt.AlignUp(new LayoutInt(9), new LayoutInt(1), target));
            Assert.Equal(new LayoutInt(10), LayoutInt.AlignUp(new LayoutInt(10), new LayoutInt(1), target));
            Assert.Equal(new LayoutInt(11), LayoutInt.AlignUp(new LayoutInt(11), new LayoutInt(1), target));
            Assert.Equal(new LayoutInt(12), LayoutInt.AlignUp(new LayoutInt(12), new LayoutInt(1), target));
            Assert.Equal(new LayoutInt(13), LayoutInt.AlignUp(new LayoutInt(13), new LayoutInt(1), target));
            Assert.Equal(new LayoutInt(14), LayoutInt.AlignUp(new LayoutInt(14), new LayoutInt(1), target));
            Assert.Equal(new LayoutInt(15), LayoutInt.AlignUp(new LayoutInt(15), new LayoutInt(1), target));
            Assert.Equal(new LayoutInt(16), LayoutInt.AlignUp(new LayoutInt(16), new LayoutInt(1), target));

            Assert.Equal(new LayoutInt(2), LayoutInt.AlignUp(new LayoutInt(1), new LayoutInt(2), target));
            Assert.Equal(new LayoutInt(2), LayoutInt.AlignUp(new LayoutInt(2), new LayoutInt(2), target));
            Assert.Equal(new LayoutInt(4), LayoutInt.AlignUp(new LayoutInt(3), new LayoutInt(2), target));
            Assert.Equal(new LayoutInt(4), LayoutInt.AlignUp(new LayoutInt(4), new LayoutInt(2), target));
            Assert.Equal(new LayoutInt(6), LayoutInt.AlignUp(new LayoutInt(5), new LayoutInt(2), target));
            Assert.Equal(new LayoutInt(6), LayoutInt.AlignUp(new LayoutInt(6), new LayoutInt(2), target));
            Assert.Equal(new LayoutInt(8), LayoutInt.AlignUp(new LayoutInt(7), new LayoutInt(2), target));
            Assert.Equal(new LayoutInt(8), LayoutInt.AlignUp(new LayoutInt(8), new LayoutInt(2), target));
            Assert.Equal(new LayoutInt(10), LayoutInt.AlignUp(new LayoutInt(9), new LayoutInt(2), target));
            Assert.Equal(new LayoutInt(10), LayoutInt.AlignUp(new LayoutInt(10), new LayoutInt(2), target));
            Assert.Equal(new LayoutInt(12), LayoutInt.AlignUp(new LayoutInt(11), new LayoutInt(2), target));
            Assert.Equal(new LayoutInt(12), LayoutInt.AlignUp(new LayoutInt(12), new LayoutInt(2), target));
            Assert.Equal(new LayoutInt(14), LayoutInt.AlignUp(new LayoutInt(13), new LayoutInt(2), target));
            Assert.Equal(new LayoutInt(14), LayoutInt.AlignUp(new LayoutInt(14), new LayoutInt(2), target));
            Assert.Equal(new LayoutInt(16), LayoutInt.AlignUp(new LayoutInt(15), new LayoutInt(2), target));
            Assert.Equal(new LayoutInt(16), LayoutInt.AlignUp(new LayoutInt(16), new LayoutInt(2), target));

            Assert.Equal(new LayoutInt(4), LayoutInt.AlignUp(new LayoutInt(1), new LayoutInt(4), target));
            Assert.Equal(new LayoutInt(4), LayoutInt.AlignUp(new LayoutInt(2), new LayoutInt(4), target));
            Assert.Equal(new LayoutInt(4), LayoutInt.AlignUp(new LayoutInt(3), new LayoutInt(4), target));
            Assert.Equal(new LayoutInt(4), LayoutInt.AlignUp(new LayoutInt(4), new LayoutInt(4), target));
            Assert.Equal(new LayoutInt(8), LayoutInt.AlignUp(new LayoutInt(5), new LayoutInt(4), target));
            Assert.Equal(new LayoutInt(8), LayoutInt.AlignUp(new LayoutInt(6), new LayoutInt(4), target));
            Assert.Equal(new LayoutInt(8), LayoutInt.AlignUp(new LayoutInt(7), new LayoutInt(4), target));
            Assert.Equal(new LayoutInt(8), LayoutInt.AlignUp(new LayoutInt(8), new LayoutInt(4), target));
            Assert.Equal(new LayoutInt(12), LayoutInt.AlignUp(new LayoutInt(9), new LayoutInt(4), target));
            Assert.Equal(new LayoutInt(12), LayoutInt.AlignUp(new LayoutInt(10), new LayoutInt(4), target));
            Assert.Equal(new LayoutInt(12), LayoutInt.AlignUp(new LayoutInt(11), new LayoutInt(4), target));
            Assert.Equal(new LayoutInt(12), LayoutInt.AlignUp(new LayoutInt(12), new LayoutInt(4), target));
            Assert.Equal(new LayoutInt(16), LayoutInt.AlignUp(new LayoutInt(13), new LayoutInt(4), target));
            Assert.Equal(new LayoutInt(16), LayoutInt.AlignUp(new LayoutInt(14), new LayoutInt(4), target));
            Assert.Equal(new LayoutInt(16), LayoutInt.AlignUp(new LayoutInt(15), new LayoutInt(4), target));
            Assert.Equal(new LayoutInt(16), LayoutInt.AlignUp(new LayoutInt(16), new LayoutInt(4), target));

            Assert.Equal(new LayoutInt(8), LayoutInt.AlignUp(new LayoutInt(1), new LayoutInt(8), target));
            Assert.Equal(new LayoutInt(8), LayoutInt.AlignUp(new LayoutInt(2), new LayoutInt(8), target));
            Assert.Equal(new LayoutInt(8), LayoutInt.AlignUp(new LayoutInt(3), new LayoutInt(8), target));
            Assert.Equal(new LayoutInt(8), LayoutInt.AlignUp(new LayoutInt(4), new LayoutInt(8), target));
            Assert.Equal(new LayoutInt(8), LayoutInt.AlignUp(new LayoutInt(5), new LayoutInt(8), target));
            Assert.Equal(new LayoutInt(8), LayoutInt.AlignUp(new LayoutInt(6), new LayoutInt(8), target));
            Assert.Equal(new LayoutInt(8), LayoutInt.AlignUp(new LayoutInt(7), new LayoutInt(8), target));
            Assert.Equal(new LayoutInt(8), LayoutInt.AlignUp(new LayoutInt(8), new LayoutInt(8), target));
            Assert.Equal(new LayoutInt(16), LayoutInt.AlignUp(new LayoutInt(9), new LayoutInt(8), target));
            Assert.Equal(new LayoutInt(16), LayoutInt.AlignUp(new LayoutInt(10), new LayoutInt(8), target));
            Assert.Equal(new LayoutInt(16), LayoutInt.AlignUp(new LayoutInt(11), new LayoutInt(8), target));
            Assert.Equal(new LayoutInt(16), LayoutInt.AlignUp(new LayoutInt(12), new LayoutInt(8), target));
            Assert.Equal(new LayoutInt(16), LayoutInt.AlignUp(new LayoutInt(13), new LayoutInt(8), target));
            Assert.Equal(new LayoutInt(16), LayoutInt.AlignUp(new LayoutInt(14), new LayoutInt(8), target));
            Assert.Equal(new LayoutInt(16), LayoutInt.AlignUp(new LayoutInt(15), new LayoutInt(8), target));
            Assert.Equal(new LayoutInt(16), LayoutInt.AlignUp(new LayoutInt(16), new LayoutInt(8), target));

            Assert.Equal(LayoutInt.Indeterminate, LayoutInt.AlignUp(new LayoutInt(1), LayoutInt.Indeterminate, target));
            Assert.Equal(LayoutInt.Indeterminate, LayoutInt.AlignUp(new LayoutInt(2), LayoutInt.Indeterminate, target));
            Assert.Equal(LayoutInt.Indeterminate, LayoutInt.AlignUp(new LayoutInt(3), LayoutInt.Indeterminate, target));
            Assert.Equal(LayoutInt.Indeterminate, LayoutInt.AlignUp(new LayoutInt(4), LayoutInt.Indeterminate, target));
            Assert.Equal(LayoutInt.Indeterminate, LayoutInt.AlignUp(new LayoutInt(5), LayoutInt.Indeterminate, target));
            Assert.Equal(LayoutInt.Indeterminate, LayoutInt.AlignUp(new LayoutInt(6), LayoutInt.Indeterminate, target));
            Assert.Equal(LayoutInt.Indeterminate, LayoutInt.AlignUp(new LayoutInt(7), LayoutInt.Indeterminate, target));
            if (target.MaximumAlignment > 8)
                Assert.Equal(LayoutInt.Indeterminate, LayoutInt.AlignUp(new LayoutInt(8), LayoutInt.Indeterminate, target));
            Assert.Equal(LayoutInt.Indeterminate, LayoutInt.AlignUp(new LayoutInt(9), LayoutInt.Indeterminate, target));
            Assert.Equal(LayoutInt.Indeterminate, LayoutInt.AlignUp(new LayoutInt(10), LayoutInt.Indeterminate, target));
            Assert.Equal(LayoutInt.Indeterminate, LayoutInt.AlignUp(new LayoutInt(11), LayoutInt.Indeterminate, target));
            Assert.Equal(LayoutInt.Indeterminate, LayoutInt.AlignUp(new LayoutInt(12), LayoutInt.Indeterminate, target));
            Assert.Equal(LayoutInt.Indeterminate, LayoutInt.AlignUp(new LayoutInt(13), LayoutInt.Indeterminate, target));
            Assert.Equal(LayoutInt.Indeterminate, LayoutInt.AlignUp(new LayoutInt(14), LayoutInt.Indeterminate, target));
            Assert.Equal(LayoutInt.Indeterminate, LayoutInt.AlignUp(new LayoutInt(15), LayoutInt.Indeterminate, target));
            if (target.MaximumAlignment > 16)
                Assert.Equal(LayoutInt.Indeterminate, LayoutInt.AlignUp(new LayoutInt(16), LayoutInt.Indeterminate, target));

            // If we the value is aligned to the maximum supported alignment, we can consider it aligned no matter
            // the value of the alignment.
            Assert.Equal(new LayoutInt(target.MaximumAlignment), LayoutInt.AlignUp(new LayoutInt(target.MaximumAlignment), LayoutInt.Indeterminate, target));
            Assert.Equal(new LayoutInt(target.MaximumAlignment * 2), LayoutInt.AlignUp(new LayoutInt(target.MaximumAlignment * 2), LayoutInt.Indeterminate, target));

            Assert.Equal(LayoutInt.Indeterminate, LayoutInt.AlignUp(LayoutInt.Indeterminate, new LayoutInt(1), target));
            Assert.Equal(LayoutInt.Indeterminate, LayoutInt.AlignUp(LayoutInt.Indeterminate, new LayoutInt(2), target));
            Assert.Equal(LayoutInt.Indeterminate, LayoutInt.AlignUp(LayoutInt.Indeterminate, new LayoutInt(4), target));
            Assert.Equal(LayoutInt.Indeterminate, LayoutInt.AlignUp(LayoutInt.Indeterminate, new LayoutInt(8), target));
        }


        private void TestLayoutOfUniversalCanonTypeOnArchitecture(TypeSystemContext context)
        {
            // Assert all of the various layout information about the universal canon type itself
            Assert.Equal(LayoutInt.Indeterminate, context.UniversalCanonType.InstanceFieldAlignment);
            Assert.Equal(LayoutInt.Indeterminate, context.UniversalCanonType.InstanceFieldSize);
            Assert.Equal(LayoutInt.Indeterminate, context.UniversalCanonType.InstanceByteAlignment);
            Assert.Equal(LayoutInt.Indeterminate, context.UniversalCanonType.InstanceByteCount);
            Assert.Equal(LayoutInt.Indeterminate, context.UniversalCanonType.InstanceByteCountUnaligned);
            Assert.Equal(LayoutInt.Zero, context.UniversalCanonType.GCStaticFieldAlignment);
            Assert.Equal(LayoutInt.Zero, context.UniversalCanonType.GCStaticFieldSize);
            Assert.Equal(LayoutInt.Zero, context.UniversalCanonType.NonGCStaticFieldAlignment);
            Assert.Equal(LayoutInt.Zero, context.UniversalCanonType.NonGCStaticFieldSize);
            Assert.Equal(LayoutInt.Zero, context.UniversalCanonType.ThreadGcStaticFieldAlignment);
            Assert.Equal(LayoutInt.Zero, context.UniversalCanonType.ThreadGcStaticFieldSize);
        }
        [Fact]
        public void TestLayoutOfUniversalCanonType()
        {
            // Assert all of the various layout information about the universal canon type itself, do this for all architectures
            TestLayoutOfUniversalCanonTypeOnArchitecture(_contextX86);
            TestLayoutOfUniversalCanonTypeOnArchitecture(_contextX64);
            TestLayoutOfUniversalCanonTypeOnArchitecture(_contextARM);
        }

        [Fact]
        public void TestAllFieldsStructUniversalGeneric()
        {
            // Given a struct with all field universal, what is the layout?
            MetadataType tGen;
            InstantiatedType genOfUUU;
            ModuleDesc testModule;
            TypeSystemContext context;

            // X64 testing
            testModule = _testModuleX64;
            context = _contextX64;

            tGen = testModule.GetType("GenericTypes", "GenStruct`3");
            genOfUUU = tGen.MakeInstantiatedType(context.UniversalCanonType, context.UniversalCanonType, context.UniversalCanonType);

            Assert.Equal(LayoutInt.Indeterminate, genOfUUU.InstanceFieldAlignment);
            Assert.Equal(LayoutInt.Indeterminate, genOfUUU.InstanceFieldSize);
            Assert.Equal(new LayoutInt(8), genOfUUU.InstanceByteAlignment);
            Assert.Equal(LayoutInt.Indeterminate, genOfUUU.InstanceByteCount);
            Assert.Equal(LayoutInt.Indeterminate, genOfUUU.InstanceByteCountUnaligned);
            Assert.Equal(0, genOfUUU.GetFields().First().Offset.AsInt);
            Assert.Equal(LayoutInt.Indeterminate, genOfUUU.GetFields().ElementAt(1).Offset);
            Assert.Equal(LayoutInt.Indeterminate, genOfUUU.GetFields().ElementAt(2).Offset);

            testModule = _testModuleX86;
            context = _contextX86;

            tGen = testModule.GetType("GenericTypes", "GenStruct`3");
            genOfUUU = tGen.MakeInstantiatedType(context.UniversalCanonType, context.UniversalCanonType, context.UniversalCanonType);

            Assert.Equal(LayoutInt.Indeterminate, genOfUUU.InstanceFieldAlignment);
            Assert.Equal(LayoutInt.Indeterminate, genOfUUU.InstanceFieldSize);
            Assert.Equal(new LayoutInt(4), genOfUUU.InstanceByteAlignment);
            Assert.Equal(LayoutInt.Indeterminate, genOfUUU.InstanceByteCount);
            Assert.Equal(LayoutInt.Indeterminate, genOfUUU.InstanceByteCountUnaligned);
            Assert.Equal(0, genOfUUU.GetFields().First().Offset.AsInt);
            Assert.Equal(LayoutInt.Indeterminate, genOfUUU.GetFields().ElementAt(1).Offset);
            Assert.Equal(LayoutInt.Indeterminate, genOfUUU.GetFields().ElementAt(2).Offset);

            testModule = _testModuleARM;
            context = _contextARM;

            tGen = testModule.GetType("GenericTypes", "GenStruct`3");
            genOfUUU = tGen.MakeInstantiatedType(context.UniversalCanonType, context.UniversalCanonType, context.UniversalCanonType);

            Assert.Equal(LayoutInt.Indeterminate, genOfUUU.InstanceFieldAlignment);
            Assert.Equal(LayoutInt.Indeterminate, genOfUUU.InstanceFieldSize);
            Assert.Equal(LayoutInt.Indeterminate, genOfUUU.InstanceByteAlignment);
            Assert.Equal(LayoutInt.Indeterminate, genOfUUU.InstanceByteCount);
            Assert.Equal(LayoutInt.Indeterminate, genOfUUU.InstanceByteCountUnaligned);
            Assert.Equal(0, genOfUUU.GetFields().First().Offset.AsInt);
            Assert.Equal(LayoutInt.Indeterminate, genOfUUU.GetFields().ElementAt(1).Offset);
            Assert.Equal(LayoutInt.Indeterminate, genOfUUU.GetFields().ElementAt(2).Offset);
        }

        private void TestIndeterminatedNestedStructFieldPerContext(TypeSystemContext context, ModuleDesc testModule, out InstantiatedType genOfIntNestedInt, out InstantiatedType genOfLongNestedInt)
        {
            // Given a struct with all field universal, what is the layout?
            MetadataType tGen = testModule.GetType("GenericTypes", "GenStruct`3");
            InstantiatedType genOfUUU = tGen.MakeInstantiatedType(context.UniversalCanonType, context.UniversalCanonType, context.UniversalCanonType);
            genOfIntNestedInt = tGen.MakeInstantiatedType(context.GetWellKnownType(WellKnownType.Int32), genOfUUU, context.GetWellKnownType(WellKnownType.Int32));
            genOfLongNestedInt = tGen.MakeInstantiatedType(context.GetWellKnownType(WellKnownType.Int64), genOfUUU, context.GetWellKnownType(WellKnownType.Int32));

            Assert.Equal(LayoutInt.Indeterminate, genOfIntNestedInt.InstanceFieldAlignment);
            Assert.Equal(LayoutInt.Indeterminate, genOfIntNestedInt.InstanceFieldSize);
            Assert.Equal(LayoutInt.Indeterminate, genOfIntNestedInt.InstanceByteCount);
            Assert.Equal(LayoutInt.Indeterminate, genOfIntNestedInt.InstanceByteCountUnaligned);
            Assert.Equal(0, genOfIntNestedInt.GetFields().First().Offset.AsInt);
            Assert.Equal(LayoutInt.Indeterminate, genOfIntNestedInt.GetFields().ElementAt(1).Offset);
            Assert.Equal(LayoutInt.Indeterminate, genOfIntNestedInt.GetFields().ElementAt(2).Offset);

            Assert.Equal(LayoutInt.Indeterminate, genOfLongNestedInt.InstanceFieldAlignment);
            Assert.Equal(LayoutInt.Indeterminate, genOfLongNestedInt.InstanceFieldSize);
            Assert.Equal(LayoutInt.Indeterminate, genOfLongNestedInt.InstanceByteCount);
            Assert.Equal(LayoutInt.Indeterminate, genOfLongNestedInt.InstanceByteCountUnaligned);
            Assert.Equal(0, genOfLongNestedInt.GetFields().First().Offset.AsInt);
            if (context.Target.MaximumAlignment <= 8)
            {
                Assert.Equal(8, genOfLongNestedInt.GetFields().ElementAt(1).Offset.AsInt);
            }
            else
            {
                Assert.Equal(LayoutInt.Indeterminate, genOfLongNestedInt.GetFields().ElementAt(1).Offset);
            }
            Assert.Equal(LayoutInt.Indeterminate, genOfLongNestedInt.GetFields().ElementAt(2).Offset);
        }

        [Fact]
        public void TestIndeterminateNestedStructField()
        {
            InstantiatedType genOfIntNestedInt;
            InstantiatedType genOfLongNestedInt;

            TestIndeterminatedNestedStructFieldPerContext(_contextX64, _testModuleX64, out genOfIntNestedInt, out genOfLongNestedInt);
            Assert.Equal(new LayoutInt(8), genOfLongNestedInt.InstanceByteAlignment);
            Assert.Equal(new LayoutInt(8), genOfLongNestedInt.InstanceByteAlignment);
            TestIndeterminatedNestedStructFieldPerContext(_contextX86, _testModuleX86, out genOfIntNestedInt, out genOfLongNestedInt);
            Assert.Equal(new LayoutInt(4), genOfLongNestedInt.InstanceByteAlignment);
            Assert.Equal(new LayoutInt(4), genOfLongNestedInt.InstanceByteAlignment);
            TestIndeterminatedNestedStructFieldPerContext(_contextARM, _testModuleARM, out genOfIntNestedInt, out genOfLongNestedInt);
            Assert.Equal(LayoutInt.Indeterminate, genOfLongNestedInt.InstanceByteAlignment);
            Assert.Equal(LayoutInt.Indeterminate, genOfLongNestedInt.InstanceByteAlignment);
        }

        private void AssertClassIndeterminateSize(TypeSystemContext context, MetadataType type, LayoutInt expectedIndeterminateByteAlignment)
        {
            Assert.Equal(context.Target.LayoutPointerSize, type.InstanceFieldAlignment);
            Assert.Equal(context.Target.LayoutPointerSize, type.InstanceFieldSize);
            Assert.Equal(expectedIndeterminateByteAlignment, type.InstanceByteAlignment);
            Assert.Equal(LayoutInt.Indeterminate, type.InstanceByteCount);
            Assert.Equal(LayoutInt.Indeterminate, type.InstanceByteCountUnaligned);
        }

        private void CommonClassLayoutTestBits(ModuleDesc testModule, 
                                               TypeSystemContext context,
                                               LayoutInt expectedIndeterminateByteAlignment,
                                               out InstantiatedType genOfIU,
                                               out InstantiatedType genOfLU,
                                               out InstantiatedType genOfUU,
                                               out InstantiatedType genOfUI,
                                               out InstantiatedType genOfUL)
        {
            MetadataType tDerivedGen = testModule.GetType("GenericTypes", "GenDerivedClass`2");
            genOfIU = tDerivedGen.MakeInstantiatedType(context.GetWellKnownType(WellKnownType.Int32), context.UniversalCanonType);
            genOfLU = tDerivedGen.MakeInstantiatedType(context.GetWellKnownType(WellKnownType.Int64), context.UniversalCanonType);
            genOfUU = tDerivedGen.MakeInstantiatedType(context.UniversalCanonType, context.UniversalCanonType);

            genOfUI = tDerivedGen.MakeInstantiatedType(context.UniversalCanonType, context.GetWellKnownType(WellKnownType.Int32));
            genOfUL = tDerivedGen.MakeInstantiatedType(context.UniversalCanonType, context.GetWellKnownType(WellKnownType.Int64));

            // Assert that the class as a whole is known to be of undefined size
            AssertClassIndeterminateSize(context, genOfIU, expectedIndeterminateByteAlignment);
            AssertClassIndeterminateSize(context, genOfLU, expectedIndeterminateByteAlignment);
            AssertClassIndeterminateSize(context, genOfUU, expectedIndeterminateByteAlignment);
            AssertClassIndeterminateSize(context, genOfUI, expectedIndeterminateByteAlignment);
            AssertClassIndeterminateSize(context, genOfUL, expectedIndeterminateByteAlignment);
        }

        [Fact]
        public void TestClassLayout()
        {
            // Tests class layout behavior with universal generics
            // Tests handling universal base types as well as non-universal base types

            InstantiatedType genOfIU;
            InstantiatedType genOfLU;
            InstantiatedType genOfUU;
            InstantiatedType genOfUI;
            InstantiatedType genOfUL;

            ModuleDesc testModule;
            TypeSystemContext context;

            // X64 testing
            testModule = _testModuleX64;
            context = _contextX64;

            CommonClassLayoutTestBits(testModule,
                                      context,
                                      new LayoutInt(8),
                                      out genOfIU,
                                      out genOfLU,
                                      out genOfUU,
                                      out genOfUI,
                                      out genOfUL);

            // On x64 first field offset is well known always
            Assert.Equal(8, genOfIU.BaseType.GetFields().First().Offset.AsInt);
            Assert.Equal(8, genOfLU.BaseType.GetFields().First().Offset.AsInt);
            if (context.Target.MaximumAlignment <= 8)
            {
                Assert.Equal(8, genOfUU.BaseType.GetFields().First().Offset.AsInt);
                Assert.Equal(8, genOfUI.BaseType.GetFields().First().Offset.AsInt);
                Assert.Equal(8, genOfUL.BaseType.GetFields().First().Offset.AsInt);
            }
            else
            {

                Assert.Equal(LayoutInt.Indeterminate, genOfUU.BaseType.GetFields().First().Offset);
                Assert.Equal(LayoutInt.Indeterminate, genOfUI.BaseType.GetFields().First().Offset);
                Assert.Equal(LayoutInt.Indeterminate, genOfUL.BaseType.GetFields().First().Offset);
            }

            Assert.Equal(LayoutInt.Indeterminate, genOfIU.GetFields().First().Offset);
            if (context.Target.MaximumAlignment <= 16)
            {
                Assert.Equal(16, genOfLU.GetFields().First().Offset.AsInt);
            }
            else
            {
                Assert.Equal(LayoutInt.Indeterminate, genOfLU.GetFields().First().Offset);
            }
            Assert.Equal(LayoutInt.Indeterminate, genOfUU.GetFields().First().Offset);
            Assert.Equal(LayoutInt.Indeterminate, genOfUI.GetFields().First().Offset);
            Assert.Equal(LayoutInt.Indeterminate, genOfUL.GetFields().First().Offset);

            // X86 testing
            testModule = _testModuleX86;
            context = _contextX86;

            CommonClassLayoutTestBits(testModule,
                                      context,
                                      new LayoutInt(4),
                                      out genOfIU,
                                      out genOfLU,
                                      out genOfUU,
                                      out genOfUI,
                                      out genOfUL);

            Assert.Equal(4, genOfIU.BaseType.GetFields().First().Offset.AsInt);
            Assert.Equal(4, genOfLU.BaseType.GetFields().First().Offset.AsInt);

            // Like X64, X86 first field location is always 4 bytes from start
            // This results in 8 byte aligned quantities being aligned at unusual offsets
            Assert.Equal(4, genOfUU.BaseType.GetFields().First().Offset.AsInt);
            Assert.Equal(4, genOfUI.BaseType.GetFields().First().Offset.AsInt);
            Assert.Equal(4, genOfUL.BaseType.GetFields().First().Offset.AsInt);

            if (context.Target.MaximumAlignment <= 8)
            {
                Assert.Equal(8, genOfIU.GetFields().First().Offset.AsInt);
            }
            else
            {
                Assert.Equal(LayoutInt.Indeterminate, genOfIU.GetFields().First().Offset);
            }
            if (context.Target.MaximumAlignment <= 16)
            {
                Assert.Equal(16, genOfLU.GetFields().First().Offset.AsInt);
            }
            else
            {
                Assert.Equal(LayoutInt.Indeterminate, genOfLU.GetFields().First().Offset);
            }
            Assert.Equal(LayoutInt.Indeterminate, genOfUU.GetFields().First().Offset);
            Assert.Equal(LayoutInt.Indeterminate, genOfUI.GetFields().First().Offset);
            Assert.Equal(LayoutInt.Indeterminate, genOfUL.GetFields().First().Offset);

            // ARM testing
            testModule = _testModuleARM;
            context = _contextARM;

            CommonClassLayoutTestBits(testModule,
                                      context,
                                      LayoutInt.Indeterminate,
                                      out genOfIU,
                                      out genOfLU,
                                      out genOfUU,
                                      out genOfUI,
                                      out genOfUL);

            Assert.Equal(4, genOfIU.BaseType.GetFields().First().Offset.AsInt);
            Assert.Equal(8, genOfLU.BaseType.GetFields().First().Offset.AsInt);
            Assert.Equal(LayoutInt.Indeterminate, genOfUU.BaseType.GetFields().First().Offset);
            Assert.Equal(LayoutInt.Indeterminate, genOfUI.BaseType.GetFields().First().Offset);
            Assert.Equal(LayoutInt.Indeterminate, genOfUL.BaseType.GetFields().First().Offset);

            if (context.Target.MaximumAlignment <= 8)
            {
                Assert.Equal(8, genOfIU.GetFields().First().Offset.AsInt);
            }
            else
            {
                Assert.Equal(LayoutInt.Indeterminate, genOfIU.GetFields().First().Offset);
            }
            if (context.Target.MaximumAlignment <= 16)
            {
                Assert.Equal(16, genOfLU.GetFields().First().Offset.AsInt);
            }
            else
            {
                Assert.Equal(LayoutInt.Indeterminate, genOfLU.GetFields().First().Offset);
            }
            Assert.Equal(LayoutInt.Indeterminate, genOfUU.GetFields().First().Offset);
            Assert.Equal(LayoutInt.Indeterminate, genOfUI.GetFields().First().Offset);
            Assert.Equal(LayoutInt.Indeterminate, genOfUL.GetFields().First().Offset);
        }
    }
}
