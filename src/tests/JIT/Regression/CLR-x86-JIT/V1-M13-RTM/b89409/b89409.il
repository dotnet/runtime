// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly test
{
}
.module test.exe
// MVID: {A814820F-7F5B-4AEC-98B2-8CA067CCDA19}


.file alignment 512

// Image base: 0x04800000


.class private auto ansi beforefieldinit Test
       extends [mscorlib]System.Object
{

.field private static int32 _a
.field private static int32 _b
.field private static int32 _c

.method private hidebysig specialname rtspecialname static 
        void  .cctor() cil managed
{
  // Code size       22 (0x16)
  .maxstack  8
  IL_0000:  ldc.i4     2147483647
  IL_0002:  stsfld     int32 Test::_a
  IL_0007:  ldc.i4.s   10
  IL_0009:  stsfld     int32 Test::_b
  IL_000e:  ldc.i4.s   30
  IL_0010:  stsfld     int32 Test::_c
  IL_0015:  ret
} // end of method Test::.cctor

.method public hidebysig specialname rtspecialname 
        instance void  .ctor() cil managed
{
  // Code size       7 (0x7)
  .maxstack  8
  IL_0000:  ldarg.0
  IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
  IL_0006:  ret
} // end of method Test::.ctor

.method private hidebysig static int32 Main() cil managed
{
  .entrypoint
  // Code size       23 (0x17)
  .maxstack  2
  .locals (int32 V_0,
           int32 V_1,
           int32 V_2)
  .try {
  IL_0000:  ldsfld     int32 Test::_a
  IL_0005:  stloc.0
  IL_0006:  ldsfld     int32 Test::_b
  IL_000b:  stloc.1
  IL_000c:  ldsfld     int32 Test::_c
  IL_0011:  stloc.2
  IL_0012:  ldloc.0
  IL_0013:  ldloc.1
  IL_0014:  add.ovf
  IL_0015:  stloc.0
  leave.s IL_FAIL
  } catch [mscorlib]System.OverflowException {
  leave.s IL_OK
  }
  IL_FAIL:
  ldc.i4.s 101
  ret
  IL_OK:
  ldc.i4.s 100
  ret
} // end of method Test::Main

} // end of class Test
