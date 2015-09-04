// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0xe012a579 {}
.class ILGEN_0xe012a579 {

.method static int32 Main() {
.entrypoint
.maxstack	20

 ldc.i4 0x44 
 conv.i8
 ldc.i4 255
 newarr [mscorlib]System.Int32 
 ldc.i4 0x68 
 ldelem.i4
 conv.i8
 mul
 conv.i4
ldc.i4 100
add
ret
}

}
 