// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) .ver 4:0:0:0 }
.assembly extern TestFramework { .publickeytoken = ( B0 3F 5F 7F 11 D5 0A 3A )  }

//TYPES IN FORWARDER ASSEMBLIES:

//TEST ASSEMBLY:
.assembly extern xunit.core {}

.class public Generated24.BaseClass0 
{
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void [mscorlib]System.Object::.ctor()
		ret
	}
}
.class public Generated24.BaseClass1 
		extends Generated24.BaseClass0
{
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void Generated24.BaseClass0::.ctor()
		ret
	}
}
.class public sequential sealed Generated24.MyStruct74`1<T0> 
		extends [mscorlib]System.ValueType
		implements IBase0, class Generated24.IBase2`2<!T0,class Generated24.BaseClass1> 
{
	.pack 0
	.size 1
	.method public hidebysig newslot virtual instance string Method0() cil managed noinlining { 
		ldstr "MyStruct74::Method0.586()"
		ret
	}
	.method public hidebysig virtual instance string Method1() cil managed noinlining { 
		ldstr "MyStruct74::Method1.587()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase0.Method1'() cil managed noinlining { 
		.override method instance string Generated24.IBase0::Method1()
		ldstr "MyStruct74::Method1.MI.588()"
		ret
	}
	.method public hidebysig virtual instance string Method2<M0>() cil managed noinlining { 
		ldstr "MyStruct74::Method2.589<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase0.Method2'<M0>() cil managed noinlining { 
		.override method instance string Generated24.IBase0::Method2<[1]>()
		ldstr "MyStruct74::Method2.MI.590<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string Method3<M0>() cil managed noinlining { 
		ldstr "MyStruct74::Method3.591<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase0.Method3'<M0>() cil managed noinlining { 
		.override method instance string Generated24.IBase0::Method3<[1]>()
		ldstr "MyStruct74::Method3.MI.592<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "MyStruct74::Method7.593<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot instance string ClassMethod154() cil managed noinlining { 
		ldstr "MyStruct74::ClassMethod154.594()"
		ret
	}
	.method public hidebysig virtual instance bool Equals(object obj) cil managed { ldc.i4.0 ret }
	.method public hidebysig virtual instance int32 GetHashCode() cil managed { ldc.i4.0 ret }
	.method public hidebysig virtual instance string ToString() cil managed { ldstr "" ret }
}
.class interface public abstract Generated24.IBase0 
{
	.method public hidebysig newslot abstract virtual instance string Method0() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method1() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method2<M0>() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method3<M0>() cil managed { }
}
.class interface public abstract Generated24.IBase2`2<+T0, -T1> 
{
	.method public hidebysig newslot abstract virtual instance string Method7<M0>() cil managed { }
}
.class public auto ansi beforefieldinit Generated24.Generated24 {
	.method static void M.BaseClass0<(Generated24.BaseClass0)W>(!!W inst, string exp) cil managed {
		.maxstack 5
		.locals init (string[] actualResults)
		ldc.i4.s    0
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.BaseClass0<(Generated24.BaseClass0)W>(!!W inst, string exp)"
		ldc.i4.s 0
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.BaseClass1<(Generated24.BaseClass1)W>(!!W inst, string exp) cil managed {
		.maxstack 5
		.locals init (string[] actualResults)
		ldc.i4.s    0
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.BaseClass1<(Generated24.BaseClass1)W>(!!W inst, string exp)"
		ldc.i4.s 0
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase0<(Generated24.IBase0)W>(!!W inst, string exp) cil managed {
		.maxstack 9
		.locals init (string[] actualResults)
		ldc.i4.s    4
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase0<(Generated24.IBase0)W>(!!W inst, string exp)"
		ldc.i4.s 4
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string Generated24.IBase0::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string Generated24.IBase0::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string Generated24.IBase0::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string Generated24.IBase0::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.T.T<T0,T1,(class Generated24.IBase2`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.T.T<T0,T1,(class Generated24.IBase2`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated24.IBase2`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.A.T<T1,(class Generated24.IBase2`2<class Generated24.BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.A.T<T1,(class Generated24.IBase2`2<class Generated24.BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated24.IBase2`2<class Generated24.BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.A.A<(class Generated24.IBase2`2<class Generated24.BaseClass0,class Generated24.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.A.A<(class Generated24.IBase2`2<class Generated24.BaseClass0,class Generated24.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated24.IBase2`2<class Generated24.BaseClass0,class Generated24.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.A.B<(class Generated24.IBase2`2<class Generated24.BaseClass0,class Generated24.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.A.B<(class Generated24.IBase2`2<class Generated24.BaseClass0,class Generated24.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated24.IBase2`2<class Generated24.BaseClass0,class Generated24.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.B.T<T1,(class Generated24.IBase2`2<class Generated24.BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.B.T<T1,(class Generated24.IBase2`2<class Generated24.BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated24.IBase2`2<class Generated24.BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.B.A<(class Generated24.IBase2`2<class Generated24.BaseClass1,class Generated24.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.B.A<(class Generated24.IBase2`2<class Generated24.BaseClass1,class Generated24.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated24.IBase2`2<class Generated24.BaseClass1,class Generated24.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.B.B<(class Generated24.IBase2`2<class Generated24.BaseClass1,class Generated24.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.B.B<(class Generated24.IBase2`2<class Generated24.BaseClass1,class Generated24.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated24.IBase2`2<class Generated24.BaseClass1,class Generated24.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.MyStruct74.T<T0,(valuetype Generated24.MyStruct74`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 10
		.locals init (string[] actualResults)
		ldc.i4.s    5
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.MyStruct74.T<T0,(valuetype Generated24.MyStruct74`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 5
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. valuetype Generated24.MyStruct74`1<!!T0>
		callvirt     instance string Generated24.IBase0::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. valuetype Generated24.MyStruct74`1<!!T0>
		callvirt     instance string Generated24.IBase0::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. valuetype Generated24.MyStruct74`1<!!T0>
		callvirt     instance string Generated24.IBase0::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. valuetype Generated24.MyStruct74`1<!!T0>
		callvirt     instance string Generated24.IBase0::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. valuetype Generated24.MyStruct74`1<!!T0>
		callvirt     instance string class Generated24.IBase2`2<!!T0,class Generated24.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.MyStruct74.A<(valuetype Generated24.MyStruct74`1<class Generated24.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 10
		.locals init (string[] actualResults)
		ldc.i4.s    5
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.MyStruct74.A<(valuetype Generated24.MyStruct74`1<class Generated24.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 5
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. valuetype Generated24.MyStruct74`1<class Generated24.BaseClass0>
		callvirt     instance string Generated24.IBase0::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. valuetype Generated24.MyStruct74`1<class Generated24.BaseClass0>
		callvirt     instance string Generated24.IBase0::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. valuetype Generated24.MyStruct74`1<class Generated24.BaseClass0>
		callvirt     instance string Generated24.IBase0::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. valuetype Generated24.MyStruct74`1<class Generated24.BaseClass0>
		callvirt     instance string Generated24.IBase0::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. valuetype Generated24.MyStruct74`1<class Generated24.BaseClass0>
		callvirt     instance string class Generated24.IBase2`2<class Generated24.BaseClass0,class Generated24.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.MyStruct74.B<(valuetype Generated24.MyStruct74`1<class Generated24.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 10
		.locals init (string[] actualResults)
		ldc.i4.s    5
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.MyStruct74.B<(valuetype Generated24.MyStruct74`1<class Generated24.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 5
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. valuetype Generated24.MyStruct74`1<class Generated24.BaseClass1>
		callvirt     instance string Generated24.IBase0::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. valuetype Generated24.MyStruct74`1<class Generated24.BaseClass1>
		callvirt     instance string Generated24.IBase0::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. valuetype Generated24.MyStruct74`1<class Generated24.BaseClass1>
		callvirt     instance string Generated24.IBase0::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. valuetype Generated24.MyStruct74`1<class Generated24.BaseClass1>
		callvirt     instance string Generated24.IBase0::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. valuetype Generated24.MyStruct74`1<class Generated24.BaseClass1>
		callvirt     instance string class Generated24.IBase2`2<class Generated24.BaseClass1,class Generated24.BaseClass1>::Method7<object>()
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
		.locals init (valuetype Generated24.MyStruct74`1<class Generated24.BaseClass0> V_1)
		ldloca V_1
		initobj valuetype Generated24.MyStruct74`1<class Generated24.BaseClass0>
		ldloca V_1
		dup
		call instance string valuetype Generated24.MyStruct74`1<class Generated24.BaseClass0>::Method0()
		ldstr "MyStruct74::Method0.586()"
		ldstr "valuetype MyStruct74`1<class Generated24.BaseClass0> on type MyStruct74"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype Generated24.MyStruct74`1<class Generated24.BaseClass0>::Method1()
		ldstr "MyStruct74::Method1.587()"
		ldstr "valuetype MyStruct74`1<class Generated24.BaseClass0> on type MyStruct74"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype Generated24.MyStruct74`1<class Generated24.BaseClass0>::Method2<object>()
		ldstr "MyStruct74::Method2.589<System.Object>()"
		ldstr "valuetype MyStruct74`1<class Generated24.BaseClass0> on type MyStruct74"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype Generated24.MyStruct74`1<class Generated24.BaseClass0>::Method3<object>()
		ldstr "MyStruct74::Method3.591<System.Object>()"
		ldstr "valuetype MyStruct74`1<class Generated24.BaseClass0> on type MyStruct74"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype Generated24.MyStruct74`1<class Generated24.BaseClass0>::Method7<object>()
		ldstr "MyStruct74::Method7.593<System.Object>()"
		ldstr "valuetype MyStruct74`1<class Generated24.BaseClass0> on type MyStruct74"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype Generated24.MyStruct74`1<class Generated24.BaseClass0>::ClassMethod154()
		ldstr "MyStruct74::ClassMethod154.594()"
		ldstr "valuetype MyStruct74`1<class Generated24.BaseClass0> on type MyStruct74"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup ldnull call instance bool valuetype Generated24.MyStruct74`1<class Generated24.BaseClass0>::Equals(object) pop
		dup call instance int32 valuetype Generated24.MyStruct74`1<class Generated24.BaseClass0>::GetHashCode() pop
		dup call instance string valuetype Generated24.MyStruct74`1<class Generated24.BaseClass0>::ToString() pop
		pop
		ldloc V_1
		box valuetype Generated24.MyStruct74`1<class Generated24.BaseClass0>
		dup
		callvirt instance string Generated24.IBase0::Method0()
		ldstr "MyStruct74::Method0.586()"
		ldstr "IBase0 on type valuetype Generated24.MyStruct74`1<class Generated24.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated24.IBase0::Method1()
		ldstr "MyStruct74::Method1.MI.588()"
		ldstr "IBase0 on type valuetype Generated24.MyStruct74`1<class Generated24.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated24.IBase0::Method2<object>()
		ldstr "MyStruct74::Method2.MI.590<System.Object>()"
		ldstr "IBase0 on type valuetype Generated24.MyStruct74`1<class Generated24.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated24.IBase0::Method3<object>()
		ldstr "MyStruct74::Method3.MI.592<System.Object>()"
		ldstr "IBase0 on type valuetype Generated24.MyStruct74`1<class Generated24.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc V_1
		box valuetype Generated24.MyStruct74`1<class Generated24.BaseClass0>
		dup
		callvirt instance string class Generated24.IBase2`2<class Generated24.BaseClass0,class Generated24.BaseClass1>::Method7<object>()
		ldstr "MyStruct74::Method7.593<System.Object>()"
		ldstr "class IBase2`2<class Generated24.BaseClass0,class Generated24.BaseClass1> on type valuetype Generated24.MyStruct74`1<class Generated24.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		.locals init (valuetype Generated24.MyStruct74`1<class Generated24.BaseClass1> V_2)
		ldloca V_2
		initobj valuetype Generated24.MyStruct74`1<class Generated24.BaseClass1>
		ldloca V_2
		dup
		call instance string valuetype Generated24.MyStruct74`1<class Generated24.BaseClass1>::Method0()
		ldstr "MyStruct74::Method0.586()"
		ldstr "valuetype MyStruct74`1<class Generated24.BaseClass1> on type MyStruct74"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype Generated24.MyStruct74`1<class Generated24.BaseClass1>::Method1()
		ldstr "MyStruct74::Method1.587()"
		ldstr "valuetype MyStruct74`1<class Generated24.BaseClass1> on type MyStruct74"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype Generated24.MyStruct74`1<class Generated24.BaseClass1>::Method2<object>()
		ldstr "MyStruct74::Method2.589<System.Object>()"
		ldstr "valuetype MyStruct74`1<class Generated24.BaseClass1> on type MyStruct74"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype Generated24.MyStruct74`1<class Generated24.BaseClass1>::Method3<object>()
		ldstr "MyStruct74::Method3.591<System.Object>()"
		ldstr "valuetype MyStruct74`1<class Generated24.BaseClass1> on type MyStruct74"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype Generated24.MyStruct74`1<class Generated24.BaseClass1>::Method7<object>()
		ldstr "MyStruct74::Method7.593<System.Object>()"
		ldstr "valuetype MyStruct74`1<class Generated24.BaseClass1> on type MyStruct74"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype Generated24.MyStruct74`1<class Generated24.BaseClass1>::ClassMethod154()
		ldstr "MyStruct74::ClassMethod154.594()"
		ldstr "valuetype MyStruct74`1<class Generated24.BaseClass1> on type MyStruct74"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup ldnull call instance bool valuetype Generated24.MyStruct74`1<class Generated24.BaseClass1>::Equals(object) pop
		dup call instance int32 valuetype Generated24.MyStruct74`1<class Generated24.BaseClass1>::GetHashCode() pop
		dup call instance string valuetype Generated24.MyStruct74`1<class Generated24.BaseClass1>::ToString() pop
		pop
		ldloc V_2
		box valuetype Generated24.MyStruct74`1<class Generated24.BaseClass1>
		dup
		callvirt instance string Generated24.IBase0::Method0()
		ldstr "MyStruct74::Method0.586()"
		ldstr "IBase0 on type valuetype Generated24.MyStruct74`1<class Generated24.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated24.IBase0::Method1()
		ldstr "MyStruct74::Method1.MI.588()"
		ldstr "IBase0 on type valuetype Generated24.MyStruct74`1<class Generated24.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated24.IBase0::Method2<object>()
		ldstr "MyStruct74::Method2.MI.590<System.Object>()"
		ldstr "IBase0 on type valuetype Generated24.MyStruct74`1<class Generated24.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated24.IBase0::Method3<object>()
		ldstr "MyStruct74::Method3.MI.592<System.Object>()"
		ldstr "IBase0 on type valuetype Generated24.MyStruct74`1<class Generated24.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc V_2
		box valuetype Generated24.MyStruct74`1<class Generated24.BaseClass1>
		dup
		callvirt instance string class Generated24.IBase2`2<class Generated24.BaseClass1,class Generated24.BaseClass1>::Method7<object>()
		ldstr "MyStruct74::Method7.593<System.Object>()"
		ldstr "class IBase2`2<class Generated24.BaseClass1,class Generated24.BaseClass1> on type valuetype Generated24.MyStruct74`1<class Generated24.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc V_2
		box valuetype Generated24.MyStruct74`1<class Generated24.BaseClass1>
		dup
		callvirt instance string class Generated24.IBase2`2<class Generated24.BaseClass0,class Generated24.BaseClass1>::Method7<object>()
		ldstr "MyStruct74::Method7.593<System.Object>()"
		ldstr "class IBase2`2<class Generated24.BaseClass0,class Generated24.BaseClass1> on type valuetype Generated24.MyStruct74`1<class Generated24.BaseClass1>"
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
		.locals init (valuetype Generated24.MyStruct74`1<class Generated24.BaseClass0> V_3)
		ldloca V_3
		initobj valuetype Generated24.MyStruct74`1<class Generated24.BaseClass0>
		.try {  ldloc V_3
				ldstr "MyStruct74::Method0.586()#MyStruct74::Method1.MI.588()#MyStruct74::Method2.MI.590<System.Object>()#MyStruct74::Method3.MI.592<System.Object>()#"
				call void Generated24.Generated24::M.IBase0<valuetype Generated24.MyStruct74`1<class Generated24.BaseClass0>>(!!0,string) leave.s LV0
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV0} LV0:
		.try {  ldloc V_3
				ldstr "MyStruct74::Method7.593<System.Object>()#"
				call void Generated24.Generated24::M.IBase2.T.T<class Generated24.BaseClass0,class Generated24.BaseClass1,valuetype Generated24.MyStruct74`1<class Generated24.BaseClass0>>(!!2,string) leave.s LV1
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV1} LV1:
		.try {  ldloc V_3
				ldstr "MyStruct74::Method7.593<System.Object>()#"
				call void Generated24.Generated24::M.IBase2.A.T<class Generated24.BaseClass1,valuetype Generated24.MyStruct74`1<class Generated24.BaseClass0>>(!!1,string) leave.s LV2
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV2} LV2:
		.try {  ldloc V_3
				ldstr "MyStruct74::Method7.593<System.Object>()#"
				call void Generated24.Generated24::M.IBase2.A.B<valuetype Generated24.MyStruct74`1<class Generated24.BaseClass0>>(!!0,string) leave.s LV3
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV3} LV3:
		.locals init (valuetype Generated24.MyStruct74`1<class Generated24.BaseClass1> V_4)
		ldloca V_4
		initobj valuetype Generated24.MyStruct74`1<class Generated24.BaseClass1>
		.try {  ldloc V_4
				ldstr "MyStruct74::Method0.586()#MyStruct74::Method1.MI.588()#MyStruct74::Method2.MI.590<System.Object>()#MyStruct74::Method3.MI.592<System.Object>()#"
				call void Generated24.Generated24::M.IBase0<valuetype Generated24.MyStruct74`1<class Generated24.BaseClass1>>(!!0,string) leave.s LV4
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV4} LV4:
		.try {  ldloc V_4
				ldstr "MyStruct74::Method7.593<System.Object>()#"
				call void Generated24.Generated24::M.IBase2.T.T<class Generated24.BaseClass1,class Generated24.BaseClass1,valuetype Generated24.MyStruct74`1<class Generated24.BaseClass1>>(!!2,string) leave.s LV5
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV5} LV5:
		.try {  ldloc V_4
				ldstr "MyStruct74::Method7.593<System.Object>()#"
				call void Generated24.Generated24::M.IBase2.B.T<class Generated24.BaseClass1,valuetype Generated24.MyStruct74`1<class Generated24.BaseClass1>>(!!1,string) leave.s LV6
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV6} LV6:
		.try {  ldloc V_4
				ldstr "MyStruct74::Method7.593<System.Object>()#"
				call void Generated24.Generated24::M.IBase2.B.B<valuetype Generated24.MyStruct74`1<class Generated24.BaseClass1>>(!!0,string) leave.s LV7
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV7} LV7:
		.try {  ldloc V_4
				ldstr "MyStruct74::Method7.593<System.Object>()#"
				call void Generated24.Generated24::M.IBase2.T.T<class Generated24.BaseClass0,class Generated24.BaseClass1,valuetype Generated24.MyStruct74`1<class Generated24.BaseClass1>>(!!2,string) leave.s LV8
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV8} LV8:
		.try {  ldloc V_4
				ldstr "MyStruct74::Method7.593<System.Object>()#"
				call void Generated24.Generated24::M.IBase2.A.T<class Generated24.BaseClass1,valuetype Generated24.MyStruct74`1<class Generated24.BaseClass1>>(!!1,string) leave.s LV9
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV9} LV9:
		.try {  ldloc V_4
				ldstr "MyStruct74::Method7.593<System.Object>()#"
				call void Generated24.Generated24::M.IBase2.A.B<valuetype Generated24.MyStruct74`1<class Generated24.BaseClass1>>(!!0,string) leave.s LV10
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV10} LV10:
		ldstr "========================================================================\n\n"
		call void [mscorlib]System.Console::WriteLine(string)
		ret
	}
	.method public hidebysig static void StructConstrainedInterfaceCallsTest() cil managed
	{
		.maxstack  10
		ldstr "===================== Struct Constrained Interface Calls Test ====================="
		call void [mscorlib]System.Console::WriteLine(string)
		.locals init (valuetype Generated24.MyStruct74`1<class Generated24.BaseClass0> V_5)
		ldloca V_5
		initobj valuetype Generated24.MyStruct74`1<class Generated24.BaseClass0>
		.try {  ldloc V_5
				ldstr "MyStruct74::Method0.586()#" +
				      "MyStruct74::Method1.MI.588()#" +
				      "MyStruct74::Method2.MI.590<System.Object>()#" +
				      "MyStruct74::Method3.MI.592<System.Object>()#" +
				      "MyStruct74::Method7.593<System.Object>()#"
				call void Generated24.Generated24::M.MyStruct74.T<class Generated24.BaseClass0,valuetype Generated24.MyStruct74`1<class Generated24.BaseClass0>>(!!1,string) leave.s LV0
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV0} LV0:
		.try {  ldloc V_5
				ldstr "MyStruct74::Method0.586()#" +
				      "MyStruct74::Method1.MI.588()#" +
				      "MyStruct74::Method2.MI.590<System.Object>()#" +
				      "MyStruct74::Method3.MI.592<System.Object>()#" +
				      "MyStruct74::Method7.593<System.Object>()#"
				call void Generated24.Generated24::M.MyStruct74.A<valuetype Generated24.MyStruct74`1<class Generated24.BaseClass0>>(!!0,string) leave.s LV1
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV1} LV1:
		.locals init (valuetype Generated24.MyStruct74`1<class Generated24.BaseClass1> V_6)
		ldloca V_6
		initobj valuetype Generated24.MyStruct74`1<class Generated24.BaseClass1>
		.try {  ldloc V_6
				ldstr "MyStruct74::Method0.586()#" +
				      "MyStruct74::Method1.MI.588()#" +
				      "MyStruct74::Method2.MI.590<System.Object>()#" +
				      "MyStruct74::Method3.MI.592<System.Object>()#" +
				      "MyStruct74::Method7.593<System.Object>()#"
				call void Generated24.Generated24::M.MyStruct74.T<class Generated24.BaseClass1,valuetype Generated24.MyStruct74`1<class Generated24.BaseClass1>>(!!1,string) leave.s LV2
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV2} LV2:
		.try {  ldloc V_6
				ldstr "MyStruct74::Method0.586()#" +
				      "MyStruct74::Method1.MI.588()#" +
				      "MyStruct74::Method2.MI.590<System.Object>()#" +
				      "MyStruct74::Method3.MI.592<System.Object>()#" +
				      "MyStruct74::Method7.593<System.Object>()#"
				call void Generated24.Generated24::M.MyStruct74.B<valuetype Generated24.MyStruct74`1<class Generated24.BaseClass1>>(!!0,string) leave.s LV3
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV3} LV3:
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
		.locals init (valuetype Generated24.MyStruct74`1<class Generated24.BaseClass0> V_7)
		ldloca V_7
		initobj valuetype Generated24.MyStruct74`1<class Generated24.BaseClass0>
		ldloc V_7
		box valuetype Generated24.MyStruct74`1<class Generated24.BaseClass0>
		ldloc V_7
		box valuetype Generated24.MyStruct74`1<class Generated24.BaseClass0>
		ldvirtftn instance string valuetype Generated24.MyStruct74`1<class Generated24.BaseClass0>::Method0()
		calli default string(object)
		ldstr "MyStruct74::Method0.586()"
		ldstr "valuetype MyStruct74`1<class Generated24.BaseClass0> on type valuetype Generated24.MyStruct74`1<class Generated24.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_7
		box valuetype Generated24.MyStruct74`1<class Generated24.BaseClass0>
		ldloc V_7
		box valuetype Generated24.MyStruct74`1<class Generated24.BaseClass0>
		ldvirtftn instance string valuetype Generated24.MyStruct74`1<class Generated24.BaseClass0>::Method1()
		calli default string(object)
		ldstr "MyStruct74::Method1.587()"
		ldstr "valuetype MyStruct74`1<class Generated24.BaseClass0> on type valuetype Generated24.MyStruct74`1<class Generated24.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_7
		box valuetype Generated24.MyStruct74`1<class Generated24.BaseClass0>
		ldloc V_7
		box valuetype Generated24.MyStruct74`1<class Generated24.BaseClass0>
		ldvirtftn instance string valuetype Generated24.MyStruct74`1<class Generated24.BaseClass0>::Method2<object>()
		calli default string(object)
		ldstr "MyStruct74::Method2.589<System.Object>()"
		ldstr "valuetype MyStruct74`1<class Generated24.BaseClass0> on type valuetype Generated24.MyStruct74`1<class Generated24.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_7
		box valuetype Generated24.MyStruct74`1<class Generated24.BaseClass0>
		ldloc V_7
		box valuetype Generated24.MyStruct74`1<class Generated24.BaseClass0>
		ldvirtftn instance string valuetype Generated24.MyStruct74`1<class Generated24.BaseClass0>::Method3<object>()
		calli default string(object)
		ldstr "MyStruct74::Method3.591<System.Object>()"
		ldstr "valuetype MyStruct74`1<class Generated24.BaseClass0> on type valuetype Generated24.MyStruct74`1<class Generated24.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_7
		box valuetype Generated24.MyStruct74`1<class Generated24.BaseClass0>
		ldloc V_7
		box valuetype Generated24.MyStruct74`1<class Generated24.BaseClass0>
		ldvirtftn instance string valuetype Generated24.MyStruct74`1<class Generated24.BaseClass0>::Method7<object>()
		calli default string(object)
		ldstr "MyStruct74::Method7.593<System.Object>()"
		ldstr "valuetype MyStruct74`1<class Generated24.BaseClass0> on type valuetype Generated24.MyStruct74`1<class Generated24.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_7
		box valuetype Generated24.MyStruct74`1<class Generated24.BaseClass0>
		ldloc V_7
		box valuetype Generated24.MyStruct74`1<class Generated24.BaseClass0>
		ldvirtftn instance string valuetype Generated24.MyStruct74`1<class Generated24.BaseClass0>::ClassMethod154()
		calli default string(object)
		ldstr "MyStruct74::ClassMethod154.594()"
		ldstr "valuetype MyStruct74`1<class Generated24.BaseClass0> on type valuetype Generated24.MyStruct74`1<class Generated24.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_7 box valuetype Generated24.MyStruct74`1<class Generated24.BaseClass0> ldnull 
		ldloc V_7 box valuetype Generated24.MyStruct74`1<class Generated24.BaseClass0>
		ldvirtftn instance bool valuetype Generated24.MyStruct74`1<class Generated24.BaseClass0>::Equals(object) calli default bool(object,object) pop
		ldloc V_7 box valuetype Generated24.MyStruct74`1<class Generated24.BaseClass0>
		ldloc V_7 box valuetype Generated24.MyStruct74`1<class Generated24.BaseClass0>
		ldvirtftn instance int32 valuetype Generated24.MyStruct74`1<class Generated24.BaseClass0>::GetHashCode() calli default int32(object) pop
		ldloc V_7 box valuetype Generated24.MyStruct74`1<class Generated24.BaseClass0>
		ldloc V_7 box valuetype Generated24.MyStruct74`1<class Generated24.BaseClass0>
		ldvirtftn instance string valuetype Generated24.MyStruct74`1<class Generated24.BaseClass0>::ToString() calli default string(object) pop
		ldloc V_7
		box valuetype Generated24.MyStruct74`1<class Generated24.BaseClass0>
		ldloc V_7
		box valuetype Generated24.MyStruct74`1<class Generated24.BaseClass0>
		ldvirtftn instance string Generated24.IBase0::Method0()
		calli default string(object)
		ldstr "MyStruct74::Method0.586()"
		ldstr "IBase0 on type valuetype Generated24.MyStruct74`1<class Generated24.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_7
		box valuetype Generated24.MyStruct74`1<class Generated24.BaseClass0>
		ldloc V_7
		box valuetype Generated24.MyStruct74`1<class Generated24.BaseClass0>
		ldvirtftn instance string Generated24.IBase0::Method1()
		calli default string(object)
		ldstr "MyStruct74::Method1.MI.588()"
		ldstr "IBase0 on type valuetype Generated24.MyStruct74`1<class Generated24.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_7
		box valuetype Generated24.MyStruct74`1<class Generated24.BaseClass0>
		ldloc V_7
		box valuetype Generated24.MyStruct74`1<class Generated24.BaseClass0>
		ldvirtftn instance string Generated24.IBase0::Method2<object>()
		calli default string(object)
		ldstr "MyStruct74::Method2.MI.590<System.Object>()"
		ldstr "IBase0 on type valuetype Generated24.MyStruct74`1<class Generated24.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_7
		box valuetype Generated24.MyStruct74`1<class Generated24.BaseClass0>
		ldloc V_7
		box valuetype Generated24.MyStruct74`1<class Generated24.BaseClass0>
		ldvirtftn instance string Generated24.IBase0::Method3<object>()
		calli default string(object)
		ldstr "MyStruct74::Method3.MI.592<System.Object>()"
		ldstr "IBase0 on type valuetype Generated24.MyStruct74`1<class Generated24.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_7
		box valuetype Generated24.MyStruct74`1<class Generated24.BaseClass0>
		ldloc V_7
		box valuetype Generated24.MyStruct74`1<class Generated24.BaseClass0>
		ldvirtftn instance string class Generated24.IBase2`2<class Generated24.BaseClass0,class Generated24.BaseClass1>::Method7<object>()
		calli default string(object)
		ldstr "MyStruct74::Method7.593<System.Object>()"
		ldstr "class IBase2`2<class Generated24.BaseClass0,class Generated24.BaseClass1> on type valuetype Generated24.MyStruct74`1<class Generated24.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		.locals init (valuetype Generated24.MyStruct74`1<class Generated24.BaseClass1> V_8)
		ldloca V_8
		initobj valuetype Generated24.MyStruct74`1<class Generated24.BaseClass1>
		ldloc V_8
		box valuetype Generated24.MyStruct74`1<class Generated24.BaseClass1>
		ldloc V_8
		box valuetype Generated24.MyStruct74`1<class Generated24.BaseClass1>
		ldvirtftn instance string valuetype Generated24.MyStruct74`1<class Generated24.BaseClass1>::Method0()
		calli default string(object)
		ldstr "MyStruct74::Method0.586()"
		ldstr "valuetype MyStruct74`1<class Generated24.BaseClass1> on type valuetype Generated24.MyStruct74`1<class Generated24.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_8
		box valuetype Generated24.MyStruct74`1<class Generated24.BaseClass1>
		ldloc V_8
		box valuetype Generated24.MyStruct74`1<class Generated24.BaseClass1>
		ldvirtftn instance string valuetype Generated24.MyStruct74`1<class Generated24.BaseClass1>::Method1()
		calli default string(object)
		ldstr "MyStruct74::Method1.587()"
		ldstr "valuetype MyStruct74`1<class Generated24.BaseClass1> on type valuetype Generated24.MyStruct74`1<class Generated24.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_8
		box valuetype Generated24.MyStruct74`1<class Generated24.BaseClass1>
		ldloc V_8
		box valuetype Generated24.MyStruct74`1<class Generated24.BaseClass1>
		ldvirtftn instance string valuetype Generated24.MyStruct74`1<class Generated24.BaseClass1>::Method2<object>()
		calli default string(object)
		ldstr "MyStruct74::Method2.589<System.Object>()"
		ldstr "valuetype MyStruct74`1<class Generated24.BaseClass1> on type valuetype Generated24.MyStruct74`1<class Generated24.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_8
		box valuetype Generated24.MyStruct74`1<class Generated24.BaseClass1>
		ldloc V_8
		box valuetype Generated24.MyStruct74`1<class Generated24.BaseClass1>
		ldvirtftn instance string valuetype Generated24.MyStruct74`1<class Generated24.BaseClass1>::Method3<object>()
		calli default string(object)
		ldstr "MyStruct74::Method3.591<System.Object>()"
		ldstr "valuetype MyStruct74`1<class Generated24.BaseClass1> on type valuetype Generated24.MyStruct74`1<class Generated24.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_8
		box valuetype Generated24.MyStruct74`1<class Generated24.BaseClass1>
		ldloc V_8
		box valuetype Generated24.MyStruct74`1<class Generated24.BaseClass1>
		ldvirtftn instance string valuetype Generated24.MyStruct74`1<class Generated24.BaseClass1>::Method7<object>()
		calli default string(object)
		ldstr "MyStruct74::Method7.593<System.Object>()"
		ldstr "valuetype MyStruct74`1<class Generated24.BaseClass1> on type valuetype Generated24.MyStruct74`1<class Generated24.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_8
		box valuetype Generated24.MyStruct74`1<class Generated24.BaseClass1>
		ldloc V_8
		box valuetype Generated24.MyStruct74`1<class Generated24.BaseClass1>
		ldvirtftn instance string valuetype Generated24.MyStruct74`1<class Generated24.BaseClass1>::ClassMethod154()
		calli default string(object)
		ldstr "MyStruct74::ClassMethod154.594()"
		ldstr "valuetype MyStruct74`1<class Generated24.BaseClass1> on type valuetype Generated24.MyStruct74`1<class Generated24.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_8 box valuetype Generated24.MyStruct74`1<class Generated24.BaseClass1> ldnull 
		ldloc V_8 box valuetype Generated24.MyStruct74`1<class Generated24.BaseClass1>
		ldvirtftn instance bool valuetype Generated24.MyStruct74`1<class Generated24.BaseClass1>::Equals(object) calli default bool(object,object) pop
		ldloc V_8 box valuetype Generated24.MyStruct74`1<class Generated24.BaseClass1>
		ldloc V_8 box valuetype Generated24.MyStruct74`1<class Generated24.BaseClass1>
		ldvirtftn instance int32 valuetype Generated24.MyStruct74`1<class Generated24.BaseClass1>::GetHashCode() calli default int32(object) pop
		ldloc V_8 box valuetype Generated24.MyStruct74`1<class Generated24.BaseClass1>
		ldloc V_8 box valuetype Generated24.MyStruct74`1<class Generated24.BaseClass1>
		ldvirtftn instance string valuetype Generated24.MyStruct74`1<class Generated24.BaseClass1>::ToString() calli default string(object) pop
		ldloc V_8
		box valuetype Generated24.MyStruct74`1<class Generated24.BaseClass1>
		ldloc V_8
		box valuetype Generated24.MyStruct74`1<class Generated24.BaseClass1>
		ldvirtftn instance string Generated24.IBase0::Method0()
		calli default string(object)
		ldstr "MyStruct74::Method0.586()"
		ldstr "IBase0 on type valuetype Generated24.MyStruct74`1<class Generated24.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_8
		box valuetype Generated24.MyStruct74`1<class Generated24.BaseClass1>
		ldloc V_8
		box valuetype Generated24.MyStruct74`1<class Generated24.BaseClass1>
		ldvirtftn instance string Generated24.IBase0::Method1()
		calli default string(object)
		ldstr "MyStruct74::Method1.MI.588()"
		ldstr "IBase0 on type valuetype Generated24.MyStruct74`1<class Generated24.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_8
		box valuetype Generated24.MyStruct74`1<class Generated24.BaseClass1>
		ldloc V_8
		box valuetype Generated24.MyStruct74`1<class Generated24.BaseClass1>
		ldvirtftn instance string Generated24.IBase0::Method2<object>()
		calli default string(object)
		ldstr "MyStruct74::Method2.MI.590<System.Object>()"
		ldstr "IBase0 on type valuetype Generated24.MyStruct74`1<class Generated24.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_8
		box valuetype Generated24.MyStruct74`1<class Generated24.BaseClass1>
		ldloc V_8
		box valuetype Generated24.MyStruct74`1<class Generated24.BaseClass1>
		ldvirtftn instance string Generated24.IBase0::Method3<object>()
		calli default string(object)
		ldstr "MyStruct74::Method3.MI.592<System.Object>()"
		ldstr "IBase0 on type valuetype Generated24.MyStruct74`1<class Generated24.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_8
		box valuetype Generated24.MyStruct74`1<class Generated24.BaseClass1>
		ldloc V_8
		box valuetype Generated24.MyStruct74`1<class Generated24.BaseClass1>
		ldvirtftn instance string class Generated24.IBase2`2<class Generated24.BaseClass1,class Generated24.BaseClass1>::Method7<object>()
		calli default string(object)
		ldstr "MyStruct74::Method7.593<System.Object>()"
		ldstr "class IBase2`2<class Generated24.BaseClass1,class Generated24.BaseClass1> on type valuetype Generated24.MyStruct74`1<class Generated24.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_8
		box valuetype Generated24.MyStruct74`1<class Generated24.BaseClass1>
		ldloc V_8
		box valuetype Generated24.MyStruct74`1<class Generated24.BaseClass1>
		ldvirtftn instance string class Generated24.IBase2`2<class Generated24.BaseClass0,class Generated24.BaseClass1>::Method7<object>()
		calli default string(object)
		ldstr "MyStruct74::Method7.593<System.Object>()"
		ldstr "class IBase2`2<class Generated24.BaseClass0,class Generated24.BaseClass1> on type valuetype Generated24.MyStruct74`1<class Generated24.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldstr "========================================================================\n\n"
		call void [mscorlib]System.Console::WriteLine(string)
		ret
	}
	.method public hidebysig static int32  Main() cil managed
	{
		.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
		    01 00 00 00
		)
		.entrypoint
		.maxstack  10
		call void Generated24.Generated24::MethodCallingTest()
		call void Generated24.Generated24::ConstrainedCallsTest()
		call void Generated24.Generated24::StructConstrainedInterfaceCallsTest()
		call void Generated24.Generated24::CalliTest()
		ldc.i4 100
		ret
	}
}
