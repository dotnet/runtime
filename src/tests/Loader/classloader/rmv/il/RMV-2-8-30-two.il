// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console { }
.assembly extern xunit.core {}
/* RDawson - 12/1/200

   Check classes can obtain contracted method bodies via itself and from its
   parent class.
*/

.assembly Bob{}
.assembly extern mscorlib {}

.class C1{
	.method public void f1(){
		ldstr	"Hello World!"
		call	void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
		ret
	}
	.method virtual public void f3(){
		ldstr	"Hello World!"
		call	void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
		ret
	}
}

.class interface I1{
	.method virtual abstract public void f2(){}
	.method virtual abstract public void f3(){}
}

.class public Foo extends C1 implements I1{
	.method specialname public void .ctor(){
		ldarg.0
		call	instance void [mscorlib]System.Object::.ctor()
		ret
	}

	.method virtual public void f2(){
		ldstr	"Hello World!"
		call	void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
		ret
	}
}


.method public static int32 main(){
	.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
	    01 00 00 00
	)
	.entrypoint
	.maxstack 8

	newobj instance	void Foo::.ctor()
	dup
	dup
	call	instance void Foo::f1()
	call	instance void Foo::f2()
	call	instance void Foo::f3()

	ldstr	"PASS - 2.8.30 - Was allowed to obtain method bodies from this and from super"
	call	void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
	ldc.i4	100
	ret
}
