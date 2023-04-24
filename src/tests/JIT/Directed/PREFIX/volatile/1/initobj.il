// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'initobj_volatile'{ //This byte field requests that this assembly not be verified at run time and corresponds to this C# declaration:
  //[assembly:System.Security.Permissions.SecurityPermissionAttribute( [mscorlib]System.Security.Permissions.SecurityAction.RequestMinimum, Flags=System.Security.Permissions.SecurityPermissionFlag.SkipVerification )]
                        }

.class public value sealed valClass{
	.field int32		int32_Field
	.field static int32	int32_SField
	.field float32		float32_Field
	.field static float32	float32_SField
	.field int32*		int32_ptr_Field
	.field static int32*	int32_ptr_SField
}

.class	public _initoj {

	//-------------------------
	// Entry point - Main     -
	//-------------------------
	.method public static int32 main() {
	.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
	    01 00 00 00
	)
	.entrypoint
	.locals	(value class valClass, int32)
	.maxstack	10
	//----------------
	// begin testing -
	//----------------
	
//Set all valClass fields to a known state
		ldloca	0
		ldc.i4	0xAB
		volatile.
		stfld	int32 valClass::int32_Field	

		ldc.i4	0xCD
		volatile.
		stsfld	int32 valClass::int32_SField	
		
		ldloca	0
		ldc.r4	8.8
		volatile.
		stfld	float32 valClass::float32_Field	

		ldc.r4	9.9
		volatile.
		stsfld	float32 valClass::float32_SField	

		ldloca	0
		ldloca	1
		volatile.
		stfld	int32* valClass::int32_ptr_Field	

		ldloca	1
		volatile.
		stsfld	int32* valClass::int32_ptr_SField	

//Check the state of all valClass fields to be sure they were initialized correctly
		ldloc	0
		volatile.
		ldfld	int32 valClass::int32_Field
		ldc.i4	0xAB
		bne.un	FAIL

		ldsfld	int32 valClass::int32_SField
		ldc.i4	0xCD
		bne.un	FAIL	

		ldloc	0
		volatile.
		ldfld	float32 valClass::float32_Field
		ldc.r4	8.8
		bne.un	FAIL

		ldsfld	float32 valClass::float32_SField
		ldc.r4	9.9
		bne.un	FAIL	

		ldloc	0
		volatile.
		ldfld	int32* valClass::int32_ptr_Field
		ldloca	1
		bne.un	FAIL

		volatile.
	 	ldsfld	int32* valClass::int32_ptr_SField
		ldloca	1
		bne.un	FAIL	

//Call initobj
		ldloca	0
		initobj valClass

//Make sure all fields are set to zero or null and all static fields are unchanged.
		ldloc	0
		volatile.
		ldfld	int32 valClass::int32_Field
		ldc.i4	0x0
		bne.un	FAIL

		volatile.
		ldsfld	int32 valClass::int32_SField
		ldc.i4	0xCD
		bne.un	FAIL	

		ldloc	0
		volatile.
		ldfld	float32 valClass::float32_Field
		ldc.r4	0.0
		bne.un	FAIL

		volatile.
		ldsfld	float32 valClass::float32_SField
		ldc.r4	9.9
		bne.un	FAIL	

		ldloc	0
		volatile.
		ldfld	int32* valClass::int32_ptr_Field
		ldnull
		bne.un	FAIL

		volatile.
		ldsfld	int32* valClass::int32_ptr_SField
		ldloca	1
		bne.un	FAIL	


		PASS:
		ldc.i4	0x64
		ret
	//----------------
		FAIL:
		ldc.i4	0x0
		ret
	}
}
