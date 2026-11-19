// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Runtime
{
}

.assembly ValidationOnlyTests
{
}

.class public sequential ansi sealed beforefieldinit ValidationOnlyStruct
       extends [System.Runtime]System.ValueType
{
    .field public int32 F
}

.class public sequential ansi sealed beforefieldinit ValidationOnlyRefStruct
       extends [System.Runtime]System.ValueType
{
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.IsByRefLikeAttribute::.ctor() = ( 01 00 00 00 )
    .field public int32& P
}

// IL that is valid but not verifiable, and the validity checks that still apply to it.
// Native int parameters stand for unmanaged pointers.
.class public auto ansi beforefieldinit ValidationOnlyTestsType
       extends [System.Runtime]System.Object
{
    .field public int32 F

    .method public hidebysig static void Target() cil managed
    {
        ret
    }

    .method public hidebysig static void TakesInt32(int32 i) cil managed
    {
        ret
    }

    .method public hidebysig static void TakesRef(int32& r) cil managed
    {
        ret
    }

    .method public hidebysig static void TakesString(string s) cil managed
    {
        ret
    }

    .method public hidebysig static void TakesPointer(void* p) cil managed
    {
        ret
    }

    // Instructions that are never verifiable (III.3.37, III.3.30, III.3.36, III.2.2)

    .method public hidebysig static void Jmp.ToTarget_Invalid_Unverifiable_Validation_Valid() cil managed
    {
        jmp void ValidationOnlyTestsType::Target()
    }

    .method public hidebysig static void CopyBlock.UnmanagedPointers_Invalid_Unverifiable_Validation_Valid(native int d, native int s) cil managed
    {
        ldarg.0
        ldarg.1
        ldc.i4.4
        cpblk
        ret
    }

    .method public hidebysig static void InitBlock.UnmanagedPointer_Invalid_Unverifiable_Validation_Valid(native int d) cil managed
    {
        ldarg.0
        ldc.i4.0
        ldc.i4.4
        initblk
        ret
    }

    .method public hidebysig static void NoPrefix.Castclass_Invalid_Unverifiable_Validation_Valid(object o) cil managed
    {
        ldarg.0
        // no. typecheck
        .emitbyte 0xFE
        .emitbyte 0x19
        .emitbyte 0x01
        castclass [System.Runtime]System.Object
        pop
        ret
    }

    // Their validity rules are still reported

    .method public hidebysig static void Jmp.StackNotEmpty_Invalid_Unverifiable_Validation_StackUnexpected() cil managed
    {
        ldc.i4.0
        jmp void ValidationOnlyTestsType::Target()
    }

    .method public hidebysig static void Jmp.ArgumentCount_Invalid_Unverifiable_Validation_StackUnexpected() cil managed
    {
        jmp void ValidationOnlyTestsType::TakesInt32(int32)
    }

    .method public hidebysig static void Jmp.ArgumentType_Invalid_Unverifiable_Validation_StackUnexpected(string s) cil managed
    {
        jmp void ValidationOnlyTestsType::TakesInt32(int32)
    }

    .method public hidebysig static void Jmp.SameArgument_Invalid_Unverifiable_Validation_Valid(int32 i) cil managed
    {
        jmp void ValidationOnlyTestsType::TakesInt32(int32)
    }

    .method public hidebysig instance void Jmp.InstanceToStatic_Invalid_Unverifiable_Validation_StackUnexpected() cil managed
    {
        jmp void ValidationOnlyTestsType::Target()
    }

    .method public hidebysig instance void InstanceTarget() cil managed
    {
        ret
    }

    .class nested public auto ansi beforefieldinit OtherType
           extends [System.Runtime]System.Object
    {
        .method public hidebysig instance void InstanceTarget() cil managed
        {
            ret
        }
    }

    .method public hidebysig instance void Jmp.InstanceToSameType_Invalid_Unverifiable_Validation_Valid() cil managed
    {
        jmp instance void ValidationOnlyTestsType::InstanceTarget()
    }

    .method public hidebysig instance void Jmp.InstanceToOtherType_Invalid_Unverifiable_Validation_StackUnexpected() cil managed
    {
        jmp instance void ValidationOnlyTestsType/OtherType::InstanceTarget()
    }

    .class nested public auto ansi beforefieldinit DerivedType
           extends ValidationOnlyTestsType/OtherType
    {
        .method public hidebysig instance void Jmp.InstanceToBaseType_Invalid_Unverifiable_Validation_Valid() cil managed
        {
            jmp instance void ValidationOnlyTestsType/OtherType::InstanceTarget()
        }
    }

    .class nested public sequential ansi sealed beforefieldinit Struct
           extends [System.Runtime]System.ValueType
    {
        .method public hidebysig instance void InstanceTarget() cil managed
        {
            ret
        }

        .method public hidebysig instance void Jmp.ValueTypeToSameType_Invalid_Unverifiable_Validation_Valid() cil managed
        {
            jmp instance void ValidationOnlyTestsType/Struct::InstanceTarget()
        }

        .method public hidebysig instance void Jmp.ValueTypeToClass_Invalid_Unverifiable_Validation_StackUnexpected() cil managed
        {
            jmp instance void ValidationOnlyTestsType/OtherType::InstanceTarget()
        }
    }

    .method public hidebysig static void Jmp.InsideTry_Invalid_Unverifiable_Validation_JmpInsideER() cil managed
    {
        .try
        {
            jmp void ValidationOnlyTestsType::Target()
        }
        catch [System.Runtime]System.Object
        {
            pop
            leave.s END
        }
    END:
        ret
    }

    .method public hidebysig static void Jmp.InsideFilter_Invalid_Unverifiable_Validation_JmpInsideER() cil managed
    {
        .try
        {
            leave.s END
        }
        filter
        {
            pop
            jmp void ValidationOnlyTestsType::Target()
            endfilter
        }
        {
            pop
            leave.s END
        }
    END:
        ret
    }

    .method public hidebysig static void Jmp.InsideHandler_Invalid_Unverifiable_Validation_JmpInsideER() cil managed
    {
        .try
        {
            leave.s END
        }
        catch [System.Runtime]System.Object
        {
            pop
            jmp void ValidationOnlyTestsType::Target()
        }
    END:
        ret
    }

    .method public hidebysig static void Jmp.FromSynchronized_Invalid_Unverifiable_Validation_JmpInsideER() cil managed synchronized
    {
        jmp void ValidationOnlyTestsType::Target()
    }

    .method public hidebysig static void CopyBlock.FloatSource_Invalid_Unverifiable_Validation_StackByRef(native int d) cil managed
    {
        ldarg.0
        ldc.r8 1
        ldc.i4.4
        cpblk
        ret
    }

    .method public hidebysig static void CopyBlock.FloatDestination_Invalid_Unverifiable_Validation_StackByRef(native int s) cil managed
    {
        ldc.r8 1
        ldarg.0
        ldc.i4.4
        cpblk
        ret
    }

    .method public hidebysig static void CopyBlock.FloatSize_Invalid_Unverifiable.ExpectedIntegerType_Validation_ExpectedIntegerType(native int d, native int s) cil managed
    {
        ldarg.0
        ldarg.1
        ldc.r8 1
        cpblk
        ret
    }

    .method public hidebysig static void InitBlock.FloatAddress_Invalid_Unverifiable_Validation_StackByRef() cil managed
    {
        ldc.r8 1
        ldc.i4.0
        ldc.i4.4
        initblk
        ret
    }

    .method public hidebysig static void InitBlock.ManagedPointer_Invalid_Unverifiable_Validation_Valid(int32& d) cil managed
    {
        ldarg.0
        ldc.i4.0
        ldc.i4.4
        initblk
        ret
    }

    .method public hidebysig static void InitBlock.FloatValue_Invalid_Unverifiable.ExpectedIntegerType_Validation_ExpectedIntegerType(native int d) cil managed
    {
        ldarg.0
        ldc.r8 1
        ldc.i4.4
        initblk
        ret
    }

    .method public hidebysig static void InitBlock.FloatSize_Invalid_Unverifiable.ExpectedIntegerType_Validation_ExpectedIntegerType(native int d) cil managed
    {
        ldarg.0
        ldc.i4.0
        ldc.r8 1
        initblk
        ret
    }

    .method public hidebysig static void CopyBlock.Int64Size_Invalid_Unverifiable_Validation_ExpectedIntegerType(native int d, native int s) cil managed
    {
        ldarg.0
        ldarg.1
        ldc.i8 4
        cpblk
        ret
    }

    .method public hidebysig static void InitBlock.Int64Size_Invalid_Unverifiable_Validation_ExpectedIntegerType(native int d) cil managed
    {
        ldarg.0
        ldc.i4.0
        ldc.i8 4
        initblk
        ret
    }

    .method public hidebysig static void InitBlock.Int64Value_Invalid_Unverifiable_Validation_ExpectedIntegerType(native int d) cil managed
    {
        ldarg.0
        ldc.i8 0
        ldc.i4.4
        initblk
        ret
    }

    // localsinit is required only for verifiable methods (III.1.8.1.1)

    .method public hidebysig static void InitLocals.LoadLocal_Invalid_InitLocals_Validation_Valid() cil managed
    {
        .locals (int32 V_0)
        ldloc.0
        pop
        ret
    }

    .method public hidebysig static void InitLocals.LoadLocalAddress_Invalid_InitLocals_Validation_Valid() cil managed
    {
        .locals (int32 V_0)
        ldloca.s V_0
        pop
        ret
    }

    // Unmanaged pointers (III.1.1.5.1); a native int is valid where a managed pointer is expected (III.1.6)

    .method public hidebysig static void UnmanagedPointer.LoadArgument_Invalid_UnmanagedPointer_Validation_Valid(int32* p) cil managed
    {
        ldarg.0
        pop
        ret
    }

    .method public hidebysig static void Call.NativeIntForByRef_Invalid_StackUnexpected_Validation_Valid(native int p) cil managed
    {
        ldarg.0
        call void ValidationOnlyTestsType::TakesRef(int32&)
        ret
    }

    // A managed pointer passed for a native int argument, as C# emits for u8 literals; only as an argument

    .method public hidebysig static void Call.ByRefForNativeInt_Invalid_StackUnexpected_Validation_Valid(int32& a) cil managed
    {
        ldarg.0
        call void ValidationOnlyTestsType::TakesPointer(void*)
        ret
    }

    .method public hidebysig static void Calli.ByRefForNativeInt_Invalid_Unverifiable.StackUnexpected_Validation_Valid(int32& a) cil managed
    {
        ldarg.0
        ldftn void ValidationOnlyTestsType::TakesPointer(void*)
        calli void(void*)
        ret
    }

    .method public hidebysig static void Call.ReadOnlyByRefForNativeInt_Invalid_StackUnexpected(int32[] a) cil managed
    {
        ldarg.0
        ldc.i4.0
        readonly.
        ldelema [System.Runtime]System.Int32
        call void ValidationOnlyTestsType::TakesPointer(void*)
        ret
    }

    .method public hidebysig static native int Return.ByRefForNativeInt_Invalid_StackUnexpected(int32& a) cil managed
    {
        ldarg.0
        ret
    }

    // ILVerify does not track which frame a managed pointer points into (III.2.4), so a tail call may pass none

    .method public hidebysig static void TailCall.ByRefForNativeInt_Invalid_StackUnexpected_Validation_TailByRef(int32& a) cil managed
    {
        ldarg.0
        tail. call void ValidationOnlyTestsType::TakesPointer(void*)
        ret
    }

    .method public hidebysig static void TailCall.CalliByRefForNativeInt_Invalid_Unverifiable.StackUnexpected_Validation_TailByRef(int32& a) cil managed
    {
        ldarg.0
        ldftn void ValidationOnlyTestsType::TakesPointer(void*)
        tail. calli void(void*)
        ret
    }

    // A native int and a managed pointer merge to a native int; the value on the first path to JOIN sets its entry

    .method public hidebysig static void Merge.ByRefIntoNativeInt_Invalid_PathStackUnexpected_Validation_Valid(native int p, bool c, int32& a) cil managed
    {
        ldarg.0
        ldarg.1
        brtrue.s JOIN
        pop
        ldarg.2
    JOIN:
        pop
        ret
    }

    .method public hidebysig static native int Merge.NativeIntIntoByRef_Invalid_PathStackUnexpected_Validation_Valid(int32& a, native int p, bool c) cil managed
    {
        ldarg.0
        ldarg.2
        brtrue.s JOIN
        pop
        ldarg.1
    JOIN:
        ret
    }

    .method public hidebysig static void Merge.ReadOnlyIntoNativeInt_Invalid_PathStackUnexpected(native int p, bool c, int32[] a) cil managed
    {
        ldarg.0
        ldarg.1
        brtrue.s JOIN
        pop
        ldarg.2
        ldc.i4.0
        readonly.
        ldelema [System.Runtime]System.Int32
    JOIN:
        pop
        ret
    }

    .method public hidebysig static void Merge.NativeIntIntoReadOnly_Invalid_PathStackUnexpected(int32[] a, bool c, native int p) cil managed
    {
        ldarg.0
        ldc.i4.0
        readonly.
        ldelema [System.Runtime]System.Int32
        ldarg.1
        brtrue.s JOIN
        pop
        ldarg.2
    JOIN:
        pop
        ret
    }

    .method public hidebysig static void Merge.ByRefIntoInt32_Invalid_PathStackUnexpected(bool c, int32& a) cil managed
    {
        ldc.i4.0
        ldarg.0
        brtrue.s JOIN
        pop
        ldarg.1
    JOIN:
        pop
        ret
    }

    .method public hidebysig static void Merge.Int32IntoByRef_Invalid_PathStackUnexpected(int32& a, bool c) cil managed
    {
        ldarg.0
        ldarg.1
        brtrue.s JOIN
        pop
        ldc.i4.0
    JOIN:
        pop
        ret
    }

    .method public hidebysig static void Merge.Int32IntoNativeInt_Invalid_PathStackUnexpected(native int p, bool c) cil managed
    {
        ldarg.0
        ldarg.1
        brtrue.s JOIN
        pop
        ldc.i4.0
    JOIN:
        pop
        ret
    }

    .method public hidebysig static void Merge.NativeIntIntoInt32_Invalid_PathStackUnexpected(bool c, native int p) cil managed
    {
        ldc.i4.0
        ldarg.0
        brtrue.s JOIN
        pop
        ldarg.1
    JOIN:
        pop
        ret
    }

    .method public hidebysig static void Merge.NativeIntAfterJoin_Invalid_PathStackUnexpected_Validation_StackUnexpected(valuetype ValidationOnlyRefStruct& r, int32& a, native int p, bool c) cil managed
    {
        // JOIN is imported with the managed pointer first; the native int must make it be imported again.
        ldarg.0
        ldflda int32& ValidationOnlyRefStruct::P
        ldarg.1
        ldarg.3
        brfalse.s OTHER
        br.s JOIN
    OTHER:
        pop
        ldarg.2
    JOIN:
        stind.ref
        ret
    }

    .method public hidebysig static void Constrained.NativeIntThis_Invalid_ConstrainedCallWithNonByRefThis.StackUnexpected_Validation_Valid(native int p) cil managed
    {
        ldarg.0
        constrained. ValidationOnlyStruct
        callvirt instance string [System.Runtime]System.Object::ToString()
        pop
        ret
    }

    .method public hidebysig static string Constrained.ObjectThis_Invalid_ConstrainedCallWithNonByRefThis(object o) cil managed
    {
        ldarg.0
        constrained. [System.Runtime]System.Object
        callvirt instance string [System.Runtime]System.Object::ToString()
        ret
    }

    // A managed pointer plus an integer, as C# emits to index a fixed-size buffer (Table III.2)

    .method public hidebysig static int32& Arithmetic.AddByRefInt32_Invalid_ExpectedNumericType_Validation_Valid(int32& a) cil managed
    {
        ldarg.0
        ldc.i4.4
        add
        ret
    }

    .method public hidebysig static native int Arithmetic.AddByRefNativeInt_Invalid_ExpectedNumericType_Validation_StackUnexpected(int32& a, native int n) cil managed
    {
        // The sum is a managed pointer, so it is not returned as a native int.
        ldarg.0
        ldarg.1
        add
        ret
    }

    .method public hidebysig static void Arithmetic.AddInt32ByRef_Invalid_ExpectedNumericType(int32& a) cil managed
    {
        ldc.i4.4
        ldarg.0
        add
        pop
        ret
    }

    .method public hidebysig static void Arithmetic.SubByRefInt32_Invalid_ExpectedNumericType(int32& a) cil managed
    {
        ldarg.0
        ldc.i4.4
        sub
        pop
        ret
    }

    .method public hidebysig static void Arithmetic.AddByRefInt64_Invalid_ExpectedNumericType(int32& a) cil managed
    {
        ldarg.0
        ldc.i8 4
        add
        pop
        ret
    }

    .method public hidebysig static void Arithmetic.AddReadOnlyByRefInt32_Invalid_ExpectedNumericType(int32[] a) cil managed
    {
        ldarg.0
        ldc.i4.0
        readonly.
        ldelema [System.Runtime]System.Int32
        ldc.i4.4
        add
        pop
        ret
    }

    // Conversions to native int that stop GC tracking, as C# emits in fixed statements (Table III.8)

    .method public hidebysig static native uint Convert.ByRefToNativeUInt_Invalid_ExpectedNumericType_Validation_Valid(int32& a) cil managed
    {
        ldarg.0
        conv.u
        ret
    }

    .method public hidebysig static native int Convert.ObjectToNativeInt_Invalid_ExpectedNumericType_Validation_Valid(object o) cil managed
    {
        ldarg.0
        conv.i
        ret
    }

    .method public hidebysig static native uint Convert.ByRefToNativeUIntWithOverflowCheck_Invalid_ExpectedNumericType_Validation_Valid(int32& a) cil managed
    {
        ldarg.0
        conv.ovf.u
        ret
    }

    .method public hidebysig static int64 Convert.ObjectToInt64_Invalid_ExpectedNumericType(object o) cil managed
    {
        ldarg.0
        conv.i8
        ret
    }

    .method public hidebysig static native uint Convert.ReadOnlyByRefToNativeUInt_Invalid_ExpectedNumericType(int32[] a) cil managed
    {
        ldarg.0
        ldc.i4.0
        readonly.
        ldelema [System.Runtime]System.Int32
        conv.u
        ret
    }

    // Fields, indirect loads and stores, initobj and cpobj through an unmanaged pointer

    .method public hidebysig static int32 LoadField.UnmanagedPointer_Invalid_StackUnexpected_Validation_Valid(native int p) cil managed
    {
        ldarg.0
        ldfld int32 ValidationOnlyStruct::F
        ret
    }

    .method public hidebysig static int32 LoadField.NativeIntForObject_Invalid_StackUnexpected(native int p) cil managed
    {
        ldarg.0
        ldfld int32 ValidationOnlyTestsType::F
        ret
    }

    .method public hidebysig static void LoadFieldAddress.NativeIntForObject_Invalid_StackUnexpected(native int p) cil managed
    {
        ldarg.0
        ldflda int32 ValidationOnlyTestsType::F
        pop
        ret
    }

    .method public hidebysig static void StoreField.NativeIntForObject_Invalid_StackUnexpected(native int p) cil managed
    {
        ldarg.0
        ldc.i4.0
        stfld int32 ValidationOnlyTestsType::F
        ret
    }

    .method public hidebysig static native int LoadFieldAddress.UnmanagedPointer_Invalid_StackUnexpected.StackUnexpected_Validation_Valid(native int p) cil managed
    {
        // Through an unmanaged pointer, ldflda pushes an unmanaged pointer.
        ldarg.0
        ldflda int32 ValidationOnlyStruct::F
        ret
    }

    .method public hidebysig static void StoreField.UnmanagedPointer_Invalid_StackUnexpected_Validation_Valid(native int p) cil managed
    {
        ldarg.0
        ldc.i4.0
        stfld int32 ValidationOnlyStruct::F
        ret
    }

    .method public hidebysig static int32 LoadIndirect.UnmanagedPointer_Invalid_StackByRef_Validation_Valid(native int p) cil managed
    {
        ldarg.0
        ldind.i4
        ret
    }

    .method public hidebysig static void StoreIndirect.UnmanagedPointer_Invalid_StackByRef_Validation_Valid(native int p) cil managed
    {
        ldarg.0
        ldc.i4.0
        stind.i4
        ret
    }

    .method public hidebysig static void StoreIndirect.UnmanagedPointerObject_Invalid_StackByRef_Validation_StackUnexpected(native int p, object o) cil managed
    {
        ldarg.0
        ldarg.1
        stind.i4
        ret
    }

    .method public hidebysig static void StoreIndirectRef.UnmanagedPointerInt32_Invalid_StackByRef_Validation_StackObjRef(native int p) cil managed
    {
        ldarg.0
        ldc.i4.0
        stind.ref
        ret
    }

    .method public hidebysig static void StoreIndirect.NativeIntIntoByRefField_Invalid_StackUnexpected(valuetype ValidationOnlyRefStruct& r, native int p) cil managed
    {
        // The field holds a managed pointer, so stind.i does not store into it.
        ldarg.0
        ldflda int32& ValidationOnlyRefStruct::P
        ldarg.1
        stind.i
        ret
    }

    .method public hidebysig static void StoreIndirectRef.NativeIntIntoByRefField_Invalid_StackUnexpected(valuetype ValidationOnlyRefStruct& r, native int p) cil managed
    {
        ldarg.0
        ldflda int32& ValidationOnlyRefStruct::P
        ldarg.1
        stind.ref
        ret
    }

    .method public hidebysig static void InitObj.UnmanagedPointer_Invalid_StackByRef.StackUnexpected_Validation_Valid(native int p) cil managed
    {
        ldarg.0
        initobj [System.Runtime]System.Int32
        ret
    }

    .method public hidebysig static void CopyObject.UnmanagedPointerSource_Invalid_StackByRef_Validation_Valid(int32& d, native int s) cil managed
    {
        ldarg.0
        ldarg.1
        cpobj [System.Runtime]System.Int32
        ret
    }

    .method public hidebysig static void CopyObject.UnmanagedPointerDestination_Invalid_StackByRef_Validation_Valid(native int d, int32& s) cil managed
    {
        ldarg.0
        ldarg.1
        cpobj [System.Runtime]System.Int32
        ret
    }

    // ldind.ref through an unmanaged pointer pushes an object reference of unknown class, as for C#'s *(object*)p

    .method public hidebysig static void LoadIndirectRef.UnmanagedPointer_Invalid_StackByRef_Validation_Valid(native int p) cil managed
    {
        ldarg.0
        ldind.ref
        call void ValidationOnlyTestsType::TakesString(string)
        ret
    }

    .method public hidebysig static void Delegate.ClosedOverUnknownClass_Invalid_StackByRef_Validation_Valid(native int p) cil managed
    {
        ldarg.0
        ldind.ref
        ldftn instance class [System.Runtime]System.Type [System.Runtime]System.Object::GetType()
        newobj instance void class [System.Runtime]System.Func`1<class [System.Runtime]System.Type>::.ctor(object, native int)
        pop
        ret
    }

    .method public hidebysig static void Delegate.ClosedOverUnknownClassElement_Invalid_StackByRef_Validation_Valid(native int p) cil managed
    {
        ldarg.0
        ldind.ref
        ldc.i4.0
        ldelem.ref
        ldftn instance class [System.Runtime]System.Type [System.Runtime]System.Object::GetType()
        newobj instance void class [System.Runtime]System.Func`1<class [System.Runtime]System.Type>::.ctor(object, native int)
        pop
        ret
    }

    .method public hidebysig static void Delegate.ClosedOverNull_Invalid_DelegateCtor() cil managed
    {
        ldnull
        ldftn instance class [System.Runtime]System.Type [System.Runtime]System.Object::GetType()
        newobj instance void class [System.Runtime]System.Func`1<class [System.Runtime]System.Type>::.ctor(object, native int)
        pop
        ret
    }

    // At a join not yet imported, null and an object reference of unknown class merge to unknown class, in either order
    .method public hidebysig static void Delegate.ClosedOverNullThenUnknownClass_Invalid_StackByRef_Validation_Valid(bool c, native int p) cil managed
    {
        ldnull
        ldarg.0
        brtrue.s JOIN
        pop
        ldarg.1
        ldind.ref
    JOIN:
        ldftn instance class [System.Runtime]System.Type [System.Runtime]System.Object::GetType()
        newobj instance void class [System.Runtime]System.Func`1<class [System.Runtime]System.Type>::.ctor(object, native int)
        pop
        ret
    }

    .method public hidebysig static void Delegate.ClosedOverUnknownClassThenNull_Invalid_StackByRef_Validation_Valid(bool c, native int p) cil managed
    {
        ldarg.1
        ldind.ref
        ldarg.0
        brtrue.s JOIN
        pop
        ldnull
    JOIN:
        ldftn instance class [System.Runtime]System.Type [System.Runtime]System.Object::GetType()
        newobj instance void class [System.Runtime]System.Func`1<class [System.Runtime]System.Type>::.ctor(object, native int)
        pop
        ret
    }

    // Like null, an object reference of unknown class joined with a known class becomes that class, in either order
    .method public hidebysig static void Join.ObjectThenUnknownClass_Invalid_StackByRef_Validation_StackUnexpected(bool c, object o, native int p) cil managed
    {
        ldarg.1
        ldarg.0
        brtrue.s JOIN
        pop
        ldarg.2
        ldind.ref
    JOIN:
        call void ValidationOnlyTestsType::TakesString(string)
        ret
    }

    .method public hidebysig static void Join.UnknownClassThenObject_Invalid_StackByRef_Validation_StackUnexpected(bool c, object o, native int p) cil managed
    {
        ldarg.2
        ldind.ref
        ldarg.0
        brtrue.s JOIN
        pop
        ldarg.1
    JOIN:
        call void ValidationOnlyTestsType::TakesString(string)
        ret
    }

    .method public hidebysig static void Delegate.OpenWithUnknownClassTarget_Invalid_StackByRef_Validation_DelegateCtor(native int p) cil managed
    {
        ldarg.0
        ldind.ref
        ldftn instance class [System.Runtime]System.Type [System.Runtime]System.Object::GetType()
        newobj instance void class [System.Runtime]System.Func`2<object, class [System.Runtime]System.Type>::.ctor(object, native int)
        pop
        ret
    }

    // The join is first imported with null, a valid open delegate target; the unknown class arrives later and is re-imported
    .method public hidebysig static void Delegate.OpenOverNullThenUnknownClass_Invalid_StackByRef_Validation_DelegateCtor(bool c, native int p) cil managed
    {
        ldarg.0
        brtrue.s UNKNOWN
        ldnull
        br.s JOIN
    UNKNOWN:
        ldarg.1
        ldind.ref
    JOIN:
        ldftn instance class [System.Runtime]System.Type [System.Runtime]System.Object::GetType()
        newobj instance void class [System.Runtime]System.Func`2<object, class [System.Runtime]System.Type>::.ctor(object, native int)
        pop
        ret
    }

    .method public hidebysig static void StoreElement.Int32IntoUnknownClassArray_Invalid_StackByRef_Validation_StackObjRef(native int p) cil managed
    {
        ldarg.0
        ldind.ref
        ldc.i4.0
        ldc.i4.s 42
        stelem.ref
        ret
    }
}
