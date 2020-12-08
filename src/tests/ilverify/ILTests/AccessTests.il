// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Runtime
{
}

.assembly extern AccessTestsFriend
{
}

.assembly AccessTests
{
}

.class private auto ansi beforefieldinit PrivateClass
{
    .method public hidebysig specialname rtspecialname instance void .ctor() cil managed 
    {
        ldarg.0
        call        instance void [System.Runtime]System.Object::.ctor()
        ret
    }
}

.class public auto ansi beforefieldinit GenericClass<S, T>
    extends [System.Runtime]System.Object
{
    .method public hidebysig specialname rtspecialname instance void .ctor() cil managed 
    {
        ldarg.0
        call        instance void [System.Runtime]System.Object::.ctor()
        ret
    }
}

.class public auto ansi beforefieldinit SimpleClass
        extends [System.Runtime]System.Object
{
    .field public static int32 publicField
    .field private static int32 privateField
    .field family static int32 familyField
    .field assembly static int32 assemblyField
    .field famorassem static int32 familyOrAssemblyField
    .field famandassem static int32 familyAndAssemblyField

    .field family int32 instanceFamilyField

    .method public hidebysig specialname rtspecialname instance void .ctor() cil managed 
    {
        ldarg.0
        call        instance void [System.Runtime]System.Object::.ctor()
        ret
    }

    .method private hidebysig static void PrivateMethod() cil managed 
    {
        ret
    }

    .method family hidebysig static void FamilyMethod() cil managed 
    {
        ret
    }

    .method assembly hidebysig static void AssemblyMethod() cil managed 
    {
        ret
    }

    .method famorassem hidebysig static void FamilyOrAssemblyMethod() cil managed 
    {
        ret
    }

    .method famandassem hidebysig static void FamilyAndAssemblyMethod() cil managed 
    {
        ret
    }

    .method public hidebysig static void InaccessibleParamMethod(class SimpleClass/PrivateNestedClass c) cil managed 
    {
        ret
    }

    .method public hidebysig static void GenericMethod<T>() cil managed 
    {
        ret
    }

    .method family hidebysig instance void FamilyMethod() cil managed 
    {
        ret
    }

    .method public hidebysig static class SimpleClass/PrivateNestedClass InaccessibleReturnMethod() cil managed 
    {
        newobj      instance void SimpleClass/PrivateNestedClass::.ctor()
        ret
    }

    .class nested public auto ansi beforefieldinit PublicNestedClass
        extends [System.Runtime]System.Object
    {
        .method public hidebysig specialname rtspecialname instance void .ctor() cil managed 
        {
            ldarg.0
            call        instance void [System.Runtime]System.Object::.ctor()
            ret
        }

        .method public hidebysig static void Instantiate.PrivateNestedClassFromNested_Valid() cil managed 
        {
            newobj      instance void SimpleClass/PrivateNestedClass::.ctor()
            pop
            ret
        }

        .method public hidebysig static void Load.PrivateFieldFromNestedClass_Valid() cil managed 
        {
            ldsfld      int32 SimpleClass::privateField
            pop
            ret
        }
    }

    .class nested private auto ansi beforefieldinit PrivateNestedClass
        extends [System.Runtime]System.Object
    {
        .field public static int32 publicField

        .method public hidebysig specialname rtspecialname instance void .ctor() cil managed 
        {
            ldarg.0
            call        instance void [System.Runtime]System.Object::.ctor()
            ret
        }
    }

    .class nested assembly auto ansi beforefieldinit AssemblyNestedClass
        extends [System.Runtime]System.Object
    {
        .method public hidebysig specialname rtspecialname instance void .ctor() cil managed 
        {
            ldarg.0
            call        instance void [System.Runtime]System.Object::.ctor()
            ret
        }
    }

    .class nested famorassem auto ansi beforefieldinit FamilyOrAssemblyNestedClass
        extends [System.Runtime]System.Object
    {
        .method public hidebysig specialname rtspecialname instance void .ctor() cil managed 
        {
            ldarg.0
            call        instance void [System.Runtime]System.Object::.ctor()
            ret
        }
    }

    .class nested famandassem auto ansi beforefieldinit FamilyAndAssemblyNestedClass
        extends [System.Runtime]System.Object
    {
        .method public hidebysig specialname rtspecialname instance void .ctor() cil managed 
        {
            ldarg.0
            call        instance void [System.Runtime]System.Object::.ctor()
            ret
        }
    }

    .class nested family auto ansi beforefieldinit FamilyNestedClass
        extends [System.Runtime]System.Object
    {
        .method public hidebysig specialname rtspecialname instance void .ctor() cil managed 
        {
            ldarg.0
            call        instance void [System.Runtime]System.Object::.ctor()
            ret
        }
    }

    .method private hidebysig static void Call.PrivateClassParamIntern_Valid(class SimpleClass/PrivateNestedClass c) cil managed 
    {
        ldarg.0
        call        void SimpleClass::InaccessibleParamMethod(class SimpleClass/PrivateNestedClass)
        ret
    }

    .method private hidebysig static void Call.PrivateReturnTypeIntern_Valid() cil managed 
    {
        call        class SimpleClass/PrivateNestedClass SimpleClass::InaccessibleReturnMethod()
        pop
        ret
    }
}

