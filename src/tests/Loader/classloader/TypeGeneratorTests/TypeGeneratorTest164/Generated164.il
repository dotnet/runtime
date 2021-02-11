// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) .ver 4:0:0:0 }
.assembly extern TestFramework { .publickeytoken = ( B0 3F 5F 7F 11 D5 0A 3A )  }

//TYPES IN FORWARDER ASSEMBLIES:

//TEST ASSEMBLY:
.assembly Generated164 { .hash algorithm 0x00008004 }
.module Generated164.exe

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
.class public sequential sealed MyStruct214`1<T0> 
		extends [mscorlib]System.ValueType
		implements class IBase2`2<!T0,!T0>, class IBase2`2<class BaseClass0,!T0> 
{
	.pack 0
	.size 1
	.method public hidebysig virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "MyStruct214::Method7.1646<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase2<class BaseClass0,T0>.Method7'<M0>() cil managed noinlining { 
		.override method instance string class IBase2`2<class BaseClass0,!T0>::Method7<[1]>()
		ldstr "MyStruct214::Method7.MI.1648<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot instance string ClassMethod425() cil managed noinlining { 
		ldstr "MyStruct214::ClassMethod425.1649()"
		ret
	}
	.method public hidebysig newslot instance string ClassMethod426() cil managed noinlining { 
		ldstr "MyStruct214::ClassMethod426.1650()"
		ret
	}
	.method public hidebysig virtual instance bool Equals(object obj) cil managed { ldc.i4.0 ret }
	.method public hidebysig virtual instance int32 GetHashCode() cil managed { ldc.i4.0 ret }
	.method public hidebysig virtual instance string ToString() cil managed { ldstr "" ret }
}
.class interface public abstract IBase2`2<+T0, -T1> 
{
	.method public hidebysig newslot abstract virtual instance string Method7<M0>() cil managed { }
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
	.method static void M.IBase2.T.T<T0,T1,(class IBase2`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.T.T<T0,T1,(class IBase2`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class IBase2`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.A.T<T1,(class IBase2`2<class BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.A.T<T1,(class IBase2`2<class BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class IBase2`2<class BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.A.A<(class IBase2`2<class BaseClass0,class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.A.A<(class IBase2`2<class BaseClass0,class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.A.B<(class IBase2`2<class BaseClass0,class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.A.B<(class IBase2`2<class BaseClass0,class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.B.T<T1,(class IBase2`2<class BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.B.T<T1,(class IBase2`2<class BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class IBase2`2<class BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.B.A<(class IBase2`2<class BaseClass1,class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.B.A<(class IBase2`2<class BaseClass1,class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.B.B<(class IBase2`2<class BaseClass1,class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.B.B<(class IBase2`2<class BaseClass1,class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.MyStruct214.T<T0,(valuetype MyStruct214`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 7
		.locals init (string[] actualResults)
		ldc.i4.s    2
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.MyStruct214.T<T0,(valuetype MyStruct214`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 2
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. valuetype MyStruct214`1<!!T0>
		callvirt     instance string class IBase2`2<!!T0,!!T0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. valuetype MyStruct214`1<!!T0>
		callvirt     instance string class IBase2`2<class BaseClass0,!!T0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.MyStruct214.B<(valuetype MyStruct214`1<class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 7
		.locals init (string[] actualResults)
		ldc.i4.s    2
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.MyStruct214.B<(valuetype MyStruct214`1<class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 2
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. valuetype MyStruct214`1<class BaseClass1>
		callvirt     instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. valuetype MyStruct214`1<class BaseClass1>
		callvirt     instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		stelem.ref
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
		.locals init (valuetype MyStruct214`1<class BaseClass1> V_2)
		ldloca V_2
		initobj valuetype MyStruct214`1<class BaseClass1>
		ldloca V_2
		dup
		call instance string valuetype MyStruct214`1<class BaseClass1>::Method7<object>()
		ldstr "MyStruct214::Method7.1646<System.Object>()"
		ldstr "valuetype MyStruct214`1<class BaseClass1> on type MyStruct214"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype MyStruct214`1<class BaseClass1>::ClassMethod425()
		ldstr "MyStruct214::ClassMethod425.1649()"
		ldstr "valuetype MyStruct214`1<class BaseClass1> on type MyStruct214"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype MyStruct214`1<class BaseClass1>::ClassMethod426()
		ldstr "MyStruct214::ClassMethod426.1650()"
		ldstr "valuetype MyStruct214`1<class BaseClass1> on type MyStruct214"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup ldnull call instance bool valuetype MyStruct214`1<class BaseClass1>::Equals(object) pop
		dup call instance int32 valuetype MyStruct214`1<class BaseClass1>::GetHashCode() pop
		dup call instance string valuetype MyStruct214`1<class BaseClass1>::ToString() pop
		pop
		ldloc V_2
		box valuetype MyStruct214`1<class BaseClass1>
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "MyStruct214::Method7.1646<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type valuetype MyStruct214`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc V_2
		box valuetype MyStruct214`1<class BaseClass1>
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "MyStruct214::Method7.MI.1648<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type valuetype MyStruct214`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
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
		.locals init (valuetype MyStruct214`1<class BaseClass1> V_4)
		ldloca V_4
		initobj valuetype MyStruct214`1<class BaseClass1>
		.try {  ldloc V_4
				ldstr "MyStruct214::Method7.1646<System.Object>()#"
				call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass1,valuetype MyStruct214`1<class BaseClass1>>(!!2,string) leave.s LV0
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV0} LV0:
		.try {  ldloc V_4
				ldstr "MyStruct214::Method7.1646<System.Object>()#"
				call void Framework::M.IBase2.B.T<class BaseClass1,valuetype MyStruct214`1<class BaseClass1>>(!!1,string) leave.s LV1
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV1} LV1:
		.try {  ldloc V_4
				ldstr "MyStruct214::Method7.1646<System.Object>()#"
				call void Framework::M.IBase2.B.B<valuetype MyStruct214`1<class BaseClass1>>(!!0,string) leave.s LV2
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV2} LV2:
		.try {  ldloc V_4
				ldstr "MyStruct214::Method7.MI.1648<System.Object>()#"
				call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,valuetype MyStruct214`1<class BaseClass1>>(!!2,string) leave.s LV3
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV3} LV3:
		.try {  ldloc V_4
				ldstr "MyStruct214::Method7.MI.1648<System.Object>()#"
				call void Framework::M.IBase2.A.T<class BaseClass1,valuetype MyStruct214`1<class BaseClass1>>(!!1,string) leave.s LV4
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV4} LV4:
		.try {  ldloc V_4
				ldstr "MyStruct214::Method7.MI.1648<System.Object>()#"
				call void Framework::M.IBase2.A.B<valuetype MyStruct214`1<class BaseClass1>>(!!0,string) leave.s LV5
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV5} LV5:
		ldstr "========================================================================\n\n"
		call void [mscorlib]System.Console::WriteLine(string)
		ret
	}
	.method public hidebysig static void StructConstrainedInterfaceCallsTest() cil managed
	{
		.maxstack  10
		ldstr "===================== Struct Constrained Interface Calls Test ====================="
		call void [mscorlib]System.Console::WriteLine(string)
		.locals init (valuetype MyStruct214`1<class BaseClass1> V_6)
		ldloca V_6
		initobj valuetype MyStruct214`1<class BaseClass1>
		.try {  ldloc V_6
				ldstr "MyStruct214::Method7.1646<System.Object>()#" +
				      "MyStruct214::Method7.MI.1648<System.Object>()#"
				call void Framework::M.MyStruct214.T<class BaseClass1,valuetype MyStruct214`1<class BaseClass1>>(!!1,string) leave.s LV0
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV0} LV0:
		.try {  ldloc V_6
				ldstr "MyStruct214::Method7.1646<System.Object>()#" +
				      "MyStruct214::Method7.MI.1648<System.Object>()#"
				call void Framework::M.MyStruct214.B<valuetype MyStruct214`1<class BaseClass1>>(!!0,string) leave.s LV1
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV1} LV1:
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
		.locals init (valuetype MyStruct214`1<class BaseClass1> V_8)
		ldloca V_8
		initobj valuetype MyStruct214`1<class BaseClass1>
		ldloc V_8
		box valuetype MyStruct214`1<class BaseClass1>
		ldloc V_8
		box valuetype MyStruct214`1<class BaseClass1>
		ldvirtftn instance string valuetype MyStruct214`1<class BaseClass1>::Method7<object>()
		calli default string(object)
		ldstr "MyStruct214::Method7.1646<System.Object>()"
		ldstr "valuetype MyStruct214`1<class BaseClass1> on type valuetype MyStruct214`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_8
		box valuetype MyStruct214`1<class BaseClass1>
		ldloc V_8
		box valuetype MyStruct214`1<class BaseClass1>
		ldvirtftn instance string valuetype MyStruct214`1<class BaseClass1>::ClassMethod425()
		calli default string(object)
		ldstr "MyStruct214::ClassMethod425.1649()"
		ldstr "valuetype MyStruct214`1<class BaseClass1> on type valuetype MyStruct214`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_8
		box valuetype MyStruct214`1<class BaseClass1>
		ldloc V_8
		box valuetype MyStruct214`1<class BaseClass1>
		ldvirtftn instance string valuetype MyStruct214`1<class BaseClass1>::ClassMethod426()
		calli default string(object)
		ldstr "MyStruct214::ClassMethod426.1650()"
		ldstr "valuetype MyStruct214`1<class BaseClass1> on type valuetype MyStruct214`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_8 box valuetype MyStruct214`1<class BaseClass1> ldnull 
		ldloc V_8 box valuetype MyStruct214`1<class BaseClass1>
		ldvirtftn instance bool valuetype MyStruct214`1<class BaseClass1>::Equals(object) calli default bool(object,object) pop
		ldloc V_8 box valuetype MyStruct214`1<class BaseClass1>
		ldloc V_8 box valuetype MyStruct214`1<class BaseClass1>
		ldvirtftn instance int32 valuetype MyStruct214`1<class BaseClass1>::GetHashCode() calli default int32(object) pop
		ldloc V_8 box valuetype MyStruct214`1<class BaseClass1>
		ldloc V_8 box valuetype MyStruct214`1<class BaseClass1>
		ldvirtftn instance string valuetype MyStruct214`1<class BaseClass1>::ToString() calli default string(object) pop
		ldloc V_8
		box valuetype MyStruct214`1<class BaseClass1>
		ldloc V_8
		box valuetype MyStruct214`1<class BaseClass1>
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(object)
		ldstr "MyStruct214::Method7.1646<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type valuetype MyStruct214`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_8
		box valuetype MyStruct214`1<class BaseClass1>
		ldloc V_8
		box valuetype MyStruct214`1<class BaseClass1>
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(object)
		ldstr "MyStruct214::Method7.MI.1648<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type valuetype MyStruct214`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
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
