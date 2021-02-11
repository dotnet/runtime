.assembly extern mscorlib
{
  .ver 2:0:0:0
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) // .z\V.4..
}
.assembly 'repro'
{
  .hash algorithm 0x00008004
  .ver  0:0:0:0
}

.module repro.exe


.class public auto ansi beforefieldinit Driver extends [mscorlib]System.Object
{
	.method private static  hidebysig  default void Main ()  cil managed 
	{
		.entrypoint
		.maxstack 8

		br AFTER_END
END:
		ret 
AFTER_END:
		.try {
			leave END
		} catch [mscorlib]System.Exception
		{
			pop
			leave END
		}
	}
}
