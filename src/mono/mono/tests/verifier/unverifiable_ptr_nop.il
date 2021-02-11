.assembly 'unverifiable_ptr_nop'
{
  .hash algorithm 0x00008004
  .ver  0:0:0:0
}

.assembly extern mscorlib
{
  .ver 1:0:5000:0
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) // .z\V.4..
}


.method public static void Foo(method int32 *(float64) arg, method int32 *(int32) arg2) cil managed
{
	.maxstack 2
	.locals init (
	    method int32 *(float64) V_0,
	    method int32 *(int32) V_1
	)
	ldloc.1
	stloc.0 // unverifiable, method int32 *(int32) cannot be stored in method int32 *(float64).
	ret
}

.method public static void Main() cil managed
{
	.maxstack 2
	.entrypoint
	ldnull
	ldnull
	call void Foo(method int32 *(float64) arg, method int32 *(int32) arg2)
	ret
}
