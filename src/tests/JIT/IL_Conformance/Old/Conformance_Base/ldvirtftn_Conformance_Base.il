// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


.assembly extern mscorlib {}
.assembly extern xunit.core {}
.namespace ldvirtftn_Conformance_Base
{
.class public Ldvirtftn {
.method public static int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
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
.assembly 'ldvirtftn_Conformance_Base'{}
}
