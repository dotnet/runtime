
//This scenario is not quite possible, as the only block before the handler is the filter block
//So we test a filter without endfilter at end
.assembly 'invalid_fallthru_into_handler_block'
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
		nop
		nop
BB_03:


BB_04:
		nop
		nop
		leave END
BB_05:

END:
	ldc.i4.0
	ret

.try BB_00 to BB_01 filter BB_02 handler BB_04 to BB_05


}
