// Invalid CIL which breaks the ECMA-335,III,3.19 rule. 
// This CIL should fail verification by a conforming CLI verifier.


.assembly 'test_generated'
{
  .hash algorithm 0x00008004
  .ver  0:0:0:0
}


.method public static int32 f(int32 i) cil managed
{
	ldc.i4.0
	ret
}

.method public static int32 Main() cil managed
{
	.entrypoint
	.maxstack 1
	call int32 f(int32) // Invalid, stack empty.
	ret
}
