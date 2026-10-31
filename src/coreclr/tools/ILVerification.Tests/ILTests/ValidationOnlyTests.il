// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Runtime
{
}

.assembly ValidationOnlyTests
{
}

// Tests for VerifierOptions.ValidationOnly. Each method reaches one check that only verifiable IL must follow
// (valid but unverifiable, so validation-only mode does not report it), or the part of a check that valid IL
// must follow (reported in both modes). Native int parameters stand for unmanaged pointers.

.class public sequential ansi sealed beforefieldinit ValidationOnlyStruct
       extends [System.Runtime]System.ValueType
{
    .field public int32 F

    .method public hidebysig instance void M() cil managed
    {
        ret
    }
}

.class public sequential ansi sealed beforefieldinit ValidationOnlyOtherStruct
       extends [System.Runtime]System.ValueType
{
    .field public int64 G
}

.class public sequential ansi sealed beforefieldinit ValidationOnlyRefStruct
       extends [System.Runtime]System.ValueType
{
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.IsByRefLikeAttribute::.ctor() = ( 01 00 00 00 )
    .field public int32& P
}

.class public auto ansi beforefieldinit ValidationOnlyClass
       extends [System.Runtime]System.Object
{
    .field public int32 F
    .field public string S
}

.class public auto ansi beforefieldinit ValidationOnlyTestsType
       extends [System.Runtime]System.Object
{
    .field public static int32 StaticField

    .method public hidebysig static void Target() cil managed
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

    // A managed pointer passed where a native int is expected: the conversion conv.u makes explicitly (Table III.8)

    .method public hidebysig static void Call.ByRefForNativeInt_Invalid_StackUnexpected_Validation_Valid(int32& a) cil managed
    {
        ldarg.0
        call void ValidationOnlyTestsType::TakesPointer(void*)
        ret
    }

    .method public hidebysig static void Newobj.SpanOverStaticField_Invalid_StackUnexpected_Validation_Valid() cil managed
    {
        // The C# compiler's pattern for u8 literals and static data.
        ldsflda int32 ValidationOnlyTestsType::StaticField
        ldc.i4.4
        newobj instance void valuetype [System.Runtime]System.ReadOnlySpan`1<uint8>::.ctor(void*, int32)
        pop
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

    .method public hidebysig static void Newobj.DelegateByRefMethodPointer_Invalid_StackMethod.DelegatePattern_Validation_Valid(int32& a) cil managed
    {
        ldnull
        ldarg.0
        newobj instance void [System.Runtime]System.Action::.ctor(object, native int)
        pop
        ret
    }

    .method public hidebysig static void StoreLocal.ByRefToNativeInt_Invalid_StackUnexpected(int32& a) cil managed
    {
        .locals init (native int V_0)
        ldarg.0
        stloc.0
        ret
    }

    // A native int and a managed pointer merge to a native int

    .method public hidebysig static void Merge.ByRefIntoNativeInt_Invalid_PathStackUnexpected_Validation_Valid(bool c, int32& a, native int p) cil managed
    {
        .locals init (native int V_0)
        ldarg.0
        brtrue.s OTHER
        ldarg.2
        br.s JOIN
    OTHER:
        ldarg.1
    JOIN:
        stloc.0
        ret
    }

    .method public hidebysig static void Merge.NativeIntIntoByRef_Invalid_PathStackUnexpected_Validation_Valid(int32& a, native int p, bool c) cil managed
    {
        // The managed pointer reaches JOIN first; the native int then turns the slot into a native int before
        // JOIN is imported.
        .locals init (native int V_0)
        ldarg.0
        ldarg.2
        brtrue.s JOIN
        pop
        ldarg.1
    JOIN:
        stloc.0
        ret
    }

    .method public hidebysig static void Merge.Int32AndByRef_Invalid_PathStackUnexpected(bool c, int32& a) cil managed
    {
        ldarg.0
        brfalse.s OTHER
        ldarg.1
        br.s JOIN
    OTHER:
        ldc.i4.0
    JOIN:
        pop
        ret
    }

    .method public hidebysig static void Merge.ByRefIntoInt32_Invalid_PathStackUnexpected(bool c, int32& a) cil managed
    {
        ldarg.0
        brfalse.s OTHER
        ldc.i4.0
        br.s JOIN
    OTHER:
        ldarg.1
    JOIN:
        pop
        ret
    }

    .method public hidebysig static void Merge.NativeIntAfterImport_Invalid_ExpectedNumericType(int32& a, native int p, bool c) cil managed
    {
        // JOIN is imported with the managed pointer first, where & - & is valid. The native int then turns the
        // slot into a native int and JOIN is imported again, where native int - & is not valid (Table III.2).
        ldarg.2
        brtrue.s OTHER
        ldarg.0
        br.s JOIN
    OTHER:
        ldarg.1
    JOIN:
        ldarg.0
        sub
        pop
        ret
    }

    // constrained. with an unmanaged pointer as 'this'

    .method public hidebysig static void Constrained.NativeIntThis_Invalid_ConstrainedCallWithNonByRefThis.StackUnexpected_Validation_Valid(native int p) cil managed
    {
        ldarg.0
        constrained. ValidationOnlyStruct
        callvirt instance string [System.Runtime]System.Object::ToString()
        pop
        ret
    }

    // stind: the type the pointer points to is not a stack type; the value's stack type is checked

    .method public hidebysig static void StoreIndirect.PointeeMismatch_Invalid_StackUnexpected_Validation_Valid(int64& a) cil managed
    {
        ldarg.0
        ldc.i4.0
        stind.i4
        ret
    }

    .method public hidebysig static void StoreIndirect.ObjectValue_Invalid_StackUnexpected(int32& a, object o) cil managed
    {
        ldarg.0
        ldarg.1
        stind.i4
        ret
    }

    // Only stack types are checked (I.12.3.2.1): not the class of an object reference, the type a managed pointer
    // points to, or the element type of an array

    .method public hidebysig static string Constrained.OtherType_Invalid_StackUnexpected_Validation_Valid(int32& a) cil managed
    {
        ldarg.0
        constrained. [System.Runtime]System.Int64
        callvirt instance string [System.Runtime]System.Object::ToString()
        ret
    }

    .method public hidebysig static void Calli.ClassMismatch_Invalid_Unverifiable.StackUnexpected_Validation_Valid(object o) cil managed
    {
        ldarg.0
        ldftn void ValidationOnlyTestsType::TakesString(string)
        calli void(string)
        ret
    }

    .method public hidebysig static void LoadElement.Int32FromInt64Array_Invalid_StackUnexpected_Validation_Valid(int64[] a) cil managed
    {
        ldarg.0
        ldc.i4.0
        ldelem.i4
        pop
        ret
    }

    .method public hidebysig static void LoadElement.RefFromInt32Array_Invalid_StackUnexpected_Validation_Valid(int32[] a) cil managed
    {
        ldarg.0
        ldc.i4.0
        ldelem.ref
        pop
        ret
    }

    .method public hidebysig static void StoreElement.Int32ToInt64Array_Invalid_StackUnexpected_Validation_Valid(int64[] a) cil managed
    {
        ldarg.0
        ldc.i4.0
        ldc.i4.0
        stelem.i4
        ret
    }

    .method public hidebysig static void StoreElement.NullRefToInt32Array_Invalid_StackUnexpected.StackUnexpected_Validation_Valid(int32[] a) cil managed
    {
        ldarg.0
        ldc.i4.0
        ldnull
        stelem.ref
        ret
    }

    .method public hidebysig static void LoadElementAddress.Int64OfInt32Array_Invalid_StackUnexpectedArrayType_Validation_Valid(int32[] a) cil managed
    {
        ldarg.0
        ldc.i4.0
        ldelema [System.Runtime]System.Int64
        pop
        ret
    }

    .method public hidebysig static void StoreObject.UnmanagedPointerObjectAsString_Invalid_StackByRef_Validation_Valid(native int p, object o) cil managed
    {
        ldarg.0
        ldarg.1
        stobj [System.Runtime]System.String
        ret
    }

    .method public hidebysig static void Box.ObjectAsString_Invalid_StackUnexpected_Validation_Valid(object o) cil managed
    {
        ldarg.0
        box [System.Runtime]System.String
        pop
        ret
    }

    .method public hidebysig static void StoreElement.ObjectToStringArray_Invalid_StackUnexpected_Validation_Valid(string[] a, object o) cil managed
    {
        ldarg.0
        ldc.i4.0
        ldarg.1
        stelem [System.Runtime]System.String
        ret
    }

    .method public hidebysig static void LoadIndirect.RefFromInt32RefIsObjRef_Invalid_StackUnexpected.StackUnexpected_Validation_Valid(int32& a) cil managed
    {
        // ldind.ref loads an object reference whatever the pointer points to.
        ldarg.0
        ldind.ref
        call void ValidationOnlyTestsType::TakesString(string)
        ret
    }

    .method public hidebysig static object LoadElement.RefOfUnknownClassArray_Invalid_StackByRef_Validation_Valid(native int p) cil managed
    {
        // ldind.ref through an unmanaged pointer pushes an object reference of unknown class; ldelem.ref accepts it as
        // an array, as it accepts null, and pushes a null reference.
        ldarg.0
        ldind.ref
        ldc.i4.0
        ldelem.ref
        ret
    }

    .method public hidebysig static void LoadElement.RefFromInt32ArrayIsObjRef_Invalid_StackUnexpected.StackUnexpected_Validation_Valid(int32[] a) cil managed
    {
        // ldelem.ref loads an object reference whatever the array holds.
        ldarg.0
        ldc.i4.0
        ldelem.ref
        call void ValidationOnlyTestsType::TakesString(string)
        ret
    }

    .method public hidebysig static void StoreIndirect.RefThroughInt32Ref_Invalid_StackUnexpected_Validation_Valid(int32& a, object o) cil managed
    {
        ldarg.0
        ldarg.1
        stind.ref
        ret
    }

    .method public hidebysig static class ValidationOnlyClass TailCall.ReturnOtherClass_Invalid_TailRetType.StackUnexpected_Validation_Valid() cil managed
    {
        tail. call string ValidationOnlyKeptTestsType::ReturnsString()
        ret
    }

    // Instructions that are never verifiable (III.3.37, III.3.30, III.3.36, III.2.2)

    .method public hidebysig static void Jmp.ToTarget_Invalid_Unverifiable_Validation_Valid() cil managed
    {
        jmp void ValidationOnlyTestsType::Target()
    }

    .method public hidebysig static void CopyBlock.UnmanagedPointers_Invalid_Unverifiable_Validation_Valid(native int destination, native int source) cil managed
    {
        ldarg.0
        ldarg.1
        ldc.i4.4
        cpblk
        ret
    }

    .method public hidebysig static void InitBlock.UnmanagedPointer_Invalid_Unverifiable_Validation_Valid(native int destination) cil managed
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

    // Unmanaged pointers (III.1.1.5.1)

    .method public hidebysig static void UnmanagedPointer.LoadArgument_Invalid_UnmanagedPointer_Validation_Valid(int32* p) cil managed
    {
        ldarg.0
        pop
        ret
    }

    // Tail calls with managed pointers: correct IL passes none into the frame being removed (III.2.4 Correctness).
    // ILVerify does not track which frame a pointer points into, so both modes report every managed pointer.

    .method public hidebysig static void TailCall.LocalAddress_Invalid_TailByRef() cil managed
    {
        .locals init (int32 V_0)
        ldloca.s V_0
        tail. call void ValidationOnlyTestsType::TakesRef(int32&)
        ret
    }

    .method public hidebysig static void TailCall.LocalAddressAsNativeInt_Invalid_StackUnexpected_Validation_TailByRef() cil managed
    {
        // Validation-only mode accepts a managed pointer for a native int argument; it is still a managed pointer.
        .locals init (int32 V_0)
        ldloca.s V_0
        tail. call void ValidationOnlyTestsType::TakesPointer(void*)
        ret
    }

    .method public hidebysig static void TailCall.CalliLocalAddressAsNativeInt_Invalid_Unverifiable.StackUnexpected_Validation_TailByRef() cil managed
    {
        .locals init (int32 V_0)
        ldloca.s V_0
        ldftn void ValidationOnlyTestsType::TakesPointer(void*)
        tail. calli void(void*)
        ret
    }

    .method public hidebysig static void TailCall.ByRefArgument_Invalid_TailByRef(int32& r) cil managed
    {
        ldarg.0
        tail. call void ValidationOnlyTestsType::TakesRef(int32&)
        ret
    }

    .method public hidebysig static void TailCall.ByRefThis_Invalid_TailByRef(valuetype ValidationOnlyStruct& s) cil managed
    {
        ldarg.0
        tail. call instance void ValidationOnlyStruct::M()
        ret
    }

    .method public hidebysig static string TailCall.Constrained_Invalid_Constrained.TailByRef<T>(!!T& t) cil managed
    {
        ldarg.0
        constrained. !!T
        tail. callvirt instance string [System.Runtime]System.Object::ToString()
        ret
    }

    // initobj: only verification checks the type pointed to (III.4.5)

    .method public hidebysig static void InitObj.PointeeMismatch_Invalid_StackUnexpected_Validation_Valid() cil managed
    {
        .locals init (int32 V_0)
        ldloca.s V_0
        initobj [System.Runtime]System.Int64
        ret
    }

    .method public hidebysig static void InitObj.ReadOnlyAddress_Invalid_StackUnexpected(int64[] a) cil managed
    {
        // A controlled-mutability managed pointer cannot be the destination of initobj (III.1.8.1.2.2).
        ldarg.0
        ldc.i4.0
        readonly.
        ldelema [System.Runtime]System.Int64
        initobj [System.Runtime]System.Int64
        ret
    }

    .method public hidebysig static void InitObj.UnmanagedPointer_Invalid_StackByRef.StackUnexpected_Validation_Valid(native int p) cil managed
    {
        ldarg.0
        initobj [System.Runtime]System.Int32
        ret
    }

    .method public hidebysig static void InitObj.Int32Address_Invalid_StackByRef.StackUnexpected_Validation_StackByRef() cil managed
    {
        ldc.i4.0
        initobj [System.Runtime]System.Int32
        ret
    }

    // stloc: only the stack type is a validity rule (III.3.63); a native int is valid as a byref

    .method public hidebysig static void StoreLocal.NativeIntToByRef_Invalid_StackUnexpected_Validation_Valid(native int p) cil managed
    {
        .locals init (int32& V_0)
        ldarg.0
        stloc.0
        ret
    }

    .method public hidebysig static void StoreLocal.ByRefToOtherByRef_Invalid_StackUnexpected_Validation_Valid(int32& r) cil managed
    {
        .locals init (int64& V_0)
        ldarg.0
        stloc.0
        ret
    }

    .method public hidebysig static void StoreLocal.Int32ToObject_Invalid_StackUnexpected() cil managed
    {
        .locals init (object V_0)
        ldc.i4.0
        stloc.0
        ret
    }

    .method public hidebysig static void StoreLocal.ReadOnlyByRef_Invalid_StackUnexpected(object[] a) cil managed
    {
        .locals init (object& V_0)
        ldarg.0
        ldc.i4.0
        readonly.
        ldelema [System.Runtime]System.Object
        stloc.0
        ret
    }

    // starg: only the stack type is checked, and a native int is valid as a byref (III.3.61)

    .method public hidebysig static void StoreArgument.NativeIntToByRef_Invalid_StackUnexpected_Validation_Valid(int32& r, native int p) cil managed
    {
        ldarg.1
        starg.s r
        ret
    }

    .method public hidebysig static void StoreArgument.ObjectToString_Invalid_StackUnexpected_Validation_Valid(string s, object o) cil managed
    {
        ldarg.1
        starg.s s
        ret
    }

    .method public hidebysig static void StoreArgument.Int32ToString_Invalid_StackUnexpected(string s) cil managed
    {
        ldc.i4.0
        starg.s s
        ret
    }

    // Calls: correct IL also passes a native int as a byref (III.1.6)

    .method public hidebysig static void Call.NativeIntForByRef_Invalid_StackUnexpected_Validation_Valid(native int p) cil managed
    {
        ldarg.0
        call void ValidationOnlyTestsType::TakesRef(int32&)
        ret
    }

    .method public hidebysig static void Call.NativeIntForValueTypeThis_Invalid_StackUnexpected_Validation_Valid(native int p) cil managed
    {
        ldarg.0
        call instance void ValidationOnlyStruct::M()
        ret
    }

    .method public hidebysig static void Calli.NativeIntForByRef_Invalid_Unverifiable.StackUnexpected_Validation_Valid(native int p) cil managed
    {
        ldarg.0
        ldftn void ValidationOnlyTestsType::TakesRef(int32&)
        calli void(int32&)
        ret
    }

    // ret: only the stack type is a validity rule (III.3.57)

    .method public hidebysig static int64& Return.ByRefToOtherByRef_Invalid_StackUnexpected_Validation_Valid() cil managed
    {
        ldsflda int32 ValidationOnlyTestsType::StaticField
        ret
    }

    .method public hidebysig static int32& Return.NativeIntForByRef_Invalid_StackUnexpected.ReturnPtrToStack_Validation_Valid(native int p) cil managed
    {
        ldarg.0
        ret
    }

    // Merging managed pointers to different types (I.12.3.2.1, III.1.8.1.3)

    .method public hidebysig static void Merge.ByRefsToDifferentTypes_Invalid_PathStackUnexpected_Validation_Valid(bool c, int32& a, int64& b) cil managed
    {
        ldarg.0
        brfalse.s OTHER
        ldarg.1
        br.s JOIN
    OTHER:
        ldarg.2
    JOIN:
        pop
        ret
    }

    .method public hidebysig static void Merge.WritableThenReadOnly_Invalid_PathStackUnexpected_Validation_ReadOnlyIllegalWrite(bool c, int32& p, int64[] a) cil managed
    {
        // The writable int32& reaches the join first; the readonly int64& makes the merged value readonly.
        ldarg.0
        brfalse.s OTHER
        ldarg.1
        br.s JOIN
    OTHER:
        ldarg.2
        ldc.i4.0
        readonly.
        ldelema [System.Runtime]System.Int64
    JOIN:
        ldc.i4.0
        stind.i4
        ret
    }

    .method public hidebysig static void Merge.ReadOnlyThenWritable_Invalid_ReadOnlyIllegalWrite.StackUnexpected.PathStackUnexpected_Validation_ReadOnlyIllegalWrite(bool c, int32& p, int64[] a) cil managed
    {
        // The readonly int64& reaches the join first and stays readonly.
        ldarg.0
        brfalse.s OTHER
        ldarg.2
        ldc.i4.0
        readonly.
        ldelema [System.Runtime]System.Int64
        br.s JOIN
    OTHER:
        ldarg.1
    JOIN:
        ldc.i4.0
        stind.i4
        ret
    }

    .method public hidebysig static void Merge.NativeIntThenReadOnly_Invalid_StackByRef_Validation_PathStackUnexpected(bool c, native int p, int64[] a) cil managed
    {
        // A readonly & does not merge with a native int.
        ldarg.0
        brfalse.s OTHER
        ldarg.1
        br.s JOIN
    OTHER:
        ldarg.2
        ldc.i4.0
        readonly.
        ldelema [System.Runtime]System.Int64
    JOIN:
        ldc.i4.0
        stind.i4
        ret
    }

    .method public hidebysig static void Merge.ReadOnlyThenNativeInt_Invalid_ReadOnlyIllegalWrite.StackUnexpected.PathStackUnexpected_Validation_ReadOnlyIllegalWrite.PathStackUnexpected(bool c, native int p, int64[] a) cil managed
    {
        // A readonly & does not merge with a native int.
        ldarg.0
        brfalse.s OTHER
        ldarg.2
        ldc.i4.0
        readonly.
        ldelema [System.Runtime]System.Int64
        br.s JOIN
    OTHER:
        ldarg.1
    JOIN:
        ldc.i4.0
        stind.i4
        ret
    }

    .method public hidebysig static void Merge.DifferentValueTypes_Invalid_PathStackUnexpected(bool c, valuetype ValidationOnlyStruct a, valuetype ValidationOnlyOtherStruct b) cil managed
    {
        ldarg.0
        brfalse.s OTHER
        ldarg.1
        br.s JOIN
    OTHER:
        ldarg.2
    JOIN:
        pop
        ret
    }

    // Managed pointer arithmetic: shaded cells of Tables III.2 and III.7 are valid, x cells are not

    .method public hidebysig static void Arithmetic.AddByRefInt32_Invalid_ExpectedNumericType_Validation_Valid(int32& a) cil managed
    {
        ldarg.0
        ldc.i4.4
        add
        pop
        ret
    }

    .method public hidebysig static void Arithmetic.AddInt32ByRef_Invalid_ExpectedNumericType_Validation_Valid(int32& a) cil managed
    {
        ldc.i4.4
        ldarg.0
        add
        pop
        ret
    }

    .method public hidebysig static void Arithmetic.SubByRefNativeInt_Invalid_ExpectedNumericType_Validation_Valid(int32& a, native int n) cil managed
    {
        ldarg.0
        ldarg.1
        sub
        pop
        ret
    }

    .method public hidebysig static void Arithmetic.SubByRefByRef_Invalid_ExpectedNumericType_Validation_Valid(int32& a, int32& b) cil managed
    {
        ldarg.0
        ldarg.1
        sub
        pop
        ret
    }

    .method public hidebysig static void Arithmetic.AddOvfUnByRefNativeInt_Invalid_ExpectedIntegerType_Validation_Valid(int32& a, native int n) cil managed
    {
        ldarg.0
        ldarg.1
        add.ovf.un
        pop
        ret
    }

    .method public hidebysig static void Arithmetic.AddOvfUnInt32ByRef_Invalid_ExpectedIntegerType_Validation_Valid(int32& a) cil managed
    {
        ldc.i4.4
        ldarg.0
        add.ovf.un
        pop
        ret
    }

    .method public hidebysig static void Arithmetic.SubOvfUnByRefByRef_Invalid_ExpectedIntegerType_Validation_Valid(int32& a, int32& b) cil managed
    {
        ldarg.0
        ldarg.1
        sub.ovf.un
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

    .method public hidebysig static void Arithmetic.AddResultIsByRef_Invalid_ExpectedNumericType_Validation_Valid(bool c, int32& a) cil managed
    {
        // The sum merges with a managed pointer, so it must be one.
        ldarg.0
        brfalse.s OTHER
        ldarg.1
        ldc.i4.4
        add
        br.s JOIN
    OTHER:
        ldarg.1
    JOIN:
        pop
        ret
    }

    .method public hidebysig static void Arithmetic.SubResultIsNativeInt_Invalid_ExpectedNumericType_Validation_Valid(bool c, int32& a, int32& b, native int n) cil managed
    {
        // The difference merges with a native int, so it must be one.
        ldarg.0
        brfalse.s OTHER
        ldarg.1
        ldarg.2
        sub
        br.s JOIN
    OTHER:
        ldarg.3
    JOIN:
        pop
        ret
    }

    .method public hidebysig static void Arithmetic.AddInt64ByRef_Invalid_ExpectedNumericType(int32& a) cil managed
    {
        ldc.i8 4
        ldarg.0
        add
        pop
        ret
    }

    .method public hidebysig static void Arithmetic.AddByRefByRef_Invalid_ExpectedNumericType(int32& a, int32& b) cil managed
    {
        ldarg.0
        ldarg.1
        add
        pop
        ret
    }

    .method public hidebysig static void Arithmetic.MulByRefInt32_Invalid_ExpectedNumericType(int32& a) cil managed
    {
        ldarg.0
        ldc.i4.4
        mul
        pop
        ret
    }

    .method public hidebysig static void Arithmetic.SubInt32ByRef_Invalid_ExpectedNumericType(int32& a) cil managed
    {
        ldc.i4.4
        ldarg.0
        sub
        pop
        ret
    }

    .method public hidebysig static void Arithmetic.AddOvfByRefInt32_Invalid_ExpectedIntegerType(int32& a) cil managed
    {
        ldarg.0
        ldc.i4.4
        add.ovf
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

    // Conversions that stop GC tracking: shaded cells of Table III.8 are valid, x cells are not

    .method public hidebysig static void Convert.ByRefToNativeUInt_Invalid_ExpectedNumericType_Validation_Valid(int32& a) cil managed
    {
        ldarg.0
        conv.u
        pop
        ret
    }

    .method public hidebysig static void Convert.ByRefToNativeInt_Invalid_ExpectedNumericType_Validation_Valid(int32& a) cil managed
    {
        ldarg.0
        conv.i
        pop
        ret
    }

    .method public hidebysig static void Convert.ObjectToInt64_Invalid_ExpectedNumericType_Validation_Valid(object o) cil managed
    {
        ldarg.0
        conv.i8
        pop
        ret
    }

    .method public hidebysig static void Convert.ObjectToUInt64_Invalid_ExpectedNumericType_Validation_Valid(object o) cil managed
    {
        ldarg.0
        conv.u8
        pop
        ret
    }

    .method public hidebysig static void Convert.ByRefToInt32_Invalid_ExpectedNumericType(int32& a) cil managed
    {
        ldarg.0
        conv.i4
        pop
        ret
    }

    .method public hidebysig static void Convert.ObjectToFloat64_Invalid_ExpectedNumericType(object o) cil managed
    {
        ldarg.0
        conv.r8
        pop
        ret
    }

    .method public hidebysig static void Convert.ReadOnlyByRefToNativeUInt_Invalid_ExpectedNumericType(int32[] a) cil managed
    {
        ldarg.0
        ldc.i4.0
        readonly.
        ldelema [System.Runtime]System.Int32
        conv.u
        pop
        ret
    }

    // Fields through an unmanaged pointer (III.4.10, III.4.11, III.4.28)

    .method public hidebysig static void LoadField.UnmanagedPointer_Invalid_StackUnexpected_Validation_Valid(native int p) cil managed
    {
        ldarg.0
        ldfld int32 ValidationOnlyStruct::F
        pop
        ret
    }

    .method public hidebysig static void LoadField.WrongClass_Invalid_StackUnexpected_Validation_Valid(string s) cil managed
    {
        ldarg.0
        ldfld int32 ValidationOnlyClass::F
        pop
        ret
    }

    .method public hidebysig static void LoadFieldAddress.UnmanagedPointer_Invalid_StackUnexpected.StackUnexpected_Validation_Valid(native int p) cil managed
    {
        // Through an unmanaged pointer, ldflda pushes an unmanaged pointer.
        .locals init (native int V_0)
        ldarg.0
        ldflda int32 ValidationOnlyStruct::F
        stloc.0
        ret
    }

    .method public hidebysig static void LoadFieldAddress.WrongClass_Invalid_StackUnexpected_Validation_Valid(string s) cil managed
    {
        ldarg.0
        ldflda int32 ValidationOnlyClass::F
        pop
        ret
    }

    .method public hidebysig static void StoreField.UnmanagedPointer_Invalid_StackUnexpected_Validation_Valid(native int p) cil managed
    {
        ldarg.0
        ldc.i4.0
        stfld int32 ValidationOnlyStruct::F
        ret
    }

    .method public hidebysig static void StoreField.WrongClass_Invalid_StackUnexpected_Validation_Valid(string s) cil managed
    {
        ldarg.0
        ldc.i4.0
        stfld int32 ValidationOnlyClass::F
        ret
    }

    .method public hidebysig static void StoreField.NativeIntToByRefField_Invalid_StackUnexpected_Validation_Valid(valuetype ValidationOnlyRefStruct& r, native int p) cil managed
    {
        ldarg.0
        ldarg.1
        stfld int32& ValidationOnlyRefStruct::P
        ret
    }

    .method public hidebysig static void StoreField.ObjectToString_Invalid_StackUnexpected_Validation_Valid(class ValidationOnlyClass c, object o) cil managed
    {
        ldarg.0
        ldarg.1
        stfld string ValidationOnlyClass::S
        ret
    }

    .method public hidebysig static void StoreField.Int32ToString_Invalid_StackUnexpected(class ValidationOnlyClass c) cil managed
    {
        ldarg.0
        ldc.i4.0
        stfld string ValidationOnlyClass::S
        ret
    }

    .method public hidebysig static void LoadField.Int32Object_Invalid_StackUnexpected(int32 i) cil managed
    {
        ldarg.0
        ldfld int32 ValidationOnlyClass::F
        pop
        ret
    }

    .method public hidebysig static void LoadFieldAddress.Int32Object_Invalid_StackUnexpected(int32 i) cil managed
    {
        ldarg.0
        ldflda int32 ValidationOnlyClass::F
        pop
        ret
    }

    .method public hidebysig static void StoreField.Int32Object_Invalid_StackUnexpected(int32 i) cil managed
    {
        ldarg.0
        ldc.i4.0
        stfld int32 ValidationOnlyClass::F
        ret
    }

    // Indirect loads and stores through an unmanaged pointer (III.3.42, III.3.62, III.4.13, III.4.29)

    .method public hidebysig static void LoadIndirect.UnmanagedPointer_Invalid_StackByRef_Validation_Valid(native int p) cil managed
    {
        ldarg.0
        ldind.i4
        pop
        ret
    }

    .method public hidebysig static void LoadObject.UnmanagedPointer_Invalid_StackByRef_Validation_Valid(native int p) cil managed
    {
        ldarg.0
        ldobj [System.Runtime]System.Int64
        pop
        ret
    }

    .method public hidebysig static void LoadIndirectRef.UnmanagedPointer_Invalid_StackByRef_Validation_Valid(native int p) cil managed
    {
        // The object's class is unknown, so it can be passed as a string.
        ldarg.0
        ldind.ref
        call void ValidationOnlyTestsType::TakesString(string)
        ret
    }

    .method public hidebysig static void LoadIndirect.ObjectAddress_Invalid_StackByRef(object o) cil managed
    {
        ldarg.0
        ldind.i4
        pop
        ret
    }

    .method public hidebysig static void LoadObject.PointeeMismatch_Invalid_StackUnexpected_Validation_Valid(int64& a) cil managed
    {
        ldarg.0
        ldobj [System.Runtime]System.Int32
        pop
        ret
    }

    .method public hidebysig static void StoreIndirect.UnmanagedPointer_Invalid_StackByRef_Validation_Valid(native int p) cil managed
    {
        ldarg.0
        ldc.i4.0
        stind.i4
        ret
    }

    .method public hidebysig static void StoreIndirectRef.UnmanagedPointer_Invalid_StackByRef_Validation_Valid(native int p) cil managed
    {
        ldarg.0
        ldstr "a"
        stind.ref
        ret
    }

    .method public hidebysig static void StoreIndirectRef.UnmanagedPointerInt32_Invalid_StackByRef_Validation_StackObjRef(native int p) cil managed
    {
        ldarg.0
        ldc.i4.0
        stind.ref
        ret
    }

    .method public hidebysig static void StoreIndirect.UnmanagedPointerObject_Invalid_StackByRef_Validation_StackUnexpected(native int p, object o) cil managed
    {
        ldarg.0
        ldarg.1
        stind.i4
        ret
    }

    .method public hidebysig static void StoreIndirect.ObjectAddress_Invalid_StackByRef(object o) cil managed
    {
        ldarg.0
        ldc.i4.0
        stind.i4
        ret
    }

    // cpobj and mkrefany with an unmanaged pointer (III.4.4, III.4.19)

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

    .method public hidebysig static void CopyObject.Int32Source_Invalid_StackByRef(int32& d) cil managed
    {
        ldarg.0
        ldc.i4.0
        cpobj [System.Runtime]System.Int32
        ret
    }

    .method public hidebysig static void CopyObject.Int32Destination_Invalid_StackByRef(int32& s) cil managed
    {
        ldc.i4.0
        ldarg.0
        cpobj [System.Runtime]System.Int32
        ret
    }

    .method public hidebysig static void Mkrefany.Int32Value_Invalid_StackByRef() cil managed
    {
        ldc.i4.0
        mkrefany [System.Runtime]System.Int32
        pop
        ret
    }
}

.class public abstract auto ansi beforefieldinit ValidationOnlyAbstract
       extends [System.Runtime]System.Object
{
    .method public hidebysig newslot abstract virtual instance void M() cil managed
    {
    }

    .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
    {
        ldarg.0
        call instance void [System.Runtime]System.Object::.ctor()
        ret
    }
}

.class public auto ansi sealed ValidationOnlyThreeParameterDelegate
       extends [System.Runtime]System.MulticastDelegate
{
    .method public hidebysig specialname rtspecialname instance void .ctor(object o, native int m, int32 x) runtime managed
    {
    }

    .method public hidebysig newslot virtual instance void Invoke() runtime managed
    {
    }
}

.class public auto ansi sealed ValidationOnlyInt32Delegate
       extends [System.Runtime]System.MulticastDelegate
{
    .method public hidebysig specialname rtspecialname instance void .ctor(object o, int32 m) runtime managed
    {
    }

    .method public hidebysig newslot virtual instance void Invoke() runtime managed
    {
    }
}

.class public auto ansi sealed ValidationOnlyInt32ObjectDelegate
       extends [System.Runtime]System.MulticastDelegate
{
    .method public hidebysig specialname rtspecialname instance void .ctor(int32 o, native int m) runtime managed
    {
    }

    .method public hidebysig newslot virtual instance void Invoke() runtime managed
    {
    }
}

.class public auto ansi sealed ValidationOnlyStringObjectDelegate
       extends [System.Runtime]System.MulticastDelegate
{
    .method public hidebysig specialname rtspecialname instance void .ctor(string o, native int m) runtime managed
    {
    }

    .method public hidebysig newslot virtual instance class [System.Runtime]System.Type Invoke() runtime managed
    {
    }
}

.class public auto ansi beforefieldinit ValidationOnlyPrivate
       extends [System.Runtime]System.Object
{
    .field private static int32 s_private
    .field private int32 _private
}

// Rules that valid IL must follow and that no other test reached in validation-only mode. Each method reaches
// one such rule (some also reach others on the way); validation-only mode reports what verification reports,
// minus the rules that only verifiable IL must follow.
.class public auto ansi beforefieldinit ValidationOnlyKeptTestsType
       extends [System.Runtime]System.Object
{
    .field public int32 InstanceField

    .method public hidebysig static void Target() cil managed
    {
        ret
    }

    .method public hidebysig static void TakesInt32(int32 i) cil managed
    {
        ret
    }

    .method public hidebysig static string ReturnsString() cil managed
    {
        ldnull
        ret
    }

    .method public hidebysig static void StructOnly<valuetype .ctor ([System.Runtime]System.ValueType) T>() cil managed
    {
        ret
    }

    // Stack, tokens and the instruction stream

    .method public hidebysig static void Kept.StackOverflow_Invalid_StackOverflow() cil managed
    {
        .maxstack 1
        ldc.i4.0
        ldc.i4.0
        pop
        pop
        ret
    }

    .method public hidebysig static void Kept.ArgumentNumber_Invalid_UnrecognizedArgumentNumber(int32 a) cil managed
    {
        // ldarg.s 5
        .emitbyte 0x0E
        .emitbyte 0x05
        pop
        ret
    }

    .method public hidebysig static void Kept.LocalNumber_Invalid_UnrecognizedLocalNumber() cil managed
    {
        .locals init (int32 V_0)
        // ldloc.s 5
        .emitbyte 0x11
        .emitbyte 0x05
        pop
        ret
    }

    .method public hidebysig static void Kept.TokenResolve_Invalid_TokenResolve() cil managed
    {
        // ldtoken 0x0100FFFF, a TypeRef row that does not exist
        .emitbyte 0xD0
        .emitbyte 0xFF
        .emitbyte 0xFF
        .emitbyte 0x00
        .emitbyte 0x01
        pop
        ret
    }

    .method public hidebysig static void Kept.TypeTokenIsMethod_Invalid_ExpectedTypeToken(object o) cil managed
    {
        ldarg.0
        // castclass 0x06000001 (a method)
        .emitbyte 0x74
        .emitbyte 0x01
        .emitbyte 0x00
        .emitbyte 0x00
        .emitbyte 0x06
        pop
        ret
    }

    .method public hidebysig static void Kept.FieldTokenIsMethod_Invalid_ExpectedFieldToken() cil managed
    {
        // ldsfld 0x06000001 (a method)
        .emitbyte 0x7E
        .emitbyte 0x01
        .emitbyte 0x00
        .emitbyte 0x00
        .emitbyte 0x06
        pop
        ret
    }

    .method public hidebysig static void Kept.MethodTokenIsField_Invalid_ExpectedMethodToken() cil managed
    {
        // call 0x04000001 (a field)
        .emitbyte 0x28
        .emitbyte 0x01
        .emitbyte 0x00
        .emitbyte 0x00
        .emitbyte 0x04
        ret
    }

    .method public hidebysig static void Kept.StringTokenIsMethod_Invalid_StringOperand() cil managed
    {
        // ldstr 0x06000001 (a method)
        .emitbyte 0x72
        .emitbyte 0x01
        .emitbyte 0x00
        .emitbyte 0x00
        .emitbyte 0x06
        pop
        ret
    }

    .method public hidebysig static void Kept.MethodEndsInsideInstruction_Invalid_MethodEnd() cil managed
    {
        ret
        // ldc.i4 without its operand
        .emitbyte 0x20
    }

    .method public hidebysig static void Kept.UnknownOpcode_Invalid_UnknownOpcode() cil managed
    {
        .emitbyte 0xA6
        ret
    }

    .method public hidebysig static void Kept.UnalignedPrefixOnNop_Invalid_Unaligned() cil managed
    {
        unaligned. 1
        nop
        ret
    }

    .method public hidebysig static void Kept.VolatilePrefixOnNop_Invalid_Volatile() cil managed
    {
        volatile.
        nop
        ret
    }

    .method public hidebysig static void Kept.PathStackDepth_Invalid_PathStackDepth(bool c) cil managed
    {
        ldc.i4.0
        ldarg.0
        brfalse.s JOIN
        ldc.i4.0
    JOIN:
        pop
        ret
    }

    // Arguments, locals, calls and fields

    .method public hidebysig static void Kept.ByrefOfByref_Invalid_ByrefOfByref(int32& r) cil managed
    {
        ldarga.s r
        pop
        ret
    }

    .method public hidebysig static void Kept.CallVirtOnValueType_Invalid_CallVirtOnValueType() cil managed
    {
        .locals init (valuetype ValidationOnlyStruct V_0)
        ldloca.s V_0
        callvirt instance void ValidationOnlyStruct::M()
        ret
    }

    .method public hidebysig static void Kept.CallAbstract_Invalid_CallAbstract.ThisMismatch_Validation_CallAbstract(class ValidationOnlyAbstract a) cil managed
    {
        ldarg.0
        call instance void ValidationOnlyAbstract::M()
        ret
    }

    .method public hidebysig static void Kept.NewobjAbstractClass_Invalid_NewobjAbstractClass() cil managed
    {
        newobj instance void ValidationOnlyAbstract::.ctor()
        pop
        ret
    }

    .method public hidebysig static void Kept.NewobjArrayOfByRefLike_Invalid_ArrayByRef() cil managed
    {
        ldc.i4.1
        newobj instance void valuetype ValidationOnlyRefStruct[]::.ctor(int32)
        pop
        ret
    }

    .method public hidebysig static void Kept.CallCtorOnOtherObject_Invalid_CallCtor(object o) cil managed
    {
        ldarg.0
        call instance void [System.Runtime]System.Object::.ctor()
        ret
    }

    .method public hidebysig static string Kept.ConstrainedObjectThis_Invalid_ConstrainedCallWithNonByRefThis(object o) cil managed
    {
        ldarg.0
        constrained. [System.Runtime]System.Object
        callvirt instance string [System.Runtime]System.Object::ToString()
        ret
    }

    .method public hidebysig static void Kept.UnsatisfiedMethodInst_Invalid_UnsatisfiedMethodInst() cil managed
    {
        call void ValidationOnlyKeptTestsType::StructOnly<string>()
        ret
    }

    .method public hidebysig static void Kept.DelegateCtorThreeParameters_Invalid_DelegateCtor() cil managed
    {
        // ILVerify pops the two arguments of a delegate constructor and leaves the third, int32, on the stack.
        ldc.i4.0
        ldnull
        ldftn void ValidationOnlyKeptTestsType::Target()
        newobj instance void ValidationOnlyThreeParameterDelegate::.ctor(object, native int, int32)
        pop
        pop
        ret
    }

    .method public hidebysig static void Kept.DelegateCtorInt32Object_Invalid_StackUnexpected_Validation_DelegateCtorSigO.StackUnexpected() cil managed
    {
        ldnull
        ldftn void ValidationOnlyKeptTestsType::Target()
        newobj instance void ValidationOnlyInt32ObjectDelegate::.ctor(int32, native int)
        pop
        ret
    }

    .method public hidebysig static void Kept.DelegateCtorUnboxedTarget_Invalid_StackUnexpected.DelegateCtorSigO.DelegateCtor() cil managed
    {
        .locals init (valuetype ValidationOnlyStruct V_0)
        ldloc.0
        ldftn instance void ValidationOnlyStruct::M()
        newobj instance void [System.Runtime]System.Action::.ctor(object, native int)
        pop
        ret
    }

    .method public hidebysig static void Kept.DelegateCtorStringObject_Invalid_StackUnexpected_Validation_DelegateCtorSigO() cil managed
    {
        // A delegate constructor whose first parameter is not System.Object (II.14.6).
        newobj instance void [System.Runtime]System.Object::.ctor()
        ldftn instance class [System.Runtime]System.Type [System.Runtime]System.Object::GetType()
        newobj instance void ValidationOnlyStringObjectDelegate::.ctor(string, native int)
        pop
        ret
    }

    .method public hidebysig static void Kept.DelegateCtorNullMethodPointer_Invalid_StackMethod.DelegatePattern_Validation_StackUnexpected() cil managed
    {
        ldnull
        ldnull
        newobj instance void [System.Runtime]System.Action::.ctor(object, native int)
        pop
        ret
    }

    .method public hidebysig static void Kept.DelegateCtorFloatMethodPointer_Invalid_StackMethod.DelegatePattern_Validation_StackUnexpected() cil managed
    {
        ldnull
        ldc.r8 1
        newobj instance void [System.Runtime]System.Action::.ctor(object, native int)
        pop
        ret
    }

    .method public hidebysig static void Kept.DelegateCtorInt32Method_Invalid_DelegateCtorSigI() cil managed
    {
        ldnull
        ldftn void ValidationOnlyKeptTestsType::Target()
        newobj instance void ValidationOnlyInt32Delegate::.ctor(object, int32)
        pop
        ret
    }

    .method public hidebysig static void Kept.LdftnCtor_Invalid_LdftnCtor() cil managed
    {
        ldftn instance void [System.Runtime]System.Object::.ctor()
        pop
        ret
    }

    .method public hidebysig static int32 Kept.TailCallReturnType_Invalid_TailRetType.StackUnexpected() cil managed
    {
        tail. call string ValidationOnlyKeptTestsType::ReturnsString()
        ret
    }

    .method public hidebysig static void Kept.TailCallStackNotEmpty_Invalid_TailStackEmpty.ReturnVoid() cil managed
    {
        ldc.i4.0
        ldc.i4.0
        tail. call void ValidationOnlyKeptTestsType::TakesInt32(int32)
        ret
    }

    .method public hidebysig static void Kept.LoadStaticFromInstanceField_Invalid_ExpectedStaticField() cil managed
    {
        ldsfld int32 ValidationOnlyKeptTestsType::InstanceField
        pop
        ret
    }

    .method public hidebysig static void Kept.LoadStaticAddressOfInstanceField_Invalid_ExpectedStaticField() cil managed
    {
        ldsflda int32 ValidationOnlyKeptTestsType::InstanceField
        pop
        ret
    }

    .method public hidebysig static void Kept.StoreStaticToInstanceField_Invalid_ExpectedStaticField() cil managed
    {
        ldc.i4.0
        stsfld int32 ValidationOnlyKeptTestsType::InstanceField
        ret
    }

    .method public hidebysig static void Kept.LoadAddressOfPrivateField_Invalid_FieldAccess() cil managed
    {
        ldsflda int32 ValidationOnlyPrivate::s_private
        pop
        ret
    }

    .method public hidebysig static void Kept.StorePrivateField_Invalid_FieldAccess() cil managed
    {
        ldc.i4.0
        stsfld int32 ValidationOnlyPrivate::s_private
        ret
    }

    .method public hidebysig static void Kept.StoreIndirectRefNativeInt_Invalid_StackUnexpected_Validation_StackObjRef(valuetype ValidationOnlyRefStruct& r, native int p) cil managed
    {
        // stind.ref stores an object reference (III.3.62)
        ldarg.0
        ldflda int32& ValidationOnlyRefStruct::P
        ldarg.1
        stind.ref
        ret
    }

    .method public hidebysig static void Kept.StoreThroughReadOnlyByRef_Invalid_ReadOnlyIllegalWrite(int32[] a) cil managed
    {
        ldarg.0
        ldc.i4.0
        readonly.
        ldelema [System.Runtime]System.Int32
        ldc.i4.0
        stind.i4
        ret
    }

    // Operands of single instructions

    .method public hidebysig static void Kept.BranchOnFloat_Invalid_StackUnexpected() cil managed
    {
        ldc.r8 1
        brtrue.s END
    END:
        ret
    }

    .method public hidebysig static void Kept.BranchInt32Int64_Invalid_StackUnexpected() cil managed
    {
        ldc.i4.0
        ldc.i8 0
        beq.s END
    END:
        ret
    }

    .method public hidebysig static void Kept.CompareInt32Int64_Invalid_StackUnexpected() cil managed
    {
        ldc.i4.0
        ldc.i8 0
        ceq
        pop
        ret
    }

    .method public hidebysig static void Kept.AddInt32Int64_Invalid_StackUnexpected() cil managed
    {
        ldc.i4.0
        ldc.i8 0
        add
        pop
        ret
    }

    .method public hidebysig static void Kept.NegateObject_Invalid_ExpectedNumericType(object o) cil managed
    {
        ldarg.0
        neg
        pop
        ret
    }

    .method public hidebysig static void Kept.NotFloat_Invalid_ExpectedIntegerType() cil managed
    {
        ldc.r8 1
        not
        pop
        ret
    }

    .method public hidebysig static void Kept.ThrowInt32_Invalid_StackObjRef.ThrowOrCatchOnlyExceptionType() cil managed
    {
        ldc.i4.0
        throw
    }

    .method public hidebysig static void Kept.BoxNullAsInt32_Invalid_StackUnexpected() cil managed
    {
        ldnull
        box [System.Runtime]System.Int32
        pop
        ret
    }

    .method public hidebysig static void Kept.UnboxInt32_Invalid_StackObjRef() cil managed
    {
        ldc.i4.0
        unbox.any [System.Runtime]System.Int32
        pop
        ret
    }

    .method public hidebysig static void Kept.UnboxReferenceType_Invalid_ValueTypeExpected(object o) cil managed
    {
        ldarg.0
        unbox [System.Runtime]System.String
        pop
        ret
    }

    .method public hidebysig static void Kept.CkfiniteInt32_Invalid_ExpectedFloatType() cil managed
    {
        ldc.i4.0
        ckfinite
        pop
        ret
    }

    .method public hidebysig static void Kept.LocallocFloatSize_Invalid_Unverifiable.ExpectedIntegerType_Validation_ExpectedIntegerType() cil managed
    {
        ldc.r8 1
        localloc
        pop
        ret
    }

    .method public hidebysig static void Kept.CopyBlockFloatSize_Invalid_Unverifiable.ExpectedIntegerType_Validation_ExpectedIntegerType(native int d, native int s) cil managed
    {
        ldarg.0
        ldarg.1
        ldc.r8 1
        cpblk
        ret
    }

    .method public hidebysig static void Kept.InitBlockFloatSize_Invalid_Unverifiable.ExpectedIntegerType_Validation_ExpectedIntegerType(native int d) cil managed
    {
        ldarg.0
        ldc.i4.0
        ldc.r8 1
        initblk
        ret
    }

    .method public hidebysig static void Kept.InitBlockFloatValue_Invalid_Unverifiable.ExpectedIntegerType_Validation_ExpectedIntegerType(native int d) cil managed
    {
        ldarg.0
        ldc.r8 1
        ldc.i4.4
        initblk
        ret
    }

    // Arrays

    .method public hidebysig static void Kept.LoadElementInt64Index_Invalid_StackUnexpected(int32[] a) cil managed
    {
        ldarg.0
        ldc.i8 0
        ldelem.i4
        pop
        ret
    }

    .method public hidebysig static void Kept.LoadElementFromInt32_Invalid_ExpectedArray() cil managed
    {
        ldc.i4.0
        ldc.i4.0
        ldelem.i4
        pop
        ret
    }

    .method public hidebysig static void Kept.StoreElementInt64Index_Invalid_StackUnexpected(int32[] a) cil managed
    {
        ldarg.0
        ldc.i8 0
        ldc.i4.0
        stelem.i4
        ret
    }

    .method public hidebysig static void Kept.StoreElementToInt32_Invalid_ExpectedArray() cil managed
    {
        ldc.i4.0
        ldc.i4.0
        ldc.i4.0
        stelem.i4
        ret
    }

    .method public hidebysig static void Kept.StoreElementRefInt32_Invalid_StackUnexpected_Validation_StackObjRef(int32[] a) cil managed
    {
        // stelem.ref stores an object reference (III.4.26)
        ldarg.0
        ldc.i4.0
        ldc.i4.0
        stelem.ref
        ret
    }

    .method public hidebysig static void Kept.StoreElementObjectToInt32Array_Invalid_StackUnexpected(int32[] a, object o) cil managed
    {
        ldarg.0
        ldc.i4.0
        ldarg.1
        stelem.i4
        ret
    }

    .method public hidebysig static void Kept.LoadElementAddressInt64Index_Invalid_StackUnexpected(int32[] a) cil managed
    {
        ldarg.0
        ldc.i8 0
        ldelema [System.Runtime]System.Int32
        pop
        ret
    }

    .method public hidebysig static void Kept.LoadElementAddressFromInt32_Invalid_ExpectedArray() cil managed
    {
        ldc.i4.0
        ldc.i4.0
        ldelema [System.Runtime]System.Int32
        pop
        ret
    }

    .method public hidebysig static void Kept.LoadLengthOfObject_Invalid_ExpectedArray(object o) cil managed
    {
        ldarg.0
        ldlen
        pop
        ret
    }

    // Exception handling

    .method public hidebysig static void Kept.EndfinallyOutsideHandler_Invalid_Endfinally() cil managed
    {
        endfinally
    }

    .method public hidebysig static void Kept.EndfilterNotLast_Invalid_EHClauseFilterEnd.Endfilter() cil managed
    {
        .try
        {
            leave.s END
        }
        filter
        {
            pop
            ldc.i4.1
            endfilter
            ldc.i4.1
            endfilter
        }
        {
            pop
            leave.s END
        }
    END:
        ret
    }

    .method public hidebysig static void Kept.EndfilterObject_Invalid_StackUnexpected() cil managed
    {
        .try
        {
            leave.s END
        }
        filter
        {
            pop
            ldnull
            endfilter
        }
        {
            pop
            leave.s END
        }
    END:
        ret
    }

    .method public hidebysig static void Kept.LeaveIntoNestedTry_Invalid_LeaveIntoTry() cil managed
    {
        .try
        {
            leave.s INNER
            .try
            {
                nop
                .try
                {
                INNER:
                    leave.s END
                }
                finally
                {
                    endfinally
                }
                leave.s END
            }
            finally
            {
                endfinally
            }
        }
        finally
        {
            endfinally
        }
    END:
        ret
    }

    .method public hidebysig static void Kept.LeaveFromCatchIntoTry_Invalid_LeaveIntoTry.LeaveIntoTry() cil managed
    {
        .try
        {
            leave.s END
        }
        catch [System.Runtime]System.Object
        {
            pop
            leave.s MIDDLE
        }
        .try
        {
            nop
        MIDDLE:
            leave.s END
        }
        finally
        {
            endfinally
        }
    END:
        ret
    }

    .method public hidebysig static void Kept.FallthroughFromNestedTry_Invalid_FallthroughException() cil managed
    {
    OUTER_BEGIN:
        nop
    INNER_END:
        nop
        ldc.i4.0
        brtrue.s INNER_END
        leave.s END
    INNER_HANDLER:
        endfinally
    OUTER_END:
        endfinally
    OUTER_HANDLER_END:
    END:
        ret
        .try OUTER_BEGIN to INNER_END finally handler INNER_HANDLER to OUTER_END
        .try OUTER_BEGIN to OUTER_END finally handler OUTER_END to OUTER_HANDLER_END
    }

    .method public hidebysig static void Kept.FallthroughBetweenCatchHandlers_Invalid_FallthroughIntoHandler() cil managed
    {
    TRY1:
        leave.s END
    TRY2:
        leave.s END
    H1:
        pop
        nop
    H2:
        pop
        leave.s END
    H2_END:
    END:
        ret
        .try TRY1 to TRY2 catch [System.Runtime]System.Object handler H1 to H2
        .try TRY2 to H1 catch [System.Runtime]System.Object handler H2 to H2_END
    }

    .method public hidebysig static void Kept.BranchBetweenCatchHandlers_Invalid_BranchIntoHandler() cil managed
    {
    TRY1:
        leave.s END
    TRY2:
        leave.s END
    H1:
        pop
        br.s H2_MIDDLE
    H2:
        pop
    H2_MIDDLE:
        leave.s END
    H2_END:
    END:
        ret
        .try TRY1 to TRY2 catch [System.Runtime]System.Object handler H1 to H2
        .try TRY2 to H1 catch [System.Runtime]System.Object handler H2 to H2_END
    }

    .method public hidebysig static void Kept.FallthroughOutOfNestedCatch_Invalid_FallthroughException() cil managed
    {
    TRY_B:
        leave.s END
    HB:
        pop
    TRY_A:
        leave.s AFTER
    HA:
        pop
        nop
    AFTER:
        leave.s END
    HB_END:
    END:
        ret
        .try TRY_A to HA catch [System.Runtime]System.Object handler HA to AFTER
        .try TRY_B to HB catch [System.Runtime]System.Object handler HB to HB_END
    }

    .method public hidebysig static void Kept.BranchOutOfNestedFinally_Invalid_BranchOutOfFinally() cil managed
    {
    TRY_B:
        leave.s END
    HB:
        pop
    TRY_A:
        leave.s AFTER
    HA:
        br.s AFTER
    AFTER:
        leave.s END
    HB_END:
    END:
        ret
        .try TRY_A to HA finally handler HA to AFTER
        .try TRY_B to HB catch [System.Runtime]System.Object handler HB to HB_END
    }

    .method public hidebysig static void Kept.BranchOutOfNestedCatch_Invalid_BranchOutOfHandler() cil managed
    {
    TRY_B:
        leave.s END
    HB:
        pop
    TRY_A:
        leave.s AFTER
    HA:
        pop
        br.s AFTER
    AFTER:
        leave.s END
    HB_END:
    END:
        ret
        .try TRY_A to HA catch [System.Runtime]System.Object handler HA to AFTER
        .try TRY_B to HB catch [System.Runtime]System.Object handler HB to HB_END
    }

    .method public hidebysig static void Kept.BranchBetweenFilters_Invalid_BranchIntoFilter() cil managed
    {
    TRY1:
        leave.s END
    TRY2:
        leave.s END
    F1:
        pop
        ldc.i4.0
        brtrue.s F2_MIDDLE
        ldc.i4.1
        endfilter
    H1:
        pop
        leave.s END
    F2:
        pop
    F2_MIDDLE:
        ldc.i4.1
        endfilter
    H2:
        pop
        leave.s END
    H2_END:
    END:
        ret
        .try TRY1 to TRY2 filter F1 handler H1 to F2
        .try TRY2 to F1 filter F2 handler H2 to H2_END
    }

    .method public hidebysig static void Kept.BranchBackBetweenFilters_Invalid_BranchOutOfFilter() cil managed
    {
    TRY1:
        leave.s TRY2
    TRY2:
        leave.s END
    F1:
        pop
    F1_MIDDLE:
        ldc.i4.1
        endfilter
    H1:
        pop
        leave.s END
    F2:
        pop
        ldc.i4.0
        brtrue.s F1_MIDDLE
        ldc.i4.1
        endfilter
    H2:
        pop
        leave.s END
    H2_END:
    END:
        ret
        .try TRY1 to TRY2 filter F1 handler H1 to F2
        .try TRY2 to F1 filter F2 handler H2 to H2_END
    }
}
