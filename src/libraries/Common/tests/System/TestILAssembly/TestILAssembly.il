// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Runtime
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A)
  .ver 4:0:0:0
}

.assembly TestILAssembly
{
    .ver 1:0:0:0
}

.module TestILAssembly.dll

.namespace System.Tests
{
	.class public auto ansi beforefieldinit GenericWithModifiers
				extends [System.Runtime]System.Object
	{
		.method public hidebysig specialname rtspecialname 
				instance void  .ctor() cil managed
		{
			.maxstack  8
			IL_0000:  ldarg.0
			IL_0001:  call       instance void [System.Runtime]System.Object::.ctor()
			IL_0006:  nop
			IL_0007:  ret
		}

		.method public hidebysig instance void  MethodWithGenericParameter(class [System.Runtime]System.Tuple`2<int32,bool modopt([System.Runtime]System.Runtime.CompilerServices.IsConst)> t) cil managed
		{
			.maxstack  8
			IL_0000:  nop
			IL_0001:  ret
		}

		.method public hidebysig instance void  GenericMethod<T>(!!T modopt([System.Runtime]System.Runtime.CompilerServices.IsConst) t) cil managed
		{
			.maxstack  8
			IL_0000:  nop
			IL_0001:  ret
		}

		.field public class [System.Runtime]System.Tuple`2<int32,bool modopt([System.Runtime]System.Runtime.CompilerServices.IsConst)> GenericField

		.method public hidebysig specialname instance class [System.Runtime]System.Tuple`2<int32,bool modopt([System.Runtime]System.Runtime.CompilerServices.IsConst)> get_GenericField () cil managed
		{
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: ldfld class [System.Runtime]System.Tuple`2<int32,bool modopt([System.Runtime]System.Runtime.CompilerServices.IsConst)> System.Tests.GenericWithModifiers::GenericField
			IL_0006: ret
		}

		.property instance class [System.Runtime]System.Tuple`2<int32,bool modopt([System.Runtime]System.Runtime.CompilerServices.IsConst)> GenericProperty()
		{
			.get instance class [System.Runtime]System.Tuple`2<int32,bool modopt([System.Runtime]System.Runtime.CompilerServices.IsConst)> System.Tests.GenericWithModifiers::get_GenericField()
		}
	}
}
