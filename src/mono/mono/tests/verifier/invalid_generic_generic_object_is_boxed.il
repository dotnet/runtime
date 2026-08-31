.assembly extern mscorlib
{
	.ver 2:0:0:0
	.publickeytoken = (B7 7A 5C 56 19 34 E0 89 )
}
.assembly 'repro'
{
	.hash algorithm 0x00008004
	.ver  0:0:0:0
}
.module repro.exe


.class private auto ansi abstract sealed beforefieldinit Driver extends [mscorlib]System.Object
{
	.method private static  hidebysig 
	default void M<(class [mscorlib]System.Exception) T> () cil managed 
	{
		.maxstack 1
		.locals init (!!T V_0)
		.try {
			leave END
		}
		catch !!T {
			stloc.0 
			leave END
		}
END:
		ret
	}

	.method private static hidebysig default void Main () cil managed 
	{
		.entrypoint
		.maxstack 8
		call void class Driver::M<class [mscorlib]System.TypeLoadException> ()
		ret 
	}
}

