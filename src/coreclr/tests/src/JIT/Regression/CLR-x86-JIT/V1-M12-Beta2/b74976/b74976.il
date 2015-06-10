// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

.assembly extern legacy library mscorlib {}
.assembly a {}
.class b {
.method static int32 c() {
.entrypoint
.maxstack 1
 ldc.i4.s 100
 ret
}
}