.assembly extern mscorlib
{
  .ver 2:0:0:0
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) // .z\V.4..
}
.assembly 'tt'
{
  .custom instance void class [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::'.ctor'() =  (
		01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
		63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01       ) // ceptionThrows.

  .hash algorithm 0x00008004
  .ver  0:0:0:0
}
.module tt.exe // GUID = {3AC42A5A-D344-4AED-B39B-FCA95938B725}

  .class private auto ansi beforefieldinit Driver
  	extends [mscorlib]System.Object
  {

    .method private static  hidebysig default void Main ()  cil managed 
    {
	.entrypoint
	.maxstack 0
	.locals init (
		int32	V_0,
		int32    V_1)

		.try {
		  leave END
		}
		catch class [mscorlib]System.Object { // 0
		  pop 
		  leave END
		}
END:
		ret 
    }
}
