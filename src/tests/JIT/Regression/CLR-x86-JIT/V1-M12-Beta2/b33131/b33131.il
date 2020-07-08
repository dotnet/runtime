// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0x5350b1c3 {}
.class ILGEN_0x5350b1c3 {

.field static	unsigned int16 field_0x0

.method static float32 Method_0x606b(int64 Arg_0x0) {
.maxstack	11

 .locals (int64 local_0x1,unsigned int64 local_0x4)
ldc.i8 0x1c544318663d332d 
stloc local_0x1
ldc.i8 0x1b2d5060ee751fb 
stloc local_0x4

ldc.i4 0x3eca654c 
stsfld unsigned int16 ILGEN_0x5350b1c3::field_0x0


 ldc.i8 0x534d2dd954c9c2f
 conv.i8
 ldsfld unsigned int16 ILGEN_0x5350b1c3::field_0x0
 ldc.i4.s 104
 bge.un Branch_0x1c

 ldarg Arg_0x0
 br Branch_0x1d

 Branch_0x1c:

 ldloc local_0x1
 Branch_0x1d:

 bge Branch_0x1a

 ldc.i8 0x1f051a168d066b2
 conv.u1
 br Branch_0x1b

 Branch_0x1a:

 ldc.i8 0x3277267278f94f0
 ldloc local_0x4
 cgt.un
 Branch_0x1b:

 conv.r4

ret
}
.method static int32 Main() {
.entrypoint
.maxstack	20
ldc.i8 0x48a05eba100c2471 

call float32 ILGEN_0x5350b1c3::Method_0x606b(int64 Arg_0x0)
conv.i4
ldc.i4 78
sub
 ret
}

}
