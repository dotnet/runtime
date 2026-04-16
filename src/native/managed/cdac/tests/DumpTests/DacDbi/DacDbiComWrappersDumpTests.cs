// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.Legacy;
using Microsoft.DotNet.XUnitExtensions;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.DumpTests;

/// <summary>
/// Dump-based integration tests for DacDbiImpl.GetObjectForCCW via the ComWrappers path.
/// Uses the ComWrappers debuggee (full dump).
/// </summary>
public class DacDbiComWrappersDumpTests : DumpTestBase
{
    protected override string DebuggeeName => "ComWrappers";
    protected override string DumpType => "full";

    private DacDbiImpl CreateDacDbi() => new DacDbiImpl(Target, legacyObj: null);

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "ComWrappers cDAC support not available in .NET 10")]
    public unsafe void GetObjectForCCW_ComWrappersIdentityPointer_ReturnsManagedObjectWrapperHandle(TestConfiguration config)
    {
        InitializeDumpTest(config);
        DacDbiImpl dbi = CreateDacDbi();
        IGC gc = Target.Contracts.GC;
        IComWrappers comWrappers = Target.Contracts.ComWrappers;

        foreach (HandleData handleData in gc.GetHandles([HandleType.Strong]))
        {
            TargetPointer objectAddress = Target.ReadPointer(handleData.Handle);
            if (objectAddress == TargetPointer.Null)
                continue;

            List<TargetPointer> mows = comWrappers.GetMOWs(objectAddress, out bool hasMowTable);
            if (!hasMowTable || mows.Count == 0)
                continue;

            TargetPointer mow = mows[0];
            TargetPointer identity = comWrappers.GetIdentityForMOW(mow);
            if (identity == TargetPointer.Null)
                continue;

            ulong expectedHandle = Target.ReadPointer(mow).Value;
            ulong actualHandle;
            int hr = dbi.GetObjectForCCW(identity.Value, &actualHandle);

            Assert.Equal(System.HResults.S_OK, hr);
            Assert.Equal(expectedHandle, actualHandle);

            // Verify the returned handle dereferences to a live object in the dump.
            TargetPointer actualHandleTarget = new(actualHandle);
            TargetPointer actualObjectAddress = Target.ReadPointer(actualHandleTarget);
            Assert.NotEqual(TargetPointer.Null, actualObjectAddress);
            return;
        }

        throw new SkipTestException("No ComWrappers MOW/CCW identity found in dump.");
    }
}
