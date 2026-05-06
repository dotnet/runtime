// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console { }
.assembly extern xunit.core {}
.assembly extern mscorlib {}
.assembly 'dev10_788724' {}

.class public sequential sealed MyStruct0 extends [mscorlib]System.ValueType
{
	.pack 0
	.size 1
	.method public hidebysig virtual instance bool Equals(object obj) cil managed { ldc.i4.0 ret }
	.method public hidebysig virtual instance int32 GetHashCode() cil managed { ldc.i4.0 ret }
	.method public hidebysig virtual instance string ToString() cil managed { ldstr "ToString()" ret }
}
.class public sequential sealed MyStruct1`1<T0> extends [mscorlib]System.ValueType
{
	.pack 0
	.size 1
	.method public hidebysig virtual instance bool Equals(object obj) cil managed { ldc.i4.0 ret }
	.method public hidebysig virtual instance int32 GetHashCode() cil managed { ldc.i4.0 ret }
	.method public hidebysig virtual instance string ToString() cil managed { ldstr "ToString()" ret }
}
.class public auto ansi beforefieldinit Framework {
	.method public hidebysig static void StructCallvirtVerificationTest() cil managed
	{
		.maxstack  10
		ldstr "========================== Struct Callvirt Verification Test =========================="
		call void [System.Console]System.Console::WriteLine(string)
		
		//Testing with a non-generic struct:
		.locals init (valuetype MyStruct0 V_1)
		ldloca V_1
		initobj valuetype MyStruct0
		.try { 
			ldloca V_1 
			ldnull 
			callvirt instance bool valuetype MyStruct0::Equals(object obj) 
			call void [System.Console]System.Console::WriteLine(bool) 
			leave.s LV6
		} catch [mscorlib]System.Security.VerificationException {
			pop
			ldstr "VerificationException1"
			call void [System.Console]System.Console::WriteLine(string)
			leave.s LV6
		} LV6:
		.try { 
			ldloca V_1 
			callvirt 
			instance int32 valuetype MyStruct0::GetHashCode() 
			call void [System.Console]System.Console::WriteLine(int32) 
			leave.s LV7
		} catch [mscorlib]System.Security.VerificationException { 
			pop
			ldstr "VerificationException2"
			call void [System.Console]System.Console::WriteLine(string)
			leave.s LV7
		} LV7:
		.try { 
			ldloca V_1 
			callvirt instance string valuetype MyStruct0::ToString() 
			call void [System.Console]System.Console::WriteLine(string) 
			leave.s LV8
		} catch [mscorlib]System.Security.VerificationException { 
			pop
			ldstr "VerificationException3"
			call void [System.Console]System.Console::WriteLine(string)
			leave.s LV8
		} LV8:
		.try { 
			ldloca V_1 
			ldnull 
			constrained. valuetype MyStruct0 
			callvirt instance bool valuetype MyStruct0::Equals(object) 
			call void [System.Console]System.Console::WriteLine(bool) 
			leave.s LV9
		} catch [mscorlib]System.Security.VerificationException { 
			pop
			ldstr "VerificationException4"
			call void [System.Console]System.Console::WriteLine(string)
			leave.s LV9
		} LV9:
		.try { 
			ldloca V_1 
			constrained. valuetype MyStruct0 
			callvirt instance int32 valuetype MyStruct0::GetHashCode() 
			call void [System.Console]System.Console::WriteLine(int32) 
			leave.s LV10
		} catch [mscorlib]System.Security.VerificationException { 
			pop
			ldstr "VerificationException5"
			call void [System.Console]System.Console::WriteLine(string)
			leave.s LV10
		} LV10:
		.try { 
			ldloca V_1 
			constrained. valuetype MyStruct0 
			callvirt instance string valuetype MyStruct0::ToString() 
			call void [System.Console]System.Console::WriteLine(string) 
			leave.s LV11
		} catch [mscorlib]System.Security.VerificationException { 
			pop
			ldstr "VerificationException6"
			call void [System.Console]System.Console::WriteLine(string)
			leave.s LV11
		} LV11:
		
		//Testing with a generic struct:
		.locals init (valuetype MyStruct1`1<string> V_2)
		ldloca V_2
		initobj valuetype MyStruct1`1<string>
		.try { 
			ldloca V_2 
			ldnull 
			callvirt instance bool valuetype MyStruct1`1<string>::Equals(object obj) 
			call void [System.Console]System.Console::WriteLine(bool) 
			leave.s LV14
		} catch [mscorlib]System.Security.VerificationException { 
			pop
			ldstr "VerificationException7"
			call void [System.Console]System.Console::WriteLine(string)
			leave.s LV14
		} LV14:
		.try { 
			ldloca V_2 
			callvirt instance int32 valuetype MyStruct1`1<string>::GetHashCode() 
			call void [System.Console]System.Console::WriteLine(int32) 
			leave.s LV15
		} catch [mscorlib]System.Security.VerificationException { 
			pop
			ldstr "VerificationException8"
			call void [System.Console]System.Console::WriteLine(string)
			leave.s LV15
		} LV15:
		.try { 
			ldloca V_2 
			callvirt instance string valuetype MyStruct1`1<string>::ToString() 
			call void [System.Console]System.Console::WriteLine(string) 
			leave.s LV16
		} catch [mscorlib]System.Security.VerificationException { 
			pop
			ldstr "VerificationException9"
			call void [System.Console]System.Console::WriteLine(string)
			leave.s LV16
		} LV16:
		.try { 
			ldloca V_2 
			ldnull 
			constrained. valuetype MyStruct1`1<string> 
			callvirt instance bool valuetype MyStruct1`1<string>::Equals(object) 
			call void [System.Console]System.Console::WriteLine(bool) 
			leave.s LV17
		} catch [mscorlib]System.Security.VerificationException { 
			pop
			ldstr "VerificationException10"
			call void [System.Console]System.Console::WriteLine(string)
			leave.s LV17
		} LV17:
		.try { 
			ldloca V_2 
			constrained. valuetype MyStruct1`1<string> 
			callvirt instance int32 valuetype MyStruct1`1<string>::GetHashCode() 
			call void [System.Console]System.Console::WriteLine(int32) 
			leave.s LV18
		} catch [mscorlib]System.Security.VerificationException { 
			pop
			ldstr "VerificationException11"
			call void [System.Console]System.Console::WriteLine(string)
			leave.s LV18
		} LV18:
		.try { 
			ldloca V_2 
			constrained. valuetype MyStruct1`1<string> 
			callvirt instance string valuetype MyStruct1`1<string>::ToString() 
			call void [System.Console]System.Console::WriteLine(string) 
			leave.s LV19
		} catch [mscorlib]System.Security.VerificationException { 
			pop
			ldstr "VerificationException12"
			call void [System.Console]System.Console::WriteLine(string)
			leave.s LV19
		} LV19:

		ldstr "=======================================================================================\n\n"
		call void [System.Console]System.Console::WriteLine(string)
		ret
	}
	.method public hidebysig static int32  Main() cil managed
	{
		.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
		    01 00 00 00
		)
		.entrypoint
		.maxstack  10
		
		call void Framework::StructCallvirtVerificationTest()
		
		ldc.i4 100
		ret
	}
}
