// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Runtime { auto }
.assembly modifiers { }

.method private hidebysig static int32  Main() cil managed
{
  .entrypoint
  .locals init (valuetype [System.Runtime]System.RuntimeTypeHandle)

  ldtoken string[]
  stloc.0
  ldloca 0
  ldtoken string modopt (MyModifier)[]
  call instance bool valuetype [System.Runtime]System.RuntimeTypeHandle::Equals(valuetype [System.Runtime]System.RuntimeTypeHandle)
  brtrue StringArrayModifiedStringArrayOK
  ldc.i4.1
  ret
StringArrayModifiedStringArrayOK:

  ldtoken int32[]
  stloc.0
  ldloca 0
  ldtoken int32 modreq (MyModifier)[]
  call instance bool valuetype [System.Runtime]System.RuntimeTypeHandle::Equals(valuetype [System.Runtime]System.RuntimeTypeHandle)
  brtrue IntArrayModifiedIntArrayOK
  ldc.i4.2
  ret
IntArrayModifiedIntArrayOK:

  ldtoken int32 * modreq(MyModifier) []
  stloc.0
  ldloca 0
  ldtoken int32 modreq (MyModifier) * []
  call instance bool valuetype [System.Runtime]System.RuntimeTypeHandle::Equals(valuetype [System.Runtime]System.RuntimeTypeHandle)
  brtrue IntPtrArrayModifiedIntPtrArrayOK
  ldc.i4.3
  ret
IntPtrArrayModifiedIntPtrArrayOK:

  ldtoken uint32[]
  stloc.0
  ldloca 0
  ldtoken uint32 modreq (int32)[]
  call instance bool valuetype [System.Runtime]System.RuntimeTypeHandle::Equals(valuetype [System.Runtime]System.RuntimeTypeHandle)
  brtrue UIntArrayModifiedUIntArrayOK
  ldc.i4.4
  ret
UIntArrayModifiedUIntArrayOK:

  ldtoken valuetype MyStruct*
  stloc.0
  ldloca 0
  ldtoken valuetype MyStruct modreq (MyModifier) *
  call instance bool valuetype [System.Runtime]System.RuntimeTypeHandle::Equals(valuetype [System.Runtime]System.RuntimeTypeHandle)
  brtrue MyStructPtrModifiedMyStructPtrOK
  ldc.i4.5
  ret
MyStructPtrModifiedMyStructPtrOK:

  ldtoken method int32 *()
  stloc.0
  ldloca 0
  ldtoken method int32 modopt(MyModifier) *()

  call instance bool valuetype [System.Runtime]System.RuntimeTypeHandle::Equals(valuetype [System.Runtime]System.RuntimeTypeHandle)
  // All modifiers are considered as part of identity.
  brfalse IntReturnManagedFunctionPointerModifiedOK
  ldc.i4.6
  ret
IntReturnManagedFunctionPointerModifiedOK:

  ldtoken method unmanaged cdecl int32 modopt(MyModifier) *()
  stloc.0
  ldloca 0
  ldtoken method unmanaged cdecl int32 modopt(MyModifier) *()

  call instance bool valuetype [System.Runtime]System.RuntimeTypeHandle::Equals(valuetype [System.Runtime]System.RuntimeTypeHandle)
  // CallKind and modifiers are the same
  brtrue IntReturnUnmanagedFunctionPointerModifiedOK
  ldc.i4.7
  ret
IntReturnUnmanagedFunctionPointerModifiedOK:

  ldtoken method unmanaged cdecl int32 modopt([System.Runtime]System.Runtime.CompilerServices.CallConvStdcall) *()
  stloc.0
  ldloca 0
  ldtoken method unmanaged cdecl int32 modopt([System.Runtime]System.Runtime.CompilerServices.CallConvSuppressGCTransition) modopt([System.Runtime]System.Runtime.CompilerServices.CallConvStdcall) *()
  call instance bool valuetype [System.Runtime]System.RuntimeTypeHandle::Equals(valuetype [System.Runtime]System.RuntimeTypeHandle)
  brfalse IntReturnUnmanagedFunctionPointerWithCallConvModifiedOK
  ldc.i4.8
  ret

IntReturnUnmanagedFunctionPointerWithCallConvModifiedOK:
  ldc.i4 100
  ret
}

.class public MyModifier { }

.class value sequential public MyStruct { .size 1 }
