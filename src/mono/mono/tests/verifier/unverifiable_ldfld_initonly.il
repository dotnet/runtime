// Invalid CIL which breaks the ECMA-335,III,4.10 rules. 
// This CIL should fail verification by a conforming CLI verifier.

.class public auto ansi beforefieldinit c
  	extends [mscorlib]System.Object
{
	.field public initonly int32 valid
}

.method public static int32 Main() cil managed
{
	.entrypoint
	.maxstack 1
	.locals init (
		class c
	)
	ldloc.0
	ldfld int32 c::valid // Unverifiable, valid initonly.
	pop
	ldc.i4.0
	ret
}