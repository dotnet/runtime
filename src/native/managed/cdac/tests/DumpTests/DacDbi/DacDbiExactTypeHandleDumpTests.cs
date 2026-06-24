// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.Legacy;
using Microsoft.Diagnostics.DataContractReader.TestInfrastructure;
using Microsoft.DotNet.XUnitExtensions;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.DumpTests;

/// <summary>
/// Dump-based round-trip tests for <see cref="DacDbiImpl.GetExactTypeHandle"/>.
/// For every heap object reachable from a GC handle in the ExactTypeHandle debuggee, takes the
/// <c>(DebuggerIPCE_ExpandedTypeData, ArgInfoList)</c> pair the right-side debugger would send
/// over IPC and feeds it back through <c>GetExactTypeHandle</c>; the resulting
/// <c>vmTypeHandle</c> must equal the object's MethodTable.
///
/// <para>
/// The right-side <c>ArgInfoList</c> isn't produced by any public DacDbi API — the debugger walks
/// the type itself. Tests reproduce that walk by reusing the internal helper
/// <see cref="DacDbiImpl.FillBasicTypeInfo"/> via <c>InternalsVisibleTo</c>.
/// </para>
/// </summary>
public class DacDbiExactTypeHandleDumpTests : DumpTestBase
{
    protected override string DebuggeeName => "ExactTypeHandle";

    private DacDbiImpl CreateDacDbi() => new DacDbiImpl(Target, legacyObj: null);

    /// <summary>
    /// All non-null heap objects reachable from Strong / Pinned / WeakLong / Dependent handles
    /// in the debuggee round-trip through GetExactTypeHandle to their MethodTable.
    /// </summary>
    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    public unsafe void RoundTrip_AllReachableHandleObjects_MatchMethodTable(TestConfiguration config)
    {
        InitializeDumpTest(config);
        DacDbiImpl dbi = CreateDacDbi();
        IGC gc = Target.Contracts.GC;

        HandleType[] handleKinds =
        [
            HandleType.Strong,
            HandleType.Pinned,
            HandleType.WeakLong,
            HandleType.Dependent,
        ];

        int roundTripped = 0;
        foreach (HandleData handle in gc.GetHandles(handleKinds))
        {
            TargetPointer objAddr = Target.ReadPointer(handle.Handle);
            if (objAddr == TargetPointer.Null)
                continue;

            AssertRoundTrip(dbi, objAddr.Value);
            roundTripped++;
        }

        Assert.True(roundTripped > 0, "Expected at least one reachable handle object to round-trip.");
    }

    /// <summary>
    /// For an object at <paramref name="objAddr"/>: compute the right-side
    /// <c>(DebuggerIPCE_ExpandedTypeData, ArgInfoList)</c> pair, feed it to
    /// <see cref="DacDbiImpl.GetExactTypeHandle"/>, and assert the resulting <c>vmTypeHandle</c>
    /// equals the object's MethodTable.
    /// </summary>
    private unsafe void AssertRoundTrip(DacDbiImpl dbi, ulong objAddr)
    {
        IRuntimeTypeSystem rts = Target.Contracts.RuntimeTypeSystem;

        TargetPointer expectedMT = Target.Contracts.Object.GetMethodTableAddress(new TargetPointer(objAddr));
        ITypeHandle expectedTh = rts.GetTypeHandle(expectedMT);

        DebuggerIPCE_ExpandedTypeData expanded;
        int hr = dbi.GetObjectExpandedTypeInfo(AreValueTypesBoxed.NoValueTypeBoxing, objAddr, &expanded);
        Assert.Equal(System.HResults.S_OK, hr);

        // Build the ArgInfoList the right side would send. Mirrors the shape consumed by
        // GetExactArrayTypeHandle / GetExactClassTypeHandle / GetExactPtrOrByRefTypeHandle:
        //   Array/SzArray   -> 1 entry, the element type
        //   Ptr/Byref       -> 1 entry, the referent type   (unreachable from heap objects)
        //   Class/ValueType -> N entries, one per generic-instantiation arg (0 if non-generic)
        //   FnPtr           -> N entries, ret + args        (unreachable from heap objects)
        //   anything else   -> ArgInfoList is unused (default branch returns a primitive type)
        DebuggerIPCE_BasicTypeData[] args = BuildArgInfoList(dbi, rts, expectedTh);

        fixed (DebuggerIPCE_BasicTypeData* pArgs = args)
        {
            ArgInfoList argInfo = new ArgInfoList { m_pList = pArgs, m_nEntries = args.Length };
            ulong vmTh;
            hr = dbi.GetExactTypeHandle(&expanded, &argInfo, &vmTh);
            Assert.Equal(System.HResults.S_OK, hr);
            Assert.Equal(expectedMT.Value, vmTh);
        }
    }

    private static DebuggerIPCE_BasicTypeData[] BuildArgInfoList(DacDbiImpl dbi, IRuntimeTypeSystem rts, ITypeHandle typeHandle)
    {
        if (rts.IsArray(typeHandle, out _))
        {
            DebuggerIPCE_BasicTypeData[] one = new DebuggerIPCE_BasicTypeData[1];
            dbi.FillBasicTypeInfo(rts, rts.GetTypeParam(typeHandle), out one[0]);
            return one;
        }

        ImmutableArray<ITypeHandle> instantiation = rts.GetInstantiation(typeHandle);
        if (instantiation.Length == 0)
            return Array.Empty<DebuggerIPCE_BasicTypeData>();

        DebuggerIPCE_BasicTypeData[] result = new DebuggerIPCE_BasicTypeData[instantiation.Length];
        for (int i = 0; i < instantiation.Length; i++)
            dbi.FillBasicTypeInfo(rts, instantiation[i], out result[i]);
        return result;
    }
}
