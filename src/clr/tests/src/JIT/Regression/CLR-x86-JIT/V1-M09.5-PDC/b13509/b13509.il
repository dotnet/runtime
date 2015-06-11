// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

.assembly extern legacy library mscorlib {}
.assembly b13509
{
}
.class I {
 .method static int32 main() {
 .entrypoint
 .maxstack	3
 ldc.i8 1
 conv.ovf.u8
 conv.u4
 
 ldc.i4.1
 sub
ldc.i4 100
add
 ret
}
}
