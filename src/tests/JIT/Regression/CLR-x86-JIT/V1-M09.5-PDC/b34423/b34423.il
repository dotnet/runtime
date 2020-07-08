// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0x474a9f71 {}
.class ILGEN_0x474a9f71 {


.method static int32 Method_0x2e37(int64 Arg_0x0, unsigned int16 Arg_0x1, int64 Arg_0x2, float32 Arg_0x3, float64 Arg_0x4, int32 Arg_0x5, int16 Arg_0x6, float32 Arg_0x7) {
.maxstack	7

ldc.i4 55
ret
}
.method static int32 Main() {
.entrypoint
.maxstack	20
ldc.i8 0x12482f5215abd14 
ldc.i4 0x67403b18 
ldc.i8 0x12622843b845bf3 
ldc.r4 float32(0x550fadf) 
ldc.r8 float64(0x4a424311d7712fa) 
ldc.i4 0x935fdb 
ldc.i4 0x46bf2a1a 
ldc.r4 float32(0x3d124042) 

call int32 ILGEN_0x474a9f71::Method_0x2e37(int64 Arg_0x0, unsigned int16 Arg_0x1, int64 Arg_0x2, float32 Arg_0x3, float64 Arg_0x4, int32 Arg_0x5, int16 Arg_0x6, float32 Arg_0x7)
ldc.i4 55
sub
ldc.i4 100
add
ret
}

}
