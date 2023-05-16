// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//-----------------------------------------------------
.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly b15299{}

.class public TestStatic {

.method static	int32 StaticMethodTest(int32) {
.maxstack	10

	ldarg	0
	ldc.i4		0x2
	mul
	ret

}
// ---------------------------------------------------------


}
.class public callstatic {

.method public static int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	10

	ldc.i4		0x6
	call		int32 TestStatic::StaticMethodTest(int32)
	ldc.i4		0xC
	ceq
	brfalse		FAIL

//----------------
PASS:
	ldc.i4	100
	ret
//----------------
FAIL:
	ldc.i4	0x1
	ret
}
}
