// Invalid CIL which breaks the ECMA-335,III,3.33 rules. 
// This CIL should fail verification by a conforming CLI verifier.


.assembly 'test_generated'
{
  .hash algorithm 0x00008004
  .ver  0:0:0:0
}

.method public static int32 Main() cil managed
{
	.entrypoint
	.maxstack 1
	ldc.i4.0
	dup
	dup
	dup
	dup
	dup
	dup
	dup
	dup
	dup // Invalid, stack larger than maxstack.
	pop
	pop
	pop
	pop
	pop
	pop
	pop
	pop
	pop
	ret
}
