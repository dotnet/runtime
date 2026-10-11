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

.class public auto ansi beforefieldinit ExplicitVirtualMethod_ValidType_Valid
    extends [System.Runtime]System.Object
    implements IInterface
{
    .method private final hidebysig newslot virtual instance void ExplicitMethod() cil managed
    {
        .override method instance void IInterface::InterfaceMethod()
        .maxstack 0
        ret
    }
}

.class public auto ansi beforefieldinit ExplicitNonVirtualMethod_InvalidType_InvalidMethodImpl
    extends [System.Runtime]System.Object
    implements IInterface
{
    .method private hidebysig instance void ExplicitMethod() cil managed
    {
        .override method instance void IInterface::InterfaceMethod()
        .maxstack 0
        ret
    }
}

.class public auto ansi beforefieldinit ImplicitNonVirtualMethod_InvalidType_InterfaceMethodNotImplemented
    extends [System.Runtime]System.Object
    implements IInterface
{
    .method public hidebysig instance void InterfaceMethod() cil managed
    {
        .maxstack 0
        ret
    }
}

.class public sequential ansi sealed beforefieldinit ExplicitVirtualMethodOnStruct_ValidType_Valid
    extends [System.Runtime]System.ValueType
    implements IInterface
{
    .method private final hidebysig newslot virtual instance void ExplicitMethod() cil managed
    {
        .override method instance void IInterface::InterfaceMethod()
        .maxstack 0
        ret
    }
}

.class public sequential ansi sealed beforefieldinit ExplicitNonVirtualMethodOnStruct_InvalidType_InvalidMethodImpl
    extends [System.Runtime]System.ValueType
    implements IInterface
{
    .method private hidebysig instance void ExplicitMethod() cil managed
    {
        .override method instance void IInterface::InterfaceMethod()
        .maxstack 0
        ret
    }
}

.class interface public auto ansi abstract IStaticInterface
{
    .method public hidebysig newslot abstract virtual static int32 StaticMethod() cil managed
    {
    }
}

.class public auto ansi beforefieldinit ExplicitStaticMethod_ValidType_Valid
    extends [System.Runtime]System.Object
    implements IStaticInterface
{
    .method private hidebysig static int32 ExplicitMethod() cil managed
    {
        .override method int32 IStaticInterface::StaticMethod()
        .maxstack 1
        ldc.i4.s 42
        ret
    }
}

.class public sequential ansi sealed beforefieldinit ExplicitStaticMethodOnStruct_ValidType_Valid
    extends [System.Runtime]System.ValueType
    implements IStaticInterface
{
    .method private hidebysig static int32 ExplicitMethod() cil managed
    {
        .override method int32 IStaticInterface::StaticMethod()
        .maxstack 1
        ldc.i4.s 42
        ret
    }
}

.class public auto ansi beforefieldinit InheritedStaticMethod_ValidType_Valid
    extends ExplicitStaticMethod_ValidType_Valid
    implements IStaticInterface
{
}

.class public auto ansi beforefieldinit MissingStaticMethod_InvalidType_InterfaceMethodNotImplemented
    extends [System.Runtime]System.Object
    implements IStaticInterface
{
}

.class interface public auto ansi abstract IDefaultStaticInterface
    implements IStaticInterface
{
    .method private hidebysig static int32 ExplicitMethod() cil managed
    {
        .override method int32 IStaticInterface::StaticMethod()
        .maxstack 1
        ldc.i4.s 42
        ret
    }
}

.class public auto ansi beforefieldinit DefaultStaticMethod_ValidType_Valid
    extends [System.Runtime]System.Object
    implements IDefaultStaticInterface, IStaticInterface
{
}

.class public auto ansi beforefieldinit VirtualMethodImplBase
    extends [System.Runtime]System.Object
{
    .method public hidebysig newslot virtual instance void ExplicitMethod() cil managed
    {
        .maxstack 0
        ret
    }
}

.class public auto ansi beforefieldinit NonVirtualMethodHidesBaseVirtualMethod_InvalidType_InvalidMethodImpl
    extends VirtualMethodImplBase
    implements IInterface
{
    .method public hidebysig instance void ExplicitMethod() cil managed
    {
        .override method instance void IInterface::InterfaceMethod()
        .maxstack 0
        ret
    }
}

.class public auto ansi beforefieldinit VirtualMethodOverridesBaseVirtualMethod_ValidType_Valid
    extends VirtualMethodImplBase
    implements IInterface
{
    .method public hidebysig virtual instance void ExplicitMethod() cil managed
    {
        .override method instance void IInterface::InterfaceMethod()
        .maxstack 0
        ret
    }
}

.class public auto ansi beforefieldinit InheritedNonVirtualMethod_InvalidType_InvalidMethodImpl
    extends ExplicitNonVirtualMethod_InvalidType_InvalidMethodImpl
    implements IInterface
{
}

