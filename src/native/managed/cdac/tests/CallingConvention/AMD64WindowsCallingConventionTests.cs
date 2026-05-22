// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

public class AMD64WindowsCallingConventionTests
{
    private static readonly Lazy<SyntheticVectorMetadata> s_syntheticVectorMetadata = new(SyntheticVectorMetadata.Create);

    private static CallConvTestCase Case => CallConvCases.AMD64Windows;

    private static int OffsetOfFirstGPArg => Case.ArgumentRegistersOffset;
    private static int OffsetOfStackArgs => Case.OffsetOfArgs + Case.NumArgumentRegisters * Case.PointerSize;
    private static int OffsetOfFirstFPArg => Case.OffsetOfFloatArgumentRegisters.Value;

    // Integer-valued args consume RCX/RDX/R8/R9 in order, then spill to stack
    // slots that advance by one pointer-sized home slot per argument.
    [Theory]
    [InlineData(CorElementType.I1, 4)]
    [InlineData(CorElementType.U1, 4)]
    [InlineData(CorElementType.I2, 4)]
    [InlineData(CorElementType.U2, 4)]
    [InlineData(CorElementType.I4, 4)]
    [InlineData(CorElementType.U4, 4)]
    [InlineData(CorElementType.I8, 4)]
    [InlineData(CorElementType.U8, 4)]
    [InlineData(CorElementType.I4, 5)]
    [InlineData(CorElementType.I8, 5)]
    public void IntArgs_FillRegsAndSpillToStack(CorElementType elementType, int count)
    {
        var (target, mdh) = CallingConventionTestHelpers.CreateTargetWithStaticMethod(
            Case,
            sig =>
            {
                sig.Return(CorElementType.Void);
                for (int i = 0; i < count; i++)
                {
                    sig.Param(elementType);
                }
            });

        CallSiteLayout layout = target.Contracts.CallingConvention.ComputeCallSiteLayout(mdh);
        Assert.Equal(count, layout.Arguments.Count);

        for (int i = 0; i < count; i++)
        {
            int expectedOffset = i < 4
                ? OffsetOfFirstGPArg + i * Case.PointerSize
                : OffsetOfStackArgs + (i - 4) * Case.PointerSize;

            Assert.Equal(expectedOffset, layout.Arguments[i].Slots[0].Offset);
        }
    }

    [Fact]
    public void InstanceMethod_ThisOffsetIsFirst()
    {
        var (target, mdh) = CallingConventionTestHelpers.CreateTargetWithMethod(
            Case, hasThis: true,
            sig => sig.Return(CorElementType.Void));

        CallSiteLayout layout = target.Contracts.CallingConvention.ComputeCallSiteLayout(mdh);
        Assert.Equal(OffsetOfFirstGPArg, layout.ThisOffset);
    }

    // Floating-point args shadow into XMM0-XMM3 by position, then spill into
    // pointer-sized stack home slots once the four register positions are used.
    [Theory]
    [InlineData(CorElementType.R4, 1)]
    [InlineData(CorElementType.R8, 1)]
    [InlineData(CorElementType.R4, 4)]
    [InlineData(CorElementType.R8, 4)]
    [InlineData(CorElementType.R4, 6)]
    [InlineData(CorElementType.R8, 6)]
    public void FloatArgs_FillFPRegsAndSpillToStack(CorElementType elementType, int count)
    {
        var (target, mdh) = CallingConventionTestHelpers.CreateTargetWithStaticMethod(
            Case,
            sig =>
            {
                sig.Return(CorElementType.Void);
                for (int i = 0; i < count; i++)
                {
                    sig.Param(elementType);
                }
            });

        CallSiteLayout layout = target.Contracts.CallingConvention.ComputeCallSiteLayout(mdh);
        Assert.Equal(count, layout.Arguments.Count);

        for (int i = 0; i < count; i++)
        {
            int expectedOffset = i < 4
                ? OffsetOfFirstFPArg + i * Case.FloatRegisterSize
                : OffsetOfStackArgs + (i - 4) * Case.PointerSize;

            Assert.Equal(elementType, layout.Arguments[i].Slots[0].ElementType);
            Assert.Equal(expectedOffset, layout.Arguments[i].Slots[0].Offset);
        }
    }

