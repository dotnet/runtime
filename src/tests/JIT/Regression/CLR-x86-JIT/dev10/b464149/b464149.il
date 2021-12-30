// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}

// Metadata version: v2.0.50727
.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 2:0:0:0
}
.assembly 'b464149'
{
  .ver 0:0:0:0
}
.assembly extern xunit.core {}

// =============== CLASS MEMBERS DECLARATION ===================

.class private sequential ansi sealed beforefieldinit HFA
       extends [mscorlib]System.ValueType
{
  .field public float64 d1
  .field public float64 d2
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor(float64 d1,
                               float64 d2) cil managed
  {
    // Code size       15 (0xf)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldarg.1
    IL_0002:  stfld      float64 HFA::d1
    IL_0007:  ldarg.0
    IL_0008:  ldarg.2
    IL_0009:  stfld      float64 HFA::d2
    IL_000e:  ret
  } // end of method HFA::.ctor

  .method public hidebysig virtual instance string 
          ToString() cil managed
  {
    // Code size       85 (0x55)
    .maxstack  4
    .locals init (object[] V_0)
    IL_0000:  ldstr      "d1 = {0:R} (0x{1:X16}), d2 = {2:R} (0x{3:X16})"
    IL_0005:  ldc.i4.4
    IL_0006:  newarr     [mscorlib]System.Object
    IL_000b:  stloc.0
    IL_000c:  ldloc.0
    IL_000d:  ldc.i4.0
    IL_000e:  ldarg.0
    IL_000f:  ldfld      float64 HFA::d1
    IL_0014:  box        [mscorlib]System.Double
    IL_0019:  stelem.ref
    IL_001a:  ldloc.0
    IL_001b:  ldc.i4.1
    IL_001c:  ldarg.0
    IL_001d:  ldfld      float64 HFA::d1
    IL_0022:  call       int64 [mscorlib]System.BitConverter::DoubleToInt64Bits(float64)
    IL_0027:  box        [mscorlib]System.Int64
    IL_002c:  stelem.ref
    IL_002d:  ldloc.0
    IL_002e:  ldc.i4.2
    IL_002f:  ldarg.0
    IL_0030:  ldfld      float64 HFA::d2
    IL_0035:  box        [mscorlib]System.Double
    IL_003a:  stelem.ref
    IL_003b:  ldloc.0
    IL_003c:  ldc.i4.3
    IL_003d:  ldarg.0
    IL_003e:  ldfld      float64 HFA::d2
    IL_0043:  call       int64 [mscorlib]System.BitConverter::DoubleToInt64Bits(float64)
    IL_0048:  box        [mscorlib]System.Int64
    IL_004d:  stelem.ref
    IL_004e:  ldloc.0
    IL_004f:  call       string [mscorlib]System.String::Format(string,
                                                                object[])
    IL_0054:  ret
  } // end of method HFA::ToString

  .method private hidebysig static int32 
          Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // Code size       288 (0x120)
    .maxstack  10
    .locals init (valuetype HFA[0...,0...,0...] V_0,
             int32 V_1, // upperbound of rank 0
             int32 V_2, // upperbound of rank 1
             int32 V_3, // upperbound of rank 2
             int32 V_4, // IV for rank 0
             int32 V_5, // IV for rank 1
             int32 V_6, // IV for rank 2
             valuetype HFA& V_7)
	ldc.i4.1
	ldc.i4.1
	ldc.i4.1
	newobj     instance void valuetype HFA[0...,0...,0...]::.ctor(int32,int32,int32)
	stloc.0
	ldloc.0
	ldc.i4.0
	ldc.i4.0
	ldc.i4.0
	ldc.r8     1.
	ldc.r8     2.
	newobj     instance void HFA::.ctor(float64,float64)
	call       instance void valuetype HFA[0...,0...,0...]::Set(int32,int32,int32,valuetype HFA)
	ldloc.0
	ldc.i4.0
	callvirt   instance int32 [mscorlib]System.Array::GetUpperBound(int32)
	stloc.1
	ldloc.0
	ldc.i4.1
	callvirt   instance int32 [mscorlib]System.Array::GetUpperBound(int32)
	stloc.2
	ldloc.0
	ldc.i4.2
	callvirt   instance int32 [mscorlib]System.Array::GetUpperBound(int32)
	stloc.3
	ldloc.0
	ldc.i4.0
	callvirt   instance int32 [mscorlib]System.Array::GetLowerBound(int32)
	stloc.s    V_4
	br.s       rank0

loop0:
	ldloc.0
	ldc.i4.1
	callvirt   instance int32 [mscorlib]System.Array::GetLowerBound(int32)
	stloc.s    V_5
	br.s       rank1

loop1:
	ldloc.0
	ldc.i4.2
	callvirt   instance int32 [mscorlib]System.Array::GetLowerBound(int32)
	stloc.s    V_6
	br.s       rank2

loop2:
	ldloc.0
	ldloc.s    V_4
	ldloc.s    V_5
	ldloc.s    V_6
	call       instance valuetype HFA valuetype HFA[0...,0...,0...]::Get(int32,int32,int32)
	box        HFA
	call       void [System.Console]System.Console::WriteLine(object)

	ldloc.s    V_6
	ldc.i4.1
	add
	stloc.s    V_6
rank2:
	ldloc.s    V_6
	ldloc.3
	ble.s      loop2

	ldloc.s    V_5
	ldc.i4.1
	add
	stloc.s    V_5
rank1:
	ldloc.s    V_5
	ldloc.2
	ble.s      loop1

	ldloc.s    V_4
	ldc.i4.1
	add
	stloc.s    V_4
rank0:
	ldloc.s    V_4
	ldloc.1
	ble.s      loop0

	ldloc.0
	ldc.i4.0
	ldc.i4.0
	ldc.i4.0
	call       instance valuetype HFA& valuetype HFA[0...,0...,0...]::Address(int32,int32,int32)
	stloc.s    V_7
	ldloc.s    V_7
	ldfld      float64 HFA::d1
	ldc.r8     1.
	bne.un.s   fail

	ldloc.s    V_7
	ldfld      float64 HFA::d2
	ldc.r8     2.
	bne.un.s   fail

	ldstr      "PASS"
	call       void [System.Console]System.Console::WriteLine(string)
	ldc.i4.s   100
	ret

fail:
	ldstr      "FAIL"
	call       void [System.Console]System.Console::WriteLine(string)
	ldc.i4.m1
	ret
  } // end of method HFA::Main

} // end of class HFA


// =============================================================

// *********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file repro.res
