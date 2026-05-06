.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 2:0:0:0
}


.assembly 'test-32'
{
  .ver 0:0:0:0
}
.module 'test-32'

.class public auto ansi beforefieldinit Driver extends [mscorlib]System.Object
{
	.method public static void Test ()
	{
		.maxstack 3
		.locals init (int32 V_0)

		.try
		{
			leave LABEL_0
		}
		catch [mscorlib]System.Object
		{
			pop

			.try
			{
				br.s LABEL_1
LABEL_2:
				nop
LABEL_1:
				ldloc.0
				brtrue.s LABEL_2
				leave.s LABEL_3
		 	}
			finally
			{
				endfinally
			}
LABEL_3:
			rethrow
		}
LABEL_0:
		nop
		ret
	}

	.method public hidebysig static int32  Main() cil managed
	{
		.entrypoint
		.maxstack  8
		.locals init ()
		call void Driver::Test ()
		ldc.i4.0
		ret
	}

}