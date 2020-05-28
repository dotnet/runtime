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

.class public auto ansi beforefieldinit Driver extends [mscorlib]System.Object
{
	.method private static  hidebysig default void Main ()  cil managed 
	{
		.entrypoint
		.maxstack 1
_TRY_START: //0
		leave END
_CATCH_START: //05
		pop
		leave END
END: //0b
		ret 
		//.try 0x0 to 0x5 catch [mscorlib]System.Exception handler 0x5 to 0xb //this is ok
		//.try 0x0 to 0x5 catch [mscorlib]System.Exception handler 0x5 to 0xb
		//.try _TRY_START to _CATCH_START catch [mscorlib]System.Exception handler _CATCH_START to END
		.try 0x0 to 0x5 catch [mscorlib]System.Exception handler 0x5 to 0xbbb
	}
}

