// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.



.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0xdbcacf66 {}
.class ILGEN_0xdbcacf66 {

//BEGIN STATIC FIELDS
//END STATIC FIELDS
.method static unsigned int64  Method_0x8216365b() {
.maxstack	11

 .locals ()
//BEGIN INIT LOCALS

//END INIT LOCALS

//BEGIN INIT OF STATIC FIELDS

//END INIT OF STATIC FIELDS

 BLOCK_1:
Start_Orphan_0:
nop
End_Orphan_0:
Start_Orphan_1:
    ldc.i8 0x6908ca4e888e2a34
   not
  conv.u
 pop
End_Orphan_1:
Start_Orphan_2:
    ldc.i8 0x6aa9b363a97a73a7
   pop
End_Orphan_2:
   ldc.i8 0x97985bd784351d74
Start_Orphan_3:
    nop
End_Orphan_3:
    ldc.i8 0xb78c0775394ad2f2
     ldc.r4 float32(0x1c6a57aa)
    conv.i8
   add
  mul
Start_Orphan_4:
   nop
End_Orphan_4:
     ldc.i4.2
    conv.i8
     ldc.i4.m1
    conv.u8
   or
  conv.u8
 add
Start_Orphan_5:
Start_Orphan_6:
   nop
End_Orphan_6:
    ldc.i4.1
   conv.r4
  conv.i
 pop
End_Orphan_5:
Start_Orphan_7:
    ldnull
   pop
End_Orphan_7:
Start_Orphan_8:
    nop
End_Orphan_8:
     ldc.i8 0x9c2e510d444b7fee
     ldc.i8 0x819851b4a3d81943
    mul
Start_Orphan_9:
    nop
End_Orphan_9:
    ldc.i8 0x9bbbb732f5c76f5e
   and
Start_Orphan_a:
    nop
End_Orphan_a:
     ldc.r8 0.00001
    conv.i1
   conv.i8
  sub
Start_Orphan_b:
    nop
End_Orphan_b:
     ldc.i4.3
    conv.i8
   conv.u
  conv.u8
 add
xor
 ret
}
.method static int32 Main() {
.entrypoint
.maxstack	2

 call unsigned int64  ILGEN_0xdbcacf66::Method_0x8216365b()
 conv.i4
 ldc.i4 1407637126
 sub
 ret
}

}
