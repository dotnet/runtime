// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern System.Console { }
// Check that if present, NumElem must match the number of vals for NamedArgs

.assembly extern 'RMV-4-CA'{}
.assembly extern mscorlib {}

.assembly 'RMV-4-1-6a'{
	//CA Blob - NumElem on the NamedArg PubInstFieldArr==1, Actual argument count==0
  .custom instance void ['RMV-4-CA']MyCA::.ctor() = ( 01 00 01 00 53 1D 08 0F 50 75 62 49 6E 73 74 46   // ....S...PubInstF
                                                      69 65 6C 64 41 72 72 01 00 00 00 ) 
}

.class CMain extends [mscorlib]System.Object{
	.method public static int32 Main(){
		.entrypoint
		ldstr	"FAIL - 4.1.6a - Was allowed to declare a customattribute whose prolog isn't 0x0001"
		call	void [System.Console]System.Console::WriteLine(string)
		ldc.i4  100
		ret
	}
}
