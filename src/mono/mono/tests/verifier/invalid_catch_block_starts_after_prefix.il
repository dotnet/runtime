// INVALID CIL


.assembly 'invalid_overlap_exception_blocks_catch_block_start_in_the_middle_or_a_instruction_generated.il'
{
  .hash algorithm 0x00008004
  .ver  0:0:0:0
}

.method public static int32 Main() cil managed
{
        .entrypoint
        .maxstack 8
	.locals init (object _X0, int32 V0)
 
TRY_BLOCK_1:
	nop
TRY_BLOCK_1_A:
	nop
	newobj instance void class [mscorlib]System.Exception::.ctor()
	throw
	nop
	leave END
TRY_BLOCK_1_END:

	volatile.	
AFTER_PREFIX_2:
	unaligned. 1
	ldind.i4
	stloc.1

	nop
	leave END
CATCH_BLOCK_1_END:

END:
        ldloc.1
        ret

	.try TRY_BLOCK_1 to TRY_BLOCK_1_END catch [mscorlib]System.Exception handler AFTER_PREFIX_2 to CATCH_BLOCK_1_END
}
