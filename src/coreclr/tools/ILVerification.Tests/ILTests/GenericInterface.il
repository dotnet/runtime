// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly GenericInterface
{
}

.assembly extern System.Runtime
{
}

.class interface public auto ansi abstract IGeneric`2<TIN, TOUT>
{	
	.method public hidebysig newslot abstract virtual 
		instance !TOUT M1 (
			!TIN par,
			!TIN par2
		) cil managed 
	{
	}
} 

.class public auto ansi beforefieldinit GenericImplementationInt32_ValidType_Valid
	extends [System.Runtime]System.Object
	implements class IGeneric`2<int32, int32>
{	
	.method public final hidebysig newslot virtual 
		instance int32 M1 (
			int32 par,
			int32 par2
		) cil managed 
	{
		.maxstack 1
		.locals init (
			[0] int32
		)

		IL_0000: nop
		IL_0001: ldc.i4.0
		IL_0002: stloc.0
		IL_0003: br.s IL_0005

		IL_0005: ldloc.0
		IL_0006: ret
	}

	.method public hidebysig specialname rtspecialname 
		instance void .ctor () cil managed 
	{
		.maxstack 8

		IL_0000: ldarg.0
		IL_0001: call instance void [System.Runtime]System.Object::.ctor()
		IL_0006: nop
		IL_0007: ret
	}

}

.class public auto ansi beforefieldinit GenericsMissingMethod_InvalidType_InterfaceMethodNotImplemented
	extends [System.Runtime]System.Object
	implements class IGeneric`2<int32, int32>
{	
	.method public hidebysig specialname rtspecialname 
		instance void .ctor () cil managed 
	{
		.maxstack 8

		IL_0000: ldarg.0
		IL_0001: call instance void [System.Runtime]System.Object::.ctor()
		IL_0006: nop
		IL_0007: ret
	}

}

.class public auto ansi beforefieldinit GenericsInvalidReturnParameter_InvalidType_InterfaceMethodNotImplemented
	extends [System.Runtime]System.Object
	implements class IGeneric`2<int32, int32>
{	
	.method public final hidebysig newslot virtual 
		instance int64 M1 (
			int32 par,
			int32 par2
		) cil managed 
	{
		.maxstack 1
		.locals init (
			[0] int32
		)

		IL_0000: nop
		IL_0001: ldc.i4.0
		IL_0002: stloc.0
		IL_0003: br.s IL_0005

		IL_0005: ldloc.0
		IL_0006: ret
	}

	.method public hidebysig specialname rtspecialname 
		instance void .ctor () cil managed 
	{
		.maxstack 8

		IL_0000: ldarg.0
		IL_0001: call instance void [System.Runtime]System.Object::.ctor()
		IL_0006: nop
		IL_0007: ret
	}
}

.class public auto ansi beforefieldinit GenericsMissingParameter_InvalidType_InterfaceMethodNotImplemented
	extends [System.Runtime]System.Object
	implements class IGeneric`2<int32, int32>
{	
	.method public final hidebysig newslot virtual 
		instance int32 M1 (int32 par2) cil managed 
	{
		.maxstack 1
		.locals init (
			[0] int32
		)

		IL_0000: nop
		IL_0001: ldc.i4.0
		IL_0002: stloc.0
		IL_0003: br.s IL_0005

		IL_0005: ldloc.0
		IL_0006: ret
	}

	.method public hidebysig specialname rtspecialname 
		instance void .ctor () cil managed 
	{
		.maxstack 8

		IL_0000: ldarg.0
		IL_0001: call instance void [System.Runtime]System.Object::.ctor()
		IL_0006: nop
		IL_0007: ret
	}
}

.class public auto ansi beforefieldinit GenericsInvalidInTypeParameter_InvalidType_InterfaceMethodNotImplemented
	extends [System.Runtime]System.Object
	implements class IGeneric`2<int32, int32>
{	
	.method public final hidebysig newslot virtual 
		instance int64 M1 (
			int64 par,
			int32 par2
		) cil managed 
	{
		.maxstack 1
		.locals init (
			[0] int32
		)

		IL_0000: nop
		IL_0001: ldc.i4.0
		IL_0002: stloc.0
		IL_0003: br.s IL_0005

		IL_0005: ldloc.0
		IL_0006: ret
	}

	.method public hidebysig specialname rtspecialname 
		instance void .ctor () cil managed 
	{
		.maxstack 8

		IL_0000: ldarg.0
		IL_0001: call instance void [System.Runtime]System.Object::.ctor()
		IL_0006: nop
		IL_0007: ret
	}
}
