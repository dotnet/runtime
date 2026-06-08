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
/// Dump-based round-trip tests for <see cref="DacDbiImpl.GetApproxTypeHandle"/>.
/// For every heap object reachable from a GC handle in the ExactTypeHandle debuggee, builds the
/// flattened <c>DebuggerIPCE_TypeArgData[]</c> tree that the right-side debugger would send
/// over IPC (via <c>CordbType::GatherTypeData</c>), feeds it back through
/// <c>GetApproxTypeHandle</c>, and asserts the resulting <c>vmTypeHandle</c> equals the
/// expected canonicalized MethodTable.
///
/// <para>
/// Canonicalization rules (mirror of
/// <see cref="TypeDataWalk.ReadLoadedTypeHandle"/> / <c>ReadLoadedTypeArg</c> /
/// <c>ClassTypeArg</c>):
///   - Top-level Class / ValueType keeps the typeDef; only its type-args are canonicalized.
///   - Reference-typed generic args collapse to <c>System.__Canon</c>.
///   - Value-type args are recursively approximated (with the same rules applied to their args).
///   - Array / Ptr / Byref: outer shape preserved; the inner type goes through the
///     type-arg canonicalization rules.
/// </para>
/// </summary>
public class DacDbiApproxTypeHandleDumpTests : DumpTestBase
{
    protected override string DebuggeeName => "ExactTypeHandle";

    private DacDbiImpl CreateDacDbi() => new DacDbiImpl(Target, legacyObj: null);

    /// <summary>
    /// All non-null heap objects reachable from Strong / Pinned / WeakLong / Dependent handles
    /// in the debuggee round-trip through GetApproxTypeHandle to their expected canonicalized
    /// MethodTable.
    /// </summary>
    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    public unsafe void RoundTrip_AllReachableHandleObjects_MatchApproxMethodTable(TestConfiguration config)
    {
        InitializeDumpTest(config);
        DacDbiImpl dbi = CreateDacDbi();
        IGC gc = Target.Contracts.GC;
        IRuntimeTypeSystem rts = Target.Contracts.RuntimeTypeSystem;

        TargetPointer canonMtPtr = Target.ReadPointer(Target.ReadGlobalPointer(Constants.Globals.CanonMethodTable));
        TypeHandle canonTh = rts.GetTypeHandle(canonMtPtr);

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

            AssertRoundTrip(dbi, rts, canonTh, objAddr.Value);
            roundTripped++;
        }

