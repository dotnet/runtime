// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

using Microsoft.Diagnostics.DataContractReader.Contracts;
using System.Collections.Generic;
using System;
namespace Microsoft.Diagnostics.DataContractReader.UnitTests;

public class ExecutionManagerTests
{

    internal class ExecutionManagerTestTarget : TestPlaceholderTarget
    {
        private readonly ulong _topRangeSectionMap;

        public static ExecutionManagerTestTarget FromBuilder (ExecutionManagerTestBuilder emBuilder)
        {
            var arch = emBuilder.Builder.TargetTestHelpers.Arch;
            ReadFromTargetDelegate reader = emBuilder.Builder.GetReadContext().ReadFromTarget;
            var topRangeSectionMap = ExecutionManagerTestBuilder.ExecutionManagerCodeRangeMapAddress;
            var typeInfo = emBuilder.TypeInfoCache;
            return new ExecutionManagerTestTarget(arch, reader, topRangeSectionMap, typeInfo);
        }

        public ExecutionManagerTestTarget(MockTarget.Architecture arch, ReadFromTargetDelegate dataReader, TargetPointer topRangeSectionMap, Dictionary<DataType, TypeInfo> typeInfoCache) : base(arch)
        {
            _topRangeSectionMap = topRangeSectionMap;
            SetDataReader(dataReader);
            SetTypeInfoCache(typeInfoCache);
            SetDataCache(new DefaultDataCache(this));
            IContractFactory<IExecutionManager> emfactory = new ExecutionManagerFactory();
            SetContracts(new TestRegistry() {
                ExecutionManagerContract = new (() => emfactory.CreateContract(this, 1)),
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
            default:
                break;
            }
            return base.ReadGlobal<T>(name);

        }

    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void LookupNull(MockTarget.Architecture arch)
    {
        ExecutionManagerTestBuilder emBuilder = new (arch, ExecutionManagerTestBuilder.DefaultAllocationRange);
        emBuilder.MarkCreated();
        var target = ExecutionManagerTestTarget.FromBuilder (emBuilder);

        var em = target.Contracts.ExecutionManager;
        Assert.NotNull(em);
        var eeInfo = em.GetCodeBlockHandle(TargetCodePointer.Null);
        Assert.Null(eeInfo);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void LookupNonNullMissing(MockTarget.Architecture arch)
    {
        ExecutionManagerTestBuilder emBuilder = new (arch, ExecutionManagerTestBuilder.DefaultAllocationRange);
        emBuilder.MarkCreated();
        var target = ExecutionManagerTestTarget.FromBuilder (emBuilder);

        var em = target.Contracts.ExecutionManager;
        Assert.NotNull(em);
        var eeInfo = em.GetCodeBlockHandle(new TargetCodePointer(0x0a0a_0000));
        Assert.Null(eeInfo);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void LookupNonNullOneRangeOneMethod(MockTarget.Architecture arch)
    {
        const ulong codeRangeStart = 0x0a0a_0000u; // arbitrary
        const uint codeRangeSize = 0xc000u; // arbitrary
        int methodSize = 0x100; // arbitrary

        TargetPointer jitManagerAddress = new (0x000b_ff00); // arbitrary

        TargetPointer expectedMethodDescAddress = new TargetPointer(0x0101_aaa0);

        ExecutionManagerTestBuilder emBuilder = new (arch, ExecutionManagerTestBuilder.DefaultAllocationRange);
        var jittedCode = emBuilder.AllocateJittedCodeRange(codeRangeStart, codeRangeSize);

        TargetCodePointer methodStart = emBuilder.AddJittedMethod(jittedCode, methodSize, expectedMethodDescAddress);

        ExecutionManagerTestBuilder.NibbleMapTestBuilder nibBuilder = emBuilder.CreateNibbleMap(codeRangeStart, codeRangeSize);
        nibBuilder.AllocateCodeChunk(methodStart, methodSize);

        TargetPointer codeHeapListNodeAddress = emBuilder.AddCodeHeapListNode(TargetPointer.Null, codeRangeStart, codeRangeStart + codeRangeSize, codeRangeStart, nibBuilder.NibbleMapFragment.Address);
        TargetPointer rangeSectionAddress = emBuilder.AddRangeSection(jittedCode, jitManagerAddress: jitManagerAddress, codeHeapListNodeAddress: codeHeapListNodeAddress);
        TargetPointer rangeSectionFragmentAddress = emBuilder.AddRangeSectionFragment(jittedCode, rangeSectionAddress);

        emBuilder.MarkCreated();

        var target = ExecutionManagerTestTarget.FromBuilder(emBuilder);

        // test

        var em = target.Contracts.ExecutionManager;
        Assert.NotNull(em);
        var eeInfo = em.GetCodeBlockHandle(methodStart);
        Assert.NotNull(eeInfo);
        TargetPointer actualMethodDesc = em.GetMethodDesc(eeInfo.Value);
        Assert.Equal(expectedMethodDescAddress, actualMethodDesc);
    }
}
