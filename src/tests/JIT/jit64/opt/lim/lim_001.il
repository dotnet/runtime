// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This test is checking to see if the AV in the if is hoisted out of the loop
// The code in the if should not hit.  Thus there should be no AV


.assembly extern mscorlib { }
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'lim_001' {}

.class public auto ansi beforefieldinit test
       extends [mscorlib]System.Object
{

.field public static int32 k
  .method public static int32 Main() cil managed
  {
	.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
	    01 00 00 00
	)
	.locals init (int32, int32, int32)
    .entrypoint

	ldc.i4 5
	stsfld int32 test::k

LOOP0_START:
	ldloc 0
	ldc.i4 7
	bge LOOP0_END
	
	ldsfld int32 test::k
	stloc 1

IF0:	ldloc 0
	ldc.i4 1
	add
	brtrue	IF0_END  // Should always branch around

	ldsfld int32 test::k
	ldind.i4		// AV here
	stloc 2

IF0_END:
	

	ldloc 0
	ldc.i4 1
	add
	stloc 0
	br LOOP0_START

LOOP0_END:

	ldloc 1
	call void [System.Console]System.Console::WriteLine(int32)

	ldloc 2
	call void [System.Console]System.Console::WriteLine(int32)		


	ldc.i4 100
	ret

  }  
}
