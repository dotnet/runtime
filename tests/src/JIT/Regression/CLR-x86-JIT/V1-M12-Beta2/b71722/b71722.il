// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0xee5a2e00 {}
.class ILGEN_0xee5a2e00 {
.method static unsigned int64  Method_0x28374f71(native int Arg_0x0) {
.maxstack	5
.locals (unsigned int32 LOCAL_0x0)
 ldc.i4 0xef7ac202 
 stloc LOCAL_0x0
  ldc.i4 255
  newarr [mscorlib]System.UInt64 
  ldc.i4 1
 ldelem.u8
  ldc.i8 0x1073aa767a1a9d17
  ldloc LOCAL_0x0
  conv.u1  //Shift of 2
 shl
 bgt Branch_0x2
 ldc.i8 0x100
 br Branch_0x3
Branch_0x2:
 ldc.i8 0x50
Branch_0x3:
 ret
}
.method static int32 Main() {
.entrypoint
.maxstack	3
 ldc.i4 0x0
 call unsigned int64  ILGEN_0xee5a2e00::Method_0x28374f71(native int Arg_0x0)
 conv.i4
 ldc.i4 156
 sub
 ret
}
}