.class public auto ansi beforefieldinit StaticBodyForInstanceMethod_InvalidType_InvalidMethodImpl
    extends [System.Runtime]System.Object
    implements IInterface
{
    .method public hidebysig static void ExplicitMethod() cil managed
    {
        .override method instance void IInterface::InterfaceMethod()
        .maxstack 0
        ret
    }
}

.class public auto ansi beforefieldinit InstanceBodyForStaticMethod_InvalidType_InvalidMethodImpl
    extends [System.Runtime]System.Object
    implements IStaticInterface
{
    .method public hidebysig newslot virtual instance int32 ExplicitMethod() cil managed
    {
        .override method int32 IStaticInterface::StaticMethod()
        .maxstack 1
        ldc.i4.s 42
        ret
    }
}

.class public auto ansi beforefieldinit InheritedInstanceBodyForStaticMethod_InvalidType_InvalidMethodImpl
    extends InstanceBodyForStaticMethod_InvalidType_InvalidMethodImpl
    implements IStaticInterface
{
}

.class interface public auto ansi abstract IDefaultInstanceInterface
    implements IInterface
{
    .method private final hidebysig virtual instance void ExplicitMethod() cil managed
    {
        .override method instance void IInterface::InterfaceMethod()
        .maxstack 0
        ret
    }
}

.class public auto ansi beforefieldinit NonVirtualMethodWithDefault_InvalidType_InvalidMethodImpl
    extends [System.Runtime]System.Object
    implements IDefaultInstanceInterface, IInterface
{
    .method public hidebysig instance void ExplicitMethod() cil managed
    {
        .override method instance void IInterface::InterfaceMethod()
        .maxstack 0
        ret
    }
}

.class interface public auto ansi abstract IGenericMethodImpl`1<T>
{
    .method public hidebysig newslot abstract virtual instance void InterfaceMethod(!T v) cil managed
    {
    }
}

.class public auto ansi beforefieldinit GenericExplicitVirtualMethod_ValidType_Valid
    extends [System.Runtime]System.Object
    implements class IGenericMethodImpl`1<int32>
{
    .method public hidebysig newslot virtual instance void ExplicitMethod(int32 v) cil managed
    {
        .override method instance void class IGenericMethodImpl`1<int32>::InterfaceMethod(!0)
        .maxstack 0
        ret
    }
}

.class public auto ansi beforefieldinit GenericExplicitNonVirtualMethod_InvalidType_InvalidMethodImpl
    extends [System.Runtime]System.Object
    implements class IGenericMethodImpl`1<int32>
{
    .method public hidebysig instance void ExplicitMethod(int32 v) cil managed
    {
        .override method instance void class IGenericMethodImpl`1<int32>::InterfaceMethod(!0)
        .maxstack 0
        ret
    }
}

.class public auto ansi beforefieldinit GenericOpenExplicitVirtualMethod_ValidType_Valid`1<T>
    extends [System.Runtime]System.Object
    implements class IGenericMethodImpl`1<!T>
{
    .method public hidebysig newslot virtual instance void ExplicitMethod(!T v) cil managed
    {
        .override method instance void class IGenericMethodImpl`1<!T>::InterfaceMethod(!0)
        .maxstack 0
        ret
    }
}

.class public auto ansi beforefieldinit SameNameMethodImplDeclarations_InvalidType_InvalidMethodImpl
    extends [System.Runtime]System.Object
    implements IInterface, class IGenericMethodImpl`1<int32>
{
    .method public hidebysig instance void ExplicitMethod() cil managed
    {
        .override method instance void IInterface::InterfaceMethod()
        .maxstack 0
        ret
    }
    .method public hidebysig newslot virtual instance void GenericMethod(int32 v) cil managed
    {
        .override method instance void class IGenericMethodImpl`1<int32>::InterfaceMethod(!0)
        .maxstack 0
        ret
    }
}

.class public auto ansi abstract beforefieldinit AbstractNonVirtualMethod_InvalidType_InvalidMethodImpl
    extends [System.Runtime]System.Object
    implements IInterface
{
    .method private hidebysig instance void ExplicitMethod() cil managed
    {
        .override method instance void IInterface::InterfaceMethod()
        .maxstack 0
        ret
    }
}

.class public auto ansi abstract beforefieldinit AbstractVirtualMethod_ValidType_Valid
    extends [System.Runtime]System.Object
    implements IInterface
{
    .method private final hidebysig newslot virtual instance void ExplicitMethod() cil managed
    {
        .override method instance void IInterface::InterfaceMethod()
        .maxstack 0
        ret
    }
}

.class interface public auto ansi abstract IDefaultMethodInterface
{
    .method public hidebysig newslot virtual instance void InterfaceMethod() cil managed
    {
        .maxstack 0
        ret
    }
}

