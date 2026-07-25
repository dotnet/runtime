// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly DefaultInterfaceMethod
{
}

.assembly extern System.Runtime
{
}

.class interface private auto ansi abstract IInterface
{
	.method public hidebysig newslot abstract virtual
		instance void Method () cil managed
	{
	}
}

.class public auto ansi beforefieldinit ChildClassDoesNotImplementDefaultInterfaceMethod_ValidType_Valid
	extends [System.Runtime]System.Object
	implements IDefaultImplInterface
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

.class public auto ansi beforefieldinit ChildClassDoesImplementDefaultInterfaceMethod_ValidType_Valid
	extends [System.Runtime]System.Object
	implements IDefaultImplInterface
{
	.method public final hidebysig newslot virtual
		instance void DefaultImplementation () cil managed
	{
		.maxstack 8

		IL_0000: ret
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

.class interface public auto ansi abstract IDefaultImplInterface
{
    .method public hidebysig newslot virtual
		instance void DefaultImplementation () cil managed
	{
		.maxstack 8

		IL_0000: ret
	}
}

.class interface public auto ansi abstract IInheritedDefaultImplInterface
	implements IInterface
{
	.method private final hidebysig virtual
		instance void IInterface.Method () cil managed
	{
		.override method instance void IInterface::Method()
		.maxstack 8

		IL_0001: ret
	}
}

.class public auto ansi beforefieldinit ClassInheritsFromInterfaceWhereContractIsImplementedByDerivedInterface_ValidType_Valid
	extends [System.Runtime]System.Object
	implements IInheritedDefaultImplInterface, IInterface
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

.class interface public auto ansi abstract IReabstractDefaultImplementation
	implements IInheritedDefaultImplInterface, IInterface
{
	.method private final hidebysig abstract virtual
		instance void IInterface.Method () cil managed
	{
		.override method instance void IInterface::Method()
	}
}

.class public auto ansi beforefieldinit ChildClassInheritsFromInterfaceWithDefaultImplementationWhereChildInterfaceReabstractsInterfaceMethod_InvalidType_InterfaceMethodNotImplemented@InterfaceMethodNotImplemented
	extends [System.Runtime]System.Object
	implements IReabstractDefaultImplementation, IInheritedDefaultImplInterface, IInterface
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
