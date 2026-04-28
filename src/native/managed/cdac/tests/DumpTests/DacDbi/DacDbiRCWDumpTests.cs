// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.Legacy;
using Microsoft.DotNet.XUnitExtensions;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.DumpTests;

/// <summary>
/// Dump-based integration tests for DacDbiImpl.IsRcw.
/// Uses the RCW debuggee (full dump).
/// </summary>
public class DacDbiRCWDumpTests : DumpTestBase
{
    protected override string DebuggeeName => "RCW";
    private DacDbiImpl CreateDacDbi() => new DacDbiImpl(Target, legacyObj: null);

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnOS(IncludeOnly = "windows", Reason = "COM interop (RCW) is only supported on Windows")]
    public unsafe void IsRcw_ReturnsTrueForBuiltInRcwObject(TestConfiguration config)
    {
        InitializeDumpTest(config);
        DacDbiImpl dbi = CreateDacDbi();
        IGC gc = Target.Contracts.GC;
        IObject obj = Target.Contracts.Object;

        foreach (HandleData handleData in gc.GetHandles([HandleType.Strong]))
        {
            TargetPointer objectAddress = Target.ReadPointer(handleData.Handle);
            if (objectAddress == TargetPointer.Null)
                continue;

            if (!obj.GetBuiltInComData(objectAddress, out TargetPointer rcw, out _, out _) || rcw == TargetPointer.Null)
                continue;

            Interop.BOOL result;
            int hr = dbi.IsRcw(objectAddress.Value, &result);
            Assert.Equal(System.HResults.S_OK, hr);
            Assert.Equal(Interop.BOOL.TRUE, result);
            return;
        }

        throw new SkipTestException("No built-in RCW object found in dump.");
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnOS(IncludeOnly = "windows", Reason = "COM interop (RCW) is only supported on Windows")]
    public unsafe void IsRcw_ReturnsFalseForNonRcwObject(TestConfiguration config)
    {
        InitializeDumpTest(config);
        DacDbiImpl dbi = CreateDacDbi();
        IGC gc = Target.Contracts.GC;
        IObject obj = Target.Contracts.Object;

        foreach (HandleData handleData in gc.GetHandles([HandleType.Strong]))
        {
            TargetPointer objectAddress = Target.ReadPointer(handleData.Handle);
            if (objectAddress == TargetPointer.Null)
                continue;

            _ = obj.GetBuiltInComData(objectAddress, out TargetPointer rcw, out _, out _);
            if (rcw != TargetPointer.Null)
                continue;

            Interop.BOOL result;
            int hr = dbi.IsRcw(objectAddress.Value, &result);
            Assert.Equal(System.HResults.S_OK, hr);
            Assert.Equal(Interop.BOOL.FALSE, result);
            return;
        }

        throw new SkipTestException("No non-RCW object found in dump.");
    }
}
