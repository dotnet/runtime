
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
	.locals init (object _X0)
 
	br BLOCK_1
        ldc.i4 0x7FFFFFFF
	pop

BLOCK_1:
	nop
	.try {
		nop
		newobj instance void class [mscorlib]System.Exception::.ctor()
		throw
		nop
IN_TRY:
BLOCK_2:
		leave END
	} catch [mscorlib]System.Exception {
		stloc.0
		ldstr "----catch {0}"
		ldloc.0
		call void class [mscorlib]System.Console::WriteLine(string, object)
		
		nop
IN_CATCH:
BLOCK_3:
		nop
		leave HERE
	}

HERE:
	.try {
		leave HERE_2
	} finally {
		nop
		
		nop
IN_FINALLY:
BLOCK_4:
		nop
		endfinally
	}

HERE_2:
	.try {
		newobj instance void class [mscorlib]System.Exception::.ctor()
		throw
		leave END
	} filter {
		stloc.0
		ldstr "---filter {0}"
		ldloc.0
		call void class [mscorlib]System.Console::WriteLine(string, object)
		nop
		
		nop
IN_FILTER:
BLOCK_5:
		nop
		ldc.i4.1
		endfilter
	}
	{
		stloc.0
		ldstr "---handler {0}"
		ldloc.0
		call void class [mscorlib]System.Console::WriteLine(string, object)
		nop
		
		nop
IN_HANDLER:
BLOCK_6:
		nop
		leave END
	}

END:
        ldc.i4.0
        ret
}
