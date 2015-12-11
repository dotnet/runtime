// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


.assembly extern mscorlib {}
.class public Ldvirtftn {
.method public static int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack       20
.locals ()
	newobj instance void [mscorlib]System.Object::.ctor()
	ldvirtftn instance class [mscorlib]System.String [mscorlib]System.Object::ToString()
	newobj instance void [mscorlib]System.Object::.ctor()
	ldvirtftn instance class [mscorlib]System.String [mscorlib]System.Object::ToString()
	ceq
    brfalse     FAIL

    ldc.i4 100
    ret
FAIL:
    ldc.i4      0x0
    ret
}
}
.assembly ldvirtftn.exe{}
