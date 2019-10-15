// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern mscorlib { .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) .ver 4:0:0:0 }
.assembly extern TestFramework { .publickeytoken = ( B0 3F 5F 7F 11 D5 0A 3A )  }

//TYPES IN FORWARDER ASSEMBLIES:

//TEST ASSEMBLY:
.assembly Generated413 { .hash algorithm 0x00008004 }
.module Generated413.exe

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
.class public sequential sealed MyStruct463`2<T0, T1> 
		extends [mscorlib]System.ValueType
		implements class IBase2`2<class BaseClass0,!T0>, class IBase2`2<!T1,class BaseClass0> 
{
	.pack 0
	.size 1
	.method public hidebysig virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "MyStruct463::Method7.3694<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase2<class BaseClass0,T0>.Method7'<M0>() cil managed noinlining { 
		.override method instance string class IBase2`2<class BaseClass0,!T0>::Method7<[1]>()
		ldstr "MyStruct463::Method7.MI.3695<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot instance string ClassMethod910() cil managed noinlining { 
		ldstr "MyStruct463::ClassMethod910.3697()"
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
	.method static void M.MyStruct463.T.T<T0,T1,(valuetype MyStruct463`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 7
		.locals init (string[] actualResults)
		ldc.i4.s    2
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.MyStruct463.T.T<T0,T1,(valuetype MyStruct463`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 2
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. valuetype MyStruct463`2<!!T0,!!T1>
		callvirt     instance string class IBase2`2<class BaseClass0,!!T0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. valuetype MyStruct463`2<!!T0,!!T1>
		callvirt     instance string class IBase2`2<!!T1,class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.MyStruct463.A.T<T1,(valuetype MyStruct463`2<class BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 7
		.locals init (string[] actualResults)
		ldc.i4.s    2
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.MyStruct463.A.T<T1,(valuetype MyStruct463`2<class BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 2
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. valuetype MyStruct463`2<class BaseClass0,!!T1>
		callvirt     instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. valuetype MyStruct463`2<class BaseClass0,!!T1>
		callvirt     instance string class IBase2`2<!!T1,class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.MyStruct463.A.B<(valuetype MyStruct463`2<class BaseClass0,class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 7
		.locals init (string[] actualResults)
		ldc.i4.s    2
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.MyStruct463.A.B<(valuetype MyStruct463`2<class BaseClass0,class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 2
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. valuetype MyStruct463`2<class BaseClass0,class BaseClass1>
		callvirt     instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. valuetype MyStruct463`2<class BaseClass0,class BaseClass1>
		callvirt     instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.MyStruct463.B.T<T1,(valuetype MyStruct463`2<class BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 7
		.locals init (string[] actualResults)
		ldc.i4.s    2
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.MyStruct463.B.T<T1,(valuetype MyStruct463`2<class BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 2
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. valuetype MyStruct463`2<class BaseClass1,!!T1>
		callvirt     instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. valuetype MyStruct463`2<class BaseClass1,!!T1>
		callvirt     instance string class IBase2`2<!!T1,class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.MyStruct463.B.A<(valuetype MyStruct463`2<class BaseClass1,class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 7
		.locals init (string[] actualResults)
		ldc.i4.s    2
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.MyStruct463.B.A<(valuetype MyStruct463`2<class BaseClass1,class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 2
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. valuetype MyStruct463`2<class BaseClass1,class BaseClass0>
		callvirt     instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. valuetype MyStruct463`2<class BaseClass1,class BaseClass0>
		callvirt     instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.MyStruct463.B.B<(valuetype MyStruct463`2<class BaseClass1,class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 7
		.locals init (string[] actualResults)
		ldc.i4.s    2
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.MyStruct463.B.B<(valuetype MyStruct463`2<class BaseClass1,class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 2
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. valuetype MyStruct463`2<class BaseClass1,class BaseClass1>
		callvirt     instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. valuetype MyStruct463`2<class BaseClass1,class BaseClass1>
		callvirt     instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
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
		.locals init (valuetype MyStruct463`2<class BaseClass0,class BaseClass1> V_2)
		ldloca V_2
		initobj valuetype MyStruct463`2<class BaseClass0,class BaseClass1>
		ldloca V_2
		dup
		call instance string valuetype MyStruct463`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "MyStruct463::Method7.3694<System.Object>()"
		ldstr "valuetype MyStruct463`2<class BaseClass0,class BaseClass1> on type MyStruct463"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype MyStruct463`2<class BaseClass0,class BaseClass1>::ClassMethod910()
		ldstr "MyStruct463::ClassMethod910.3697()"
		ldstr "valuetype MyStruct463`2<class BaseClass0,class BaseClass1> on type MyStruct463"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup ldnull call instance bool valuetype MyStruct463`2<class BaseClass0,class BaseClass1>::Equals(object) pop
		dup call instance int32 valuetype MyStruct463`2<class BaseClass0,class BaseClass1>::GetHashCode() pop
		dup call instance string valuetype MyStruct463`2<class BaseClass0,class BaseClass1>::ToString() pop
		pop
		ldloc V_2
		box valuetype MyStruct463`2<class BaseClass0,class BaseClass1>
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "MyStruct463::Method7.MI.3695<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type valuetype MyStruct463`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc V_2
		box valuetype MyStruct463`2<class BaseClass0,class BaseClass1>
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "MyStruct463::Method7.3694<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type valuetype MyStruct463`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc V_2
		box valuetype MyStruct463`2<class BaseClass0,class BaseClass1>
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "MyStruct463::Method7.MI.3695<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type valuetype MyStruct463`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc V_2
		box valuetype MyStruct463`2<class BaseClass0,class BaseClass1>
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "MyStruct463::Method7.3694<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type valuetype MyStruct463`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		.locals init (valuetype MyStruct463`2<class BaseClass1,class BaseClass0> V_3)
		ldloca V_3
		initobj valuetype MyStruct463`2<class BaseClass1,class BaseClass0>
		ldloca V_3
		dup
		call instance string valuetype MyStruct463`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "MyStruct463::Method7.3694<System.Object>()"
		ldstr "valuetype MyStruct463`2<class BaseClass1,class BaseClass0> on type MyStruct463"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype MyStruct463`2<class BaseClass1,class BaseClass0>::ClassMethod910()
		ldstr "MyStruct463::ClassMethod910.3697()"
		ldstr "valuetype MyStruct463`2<class BaseClass1,class BaseClass0> on type MyStruct463"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup ldnull call instance bool valuetype MyStruct463`2<class BaseClass1,class BaseClass0>::Equals(object) pop
		dup call instance int32 valuetype MyStruct463`2<class BaseClass1,class BaseClass0>::GetHashCode() pop
		dup call instance string valuetype MyStruct463`2<class BaseClass1,class BaseClass0>::ToString() pop
		pop
		ldloc V_3
		box valuetype MyStruct463`2<class BaseClass1,class BaseClass0>
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "MyStruct463::Method7.MI.3695<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type valuetype MyStruct463`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc V_3
		box valuetype MyStruct463`2<class BaseClass1,class BaseClass0>
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "MyStruct463::Method7.3694<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type valuetype MyStruct463`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		.locals init (valuetype MyStruct463`2<class BaseClass1,class BaseClass1> V_4)
		ldloca V_4
		initobj valuetype MyStruct463`2<class BaseClass1,class BaseClass1>
		ldloca V_4
		dup
		call instance string valuetype MyStruct463`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "MyStruct463::Method7.3694<System.Object>()"
		ldstr "valuetype MyStruct463`2<class BaseClass1,class BaseClass1> on type MyStruct463"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype MyStruct463`2<class BaseClass1,class BaseClass1>::ClassMethod910()
		ldstr "MyStruct463::ClassMethod910.3697()"
		ldstr "valuetype MyStruct463`2<class BaseClass1,class BaseClass1> on type MyStruct463"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup ldnull call instance bool valuetype MyStruct463`2<class BaseClass1,class BaseClass1>::Equals(object) pop
		dup call instance int32 valuetype MyStruct463`2<class BaseClass1,class BaseClass1>::GetHashCode() pop
		dup call instance string valuetype MyStruct463`2<class BaseClass1,class BaseClass1>::ToString() pop
		pop
		ldloc V_4
		box valuetype MyStruct463`2<class BaseClass1,class BaseClass1>
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "MyStruct463::Method7.MI.3695<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type valuetype MyStruct463`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc V_4
		box valuetype MyStruct463`2<class BaseClass1,class BaseClass1>
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "MyStruct463::Method7.3694<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type valuetype MyStruct463`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc V_4
		box valuetype MyStruct463`2<class BaseClass1,class BaseClass1>
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "MyStruct463::Method7.3694<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type valuetype MyStruct463`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc V_4
		box valuetype MyStruct463`2<class BaseClass1,class BaseClass1>
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "MyStruct463::Method7.3694<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type valuetype MyStruct463`2<class BaseClass1,class BaseClass1>"
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
		.locals init (valuetype MyStruct463`2<class BaseClass0,class BaseClass1> V_6)
		ldloca V_6
		initobj valuetype MyStruct463`2<class BaseClass0,class BaseClass1>
		.try {  ldloc V_6
				ldstr "MyStruct463::Method7.MI.3695<System.Object>()#"
				call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass0,valuetype MyStruct463`2<class BaseClass0,class BaseClass1>>(!!2,string) leave.s LV0
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV0} LV0:
		.try {  ldloc V_6
				ldstr "MyStruct463::Method7.MI.3695<System.Object>()#"
				call void Framework::M.IBase2.A.T<class BaseClass0,valuetype MyStruct463`2<class BaseClass0,class BaseClass1>>(!!1,string) leave.s LV1
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV1} LV1:
		.try {  ldloc V_6
				ldstr "MyStruct463::Method7.MI.3695<System.Object>()#"
				call void Framework::M.IBase2.A.A<valuetype MyStruct463`2<class BaseClass0,class BaseClass1>>(!!0,string) leave.s LV2
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV2} LV2:
		.try {  ldloc V_6
				ldstr "MyStruct463::Method7.3694<System.Object>()#"
				call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass0,valuetype MyStruct463`2<class BaseClass0,class BaseClass1>>(!!2,string) leave.s LV3
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV3} LV3:
		.try {  ldloc V_6
				ldstr "MyStruct463::Method7.3694<System.Object>()#"
				call void Framework::M.IBase2.B.T<class BaseClass0,valuetype MyStruct463`2<class BaseClass0,class BaseClass1>>(!!1,string) leave.s LV4
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV4} LV4:
		.try {  ldloc V_6
				ldstr "MyStruct463::Method7.3694<System.Object>()#"
				call void Framework::M.IBase2.B.A<valuetype MyStruct463`2<class BaseClass0,class BaseClass1>>(!!0,string) leave.s LV5
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV5} LV5:
		.try {  ldloc V_6
				ldstr "MyStruct463::Method7.MI.3695<System.Object>()#"
				call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,valuetype MyStruct463`2<class BaseClass0,class BaseClass1>>(!!2,string) leave.s LV6
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV6} LV6:
		.try {  ldloc V_6
				ldstr "MyStruct463::Method7.MI.3695<System.Object>()#"
				call void Framework::M.IBase2.A.T<class BaseClass1,valuetype MyStruct463`2<class BaseClass0,class BaseClass1>>(!!1,string) leave.s LV7
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV7} LV7:
		.try {  ldloc V_6
				ldstr "MyStruct463::Method7.MI.3695<System.Object>()#"
				call void Framework::M.IBase2.A.B<valuetype MyStruct463`2<class BaseClass0,class BaseClass1>>(!!0,string) leave.s LV8
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV8} LV8:
		.try {  ldloc V_6
				ldstr "MyStruct463::Method7.3694<System.Object>()#"
				call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass1,valuetype MyStruct463`2<class BaseClass0,class BaseClass1>>(!!2,string) leave.s LV9
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV9} LV9:
		.try {  ldloc V_6
				ldstr "MyStruct463::Method7.3694<System.Object>()#"
				call void Framework::M.IBase2.B.T<class BaseClass1,valuetype MyStruct463`2<class BaseClass0,class BaseClass1>>(!!1,string) leave.s LV10
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV10} LV10:
		.try {  ldloc V_6
				ldstr "MyStruct463::Method7.3694<System.Object>()#"
				call void Framework::M.IBase2.B.B<valuetype MyStruct463`2<class BaseClass0,class BaseClass1>>(!!0,string) leave.s LV11
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV11} LV11:
		.locals init (valuetype MyStruct463`2<class BaseClass1,class BaseClass0> V_7)
		ldloca V_7
		initobj valuetype MyStruct463`2<class BaseClass1,class BaseClass0>
		.try {  ldloc V_7
				ldstr "MyStruct463::Method7.MI.3695<System.Object>()#"
				call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,valuetype MyStruct463`2<class BaseClass1,class BaseClass0>>(!!2,string) leave.s LV12
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV12} LV12:
		.try {  ldloc V_7
				ldstr "MyStruct463::Method7.MI.3695<System.Object>()#"
				call void Framework::M.IBase2.A.T<class BaseClass1,valuetype MyStruct463`2<class BaseClass1,class BaseClass0>>(!!1,string) leave.s LV13
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV13} LV13:
		.try {  ldloc V_7
				ldstr "MyStruct463::Method7.MI.3695<System.Object>()#"
				call void Framework::M.IBase2.A.B<valuetype MyStruct463`2<class BaseClass1,class BaseClass0>>(!!0,string) leave.s LV14
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV14} LV14:
		.try {  ldloc V_7
				ldstr "MyStruct463::Method7.3694<System.Object>()#"
				call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass0,valuetype MyStruct463`2<class BaseClass1,class BaseClass0>>(!!2,string) leave.s LV15
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV15} LV15:
		.try {  ldloc V_7
				ldstr "MyStruct463::Method7.3694<System.Object>()#"
				call void Framework::M.IBase2.A.T<class BaseClass0,valuetype MyStruct463`2<class BaseClass1,class BaseClass0>>(!!1,string) leave.s LV16
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV16} LV16:
		.try {  ldloc V_7
				ldstr "MyStruct463::Method7.3694<System.Object>()#"
				call void Framework::M.IBase2.A.A<valuetype MyStruct463`2<class BaseClass1,class BaseClass0>>(!!0,string) leave.s LV17
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV17} LV17:
		.locals init (valuetype MyStruct463`2<class BaseClass1,class BaseClass1> V_8)
		ldloca V_8
		initobj valuetype MyStruct463`2<class BaseClass1,class BaseClass1>
		.try {  ldloc V_8
				ldstr "MyStruct463::Method7.MI.3695<System.Object>()#"
				call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,valuetype MyStruct463`2<class BaseClass1,class BaseClass1>>(!!2,string) leave.s LV18
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV18} LV18:
		.try {  ldloc V_8
				ldstr "MyStruct463::Method7.MI.3695<System.Object>()#"
				call void Framework::M.IBase2.A.T<class BaseClass1,valuetype MyStruct463`2<class BaseClass1,class BaseClass1>>(!!1,string) leave.s LV19
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV19} LV19:
		.try {  ldloc V_8
				ldstr "MyStruct463::Method7.MI.3695<System.Object>()#"
				call void Framework::M.IBase2.A.B<valuetype MyStruct463`2<class BaseClass1,class BaseClass1>>(!!0,string) leave.s LV20
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV20} LV20:
		.try {  ldloc V_8
				ldstr "MyStruct463::Method7.3694<System.Object>()#"
				call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass0,valuetype MyStruct463`2<class BaseClass1,class BaseClass1>>(!!2,string) leave.s LV21
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV21} LV21:
		.try {  ldloc V_8
				ldstr "MyStruct463::Method7.3694<System.Object>()#"
				call void Framework::M.IBase2.B.T<class BaseClass0,valuetype MyStruct463`2<class BaseClass1,class BaseClass1>>(!!1,string) leave.s LV22
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV22} LV22:
		.try {  ldloc V_8
				ldstr "MyStruct463::Method7.3694<System.Object>()#"
				call void Framework::M.IBase2.B.A<valuetype MyStruct463`2<class BaseClass1,class BaseClass1>>(!!0,string) leave.s LV23
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV23} LV23:
		.try {  ldloc V_8
				ldstr "MyStruct463::Method7.3694<System.Object>()#"
				call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass0,valuetype MyStruct463`2<class BaseClass1,class BaseClass1>>(!!2,string) leave.s LV24
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV24} LV24:
		.try {  ldloc V_8
				ldstr "MyStruct463::Method7.3694<System.Object>()#"
				call void Framework::M.IBase2.A.T<class BaseClass0,valuetype MyStruct463`2<class BaseClass1,class BaseClass1>>(!!1,string) leave.s LV25
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV25} LV25:
		.try {  ldloc V_8
				ldstr "MyStruct463::Method7.3694<System.Object>()#"
				call void Framework::M.IBase2.A.A<valuetype MyStruct463`2<class BaseClass1,class BaseClass1>>(!!0,string) leave.s LV26
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV26} LV26:
		.try {  ldloc V_8
				ldstr "MyStruct463::Method7.3694<System.Object>()#"
				call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass1,valuetype MyStruct463`2<class BaseClass1,class BaseClass1>>(!!2,string) leave.s LV27
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV27} LV27:
		.try {  ldloc V_8
				ldstr "MyStruct463::Method7.3694<System.Object>()#"
				call void Framework::M.IBase2.B.T<class BaseClass1,valuetype MyStruct463`2<class BaseClass1,class BaseClass1>>(!!1,string) leave.s LV28
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV28} LV28:
		.try {  ldloc V_8
				ldstr "MyStruct463::Method7.3694<System.Object>()#"
				call void Framework::M.IBase2.B.B<valuetype MyStruct463`2<class BaseClass1,class BaseClass1>>(!!0,string) leave.s LV29
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV29} LV29:
		ldstr "========================================================================\n\n"
		call void [mscorlib]System.Console::WriteLine(string)
		ret
	}
	.method public hidebysig static void StructConstrainedInterfaceCallsTest() cil managed
	{
		.maxstack  10
		ldstr "===================== Struct Constrained Interface Calls Test ====================="
		call void [mscorlib]System.Console::WriteLine(string)
		.locals init (valuetype MyStruct463`2<class BaseClass0,class BaseClass1> V_10)
		ldloca V_10
		initobj valuetype MyStruct463`2<class BaseClass0,class BaseClass1>
		.try {  ldloc V_10
				ldstr "MyStruct463::Method7.MI.3695<System.Object>()#" +
				      "MyStruct463::Method7.3694<System.Object>()#"
				call void Framework::M.MyStruct463.T.T<class BaseClass0,class BaseClass1,valuetype MyStruct463`2<class BaseClass0,class BaseClass1>>(!!2,string) leave.s LV0
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV0} LV0:
		.try {  ldloc V_10
				ldstr "MyStruct463::Method7.MI.3695<System.Object>()#" +
				      "MyStruct463::Method7.3694<System.Object>()#"
				call void Framework::M.MyStruct463.A.T<class BaseClass1,valuetype MyStruct463`2<class BaseClass0,class BaseClass1>>(!!1,string) leave.s LV1
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV1} LV1:
		.try {  ldloc V_10
				ldstr "MyStruct463::Method7.MI.3695<System.Object>()#" +
				      "MyStruct463::Method7.3694<System.Object>()#"
				call void Framework::M.MyStruct463.A.B<valuetype MyStruct463`2<class BaseClass0,class BaseClass1>>(!!0,string) leave.s LV2
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV2} LV2:
		.locals init (valuetype MyStruct463`2<class BaseClass1,class BaseClass0> V_11)
		ldloca V_11
		initobj valuetype MyStruct463`2<class BaseClass1,class BaseClass0>
		.try {  ldloc V_11
				ldstr "MyStruct463::Method7.MI.3695<System.Object>()#" +
				      "MyStruct463::Method7.3694<System.Object>()#"
				call void Framework::M.MyStruct463.T.T<class BaseClass1,class BaseClass0,valuetype MyStruct463`2<class BaseClass1,class BaseClass0>>(!!2,string) leave.s LV3
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV3} LV3:
		.try {  ldloc V_11
				ldstr "MyStruct463::Method7.MI.3695<System.Object>()#" +
				      "MyStruct463::Method7.3694<System.Object>()#"
				call void Framework::M.MyStruct463.B.T<class BaseClass0,valuetype MyStruct463`2<class BaseClass1,class BaseClass0>>(!!1,string) leave.s LV4
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV4} LV4:
		.try {  ldloc V_11
				ldstr "MyStruct463::Method7.MI.3695<System.Object>()#" +
				      "MyStruct463::Method7.3694<System.Object>()#"
				call void Framework::M.MyStruct463.B.A<valuetype MyStruct463`2<class BaseClass1,class BaseClass0>>(!!0,string) leave.s LV5
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV5} LV5:
		.locals init (valuetype MyStruct463`2<class BaseClass1,class BaseClass1> V_12)
		ldloca V_12
		initobj valuetype MyStruct463`2<class BaseClass1,class BaseClass1>
		.try {  ldloc V_12
				ldstr "MyStruct463::Method7.MI.3695<System.Object>()#" +
				      "MyStruct463::Method7.3694<System.Object>()#"
				call void Framework::M.MyStruct463.T.T<class BaseClass1,class BaseClass1,valuetype MyStruct463`2<class BaseClass1,class BaseClass1>>(!!2,string) leave.s LV6
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV6} LV6:
		.try {  ldloc V_12
				ldstr "MyStruct463::Method7.MI.3695<System.Object>()#" +
				      "MyStruct463::Method7.3694<System.Object>()#"
				call void Framework::M.MyStruct463.B.T<class BaseClass1,valuetype MyStruct463`2<class BaseClass1,class BaseClass1>>(!!1,string) leave.s LV7
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV7} LV7:
		.try {  ldloc V_12
				ldstr "MyStruct463::Method7.MI.3695<System.Object>()#" +
				      "MyStruct463::Method7.3694<System.Object>()#"
				call void Framework::M.MyStruct463.B.B<valuetype MyStruct463`2<class BaseClass1,class BaseClass1>>(!!0,string) leave.s LV8
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV8} LV8:
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
		.locals init (valuetype MyStruct463`2<class BaseClass0,class BaseClass1> V_14)
		ldloca V_14
		initobj valuetype MyStruct463`2<class BaseClass0,class BaseClass1>
		ldloc V_14
		box valuetype MyStruct463`2<class BaseClass0,class BaseClass1>
		ldloc V_14
		box valuetype MyStruct463`2<class BaseClass0,class BaseClass1>
		ldvirtftn instance string valuetype MyStruct463`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(object)
		ldstr "MyStruct463::Method7.3694<System.Object>()"
		ldstr "valuetype MyStruct463`2<class BaseClass0,class BaseClass1> on type valuetype MyStruct463`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_14
		box valuetype MyStruct463`2<class BaseClass0,class BaseClass1>
		ldloc V_14
		box valuetype MyStruct463`2<class BaseClass0,class BaseClass1>
		ldvirtftn instance string valuetype MyStruct463`2<class BaseClass0,class BaseClass1>::ClassMethod910()
		calli default string(object)
		ldstr "MyStruct463::ClassMethod910.3697()"
		ldstr "valuetype MyStruct463`2<class BaseClass0,class BaseClass1> on type valuetype MyStruct463`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_14 box valuetype MyStruct463`2<class BaseClass0,class BaseClass1> ldnull 
		ldloc V_14 box valuetype MyStruct463`2<class BaseClass0,class BaseClass1>
		ldvirtftn instance bool valuetype MyStruct463`2<class BaseClass0,class BaseClass1>::Equals(object) calli default bool(object,object) pop
		ldloc V_14 box valuetype MyStruct463`2<class BaseClass0,class BaseClass1>
		ldloc V_14 box valuetype MyStruct463`2<class BaseClass0,class BaseClass1>
		ldvirtftn instance int32 valuetype MyStruct463`2<class BaseClass0,class BaseClass1>::GetHashCode() calli default int32(object) pop
		ldloc V_14 box valuetype MyStruct463`2<class BaseClass0,class BaseClass1>
		ldloc V_14 box valuetype MyStruct463`2<class BaseClass0,class BaseClass1>
		ldvirtftn instance string valuetype MyStruct463`2<class BaseClass0,class BaseClass1>::ToString() calli default string(object) pop
		ldloc V_14
		box valuetype MyStruct463`2<class BaseClass0,class BaseClass1>
		ldloc V_14
		box valuetype MyStruct463`2<class BaseClass0,class BaseClass1>
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(object)
		ldstr "MyStruct463::Method7.MI.3695<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type valuetype MyStruct463`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_14
		box valuetype MyStruct463`2<class BaseClass0,class BaseClass1>
		ldloc V_14
		box valuetype MyStruct463`2<class BaseClass0,class BaseClass1>
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(object)
		ldstr "MyStruct463::Method7.3694<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type valuetype MyStruct463`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_14
		box valuetype MyStruct463`2<class BaseClass0,class BaseClass1>
		ldloc V_14
		box valuetype MyStruct463`2<class BaseClass0,class BaseClass1>
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(object)
		ldstr "MyStruct463::Method7.MI.3695<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type valuetype MyStruct463`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_14
		box valuetype MyStruct463`2<class BaseClass0,class BaseClass1>
		ldloc V_14
		box valuetype MyStruct463`2<class BaseClass0,class BaseClass1>
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(object)
		ldstr "MyStruct463::Method7.3694<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type valuetype MyStruct463`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		.locals init (valuetype MyStruct463`2<class BaseClass1,class BaseClass0> V_15)
		ldloca V_15
		initobj valuetype MyStruct463`2<class BaseClass1,class BaseClass0>
		ldloc V_15
		box valuetype MyStruct463`2<class BaseClass1,class BaseClass0>
		ldloc V_15
		box valuetype MyStruct463`2<class BaseClass1,class BaseClass0>
		ldvirtftn instance string valuetype MyStruct463`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(object)
		ldstr "MyStruct463::Method7.3694<System.Object>()"
		ldstr "valuetype MyStruct463`2<class BaseClass1,class BaseClass0> on type valuetype MyStruct463`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_15
		box valuetype MyStruct463`2<class BaseClass1,class BaseClass0>
		ldloc V_15
		box valuetype MyStruct463`2<class BaseClass1,class BaseClass0>
		ldvirtftn instance string valuetype MyStruct463`2<class BaseClass1,class BaseClass0>::ClassMethod910()
		calli default string(object)
		ldstr "MyStruct463::ClassMethod910.3697()"
		ldstr "valuetype MyStruct463`2<class BaseClass1,class BaseClass0> on type valuetype MyStruct463`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_15 box valuetype MyStruct463`2<class BaseClass1,class BaseClass0> ldnull 
		ldloc V_15 box valuetype MyStruct463`2<class BaseClass1,class BaseClass0>
		ldvirtftn instance bool valuetype MyStruct463`2<class BaseClass1,class BaseClass0>::Equals(object) calli default bool(object,object) pop
		ldloc V_15 box valuetype MyStruct463`2<class BaseClass1,class BaseClass0>
		ldloc V_15 box valuetype MyStruct463`2<class BaseClass1,class BaseClass0>
		ldvirtftn instance int32 valuetype MyStruct463`2<class BaseClass1,class BaseClass0>::GetHashCode() calli default int32(object) pop
		ldloc V_15 box valuetype MyStruct463`2<class BaseClass1,class BaseClass0>
		ldloc V_15 box valuetype MyStruct463`2<class BaseClass1,class BaseClass0>
		ldvirtftn instance string valuetype MyStruct463`2<class BaseClass1,class BaseClass0>::ToString() calli default string(object) pop
		ldloc V_15
		box valuetype MyStruct463`2<class BaseClass1,class BaseClass0>
		ldloc V_15
		box valuetype MyStruct463`2<class BaseClass1,class BaseClass0>
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(object)
		ldstr "MyStruct463::Method7.MI.3695<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type valuetype MyStruct463`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_15
		box valuetype MyStruct463`2<class BaseClass1,class BaseClass0>
		ldloc V_15
		box valuetype MyStruct463`2<class BaseClass1,class BaseClass0>
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(object)
		ldstr "MyStruct463::Method7.3694<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type valuetype MyStruct463`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		.locals init (valuetype MyStruct463`2<class BaseClass1,class BaseClass1> V_16)
		ldloca V_16
		initobj valuetype MyStruct463`2<class BaseClass1,class BaseClass1>
		ldloc V_16
		box valuetype MyStruct463`2<class BaseClass1,class BaseClass1>
		ldloc V_16
		box valuetype MyStruct463`2<class BaseClass1,class BaseClass1>
		ldvirtftn instance string valuetype MyStruct463`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(object)
		ldstr "MyStruct463::Method7.3694<System.Object>()"
		ldstr "valuetype MyStruct463`2<class BaseClass1,class BaseClass1> on type valuetype MyStruct463`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_16
		box valuetype MyStruct463`2<class BaseClass1,class BaseClass1>
		ldloc V_16
		box valuetype MyStruct463`2<class BaseClass1,class BaseClass1>
		ldvirtftn instance string valuetype MyStruct463`2<class BaseClass1,class BaseClass1>::ClassMethod910()
		calli default string(object)
		ldstr "MyStruct463::ClassMethod910.3697()"
		ldstr "valuetype MyStruct463`2<class BaseClass1,class BaseClass1> on type valuetype MyStruct463`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_16 box valuetype MyStruct463`2<class BaseClass1,class BaseClass1> ldnull 
		ldloc V_16 box valuetype MyStruct463`2<class BaseClass1,class BaseClass1>
		ldvirtftn instance bool valuetype MyStruct463`2<class BaseClass1,class BaseClass1>::Equals(object) calli default bool(object,object) pop
		ldloc V_16 box valuetype MyStruct463`2<class BaseClass1,class BaseClass1>
		ldloc V_16 box valuetype MyStruct463`2<class BaseClass1,class BaseClass1>
		ldvirtftn instance int32 valuetype MyStruct463`2<class BaseClass1,class BaseClass1>::GetHashCode() calli default int32(object) pop
		ldloc V_16 box valuetype MyStruct463`2<class BaseClass1,class BaseClass1>
		ldloc V_16 box valuetype MyStruct463`2<class BaseClass1,class BaseClass1>
		ldvirtftn instance string valuetype MyStruct463`2<class BaseClass1,class BaseClass1>::ToString() calli default string(object) pop
		ldloc V_16
		box valuetype MyStruct463`2<class BaseClass1,class BaseClass1>
		ldloc V_16
		box valuetype MyStruct463`2<class BaseClass1,class BaseClass1>
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(object)
		ldstr "MyStruct463::Method7.MI.3695<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type valuetype MyStruct463`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_16
		box valuetype MyStruct463`2<class BaseClass1,class BaseClass1>
		ldloc V_16
		box valuetype MyStruct463`2<class BaseClass1,class BaseClass1>
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(object)
		ldstr "MyStruct463::Method7.3694<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type valuetype MyStruct463`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_16
		box valuetype MyStruct463`2<class BaseClass1,class BaseClass1>
		ldloc V_16
		box valuetype MyStruct463`2<class BaseClass1,class BaseClass1>
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(object)
		ldstr "MyStruct463::Method7.3694<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type valuetype MyStruct463`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_16
		box valuetype MyStruct463`2<class BaseClass1,class BaseClass1>
		ldloc V_16
		box valuetype MyStruct463`2<class BaseClass1,class BaseClass1>
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(object)
		ldstr "MyStruct463::Method7.3694<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type valuetype MyStruct463`2<class BaseClass1,class BaseClass1>"
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
