// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.



.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly 'initobj'{
}

.namespace initobj_
{
.class public value sealed valClass{
	.field public int32		int32_Field
	.field public static int32	int32_SField
	.field public float32		float32_Field
	.field public static float32	float32_SField
	.field public int32*		int32_ptr_Field
	.field public static int32*	int32_ptr_SField

	.method public void .ctor(){
		ret
	}
}

.class public	_initoj {

	.method public static int32 main() {
	.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
	    01 00 00 00
	)
	.entrypoint
	.locals	(value class initobj_.valClass, int32)
	.maxstack	10
	
		ldloca	0
		initobj initobj_.valClass

                ldloca  0
                ldc.i4  0xAB
                stfld   int32 initobj_.valClass::int32_Field

                ldc.i4  0xCD
                stsfld  int32 initobj_.valClass::int32_SField

                ldloca  0
                ldc.r4  8.8
                stfld   float32 initobj_.valClass::float32_Field

                ldc.r4  9.9
                stsfld  float32 initobj_.valClass::float32_SField

                ldloca  0
                ldloca  1
                stfld   int32* initobj_.valClass::int32_ptr_Field

		ldloca	1
		stsfld	int32* initobj_.valClass::int32_ptr_SField	

                ldloca  0
                ldfld   int32 initobj_.valClass::int32_Field
                ldc.i4  0xAB
                bne.un  FAIL

                ldsfld  int32 initobj_.valClass::int32_SField
                ldc.i4  0xCD
                bne.un  FAIL

                ldloca  0
                ldfld   float32 initobj_.valClass::float32_Field
                ldc.r4  8.8
                bne.un  FAIL

                ldsfld  float32 initobj_.valClass::float32_SField
                ldc.r4  9.9
                bne.un  FAIL

                ldloca  0
                ldfld   int32* initobj_.valClass::int32_ptr_Field
                ldloca  1
                bne.un  FAIL

                ldsfld  int32* initobj_.valClass::int32_ptr_SField
                ldloca  1
                bne.un  FAIL

		ldloca	0
		initobj initobj_.valClass

                ldloca  0
                ldfld   int32 initobj_.valClass::int32_Field
                ldc.i4  0x0
                bne.un  FAIL

                ldsfld  int32 initobj_.valClass::int32_SField
                ldc.i4  0xCD
                bne.un  FAIL

                ldloca  0
                ldfld   float32 initobj_.valClass::float32_Field
                ldc.r4  0.0
                bne.un  FAIL

                ldsfld  float32 initobj_.valClass::float32_SField
                ldc.r4  9.9
                bne.un  FAIL

                ldloca  0
                ldfld   int32* initobj_.valClass::int32_ptr_Field
                ldnull
                bne.un  FAIL

                ldsfld  int32* initobj_.valClass::int32_ptr_SField
                ldloca  1
                bne.un  FAIL


                PASS:
                ldc.i4 100
                ret
                FAIL:
                ldc.i4  0x0
                ret
        }
}
}
