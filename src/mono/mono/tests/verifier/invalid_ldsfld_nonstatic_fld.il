// Invalid CIL which breaks the ECMA-335,III,4.14 rules. 
// This CIL should fail verification by a conforming CLI verifier.


.assembly 'test_generated'
{
  .hash algorithm 0x00008004
  .ver  0:0:0:0
}

.class public auto ansi beforefieldinit c
  	extends [mscorlib]System.Object
{
	.field public int32 nonstatic
}

.method public static int32 Main() cil managed
{
	.entrypoint
	.maxstack 2
	ldsfld int32 c::nonstatic // Field nonstatic is not static.
	ret
}
