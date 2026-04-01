// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.Legacy;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.DumpTests;

/// <summary>
/// Dump-based integration tests for DacDbiImpl GC methods.
/// Uses the BasicThreads debuggee (heap dump).
/// </summary>
public class DacDbiGCDumpTests : DumpTestBase
{
    protected override string DebuggeeName => "BasicThreads";

    private DacDbiImpl CreateDacDbi() => new DacDbiImpl(Target, legacyObj: null);

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    public unsafe void AreGCStructuresValid_CrossValidateWithContract(TestConfiguration config)
    {
        InitializeDumpTest(config);
        DacDbiImpl dbi = CreateDacDbi();

        Interop.BOOL result;
        int hr = dbi.AreGCStructuresValid(&result);
        Assert.Equal(System.HResults.S_OK, hr);

        bool contractResult = Target.Contracts.GC.GetGCStructuresValid();
        Assert.Equal(contractResult, result == Interop.BOOL.TRUE);
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    public unsafe void GetGCHeapInformation_Succeeds(TestConfiguration config)
    {
        InitializeDumpTest(config);
        DacDbiImpl dbi = CreateDacDbi();

        COR_HEAPINFO heapInfo;
        int hr = dbi.GetGCHeapInformation(&heapInfo);
        Assert.Equal(System.HResults.S_OK, hr);

        Assert.True(heapInfo.pointerSize == 4 || heapInfo.pointerSize == 8);
        Assert.True(heapInfo.numHeaps >= 1);
        Assert.True(heapInfo.areGCStructuresValid == Interop.BOOL.TRUE || heapInfo.areGCStructuresValid == Interop.BOOL.FALSE);
        Assert.True(heapInfo.concurrent == Interop.BOOL.TRUE || heapInfo.concurrent == Interop.BOOL.FALSE);
        Assert.True(heapInfo.gcType == 0 || heapInfo.gcType == 1);
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    public unsafe void GetGCHeapInformation_MatchesPointerSize(TestConfiguration config)
    {
        InitializeDumpTest(config);
        DacDbiImpl dbi = CreateDacDbi();

        COR_HEAPINFO heapInfo;
        int hr = dbi.GetGCHeapInformation(&heapInfo);
        Assert.Equal(System.HResults.S_OK, hr);

        Assert.Equal((uint)Target.PointerSize, heapInfo.pointerSize);
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    public unsafe void GetGCHeapInformation_CrossValidateWithContract(TestConfiguration config)
    {
        InitializeDumpTest(config);
        DacDbiImpl dbi = CreateDacDbi();

        COR_HEAPINFO heapInfo;
        int hr = dbi.GetGCHeapInformation(&heapInfo);
        Assert.Equal(System.HResults.S_OK, hr);

        IGC gc = Target.Contracts.GC;
        bool contractValid = gc.GetGCStructuresValid();
        Assert.Equal(contractValid, heapInfo.areGCStructuresValid == Interop.BOOL.TRUE);

        uint heapCount = gc.GetGCHeapCount();
        Assert.Equal(heapCount, heapInfo.numHeaps);
    }
}
