// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}

.assembly extern mscorlib {}
.assembly extern NonVirtualCall_Lib {}
.assembly NonVirtualCall
{
.custom instance void [mscorlib]System.Security.SecurityTransparentAttribute::.ctor() = ( 01 00 00 00 ) 
  .custom instance void [mscorlib]System.Security.SecurityRulesAttribute::.ctor(valuetype [mscorlib]System.Security.SecurityRuleSet) = ( 01 00 02 00 00 ) 
} 


.method public static void Test_CallNonFinalVirtualOutside_Neg()
{
	newobj instance void [NonVirtualCall_Lib]ChildClass::.ctor()
	call instance void [NonVirtualCall_Lib]ChildClass::VirtualMethod()
	ret
}

.method public static void Test_DelegateNonFinalVirtualOutside_Neg()
{
	newobj instance void [NonVirtualCall_Lib]ChildClass::.ctor()
	ldftn instance void [NonVirtualCall_Lib]ChildClass::VirtualMethod()
	newobj instance void [NonVirtualCall_Lib]TestDelegate::.ctor(object, native int)
	call instance void [NonVirtualCall_Lib]TestDelegate::Invoke()
	ret
}

.method public static void Test_CallFinalVirtualOutside_Pos()
{
	newobj instance void [NonVirtualCall_Lib]ChildClass::.ctor()
	call instance void [NonVirtualCall_Lib]ChildClass::FinalVirtualMethod()
	ret
}

.method public static void Test_DelegateFinalVirtualOutside_Pos()
{
	newobj instance void [NonVirtualCall_Lib]ChildClass::.ctor()
	ldftn instance void [NonVirtualCall_Lib]ChildClass::FinalVirtualMethod()
	newobj instance void [NonVirtualCall_Lib]TestDelegate::.ctor(object, native int)
	call instance void [NonVirtualCall_Lib]TestDelegate::Invoke()
	ret
}

.method public static void Test_CallSealedVirtualOutside_Pos()
{
	newobj instance void [NonVirtualCall_Lib]SealedClass::.ctor()
	call instance void [NonVirtualCall_Lib]SealedClass::VirtualMethod()
	ret
}

.method public static void Test_DelegateSealedVirtualOutside_Pos()
{
	newobj instance void [NonVirtualCall_Lib]SealedClass::.ctor()
	ldftn instance void [NonVirtualCall_Lib]SealedClass::VirtualMethod()
	newobj instance void [NonVirtualCall_Lib]TestDelegate::.ctor(object, native int)
	call instance void [NonVirtualCall_Lib]TestDelegate::Invoke()
	ret
}

.method public static void Test_CallNonFinalVirtualBoxedValueType_Pos()
{
	.locals init (value class [NonVirtualCall_Lib]TestValueType)
	ldloc 0
	box value class [NonVirtualCall_Lib]TestValueType
	call instance string [mscorlib]System.Object::ToString()
	pop
	ret
}

.method public static void Test_DelegateNonFinalVirtualBoxedValueType_Pos()
{
	.locals init (value class [NonVirtualCall_Lib]TestValueType)
	ldloc 0
	box value class [NonVirtualCall_Lib]TestValueType
	ldftn instance string [mscorlib]System.Object::ToString()
	newobj instance void [NonVirtualCall_Lib]ToStringDelegate::.ctor(object, native int)
	call instance string [NonVirtualCall_Lib]ToStringDelegate::Invoke()
	pop
	ret
}	

