// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.DumpTests;

/// <summary>
/// Dump-based integration tests for <see cref="ICallingConvention.ComputeCallSiteLayout"/>.
/// Validates the calling-convention layout produced for a range of signature
/// shapes (varargs, byref, by-value structs, ABI-byref structs, managed
/// objects, ByRefLike) against real method metadata in a dump.
///
/// Scoped to Windows x64. Other platforms are skipped via attributes; the
/// debuggee is also marked <c>WindowsOnly</c>.
/// </summary>
public class CallSiteLayoutDumpTests : DumpTestBase
{
    protected override string DebuggeeName => "CallSiteLayout";
    protected override string DumpType => "full";

    private static readonly string[] s_chainMethods =
    [
        "M_Varargs",
        "M_RefInt",
        "M_OutObject",
        "M_RefGuid",
        "M_SmallStructWithRef",
        "M_TwoInts",
        "M_Guid",
        "M_KvpStringString",
        "M_String",
        "M_Object",
        "M_IntArray",
        "M_SpanInt",
        "M_TinyRefStruct",
        "M_OneByteStruct",
        "M_TwelveByteValueTuple",
        "M_DecimalArg",
        "M_ManyInts",
        "M_MixedIntDouble",
        "M_KvpStringInt",
        "M_KvpIntKvpIntInt",
        "M_EnumByte",
        "M_InstanceIntInt",
    ];

    /// <summary>
    /// Walks the FailFast thread once and resolves every chain method's MethodDescHandle.
    /// </summary>
    private Dictionary<string, MethodDescHandle> CollectChainMethods()
    {
        ThreadData thread = DumpTestHelpers.FindThreadWithMethod(Target, "M_TinyRefStruct");
        IStackWalk stackWalk = Target.Contracts.StackWalk;
        IRuntimeTypeSystem rts = Target.Contracts.RuntimeTypeSystem;

        HashSet<string> wanted = new(s_chainMethods);
        Dictionary<string, MethodDescHandle> result = new();

        foreach (IStackDataFrameHandle frame in stackWalk.CreateStackWalk(thread))
        {
            TargetPointer mdPtr = stackWalk.GetMethodDescPtr(frame);
            if (mdPtr == TargetPointer.Null)
                continue;
            MethodDescHandle md = rts.GetMethodDescHandle(mdPtr);
            string? name = DumpTestHelpers.GetMethodName(Target, md);
            if (name is not null && wanted.Contains(name) && !result.ContainsKey(name))
                result[name] = md;
        }

        return result;
    }

    private CallSiteLayout LayoutFor(string methodName)
    {
        Dictionary<string, MethodDescHandle> methods = CollectChainMethods();
        Assert.True(methods.TryGetValue(methodName, out MethodDescHandle md),
            $"'{methodName}' frame not found on the FailFast thread");
        return Target.Contracts.CallingConvention.ComputeCallSiteLayout(md);
    }

