// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

.assembly extern legacy library mscorlib {}
.assembly b14329
{
}
.class ILGEN_622380794 {

.field static	int32 field_0x4

.method static int32 main() {
.entrypoint
.maxstack	20

 ldc.i4 50
 stsfld int32 ILGEN_622380794::field_0x4

 ldsfld int32 ILGEN_622380794::field_0x4
// ldc.i4 0xcb546abe
 ldc.i4 0x909090be
 shl
 stsfld int32 ILGEN_622380794::field_0x4

ldc.i4 100
ret
}
}
