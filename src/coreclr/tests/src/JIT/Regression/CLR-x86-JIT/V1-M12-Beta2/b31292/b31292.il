// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0x59e058c0 {}
.class ILGEN_0x59e058c0 {

.method static int32 Method_0x2dd7( unsigned int64 Arg_0x3) {
.maxstack	17

Start_Orphan_1:
 ldarg Arg_0x3
 conv.ovf.i1
 conv.u1
 pop
End_Orphan_1:

Start_Orphan_a:
 ldc.i4 255
 newarr [mscorlib]System.Int64 
 ldc.i4.2
 ldarg Arg_0x3
 stelem.i8
End_Orphan_a:

 ldc.i4.3
ret
}
.method static int32 Main() {
.entrypoint
.maxstack	20
.try {
ldc.i8 0x65d25f3b4fa63daa 
call int32 ILGEN_0x59e058c0::Method_0x2dd7(unsigned int64 Arg_0x3)
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
