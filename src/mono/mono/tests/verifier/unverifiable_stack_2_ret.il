// invalid CIL which breaks the ECMA-335 rules. 
// This CIL should fail verification by a conforming CLI verifier.

.assembly '_generated'
{
  .hash algorithm 0x00008004
  .ver  0:0:0:0
}

.class Class extends [mscorlib]System.Object
{
    .field public int32 fld
}

.method public static int32 Foo(int32 x) cil managed
{
	.maxstack 2
	ldarg.0
	ldarg.0
	ret // invalid, stack with two elements.
}

.method public static void Main() cil managed
{
	.entrypoint
	.maxstack 2
	ldarg.0
	call int32 Foo(int32)
	pop
	ret
}