        Assert.True(roundTripped > 0, "Expected at least one reachable handle object to round-trip.");
    }

    /// <summary>
    /// For an object at <paramref name="objAddr"/>: build the right-side flat
    /// <c>DebuggerIPCE_TypeArgData[]</c>, feed it to <see cref="DacDbiImpl.GetApproxTypeHandle"/>,
    /// and assert the resulting <c>vmTypeHandle</c> equals the expected canonicalized
    /// MethodTable for the object's type.
    /// </summary>
    private unsafe void AssertRoundTrip(DacDbiImpl dbi, IRuntimeTypeSystem rts, TypeHandle canonTh, ulong objAddr)
    {
        TargetPointer expectedMT = Target.Contracts.Object.GetMethodTableAddress(new TargetPointer(objAddr));
        TypeHandle expectedTh = rts.GetTypeHandle(expectedMT);

        // Build the expected canonicalized handle from the exact type. Mirrors the rules
        // applied by TypeDataWalk on the cDAC side.
        TypeHandle expectedApproxTh = ApproxTopLevel(rts, canonTh, expectedTh);
        Assert.False(expectedApproxTh.IsNull, $"Failed to compute expected approximate TypeHandle for object at 0x{objAddr:x} (MT 0x{expectedMT.Value:x}).");

        // Build the flat DebuggerIPCE_TypeArgData[] tree (preorder DFS) the right side would
        // send. Two passes: count, then fill.
        int count = CountTypeNodes(rts, expectedTh);
        DebuggerIPCE_TypeArgData[] nodes = new DebuggerIPCE_TypeArgData[count];
        fixed (DebuggerIPCE_TypeArgData* pNodes = nodes)
        {
            int idx = 0;
            FillTypeNodes(dbi, rts, expectedTh, pNodes, ref idx);
            Assert.Equal(count, idx);

            TypeInfoList list = new TypeInfoList { m_pList = pNodes, m_nEntries = nodes.Length };
            ulong vmTh;
            int hr = dbi.GetApproxTypeHandle(&list, &vmTh);
            Assert.Equal(System.HResults.S_OK, hr);
            Assert.Equal(expectedApproxTh.Address.Value, vmTh);
        }
    }

    // ----------------------------------------------------------------------------------------
    // GatherTypeData port: produce a flattened preorder DFS of DebuggerIPCE_TypeArgData[].
    // Mirrors CordbType::CountTypeDataNodes / CordbType::GatherTypeData (rstype.cpp:2580/2619).
    // Number of children that follow a node in the flat list:
    //   Array / SzArray   -> 1 (element type)
    //   Ptr   / Byref     -> 1 (referent type)        (unreachable from heap objects)
    //   Class / ValueType -> instantiation.Length     (0 if non-generic)
    //   FnPtr             -> arg count                 (unreachable from heap objects)
    //   anything else     -> 0
    // ----------------------------------------------------------------------------------------

    private static int CountTypeNodes(IRuntimeTypeSystem rts, TypeHandle th)
    {
        CorElementType et = GetElementType(rts, th);
        switch (et)
        {
            case CorElementType.Array:
            case CorElementType.SzArray:
            case CorElementType.Ptr:
            case CorElementType.Byref:
                return 1 + CountTypeNodes(rts, rts.GetTypeParam(th));

            case CorElementType.Class:
            case CorElementType.ValueType:
                {
                    int total = 1;
                    foreach (TypeHandle arg in rts.GetInstantiation(th))
                        total += CountTypeNodes(rts, arg);
                    return total;
                }

            default:
                return 1;
        }
    }

    private static unsafe void FillTypeNodes(DacDbiImpl dbi, IRuntimeTypeSystem rts, TypeHandle th, DebuggerIPCE_TypeArgData* nodes, ref int idx)
    {
        int self = idx++;
        DebuggerIPCE_TypeArgData* pSelf = &nodes[self];

        // Use the production TypeHandleToExpandedTypeInfo to fill the per-node payload —
        // exactly the data the right side would have produced from its own CordbType tree.
        int hr = dbi.TypeHandleToExpandedTypeInfo(AreValueTypesBoxed.NoValueTypeBoxing, th.Address.Value, &pSelf->data);
        Assert.Equal(System.HResults.S_OK, hr);

        CorElementType et = GetElementType(rts, th);
        switch (et)
        {
            case CorElementType.Array:
            case CorElementType.SzArray:
            case CorElementType.Ptr:
            case CorElementType.Byref:
                pSelf->numTypeArgs = 1;
                FillTypeNodes(dbi, rts, rts.GetTypeParam(th), nodes, ref idx);
                break;

            case CorElementType.Class:
            case CorElementType.ValueType:
                {
                    ReadOnlySpan<TypeHandle> inst = rts.GetInstantiation(th);
                    pSelf->numTypeArgs = (uint)inst.Length;
                    for (int i = 0; i < inst.Length; i++)
                        FillTypeNodes(dbi, rts, inst[i], nodes, ref idx);
                    break;
                }

            default:
                pSelf->numTypeArgs = 0;
                break;
        }
    }

    // ----------------------------------------------------------------------------------------
    // Expected approximate-handle computation. Mirrors TypeDataWalk.ReadLoadedTypeHandle (top
    // level) and ReadLoadedTypeArg / ClassTypeArg / ObjRefOrPrimitiveTypeArg (arg context).
    // ----------------------------------------------------------------------------------------

    // Top-level: Class / ValueType retain their own typeDef; type-args are canonicalized via
    // ApproxTypeArg. Array / Ptr / Byref preserve the outer shape; the inner type goes through
    // ApproxTypeArg. Anything else collapses to the primitive type for its element type
    // (e.g. System.Object, System.String, primitives).
    private TypeHandle ApproxTopLevel(IRuntimeTypeSystem rts, TypeHandle canonTh, TypeHandle th)
    {
        CorElementType et = GetElementType(rts, th);
        switch (et)
        {
            case CorElementType.Array:
            case CorElementType.SzArray:
                {
                    TypeHandle elem = ApproxTypeArg(rts, canonTh, rts.GetTypeParam(th));
                    rts.IsArray(th, out uint rank);
                    return rts.GetConstructedType(elem, et, (int)rank, ImmutableArray<TypeHandle>.Empty);
                }

            case CorElementType.Ptr:
            case CorElementType.Byref:
                {
                    TypeHandle referent = ApproxTypeArg(rts, canonTh, rts.GetTypeParam(th));
                    return rts.GetConstructedType(referent, et, 0, ImmutableArray<TypeHandle>.Empty);
                }

            case CorElementType.Class:
            case CorElementType.ValueType:
                return InstantiationApprox(rts, canonTh, th);

            default:
                return rts.GetPrimitiveType(et);
        }
    }

    // Arg context: Class collapses to __Canon (its children skipped); ValueType is recursively
    // approximated; Ptr preserves shape; obj-ref primitives (Class/Object/String/SzArray/Array)
    // collapse to __Canon; primitives map to their primitive TypeHandle.
    private TypeHandle ApproxTypeArg(IRuntimeTypeSystem rts, TypeHandle canonTh, TypeHandle th)
    {
        CorElementType et = GetElementType(rts, th);
        switch (et)
        {
            case CorElementType.Ptr:
                {
                    TypeHandle referent = ApproxTypeArg(rts, canonTh, rts.GetTypeParam(th));
                    return rts.GetConstructedType(referent, et, 0, ImmutableArray<TypeHandle>.Empty);
                }

            case CorElementType.Class:
                return canonTh;

            case CorElementType.ValueType:
                return InstantiationApprox(rts, canonTh, th);

            default:
                if (rts.IsCorElementTypeObjRef(et))
                    return canonTh;
                return rts.GetPrimitiveType(et);
        }
    }

    // Build a canonicalized instantiation of a Class / ValueType: look up the open generic
    // typeDef in its declaring assembly (mirrors TypeDataWalk.ReadLoadedInstantiation), then
    // construct a closed instantiation with each type-arg approximated via ApproxTypeArg.
    // Non-generic types return early — the production walker takes the
    // <c>nTypeArgs == 0</c> branch and returns the typeDef directly, which equals the type's
    // own MT for a non-generic type.
    private TypeHandle InstantiationApprox(IRuntimeTypeSystem rts, TypeHandle canonTh, TypeHandle th)
    {
        // Mirror DacDbiImpl.FillClassTypeInfo: upcast continuation-without-metadata types to
        // their parent before resolving module / typeDef token. Otherwise the synthesized token
        // won't appear in the module's TypeDefToMethodTable lookup map and the typeDef lookup
        // below would fail.
        if (rts.IsContinuationWithoutMetadata(th))
        {
            TargetPointer parentMT = rts.GetParentMethodTable(th);
            if (parentMT != TargetPointer.Null)
                th = rts.GetTypeHandle(parentMT);
        }

        ReadOnlySpan<TypeHandle> inst = rts.GetInstantiation(th);
        if (inst.Length == 0)
            return th;

        // Mirror DacDbiImpl.FillClassTypeInfo: resolve vmAssembly + metadata token, then look
        // up the open generic typeDef MT via the same helper TypeDataWalk uses.
        TargetPointer modulePtr = rts.GetModule(th);
        ILoader loader = Target.Contracts.Loader;
        Contracts.ModuleHandle moduleHandle = loader.GetModuleHandleFromModulePtr(modulePtr);
        ulong vmAssembly = loader.GetAssembly(moduleHandle).Value;
        uint metadataToken = rts.GetTypeDefToken(th);

        TypeHandle typeDef = DbiHelpers.TryLookupTypeDefOrRefInAssembly(Target, rts, vmAssembly, metadataToken);
        if (typeDef.IsNull)
            return default;

        ImmutableArray<TypeHandle>.Builder builder = ImmutableArray.CreateBuilder<TypeHandle>(inst.Length);
        for (int i = 0; i < inst.Length; i++)
        {
            TypeHandle approxArg = ApproxTypeArg(rts, canonTh, inst[i]);
            if (approxArg.IsNull)
                return default;
            builder.Add(approxArg);
        }

        return rts.GetConstructedType(typeDef, CorElementType.GenericInst, 0, builder.MoveToImmutable());
    }

    // Same element-type mapping DacDbiImpl uses (System.String -> E_T_STRING, System.Object ->
    // E_T_OBJECT, else GetSignatureCorElementType).
    private static CorElementType GetElementType(IRuntimeTypeSystem rts, TypeHandle th)
    {
        if (th.IsNull)
            return CorElementType.Void;
        if (rts.IsString(th))
            return CorElementType.String;
        if (rts.IsObject(th))
            return CorElementType.Object;
        return rts.GetSignatureCorElementType(th);
    }
}
