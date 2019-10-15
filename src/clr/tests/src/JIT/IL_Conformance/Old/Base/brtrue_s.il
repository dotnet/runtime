// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern legacy library mscorlib {}

.class _brtrue_s {

.method public static int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack       10
        ldc.i4          0x1
        brtrue.s        PASS

FAIL:
        ldc.i4  0x0
        ret
PASS:
        ldc.i4 100
        ret
}
}

.assembly brtrue_s.exe {}
