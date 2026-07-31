// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Microsoft.Diagnostics.DataContractReader.Legacy;
using Microsoft.Diagnostics.DataContractReader.TestInfrastructure;
using Microsoft.Diagnostics.DataContractReader.Tests.ExecutionManager;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

public unsafe class FunctionTableAccessTests
{
    public static IEnumerable<object[]> StdArchAllVersions()
    {
        const int highestVersion = 2;
        foreach (object[] arr in new MockTarget.StdArch())
        {
            MockTarget.Architecture arch = (MockTarget.Architecture)arr[0];
            for (int version = 1; version <= highestVersion; version++)
            {
                yield return new object[] { $"c{version}", arch };
            }
        }
    }

    private sealed class FunctionTableScenario
    {
        public required Target Target { get; init; }
        public required ulong TableAddress { get; init; }
        public required IReadOnlyList<ulong> ExpectedEntryAddresses { get; init; }
        public required uint RuntimeFunctionSize { get; init; }

        public uint ExpectedBytes => (uint)(ExpectedEntryAddresses.Count * RuntimeFunctionSize);
    }

    private static FunctionTableScenario BuildScenario(string version, MockTarget.Architecture arch)
    {
        const ulong codeRangeStart = 0x0a0a_0000u;
        const uint codeRangeSize = 0xc000u;
        const uint methodSize = 0x100;
        const ulong personalityRoutine = 0x00cc_0000u;

        MockExecutionManagerBuilder emBuilder = new(version, arch, MockExecutionManagerBuilder.DefaultAllocationRange);

        MockExecutionManagerBuilder.JittedCodeRange jittedCode = emBuilder.AllocateJittedCodeRange(codeRangeStart, codeRangeSize);
        NibbleMapTestBuilderBase nibBuilder = emBuilder.CreateNibbleMap(codeRangeStart, codeRangeSize);

        MockExecutionManagerBuilder.JittedMethodWithUnwindInfo m0 = emBuilder.AddJittedMethodWithUnwindInfo(jittedCode, methodSize, 0x0101_0000, [0x10]);
        MockExecutionManagerBuilder.JittedMethodWithUnwindInfo m1 = emBuilder.AddJittedMethodWithUnwindInfo(jittedCode, methodSize, 0x0101_1000, [0x20, 0x40]);

        foreach (MockExecutionManagerBuilder.JittedMethodWithUnwindInfo m in new[] { m0, m1 })
            nibBuilder.AllocateCodeChunk(new TargetCodePointer(m.CodeAddress), methodSize);

        ulong moduleBase = arch.Is64Bit ? personalityRoutine : codeRangeStart;

        MockCodeHeapListNode node = emBuilder.AddCodeHeapListNode(
            next: 0,
            startAddress: codeRangeStart,
            endAddress: m1.CodeAddress + methodSize,
            mapBase: codeRangeStart,
            headerMap: nibBuilder.NibbleMapFragment.Address,
            clrPersonalityRoutine: personalityRoutine);
        emBuilder.SetAllCodeHeaps(node.Address);

        MockDynamicFunctionTable table = emBuilder.AddDynamicFunctionTable(moduleBase, emBuilder.EEJitManagerAddress);

        uint rfSize = (uint)emBuilder.RuntimeFunctionLayout.Size;

        // Entries are returned in descending method start order, ascending within a method.
        List<ulong> expected =
        [
            m1.UnwindInfosAddress,
            m1.UnwindInfosAddress + rfSize,
            m0.UnwindInfosAddress,
        ];

        return new FunctionTableScenario
        {
            Target = ExecutionManagerTests.CreateTarget(emBuilder),
            TableAddress = table.Address,
            ExpectedEntryAddresses = expected,
            RuntimeFunctionSize = rfSize,
        };
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void QueryInterfaceFromIXCLRDataProcess_ReturnsProcess3(MockTarget.Architecture arch)
    {
        TestPlaceholderTarget target = new TestPlaceholderTarget.Builder(arch).Build();
        SOSDacImpl impl = new(target, legacyObj: null);
        void* process = ComInterfaceMarshaller<IXCLRDataProcess>.ConvertToUnmanaged(impl);

        try
        {
            Guid iid = typeof(IXCLRDataProcess3).GUID;
            int hr = Marshal.QueryInterface((nint)process, in iid, out nint process3);

            Assert.Equal(HResults.S_OK, hr);
            Assert.NotEqual(nint.Zero, process3);

            try
            {
                Guid iidIUnknown = new("00000000-0000-0000-C000-000000000046");
                Assert.Equal(HResults.S_OK, Marshal.QueryInterface((nint)process, in iidIUnknown, out nint identity));
                try
                {
                    foreach (Type interfaceType in new[] { typeof(IXCLRDataProcess2), typeof(IXCLRDataProcess) })
                    {
                        Guid baseIid = interfaceType.GUID;
                        Assert.Equal(HResults.S_OK, Marshal.QueryInterface(process3, in baseIid, out nint baseInterface));
                        try
                        {
                            nint baseIdentity = nint.Zero;
                            try
                            {
                                Assert.Equal(HResults.S_OK, Marshal.QueryInterface(baseInterface, in iidIUnknown, out baseIdentity));
                                Assert.Equal(identity, baseIdentity);
                            }
                            finally
                            {
                                if (baseIdentity != nint.Zero)
                                    Marshal.Release(baseIdentity);
                            }
                        }
                        finally
                        {
                            Marshal.Release(baseInterface);
                        }
                    }
                }
                finally
                {
                    Marshal.Release(identity);
                }
            }
            finally
            {
                Marshal.Release(process3);
            }
        }
        finally
        {
            ComInterfaceMarshaller<IXCLRDataProcess>.Free(process);
        }
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetFunctionTable_NullOutParameters_ReturnsEPointer(MockTarget.Architecture arch)
    {
        TestPlaceholderTarget target = new TestPlaceholderTarget.Builder(arch).Build();
        IXCLRDataProcess3 process3 = new SOSDacImpl(target, legacyObj: null);

        uint bytesNeeded = uint.MaxValue;
        uint entries = uint.MaxValue;

        Assert.Equal(HResults.E_POINTER, process3.GetFunctionTable(new ClrDataAddress(0), 0, null, null, &entries));
        Assert.Equal(0u, entries);

        entries = uint.MaxValue;
        Assert.Equal(HResults.E_POINTER, process3.GetFunctionTable(new ClrDataAddress(0), 0, null, &bytesNeeded, null));
        Assert.Equal(0u, bytesNeeded);

        Assert.Equal(HResults.E_POINTER, process3.GetFunctionTable(new ClrDataAddress(0), 0, null, null, null));
    }

    [Theory]
    [MemberData(nameof(StdArchAllVersions))]
    public void GetFunctionTable_SizeQuery_ReturnsRequiredSize(string version, MockTarget.Architecture arch)
    {
        FunctionTableScenario scenario = BuildScenario(version, arch);
        IXCLRDataProcess3 process3 = new SOSDacImpl(scenario.Target, legacyObj: null);

        uint bytesNeeded = 0;
        uint entries = 0;
        int hr = process3.GetFunctionTable(new ClrDataAddress(scenario.TableAddress), 0, null, &bytesNeeded, &entries);

        Assert.Equal(HResults.S_FALSE, hr);
        Assert.Equal((uint)scenario.ExpectedEntryAddresses.Count, entries);
        Assert.Equal(scenario.ExpectedBytes, bytesNeeded);
    }

    [Theory]
    [MemberData(nameof(StdArchAllVersions))]
    public void GetFunctionTable_BufferTooSmall_ReturnsSFalseAndWritesNothing(string version, MockTarget.Architecture arch)
    {
        FunctionTableScenario scenario = BuildScenario(version, arch);
        IXCLRDataProcess3 process3 = new SOSDacImpl(scenario.Target, legacyObj: null);

        uint bytesNeeded = 0;
        uint entries = 0;
        byte[] buffer = new byte[scenario.RuntimeFunctionSize]; // room for only one entry
        Array.Fill(buffer, (byte)0xcc);

        int hr;
        fixed (byte* pBuffer = buffer)
        {
            hr = process3.GetFunctionTable(new ClrDataAddress(scenario.TableAddress), scenario.RuntimeFunctionSize, pBuffer, &bytesNeeded, &entries);
        }

        Assert.Equal(HResults.S_FALSE, hr);
        Assert.Equal((uint)scenario.ExpectedEntryAddresses.Count, entries);
        Assert.Equal(scenario.ExpectedBytes, bytesNeeded);
        Assert.All(buffer, b => Assert.Equal((byte)0xcc, b)); // nothing written
    }

    [Theory]
    [MemberData(nameof(StdArchAllVersions))]
    public void GetFunctionTable_SufficientBuffer_WritesEntries(string version, MockTarget.Architecture arch)
    {
        FunctionTableScenario scenario = BuildScenario(version, arch);
        IXCLRDataProcess3 process3 = new SOSDacImpl(scenario.Target, legacyObj: null);

        uint bytesNeeded = 0;
        uint entries = 0;
        byte[] buffer = new byte[scenario.ExpectedBytes];

        int hr;
        fixed (byte* pBuffer = buffer)
        {
            hr = process3.GetFunctionTable(new ClrDataAddress(scenario.TableAddress), (uint)buffer.Length, pBuffer, &bytesNeeded, &entries);
        }

        Assert.Equal(HResults.S_OK, hr);
        Assert.Equal((uint)scenario.ExpectedEntryAddresses.Count, entries);
        Assert.Equal(scenario.ExpectedBytes, bytesNeeded);

        // The buffer must hold each RUNTIME_FUNCTION entry, in order, exactly as stored in the target.
        byte[] expected = new byte[scenario.ExpectedBytes];
        for (int i = 0; i < scenario.ExpectedEntryAddresses.Count; i++)
        {
            scenario.Target.ReadBuffer(
                scenario.ExpectedEntryAddresses[i],
                expected.AsSpan(i * (int)scenario.RuntimeFunctionSize, (int)scenario.RuntimeFunctionSize));
        }

        Assert.Equal(expected, buffer);
    }

    [Theory]
    [MemberData(nameof(StdArchAllVersions))]
    public void GetFunctionTable_UnmatchedTable_ReturnsSOkWithZeroEntries(string version, MockTarget.Architecture arch)
    {
        // Reuse the scenario but point at an address with no matching heap by building a fresh table.
        const ulong codeRangeStart = 0x0a0a_0000u;
        const uint codeRangeSize = 0xc000u;
        const uint methodSize = 0x100;
        const ulong personalityRoutine = 0x00cc_0000u;

        MockExecutionManagerBuilder emBuilder = new(version, arch, MockExecutionManagerBuilder.DefaultAllocationRange);
        MockExecutionManagerBuilder.JittedCodeRange jittedCode = emBuilder.AllocateJittedCodeRange(codeRangeStart, codeRangeSize);
        NibbleMapTestBuilderBase nibBuilder = emBuilder.CreateNibbleMap(codeRangeStart, codeRangeSize);
        MockExecutionManagerBuilder.JittedMethodWithUnwindInfo m0 = emBuilder.AddJittedMethodWithUnwindInfo(jittedCode, methodSize, 0x0101_0000, [0x10]);
        nibBuilder.AllocateCodeChunk(new TargetCodePointer(m0.CodeAddress), methodSize);

        MockCodeHeapListNode node = emBuilder.AddCodeHeapListNode(
            next: 0,
            startAddress: codeRangeStart,
            endAddress: m0.CodeAddress + methodSize,
            mapBase: codeRangeStart,
            headerMap: nibBuilder.NibbleMapFragment.Address,
            clrPersonalityRoutine: personalityRoutine);
        emBuilder.SetAllCodeHeaps(node.Address);

        MockDynamicFunctionTable table = emBuilder.AddDynamicFunctionTable(0xbaad_0000, emBuilder.EEJitManagerAddress);
        Target target = ExecutionManagerTests.CreateTarget(emBuilder);

        IXCLRDataProcess3 process3 = new SOSDacImpl(target, legacyObj: null);

        uint bytesNeeded = uint.MaxValue;
        uint entries = uint.MaxValue;
        int hr = process3.GetFunctionTable(new ClrDataAddress(table.Address), 0, null, &bytesNeeded, &entries);

        Assert.Equal(HResults.S_OK, hr);
        Assert.Equal(0u, entries);
        Assert.Equal(0u, bytesNeeded);
    }
}
