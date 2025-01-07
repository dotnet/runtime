// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console { }
.assembly extern xunit.core {}
/*RDawson - 11/2/200

  Check that methods marked either mdFinal, mdReuseSlot, mdNewSlot, or
  mdHideBySig, or none of those, can be specified as virtual.
*/

.assembly Bob{}
.assembly extern mscorlib {}

.class public value sealed Foo extends [mscorlib]System.ValueType{
	.method public virtual void Foo(){
		ret
	}

	.method public virtual final void Foo2(){
		ret
	}

//@todo @todo - Need to make this one mdReuseSlot not mdNewSlot
	.method public virtual newslot void Foo3(){
		ret
	}

	.method public virtual newslot void Foo4(){
		ret
	}

	.method public virtual hidebysig void Foo5(){
		ret
	}
}

.method public static int32 main(){
	.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
	    01 00 00 00
	)
	.locals (value class Foo)
	.entrypoint
	.maxstack 8
	ldstr	"PASSED - 2.13.25 - Was allowed to declare virtual methods with and without mdFinal, mdReuseSlot, mdNewSlot, mdHideBySig"
	call	void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
	ldc.i4	100
	ret
}