    // A lone float still chooses the XMM bank based on its ordinal position,
    // while the surrounding integer slots continue to map to GP registers.
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void OneFloatAmongInts_LandsInXMM(int floatPosition)
    {
        var (target, mdh) = CallingConventionTestHelpers.CreateTargetWithStaticMethod(
            Case,
            sig =>
            {
                sig.Return(CorElementType.Void);
                for (int i = 0; i < 4; i++)
                {
                    sig.Param(i == floatPosition ? CorElementType.R8 : CorElementType.I4);
                }
            });

        CallSiteLayout layout = target.Contracts.CallingConvention.ComputeCallSiteLayout(mdh);
        Assert.Equal(4, layout.Arguments.Count);

        for (int i = 0; i < 4; i++)
        {
            int expectedOffset = i == floatPosition
                ? OffsetOfFirstFPArg + i * Case.FloatRegisterSize
                : OffsetOfFirstGPArg + i * Case.PointerSize;
            CorElementType expectedType = i == floatPosition ? CorElementType.R8 : CorElementType.I4;

            Assert.Equal(expectedType, layout.Arguments[i].Slots[0].ElementType);
            Assert.Equal(expectedOffset, layout.Arguments[i].Slots[0].Offset);
        }
    }

    [Fact]
    public void InstanceMethod_FirstUserDoubleShiftsToXMM1()
    {
        // Hidden `this` consumes slot 0 (RCX). The first user-arg double therefore
        // shadows into XMM1, NOT XMM0. This is a managed-specific consequence of
        // `this` taking the first slot like any other argument.
        var (target, mdh) = CallingConventionTestHelpers.CreateTargetWithMethod(
            Case, hasThis: true,
            sig => sig.Return(CorElementType.Void).Param(CorElementType.R8));

        CallSiteLayout layout = target.Contracts.CallingConvention.ComputeCallSiteLayout(mdh);
        Assert.Single(layout.Arguments);
        Assert.Equal(OffsetOfFirstFPArg + 1 * Case.FloatRegisterSize, layout.Arguments[0].Slots[0].Offset);
    }

    [Fact]
    public void StaticMethod_RetBuf_FirstUserDoubleShiftsToXMM1()
    {
        // Method returns a 24-byte struct -> hidden retBuf in RCX. First user-arg
        // double therefore shadows into XMM1, NOT XMM0.
        var (target, mdh) = CallingConventionTestHelpers.CreateTargetWithMethod(
            Case, hasThis: false,
            (rts, sig) =>
            {
                MockMethodTable bigMT = MockDescriptors.CallingConvention.AddValueTypeMethodTable(
                    rts, "BigReturn", structSize: 24,
                    fields:
                    [
                        new(0, CorElementType.I8),
                        new(8, CorElementType.I8),
                        new(16, CorElementType.I8),
                    ]);
                sig.ReturnValueType(new TargetPointer(bigMT.Address)).Param(CorElementType.R8);
            });

        CallSiteLayout layout = target.Contracts.CallingConvention.ComputeCallSiteLayout(mdh);
        Assert.Single(layout.Arguments);
        Assert.Equal(OffsetOfFirstFPArg + 1 * Case.FloatRegisterSize, layout.Arguments[0].Slots[0].Offset);
    }

    [Fact]
    public void InstanceMethod_RetBuf_FirstUserDoubleShiftsToXMM2()
    {
        // `this` AND retBuf consume slots 0 and 1; first user-arg double -> XMM2.
        var (target, mdh) = CallingConventionTestHelpers.CreateTargetWithMethod(
            Case, hasThis: true,
            (rts, sig) =>
            {
                MockMethodTable bigMT = MockDescriptors.CallingConvention.AddValueTypeMethodTable(
                    rts, "BigReturn", structSize: 24,
                    fields:
                    [
                        new(0, CorElementType.I8),
                        new(8, CorElementType.I8),
                        new(16, CorElementType.I8),
                    ]);
                sig.ReturnValueType(new TargetPointer(bigMT.Address)).Param(CorElementType.R8);
            });

        CallSiteLayout layout = target.Contracts.CallingConvention.ComputeCallSiteLayout(mdh);
        Assert.Single(layout.Arguments);
        Assert.Equal(OffsetOfFirstFPArg + 2 * Case.FloatRegisterSize, layout.Arguments[0].Slots[0].Offset);
    }