.class public auto ansi beforefieldinit AccessTestsType
        extends [System.Runtime]System.Object
{
    .method public hidebysig instance void Instantiate.PublicNestedClass_Valid() cil managed
    {
        newobj      instance void SimpleClass/PublicNestedClass::.ctor()
        pop
        ret
    }

    .method public hidebysig instance void Instantiate.PrivateNestedClass_Invalid_MethodAccess() cil managed
    {
        newobj      instance void SimpleClass/PrivateNestedClass::.ctor()
        pop
        ret
    }

    .method public hidebysig instance void Instantiate.AssemNestedClass_Valid() cil managed
    {
        newobj      instance void SimpleClass/AssemblyNestedClass::.ctor()
        pop
        ret
    }

    .method public hidebysig instance void Instantiate.AssemOrFamNestedClass_Valid() cil managed
    {
        newobj      instance void SimpleClass/FamilyOrAssemblyNestedClass::.ctor()
        pop
        ret
    }

    .method public hidebysig instance void Instantiate.AssemAndFamNestedClass_Invalid_MethodAccess() cil managed
    {
        newobj      instance void SimpleClass/FamilyAndAssemblyNestedClass::.ctor()
        pop
        ret
    }

    .method public hidebysig instance void Instantiate.FamilyNestedClass_Invalid_MethodAccess() cil managed
    {
        newobj      instance void SimpleClass/FamilyNestedClass::.ctor()
        pop
        ret
    }

    .method public hidebysig instance void Instantiate.GenericWithPrivateNestedClass_Invalid_MethodAccess() cil managed
    {
        newobj      instance void class GenericClass<class SimpleClass/PrivateNestedClass, class SimpleClass/PublicNestedClass>::.ctor()
        pop
        ret
    }

    .method public hidebysig instance void Call.PrivateClassParamExtern_Invalid_MethodAccess(class SimpleClass/PrivateNestedClass c) cil managed
    {
        ldarg.1
        call        void SimpleClass::InaccessibleParamMethod(class SimpleClass/PrivateNestedClass)
        ret
    }

    .method public hidebysig instance void Call.PrivateReturnTypeExtern_Invalid_MethodAccess() cil managed
    {
        call        class SimpleClass/PrivateNestedClass SimpleClass::InaccessibleReturnMethod()
        pop
        ret
    }

    .method public hidebysig instance void Call.GenericMethodWithPrivateNestedClass_Invalid_MethodAccess() cil managed
    {
        call      void class SimpleClass::GenericMethod<class SimpleClass/PrivateNestedClass>()
        ret
    }

    .method public hidebysig instance void Load.PrivateFieldExtern_Invalid_FieldAccess() cil managed
    {
        ldsfld      int32 SimpleClass::privateField
        pop
        ret
    }

    .method public hidebysig instance void Load.PublicFieldOfPrivateClass_Invalid_FieldAccess() cil managed
    {
        ldsfld      int32 SimpleClass/PrivateNestedClass::publicField
        pop
        ret
    }
}

