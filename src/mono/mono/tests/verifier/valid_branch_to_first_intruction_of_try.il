
// valid CIL which breaks the ECMA-335 rules. 
// this CIL should fail verification by a conforming CLI verifier.

.assembly 'valid_branch_inside_same_block_1_generated'
{
  .hash algorithm 0x00008004
  .ver  0:0:0:0
}

.class ClassA
	extends [mscorlib]System.Object
{
}

.class sealed MyValueType
	extends [mscorlib]System.ValueType
{
	.field private int32 v
}

.class public Template`1<T>
  	extends [mscorlib]System.Object
{
}

.class sealed public ValueTypeTemplate`1<T>
  	extends [mscorlib]System.ValueType
{
	.field private int32 v
}

.method public static int32 Main() cil managed
{
        .entrypoint
        .maxstack 8
	.locals init (object _X0, int32 _X1)
 
	nop
	nop
	ldloc.1
	brtrue BLOCK_1

BLOCK_1:
	.try {
		nop
		newobj instance void class [mscorlib]System.Exception::.ctor()
		throw
		nop

		leave END
	} catch [mscorlib]System.Exception {
		stloc.0
		leave END
	}

END:
        ldc.i4.0
        ret
}
