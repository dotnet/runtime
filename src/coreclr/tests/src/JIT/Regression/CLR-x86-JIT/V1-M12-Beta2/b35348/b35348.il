// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

.assembly extern legacy library mscorlib {}
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
