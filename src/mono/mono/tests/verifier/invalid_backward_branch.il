// Invalid CIL which breaks the ECMA-335,III,1.7.5 rule "Backward branch 
// constraints". This CIL should be rejected by a conforming CLI verifier 
// or runtime without being run.

.method public static int32 Main() cil managed
{
	.entrypoint
	.maxstack 1
	br end
	branch_target: // Follows unconditional branch, stack empty.
	ldc.i4.0
	br branch_target // Stack not empty, invalid.
	end:
	ldc.i4.0
	ret
}