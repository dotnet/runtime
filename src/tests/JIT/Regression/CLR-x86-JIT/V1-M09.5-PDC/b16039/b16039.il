// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly bug
{
}
.assembly extern xunit.core {}
.class public auto ansi AA extends [mscorlib]System.Object
{

	//	Class members
	.field public int32 m_aa   // 944150320

	//	Class constructors
	.method public specialname rtspecialname instance void .ctor() il managed
	{
		ldarg.0
		call instance void [mscorlib]System.Object::.ctor()
		ldarg.0
		ldc.i4 944150320
		stfld int32 AA::m_aa
		ret
	}
}
.class public auto ansi BB extends [mscorlib]System.Object
{
	//	Static class members
	.field public static int8 m_bb   // 17

	//	Class constructors
	.method public specialname rtspecialname instance void .ctor() il managed
	{
		.maxstack 8
		ldarg.0
		call instance void [mscorlib]System.Object::.ctor()
		ret
	}
	.method public static specialname rtspecialname void .cctor() il managed
	{
		.maxstack 8
		ldc.i4 17
		stsfld int8 BB::m_bb
		ret
	}

	.method public static unsigned int32 bb(unsigned int8 param_F6C8_21C9, unsigned int8 param_CEA8_12A5, int8 param_509D_0A16, int16 param_9CE2_5B30) il managed
	{
		.maxstack 8
		newobj instance void AA::.ctor()
		ldflda int32 AA::m_aa
		ldind.i4
		ldc.i4 2075773924
		add.ovf			//	this will blow up
		ret
	}
}
.class public auto ansi MainClass extends [mscorlib]System.Object
{
	.method static int32 Main()
	{
		.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
		    01 00 00 00
		)
		.entrypoint
		.maxstack 8
		.locals (int32)
		ldc.i4.1
		stloc.0
		.try {
			ldc.i4 29
			ldc.i4 13
			ldc.i4 -15
			ldc.i4 7319
			call unsigned int32 BB::bb(unsigned int8, unsigned int8, int8, int16)
			pop
			leave Beyond
		} catch [mscorlib]System.OverflowException
		{
			pop
			ldc.i4.0
			stloc.0
			leave Beyond
		}
    Beyond:
		ldloc.0
		ldc.i4 100
		add
		ret
	}
}