.class public auto ansi beforefieldinit DerivedType
        extends SimpleClass
{
    .method public hidebysig instance void Call.DerivedFamAndAssemblyMethod_Valid() cil managed
    {
        call      void SimpleClass::FamilyAndAssemblyMethod()
        ret
    }

    .method public hidebysig instance void Instantiate.FamOrAssemblyNestedClassFromDerived_Valid() cil managed
    {
        newobj      instance void SimpleClass/FamilyOrAssemblyNestedClass::.ctor()
        pop
        ret
    }

    .method public hidebysig instance void Instantiate.FamNestedClassFromDerived_Valid() cil managed
    {
        newobj      instance void SimpleClass/FamilyNestedClass::.ctor()
        pop
        ret
    }

    .method private hidebysig instance void Load.FamFieldOfBaseWithBaseInstance_Invalid_FieldAccess(class SimpleClass c) cil managed
    {
        ldarg.1
        ldfld      int32 SimpleClass::instanceFamilyField
        pop
        ret
    }

    .method private hidebysig instance void Load.FamFieldOfBaseWithDerivedInstance_Valid() cil managed
    {
        ldarg.0
        ldfld      int32 SimpleClass::instanceFamilyField
        pop
        ret
    }

    .method private hidebysig instance void Call.FamMethodOfBaseWithBaseInstance_Invalid_MethodAccess(class SimpleClass c) cil managed
    {
        ldarg.1
        call        instance void SimpleClass::FamilyMethod()
        ret
    }

    .method private hidebysig instance void Call.FamMethodOfBaseWithDerivedInstance_Valid() cil managed
    {
        ldarg.0
        call        instance void SimpleClass::FamilyMethod()
        ret
    }

    .method private hidebysig instance void Load.AssemFieldOfFriendAssembly_Valid() cil managed
    {
        ldsfld       int32 [AccessTestsFriend]AccessTestsFriendType::assemblyField
        pop
        ret
    }

    .method private hidebysig instance void Load.PrivateFieldOfFriendAssembly_Invalid_FieldAccess() cil managed
    {
        ldsfld       int32 [AccessTestsFriend]AccessTestsFriendType::privateField
        pop
        ret
    }
}

// Special case: Nested class trying to access family field of superclass of containing class

.class private auto ansi beforefieldinit SuperClass
        extends [System.Runtime]System.Object
{
    .field family static int32 familyField
    .field family int32 instanceFamilyField

    .method public hidebysig specialname rtspecialname instance void .ctor() cil managed 
    {
        ldarg.0
        call        instance void [System.Runtime]System.Object::.ctor()
        ret
    }
}

.class private auto ansi beforefieldinit NestedClass
        extends SuperClass
{
    .method public hidebysig specialname rtspecialname instance void .ctor() cil managed 
    {
        ldarg.0
        call        instance void SuperClass::.ctor()
        ret
    }

    .class nested private auto ansi beforefieldinit NestedClass
        extends [System.Runtime]System.Object
    {
        .method public hidebysig specialname rtspecialname instance void .ctor() cil managed 
        {
            ldarg.0
            call        instance void [System.Runtime]System.Object::.ctor()
            ret
        }

        .method private hidebysig instance void Load.FamFieldOfSuperOfContaining_Valid() cil managed
        {
            ldsfld      int32 SuperClass::familyField
            pop
            ret
        }
    }
}

// Test for bugfix https://github.com/dotnet/corert/issues/6693

.class public auto ansi beforefieldinit Issue6693
	extends [System.Runtime]System.Object
{

	.class nested public auto ansi abstract beforefieldinit InnerClass`1<(Issue6693) T>
		extends class Issue6693Base`1<!T>
	{
		.method public hidebysig virtual 
			instance void BaseMethod_Valid (
				int32 index,
				!T item
			) cil managed 
		{
			.maxstack 8

			IL_0000: nop
			IL_0001: ldarg.2
			IL_0002: box !T
			IL_0007: callvirt instance void Issue6693::ParentMethod()
			IL_000c: nop
			IL_000d: ret
		}

		.method family hidebysig specialname rtspecialname 
			instance void .ctor () cil managed 
		{
			.maxstack 8

			IL_0000: ldarg.0
			IL_0001: call instance void class Issue6693Base`1<!T>::.ctor()
			IL_0006: nop
			IL_0007: ret
		}
	}

	.method family hidebysig 
		instance void ParentMethod () cil managed 
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

.class public auto ansi beforefieldinit Issue6693Base`1<T>
	extends [System.Runtime]System.Object
{
	.method public hidebysig newslot virtual 
		instance void BaseMethod_Valid (
			int32 index,
			!T item
		) cil managed 
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
