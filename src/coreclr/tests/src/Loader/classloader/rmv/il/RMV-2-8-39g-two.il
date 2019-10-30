// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern System.Console { }
/* RDawson - 11/2/200

   Checks that we can specify a enum with one integral field
*/

.assembly boo{}
.assembly extern mscorlib {}

.class Foo extends [mscorlib]System.Enum{
	.field public static literal int32 Bar = int32(0)
	.field public specialname rtspecialname int32 value__
}

.method public static int32 main(){
	.locals (value class Foo)
	.entrypoint
	.maxstack 8
	ldstr	"PASS - 2.8.39g - Was allowed to declare a class extending System.Enum with one integer instance fields."
	call	void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
	ldc.i4	100
	ret
}
