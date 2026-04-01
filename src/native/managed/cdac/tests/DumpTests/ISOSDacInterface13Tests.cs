// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using Microsoft.Diagnostics.DataContractReader.Legacy;
using Xunit;
using static Microsoft.Diagnostics.DataContractReader.Tests.TestHelpers;

namespace Microsoft.Diagnostics.DataContractReader.DumpTests;

/// <summary>
/// Dump-based integration tests for ISOSDacInterface13 APIs (loader allocator heaps).
/// Uses the MultiModule debuggee dump, which loads assemblies from multiple ALCs.
/// </summary>
public class ISOSDacInterface13Tests : DumpTestBase
{
    protected override string DebuggeeName => "MultiModule";
    protected override string DumpType => "full";

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    public unsafe void GetLoaderAllocatorHeapNames_MatchExpectedOrder(TestConfiguration config)
    {
        InitializeDumpTest(config);
        ISOSDacInterface13 sosDac = (ISOSDacInterface13)new SOSDacImpl(Target, legacyObj: null);

        int heapCount;
        int hr = sosDac.GetLoaderAllocatorHeapNames(0, null, &heapCount);
        // S_FALSE is expected when count (0) < actual heap count
        Assert.True(hr == System.HResults.S_OK || hr == System.HResults.S_FALSE,
            $"GetLoaderAllocatorHeapNames failed with {FormatHResult(hr)}");
        Assert.True(heapCount > 0, "Expected at least one loader allocator heap name");

        char** ppNames = stackalloc char*[heapCount];
        hr = sosDac.GetLoaderAllocatorHeapNames(heapCount, ppNames, null);
        AssertHResult(System.HResults.S_OK, hr);

        var actualNames = new string[heapCount];
        for (int i = 0; i < heapCount; i++)
        {
            actualNames[i] = Marshal.PtrToStringAnsi((nint)ppNames[i])!;
            Assert.NotNull(actualNames[i]);
            Assert.NotEmpty(actualNames[i]);
        }

        // The order must match LoaderAllocatorLoaderHeapNames in request.cpp,
        // filtered to only those present in the data descriptors.
        string[] expectedOrder =
        [
            "LowFrequencyHeap",
            "HighFrequencyHeap",
            "StaticsHeap",
            "StubHeap",
            "ExecutableHeap",
            "FixupPrecodeHeap",
            "NewStubPrecodeHeap",
            "DynamicHelpersStubHeap",
            "IndcellHeap",
            "CacheEntryHeap",
        ];

        // The cDAC filters by which fields exist in the data descriptors,
        // so the actual names should be a subsequence of the expected order.
        int expectedIdx = 0;
        for (int i = 0; i < actualNames.Length; i++)
        {
            while (expectedIdx < expectedOrder.Length && expectedOrder[expectedIdx] != actualNames[i])
                expectedIdx++;
            Assert.True(expectedIdx < expectedOrder.Length,
                $"Heap name '{actualNames[i]}' at index {i} is out of expected order. Actual: [{string.Join(", ", actualNames)}]");
            expectedIdx++;
        }
    }
}
