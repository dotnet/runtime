// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) .ver 4:0:0:0 }
.assembly extern TestFramework { .publickeytoken = ( B0 3F 5F 7F 11 D5 0A 3A )  }

//TYPES IN FORWARDER ASSEMBLIES:

//TEST ASSEMBLY:
.assembly extern xunit.core {}

.class public Generated201.BaseClass0 
{
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void [mscorlib]System.Object::.ctor()
		ret
	}
}
.class public Generated201.BaseClass1 
		extends Generated201.BaseClass0
{
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void Generated201.BaseClass0::.ctor()
		ret
	}
}
.class public sequential sealed Generated201.MyStruct251`2<T0, T1> 
		extends [mscorlib]System.ValueType
		implements IBase0, class Generated201.IBase2`2<class Generated201.BaseClass1,class Generated201.BaseClass1> 
{
	.pack 0
	.size 1
	.method public hidebysig newslot virtual instance string Method0() cil managed noinlining { 
		ldstr "MyStruct251::Method0.1891()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase0.Method0'() cil managed noinlining { 
		.override method instance string Generated201.IBase0::Method0()
		ldstr "MyStruct251::Method0.MI.1892()"
		ret
	}
	.method public hidebysig newslot virtual instance string Method1() cil managed noinlining { 
		ldstr "MyStruct251::Method1.1893()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase0.Method1'() cil managed noinlining { 
		.override method instance string Generated201.IBase0::Method1()
		ldstr "MyStruct251::Method1.MI.1894()"
		ret
	}
	.method public hidebysig virtual instance string Method2<M0>() cil managed noinlining { 
		ldstr "MyStruct251::Method2.1895<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase0.Method2'<M0>() cil managed noinlining { 
		.override method instance string Generated201.IBase0::Method2<[1]>()
		ldstr "MyStruct251::Method2.MI.1896<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig virtual instance string Method3<M0>() cil managed noinlining { 
		ldstr "MyStruct251::Method3.1897<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase0.Method3'<M0>() cil managed noinlining { 
		.override method instance string Generated201.IBase0::Method3<[1]>()
		ldstr "MyStruct251::Method3.MI.1898<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "MyStruct251::Method7.1899<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase2<class Generated201.BaseClass1,class Generated201.BaseClass1>.Method7'<M0>() cil managed noinlining { 
		.override method instance string class Generated201.IBase2`2<class Generated201.BaseClass1,class Generated201.BaseClass1>::Method7<[1]>()
		ldstr "MyStruct251::Method7.MI.1900<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot instance string ClassMethod508<M0>() cil managed noinlining { 
		ldstr "MyStruct251::ClassMethod508.1901<"
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
.class interface public abstract Generated201.IBase0 
{
	.method public hidebysig newslot abstract virtual instance string Method0() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method1() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method2<M0>() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method3<M0>() cil managed { }
}
.class interface public abstract Generated201.IBase2`2<+T0, -T1> 
{
	.method public hidebysig newslot abstract virtual instance string Method7<M0>() cil managed { }
}
.class public auto ansi beforefieldinit Generated201.Generated201 {
	.method static void M.BaseClass0<(Generated201.BaseClass0)W>(!!W inst, string exp) cil managed {
		.maxstack 5
		.locals init (string[] actualResults)
		ldc.i4.s    0
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.BaseClass0<(Generated201.BaseClass0)W>(!!W inst, string exp)"
		ldc.i4.s 0
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.BaseClass1<(Generated201.BaseClass1)W>(!!W inst, string exp) cil managed {
		.maxstack 5
		.locals init (string[] actualResults)
		ldc.i4.s    0
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.BaseClass1<(Generated201.BaseClass1)W>(!!W inst, string exp)"
		ldc.i4.s 0
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase0<(Generated201.IBase0)W>(!!W inst, string exp) cil managed {
		.maxstack 9
		.locals init (string[] actualResults)
		ldc.i4.s    4
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase0<(Generated201.IBase0)W>(!!W inst, string exp)"
		ldc.i4.s 4
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string Generated201.IBase0::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string Generated201.IBase0::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string Generated201.IBase0::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string Generated201.IBase0::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.T.T<T0,T1,(class Generated201.IBase2`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.T.T<T0,T1,(class Generated201.IBase2`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated201.IBase2`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.A.T<T1,(class Generated201.IBase2`2<class Generated201.BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.A.T<T1,(class Generated201.IBase2`2<class Generated201.BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated201.IBase2`2<class Generated201.BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.A.A<(class Generated201.IBase2`2<class Generated201.BaseClass0,class Generated201.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.A.A<(class Generated201.IBase2`2<class Generated201.BaseClass0,class Generated201.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated201.IBase2`2<class Generated201.BaseClass0,class Generated201.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.A.B<(class Generated201.IBase2`2<class Generated201.BaseClass0,class Generated201.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.A.B<(class Generated201.IBase2`2<class Generated201.BaseClass0,class Generated201.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated201.IBase2`2<class Generated201.BaseClass0,class Generated201.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.B.T<T1,(class Generated201.IBase2`2<class Generated201.BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.B.T<T1,(class Generated201.IBase2`2<class Generated201.BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated201.IBase2`2<class Generated201.BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.B.A<(class Generated201.IBase2`2<class Generated201.BaseClass1,class Generated201.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.B.A<(class Generated201.IBase2`2<class Generated201.BaseClass1,class Generated201.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated201.IBase2`2<class Generated201.BaseClass1,class Generated201.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.B.B<(class Generated201.IBase2`2<class Generated201.BaseClass1,class Generated201.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.B.B<(class Generated201.IBase2`2<class Generated201.BaseClass1,class Generated201.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated201.IBase2`2<class Generated201.BaseClass1,class Generated201.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.MyStruct251.T.T<T0,T1,(valuetype Generated201.MyStruct251`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 10
		.locals init (string[] actualResults)
		ldc.i4.s    5
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.MyStruct251.T.T<T0,T1,(valuetype Generated201.MyStruct251`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 5
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. valuetype Generated201.MyStruct251`2<!!T0,!!T1>
		callvirt     instance string Generated201.IBase0::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. valuetype Generated201.MyStruct251`2<!!T0,!!T1>
		callvirt     instance string Generated201.IBase0::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. valuetype Generated201.MyStruct251`2<!!T0,!!T1>
		callvirt     instance string Generated201.IBase0::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. valuetype Generated201.MyStruct251`2<!!T0,!!T1>
		callvirt     instance string Generated201.IBase0::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. valuetype Generated201.MyStruct251`2<!!T0,!!T1>
		callvirt     instance string class Generated201.IBase2`2<class Generated201.BaseClass1,class Generated201.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.MyStruct251.A.T<T1,(valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 10
		.locals init (string[] actualResults)
		ldc.i4.s    5
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.MyStruct251.A.T<T1,(valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 5
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,!!T1>
		callvirt     instance string Generated201.IBase0::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,!!T1>
		callvirt     instance string Generated201.IBase0::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,!!T1>
		callvirt     instance string Generated201.IBase0::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,!!T1>
		callvirt     instance string Generated201.IBase0::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,!!T1>
		callvirt     instance string class Generated201.IBase2`2<class Generated201.BaseClass1,class Generated201.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.MyStruct251.A.A<(valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 10
		.locals init (string[] actualResults)
		ldc.i4.s    5
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.MyStruct251.A.A<(valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 5
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass0>
		callvirt     instance string Generated201.IBase0::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass0>
		callvirt     instance string Generated201.IBase0::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass0>
		callvirt     instance string Generated201.IBase0::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass0>
		callvirt     instance string Generated201.IBase0::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass0>
		callvirt     instance string class Generated201.IBase2`2<class Generated201.BaseClass1,class Generated201.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.MyStruct251.A.B<(valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 10
		.locals init (string[] actualResults)
		ldc.i4.s    5
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.MyStruct251.A.B<(valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 5
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass1>
		callvirt     instance string Generated201.IBase0::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass1>
		callvirt     instance string Generated201.IBase0::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass1>
		callvirt     instance string Generated201.IBase0::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass1>
		callvirt     instance string Generated201.IBase0::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass1>
		callvirt     instance string class Generated201.IBase2`2<class Generated201.BaseClass1,class Generated201.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.MyStruct251.B.T<T1,(valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 10
		.locals init (string[] actualResults)
		ldc.i4.s    5
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.MyStruct251.B.T<T1,(valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 5
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,!!T1>
		callvirt     instance string Generated201.IBase0::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,!!T1>
		callvirt     instance string Generated201.IBase0::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,!!T1>
		callvirt     instance string Generated201.IBase0::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,!!T1>
		callvirt     instance string Generated201.IBase0::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,!!T1>
		callvirt     instance string class Generated201.IBase2`2<class Generated201.BaseClass1,class Generated201.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.MyStruct251.B.A<(valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 10
		.locals init (string[] actualResults)
		ldc.i4.s    5
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.MyStruct251.B.A<(valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 5
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass0>
		callvirt     instance string Generated201.IBase0::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass0>
		callvirt     instance string Generated201.IBase0::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass0>
		callvirt     instance string Generated201.IBase0::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass0>
		callvirt     instance string Generated201.IBase0::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass0>
		callvirt     instance string class Generated201.IBase2`2<class Generated201.BaseClass1,class Generated201.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.MyStruct251.B.B<(valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 10
		.locals init (string[] actualResults)
		ldc.i4.s    5
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.MyStruct251.B.B<(valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 5
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass1>
		callvirt     instance string Generated201.IBase0::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass1>
		callvirt     instance string Generated201.IBase0::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass1>
		callvirt     instance string Generated201.IBase0::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass1>
		callvirt     instance string Generated201.IBase0::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass1>
		callvirt     instance string class Generated201.IBase2`2<class Generated201.BaseClass1,class Generated201.BaseClass1>::Method7<object>()
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
		.locals init (valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass0> V_1)
		ldloca V_1
		initobj valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass0>
		ldloca V_1
		dup
		call instance string valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass0>::Method0()
		ldstr "MyStruct251::Method0.1891()"
		ldstr "valuetype MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass0> on type MyStruct251"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass0>::Method1()
		ldstr "MyStruct251::Method1.1893()"
		ldstr "valuetype MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass0> on type MyStruct251"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass0>::Method2<object>()
		ldstr "MyStruct251::Method2.1895<System.Object>()"
		ldstr "valuetype MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass0> on type MyStruct251"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass0>::Method3<object>()
		ldstr "MyStruct251::Method3.1897<System.Object>()"
		ldstr "valuetype MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass0> on type MyStruct251"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass0>::Method7<object>()
		ldstr "MyStruct251::Method7.1899<System.Object>()"
		ldstr "valuetype MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass0> on type MyStruct251"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass0>::ClassMethod508<object>()
		ldstr "MyStruct251::ClassMethod508.1901<System.Object>()"
		ldstr "valuetype MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass0> on type MyStruct251"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup ldnull call instance bool valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass0>::Equals(object) pop
		dup call instance int32 valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass0>::GetHashCode() pop
		dup call instance string valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass0>::ToString() pop
		pop
		ldloc V_1
		box valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass0>
		dup
		callvirt instance string Generated201.IBase0::Method0()
		ldstr "MyStruct251::Method0.MI.1892()"
		ldstr "IBase0 on type valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated201.IBase0::Method1()
		ldstr "MyStruct251::Method1.MI.1894()"
		ldstr "IBase0 on type valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated201.IBase0::Method2<object>()
		ldstr "MyStruct251::Method2.MI.1896<System.Object>()"
		ldstr "IBase0 on type valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated201.IBase0::Method3<object>()
		ldstr "MyStruct251::Method3.MI.1898<System.Object>()"
		ldstr "IBase0 on type valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc V_1
		box valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass0>
		dup
		callvirt instance string class Generated201.IBase2`2<class Generated201.BaseClass1,class Generated201.BaseClass1>::Method7<object>()
		ldstr "MyStruct251::Method7.MI.1900<System.Object>()"
		ldstr "class IBase2`2<class Generated201.BaseClass1,class Generated201.BaseClass1> on type valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc V_1
		box valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass0>
		dup
		callvirt instance string class Generated201.IBase2`2<class Generated201.BaseClass0,class Generated201.BaseClass1>::Method7<object>()
		ldstr "MyStruct251::Method7.MI.1900<System.Object>()"
		ldstr "class IBase2`2<class Generated201.BaseClass0,class Generated201.BaseClass1> on type valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		.locals init (valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass1> V_2)
		ldloca V_2
		initobj valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass1>
		ldloca V_2
		dup
		call instance string valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass1>::Method0()
		ldstr "MyStruct251::Method0.1891()"
		ldstr "valuetype MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass1> on type MyStruct251"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass1>::Method1()
		ldstr "MyStruct251::Method1.1893()"
		ldstr "valuetype MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass1> on type MyStruct251"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass1>::Method2<object>()
		ldstr "MyStruct251::Method2.1895<System.Object>()"
		ldstr "valuetype MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass1> on type MyStruct251"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass1>::Method3<object>()
		ldstr "MyStruct251::Method3.1897<System.Object>()"
		ldstr "valuetype MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass1> on type MyStruct251"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass1>::Method7<object>()
		ldstr "MyStruct251::Method7.1899<System.Object>()"
		ldstr "valuetype MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass1> on type MyStruct251"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass1>::ClassMethod508<object>()
		ldstr "MyStruct251::ClassMethod508.1901<System.Object>()"
		ldstr "valuetype MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass1> on type MyStruct251"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup ldnull call instance bool valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass1>::Equals(object) pop
		dup call instance int32 valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass1>::GetHashCode() pop
		dup call instance string valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass1>::ToString() pop
		pop
		ldloc V_2
		box valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass1>
		dup
		callvirt instance string Generated201.IBase0::Method0()
		ldstr "MyStruct251::Method0.MI.1892()"
		ldstr "IBase0 on type valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated201.IBase0::Method1()
		ldstr "MyStruct251::Method1.MI.1894()"
		ldstr "IBase0 on type valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated201.IBase0::Method2<object>()
		ldstr "MyStruct251::Method2.MI.1896<System.Object>()"
		ldstr "IBase0 on type valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated201.IBase0::Method3<object>()
		ldstr "MyStruct251::Method3.MI.1898<System.Object>()"
		ldstr "IBase0 on type valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc V_2
		box valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass1>
		dup
		callvirt instance string class Generated201.IBase2`2<class Generated201.BaseClass1,class Generated201.BaseClass1>::Method7<object>()
		ldstr "MyStruct251::Method7.MI.1900<System.Object>()"
		ldstr "class IBase2`2<class Generated201.BaseClass1,class Generated201.BaseClass1> on type valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc V_2
		box valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass1>
		dup
		callvirt instance string class Generated201.IBase2`2<class Generated201.BaseClass0,class Generated201.BaseClass1>::Method7<object>()
		ldstr "MyStruct251::Method7.MI.1900<System.Object>()"
		ldstr "class IBase2`2<class Generated201.BaseClass0,class Generated201.BaseClass1> on type valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		.locals init (valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass0> V_3)
		ldloca V_3
		initobj valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass0>
		ldloca V_3
		dup
		call instance string valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass0>::Method0()
		ldstr "MyStruct251::Method0.1891()"
		ldstr "valuetype MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass0> on type MyStruct251"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass0>::Method1()
		ldstr "MyStruct251::Method1.1893()"
		ldstr "valuetype MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass0> on type MyStruct251"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass0>::Method2<object>()
		ldstr "MyStruct251::Method2.1895<System.Object>()"
		ldstr "valuetype MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass0> on type MyStruct251"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass0>::Method3<object>()
		ldstr "MyStruct251::Method3.1897<System.Object>()"
		ldstr "valuetype MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass0> on type MyStruct251"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass0>::Method7<object>()
		ldstr "MyStruct251::Method7.1899<System.Object>()"
		ldstr "valuetype MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass0> on type MyStruct251"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass0>::ClassMethod508<object>()
		ldstr "MyStruct251::ClassMethod508.1901<System.Object>()"
		ldstr "valuetype MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass0> on type MyStruct251"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup ldnull call instance bool valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass0>::Equals(object) pop
		dup call instance int32 valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass0>::GetHashCode() pop
		dup call instance string valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass0>::ToString() pop
		pop
		ldloc V_3
		box valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass0>
		dup
		callvirt instance string Generated201.IBase0::Method0()
		ldstr "MyStruct251::Method0.MI.1892()"
		ldstr "IBase0 on type valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated201.IBase0::Method1()
		ldstr "MyStruct251::Method1.MI.1894()"
		ldstr "IBase0 on type valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated201.IBase0::Method2<object>()
		ldstr "MyStruct251::Method2.MI.1896<System.Object>()"
		ldstr "IBase0 on type valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated201.IBase0::Method3<object>()
		ldstr "MyStruct251::Method3.MI.1898<System.Object>()"
		ldstr "IBase0 on type valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc V_3
		box valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass0>
		dup
		callvirt instance string class Generated201.IBase2`2<class Generated201.BaseClass1,class Generated201.BaseClass1>::Method7<object>()
		ldstr "MyStruct251::Method7.MI.1900<System.Object>()"
		ldstr "class IBase2`2<class Generated201.BaseClass1,class Generated201.BaseClass1> on type valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc V_3
		box valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass0>
		dup
		callvirt instance string class Generated201.IBase2`2<class Generated201.BaseClass0,class Generated201.BaseClass1>::Method7<object>()
		ldstr "MyStruct251::Method7.MI.1900<System.Object>()"
		ldstr "class IBase2`2<class Generated201.BaseClass0,class Generated201.BaseClass1> on type valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		.locals init (valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass1> V_4)
		ldloca V_4
		initobj valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass1>
		ldloca V_4
		dup
		call instance string valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass1>::Method0()
		ldstr "MyStruct251::Method0.1891()"
		ldstr "valuetype MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass1> on type MyStruct251"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass1>::Method1()
		ldstr "MyStruct251::Method1.1893()"
		ldstr "valuetype MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass1> on type MyStruct251"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass1>::Method2<object>()
		ldstr "MyStruct251::Method2.1895<System.Object>()"
		ldstr "valuetype MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass1> on type MyStruct251"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass1>::Method3<object>()
		ldstr "MyStruct251::Method3.1897<System.Object>()"
		ldstr "valuetype MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass1> on type MyStruct251"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass1>::Method7<object>()
		ldstr "MyStruct251::Method7.1899<System.Object>()"
		ldstr "valuetype MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass1> on type MyStruct251"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass1>::ClassMethod508<object>()
		ldstr "MyStruct251::ClassMethod508.1901<System.Object>()"
		ldstr "valuetype MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass1> on type MyStruct251"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup ldnull call instance bool valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass1>::Equals(object) pop
		dup call instance int32 valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass1>::GetHashCode() pop
		dup call instance string valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass1>::ToString() pop
		pop
		ldloc V_4
		box valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass1>
		dup
		callvirt instance string Generated201.IBase0::Method0()
		ldstr "MyStruct251::Method0.MI.1892()"
		ldstr "IBase0 on type valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated201.IBase0::Method1()
		ldstr "MyStruct251::Method1.MI.1894()"
		ldstr "IBase0 on type valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated201.IBase0::Method2<object>()
		ldstr "MyStruct251::Method2.MI.1896<System.Object>()"
		ldstr "IBase0 on type valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated201.IBase0::Method3<object>()
		ldstr "MyStruct251::Method3.MI.1898<System.Object>()"
		ldstr "IBase0 on type valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc V_4
		box valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass1>
		dup
		callvirt instance string class Generated201.IBase2`2<class Generated201.BaseClass1,class Generated201.BaseClass1>::Method7<object>()
		ldstr "MyStruct251::Method7.MI.1900<System.Object>()"
		ldstr "class IBase2`2<class Generated201.BaseClass1,class Generated201.BaseClass1> on type valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc V_4
		box valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass1>
		dup
		callvirt instance string class Generated201.IBase2`2<class Generated201.BaseClass0,class Generated201.BaseClass1>::Method7<object>()
		ldstr "MyStruct251::Method7.MI.1900<System.Object>()"
		ldstr "class IBase2`2<class Generated201.BaseClass0,class Generated201.BaseClass1> on type valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass1>"
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
		.locals init (valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass0> V_5)
		ldloca V_5
		initobj valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass0>
		.try {  ldloc V_5
				ldstr "MyStruct251::Method0.MI.1892()#MyStruct251::Method1.MI.1894()#MyStruct251::Method2.MI.1896<System.Object>()#MyStruct251::Method3.MI.1898<System.Object>()#"
				call void Generated201.Generated201::M.IBase0<valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass0>>(!!0,string) leave.s LV0
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV0} LV0:
		.try {  ldloc V_5
				ldstr "MyStruct251::Method7.MI.1900<System.Object>()#"
				call void Generated201.Generated201::M.IBase2.T.T<class Generated201.BaseClass1,class Generated201.BaseClass1,valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass0>>(!!2,string) leave.s LV1
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV1} LV1:
		.try {  ldloc V_5
				ldstr "MyStruct251::Method7.MI.1900<System.Object>()#"
				call void Generated201.Generated201::M.IBase2.B.T<class Generated201.BaseClass1,valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass0>>(!!1,string) leave.s LV2
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV2} LV2:
		.try {  ldloc V_5
				ldstr "MyStruct251::Method7.MI.1900<System.Object>()#"
				call void Generated201.Generated201::M.IBase2.B.B<valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass0>>(!!0,string) leave.s LV3
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV3} LV3:
		.try {  ldloc V_5
				ldstr "MyStruct251::Method7.MI.1900<System.Object>()#"
				call void Generated201.Generated201::M.IBase2.T.T<class Generated201.BaseClass0,class Generated201.BaseClass1,valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass0>>(!!2,string) leave.s LV4
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV4} LV4:
		.try {  ldloc V_5
				ldstr "MyStruct251::Method7.MI.1900<System.Object>()#"
				call void Generated201.Generated201::M.IBase2.A.T<class Generated201.BaseClass1,valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass0>>(!!1,string) leave.s LV5
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV5} LV5:
		.try {  ldloc V_5
				ldstr "MyStruct251::Method7.MI.1900<System.Object>()#"
				call void Generated201.Generated201::M.IBase2.A.B<valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass0>>(!!0,string) leave.s LV6
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV6} LV6:
		.locals init (valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass1> V_6)
		ldloca V_6
		initobj valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass1>
		.try {  ldloc V_6
				ldstr "MyStruct251::Method0.MI.1892()#MyStruct251::Method1.MI.1894()#MyStruct251::Method2.MI.1896<System.Object>()#MyStruct251::Method3.MI.1898<System.Object>()#"
				call void Generated201.Generated201::M.IBase0<valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass1>>(!!0,string) leave.s LV7
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV7} LV7:
		.try {  ldloc V_6
				ldstr "MyStruct251::Method7.MI.1900<System.Object>()#"
				call void Generated201.Generated201::M.IBase2.T.T<class Generated201.BaseClass1,class Generated201.BaseClass1,valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass1>>(!!2,string) leave.s LV8
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV8} LV8:
		.try {  ldloc V_6
				ldstr "MyStruct251::Method7.MI.1900<System.Object>()#"
				call void Generated201.Generated201::M.IBase2.B.T<class Generated201.BaseClass1,valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass1>>(!!1,string) leave.s LV9
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV9} LV9:
		.try {  ldloc V_6
				ldstr "MyStruct251::Method7.MI.1900<System.Object>()#"
				call void Generated201.Generated201::M.IBase2.B.B<valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass1>>(!!0,string) leave.s LV10
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV10} LV10:
		.try {  ldloc V_6
				ldstr "MyStruct251::Method7.MI.1900<System.Object>()#"
				call void Generated201.Generated201::M.IBase2.T.T<class Generated201.BaseClass0,class Generated201.BaseClass1,valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass1>>(!!2,string) leave.s LV11
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV11} LV11:
		.try {  ldloc V_6
				ldstr "MyStruct251::Method7.MI.1900<System.Object>()#"
				call void Generated201.Generated201::M.IBase2.A.T<class Generated201.BaseClass1,valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass1>>(!!1,string) leave.s LV12
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV12} LV12:
		.try {  ldloc V_6
				ldstr "MyStruct251::Method7.MI.1900<System.Object>()#"
				call void Generated201.Generated201::M.IBase2.A.B<valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass1>>(!!0,string) leave.s LV13
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV13} LV13:
		.locals init (valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass0> V_7)
		ldloca V_7
		initobj valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass0>
		.try {  ldloc V_7
				ldstr "MyStruct251::Method0.MI.1892()#MyStruct251::Method1.MI.1894()#MyStruct251::Method2.MI.1896<System.Object>()#MyStruct251::Method3.MI.1898<System.Object>()#"
				call void Generated201.Generated201::M.IBase0<valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass0>>(!!0,string) leave.s LV14
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV14} LV14:
		.try {  ldloc V_7
				ldstr "MyStruct251::Method7.MI.1900<System.Object>()#"
				call void Generated201.Generated201::M.IBase2.T.T<class Generated201.BaseClass1,class Generated201.BaseClass1,valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass0>>(!!2,string) leave.s LV15
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV15} LV15:
		.try {  ldloc V_7
				ldstr "MyStruct251::Method7.MI.1900<System.Object>()#"
				call void Generated201.Generated201::M.IBase2.B.T<class Generated201.BaseClass1,valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass0>>(!!1,string) leave.s LV16
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV16} LV16:
		.try {  ldloc V_7
				ldstr "MyStruct251::Method7.MI.1900<System.Object>()#"
				call void Generated201.Generated201::M.IBase2.B.B<valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass0>>(!!0,string) leave.s LV17
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV17} LV17:
		.try {  ldloc V_7
				ldstr "MyStruct251::Method7.MI.1900<System.Object>()#"
				call void Generated201.Generated201::M.IBase2.T.T<class Generated201.BaseClass0,class Generated201.BaseClass1,valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass0>>(!!2,string) leave.s LV18
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV18} LV18:
		.try {  ldloc V_7
				ldstr "MyStruct251::Method7.MI.1900<System.Object>()#"
				call void Generated201.Generated201::M.IBase2.A.T<class Generated201.BaseClass1,valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass0>>(!!1,string) leave.s LV19
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV19} LV19:
		.try {  ldloc V_7
				ldstr "MyStruct251::Method7.MI.1900<System.Object>()#"
				call void Generated201.Generated201::M.IBase2.A.B<valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass0>>(!!0,string) leave.s LV20
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV20} LV20:
		.locals init (valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass1> V_8)
		ldloca V_8
		initobj valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass1>
		.try {  ldloc V_8
				ldstr "MyStruct251::Method0.MI.1892()#MyStruct251::Method1.MI.1894()#MyStruct251::Method2.MI.1896<System.Object>()#MyStruct251::Method3.MI.1898<System.Object>()#"
				call void Generated201.Generated201::M.IBase0<valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass1>>(!!0,string) leave.s LV21
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV21} LV21:
		.try {  ldloc V_8
				ldstr "MyStruct251::Method7.MI.1900<System.Object>()#"
				call void Generated201.Generated201::M.IBase2.T.T<class Generated201.BaseClass1,class Generated201.BaseClass1,valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass1>>(!!2,string) leave.s LV22
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV22} LV22:
		.try {  ldloc V_8
				ldstr "MyStruct251::Method7.MI.1900<System.Object>()#"
				call void Generated201.Generated201::M.IBase2.B.T<class Generated201.BaseClass1,valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass1>>(!!1,string) leave.s LV23
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV23} LV23:
		.try {  ldloc V_8
				ldstr "MyStruct251::Method7.MI.1900<System.Object>()#"
				call void Generated201.Generated201::M.IBase2.B.B<valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass1>>(!!0,string) leave.s LV24
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV24} LV24:
		.try {  ldloc V_8
				ldstr "MyStruct251::Method7.MI.1900<System.Object>()#"
				call void Generated201.Generated201::M.IBase2.T.T<class Generated201.BaseClass0,class Generated201.BaseClass1,valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass1>>(!!2,string) leave.s LV25
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV25} LV25:
		.try {  ldloc V_8
				ldstr "MyStruct251::Method7.MI.1900<System.Object>()#"
				call void Generated201.Generated201::M.IBase2.A.T<class Generated201.BaseClass1,valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass1>>(!!1,string) leave.s LV26
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV26} LV26:
		.try {  ldloc V_8
				ldstr "MyStruct251::Method7.MI.1900<System.Object>()#"
				call void Generated201.Generated201::M.IBase2.A.B<valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass1>>(!!0,string) leave.s LV27
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV27} LV27:
		ldstr "========================================================================\n\n"
		call void [mscorlib]System.Console::WriteLine(string)
		ret
	}
	.method public hidebysig static void StructConstrainedInterfaceCallsTest() cil managed
	{
		.maxstack  10
		ldstr "===================== Struct Constrained Interface Calls Test ====================="
		call void [mscorlib]System.Console::WriteLine(string)
		.locals init (valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass0> V_9)
		ldloca V_9
		initobj valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass0>
		.try {  ldloc V_9
				ldstr "MyStruct251::Method0.MI.1892()#" +
				      "MyStruct251::Method1.MI.1894()#" +
				      "MyStruct251::Method2.MI.1896<System.Object>()#" +
				      "MyStruct251::Method3.MI.1898<System.Object>()#" +
				      "MyStruct251::Method7.MI.1900<System.Object>()#"
				call void Generated201.Generated201::M.MyStruct251.T.T<class Generated201.BaseClass0,class Generated201.BaseClass0,valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass0>>(!!2,string) leave.s LV0
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV0} LV0:
		.try {  ldloc V_9
				ldstr "MyStruct251::Method0.MI.1892()#" +
				      "MyStruct251::Method1.MI.1894()#" +
				      "MyStruct251::Method2.MI.1896<System.Object>()#" +
				      "MyStruct251::Method3.MI.1898<System.Object>()#" +
				      "MyStruct251::Method7.MI.1900<System.Object>()#"
				call void Generated201.Generated201::M.MyStruct251.A.T<class Generated201.BaseClass0,valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass0>>(!!1,string) leave.s LV1
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV1} LV1:
		.try {  ldloc V_9
				ldstr "MyStruct251::Method0.MI.1892()#" +
				      "MyStruct251::Method1.MI.1894()#" +
				      "MyStruct251::Method2.MI.1896<System.Object>()#" +
				      "MyStruct251::Method3.MI.1898<System.Object>()#" +
				      "MyStruct251::Method7.MI.1900<System.Object>()#"
				call void Generated201.Generated201::M.MyStruct251.A.A<valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass0>>(!!0,string) leave.s LV2
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV2} LV2:
		.locals init (valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass1> V_10)
		ldloca V_10
		initobj valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass1>
		.try {  ldloc V_10
				ldstr "MyStruct251::Method0.MI.1892()#" +
				      "MyStruct251::Method1.MI.1894()#" +
				      "MyStruct251::Method2.MI.1896<System.Object>()#" +
				      "MyStruct251::Method3.MI.1898<System.Object>()#" +
				      "MyStruct251::Method7.MI.1900<System.Object>()#"
				call void Generated201.Generated201::M.MyStruct251.T.T<class Generated201.BaseClass0,class Generated201.BaseClass1,valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass1>>(!!2,string) leave.s LV3
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV3} LV3:
		.try {  ldloc V_10
				ldstr "MyStruct251::Method0.MI.1892()#" +
				      "MyStruct251::Method1.MI.1894()#" +
				      "MyStruct251::Method2.MI.1896<System.Object>()#" +
				      "MyStruct251::Method3.MI.1898<System.Object>()#" +
				      "MyStruct251::Method7.MI.1900<System.Object>()#"
				call void Generated201.Generated201::M.MyStruct251.A.T<class Generated201.BaseClass1,valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass1>>(!!1,string) leave.s LV4
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV4} LV4:
		.try {  ldloc V_10
				ldstr "MyStruct251::Method0.MI.1892()#" +
				      "MyStruct251::Method1.MI.1894()#" +
				      "MyStruct251::Method2.MI.1896<System.Object>()#" +
				      "MyStruct251::Method3.MI.1898<System.Object>()#" +
				      "MyStruct251::Method7.MI.1900<System.Object>()#"
				call void Generated201.Generated201::M.MyStruct251.A.B<valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass1>>(!!0,string) leave.s LV5
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV5} LV5:
		.locals init (valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass0> V_11)
		ldloca V_11
		initobj valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass0>
		.try {  ldloc V_11
				ldstr "MyStruct251::Method0.MI.1892()#" +
				      "MyStruct251::Method1.MI.1894()#" +
				      "MyStruct251::Method2.MI.1896<System.Object>()#" +
				      "MyStruct251::Method3.MI.1898<System.Object>()#" +
				      "MyStruct251::Method7.MI.1900<System.Object>()#"
				call void Generated201.Generated201::M.MyStruct251.T.T<class Generated201.BaseClass1,class Generated201.BaseClass0,valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass0>>(!!2,string) leave.s LV6
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV6} LV6:
		.try {  ldloc V_11
				ldstr "MyStruct251::Method0.MI.1892()#" +
				      "MyStruct251::Method1.MI.1894()#" +
				      "MyStruct251::Method2.MI.1896<System.Object>()#" +
				      "MyStruct251::Method3.MI.1898<System.Object>()#" +
				      "MyStruct251::Method7.MI.1900<System.Object>()#"
				call void Generated201.Generated201::M.MyStruct251.B.T<class Generated201.BaseClass0,valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass0>>(!!1,string) leave.s LV7
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV7} LV7:
		.try {  ldloc V_11
				ldstr "MyStruct251::Method0.MI.1892()#" +
				      "MyStruct251::Method1.MI.1894()#" +
				      "MyStruct251::Method2.MI.1896<System.Object>()#" +
				      "MyStruct251::Method3.MI.1898<System.Object>()#" +
				      "MyStruct251::Method7.MI.1900<System.Object>()#"
				call void Generated201.Generated201::M.MyStruct251.B.A<valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass0>>(!!0,string) leave.s LV8
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV8} LV8:
		.locals init (valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass1> V_12)
		ldloca V_12
		initobj valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass1>
		.try {  ldloc V_12
				ldstr "MyStruct251::Method0.MI.1892()#" +
				      "MyStruct251::Method1.MI.1894()#" +
				      "MyStruct251::Method2.MI.1896<System.Object>()#" +
				      "MyStruct251::Method3.MI.1898<System.Object>()#" +
				      "MyStruct251::Method7.MI.1900<System.Object>()#"
				call void Generated201.Generated201::M.MyStruct251.T.T<class Generated201.BaseClass1,class Generated201.BaseClass1,valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass1>>(!!2,string) leave.s LV9
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV9} LV9:
		.try {  ldloc V_12
				ldstr "MyStruct251::Method0.MI.1892()#" +
				      "MyStruct251::Method1.MI.1894()#" +
				      "MyStruct251::Method2.MI.1896<System.Object>()#" +
				      "MyStruct251::Method3.MI.1898<System.Object>()#" +
				      "MyStruct251::Method7.MI.1900<System.Object>()#"
				call void Generated201.Generated201::M.MyStruct251.B.T<class Generated201.BaseClass1,valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass1>>(!!1,string) leave.s LV10
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV10} LV10:
		.try {  ldloc V_12
				ldstr "MyStruct251::Method0.MI.1892()#" +
				      "MyStruct251::Method1.MI.1894()#" +
				      "MyStruct251::Method2.MI.1896<System.Object>()#" +
				      "MyStruct251::Method3.MI.1898<System.Object>()#" +
				      "MyStruct251::Method7.MI.1900<System.Object>()#"
				call void Generated201.Generated201::M.MyStruct251.B.B<valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass1>>(!!0,string) leave.s LV11
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
		.locals init (valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass0> V_13)
		ldloca V_13
		initobj valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass0>
		ldloc V_13
		box valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass0>
		ldloc V_13
		box valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass0>
		ldvirtftn instance string valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass0>::Method0()
		calli default string(object)
		ldstr "MyStruct251::Method0.1891()"
		ldstr "valuetype MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass0> on type valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_13
		box valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass0>
		ldloc V_13
		box valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass0>
		ldvirtftn instance string valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass0>::Method1()
		calli default string(object)
		ldstr "MyStruct251::Method1.1893()"
		ldstr "valuetype MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass0> on type valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_13
		box valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass0>
		ldloc V_13
		box valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass0>
		ldvirtftn instance string valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass0>::Method2<object>()
		calli default string(object)
		ldstr "MyStruct251::Method2.1895<System.Object>()"
		ldstr "valuetype MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass0> on type valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_13
		box valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass0>
		ldloc V_13
		box valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass0>
		ldvirtftn instance string valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass0>::Method3<object>()
		calli default string(object)
		ldstr "MyStruct251::Method3.1897<System.Object>()"
		ldstr "valuetype MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass0> on type valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_13
		box valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass0>
		ldloc V_13
		box valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass0>
		ldvirtftn instance string valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass0>::Method7<object>()
		calli default string(object)
		ldstr "MyStruct251::Method7.1899<System.Object>()"
		ldstr "valuetype MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass0> on type valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_13
		box valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass0>
		ldloc V_13
		box valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass0>
		ldvirtftn instance string valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass0>::ClassMethod508<object>()
		calli default string(object)
		ldstr "MyStruct251::ClassMethod508.1901<System.Object>()"
		ldstr "valuetype MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass0> on type valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_13 box valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass0> ldnull 
		ldloc V_13 box valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass0>
		ldvirtftn instance bool valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass0>::Equals(object) calli default bool(object,object) pop
		ldloc V_13 box valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass0>
		ldloc V_13 box valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass0>
		ldvirtftn instance int32 valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass0>::GetHashCode() calli default int32(object) pop
		ldloc V_13 box valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass0>
		ldloc V_13 box valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass0>
		ldvirtftn instance string valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass0>::ToString() calli default string(object) pop
		ldloc V_13
		box valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass0>
		ldloc V_13
		box valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass0>
		ldvirtftn instance string Generated201.IBase0::Method0()
		calli default string(object)
		ldstr "MyStruct251::Method0.MI.1892()"
		ldstr "IBase0 on type valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_13
		box valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass0>
		ldloc V_13
		box valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass0>
		ldvirtftn instance string Generated201.IBase0::Method1()
		calli default string(object)
		ldstr "MyStruct251::Method1.MI.1894()"
		ldstr "IBase0 on type valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_13
		box valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass0>
		ldloc V_13
		box valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass0>
		ldvirtftn instance string Generated201.IBase0::Method2<object>()
		calli default string(object)
		ldstr "MyStruct251::Method2.MI.1896<System.Object>()"
		ldstr "IBase0 on type valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_13
		box valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass0>
		ldloc V_13
		box valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass0>
		ldvirtftn instance string Generated201.IBase0::Method3<object>()
		calli default string(object)
		ldstr "MyStruct251::Method3.MI.1898<System.Object>()"
		ldstr "IBase0 on type valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_13
		box valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass0>
		ldloc V_13
		box valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass0>
		ldvirtftn instance string class Generated201.IBase2`2<class Generated201.BaseClass1,class Generated201.BaseClass1>::Method7<object>()
		calli default string(object)
		ldstr "MyStruct251::Method7.MI.1900<System.Object>()"
		ldstr "class IBase2`2<class Generated201.BaseClass1,class Generated201.BaseClass1> on type valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_13
		box valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass0>
		ldloc V_13
		box valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass0>
		ldvirtftn instance string class Generated201.IBase2`2<class Generated201.BaseClass0,class Generated201.BaseClass1>::Method7<object>()
		calli default string(object)
		ldstr "MyStruct251::Method7.MI.1900<System.Object>()"
		ldstr "class IBase2`2<class Generated201.BaseClass0,class Generated201.BaseClass1> on type valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		.locals init (valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass1> V_14)
		ldloca V_14
		initobj valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass1>
		ldloc V_14
		box valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass1>
		ldloc V_14
		box valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass1>
		ldvirtftn instance string valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass1>::Method0()
		calli default string(object)
		ldstr "MyStruct251::Method0.1891()"
		ldstr "valuetype MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass1> on type valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_14
		box valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass1>
		ldloc V_14
		box valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass1>
		ldvirtftn instance string valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass1>::Method1()
		calli default string(object)
		ldstr "MyStruct251::Method1.1893()"
		ldstr "valuetype MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass1> on type valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_14
		box valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass1>
		ldloc V_14
		box valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass1>
		ldvirtftn instance string valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass1>::Method2<object>()
		calli default string(object)
		ldstr "MyStruct251::Method2.1895<System.Object>()"
		ldstr "valuetype MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass1> on type valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_14
		box valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass1>
		ldloc V_14
		box valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass1>
		ldvirtftn instance string valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass1>::Method3<object>()
		calli default string(object)
		ldstr "MyStruct251::Method3.1897<System.Object>()"
		ldstr "valuetype MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass1> on type valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_14
		box valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass1>
		ldloc V_14
		box valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass1>
		ldvirtftn instance string valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass1>::Method7<object>()
		calli default string(object)
		ldstr "MyStruct251::Method7.1899<System.Object>()"
		ldstr "valuetype MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass1> on type valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_14
		box valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass1>
		ldloc V_14
		box valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass1>
		ldvirtftn instance string valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass1>::ClassMethod508<object>()
		calli default string(object)
		ldstr "MyStruct251::ClassMethod508.1901<System.Object>()"
		ldstr "valuetype MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass1> on type valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_14 box valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass1> ldnull 
		ldloc V_14 box valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass1>
		ldvirtftn instance bool valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass1>::Equals(object) calli default bool(object,object) pop
		ldloc V_14 box valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass1>
		ldloc V_14 box valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass1>
		ldvirtftn instance int32 valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass1>::GetHashCode() calli default int32(object) pop
		ldloc V_14 box valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass1>
		ldloc V_14 box valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass1>
		ldvirtftn instance string valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass1>::ToString() calli default string(object) pop
		ldloc V_14
		box valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass1>
		ldloc V_14
		box valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass1>
		ldvirtftn instance string Generated201.IBase0::Method0()
		calli default string(object)
		ldstr "MyStruct251::Method0.MI.1892()"
		ldstr "IBase0 on type valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_14
		box valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass1>
		ldloc V_14
		box valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass1>
		ldvirtftn instance string Generated201.IBase0::Method1()
		calli default string(object)
		ldstr "MyStruct251::Method1.MI.1894()"
		ldstr "IBase0 on type valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_14
		box valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass1>
		ldloc V_14
		box valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass1>
		ldvirtftn instance string Generated201.IBase0::Method2<object>()
		calli default string(object)
		ldstr "MyStruct251::Method2.MI.1896<System.Object>()"
		ldstr "IBase0 on type valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_14
		box valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass1>
		ldloc V_14
		box valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass1>
		ldvirtftn instance string Generated201.IBase0::Method3<object>()
		calli default string(object)
		ldstr "MyStruct251::Method3.MI.1898<System.Object>()"
		ldstr "IBase0 on type valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_14
		box valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass1>
		ldloc V_14
		box valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass1>
		ldvirtftn instance string class Generated201.IBase2`2<class Generated201.BaseClass1,class Generated201.BaseClass1>::Method7<object>()
		calli default string(object)
		ldstr "MyStruct251::Method7.MI.1900<System.Object>()"
		ldstr "class IBase2`2<class Generated201.BaseClass1,class Generated201.BaseClass1> on type valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_14
		box valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass1>
		ldloc V_14
		box valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass1>
		ldvirtftn instance string class Generated201.IBase2`2<class Generated201.BaseClass0,class Generated201.BaseClass1>::Method7<object>()
		calli default string(object)
		ldstr "MyStruct251::Method7.MI.1900<System.Object>()"
		ldstr "class IBase2`2<class Generated201.BaseClass0,class Generated201.BaseClass1> on type valuetype Generated201.MyStruct251`2<class Generated201.BaseClass0,class Generated201.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		.locals init (valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass0> V_15)
		ldloca V_15
		initobj valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass0>
		ldloc V_15
		box valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass0>
		ldloc V_15
		box valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass0>
		ldvirtftn instance string valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass0>::Method0()
		calli default string(object)
		ldstr "MyStruct251::Method0.1891()"
		ldstr "valuetype MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass0> on type valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_15
		box valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass0>
		ldloc V_15
		box valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass0>
		ldvirtftn instance string valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass0>::Method1()
		calli default string(object)
		ldstr "MyStruct251::Method1.1893()"
		ldstr "valuetype MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass0> on type valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_15
		box valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass0>
		ldloc V_15
		box valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass0>
		ldvirtftn instance string valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass0>::Method2<object>()
		calli default string(object)
		ldstr "MyStruct251::Method2.1895<System.Object>()"
		ldstr "valuetype MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass0> on type valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_15
		box valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass0>
		ldloc V_15
		box valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass0>
		ldvirtftn instance string valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass0>::Method3<object>()
		calli default string(object)
		ldstr "MyStruct251::Method3.1897<System.Object>()"
		ldstr "valuetype MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass0> on type valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_15
		box valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass0>
		ldloc V_15
		box valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass0>
		ldvirtftn instance string valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass0>::Method7<object>()
		calli default string(object)
		ldstr "MyStruct251::Method7.1899<System.Object>()"
		ldstr "valuetype MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass0> on type valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_15
		box valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass0>
		ldloc V_15
		box valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass0>
		ldvirtftn instance string valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass0>::ClassMethod508<object>()
		calli default string(object)
		ldstr "MyStruct251::ClassMethod508.1901<System.Object>()"
		ldstr "valuetype MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass0> on type valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_15 box valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass0> ldnull 
		ldloc V_15 box valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass0>
		ldvirtftn instance bool valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass0>::Equals(object) calli default bool(object,object) pop
		ldloc V_15 box valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass0>
		ldloc V_15 box valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass0>
		ldvirtftn instance int32 valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass0>::GetHashCode() calli default int32(object) pop
		ldloc V_15 box valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass0>
		ldloc V_15 box valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass0>
		ldvirtftn instance string valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass0>::ToString() calli default string(object) pop
		ldloc V_15
		box valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass0>
		ldloc V_15
		box valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass0>
		ldvirtftn instance string Generated201.IBase0::Method0()
		calli default string(object)
		ldstr "MyStruct251::Method0.MI.1892()"
		ldstr "IBase0 on type valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_15
		box valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass0>
		ldloc V_15
		box valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass0>
		ldvirtftn instance string Generated201.IBase0::Method1()
		calli default string(object)
		ldstr "MyStruct251::Method1.MI.1894()"
		ldstr "IBase0 on type valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_15
		box valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass0>
		ldloc V_15
		box valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass0>
		ldvirtftn instance string Generated201.IBase0::Method2<object>()
		calli default string(object)
		ldstr "MyStruct251::Method2.MI.1896<System.Object>()"
		ldstr "IBase0 on type valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_15
		box valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass0>
		ldloc V_15
		box valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass0>
		ldvirtftn instance string Generated201.IBase0::Method3<object>()
		calli default string(object)
		ldstr "MyStruct251::Method3.MI.1898<System.Object>()"
		ldstr "IBase0 on type valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_15
		box valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass0>
		ldloc V_15
		box valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass0>
		ldvirtftn instance string class Generated201.IBase2`2<class Generated201.BaseClass1,class Generated201.BaseClass1>::Method7<object>()
		calli default string(object)
		ldstr "MyStruct251::Method7.MI.1900<System.Object>()"
		ldstr "class IBase2`2<class Generated201.BaseClass1,class Generated201.BaseClass1> on type valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_15
		box valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass0>
		ldloc V_15
		box valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass0>
		ldvirtftn instance string class Generated201.IBase2`2<class Generated201.BaseClass0,class Generated201.BaseClass1>::Method7<object>()
		calli default string(object)
		ldstr "MyStruct251::Method7.MI.1900<System.Object>()"
		ldstr "class IBase2`2<class Generated201.BaseClass0,class Generated201.BaseClass1> on type valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		.locals init (valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass1> V_16)
		ldloca V_16
		initobj valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass1>
		ldloc V_16
		box valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass1>
		ldloc V_16
		box valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass1>
		ldvirtftn instance string valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass1>::Method0()
		calli default string(object)
		ldstr "MyStruct251::Method0.1891()"
		ldstr "valuetype MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass1> on type valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_16
		box valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass1>
		ldloc V_16
		box valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass1>
		ldvirtftn instance string valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass1>::Method1()
		calli default string(object)
		ldstr "MyStruct251::Method1.1893()"
		ldstr "valuetype MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass1> on type valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_16
		box valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass1>
		ldloc V_16
		box valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass1>
		ldvirtftn instance string valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass1>::Method2<object>()
		calli default string(object)
		ldstr "MyStruct251::Method2.1895<System.Object>()"
		ldstr "valuetype MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass1> on type valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_16
		box valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass1>
		ldloc V_16
		box valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass1>
		ldvirtftn instance string valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass1>::Method3<object>()
		calli default string(object)
		ldstr "MyStruct251::Method3.1897<System.Object>()"
		ldstr "valuetype MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass1> on type valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_16
		box valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass1>
		ldloc V_16
		box valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass1>
		ldvirtftn instance string valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass1>::Method7<object>()
		calli default string(object)
		ldstr "MyStruct251::Method7.1899<System.Object>()"
		ldstr "valuetype MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass1> on type valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_16
		box valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass1>
		ldloc V_16
		box valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass1>
		ldvirtftn instance string valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass1>::ClassMethod508<object>()
		calli default string(object)
		ldstr "MyStruct251::ClassMethod508.1901<System.Object>()"
		ldstr "valuetype MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass1> on type valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_16 box valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass1> ldnull 
		ldloc V_16 box valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass1>
		ldvirtftn instance bool valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass1>::Equals(object) calli default bool(object,object) pop
		ldloc V_16 box valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass1>
		ldloc V_16 box valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass1>
		ldvirtftn instance int32 valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass1>::GetHashCode() calli default int32(object) pop
		ldloc V_16 box valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass1>
		ldloc V_16 box valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass1>
		ldvirtftn instance string valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass1>::ToString() calli default string(object) pop
		ldloc V_16
		box valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass1>
		ldloc V_16
		box valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass1>
		ldvirtftn instance string Generated201.IBase0::Method0()
		calli default string(object)
		ldstr "MyStruct251::Method0.MI.1892()"
		ldstr "IBase0 on type valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_16
		box valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass1>
		ldloc V_16
		box valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass1>
		ldvirtftn instance string Generated201.IBase0::Method1()
		calli default string(object)
		ldstr "MyStruct251::Method1.MI.1894()"
		ldstr "IBase0 on type valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_16
		box valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass1>
		ldloc V_16
		box valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass1>
		ldvirtftn instance string Generated201.IBase0::Method2<object>()
		calli default string(object)
		ldstr "MyStruct251::Method2.MI.1896<System.Object>()"
		ldstr "IBase0 on type valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_16
		box valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass1>
		ldloc V_16
		box valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass1>
		ldvirtftn instance string Generated201.IBase0::Method3<object>()
		calli default string(object)
		ldstr "MyStruct251::Method3.MI.1898<System.Object>()"
		ldstr "IBase0 on type valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_16
		box valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass1>
		ldloc V_16
		box valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass1>
		ldvirtftn instance string class Generated201.IBase2`2<class Generated201.BaseClass1,class Generated201.BaseClass1>::Method7<object>()
		calli default string(object)
		ldstr "MyStruct251::Method7.MI.1900<System.Object>()"
		ldstr "class IBase2`2<class Generated201.BaseClass1,class Generated201.BaseClass1> on type valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_16
		box valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass1>
		ldloc V_16
		box valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass1>
		ldvirtftn instance string class Generated201.IBase2`2<class Generated201.BaseClass0,class Generated201.BaseClass1>::Method7<object>()
		calli default string(object)
		ldstr "MyStruct251::Method7.MI.1900<System.Object>()"
		ldstr "class IBase2`2<class Generated201.BaseClass0,class Generated201.BaseClass1> on type valuetype Generated201.MyStruct251`2<class Generated201.BaseClass1,class Generated201.BaseClass1>"
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
		call void Generated201.Generated201::MethodCallingTest()
		call void Generated201.Generated201::ConstrainedCallsTest()
		call void Generated201.Generated201::StructConstrainedInterfaceCallsTest()
		call void Generated201.Generated201::CalliTest()
		ldc.i4 100
		ret
	}
}
