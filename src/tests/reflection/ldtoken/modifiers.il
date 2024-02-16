// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Runtime { auto }
.assembly extern xunit.core {}
.assembly modifiers { }

.class public auto ansi modifiers {
.method public hidebysig static int32  Main() cil managed
{
  .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
      01 00 00 00
  )
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
  brtrue ModifiersNotPartOfFunctionPointerIdentityOK
  ldc.i4.6
  ret
ModifiersNotPartOfFunctionPointerIdentityOK:

  ldtoken method unmanaged cdecl int32 *()
  stloc.0
  ldloca 0
  ldtoken method unmanaged stdcall int32 *()

  call instance bool valuetype [System.Runtime]System.RuntimeTypeHandle::Equals(valuetype [System.Runtime]System.RuntimeTypeHandle)
  brtrue CallKindNotPartOfFunctionPointerIdentityOK
  ldc.i4.7
  ret
CallKindNotPartOfFunctionPointerIdentityOK:

  ldtoken method unmanaged cdecl int32 *()
  stloc.0
  ldloca 0
  ldtoken method int32 *()
  call instance bool valuetype [System.Runtime]System.RuntimeTypeHandle::Equals(valuetype [System.Runtime]System.RuntimeTypeHandle)
  // Managed vs. unmanaged part of identity
  brfalse ManagedVsUnmanagedIsPartOfFunctionPointerIdentityOK
  ldc.i4.8
  ret

ManagedVsUnmanagedIsPartOfFunctionPointerIdentityOK:
  ldc.i4 100
  ret
}
}

.class public MyModifier { }

.class value sequential public MyStruct { .size 1 }