    [Theory]
    [InlineData(false, false, false, false, 0)]
    [InlineData(true, false, false, false, 1)]
    [InlineData(false, true, false, false, 1)]
    [InlineData(true, true, false, false, 2)]
    [InlineData(false, false, true, false, 1)]
    [InlineData(true, false, true, false, 2)]
    [InlineData(false, false, false, true, 1)]
    [InlineData(true, true, true, false, 3)]
    [InlineData(true, true, false, true, 3)]
    [InlineData(true, true, true, true, -1)]
    public void HiddenArgs_ShiftFirstUserDouble(
        bool hasThis, bool hasRetBuf, bool hasParamType, bool hasAsyncCont,
        int expectedFloatPosition)
    {
        var (target, mdh) = CallingConventionTestHelpers.CreateTargetWithMethod(
            Case,
            hasThis,
            (rts, sig) =>
            {
                if (hasRetBuf)
                {
                    MockMethodTable bigMT = MockDescriptors.CallingConvention.AddValueTypeMethodTable(
                        rts,
                        "BigReturn",
                        structSize: 24,
                        fields:
                        [
                            new(0, CorElementType.I8),
                            new(8, CorElementType.I8),
                            new(16, CorElementType.I8),
                        ]);
                    sig.ReturnValueType(new TargetPointer(bigMT.Address));
                }
                else
                {
                    sig.Return(CorElementType.Void);
                }

                sig.Param(CorElementType.R8);
            },
            hasParamType,
            hasAsyncCont);

        CallSiteLayout layout = target.Contracts.CallingConvention.ComputeCallSiteLayout(mdh);
        Assert.Single(layout.Arguments);
        Assert.Equal(CorElementType.R8, layout.Arguments[0].Slots[0].ElementType);

        int expectedOffset = expectedFloatPosition >= 0
            ? OffsetOfFirstFPArg + expectedFloatPosition * Case.FloatRegisterSize
            : OffsetOfStackArgs;

        Assert.Equal(expectedOffset, layout.Arguments[0].Slots[0].Offset);
    }

    [Theory]
    [InlineData(false, false, 0, 1)]
    [InlineData(true, false, 1, 2)]
    [InlineData(false, true, 1, 2)]
    [InlineData(true, true, 2, 3)]
    public void VarArgs_CookieAndFirstUserArg_OnWindows(
        bool hasThis, bool hasRetBuf, int expectedCookieSlot, int expectedFirstUserSlot)
    {
        var (target, mdh) = CallingConventionTestHelpers.CreateTargetWithMethod(
            Case,
            hasThis,
            (rts, sig) =>
            {
                sig.VarArg();
                if (hasRetBuf)
                {
                    MockMethodTable bigMT = MockDescriptors.CallingConvention.AddValueTypeMethodTable(
                        rts,
                        "BigReturn",
                        structSize: 24,
                        fields:
                        [
                            new(0, CorElementType.I8),
                            new(8, CorElementType.I8),
                            new(16, CorElementType.I8),
                        ]);
                    sig.ReturnValueType(new TargetPointer(bigMT.Address));
                }
                else
                {
                    sig.Return(CorElementType.Void);
                }

                sig.Param(CorElementType.R8);
            });

        CallSiteLayout layout = target.Contracts.CallingConvention.ComputeCallSiteLayout(mdh);
        Assert.NotNull(layout.VarArgCookieOffset);
        Assert.Single(layout.Arguments);
        Assert.Equal(
            OffsetOfFirstGPArg + expectedCookieSlot * Case.PointerSize,
            layout.VarArgCookieOffset.Value);
        Assert.Equal(
            OffsetOfFirstFPArg + expectedFirstUserSlot * Case.FloatRegisterSize,
            layout.Arguments[0].Slots[0].Offset);
    }

    [Fact]
    public void NonVarArgs_HasNullVarArgCookieOffset()
    {
        var (target, mdh) = CallingConventionTestHelpers.CreateTargetWithStaticMethod(
            Case,
            sig => sig.Return(CorElementType.Void).Param(CorElementType.I4));

        CallSiteLayout layout = target.Contracts.CallingConvention.ComputeCallSiteLayout(mdh);
        Assert.Null(layout.VarArgCookieOffset);
    }

