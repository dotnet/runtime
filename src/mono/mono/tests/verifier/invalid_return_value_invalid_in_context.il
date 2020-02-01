.assembly extern mscorlib
{
  .ver 2:0:0:0
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) // .z\V.4..
}
.assembly 'repro'
{
  .custom instance void class [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::'.ctor'() =  (
		01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
		63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01       ) // ceptionThrows.

  .hash algorithm 0x00008004
  .ver  0:0:0:0
}
.module repro.exe


.class private auto ansi beforefieldinit Program extends [mscorlib]System.Object
{
	.method private static hidebysig default !22 Test ()  cil managed 
	{
		.maxstack 1
		.locals init ()
		ldnull
		ret
	}

	.method private static  hidebysig default int32 Main ()  cil managed 
	{
		.entrypoint
		.maxstack 1
		.locals init ()
		call !22 Program::Test ()
		pop

		ldc.i4.0 
		ret
	}
}

