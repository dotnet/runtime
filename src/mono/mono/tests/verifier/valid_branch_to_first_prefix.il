
.assembly extern mscorlib
{
  .ver 2:0:0:0
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) // .z\V.4..
}

.assembly 'cmmp_test'
{
  .hash algorithm 0x00008004
  .ver  0:0:0:0
}

.module cmmp.exe

.class ClassA extends [mscorlib]System.Object
{
    .field public int32 valid
    .field public static int32 test

	.method public hidebysig  specialname  rtspecialname instance default void .ctor ()  cil managed 
	{
		.maxstack 8
		ldarg.0 
		call instance void object::.ctor()
		ret 
	}
}



.method public static int32 Main ()
{
	.entrypoint
	.maxstack 8
	.locals init (ClassA V_0)


	newobj instance void class ClassA::.ctor()
	stloc.0

	.try {
		volatile. ldsfld int32 ClassA::test
		nop
		nop
		leave DONE
	} finally {
		nop
		endfinally
	}

DONE:
		volatile. ldsfld int32 ClassA::test
		nop
		nop
		leave END
END:
	ldc.i4.0
	ret 
}

