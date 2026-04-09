.assembly extern mscorlib { }

.assembly test
{
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}

.class private auto ansi beforefieldinit test.Test extends [mscorlib]System.Object
{
	.field private static int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) non_constant

	.method private hidebysig specialname rtspecialname static void '.cctor' () cil managed
	{
		.maxstack  8
		ldc.i4.0
		volatile.
		stsfld     int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) test.Test::non_constant
		ret
	}

	.method public specialname rtspecialname instance default void '.ctor' () cil managed
	{
		.maxstack 8
		ldarg.0
		call instance void object::'.ctor'()
		ret
	}

	// If this test succeeds, it should run to completion.
	// If it fails, mono will hang in an infinite loop while doing DCE.
	.method public static hidebysig default void Main () cil managed
	{
		.maxstack 8
		.entrypoint
		volatile.
		ldsfld int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) test.Test::non_constant
		ldc.i4.0
		cgt
		brfalse.s end
		break // Should not be executed; merely needs to be present in the instruction stream.
		end:
		ret
	}
}

