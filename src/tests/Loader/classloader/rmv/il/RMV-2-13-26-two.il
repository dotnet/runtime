// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console { }
/*RDawson - 11/2/200

  Check that methods marked mdPinvokeImpl can be specified as
  non-virtual.
*/

.assembly Bob{}
.assembly extern mscorlib {}

.class value sealed Foo extends [mscorlib]System.ValueType{
	.method public pinvokeimpl("NonExistent.dll") void Foo(){
	}
}

.method public static int32 main(){
	.locals (value class Foo)
	.entrypoint
	.maxstack 8
	ldstr	"PASSED - 2.13.26 - Was allowed to declare a non-virtual method with mdPinvokeImpl"
	call	void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
	ldc.i4	100
	ret
}