    // Stack home slots continue to progress one pointer at a time once the four
    // register-backed positions are exhausted by earlier integer arguments.
    [Fact]
    public void TenArgs_StackOffsetsProgress()
    {
        var (target, mdh) = CallingConventionTestHelpers.CreateTargetWithStaticMethod(
            Case,
            sig =>
            {
                sig.Return(CorElementType.Void);
                for (int i = 0; i < 10; i++)
                {
                    sig.Param(CorElementType.I4);
                }
            });

        CallSiteLayout layout = target.Contracts.CallingConvention.ComputeCallSiteLayout(mdh);
        Assert.Equal(10, layout.Arguments.Count);

        for (int i = 0; i < 10; i++)
        {
            int expectedOffset = i < 4
                ? OffsetOfFirstGPArg + i * Case.PointerSize
                : OffsetOfStackArgs + (i - 4) * Case.PointerSize;

            Assert.Equal(expectedOffset, layout.Arguments[i].Slots[0].Offset);
        }
    }

    // Windows x64 does not classify HFA-shaped structs into XMM registers; small
    // ones still use a GP slot, while larger ones take the existing byref path.
    [Theory]
    [InlineData(2, false)]
    [InlineData(4, true)]
    public void HFAShapedStruct_OnWindows_DoesNotEnregisterInFP(int floatCount, bool expectByref)
    {
        var (target, mdh) = CallingConventionTestHelpers.CreateTargetWithMethod(
            Case, hasThis: false,
            (rts, sig) =>
            {
                MockDescriptors.CallingConvention.ValueTypeField[] fields = new MockDescriptors.CallingConvention.ValueTypeField[floatCount];
                for (int i = 0; i < floatCount; i++)
                {
                    fields[i] = new(i * sizeof(float), CorElementType.R4);
                }

                MockMethodTable mt = MockDescriptors.CallingConvention.AddValueTypeMethodTable(
                    rts,
                    $"FloatStruct{floatCount}",
                    structSize: sizeof(float) * floatCount,
                    fields: fields);
                sig.Return(CorElementType.Void).ParamValueType(new TargetPointer(mt.Address));
            });

        CallSiteLayout layout = target.Contracts.CallingConvention.ComputeCallSiteLayout(mdh);
        Assert.Single(layout.Arguments);

        ArgLayout arg = layout.Arguments[0];
        Assert.Equal(expectByref, arg.IsPassedByRef);
        Assert.Single(arg.Slots);
        Assert.Equal(OffsetOfFirstGPArg, arg.Slots[0].Offset);
    }

    // ---- Implicit by-reference edge cases ----

    [Fact]
    public void NineByteStruct_ImplicitByref_OneSlot()
    {
        // 9-byte struct > 8 -> implicit byref. The arg slot holds an 8-byte
        // pointer, not the struct contents (would otherwise take 2 slots).
        var (target, mdh) = CallingConventionTestHelpers.CreateTargetWithMethod(
            Case, hasThis: false,
            (rts, sig) =>
            {
                MockMethodTable mt = MockDescriptors.CallingConvention.AddValueTypeMethodTable(
                    rts, "NineBytes", structSize: 9,
                    fields:
                    [
                        new(0, CorElementType.I8),
                        new(8, CorElementType.I1),
                    ]);
                sig.Return(CorElementType.Void).ParamValueType(new TargetPointer(mt.Address));
            });

        CallSiteLayout layout = target.Contracts.CallingConvention.ComputeCallSiteLayout(mdh);
        Assert.Single(layout.Arguments);
        ArgLayout arg = layout.Arguments[0];
        Assert.True(arg.IsPassedByRef);
        Assert.Single(arg.Slots);
        Assert.Equal(OffsetOfFirstGPArg, arg.Slots[0].Offset);
    }

    [Fact]
    public void ThreeByteStruct_NonPowerOfTwo_ImplicitByref()
    {
        // 3-byte struct: size <= 8 but not in {1, 2, 4, 8}. Implicit byref applies.
        // Verifies the `(size & (size - 1)) != 0` guard in IsArgPassedByRefBySize.
        var (target, mdh) = CallingConventionTestHelpers.CreateTargetWithMethod(
            Case, hasThis: false,
            (rts, sig) =>
            {
                MockMethodTable mt = MockDescriptors.CallingConvention.AddValueTypeMethodTable(
                    rts, "ThreeBytes", structSize: 3,
                    fields:
                    [
                        new(0, CorElementType.I1),
                        new(1, CorElementType.I1),
                        new(2, CorElementType.I1),
                    ]);
                sig.Return(CorElementType.Void).ParamValueType(new TargetPointer(mt.Address));
            });

        CallSiteLayout layout = target.Contracts.CallingConvention.ComputeCallSiteLayout(mdh);
        Assert.Single(layout.Arguments);
        Assert.True(layout.Arguments[0].IsPassedByRef);
    }

