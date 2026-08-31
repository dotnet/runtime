// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly InterfaceImplementation
{
}

.assembly extern System.Runtime
{
}

.assembly extern InterfaceDefinition
{
}

.class public auto ansi beforefieldinit Valid_ValidType_Valid
	extends [System.Runtime]System.Object
	implements [InterfaceDefinition]Interface
{
	.method public final hidebysig newslot virtual 
		instance void M1 () cil managed 
	{
		.maxstack 8

		IL_0000: ret
	}

	.method public final hidebysig newslot virtual 
		instance void M2 () cil managed 
	{
		.maxstack 8

		IL_0000: ret
	}

	.method public final hidebysig newslot virtual 
		instance void M3 (
			int32 i
		) cil managed 
	{
		.maxstack 8

		IL_0000: ret
	}

	.method public final hidebysig newslot virtual 
		instance int32 M4 (
			int32 i
		) cil managed 
	{
		.maxstack 8

		IL_0000: ldc.i4.0
		IL_0001: ret
	}

	.method public hidebysig specialname rtspecialname 
		instance void .ctor () cil managed 
	{
		.maxstack 8

		IL_0000: ldarg.0
		IL_0001: call instance void [System.Runtime]System.Object::.ctor()
		IL_0006: ret
	}
}

.class public auto ansi beforefieldinit InterfaceImplHasDuplicate_InvalidType_InterfaceImplHasDuplicate
	extends [System.Runtime]System.Object
	implements [InterfaceDefinition]Interface, [InterfaceDefinition]Interface
{	
	.method public final hidebysig newslot virtual 
		instance void M1 () cil managed 
	{
		.maxstack 8

		IL_0000: ret
	}

	.method public final hidebysig newslot virtual 
		instance void M2 () cil managed 
	{
		.maxstack 8

		IL_0000: ret
	}

	.method public final hidebysig newslot virtual 
		instance void M3 (
			int32 i
		) cil managed 
	{
		.maxstack 8

		IL_0000: ret
	}

	.method public final hidebysig newslot virtual 
		instance int32 M4 (
			int32 i
		) cil managed 
	{
		.maxstack 8

		IL_0000: ldc.i4.0
		IL_0001: ret
	}

	.method public hidebysig specialname rtspecialname 
		instance void .ctor () cil managed 
	{
		.maxstack 8

		IL_0000: ldarg.0
		IL_0001: call instance void [System.Runtime]System.Object::.ctor()
		IL_0006: ret
	}
}

.class public auto ansi beforefieldinit MissingMethod_InvalidType_InterfaceMethodNotImplemented
	extends [System.Runtime]System.Object
	implements [InterfaceDefinition]Interface
{
	.method public final hidebysig newslot virtual 
		instance void M2 () cil managed 
	{
		.maxstack 8
 		IL_0000: ret
	}
 	.method public final hidebysig newslot virtual 
		instance void M3 (
			int32 i
		) cil managed 
	{
		.maxstack 8
 		IL_0000: ret
	}
 	.method public final hidebysig newslot virtual 
		instance int32 M4 (
			int32 i
		) cil managed 
	{
		.maxstack 8
 		IL_0000: ldc.i4.0
		IL_0001: ret
	}
 	.method public hidebysig specialname rtspecialname 
		instance void .ctor () cil managed 
	{
		.maxstack 8
 		IL_0000: ldarg.0
		IL_0001: call instance void [System.Runtime]System.Object::.ctor()
		IL_0006: ret
	}
}

.class public auto ansi abstract beforefieldinit MissingMethod_ValidType_AbstractClass
	extends [System.Runtime]System.Object
	implements [InterfaceDefinition]Interface
{
	.method public final hidebysig newslot virtual 
		instance void M2 () cil managed 
	{
		.maxstack 8
 		IL_0000: ret
	}
 	.method public final hidebysig newslot virtual 
		instance void M3 (
			int32 i
		) cil managed 
	{
		.maxstack 8
 		IL_0000: ret
	}
 	.method public final hidebysig newslot virtual 
		instance int32 M4 (
			int32 i
		) cil managed 
	{
		.maxstack 8
 		IL_0000: ldc.i4.0
		IL_0001: ret
	}
 	.method public hidebysig specialname rtspecialname 
		instance void .ctor () cil managed 
	{
		.maxstack 8
 		IL_0000: ldarg.0
		IL_0001: call instance void [System.Runtime]System.Object::.ctor()
		IL_0006: ret
	}
}

