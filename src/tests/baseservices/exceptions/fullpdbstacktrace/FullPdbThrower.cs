// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public static class FullPdbThrower
{
    // This assembly is built with DebugType=Full (a classic Windows PDB) on Windows.
    // Rendering the stack trace forces:
    // - Coreclr runtime to create an ISymUnmanagedReader for this module to map IL offsets back to source file and line, and also test that no-op IMetaDataImport importer we pass down doesn't assert.
    // - NativeAOT to verify line blob creation in ILC supports full PDB inputs.
    [Fact]
    [PlatformSpecific(TestPlatforms.Windows)]
    public static void ExceptionToString_FullPdb_IncludesSourceLine()
    {
        try
        {
            Throw();
            Assert.Fail("Throw() did not throw.");
        }
        catch (InvalidOperationException ex)
        {
            string stackTrace = ex.ToString();
            Assert.Contains(nameof(Throw), stackTrace);

            // Mapping the IL offset back to the source file goes through diasymreader (the no-op
            // importer path). Native AOT resolves stack frames without DIA, so only assert the
            // source file when running on a runtime that uses the classic-PDB reader.
            if (!TestLibrary.Utilities.IsNativeAot)
            {
                Assert.Contains("FullPdbThrower.cs", stackTrace);
            }
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    private static void Throw()
    {
        throw new InvalidOperationException("Exception from full PDB test assembly.");
    }
}
