.assembly extern mscorlib
{
  .ver 2:0:0:0
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )
}
.assembly 'simple_assembly'
{
  .hash algorithm 0x00008004
  .ver  0:0:0:0
}

.module simple_assembly.exe

.class sealed public auto ansi beforefieldinit Generic`1<T> 
{
	.method public hidebysig  specialname rtspecialname instance default void '.ctor' ()
	{
		.maxstack 8
		ldarg.0 
		call instance void object::'.ctor'()
		ret 
	}

	.field public !T a
	.field public int32 a
}


.class sealed public auto ansi beforefieldinit Regular 
{
	.method public hidebysig  specialname rtspecialname instance default void '.ctor' ()
	{
		.maxstack 8
		ldarg.0 
		call instance void object::'.ctor'()
		ret 
	}

	.field public float32 a
	.field public int32 a
}

.class public auto ansi beforefieldinit Program
{
	.method public static int32 Main () cil managed 
	{
		.entrypoint
		.maxstack 8
		.locals init (class Generic`1<float32> V_0, class Regular	V_1)	

		newobj instance void class Generic`1<float32>::'.ctor'()
		stloc.0 
		ldloc.0 
		ldc.r4 2.
		stfld !0 class Generic`1<float32>::a
		ldloc.0 
		ldc.i4.s 0x0a
		stfld int32 class Generic`1<float32>::a
		ldloc.0 
		ldfld !0 class Generic`1<float32>::a
		ldc.r4 2.
		beq TEST_2

		ldc.i4.1 
		ret
TEST_2:
		ldloc.0 
		ldfld int32 class Generic`1<float32>::a
		ldc.i4.s 0x0a
		beq TEST_3

		ldc.i4.2 
		ret
TEST_3:
		newobj instance void class Regular::'.ctor'()
		stloc.1 
		ldloc.1 
		ldc.r4 2.
		stfld float32 Regular::a
		ldloc.1 
		ldc.i4.s 0x0a
		stfld int32 Regular::a
		ldloc.1 
		ldfld float32 Regular::a
		ldc.r4 2.
		beq TEST_4

		ldc.i4.3 
		ret
TEST_4:
		ldloc.1
		ldfld int32 Regular::a
		ldc.i4.s 0x0a
		beq GOOD

		ldc.i4.4 
		ret 
GOOD:
		ldc.i4.0 
		ret	
	}
}

