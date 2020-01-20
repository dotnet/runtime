
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

.module bne_with_generic_type_type.exe


.class public auto ansi beforefieldinit Test
        extends [mscorlib]System.Object
{
	.method public static void Method<T> ()
	{
		//.locals init (!!T V_0, !!T V_1)
		.locals init ( valuetype [mscorlib] System.DateTime V_0, valuetype [mscorlib] System.DateTime V_1)

		ldloc.0
		ldloc.1
		bne.un END
		nop
END:
		ret
	}
}


.method public static int32 Main ()
{
	.entrypoint
	.maxstack 8

	call void class Test::Method<int32>()

	ldc.i4.0
	ret 
}



