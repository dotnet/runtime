// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

.assembly extern legacy library mscorlib {}
.assembly b04384
{
}
.class _simple {

.method static int32 main(class System.String[]) {
.entrypoint
.maxstack	100

 ldc.i8 6
 ldc.i8 1
 neg
 ldc.i8 1
 neg
 add
 sub
 ldc.i8 1
 ldc.i8 1
 sub
 ldc.i8 1
 mul
 add
 conv.i4
pop
ldc.i4 100
 ret
}
}