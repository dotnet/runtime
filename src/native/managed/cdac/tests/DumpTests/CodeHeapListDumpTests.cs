// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.DumpTests;

/// <summary>
/// Dump-based integration tests for the ExecutionManager code heap enumeration APIs.
/// Uses the CodeHeap debuggee (heap dump) to exercise GetCodeHeapList and
/// GetCodeHeapInfo against a real runtime memory image that contains both
/// a LoaderCodeHeap (regular JIT-compiled methods) and a HostCodeHeap
/// (DynamicMethod instances).
/// </summary>
public class CodeHeapListDumpTests : DumpTestBase
{
    protected override string DebuggeeName => "CodeHeap";

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "Code heap list APIs were added after net10.0")]
    public void GetCodeHeapList_ReturnsNonEmptyList(TestConfiguration config)
    {
        InitializeDumpTest(config);

        IExecutionManager em = Target.Contracts.ExecutionManager;
        List<TargetPointer> heaps = em.GetCodeHeapList();

        Assert.True(heaps.Count > 0, "Expected at least one code heap in the runtime");
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "Code heap list APIs were added after net10.0")]
    public void GetCodeHeapList_ContainsLoaderCodeHeap(TestConfiguration config)
    {
        InitializeDumpTest(config);

        IExecutionManager em = Target.Contracts.ExecutionManager;
        List<TargetPointer> heaps = em.GetCodeHeapList();

        Assert.Contains(heaps, h => em.GetCodeHeapInfo(h) is LoaderCodeHeapInfo);
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "Code heap list APIs were added after net10.0")]
    public void GetCodeHeapList_ContainsHostCodeHeap(TestConfiguration config)
    {
        InitializeDumpTest(config);

        IExecutionManager em = Target.Contracts.ExecutionManager;
        List<TargetPointer> heaps = em.GetCodeHeapList();

        Assert.Contains(heaps, h => em.GetCodeHeapInfo(h) is HostCodeHeapInfo);
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "Code heap list APIs were added after net10.0")]
    public void GetCodeHeapInfo_LoaderCodeHeap_HasNonNullAddress(TestConfiguration config)
    {
        InitializeDumpTest(config);

        IExecutionManager em = Target.Contracts.ExecutionManager;
        List<TargetPointer> heaps = em.GetCodeHeapList();

        TargetPointer loaderHeapPtr = heaps.First(h => em.GetCodeHeapInfo(h) is LoaderCodeHeapInfo);
        LoaderCodeHeapInfo loader = (LoaderCodeHeapInfo)em.GetCodeHeapInfo(loaderHeapPtr);

        Assert.NotEqual(TargetPointer.Null, loader.LoaderHeapAddress);
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "Code heap list APIs were added after net10.0")]
    public void GetCodeHeapInfo_HostCodeHeap_HasValidAddresses(TestConfiguration config)
    {
        InitializeDumpTest(config);

        IExecutionManager em = Target.Contracts.ExecutionManager;
        List<TargetPointer> heaps = em.GetCodeHeapList();

        TargetPointer hostHeapPtr = heaps.First(h => em.GetCodeHeapInfo(h) is HostCodeHeapInfo);
        HostCodeHeapInfo host = (HostCodeHeapInfo)em.GetCodeHeapInfo(hostHeapPtr);

        Assert.NotEqual(TargetPointer.Null, host.BaseAddress);
        Assert.NotEqual(TargetPointer.Null, host.CurrentAddress);
        Assert.True(host.CurrentAddress.Value >= host.BaseAddress.Value,
            $"CurrentAddress (0x{host.CurrentAddress.Value:x}) should be >= BaseAddress (0x{host.BaseAddress.Value:x})");
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "Code heap list APIs were added after net10.0")]
    public void GetEEJitManagerInfo_ReturnsValidInfo(TestConfiguration config)
    {
        InitializeDumpTest(config);

        IExecutionManager em = Target.Contracts.ExecutionManager;
        JitManagerInfo info = em.GetEEJitManagerInfo();

        Assert.NotEqual(TargetPointer.Null, info.ManagerAddress);
        Assert.NotEqual(TargetPointer.Null, info.HeapListAddress);
    }
}
