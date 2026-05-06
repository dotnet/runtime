
.assembly extern mscorlib
{
  .ver 2:0:0:0
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) // .z\V.4..
}
.assembly 'bla'
{
  .hash algorithm 0x00008004
  .ver  0:0:0:0
}
.module isinst_push_boxed_type_arg.exe


.class public auto ansi beforefieldinit Test<T>
        extends [mscorlib]System.Object
{
	.method public static void Method ()
	{
		.locals init (object V_0)

		ldloc.0
		isinst !T
		brtrue END
		nop
		nop
END:
		ret
	}
}


.method public static int32 Main ()
{
	.entrypoint
	.maxstack 8
	call void class Test<int32>::Method()
	ldc.i4.0
	ret 
}

 