.class public auto ansi beforefieldinit NonVirtualBodyForDefaultInterfaceMethod_InvalidType_InvalidMethodImpl
    extends [System.Runtime]System.Object
    implements IDefaultMethodInterface
{
    .method private hidebysig instance void ExplicitMethod() cil managed
    {
        .override method instance void IDefaultMethodInterface::InterfaceMethod()
        .maxstack 0
        ret
    }
}

.class public auto ansi beforefieldinit VirtualBodyForDefaultInterfaceMethod_ValidType_Valid
    extends [System.Runtime]System.Object
    implements IDefaultMethodInterface
{
    .method private final hidebysig newslot virtual instance void ExplicitMethod() cil managed
    {
        .override method instance void IDefaultMethodInterface::InterfaceMethod()
        .maxstack 0
        ret
    }
}

.class public auto ansi beforefieldinit NonVirtualMethodOnInheritedInterface_InvalidType_InvalidMethodImpl
    extends ExplicitVirtualMethod_ValidType_Valid
{
    .method private hidebysig instance void ExplicitMethod() cil managed
    {
        .override method instance void IInterface::InterfaceMethod()
        .maxstack 0
        ret
    }
}

.class public auto ansi beforefieldinit VirtualMethodOnInheritedInterface_ValidType_Valid
    extends ExplicitVirtualMethod_ValidType_Valid
{
    .method private final hidebysig newslot virtual instance void ExplicitMethod() cil managed
    {
        .override method instance void IInterface::InterfaceMethod()
        .maxstack 0
        ret
    }
}

.class interface public auto ansi abstract INonVirtualMethodImpl_InvalidType_InvalidMethodImpl
    implements IInterface
{
    .method private hidebysig instance void ExplicitMethod() cil managed
    {
        .override method instance void IInterface::InterfaceMethod()
        .maxstack 0
        ret
    }
}

.class interface public auto ansi abstract IVirtualMethodImpl_ValidType_Valid
    implements IInterface
{
    .method private final hidebysig newslot virtual instance void ExplicitMethod() cil managed
    {
        .override method instance void IInterface::InterfaceMethod()
        .maxstack 0
        ret
    }
}

.class public auto ansi beforefieldinit InheritedNonVirtualDefaultImplementation_InvalidType_InvalidMethodImpl
    extends [System.Runtime]System.Object
    implements INonVirtualMethodImpl_InvalidType_InvalidMethodImpl, IInterface
{
}

.class public auto ansi beforefieldinit NonVirtualBodyForClassMethod_InvalidType_InvalidMethodImpl
    extends VirtualMethodImplBase
{
    .method private hidebysig instance void Implementation() cil managed
    {
        .override method instance void VirtualMethodImplBase::ExplicitMethod()
        .maxstack 0
        ret
    }
}

.class public auto ansi beforefieldinit VirtualBodyForClassMethod_ValidType_Valid
    extends VirtualMethodImplBase
{
    .method private final hidebysig newslot virtual instance void Implementation() cil managed
    {
        .override method instance void VirtualMethodImplBase::ExplicitMethod()
        .maxstack 0
        ret
    }
}

.class interface public auto ansi abstract IMixedStaticInstance
{
    .method public hidebysig newslot abstract virtual instance void M() cil managed
    {
    }

    .method public hidebysig newslot abstract virtual static void M(int32 v) cil managed
    {
    }
}

.class public auto ansi beforefieldinit SameNameStaticAndInstanceMethods_ValidType_Valid
    extends [System.Runtime]System.Object
    implements IMixedStaticInstance
{
    .method private final hidebysig newslot virtual instance void InstanceMethod() cil managed
    {
        .override method instance void IMixedStaticInstance::M()
        .maxstack 0
        ret
    }

    .method private hidebysig static void StaticMethod(int32 v) cil managed
    {
        .override method void IMixedStaticInstance::M(int32)
        .maxstack 0
        ret
    }
}

.class public auto ansi beforefieldinit InvalidMethodImplAndMissingMethod_InvalidType_InvalidMethodImpl@InterfaceMethodNotImplemented
    extends [System.Runtime]System.Object
    implements IInterface, class IGenericMethodImpl`1<int32>
{
    .method private hidebysig instance void ExplicitMethod() cil managed
    {
        .override method instance void IInterface::InterfaceMethod()
        .maxstack 0
        ret
    }
}

.class public auto ansi beforefieldinit GenericVirtualMethodImplBase`1<T>
    extends [System.Runtime]System.Object
{
    .method public hidebysig newslot virtual instance void ExplicitMethod(!T v) cil managed
    {
        .maxstack 0
        ret
    }
}

.class public auto ansi beforefieldinit GenericNonVirtualMethodHidesBaseVirtualMethod_InvalidType_InvalidMethodImpl<T>
    extends class GenericVirtualMethodImplBase`1<!T>
    implements class IGenericMethodImpl`1<!T>
{
    .method public hidebysig instance void ExplicitMethod(!T v) cil managed
    {
        .override method instance void class IGenericMethodImpl`1<!T>::InterfaceMethod(!0)
        .maxstack 0
        ret
    }
}
