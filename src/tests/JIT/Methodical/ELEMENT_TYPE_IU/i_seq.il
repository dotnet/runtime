// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
//
.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'i_seq'
{ }
.assembly extern xunit.core {}
.class public auto ansi Test_i_seq extends [mscorlib]System.Object
{
.method private hidebysig static native int calc(native int, native int) il managed
{
	.maxstack 8
	.locals (native int)
	ldarg.1
	stloc.0
	ldloc.0
	br again
again:
	ldarg.0
	brfalse.s stop
	ldarg.0
	ldarg.1
	switch (sum,fact)
	pop
	br stop
sum:
	add
	br.s cont
fact:
	mul
cont:
	ldarg.0
	ldc.i4.1
	sub
	starg 0
	br again
stop:
	ret
}

.method public hidebysig static int32 Main() il managed
{
	.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
	    01 00 00 00
	)
	.entrypoint
	.maxstack  5
	ldc.i4.s 6
	ldc.i4.1
	call native int Test_i_seq::calc(native int, native int)
	ldc.i4 720
	conv.i
	ceq
	brfalse ERROR
	ldc.i4.s 6
	ldc.i4.0
	call native int Test_i_seq::calc(native int, native int)
	ldc.i4 21
	conv.i
	ceq
	brfalse ERROR
	ldc.i4 0x64
	ldstr "PASSED"
	br EXIT
ERROR:
	ldc.i4.1
	ldstr "FAILED"
EXIT:
	call void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
	ret
}
.method public hidebysig specialname rtspecialname 
        instance void .ctor() il managed
{
	.maxstack  8
	ldarg.0
	call       instance void [mscorlib]System.Object::.ctor()
	ret
}
}
