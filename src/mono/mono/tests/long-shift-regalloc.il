.assembly extern mscorlib
{
        .ver 0:0:0:0
}
.assembly 'long-shift-regalloc'
{
  .hash algorithm 0x00008004
  .ver  0:0:0:0
}

.module '<Module>'

.method public static int32 '.start'(class [mscorlib]System.String[] 'args') cil managed
{
	.entrypoint
	.maxstack 3
	.locals (int32 'argc', int8** 'argv', int8** 'env')

	ldc.i8	81985529234382576
	stsfld	unsigned int64 'ull'

       	ldc.i4	60
	stsfld	int32 'shift2'

	call	int32 main()
	ret
} // .start


.method public static int32 'main' () cil managed
{
	.locals (unsigned int32 'cilsimp.28', unsigned int64 'cilsimp.27', int32 'cilsimp.26')

	ldsfld	unsigned int64 'ull'
	stloc	'cilsimp.27'
	ldsfld	int32 'shift2'
	stloc	'cilsimp.28'
	ldloc	'cilsimp.27'
	ldloc	'cilsimp.28'
	shr.un
	ldloc	'cilsimp.27'
	ldc.i4	64
	ldloc	'cilsimp.28'
	sub
	shl
	or
	ldc.i8	1311768467750121216
	ceq
	ldc.i4.1
	xor
	conv.u4
	ret
	.maxstack 4

} // main

.field public  static unsigned int64 'ull'

.field public  static int32 'shift2'

