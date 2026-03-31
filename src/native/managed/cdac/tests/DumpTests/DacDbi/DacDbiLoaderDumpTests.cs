// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.Legacy;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.DumpTests;

/// <summary>
/// Dump-based integration tests for DacDbiImpl loader, assembly, and module methods.
/// Uses the MultiModule debuggee (full dump), which loads assemblies from multiple ALCs.
/// </summary>
public class DacDbiLoaderDumpTests : DumpTestBase
{
    protected override string DebuggeeName => "MultiModule";
    protected override string DumpType => "full";

    private DacDbiImpl CreateDacDbi() => new DacDbiImpl(Target, legacyObj: null);

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    public void GetAppDomainFullName_ReturnsNonEmpty(TestConfiguration config)
    {
        InitializeDumpTest(config);
        DacDbiImpl dbi = CreateDacDbi();

        TargetPointer appDomainPtr = Target.ReadGlobalPointer(Constants.Globals.AppDomain);
        ulong appDomain = Target.ReadPointer(appDomainPtr);

        using var holder = new NativeStringHolder();
        int hr = dbi.GetAppDomainFullName(appDomain, holder.Ptr);
        Assert.Equal(System.HResults.S_OK, hr);
        Assert.False(string.IsNullOrEmpty(holder.Value), "AppDomain name should not be empty");
    }
}
