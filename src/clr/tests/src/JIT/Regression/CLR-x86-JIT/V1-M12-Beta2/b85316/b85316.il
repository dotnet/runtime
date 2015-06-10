// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

.assembly extern legacy library mscorlib {}
.assembly n.exe{}
.class public _n {
.method public static int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack	10
        ldc.r8  99.1
        neg
        ldc.r8  -99.1
        ceq
        brtrue PASS
        ldc.i4 5
	ret
PASS:	ldc.i4	100
	ret
}
}
