// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.Legacy;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.DumpTests;

/// <summary>
/// Dump-based integration tests for DacDbiImpl IDebugger contract methods.
/// Uses the BasicThreads debuggee (heap dump).
/// </summary>
public class DacDbiDebuggerDumpTests : DumpTestBase
{
    protected override string DebuggeeName => "BasicThreads";

    private DacDbiImpl CreateDacDbi() => new DacDbiImpl(Target, legacyObj: null);

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    public unsafe void IsLeftSideInitialized_ReturnsNonZero(TestConfiguration config)
    {
        InitializeDumpTest(config);
        DacDbiImpl dbi = CreateDacDbi();

        Interop.BOOL result;
        int hr = dbi.IsLeftSideInitialized(&result);
        Assert.Equal(System.HResults.S_OK, hr);
        Assert.NotEqual(Interop.BOOL.FALSE, result);
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    public unsafe void GetAttachStateFlags_Succeeds(TestConfiguration config)
    {
        InitializeDumpTest(config);
        DacDbiImpl dbi = CreateDacDbi();

        int flags;
        int hr = dbi.GetAttachStateFlags(&flags);
        Assert.Equal(System.HResults.S_OK, hr);
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    public unsafe void GetDefinesBitField_Succeeds(TestConfiguration config)
    {
        InitializeDumpTest(config);
        DacDbiImpl dbi = CreateDacDbi();

        uint defines;
        int hr = dbi.GetDefinesBitField(&defines);
        Assert.Equal(System.HResults.S_OK, hr);
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    public unsafe void GetMDStructuresVersion_Succeeds(TestConfiguration config)
    {
        InitializeDumpTest(config);
        DacDbiImpl dbi = CreateDacDbi();

        uint version;
        int hr = dbi.GetMDStructuresVersion(&version);
        Assert.Equal(System.HResults.S_OK, hr);
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    public unsafe void MetadataUpdatesApplied_Succeeds(TestConfiguration config)
    {
        InitializeDumpTest(config);
        DacDbiImpl dbi = CreateDacDbi();

        Interop.BOOL result;
        int hr = dbi.MetadataUpdatesApplied(&result);
        Assert.Equal(System.HResults.S_OK, hr);
        Assert.True(result == Interop.BOOL.TRUE || result == Interop.BOOL.FALSE);
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    public unsafe void IsLeftSideInitialized_CrossValidateWithContract(TestConfiguration config)
    {
        InitializeDumpTest(config);
        DacDbiImpl dbi = CreateDacDbi();

        Interop.BOOL dbiResult;
        int hr = dbi.IsLeftSideInitialized(&dbiResult);
        Assert.Equal(System.HResults.S_OK, hr);

        bool contractResult = Target.Contracts.Debugger.TryGetDebuggerData(out Contracts.DebuggerData data);
        Assert.Equal(contractResult && data.IsLeftSideInitialized, dbiResult != Interop.BOOL.FALSE);
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    public unsafe void GetDefinesBitField_CrossValidateWithContract(TestConfiguration config)
    {
        InitializeDumpTest(config);
        DacDbiImpl dbi = CreateDacDbi();

        uint dbiResult;
        int hr = dbi.GetDefinesBitField(&dbiResult);
        Assert.Equal(System.HResults.S_OK, hr);

        Assert.True(Target.Contracts.Debugger.TryGetDebuggerData(out Contracts.DebuggerData data));
        Assert.Equal(data.DefinesBitField, dbiResult);
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    public unsafe void GetMDStructuresVersion_CrossValidateWithContract(TestConfiguration config)
    {
        InitializeDumpTest(config);
        DacDbiImpl dbi = CreateDacDbi();

        uint dbiResult;
        int hr = dbi.GetMDStructuresVersion(&dbiResult);
        Assert.Equal(System.HResults.S_OK, hr);

        Assert.True(Target.Contracts.Debugger.TryGetDebuggerData(out Contracts.DebuggerData data));
        Assert.Equal(data.MDStructuresVersion, dbiResult);
    }
}
