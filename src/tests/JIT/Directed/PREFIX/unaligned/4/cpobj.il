// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern xunit.core {}

.assembly extern legacy library mscorlib {}
.assembly 'cpobj_unaligned_4' {}

.class	public		_cpobj {

.field public static	value class valClass Instance1
.field public static	value class valClass Instance2

.method public static	int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.locals		(class [mscorlib]System.IO.TextWriter)
.maxstack		2
	call        class [mscorlib]System.IO.TextWriter [System.Console]System.Console::get_Out()
	stloc	0

	ldsflda		value class valClass _cpobj::Instance1
	ldc.i4		0x89ABCDEF
	unaligned. 0x4
	stfld		int32 valClass::Field
	ldc.i4		0x01234567
	stsfld		int32 valClass::SField
	ldsflda		value class valClass _cpobj::Instance1
	unaligned. 0x4
	ldfld		int32 valClass::Field
	ldc.i4		0x89ABCDEF
	ceq
	brfalse		Init1Failure
	ldsfld		int32 valClass::SField
	ldc.i4		0x01234567
	ceq
	brtrue		InitInstance2
Init1Failure:
	ldloc	0
	ldstr		"Initialization failure in instance 1"


	callvirt	instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
	br			fail
	
InitInstance2:
	ldsflda		value class valClass _cpobj::Instance2
	ldc.i4		0xABABABAB
	unaligned. 0x4
	stfld		int32 valClass::Field
	ldsflda		value class valClass _cpobj::Instance2
	unaligned. 0x4
	ldfld		int32 valClass::Field
	ldc.i4		0xABABABAB
	ceq
	brfalse		Init2Failure
	ldsfld		int32 valClass::SField
	ldc.i4		0x01234567
	ceq
	brtrue		CheckInst1Field
Init2Failure:
	ldloc	0
	ldstr		"Initialization failure in instance 2"


	callvirt	instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
	br			fail

CheckInst1Field:
	ldsflda		value class valClass _cpobj::Instance1
	unaligned. 0x4
	ldfld		int32 valClass::Field
	ldc.i4		0x89ABCDEF
	ceq
	brtrue		cpobjTests
	ldloc	0
	ldstr		"Initialization failure in instance 1"


	callvirt	instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
	br			fail

cpobjTests:
	ldsflda		value class valClass _cpobj::Instance2
	ldsflda		value class valClass _cpobj::Instance1
	cpobj		valClass
	ldsflda		value class valClass _cpobj::Instance1
	unaligned. 0x4
	ldfld		int32 valClass::Field
	ldc.i4		0x89ABCDEF
	ceq
	brfalse		cpobjFailure
	ldsflda		value class valClass _cpobj::Instance2
	unaligned. 0x4
	ldfld		int32 valClass::Field
	ldc.i4		0x89ABCDEF
	ceq
	brfalse		cpobjFailure
	ldsfld		int32 valClass::SField
	ldc.i4		0x01234567
	ceq
	brtrue		pass

cpobjFailure:
	ldloc	0
	ldstr		"_cpobj failed.  inst1 fields != inst2 fields"



	callvirt	instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
	br			fail

pass:
	ldloc	0
	ldstr		"_cpobj tests PASS"


	callvirt	instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
	ldc.i4	0x64
	br		end
fail:
	ldloc	0
	ldstr		"!!! FAILURE !!!  CPOBJ TEST(S) HAVE FAILED !!! FAILURE !!!"


	callvirt	instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
	ldc.i4	0x0
	br		end
end:
	ret
}


}
.class public value sealed valClass {
.field	public int32 Field
.field public static	int32 SField
}
