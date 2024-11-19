// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

using System.Collections.Generic;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Moq;

namespace Microsoft.Diagnostics.DataContractReader.UnitTests.ExecutionManager;

public class ExecutionManagerTests
{
    internal class ExecutionManagerTestTarget : TestPlaceholderTarget
    {
        private readonly ulong _topRangeSectionMap;

        public static ExecutionManagerTestTarget FromBuilder(ExecutionManagerTestBuilder emBuilder)
        {
            var arch = emBuilder.Builder.TargetTestHelpers.Arch;
            ReadFromTargetDelegate reader = emBuilder.Builder.GetReadContext().ReadFromTarget;
            var topRangeSectionMap = ExecutionManagerTestBuilder.ExecutionManagerCodeRangeMapAddress;
            var typeInfo = emBuilder.Types;
            return new ExecutionManagerTestTarget(emBuilder.Version, arch, reader, topRangeSectionMap, typeInfo);
        }

        public ExecutionManagerTestTarget(int version, MockTarget.Architecture arch, ReadFromTargetDelegate dataReader, TargetPointer topRangeSectionMap, Dictionary<DataType, TypeInfo> typeInfoCache)
            : base(arch, dataReader)
        {
            _topRangeSectionMap = topRangeSectionMap;
            SetTypeInfoCache(typeInfoCache);
            IContractFactory<IExecutionManager> emfactory = new ExecutionManagerFactory();
            SetContracts(new TestRegistry() {
                ExecutionManagerContract = new (() => emfactory.CreateContract(this, version)),
                PlatformMetadataContract = new (() => new Mock<IPlatformMetadata>().Object)
            });
        }
        public override TargetPointer ReadGlobalPointer(string global)
        {
            switch (global)
            {
            case Constants.Globals.ExecutionManagerCodeRangeMapAddress:
                return new TargetPointer(_topRangeSectionMap);
            default:
                return base.ReadGlobalPointer(global);
            }
        }

        public override T ReadGlobal<T>(string name)
        {
            switch (name)
            {
            case Constants.Globals.StubCodeBlockLast:
                if (typeof(T) == typeof(byte))
                    return (T)(object)(byte)0x0Fu;
                break;
            case Constants.Globals.FeatureEHFunclets:
                if (typeof(T) == typeof(byte))
                    return (T)(object)(byte)1;
                break;
            case Constants.Globals.HashMapValueMask:
                if (typeof(T) == typeof(ulong))
                    return (T)(object)(PointerSize == 4 ? 0x7FFFFFFFu : 0x7FFFFFFFFFFFFFFFu);
                break;
            case Constants.Globals.HashMapSlotsPerBucket:
                if (typeof(T) == typeof(uint))
                    return (T)(object)4u;
                break;
            default:
                break;
            }
            return base.ReadGlobal<T>(name);
        }
    }

    [Theory]
    [MemberData(nameof(StdArchAllVersions))]
    public void GetCodeBlockHandle_Null(int version, MockTarget.Architecture arch)
    {
        ExecutionManagerTestBuilder emBuilder = new (version, arch, ExecutionManagerTestBuilder.DefaultAllocationRange);
        emBuilder.MarkCreated();
        var target = ExecutionManagerTestTarget.FromBuilder (emBuilder);

        var em = target.Contracts.ExecutionManager;
        Assert.NotNull(em);
        var eeInfo = em.GetCodeBlockHandle(TargetCodePointer.Null);
        Assert.Null(eeInfo);
    }

    [Theory]
    [MemberData(nameof(StdArchAllVersions))]
    public void GetCodeBlockHandle_NoRangeSections(int version, MockTarget.Architecture arch)
    {
        ExecutionManagerTestBuilder emBuilder = new (version, arch, ExecutionManagerTestBuilder.DefaultAllocationRange);
        emBuilder.MarkCreated();
        var target = ExecutionManagerTestTarget.FromBuilder (emBuilder);

        var em = target.Contracts.ExecutionManager;
        Assert.NotNull(em);
        var eeInfo = em.GetCodeBlockHandle(new TargetCodePointer(0x0a0a_0000));
        Assert.Null(eeInfo);
    }

