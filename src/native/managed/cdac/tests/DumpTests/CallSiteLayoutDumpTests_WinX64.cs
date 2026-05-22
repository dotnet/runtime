// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.DataContractReader.Contracts;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.DumpTests;

public class CallSiteLayoutDumpTests_WinX64 : CallSiteLayoutDumpTestsBase
{
    // ===== Category A: register-bank fill / spill (no GC refs) =====

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnOS(IncludeOnly = "windows", Reason = "Debuggee is Windows-only")]
    [SkipOnArch(IncludeOnly = "x64", Reason = "Layout asserts use Win-x64 ABI specifics")]
    [SkipOnVersion("net10.0", "CodeVersions descriptor format incompatible with current cDAC reader on net10.0 dumps")]
    public void Empty_NoArguments(TestConfiguration config)
    {
        InitializeDumpTest(config);
        CallSiteLayout layout = LayoutFor("M_Empty");
        Assert.Null(layout.ThisOffset);
        Assert.Null(layout.VarArgCookieOffset);
        Assert.Empty(layout.Arguments);
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnOS(IncludeOnly = "windows", Reason = "Debuggee is Windows-only")]
    [SkipOnArch(IncludeOnly = "x64", Reason = "Layout asserts use Win-x64 ABI specifics")]
    [SkipOnVersion("net10.0", "CodeVersions descriptor format incompatible with current cDAC reader on net10.0 dumps")]
    public void IntSix_AllFitsInRegisterSlots(TestConfiguration config)
    {
        InitializeDumpTest(config);
        CallSiteLayout layout = LayoutFor("M_Int_Six");
        Assert.Equal(6, layout.Arguments.Count);
        int prev = int.MinValue;
        foreach (ArgLayout a in layout.Arguments)
        {
            Assert.False(a.IsPassedByRef);
            Assert.Null(a.ValueTypeHandle);
            Assert.Single(a.Slots);
            Assert.Equal(CorElementType.I4, a.Slots[0].ElementType);
            Assert.True(a.Slots[0].Offset > prev);
            prev = a.Slots[0].Offset;
        }
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnOS(IncludeOnly = "windows", Reason = "Debuggee is Windows-only")]
    [SkipOnArch(IncludeOnly = "x64", Reason = "Layout asserts use Win-x64 ABI specifics")]
    [SkipOnVersion("net10.0", "CodeVersions descriptor format incompatible with current cDAC reader on net10.0 dumps")]
    public void IntNine_SpillsArgsBeyondRegisters(TestConfiguration config)
    {
        InitializeDumpTest(config);
        CallSiteLayout layout = LayoutFor("M_Int_Nine");
        Assert.Equal(9, layout.Arguments.Count);
        // Win-x64 has 4 argument registers; args 5-9 spill to stack at +8 strides.
        Assert.True(layout.Arguments[4].Slots[0].Offset >= layout.Arguments[3].Slots[0].Offset + 8);
        Assert.True(layout.Arguments[8].Slots[0].Offset >= layout.Arguments[4].Slots[0].Offset + 32);
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnOS(IncludeOnly = "windows", Reason = "Debuggee is Windows-only")]
    [SkipOnArch(IncludeOnly = "x64", Reason = "Layout asserts use Win-x64 ABI specifics")]
    [SkipOnVersion("net10.0", "CodeVersions descriptor format incompatible with current cDAC reader on net10.0 dumps")]
    public void DoubleFour_FloatsUseFpSlots(TestConfiguration config)
    {
        InitializeDumpTest(config);
        CallSiteLayout layout = LayoutFor("M_Double_Four");
        Assert.Equal(4, layout.Arguments.Count);
        foreach (ArgLayout a in layout.Arguments)
        {
            Assert.False(a.IsPassedByRef);
            Assert.Null(a.ValueTypeHandle);
            Assert.Equal(CorElementType.R8, a.Slots[0].ElementType);
        }
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnOS(IncludeOnly = "windows", Reason = "Debuggee is Windows-only")]
    [SkipOnArch(IncludeOnly = "x64", Reason = "Layout asserts use Win-x64 ABI specifics")]
    [SkipOnVersion("net10.0", "CodeVersions descriptor format incompatible with current cDAC reader on net10.0 dumps")]
    public void MixedID_AlternatingIntDouble(TestConfiguration config)
    {
        InitializeDumpTest(config);
        CallSiteLayout layout = LayoutFor("M_MixedID");
        Assert.Equal(6, layout.Arguments.Count);
        Assert.Equal(CorElementType.I4, layout.Arguments[0].Slots[0].ElementType);
        Assert.Equal(CorElementType.R8, layout.Arguments[1].Slots[0].ElementType);
        Assert.Equal(CorElementType.I4, layout.Arguments[2].Slots[0].ElementType);
        Assert.Equal(CorElementType.R8, layout.Arguments[3].Slots[0].ElementType);
    }

    // ===== Category B: reference-typed args =====

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnOS(IncludeOnly = "windows", Reason = "Debuggee is Windows-only")]
    [SkipOnArch(IncludeOnly = "x64", Reason = "Layout asserts use Win-x64 ABI specifics")]
    [SkipOnVersion("net10.0", "CodeVersions descriptor format incompatible with current cDAC reader on net10.0 dumps")]
    public void RefArgs_String_NoValueTypeHandle(TestConfiguration config)
    {
        InitializeDumpTest(config);
        AssertSingleManagedRef("M_RefArgs_String");
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnOS(IncludeOnly = "windows", Reason = "Debuggee is Windows-only")]
    [SkipOnArch(IncludeOnly = "x64", Reason = "Layout asserts use Win-x64 ABI specifics")]
    [SkipOnVersion("net10.0", "CodeVersions descriptor format incompatible with current cDAC reader on net10.0 dumps")]
    public void RefArgs_Object_NoValueTypeHandle(TestConfiguration config)
    {
        InitializeDumpTest(config);
        AssertSingleManagedRef("M_RefArgs_Object");
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnOS(IncludeOnly = "windows", Reason = "Debuggee is Windows-only")]
    [SkipOnArch(IncludeOnly = "x64", Reason = "Layout asserts use Win-x64 ABI specifics")]
    [SkipOnVersion("net10.0", "CodeVersions descriptor format incompatible with current cDAC reader on net10.0 dumps")]
    public void RefArgs_SzArray_NoValueTypeHandle(TestConfiguration config)
    {
        InitializeDumpTest(config);
        AssertSingleManagedRef("M_RefArgs_SzArray");
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnOS(IncludeOnly = "windows", Reason = "Debuggee is Windows-only")]
    [SkipOnArch(IncludeOnly = "x64", Reason = "Layout asserts use Win-x64 ABI specifics")]
    [SkipOnVersion("net10.0", "CodeVersions descriptor format incompatible with current cDAC reader on net10.0 dumps")]
    public void RefArgs_MdArray_NoValueTypeHandle(TestConfiguration config)
    {
        InitializeDumpTest(config);
        AssertSingleManagedRef("M_RefArgs_MdArray");
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnOS(IncludeOnly = "windows", Reason = "Debuggee is Windows-only")]
    [SkipOnArch(IncludeOnly = "x64", Reason = "Layout asserts use Win-x64 ABI specifics")]
    [SkipOnVersion("net10.0", "CodeVersions descriptor format incompatible with current cDAC reader on net10.0 dumps")]
    public void RefArgs_RefInt_IsByRef(TestConfiguration config)
    {
        InitializeDumpTest(config);
        AssertSingleByRef("M_RefArgs_RefInt");
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnOS(IncludeOnly = "windows", Reason = "Debuggee is Windows-only")]
    [SkipOnArch(IncludeOnly = "x64", Reason = "Layout asserts use Win-x64 ABI specifics")]
    [SkipOnVersion("net10.0", "CodeVersions descriptor format incompatible with current cDAC reader on net10.0 dumps")]
    public void RefArgs_OutObject_IsByRef(TestConfiguration config)
    {
        InitializeDumpTest(config);
        AssertSingleByRef("M_RefArgs_OutObject");
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnOS(IncludeOnly = "windows", Reason = "Debuggee is Windows-only")]
    [SkipOnArch(IncludeOnly = "x64", Reason = "Layout asserts use Win-x64 ABI specifics")]
    [SkipOnVersion("net10.0", "CodeVersions descriptor format incompatible with current cDAC reader on net10.0 dumps")]
    public void RefArgs_RefStruct_IsByRef(TestConfiguration config)
    {
        InitializeDumpTest(config);
        AssertSingleByRef("M_RefArgs_RefStruct");
    }

    // ===== Category C: small by-value structs =====

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnOS(IncludeOnly = "windows", Reason = "Debuggee is Windows-only")]
    [SkipOnArch(IncludeOnly = "x64", Reason = "Layout asserts use Win-x64 ABI specifics")]
    [SkipOnVersion("net10.0", "CodeVersions descriptor format incompatible with current cDAC reader on net10.0 dumps")]
    public void VT_Byte_EnregisteredByValue(TestConfiguration config)
    {
        InitializeDumpTest(config);
        AssertSingleByValueVT("M_VT_Byte", "ByteStruct");
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnOS(IncludeOnly = "windows", Reason = "Debuggee is Windows-only")]
    [SkipOnArch(IncludeOnly = "x64", Reason = "Layout asserts use Win-x64 ABI specifics")]
    [SkipOnVersion("net10.0", "CodeVersions descriptor format incompatible with current cDAC reader on net10.0 dumps")]
    public void VT_Short_EnregisteredByValue(TestConfiguration config)
    {
        InitializeDumpTest(config);
        AssertSingleByValueVT("M_VT_Short", "ShortStruct");
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnOS(IncludeOnly = "windows", Reason = "Debuggee is Windows-only")]
    [SkipOnArch(IncludeOnly = "x64", Reason = "Layout asserts use Win-x64 ABI specifics")]
    [SkipOnVersion("net10.0", "CodeVersions descriptor format incompatible with current cDAC reader on net10.0 dumps")]
    public void VT_Int_EnregisteredByValue(TestConfiguration config)
    {
        InitializeDumpTest(config);
        AssertSingleByValueVT("M_VT_Int", "IntStruct");
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnOS(IncludeOnly = "windows", Reason = "Debuggee is Windows-only")]
    [SkipOnArch(IncludeOnly = "x64", Reason = "Layout asserts use Win-x64 ABI specifics")]
    [SkipOnVersion("net10.0", "CodeVersions descriptor format incompatible with current cDAC reader on net10.0 dumps")]
    public void VT_TwoInts_EnregisteredByValue(TestConfiguration config)
    {
        InitializeDumpTest(config);
        AssertSingleByValueVT("M_VT_TwoInts", "TwoInts");
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnOS(IncludeOnly = "windows", Reason = "Debuggee is Windows-only")]
    [SkipOnArch(IncludeOnly = "x64", Reason = "Layout asserts use Win-x64 ABI specifics")]
    [SkipOnVersion("net10.0", "CodeVersions descriptor format incompatible with current cDAC reader on net10.0 dumps")]
    public void VT_ObjectOnly_PopulatesValueTypeHandle(TestConfiguration config)
    {
        // 8-byte struct containing a single object ref. This is the canonical
        // Win-x64 case where the GCDesc walk is needed at scan time.
        InitializeDumpTest(config);
        AssertSingleByValueVT("M_VT_ObjectOnly", "ObjectStruct");
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnOS(IncludeOnly = "windows", Reason = "Debuggee is Windows-only")]
    [SkipOnArch(IncludeOnly = "x64", Reason = "Layout asserts use Win-x64 ABI specifics")]
    [SkipOnVersion("net10.0", "CodeVersions descriptor format incompatible with current cDAC reader on net10.0 dumps")]
    public void VT_StringOnly_PopulatesValueTypeHandle(TestConfiguration config)
    {
        InitializeDumpTest(config);
        AssertSingleByValueVT("M_VT_StringOnly", "StringStruct");
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnOS(IncludeOnly = "windows", Reason = "Debuggee is Windows-only")]
    [SkipOnArch(IncludeOnly = "x64", Reason = "Layout asserts use Win-x64 ABI specifics")]
    [SkipOnVersion("net10.0", "CodeVersions descriptor format incompatible with current cDAC reader on net10.0 dumps")]
    public void VT_ThreeByte_AbiByRef(TestConfiguration config)
    {
        // 3 bytes -> non-power-of-two -> Win-x64 passes by implicit reference.
        InitializeDumpTest(config);
        AssertSingleByRef("M_VT_ThreeByte");
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnOS(IncludeOnly = "windows", Reason = "Debuggee is Windows-only")]
    [SkipOnArch(IncludeOnly = "x64", Reason = "Layout asserts use Win-x64 ABI specifics")]
    [SkipOnVersion("net10.0", "CodeVersions descriptor format incompatible with current cDAC reader on net10.0 dumps")]
    public void VT_FiveByte_AbiByRef(TestConfiguration config)
    {
        InitializeDumpTest(config);
        AssertSingleByRef("M_VT_FiveByte");
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnOS(IncludeOnly = "windows", Reason = "Debuggee is Windows-only")]
    [SkipOnArch(IncludeOnly = "x64", Reason = "Layout asserts use Win-x64 ABI specifics")]
    [SkipOnVersion("net10.0", "CodeVersions descriptor format incompatible with current cDAC reader on net10.0 dumps")]
    public void VT_Twelve_AbiByRef(TestConfiguration config)
    {
        InitializeDumpTest(config);
        AssertSingleByRef("M_VT_Twelve");
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnOS(IncludeOnly = "windows", Reason = "Debuggee is Windows-only")]
    [SkipOnArch(IncludeOnly = "x64", Reason = "Layout asserts use Win-x64 ABI specifics")]
    [SkipOnVersion("net10.0", "CodeVersions descriptor format incompatible with current cDAC reader on net10.0 dumps")]
    public void VT_Guid_AbiByRef(TestConfiguration config)
    {
        // 16 bytes -> Win-x64 passes by implicit reference.
        InitializeDumpTest(config);
        AssertSingleByRef("M_VT_Guid");
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnOS(IncludeOnly = "windows", Reason = "Debuggee is Windows-only")]
    [SkipOnArch(IncludeOnly = "x64", Reason = "Layout asserts use Win-x64 ABI specifics")]
    [SkipOnVersion("net10.0", "CodeVersions descriptor format incompatible with current cDAC reader on net10.0 dumps")]
    public void VT_Decimal_AbiByRef(TestConfiguration config)
    {
        InitializeDumpTest(config);
        AssertSingleByRef("M_VT_Decimal");
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnOS(IncludeOnly = "windows", Reason = "Debuggee is Windows-only")]
    [SkipOnArch(IncludeOnly = "x64", Reason = "Layout asserts use Win-x64 ABI specifics")]
    [SkipOnVersion("net10.0", "CodeVersions descriptor format incompatible with current cDAC reader on net10.0 dumps")]
    public void VT_KvpStrStr_AbiByRef(TestConfiguration config)
    {
        InitializeDumpTest(config);
        AssertSingleByRef("M_VT_KvpStrStr");
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnOS(IncludeOnly = "windows", Reason = "Debuggee is Windows-only")]
    [SkipOnArch(IncludeOnly = "x64", Reason = "Layout asserts use Win-x64 ABI specifics")]
    [SkipOnVersion("net10.0", "CodeVersions descriptor format incompatible with current cDAC reader on net10.0 dumps")]
    public void VT_KvpStrInt_AbiByRef(TestConfiguration config)
    {
        // 16 bytes (ref + 4 padded to 8). Exercises the nested
        // GetGenericInstantiation path resolving both ref- and primitive-
        // typed type args at one level of nesting.
        InitializeDumpTest(config);
        AssertSingleByRef("M_VT_KvpStrInt");
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnOS(IncludeOnly = "windows", Reason = "Debuggee is Windows-only")]
    [SkipOnArch(IncludeOnly = "x64", Reason = "Layout asserts use Win-x64 ABI specifics")]
    [SkipOnVersion("net10.0", "CodeVersions descriptor format incompatible with current cDAC reader on net10.0 dumps")]
    public void VT_TwoFloats_EnregisteredByValue(TestConfiguration config)
    {
        // 8-byte HFA candidate. Win-x64 does NOT enregister HFAs as scalars;
        // the whole struct travels in one slot by value.
        InitializeDumpTest(config);
        AssertSingleByValueVT("M_VT_TwoFloats", "TwoFloats");
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnOS(IncludeOnly = "windows", Reason = "Debuggee is Windows-only")]
    [SkipOnArch(IncludeOnly = "x64", Reason = "Layout asserts use Win-x64 ABI specifics")]
    [SkipOnVersion("net10.0", "CodeVersions descriptor format incompatible with current cDAC reader on net10.0 dumps")]
    public void VT_TwoDoubles_AbiByRef(TestConfiguration config)
    {
        // 16-byte HFA candidate -> Win-x64 implicit-byref (no HFA enregistration).
        InitializeDumpTest(config);
        AssertSingleByRef("M_VT_TwoDoubles");
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnOS(IncludeOnly = "windows", Reason = "Debuggee is Windows-only")]
    [SkipOnArch(IncludeOnly = "x64", Reason = "Layout asserts use Win-x64 ABI specifics")]
    [SkipOnVersion("net10.0", "CodeVersions descriptor format incompatible with current cDAC reader on net10.0 dumps")]
    public void VT_IntDouble_AbiByRef(TestConfiguration config)
    {
        // 16 bytes (4-byte int + 4 padding + 8-byte double) -> implicit-byref.
        InitializeDumpTest(config);
        AssertSingleByRef("M_VT_IntDouble");
    }

    // ===== Category D: large stack-passed structs =====

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnOS(IncludeOnly = "windows", Reason = "Debuggee is Windows-only")]
    [SkipOnArch(IncludeOnly = "x64", Reason = "Layout asserts use Win-x64 ABI specifics")]
    [SkipOnVersion("net10.0", "CodeVersions descriptor format incompatible with current cDAC reader on net10.0 dumps")]
    public void VT_Big24_AbiByRef(TestConfiguration config)
    {
        InitializeDumpTest(config);
        AssertSingleByRef("M_VT_Big24");
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnOS(IncludeOnly = "windows", Reason = "Debuggee is Windows-only")]
    [SkipOnArch(IncludeOnly = "x64", Reason = "Layout asserts use Win-x64 ABI specifics")]
    [SkipOnVersion("net10.0", "CodeVersions descriptor format incompatible with current cDAC reader on net10.0 dumps")]
    public void VT_Big24WithRef_AbiByRef(TestConfiguration config)
    {
        // SysV would pass this on-stack by value and require a GCDesc walk; on
        // Win-x64 it is implicit-byref so no in-arg GC scanning is needed --
        // the caller's stack temp holds the live ref and is covered by the
        // caller's GC info.
        InitializeDumpTest(config);
        AssertSingleByRef("M_VT_Big24WithRef");
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnOS(IncludeOnly = "windows", Reason = "Debuggee is Windows-only")]
    [SkipOnArch(IncludeOnly = "x64", Reason = "Layout asserts use Win-x64 ABI specifics")]
    [SkipOnVersion("net10.0", "CodeVersions descriptor format incompatible with current cDAC reader on net10.0 dumps")]
    public void VT_Big48WithTwoRefs_AbiByRef(TestConfiguration config)
    {
        InitializeDumpTest(config);
        AssertSingleByRef("M_VT_Big48WithTwoRefs");
    }

    // ===== Category E: ByRefLike =====

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnOS(IncludeOnly = "windows", Reason = "Debuggee is Windows-only")]
    [SkipOnArch(IncludeOnly = "x64", Reason = "Layout asserts use Win-x64 ABI specifics")]
    [SkipOnVersion("net10.0", "CodeVersions descriptor format incompatible with current cDAC reader on net10.0 dumps")]
    public void BRL_SpanInt_AbiByRef(TestConfiguration config)
    {
        // Span<int> is 16 bytes -> Win-x64 implicit-byref. The IsByRef short
        // circuit fires before the ByRefLike dispatch can populate
        // ValueTypeHandle, so VTH must be null here.
        InitializeDumpTest(config);
        AssertSingleByRef("M_BRL_SpanInt");
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnOS(IncludeOnly = "windows", Reason = "Debuggee is Windows-only")]
    [SkipOnArch(IncludeOnly = "x64", Reason = "Layout asserts use Win-x64 ABI specifics")]
    [SkipOnVersion("net10.0", "CodeVersions descriptor format incompatible with current cDAC reader on net10.0 dumps")]
    public void BRL_SpanObject_AbiByRef(TestConfiguration config)
    {
        InitializeDumpTest(config);
        AssertSingleByRef("M_BRL_SpanObject");
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnOS(IncludeOnly = "windows", Reason = "Debuggee is Windows-only")]
    [SkipOnArch(IncludeOnly = "x64", Reason = "Layout asserts use Win-x64 ABI specifics")]
    [SkipOnVersion("net10.0", "CodeVersions descriptor format incompatible with current cDAC reader on net10.0 dumps")]
    public void BRL_SmallRefStruct_ByValueRefersToByRefLike(TestConfiguration config)
    {
        // Pointer-sized ref struct holding a single managed byref. Passed by
        // value on Win-x64 (1 slot, 8 bytes). The producer keeps the
        // ValueTypeHandle so the consumer can dispatch to the ByRefLike walker
        // and emit the inner byref field as an INTERIOR root.
        InitializeDumpTest(config);
        CallSiteLayout layout = LayoutFor("M_BRL_SmallRefStruct");
        Assert.Single(layout.Arguments);
        ArgLayout arg = layout.Arguments[0];
        Assert.False(arg.IsPassedByRef);
        Assert.NotNull(arg.ValueTypeHandle);
        Assert.Equal("SmallRefStruct", DumpTestHelpers.GetTypeName(Target, arg.ValueTypeHandle.Value));
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnOS(IncludeOnly = "windows", Reason = "Debuggee is Windows-only")]
    [SkipOnArch(IncludeOnly = "x64", Reason = "Layout asserts use Win-x64 ABI specifics")]
    [SkipOnVersion("net10.0", "CodeVersions descriptor format incompatible with current cDAC reader on net10.0 dumps")]
    public void BRL_RefStructWithObject_AbiByRef(TestConfiguration config)
    {
        // RefStructWithObject is { object Obj; int Prim; } -> 16 bytes on x64.
        // Implicit-byref on Win-x64; the caller's stack temp holds the live
        // object so cross-frame GC scanning relies on the caller's reporting.
        InitializeDumpTest(config);
        AssertSingleByRef("M_BRL_RefStructWithObject");
    }

    // ===== Category F: special slots =====

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnOS(IncludeOnly = "windows", Reason = "Debuggee is Windows-only")]
    [SkipOnArch(IncludeOnly = "x64", Reason = "Layout asserts use Win-x64 ABI specifics")]
    [SkipOnVersion("net10.0", "CodeVersions descriptor format incompatible with current cDAC reader on net10.0 dumps")]
    public void Special_Instance_PopulatesThisOffset(TestConfiguration config)
    {
        InitializeDumpTest(config);
        CallSiteLayout layout = LayoutFor("M_Special_Instance");
        Assert.NotNull(layout.ThisOffset);
        Assert.False(layout.IsValueTypeThis);
        Assert.Equal(2, layout.Arguments.Count);
        Assert.True(layout.Arguments[0].Slots[0].Offset > layout.ThisOffset!.Value);
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnOS(IncludeOnly = "windows", Reason = "Debuggee is Windows-only")]
    [SkipOnArch(IncludeOnly = "x64", Reason = "Layout asserts use Win-x64 ABI specifics")]
    [SkipOnVersion("net10.0", "CodeVersions descriptor format incompatible with current cDAC reader on net10.0 dumps")]
    public void Special_GenericRef_TypeArgIsManagedRef(TestConfiguration config)
    {
        // Generic method instantiated over `string`. The single fixed arg is a
        // managed object reference -- no VTH, not byref.
        InitializeDumpTest(config);
        AssertSingleManagedRef("M_Special_GenericRef");
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnOS(IncludeOnly = "windows", Reason = "Debuggee is Windows-only")]
    [SkipOnArch(IncludeOnly = "x64", Reason = "Layout asserts use Win-x64 ABI specifics")]
    [SkipOnVersion("net10.0", "CodeVersions descriptor format incompatible with current cDAC reader on net10.0 dumps")]
    public void Special_GenericVal_TypeArgIsPrimitive(TestConfiguration config)
    {
        // Generic method instantiated over `int` -- single primitive arg, no VTH.
        InitializeDumpTest(config);
        CallSiteLayout layout = LayoutFor("M_Special_GenericVal");
        Assert.Single(layout.Arguments);
        ArgLayout arg = layout.Arguments[0];
        Assert.False(arg.IsPassedByRef);
        Assert.Null(arg.ValueTypeHandle);
        Assert.Equal(CorElementType.I4, arg.Slots[0].ElementType);
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnOS(IncludeOnly = "windows", Reason = "Debuggee is Windows-only")]
    [SkipOnArch(IncludeOnly = "x64", Reason = "Layout asserts use Win-x64 ABI specifics")]
    [SkipOnVersion("net10.0", "CodeVersions descriptor format incompatible with current cDAC reader on net10.0 dumps")]
    public void Special_Varargs_HasVarArgCookieAndFixedArg(TestConfiguration config)
    {
        InitializeDumpTest(config);
        CallSiteLayout layout = LayoutFor("M_Special_Varargs");
        Assert.NotNull(layout.VarArgCookieOffset);
        // Only the fixed `int` is enumerated; vararg extras are not in Arguments.
        Assert.Single(layout.Arguments);
        ArgLayout fixedArg = layout.Arguments[0];
        Assert.False(fixedArg.IsPassedByRef);
        Assert.Null(fixedArg.ValueTypeHandle);
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnOS(IncludeOnly = "windows", Reason = "Debuggee is Windows-only")]
    [SkipOnArch(IncludeOnly = "x64", Reason = "Layout asserts use Win-x64 ABI specifics")]
    [SkipOnVersion("net10.0", "CodeVersions descriptor format incompatible with current cDAC reader on net10.0 dumps")]
    public void Special_NoVarargs_NoCookie(TestConfiguration config)
    {
        InitializeDumpTest(config);
        CallSiteLayout layout = LayoutFor("M_Special_NoVarargs");
        Assert.Null(layout.VarArgCookieOffset);
        Assert.Equal(2, layout.Arguments.Count);
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnOS(IncludeOnly = "windows", Reason = "Debuggee is Windows-only")]
    [SkipOnArch(IncludeOnly = "x64", Reason = "Layout asserts use Win-x64 ABI specifics")]
    [SkipOnVersion("net10.0", "CodeVersions descriptor format incompatible with current cDAC reader on net10.0 dumps")]
    public void Special_InstanceGenericClassGenericMethod_PopulatesThisOffset(TestConfiguration config)
    {
        InitializeDumpTest(config);
        CallSiteLayout layout = LayoutFor("M_Special_InstanceGenericClassGenericMethod");
        Assert.NotNull(layout.ThisOffset);
        Assert.False(layout.IsValueTypeThis);
        Assert.Equal(2, layout.Arguments.Count);
    }

    // ===== Category G: vectors =====
    // Vector{256,512} are runtime-supported but the IsSupported gate in the
    // debuggee may bypass the call; the frames are still on the chain because
    // their parents pass Vector{N}.Zero unconditionally.

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnOS(IncludeOnly = "windows", Reason = "Debuggee is Windows-only")]
    [SkipOnArch(IncludeOnly = "x64", Reason = "Layout asserts use Win-x64 ABI specifics")]
    [SkipOnVersion("net10.0", "CodeVersions descriptor format incompatible with current cDAC reader on net10.0 dumps")]
    public void Vec_64_EnregisteredByValue(TestConfiguration config)
    {
        // Vector64<int> is 8 bytes. Win-x64 passes 8-byte structs by value.
        InitializeDumpTest(config);
        CallSiteLayout layout = LayoutFor("M_Vec_64");
        Assert.Single(layout.Arguments);
        Assert.False(layout.Arguments[0].IsPassedByRef);
        Assert.NotNull(layout.Arguments[0].ValueTypeHandle);
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnOS(IncludeOnly = "windows", Reason = "Debuggee is Windows-only")]
    [SkipOnArch(IncludeOnly = "x64", Reason = "Layout asserts use Win-x64 ABI specifics")]
    [SkipOnVersion("net10.0", "CodeVersions descriptor format incompatible with current cDAC reader on net10.0 dumps")]
    public void Vec_128_AbiByRef(TestConfiguration config)
    {
        // Vector128 is 16 bytes -> Win-x64 implicit-byref.
        InitializeDumpTest(config);
        AssertSingleByRef("M_Vec_128");
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnOS(IncludeOnly = "windows", Reason = "Debuggee is Windows-only")]
    [SkipOnArch(IncludeOnly = "x64", Reason = "Layout asserts use Win-x64 ABI specifics")]
    [SkipOnVersion("net10.0", "CodeVersions descriptor format incompatible with current cDAC reader on net10.0 dumps")]
    public void Vec_256_AbiByRef(TestConfiguration config)
    {
        // Vector256 is 32 bytes -> Win-x64 implicit-byref.
        InitializeDumpTest(config);
        AssertSingleByRef("M_Vec_256");
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnOS(IncludeOnly = "windows", Reason = "Debuggee is Windows-only")]
    [SkipOnArch(IncludeOnly = "x64", Reason = "Layout asserts use Win-x64 ABI specifics")]
    [SkipOnVersion("net10.0", "CodeVersions descriptor format incompatible with current cDAC reader on net10.0 dumps")]
    public void Vec_512_AbiByRef(TestConfiguration config)
    {
        // Vector512 is 64 bytes -> Win-x64 implicit-byref.
        InitializeDumpTest(config);
        AssertSingleByRef("M_Vec_512");
    }

    // ===== Category H: composite frames =====

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnOS(IncludeOnly = "windows", Reason = "Debuggee is Windows-only")]
    [SkipOnArch(IncludeOnly = "x64", Reason = "Layout asserts use Win-x64 ABI specifics")]
    [SkipOnVersion("net10.0", "CodeVersions descriptor format incompatible with current cDAC reader on net10.0 dumps")]
    public void Combo_RegBankExhaustion_AllArgsAccounted(TestConfiguration config)
    {
        InitializeDumpTest(config);
        CallSiteLayout layout = LayoutFor("M_Combo_RegBankExhaustion");
        Assert.Equal(15, layout.Arguments.Count);
        // First six pairs are int/double scalars - check the trailing 3 are object/string/array.
        for (int i = 12; i < 15; i++)
        {
            Assert.False(layout.Arguments[i].IsPassedByRef);
            Assert.Null(layout.Arguments[i].ValueTypeHandle);
        }
    }
}
