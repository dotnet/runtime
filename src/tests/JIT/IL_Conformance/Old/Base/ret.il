// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}

.assembly 'ret'{}

.namespace ret_
{
.class public _ret {


.field public static native int PTR

.method public void .ctor() {

.maxstack	10
	ldarg.0
	call instance void [mscorlib]System.Object::.ctor()
	ret
}

.method public static		int32 _rem(int32) {
.maxstack	5
	ldarg	0
	ret
}

.method public static		int64 reti8(int64) {
.maxstack	5
	ldarg	0
	ret
}

.method public static		float32 retr4(float32) {
.maxstack	5
	ldarg	0
	ret
}

.method public static		float64 retr8(float64) {
.maxstack	5
	ldarg	0
	ret
}

.method public static		class ret_._ret retref(class ret_._ret) {
.maxstack	5
	ldarg	0
	ret
}

.method public static		int32 retI(int32*) {
.maxstack	5
	ldarg		0
	conv.i4
	ret
}

.method public static int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	10
.locals	(class ret_._ret)
	newobj	instance 	void ret_._ret::.ctor()
	stloc	0	

        ldc.i4          0x1
        call    int32 ret_._ret::_rem(int32)
        ldc.i4          0x1
        ceq
        brfalse         FAIL

        ldc.i8          0x2
        call    int64 ret_._ret::reti8(int64)
        ldc.i8          0x2
        ceq
        brfalse         FAIL
        ldc.r4          1.1
        call    float32 ret_._ret::retr4(float32)
        ldc.r4          1.1
        ceq
        brfalse         FAIL

        ldc.r8          2.2
        call    float64 ret_._ret::retr8(float64)
        ldc.r8          2.2
        ceq
        brfalse         FAIL

        ldloc   0
        call    class ret_._ret ret_._ret::retref(class ret_._ret)
        ldloc   0
        ceq
        brfalse         FAIL

        ldsflda		native int ret_._ret::PTR
        call    int32 ret_._ret::retI(int32*)
        ldsflda		native int ret_._ret::PTR
	conv.i4
        ceq
        brfalse         FAIL

PASS:
	ldc.i4 100
	ret
FAIL:
	ldc.i4	0x0
	ret
}
}
}