    [Theory]
    [MemberData(nameof(StdArchAllVersions))]
    public void GetMethodDesc_OneRangeOneMethod(int version, MockTarget.Architecture arch)
    {
        const ulong codeRangeStart = 0x0a0a_0000u; // arbitrary
        const uint codeRangeSize = 0xc000u; // arbitrary
        const uint methodSize = 0x450; // arbitrary

        TargetPointer jitManagerAddress = new (0x000b_ff00); // arbitrary

        TargetPointer expectedMethodDescAddress = new TargetPointer(0x0101_aaa0);

        ExecutionManagerTestBuilder emBuilder = new(version, arch, ExecutionManagerTestBuilder.DefaultAllocationRange);
        var jittedCode = emBuilder.AllocateJittedCodeRange(codeRangeStart, codeRangeSize);

        TargetCodePointer methodStart = emBuilder.AddJittedMethod(jittedCode, methodSize, expectedMethodDescAddress);

        NibbleMapTestBuilderBase nibBuilder = emBuilder.CreateNibbleMap(codeRangeStart, codeRangeSize);
        nibBuilder.AllocateCodeChunk(methodStart, methodSize);

        TargetPointer codeHeapListNodeAddress = emBuilder.AddCodeHeapListNode(TargetPointer.Null, codeRangeStart, codeRangeStart + codeRangeSize, codeRangeStart, nibBuilder.NibbleMapFragment.Address);
        TargetPointer rangeSectionAddress = emBuilder.AddRangeSection(jittedCode, jitManagerAddress: jitManagerAddress, codeHeapListNodeAddress: codeHeapListNodeAddress);
        TargetPointer rangeSectionFragmentAddress = emBuilder.AddRangeSectionFragment(jittedCode, rangeSectionAddress);

        emBuilder.MarkCreated();

        var target = ExecutionManagerTestTarget.FromBuilder(emBuilder);

        var em = target.Contracts.ExecutionManager;
        Assert.NotNull(em);

        // test at method start
        var eeInfo = em.GetCodeBlockHandle(methodStart);
        Assert.NotNull(eeInfo);
        TargetPointer actualMethodDesc = em.GetMethodDesc(eeInfo.Value);
        Assert.Equal(expectedMethodDescAddress, actualMethodDesc);

        // test middle of method
        eeInfo = em.GetCodeBlockHandle(methodStart + methodSize / 2);
        Assert.NotNull(eeInfo);
        actualMethodDesc = em.GetMethodDesc(eeInfo.Value);
        Assert.Equal(expectedMethodDescAddress, actualMethodDesc);

        // test end of method
        eeInfo = em.GetCodeBlockHandle(methodStart + methodSize - 1);
        Assert.NotNull(eeInfo);
        actualMethodDesc = em.GetMethodDesc(eeInfo.Value);
        Assert.Equal(expectedMethodDescAddress, actualMethodDesc);
    }

    [Theory]
    [MemberData(nameof(StdArchAllVersions))]
    public void GetCodeBlockHandle_OneRangeZeroMethod(int version, MockTarget.Architecture arch)
    {
        const ulong codeRangeStart = 0x0a0a_0000u; // arbitrary
        const uint codeRangeSize = 0xc000u; // arbitrary

        TargetPointer jitManagerAddress = new (0x000b_ff00); // arbitrary

        ExecutionManagerTestBuilder emBuilder = new(version, arch, ExecutionManagerTestBuilder.DefaultAllocationRange);
        var jittedCode = emBuilder.AllocateJittedCodeRange(codeRangeStart, codeRangeSize);

        NibbleMapTestBuilderBase nibBuilder = emBuilder.CreateNibbleMap(codeRangeStart, codeRangeSize);

        TargetPointer codeHeapListNodeAddress = emBuilder.AddCodeHeapListNode(TargetPointer.Null, codeRangeStart, codeRangeStart + codeRangeSize, codeRangeStart, nibBuilder.NibbleMapFragment.Address);
        TargetPointer rangeSectionAddress = emBuilder.AddRangeSection(jittedCode, jitManagerAddress: jitManagerAddress, codeHeapListNodeAddress: codeHeapListNodeAddress);
        TargetPointer rangeSectionFragmentAddress = emBuilder.AddRangeSectionFragment(jittedCode, rangeSectionAddress);

        emBuilder.MarkCreated();

        var target = ExecutionManagerTestTarget.FromBuilder(emBuilder);

        var em = target.Contracts.ExecutionManager;
        Assert.NotNull(em);

        // test at code range start
        var eeInfo = em.GetCodeBlockHandle(codeRangeSize + codeRangeSize);
        Assert.Null(eeInfo);

        // test middle of code range
        eeInfo = em.GetCodeBlockHandle(codeRangeSize + codeRangeSize / 2);
        Assert.Null(eeInfo);

        // test end of code range
        eeInfo = em.GetCodeBlockHandle(codeRangeSize + codeRangeSize - 1);
        Assert.Null(eeInfo);
    }

