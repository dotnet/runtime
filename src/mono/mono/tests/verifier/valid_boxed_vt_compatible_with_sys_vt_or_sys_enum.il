.assembly extern mscorlib
{
  .ver 2:0:0:0
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) // .z\V.4..
}
.assembly 'ltest-07'
{
  .hash algorithm 0x00008004
  .ver  0:0:0:0
}
.module 'ltest-07.exe'


.class public auto ansi beforefieldinit Driver extends [mscorlib]System.Object
{
	.method public static hidebysig default void WithEnum ([mscorlib]System.Enum a) cil managed 
	{
		ret
	}

	.method public static hidebysig default void WithValueType ([mscorlib]System.ValueType a) cil managed 
	{
		ret
	}

	.method public static hidebysig default void Main ()  cil managed 
	{
		.entrypoint
		.maxstack 8
		.locals init (valuetype [mscorlib]System.Nullable`1<valuetype [mscorlib]System.DayOfWeek>	V_0,
					  valuetype [mscorlib]System.DayOfWeek V_2)

		ldc.i4.1
		box int32
		call void Driver::WithValueType([mscorlib]System.ValueType)

		ldloc.1
		box valuetype [mscorlib]System.DayOfWeek
		call void Driver::WithValueType([mscorlib]System.ValueType)

		ldloc.1
		box valuetype [mscorlib]System.DayOfWeek
		call void Driver::WithEnum ([mscorlib]System.Enum)

		ldloc.0
		box valuetype [mscorlib]System.Nullable`1<valuetype [mscorlib]System.DayOfWeek>
		call void Driver::WithValueType([mscorlib]System.ValueType)
		
		ldloc.0
		box valuetype [mscorlib]System.Nullable`1<valuetype [mscorlib]System.DayOfWeek>
		call void Driver::WithEnum ([mscorlib]System.Enum)

		ret
	}
}


