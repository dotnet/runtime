// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

.assembly extern legacy library mscorlib {}
.assembly b14202
{
}
.class ILGEN_622380794 {

.field static	int32 field_0x0

.method static int32 main() {
.entrypoint
.maxstack	10

 ldsfld int32 ILGEN_622380794::field_0x0
 ldc.i4 0
 shl
 stsfld int32 ILGEN_622380794::field_0x0

ldc.i4 100
ret
}
}
