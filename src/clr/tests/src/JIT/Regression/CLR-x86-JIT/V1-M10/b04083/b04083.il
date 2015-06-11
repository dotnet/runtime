// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

.assembly extern legacy library mscorlib {}
.assembly b04083
{
}
.class _simple {

.method static int32 main(class System.String[]) {
.entrypoint
.maxstack	100
 ldc.i4 43
 not
 conv.u8
 conv.i4
 pop
 ldc.i4 100
 ret
}
}