    [Fact]
    public void EightByteStruct_Enregisters_NotByref()
    {
        // 8-byte struct is in the {1, 2, 4, 8} exception list -> NOT byref;
        // enregistered as a value in RCX.
        var (target, mdh) = CallingConventionTestHelpers.CreateTargetWithMethod(
            Case, hasThis: false,
            (rts, sig) =>
            {
                MockMethodTable mt = MockDescriptors.CallingConvention.AddValueTypeMethodTable(
                    rts, "EightBytes", structSize: 8,
                    fields: [new(0, CorElementType.I8)]);
                sig.Return(CorElementType.Void).ParamValueType(new TargetPointer(mt.Address));
            });

        CallSiteLayout layout = target.Contracts.CallingConvention.ComputeCallSiteLayout(mdh);
        Assert.Single(layout.Arguments);
        ArgLayout arg = layout.Arguments[0];
        Assert.False(arg.IsPassedByRef);
        Assert.Equal(OffsetOfFirstGPArg, arg.Slots[0].Offset);
    }

    [Theory]
    [InlineData(CorElementType.I4)]
    [InlineData(CorElementType.I8)]
    [InlineData(CorElementType.R4)]
    [InlineData(CorElementType.R8)]
    public void Return_PrimitiveType_NoRetBuf(CorElementType retType)
    {
        var (target, mdh) = CallingConventionTestHelpers.CreateTargetWithStaticMethod(
            Case,
            sig => sig.Return(retType).Param(CorElementType.I4));

        CallSiteLayout layout = target.Contracts.CallingConvention.ComputeCallSiteLayout(mdh);
        Assert.Null(layout.ThisOffset);
        Assert.Single(layout.Arguments);
        Assert.Equal(OffsetOfFirstGPArg, layout.Arguments[0].Slots[0].Offset);
    }

    [Theory]
    [InlineData(4, false)]
    [InlineData(8, false)]
    [InlineData(16, true)]
    [InlineData(24, true)]
    public void Return_ValueType_RetBufBySize(int structSize, bool expectRetBuf)
    {
        var (target, mdh) = CallingConventionTestHelpers.CreateTargetWithMethod(
            Case, hasThis: false,
            (rts, sig) =>
            {
                MockDescriptors.CallingConvention.ValueTypeField[] fields = new MockDescriptors.CallingConvention.ValueTypeField[structSize / sizeof(int)];
                for (int i = 0; i < fields.Length; i++)
                {
                    fields[i] = new(i * sizeof(int), CorElementType.I4);
                }

                MockMethodTable mt = MockDescriptors.CallingConvention.AddValueTypeMethodTable(
                    rts, $"ReturnStruct{structSize}", structSize, fields);
                sig.ReturnValueType(new TargetPointer(mt.Address)).Param(CorElementType.I4);
            });

        CallSiteLayout layout = target.Contracts.CallingConvention.ComputeCallSiteLayout(mdh);
        Assert.Single(layout.Arguments);
        Assert.Equal(
            expectRetBuf ? OffsetOfFirstGPArg + Case.PointerSize : OffsetOfFirstGPArg,
            layout.Arguments[0].Slots[0].Offset);
    }

    [Fact]
    public void EmptyStruct_ImplicitByref()
    {
        var (target, mdh) = CallingConventionTestHelpers.CreateTargetWithMethod(
            Case, hasThis: false,
            (rts, sig) =>
            {
                MockMethodTable mt = MockDescriptors.CallingConvention.AddValueTypeMethodTable(
                    rts, "Empty", structSize: 0, fields: []);
                sig.Return(CorElementType.Void).ParamValueType(new TargetPointer(mt.Address));
            });

        CallSiteLayout layout = target.Contracts.CallingConvention.ComputeCallSiteLayout(mdh);
        Assert.Single(layout.Arguments);
        Assert.True(layout.Arguments[0].IsPassedByRef);
    }

