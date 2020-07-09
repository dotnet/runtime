// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0x7c08a1ce {}
.class ILGEN_0x7c08a1ce {

.method static int32 Method_0x4406(unsigned int32 Arg_0x0){
.maxstack	9

 ldarg Arg_0x0
 conv.r4
 neg
 conv.i4
ret
}
.method static int32 Main() {
.entrypoint
.maxstack	20

ldc.i4 0x100 
call int32 ILGEN_0x7c08a1ce::Method_0x4406(unsigned int32 Arg_0x0)
ldc.i4 -356
sub
ret
}

}
