.assembly 'invalid_try_block_inside_filter_block'
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
		pop
		ldloc.0
		brfalse BB_N03

BB_N00:
		newobj instance void class [mscorlib]System.Exception::.ctor()
		throw
		leave END
BB_N01:

BB_N02:
		pop
		leave END
BB_N03:

		ldc.i4.0
		endfilter
BB_03:

BB_04:
		leave END
BB_05:


END:
	ldc.i4.0
	ret

.try BB_N00 to BB_N01 catch [mscorlib]System.Exception handler BB_N02 to BB_N03
.try BB_00 to BB_01 filter BB_02 handler BB_04 to BB_05

}