    // ===== varargs =====

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnOS(IncludeOnly = "windows", Reason = "Debuggee is Windows-only")]
    [SkipOnArch(IncludeOnly = "x64", Reason = "Layout asserts use Win-x64 ABI specifics")]
    public void Varargs_HasVarArgCookieAndFixedArg(TestConfiguration config)
    {
        InitializeDumpTest(config);
        CallSiteLayout layout = LayoutFor("M_Varargs");

        Assert.NotNull(layout.VarArgCookieOffset);
        // Only the fixed `int fixedArg` is in Arguments; vararg slots are not enumerated here.
        Assert.Single(layout.Arguments);
        ArgLayout fixedArg = layout.Arguments[0];
        Assert.False(fixedArg.IsPassedByRef);
        Assert.Null(fixedArg.ValueTypeHandle);
    }

    // ===== byref =====

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnOS(IncludeOnly = "windows", Reason = "Debuggee is Windows-only")]
    [SkipOnArch(IncludeOnly = "x64", Reason = "Layout asserts use Win-x64 ABI specifics")]
    public void RefInt_IsByRefNoValueTypeHandle(TestConfiguration config)
    {
        InitializeDumpTest(config);
        CallSiteLayout layout = LayoutFor("M_RefInt");

        Assert.Null(layout.VarArgCookieOffset);
        Assert.Single(layout.Arguments);
        ArgLayout arg = layout.Arguments[0];
        Assert.True(arg.IsPassedByRef);
        Assert.Null(arg.ValueTypeHandle);
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnOS(IncludeOnly = "windows", Reason = "Debuggee is Windows-only")]
    [SkipOnArch(IncludeOnly = "x64", Reason = "Layout asserts use Win-x64 ABI specifics")]
    public void OutObject_IsByRefNoValueTypeHandle(TestConfiguration config)
    {
        InitializeDumpTest(config);
        CallSiteLayout layout = LayoutFor("M_OutObject");

        Assert.Single(layout.Arguments);
        ArgLayout arg = layout.Arguments[0];
        Assert.True(arg.IsPassedByRef);
        Assert.Null(arg.ValueTypeHandle);
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnOS(IncludeOnly = "windows", Reason = "Debuggee is Windows-only")]
    [SkipOnArch(IncludeOnly = "x64", Reason = "Layout asserts use Win-x64 ABI specifics")]
    public void RefGuid_IsByRefNoValueTypeHandle(TestConfiguration config)
    {
        InitializeDumpTest(config);
        CallSiteLayout layout = LayoutFor("M_RefGuid");

        Assert.Single(layout.Arguments);
        ArgLayout arg = layout.Arguments[0];
        Assert.True(arg.IsPassedByRef);
        Assert.Null(arg.ValueTypeHandle);
    }

    // ===== structs (by-value) =====

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnOS(IncludeOnly = "windows", Reason = "Debuggee is Windows-only")]
    [SkipOnArch(IncludeOnly = "x64", Reason = "Layout asserts use Win-x64 ABI specifics")]
    public void SmallStructWithRef_PopulatesValueTypeHandle(TestConfiguration config)
    {
        InitializeDumpTest(config);
        CallSiteLayout layout = LayoutFor("M_SmallStructWithRef");

        Assert.Single(layout.Arguments);
        ArgLayout arg = layout.Arguments[0];
        Assert.False(arg.IsPassedByRef);
        Assert.NotNull(arg.ValueTypeHandle);
        Assert.Equal("StructWithRef", DumpTestHelpers.GetTypeName(Target, arg.ValueTypeHandle.Value));
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnOS(IncludeOnly = "windows", Reason = "Debuggee is Windows-only")]
    [SkipOnArch(IncludeOnly = "x64", Reason = "Layout asserts use Win-x64 ABI specifics")]
    public void TwoInts_PopulatesValueTypeHandle(TestConfiguration config)
    {
        InitializeDumpTest(config);
        CallSiteLayout layout = LayoutFor("M_TwoInts");

        Assert.Single(layout.Arguments);
        ArgLayout arg = layout.Arguments[0];
        Assert.False(arg.IsPassedByRef);
        Assert.NotNull(arg.ValueTypeHandle);
        Assert.Equal("TwoInts", DumpTestHelpers.GetTypeName(Target, arg.ValueTypeHandle.Value));
    }

    // ===== struct > 8 bytes -> ABI byref =====

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnOS(IncludeOnly = "windows", Reason = "Debuggee is Windows-only")]
    [SkipOnArch(IncludeOnly = "x64", Reason = "Layout asserts use Win-x64 ABI specifics")]
    public void Guid_AbiByRefNoValueTypeHandle(TestConfiguration config)
    {
        InitializeDumpTest(config);
        CallSiteLayout layout = LayoutFor("M_Guid");

        Assert.Single(layout.Arguments);
        ArgLayout arg = layout.Arguments[0];
        // Guid is 16 bytes -> implicit-byref on Win-x64.
        Assert.True(arg.IsPassedByRef);
        Assert.Null(arg.ValueTypeHandle);
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnOS(IncludeOnly = "windows", Reason = "Debuggee is Windows-only")]
    [SkipOnArch(IncludeOnly = "x64", Reason = "Layout asserts use Win-x64 ABI specifics")]
    public void KvpStringString_AbiByRefNoValueTypeHandle(TestConfiguration config)
    {
        InitializeDumpTest(config);
        CallSiteLayout layout = LayoutFor("M_KvpStringString");

        Assert.Single(layout.Arguments);
        ArgLayout arg = layout.Arguments[0];
        Assert.True(arg.IsPassedByRef);
        Assert.Null(arg.ValueTypeHandle);
    }

    // ===== managed objects =====

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnOS(IncludeOnly = "windows", Reason = "Debuggee is Windows-only")]
    [SkipOnArch(IncludeOnly = "x64", Reason = "Layout asserts use Win-x64 ABI specifics")]
    public void StringArg_NoValueTypeHandle(TestConfiguration config)
    {
        InitializeDumpTest(config);
        CallSiteLayout layout = LayoutFor("M_String");

        Assert.Single(layout.Arguments);
        ArgLayout arg = layout.Arguments[0];
        Assert.False(arg.IsPassedByRef);
        Assert.Null(arg.ValueTypeHandle);
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnOS(IncludeOnly = "windows", Reason = "Debuggee is Windows-only")]
    [SkipOnArch(IncludeOnly = "x64", Reason = "Layout asserts use Win-x64 ABI specifics")]
    public void ObjectArg_NoValueTypeHandle(TestConfiguration config)
    {
        InitializeDumpTest(config);
        CallSiteLayout layout = LayoutFor("M_Object");

        Assert.Single(layout.Arguments);
        ArgLayout arg = layout.Arguments[0];
        Assert.False(arg.IsPassedByRef);
        Assert.Null(arg.ValueTypeHandle);
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnOS(IncludeOnly = "windows", Reason = "Debuggee is Windows-only")]
    [SkipOnArch(IncludeOnly = "x64", Reason = "Layout asserts use Win-x64 ABI specifics")]
    public void IntArray_NoValueTypeHandle(TestConfiguration config)
    {
        InitializeDumpTest(config);
        CallSiteLayout layout = LayoutFor("M_IntArray");

        Assert.Single(layout.Arguments);
        ArgLayout arg = layout.Arguments[0];
        Assert.False(arg.IsPassedByRef);
        Assert.Null(arg.ValueTypeHandle);
    }

    // ===== ByRefLike =====

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnOS(IncludeOnly = "windows", Reason = "Debuggee is Windows-only")]
    [SkipOnArch(IncludeOnly = "x64", Reason = "Layout asserts use Win-x64 ABI specifics")]
    public void SpanInt_AbiByRefNoValueTypeHandle(TestConfiguration config)
    {
        InitializeDumpTest(config);
        CallSiteLayout layout = LayoutFor("M_SpanInt");

        Assert.Single(layout.Arguments);
        ArgLayout arg = layout.Arguments[0];
        // Span<int> is 16 bytes -> implicit-byref on Win-x64. Rule 1 (IsByRef)
        // nulls ValueTypeHandle before the ByRefLike check has a chance to fire.
        Assert.True(arg.IsPassedByRef);
        Assert.Null(arg.ValueTypeHandle);
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnOS(IncludeOnly = "windows", Reason = "Debuggee is Windows-only")]
    [SkipOnArch(IncludeOnly = "x64", Reason = "Layout asserts use Win-x64 ABI specifics")]
    public void TinyRefStruct_ByRefLikeGuardNullsValueTypeHandle(TestConfiguration config)
    {
        InitializeDumpTest(config);
        CallSiteLayout layout = LayoutFor("M_TinyRefStruct");

        Assert.Single(layout.Arguments);
        ArgLayout arg = layout.Arguments[0];
        // 8-byte ref struct -> passed by value, but ByRefLike guard nulls
        // ValueTypeHandle because GCDesc walk alone can't report its ref field.
        Assert.False(arg.IsPassedByRef);
        Assert.Null(arg.ValueTypeHandle);
    }

    // ===== size-rule matrix =====

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnOS(IncludeOnly = "windows", Reason = "Debuggee is Windows-only")]
    [SkipOnArch(IncludeOnly = "x64", Reason = "Layout asserts use Win-x64 ABI specifics")]
    public void OneByteStruct_EnregisteredByValue(TestConfiguration config)
    {
        InitializeDumpTest(config);
        CallSiteLayout layout = LayoutFor("M_OneByteStruct");

        Assert.Single(layout.Arguments);
        ArgLayout arg = layout.Arguments[0];
        // 1-byte struct -> enregistered by value on Win-x64.
        Assert.False(arg.IsPassedByRef);
        Assert.NotNull(arg.ValueTypeHandle);
        Assert.Equal("OneByteStruct", DumpTestHelpers.GetTypeName(Target, arg.ValueTypeHandle.Value));
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnOS(IncludeOnly = "windows", Reason = "Debuggee is Windows-only")]
    [SkipOnArch(IncludeOnly = "x64", Reason = "Layout asserts use Win-x64 ABI specifics")]
    public void TwelveByteValueTuple_AbiByRefNoValueTypeHandle(TestConfiguration config)
    {
        InitializeDumpTest(config);
        CallSiteLayout layout = LayoutFor("M_TwelveByteValueTuple");

        Assert.Single(layout.Arguments);
        ArgLayout arg = layout.Arguments[0];
        // ValueTuple<int,int,int> is 12 bytes -- not a power of two,
        // so Win-x64 passes it by implicit reference.
        Assert.True(arg.IsPassedByRef);
        Assert.Null(arg.ValueTypeHandle);
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnOS(IncludeOnly = "windows", Reason = "Debuggee is Windows-only")]
    [SkipOnArch(IncludeOnly = "x64", Reason = "Layout asserts use Win-x64 ABI specifics")]
    public void DecimalArg_AbiByRefNoValueTypeHandle(TestConfiguration config)
    {
        InitializeDumpTest(config);
        CallSiteLayout layout = LayoutFor("M_DecimalArg");

        Assert.Single(layout.Arguments);
        ArgLayout arg = layout.Arguments[0];
        // decimal is 16 bytes -> ABI byref on Win-x64.
        Assert.True(arg.IsPassedByRef);
        Assert.Null(arg.ValueTypeHandle);
    }

    // ===== register / stack slot mechanics =====

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnOS(IncludeOnly = "windows", Reason = "Debuggee is Windows-only")]
    [SkipOnArch(IncludeOnly = "x64", Reason = "Layout asserts use Win-x64 ABI specifics")]
    public void ManyInts_SpillsLastArgsToStack(TestConfiguration config)
    {
        InitializeDumpTest(config);
        CallSiteLayout layout = LayoutFor("M_ManyInts");

        Assert.Equal(6, layout.Arguments.Count);
        // Win-x64 has 4 argument registers (RCX/RDX/R8/R9); the 5th and 6th args
        // spill onto the stack. Slot offsets are monotonically increasing.
        int prevOffset = int.MinValue;
        for (int i = 0; i < layout.Arguments.Count; i++)
        {
            ArgLayout arg = layout.Arguments[i];
            Assert.False(arg.IsPassedByRef);
            Assert.Null(arg.ValueTypeHandle);
            Assert.Single(arg.Slots);
            Assert.True(arg.Slots[0].Offset > prevOffset,
                $"Arg {i} offset {arg.Slots[0].Offset} not greater than previous {prevOffset}");
            prevOffset = arg.Slots[0].Offset;
        }
        // The last two slot offsets must be at least 8 bytes apart from the
        // 4th -- confirming they live past the 4-register window.
        Assert.True(layout.Arguments[4].Slots[0].Offset >= layout.Arguments[3].Slots[0].Offset + 8);
        Assert.True(layout.Arguments[5].Slots[0].Offset >= layout.Arguments[4].Slots[0].Offset + 8);
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnOS(IncludeOnly = "windows", Reason = "Debuggee is Windows-only")]
    [SkipOnArch(IncludeOnly = "x64", Reason = "Layout asserts use Win-x64 ABI specifics")]
    public void MixedIntDouble_FloatArgsUseFpRegisterSlots(TestConfiguration config)
    {
        InitializeDumpTest(config);
        CallSiteLayout layout = LayoutFor("M_MixedIntDouble");

        Assert.Equal(4, layout.Arguments.Count);
        // (int, double, int, double): the doubles should surface as R8 slots
        // (XMM register lanes), the ints as integer-typed slots.
        Assert.Equal(CorElementType.I4, layout.Arguments[0].Slots[0].ElementType);
        Assert.Equal(CorElementType.R8, layout.Arguments[1].Slots[0].ElementType);
        Assert.Equal(CorElementType.I4, layout.Arguments[2].Slots[0].ElementType);
        Assert.Equal(CorElementType.R8, layout.Arguments[3].Slots[0].ElementType);
        foreach (ArgLayout arg in layout.Arguments)
        {
            Assert.False(arg.IsPassedByRef);
            Assert.Null(arg.ValueTypeHandle);
        }
    }

    // ===== generics: heterogeneous + nested =====

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnOS(IncludeOnly = "windows", Reason = "Debuggee is Windows-only")]
    [SkipOnArch(IncludeOnly = "x64", Reason = "Layout asserts use Win-x64 ABI specifics")]
    public void KvpStringInt_AbiByRefNoValueTypeHandle(TestConfiguration config)
    {
        InitializeDumpTest(config);
        CallSiteLayout layout = LayoutFor("M_KvpStringInt");

        Assert.Single(layout.Arguments);
        ArgLayout arg = layout.Arguments[0];
        // KeyValuePair<string,int> is 16 bytes on x64 (ref + 4 bytes padded to 8).
        // The fix to GetGenericInstantiation must resolve both the ref-typed and
        // primitive-typed type-args to TypeHandles so the instantiated MT is found.
        Assert.True(arg.IsPassedByRef);
        Assert.Null(arg.ValueTypeHandle);
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnOS(IncludeOnly = "windows", Reason = "Debuggee is Windows-only")]
    [SkipOnArch(IncludeOnly = "x64", Reason = "Layout asserts use Win-x64 ABI specifics")]
    public void KvpIntKvpIntInt_NestedGenericResolvesViaRecursion(TestConfiguration config)
    {
        InitializeDumpTest(config);
        CallSiteLayout layout = LayoutFor("M_KvpIntKvpIntInt");

        Assert.Single(layout.Arguments);
        ArgLayout arg = layout.Arguments[0];
        // KeyValuePair<int, KeyValuePair<int,int>> is 12 bytes (int + 8-byte KVP<int,int>).
        // Both instantiations are inline GENERICINST blobs in the method signature, so
        // SRM recurses through ArgTypeInfoSignatureProvider.GetGenericInstantiation at
        // every level rather than dispatching to GetTypeFromSpecification. The inner
        // KVP<int,int> resolves to an 8-byte loaded MT, supplying a real TypeHandle as
        // the second type arg of the outer instantiation; the outer then resolves to a
        // loaded 12-byte MT.
        //
        // Discriminator: if inner resolution had failed (UnresolvedValueType, no
        // TypeHandle), the outer would also fall back to UnresolvedValueType (size 8),
        // and 8 is pow2 / <=8 so IsPassedByRef would be FALSE. Asserting IsPassedByRef
        // == true proves the recursive GetGenericInstantiation chain succeeded all the
        // way through the nested instantiation.
        Assert.True(arg.IsPassedByRef);
        Assert.Null(arg.ValueTypeHandle);
    }

    // ===== enum collapse =====

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnOS(IncludeOnly = "windows", Reason = "Debuggee is Windows-only")]
    [SkipOnArch(IncludeOnly = "x64", Reason = "Layout asserts use Win-x64 ABI specifics")]
    public void EnumByte_TreatedAsValueType(TestConfiguration config)
    {
        InitializeDumpTest(config);
        CallSiteLayout layout = LayoutFor("M_EnumByte");

        Assert.Single(layout.Arguments);
        ArgLayout arg = layout.Arguments[0];
        // Native MetaSig::NextArgNormalized collapses enums to their underlying
        // primitive (here U1). The cDAC iterator currently keeps the enum's
        // MT-level classification (ValueType) and attaches its TypeHandle.
        // That is *safe* for GC scanning (a byte enum has no embedded refs,
        // so the GCDesc walk yields zero refs) but diverges from native.
        // Tracking parity with NextArgNormalized's enum collapse is left as a
        // TODO; this test pins the current behavior so the divergence is
        // visible if/when the iterator is updated.
        Assert.False(arg.IsPassedByRef);
        Assert.NotNull(arg.ValueTypeHandle);
        Assert.Equal("SmallByteEnum", DumpTestHelpers.GetTypeName(Target, arg.ValueTypeHandle.Value));
    }

    // ===== instance method =====

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnOS(IncludeOnly = "windows", Reason = "Debuggee is Windows-only")]
    [SkipOnArch(IncludeOnly = "x64", Reason = "Layout asserts use Win-x64 ABI specifics")]
    public void InstanceIntInt_PopulatesThisOffset(TestConfiguration config)
    {
        InitializeDumpTest(config);
        CallSiteLayout layout = LayoutFor("M_InstanceIntInt");

        // Instance method on a reference type: ThisOffset is populated and
        // IsValueTypeThis is false. The two fixed args follow `this`.
        Assert.NotNull(layout.ThisOffset);
        Assert.False(layout.IsValueTypeThis);
        Assert.Equal(2, layout.Arguments.Count);
        foreach (ArgLayout arg in layout.Arguments)
        {
            Assert.False(arg.IsPassedByRef);
            Assert.Null(arg.ValueTypeHandle);
            Assert.Equal(CorElementType.I4, arg.Slots[0].ElementType);
        }
        // `this` occupies the first register slot; the two ints come after.
        Assert.True(layout.Arguments[0].Slots[0].Offset > layout.ThisOffset.Value);
    }
}


