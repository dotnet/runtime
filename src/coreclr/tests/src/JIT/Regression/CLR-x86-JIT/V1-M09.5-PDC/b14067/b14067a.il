// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

.assembly extern legacy library mscorlib {}
.assembly b14067a
{
}
.class ILGEN_622380794 {

.field static	int64 field_0x5

.method static int32 main() {
.entrypoint
.maxstack	20

 ldsflda int64 ILGEN_622380794::field_0x5
 ldind.i8
 conv.u
 conv.r.un
 conv.i4
ldc.i4 100
add
 ret
}
}
