// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) .ver 4:0:0:0 }
.assembly extern TestFramework { .publickeytoken = ( B0 3F 5F 7F 11 D5 0A 3A )  }

//TYPES IN FORWARDER ASSEMBLIES:

//TEST ASSEMBLY:
.assembly Generated581 { .hash algorithm 0x00008004 }
.module Generated581.exe

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
.class public sequential sealed MyStruct631`2<T0, T1> 
		extends [mscorlib]System.ValueType
		implements class IBase2`2<!T1,class BaseClass1>, class IBase2`2<!T1,!T1> 
{
	.pack 0
	.size 1
	.method public hidebysig virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "MyStruct631::Method7.4618<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase2<T1,T1>.Method7'<M0>() cil managed noinlining { 
		.override method instance string class IBase2`2<!T1,!T1>::Method7<[1]>()
		ldstr "MyStruct631::Method7.MI.4620<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot instance string ClassMethod1258() cil managed noinlining { 
		ldstr "MyStruct631::ClassMethod1258.4621()"
		ret
	}
	.method public hidebysig newslot instance string ClassMethod1259() cil managed noinlining { 
		ldstr "MyStruct631::ClassMethod1259.4622()"
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
	.method static void M.MyStruct631.T.T<T0,T1,(valuetype MyStruct631`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 7
		.locals init (string[] actualResults)
		ldc.i4.s    2
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.MyStruct631.T.T<T0,T1,(valuetype MyStruct631`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 2
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. valuetype MyStruct631`2<!!T0,!!T1>
		callvirt     instance string class IBase2`2<!!T1,class BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. valuetype MyStruct631`2<!!T0,!!T1>
		callvirt     instance string class IBase2`2<!!T1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.MyStruct631.A.T<T1,(valuetype MyStruct631`2<class BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 7
		.locals init (string[] actualResults)
		ldc.i4.s    2
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.MyStruct631.A.T<T1,(valuetype MyStruct631`2<class BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 2
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. valuetype MyStruct631`2<class BaseClass0,!!T1>
		callvirt     instance string class IBase2`2<!!T1,class BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. valuetype MyStruct631`2<class BaseClass0,!!T1>
		callvirt     instance string class IBase2`2<!!T1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.MyStruct631.A.A<(valuetype MyStruct631`2<class BaseClass0,class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 7
		.locals init (string[] actualResults)
		ldc.i4.s    2
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.MyStruct631.A.A<(valuetype MyStruct631`2<class BaseClass0,class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 2
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. valuetype MyStruct631`2<class BaseClass0,class BaseClass0>
		callvirt     instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. valuetype MyStruct631`2<class BaseClass0,class BaseClass0>
		callvirt     instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.MyStruct631.B.T<T1,(valuetype MyStruct631`2<class BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 7
		.locals init (string[] actualResults)
		ldc.i4.s    2
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.MyStruct631.B.T<T1,(valuetype MyStruct631`2<class BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 2
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. valuetype MyStruct631`2<class BaseClass1,!!T1>
		callvirt     instance string class IBase2`2<!!T1,class BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. valuetype MyStruct631`2<class BaseClass1,!!T1>
		callvirt     instance string class IBase2`2<!!T1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.MyStruct631.B.A<(valuetype MyStruct631`2<class BaseClass1,class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 7
		.locals init (string[] actualResults)
		ldc.i4.s    2
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.MyStruct631.B.A<(valuetype MyStruct631`2<class BaseClass1,class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 2
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. valuetype MyStruct631`2<class BaseClass1,class BaseClass0>
		callvirt     instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. valuetype MyStruct631`2<class BaseClass1,class BaseClass0>
		callvirt     instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
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
		.locals init (valuetype MyStruct631`2<class BaseClass0,class BaseClass0> V_1)
		ldloca V_1
		initobj valuetype MyStruct631`2<class BaseClass0,class BaseClass0>
		ldloca V_1
		dup
		call instance string valuetype MyStruct631`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "MyStruct631::Method7.4618<System.Object>()"
		ldstr "valuetype MyStruct631`2<class BaseClass0,class BaseClass0> on type MyStruct631"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype MyStruct631`2<class BaseClass0,class BaseClass0>::ClassMethod1258()
		ldstr "MyStruct631::ClassMethod1258.4621()"
		ldstr "valuetype MyStruct631`2<class BaseClass0,class BaseClass0> on type MyStruct631"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype MyStruct631`2<class BaseClass0,class BaseClass0>::ClassMethod1259()
		ldstr "MyStruct631::ClassMethod1259.4622()"
		ldstr "valuetype MyStruct631`2<class BaseClass0,class BaseClass0> on type MyStruct631"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup ldnull call instance bool valuetype MyStruct631`2<class BaseClass0,class BaseClass0>::Equals(object) pop
		dup call instance int32 valuetype MyStruct631`2<class BaseClass0,class BaseClass0>::GetHashCode() pop
		dup call instance string valuetype MyStruct631`2<class BaseClass0,class BaseClass0>::ToString() pop
		pop
		ldloc V_1
		box valuetype MyStruct631`2<class BaseClass0,class BaseClass0>
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "MyStruct631::Method7.4618<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type valuetype MyStruct631`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc V_1
		box valuetype MyStruct631`2<class BaseClass0,class BaseClass0>
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "MyStruct631::Method7.MI.4620<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type valuetype MyStruct631`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		.locals init (valuetype MyStruct631`2<class BaseClass1,class BaseClass0> V_3)
		ldloca V_3
		initobj valuetype MyStruct631`2<class BaseClass1,class BaseClass0>
		ldloca V_3
		dup
		call instance string valuetype MyStruct631`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "MyStruct631::Method7.4618<System.Object>()"
		ldstr "valuetype MyStruct631`2<class BaseClass1,class BaseClass0> on type MyStruct631"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype MyStruct631`2<class BaseClass1,class BaseClass0>::ClassMethod1258()
		ldstr "MyStruct631::ClassMethod1258.4621()"
		ldstr "valuetype MyStruct631`2<class BaseClass1,class BaseClass0> on type MyStruct631"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype MyStruct631`2<class BaseClass1,class BaseClass0>::ClassMethod1259()
		ldstr "MyStruct631::ClassMethod1259.4622()"
		ldstr "valuetype MyStruct631`2<class BaseClass1,class BaseClass0> on type MyStruct631"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup ldnull call instance bool valuetype MyStruct631`2<class BaseClass1,class BaseClass0>::Equals(object) pop
		dup call instance int32 valuetype MyStruct631`2<class BaseClass1,class BaseClass0>::GetHashCode() pop
		dup call instance string valuetype MyStruct631`2<class BaseClass1,class BaseClass0>::ToString() pop
		pop
		ldloc V_3
		box valuetype MyStruct631`2<class BaseClass1,class BaseClass0>
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "MyStruct631::Method7.4618<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type valuetype MyStruct631`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc V_3
		box valuetype MyStruct631`2<class BaseClass1,class BaseClass0>
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "MyStruct631::Method7.MI.4620<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type valuetype MyStruct631`2<class BaseClass1,class BaseClass0>"
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
		.locals init (valuetype MyStruct631`2<class BaseClass0,class BaseClass0> V_5)
		ldloca V_5
		initobj valuetype MyStruct631`2<class BaseClass0,class BaseClass0>
		.try {  ldloc V_5
				ldstr "MyStruct631::Method7.4618<System.Object>()#"
				call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,valuetype MyStruct631`2<class BaseClass0,class BaseClass0>>(!!2,string) leave.s LV0
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV0} LV0:
		.try {  ldloc V_5
				ldstr "MyStruct631::Method7.4618<System.Object>()#"
				call void Framework::M.IBase2.A.T<class BaseClass1,valuetype MyStruct631`2<class BaseClass0,class BaseClass0>>(!!1,string) leave.s LV1
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV1} LV1:
		.try {  ldloc V_5
				ldstr "MyStruct631::Method7.4618<System.Object>()#"
				call void Framework::M.IBase2.A.B<valuetype MyStruct631`2<class BaseClass0,class BaseClass0>>(!!0,string) leave.s LV2
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV2} LV2:
		.try {  ldloc V_5
				ldstr "MyStruct631::Method7.MI.4620<System.Object>()#"
				call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass0,valuetype MyStruct631`2<class BaseClass0,class BaseClass0>>(!!2,string) leave.s LV3
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV3} LV3:
		.try {  ldloc V_5
				ldstr "MyStruct631::Method7.MI.4620<System.Object>()#"
				call void Framework::M.IBase2.A.T<class BaseClass0,valuetype MyStruct631`2<class BaseClass0,class BaseClass0>>(!!1,string) leave.s LV4
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV4} LV4:
		.try {  ldloc V_5
				ldstr "MyStruct631::Method7.MI.4620<System.Object>()#"
				call void Framework::M.IBase2.A.A<valuetype MyStruct631`2<class BaseClass0,class BaseClass0>>(!!0,string) leave.s LV5
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV5} LV5:
		.locals init (valuetype MyStruct631`2<class BaseClass1,class BaseClass0> V_7)
		ldloca V_7
		initobj valuetype MyStruct631`2<class BaseClass1,class BaseClass0>
		.try {  ldloc V_7
				ldstr "MyStruct631::Method7.4618<System.Object>()#"
				call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,valuetype MyStruct631`2<class BaseClass1,class BaseClass0>>(!!2,string) leave.s LV6
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV6} LV6:
		.try {  ldloc V_7
				ldstr "MyStruct631::Method7.4618<System.Object>()#"
				call void Framework::M.IBase2.A.T<class BaseClass1,valuetype MyStruct631`2<class BaseClass1,class BaseClass0>>(!!1,string) leave.s LV7
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV7} LV7:
		.try {  ldloc V_7
				ldstr "MyStruct631::Method7.4618<System.Object>()#"
				call void Framework::M.IBase2.A.B<valuetype MyStruct631`2<class BaseClass1,class BaseClass0>>(!!0,string) leave.s LV8
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV8} LV8:
		.try {  ldloc V_7
				ldstr "MyStruct631::Method7.MI.4620<System.Object>()#"
				call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass0,valuetype MyStruct631`2<class BaseClass1,class BaseClass0>>(!!2,string) leave.s LV9
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV9} LV9:
		.try {  ldloc V_7
				ldstr "MyStruct631::Method7.MI.4620<System.Object>()#"
				call void Framework::M.IBase2.A.T<class BaseClass0,valuetype MyStruct631`2<class BaseClass1,class BaseClass0>>(!!1,string) leave.s LV10
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV10} LV10:
		.try {  ldloc V_7
				ldstr "MyStruct631::Method7.MI.4620<System.Object>()#"
				call void Framework::M.IBase2.A.A<valuetype MyStruct631`2<class BaseClass1,class BaseClass0>>(!!0,string) leave.s LV11
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV11} LV11:
		ldstr "========================================================================\n\n"
		call void [mscorlib]System.Console::WriteLine(string)
		ret
	}
	.method public hidebysig static void StructConstrainedInterfaceCallsTest() cil managed
	{
		.maxstack  10
		ldstr "===================== Struct Constrained Interface Calls Test ====================="
		call void [mscorlib]System.Console::WriteLine(string)
		.locals init (valuetype MyStruct631`2<class BaseClass0,class BaseClass0> V_9)
		ldloca V_9
		initobj valuetype MyStruct631`2<class BaseClass0,class BaseClass0>
		.try {  ldloc V_9
				ldstr "MyStruct631::Method7.4618<System.Object>()#" +
				      "MyStruct631::Method7.MI.4620<System.Object>()#"
				call void Framework::M.MyStruct631.T.T<class BaseClass0,class BaseClass0,valuetype MyStruct631`2<class BaseClass0,class BaseClass0>>(!!2,string) leave.s LV0
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV0} LV0:
		.try {  ldloc V_9
				ldstr "MyStruct631::Method7.4618<System.Object>()#" +
				      "MyStruct631::Method7.MI.4620<System.Object>()#"
				call void Framework::M.MyStruct631.A.T<class BaseClass0,valuetype MyStruct631`2<class BaseClass0,class BaseClass0>>(!!1,string) leave.s LV1
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV1} LV1:
		.try {  ldloc V_9
				ldstr "MyStruct631::Method7.4618<System.Object>()#" +
				      "MyStruct631::Method7.MI.4620<System.Object>()#"
				call void Framework::M.MyStruct631.A.A<valuetype MyStruct631`2<class BaseClass0,class BaseClass0>>(!!0,string) leave.s LV2
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV2} LV2:
		.locals init (valuetype MyStruct631`2<class BaseClass1,class BaseClass0> V_11)
		ldloca V_11
		initobj valuetype MyStruct631`2<class BaseClass1,class BaseClass0>
		.try {  ldloc V_11
				ldstr "MyStruct631::Method7.4618<System.Object>()#" +
				      "MyStruct631::Method7.MI.4620<System.Object>()#"
				call void Framework::M.MyStruct631.T.T<class BaseClass1,class BaseClass0,valuetype MyStruct631`2<class BaseClass1,class BaseClass0>>(!!2,string) leave.s LV3
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV3} LV3:
		.try {  ldloc V_11
				ldstr "MyStruct631::Method7.4618<System.Object>()#" +
				      "MyStruct631::Method7.MI.4620<System.Object>()#"
				call void Framework::M.MyStruct631.B.T<class BaseClass0,valuetype MyStruct631`2<class BaseClass1,class BaseClass0>>(!!1,string) leave.s LV4
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV4} LV4:
		.try {  ldloc V_11
				ldstr "MyStruct631::Method7.4618<System.Object>()#" +
				      "MyStruct631::Method7.MI.4620<System.Object>()#"
				call void Framework::M.MyStruct631.B.A<valuetype MyStruct631`2<class BaseClass1,class BaseClass0>>(!!0,string) leave.s LV5
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV5} LV5:
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
		.locals init (valuetype MyStruct631`2<class BaseClass0,class BaseClass0> V_13)
		ldloca V_13
		initobj valuetype MyStruct631`2<class BaseClass0,class BaseClass0>
		ldloc V_13
		box valuetype MyStruct631`2<class BaseClass0,class BaseClass0>
		ldloc V_13
		box valuetype MyStruct631`2<class BaseClass0,class BaseClass0>
		ldvirtftn instance string valuetype MyStruct631`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(object)
		ldstr "MyStruct631::Method7.4618<System.Object>()"
		ldstr "valuetype MyStruct631`2<class BaseClass0,class BaseClass0> on type valuetype MyStruct631`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_13
		box valuetype MyStruct631`2<class BaseClass0,class BaseClass0>
		ldloc V_13
		box valuetype MyStruct631`2<class BaseClass0,class BaseClass0>
		ldvirtftn instance string valuetype MyStruct631`2<class BaseClass0,class BaseClass0>::ClassMethod1258()
		calli default string(object)
		ldstr "MyStruct631::ClassMethod1258.4621()"
		ldstr "valuetype MyStruct631`2<class BaseClass0,class BaseClass0> on type valuetype MyStruct631`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_13
		box valuetype MyStruct631`2<class BaseClass0,class BaseClass0>
		ldloc V_13
		box valuetype MyStruct631`2<class BaseClass0,class BaseClass0>
		ldvirtftn instance string valuetype MyStruct631`2<class BaseClass0,class BaseClass0>::ClassMethod1259()
		calli default string(object)
		ldstr "MyStruct631::ClassMethod1259.4622()"
		ldstr "valuetype MyStruct631`2<class BaseClass0,class BaseClass0> on type valuetype MyStruct631`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_13 box valuetype MyStruct631`2<class BaseClass0,class BaseClass0> ldnull 
		ldloc V_13 box valuetype MyStruct631`2<class BaseClass0,class BaseClass0>
		ldvirtftn instance bool valuetype MyStruct631`2<class BaseClass0,class BaseClass0>::Equals(object) calli default bool(object,object) pop
		ldloc V_13 box valuetype MyStruct631`2<class BaseClass0,class BaseClass0>
		ldloc V_13 box valuetype MyStruct631`2<class BaseClass0,class BaseClass0>
		ldvirtftn instance int32 valuetype MyStruct631`2<class BaseClass0,class BaseClass0>::GetHashCode() calli default int32(object) pop
		ldloc V_13 box valuetype MyStruct631`2<class BaseClass0,class BaseClass0>
		ldloc V_13 box valuetype MyStruct631`2<class BaseClass0,class BaseClass0>
		ldvirtftn instance string valuetype MyStruct631`2<class BaseClass0,class BaseClass0>::ToString() calli default string(object) pop
		ldloc V_13
		box valuetype MyStruct631`2<class BaseClass0,class BaseClass0>
		ldloc V_13
		box valuetype MyStruct631`2<class BaseClass0,class BaseClass0>
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(object)
		ldstr "MyStruct631::Method7.4618<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type valuetype MyStruct631`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_13
		box valuetype MyStruct631`2<class BaseClass0,class BaseClass0>
		ldloc V_13
		box valuetype MyStruct631`2<class BaseClass0,class BaseClass0>
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(object)
		ldstr "MyStruct631::Method7.MI.4620<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type valuetype MyStruct631`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		.locals init (valuetype MyStruct631`2<class BaseClass1,class BaseClass0> V_15)
		ldloca V_15
		initobj valuetype MyStruct631`2<class BaseClass1,class BaseClass0>
		ldloc V_15
		box valuetype MyStruct631`2<class BaseClass1,class BaseClass0>
		ldloc V_15
		box valuetype MyStruct631`2<class BaseClass1,class BaseClass0>
		ldvirtftn instance string valuetype MyStruct631`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(object)
		ldstr "MyStruct631::Method7.4618<System.Object>()"
		ldstr "valuetype MyStruct631`2<class BaseClass1,class BaseClass0> on type valuetype MyStruct631`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_15
		box valuetype MyStruct631`2<class BaseClass1,class BaseClass0>
		ldloc V_15
		box valuetype MyStruct631`2<class BaseClass1,class BaseClass0>
		ldvirtftn instance string valuetype MyStruct631`2<class BaseClass1,class BaseClass0>::ClassMethod1258()
		calli default string(object)
		ldstr "MyStruct631::ClassMethod1258.4621()"
		ldstr "valuetype MyStruct631`2<class BaseClass1,class BaseClass0> on type valuetype MyStruct631`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_15
		box valuetype MyStruct631`2<class BaseClass1,class BaseClass0>
		ldloc V_15
		box valuetype MyStruct631`2<class BaseClass1,class BaseClass0>
		ldvirtftn instance string valuetype MyStruct631`2<class BaseClass1,class BaseClass0>::ClassMethod1259()
		calli default string(object)
		ldstr "MyStruct631::ClassMethod1259.4622()"
		ldstr "valuetype MyStruct631`2<class BaseClass1,class BaseClass0> on type valuetype MyStruct631`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_15 box valuetype MyStruct631`2<class BaseClass1,class BaseClass0> ldnull 
		ldloc V_15 box valuetype MyStruct631`2<class BaseClass1,class BaseClass0>
		ldvirtftn instance bool valuetype MyStruct631`2<class BaseClass1,class BaseClass0>::Equals(object) calli default bool(object,object) pop
		ldloc V_15 box valuetype MyStruct631`2<class BaseClass1,class BaseClass0>
		ldloc V_15 box valuetype MyStruct631`2<class BaseClass1,class BaseClass0>
		ldvirtftn instance int32 valuetype MyStruct631`2<class BaseClass1,class BaseClass0>::GetHashCode() calli default int32(object) pop
		ldloc V_15 box valuetype MyStruct631`2<class BaseClass1,class BaseClass0>
		ldloc V_15 box valuetype MyStruct631`2<class BaseClass1,class BaseClass0>
		ldvirtftn instance string valuetype MyStruct631`2<class BaseClass1,class BaseClass0>::ToString() calli default string(object) pop
		ldloc V_15
		box valuetype MyStruct631`2<class BaseClass1,class BaseClass0>
		ldloc V_15
		box valuetype MyStruct631`2<class BaseClass1,class BaseClass0>
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(object)
		ldstr "MyStruct631::Method7.4618<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type valuetype MyStruct631`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_15
		box valuetype MyStruct631`2<class BaseClass1,class BaseClass0>
		ldloc V_15
		box valuetype MyStruct631`2<class BaseClass1,class BaseClass0>
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(object)
		ldstr "MyStruct631::Method7.MI.4620<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type valuetype MyStruct631`2<class BaseClass1,class BaseClass0>"
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
