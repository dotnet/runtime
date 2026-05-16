// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.Legacy;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.DumpTests;

/// <summary>
/// Dump-based integration tests for DacDbiImpl methods that need a debuggee with
/// extra loader features. Uses the MultiModule debuggee (heap dump), which
/// additionally loads an assembly from in-memory bytes with an in-memory PDB
/// so that the <c>GetSymbolsBuffer</c> / <c>TryGetSymbolStream</c> code path can
/// be exercised against an actual module with in-memory symbols.
/// </summary>
public class DacDbiMultiModuleDumpTests : DumpTestBase
{
    protected override string DebuggeeName => "MultiModule";

    private DacDbiImpl CreateDacDbi() => new DacDbiImpl(Target, legacyObj: null);

    private IEnumerable<ModuleHandle> GetAllModules()
    {
        ILoader loader = Target.Contracts.Loader;
        TargetPointer appDomainPtr = Target.ReadGlobalPointer(Constants.Globals.AppDomain);
        ulong appDomain = Target.ReadPointer(appDomainPtr);
        return loader.GetModuleHandles(new TargetPointer(appDomain),
            AssemblyIterationFlags.IncludeLoaded | AssemblyIterationFlags.IncludeExecution);
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    public unsafe void GetSymbolsBuffer_FindsInMemorySymbols(TestConfiguration config)
    {
        InitializeDumpTest(config);
        DacDbiImpl dbi = CreateDacDbi();
        ILoader loader = Target.Contracts.Loader;

        bool foundInMemorySymbols = false;
        foreach (ModuleHandle module in GetAllModules())
        {
            TargetPointer moduleAddr = loader.GetModule(module);

            DacDbiTargetBuffer targetBuffer;
            SymbolFormat symbolFormat;
            int hr = dbi.GetSymbolsBuffer(moduleAddr.Value, &targetBuffer, &symbolFormat);
            Assert.Equal(System.HResults.S_OK, hr);

            if (symbolFormat == SymbolFormat.Pdb)
            {
                // When PDB symbols are reported, the buffer must be non-empty.
                Assert.NotEqual(0UL, targetBuffer.pAddress);
                Assert.NotEqual(0u, targetBuffer.cbSize);
                foundInMemorySymbols = true;
            }
            else
            {
                Assert.Equal(0UL, targetBuffer.pAddress);
                Assert.Equal(0u, targetBuffer.cbSize);
            }
        }

        Assert.True(foundInMemorySymbols,
            "Expected at least one module in the MultiModule debuggee dump to have an in-memory PDB symbol stream.");
    }
}
