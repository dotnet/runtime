// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ldsfldainitonlyvt {}
.class public sequential ansi sealed beforefieldinit VT
       extends [mscorlib]System.ValueType
{
  .field public int32 m
} // end of class VT

.class public auto ansi beforefieldinit ldsfldainitonlyvt
       extends [mscorlib]System.Object
{
  .field private static initonly valuetype VT vt1
  .method public hidebysig static void  f(valuetype VT& arg) cil managed
  {
    // Code size       12 (0xc)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  //ldsfld     valuetype VT ldsfldainitonlyvt::vt1
    IL_0006:  //stobj      VT
		ldsflda     valuetype VT ldsfldainitonlyvt::vt1
		cpobj valuetype VT
    IL_000b:  ret
  } // end of method ldsfldainitonlyvt::f

  .method public hidebysig static int32  Main() cil managed
  {
    .entrypoint
    // Code size       39 (0x27)
    .maxstack  2
    .locals init (valuetype VT V_0,
             int32 V_1)
    IL_0000:  ldsflda    valuetype VT ldsfldainitonlyvt::vt1
    IL_0005:  ldc.i4.s   100
    IL_0007:  stfld      int32 VT::m
    IL_000c:  ldloca.s   V_0
    IL_000e:  initobj    VT
    IL_0014:  ldloca.s   V_0
    IL_0016:  call       void ldsfldainitonlyvt::f(valuetype VT&)
    IL_001b:  ldloca.s   V_0
    IL_001d:  ldfld      int32 VT::m
    IL_0022:  stloc.1
    IL_0023:  br.s       IL_0025

    IL_0025:  ldloc.1
	      ldc.i4 100
		bne.un FAIL
PASS:
	ldstr "PASS"
	call void [System.Console]System.Console::WriteLine(string)
	ldc.i4 100
	ret
FAIL:
	ldstr "FAIL"
	call void [System.Console]System.Console::WriteLine(string)
	ldc.i4 1
	ret
  } // end of method ldsfldainitonlyvt::Main

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method ldsfldainitonlyvt::.ctor

}