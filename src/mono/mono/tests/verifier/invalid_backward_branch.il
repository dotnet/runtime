// Invalid CIL which breaks the ECMA-335,III,1.7.5 rule "Backward branch 
// constraints". This CIL should be rejected by a conforming CLI verifier 
// or runtime without being run.

.assembly 'test_generated'
{
  .hash algorithm 0x00008004
  .ver  0:0:0:0
}



.method public static int32 Main() cil managed
{
	.entrypoint
	.maxstack 2
	br end
	branch_target: // Follows unconditional branch, stack empty.
	ldc.i4.0
	br branch_target // Stack not empty, invalid.
	end:
	ldc.i4.0
	pop
	//the verifier can do DCE before performing stack merging
	newobj instance void object::.ctor()
	callvirt instance int32 object::GetHashCode()
	beq.s branch_target
	ldc.i4.0
	ret
}
