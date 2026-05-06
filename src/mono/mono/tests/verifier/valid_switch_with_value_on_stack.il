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
.module cast.exe

.class private auto ansi beforefieldinit Driver
{
	.method static default void Test ()  cil managed 
	{
		ret
	}


	.method private static hidebysig default void Main ()  cil managed 
	{
		.entrypoint

		ldc.i4.1
		ldc.i4.1
		switch (LB_1)

		ldc.i4.1
		nop
		br.s LB_2

LB_1:
		ldc.i4.2
		nop

LB_2:
		pop
		pop
		nop

		ret
	}


}

