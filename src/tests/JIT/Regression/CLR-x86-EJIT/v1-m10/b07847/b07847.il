// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern xunit.core {}
.assembly extern legacy library mscorlib {}
.assembly b07847
{
}

.data tls FieldData = int32(51966)
.class public test {

	.field public static int32 TLSFieldData at FieldData
	.method public static int32 main() 
	{
		.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
		    01 00 00 00
		)
		.entrypoint
		ldsfld	int32 test::TLSFieldData  
		ldc.i4  51966
		beq		Passed1
		br		Failed
		
Passed1:
		ldc.i4  51967
		dup
		stsfld	int32 test::TLSFieldData  
		ldsfld	int32 test::TLSFieldData  
		beq		Passed2
		br		Failed
		
Passed2:
		ldstr "Test passed."
		call void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
		ldc.i4  100
		ret
		
Failed:	
		ldstr "Test failed."
		call void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
		ldc.i4  1
		ret
	}
}
