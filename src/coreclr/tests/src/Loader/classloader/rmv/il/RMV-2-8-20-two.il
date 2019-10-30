// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern System.Console { }
/* RDawson - 11/2/200

   Can't have a size 0 value class.

This rule actually checks an internal runtime mechanism that pads zero sized
value classes by a byte.  So we should never see this throw, even if we
give the loader a zero sized value class.
*/

.assembly Bob{}
.assembly extern mscorlib {}

.class value sealed Foo extends [mscorlib]System.ValueType{}


.method public static int32 main(){
	.locals	(value class Foo)
	.entrypoint
	.maxstack 8
	ldstr	"PASS - 2.8.20 - Was allowed to define a value class of size 0"
	call	void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
	ldc.i4	100
	ret

}
