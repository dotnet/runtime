// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.Legacy;
using Microsoft.DotNet.XUnitExtensions;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.DumpTests;

/// <summary>
/// Dump-based integration tests for DacDbiImpl.GetObjectForCCW.
/// Uses the CCW debuggee (full dump).
/// </summary>
public class DacDbiCCWDumpTests : DumpTestBase
{
    protected override string DebuggeeName => "CCW";
    private DacDbiImpl CreateDacDbi() => new DacDbiImpl(Target, legacyObj: null);

    private (TargetPointer Ccw, TargetPointer InterfacePointer) FindBuiltInComCcwWithInterface()
    {
        IGC gc = Target.Contracts.GC;
        IObject obj = Target.Contracts.Object;
        IBuiltInCOM builtInCOM = Target.Contracts.BuiltInCOM;

        foreach (HandleData handleData in gc.GetHandles([HandleType.Strong]))
        {
            TargetPointer objectAddress = Target.ReadPointer(handleData.Handle);
            if (objectAddress == TargetPointer.Null)
                continue;

            if (!obj.GetBuiltInComData(objectAddress, out _, out TargetPointer ccw, out _)
                || ccw == TargetPointer.Null)
            {
                continue;
            }

            // Normalize to the start wrapper, matching what DacDbiImpl.GetObjectForCCW does
            // before calling GetObjectHandle, so the expected handle in the test is consistent.
            TargetPointer startCcw = builtInCOM.GetStartWrapper(ccw);

            List<COMInterfacePointerData> interfaces = builtInCOM.GetCCWInterfaces(startCcw).ToList();
            if (interfaces.Count == 0)
                continue;

            return (startCcw, interfaces[0].InterfacePointerAddress);
        }

        throw new SkipTestException("No BuiltInCOM CCW interface pointer found in dump.");
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnOS(IncludeOnly = "windows", Reason = "COM callable wrappers require Windows")]
    public unsafe void GetObjectForCCW_ReturnsBuiltInComObjectHandle(TestConfiguration config)
    {
        InitializeDumpTest(config);
        DacDbiImpl dbi = CreateDacDbi();
        IBuiltInCOM builtInCOM = Target.Contracts.BuiltInCOM;

        (TargetPointer ccw, TargetPointer interfacePointer) = FindBuiltInComCcwWithInterface();

        ulong resultHandle;
        int hr = dbi.GetObjectForCCW(interfacePointer.Value, &resultHandle);

        Assert.Equal(System.HResults.S_OK, hr);
        Assert.Equal(builtInCOM.GetObjectHandle(ccw).Value, resultHandle);
        Assert.NotEqual(TargetPointer.Null, Target.ReadPointer(new TargetPointer(resultHandle)));
    }
}