    [Theory]
    [MemberData(nameof(StdArchAllVersions))]
    public void GetCodeBlockHandle_R2R_NoRuntimeFunctionMatch(int version, MockTarget.Architecture arch)
    {
        const ulong codeRangeStart = 0x0a0a_0000u; // arbitrary
        const uint codeRangeSize = 0xc000u; // arbitrary
        TargetPointer jitManagerAddress = new(0x000b_ff00); // arbitrary

        ExecutionManagerTestBuilder emBuilder = new(version, arch, ExecutionManagerTestBuilder.DefaultAllocationRange);
        var jittedCode = emBuilder.AllocateJittedCodeRange(codeRangeStart, codeRangeSize);

        uint runtimeFunction = 0x100;

        TargetPointer r2rInfo = emBuilder.AddReadyToRunInfo([runtimeFunction]);
        MockDescriptors.HashMap hashMapBuilder = new(emBuilder.Builder);
        hashMapBuilder.PopulatePtrMap(
            r2rInfo + (uint)emBuilder.Types[DataType.ReadyToRunInfo].Fields[nameof(Data.ReadyToRunInfo.EntryPointToMethodDescMap)].Offset,
            []);

        TargetPointer r2rModule = emBuilder.AddReadyToRunModule(r2rInfo);
        TargetPointer rangeSectionAddress = emBuilder.AddReadyToRunRangeSection(jittedCode, jitManagerAddress, r2rModule);
        _ = emBuilder.AddRangeSectionFragment(jittedCode, rangeSectionAddress);

        emBuilder.MarkCreated();

        Target target = ExecutionManagerTestTarget.FromBuilder(emBuilder);

        IExecutionManager em = target.Contracts.ExecutionManager;
        Assert.NotNull(em);

        // Before any functions
        var handle = em.GetCodeBlockHandle(codeRangeStart + runtimeFunction - 1);
        Assert.Null(handle);
    }

    [Theory]
    [MemberData(nameof(StdArchAllVersions))]
    public void GetMethodDesc_R2R_OneRuntimeFunction(int version, MockTarget.Architecture arch)
    {
        const ulong codeRangeStart = 0x0a0a_0000u; // arbitrary
        const uint codeRangeSize = 0xc000u; // arbitrary
        TargetPointer jitManagerAddress = new(0x000b_ff00); // arbitrary

        TargetPointer expectedMethodDescAddress = new TargetPointer(0x0101_aaa0);

        ExecutionManagerTestBuilder emBuilder = new(version, arch, ExecutionManagerTestBuilder.DefaultAllocationRange);
        var jittedCode = emBuilder.AllocateJittedCodeRange(codeRangeStart, codeRangeSize);

        uint expectedRuntimeFunction = 0x100;

        TargetPointer r2rInfo = emBuilder.AddReadyToRunInfo([expectedRuntimeFunction]);
        MockDescriptors.HashMap hashMapBuilder = new(emBuilder.Builder);
        hashMapBuilder.PopulatePtrMap(
            r2rInfo + (uint)emBuilder.Types[DataType.ReadyToRunInfo].Fields[nameof(Data.ReadyToRunInfo.EntryPointToMethodDescMap)].Offset,
            [(jittedCode.RangeStart + expectedRuntimeFunction, expectedMethodDescAddress)]);

        TargetPointer r2rModule = emBuilder.AddReadyToRunModule(r2rInfo);
        TargetPointer rangeSectionAddress = emBuilder.AddReadyToRunRangeSection(jittedCode, jitManagerAddress, r2rModule);
        _ = emBuilder.AddRangeSectionFragment(jittedCode, rangeSectionAddress);

        emBuilder.MarkCreated();

        Target target = ExecutionManagerTestTarget.FromBuilder(emBuilder);

        IExecutionManager em = target.Contracts.ExecutionManager;
        Assert.NotNull(em);

        {
            // Function start
            var handle = em.GetCodeBlockHandle(codeRangeStart + expectedRuntimeFunction);
            Assert.NotNull(handle);
            TargetPointer actualMethodDesc = em.GetMethodDesc(handle.Value);
            Assert.Equal(expectedMethodDescAddress, actualMethodDesc);
        }
        {
            // Past function start
            var handle = em.GetCodeBlockHandle(codeRangeStart + expectedRuntimeFunction * 2);
            Assert.NotNull(handle);
            TargetPointer actualMethodDesc = em.GetMethodDesc(handle.Value);
            Assert.Equal(expectedMethodDescAddress, actualMethodDesc);
        }
    }

