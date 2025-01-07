// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console { }
.assembly extern xunit.core {}
/*RDawson - 11/2/200

  Check that static and instance methods of a value class 
  can be non-synchronized
*/

.assembly Bob{}
.assembly extern mscorlib {}

.class public value sealed Foo extends [mscorlib]System.ValueType{
	.field public int32 foo
	.method public specialname void .ctor(){
		ldarg.0
		ldc.i4	0
		stfld	int32 Foo::foo
		ret
	}

	.method public static void Foo(){
		ret
	}

	.method public void Foo2(){
		ret
	}
}

.method public static int32 main(){
	.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
	    01 00 00 00
	)
	.locals(value class Foo)
	.entrypoint
	.maxstack 8
	newobj instance	void Foo::.ctor()
	stloc	0
	ldloca	0
	call	instance void Foo::Foo2()
	call	void Foo::Foo()
	ldstr	"PASSED - 2.13.22 - Was allowed to declare a static and instance method in a value class, both as non-synchronized"
	call	void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
	ldc.i4	100
	ret
}