.class public auto ansi beforefieldinit InterfaceImplHasDuplicateAndMissingMethod_InvalidType_InterfaceImplHasDuplicate@InterfaceMethodNotImplemented
	extends [System.Runtime]System.Object
	implements [InterfaceDefinition]Interface, [InterfaceDefinition]Interface
{	
	.method public final hidebysig newslot virtual 
		instance void M2 () cil managed 
	{
		.maxstack 8

		IL_0000: ret
	}

	.method public final hidebysig newslot virtual 
		instance void M3 (
			int32 i
		) cil managed 
	{
		.maxstack 8

		IL_0000: ret
	}

	.method public final hidebysig newslot virtual 
		instance int32 M4 (
			int32 i
		) cil managed 
	{
		.maxstack 8

		IL_0000: ldc.i4.0
		IL_0001: ret
	}

	.method public hidebysig specialname rtspecialname 
		instance void .ctor () cil managed 
	{
		.maxstack 8

		IL_0000: ldarg.0
		IL_0001: call instance void [System.Runtime]System.Object::.ctor()
		IL_0006: ret
	}
}

.class public auto ansi beforefieldinit InvalidReturnTypeM1_InvalidType_InterfaceMethodNotImplemented
	extends [System.Runtime]System.Object
	implements [InterfaceDefinition]Interface
{
	.method public final hidebysig newslot virtual 
		instance int32 M1 () cil managed 
	{
		.maxstack 8

		IL_0000: ret
	}

	.method public final hidebysig newslot virtual 
		instance void M2 () cil managed 
	{
		.maxstack 8

		IL_0000: ret
	}

	.method public final hidebysig newslot virtual 
		instance void M3 (
			int32 i
		) cil managed 
	{
		.maxstack 8

		IL_0000: ret
	}

	.method public final hidebysig newslot virtual 
		instance int32 M4 (
			int32 i
		) cil managed 
	{
		.maxstack 8

		IL_0000: ldc.i4.0
		IL_0001: ret
	}

	.method public hidebysig specialname rtspecialname 
		instance void .ctor () cil managed 
	{
		.maxstack 8

		IL_0000: ldarg.0
		IL_0001: call instance void [System.Runtime]System.Object::.ctor()
		IL_0006: ret
	}
}

.class public auto ansi beforefieldinit InvalidParameterTypeM3_InvalidType_InterfaceMethodNotImplemented
	extends [System.Runtime]System.Object
	implements [InterfaceDefinition]Interface
{
	.method public final hidebysig newslot virtual 
		instance void M1 () cil managed 
	{
		.maxstack 8

		IL_0000: ret
	}

	.method public final hidebysig newslot virtual 
		instance void M2 () cil managed 
	{
		.maxstack 8

		IL_0000: ret
	}

	.method public final hidebysig newslot virtual 
		instance void M3 (
			int64 i
		) cil managed 
	{
		.maxstack 8

		IL_0000: ret
	}

	.method public final hidebysig newslot virtual 
		instance int32 M4 (
			int32 i
		) cil managed 
	{
		.maxstack 8

		IL_0000: ldc.i4.0
		IL_0001: ret
	}

	.method public hidebysig specialname rtspecialname 
		instance void .ctor () cil managed 
	{
		.maxstack 8

		IL_0000: ldarg.0
		IL_0001: call instance void [System.Runtime]System.Object::.ctor()
		IL_0006: ret
	}
}

.class public auto ansi beforefieldinit InvalidMethodNameM10_InvalidType_InterfaceMethodNotImplemented
	extends [System.Runtime]System.Object
	implements [InterfaceDefinition]Interface
{
	.method public final hidebysig newslot virtual 
		instance void M10 () cil managed 
	{
		.maxstack 8

		IL_0000: ret
	}

	.method public final hidebysig newslot virtual 
		instance void M2 () cil managed 
	{
		.maxstack 8

		IL_0000: ret
	}

	.method public final hidebysig newslot virtual 
		instance void M3 (
			int32 i
		) cil managed 
	{
		.maxstack 8

		IL_0000: ret
	}

	.method public final hidebysig newslot virtual 
		instance int32 M4 (
			int32 i
		) cil managed 
	{
		.maxstack 8

		IL_0000: ldc.i4.0
		IL_0001: ret
	}

	.method public hidebysig specialname rtspecialname 
		instance void .ctor () cil managed 
	{
		.maxstack 8

		IL_0000: ldarg.0
		IL_0001: call instance void [System.Runtime]System.Object::.ctor()
		IL_0006: ret
	}
}

.class public auto ansi beforefieldinit BaseClass
	extends [System.Runtime]System.Object
	implements IInterface
{
	.method public final hidebysig newslot virtual 
		instance void InterfaceMethod () cil managed 
	{
		.maxstack 8

		IL_0000: nop
		IL_0001: ret
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

.class public auto ansi beforefieldinit ChildClass_ValidType_Valid
	extends BaseClass
	implements IInterface
{
	.method public hidebysig 
		instance void ChildMethod () cil managed 
	{
		.maxstack 8

		IL_0000: nop
		IL_0001: ret
	}

	.method public hidebysig specialname rtspecialname 
		instance void .ctor () cil managed 
	{
		.maxstack 8

		IL_0000: ldarg.0
		IL_0001: call instance void BaseClass::.ctor()
		IL_0006: nop
		IL_0007: ret
	}
}

.class interface public auto ansi abstract IInterface
{
	.method public hidebysig newslot abstract virtual 
		instance void InterfaceMethod () cil managed 
	{
	}
}
