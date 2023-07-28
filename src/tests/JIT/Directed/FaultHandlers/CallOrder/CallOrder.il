// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'CallOrder'
{
}
//------------------------------------------------------
//	This test ensures that fault handlers are called in
//	the correct order.
.class public TEST
{
	.field public static int32 result
	
	.method public specialname rtspecialname instance void .ctor(int32) il managed
	{
		ldarg 0
		call instance void [mscorlib]System.Object::.ctor()
		ret
	}

	.method public static void Checkpoint(int32 mask) il managed
	{
		//	TEST::result |= mask;
		ldsfld int32 TEST::result
		ldarg  mask
		or
		stsfld int32 TEST::result
		ret
	}

	.method public static int32 CallOrder(int32 bThrowException) il managed
	{
		//	zero result
		ldc.i4.0
		stsfld int32 TEST::result
		
		ldc.i4  0x0001
		call	void TEST::Checkpoint(int32)
		.try {
			.try {
				.try {
					.try {
						ldc.i4	0x0002
						call	void TEST::Checkpoint(int32)
						
						ldarg	bThrowException
						brfalse	skip
						
						newobj     instance void [mscorlib]System.Exception::.ctor()
						throw
						
						ldc.i4  0x0004
						call	void TEST::Checkpoint(int32)
						
					skip:
						ldc.i4  0x0008
						call	void TEST::Checkpoint(int32)
						leave the_end
					}
					fault {
						ldc.i4  0x0010
						call	void TEST::Checkpoint(int32)
						endfinally
					}
				}
				fault {
					ldsfld int32 TEST::result
					ldc.i4	0x0010
					and
					brfalse skip1
					
					ldc.i4  0x0020
					call	void TEST::Checkpoint(int32)
					
				skip1:
					endfinally
				}
			}
			fault {
				ldsfld int32 TEST::result
				ldc.i4	0x0030
				and
				ldc.i4	0x0030
				ceq
				brfalse skip2
				
				ldc.i4  0x0040
				call	void TEST::Checkpoint(int32)
				
			skip2:
				endfinally
			}
		}
		catch [mscorlib]System.Exception {
			pop
			ldc.i4  0x0180
			call	void TEST::Checkpoint(int32)
			leave	the_end
		}
	the_end:
		ldsfld int32 TEST::result
		ret
	}

	.method public static int32 Main() il managed
	{
		.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
		    01 00 00 00
		)
		.entrypoint
		.locals (int32)

		//	Case # 1 - no exception thrown.
		ldc.i4  1
		stloc.0
		ldc.i4.0
		call	int32 TEST::CallOrder(int32)
		ldc.i4	0x000b
		ceq
		brfalse failed

		//	Case # 2 - exception raised.
		ldc.i4  2
		stloc.0
		ldc.i4.1
		call	int32 TEST::CallOrder(int32)
		ldc.i4	0x01f3
		ceq
		brfalse failed
		
	passed:
		ldstr	"Test passed."
		call	void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
		ldc.i4  0x64
		ret
		
	failed:
		ldstr	"Test # "
		call	void [System.Console]System.Console::Write(class [mscorlib]System.String)
		ldloc.0
		call	void [System.Console]System.Console::Write(int32)
		ldstr	" failed!"
		call	void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
		ldc.i4.1
		ret
	}
}
