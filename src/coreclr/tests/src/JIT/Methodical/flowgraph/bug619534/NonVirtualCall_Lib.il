
//  Microsoft (R) .NET Framework IL Disassembler.  Version 2.0.50620.00
//   Microsoft Corporation. All rights reserved.



// Metadata version: v2.0.50620
.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 2:0:0:0
}
.assembly NonVirtualCall_Lib
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::.ctor() = ( 01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
                                                                                                             63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01 )       // ceptionThrows.
  .custom instance void [mscorlib]System.Security.SecurityTransparentAttribute::.ctor() = ( 01 00 00 00 ) 
  .custom instance void [mscorlib]System.Security.SecurityRulesAttribute::.ctor(valuetype [mscorlib]System.Security.SecurityRuleSet) = ( 01 00 02 00 00 ) 
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
.module NonVirtualCall_Lib.dll
// MVID: {06A0A842-CB0F-4CC1-9439-EECC24ABD077}
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x01010000


// =============== CLASS MEMBERS DECLARATION ===================

.class public ChildClass extends ParentClass
{
	.method public instance void .ctor()
	{
		ldarg 0
		call instance void ParentClass::.ctor()
		ret
	}

	.method public virtual void VirtualMethod()
	{
		ret
	}

	.method public final virtual void FinalVirtualMethod()
	{
		ret
	}

	// In-class Testcases
	//
	.method public instance void Test_CallNonFinalVirtualInside_Pos()
	{
		ldarg 0
		call instance void ChildClass::VirtualMethod()
		ret
	}

	.method public instance void Test_DelegateNonFinalVirtualInside_Pos()
	{
		ldarg 0
		ldftn instance void ChildClass::VirtualMethod()
		newobj instance void TestDelegate::.ctor(object, native int)
		call instance void TestDelegate::Invoke()
		ret
	}

	.method public instance void Test_CallParentNonFinalVirtualInside_Pos()
	{
		ldarg 0
		call instance void ParentClass::VirtualMethod()
		ret
	}

	.method public instance void Test_DelegateParentNonFinalVirtualInside_Pos()
	{
		ldarg 0
		ldftn instance void ParentClass::VirtualMethod()
		newobj instance void TestDelegate::.ctor(object, native int)
		call instance void TestDelegate::Invoke()
		ret
	}

	.method public instance void Test_CallGrandparentNonFinalVirtualInside_Pos()
	{
		ldarg 0
		call instance void GrandparentClass::VirtualMethod()
		ret
	}

	.method public instance void Test_DelegateGrandparentNonFinalVirtualInside_Pos()
	{
		ldarg 0
		ldftn instance void GrandparentClass::VirtualMethod()
		newobj instance void TestDelegate::.ctor(object, native int)
		call instance void TestDelegate::Invoke()
		ret
	}

	.method public instance void Test_CallParentNonFinalVirtualAfterLdarga_Neg()
	{
		ldarg 0
		ldarga 0
		pop
		call instance void ParentClass::VirtualMethod()
		ret
	}

	.method public instance void Test_CallParentNonFinalVirtualAfterLdargaS_Neg()
	{
		// Switched the order of ldarg/ldarga to cover both opcodes, plus before/after ldarg opcode
		//
		ldarga.s 0
		pop
		ldarg 0
		call instance void ParentClass::VirtualMethod()
		ret
	}

	.method public instance void Test_CallParentNonFinalVirtualAfterStarg_Neg()
	{
		ldarg 0
		dup
		starg 0
		call instance void ParentClass::VirtualMethod()
		ret
	}

	.method public instance void Test_CallParentNonFinalVirtualAfterStargS_Neg()
	{
		// Switched the order of ldarg/starg to cover both opcodes, plus before/after ldarg opcode
		//
		ldarg 0
		starg.s 0
		ldarg 0
		call instance void ParentClass::VirtualMethod()
		ret
	}	

	.method public instance void Test_CallParentNonFinalVirtualAfterStargNonZero_Pos(class ChildClass)
	{
		ldarg 0
		starg 1
		ldarg 0
		call instance void ParentClass::VirtualMethod()
		ret
	}	

	.method public instance void Test_DelegateParentNonFinalVirtualAfterStarg_Neg()
	{
		ldarg 0
		starg 0
		ldarg 0
		ldftn instance void ParentClass::VirtualMethod()
		newobj instance void TestDelegate::.ctor(object, native int)
		call instance void TestDelegate::Invoke()
		ret
	}

	.method public instance void Test_CallParentNonFinalVirtualAfterLdlocStloc_Neg()
	{
		.locals init (class ChildClass)
		ldarg 0
		stloc 0
		ldloc 0
		call instance void ParentClass::VirtualMethod()
		ret
	}

	.method public instance void Test_CallParentNonFinalVirtualAfterNewobj_Neg()
	{
		newobj instance void ChildClass::.ctor()
		call instance void ParentClass::VirtualMethod()
		ret
	}

	.method public instance void Test_CallParentNonFinalVirtualWithNull_Neg()
	{
		ldnull
		call instance void ParentClass::VirtualMethod()
		ret
	}

	.method public instance void Test_DelegateParentNonFinalVirtualWithNull_Neg()
	{
		ldnull
		ldftn instance void ParentClass::VirtualMethod()
		newobj instance void TestDelegate::.ctor(object, native int)
		pop
		ret
	}

	.method public instance void Test_CallParentNonFinalVirtualWithDup_Pos()
	{
		ldarg 0
		dup
		call instance void ParentClass::VirtualMethod()
		pop
		ret
	}

	.method public instance void Test_CallParentNonFinalVirtualAfterCondStarg_Neg(int32)
	{
		ldarg 1
		brfalse Label
		ldarg 0
		starg 0
Label:
		ldarg 0
		call instance void ParentClass::VirtualMethod()
		ret
	}

	.method public instance void Test_CallParentNonFinalVirtualInException_Neg()
	{
		.try
		{
			ldarg 0
			starg 0
			newobj instance void [mscorlib]System.Exception::.ctor()
			throw
		}
		catch [mscorlib]System.Exception
		{
			ldarg 0
			call instance void ParentClass::VirtualMethod()
			leave EndSEH
		}

EndSEH:
		ret
	}	

	.method public instance void Test_CallParentNonFinalVirtualBeforeStarg_Neg()
	{
		ldarg 0
		call instance void ParentClass::VirtualMethod()
		ldarg 0
		starg 0
		ret
	}	

	.method public instance void Test_CallNonFinalVirtualBoxedValueTypeAfterStarg_Pos()
	{
		.locals init (value class TestValueType)
		ldarg 0
		starg 0
		ldloc 0
		box value class TestValueType
		call instance string [mscorlib]System.Object::ToString()
		pop
		ret
	}
}
.class public GrandparentClass extends [mscorlib]System.Object
{
	.method public instance void .ctor()
	{
		ldarg 0
		call instance void [mscorlib]System.Object::.ctor()
		ret
	}

	.method public virtual void VirtualMethod()
	{
		ret
	}
}

.class public ParentClass extends GrandparentClass
{
	.method public instance void .ctor()
	{
		ldarg 0
		call instance void GrandparentClass::.ctor()
		ret
	}

	.method public virtual void VirtualMethod()
	{
		ret
	}
}


// Helper Classes
//
.class sealed public TestDelegate extends [mscorlib]System.MulticastDelegate
{
	.method public instance void .ctor(object, native int) runtime { }
	.method public instance void Invoke() runtime { }
}

.class sealed public ToStringDelegate extends [mscorlib]System.MulticastDelegate
{
	.method public instance void .ctor(object, native int) runtime { }
	.method public instance string Invoke() runtime { }
}

.class public sealed auto TestValueType extends [mscorlib]System.ValueType
{
	.field public int32 i

	.method public virtual void VirtualMethod()
	{
		ret
	}
}
.class public sealed SealedClass extends [mscorlib]System.Object
{
	.method public instance void .ctor()
	{
		ldarg 0
		call instance void [mscorlib]System.Object::.ctor()
		ret
	}

	.method public virtual void VirtualMethod()
	{
		ret
	}
}


// =============================================================

// *********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file NonVirtualCall_Lib.res
