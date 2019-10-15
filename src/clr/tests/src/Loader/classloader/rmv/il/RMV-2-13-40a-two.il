// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern System.Console { }
/* RDawson - 11/2/200

  Check that a standard .ctor is valid.
*/

.assembly Bob{}
.assembly extern mscorlib {}

.class Foo{
	.method public void .ctor(){
		ldarg.0
		call 	instance void [mscorlib]System.Object::.ctor()
		ret
	}

	.method public void .ctor(int32){
		ldarg.0
		call 	instance void [mscorlib]System.Object::.ctor()
		ret
	}
}

.class value sealed Foo2 extends [mscorlib]System.ValueType{
	.field public int32 foofield
	.method public void .ctor(){
//		ldarg.0
//		call	instance void [mscorlib]System.ValueType::.ctor()
		ret
	}

	.method public void .ctor(int32){
//		ldarg.0
//		call	instance void [mscorlib]System.ValueType::.ctor()
		ret
	}
}

.method public static int32 main(){
	.entrypoint
	.maxstack 8
	newobj instance	void Foo::.ctor()
	pop
	newobj instance	void Foo2::.ctor()
	pop
	ldstr	"PASSED - 2.13.40a - Was allowed to declare a valid .ctor()"
	call	void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
	ldc.i4	100
	ret
}
