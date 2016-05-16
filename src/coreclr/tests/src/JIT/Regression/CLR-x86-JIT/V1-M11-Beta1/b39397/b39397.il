// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0xcbda6722 {}
.class ILGEN_0xcbda6722 {
.method static float32 Method_0x796a(int64 Arg_0x0) {
.maxstack	17

 ldc.r4 55.0
 conv.u
 conv.u8
 ldc.i4.2
 conv.u8
 mul.ovf.un
 conv.r8
ret
}
.method static int32 Main() {
.entrypoint
.maxstack	20
ldc.i8 0x36cd60286b4444a9 

call float32 ILGEN_0xcbda6722::Method_0x796a(int64 Arg_0x0)
conv.i4
ldc.i4 10
sub
 ret
}

}
