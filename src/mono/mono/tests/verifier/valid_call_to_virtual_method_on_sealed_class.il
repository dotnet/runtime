.assembly extern mscorlib
{
  .ver 2:0:0:0
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) // .z\V.4..
}
.assembly 'cast'
{
  .hash algorithm 0x00008004
  .ver  0:0:0:0
}
.module cast.exe // GUID = {9CC094E7-72A4-43FA-B1DD-E936A7B60966}

.class private auto ansi beforefieldinit X extends [mscorlib]System.Object
{
	.method private static hidebysig default void Main ()  cil managed 
	{
		.entrypoint

		ldstr "test"
		call instance int32 [mscorlib]System.String::GetHashCode()
		pop
		ret
	}

}

