// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern mscorlib { .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) .ver 4:0:0:0 }
.assembly extern TestFramework { .publickeytoken = ( B0 3F 5F 7F 11 D5 0A 3A )  }

//TYPES IN FORWARDER ASSEMBLIES:

//TEST ASSEMBLY:
.assembly Generated227 { .hash algorithm 0x00008004 }
.module Generated227.exe

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
.class public sequential sealed MyStruct277`2<T0, T1> 
		extends [mscorlib]System.ValueType
		implements class IBase1`1<!T1>, class IBase1`1<!T0> 
{
	.pack 0
	.size 1
	.method public hidebysig newslot virtual instance string Method4() cil managed noinlining { 
		ldstr "MyStruct277::Method4.2192()"
		ret
	}
	.method public hidebysig newslot virtual instance string Method5() cil managed noinlining { 
		ldstr "MyStruct277::Method5.2193()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<T1>.Method5'() cil managed noinlining { 
		.override method instance string class IBase1`1<!T1>::Method5()
		ldstr "MyStruct277::Method5.MI.2194()"
		ret
	}
	.method public hidebysig newslot virtual instance string Method6<M0>() cil managed noinlining { 
		ldstr "MyStruct277::Method6.2195<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<T0>.Method5'() cil managed noinlining { 
		.override method instance string class IBase1`1<!T0>::Method5()
		ldstr "MyStruct277::Method5.MI.2198()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<T0>.Method6'<M0>() cil managed noinlining { 
		.override method instance string class IBase1`1<!T0>::Method6<[1]>()
		ldstr "MyStruct277::Method6.MI.2200<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot instance string ClassMethod555<M0>() cil managed noinlining { 
		ldstr "MyStruct277::ClassMethod555.2201<"
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
.class interface public abstract IBase1`1<+T0> 
{
	.method public hidebysig newslot abstract virtual instance string Method4() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method5() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method6<M0>() cil managed { }
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
	.method static void M.IBase1.T<T0,(class IBase1`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase1.T<T0,(class IBase1`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class IBase1`1<!!T0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class IBase1`1<!!T0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class IBase1`1<!!T0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase1.A<(class IBase1`1<class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase1.A<(class IBase1`1<class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class IBase1`1<class BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class IBase1`1<class BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class IBase1`1<class BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase1.B<(class IBase1`1<class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase1.B<(class IBase1`1<class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class IBase1`1<class BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class IBase1`1<class BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class IBase1`1<class BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.MyStruct277.T.T<T0,T1,(valuetype MyStruct277`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.MyStruct277.T.T<T0,T1,(valuetype MyStruct277`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. valuetype MyStruct277`2<!!T0,!!T1>
		callvirt     instance string class IBase1`1<!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. valuetype MyStruct277`2<!!T0,!!T1>
		callvirt     instance string class IBase1`1<!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. valuetype MyStruct277`2<!!T0,!!T1>
		callvirt     instance string class IBase1`1<!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. valuetype MyStruct277`2<!!T0,!!T1>
		callvirt     instance string class IBase1`1<!!T0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. valuetype MyStruct277`2<!!T0,!!T1>
		callvirt     instance string class IBase1`1<!!T0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. valuetype MyStruct277`2<!!T0,!!T1>
		callvirt     instance string class IBase1`1<!!T0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.MyStruct277.A.T<T1,(valuetype MyStruct277`2<class BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.MyStruct277.A.T<T1,(valuetype MyStruct277`2<class BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. valuetype MyStruct277`2<class BaseClass0,!!T1>
		callvirt     instance string class IBase1`1<!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. valuetype MyStruct277`2<class BaseClass0,!!T1>
		callvirt     instance string class IBase1`1<!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. valuetype MyStruct277`2<class BaseClass0,!!T1>
		callvirt     instance string class IBase1`1<!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. valuetype MyStruct277`2<class BaseClass0,!!T1>
		callvirt     instance string class IBase1`1<class BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. valuetype MyStruct277`2<class BaseClass0,!!T1>
		callvirt     instance string class IBase1`1<class BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. valuetype MyStruct277`2<class BaseClass0,!!T1>
		callvirt     instance string class IBase1`1<class BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.MyStruct277.A.B<(valuetype MyStruct277`2<class BaseClass0,class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.MyStruct277.A.B<(valuetype MyStruct277`2<class BaseClass0,class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. valuetype MyStruct277`2<class BaseClass0,class BaseClass1>
		callvirt     instance string class IBase1`1<class BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. valuetype MyStruct277`2<class BaseClass0,class BaseClass1>
		callvirt     instance string class IBase1`1<class BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. valuetype MyStruct277`2<class BaseClass0,class BaseClass1>
		callvirt     instance string class IBase1`1<class BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. valuetype MyStruct277`2<class BaseClass0,class BaseClass1>
		callvirt     instance string class IBase1`1<class BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. valuetype MyStruct277`2<class BaseClass0,class BaseClass1>
		callvirt     instance string class IBase1`1<class BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. valuetype MyStruct277`2<class BaseClass0,class BaseClass1>
		callvirt     instance string class IBase1`1<class BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.MyStruct277.B.T<T1,(valuetype MyStruct277`2<class BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.MyStruct277.B.T<T1,(valuetype MyStruct277`2<class BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. valuetype MyStruct277`2<class BaseClass1,!!T1>
		callvirt     instance string class IBase1`1<!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. valuetype MyStruct277`2<class BaseClass1,!!T1>
		callvirt     instance string class IBase1`1<!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. valuetype MyStruct277`2<class BaseClass1,!!T1>
		callvirt     instance string class IBase1`1<!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. valuetype MyStruct277`2<class BaseClass1,!!T1>
		callvirt     instance string class IBase1`1<class BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. valuetype MyStruct277`2<class BaseClass1,!!T1>
		callvirt     instance string class IBase1`1<class BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. valuetype MyStruct277`2<class BaseClass1,!!T1>
		callvirt     instance string class IBase1`1<class BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.MyStruct277.B.A<(valuetype MyStruct277`2<class BaseClass1,class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.MyStruct277.B.A<(valuetype MyStruct277`2<class BaseClass1,class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. valuetype MyStruct277`2<class BaseClass1,class BaseClass0>
		callvirt     instance string class IBase1`1<class BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. valuetype MyStruct277`2<class BaseClass1,class BaseClass0>
		callvirt     instance string class IBase1`1<class BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. valuetype MyStruct277`2<class BaseClass1,class BaseClass0>
		callvirt     instance string class IBase1`1<class BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. valuetype MyStruct277`2<class BaseClass1,class BaseClass0>
		callvirt     instance string class IBase1`1<class BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. valuetype MyStruct277`2<class BaseClass1,class BaseClass0>
		callvirt     instance string class IBase1`1<class BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. valuetype MyStruct277`2<class BaseClass1,class BaseClass0>
		callvirt     instance string class IBase1`1<class BaseClass1>::Method6<object>()
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
		.locals init (valuetype MyStruct277`2<class BaseClass0,class BaseClass1> V_2)
		ldloca V_2
		initobj valuetype MyStruct277`2<class BaseClass0,class BaseClass1>
		ldloca V_2
		dup
		call instance string valuetype MyStruct277`2<class BaseClass0,class BaseClass1>::Method4()
		ldstr "MyStruct277::Method4.2192()"
		ldstr "valuetype MyStruct277`2<class BaseClass0,class BaseClass1> on type MyStruct277"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype MyStruct277`2<class BaseClass0,class BaseClass1>::Method5()
		ldstr "MyStruct277::Method5.2193()"
		ldstr "valuetype MyStruct277`2<class BaseClass0,class BaseClass1> on type MyStruct277"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype MyStruct277`2<class BaseClass0,class BaseClass1>::Method6<object>()
		ldstr "MyStruct277::Method6.2195<System.Object>()"
		ldstr "valuetype MyStruct277`2<class BaseClass0,class BaseClass1> on type MyStruct277"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype MyStruct277`2<class BaseClass0,class BaseClass1>::ClassMethod555<object>()
		ldstr "MyStruct277::ClassMethod555.2201<System.Object>()"
		ldstr "valuetype MyStruct277`2<class BaseClass0,class BaseClass1> on type MyStruct277"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup ldnull call instance bool valuetype MyStruct277`2<class BaseClass0,class BaseClass1>::Equals(object) pop
		dup call instance int32 valuetype MyStruct277`2<class BaseClass0,class BaseClass1>::GetHashCode() pop
		dup call instance string valuetype MyStruct277`2<class BaseClass0,class BaseClass1>::ToString() pop
		pop
		ldloc V_2
		box valuetype MyStruct277`2<class BaseClass0,class BaseClass1>
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method4()
		ldstr "MyStruct277::Method4.2192()"
		ldstr "class IBase1`1<class BaseClass1> on type valuetype MyStruct277`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method5()
		ldstr "MyStruct277::Method5.MI.2194()"
		ldstr "class IBase1`1<class BaseClass1> on type valuetype MyStruct277`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method6<object>()
		ldstr "MyStruct277::Method6.2195<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type valuetype MyStruct277`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc V_2
		box valuetype MyStruct277`2<class BaseClass0,class BaseClass1>
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "MyStruct277::Method4.2192()"
		ldstr "class IBase1`1<class BaseClass0> on type valuetype MyStruct277`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "MyStruct277::Method5.MI.2198()"
		ldstr "class IBase1`1<class BaseClass0> on type valuetype MyStruct277`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "MyStruct277::Method6.MI.2200<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type valuetype MyStruct277`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		.locals init (valuetype MyStruct277`2<class BaseClass1,class BaseClass0> V_3)
		ldloca V_3
		initobj valuetype MyStruct277`2<class BaseClass1,class BaseClass0>
		ldloca V_3
		dup
		call instance string valuetype MyStruct277`2<class BaseClass1,class BaseClass0>::Method4()
		ldstr "MyStruct277::Method4.2192()"
		ldstr "valuetype MyStruct277`2<class BaseClass1,class BaseClass0> on type MyStruct277"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype MyStruct277`2<class BaseClass1,class BaseClass0>::Method5()
		ldstr "MyStruct277::Method5.2193()"
		ldstr "valuetype MyStruct277`2<class BaseClass1,class BaseClass0> on type MyStruct277"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype MyStruct277`2<class BaseClass1,class BaseClass0>::Method6<object>()
		ldstr "MyStruct277::Method6.2195<System.Object>()"
		ldstr "valuetype MyStruct277`2<class BaseClass1,class BaseClass0> on type MyStruct277"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype MyStruct277`2<class BaseClass1,class BaseClass0>::ClassMethod555<object>()
		ldstr "MyStruct277::ClassMethod555.2201<System.Object>()"
		ldstr "valuetype MyStruct277`2<class BaseClass1,class BaseClass0> on type MyStruct277"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup ldnull call instance bool valuetype MyStruct277`2<class BaseClass1,class BaseClass0>::Equals(object) pop
		dup call instance int32 valuetype MyStruct277`2<class BaseClass1,class BaseClass0>::GetHashCode() pop
		dup call instance string valuetype MyStruct277`2<class BaseClass1,class BaseClass0>::ToString() pop
		pop
		ldloc V_3
		box valuetype MyStruct277`2<class BaseClass1,class BaseClass0>
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "MyStruct277::Method4.2192()"
		ldstr "class IBase1`1<class BaseClass0> on type valuetype MyStruct277`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "MyStruct277::Method5.MI.2194()"
		ldstr "class IBase1`1<class BaseClass0> on type valuetype MyStruct277`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "MyStruct277::Method6.2195<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type valuetype MyStruct277`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc V_3
		box valuetype MyStruct277`2<class BaseClass1,class BaseClass0>
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method4()
		ldstr "MyStruct277::Method4.2192()"
		ldstr "class IBase1`1<class BaseClass1> on type valuetype MyStruct277`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method5()
		ldstr "MyStruct277::Method5.MI.2198()"
		ldstr "class IBase1`1<class BaseClass1> on type valuetype MyStruct277`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method6<object>()
		ldstr "MyStruct277::Method6.MI.2200<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type valuetype MyStruct277`2<class BaseClass1,class BaseClass0>"
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
		.locals init (valuetype MyStruct277`2<class BaseClass0,class BaseClass1> V_6)
		ldloca V_6
		initobj valuetype MyStruct277`2<class BaseClass0,class BaseClass1>
		.try {  ldloc V_6
				ldstr "MyStruct277::Method4.2192()#MyStruct277::Method5.MI.2194()#MyStruct277::Method6.2195<System.Object>()#"
				call void Framework::M.IBase1.T<class BaseClass1,valuetype MyStruct277`2<class BaseClass0,class BaseClass1>>(!!1,string) leave.s LV0
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV0} LV0:
		.try {  ldloc V_6
				ldstr "MyStruct277::Method4.2192()#MyStruct277::Method5.MI.2194()#MyStruct277::Method6.2195<System.Object>()#"
				call void Framework::M.IBase1.B<valuetype MyStruct277`2<class BaseClass0,class BaseClass1>>(!!0,string) leave.s LV1
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV1} LV1:
		.try {  ldloc V_6
				ldstr "MyStruct277::Method4.2192()#MyStruct277::Method5.MI.2198()#MyStruct277::Method6.MI.2200<System.Object>()#"
				call void Framework::M.IBase1.T<class BaseClass0,valuetype MyStruct277`2<class BaseClass0,class BaseClass1>>(!!1,string) leave.s LV2
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV2} LV2:
		.try {  ldloc V_6
				ldstr "MyStruct277::Method4.2192()#MyStruct277::Method5.MI.2198()#MyStruct277::Method6.MI.2200<System.Object>()#"
				call void Framework::M.IBase1.A<valuetype MyStruct277`2<class BaseClass0,class BaseClass1>>(!!0,string) leave.s LV3
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV3} LV3:
		.locals init (valuetype MyStruct277`2<class BaseClass1,class BaseClass0> V_7)
		ldloca V_7
		initobj valuetype MyStruct277`2<class BaseClass1,class BaseClass0>
		.try {  ldloc V_7
				ldstr "MyStruct277::Method4.2192()#MyStruct277::Method5.MI.2194()#MyStruct277::Method6.2195<System.Object>()#"
				call void Framework::M.IBase1.T<class BaseClass0,valuetype MyStruct277`2<class BaseClass1,class BaseClass0>>(!!1,string) leave.s LV4
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV4} LV4:
		.try {  ldloc V_7
				ldstr "MyStruct277::Method4.2192()#MyStruct277::Method5.MI.2194()#MyStruct277::Method6.2195<System.Object>()#"
				call void Framework::M.IBase1.A<valuetype MyStruct277`2<class BaseClass1,class BaseClass0>>(!!0,string) leave.s LV5
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV5} LV5:
		.try {  ldloc V_7
				ldstr "MyStruct277::Method4.2192()#MyStruct277::Method5.MI.2198()#MyStruct277::Method6.MI.2200<System.Object>()#"
				call void Framework::M.IBase1.T<class BaseClass1,valuetype MyStruct277`2<class BaseClass1,class BaseClass0>>(!!1,string) leave.s LV6
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV6} LV6:
		.try {  ldloc V_7
				ldstr "MyStruct277::Method4.2192()#MyStruct277::Method5.MI.2198()#MyStruct277::Method6.MI.2200<System.Object>()#"
				call void Framework::M.IBase1.B<valuetype MyStruct277`2<class BaseClass1,class BaseClass0>>(!!0,string) leave.s LV7
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV7} LV7:
		ldstr "========================================================================\n\n"
		call void [mscorlib]System.Console::WriteLine(string)
		ret
	}
	.method public hidebysig static void StructConstrainedInterfaceCallsTest() cil managed
	{
		.maxstack  10
		ldstr "===================== Struct Constrained Interface Calls Test ====================="
		call void [mscorlib]System.Console::WriteLine(string)
		.locals init (valuetype MyStruct277`2<class BaseClass0,class BaseClass1> V_10)
		ldloca V_10
		initobj valuetype MyStruct277`2<class BaseClass0,class BaseClass1>
		.try {  ldloc V_10
				ldstr "MyStruct277::Method4.2192()#" +
				      "MyStruct277::Method5.MI.2194()#" +
				      "MyStruct277::Method6.2195<System.Object>()#" +
				      "MyStruct277::Method4.2192()#" +
				      "MyStruct277::Method5.MI.2198()#" +
				      "MyStruct277::Method6.MI.2200<System.Object>()#"
				call void Framework::M.MyStruct277.T.T<class BaseClass0,class BaseClass1,valuetype MyStruct277`2<class BaseClass0,class BaseClass1>>(!!2,string) leave.s LV0
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV0} LV0:
		.try {  ldloc V_10
				ldstr "MyStruct277::Method4.2192()#" +
				      "MyStruct277::Method5.MI.2194()#" +
				      "MyStruct277::Method6.2195<System.Object>()#" +
				      "MyStruct277::Method4.2192()#" +
				      "MyStruct277::Method5.MI.2198()#" +
				      "MyStruct277::Method6.MI.2200<System.Object>()#"
				call void Framework::M.MyStruct277.A.T<class BaseClass1,valuetype MyStruct277`2<class BaseClass0,class BaseClass1>>(!!1,string) leave.s LV1
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV1} LV1:
		.try {  ldloc V_10
				ldstr "MyStruct277::Method4.2192()#" +
				      "MyStruct277::Method5.MI.2194()#" +
				      "MyStruct277::Method6.2195<System.Object>()#" +
				      "MyStruct277::Method4.2192()#" +
				      "MyStruct277::Method5.MI.2198()#" +
				      "MyStruct277::Method6.MI.2200<System.Object>()#"
				call void Framework::M.MyStruct277.A.B<valuetype MyStruct277`2<class BaseClass0,class BaseClass1>>(!!0,string) leave.s LV2
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV2} LV2:
		.locals init (valuetype MyStruct277`2<class BaseClass1,class BaseClass0> V_11)
		ldloca V_11
		initobj valuetype MyStruct277`2<class BaseClass1,class BaseClass0>
		.try {  ldloc V_11
				ldstr "MyStruct277::Method4.2192()#" +
				      "MyStruct277::Method5.MI.2194()#" +
				      "MyStruct277::Method6.2195<System.Object>()#" +
				      "MyStruct277::Method4.2192()#" +
				      "MyStruct277::Method5.MI.2198()#" +
				      "MyStruct277::Method6.MI.2200<System.Object>()#"
				call void Framework::M.MyStruct277.T.T<class BaseClass1,class BaseClass0,valuetype MyStruct277`2<class BaseClass1,class BaseClass0>>(!!2,string) leave.s LV3
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV3} LV3:
		.try {  ldloc V_11
				ldstr "MyStruct277::Method4.2192()#" +
				      "MyStruct277::Method5.MI.2194()#" +
				      "MyStruct277::Method6.2195<System.Object>()#" +
				      "MyStruct277::Method4.2192()#" +
				      "MyStruct277::Method5.MI.2198()#" +
				      "MyStruct277::Method6.MI.2200<System.Object>()#"
				call void Framework::M.MyStruct277.B.T<class BaseClass0,valuetype MyStruct277`2<class BaseClass1,class BaseClass0>>(!!1,string) leave.s LV4
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV4} LV4:
		.try {  ldloc V_11
				ldstr "MyStruct277::Method4.2192()#" +
				      "MyStruct277::Method5.MI.2194()#" +
				      "MyStruct277::Method6.2195<System.Object>()#" +
				      "MyStruct277::Method4.2192()#" +
				      "MyStruct277::Method5.MI.2198()#" +
				      "MyStruct277::Method6.MI.2200<System.Object>()#"
				call void Framework::M.MyStruct277.B.A<valuetype MyStruct277`2<class BaseClass1,class BaseClass0>>(!!0,string) leave.s LV5
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
		.locals init (valuetype MyStruct277`2<class BaseClass0,class BaseClass1> V_14)
		ldloca V_14
		initobj valuetype MyStruct277`2<class BaseClass0,class BaseClass1>
		ldloc V_14
		box valuetype MyStruct277`2<class BaseClass0,class BaseClass1>
		ldloc V_14
		box valuetype MyStruct277`2<class BaseClass0,class BaseClass1>
		ldvirtftn instance string valuetype MyStruct277`2<class BaseClass0,class BaseClass1>::Method4()
		calli default string(object)
		ldstr "MyStruct277::Method4.2192()"
		ldstr "valuetype MyStruct277`2<class BaseClass0,class BaseClass1> on type valuetype MyStruct277`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_14
		box valuetype MyStruct277`2<class BaseClass0,class BaseClass1>
		ldloc V_14
		box valuetype MyStruct277`2<class BaseClass0,class BaseClass1>
		ldvirtftn instance string valuetype MyStruct277`2<class BaseClass0,class BaseClass1>::Method5()
		calli default string(object)
		ldstr "MyStruct277::Method5.2193()"
		ldstr "valuetype MyStruct277`2<class BaseClass0,class BaseClass1> on type valuetype MyStruct277`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_14
		box valuetype MyStruct277`2<class BaseClass0,class BaseClass1>
		ldloc V_14
		box valuetype MyStruct277`2<class BaseClass0,class BaseClass1>
		ldvirtftn instance string valuetype MyStruct277`2<class BaseClass0,class BaseClass1>::Method6<object>()
		calli default string(object)
		ldstr "MyStruct277::Method6.2195<System.Object>()"
		ldstr "valuetype MyStruct277`2<class BaseClass0,class BaseClass1> on type valuetype MyStruct277`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_14
		box valuetype MyStruct277`2<class BaseClass0,class BaseClass1>
		ldloc V_14
		box valuetype MyStruct277`2<class BaseClass0,class BaseClass1>
		ldvirtftn instance string valuetype MyStruct277`2<class BaseClass0,class BaseClass1>::ClassMethod555<object>()
		calli default string(object)
		ldstr "MyStruct277::ClassMethod555.2201<System.Object>()"
		ldstr "valuetype MyStruct277`2<class BaseClass0,class BaseClass1> on type valuetype MyStruct277`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_14 box valuetype MyStruct277`2<class BaseClass0,class BaseClass1> ldnull 
		ldloc V_14 box valuetype MyStruct277`2<class BaseClass0,class BaseClass1>
		ldvirtftn instance bool valuetype MyStruct277`2<class BaseClass0,class BaseClass1>::Equals(object) calli default bool(object,object) pop
		ldloc V_14 box valuetype MyStruct277`2<class BaseClass0,class BaseClass1>
		ldloc V_14 box valuetype MyStruct277`2<class BaseClass0,class BaseClass1>
		ldvirtftn instance int32 valuetype MyStruct277`2<class BaseClass0,class BaseClass1>::GetHashCode() calli default int32(object) pop
		ldloc V_14 box valuetype MyStruct277`2<class BaseClass0,class BaseClass1>
		ldloc V_14 box valuetype MyStruct277`2<class BaseClass0,class BaseClass1>
		ldvirtftn instance string valuetype MyStruct277`2<class BaseClass0,class BaseClass1>::ToString() calli default string(object) pop
		ldloc V_14
		box valuetype MyStruct277`2<class BaseClass0,class BaseClass1>
		ldloc V_14
		box valuetype MyStruct277`2<class BaseClass0,class BaseClass1>
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method4()
		calli default string(object)
		ldstr "MyStruct277::Method4.2192()"
		ldstr "class IBase1`1<class BaseClass1> on type valuetype MyStruct277`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_14
		box valuetype MyStruct277`2<class BaseClass0,class BaseClass1>
		ldloc V_14
		box valuetype MyStruct277`2<class BaseClass0,class BaseClass1>
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method5()
		calli default string(object)
		ldstr "MyStruct277::Method5.MI.2194()"
		ldstr "class IBase1`1<class BaseClass1> on type valuetype MyStruct277`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_14
		box valuetype MyStruct277`2<class BaseClass0,class BaseClass1>
		ldloc V_14
		box valuetype MyStruct277`2<class BaseClass0,class BaseClass1>
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method6<object>()
		calli default string(object)
		ldstr "MyStruct277::Method6.2195<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type valuetype MyStruct277`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_14
		box valuetype MyStruct277`2<class BaseClass0,class BaseClass1>
		ldloc V_14
		box valuetype MyStruct277`2<class BaseClass0,class BaseClass1>
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(object)
		ldstr "MyStruct277::Method4.2192()"
		ldstr "class IBase1`1<class BaseClass0> on type valuetype MyStruct277`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_14
		box valuetype MyStruct277`2<class BaseClass0,class BaseClass1>
		ldloc V_14
		box valuetype MyStruct277`2<class BaseClass0,class BaseClass1>
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(object)
		ldstr "MyStruct277::Method5.MI.2198()"
		ldstr "class IBase1`1<class BaseClass0> on type valuetype MyStruct277`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_14
		box valuetype MyStruct277`2<class BaseClass0,class BaseClass1>
		ldloc V_14
		box valuetype MyStruct277`2<class BaseClass0,class BaseClass1>
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(object)
		ldstr "MyStruct277::Method6.MI.2200<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type valuetype MyStruct277`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		.locals init (valuetype MyStruct277`2<class BaseClass1,class BaseClass0> V_15)
		ldloca V_15
		initobj valuetype MyStruct277`2<class BaseClass1,class BaseClass0>
		ldloc V_15
		box valuetype MyStruct277`2<class BaseClass1,class BaseClass0>
		ldloc V_15
		box valuetype MyStruct277`2<class BaseClass1,class BaseClass0>
		ldvirtftn instance string valuetype MyStruct277`2<class BaseClass1,class BaseClass0>::Method4()
		calli default string(object)
		ldstr "MyStruct277::Method4.2192()"
		ldstr "valuetype MyStruct277`2<class BaseClass1,class BaseClass0> on type valuetype MyStruct277`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_15
		box valuetype MyStruct277`2<class BaseClass1,class BaseClass0>
		ldloc V_15
		box valuetype MyStruct277`2<class BaseClass1,class BaseClass0>
		ldvirtftn instance string valuetype MyStruct277`2<class BaseClass1,class BaseClass0>::Method5()
		calli default string(object)
		ldstr "MyStruct277::Method5.2193()"
		ldstr "valuetype MyStruct277`2<class BaseClass1,class BaseClass0> on type valuetype MyStruct277`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_15
		box valuetype MyStruct277`2<class BaseClass1,class BaseClass0>
		ldloc V_15
		box valuetype MyStruct277`2<class BaseClass1,class BaseClass0>
		ldvirtftn instance string valuetype MyStruct277`2<class BaseClass1,class BaseClass0>::Method6<object>()
		calli default string(object)
		ldstr "MyStruct277::Method6.2195<System.Object>()"
		ldstr "valuetype MyStruct277`2<class BaseClass1,class BaseClass0> on type valuetype MyStruct277`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_15
		box valuetype MyStruct277`2<class BaseClass1,class BaseClass0>
		ldloc V_15
		box valuetype MyStruct277`2<class BaseClass1,class BaseClass0>
		ldvirtftn instance string valuetype MyStruct277`2<class BaseClass1,class BaseClass0>::ClassMethod555<object>()
		calli default string(object)
		ldstr "MyStruct277::ClassMethod555.2201<System.Object>()"
		ldstr "valuetype MyStruct277`2<class BaseClass1,class BaseClass0> on type valuetype MyStruct277`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_15 box valuetype MyStruct277`2<class BaseClass1,class BaseClass0> ldnull 
		ldloc V_15 box valuetype MyStruct277`2<class BaseClass1,class BaseClass0>
		ldvirtftn instance bool valuetype MyStruct277`2<class BaseClass1,class BaseClass0>::Equals(object) calli default bool(object,object) pop
		ldloc V_15 box valuetype MyStruct277`2<class BaseClass1,class BaseClass0>
		ldloc V_15 box valuetype MyStruct277`2<class BaseClass1,class BaseClass0>
		ldvirtftn instance int32 valuetype MyStruct277`2<class BaseClass1,class BaseClass0>::GetHashCode() calli default int32(object) pop
		ldloc V_15 box valuetype MyStruct277`2<class BaseClass1,class BaseClass0>
		ldloc V_15 box valuetype MyStruct277`2<class BaseClass1,class BaseClass0>
		ldvirtftn instance string valuetype MyStruct277`2<class BaseClass1,class BaseClass0>::ToString() calli default string(object) pop
		ldloc V_15
		box valuetype MyStruct277`2<class BaseClass1,class BaseClass0>
		ldloc V_15
		box valuetype MyStruct277`2<class BaseClass1,class BaseClass0>
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(object)
		ldstr "MyStruct277::Method4.2192()"
		ldstr "class IBase1`1<class BaseClass0> on type valuetype MyStruct277`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_15
		box valuetype MyStruct277`2<class BaseClass1,class BaseClass0>
		ldloc V_15
		box valuetype MyStruct277`2<class BaseClass1,class BaseClass0>
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(object)
		ldstr "MyStruct277::Method5.MI.2194()"
		ldstr "class IBase1`1<class BaseClass0> on type valuetype MyStruct277`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_15
		box valuetype MyStruct277`2<class BaseClass1,class BaseClass0>
		ldloc V_15
		box valuetype MyStruct277`2<class BaseClass1,class BaseClass0>
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(object)
		ldstr "MyStruct277::Method6.2195<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type valuetype MyStruct277`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_15
		box valuetype MyStruct277`2<class BaseClass1,class BaseClass0>
		ldloc V_15
		box valuetype MyStruct277`2<class BaseClass1,class BaseClass0>
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method4()
		calli default string(object)
		ldstr "MyStruct277::Method4.2192()"
		ldstr "class IBase1`1<class BaseClass1> on type valuetype MyStruct277`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_15
		box valuetype MyStruct277`2<class BaseClass1,class BaseClass0>
		ldloc V_15
		box valuetype MyStruct277`2<class BaseClass1,class BaseClass0>
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method5()
		calli default string(object)
		ldstr "MyStruct277::Method5.MI.2198()"
		ldstr "class IBase1`1<class BaseClass1> on type valuetype MyStruct277`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_15
		box valuetype MyStruct277`2<class BaseClass1,class BaseClass0>
		ldloc V_15
		box valuetype MyStruct277`2<class BaseClass1,class BaseClass0>
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method6<object>()
		calli default string(object)
		ldstr "MyStruct277::Method6.MI.2200<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type valuetype MyStruct277`2<class BaseClass1,class BaseClass0>"
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
