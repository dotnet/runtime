// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern legacy library mscorlib {}


.class Ldvirtftn {
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
.assembly c_ldvirtftn.exe {}
