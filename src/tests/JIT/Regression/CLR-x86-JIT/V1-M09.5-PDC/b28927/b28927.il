// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
//-----------------------------------------------------


//    .data PTR = int32(0x00000000)

//-------------------------
// Entry point - Main     -
//-------------------------
.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b28927'
{

}
.class public _ldtoken 
{

.method public static int32 main()
{
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.locals	()
.maxstack	2
	
	ldtoken	  method instance vararg int32 _ldtoken::test(int32)
	stsfld valuetype [mscorlib]System.RuntimeMethodHandle _ldtoken::PTR

// for now, if we dont crash, we pass...

PASS:
	ldstr "Passed"
	call   void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
	ldc.i4	100
	ret
FAIL:
	ldc.i4	0x1
	ret
}



.field static valuetype [mscorlib]System.RuntimeMethodHandle PTR

//# constructor ##
.method void _ldtoken() 
{
.maxstack	4
	ret
}

.method vararg int32 test(int32) 
{
.maxstack	2
	ldarg	1
	ldc.i4	0x2
	mul
	ret
}
}
