// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.class public Generated769.BaseClass0 
{
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void [mscorlib]System.Object::.ctor()
		ret
	}
}
.class public Generated769.BaseClass1 
		extends Generated769.BaseClass0
{
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void Generated769.BaseClass0::.ctor()
		ret
	}
}
.class public Generated769.G3_C1241`1<T0> 
		extends class Generated769.G2_C292`2<class Generated769.BaseClass0,class Generated769.BaseClass1>
		implements class Generated769.IBase2`2<class Generated769.BaseClass0,!T0> 
{
	.method public hidebysig virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G3_C1241::Method7.15066<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase2<class Generated769.BaseClass0,T0>.Method7'<M0>() cil managed noinlining { 
		.override method instance string class Generated769.IBase2`2<class Generated769.BaseClass0,!T0>::Method7<[1]>()
		ldstr "G3_C1241::Method7.MI.15067<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod3864() cil managed noinlining { 
		ldstr "G3_C1241::ClassMethod3864.15068()"
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class Generated769.G2_C292`2<class Generated769.BaseClass0,class Generated769.BaseClass1>::.ctor()
		ret
	}
}
.class public abstract Generated769.G2_C292`2<T0, T1> 
		extends class Generated769.G1_C6`2<class Generated769.BaseClass1,!T0>
		implements class Generated769.IBase2`2<!T1,!T0>, Generated769.IBase0 
{
	.method public hidebysig virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G2_C292::Method7.7457<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig virtual instance string Method0() cil managed noinlining { 
		ldstr "G2_C292::Method0.7458()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'Generated769.IBase0.Method0'() cil managed noinlining { 
		.override method instance string Generated769.IBase0::Method0()
		ldstr "G2_C292::Method0.MI.7459()"
		ret
	}
	.method public hidebysig newslot virtual instance string Method1() cil managed noinlining { 
		ldstr "G2_C292::Method1.7460()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'Generated769.IBase0.Method1'() cil managed noinlining { 
		.override method instance string Generated769.IBase0::Method1()
		ldstr "G2_C292::Method1.MI.7461()"
		ret
	}
	.method public hidebysig newslot virtual instance string Method2<M0>() cil managed noinlining { 
		ldstr "G2_C292::Method2.7462<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig virtual instance string Method3<M0>() cil managed noinlining { 
		ldstr "G2_C292::Method3.7463<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'Generated769.IBase0.Method3'<M0>() cil managed noinlining { 
		.override method instance string Generated769.IBase0::Method3<[1]>()
		ldstr "G2_C292::Method3.MI.7464<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1911<M0>() cil managed noinlining { 
		ldstr "G2_C292::ClassMethod1911.7465<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1912<M0>() cil managed noinlining { 
		ldstr "G2_C292::ClassMethod1912.7466<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'G1_C6<class Generated769.BaseClass1,T0>.ClassMethod1326'() cil managed noinlining { 
		.override method instance string class Generated769.G1_C6`2<class Generated769.BaseClass1,!T0>::ClassMethod1326()
		ldstr "G2_C292::ClassMethod1326.MI.7467()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'G1_C6<class Generated769.BaseClass1,T0>.ClassMethod1327'<M0>() cil managed noinlining { 
		.override method instance string class Generated769.G1_C6`2<class Generated769.BaseClass1,!T0>::ClassMethod1327<[1]>()
		ldstr "G2_C292::ClassMethod1327.MI.7468<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class Generated769.G1_C6`2<class Generated769.BaseClass1,!T0>::.ctor()
		ret
	}
}
.class interface public abstract Generated769.IBase2`2<+T0, -T1> 
{
	.method public hidebysig newslot abstract virtual instance string Method7<M0>() cil managed { }
}
.class public Generated769.G1_C6`2<T0, T1> 
		implements class Generated769.IBase2`2<class Generated769.BaseClass1,!T0> 
{
	.method public hidebysig newslot virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G1_C6::Method7.4808<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1326() cil managed noinlining { 
		ldstr "G1_C6::ClassMethod1326.4809()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1327<M0>() cil managed noinlining { 
		ldstr "G1_C6::ClassMethod1327.4810<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void [mscorlib]System.Object::.ctor()
		ret
	}
}
.class interface public abstract Generated769.IBase0 
{
	.method public hidebysig newslot abstract virtual instance string Method0() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method1() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method2<M0>() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method3<M0>() cil managed { }
}
.class public auto ansi beforefieldinit Generated769.Generated769 {
	.method static void M.BaseClass0<(Generated769.BaseClass0)W>(!!W inst, string exp) cil managed {
		.maxstack 5
		.locals init (string[] actualResults)
		ldc.i4.s    0
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.BaseClass0<(Generated769.BaseClass0)W>(!!W inst, string exp)"
		ldc.i4.s 0
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.BaseClass1<(Generated769.BaseClass1)W>(!!W inst, string exp) cil managed {
		.maxstack 5
		.locals init (string[] actualResults)
		ldc.i4.s    0
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.BaseClass1<(Generated769.BaseClass1)W>(!!W inst, string exp)"
		ldc.i4.s 0
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1241.T<T0,(class Generated769.G3_C1241`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 15
		.locals init (string[] actualResults)
		ldc.i4.s    10
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1241.T<T0,(class Generated769.G3_C1241`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 10
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated769.G3_C1241`1<!!T0>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated769.G3_C1241`1<!!T0>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated769.G3_C1241`1<!!T0>::ClassMethod1911<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated769.G3_C1241`1<!!T0>::ClassMethod1912<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated769.G3_C1241`1<!!T0>::ClassMethod3864()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated769.G3_C1241`1<!!T0>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated769.G3_C1241`1<!!T0>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated769.G3_C1241`1<!!T0>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated769.G3_C1241`1<!!T0>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated769.G3_C1241`1<!!T0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1241.A<(class Generated769.G3_C1241`1<class Generated769.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 15
		.locals init (string[] actualResults)
		ldc.i4.s    10
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1241.A<(class Generated769.G3_C1241`1<class Generated769.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 10
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated769.G3_C1241`1<class Generated769.BaseClass0>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated769.G3_C1241`1<class Generated769.BaseClass0>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated769.G3_C1241`1<class Generated769.BaseClass0>::ClassMethod1911<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated769.G3_C1241`1<class Generated769.BaseClass0>::ClassMethod1912<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated769.G3_C1241`1<class Generated769.BaseClass0>::ClassMethod3864()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated769.G3_C1241`1<class Generated769.BaseClass0>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated769.G3_C1241`1<class Generated769.BaseClass0>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated769.G3_C1241`1<class Generated769.BaseClass0>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated769.G3_C1241`1<class Generated769.BaseClass0>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated769.G3_C1241`1<class Generated769.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1241.B<(class Generated769.G3_C1241`1<class Generated769.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 15
		.locals init (string[] actualResults)
		ldc.i4.s    10
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1241.B<(class Generated769.G3_C1241`1<class Generated769.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 10
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated769.G3_C1241`1<class Generated769.BaseClass1>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated769.G3_C1241`1<class Generated769.BaseClass1>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated769.G3_C1241`1<class Generated769.BaseClass1>::ClassMethod1911<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated769.G3_C1241`1<class Generated769.BaseClass1>::ClassMethod1912<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated769.G3_C1241`1<class Generated769.BaseClass1>::ClassMethod3864()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated769.G3_C1241`1<class Generated769.BaseClass1>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated769.G3_C1241`1<class Generated769.BaseClass1>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated769.G3_C1241`1<class Generated769.BaseClass1>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated769.G3_C1241`1<class Generated769.BaseClass1>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated769.G3_C1241`1<class Generated769.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C292.T.T<T0,T1,(class Generated769.G2_C292`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 14
		.locals init (string[] actualResults)
		ldc.i4.s    9
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C292.T.T<T0,T1,(class Generated769.G2_C292`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 9
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated769.G2_C292`2<!!T0,!!T1>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated769.G2_C292`2<!!T0,!!T1>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated769.G2_C292`2<!!T0,!!T1>::ClassMethod1911<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated769.G2_C292`2<!!T0,!!T1>::ClassMethod1912<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated769.G2_C292`2<!!T0,!!T1>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated769.G2_C292`2<!!T0,!!T1>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated769.G2_C292`2<!!T0,!!T1>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated769.G2_C292`2<!!T0,!!T1>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated769.G2_C292`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C292.A.T<T1,(class Generated769.G2_C292`2<class Generated769.BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 14
		.locals init (string[] actualResults)
		ldc.i4.s    9
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C292.A.T<T1,(class Generated769.G2_C292`2<class Generated769.BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 9
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated769.G2_C292`2<class Generated769.BaseClass0,!!T1>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated769.G2_C292`2<class Generated769.BaseClass0,!!T1>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated769.G2_C292`2<class Generated769.BaseClass0,!!T1>::ClassMethod1911<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated769.G2_C292`2<class Generated769.BaseClass0,!!T1>::ClassMethod1912<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated769.G2_C292`2<class Generated769.BaseClass0,!!T1>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated769.G2_C292`2<class Generated769.BaseClass0,!!T1>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated769.G2_C292`2<class Generated769.BaseClass0,!!T1>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated769.G2_C292`2<class Generated769.BaseClass0,!!T1>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated769.G2_C292`2<class Generated769.BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C292.A.A<(class Generated769.G2_C292`2<class Generated769.BaseClass0,class Generated769.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 14
		.locals init (string[] actualResults)
		ldc.i4.s    9
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C292.A.A<(class Generated769.G2_C292`2<class Generated769.BaseClass0,class Generated769.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 9
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated769.G2_C292`2<class Generated769.BaseClass0,class Generated769.BaseClass0>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated769.G2_C292`2<class Generated769.BaseClass0,class Generated769.BaseClass0>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated769.G2_C292`2<class Generated769.BaseClass0,class Generated769.BaseClass0>::ClassMethod1911<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated769.G2_C292`2<class Generated769.BaseClass0,class Generated769.BaseClass0>::ClassMethod1912<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated769.G2_C292`2<class Generated769.BaseClass0,class Generated769.BaseClass0>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated769.G2_C292`2<class Generated769.BaseClass0,class Generated769.BaseClass0>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated769.G2_C292`2<class Generated769.BaseClass0,class Generated769.BaseClass0>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated769.G2_C292`2<class Generated769.BaseClass0,class Generated769.BaseClass0>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated769.G2_C292`2<class Generated769.BaseClass0,class Generated769.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C292.A.B<(class Generated769.G2_C292`2<class Generated769.BaseClass0,class Generated769.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 14
		.locals init (string[] actualResults)
		ldc.i4.s    9
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C292.A.B<(class Generated769.G2_C292`2<class Generated769.BaseClass0,class Generated769.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 9
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated769.G2_C292`2<class Generated769.BaseClass0,class Generated769.BaseClass1>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated769.G2_C292`2<class Generated769.BaseClass0,class Generated769.BaseClass1>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated769.G2_C292`2<class Generated769.BaseClass0,class Generated769.BaseClass1>::ClassMethod1911<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated769.G2_C292`2<class Generated769.BaseClass0,class Generated769.BaseClass1>::ClassMethod1912<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated769.G2_C292`2<class Generated769.BaseClass0,class Generated769.BaseClass1>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated769.G2_C292`2<class Generated769.BaseClass0,class Generated769.BaseClass1>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated769.G2_C292`2<class Generated769.BaseClass0,class Generated769.BaseClass1>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated769.G2_C292`2<class Generated769.BaseClass0,class Generated769.BaseClass1>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated769.G2_C292`2<class Generated769.BaseClass0,class Generated769.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C292.B.T<T1,(class Generated769.G2_C292`2<class Generated769.BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 14
		.locals init (string[] actualResults)
		ldc.i4.s    9
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C292.B.T<T1,(class Generated769.G2_C292`2<class Generated769.BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 9
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated769.G2_C292`2<class Generated769.BaseClass1,!!T1>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated769.G2_C292`2<class Generated769.BaseClass1,!!T1>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated769.G2_C292`2<class Generated769.BaseClass1,!!T1>::ClassMethod1911<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated769.G2_C292`2<class Generated769.BaseClass1,!!T1>::ClassMethod1912<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated769.G2_C292`2<class Generated769.BaseClass1,!!T1>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated769.G2_C292`2<class Generated769.BaseClass1,!!T1>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated769.G2_C292`2<class Generated769.BaseClass1,!!T1>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated769.G2_C292`2<class Generated769.BaseClass1,!!T1>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated769.G2_C292`2<class Generated769.BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C292.B.A<(class Generated769.G2_C292`2<class Generated769.BaseClass1,class Generated769.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 14
		.locals init (string[] actualResults)
		ldc.i4.s    9
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C292.B.A<(class Generated769.G2_C292`2<class Generated769.BaseClass1,class Generated769.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 9
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated769.G2_C292`2<class Generated769.BaseClass1,class Generated769.BaseClass0>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated769.G2_C292`2<class Generated769.BaseClass1,class Generated769.BaseClass0>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated769.G2_C292`2<class Generated769.BaseClass1,class Generated769.BaseClass0>::ClassMethod1911<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated769.G2_C292`2<class Generated769.BaseClass1,class Generated769.BaseClass0>::ClassMethod1912<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated769.G2_C292`2<class Generated769.BaseClass1,class Generated769.BaseClass0>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated769.G2_C292`2<class Generated769.BaseClass1,class Generated769.BaseClass0>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated769.G2_C292`2<class Generated769.BaseClass1,class Generated769.BaseClass0>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated769.G2_C292`2<class Generated769.BaseClass1,class Generated769.BaseClass0>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated769.G2_C292`2<class Generated769.BaseClass1,class Generated769.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C292.B.B<(class Generated769.G2_C292`2<class Generated769.BaseClass1,class Generated769.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 14
		.locals init (string[] actualResults)
		ldc.i4.s    9
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C292.B.B<(class Generated769.G2_C292`2<class Generated769.BaseClass1,class Generated769.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 9
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated769.G2_C292`2<class Generated769.BaseClass1,class Generated769.BaseClass1>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated769.G2_C292`2<class Generated769.BaseClass1,class Generated769.BaseClass1>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated769.G2_C292`2<class Generated769.BaseClass1,class Generated769.BaseClass1>::ClassMethod1911<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated769.G2_C292`2<class Generated769.BaseClass1,class Generated769.BaseClass1>::ClassMethod1912<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated769.G2_C292`2<class Generated769.BaseClass1,class Generated769.BaseClass1>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated769.G2_C292`2<class Generated769.BaseClass1,class Generated769.BaseClass1>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated769.G2_C292`2<class Generated769.BaseClass1,class Generated769.BaseClass1>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated769.G2_C292`2<class Generated769.BaseClass1,class Generated769.BaseClass1>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated769.G2_C292`2<class Generated769.BaseClass1,class Generated769.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.T.T<T0,T1,(class Generated769.IBase2`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.T.T<T0,T1,(class Generated769.IBase2`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated769.IBase2`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.A.T<T1,(class Generated769.IBase2`2<class Generated769.BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.A.T<T1,(class Generated769.IBase2`2<class Generated769.BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated769.IBase2`2<class Generated769.BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.A.A<(class Generated769.IBase2`2<class Generated769.BaseClass0,class Generated769.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.A.A<(class Generated769.IBase2`2<class Generated769.BaseClass0,class Generated769.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated769.IBase2`2<class Generated769.BaseClass0,class Generated769.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.A.B<(class Generated769.IBase2`2<class Generated769.BaseClass0,class Generated769.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.A.B<(class Generated769.IBase2`2<class Generated769.BaseClass0,class Generated769.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated769.IBase2`2<class Generated769.BaseClass0,class Generated769.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.B.T<T1,(class Generated769.IBase2`2<class Generated769.BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.B.T<T1,(class Generated769.IBase2`2<class Generated769.BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated769.IBase2`2<class Generated769.BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.B.A<(class Generated769.IBase2`2<class Generated769.BaseClass1,class Generated769.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.B.A<(class Generated769.IBase2`2<class Generated769.BaseClass1,class Generated769.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated769.IBase2`2<class Generated769.BaseClass1,class Generated769.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.B.B<(class Generated769.IBase2`2<class Generated769.BaseClass1,class Generated769.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.B.B<(class Generated769.IBase2`2<class Generated769.BaseClass1,class Generated769.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated769.IBase2`2<class Generated769.BaseClass1,class Generated769.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C6.T.T<T0,T1,(class Generated769.G1_C6`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C6.T.T<T0,T1,(class Generated769.G1_C6`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated769.G1_C6`2<!!T0,!!T1>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated769.G1_C6`2<!!T0,!!T1>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated769.G1_C6`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C6.A.T<T1,(class Generated769.G1_C6`2<class Generated769.BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C6.A.T<T1,(class Generated769.G1_C6`2<class Generated769.BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated769.G1_C6`2<class Generated769.BaseClass0,!!T1>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated769.G1_C6`2<class Generated769.BaseClass0,!!T1>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated769.G1_C6`2<class Generated769.BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C6.A.A<(class Generated769.G1_C6`2<class Generated769.BaseClass0,class Generated769.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C6.A.A<(class Generated769.G1_C6`2<class Generated769.BaseClass0,class Generated769.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated769.G1_C6`2<class Generated769.BaseClass0,class Generated769.BaseClass0>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated769.G1_C6`2<class Generated769.BaseClass0,class Generated769.BaseClass0>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated769.G1_C6`2<class Generated769.BaseClass0,class Generated769.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C6.A.B<(class Generated769.G1_C6`2<class Generated769.BaseClass0,class Generated769.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C6.A.B<(class Generated769.G1_C6`2<class Generated769.BaseClass0,class Generated769.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated769.G1_C6`2<class Generated769.BaseClass0,class Generated769.BaseClass1>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated769.G1_C6`2<class Generated769.BaseClass0,class Generated769.BaseClass1>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated769.G1_C6`2<class Generated769.BaseClass0,class Generated769.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C6.B.T<T1,(class Generated769.G1_C6`2<class Generated769.BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C6.B.T<T1,(class Generated769.G1_C6`2<class Generated769.BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated769.G1_C6`2<class Generated769.BaseClass1,!!T1>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated769.G1_C6`2<class Generated769.BaseClass1,!!T1>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated769.G1_C6`2<class Generated769.BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C6.B.A<(class Generated769.G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C6.B.A<(class Generated769.G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated769.G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass0>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated769.G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass0>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated769.G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C6.B.B<(class Generated769.G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C6.B.B<(class Generated769.G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated769.G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass1>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated769.G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass1>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated769.G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.Generated769.IBase0<(Generated769.IBase0)W>(!!W inst, string exp) cil managed {
		.maxstack 9
		.locals init (string[] actualResults)
		ldc.i4.s    4
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.Generated769.IBase0<(Generated769.IBase0)W>(!!W inst, string exp)"
		ldc.i4.s 4
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string Generated769.IBase0::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string Generated769.IBase0::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string Generated769.IBase0::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string Generated769.IBase0::Method3<object>()
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
		newobj instance void class Generated769.G3_C1241`1<class Generated769.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated769.G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass0>
		callvirt instance string class Generated769.G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass0>::ClassMethod1327<object>()
		ldstr "G2_C292::ClassMethod1327.MI.7468<System.Object>()"
		ldstr "class G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass0> on type class Generated769.G3_C1241`1<class Generated769.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated769.G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass0>
		callvirt instance string class Generated769.G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass0>::ClassMethod1326()
		ldstr "G2_C292::ClassMethod1326.MI.7467()"
		ldstr "class G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass0> on type class Generated769.G3_C1241`1<class Generated769.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated769.G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass0>
		callvirt instance string class Generated769.G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass0>::Method7<object>()
		ldstr "G3_C1241::Method7.15066<System.Object>()"
		ldstr "class G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass0> on type class Generated769.G3_C1241`1<class Generated769.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated769.IBase2`2<class Generated769.BaseClass1,class Generated769.BaseClass1>::Method7<object>()
		ldstr "G3_C1241::Method7.15066<System.Object>()"
		ldstr "class IBase2`2<class Generated769.BaseClass1,class Generated769.BaseClass1> on type class Generated769.G3_C1241`1<class Generated769.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated769.IBase2`2<class Generated769.BaseClass0,class Generated769.BaseClass1>::Method7<object>()
		ldstr "G3_C1241::Method7.MI.15067<System.Object>()"
		ldstr "class IBase2`2<class Generated769.BaseClass0,class Generated769.BaseClass1> on type class Generated769.G3_C1241`1<class Generated769.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated769.G2_C292`2<class Generated769.BaseClass0,class Generated769.BaseClass1>
		callvirt instance string class Generated769.G2_C292`2<class Generated769.BaseClass0,class Generated769.BaseClass1>::ClassMethod1912<object>()
		ldstr "G2_C292::ClassMethod1912.7466<System.Object>()"
		ldstr "class G2_C292`2<class Generated769.BaseClass0,class Generated769.BaseClass1> on type class Generated769.G3_C1241`1<class Generated769.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated769.G2_C292`2<class Generated769.BaseClass0,class Generated769.BaseClass1>
		callvirt instance string class Generated769.G2_C292`2<class Generated769.BaseClass0,class Generated769.BaseClass1>::ClassMethod1911<object>()
		ldstr "G2_C292::ClassMethod1911.7465<System.Object>()"
		ldstr "class G2_C292`2<class Generated769.BaseClass0,class Generated769.BaseClass1> on type class Generated769.G3_C1241`1<class Generated769.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated769.G2_C292`2<class Generated769.BaseClass0,class Generated769.BaseClass1>
		callvirt instance string class Generated769.G2_C292`2<class Generated769.BaseClass0,class Generated769.BaseClass1>::Method3<object>()
		ldstr "G2_C292::Method3.7463<System.Object>()"
		ldstr "class G2_C292`2<class Generated769.BaseClass0,class Generated769.BaseClass1> on type class Generated769.G3_C1241`1<class Generated769.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated769.G2_C292`2<class Generated769.BaseClass0,class Generated769.BaseClass1>
		callvirt instance string class Generated769.G2_C292`2<class Generated769.BaseClass0,class Generated769.BaseClass1>::Method2<object>()
		ldstr "G2_C292::Method2.7462<System.Object>()"
		ldstr "class G2_C292`2<class Generated769.BaseClass0,class Generated769.BaseClass1> on type class Generated769.G3_C1241`1<class Generated769.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated769.G2_C292`2<class Generated769.BaseClass0,class Generated769.BaseClass1>
		callvirt instance string class Generated769.G2_C292`2<class Generated769.BaseClass0,class Generated769.BaseClass1>::Method1()
		ldstr "G2_C292::Method1.7460()"
		ldstr "class G2_C292`2<class Generated769.BaseClass0,class Generated769.BaseClass1> on type class Generated769.G3_C1241`1<class Generated769.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated769.G2_C292`2<class Generated769.BaseClass0,class Generated769.BaseClass1>
		callvirt instance string class Generated769.G2_C292`2<class Generated769.BaseClass0,class Generated769.BaseClass1>::Method0()
		ldstr "G2_C292::Method0.7458()"
		ldstr "class G2_C292`2<class Generated769.BaseClass0,class Generated769.BaseClass1> on type class Generated769.G3_C1241`1<class Generated769.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated769.G2_C292`2<class Generated769.BaseClass0,class Generated769.BaseClass1>
		callvirt instance string class Generated769.G2_C292`2<class Generated769.BaseClass0,class Generated769.BaseClass1>::Method7<object>()
		ldstr "G3_C1241::Method7.15066<System.Object>()"
		ldstr "class G2_C292`2<class Generated769.BaseClass0,class Generated769.BaseClass1> on type class Generated769.G3_C1241`1<class Generated769.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated769.G2_C292`2<class Generated769.BaseClass0,class Generated769.BaseClass1>
		callvirt instance string class Generated769.G2_C292`2<class Generated769.BaseClass0,class Generated769.BaseClass1>::ClassMethod1327<object>()
		ldstr "G2_C292::ClassMethod1327.MI.7468<System.Object>()"
		ldstr "class G2_C292`2<class Generated769.BaseClass0,class Generated769.BaseClass1> on type class Generated769.G3_C1241`1<class Generated769.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated769.G2_C292`2<class Generated769.BaseClass0,class Generated769.BaseClass1>
		callvirt instance string class Generated769.G2_C292`2<class Generated769.BaseClass0,class Generated769.BaseClass1>::ClassMethod1326()
		ldstr "G2_C292::ClassMethod1326.MI.7467()"
		ldstr "class G2_C292`2<class Generated769.BaseClass0,class Generated769.BaseClass1> on type class Generated769.G3_C1241`1<class Generated769.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated769.IBase2`2<class Generated769.BaseClass1,class Generated769.BaseClass0>::Method7<object>()
		ldstr "G3_C1241::Method7.15066<System.Object>()"
		ldstr "class IBase2`2<class Generated769.BaseClass1,class Generated769.BaseClass0> on type class Generated769.G3_C1241`1<class Generated769.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string Generated769.IBase0::Method0()
		ldstr "G2_C292::Method0.MI.7459()"
		ldstr "Generated769.IBase0 on type class Generated769.G3_C1241`1<class Generated769.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated769.IBase0::Method1()
		ldstr "G2_C292::Method1.MI.7461()"
		ldstr "Generated769.IBase0 on type class Generated769.G3_C1241`1<class Generated769.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated769.IBase0::Method2<object>()
		ldstr "G2_C292::Method2.7462<System.Object>()"
		ldstr "Generated769.IBase0 on type class Generated769.G3_C1241`1<class Generated769.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated769.IBase0::Method3<object>()
		ldstr "G2_C292::Method3.MI.7464<System.Object>()"
		ldstr "Generated769.IBase0 on type class Generated769.G3_C1241`1<class Generated769.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated769.IBase2`2<class Generated769.BaseClass0,class Generated769.BaseClass0>::Method7<object>()
		ldstr "G3_C1241::Method7.MI.15067<System.Object>()"
		ldstr "class IBase2`2<class Generated769.BaseClass0,class Generated769.BaseClass0> on type class Generated769.G3_C1241`1<class Generated769.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated769.G3_C1241`1<class Generated769.BaseClass0>
		callvirt instance string class Generated769.G3_C1241`1<class Generated769.BaseClass0>::ClassMethod3864()
		ldstr "G3_C1241::ClassMethod3864.15068()"
		ldstr "class G3_C1241`1<class Generated769.BaseClass0> on type class Generated769.G3_C1241`1<class Generated769.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated769.G3_C1241`1<class Generated769.BaseClass0>
		callvirt instance string class Generated769.G3_C1241`1<class Generated769.BaseClass0>::Method7<object>()
		ldstr "G3_C1241::Method7.15066<System.Object>()"
		ldstr "class G3_C1241`1<class Generated769.BaseClass0> on type class Generated769.G3_C1241`1<class Generated769.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated769.G3_C1241`1<class Generated769.BaseClass0>
		callvirt instance string class Generated769.G3_C1241`1<class Generated769.BaseClass0>::ClassMethod1912<object>()
		ldstr "G2_C292::ClassMethod1912.7466<System.Object>()"
		ldstr "class G3_C1241`1<class Generated769.BaseClass0> on type class Generated769.G3_C1241`1<class Generated769.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated769.G3_C1241`1<class Generated769.BaseClass0>
		callvirt instance string class Generated769.G3_C1241`1<class Generated769.BaseClass0>::ClassMethod1911<object>()
		ldstr "G2_C292::ClassMethod1911.7465<System.Object>()"
		ldstr "class G3_C1241`1<class Generated769.BaseClass0> on type class Generated769.G3_C1241`1<class Generated769.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated769.G3_C1241`1<class Generated769.BaseClass0>
		callvirt instance string class Generated769.G3_C1241`1<class Generated769.BaseClass0>::Method3<object>()
		ldstr "G2_C292::Method3.7463<System.Object>()"
		ldstr "class G3_C1241`1<class Generated769.BaseClass0> on type class Generated769.G3_C1241`1<class Generated769.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated769.G3_C1241`1<class Generated769.BaseClass0>
		callvirt instance string class Generated769.G3_C1241`1<class Generated769.BaseClass0>::Method2<object>()
		ldstr "G2_C292::Method2.7462<System.Object>()"
		ldstr "class G3_C1241`1<class Generated769.BaseClass0> on type class Generated769.G3_C1241`1<class Generated769.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated769.G3_C1241`1<class Generated769.BaseClass0>
		callvirt instance string class Generated769.G3_C1241`1<class Generated769.BaseClass0>::Method1()
		ldstr "G2_C292::Method1.7460()"
		ldstr "class G3_C1241`1<class Generated769.BaseClass0> on type class Generated769.G3_C1241`1<class Generated769.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated769.G3_C1241`1<class Generated769.BaseClass0>
		callvirt instance string class Generated769.G3_C1241`1<class Generated769.BaseClass0>::Method0()
		ldstr "G2_C292::Method0.7458()"
		ldstr "class G3_C1241`1<class Generated769.BaseClass0> on type class Generated769.G3_C1241`1<class Generated769.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated769.G3_C1241`1<class Generated769.BaseClass0>
		callvirt instance string class Generated769.G3_C1241`1<class Generated769.BaseClass0>::ClassMethod1327<object>()
		ldstr "G2_C292::ClassMethod1327.MI.7468<System.Object>()"
		ldstr "class G3_C1241`1<class Generated769.BaseClass0> on type class Generated769.G3_C1241`1<class Generated769.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated769.G3_C1241`1<class Generated769.BaseClass0>
		callvirt instance string class Generated769.G3_C1241`1<class Generated769.BaseClass0>::ClassMethod1326()
		ldstr "G2_C292::ClassMethod1326.MI.7467()"
		ldstr "class G3_C1241`1<class Generated769.BaseClass0> on type class Generated769.G3_C1241`1<class Generated769.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated769.G3_C1241`1<class Generated769.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated769.G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass0>
		callvirt instance string class Generated769.G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass0>::ClassMethod1327<object>()
		ldstr "G2_C292::ClassMethod1327.MI.7468<System.Object>()"
		ldstr "class G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass0> on type class Generated769.G3_C1241`1<class Generated769.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated769.G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass0>
		callvirt instance string class Generated769.G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass0>::ClassMethod1326()
		ldstr "G2_C292::ClassMethod1326.MI.7467()"
		ldstr "class G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass0> on type class Generated769.G3_C1241`1<class Generated769.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated769.G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass0>
		callvirt instance string class Generated769.G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass0>::Method7<object>()
		ldstr "G3_C1241::Method7.15066<System.Object>()"
		ldstr "class G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass0> on type class Generated769.G3_C1241`1<class Generated769.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated769.IBase2`2<class Generated769.BaseClass1,class Generated769.BaseClass1>::Method7<object>()
		ldstr "G3_C1241::Method7.15066<System.Object>()"
		ldstr "class IBase2`2<class Generated769.BaseClass1,class Generated769.BaseClass1> on type class Generated769.G3_C1241`1<class Generated769.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated769.IBase2`2<class Generated769.BaseClass0,class Generated769.BaseClass1>::Method7<object>()
		ldstr "G3_C1241::Method7.MI.15067<System.Object>()"
		ldstr "class IBase2`2<class Generated769.BaseClass0,class Generated769.BaseClass1> on type class Generated769.G3_C1241`1<class Generated769.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated769.G2_C292`2<class Generated769.BaseClass0,class Generated769.BaseClass1>
		callvirt instance string class Generated769.G2_C292`2<class Generated769.BaseClass0,class Generated769.BaseClass1>::ClassMethod1912<object>()
		ldstr "G2_C292::ClassMethod1912.7466<System.Object>()"
		ldstr "class G2_C292`2<class Generated769.BaseClass0,class Generated769.BaseClass1> on type class Generated769.G3_C1241`1<class Generated769.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated769.G2_C292`2<class Generated769.BaseClass0,class Generated769.BaseClass1>
		callvirt instance string class Generated769.G2_C292`2<class Generated769.BaseClass0,class Generated769.BaseClass1>::ClassMethod1911<object>()
		ldstr "G2_C292::ClassMethod1911.7465<System.Object>()"
		ldstr "class G2_C292`2<class Generated769.BaseClass0,class Generated769.BaseClass1> on type class Generated769.G3_C1241`1<class Generated769.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated769.G2_C292`2<class Generated769.BaseClass0,class Generated769.BaseClass1>
		callvirt instance string class Generated769.G2_C292`2<class Generated769.BaseClass0,class Generated769.BaseClass1>::Method3<object>()
		ldstr "G2_C292::Method3.7463<System.Object>()"
		ldstr "class G2_C292`2<class Generated769.BaseClass0,class Generated769.BaseClass1> on type class Generated769.G3_C1241`1<class Generated769.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated769.G2_C292`2<class Generated769.BaseClass0,class Generated769.BaseClass1>
		callvirt instance string class Generated769.G2_C292`2<class Generated769.BaseClass0,class Generated769.BaseClass1>::Method2<object>()
		ldstr "G2_C292::Method2.7462<System.Object>()"
		ldstr "class G2_C292`2<class Generated769.BaseClass0,class Generated769.BaseClass1> on type class Generated769.G3_C1241`1<class Generated769.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated769.G2_C292`2<class Generated769.BaseClass0,class Generated769.BaseClass1>
		callvirt instance string class Generated769.G2_C292`2<class Generated769.BaseClass0,class Generated769.BaseClass1>::Method1()
		ldstr "G2_C292::Method1.7460()"
		ldstr "class G2_C292`2<class Generated769.BaseClass0,class Generated769.BaseClass1> on type class Generated769.G3_C1241`1<class Generated769.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated769.G2_C292`2<class Generated769.BaseClass0,class Generated769.BaseClass1>
		callvirt instance string class Generated769.G2_C292`2<class Generated769.BaseClass0,class Generated769.BaseClass1>::Method0()
		ldstr "G2_C292::Method0.7458()"
		ldstr "class G2_C292`2<class Generated769.BaseClass0,class Generated769.BaseClass1> on type class Generated769.G3_C1241`1<class Generated769.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated769.G2_C292`2<class Generated769.BaseClass0,class Generated769.BaseClass1>
		callvirt instance string class Generated769.G2_C292`2<class Generated769.BaseClass0,class Generated769.BaseClass1>::Method7<object>()
		ldstr "G3_C1241::Method7.15066<System.Object>()"
		ldstr "class G2_C292`2<class Generated769.BaseClass0,class Generated769.BaseClass1> on type class Generated769.G3_C1241`1<class Generated769.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated769.G2_C292`2<class Generated769.BaseClass0,class Generated769.BaseClass1>
		callvirt instance string class Generated769.G2_C292`2<class Generated769.BaseClass0,class Generated769.BaseClass1>::ClassMethod1327<object>()
		ldstr "G2_C292::ClassMethod1327.MI.7468<System.Object>()"
		ldstr "class G2_C292`2<class Generated769.BaseClass0,class Generated769.BaseClass1> on type class Generated769.G3_C1241`1<class Generated769.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated769.G2_C292`2<class Generated769.BaseClass0,class Generated769.BaseClass1>
		callvirt instance string class Generated769.G2_C292`2<class Generated769.BaseClass0,class Generated769.BaseClass1>::ClassMethod1326()
		ldstr "G2_C292::ClassMethod1326.MI.7467()"
		ldstr "class G2_C292`2<class Generated769.BaseClass0,class Generated769.BaseClass1> on type class Generated769.G3_C1241`1<class Generated769.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated769.IBase2`2<class Generated769.BaseClass1,class Generated769.BaseClass0>::Method7<object>()
		ldstr "G3_C1241::Method7.15066<System.Object>()"
		ldstr "class IBase2`2<class Generated769.BaseClass1,class Generated769.BaseClass0> on type class Generated769.G3_C1241`1<class Generated769.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string Generated769.IBase0::Method0()
		ldstr "G2_C292::Method0.MI.7459()"
		ldstr "Generated769.IBase0 on type class Generated769.G3_C1241`1<class Generated769.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated769.IBase0::Method1()
		ldstr "G2_C292::Method1.MI.7461()"
		ldstr "Generated769.IBase0 on type class Generated769.G3_C1241`1<class Generated769.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated769.IBase0::Method2<object>()
		ldstr "G2_C292::Method2.7462<System.Object>()"
		ldstr "Generated769.IBase0 on type class Generated769.G3_C1241`1<class Generated769.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated769.IBase0::Method3<object>()
		ldstr "G2_C292::Method3.MI.7464<System.Object>()"
		ldstr "Generated769.IBase0 on type class Generated769.G3_C1241`1<class Generated769.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated769.IBase2`2<class Generated769.BaseClass0,class Generated769.BaseClass0>::Method7<object>()
		ldstr "G3_C1241::Method7.15066<System.Object>()"
		ldstr "class IBase2`2<class Generated769.BaseClass0,class Generated769.BaseClass0> on type class Generated769.G3_C1241`1<class Generated769.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated769.G3_C1241`1<class Generated769.BaseClass1>
		callvirt instance string class Generated769.G3_C1241`1<class Generated769.BaseClass1>::ClassMethod3864()
		ldstr "G3_C1241::ClassMethod3864.15068()"
		ldstr "class G3_C1241`1<class Generated769.BaseClass1> on type class Generated769.G3_C1241`1<class Generated769.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated769.G3_C1241`1<class Generated769.BaseClass1>
		callvirt instance string class Generated769.G3_C1241`1<class Generated769.BaseClass1>::Method7<object>()
		ldstr "G3_C1241::Method7.15066<System.Object>()"
		ldstr "class G3_C1241`1<class Generated769.BaseClass1> on type class Generated769.G3_C1241`1<class Generated769.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated769.G3_C1241`1<class Generated769.BaseClass1>
		callvirt instance string class Generated769.G3_C1241`1<class Generated769.BaseClass1>::ClassMethod1912<object>()
		ldstr "G2_C292::ClassMethod1912.7466<System.Object>()"
		ldstr "class G3_C1241`1<class Generated769.BaseClass1> on type class Generated769.G3_C1241`1<class Generated769.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated769.G3_C1241`1<class Generated769.BaseClass1>
		callvirt instance string class Generated769.G3_C1241`1<class Generated769.BaseClass1>::ClassMethod1911<object>()
		ldstr "G2_C292::ClassMethod1911.7465<System.Object>()"
		ldstr "class G3_C1241`1<class Generated769.BaseClass1> on type class Generated769.G3_C1241`1<class Generated769.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated769.G3_C1241`1<class Generated769.BaseClass1>
		callvirt instance string class Generated769.G3_C1241`1<class Generated769.BaseClass1>::Method3<object>()
		ldstr "G2_C292::Method3.7463<System.Object>()"
		ldstr "class G3_C1241`1<class Generated769.BaseClass1> on type class Generated769.G3_C1241`1<class Generated769.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated769.G3_C1241`1<class Generated769.BaseClass1>
		callvirt instance string class Generated769.G3_C1241`1<class Generated769.BaseClass1>::Method2<object>()
		ldstr "G2_C292::Method2.7462<System.Object>()"
		ldstr "class G3_C1241`1<class Generated769.BaseClass1> on type class Generated769.G3_C1241`1<class Generated769.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated769.G3_C1241`1<class Generated769.BaseClass1>
		callvirt instance string class Generated769.G3_C1241`1<class Generated769.BaseClass1>::Method1()
		ldstr "G2_C292::Method1.7460()"
		ldstr "class G3_C1241`1<class Generated769.BaseClass1> on type class Generated769.G3_C1241`1<class Generated769.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated769.G3_C1241`1<class Generated769.BaseClass1>
		callvirt instance string class Generated769.G3_C1241`1<class Generated769.BaseClass1>::Method0()
		ldstr "G2_C292::Method0.7458()"
		ldstr "class G3_C1241`1<class Generated769.BaseClass1> on type class Generated769.G3_C1241`1<class Generated769.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated769.G3_C1241`1<class Generated769.BaseClass1>
		callvirt instance string class Generated769.G3_C1241`1<class Generated769.BaseClass1>::ClassMethod1327<object>()
		ldstr "G2_C292::ClassMethod1327.MI.7468<System.Object>()"
		ldstr "class G3_C1241`1<class Generated769.BaseClass1> on type class Generated769.G3_C1241`1<class Generated769.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated769.G3_C1241`1<class Generated769.BaseClass1>
		callvirt instance string class Generated769.G3_C1241`1<class Generated769.BaseClass1>::ClassMethod1326()
		ldstr "G2_C292::ClassMethod1326.MI.7467()"
		ldstr "class G3_C1241`1<class Generated769.BaseClass1> on type class Generated769.G3_C1241`1<class Generated769.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated769.G1_C6`2<class Generated769.BaseClass0,class Generated769.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated769.G1_C6`2<class Generated769.BaseClass0,class Generated769.BaseClass0>
		callvirt instance string class Generated769.G1_C6`2<class Generated769.BaseClass0,class Generated769.BaseClass0>::ClassMethod1327<object>()
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G1_C6`2<class Generated769.BaseClass0,class Generated769.BaseClass0> on type class Generated769.G1_C6`2<class Generated769.BaseClass0,class Generated769.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated769.G1_C6`2<class Generated769.BaseClass0,class Generated769.BaseClass0>
		callvirt instance string class Generated769.G1_C6`2<class Generated769.BaseClass0,class Generated769.BaseClass0>::ClassMethod1326()
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G1_C6`2<class Generated769.BaseClass0,class Generated769.BaseClass0> on type class Generated769.G1_C6`2<class Generated769.BaseClass0,class Generated769.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated769.G1_C6`2<class Generated769.BaseClass0,class Generated769.BaseClass0>
		callvirt instance string class Generated769.G1_C6`2<class Generated769.BaseClass0,class Generated769.BaseClass0>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class G1_C6`2<class Generated769.BaseClass0,class Generated769.BaseClass0> on type class Generated769.G1_C6`2<class Generated769.BaseClass0,class Generated769.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated769.IBase2`2<class Generated769.BaseClass1,class Generated769.BaseClass0>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated769.BaseClass1,class Generated769.BaseClass0> on type class Generated769.G1_C6`2<class Generated769.BaseClass0,class Generated769.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated769.IBase2`2<class Generated769.BaseClass0,class Generated769.BaseClass0>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated769.BaseClass0,class Generated769.BaseClass0> on type class Generated769.G1_C6`2<class Generated769.BaseClass0,class Generated769.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated769.IBase2`2<class Generated769.BaseClass0,class Generated769.BaseClass1>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated769.BaseClass0,class Generated769.BaseClass1> on type class Generated769.G1_C6`2<class Generated769.BaseClass0,class Generated769.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated769.IBase2`2<class Generated769.BaseClass1,class Generated769.BaseClass1>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated769.BaseClass1,class Generated769.BaseClass1> on type class Generated769.G1_C6`2<class Generated769.BaseClass0,class Generated769.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated769.G1_C6`2<class Generated769.BaseClass0,class Generated769.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated769.G1_C6`2<class Generated769.BaseClass0,class Generated769.BaseClass1>
		callvirt instance string class Generated769.G1_C6`2<class Generated769.BaseClass0,class Generated769.BaseClass1>::ClassMethod1327<object>()
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G1_C6`2<class Generated769.BaseClass0,class Generated769.BaseClass1> on type class Generated769.G1_C6`2<class Generated769.BaseClass0,class Generated769.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated769.G1_C6`2<class Generated769.BaseClass0,class Generated769.BaseClass1>
		callvirt instance string class Generated769.G1_C6`2<class Generated769.BaseClass0,class Generated769.BaseClass1>::ClassMethod1326()
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G1_C6`2<class Generated769.BaseClass0,class Generated769.BaseClass1> on type class Generated769.G1_C6`2<class Generated769.BaseClass0,class Generated769.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated769.G1_C6`2<class Generated769.BaseClass0,class Generated769.BaseClass1>
		callvirt instance string class Generated769.G1_C6`2<class Generated769.BaseClass0,class Generated769.BaseClass1>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class G1_C6`2<class Generated769.BaseClass0,class Generated769.BaseClass1> on type class Generated769.G1_C6`2<class Generated769.BaseClass0,class Generated769.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated769.IBase2`2<class Generated769.BaseClass1,class Generated769.BaseClass0>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated769.BaseClass1,class Generated769.BaseClass0> on type class Generated769.G1_C6`2<class Generated769.BaseClass0,class Generated769.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated769.IBase2`2<class Generated769.BaseClass0,class Generated769.BaseClass0>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated769.BaseClass0,class Generated769.BaseClass0> on type class Generated769.G1_C6`2<class Generated769.BaseClass0,class Generated769.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated769.IBase2`2<class Generated769.BaseClass0,class Generated769.BaseClass1>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated769.BaseClass0,class Generated769.BaseClass1> on type class Generated769.G1_C6`2<class Generated769.BaseClass0,class Generated769.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated769.IBase2`2<class Generated769.BaseClass1,class Generated769.BaseClass1>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated769.BaseClass1,class Generated769.BaseClass1> on type class Generated769.G1_C6`2<class Generated769.BaseClass0,class Generated769.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated769.G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated769.G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass0>
		callvirt instance string class Generated769.G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass0>::ClassMethod1327<object>()
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass0> on type class Generated769.G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated769.G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass0>
		callvirt instance string class Generated769.G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass0>::ClassMethod1326()
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass0> on type class Generated769.G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated769.G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass0>
		callvirt instance string class Generated769.G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass0>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass0> on type class Generated769.G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated769.IBase2`2<class Generated769.BaseClass1,class Generated769.BaseClass1>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated769.BaseClass1,class Generated769.BaseClass1> on type class Generated769.G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated769.IBase2`2<class Generated769.BaseClass0,class Generated769.BaseClass1>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated769.BaseClass0,class Generated769.BaseClass1> on type class Generated769.G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated769.G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated769.G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass1>
		callvirt instance string class Generated769.G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass1>::ClassMethod1327<object>()
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass1> on type class Generated769.G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated769.G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass1>
		callvirt instance string class Generated769.G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass1>::ClassMethod1326()
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass1> on type class Generated769.G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated769.G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass1>
		callvirt instance string class Generated769.G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass1>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass1> on type class Generated769.G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated769.IBase2`2<class Generated769.BaseClass1,class Generated769.BaseClass1>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated769.BaseClass1,class Generated769.BaseClass1> on type class Generated769.G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated769.IBase2`2<class Generated769.BaseClass0,class Generated769.BaseClass1>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated769.BaseClass0,class Generated769.BaseClass1> on type class Generated769.G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass1>"
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
		newobj instance void class Generated769.G3_C1241`1<class Generated769.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C292::ClassMethod1326.MI.7467()#G2_C292::ClassMethod1327.MI.7468<System.Object>()#G3_C1241::Method7.15066<System.Object>()#"
		call void Generated769.Generated769::M.G1_C6.T.T<class Generated769.BaseClass1,class Generated769.BaseClass0,class Generated769.G3_C1241`1<class Generated769.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C292::ClassMethod1326.MI.7467()#G2_C292::ClassMethod1327.MI.7468<System.Object>()#G3_C1241::Method7.15066<System.Object>()#"
		call void Generated769.Generated769::M.G1_C6.B.T<class Generated769.BaseClass0,class Generated769.G3_C1241`1<class Generated769.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C292::ClassMethod1326.MI.7467()#G2_C292::ClassMethod1327.MI.7468<System.Object>()#G3_C1241::Method7.15066<System.Object>()#"
		call void Generated769.Generated769::M.G1_C6.B.A<class Generated769.G3_C1241`1<class Generated769.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1241::Method7.15066<System.Object>()#"
		call void Generated769.Generated769::M.IBase2.T.T<class Generated769.BaseClass1,class Generated769.BaseClass1,class Generated769.G3_C1241`1<class Generated769.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G3_C1241::Method7.15066<System.Object>()#"
		call void Generated769.Generated769::M.IBase2.B.T<class Generated769.BaseClass1,class Generated769.G3_C1241`1<class Generated769.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G3_C1241::Method7.15066<System.Object>()#"
		call void Generated769.Generated769::M.IBase2.B.B<class Generated769.G3_C1241`1<class Generated769.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1241::Method7.MI.15067<System.Object>()#"
		call void Generated769.Generated769::M.IBase2.T.T<class Generated769.BaseClass0,class Generated769.BaseClass1,class Generated769.G3_C1241`1<class Generated769.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G3_C1241::Method7.MI.15067<System.Object>()#"
		call void Generated769.Generated769::M.IBase2.A.T<class Generated769.BaseClass1,class Generated769.G3_C1241`1<class Generated769.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G3_C1241::Method7.MI.15067<System.Object>()#"
		call void Generated769.Generated769::M.IBase2.A.B<class Generated769.G3_C1241`1<class Generated769.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C292::ClassMethod1326.MI.7467()#G2_C292::ClassMethod1327.MI.7468<System.Object>()#G2_C292::ClassMethod1911.7465<System.Object>()#G2_C292::ClassMethod1912.7466<System.Object>()#G2_C292::Method0.7458()#G2_C292::Method1.7460()#G2_C292::Method2.7462<System.Object>()#G2_C292::Method3.7463<System.Object>()#G3_C1241::Method7.15066<System.Object>()#"
		call void Generated769.Generated769::M.G2_C292.T.T<class Generated769.BaseClass0,class Generated769.BaseClass1,class Generated769.G3_C1241`1<class Generated769.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C292::ClassMethod1326.MI.7467()#G2_C292::ClassMethod1327.MI.7468<System.Object>()#G2_C292::ClassMethod1911.7465<System.Object>()#G2_C292::ClassMethod1912.7466<System.Object>()#G2_C292::Method0.7458()#G2_C292::Method1.7460()#G2_C292::Method2.7462<System.Object>()#G2_C292::Method3.7463<System.Object>()#G3_C1241::Method7.15066<System.Object>()#"
		call void Generated769.Generated769::M.G2_C292.A.T<class Generated769.BaseClass1,class Generated769.G3_C1241`1<class Generated769.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C292::ClassMethod1326.MI.7467()#G2_C292::ClassMethod1327.MI.7468<System.Object>()#G2_C292::ClassMethod1911.7465<System.Object>()#G2_C292::ClassMethod1912.7466<System.Object>()#G2_C292::Method0.7458()#G2_C292::Method1.7460()#G2_C292::Method2.7462<System.Object>()#G2_C292::Method3.7463<System.Object>()#G3_C1241::Method7.15066<System.Object>()#"
		call void Generated769.Generated769::M.G2_C292.A.B<class Generated769.G3_C1241`1<class Generated769.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1241::Method7.15066<System.Object>()#"
		call void Generated769.Generated769::M.IBase2.T.T<class Generated769.BaseClass1,class Generated769.BaseClass0,class Generated769.G3_C1241`1<class Generated769.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G3_C1241::Method7.15066<System.Object>()#"
		call void Generated769.Generated769::M.IBase2.B.T<class Generated769.BaseClass0,class Generated769.G3_C1241`1<class Generated769.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G3_C1241::Method7.15066<System.Object>()#"
		call void Generated769.Generated769::M.IBase2.B.A<class Generated769.G3_C1241`1<class Generated769.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C292::Method0.MI.7459()#G2_C292::Method1.MI.7461()#G2_C292::Method2.7462<System.Object>()#G2_C292::Method3.MI.7464<System.Object>()#"
		call void Generated769.Generated769::M.Generated769.IBase0<class Generated769.G3_C1241`1<class Generated769.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1241::Method7.MI.15067<System.Object>()#"
		call void Generated769.Generated769::M.IBase2.T.T<class Generated769.BaseClass0,class Generated769.BaseClass0,class Generated769.G3_C1241`1<class Generated769.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G3_C1241::Method7.MI.15067<System.Object>()#"
		call void Generated769.Generated769::M.IBase2.A.T<class Generated769.BaseClass0,class Generated769.G3_C1241`1<class Generated769.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G3_C1241::Method7.MI.15067<System.Object>()#"
		call void Generated769.Generated769::M.IBase2.A.A<class Generated769.G3_C1241`1<class Generated769.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C292::ClassMethod1326.MI.7467()#G2_C292::ClassMethod1327.MI.7468<System.Object>()#G2_C292::ClassMethod1911.7465<System.Object>()#G2_C292::ClassMethod1912.7466<System.Object>()#G3_C1241::ClassMethod3864.15068()#G2_C292::Method0.7458()#G2_C292::Method1.7460()#G2_C292::Method2.7462<System.Object>()#G2_C292::Method3.7463<System.Object>()#G3_C1241::Method7.15066<System.Object>()#"
		call void Generated769.Generated769::M.G3_C1241.T<class Generated769.BaseClass0,class Generated769.G3_C1241`1<class Generated769.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C292::ClassMethod1326.MI.7467()#G2_C292::ClassMethod1327.MI.7468<System.Object>()#G2_C292::ClassMethod1911.7465<System.Object>()#G2_C292::ClassMethod1912.7466<System.Object>()#G3_C1241::ClassMethod3864.15068()#G2_C292::Method0.7458()#G2_C292::Method1.7460()#G2_C292::Method2.7462<System.Object>()#G2_C292::Method3.7463<System.Object>()#G3_C1241::Method7.15066<System.Object>()#"
		call void Generated769.Generated769::M.G3_C1241.A<class Generated769.G3_C1241`1<class Generated769.BaseClass0>>(!!0,string)
		newobj instance void class Generated769.G3_C1241`1<class Generated769.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C292::ClassMethod1326.MI.7467()#G2_C292::ClassMethod1327.MI.7468<System.Object>()#G3_C1241::Method7.15066<System.Object>()#"
		call void Generated769.Generated769::M.G1_C6.T.T<class Generated769.BaseClass1,class Generated769.BaseClass0,class Generated769.G3_C1241`1<class Generated769.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C292::ClassMethod1326.MI.7467()#G2_C292::ClassMethod1327.MI.7468<System.Object>()#G3_C1241::Method7.15066<System.Object>()#"
		call void Generated769.Generated769::M.G1_C6.B.T<class Generated769.BaseClass0,class Generated769.G3_C1241`1<class Generated769.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C292::ClassMethod1326.MI.7467()#G2_C292::ClassMethod1327.MI.7468<System.Object>()#G3_C1241::Method7.15066<System.Object>()#"
		call void Generated769.Generated769::M.G1_C6.B.A<class Generated769.G3_C1241`1<class Generated769.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1241::Method7.15066<System.Object>()#"
		call void Generated769.Generated769::M.IBase2.T.T<class Generated769.BaseClass1,class Generated769.BaseClass1,class Generated769.G3_C1241`1<class Generated769.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G3_C1241::Method7.15066<System.Object>()#"
		call void Generated769.Generated769::M.IBase2.B.T<class Generated769.BaseClass1,class Generated769.G3_C1241`1<class Generated769.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1241::Method7.15066<System.Object>()#"
		call void Generated769.Generated769::M.IBase2.B.B<class Generated769.G3_C1241`1<class Generated769.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1241::Method7.MI.15067<System.Object>()#"
		call void Generated769.Generated769::M.IBase2.T.T<class Generated769.BaseClass0,class Generated769.BaseClass1,class Generated769.G3_C1241`1<class Generated769.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G3_C1241::Method7.MI.15067<System.Object>()#"
		call void Generated769.Generated769::M.IBase2.A.T<class Generated769.BaseClass1,class Generated769.G3_C1241`1<class Generated769.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1241::Method7.MI.15067<System.Object>()#"
		call void Generated769.Generated769::M.IBase2.A.B<class Generated769.G3_C1241`1<class Generated769.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C292::ClassMethod1326.MI.7467()#G2_C292::ClassMethod1327.MI.7468<System.Object>()#G2_C292::ClassMethod1911.7465<System.Object>()#G2_C292::ClassMethod1912.7466<System.Object>()#G2_C292::Method0.7458()#G2_C292::Method1.7460()#G2_C292::Method2.7462<System.Object>()#G2_C292::Method3.7463<System.Object>()#G3_C1241::Method7.15066<System.Object>()#"
		call void Generated769.Generated769::M.G2_C292.T.T<class Generated769.BaseClass0,class Generated769.BaseClass1,class Generated769.G3_C1241`1<class Generated769.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C292::ClassMethod1326.MI.7467()#G2_C292::ClassMethod1327.MI.7468<System.Object>()#G2_C292::ClassMethod1911.7465<System.Object>()#G2_C292::ClassMethod1912.7466<System.Object>()#G2_C292::Method0.7458()#G2_C292::Method1.7460()#G2_C292::Method2.7462<System.Object>()#G2_C292::Method3.7463<System.Object>()#G3_C1241::Method7.15066<System.Object>()#"
		call void Generated769.Generated769::M.G2_C292.A.T<class Generated769.BaseClass1,class Generated769.G3_C1241`1<class Generated769.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C292::ClassMethod1326.MI.7467()#G2_C292::ClassMethod1327.MI.7468<System.Object>()#G2_C292::ClassMethod1911.7465<System.Object>()#G2_C292::ClassMethod1912.7466<System.Object>()#G2_C292::Method0.7458()#G2_C292::Method1.7460()#G2_C292::Method2.7462<System.Object>()#G2_C292::Method3.7463<System.Object>()#G3_C1241::Method7.15066<System.Object>()#"
		call void Generated769.Generated769::M.G2_C292.A.B<class Generated769.G3_C1241`1<class Generated769.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1241::Method7.15066<System.Object>()#"
		call void Generated769.Generated769::M.IBase2.T.T<class Generated769.BaseClass1,class Generated769.BaseClass0,class Generated769.G3_C1241`1<class Generated769.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G3_C1241::Method7.15066<System.Object>()#"
		call void Generated769.Generated769::M.IBase2.B.T<class Generated769.BaseClass0,class Generated769.G3_C1241`1<class Generated769.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1241::Method7.15066<System.Object>()#"
		call void Generated769.Generated769::M.IBase2.B.A<class Generated769.G3_C1241`1<class Generated769.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C292::Method0.MI.7459()#G2_C292::Method1.MI.7461()#G2_C292::Method2.7462<System.Object>()#G2_C292::Method3.MI.7464<System.Object>()#"
		call void Generated769.Generated769::M.Generated769.IBase0<class Generated769.G3_C1241`1<class Generated769.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1241::Method7.15066<System.Object>()#"
		call void Generated769.Generated769::M.IBase2.T.T<class Generated769.BaseClass0,class Generated769.BaseClass0,class Generated769.G3_C1241`1<class Generated769.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G3_C1241::Method7.15066<System.Object>()#"
		call void Generated769.Generated769::M.IBase2.A.T<class Generated769.BaseClass0,class Generated769.G3_C1241`1<class Generated769.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1241::Method7.15066<System.Object>()#"
		call void Generated769.Generated769::M.IBase2.A.A<class Generated769.G3_C1241`1<class Generated769.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C292::ClassMethod1326.MI.7467()#G2_C292::ClassMethod1327.MI.7468<System.Object>()#G2_C292::ClassMethod1911.7465<System.Object>()#G2_C292::ClassMethod1912.7466<System.Object>()#G3_C1241::ClassMethod3864.15068()#G2_C292::Method0.7458()#G2_C292::Method1.7460()#G2_C292::Method2.7462<System.Object>()#G2_C292::Method3.7463<System.Object>()#G3_C1241::Method7.15066<System.Object>()#"
		call void Generated769.Generated769::M.G3_C1241.T<class Generated769.BaseClass1,class Generated769.G3_C1241`1<class Generated769.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C292::ClassMethod1326.MI.7467()#G2_C292::ClassMethod1327.MI.7468<System.Object>()#G2_C292::ClassMethod1911.7465<System.Object>()#G2_C292::ClassMethod1912.7466<System.Object>()#G3_C1241::ClassMethod3864.15068()#G2_C292::Method0.7458()#G2_C292::Method1.7460()#G2_C292::Method2.7462<System.Object>()#G2_C292::Method3.7463<System.Object>()#G3_C1241::Method7.15066<System.Object>()#"
		call void Generated769.Generated769::M.G3_C1241.B<class Generated769.G3_C1241`1<class Generated769.BaseClass1>>(!!0,string)
		newobj instance void class Generated769.G1_C6`2<class Generated769.BaseClass0,class Generated769.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated769.Generated769::M.G1_C6.T.T<class Generated769.BaseClass0,class Generated769.BaseClass0,class Generated769.G1_C6`2<class Generated769.BaseClass0,class Generated769.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated769.Generated769::M.G1_C6.A.T<class Generated769.BaseClass0,class Generated769.G1_C6`2<class Generated769.BaseClass0,class Generated769.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated769.Generated769::M.G1_C6.A.A<class Generated769.G1_C6`2<class Generated769.BaseClass0,class Generated769.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated769.Generated769::M.IBase2.T.T<class Generated769.BaseClass1,class Generated769.BaseClass0,class Generated769.G1_C6`2<class Generated769.BaseClass0,class Generated769.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated769.Generated769::M.IBase2.B.T<class Generated769.BaseClass0,class Generated769.G1_C6`2<class Generated769.BaseClass0,class Generated769.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated769.Generated769::M.IBase2.B.A<class Generated769.G1_C6`2<class Generated769.BaseClass0,class Generated769.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated769.Generated769::M.IBase2.T.T<class Generated769.BaseClass0,class Generated769.BaseClass0,class Generated769.G1_C6`2<class Generated769.BaseClass0,class Generated769.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated769.Generated769::M.IBase2.A.T<class Generated769.BaseClass0,class Generated769.G1_C6`2<class Generated769.BaseClass0,class Generated769.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated769.Generated769::M.IBase2.A.A<class Generated769.G1_C6`2<class Generated769.BaseClass0,class Generated769.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated769.Generated769::M.IBase2.T.T<class Generated769.BaseClass0,class Generated769.BaseClass1,class Generated769.G1_C6`2<class Generated769.BaseClass0,class Generated769.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated769.Generated769::M.IBase2.A.T<class Generated769.BaseClass1,class Generated769.G1_C6`2<class Generated769.BaseClass0,class Generated769.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated769.Generated769::M.IBase2.A.B<class Generated769.G1_C6`2<class Generated769.BaseClass0,class Generated769.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated769.Generated769::M.IBase2.T.T<class Generated769.BaseClass1,class Generated769.BaseClass1,class Generated769.G1_C6`2<class Generated769.BaseClass0,class Generated769.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated769.Generated769::M.IBase2.B.T<class Generated769.BaseClass1,class Generated769.G1_C6`2<class Generated769.BaseClass0,class Generated769.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated769.Generated769::M.IBase2.B.B<class Generated769.G1_C6`2<class Generated769.BaseClass0,class Generated769.BaseClass0>>(!!0,string)
		newobj instance void class Generated769.G1_C6`2<class Generated769.BaseClass0,class Generated769.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated769.Generated769::M.G1_C6.T.T<class Generated769.BaseClass0,class Generated769.BaseClass1,class Generated769.G1_C6`2<class Generated769.BaseClass0,class Generated769.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated769.Generated769::M.G1_C6.A.T<class Generated769.BaseClass1,class Generated769.G1_C6`2<class Generated769.BaseClass0,class Generated769.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated769.Generated769::M.G1_C6.A.B<class Generated769.G1_C6`2<class Generated769.BaseClass0,class Generated769.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated769.Generated769::M.IBase2.T.T<class Generated769.BaseClass1,class Generated769.BaseClass0,class Generated769.G1_C6`2<class Generated769.BaseClass0,class Generated769.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated769.Generated769::M.IBase2.B.T<class Generated769.BaseClass0,class Generated769.G1_C6`2<class Generated769.BaseClass0,class Generated769.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated769.Generated769::M.IBase2.B.A<class Generated769.G1_C6`2<class Generated769.BaseClass0,class Generated769.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated769.Generated769::M.IBase2.T.T<class Generated769.BaseClass0,class Generated769.BaseClass0,class Generated769.G1_C6`2<class Generated769.BaseClass0,class Generated769.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated769.Generated769::M.IBase2.A.T<class Generated769.BaseClass0,class Generated769.G1_C6`2<class Generated769.BaseClass0,class Generated769.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated769.Generated769::M.IBase2.A.A<class Generated769.G1_C6`2<class Generated769.BaseClass0,class Generated769.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated769.Generated769::M.IBase2.T.T<class Generated769.BaseClass0,class Generated769.BaseClass1,class Generated769.G1_C6`2<class Generated769.BaseClass0,class Generated769.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated769.Generated769::M.IBase2.A.T<class Generated769.BaseClass1,class Generated769.G1_C6`2<class Generated769.BaseClass0,class Generated769.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated769.Generated769::M.IBase2.A.B<class Generated769.G1_C6`2<class Generated769.BaseClass0,class Generated769.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated769.Generated769::M.IBase2.T.T<class Generated769.BaseClass1,class Generated769.BaseClass1,class Generated769.G1_C6`2<class Generated769.BaseClass0,class Generated769.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated769.Generated769::M.IBase2.B.T<class Generated769.BaseClass1,class Generated769.G1_C6`2<class Generated769.BaseClass0,class Generated769.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated769.Generated769::M.IBase2.B.B<class Generated769.G1_C6`2<class Generated769.BaseClass0,class Generated769.BaseClass1>>(!!0,string)
		newobj instance void class Generated769.G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated769.Generated769::M.G1_C6.T.T<class Generated769.BaseClass1,class Generated769.BaseClass0,class Generated769.G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated769.Generated769::M.G1_C6.B.T<class Generated769.BaseClass0,class Generated769.G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated769.Generated769::M.G1_C6.B.A<class Generated769.G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated769.Generated769::M.IBase2.T.T<class Generated769.BaseClass1,class Generated769.BaseClass1,class Generated769.G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated769.Generated769::M.IBase2.B.T<class Generated769.BaseClass1,class Generated769.G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated769.Generated769::M.IBase2.B.B<class Generated769.G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated769.Generated769::M.IBase2.T.T<class Generated769.BaseClass0,class Generated769.BaseClass1,class Generated769.G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated769.Generated769::M.IBase2.A.T<class Generated769.BaseClass1,class Generated769.G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated769.Generated769::M.IBase2.A.B<class Generated769.G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass0>>(!!0,string)
		newobj instance void class Generated769.G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated769.Generated769::M.G1_C6.T.T<class Generated769.BaseClass1,class Generated769.BaseClass1,class Generated769.G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated769.Generated769::M.G1_C6.B.T<class Generated769.BaseClass1,class Generated769.G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated769.Generated769::M.G1_C6.B.B<class Generated769.G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated769.Generated769::M.IBase2.T.T<class Generated769.BaseClass1,class Generated769.BaseClass1,class Generated769.G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated769.Generated769::M.IBase2.B.T<class Generated769.BaseClass1,class Generated769.G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated769.Generated769::M.IBase2.B.B<class Generated769.G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated769.Generated769::M.IBase2.T.T<class Generated769.BaseClass0,class Generated769.BaseClass1,class Generated769.G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated769.Generated769::M.IBase2.A.T<class Generated769.BaseClass1,class Generated769.G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated769.Generated769::M.IBase2.A.B<class Generated769.G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass1>>(!!0,string)
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
		newobj instance void class Generated769.G3_C1241`1<class Generated769.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated769.G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated769.G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass0>::ClassMethod1327<object>()
		calli default string(class Generated769.G3_C1241`1<class Generated769.BaseClass0>)
		ldstr "G2_C292::ClassMethod1327.MI.7468<System.Object>()"
		ldstr "class G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass0> on type class Generated769.G3_C1241`1<class Generated769.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated769.G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated769.G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass0>::ClassMethod1326()
		calli default string(class Generated769.G3_C1241`1<class Generated769.BaseClass0>)
		ldstr "G2_C292::ClassMethod1326.MI.7467()"
		ldstr "class G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass0> on type class Generated769.G3_C1241`1<class Generated769.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated769.G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated769.G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass0>::Method7<object>()
		calli default string(class Generated769.G3_C1241`1<class Generated769.BaseClass0>)
		ldstr "G3_C1241::Method7.15066<System.Object>()"
		ldstr "class G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass0> on type class Generated769.G3_C1241`1<class Generated769.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated769.IBase2`2<class Generated769.BaseClass1,class Generated769.BaseClass1>::Method7<object>()
		calli default string(class Generated769.G3_C1241`1<class Generated769.BaseClass0>)
		ldstr "G3_C1241::Method7.15066<System.Object>()"
		ldstr "class IBase2`2<class Generated769.BaseClass1,class Generated769.BaseClass1> on type class Generated769.G3_C1241`1<class Generated769.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated769.IBase2`2<class Generated769.BaseClass0,class Generated769.BaseClass1>::Method7<object>()
		calli default string(class Generated769.G3_C1241`1<class Generated769.BaseClass0>)
		ldstr "G3_C1241::Method7.MI.15067<System.Object>()"
		ldstr "class IBase2`2<class Generated769.BaseClass0,class Generated769.BaseClass1> on type class Generated769.G3_C1241`1<class Generated769.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated769.G2_C292`2<class Generated769.BaseClass0,class Generated769.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated769.G2_C292`2<class Generated769.BaseClass0,class Generated769.BaseClass1>::ClassMethod1912<object>()
		calli default string(class Generated769.G3_C1241`1<class Generated769.BaseClass0>)
		ldstr "G2_C292::ClassMethod1912.7466<System.Object>()"
		ldstr "class G2_C292`2<class Generated769.BaseClass0,class Generated769.BaseClass1> on type class Generated769.G3_C1241`1<class Generated769.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated769.G2_C292`2<class Generated769.BaseClass0,class Generated769.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated769.G2_C292`2<class Generated769.BaseClass0,class Generated769.BaseClass1>::ClassMethod1911<object>()
		calli default string(class Generated769.G3_C1241`1<class Generated769.BaseClass0>)
		ldstr "G2_C292::ClassMethod1911.7465<System.Object>()"
		ldstr "class G2_C292`2<class Generated769.BaseClass0,class Generated769.BaseClass1> on type class Generated769.G3_C1241`1<class Generated769.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated769.G2_C292`2<class Generated769.BaseClass0,class Generated769.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated769.G2_C292`2<class Generated769.BaseClass0,class Generated769.BaseClass1>::Method3<object>()
		calli default string(class Generated769.G3_C1241`1<class Generated769.BaseClass0>)
		ldstr "G2_C292::Method3.7463<System.Object>()"
		ldstr "class G2_C292`2<class Generated769.BaseClass0,class Generated769.BaseClass1> on type class Generated769.G3_C1241`1<class Generated769.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated769.G2_C292`2<class Generated769.BaseClass0,class Generated769.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated769.G2_C292`2<class Generated769.BaseClass0,class Generated769.BaseClass1>::Method2<object>()
		calli default string(class Generated769.G3_C1241`1<class Generated769.BaseClass0>)
		ldstr "G2_C292::Method2.7462<System.Object>()"
		ldstr "class G2_C292`2<class Generated769.BaseClass0,class Generated769.BaseClass1> on type class Generated769.G3_C1241`1<class Generated769.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated769.G2_C292`2<class Generated769.BaseClass0,class Generated769.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated769.G2_C292`2<class Generated769.BaseClass0,class Generated769.BaseClass1>::Method1()
		calli default string(class Generated769.G3_C1241`1<class Generated769.BaseClass0>)
		ldstr "G2_C292::Method1.7460()"
		ldstr "class G2_C292`2<class Generated769.BaseClass0,class Generated769.BaseClass1> on type class Generated769.G3_C1241`1<class Generated769.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated769.G2_C292`2<class Generated769.BaseClass0,class Generated769.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated769.G2_C292`2<class Generated769.BaseClass0,class Generated769.BaseClass1>::Method0()
		calli default string(class Generated769.G3_C1241`1<class Generated769.BaseClass0>)
		ldstr "G2_C292::Method0.7458()"
		ldstr "class G2_C292`2<class Generated769.BaseClass0,class Generated769.BaseClass1> on type class Generated769.G3_C1241`1<class Generated769.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated769.G2_C292`2<class Generated769.BaseClass0,class Generated769.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated769.G2_C292`2<class Generated769.BaseClass0,class Generated769.BaseClass1>::Method7<object>()
		calli default string(class Generated769.G3_C1241`1<class Generated769.BaseClass0>)
		ldstr "G3_C1241::Method7.15066<System.Object>()"
		ldstr "class G2_C292`2<class Generated769.BaseClass0,class Generated769.BaseClass1> on type class Generated769.G3_C1241`1<class Generated769.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated769.G2_C292`2<class Generated769.BaseClass0,class Generated769.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated769.G2_C292`2<class Generated769.BaseClass0,class Generated769.BaseClass1>::ClassMethod1327<object>()
		calli default string(class Generated769.G3_C1241`1<class Generated769.BaseClass0>)
		ldstr "G2_C292::ClassMethod1327.MI.7468<System.Object>()"
		ldstr "class G2_C292`2<class Generated769.BaseClass0,class Generated769.BaseClass1> on type class Generated769.G3_C1241`1<class Generated769.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated769.G2_C292`2<class Generated769.BaseClass0,class Generated769.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated769.G2_C292`2<class Generated769.BaseClass0,class Generated769.BaseClass1>::ClassMethod1326()
		calli default string(class Generated769.G3_C1241`1<class Generated769.BaseClass0>)
		ldstr "G2_C292::ClassMethod1326.MI.7467()"
		ldstr "class G2_C292`2<class Generated769.BaseClass0,class Generated769.BaseClass1> on type class Generated769.G3_C1241`1<class Generated769.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated769.IBase2`2<class Generated769.BaseClass1,class Generated769.BaseClass0>::Method7<object>()
		calli default string(class Generated769.G3_C1241`1<class Generated769.BaseClass0>)
		ldstr "G3_C1241::Method7.15066<System.Object>()"
		ldstr "class IBase2`2<class Generated769.BaseClass1,class Generated769.BaseClass0> on type class Generated769.G3_C1241`1<class Generated769.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated769.IBase0::Method0()
		calli default string(class Generated769.G3_C1241`1<class Generated769.BaseClass0>)
		ldstr "G2_C292::Method0.MI.7459()"
		ldstr "Generated769.IBase0 on type class Generated769.G3_C1241`1<class Generated769.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated769.IBase0::Method1()
		calli default string(class Generated769.G3_C1241`1<class Generated769.BaseClass0>)
		ldstr "G2_C292::Method1.MI.7461()"
		ldstr "Generated769.IBase0 on type class Generated769.G3_C1241`1<class Generated769.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated769.IBase0::Method2<object>()
		calli default string(class Generated769.G3_C1241`1<class Generated769.BaseClass0>)
		ldstr "G2_C292::Method2.7462<System.Object>()"
		ldstr "Generated769.IBase0 on type class Generated769.G3_C1241`1<class Generated769.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated769.IBase0::Method3<object>()
		calli default string(class Generated769.G3_C1241`1<class Generated769.BaseClass0>)
		ldstr "G2_C292::Method3.MI.7464<System.Object>()"
		ldstr "Generated769.IBase0 on type class Generated769.G3_C1241`1<class Generated769.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated769.IBase2`2<class Generated769.BaseClass0,class Generated769.BaseClass0>::Method7<object>()
		calli default string(class Generated769.G3_C1241`1<class Generated769.BaseClass0>)
		ldstr "G3_C1241::Method7.MI.15067<System.Object>()"
		ldstr "class IBase2`2<class Generated769.BaseClass0,class Generated769.BaseClass0> on type class Generated769.G3_C1241`1<class Generated769.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated769.G3_C1241`1<class Generated769.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated769.G3_C1241`1<class Generated769.BaseClass0>::ClassMethod3864()
		calli default string(class Generated769.G3_C1241`1<class Generated769.BaseClass0>)
		ldstr "G3_C1241::ClassMethod3864.15068()"
		ldstr "class G3_C1241`1<class Generated769.BaseClass0> on type class Generated769.G3_C1241`1<class Generated769.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated769.G3_C1241`1<class Generated769.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated769.G3_C1241`1<class Generated769.BaseClass0>::Method7<object>()
		calli default string(class Generated769.G3_C1241`1<class Generated769.BaseClass0>)
		ldstr "G3_C1241::Method7.15066<System.Object>()"
		ldstr "class G3_C1241`1<class Generated769.BaseClass0> on type class Generated769.G3_C1241`1<class Generated769.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated769.G3_C1241`1<class Generated769.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated769.G3_C1241`1<class Generated769.BaseClass0>::ClassMethod1912<object>()
		calli default string(class Generated769.G3_C1241`1<class Generated769.BaseClass0>)
		ldstr "G2_C292::ClassMethod1912.7466<System.Object>()"
		ldstr "class G3_C1241`1<class Generated769.BaseClass0> on type class Generated769.G3_C1241`1<class Generated769.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated769.G3_C1241`1<class Generated769.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated769.G3_C1241`1<class Generated769.BaseClass0>::ClassMethod1911<object>()
		calli default string(class Generated769.G3_C1241`1<class Generated769.BaseClass0>)
		ldstr "G2_C292::ClassMethod1911.7465<System.Object>()"
		ldstr "class G3_C1241`1<class Generated769.BaseClass0> on type class Generated769.G3_C1241`1<class Generated769.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated769.G3_C1241`1<class Generated769.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated769.G3_C1241`1<class Generated769.BaseClass0>::Method3<object>()
		calli default string(class Generated769.G3_C1241`1<class Generated769.BaseClass0>)
		ldstr "G2_C292::Method3.7463<System.Object>()"
		ldstr "class G3_C1241`1<class Generated769.BaseClass0> on type class Generated769.G3_C1241`1<class Generated769.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated769.G3_C1241`1<class Generated769.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated769.G3_C1241`1<class Generated769.BaseClass0>::Method2<object>()
		calli default string(class Generated769.G3_C1241`1<class Generated769.BaseClass0>)
		ldstr "G2_C292::Method2.7462<System.Object>()"
		ldstr "class G3_C1241`1<class Generated769.BaseClass0> on type class Generated769.G3_C1241`1<class Generated769.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated769.G3_C1241`1<class Generated769.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated769.G3_C1241`1<class Generated769.BaseClass0>::Method1()
		calli default string(class Generated769.G3_C1241`1<class Generated769.BaseClass0>)
		ldstr "G2_C292::Method1.7460()"
		ldstr "class G3_C1241`1<class Generated769.BaseClass0> on type class Generated769.G3_C1241`1<class Generated769.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated769.G3_C1241`1<class Generated769.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated769.G3_C1241`1<class Generated769.BaseClass0>::Method0()
		calli default string(class Generated769.G3_C1241`1<class Generated769.BaseClass0>)
		ldstr "G2_C292::Method0.7458()"
		ldstr "class G3_C1241`1<class Generated769.BaseClass0> on type class Generated769.G3_C1241`1<class Generated769.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated769.G3_C1241`1<class Generated769.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated769.G3_C1241`1<class Generated769.BaseClass0>::ClassMethod1327<object>()
		calli default string(class Generated769.G3_C1241`1<class Generated769.BaseClass0>)
		ldstr "G2_C292::ClassMethod1327.MI.7468<System.Object>()"
		ldstr "class G3_C1241`1<class Generated769.BaseClass0> on type class Generated769.G3_C1241`1<class Generated769.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated769.G3_C1241`1<class Generated769.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated769.G3_C1241`1<class Generated769.BaseClass0>::ClassMethod1326()
		calli default string(class Generated769.G3_C1241`1<class Generated769.BaseClass0>)
		ldstr "G2_C292::ClassMethod1326.MI.7467()"
		ldstr "class G3_C1241`1<class Generated769.BaseClass0> on type class Generated769.G3_C1241`1<class Generated769.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated769.G3_C1241`1<class Generated769.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated769.G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated769.G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass0>::ClassMethod1327<object>()
		calli default string(class Generated769.G3_C1241`1<class Generated769.BaseClass1>)
		ldstr "G2_C292::ClassMethod1327.MI.7468<System.Object>()"
		ldstr "class G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass0> on type class Generated769.G3_C1241`1<class Generated769.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated769.G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated769.G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass0>::ClassMethod1326()
		calli default string(class Generated769.G3_C1241`1<class Generated769.BaseClass1>)
		ldstr "G2_C292::ClassMethod1326.MI.7467()"
		ldstr "class G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass0> on type class Generated769.G3_C1241`1<class Generated769.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated769.G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated769.G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass0>::Method7<object>()
		calli default string(class Generated769.G3_C1241`1<class Generated769.BaseClass1>)
		ldstr "G3_C1241::Method7.15066<System.Object>()"
		ldstr "class G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass0> on type class Generated769.G3_C1241`1<class Generated769.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated769.IBase2`2<class Generated769.BaseClass1,class Generated769.BaseClass1>::Method7<object>()
		calli default string(class Generated769.G3_C1241`1<class Generated769.BaseClass1>)
		ldstr "G3_C1241::Method7.15066<System.Object>()"
		ldstr "class IBase2`2<class Generated769.BaseClass1,class Generated769.BaseClass1> on type class Generated769.G3_C1241`1<class Generated769.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated769.IBase2`2<class Generated769.BaseClass0,class Generated769.BaseClass1>::Method7<object>()
		calli default string(class Generated769.G3_C1241`1<class Generated769.BaseClass1>)
		ldstr "G3_C1241::Method7.MI.15067<System.Object>()"
		ldstr "class IBase2`2<class Generated769.BaseClass0,class Generated769.BaseClass1> on type class Generated769.G3_C1241`1<class Generated769.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated769.G2_C292`2<class Generated769.BaseClass0,class Generated769.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated769.G2_C292`2<class Generated769.BaseClass0,class Generated769.BaseClass1>::ClassMethod1912<object>()
		calli default string(class Generated769.G3_C1241`1<class Generated769.BaseClass1>)
		ldstr "G2_C292::ClassMethod1912.7466<System.Object>()"
		ldstr "class G2_C292`2<class Generated769.BaseClass0,class Generated769.BaseClass1> on type class Generated769.G3_C1241`1<class Generated769.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated769.G2_C292`2<class Generated769.BaseClass0,class Generated769.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated769.G2_C292`2<class Generated769.BaseClass0,class Generated769.BaseClass1>::ClassMethod1911<object>()
		calli default string(class Generated769.G3_C1241`1<class Generated769.BaseClass1>)
		ldstr "G2_C292::ClassMethod1911.7465<System.Object>()"
		ldstr "class G2_C292`2<class Generated769.BaseClass0,class Generated769.BaseClass1> on type class Generated769.G3_C1241`1<class Generated769.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated769.G2_C292`2<class Generated769.BaseClass0,class Generated769.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated769.G2_C292`2<class Generated769.BaseClass0,class Generated769.BaseClass1>::Method3<object>()
		calli default string(class Generated769.G3_C1241`1<class Generated769.BaseClass1>)
		ldstr "G2_C292::Method3.7463<System.Object>()"
		ldstr "class G2_C292`2<class Generated769.BaseClass0,class Generated769.BaseClass1> on type class Generated769.G3_C1241`1<class Generated769.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated769.G2_C292`2<class Generated769.BaseClass0,class Generated769.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated769.G2_C292`2<class Generated769.BaseClass0,class Generated769.BaseClass1>::Method2<object>()
		calli default string(class Generated769.G3_C1241`1<class Generated769.BaseClass1>)
		ldstr "G2_C292::Method2.7462<System.Object>()"
		ldstr "class G2_C292`2<class Generated769.BaseClass0,class Generated769.BaseClass1> on type class Generated769.G3_C1241`1<class Generated769.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated769.G2_C292`2<class Generated769.BaseClass0,class Generated769.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated769.G2_C292`2<class Generated769.BaseClass0,class Generated769.BaseClass1>::Method1()
		calli default string(class Generated769.G3_C1241`1<class Generated769.BaseClass1>)
		ldstr "G2_C292::Method1.7460()"
		ldstr "class G2_C292`2<class Generated769.BaseClass0,class Generated769.BaseClass1> on type class Generated769.G3_C1241`1<class Generated769.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated769.G2_C292`2<class Generated769.BaseClass0,class Generated769.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated769.G2_C292`2<class Generated769.BaseClass0,class Generated769.BaseClass1>::Method0()
		calli default string(class Generated769.G3_C1241`1<class Generated769.BaseClass1>)
		ldstr "G2_C292::Method0.7458()"
		ldstr "class G2_C292`2<class Generated769.BaseClass0,class Generated769.BaseClass1> on type class Generated769.G3_C1241`1<class Generated769.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated769.G2_C292`2<class Generated769.BaseClass0,class Generated769.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated769.G2_C292`2<class Generated769.BaseClass0,class Generated769.BaseClass1>::Method7<object>()
		calli default string(class Generated769.G3_C1241`1<class Generated769.BaseClass1>)
		ldstr "G3_C1241::Method7.15066<System.Object>()"
		ldstr "class G2_C292`2<class Generated769.BaseClass0,class Generated769.BaseClass1> on type class Generated769.G3_C1241`1<class Generated769.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated769.G2_C292`2<class Generated769.BaseClass0,class Generated769.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated769.G2_C292`2<class Generated769.BaseClass0,class Generated769.BaseClass1>::ClassMethod1327<object>()
		calli default string(class Generated769.G3_C1241`1<class Generated769.BaseClass1>)
		ldstr "G2_C292::ClassMethod1327.MI.7468<System.Object>()"
		ldstr "class G2_C292`2<class Generated769.BaseClass0,class Generated769.BaseClass1> on type class Generated769.G3_C1241`1<class Generated769.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated769.G2_C292`2<class Generated769.BaseClass0,class Generated769.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated769.G2_C292`2<class Generated769.BaseClass0,class Generated769.BaseClass1>::ClassMethod1326()
		calli default string(class Generated769.G3_C1241`1<class Generated769.BaseClass1>)
		ldstr "G2_C292::ClassMethod1326.MI.7467()"
		ldstr "class G2_C292`2<class Generated769.BaseClass0,class Generated769.BaseClass1> on type class Generated769.G3_C1241`1<class Generated769.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated769.IBase2`2<class Generated769.BaseClass1,class Generated769.BaseClass0>::Method7<object>()
		calli default string(class Generated769.G3_C1241`1<class Generated769.BaseClass1>)
		ldstr "G3_C1241::Method7.15066<System.Object>()"
		ldstr "class IBase2`2<class Generated769.BaseClass1,class Generated769.BaseClass0> on type class Generated769.G3_C1241`1<class Generated769.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated769.IBase0::Method0()
		calli default string(class Generated769.G3_C1241`1<class Generated769.BaseClass1>)
		ldstr "G2_C292::Method0.MI.7459()"
		ldstr "Generated769.IBase0 on type class Generated769.G3_C1241`1<class Generated769.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated769.IBase0::Method1()
		calli default string(class Generated769.G3_C1241`1<class Generated769.BaseClass1>)
		ldstr "G2_C292::Method1.MI.7461()"
		ldstr "Generated769.IBase0 on type class Generated769.G3_C1241`1<class Generated769.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated769.IBase0::Method2<object>()
		calli default string(class Generated769.G3_C1241`1<class Generated769.BaseClass1>)
		ldstr "G2_C292::Method2.7462<System.Object>()"
		ldstr "Generated769.IBase0 on type class Generated769.G3_C1241`1<class Generated769.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated769.IBase0::Method3<object>()
		calli default string(class Generated769.G3_C1241`1<class Generated769.BaseClass1>)
		ldstr "G2_C292::Method3.MI.7464<System.Object>()"
		ldstr "Generated769.IBase0 on type class Generated769.G3_C1241`1<class Generated769.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated769.IBase2`2<class Generated769.BaseClass0,class Generated769.BaseClass0>::Method7<object>()
		calli default string(class Generated769.G3_C1241`1<class Generated769.BaseClass1>)
		ldstr "G3_C1241::Method7.15066<System.Object>()"
		ldstr "class IBase2`2<class Generated769.BaseClass0,class Generated769.BaseClass0> on type class Generated769.G3_C1241`1<class Generated769.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated769.G3_C1241`1<class Generated769.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated769.G3_C1241`1<class Generated769.BaseClass1>::ClassMethod3864()
		calli default string(class Generated769.G3_C1241`1<class Generated769.BaseClass1>)
		ldstr "G3_C1241::ClassMethod3864.15068()"
		ldstr "class G3_C1241`1<class Generated769.BaseClass1> on type class Generated769.G3_C1241`1<class Generated769.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated769.G3_C1241`1<class Generated769.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated769.G3_C1241`1<class Generated769.BaseClass1>::Method7<object>()
		calli default string(class Generated769.G3_C1241`1<class Generated769.BaseClass1>)
		ldstr "G3_C1241::Method7.15066<System.Object>()"
		ldstr "class G3_C1241`1<class Generated769.BaseClass1> on type class Generated769.G3_C1241`1<class Generated769.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated769.G3_C1241`1<class Generated769.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated769.G3_C1241`1<class Generated769.BaseClass1>::ClassMethod1912<object>()
		calli default string(class Generated769.G3_C1241`1<class Generated769.BaseClass1>)
		ldstr "G2_C292::ClassMethod1912.7466<System.Object>()"
		ldstr "class G3_C1241`1<class Generated769.BaseClass1> on type class Generated769.G3_C1241`1<class Generated769.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated769.G3_C1241`1<class Generated769.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated769.G3_C1241`1<class Generated769.BaseClass1>::ClassMethod1911<object>()
		calli default string(class Generated769.G3_C1241`1<class Generated769.BaseClass1>)
		ldstr "G2_C292::ClassMethod1911.7465<System.Object>()"
		ldstr "class G3_C1241`1<class Generated769.BaseClass1> on type class Generated769.G3_C1241`1<class Generated769.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated769.G3_C1241`1<class Generated769.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated769.G3_C1241`1<class Generated769.BaseClass1>::Method3<object>()
		calli default string(class Generated769.G3_C1241`1<class Generated769.BaseClass1>)
		ldstr "G2_C292::Method3.7463<System.Object>()"
		ldstr "class G3_C1241`1<class Generated769.BaseClass1> on type class Generated769.G3_C1241`1<class Generated769.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated769.G3_C1241`1<class Generated769.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated769.G3_C1241`1<class Generated769.BaseClass1>::Method2<object>()
		calli default string(class Generated769.G3_C1241`1<class Generated769.BaseClass1>)
		ldstr "G2_C292::Method2.7462<System.Object>()"
		ldstr "class G3_C1241`1<class Generated769.BaseClass1> on type class Generated769.G3_C1241`1<class Generated769.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated769.G3_C1241`1<class Generated769.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated769.G3_C1241`1<class Generated769.BaseClass1>::Method1()
		calli default string(class Generated769.G3_C1241`1<class Generated769.BaseClass1>)
		ldstr "G2_C292::Method1.7460()"
		ldstr "class G3_C1241`1<class Generated769.BaseClass1> on type class Generated769.G3_C1241`1<class Generated769.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated769.G3_C1241`1<class Generated769.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated769.G3_C1241`1<class Generated769.BaseClass1>::Method0()
		calli default string(class Generated769.G3_C1241`1<class Generated769.BaseClass1>)
		ldstr "G2_C292::Method0.7458()"
		ldstr "class G3_C1241`1<class Generated769.BaseClass1> on type class Generated769.G3_C1241`1<class Generated769.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated769.G3_C1241`1<class Generated769.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated769.G3_C1241`1<class Generated769.BaseClass1>::ClassMethod1327<object>()
		calli default string(class Generated769.G3_C1241`1<class Generated769.BaseClass1>)
		ldstr "G2_C292::ClassMethod1327.MI.7468<System.Object>()"
		ldstr "class G3_C1241`1<class Generated769.BaseClass1> on type class Generated769.G3_C1241`1<class Generated769.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated769.G3_C1241`1<class Generated769.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated769.G3_C1241`1<class Generated769.BaseClass1>::ClassMethod1326()
		calli default string(class Generated769.G3_C1241`1<class Generated769.BaseClass1>)
		ldstr "G2_C292::ClassMethod1326.MI.7467()"
		ldstr "class G3_C1241`1<class Generated769.BaseClass1> on type class Generated769.G3_C1241`1<class Generated769.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated769.G1_C6`2<class Generated769.BaseClass0,class Generated769.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated769.G1_C6`2<class Generated769.BaseClass0,class Generated769.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated769.G1_C6`2<class Generated769.BaseClass0,class Generated769.BaseClass0>::ClassMethod1327<object>()
		calli default string(class Generated769.G1_C6`2<class Generated769.BaseClass0,class Generated769.BaseClass0>)
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G1_C6`2<class Generated769.BaseClass0,class Generated769.BaseClass0> on type class Generated769.G1_C6`2<class Generated769.BaseClass0,class Generated769.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated769.G1_C6`2<class Generated769.BaseClass0,class Generated769.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated769.G1_C6`2<class Generated769.BaseClass0,class Generated769.BaseClass0>::ClassMethod1326()
		calli default string(class Generated769.G1_C6`2<class Generated769.BaseClass0,class Generated769.BaseClass0>)
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G1_C6`2<class Generated769.BaseClass0,class Generated769.BaseClass0> on type class Generated769.G1_C6`2<class Generated769.BaseClass0,class Generated769.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated769.G1_C6`2<class Generated769.BaseClass0,class Generated769.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated769.G1_C6`2<class Generated769.BaseClass0,class Generated769.BaseClass0>::Method7<object>()
		calli default string(class Generated769.G1_C6`2<class Generated769.BaseClass0,class Generated769.BaseClass0>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class G1_C6`2<class Generated769.BaseClass0,class Generated769.BaseClass0> on type class Generated769.G1_C6`2<class Generated769.BaseClass0,class Generated769.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated769.IBase2`2<class Generated769.BaseClass1,class Generated769.BaseClass0>::Method7<object>()
		calli default string(class Generated769.G1_C6`2<class Generated769.BaseClass0,class Generated769.BaseClass0>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated769.BaseClass1,class Generated769.BaseClass0> on type class Generated769.G1_C6`2<class Generated769.BaseClass0,class Generated769.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated769.IBase2`2<class Generated769.BaseClass0,class Generated769.BaseClass0>::Method7<object>()
		calli default string(class Generated769.G1_C6`2<class Generated769.BaseClass0,class Generated769.BaseClass0>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated769.BaseClass0,class Generated769.BaseClass0> on type class Generated769.G1_C6`2<class Generated769.BaseClass0,class Generated769.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated769.IBase2`2<class Generated769.BaseClass0,class Generated769.BaseClass1>::Method7<object>()
		calli default string(class Generated769.G1_C6`2<class Generated769.BaseClass0,class Generated769.BaseClass0>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated769.BaseClass0,class Generated769.BaseClass1> on type class Generated769.G1_C6`2<class Generated769.BaseClass0,class Generated769.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated769.IBase2`2<class Generated769.BaseClass1,class Generated769.BaseClass1>::Method7<object>()
		calli default string(class Generated769.G1_C6`2<class Generated769.BaseClass0,class Generated769.BaseClass0>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated769.BaseClass1,class Generated769.BaseClass1> on type class Generated769.G1_C6`2<class Generated769.BaseClass0,class Generated769.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated769.G1_C6`2<class Generated769.BaseClass0,class Generated769.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated769.G1_C6`2<class Generated769.BaseClass0,class Generated769.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated769.G1_C6`2<class Generated769.BaseClass0,class Generated769.BaseClass1>::ClassMethod1327<object>()
		calli default string(class Generated769.G1_C6`2<class Generated769.BaseClass0,class Generated769.BaseClass1>)
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G1_C6`2<class Generated769.BaseClass0,class Generated769.BaseClass1> on type class Generated769.G1_C6`2<class Generated769.BaseClass0,class Generated769.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated769.G1_C6`2<class Generated769.BaseClass0,class Generated769.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated769.G1_C6`2<class Generated769.BaseClass0,class Generated769.BaseClass1>::ClassMethod1326()
		calli default string(class Generated769.G1_C6`2<class Generated769.BaseClass0,class Generated769.BaseClass1>)
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G1_C6`2<class Generated769.BaseClass0,class Generated769.BaseClass1> on type class Generated769.G1_C6`2<class Generated769.BaseClass0,class Generated769.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated769.G1_C6`2<class Generated769.BaseClass0,class Generated769.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated769.G1_C6`2<class Generated769.BaseClass0,class Generated769.BaseClass1>::Method7<object>()
		calli default string(class Generated769.G1_C6`2<class Generated769.BaseClass0,class Generated769.BaseClass1>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class G1_C6`2<class Generated769.BaseClass0,class Generated769.BaseClass1> on type class Generated769.G1_C6`2<class Generated769.BaseClass0,class Generated769.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated769.IBase2`2<class Generated769.BaseClass1,class Generated769.BaseClass0>::Method7<object>()
		calli default string(class Generated769.G1_C6`2<class Generated769.BaseClass0,class Generated769.BaseClass1>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated769.BaseClass1,class Generated769.BaseClass0> on type class Generated769.G1_C6`2<class Generated769.BaseClass0,class Generated769.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated769.IBase2`2<class Generated769.BaseClass0,class Generated769.BaseClass0>::Method7<object>()
		calli default string(class Generated769.G1_C6`2<class Generated769.BaseClass0,class Generated769.BaseClass1>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated769.BaseClass0,class Generated769.BaseClass0> on type class Generated769.G1_C6`2<class Generated769.BaseClass0,class Generated769.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated769.IBase2`2<class Generated769.BaseClass0,class Generated769.BaseClass1>::Method7<object>()
		calli default string(class Generated769.G1_C6`2<class Generated769.BaseClass0,class Generated769.BaseClass1>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated769.BaseClass0,class Generated769.BaseClass1> on type class Generated769.G1_C6`2<class Generated769.BaseClass0,class Generated769.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated769.IBase2`2<class Generated769.BaseClass1,class Generated769.BaseClass1>::Method7<object>()
		calli default string(class Generated769.G1_C6`2<class Generated769.BaseClass0,class Generated769.BaseClass1>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated769.BaseClass1,class Generated769.BaseClass1> on type class Generated769.G1_C6`2<class Generated769.BaseClass0,class Generated769.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated769.G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated769.G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated769.G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass0>::ClassMethod1327<object>()
		calli default string(class Generated769.G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass0>)
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass0> on type class Generated769.G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated769.G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated769.G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass0>::ClassMethod1326()
		calli default string(class Generated769.G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass0>)
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass0> on type class Generated769.G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated769.G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated769.G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass0>::Method7<object>()
		calli default string(class Generated769.G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass0>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass0> on type class Generated769.G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated769.IBase2`2<class Generated769.BaseClass1,class Generated769.BaseClass1>::Method7<object>()
		calli default string(class Generated769.G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass0>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated769.BaseClass1,class Generated769.BaseClass1> on type class Generated769.G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated769.IBase2`2<class Generated769.BaseClass0,class Generated769.BaseClass1>::Method7<object>()
		calli default string(class Generated769.G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass0>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated769.BaseClass0,class Generated769.BaseClass1> on type class Generated769.G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated769.G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated769.G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated769.G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass1>::ClassMethod1327<object>()
		calli default string(class Generated769.G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass1>)
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass1> on type class Generated769.G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated769.G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated769.G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass1>::ClassMethod1326()
		calli default string(class Generated769.G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass1>)
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass1> on type class Generated769.G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated769.G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated769.G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass1>::Method7<object>()
		calli default string(class Generated769.G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass1>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass1> on type class Generated769.G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated769.IBase2`2<class Generated769.BaseClass1,class Generated769.BaseClass1>::Method7<object>()
		calli default string(class Generated769.G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass1>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated769.BaseClass1,class Generated769.BaseClass1> on type class Generated769.G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated769.IBase2`2<class Generated769.BaseClass0,class Generated769.BaseClass1>::Method7<object>()
		calli default string(class Generated769.G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass1>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated769.BaseClass0,class Generated769.BaseClass1> on type class Generated769.G1_C6`2<class Generated769.BaseClass1,class Generated769.BaseClass1>"
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
				.maxstack  10
		call void Generated769.Generated769::MethodCallingTest()
		call void Generated769.Generated769::ConstrainedCallsTest()
		call void Generated769.Generated769::StructConstrainedInterfaceCallsTest()
		call void Generated769.Generated769::CalliTest()
		ldc.i4 100
		ret
	}
}
