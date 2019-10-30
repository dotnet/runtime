// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern System.Console { }
/* RDawson - 11/2/200

   Check that interfaces are required to be abstract
*/

.assembly Bob{}
.assembly extern mscorlib {}

.class interface abstract Foo{
}

.class Bar implements Foo{
	.method public specialname void .ctor(){
		ldarg.0
		call	instance void [mscorlib]System.Object::.ctor()
		ret
	}
}

.method public static int32 main(){
	.entrypoint
	.maxstack 8
	newobj instance	void Bar::.ctor()
	pop
	ldstr	"PASS - 2.8.28 - Was allowed to define an abstract interface"
	call	void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
	ldc.i4	100
	ret
}
