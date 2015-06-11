// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

.assembly extern legacy library mscorlib {}
.assembly b27538 {}
.class locali4 {

.method static int32 main() {
.entrypoint
.maxstack	17

.locals(float32)

 ldc.r8 3.3
 stloc 0

 ldloc 0
 conv.i4
 ldc.i4.3
 sub
ldc.i4 100
add
 ret
}
}
