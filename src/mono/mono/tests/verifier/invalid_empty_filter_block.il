.assembly 'invalid_empty_filter_block'
{
  .hash algorithm 0x00008004
  .ver  0:0:0:0
}

.method public static int32 Main() cil managed
{
	.entrypoint
	.maxstack 8
	.locals init (int32 V0)

BB_00:
		newobj instance void class [mscorlib]System.Exception::.ctor()
		throw
		leave END
BB_01:

BB_02:
		nop
		nop
		br END
BB_03:

BB_04:
		pop
		leave END
BB_05:



END:
	ldc.i4.0
	ret

.try BB_00 to BB_01 filter BB_03 handler BB_04 to BB_05


}
