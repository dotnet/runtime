// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

//
//-----------------------------------------------------


//    .data PTR = int32(0x00000000)

//-------------------------
// Entry point - Main     -
//-------------------------
.assembly extern legacy library mscorlib {}
.assembly b14777
{
  .permission reqmin ['mscorlib']System.Security.Permissions.SecurityPermissionAttribute ( "SkipVerification" = true )

}
.class public _ldtoken 
{

.method static int32 main(class [mscorlib]System.String[]) 
{
.entrypoint
.locals	()
.maxstack	2
	
	ldtoken	  method instance vararg int32 _ldtoken::test(int32)
	stsfld valuetype [mscorlib]System.RuntimeMethodHandle _ldtoken::PTR

// for now, if we dont crash, we pass...

PASS:
	ldstr "Passed"
	call   void [mscorlib]System.Console::WriteLine(class [mscorlib]System.String)
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