    [Theory]
    [InlineData(CorElementType.Object)]
    [InlineData(CorElementType.String)]
    public void GCReferenceArgs_GoToGPRegs_NotByref(CorElementType refType)
    {
        var (target, mdh) = CallingConventionTestHelpers.CreateTargetWithStaticMethod(
            Case,
            sig => sig.Return(CorElementType.Void).Param(refType));

        CallSiteLayout layout = target.Contracts.CallingConvention.ComputeCallSiteLayout(mdh);
        Assert.Single(layout.Arguments);
        ArgLayout arg = layout.Arguments[0];
        Assert.False(arg.IsPassedByRef);
        Assert.Equal(OffsetOfFirstGPArg, arg.Slots[0].Offset);
    }

    [Theory]
    [InlineData("Vector64`1", 8, false)]
    [InlineData("Vector128`1", 16, true)]
    public void VectorType_OnWindows_ClassifiedBySizeNotVectorness(
        string vectorTypeName,
        int vectorByteSize,
        bool expectByref)
    {
        SyntheticVectorMetadata metadata = s_syntheticVectorMetadata.Value;
        var (target, mdh) = CallingConventionTestHelpers.CreateTargetWithMethod(
            Case,
            hasThis: false,
            (rts, sig) =>
            {
                MockMethodTable mt = MockDescriptors.CallingConvention.AddVectorMethodTable(
                    rts,
                    vectorTypeName,
                    vectorByteSize,
                    metadata.GetTypeDefToken(vectorTypeName));
                sig.Return(CorElementType.Void).ParamValueType(new TargetPointer(mt.Address));
            },
            syntheticMetadata: metadata);

        CallSiteLayout layout = target.Contracts.CallingConvention.ComputeCallSiteLayout(mdh);
        Assert.Single(layout.Arguments);

        ArgLayout arg = layout.Arguments[0];
        Assert.Equal(expectByref, arg.IsPassedByRef);
        Assert.Single(arg.Slots);
        Assert.Equal(OffsetOfFirstGPArg, arg.Slots[0].Offset);
    }

    [Fact]
    public void TypedReference_ImplicitByref_OneSlot()
    {
        // TypedReference is 16 bytes -> implicit byref on Win x64 (in contrast
        // with SysV where it lands in 2 GP regs). Verifies that the substitution
        // through g_TypedReferenceMT produces a 16-byte ArgTypeInfo that then
        // takes the byref path.
        var (target, mdh) = CallingConventionTestHelpers.CreateTargetWithMethod(
            Case, hasThis: false,
            (rts, sig) =>
            {
                MockMethodTable typedRefMT = MockDescriptors.CallingConvention.AddValueTypeMethodTable(
                    rts, "System.TypedReference", structSize: 16,
                    fields: [new(0, CorElementType.Byref), new(8, CorElementType.I)]);
                rts.SetTypedReferenceMethodTable(typedRefMT.Address);
                sig.Return(CorElementType.Void).Param(CorElementType.TypedByRef);
            });

        CallSiteLayout layout = target.Contracts.CallingConvention.ComputeCallSiteLayout(mdh);
        Assert.Single(layout.Arguments);
        ArgLayout arg = layout.Arguments[0];
        Assert.True(arg.IsPassedByRef);
        Assert.Single(arg.Slots);
        Assert.Equal(OffsetOfFirstGPArg, arg.Slots[0].Offset);
    }

    // ----- ValueTypeHandle population for by-value value-type args -----
    //
    // The GC scanner needs the value-type's TypeHandle to walk the GCDesc and
    // report embedded managed references inside structs that the iterator did
    // NOT pre-decompose into GC-typed ArgSlots. On AMD64-Windows, enregistered
    // and stack-passed value-type args go through the GP path without per-slot
    // GC typing, so ValueTypeHandle must be populated.

    [Fact]
    public void ValueTypeByValue_Enregistered_PopulatesValueTypeHandle()
    {
        // 8-byte struct -> fits in RCX, NOT byref. ValueTypeHandle should point
        // to the struct's MethodTable so GC scanner can walk its GCDesc.
        ulong expectedMtAddress = 0;
        var (target, mdh) = CallingConventionTestHelpers.CreateTargetWithMethod(
            Case, hasThis: false,
            (rts, sig) =>
            {
                MockMethodTable mt = MockDescriptors.CallingConvention.AddValueTypeMethodTable(
                    rts, "ObjectAndPad", structSize: 8,
                    fields: [new(0, CorElementType.Object)]);
                expectedMtAddress = mt.Address;
                sig.Return(CorElementType.Void).ParamValueType(new TargetPointer(mt.Address));
            });

        CallSiteLayout layout = target.Contracts.CallingConvention.ComputeCallSiteLayout(mdh);
        Assert.Single(layout.Arguments);
        ArgLayout arg = layout.Arguments[0];
        Assert.False(arg.IsPassedByRef);
        Assert.NotNull(arg.ValueTypeHandle);
        Assert.Equal(expectedMtAddress, (ulong)arg.ValueTypeHandle.Value.Address);
    }

