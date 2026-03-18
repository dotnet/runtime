// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.DumpTests;

/// <summary>
/// Dump-based integration tests for the BuiltInCOM contract's GetRCWInterfaces API.
/// Uses the RCWInterfaces debuggee which creates a COM RCW and populates the
/// inline interface entry cache before crashing.
/// </summary>
public class RCWInterfacesDumpTests : DumpTestBase
{
    protected override string DebuggeeName => "RCWInterfaces";
    protected override string DumpType => "full";

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnOS(IncludeOnly = "windows", Reason = "COM interop (RCW) is only supported on Windows")]
    public void GetRCWInterfaces_FindsRCWAndEnumeratesInterfaces(TestConfiguration config)
    {
        InitializeDumpTest(config);
        IBuiltInCOM builtInCOM = Target.Contracts.BuiltInCOM;
        IObject objectContract = Target.Contracts.Object;
        IGC gcContract = Target.Contracts.GC;

        Assert.NotNull(builtInCOM);
        Assert.NotNull(objectContract);
        Assert.NotNull(gcContract);

        // Walk all strong GC handles to find objects with COM data (RCWs)
        List<HandleData> strongHandles = gcContract.GetHandles([HandleType.Strong]);
        TargetPointer rcwPtr = TargetPointer.Null;

        foreach (HandleData handleData in strongHandles)
        {
            TargetPointer objectAddress = Target.ReadPointer(handleData.Handle);
            if (objectAddress == TargetPointer.Null)
                continue;

            if (objectContract.GetBuiltInComData(objectAddress, out TargetPointer rcw, out _, out _)
                && rcw != TargetPointer.Null)
            {
                rcwPtr = rcw;
                break;
            }
        }

        Assert.NotEqual(TargetPointer.Null, rcwPtr);

        // Assert that the cookie is not null
        TargetPointer cookie = builtInCOM.GetRCWContext(rcwPtr);
        Assert.NotEqual(TargetPointer.Null, cookie);

        // Call GetRCWInterfaces on the found RCW — must not throw
        List<(TargetPointer MethodTable, TargetPointer Unknown)> interfaces =
            builtInCOM.GetRCWInterfaces(rcwPtr).ToList();

        // The debuggee interacts with the RCW via IGlobalInterfaceTable / IUnknown,
        // so the entry cache should have at least one cached interface entry
        Assert.True(interfaces.Count >= 1,
            $"Expected at least one cached interface entry in the RCW, got {interfaces.Count}");

        // Every returned entry must have non-null MethodTable and Unknown pointers
        foreach ((TargetPointer mt, TargetPointer unk) in interfaces)
        {
            Assert.NotEqual(TargetPointer.Null, mt);
            Assert.NotEqual(TargetPointer.Null, unk);
        }
    }
}
