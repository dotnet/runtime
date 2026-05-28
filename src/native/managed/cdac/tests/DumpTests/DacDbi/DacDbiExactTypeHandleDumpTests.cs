// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.Legacy;
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
    /// Drills into the specific shapes the debuggee allocates (primitive SzArray, reference
    /// SzArray, multi-dim array, generic instantiation, plain class) and asserts each one is
    /// covered by the round-trip — protects against the bulk pass silently skipping a shape
    /// because the debuggee stopped producing it.
    /// </summary>
    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    public unsafe void RoundTrip_CoversAllExpectedShapes(TestConfiguration config)
    {
        InitializeDumpTest(config);
        DacDbiImpl dbi = CreateDacDbi();
        IRuntimeTypeSystem rts = Target.Contracts.RuntimeTypeSystem;
        IGC gc = Target.Contracts.GC;

        bool sawObjectArray = false;     // object[]
        bool sawStringArray = false;     // string[]
        bool sawPrimitiveSzArray = false; // int[]
        bool sawMultiDimArray = false;   // int[,]
        bool sawGenericClass = false;    // Dictionary<string,int>
        bool sawPlainObject = false;     // System.Object
        bool sawString = false;          // System.String
        bool sawUserClass = false;       // ExactTypeHandle.Program+PlainClass

        HandleType[] handleKinds =
        [
            HandleType.Strong,
            HandleType.Pinned,
            HandleType.WeakLong,
            HandleType.Dependent,
        ];

        foreach (HandleData handle in gc.GetHandles(handleKinds))
        {
            TargetPointer objAddr = Target.ReadPointer(handle.Handle);
            if (objAddr == TargetPointer.Null)
                continue;

            TargetPointer mt = Target.Contracts.Object.GetMethodTableAddress(objAddr);
            TypeHandle th = rts.GetTypeHandle(mt);

            if (rts.IsArray(th, out uint rank))
            {
                TypeHandle elem = rts.GetTypeParam(th);
                CorElementType elemSig = rts.GetSignatureCorElementType(elem);
                if (rank > 1)
                    sawMultiDimArray = true;
                else if (rts.IsObject(elem))
                    sawObjectArray = true;
                else if (rts.IsString(elem))
                    sawStringArray = true;
                else if (elemSig == CorElementType.I4)
                    sawPrimitiveSzArray = true;
            }
            else if (rts.IsString(th))
            {
                sawString = true;
            }
            else if (rts.IsObject(th))
            {
                sawPlainObject = true;
            }
            else
            {
                ReadOnlySpan<TypeHandle> inst = rts.GetInstantiation(th);
                if (inst.Length > 0)
                    sawGenericClass = true;
                else
                    sawUserClass = true;
            }

            AssertRoundTrip(dbi, objAddr.Value);
        }

        Assert.True(sawObjectArray, "object[] not found among reachable handle objects");
        Assert.True(sawStringArray, "string[] not found among reachable handle objects");
        Assert.True(sawPrimitiveSzArray, "int[] not found among reachable handle objects");
        Assert.True(sawMultiDimArray, "int[,] not found among reachable handle objects");
        Assert.True(sawGenericClass, "Dictionary<string,int> not found among reachable handle objects");
        Assert.True(sawPlainObject, "System.Object instance not found among reachable handle objects");
        Assert.True(sawString, "System.String instance not found among reachable handle objects");
        Assert.True(sawUserClass, "PlainClass instance not found among reachable handle objects");
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
        TypeHandle expectedTh = rts.GetTypeHandle(expectedMT);

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

    private static DebuggerIPCE_BasicTypeData[] BuildArgInfoList(DacDbiImpl dbi, IRuntimeTypeSystem rts, TypeHandle typeHandle)
    {
        if (rts.IsArray(typeHandle, out _))
        {
            DebuggerIPCE_BasicTypeData[] one = new DebuggerIPCE_BasicTypeData[1];
            dbi.FillBasicTypeInfo(rts, rts.GetTypeParam(typeHandle), out one[0]);
            return one;
        }

        ReadOnlySpan<TypeHandle> instantiation = rts.GetInstantiation(typeHandle);
        if (instantiation.Length == 0)
            return Array.Empty<DebuggerIPCE_BasicTypeData>();

        DebuggerIPCE_BasicTypeData[] result = new DebuggerIPCE_BasicTypeData[instantiation.Length];
        for (int i = 0; i < instantiation.Length; i++)
            dbi.FillBasicTypeInfo(rts, instantiation[i], out result[i]);
        return result;
    }
}
