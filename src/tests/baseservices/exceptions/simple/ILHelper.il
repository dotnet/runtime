// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern mscorlib {}
.assembly ILHelper {}
.module ILHelper.dll

.class public auto ansi beforefieldinit ILHelper
			 extends [mscorlib]System.Object
{
	.method public hidebysig static bool Int32Overflow() cil managed
	{
	.locals init (int32 V_0,
			 int32 V_1,
			 int32 V_2)
		ldc.i4		 0x7fffffff
		stloc.0
		ldc.i4.2
		stloc.1
		ldloc.0
		ldloc.1
		// changed to include .ovf
		add.ovf
		stloc.2
		ldc.i4.1
		ret
	}

	.method public hidebysig static bool Int64Overflow() cil managed
	{
	.locals init (int64 V_0,
			 int64 V_1,
			 int64 V_2)
		ldc.i8		 0x7fffffffffffffff
		stloc.0
		ldc.i4.2
		conv.i8
		stloc.1
		ldloc.0
		ldloc.1
		// changed to include .ovf
		mul.ovf
		stloc.2
		ldc.i4.1
		ret
	}

	.method public hidebysig specialname rtspecialname 
					instance void	.ctor() cil managed
	{
		ldarg.0
		call			 instance void [mscorlib]System.Object::.ctor()
		ret
	}

}
