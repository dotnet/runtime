// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern mscorlib { .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) .ver 4:0:0:0 }
.assembly extern TestFramework { .publickeytoken = ( B0 3F 5F 7F 11 D5 0A 3A )  }

//TYPES IN FORWARDER ASSEMBLIES:

//TEST ASSEMBLY:
.assembly Generated170 { .hash algorithm 0x00008004 }
.module Generated170.exe

.class public BaseClass0 
{
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void [mscorlib]System.Object::.ctor()
		ret
	}
}
.class public BaseClass1 
		extends BaseClass0
{
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void BaseClass0::.ctor()
		ret
	}
}
.class public sequential sealed MyStruct220`2<T0, T1> 
		extends [mscorlib]System.ValueType
{
	.pack 0
	.size 1
	.method public hidebysig newslot instance string ClassMethod442() cil managed noinlining { 
		ldstr "MyStruct220::ClassMethod442.1681()"
		ret
	}
	.method public hidebysig newslot instance string ClassMethod443() cil managed noinlining { 
		ldstr "MyStruct220::ClassMethod443.1682()"
		ret
	}
	.method public hidebysig newslot instance string ClassMethod444<M0>() cil managed noinlining { 
		ldstr "MyStruct220::ClassMethod444.1683<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig virtual instance bool Equals(object obj) cil managed { ldc.i4.0 ret }
	.method public hidebysig virtual instance int32 GetHashCode() cil managed { ldc.i4.0 ret }
	.method public hidebysig virtual instance string ToString() cil managed { ldstr "" ret }
}
.class private auto ansi beforefieldinit Framework {
	.method static void M.BaseClass0<(BaseClass0)W>(!!W inst, string exp) cil managed {
		.maxstack 5
		.locals init (string[] actualResults)
		ldc.i4.s    0
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.BaseClass0<(BaseClass0)W>(!!W inst, string exp)"
		ldc.i4.s 0
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.BaseClass1<(BaseClass1)W>(!!W inst, string exp) cil managed {
		.maxstack 5
		.locals init (string[] actualResults)
		ldc.i4.s    0
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.BaseClass1<(BaseClass1)W>(!!W inst, string exp)"
		ldc.i4.s 0
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method public hidebysig static void MethodCallingTest() cil managed
	{
		.maxstack  10
		.locals init (object V_0)
		ldstr "========================== Method Calling Test =========================="
		call void [mscorlib]System.Console::WriteLine(string)
		.locals init (valuetype MyStruct220`2<class BaseClass0,class BaseClass0> V_1)
		ldloca V_1
		initobj valuetype MyStruct220`2<class BaseClass0,class BaseClass0>
		ldloca V_1
		dup
		call instance string valuetype MyStruct220`2<class BaseClass0,class BaseClass0>::ClassMethod442()
		ldstr "MyStruct220::ClassMethod442.1681()"
		ldstr "valuetype MyStruct220`2<class BaseClass0,class BaseClass0> on type MyStruct220"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype MyStruct220`2<class BaseClass0,class BaseClass0>::ClassMethod443()
		ldstr "MyStruct220::ClassMethod443.1682()"
		ldstr "valuetype MyStruct220`2<class BaseClass0,class BaseClass0> on type MyStruct220"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype MyStruct220`2<class BaseClass0,class BaseClass0>::ClassMethod444<object>()
		ldstr "MyStruct220::ClassMethod444.1683<System.Object>()"
		ldstr "valuetype MyStruct220`2<class BaseClass0,class BaseClass0> on type MyStruct220"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup ldnull call instance bool valuetype MyStruct220`2<class BaseClass0,class BaseClass0>::Equals(object) pop
		dup call instance int32 valuetype MyStruct220`2<class BaseClass0,class BaseClass0>::GetHashCode() pop
		dup call instance string valuetype MyStruct220`2<class BaseClass0,class BaseClass0>::ToString() pop
		pop
		.locals init (valuetype MyStruct220`2<class BaseClass0,class BaseClass1> V_2)
		ldloca V_2
		initobj valuetype MyStruct220`2<class BaseClass0,class BaseClass1>
		ldloca V_2
		dup
		call instance string valuetype MyStruct220`2<class BaseClass0,class BaseClass1>::ClassMethod442()
		ldstr "MyStruct220::ClassMethod442.1681()"
		ldstr "valuetype MyStruct220`2<class BaseClass0,class BaseClass1> on type MyStruct220"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype MyStruct220`2<class BaseClass0,class BaseClass1>::ClassMethod443()
		ldstr "MyStruct220::ClassMethod443.1682()"
		ldstr "valuetype MyStruct220`2<class BaseClass0,class BaseClass1> on type MyStruct220"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype MyStruct220`2<class BaseClass0,class BaseClass1>::ClassMethod444<object>()
		ldstr "MyStruct220::ClassMethod444.1683<System.Object>()"
		ldstr "valuetype MyStruct220`2<class BaseClass0,class BaseClass1> on type MyStruct220"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup ldnull call instance bool valuetype MyStruct220`2<class BaseClass0,class BaseClass1>::Equals(object) pop
		dup call instance int32 valuetype MyStruct220`2<class BaseClass0,class BaseClass1>::GetHashCode() pop
		dup call instance string valuetype MyStruct220`2<class BaseClass0,class BaseClass1>::ToString() pop
		pop
		.locals init (valuetype MyStruct220`2<class BaseClass1,class BaseClass0> V_3)
		ldloca V_3
		initobj valuetype MyStruct220`2<class BaseClass1,class BaseClass0>
		ldloca V_3
		dup
		call instance string valuetype MyStruct220`2<class BaseClass1,class BaseClass0>::ClassMethod442()
		ldstr "MyStruct220::ClassMethod442.1681()"
		ldstr "valuetype MyStruct220`2<class BaseClass1,class BaseClass0> on type MyStruct220"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype MyStruct220`2<class BaseClass1,class BaseClass0>::ClassMethod443()
		ldstr "MyStruct220::ClassMethod443.1682()"
		ldstr "valuetype MyStruct220`2<class BaseClass1,class BaseClass0> on type MyStruct220"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype MyStruct220`2<class BaseClass1,class BaseClass0>::ClassMethod444<object>()
		ldstr "MyStruct220::ClassMethod444.1683<System.Object>()"
		ldstr "valuetype MyStruct220`2<class BaseClass1,class BaseClass0> on type MyStruct220"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup ldnull call instance bool valuetype MyStruct220`2<class BaseClass1,class BaseClass0>::Equals(object) pop
		dup call instance int32 valuetype MyStruct220`2<class BaseClass1,class BaseClass0>::GetHashCode() pop
		dup call instance string valuetype MyStruct220`2<class BaseClass1,class BaseClass0>::ToString() pop
		pop
		.locals init (valuetype MyStruct220`2<class BaseClass1,class BaseClass1> V_4)
		ldloca V_4
		initobj valuetype MyStruct220`2<class BaseClass1,class BaseClass1>
		ldloca V_4
		dup
		call instance string valuetype MyStruct220`2<class BaseClass1,class BaseClass1>::ClassMethod442()
		ldstr "MyStruct220::ClassMethod442.1681()"
		ldstr "valuetype MyStruct220`2<class BaseClass1,class BaseClass1> on type MyStruct220"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype MyStruct220`2<class BaseClass1,class BaseClass1>::ClassMethod443()
		ldstr "MyStruct220::ClassMethod443.1682()"
		ldstr "valuetype MyStruct220`2<class BaseClass1,class BaseClass1> on type MyStruct220"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype MyStruct220`2<class BaseClass1,class BaseClass1>::ClassMethod444<object>()
		ldstr "MyStruct220::ClassMethod444.1683<System.Object>()"
		ldstr "valuetype MyStruct220`2<class BaseClass1,class BaseClass1> on type MyStruct220"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup ldnull call instance bool valuetype MyStruct220`2<class BaseClass1,class BaseClass1>::Equals(object) pop
		dup call instance int32 valuetype MyStruct220`2<class BaseClass1,class BaseClass1>::GetHashCode() pop
		dup call instance string valuetype MyStruct220`2<class BaseClass1,class BaseClass1>::ToString() pop
		pop
		ldstr "========================================================================\n\n"
		call void [mscorlib]System.Console::WriteLine(string)
		ret
	}
	.method public hidebysig static void ConstrainedCallsTest() cil managed
	{
		.maxstack  10
		.locals init (object V_0)
		ldstr "========================== Constrained Calls Test =========================="
		call void [mscorlib]System.Console::WriteLine(string)
		.locals init (valuetype MyStruct220`2<class BaseClass0,class BaseClass0> V_5)
		ldloca V_5
		initobj valuetype MyStruct220`2<class BaseClass0,class BaseClass0>
		.locals init (valuetype MyStruct220`2<class BaseClass0,class BaseClass1> V_6)
		ldloca V_6
		initobj valuetype MyStruct220`2<class BaseClass0,class BaseClass1>
		.locals init (valuetype MyStruct220`2<class BaseClass1,class BaseClass0> V_7)
		ldloca V_7
		initobj valuetype MyStruct220`2<class BaseClass1,class BaseClass0>
		.locals init (valuetype MyStruct220`2<class BaseClass1,class BaseClass1> V_8)
		ldloca V_8
		initobj valuetype MyStruct220`2<class BaseClass1,class BaseClass1>
		ldstr "========================================================================\n\n"
		call void [mscorlib]System.Console::WriteLine(string)
		ret
	}
	.method public hidebysig static void StructConstrainedInterfaceCallsTest() cil managed
	{
		.maxstack  10
		ldstr "===================== Struct Constrained Interface Calls Test ====================="
		call void [mscorlib]System.Console::WriteLine(string)
		ldstr "========================================================================\n\n"
		call void [mscorlib]System.Console::WriteLine(string)
		ret
	}
	.method public hidebysig static void CalliTest() cil managed
	{
		.maxstack  10
		.locals init (object V_0)
		ldstr "========================== Method Calli Test =========================="
		call void [mscorlib]System.Console::WriteLine(string)
		.locals init (valuetype MyStruct220`2<class BaseClass0,class BaseClass0> V_9)
		ldloca V_9
		initobj valuetype MyStruct220`2<class BaseClass0,class BaseClass0>
		ldloc V_9
		box valuetype MyStruct220`2<class BaseClass0,class BaseClass0>
		ldloc V_9
		box valuetype MyStruct220`2<class BaseClass0,class BaseClass0>
		ldvirtftn instance string valuetype MyStruct220`2<class BaseClass0,class BaseClass0>::ClassMethod442()
		calli default string(object)
		ldstr "MyStruct220::ClassMethod442.1681()"
		ldstr "valuetype MyStruct220`2<class BaseClass0,class BaseClass0> on type valuetype MyStruct220`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_9
		box valuetype MyStruct220`2<class BaseClass0,class BaseClass0>
		ldloc V_9
		box valuetype MyStruct220`2<class BaseClass0,class BaseClass0>
		ldvirtftn instance string valuetype MyStruct220`2<class BaseClass0,class BaseClass0>::ClassMethod443()
		calli default string(object)
		ldstr "MyStruct220::ClassMethod443.1682()"
		ldstr "valuetype MyStruct220`2<class BaseClass0,class BaseClass0> on type valuetype MyStruct220`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_9
		box valuetype MyStruct220`2<class BaseClass0,class BaseClass0>
		ldloc V_9
		box valuetype MyStruct220`2<class BaseClass0,class BaseClass0>
		ldvirtftn instance string valuetype MyStruct220`2<class BaseClass0,class BaseClass0>::ClassMethod444<object>()
		calli default string(object)
		ldstr "MyStruct220::ClassMethod444.1683<System.Object>()"
		ldstr "valuetype MyStruct220`2<class BaseClass0,class BaseClass0> on type valuetype MyStruct220`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_9 box valuetype MyStruct220`2<class BaseClass0,class BaseClass0> ldnull 
		ldloc V_9 box valuetype MyStruct220`2<class BaseClass0,class BaseClass0>
		ldvirtftn instance bool valuetype MyStruct220`2<class BaseClass0,class BaseClass0>::Equals(object) calli default bool(object,object) pop
		ldloc V_9 box valuetype MyStruct220`2<class BaseClass0,class BaseClass0>
		ldloc V_9 box valuetype MyStruct220`2<class BaseClass0,class BaseClass0>
		ldvirtftn instance int32 valuetype MyStruct220`2<class BaseClass0,class BaseClass0>::GetHashCode() calli default int32(object) pop
		ldloc V_9 box valuetype MyStruct220`2<class BaseClass0,class BaseClass0>
		ldloc V_9 box valuetype MyStruct220`2<class BaseClass0,class BaseClass0>
		ldvirtftn instance string valuetype MyStruct220`2<class BaseClass0,class BaseClass0>::ToString() calli default string(object) pop
		.locals init (valuetype MyStruct220`2<class BaseClass0,class BaseClass1> V_10)
		ldloca V_10
		initobj valuetype MyStruct220`2<class BaseClass0,class BaseClass1>
		ldloc V_10
		box valuetype MyStruct220`2<class BaseClass0,class BaseClass1>
		ldloc V_10
		box valuetype MyStruct220`2<class BaseClass0,class BaseClass1>
		ldvirtftn instance string valuetype MyStruct220`2<class BaseClass0,class BaseClass1>::ClassMethod442()
		calli default string(object)
		ldstr "MyStruct220::ClassMethod442.1681()"
		ldstr "valuetype MyStruct220`2<class BaseClass0,class BaseClass1> on type valuetype MyStruct220`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_10
		box valuetype MyStruct220`2<class BaseClass0,class BaseClass1>
		ldloc V_10
		box valuetype MyStruct220`2<class BaseClass0,class BaseClass1>
		ldvirtftn instance string valuetype MyStruct220`2<class BaseClass0,class BaseClass1>::ClassMethod443()
		calli default string(object)
		ldstr "MyStruct220::ClassMethod443.1682()"
		ldstr "valuetype MyStruct220`2<class BaseClass0,class BaseClass1> on type valuetype MyStruct220`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_10
		box valuetype MyStruct220`2<class BaseClass0,class BaseClass1>
		ldloc V_10
		box valuetype MyStruct220`2<class BaseClass0,class BaseClass1>
		ldvirtftn instance string valuetype MyStruct220`2<class BaseClass0,class BaseClass1>::ClassMethod444<object>()
		calli default string(object)
		ldstr "MyStruct220::ClassMethod444.1683<System.Object>()"
		ldstr "valuetype MyStruct220`2<class BaseClass0,class BaseClass1> on type valuetype MyStruct220`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_10 box valuetype MyStruct220`2<class BaseClass0,class BaseClass1> ldnull 
		ldloc V_10 box valuetype MyStruct220`2<class BaseClass0,class BaseClass1>
		ldvirtftn instance bool valuetype MyStruct220`2<class BaseClass0,class BaseClass1>::Equals(object) calli default bool(object,object) pop
		ldloc V_10 box valuetype MyStruct220`2<class BaseClass0,class BaseClass1>
		ldloc V_10 box valuetype MyStruct220`2<class BaseClass0,class BaseClass1>
		ldvirtftn instance int32 valuetype MyStruct220`2<class BaseClass0,class BaseClass1>::GetHashCode() calli default int32(object) pop
		ldloc V_10 box valuetype MyStruct220`2<class BaseClass0,class BaseClass1>
		ldloc V_10 box valuetype MyStruct220`2<class BaseClass0,class BaseClass1>
		ldvirtftn instance string valuetype MyStruct220`2<class BaseClass0,class BaseClass1>::ToString() calli default string(object) pop
		.locals init (valuetype MyStruct220`2<class BaseClass1,class BaseClass0> V_11)
		ldloca V_11
		initobj valuetype MyStruct220`2<class BaseClass1,class BaseClass0>
		ldloc V_11
		box valuetype MyStruct220`2<class BaseClass1,class BaseClass0>
		ldloc V_11
		box valuetype MyStruct220`2<class BaseClass1,class BaseClass0>
		ldvirtftn instance string valuetype MyStruct220`2<class BaseClass1,class BaseClass0>::ClassMethod442()
		calli default string(object)
		ldstr "MyStruct220::ClassMethod442.1681()"
		ldstr "valuetype MyStruct220`2<class BaseClass1,class BaseClass0> on type valuetype MyStruct220`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_11
		box valuetype MyStruct220`2<class BaseClass1,class BaseClass0>
		ldloc V_11
		box valuetype MyStruct220`2<class BaseClass1,class BaseClass0>
		ldvirtftn instance string valuetype MyStruct220`2<class BaseClass1,class BaseClass0>::ClassMethod443()
		calli default string(object)
		ldstr "MyStruct220::ClassMethod443.1682()"
		ldstr "valuetype MyStruct220`2<class BaseClass1,class BaseClass0> on type valuetype MyStruct220`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_11
		box valuetype MyStruct220`2<class BaseClass1,class BaseClass0>
		ldloc V_11
		box valuetype MyStruct220`2<class BaseClass1,class BaseClass0>
		ldvirtftn instance string valuetype MyStruct220`2<class BaseClass1,class BaseClass0>::ClassMethod444<object>()
		calli default string(object)
		ldstr "MyStruct220::ClassMethod444.1683<System.Object>()"
		ldstr "valuetype MyStruct220`2<class BaseClass1,class BaseClass0> on type valuetype MyStruct220`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_11 box valuetype MyStruct220`2<class BaseClass1,class BaseClass0> ldnull 
		ldloc V_11 box valuetype MyStruct220`2<class BaseClass1,class BaseClass0>
		ldvirtftn instance bool valuetype MyStruct220`2<class BaseClass1,class BaseClass0>::Equals(object) calli default bool(object,object) pop
		ldloc V_11 box valuetype MyStruct220`2<class BaseClass1,class BaseClass0>
		ldloc V_11 box valuetype MyStruct220`2<class BaseClass1,class BaseClass0>
		ldvirtftn instance int32 valuetype MyStruct220`2<class BaseClass1,class BaseClass0>::GetHashCode() calli default int32(object) pop
		ldloc V_11 box valuetype MyStruct220`2<class BaseClass1,class BaseClass0>
		ldloc V_11 box valuetype MyStruct220`2<class BaseClass1,class BaseClass0>
		ldvirtftn instance string valuetype MyStruct220`2<class BaseClass1,class BaseClass0>::ToString() calli default string(object) pop
		.locals init (valuetype MyStruct220`2<class BaseClass1,class BaseClass1> V_12)
		ldloca V_12
		initobj valuetype MyStruct220`2<class BaseClass1,class BaseClass1>
		ldloc V_12
		box valuetype MyStruct220`2<class BaseClass1,class BaseClass1>
		ldloc V_12
		box valuetype MyStruct220`2<class BaseClass1,class BaseClass1>
		ldvirtftn instance string valuetype MyStruct220`2<class BaseClass1,class BaseClass1>::ClassMethod442()
		calli default string(object)
		ldstr "MyStruct220::ClassMethod442.1681()"
		ldstr "valuetype MyStruct220`2<class BaseClass1,class BaseClass1> on type valuetype MyStruct220`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_12
		box valuetype MyStruct220`2<class BaseClass1,class BaseClass1>
		ldloc V_12
		box valuetype MyStruct220`2<class BaseClass1,class BaseClass1>
		ldvirtftn instance string valuetype MyStruct220`2<class BaseClass1,class BaseClass1>::ClassMethod443()
		calli default string(object)
		ldstr "MyStruct220::ClassMethod443.1682()"
		ldstr "valuetype MyStruct220`2<class BaseClass1,class BaseClass1> on type valuetype MyStruct220`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_12
		box valuetype MyStruct220`2<class BaseClass1,class BaseClass1>
		ldloc V_12
		box valuetype MyStruct220`2<class BaseClass1,class BaseClass1>
		ldvirtftn instance string valuetype MyStruct220`2<class BaseClass1,class BaseClass1>::ClassMethod444<object>()
		calli default string(object)
		ldstr "MyStruct220::ClassMethod444.1683<System.Object>()"
		ldstr "valuetype MyStruct220`2<class BaseClass1,class BaseClass1> on type valuetype MyStruct220`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_12 box valuetype MyStruct220`2<class BaseClass1,class BaseClass1> ldnull 
		ldloc V_12 box valuetype MyStruct220`2<class BaseClass1,class BaseClass1>
		ldvirtftn instance bool valuetype MyStruct220`2<class BaseClass1,class BaseClass1>::Equals(object) calli default bool(object,object) pop
		ldloc V_12 box valuetype MyStruct220`2<class BaseClass1,class BaseClass1>
		ldloc V_12 box valuetype MyStruct220`2<class BaseClass1,class BaseClass1>
		ldvirtftn instance int32 valuetype MyStruct220`2<class BaseClass1,class BaseClass1>::GetHashCode() calli default int32(object) pop
		ldloc V_12 box valuetype MyStruct220`2<class BaseClass1,class BaseClass1>
		ldloc V_12 box valuetype MyStruct220`2<class BaseClass1,class BaseClass1>
		ldvirtftn instance string valuetype MyStruct220`2<class BaseClass1,class BaseClass1>::ToString() calli default string(object) pop
		ldstr "========================================================================\n\n"
		call void [mscorlib]System.Console::WriteLine(string)
		ret
	}
	.method public hidebysig static int32  Main() cil managed
	{
		.entrypoint
		.maxstack  10
		call void Framework::MethodCallingTest()
		call void Framework::ConstrainedCallsTest()
		call void Framework::StructConstrainedInterfaceCallsTest()
		call void Framework::CalliTest()
		ldc.i4 100
		ret
	}
}
