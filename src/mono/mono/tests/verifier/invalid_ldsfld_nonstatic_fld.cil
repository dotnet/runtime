// Invalid CIL which breaks the ECMA-335,III,4.14 rules. 
// This CIL should fail verification by a conforming CLI verifier.

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