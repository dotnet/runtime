// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0x25b2a016 {}
.class ILGEN_0x25b2a016 {

.field static	int64 field_0x0
.field static	unsigned int8[] field_0x1

.method static int32 Method_0x216d(int8 Arg_0x5) {
.maxstack	13

 .locals (unsigned int8[] local_0x1)

 ldc.i4 255
 newarr [mscorlib]System.Byte 
 stloc local_0x1

 ldc.i8 0x505e633a66c263a6 
 stsfld int64 ILGEN_0x25b2a016::field_0x0
 ldc.i4 255
 newarr [mscorlib]System.Byte 
 stsfld unsigned int8[] ILGEN_0x25b2a016::field_0x1


Start_Orphan_0:
 ldarg Arg_0x5
 conv.ovf.u1.un
 newarr [mscorlib]System.Byte
 stloc local_0x1
End_Orphan_0:

 ldc.i4 55
 ret
}
.method static int32 Main() {
.entrypoint
.maxstack	20
.try {
 ldc.i4 0x2cd869e4 
 call int32 ILGEN_0x25b2a016::Method_0x216d(int8 Arg_0x5)
 pop
  leave stop
 } catch [mscorlib]System.OverflowException {
 pop
 leave stop
 }
 stop:
ldc.i4 100
 ret
}

}
