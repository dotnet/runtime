// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

.assembly extern legacy library mscorlib {}
.assembly ILGEN_0x2e3bca41 {}
.class ILGEN_0x2e3bca41 {
.method static int32 Method_0x76a09634() {
.maxstack	1
 ldc.i4.2
ret
}
.method static int32 Main() {
.entrypoint
.maxstack	20
 call int32 ILGEN_0x2e3bca41::Method_0x76a09634()
 ldc.i4.2
 sub
ldc.i4 100
add
 ret
}
}