    [Fact]
    public void ValueTypeByValue_OnStack_PopulatesValueTypeHandle()
    {
        // 8-byte enregisterable struct as the 5th arg lands on the stack
        // (RCX/RDX/R8/R9 consumed by 4 prior I8s) -- still by value, MT populated.
        ulong expectedMtAddress = 0;
        var (target, mdh) = CallingConventionTestHelpers.CreateTargetWithMethod(
            Case, hasThis: false,
            (rts, sig) =>
            {
                MockMethodTable mt = MockDescriptors.CallingConvention.AddValueTypeMethodTable(
                    rts, "ObjectAndPad", structSize: 8,
                    fields: [new(0, CorElementType.Object)]);
                expectedMtAddress = mt.Address;
                sig.Return(CorElementType.Void);
                for (int i = 0; i < 4; i++) sig.Param(CorElementType.I8);
                sig.ParamValueType(new TargetPointer(mt.Address));
            });

        CallSiteLayout layout = target.Contracts.CallingConvention.ComputeCallSiteLayout(mdh);
        Assert.Equal(5, layout.Arguments.Count);
        ArgLayout structArg = layout.Arguments[4];
        Assert.False(structArg.IsPassedByRef);
        Assert.NotNull(structArg.ValueTypeHandle);
        Assert.Equal(expectedMtAddress, (ulong)structArg.ValueTypeHandle.Value.Address);
    }

    [Fact]
    public void ValueTypeByRef_DoesNotPopulateValueTypeHandle()
    {
        // 9-byte struct -> implicit byref. The slot carries a pointer, not the
        // value bytes; GC scanner reports it via the byref path, not GCDesc walk.
        // Regression guard: pins the IsByRef short-circuit in ComputeValueTypeHandle.
        var (target, mdh) = CallingConventionTestHelpers.CreateTargetWithMethod(
            Case, hasThis: false,
            (rts, sig) =>
            {
                MockMethodTable mt = MockDescriptors.CallingConvention.AddValueTypeMethodTable(
                    rts, "NineBytes", structSize: 9,
                    fields: [new(0, CorElementType.I8), new(8, CorElementType.I1)]);
                sig.Return(CorElementType.Void).ParamValueType(new TargetPointer(mt.Address));
            });

        CallSiteLayout layout = target.Contracts.CallingConvention.ComputeCallSiteLayout(mdh);
        Assert.True(layout.Arguments[0].IsPassedByRef);
        Assert.Null(layout.Arguments[0].ValueTypeHandle);
    }

    [Fact]
    public void ValueTypeByValue_ByRefLike_PopulatesValueTypeHandle()
    {
        // ByRefLike value-type args (Span<T>, ref structs) are surfaced through
        // ValueTypeHandle just like ordinary value types. The GC scanner queries
        // IRuntimeTypeSystem.IsByRefLike to choose its walk strategy: a CGCDesc
        // walk for ordinary types, or a field-by-field walk for ByRefLike types
        // (the CGCDesc series doesn't encode managed byref fields).
        const uint IsByRefLikeFlag = 0x00001000;
        var (target, mdh) = CallingConventionTestHelpers.CreateTargetWithMethod(
            Case, hasThis: false,
            (rts, sig) =>
            {
                MockMethodTable mt = MockDescriptors.CallingConvention.AddValueTypeMethodTable(
                    rts, "ByRefLikeStruct", structSize: 8,
                    fields: [new(0, CorElementType.I8)]);
                mt.MTFlags |= IsByRefLikeFlag;
                sig.Return(CorElementType.Void).ParamValueType(new TargetPointer(mt.Address));
            });

        CallSiteLayout layout = target.Contracts.CallingConvention.ComputeCallSiteLayout(mdh);
        Assert.Single(layout.Arguments);
        ArgLayout arg = layout.Arguments[0];
        Assert.False(arg.IsPassedByRef);
        Assert.Single(arg.Slots);
        Assert.NotNull(arg.ValueTypeHandle);
    }
}
