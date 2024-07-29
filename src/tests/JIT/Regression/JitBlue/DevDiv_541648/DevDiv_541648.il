// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { auto }
.assembly extern System.Runtime { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) }
.assembly extern System.Console { auto }

.assembly DevDiv_541648 { }
.assembly extern xunit.core {}

// The test originally hit a bug in the handling of an addressing mode (LEA) under a block
// store, which must not be contained. This was created because the address used as the source
// of the block store was the address of a scalar field.


.class public sequential ansi sealed beforefieldinit MyStruct
       extends [System.Runtime]System.ValueType
{
  .field public int32 f1
  .field public int32 f2
  .field public int64 f3
  .field public int64 f4
  .field public int64 f5
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor(int32 i) cil managed noinlining
  {
         ldarg.0
         ldarg.1
         stfld      int32 MyStruct::f1

         ldarg.0
         ldarg.1
         ldc.i4.1
         add
         stfld      int32 MyStruct::f2

         ldarg.0
         ldarg.1
         ldc.i4.2
         add
         conv.i8
         stfld      int64 MyStruct::f3

         ldarg.0
         ldarg.1
         ldc.i4.3
         add
         conv.i8
         stfld      int64 MyStruct::f4

         ldarg.0
         ldarg.1
         ldc.i4.4
         add
         conv.i8
         stfld      int64 MyStruct::f5

         ret
  } // end of method MyStruct::.ctor

} // end of class MyStruct

.class public auto ansi beforefieldinit DevDiv_541648
       extends [System.Runtime]System.Object
{
  .field public static literal int32 Pass = int32(0x00000064)
  .field public static literal int32 Fail = int32(0xFFFFFFFF)
  .method public hidebysig static valuetype MyStruct 
          Test(valuetype MyStruct& c) cil managed noinlining
  {
    .locals init ([0] valuetype MyStruct V_0)
        ldloca.s   0
        ldarg.s    0

        // Here we take the address of the second field and copy 28 bytes from it (the size of the
        // remaining part of the struct.

        ldflda     int32 MyStruct::f2
        ldc.i4     28
        cpblk
        br.s       L1

    L1: ldloc.0
        ret
  } // end of method DevDiv_541648::Test

  .method public hidebysig static int32  Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // Code size       59 (0x3b)
    .maxstack  2
    .locals init ([0] int32 retVal,
             [1] valuetype MyStruct c,
             [2] valuetype MyStruct s,
             [3] bool V_3,
             [4] int32 V_4)

         ldc.i4.s   100
         stloc.0
         ldloca.s   c
         ldc.i4.1
         call       instance void MyStruct::.ctor(int32)
         ldloca.s   c
         call       valuetype MyStruct DevDiv_541648::Test(valuetype MyStruct&)
         stloc.2
         ldloc.2
         ldfld      int32 MyStruct::f1
         ldc.i4.2
         ceq
         ldc.i4.0
         ceq
         stloc.3
         ldloc.3
         brfalse.s  L2

         ldstr      "FAIL"
         call       void [System.Console]System.Console::WriteLine(string)

         ldc.i4.m1
         stloc.0

    L2:  ldloc.0
         stloc.s    V_4
         br.s       L3

    L3:  ldloc.s    V_4
         ret
  } // end of method DevDiv_541648::Main

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
         ldarg.0
         call       instance void [System.Runtime]System.Object::.ctor()
         ret
  } // end of method DevDiv_541648::.ctor

} // end of class DevDiv_541648

