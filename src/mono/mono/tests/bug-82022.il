.assembly extern mscorlib { }
.assembly test
{
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}

.class private auto ansi sealed FieldAttributes extends [mscorlib]System.Enum
{
}

.class private auto ansi beforefieldinit Program extends [mscorlib]System.Object
{
	.method private hidebysig static int32 Main() cil managed
	{
		.entrypoint
		.maxstack	2
		.locals init (
			class [mscorlib]System.Exception	V_0,
			int32	V_1)
		.try {
			call int32 Program::triggerError ()
			stloc.1
			leave end_block
		}
		catch [mscorlib]System.Exception { 
			stloc.0
			ldc.i4.0
			stloc.1
			leave end_block
		}
end_block:
		ldloc.1
		ret
	}

	.method static int32 triggerError () cil managed
	{
		.locals init (class [mscorlib]System.Type V_0)
		ldtoken FieldAttributes
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		stloc.0
		ldloc.0
		callvirt instance string [mscorlib]System.Type::get_FullName()
		pop
		ldc.i4.1
		ret
	}


	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed
	{
		.maxstack	1
		ldarg.0
		call			 instance void [mscorlib]System.Object::.ctor()
		ret
	}
}

.class private auto ansi sealed FieldAttributes extends [mscorlib]System.Enum
{
}

