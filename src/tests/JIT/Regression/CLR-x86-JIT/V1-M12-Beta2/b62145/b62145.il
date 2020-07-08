// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0xa00c093a {}
.class ILGEN_0xa00c093a {
.method static int32 Method_0x8839aba4(int16 Arg_0x0,native int Arg_0x7, native int Arg_0x8) {
.maxstack	5

 ldc.r4 1.1
 ldc.i4.6
 ldarg Arg_0x7
 ble.un Branch_0x2
 ldc.r8 5.5
 br Branch_0x3
Branch_0x2:
 ldc.r4 0.1
Branch_0x3:
 bge Branch_0x0
 ldc.i4.s 61
 br Branch_0x1
Branch_0x0:
 ldarg Arg_0x8
 ldarg Arg_0x0
 clt
Branch_0x1:
 ret
}
.method static int32 Main() {
.entrypoint
.maxstack	20
ldc.i4 0x3c95b9ed 
ldc.i4 0x289f8227 conv.i
ldc.i4 0x5647a735 conv.i

call int32 ILGEN_0xa00c093a::Method_0x8839aba4(int16 Arg_0x0, native int Arg_0x7, native int Arg_0x8)
ldc.i4 100
or
 ret
}

}
