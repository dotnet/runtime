// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

.assembly extern legacy library mscorlib {}
.assembly ILGEN_0xcd02950a {}
.class ILGEN_0xcd02950a {
.method static int32 Main() {
 .entrypoint
 .maxstack	20
 .locals (unsigned int32 local_0x0)
 ldc.i4 0x2a256bb9 
 stloc local_0x0
 ldc.i4 44
 ldloc local_0x0
 conv.u8
 conv.u4
 cgt.un
ldc.i4 100
add
 ret
}
}
