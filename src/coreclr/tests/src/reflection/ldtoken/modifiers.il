// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern System.Runtime { auto }
.assembly modifiers { }

.method private hidebysig static int32  Main() cil managed
{
  .locals init (valuetype [System.Runtime]System.RuntimeTypeHandle)
  .entrypoint

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

  ldc.i4 100
  ret
}

.class public MyModifier { }

.class value sequential public MyStruct { .size 1 }
