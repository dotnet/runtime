// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

.assembly extern legacy library mscorlib {}
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
 