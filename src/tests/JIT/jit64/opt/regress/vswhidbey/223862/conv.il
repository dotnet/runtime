// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern xunit.core {}

.assembly 'conv_223862' {}
.assembly extern mscorlib{auto}
.class public Class_test {
.method public static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	5

	.try
	{

//     ldc.i4.2
     ldc.i4.0
     ldc.i4.2
    sub
     dup
     call       void [System.Console]System.Console::WriteLine(int32)




conv.ovf.i1.un
     call       void [System.Console]System.Console::WriteLine(int32)
IL_000c:  leave.s    IL_02
   }
	catch [mscorlib]System.OverflowException
	{
		pop
		ldstr "Pass"
		call       void [System.Console]System.Console::WriteLine(string)
		leave IL_01
	} 

// this is to make the errorlevel 100 if the answer is right ( ie 0)
IL_02:
    ldc.i4 1
    ret
IL_01: 
   ldc.i4 100

 ret
}
}