    [Theory]
    [MemberData(nameof(StdArchAllVersions))]
    public void GetMethodDesc_R2R_MultipleRuntimeFunctions(int version, MockTarget.Architecture arch)
    {
        const ulong codeRangeStart = 0x0a0a_0000u; // arbitrary
        const uint codeRangeSize = 0xc000u; // arbitrary
        TargetPointer jitManagerAddress = new(0x000b_ff00); // arbitrary

        TargetPointer[] methodDescAddresses = [ 0x0101_aaa0, 0x0201_aaa0];

        ExecutionManagerTestBuilder emBuilder = new(version, arch, ExecutionManagerTestBuilder.DefaultAllocationRange);
        var jittedCode = emBuilder.AllocateJittedCodeRange(codeRangeStart, codeRangeSize);

        uint[] runtimeFunctions = [ 0x100, 0xc00 ];

        TargetPointer r2rInfo = emBuilder.AddReadyToRunInfo(runtimeFunctions);
        MockDescriptors.HashMap hashMapBuilder = new(emBuilder.Builder);
        hashMapBuilder.PopulatePtrMap(
            r2rInfo + (uint)emBuilder.Types[DataType.ReadyToRunInfo].Fields[nameof(Data.ReadyToRunInfo.EntryPointToMethodDescMap)].Offset,
            [
                (jittedCode.RangeStart + runtimeFunctions[0], methodDescAddresses[0]),
                (jittedCode.RangeStart + runtimeFunctions[1], methodDescAddresses[1]),
            ]);

        TargetPointer r2rModule = emBuilder.AddReadyToRunModule(r2rInfo);
        TargetPointer rangeSectionAddress = emBuilder.AddReadyToRunRangeSection(jittedCode, jitManagerAddress, r2rModule);
        _ = emBuilder.AddRangeSectionFragment(jittedCode, rangeSectionAddress);

        emBuilder.MarkCreated();

        Target target = ExecutionManagerTestTarget.FromBuilder(emBuilder);

        IExecutionManager em = target.Contracts.ExecutionManager;
        Assert.NotNull(em);

        {
            // Match first function
            var handle = em.GetCodeBlockHandle(codeRangeStart + runtimeFunctions[0]);
            Assert.NotNull(handle);
            TargetPointer actualMethodDesc = em.GetMethodDesc(handle.Value);
            Assert.Equal(methodDescAddresses[0], actualMethodDesc);
        }
        {
            // After first function, before second - match first function
            uint betweenFirstAndSecond = runtimeFunctions[0] + (runtimeFunctions[1] - runtimeFunctions[0]) / 2;
            var handle = em.GetCodeBlockHandle(codeRangeStart + betweenFirstAndSecond);
            Assert.NotNull(handle);
            TargetPointer actualMethodDesc = em.GetMethodDesc(handle.Value);
            Assert.Equal(methodDescAddresses[0], actualMethodDesc);
        }
        {
            // Match second function
            var handle = em.GetCodeBlockHandle(codeRangeStart + runtimeFunctions[1]);
            Assert.NotNull(handle);
            TargetPointer actualMethodDesc = em.GetMethodDesc(handle.Value);
            Assert.Equal(methodDescAddresses[1], actualMethodDesc);
        }
        {
            // After second/last function - match second/last function
            var handle = em.GetCodeBlockHandle(codeRangeStart + runtimeFunctions[1] * 2);
            Assert.NotNull(handle);
            TargetPointer actualMethodDesc = em.GetMethodDesc(handle.Value);
            Assert.Equal(methodDescAddresses[1], actualMethodDesc);
        }
    }

    public static IEnumerable<object[]> StdArchAllVersions()
    {
        const int highestVersion = 2;
        foreach(object[] arr in new MockTarget.StdArch())
        {
            MockTarget.Architecture arch = (MockTarget.Architecture)arr[0];
            for(int version = 1; version <= highestVersion; version++){
                yield return new object[] { version, arch };
            }
        }
    }
}
