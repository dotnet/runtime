// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) .ver 4:0:0:0 }
.assembly extern TestFramework { .publickeytoken = ( B0 3F 5F 7F 11 D5 0A 3A )  }

//TYPES IN FORWARDER ASSEMBLIES:

//TEST ASSEMBLY:
.assembly extern xunit.core {}

.class public Generated212.BaseClass0 
{
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void [mscorlib]System.Object::.ctor()
		ret
	}
}
.class public Generated212.BaseClass1 
		extends Generated212.BaseClass0
{
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void Generated212.BaseClass0::.ctor()
		ret
	}
}
.class public sequential sealed Generated212.MyStruct262`2<T0, T1> 
		extends [mscorlib]System.ValueType
		implements class Generated212.IBase1`1<class Generated212.BaseClass0>, IBase0 
{
	.pack 0
	.size 1
	.method public hidebysig virtual instance string Method4() cil managed noinlining { 
		ldstr "MyStruct262::Method4.2002()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<class Generated212.BaseClass0>.Method4'() cil managed noinlining { 
		.override method instance string class Generated212.IBase1`1<class Generated212.BaseClass0>::Method4()
		ldstr "MyStruct262::Method4.MI.2003()"
		ret
	}
	.method public hidebysig virtual instance string Method5() cil managed noinlining { 
		ldstr "MyStruct262::Method5.2004()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<class Generated212.BaseClass0>.Method5'() cil managed noinlining { 
		.override method instance string class Generated212.IBase1`1<class Generated212.BaseClass0>::Method5()
		ldstr "MyStruct262::Method5.MI.2005()"
		ret
	}
	.method public hidebysig newslot virtual instance string Method6<M0>() cil managed noinlining { 
		ldstr "MyStruct262::Method6.2006<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string Method0() cil managed noinlining { 
		ldstr "MyStruct262::Method0.2007()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase0.Method0'() cil managed noinlining { 
		.override method instance string Generated212.IBase0::Method0()
		ldstr "MyStruct262::Method0.MI.2008()"
		ret
	}
	.method public hidebysig newslot virtual instance string Method1() cil managed noinlining { 
		ldstr "MyStruct262::Method1.2009()"
		ret
	}
	.method public hidebysig newslot virtual instance string Method2<M0>() cil managed noinlining { 
		ldstr "MyStruct262::Method2.2010<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase0.Method2'<M0>() cil managed noinlining { 
		.override method instance string Generated212.IBase0::Method2<[1]>()
		ldstr "MyStruct262::Method2.MI.2011<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string Method3<M0>() cil managed noinlining { 
		ldstr "MyStruct262::Method3.2012<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase0.Method3'<M0>() cil managed noinlining { 
		.override method instance string Generated212.IBase0::Method3<[1]>()
		ldstr "MyStruct262::Method3.MI.2013<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot instance string ClassMethod529() cil managed noinlining { 
		ldstr "MyStruct262::ClassMethod529.2014()"
		ret
	}
	.method public hidebysig newslot instance string ClassMethod530() cil managed noinlining { 
		ldstr "MyStruct262::ClassMethod530.2015()"
		ret
	}
	.method public hidebysig newslot instance string ClassMethod531<M0>() cil managed noinlining { 
		ldstr "MyStruct262::ClassMethod531.2016<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot instance string ClassMethod532<M0>() cil managed noinlining { 
		ldstr "MyStruct262::ClassMethod532.2017<"
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
.class interface public abstract Generated212.IBase1`1<+T0> 
{
	.method public hidebysig newslot abstract virtual instance string Method4() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method5() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method6<M0>() cil managed { }
}
.class interface public abstract Generated212.IBase0 
{
	.method public hidebysig newslot abstract virtual instance string Method0() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method1() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method2<M0>() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method3<M0>() cil managed { }
}
.class public auto ansi beforefieldinit Generated212.Generated212 {
	.method static void M.BaseClass0<(Generated212.BaseClass0)W>(!!W inst, string exp) cil managed {
		.maxstack 5
		.locals init (string[] actualResults)
		ldc.i4.s    0
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.BaseClass0<(Generated212.BaseClass0)W>(!!W inst, string exp)"
		ldc.i4.s 0
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.BaseClass1<(Generated212.BaseClass1)W>(!!W inst, string exp) cil managed {
		.maxstack 5
		.locals init (string[] actualResults)
		ldc.i4.s    0
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.BaseClass1<(Generated212.BaseClass1)W>(!!W inst, string exp)"
		ldc.i4.s 0
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase1.T<T0,(class Generated212.IBase1`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase1.T<T0,(class Generated212.IBase1`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated212.IBase1`1<!!T0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated212.IBase1`1<!!T0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated212.IBase1`1<!!T0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase1.A<(class Generated212.IBase1`1<class Generated212.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase1.A<(class Generated212.IBase1`1<class Generated212.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated212.IBase1`1<class Generated212.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated212.IBase1`1<class Generated212.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated212.IBase1`1<class Generated212.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase1.B<(class Generated212.IBase1`1<class Generated212.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase1.B<(class Generated212.IBase1`1<class Generated212.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated212.IBase1`1<class Generated212.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated212.IBase1`1<class Generated212.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated212.IBase1`1<class Generated212.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase0<(Generated212.IBase0)W>(!!W inst, string exp) cil managed {
		.maxstack 9
		.locals init (string[] actualResults)
		ldc.i4.s    4
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase0<(Generated212.IBase0)W>(!!W inst, string exp)"
		ldc.i4.s 4
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string Generated212.IBase0::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string Generated212.IBase0::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string Generated212.IBase0::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string Generated212.IBase0::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.MyStruct262.T.T<T0,T1,(valuetype Generated212.MyStruct262`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 12
		.locals init (string[] actualResults)
		ldc.i4.s    7
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.MyStruct262.T.T<T0,T1,(valuetype Generated212.MyStruct262`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 7
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. valuetype Generated212.MyStruct262`2<!!T0,!!T1>
		callvirt     instance string class Generated212.IBase1`1<class Generated212.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. valuetype Generated212.MyStruct262`2<!!T0,!!T1>
		callvirt     instance string class Generated212.IBase1`1<class Generated212.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. valuetype Generated212.MyStruct262`2<!!T0,!!T1>
		callvirt     instance string class Generated212.IBase1`1<class Generated212.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. valuetype Generated212.MyStruct262`2<!!T0,!!T1>
		callvirt     instance string Generated212.IBase0::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. valuetype Generated212.MyStruct262`2<!!T0,!!T1>
		callvirt     instance string Generated212.IBase0::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. valuetype Generated212.MyStruct262`2<!!T0,!!T1>
		callvirt     instance string Generated212.IBase0::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. valuetype Generated212.MyStruct262`2<!!T0,!!T1>
		callvirt     instance string Generated212.IBase0::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.MyStruct262.A.T<T1,(valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 12
		.locals init (string[] actualResults)
		ldc.i4.s    7
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.MyStruct262.A.T<T1,(valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 7
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,!!T1>
		callvirt     instance string class Generated212.IBase1`1<class Generated212.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,!!T1>
		callvirt     instance string class Generated212.IBase1`1<class Generated212.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,!!T1>
		callvirt     instance string class Generated212.IBase1`1<class Generated212.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,!!T1>
		callvirt     instance string Generated212.IBase0::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,!!T1>
		callvirt     instance string Generated212.IBase0::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,!!T1>
		callvirt     instance string Generated212.IBase0::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,!!T1>
		callvirt     instance string Generated212.IBase0::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.MyStruct262.A.A<(valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 12
		.locals init (string[] actualResults)
		ldc.i4.s    7
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.MyStruct262.A.A<(valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 7
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0>
		callvirt     instance string class Generated212.IBase1`1<class Generated212.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0>
		callvirt     instance string class Generated212.IBase1`1<class Generated212.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0>
		callvirt     instance string class Generated212.IBase1`1<class Generated212.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0>
		callvirt     instance string Generated212.IBase0::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0>
		callvirt     instance string Generated212.IBase0::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0>
		callvirt     instance string Generated212.IBase0::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0>
		callvirt     instance string Generated212.IBase0::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.MyStruct262.A.B<(valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 12
		.locals init (string[] actualResults)
		ldc.i4.s    7
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.MyStruct262.A.B<(valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 7
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1>
		callvirt     instance string class Generated212.IBase1`1<class Generated212.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1>
		callvirt     instance string class Generated212.IBase1`1<class Generated212.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1>
		callvirt     instance string class Generated212.IBase1`1<class Generated212.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1>
		callvirt     instance string Generated212.IBase0::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1>
		callvirt     instance string Generated212.IBase0::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1>
		callvirt     instance string Generated212.IBase0::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1>
		callvirt     instance string Generated212.IBase0::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.MyStruct262.B.T<T1,(valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 12
		.locals init (string[] actualResults)
		ldc.i4.s    7
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.MyStruct262.B.T<T1,(valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 7
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,!!T1>
		callvirt     instance string class Generated212.IBase1`1<class Generated212.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,!!T1>
		callvirt     instance string class Generated212.IBase1`1<class Generated212.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,!!T1>
		callvirt     instance string class Generated212.IBase1`1<class Generated212.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,!!T1>
		callvirt     instance string Generated212.IBase0::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,!!T1>
		callvirt     instance string Generated212.IBase0::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,!!T1>
		callvirt     instance string Generated212.IBase0::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,!!T1>
		callvirt     instance string Generated212.IBase0::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.MyStruct262.B.A<(valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 12
		.locals init (string[] actualResults)
		ldc.i4.s    7
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.MyStruct262.B.A<(valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 7
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0>
		callvirt     instance string class Generated212.IBase1`1<class Generated212.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0>
		callvirt     instance string class Generated212.IBase1`1<class Generated212.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0>
		callvirt     instance string class Generated212.IBase1`1<class Generated212.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0>
		callvirt     instance string Generated212.IBase0::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0>
		callvirt     instance string Generated212.IBase0::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0>
		callvirt     instance string Generated212.IBase0::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0>
		callvirt     instance string Generated212.IBase0::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.MyStruct262.B.B<(valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 12
		.locals init (string[] actualResults)
		ldc.i4.s    7
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.MyStruct262.B.B<(valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 7
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1>
		callvirt     instance string class Generated212.IBase1`1<class Generated212.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1>
		callvirt     instance string class Generated212.IBase1`1<class Generated212.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1>
		callvirt     instance string class Generated212.IBase1`1<class Generated212.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1>
		callvirt     instance string Generated212.IBase0::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1>
		callvirt     instance string Generated212.IBase0::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1>
		callvirt     instance string Generated212.IBase0::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1>
		callvirt     instance string Generated212.IBase0::Method3<object>()
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
		.locals init (valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0> V_1)
		ldloca V_1
		initobj valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0>
		ldloca V_1
		dup
		call instance string valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0>::Method4()
		ldstr "MyStruct262::Method4.2002()"
		ldstr "valuetype MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0> on type MyStruct262"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0>::Method5()
		ldstr "MyStruct262::Method5.2004()"
		ldstr "valuetype MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0> on type MyStruct262"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0>::Method6<object>()
		ldstr "MyStruct262::Method6.2006<System.Object>()"
		ldstr "valuetype MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0> on type MyStruct262"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0>::Method0()
		ldstr "MyStruct262::Method0.2007()"
		ldstr "valuetype MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0> on type MyStruct262"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0>::Method1()
		ldstr "MyStruct262::Method1.2009()"
		ldstr "valuetype MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0> on type MyStruct262"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0>::Method2<object>()
		ldstr "MyStruct262::Method2.2010<System.Object>()"
		ldstr "valuetype MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0> on type MyStruct262"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0>::Method3<object>()
		ldstr "MyStruct262::Method3.2012<System.Object>()"
		ldstr "valuetype MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0> on type MyStruct262"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0>::ClassMethod529()
		ldstr "MyStruct262::ClassMethod529.2014()"
		ldstr "valuetype MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0> on type MyStruct262"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0>::ClassMethod530()
		ldstr "MyStruct262::ClassMethod530.2015()"
		ldstr "valuetype MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0> on type MyStruct262"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0>::ClassMethod531<object>()
		ldstr "MyStruct262::ClassMethod531.2016<System.Object>()"
		ldstr "valuetype MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0> on type MyStruct262"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0>::ClassMethod532<object>()
		ldstr "MyStruct262::ClassMethod532.2017<System.Object>()"
		ldstr "valuetype MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0> on type MyStruct262"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup ldnull call instance bool valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0>::Equals(object) pop
		dup call instance int32 valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0>::GetHashCode() pop
		dup call instance string valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0>::ToString() pop
		pop
		ldloc V_1
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0>
		dup
		callvirt instance string class Generated212.IBase1`1<class Generated212.BaseClass0>::Method4()
		ldstr "MyStruct262::Method4.MI.2003()"
		ldstr "class IBase1`1<class Generated212.BaseClass0> on type valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated212.IBase1`1<class Generated212.BaseClass0>::Method5()
		ldstr "MyStruct262::Method5.MI.2005()"
		ldstr "class IBase1`1<class Generated212.BaseClass0> on type valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated212.IBase1`1<class Generated212.BaseClass0>::Method6<object>()
		ldstr "MyStruct262::Method6.2006<System.Object>()"
		ldstr "class IBase1`1<class Generated212.BaseClass0> on type valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc V_1
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0>
		dup
		callvirt instance string Generated212.IBase0::Method0()
		ldstr "MyStruct262::Method0.MI.2008()"
		ldstr "IBase0 on type valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated212.IBase0::Method1()
		ldstr "MyStruct262::Method1.2009()"
		ldstr "IBase0 on type valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated212.IBase0::Method2<object>()
		ldstr "MyStruct262::Method2.MI.2011<System.Object>()"
		ldstr "IBase0 on type valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated212.IBase0::Method3<object>()
		ldstr "MyStruct262::Method3.MI.2013<System.Object>()"
		ldstr "IBase0 on type valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		.locals init (valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1> V_2)
		ldloca V_2
		initobj valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1>
		ldloca V_2
		dup
		call instance string valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1>::Method4()
		ldstr "MyStruct262::Method4.2002()"
		ldstr "valuetype MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1> on type MyStruct262"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1>::Method5()
		ldstr "MyStruct262::Method5.2004()"
		ldstr "valuetype MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1> on type MyStruct262"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1>::Method6<object>()
		ldstr "MyStruct262::Method6.2006<System.Object>()"
		ldstr "valuetype MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1> on type MyStruct262"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1>::Method0()
		ldstr "MyStruct262::Method0.2007()"
		ldstr "valuetype MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1> on type MyStruct262"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1>::Method1()
		ldstr "MyStruct262::Method1.2009()"
		ldstr "valuetype MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1> on type MyStruct262"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1>::Method2<object>()
		ldstr "MyStruct262::Method2.2010<System.Object>()"
		ldstr "valuetype MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1> on type MyStruct262"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1>::Method3<object>()
		ldstr "MyStruct262::Method3.2012<System.Object>()"
		ldstr "valuetype MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1> on type MyStruct262"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1>::ClassMethod529()
		ldstr "MyStruct262::ClassMethod529.2014()"
		ldstr "valuetype MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1> on type MyStruct262"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1>::ClassMethod530()
		ldstr "MyStruct262::ClassMethod530.2015()"
		ldstr "valuetype MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1> on type MyStruct262"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1>::ClassMethod531<object>()
		ldstr "MyStruct262::ClassMethod531.2016<System.Object>()"
		ldstr "valuetype MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1> on type MyStruct262"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1>::ClassMethod532<object>()
		ldstr "MyStruct262::ClassMethod532.2017<System.Object>()"
		ldstr "valuetype MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1> on type MyStruct262"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup ldnull call instance bool valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1>::Equals(object) pop
		dup call instance int32 valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1>::GetHashCode() pop
		dup call instance string valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1>::ToString() pop
		pop
		ldloc V_2
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1>
		dup
		callvirt instance string class Generated212.IBase1`1<class Generated212.BaseClass0>::Method4()
		ldstr "MyStruct262::Method4.MI.2003()"
		ldstr "class IBase1`1<class Generated212.BaseClass0> on type valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated212.IBase1`1<class Generated212.BaseClass0>::Method5()
		ldstr "MyStruct262::Method5.MI.2005()"
		ldstr "class IBase1`1<class Generated212.BaseClass0> on type valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated212.IBase1`1<class Generated212.BaseClass0>::Method6<object>()
		ldstr "MyStruct262::Method6.2006<System.Object>()"
		ldstr "class IBase1`1<class Generated212.BaseClass0> on type valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc V_2
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1>
		dup
		callvirt instance string Generated212.IBase0::Method0()
		ldstr "MyStruct262::Method0.MI.2008()"
		ldstr "IBase0 on type valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated212.IBase0::Method1()
		ldstr "MyStruct262::Method1.2009()"
		ldstr "IBase0 on type valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated212.IBase0::Method2<object>()
		ldstr "MyStruct262::Method2.MI.2011<System.Object>()"
		ldstr "IBase0 on type valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated212.IBase0::Method3<object>()
		ldstr "MyStruct262::Method3.MI.2013<System.Object>()"
		ldstr "IBase0 on type valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		.locals init (valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0> V_3)
		ldloca V_3
		initobj valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0>
		ldloca V_3
		dup
		call instance string valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0>::Method4()
		ldstr "MyStruct262::Method4.2002()"
		ldstr "valuetype MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0> on type MyStruct262"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0>::Method5()
		ldstr "MyStruct262::Method5.2004()"
		ldstr "valuetype MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0> on type MyStruct262"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0>::Method6<object>()
		ldstr "MyStruct262::Method6.2006<System.Object>()"
		ldstr "valuetype MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0> on type MyStruct262"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0>::Method0()
		ldstr "MyStruct262::Method0.2007()"
		ldstr "valuetype MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0> on type MyStruct262"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0>::Method1()
		ldstr "MyStruct262::Method1.2009()"
		ldstr "valuetype MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0> on type MyStruct262"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0>::Method2<object>()
		ldstr "MyStruct262::Method2.2010<System.Object>()"
		ldstr "valuetype MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0> on type MyStruct262"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0>::Method3<object>()
		ldstr "MyStruct262::Method3.2012<System.Object>()"
		ldstr "valuetype MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0> on type MyStruct262"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0>::ClassMethod529()
		ldstr "MyStruct262::ClassMethod529.2014()"
		ldstr "valuetype MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0> on type MyStruct262"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0>::ClassMethod530()
		ldstr "MyStruct262::ClassMethod530.2015()"
		ldstr "valuetype MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0> on type MyStruct262"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0>::ClassMethod531<object>()
		ldstr "MyStruct262::ClassMethod531.2016<System.Object>()"
		ldstr "valuetype MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0> on type MyStruct262"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0>::ClassMethod532<object>()
		ldstr "MyStruct262::ClassMethod532.2017<System.Object>()"
		ldstr "valuetype MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0> on type MyStruct262"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup ldnull call instance bool valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0>::Equals(object) pop
		dup call instance int32 valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0>::GetHashCode() pop
		dup call instance string valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0>::ToString() pop
		pop
		ldloc V_3
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0>
		dup
		callvirt instance string class Generated212.IBase1`1<class Generated212.BaseClass0>::Method4()
		ldstr "MyStruct262::Method4.MI.2003()"
		ldstr "class IBase1`1<class Generated212.BaseClass0> on type valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated212.IBase1`1<class Generated212.BaseClass0>::Method5()
		ldstr "MyStruct262::Method5.MI.2005()"
		ldstr "class IBase1`1<class Generated212.BaseClass0> on type valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated212.IBase1`1<class Generated212.BaseClass0>::Method6<object>()
		ldstr "MyStruct262::Method6.2006<System.Object>()"
		ldstr "class IBase1`1<class Generated212.BaseClass0> on type valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc V_3
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0>
		dup
		callvirt instance string Generated212.IBase0::Method0()
		ldstr "MyStruct262::Method0.MI.2008()"
		ldstr "IBase0 on type valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated212.IBase0::Method1()
		ldstr "MyStruct262::Method1.2009()"
		ldstr "IBase0 on type valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated212.IBase0::Method2<object>()
		ldstr "MyStruct262::Method2.MI.2011<System.Object>()"
		ldstr "IBase0 on type valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated212.IBase0::Method3<object>()
		ldstr "MyStruct262::Method3.MI.2013<System.Object>()"
		ldstr "IBase0 on type valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		.locals init (valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1> V_4)
		ldloca V_4
		initobj valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1>
		ldloca V_4
		dup
		call instance string valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1>::Method4()
		ldstr "MyStruct262::Method4.2002()"
		ldstr "valuetype MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1> on type MyStruct262"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1>::Method5()
		ldstr "MyStruct262::Method5.2004()"
		ldstr "valuetype MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1> on type MyStruct262"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1>::Method6<object>()
		ldstr "MyStruct262::Method6.2006<System.Object>()"
		ldstr "valuetype MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1> on type MyStruct262"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1>::Method0()
		ldstr "MyStruct262::Method0.2007()"
		ldstr "valuetype MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1> on type MyStruct262"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1>::Method1()
		ldstr "MyStruct262::Method1.2009()"
		ldstr "valuetype MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1> on type MyStruct262"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1>::Method2<object>()
		ldstr "MyStruct262::Method2.2010<System.Object>()"
		ldstr "valuetype MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1> on type MyStruct262"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1>::Method3<object>()
		ldstr "MyStruct262::Method3.2012<System.Object>()"
		ldstr "valuetype MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1> on type MyStruct262"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1>::ClassMethod529()
		ldstr "MyStruct262::ClassMethod529.2014()"
		ldstr "valuetype MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1> on type MyStruct262"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1>::ClassMethod530()
		ldstr "MyStruct262::ClassMethod530.2015()"
		ldstr "valuetype MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1> on type MyStruct262"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1>::ClassMethod531<object>()
		ldstr "MyStruct262::ClassMethod531.2016<System.Object>()"
		ldstr "valuetype MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1> on type MyStruct262"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1>::ClassMethod532<object>()
		ldstr "MyStruct262::ClassMethod532.2017<System.Object>()"
		ldstr "valuetype MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1> on type MyStruct262"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup ldnull call instance bool valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1>::Equals(object) pop
		dup call instance int32 valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1>::GetHashCode() pop
		dup call instance string valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1>::ToString() pop
		pop
		ldloc V_4
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1>
		dup
		callvirt instance string class Generated212.IBase1`1<class Generated212.BaseClass0>::Method4()
		ldstr "MyStruct262::Method4.MI.2003()"
		ldstr "class IBase1`1<class Generated212.BaseClass0> on type valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated212.IBase1`1<class Generated212.BaseClass0>::Method5()
		ldstr "MyStruct262::Method5.MI.2005()"
		ldstr "class IBase1`1<class Generated212.BaseClass0> on type valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated212.IBase1`1<class Generated212.BaseClass0>::Method6<object>()
		ldstr "MyStruct262::Method6.2006<System.Object>()"
		ldstr "class IBase1`1<class Generated212.BaseClass0> on type valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc V_4
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1>
		dup
		callvirt instance string Generated212.IBase0::Method0()
		ldstr "MyStruct262::Method0.MI.2008()"
		ldstr "IBase0 on type valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated212.IBase0::Method1()
		ldstr "MyStruct262::Method1.2009()"
		ldstr "IBase0 on type valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated212.IBase0::Method2<object>()
		ldstr "MyStruct262::Method2.MI.2011<System.Object>()"
		ldstr "IBase0 on type valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated212.IBase0::Method3<object>()
		ldstr "MyStruct262::Method3.MI.2013<System.Object>()"
		ldstr "IBase0 on type valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1>"
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
		.locals init (valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0> V_5)
		ldloca V_5
		initobj valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0>
		.try {  ldloc V_5
				ldstr "MyStruct262::Method4.MI.2003()#MyStruct262::Method5.MI.2005()#MyStruct262::Method6.2006<System.Object>()#"
				call void Generated212.Generated212::M.IBase1.T<class Generated212.BaseClass0,valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0>>(!!1,string) leave.s LV0
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV0} LV0:
		.try {  ldloc V_5
				ldstr "MyStruct262::Method4.MI.2003()#MyStruct262::Method5.MI.2005()#MyStruct262::Method6.2006<System.Object>()#"
				call void Generated212.Generated212::M.IBase1.A<valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0>>(!!0,string) leave.s LV1
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV1} LV1:
		.try {  ldloc V_5
				ldstr "MyStruct262::Method0.MI.2008()#MyStruct262::Method1.2009()#MyStruct262::Method2.MI.2011<System.Object>()#MyStruct262::Method3.MI.2013<System.Object>()#"
				call void Generated212.Generated212::M.IBase0<valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0>>(!!0,string) leave.s LV2
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV2} LV2:
		.locals init (valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1> V_6)
		ldloca V_6
		initobj valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1>
		.try {  ldloc V_6
				ldstr "MyStruct262::Method4.MI.2003()#MyStruct262::Method5.MI.2005()#MyStruct262::Method6.2006<System.Object>()#"
				call void Generated212.Generated212::M.IBase1.T<class Generated212.BaseClass0,valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1>>(!!1,string) leave.s LV3
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV3} LV3:
		.try {  ldloc V_6
				ldstr "MyStruct262::Method4.MI.2003()#MyStruct262::Method5.MI.2005()#MyStruct262::Method6.2006<System.Object>()#"
				call void Generated212.Generated212::M.IBase1.A<valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1>>(!!0,string) leave.s LV4
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV4} LV4:
		.try {  ldloc V_6
				ldstr "MyStruct262::Method0.MI.2008()#MyStruct262::Method1.2009()#MyStruct262::Method2.MI.2011<System.Object>()#MyStruct262::Method3.MI.2013<System.Object>()#"
				call void Generated212.Generated212::M.IBase0<valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1>>(!!0,string) leave.s LV5
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV5} LV5:
		.locals init (valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0> V_7)
		ldloca V_7
		initobj valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0>
		.try {  ldloc V_7
				ldstr "MyStruct262::Method4.MI.2003()#MyStruct262::Method5.MI.2005()#MyStruct262::Method6.2006<System.Object>()#"
				call void Generated212.Generated212::M.IBase1.T<class Generated212.BaseClass0,valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0>>(!!1,string) leave.s LV6
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV6} LV6:
		.try {  ldloc V_7
				ldstr "MyStruct262::Method4.MI.2003()#MyStruct262::Method5.MI.2005()#MyStruct262::Method6.2006<System.Object>()#"
				call void Generated212.Generated212::M.IBase1.A<valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0>>(!!0,string) leave.s LV7
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV7} LV7:
		.try {  ldloc V_7
				ldstr "MyStruct262::Method0.MI.2008()#MyStruct262::Method1.2009()#MyStruct262::Method2.MI.2011<System.Object>()#MyStruct262::Method3.MI.2013<System.Object>()#"
				call void Generated212.Generated212::M.IBase0<valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0>>(!!0,string) leave.s LV8
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV8} LV8:
		.locals init (valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1> V_8)
		ldloca V_8
		initobj valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1>
		.try {  ldloc V_8
				ldstr "MyStruct262::Method4.MI.2003()#MyStruct262::Method5.MI.2005()#MyStruct262::Method6.2006<System.Object>()#"
				call void Generated212.Generated212::M.IBase1.T<class Generated212.BaseClass0,valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1>>(!!1,string) leave.s LV9
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV9} LV9:
		.try {  ldloc V_8
				ldstr "MyStruct262::Method4.MI.2003()#MyStruct262::Method5.MI.2005()#MyStruct262::Method6.2006<System.Object>()#"
				call void Generated212.Generated212::M.IBase1.A<valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1>>(!!0,string) leave.s LV10
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV10} LV10:
		.try {  ldloc V_8
				ldstr "MyStruct262::Method0.MI.2008()#MyStruct262::Method1.2009()#MyStruct262::Method2.MI.2011<System.Object>()#MyStruct262::Method3.MI.2013<System.Object>()#"
				call void Generated212.Generated212::M.IBase0<valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1>>(!!0,string) leave.s LV11
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
		.locals init (valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0> V_9)
		ldloca V_9
		initobj valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0>
		.try {  ldloc V_9
				ldstr "MyStruct262::Method4.MI.2003()#" +
				      "MyStruct262::Method5.MI.2005()#" +
				      "MyStruct262::Method6.2006<System.Object>()#" +
				      "MyStruct262::Method0.MI.2008()#" +
				      "MyStruct262::Method1.2009()#" +
				      "MyStruct262::Method2.MI.2011<System.Object>()#" +
				      "MyStruct262::Method3.MI.2013<System.Object>()#"
				call void Generated212.Generated212::M.MyStruct262.T.T<class Generated212.BaseClass0,class Generated212.BaseClass0,valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0>>(!!2,string) leave.s LV0
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV0} LV0:
		.try {  ldloc V_9
				ldstr "MyStruct262::Method4.MI.2003()#" +
				      "MyStruct262::Method5.MI.2005()#" +
				      "MyStruct262::Method6.2006<System.Object>()#" +
				      "MyStruct262::Method0.MI.2008()#" +
				      "MyStruct262::Method1.2009()#" +
				      "MyStruct262::Method2.MI.2011<System.Object>()#" +
				      "MyStruct262::Method3.MI.2013<System.Object>()#"
				call void Generated212.Generated212::M.MyStruct262.A.T<class Generated212.BaseClass0,valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0>>(!!1,string) leave.s LV1
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV1} LV1:
		.try {  ldloc V_9
				ldstr "MyStruct262::Method4.MI.2003()#" +
				      "MyStruct262::Method5.MI.2005()#" +
				      "MyStruct262::Method6.2006<System.Object>()#" +
				      "MyStruct262::Method0.MI.2008()#" +
				      "MyStruct262::Method1.2009()#" +
				      "MyStruct262::Method2.MI.2011<System.Object>()#" +
				      "MyStruct262::Method3.MI.2013<System.Object>()#"
				call void Generated212.Generated212::M.MyStruct262.A.A<valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0>>(!!0,string) leave.s LV2
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV2} LV2:
		.locals init (valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1> V_10)
		ldloca V_10
		initobj valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1>
		.try {  ldloc V_10
				ldstr "MyStruct262::Method4.MI.2003()#" +
				      "MyStruct262::Method5.MI.2005()#" +
				      "MyStruct262::Method6.2006<System.Object>()#" +
				      "MyStruct262::Method0.MI.2008()#" +
				      "MyStruct262::Method1.2009()#" +
				      "MyStruct262::Method2.MI.2011<System.Object>()#" +
				      "MyStruct262::Method3.MI.2013<System.Object>()#"
				call void Generated212.Generated212::M.MyStruct262.T.T<class Generated212.BaseClass0,class Generated212.BaseClass1,valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1>>(!!2,string) leave.s LV3
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV3} LV3:
		.try {  ldloc V_10
				ldstr "MyStruct262::Method4.MI.2003()#" +
				      "MyStruct262::Method5.MI.2005()#" +
				      "MyStruct262::Method6.2006<System.Object>()#" +
				      "MyStruct262::Method0.MI.2008()#" +
				      "MyStruct262::Method1.2009()#" +
				      "MyStruct262::Method2.MI.2011<System.Object>()#" +
				      "MyStruct262::Method3.MI.2013<System.Object>()#"
				call void Generated212.Generated212::M.MyStruct262.A.T<class Generated212.BaseClass1,valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1>>(!!1,string) leave.s LV4
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV4} LV4:
		.try {  ldloc V_10
				ldstr "MyStruct262::Method4.MI.2003()#" +
				      "MyStruct262::Method5.MI.2005()#" +
				      "MyStruct262::Method6.2006<System.Object>()#" +
				      "MyStruct262::Method0.MI.2008()#" +
				      "MyStruct262::Method1.2009()#" +
				      "MyStruct262::Method2.MI.2011<System.Object>()#" +
				      "MyStruct262::Method3.MI.2013<System.Object>()#"
				call void Generated212.Generated212::M.MyStruct262.A.B<valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1>>(!!0,string) leave.s LV5
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV5} LV5:
		.locals init (valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0> V_11)
		ldloca V_11
		initobj valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0>
		.try {  ldloc V_11
				ldstr "MyStruct262::Method4.MI.2003()#" +
				      "MyStruct262::Method5.MI.2005()#" +
				      "MyStruct262::Method6.2006<System.Object>()#" +
				      "MyStruct262::Method0.MI.2008()#" +
				      "MyStruct262::Method1.2009()#" +
				      "MyStruct262::Method2.MI.2011<System.Object>()#" +
				      "MyStruct262::Method3.MI.2013<System.Object>()#"
				call void Generated212.Generated212::M.MyStruct262.T.T<class Generated212.BaseClass1,class Generated212.BaseClass0,valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0>>(!!2,string) leave.s LV6
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV6} LV6:
		.try {  ldloc V_11
				ldstr "MyStruct262::Method4.MI.2003()#" +
				      "MyStruct262::Method5.MI.2005()#" +
				      "MyStruct262::Method6.2006<System.Object>()#" +
				      "MyStruct262::Method0.MI.2008()#" +
				      "MyStruct262::Method1.2009()#" +
				      "MyStruct262::Method2.MI.2011<System.Object>()#" +
				      "MyStruct262::Method3.MI.2013<System.Object>()#"
				call void Generated212.Generated212::M.MyStruct262.B.T<class Generated212.BaseClass0,valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0>>(!!1,string) leave.s LV7
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV7} LV7:
		.try {  ldloc V_11
				ldstr "MyStruct262::Method4.MI.2003()#" +
				      "MyStruct262::Method5.MI.2005()#" +
				      "MyStruct262::Method6.2006<System.Object>()#" +
				      "MyStruct262::Method0.MI.2008()#" +
				      "MyStruct262::Method1.2009()#" +
				      "MyStruct262::Method2.MI.2011<System.Object>()#" +
				      "MyStruct262::Method3.MI.2013<System.Object>()#"
				call void Generated212.Generated212::M.MyStruct262.B.A<valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0>>(!!0,string) leave.s LV8
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV8} LV8:
		.locals init (valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1> V_12)
		ldloca V_12
		initobj valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1>
		.try {  ldloc V_12
				ldstr "MyStruct262::Method4.MI.2003()#" +
				      "MyStruct262::Method5.MI.2005()#" +
				      "MyStruct262::Method6.2006<System.Object>()#" +
				      "MyStruct262::Method0.MI.2008()#" +
				      "MyStruct262::Method1.2009()#" +
				      "MyStruct262::Method2.MI.2011<System.Object>()#" +
				      "MyStruct262::Method3.MI.2013<System.Object>()#"
				call void Generated212.Generated212::M.MyStruct262.T.T<class Generated212.BaseClass1,class Generated212.BaseClass1,valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1>>(!!2,string) leave.s LV9
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV9} LV9:
		.try {  ldloc V_12
				ldstr "MyStruct262::Method4.MI.2003()#" +
				      "MyStruct262::Method5.MI.2005()#" +
				      "MyStruct262::Method6.2006<System.Object>()#" +
				      "MyStruct262::Method0.MI.2008()#" +
				      "MyStruct262::Method1.2009()#" +
				      "MyStruct262::Method2.MI.2011<System.Object>()#" +
				      "MyStruct262::Method3.MI.2013<System.Object>()#"
				call void Generated212.Generated212::M.MyStruct262.B.T<class Generated212.BaseClass1,valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1>>(!!1,string) leave.s LV10
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV10} LV10:
		.try {  ldloc V_12
				ldstr "MyStruct262::Method4.MI.2003()#" +
				      "MyStruct262::Method5.MI.2005()#" +
				      "MyStruct262::Method6.2006<System.Object>()#" +
				      "MyStruct262::Method0.MI.2008()#" +
				      "MyStruct262::Method1.2009()#" +
				      "MyStruct262::Method2.MI.2011<System.Object>()#" +
				      "MyStruct262::Method3.MI.2013<System.Object>()#"
				call void Generated212.Generated212::M.MyStruct262.B.B<valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1>>(!!0,string) leave.s LV11
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV11} LV11:
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
		.locals init (valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0> V_13)
		ldloca V_13
		initobj valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0>
		ldloc V_13
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0>
		ldloc V_13
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0>
		ldvirtftn instance string valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0>::Method4()
		calli default string(object)
		ldstr "MyStruct262::Method4.2002()"
		ldstr "valuetype MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0> on type valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_13
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0>
		ldloc V_13
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0>
		ldvirtftn instance string valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0>::Method5()
		calli default string(object)
		ldstr "MyStruct262::Method5.2004()"
		ldstr "valuetype MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0> on type valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_13
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0>
		ldloc V_13
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0>
		ldvirtftn instance string valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0>::Method6<object>()
		calli default string(object)
		ldstr "MyStruct262::Method6.2006<System.Object>()"
		ldstr "valuetype MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0> on type valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_13
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0>
		ldloc V_13
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0>
		ldvirtftn instance string valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0>::Method0()
		calli default string(object)
		ldstr "MyStruct262::Method0.2007()"
		ldstr "valuetype MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0> on type valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_13
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0>
		ldloc V_13
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0>
		ldvirtftn instance string valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0>::Method1()
		calli default string(object)
		ldstr "MyStruct262::Method1.2009()"
		ldstr "valuetype MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0> on type valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_13
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0>
		ldloc V_13
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0>
		ldvirtftn instance string valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0>::Method2<object>()
		calli default string(object)
		ldstr "MyStruct262::Method2.2010<System.Object>()"
		ldstr "valuetype MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0> on type valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_13
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0>
		ldloc V_13
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0>
		ldvirtftn instance string valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0>::Method3<object>()
		calli default string(object)
		ldstr "MyStruct262::Method3.2012<System.Object>()"
		ldstr "valuetype MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0> on type valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_13
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0>
		ldloc V_13
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0>
		ldvirtftn instance string valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0>::ClassMethod529()
		calli default string(object)
		ldstr "MyStruct262::ClassMethod529.2014()"
		ldstr "valuetype MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0> on type valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_13
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0>
		ldloc V_13
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0>
		ldvirtftn instance string valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0>::ClassMethod530()
		calli default string(object)
		ldstr "MyStruct262::ClassMethod530.2015()"
		ldstr "valuetype MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0> on type valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_13
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0>
		ldloc V_13
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0>
		ldvirtftn instance string valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0>::ClassMethod531<object>()
		calli default string(object)
		ldstr "MyStruct262::ClassMethod531.2016<System.Object>()"
		ldstr "valuetype MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0> on type valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_13
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0>
		ldloc V_13
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0>
		ldvirtftn instance string valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0>::ClassMethod532<object>()
		calli default string(object)
		ldstr "MyStruct262::ClassMethod532.2017<System.Object>()"
		ldstr "valuetype MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0> on type valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_13 box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0> ldnull 
		ldloc V_13 box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0>
		ldvirtftn instance bool valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0>::Equals(object) calli default bool(object,object) pop
		ldloc V_13 box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0>
		ldloc V_13 box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0>
		ldvirtftn instance int32 valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0>::GetHashCode() calli default int32(object) pop
		ldloc V_13 box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0>
		ldloc V_13 box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0>
		ldvirtftn instance string valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0>::ToString() calli default string(object) pop
		ldloc V_13
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0>
		ldloc V_13
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0>
		ldvirtftn instance string class Generated212.IBase1`1<class Generated212.BaseClass0>::Method4()
		calli default string(object)
		ldstr "MyStruct262::Method4.MI.2003()"
		ldstr "class IBase1`1<class Generated212.BaseClass0> on type valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_13
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0>
		ldloc V_13
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0>
		ldvirtftn instance string class Generated212.IBase1`1<class Generated212.BaseClass0>::Method5()
		calli default string(object)
		ldstr "MyStruct262::Method5.MI.2005()"
		ldstr "class IBase1`1<class Generated212.BaseClass0> on type valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_13
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0>
		ldloc V_13
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0>
		ldvirtftn instance string class Generated212.IBase1`1<class Generated212.BaseClass0>::Method6<object>()
		calli default string(object)
		ldstr "MyStruct262::Method6.2006<System.Object>()"
		ldstr "class IBase1`1<class Generated212.BaseClass0> on type valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_13
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0>
		ldloc V_13
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0>
		ldvirtftn instance string Generated212.IBase0::Method0()
		calli default string(object)
		ldstr "MyStruct262::Method0.MI.2008()"
		ldstr "IBase0 on type valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_13
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0>
		ldloc V_13
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0>
		ldvirtftn instance string Generated212.IBase0::Method1()
		calli default string(object)
		ldstr "MyStruct262::Method1.2009()"
		ldstr "IBase0 on type valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_13
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0>
		ldloc V_13
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0>
		ldvirtftn instance string Generated212.IBase0::Method2<object>()
		calli default string(object)
		ldstr "MyStruct262::Method2.MI.2011<System.Object>()"
		ldstr "IBase0 on type valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_13
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0>
		ldloc V_13
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0>
		ldvirtftn instance string Generated212.IBase0::Method3<object>()
		calli default string(object)
		ldstr "MyStruct262::Method3.MI.2013<System.Object>()"
		ldstr "IBase0 on type valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		.locals init (valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1> V_14)
		ldloca V_14
		initobj valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1>
		ldloc V_14
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1>
		ldloc V_14
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1>
		ldvirtftn instance string valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1>::Method4()
		calli default string(object)
		ldstr "MyStruct262::Method4.2002()"
		ldstr "valuetype MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1> on type valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_14
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1>
		ldloc V_14
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1>
		ldvirtftn instance string valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1>::Method5()
		calli default string(object)
		ldstr "MyStruct262::Method5.2004()"
		ldstr "valuetype MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1> on type valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_14
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1>
		ldloc V_14
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1>
		ldvirtftn instance string valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1>::Method6<object>()
		calli default string(object)
		ldstr "MyStruct262::Method6.2006<System.Object>()"
		ldstr "valuetype MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1> on type valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_14
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1>
		ldloc V_14
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1>
		ldvirtftn instance string valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1>::Method0()
		calli default string(object)
		ldstr "MyStruct262::Method0.2007()"
		ldstr "valuetype MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1> on type valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_14
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1>
		ldloc V_14
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1>
		ldvirtftn instance string valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1>::Method1()
		calli default string(object)
		ldstr "MyStruct262::Method1.2009()"
		ldstr "valuetype MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1> on type valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_14
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1>
		ldloc V_14
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1>
		ldvirtftn instance string valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1>::Method2<object>()
		calli default string(object)
		ldstr "MyStruct262::Method2.2010<System.Object>()"
		ldstr "valuetype MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1> on type valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_14
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1>
		ldloc V_14
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1>
		ldvirtftn instance string valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1>::Method3<object>()
		calli default string(object)
		ldstr "MyStruct262::Method3.2012<System.Object>()"
		ldstr "valuetype MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1> on type valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_14
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1>
		ldloc V_14
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1>
		ldvirtftn instance string valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1>::ClassMethod529()
		calli default string(object)
		ldstr "MyStruct262::ClassMethod529.2014()"
		ldstr "valuetype MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1> on type valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_14
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1>
		ldloc V_14
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1>
		ldvirtftn instance string valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1>::ClassMethod530()
		calli default string(object)
		ldstr "MyStruct262::ClassMethod530.2015()"
		ldstr "valuetype MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1> on type valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_14
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1>
		ldloc V_14
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1>
		ldvirtftn instance string valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1>::ClassMethod531<object>()
		calli default string(object)
		ldstr "MyStruct262::ClassMethod531.2016<System.Object>()"
		ldstr "valuetype MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1> on type valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_14
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1>
		ldloc V_14
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1>
		ldvirtftn instance string valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1>::ClassMethod532<object>()
		calli default string(object)
		ldstr "MyStruct262::ClassMethod532.2017<System.Object>()"
		ldstr "valuetype MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1> on type valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_14 box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1> ldnull 
		ldloc V_14 box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1>
		ldvirtftn instance bool valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1>::Equals(object) calli default bool(object,object) pop
		ldloc V_14 box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1>
		ldloc V_14 box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1>
		ldvirtftn instance int32 valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1>::GetHashCode() calli default int32(object) pop
		ldloc V_14 box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1>
		ldloc V_14 box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1>
		ldvirtftn instance string valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1>::ToString() calli default string(object) pop
		ldloc V_14
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1>
		ldloc V_14
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1>
		ldvirtftn instance string class Generated212.IBase1`1<class Generated212.BaseClass0>::Method4()
		calli default string(object)
		ldstr "MyStruct262::Method4.MI.2003()"
		ldstr "class IBase1`1<class Generated212.BaseClass0> on type valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_14
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1>
		ldloc V_14
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1>
		ldvirtftn instance string class Generated212.IBase1`1<class Generated212.BaseClass0>::Method5()
		calli default string(object)
		ldstr "MyStruct262::Method5.MI.2005()"
		ldstr "class IBase1`1<class Generated212.BaseClass0> on type valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_14
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1>
		ldloc V_14
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1>
		ldvirtftn instance string class Generated212.IBase1`1<class Generated212.BaseClass0>::Method6<object>()
		calli default string(object)
		ldstr "MyStruct262::Method6.2006<System.Object>()"
		ldstr "class IBase1`1<class Generated212.BaseClass0> on type valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_14
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1>
		ldloc V_14
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1>
		ldvirtftn instance string Generated212.IBase0::Method0()
		calli default string(object)
		ldstr "MyStruct262::Method0.MI.2008()"
		ldstr "IBase0 on type valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_14
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1>
		ldloc V_14
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1>
		ldvirtftn instance string Generated212.IBase0::Method1()
		calli default string(object)
		ldstr "MyStruct262::Method1.2009()"
		ldstr "IBase0 on type valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_14
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1>
		ldloc V_14
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1>
		ldvirtftn instance string Generated212.IBase0::Method2<object>()
		calli default string(object)
		ldstr "MyStruct262::Method2.MI.2011<System.Object>()"
		ldstr "IBase0 on type valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_14
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1>
		ldloc V_14
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1>
		ldvirtftn instance string Generated212.IBase0::Method3<object>()
		calli default string(object)
		ldstr "MyStruct262::Method3.MI.2013<System.Object>()"
		ldstr "IBase0 on type valuetype Generated212.MyStruct262`2<class Generated212.BaseClass0,class Generated212.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		.locals init (valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0> V_15)
		ldloca V_15
		initobj valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0>
		ldloc V_15
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0>
		ldloc V_15
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0>
		ldvirtftn instance string valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0>::Method4()
		calli default string(object)
		ldstr "MyStruct262::Method4.2002()"
		ldstr "valuetype MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0> on type valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_15
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0>
		ldloc V_15
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0>
		ldvirtftn instance string valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0>::Method5()
		calli default string(object)
		ldstr "MyStruct262::Method5.2004()"
		ldstr "valuetype MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0> on type valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_15
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0>
		ldloc V_15
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0>
		ldvirtftn instance string valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0>::Method6<object>()
		calli default string(object)
		ldstr "MyStruct262::Method6.2006<System.Object>()"
		ldstr "valuetype MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0> on type valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_15
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0>
		ldloc V_15
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0>
		ldvirtftn instance string valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0>::Method0()
		calli default string(object)
		ldstr "MyStruct262::Method0.2007()"
		ldstr "valuetype MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0> on type valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_15
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0>
		ldloc V_15
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0>
		ldvirtftn instance string valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0>::Method1()
		calli default string(object)
		ldstr "MyStruct262::Method1.2009()"
		ldstr "valuetype MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0> on type valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_15
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0>
		ldloc V_15
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0>
		ldvirtftn instance string valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0>::Method2<object>()
		calli default string(object)
		ldstr "MyStruct262::Method2.2010<System.Object>()"
		ldstr "valuetype MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0> on type valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_15
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0>
		ldloc V_15
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0>
		ldvirtftn instance string valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0>::Method3<object>()
		calli default string(object)
		ldstr "MyStruct262::Method3.2012<System.Object>()"
		ldstr "valuetype MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0> on type valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_15
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0>
		ldloc V_15
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0>
		ldvirtftn instance string valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0>::ClassMethod529()
		calli default string(object)
		ldstr "MyStruct262::ClassMethod529.2014()"
		ldstr "valuetype MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0> on type valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_15
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0>
		ldloc V_15
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0>
		ldvirtftn instance string valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0>::ClassMethod530()
		calli default string(object)
		ldstr "MyStruct262::ClassMethod530.2015()"
		ldstr "valuetype MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0> on type valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_15
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0>
		ldloc V_15
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0>
		ldvirtftn instance string valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0>::ClassMethod531<object>()
		calli default string(object)
		ldstr "MyStruct262::ClassMethod531.2016<System.Object>()"
		ldstr "valuetype MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0> on type valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_15
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0>
		ldloc V_15
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0>
		ldvirtftn instance string valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0>::ClassMethod532<object>()
		calli default string(object)
		ldstr "MyStruct262::ClassMethod532.2017<System.Object>()"
		ldstr "valuetype MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0> on type valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_15 box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0> ldnull 
		ldloc V_15 box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0>
		ldvirtftn instance bool valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0>::Equals(object) calli default bool(object,object) pop
		ldloc V_15 box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0>
		ldloc V_15 box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0>
		ldvirtftn instance int32 valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0>::GetHashCode() calli default int32(object) pop
		ldloc V_15 box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0>
		ldloc V_15 box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0>
		ldvirtftn instance string valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0>::ToString() calli default string(object) pop
		ldloc V_15
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0>
		ldloc V_15
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0>
		ldvirtftn instance string class Generated212.IBase1`1<class Generated212.BaseClass0>::Method4()
		calli default string(object)
		ldstr "MyStruct262::Method4.MI.2003()"
		ldstr "class IBase1`1<class Generated212.BaseClass0> on type valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_15
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0>
		ldloc V_15
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0>
		ldvirtftn instance string class Generated212.IBase1`1<class Generated212.BaseClass0>::Method5()
		calli default string(object)
		ldstr "MyStruct262::Method5.MI.2005()"
		ldstr "class IBase1`1<class Generated212.BaseClass0> on type valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_15
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0>
		ldloc V_15
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0>
		ldvirtftn instance string class Generated212.IBase1`1<class Generated212.BaseClass0>::Method6<object>()
		calli default string(object)
		ldstr "MyStruct262::Method6.2006<System.Object>()"
		ldstr "class IBase1`1<class Generated212.BaseClass0> on type valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_15
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0>
		ldloc V_15
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0>
		ldvirtftn instance string Generated212.IBase0::Method0()
		calli default string(object)
		ldstr "MyStruct262::Method0.MI.2008()"
		ldstr "IBase0 on type valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_15
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0>
		ldloc V_15
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0>
		ldvirtftn instance string Generated212.IBase0::Method1()
		calli default string(object)
		ldstr "MyStruct262::Method1.2009()"
		ldstr "IBase0 on type valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_15
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0>
		ldloc V_15
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0>
		ldvirtftn instance string Generated212.IBase0::Method2<object>()
		calli default string(object)
		ldstr "MyStruct262::Method2.MI.2011<System.Object>()"
		ldstr "IBase0 on type valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_15
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0>
		ldloc V_15
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0>
		ldvirtftn instance string Generated212.IBase0::Method3<object>()
		calli default string(object)
		ldstr "MyStruct262::Method3.MI.2013<System.Object>()"
		ldstr "IBase0 on type valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		.locals init (valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1> V_16)
		ldloca V_16
		initobj valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1>
		ldloc V_16
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1>
		ldloc V_16
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1>
		ldvirtftn instance string valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1>::Method4()
		calli default string(object)
		ldstr "MyStruct262::Method4.2002()"
		ldstr "valuetype MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1> on type valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_16
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1>
		ldloc V_16
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1>
		ldvirtftn instance string valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1>::Method5()
		calli default string(object)
		ldstr "MyStruct262::Method5.2004()"
		ldstr "valuetype MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1> on type valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_16
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1>
		ldloc V_16
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1>
		ldvirtftn instance string valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1>::Method6<object>()
		calli default string(object)
		ldstr "MyStruct262::Method6.2006<System.Object>()"
		ldstr "valuetype MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1> on type valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_16
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1>
		ldloc V_16
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1>
		ldvirtftn instance string valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1>::Method0()
		calli default string(object)
		ldstr "MyStruct262::Method0.2007()"
		ldstr "valuetype MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1> on type valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_16
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1>
		ldloc V_16
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1>
		ldvirtftn instance string valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1>::Method1()
		calli default string(object)
		ldstr "MyStruct262::Method1.2009()"
		ldstr "valuetype MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1> on type valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_16
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1>
		ldloc V_16
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1>
		ldvirtftn instance string valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1>::Method2<object>()
		calli default string(object)
		ldstr "MyStruct262::Method2.2010<System.Object>()"
		ldstr "valuetype MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1> on type valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_16
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1>
		ldloc V_16
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1>
		ldvirtftn instance string valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1>::Method3<object>()
		calli default string(object)
		ldstr "MyStruct262::Method3.2012<System.Object>()"
		ldstr "valuetype MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1> on type valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_16
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1>
		ldloc V_16
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1>
		ldvirtftn instance string valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1>::ClassMethod529()
		calli default string(object)
		ldstr "MyStruct262::ClassMethod529.2014()"
		ldstr "valuetype MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1> on type valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_16
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1>
		ldloc V_16
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1>
		ldvirtftn instance string valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1>::ClassMethod530()
		calli default string(object)
		ldstr "MyStruct262::ClassMethod530.2015()"
		ldstr "valuetype MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1> on type valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_16
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1>
		ldloc V_16
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1>
		ldvirtftn instance string valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1>::ClassMethod531<object>()
		calli default string(object)
		ldstr "MyStruct262::ClassMethod531.2016<System.Object>()"
		ldstr "valuetype MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1> on type valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_16
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1>
		ldloc V_16
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1>
		ldvirtftn instance string valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1>::ClassMethod532<object>()
		calli default string(object)
		ldstr "MyStruct262::ClassMethod532.2017<System.Object>()"
		ldstr "valuetype MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1> on type valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_16 box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1> ldnull 
		ldloc V_16 box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1>
		ldvirtftn instance bool valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1>::Equals(object) calli default bool(object,object) pop
		ldloc V_16 box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1>
		ldloc V_16 box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1>
		ldvirtftn instance int32 valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1>::GetHashCode() calli default int32(object) pop
		ldloc V_16 box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1>
		ldloc V_16 box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1>
		ldvirtftn instance string valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1>::ToString() calli default string(object) pop
		ldloc V_16
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1>
		ldloc V_16
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1>
		ldvirtftn instance string class Generated212.IBase1`1<class Generated212.BaseClass0>::Method4()
		calli default string(object)
		ldstr "MyStruct262::Method4.MI.2003()"
		ldstr "class IBase1`1<class Generated212.BaseClass0> on type valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_16
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1>
		ldloc V_16
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1>
		ldvirtftn instance string class Generated212.IBase1`1<class Generated212.BaseClass0>::Method5()
		calli default string(object)
		ldstr "MyStruct262::Method5.MI.2005()"
		ldstr "class IBase1`1<class Generated212.BaseClass0> on type valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_16
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1>
		ldloc V_16
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1>
		ldvirtftn instance string class Generated212.IBase1`1<class Generated212.BaseClass0>::Method6<object>()
		calli default string(object)
		ldstr "MyStruct262::Method6.2006<System.Object>()"
		ldstr "class IBase1`1<class Generated212.BaseClass0> on type valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_16
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1>
		ldloc V_16
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1>
		ldvirtftn instance string Generated212.IBase0::Method0()
		calli default string(object)
		ldstr "MyStruct262::Method0.MI.2008()"
		ldstr "IBase0 on type valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_16
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1>
		ldloc V_16
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1>
		ldvirtftn instance string Generated212.IBase0::Method1()
		calli default string(object)
		ldstr "MyStruct262::Method1.2009()"
		ldstr "IBase0 on type valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_16
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1>
		ldloc V_16
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1>
		ldvirtftn instance string Generated212.IBase0::Method2<object>()
		calli default string(object)
		ldstr "MyStruct262::Method2.MI.2011<System.Object>()"
		ldstr "IBase0 on type valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_16
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1>
		ldloc V_16
		box valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1>
		ldvirtftn instance string Generated212.IBase0::Method3<object>()
		calli default string(object)
		ldstr "MyStruct262::Method3.MI.2013<System.Object>()"
		ldstr "IBase0 on type valuetype Generated212.MyStruct262`2<class Generated212.BaseClass1,class Generated212.BaseClass1>"
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
		call void Generated212.Generated212::MethodCallingTest()
		call void Generated212.Generated212::ConstrainedCallsTest()
		call void Generated212.Generated212::StructConstrainedInterfaceCallsTest()
		call void Generated212.Generated212::CalliTest()
		ldc.i4 100
		ret
	}
}
