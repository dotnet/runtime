// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.class public stind_ref {

.method public	void stind_ref() {
.maxstack		0
	ret
}

.method public static	int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack		2
.locals	(class stind_ref, class stind_ref&)
    newobj	instance	void stind_ref::stind_ref()
    stloc.0
    ldloca.s 0
    stloc.1
    ldloc.1
    ldnull
    stind.ref
    ldloc.1
    ldloc.0
    stind.ref
    ldloc.0
    ldloc.1
    ldind.ref
    bne.un.s FAIL
    ldc.i4.s 100
    ret
FAIL:
	ldc.i4	0x0
	ret
}
}

.assembly stind_ref{}