// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0xa967417e {}
.class ILGEN_0xa967417e {
.method static int32 Method_0xdb3affb(unsigned int16 Arg_0x0, int64 Arg_0x1, native int Arg_0x2) {
.maxstack	3
 ldarg Arg_0x2
 ldarg Arg_0x2
clt
 ret
}
.method static int32 Main() {
.entrypoint
.maxstack	6
 ldc.i4 0xb1eb25b3 
 ldc.i8 0x2d43e0dd9245d7c0 
 ldc.i4 0x1289a16d
 conv.i
 call int32 ILGEN_0xa967417e::Method_0xdb3affb(unsigned int16 Arg_0x0, int64 Arg_0x1, native int Arg_0x2)
 conv.i4
 ldc.i4 100
 add
 ret
}
}