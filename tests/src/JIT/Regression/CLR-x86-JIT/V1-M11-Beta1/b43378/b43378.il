// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

.assembly extern legacy library mscorlib {}
.assembly ILGEN_0x144897ef {}
.class ILGEN_0x144897ef {
.field static	unsigned int8 field_0x4
.method static int32 Main() {
.entrypoint
.maxstack	20

 ldsfld unsigned int8 ILGEN_0x144897ef::field_0x4
 neg
 stsfld unsigned int8 ILGEN_0x144897ef::field_0x4

 ldc.i4 100
 ret
}
}