.method public static int32 Main()
{
	.entrypoint

	.try
	{
		call void Test_CallNonFinalVirtualOutside_Neg()
		ldstr "Error: Test_CallNonFinalVirtualOutside_Neg did not throw an exception"
		call void [System.Console]System.Console::WriteLine(string)
		leave Fail
	}
	catch [mscorlib]System.Security.VerificationException
	{
		leave Next0
	}
	catch [mscorlib]System.Exception
	{
		ldstr "Error: Test_CallNonFinalVirtualOutside_Neg threw an unexpected exception"
		call void [System.Console]System.Console::WriteLine(string)
		leave Fail
	}

Next0:
	.try
	{
		call void Test_DelegateNonFinalVirtualOutside_Neg()
		ldstr "Error: Test_DelegateNonFinalVirtualOutside_Neg did not throw an exception"
		call void [System.Console]System.Console::WriteLine(string)
		leave Fail
	}
	catch [mscorlib]System.Security.VerificationException
	{
		leave Next1
	}
	catch [mscorlib]System.Exception
	{
		ldstr "Error: Test_DelegateNonFinalVirtualOutside_Neg threw an unexpected exception"
		call void [System.Console]System.Console::WriteLine(string)
		leave Fail
	}
		

Next1:
	call void Test_CallFinalVirtualOutside_Pos()
	call void Test_DelegateFinalVirtualOutside_Pos()
	call void Test_CallSealedVirtualOutside_Pos()
	call void Test_CallSealedVirtualOutside_Pos()

	newobj instance void [NonVirtualCall_Lib]ChildClass::.ctor()
	dup
	dup
	dup
	dup
	dup
	call instance void [NonVirtualCall_Lib]ChildClass::Test_CallNonFinalVirtualInside_Pos()
	call instance void [NonVirtualCall_Lib]ChildClass::Test_DelegateNonFinalVirtualInside_Pos()
	call instance void [NonVirtualCall_Lib]ChildClass::Test_CallParentNonFinalVirtualInside_Pos()
	call instance void [NonVirtualCall_Lib]ChildClass::Test_DelegateParentNonFinalVirtualInside_Pos()
	call instance void [NonVirtualCall_Lib]ChildClass::Test_CallGrandparentNonFinalVirtualInside_Pos()
	call instance void [NonVirtualCall_Lib]ChildClass::Test_DelegateGrandparentNonFinalVirtualInside_Pos()

	call void Test_CallNonFinalVirtualBoxedValueType_Pos()
	call void Test_DelegateNonFinalVirtualBoxedValueType_Pos()

	.try
	{
		newobj instance void [NonVirtualCall_Lib]ChildClass::.ctor()
		call instance void [NonVirtualCall_Lib]ChildClass::Test_CallParentNonFinalVirtualAfterLdarga_Neg()
		ldstr "Error: Test_CallParentNonFinalVirtualAfterLdarga_Neg did not throw an exception"
		call void [System.Console]System.Console::WriteLine(string)
		leave Fail
	}
	catch [mscorlib]System.Security.VerificationException
	{
		leave Next2
	}
	catch [mscorlib]System.Exception
	{
		ldstr "Error: Test_CallParentNonFinalVirtualAfterLdarga_Neg threw an unexpected exception"
		call void [System.Console]System.Console::WriteLine(string)
		leave Fail
	}

Next2:
	.try
	{
		newobj instance void [NonVirtualCall_Lib]ChildClass::.ctor()
		call instance void [NonVirtualCall_Lib]ChildClass::Test_CallParentNonFinalVirtualAfterLdargaS_Neg()
		ldstr "Error: Test_CallParentNonFinalVirtualAfterLdargaS_Neg did not throw an exception"
		call void [System.Console]System.Console::WriteLine(string)
		leave Fail
	}
	catch [mscorlib]System.Security.VerificationException
	{
		leave Next3
	}
	catch [mscorlib]System.Exception
	{
		ldstr "Error: Test_CallParentNonFinalVirtualAfterLdargaS_Neg threw an unexpected exception"
		call void [System.Console]System.Console::WriteLine(string)
		leave Fail
	}

Next3:
	.try
	{
		newobj instance void [NonVirtualCall_Lib]ChildClass::.ctor()
		call instance void [NonVirtualCall_Lib]ChildClass::Test_CallParentNonFinalVirtualAfterStarg_Neg()
		ldstr "Error: Test_CallParentNonFinalVirtualAfterStarg_Neg did not throw an exception"
		call void [System.Console]System.Console::WriteLine(string)
		leave Fail
	}
	catch [mscorlib]System.Security.VerificationException
	{
		leave Next4
	}
	catch [mscorlib]System.Exception
	{
		ldstr "Error: Test_CallParentNonFinalVirtualAfterStarg_Neg threw an unexpected exception"
		call void [System.Console]System.Console::WriteLine(string)
		leave Fail
	}

Next4:
	.try
	{
		newobj instance void [NonVirtualCall_Lib]ChildClass::.ctor()
		call instance void [NonVirtualCall_Lib]ChildClass::Test_CallParentNonFinalVirtualAfterStargS_Neg()
		ldstr "Error: Test_CallParentNonFinalVirtualAfterStargS_Neg did not throw an exception"
		call void [System.Console]System.Console::WriteLine(string)
		leave Fail
	}
	catch [mscorlib]System.Security.VerificationException
	{
		leave Next5
	}
	catch [mscorlib]System.Exception
	{
		ldstr "Error: Test_CallParentNonFinalVirtualAfterStargS_Neg threw an unexpected exception"
		call void [System.Console]System.Console::WriteLine(string)
		leave Fail
	}

Next5:
	newobj instance void [NonVirtualCall_Lib]ChildClass::.ctor()
	dup
	call instance void [NonVirtualCall_Lib]ChildClass::Test_CallParentNonFinalVirtualAfterStargNonZero_Pos(class [NonVirtualCall_Lib]ChildClass)

	.try
	{
		newobj instance void [NonVirtualCall_Lib]ChildClass::.ctor()
		call instance void [NonVirtualCall_Lib]ChildClass::Test_DelegateParentNonFinalVirtualAfterStarg_Neg()
		ldstr "Error: Test_DelegateParentNonFinalVirtualAfterStarg_Neg did not throw an exception"
		call void [System.Console]System.Console::WriteLine(string)
		leave Fail
	}
	catch [mscorlib]System.Security.VerificationException
	{
		leave Next6
	}
	catch [mscorlib]System.Exception
	{
		ldstr "Error: Test_DelegateParentNonFinalVirtualAfterStarg_Neg threw an unexpected exception"
		call void [System.Console]System.Console::WriteLine(string)
		leave Fail
	}

Next6:
	.try
	{
		newobj instance void [NonVirtualCall_Lib]ChildClass::.ctor()
		call instance void [NonVirtualCall_Lib]ChildClass::Test_CallParentNonFinalVirtualAfterLdlocStloc_Neg()
		ldstr "Error: Test_CallParentNonFinalVirtualAfterLdlocStloc_Neg did not throw an exception"
		call void [System.Console]System.Console::WriteLine(string)
		leave Fail
	}
	catch [mscorlib]System.Security.VerificationException
	{
		leave Next7
	}
	catch [mscorlib]System.Exception
	{
		ldstr "Error: Test_CallParentNonFinalVirtualAfterLdlocStloc_Neg threw an unexpected exception"
		call void [System.Console]System.Console::WriteLine(string)
		leave Fail
	}

Next7:
	.try
	{
		newobj instance void [NonVirtualCall_Lib]ChildClass::.ctor()
		call instance void [NonVirtualCall_Lib]ChildClass::Test_CallParentNonFinalVirtualAfterNewobj_Neg()
		ldstr "Error: Test_CallParentNonFinalVirtualAfterNewobj_Neg did not throw an exception"
		call void [System.Console]System.Console::WriteLine(string)
		leave Fail
	}
	catch [mscorlib]System.Security.VerificationException
	{
		leave Next8
	}
	catch [mscorlib]System.Exception
	{
		ldstr "Error: Test_CallParentNonFinalVirtualAfterNewobj_Neg threw an unexpected exception"
		call void [System.Console]System.Console::WriteLine(string)
		leave Fail
	}

Next8:
	.try
	{
		newobj instance void [NonVirtualCall_Lib]ChildClass::.ctor()
		call instance void [NonVirtualCall_Lib]ChildClass::Test_CallParentNonFinalVirtualWithNull_Neg()
		ldstr "Error: Test_CallParentNonFinalVirtualWithNull_Neg did not throw an exception"
		call void [System.Console]System.Console::WriteLine(string)
		leave Fail
	}
	catch [mscorlib]System.Security.VerificationException
	{
		leave Next9
	}
	catch [mscorlib]System.Exception
	{
		ldstr "Error: Test_CallParentNonFinalVirtualWithNull_Neg threw an unexpected exception"
		call void [System.Console]System.Console::WriteLine(string)
		leave Fail
	}

Next9:
	.try
	{
		newobj instance void [NonVirtualCall_Lib]ChildClass::.ctor()
		call instance void [NonVirtualCall_Lib]ChildClass::Test_DelegateParentNonFinalVirtualWithNull_Neg()
		ldstr "Error: Test_DelegateParentNonFinalVirtualWithNull_Neg did not throw an exception"
		call void [System.Console]System.Console::WriteLine(string)
		leave Fail
	}
	catch [mscorlib]System.Security.VerificationException
	{
		leave Next10
	}
	catch [mscorlib]System.Exception
	{
		ldstr "Error: Test_DelegateParentNonFinalVirtualWithNull_Neg threw an unexpected exception"
		call void [System.Console]System.Console::WriteLine(string)
		leave Fail
	}

Next10:
	newobj instance void [NonVirtualCall_Lib]ChildClass::.ctor()
	call instance void [NonVirtualCall_Lib]ChildClass::Test_CallParentNonFinalVirtualWithDup_Pos()

	.try
	{
		newobj instance void [NonVirtualCall_Lib]ChildClass::.ctor()
		ldc.i4.0
		call instance void [NonVirtualCall_Lib]ChildClass::Test_CallParentNonFinalVirtualAfterCondStarg_Neg(int32)
		ldstr "Error: Test_CallParentNonFinalVirtualAfterCondStarg_Neg did not throw an exception"
		call void [System.Console]System.Console::WriteLine(string)
		leave Fail
	}
	catch [mscorlib]System.Security.VerificationException
	{
		leave Next11
	}
	catch [mscorlib]System.Exception
	{
		ldstr "Error: Test_CallParentNonFinalVirtualAfterCondStarg_Neg threw an unexpected exception"
		call void [System.Console]System.Console::WriteLine(string)
		leave Fail
	}

Next11:
	.try
	{
		newobj instance void [NonVirtualCall_Lib]ChildClass::.ctor()
		call instance void [NonVirtualCall_Lib]ChildClass::Test_CallParentNonFinalVirtualInException_Neg()
		ldstr "Error: Test_CallParentNonFinalVirtualInException_Neg did not throw an exception"
		call void [System.Console]System.Console::WriteLine(string)
		leave Fail
	}
	catch [mscorlib]System.Security.VerificationException
	{
		leave Next12
	}
	catch [mscorlib]System.Exception
	{
		ldstr "Error: Test_CallParentNonFinalVirtualInException_Neg threw an unexpected exception"
		call void [System.Console]System.Console::WriteLine(string)
		leave Fail
	}

Next12:
	.try
	{
		newobj instance void [NonVirtualCall_Lib]ChildClass::.ctor()
		call instance void [NonVirtualCall_Lib]ChildClass::Test_CallParentNonFinalVirtualBeforeStarg_Neg()
		ldstr "Error: Test_CallParentNonFinalVirtualBeforeStarg_Neg did not throw an exception"
		call void [System.Console]System.Console::WriteLine(string)
		leave Fail
	}
	catch [mscorlib]System.Security.VerificationException
	{
		leave Next13
	}
	catch [mscorlib]System.Exception
	{
		ldstr "Error: Test_CallParentNonFinalVirtualBeforeStarg_Neg threw an unexpected exception"
		call void [System.Console]System.Console::WriteLine(string)
		leave Fail
	}

Next13:
	newobj instance void [NonVirtualCall_Lib]ChildClass::.ctor()
	call instance void [NonVirtualCall_Lib]ChildClass::Test_CallNonFinalVirtualBoxedValueTypeAfterStarg_Pos()

	ldstr "Test Passed"
	call void [System.Console]System.Console::WriteLine(string)
	ldc.i4 100
	br End

Fail:
	ldstr "Test Failed"
	call void [System.Console]System.Console::WriteLine(string)
	ldc.i4 101

End:
	ret
}
