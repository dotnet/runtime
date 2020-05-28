// Invalid CIL which breaks the ECMA-335,III,4.10 rules. 
// This CIL should fail verification by a conforming CLI verifier.

.assembly '${TEST_NAME}_generated'
{
  .hash algorithm 0x00008004
  .ver  0:0:0:0
}


.class public auto ansi beforefieldinit c
  	extends [mscorlib]System.Object
{
	.field public initonly int32 valid

	.method public hidebysig specialname rtspecialname instance default void .ctor () cil managed
	{
		.maxstack 8
		ldarg.0 
		call instance void object::.ctor()
		ret
	}
}

.method public static int32 Main() cil managed
{
	.entrypoint
	.maxstack 2
	newobj instance void c::.ctor()
	ldfld int32 c::valid // Unverifiable, valid initonly.
	pop
	ldc.i4.0
	ret
}
