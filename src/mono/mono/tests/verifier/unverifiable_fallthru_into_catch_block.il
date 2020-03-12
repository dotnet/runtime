.assembly 'invalid_fallthru_into_catch_block'
{
  .hash algorithm 0x00008004
  .ver  0:0:0:0
}

.method public static int32 Main() cil managed
{
	.entrypoint
	.maxstack 8
	.locals init (int32 V0)
	
	ldloc.0
	brfalse BB_MIDDLE

BB_00:
		newobj instance void class [mscorlib]System.Exception::.ctor()
		throw
		leave END
BB_01:

BB_MIDDLE:
		nop
		nop
BB_02:
		pop
		nop
		nop
		leave END
BB_03:


END:
	ldc.i4.0
	ret

.try BB_00 to BB_01 catch [mscorlib]System.Exception handler BB_02 to BB_03


}
