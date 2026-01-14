// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) .ver 4:0:0:0 }
.assembly extern TestFramework { .publickeytoken = ( B0 3F 5F 7F 11 D5 0A 3A )  }

//TYPES IN FORWARDER ASSEMBLIES:

//TEST ASSEMBLY:
.assembly extern xunit.core {}

.class public Generated1291.BaseClass0 
{
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void [mscorlib]System.Object::.ctor()
		ret
	}
}
.class public Generated1291.BaseClass1 
		extends Generated1291.BaseClass0
{
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void Generated1291.BaseClass0::.ctor()
		ret
	}
}
.class public Generated1291.G3_C1763`1<T0> 
		extends class Generated1291.G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>
		implements Generated1291.IBase0 
{
	.method public hidebysig virtual instance string Method0() cil managed noinlining { 
		ldstr "G3_C1763::Method0.17854()"
		ret
	}
	.method public hidebysig newslot virtual instance string Method1() cil managed noinlining { 
		ldstr "G3_C1763::Method1.17855()"
		ret
	}
	.method public hidebysig newslot virtual instance string Method2<M0>() cil managed noinlining { 
		ldstr "G3_C1763::Method2.17856<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string Method3<M0>() cil managed noinlining { 
		ldstr "G3_C1763::Method3.17857<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod4928() cil managed noinlining { 
		ldstr "G3_C1763::ClassMethod4928.17858()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'G2_C715<class Generated1291.BaseClass1,class Generated1291.BaseClass0>.ClassMethod2796'<M0>() cil managed noinlining { 
		.override method instance string class Generated1291.G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>::ClassMethod2796<[1]>()
		ldstr "G3_C1763::ClassMethod2796.MI.17859<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class Generated1291.G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>::.ctor()
		ret
	}
}
.class public abstract Generated1291.G2_C715`2<T0, T1> 
		extends class Generated1291.G1_C13`2<!T0,!T1>
		implements class Generated1291.IBase2`2<class Generated1291.BaseClass1,!T1> 
{
	.method public hidebysig newslot virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G2_C715::Method7.11587<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod2794() cil managed noinlining { 
		ldstr "G2_C715::ClassMethod2794.11588()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod2795() cil managed noinlining { 
		ldstr "G2_C715::ClassMethod2795.11589()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod2796<M0>() cil managed noinlining { 
		ldstr "G2_C715::ClassMethod2796.11590<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class Generated1291.G1_C13`2<!T0,!T1>::.ctor()
		ret
	}
}
.class interface public abstract Generated1291.IBase0 
{
	.method public hidebysig newslot abstract virtual instance string Method0() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method1() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method2<M0>() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method3<M0>() cil managed { }
}
.class public abstract Generated1291.G1_C13`2<T0, T1> 
		implements class Generated1291.IBase2`2<!T0,!T1>, class Generated1291.IBase1`1<!T0> 
{
	.method public hidebysig virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G1_C13::Method7.4871<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string Method4() cil managed noinlining { 
		ldstr "G1_C13::Method4.4872()"
		ret
	}
	.method public hidebysig newslot virtual instance string Method5() cil managed noinlining { 
		ldstr "G1_C13::Method5.4873()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<T0>.Method5'() cil managed noinlining { 
		.override method instance string class Generated1291.IBase1`1<!T0>::Method5()
		ldstr "G1_C13::Method5.MI.4874()"
		ret
	}
	.method public hidebysig newslot virtual instance string Method6<M0>() cil managed noinlining { 
		ldstr "G1_C13::Method6.4875<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1348<M0>() cil managed noinlining { 
		ldstr "G1_C13::ClassMethod1348.4876<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1349<M0>() cil managed noinlining { 
		ldstr "G1_C13::ClassMethod1349.4877<"
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
.class interface public abstract Generated1291.IBase2`2<+T0, -T1> 
{
	.method public hidebysig newslot abstract virtual instance string Method7<M0>() cil managed { }
}
.class interface public abstract Generated1291.IBase1`1<+T0> 
{
	.method public hidebysig newslot abstract virtual instance string Method4() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method5() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method6<M0>() cil managed { }
}
.class public auto ansi beforefieldinit Generated1291.Generated1291 {
	.method static void M.BaseClass0<(Generated1291.BaseClass0)W>(!!W inst, string exp) cil managed {
		.maxstack 5
		.locals init (string[] actualResults)
		ldc.i4.s    0
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.BaseClass0<(Generated1291.BaseClass0)W>(!!W inst, string exp)"
		ldc.i4.s 0
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.BaseClass1<(Generated1291.BaseClass1)W>(!!W inst, string exp) cil managed {
		.maxstack 5
		.locals init (string[] actualResults)
		ldc.i4.s    0
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.BaseClass1<(Generated1291.BaseClass1)W>(!!W inst, string exp)"
		ldc.i4.s 0
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1763.T<T0,(class Generated1291.G3_C1763`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 19
		.locals init (string[] actualResults)
		ldc.i4.s    14
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1763.T<T0,(class Generated1291.G3_C1763`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 14
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G3_C1763`1<!!T0>::ClassMethod1348<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G3_C1763`1<!!T0>::ClassMethod1349<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G3_C1763`1<!!T0>::ClassMethod2794()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G3_C1763`1<!!T0>::ClassMethod2795()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G3_C1763`1<!!T0>::ClassMethod2796<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G3_C1763`1<!!T0>::ClassMethod4928()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G3_C1763`1<!!T0>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G3_C1763`1<!!T0>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G3_C1763`1<!!T0>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G3_C1763`1<!!T0>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G3_C1763`1<!!T0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G3_C1763`1<!!T0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G3_C1763`1<!!T0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     13
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G3_C1763`1<!!T0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1763.A<(class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 19
		.locals init (string[] actualResults)
		ldc.i4.s    14
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1763.A<(class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 14
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>::ClassMethod1348<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>::ClassMethod1349<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>::ClassMethod2794()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>::ClassMethod2795()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>::ClassMethod2796<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>::ClassMethod4928()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     13
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1763.B<(class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 19
		.locals init (string[] actualResults)
		ldc.i4.s    14
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1763.B<(class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 14
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>::ClassMethod1348<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>::ClassMethod1349<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>::ClassMethod2794()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>::ClassMethod2795()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>::ClassMethod2796<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>::ClassMethod4928()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     13
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C715.T.T<T0,T1,(class Generated1291.G2_C715`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 14
		.locals init (string[] actualResults)
		ldc.i4.s    9
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C715.T.T<T0,T1,(class Generated1291.G2_C715`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 9
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G2_C715`2<!!T0,!!T1>::ClassMethod1348<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G2_C715`2<!!T0,!!T1>::ClassMethod1349<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G2_C715`2<!!T0,!!T1>::ClassMethod2794()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G2_C715`2<!!T0,!!T1>::ClassMethod2795()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G2_C715`2<!!T0,!!T1>::ClassMethod2796<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G2_C715`2<!!T0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G2_C715`2<!!T0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G2_C715`2<!!T0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G2_C715`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C715.A.T<T1,(class Generated1291.G2_C715`2<class Generated1291.BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 14
		.locals init (string[] actualResults)
		ldc.i4.s    9
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C715.A.T<T1,(class Generated1291.G2_C715`2<class Generated1291.BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 9
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G2_C715`2<class Generated1291.BaseClass0,!!T1>::ClassMethod1348<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G2_C715`2<class Generated1291.BaseClass0,!!T1>::ClassMethod1349<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G2_C715`2<class Generated1291.BaseClass0,!!T1>::ClassMethod2794()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G2_C715`2<class Generated1291.BaseClass0,!!T1>::ClassMethod2795()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G2_C715`2<class Generated1291.BaseClass0,!!T1>::ClassMethod2796<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G2_C715`2<class Generated1291.BaseClass0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G2_C715`2<class Generated1291.BaseClass0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G2_C715`2<class Generated1291.BaseClass0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G2_C715`2<class Generated1291.BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C715.A.A<(class Generated1291.G2_C715`2<class Generated1291.BaseClass0,class Generated1291.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 14
		.locals init (string[] actualResults)
		ldc.i4.s    9
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C715.A.A<(class Generated1291.G2_C715`2<class Generated1291.BaseClass0,class Generated1291.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 9
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G2_C715`2<class Generated1291.BaseClass0,class Generated1291.BaseClass0>::ClassMethod1348<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G2_C715`2<class Generated1291.BaseClass0,class Generated1291.BaseClass0>::ClassMethod1349<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G2_C715`2<class Generated1291.BaseClass0,class Generated1291.BaseClass0>::ClassMethod2794()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G2_C715`2<class Generated1291.BaseClass0,class Generated1291.BaseClass0>::ClassMethod2795()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G2_C715`2<class Generated1291.BaseClass0,class Generated1291.BaseClass0>::ClassMethod2796<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G2_C715`2<class Generated1291.BaseClass0,class Generated1291.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G2_C715`2<class Generated1291.BaseClass0,class Generated1291.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G2_C715`2<class Generated1291.BaseClass0,class Generated1291.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G2_C715`2<class Generated1291.BaseClass0,class Generated1291.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C715.A.B<(class Generated1291.G2_C715`2<class Generated1291.BaseClass0,class Generated1291.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 14
		.locals init (string[] actualResults)
		ldc.i4.s    9
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C715.A.B<(class Generated1291.G2_C715`2<class Generated1291.BaseClass0,class Generated1291.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 9
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G2_C715`2<class Generated1291.BaseClass0,class Generated1291.BaseClass1>::ClassMethod1348<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G2_C715`2<class Generated1291.BaseClass0,class Generated1291.BaseClass1>::ClassMethod1349<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G2_C715`2<class Generated1291.BaseClass0,class Generated1291.BaseClass1>::ClassMethod2794()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G2_C715`2<class Generated1291.BaseClass0,class Generated1291.BaseClass1>::ClassMethod2795()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G2_C715`2<class Generated1291.BaseClass0,class Generated1291.BaseClass1>::ClassMethod2796<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G2_C715`2<class Generated1291.BaseClass0,class Generated1291.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G2_C715`2<class Generated1291.BaseClass0,class Generated1291.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G2_C715`2<class Generated1291.BaseClass0,class Generated1291.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G2_C715`2<class Generated1291.BaseClass0,class Generated1291.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C715.B.T<T1,(class Generated1291.G2_C715`2<class Generated1291.BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 14
		.locals init (string[] actualResults)
		ldc.i4.s    9
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C715.B.T<T1,(class Generated1291.G2_C715`2<class Generated1291.BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 9
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G2_C715`2<class Generated1291.BaseClass1,!!T1>::ClassMethod1348<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G2_C715`2<class Generated1291.BaseClass1,!!T1>::ClassMethod1349<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G2_C715`2<class Generated1291.BaseClass1,!!T1>::ClassMethod2794()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G2_C715`2<class Generated1291.BaseClass1,!!T1>::ClassMethod2795()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G2_C715`2<class Generated1291.BaseClass1,!!T1>::ClassMethod2796<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G2_C715`2<class Generated1291.BaseClass1,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G2_C715`2<class Generated1291.BaseClass1,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G2_C715`2<class Generated1291.BaseClass1,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G2_C715`2<class Generated1291.BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C715.B.A<(class Generated1291.G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 14
		.locals init (string[] actualResults)
		ldc.i4.s    9
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C715.B.A<(class Generated1291.G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 9
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>::ClassMethod1348<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>::ClassMethod1349<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>::ClassMethod2794()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>::ClassMethod2795()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>::ClassMethod2796<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C715.B.B<(class Generated1291.G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 14
		.locals init (string[] actualResults)
		ldc.i4.s    9
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C715.B.B<(class Generated1291.G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 9
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass1>::ClassMethod1348<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass1>::ClassMethod1349<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass1>::ClassMethod2794()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass1>::ClassMethod2795()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass1>::ClassMethod2796<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase0<(Generated1291.IBase0)W>(!!W inst, string exp) cil managed {
		.maxstack 9
		.locals init (string[] actualResults)
		ldc.i4.s    4
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase0<(Generated1291.IBase0)W>(!!W inst, string exp)"
		ldc.i4.s 4
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string Generated1291.IBase0::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string Generated1291.IBase0::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string Generated1291.IBase0::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string Generated1291.IBase0::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C13.T.T<T0,T1,(class Generated1291.G1_C13`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C13.T.T<T0,T1,(class Generated1291.G1_C13`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G1_C13`2<!!T0,!!T1>::ClassMethod1348<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G1_C13`2<!!T0,!!T1>::ClassMethod1349<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G1_C13`2<!!T0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G1_C13`2<!!T0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G1_C13`2<!!T0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G1_C13`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C13.A.T<T1,(class Generated1291.G1_C13`2<class Generated1291.BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C13.A.T<T1,(class Generated1291.G1_C13`2<class Generated1291.BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G1_C13`2<class Generated1291.BaseClass0,!!T1>::ClassMethod1348<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G1_C13`2<class Generated1291.BaseClass0,!!T1>::ClassMethod1349<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G1_C13`2<class Generated1291.BaseClass0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G1_C13`2<class Generated1291.BaseClass0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G1_C13`2<class Generated1291.BaseClass0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G1_C13`2<class Generated1291.BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C13.A.A<(class Generated1291.G1_C13`2<class Generated1291.BaseClass0,class Generated1291.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C13.A.A<(class Generated1291.G1_C13`2<class Generated1291.BaseClass0,class Generated1291.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G1_C13`2<class Generated1291.BaseClass0,class Generated1291.BaseClass0>::ClassMethod1348<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G1_C13`2<class Generated1291.BaseClass0,class Generated1291.BaseClass0>::ClassMethod1349<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G1_C13`2<class Generated1291.BaseClass0,class Generated1291.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G1_C13`2<class Generated1291.BaseClass0,class Generated1291.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G1_C13`2<class Generated1291.BaseClass0,class Generated1291.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G1_C13`2<class Generated1291.BaseClass0,class Generated1291.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C13.A.B<(class Generated1291.G1_C13`2<class Generated1291.BaseClass0,class Generated1291.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C13.A.B<(class Generated1291.G1_C13`2<class Generated1291.BaseClass0,class Generated1291.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G1_C13`2<class Generated1291.BaseClass0,class Generated1291.BaseClass1>::ClassMethod1348<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G1_C13`2<class Generated1291.BaseClass0,class Generated1291.BaseClass1>::ClassMethod1349<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G1_C13`2<class Generated1291.BaseClass0,class Generated1291.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G1_C13`2<class Generated1291.BaseClass0,class Generated1291.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G1_C13`2<class Generated1291.BaseClass0,class Generated1291.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G1_C13`2<class Generated1291.BaseClass0,class Generated1291.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C13.B.T<T1,(class Generated1291.G1_C13`2<class Generated1291.BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C13.B.T<T1,(class Generated1291.G1_C13`2<class Generated1291.BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G1_C13`2<class Generated1291.BaseClass1,!!T1>::ClassMethod1348<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G1_C13`2<class Generated1291.BaseClass1,!!T1>::ClassMethod1349<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G1_C13`2<class Generated1291.BaseClass1,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G1_C13`2<class Generated1291.BaseClass1,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G1_C13`2<class Generated1291.BaseClass1,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G1_C13`2<class Generated1291.BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C13.B.A<(class Generated1291.G1_C13`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C13.B.A<(class Generated1291.G1_C13`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G1_C13`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>::ClassMethod1348<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G1_C13`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>::ClassMethod1349<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G1_C13`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G1_C13`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G1_C13`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G1_C13`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C13.B.B<(class Generated1291.G1_C13`2<class Generated1291.BaseClass1,class Generated1291.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C13.B.B<(class Generated1291.G1_C13`2<class Generated1291.BaseClass1,class Generated1291.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G1_C13`2<class Generated1291.BaseClass1,class Generated1291.BaseClass1>::ClassMethod1348<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G1_C13`2<class Generated1291.BaseClass1,class Generated1291.BaseClass1>::ClassMethod1349<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G1_C13`2<class Generated1291.BaseClass1,class Generated1291.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G1_C13`2<class Generated1291.BaseClass1,class Generated1291.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G1_C13`2<class Generated1291.BaseClass1,class Generated1291.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.G1_C13`2<class Generated1291.BaseClass1,class Generated1291.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.T.T<T0,T1,(class Generated1291.IBase2`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.T.T<T0,T1,(class Generated1291.IBase2`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.IBase2`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.A.T<T1,(class Generated1291.IBase2`2<class Generated1291.BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.A.T<T1,(class Generated1291.IBase2`2<class Generated1291.BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.IBase2`2<class Generated1291.BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.A.A<(class Generated1291.IBase2`2<class Generated1291.BaseClass0,class Generated1291.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.A.A<(class Generated1291.IBase2`2<class Generated1291.BaseClass0,class Generated1291.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.IBase2`2<class Generated1291.BaseClass0,class Generated1291.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.A.B<(class Generated1291.IBase2`2<class Generated1291.BaseClass0,class Generated1291.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.A.B<(class Generated1291.IBase2`2<class Generated1291.BaseClass0,class Generated1291.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.IBase2`2<class Generated1291.BaseClass0,class Generated1291.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.B.T<T1,(class Generated1291.IBase2`2<class Generated1291.BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.B.T<T1,(class Generated1291.IBase2`2<class Generated1291.BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.IBase2`2<class Generated1291.BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.B.A<(class Generated1291.IBase2`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.B.A<(class Generated1291.IBase2`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.IBase2`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.B.B<(class Generated1291.IBase2`2<class Generated1291.BaseClass1,class Generated1291.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.B.B<(class Generated1291.IBase2`2<class Generated1291.BaseClass1,class Generated1291.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.IBase2`2<class Generated1291.BaseClass1,class Generated1291.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase1.T<T0,(class Generated1291.IBase1`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase1.T<T0,(class Generated1291.IBase1`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.IBase1`1<!!T0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.IBase1`1<!!T0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.IBase1`1<!!T0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase1.A<(class Generated1291.IBase1`1<class Generated1291.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase1.A<(class Generated1291.IBase1`1<class Generated1291.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.IBase1`1<class Generated1291.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.IBase1`1<class Generated1291.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.IBase1`1<class Generated1291.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase1.B<(class Generated1291.IBase1`1<class Generated1291.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase1.B<(class Generated1291.IBase1`1<class Generated1291.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.IBase1`1<class Generated1291.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.IBase1`1<class Generated1291.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1291.IBase1`1<class Generated1291.BaseClass1>::Method6<object>()
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
		newobj instance void class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1291.G1_C13`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>
		callvirt instance string class Generated1291.G1_C13`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>::ClassMethod1349<object>()
		ldstr "G1_C13::ClassMethod1349.4877<System.Object>()"
		ldstr "class G1_C13`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1291.G1_C13`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>
		callvirt instance string class Generated1291.G1_C13`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>::ClassMethod1348<object>()
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()"
		ldstr "class G1_C13`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1291.G1_C13`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>
		callvirt instance string class Generated1291.G1_C13`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>::Method6<object>()
		ldstr "G1_C13::Method6.4875<System.Object>()"
		ldstr "class G1_C13`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1291.G1_C13`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>
		callvirt instance string class Generated1291.G1_C13`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>::Method5()
		ldstr "G1_C13::Method5.4873()"
		ldstr "class G1_C13`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1291.G1_C13`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>
		callvirt instance string class Generated1291.G1_C13`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>::Method4()
		ldstr "G1_C13::Method4.4872()"
		ldstr "class G1_C13`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1291.G1_C13`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>
		callvirt instance string class Generated1291.G1_C13`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>::Method7<object>()
		ldstr "G1_C13::Method7.4871<System.Object>()"
		ldstr "class G1_C13`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1291.IBase2`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>::Method7<object>()
		ldstr "G2_C715::Method7.11587<System.Object>()"
		ldstr "class IBase2`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1291.IBase1`1<class Generated1291.BaseClass1>::Method4()
		ldstr "G1_C13::Method4.4872()"
		ldstr "class IBase1`1<class Generated1291.BaseClass1> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1291.IBase1`1<class Generated1291.BaseClass1>::Method5()
		ldstr "G1_C13::Method5.MI.4874()"
		ldstr "class IBase1`1<class Generated1291.BaseClass1> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1291.IBase1`1<class Generated1291.BaseClass1>::Method6<object>()
		ldstr "G1_C13::Method6.4875<System.Object>()"
		ldstr "class IBase1`1<class Generated1291.BaseClass1> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1291.IBase2`2<class Generated1291.BaseClass0,class Generated1291.BaseClass0>::Method7<object>()
		ldstr "G2_C715::Method7.11587<System.Object>()"
		ldstr "class IBase2`2<class Generated1291.BaseClass0,class Generated1291.BaseClass0> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1291.IBase2`2<class Generated1291.BaseClass0,class Generated1291.BaseClass1>::Method7<object>()
		ldstr "G2_C715::Method7.11587<System.Object>()"
		ldstr "class IBase2`2<class Generated1291.BaseClass0,class Generated1291.BaseClass1> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1291.IBase2`2<class Generated1291.BaseClass1,class Generated1291.BaseClass1>::Method7<object>()
		ldstr "G2_C715::Method7.11587<System.Object>()"
		ldstr "class IBase2`2<class Generated1291.BaseClass1,class Generated1291.BaseClass1> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1291.IBase1`1<class Generated1291.BaseClass0>::Method4()
		ldstr "G1_C13::Method4.4872()"
		ldstr "class IBase1`1<class Generated1291.BaseClass0> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1291.IBase1`1<class Generated1291.BaseClass0>::Method5()
		ldstr "G1_C13::Method5.MI.4874()"
		ldstr "class IBase1`1<class Generated1291.BaseClass0> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1291.IBase1`1<class Generated1291.BaseClass0>::Method6<object>()
		ldstr "G1_C13::Method6.4875<System.Object>()"
		ldstr "class IBase1`1<class Generated1291.BaseClass0> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1291.G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>
		callvirt instance string class Generated1291.G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>::ClassMethod2796<object>()
		ldstr "G3_C1763::ClassMethod2796.MI.17859<System.Object>()"
		ldstr "class G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1291.G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>
		callvirt instance string class Generated1291.G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>::ClassMethod2795()
		ldstr "G2_C715::ClassMethod2795.11589()"
		ldstr "class G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1291.G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>
		callvirt instance string class Generated1291.G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>::ClassMethod2794()
		ldstr "G2_C715::ClassMethod2794.11588()"
		ldstr "class G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1291.G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>
		callvirt instance string class Generated1291.G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>::Method7<object>()
		ldstr "G2_C715::Method7.11587<System.Object>()"
		ldstr "class G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1291.G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>
		callvirt instance string class Generated1291.G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>::ClassMethod1349<object>()
		ldstr "G1_C13::ClassMethod1349.4877<System.Object>()"
		ldstr "class G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1291.G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>
		callvirt instance string class Generated1291.G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>::ClassMethod1348<object>()
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()"
		ldstr "class G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1291.G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>
		callvirt instance string class Generated1291.G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>::Method6<object>()
		ldstr "G1_C13::Method6.4875<System.Object>()"
		ldstr "class G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1291.G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>
		callvirt instance string class Generated1291.G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>::Method5()
		ldstr "G1_C13::Method5.4873()"
		ldstr "class G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1291.G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>
		callvirt instance string class Generated1291.G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>::Method4()
		ldstr "G1_C13::Method4.4872()"
		ldstr "class G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>
		callvirt instance string class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>::ClassMethod4928()
		ldstr "G3_C1763::ClassMethod4928.17858()"
		ldstr "class G3_C1763`1<class Generated1291.BaseClass0> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>
		callvirt instance string class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>::Method3<object>()
		ldstr "G3_C1763::Method3.17857<System.Object>()"
		ldstr "class G3_C1763`1<class Generated1291.BaseClass0> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>
		callvirt instance string class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>::Method2<object>()
		ldstr "G3_C1763::Method2.17856<System.Object>()"
		ldstr "class G3_C1763`1<class Generated1291.BaseClass0> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>
		callvirt instance string class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>::Method1()
		ldstr "G3_C1763::Method1.17855()"
		ldstr "class G3_C1763`1<class Generated1291.BaseClass0> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>
		callvirt instance string class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>::Method0()
		ldstr "G3_C1763::Method0.17854()"
		ldstr "class G3_C1763`1<class Generated1291.BaseClass0> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>
		callvirt instance string class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>::ClassMethod2796<object>()
		ldstr "G3_C1763::ClassMethod2796.MI.17859<System.Object>()"
		ldstr "class G3_C1763`1<class Generated1291.BaseClass0> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>
		callvirt instance string class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>::ClassMethod2795()
		ldstr "G2_C715::ClassMethod2795.11589()"
		ldstr "class G3_C1763`1<class Generated1291.BaseClass0> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>
		callvirt instance string class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>::ClassMethod2794()
		ldstr "G2_C715::ClassMethod2794.11588()"
		ldstr "class G3_C1763`1<class Generated1291.BaseClass0> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>
		callvirt instance string class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>::Method7<object>()
		ldstr "G2_C715::Method7.11587<System.Object>()"
		ldstr "class G3_C1763`1<class Generated1291.BaseClass0> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>
		callvirt instance string class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>::ClassMethod1349<object>()
		ldstr "G1_C13::ClassMethod1349.4877<System.Object>()"
		ldstr "class G3_C1763`1<class Generated1291.BaseClass0> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>
		callvirt instance string class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>::ClassMethod1348<object>()
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()"
		ldstr "class G3_C1763`1<class Generated1291.BaseClass0> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>
		callvirt instance string class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>::Method6<object>()
		ldstr "G1_C13::Method6.4875<System.Object>()"
		ldstr "class G3_C1763`1<class Generated1291.BaseClass0> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>
		callvirt instance string class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>::Method5()
		ldstr "G1_C13::Method5.4873()"
		ldstr "class G3_C1763`1<class Generated1291.BaseClass0> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>
		callvirt instance string class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>::Method4()
		ldstr "G1_C13::Method4.4872()"
		ldstr "class G3_C1763`1<class Generated1291.BaseClass0> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string Generated1291.IBase0::Method0()
		ldstr "G3_C1763::Method0.17854()"
		ldstr "IBase0 on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated1291.IBase0::Method1()
		ldstr "G3_C1763::Method1.17855()"
		ldstr "IBase0 on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated1291.IBase0::Method2<object>()
		ldstr "G3_C1763::Method2.17856<System.Object>()"
		ldstr "IBase0 on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated1291.IBase0::Method3<object>()
		ldstr "G3_C1763::Method3.17857<System.Object>()"
		ldstr "IBase0 on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1291.G1_C13`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>
		callvirt instance string class Generated1291.G1_C13`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>::ClassMethod1349<object>()
		ldstr "G1_C13::ClassMethod1349.4877<System.Object>()"
		ldstr "class G1_C13`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1291.G1_C13`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>
		callvirt instance string class Generated1291.G1_C13`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>::ClassMethod1348<object>()
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()"
		ldstr "class G1_C13`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1291.G1_C13`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>
		callvirt instance string class Generated1291.G1_C13`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>::Method6<object>()
		ldstr "G1_C13::Method6.4875<System.Object>()"
		ldstr "class G1_C13`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1291.G1_C13`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>
		callvirt instance string class Generated1291.G1_C13`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>::Method5()
		ldstr "G1_C13::Method5.4873()"
		ldstr "class G1_C13`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1291.G1_C13`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>
		callvirt instance string class Generated1291.G1_C13`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>::Method4()
		ldstr "G1_C13::Method4.4872()"
		ldstr "class G1_C13`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1291.G1_C13`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>
		callvirt instance string class Generated1291.G1_C13`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>::Method7<object>()
		ldstr "G1_C13::Method7.4871<System.Object>()"
		ldstr "class G1_C13`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1291.IBase2`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>::Method7<object>()
		ldstr "G2_C715::Method7.11587<System.Object>()"
		ldstr "class IBase2`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1291.IBase1`1<class Generated1291.BaseClass1>::Method4()
		ldstr "G1_C13::Method4.4872()"
		ldstr "class IBase1`1<class Generated1291.BaseClass1> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1291.IBase1`1<class Generated1291.BaseClass1>::Method5()
		ldstr "G1_C13::Method5.MI.4874()"
		ldstr "class IBase1`1<class Generated1291.BaseClass1> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1291.IBase1`1<class Generated1291.BaseClass1>::Method6<object>()
		ldstr "G1_C13::Method6.4875<System.Object>()"
		ldstr "class IBase1`1<class Generated1291.BaseClass1> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1291.IBase2`2<class Generated1291.BaseClass0,class Generated1291.BaseClass0>::Method7<object>()
		ldstr "G2_C715::Method7.11587<System.Object>()"
		ldstr "class IBase2`2<class Generated1291.BaseClass0,class Generated1291.BaseClass0> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1291.IBase2`2<class Generated1291.BaseClass0,class Generated1291.BaseClass1>::Method7<object>()
		ldstr "G2_C715::Method7.11587<System.Object>()"
		ldstr "class IBase2`2<class Generated1291.BaseClass0,class Generated1291.BaseClass1> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1291.IBase2`2<class Generated1291.BaseClass1,class Generated1291.BaseClass1>::Method7<object>()
		ldstr "G2_C715::Method7.11587<System.Object>()"
		ldstr "class IBase2`2<class Generated1291.BaseClass1,class Generated1291.BaseClass1> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1291.IBase1`1<class Generated1291.BaseClass0>::Method4()
		ldstr "G1_C13::Method4.4872()"
		ldstr "class IBase1`1<class Generated1291.BaseClass0> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1291.IBase1`1<class Generated1291.BaseClass0>::Method5()
		ldstr "G1_C13::Method5.MI.4874()"
		ldstr "class IBase1`1<class Generated1291.BaseClass0> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1291.IBase1`1<class Generated1291.BaseClass0>::Method6<object>()
		ldstr "G1_C13::Method6.4875<System.Object>()"
		ldstr "class IBase1`1<class Generated1291.BaseClass0> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1291.G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>
		callvirt instance string class Generated1291.G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>::ClassMethod2796<object>()
		ldstr "G3_C1763::ClassMethod2796.MI.17859<System.Object>()"
		ldstr "class G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1291.G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>
		callvirt instance string class Generated1291.G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>::ClassMethod2795()
		ldstr "G2_C715::ClassMethod2795.11589()"
		ldstr "class G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1291.G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>
		callvirt instance string class Generated1291.G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>::ClassMethod2794()
		ldstr "G2_C715::ClassMethod2794.11588()"
		ldstr "class G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1291.G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>
		callvirt instance string class Generated1291.G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>::Method7<object>()
		ldstr "G2_C715::Method7.11587<System.Object>()"
		ldstr "class G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1291.G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>
		callvirt instance string class Generated1291.G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>::ClassMethod1349<object>()
		ldstr "G1_C13::ClassMethod1349.4877<System.Object>()"
		ldstr "class G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1291.G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>
		callvirt instance string class Generated1291.G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>::ClassMethod1348<object>()
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()"
		ldstr "class G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1291.G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>
		callvirt instance string class Generated1291.G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>::Method6<object>()
		ldstr "G1_C13::Method6.4875<System.Object>()"
		ldstr "class G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1291.G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>
		callvirt instance string class Generated1291.G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>::Method5()
		ldstr "G1_C13::Method5.4873()"
		ldstr "class G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1291.G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>
		callvirt instance string class Generated1291.G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>::Method4()
		ldstr "G1_C13::Method4.4872()"
		ldstr "class G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>
		callvirt instance string class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>::ClassMethod4928()
		ldstr "G3_C1763::ClassMethod4928.17858()"
		ldstr "class G3_C1763`1<class Generated1291.BaseClass1> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>
		callvirt instance string class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>::Method3<object>()
		ldstr "G3_C1763::Method3.17857<System.Object>()"
		ldstr "class G3_C1763`1<class Generated1291.BaseClass1> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>
		callvirt instance string class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>::Method2<object>()
		ldstr "G3_C1763::Method2.17856<System.Object>()"
		ldstr "class G3_C1763`1<class Generated1291.BaseClass1> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>
		callvirt instance string class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>::Method1()
		ldstr "G3_C1763::Method1.17855()"
		ldstr "class G3_C1763`1<class Generated1291.BaseClass1> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>
		callvirt instance string class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>::Method0()
		ldstr "G3_C1763::Method0.17854()"
		ldstr "class G3_C1763`1<class Generated1291.BaseClass1> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>
		callvirt instance string class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>::ClassMethod2796<object>()
		ldstr "G3_C1763::ClassMethod2796.MI.17859<System.Object>()"
		ldstr "class G3_C1763`1<class Generated1291.BaseClass1> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>
		callvirt instance string class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>::ClassMethod2795()
		ldstr "G2_C715::ClassMethod2795.11589()"
		ldstr "class G3_C1763`1<class Generated1291.BaseClass1> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>
		callvirt instance string class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>::ClassMethod2794()
		ldstr "G2_C715::ClassMethod2794.11588()"
		ldstr "class G3_C1763`1<class Generated1291.BaseClass1> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>
		callvirt instance string class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>::Method7<object>()
		ldstr "G2_C715::Method7.11587<System.Object>()"
		ldstr "class G3_C1763`1<class Generated1291.BaseClass1> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>
		callvirt instance string class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>::ClassMethod1349<object>()
		ldstr "G1_C13::ClassMethod1349.4877<System.Object>()"
		ldstr "class G3_C1763`1<class Generated1291.BaseClass1> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>
		callvirt instance string class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>::ClassMethod1348<object>()
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()"
		ldstr "class G3_C1763`1<class Generated1291.BaseClass1> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>
		callvirt instance string class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>::Method6<object>()
		ldstr "G1_C13::Method6.4875<System.Object>()"
		ldstr "class G3_C1763`1<class Generated1291.BaseClass1> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>
		callvirt instance string class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>::Method5()
		ldstr "G1_C13::Method5.4873()"
		ldstr "class G3_C1763`1<class Generated1291.BaseClass1> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>
		callvirt instance string class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>::Method4()
		ldstr "G1_C13::Method4.4872()"
		ldstr "class G3_C1763`1<class Generated1291.BaseClass1> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string Generated1291.IBase0::Method0()
		ldstr "G3_C1763::Method0.17854()"
		ldstr "IBase0 on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated1291.IBase0::Method1()
		ldstr "G3_C1763::Method1.17855()"
		ldstr "IBase0 on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated1291.IBase0::Method2<object>()
		ldstr "G3_C1763::Method2.17856<System.Object>()"
		ldstr "IBase0 on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated1291.IBase0::Method3<object>()
		ldstr "G3_C1763::Method3.17857<System.Object>()"
		ldstr "IBase0 on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>"
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
		newobj instance void class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G1_C13::Method4.4872()#G1_C13::Method5.4873()#G1_C13::Method6.4875<System.Object>()#G1_C13::Method7.4871<System.Object>()#"
		call void Generated1291.Generated1291::M.G1_C13.T.T<class Generated1291.BaseClass1,class Generated1291.BaseClass0,class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G1_C13::Method4.4872()#G1_C13::Method5.4873()#G1_C13::Method6.4875<System.Object>()#G1_C13::Method7.4871<System.Object>()#"
		call void Generated1291.Generated1291::M.G1_C13.B.T<class Generated1291.BaseClass0,class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G1_C13::Method4.4872()#G1_C13::Method5.4873()#G1_C13::Method6.4875<System.Object>()#G1_C13::Method7.4871<System.Object>()#"
		call void Generated1291.Generated1291::M.G1_C13.B.A<class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C715::Method7.11587<System.Object>()#"
		call void Generated1291.Generated1291::M.IBase2.T.T<class Generated1291.BaseClass1,class Generated1291.BaseClass0,class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C715::Method7.11587<System.Object>()#"
		call void Generated1291.Generated1291::M.IBase2.B.T<class Generated1291.BaseClass0,class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C715::Method7.11587<System.Object>()#"
		call void Generated1291.Generated1291::M.IBase2.B.A<class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C13::Method4.4872()#G1_C13::Method5.MI.4874()#G1_C13::Method6.4875<System.Object>()#"
		call void Generated1291.Generated1291::M.IBase1.T<class Generated1291.BaseClass1,class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C13::Method4.4872()#G1_C13::Method5.MI.4874()#G1_C13::Method6.4875<System.Object>()#"
		call void Generated1291.Generated1291::M.IBase1.B<class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C715::Method7.11587<System.Object>()#"
		call void Generated1291.Generated1291::M.IBase2.T.T<class Generated1291.BaseClass0,class Generated1291.BaseClass0,class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C715::Method7.11587<System.Object>()#"
		call void Generated1291.Generated1291::M.IBase2.A.T<class Generated1291.BaseClass0,class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C715::Method7.11587<System.Object>()#"
		call void Generated1291.Generated1291::M.IBase2.A.A<class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C715::Method7.11587<System.Object>()#"
		call void Generated1291.Generated1291::M.IBase2.T.T<class Generated1291.BaseClass0,class Generated1291.BaseClass1,class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C715::Method7.11587<System.Object>()#"
		call void Generated1291.Generated1291::M.IBase2.A.T<class Generated1291.BaseClass1,class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C715::Method7.11587<System.Object>()#"
		call void Generated1291.Generated1291::M.IBase2.A.B<class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C715::Method7.11587<System.Object>()#"
		call void Generated1291.Generated1291::M.IBase2.T.T<class Generated1291.BaseClass1,class Generated1291.BaseClass1,class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C715::Method7.11587<System.Object>()#"
		call void Generated1291.Generated1291::M.IBase2.B.T<class Generated1291.BaseClass1,class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C715::Method7.11587<System.Object>()#"
		call void Generated1291.Generated1291::M.IBase2.B.B<class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C13::Method4.4872()#G1_C13::Method5.MI.4874()#G1_C13::Method6.4875<System.Object>()#"
		call void Generated1291.Generated1291::M.IBase1.T<class Generated1291.BaseClass0,class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C13::Method4.4872()#G1_C13::Method5.MI.4874()#G1_C13::Method6.4875<System.Object>()#"
		call void Generated1291.Generated1291::M.IBase1.A<class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G2_C715::ClassMethod2794.11588()#G2_C715::ClassMethod2795.11589()#G3_C1763::ClassMethod2796.MI.17859<System.Object>()#G1_C13::Method4.4872()#G1_C13::Method5.4873()#G1_C13::Method6.4875<System.Object>()#G2_C715::Method7.11587<System.Object>()#"
		call void Generated1291.Generated1291::M.G2_C715.T.T<class Generated1291.BaseClass1,class Generated1291.BaseClass0,class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G2_C715::ClassMethod2794.11588()#G2_C715::ClassMethod2795.11589()#G3_C1763::ClassMethod2796.MI.17859<System.Object>()#G1_C13::Method4.4872()#G1_C13::Method5.4873()#G1_C13::Method6.4875<System.Object>()#G2_C715::Method7.11587<System.Object>()#"
		call void Generated1291.Generated1291::M.G2_C715.B.T<class Generated1291.BaseClass0,class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G2_C715::ClassMethod2794.11588()#G2_C715::ClassMethod2795.11589()#G3_C1763::ClassMethod2796.MI.17859<System.Object>()#G1_C13::Method4.4872()#G1_C13::Method5.4873()#G1_C13::Method6.4875<System.Object>()#G2_C715::Method7.11587<System.Object>()#"
		call void Generated1291.Generated1291::M.G2_C715.B.A<class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G2_C715::ClassMethod2794.11588()#G2_C715::ClassMethod2795.11589()#G3_C1763::ClassMethod2796.MI.17859<System.Object>()#G3_C1763::ClassMethod4928.17858()#G3_C1763::Method0.17854()#G3_C1763::Method1.17855()#G3_C1763::Method2.17856<System.Object>()#G3_C1763::Method3.17857<System.Object>()#G1_C13::Method4.4872()#G1_C13::Method5.4873()#G1_C13::Method6.4875<System.Object>()#G2_C715::Method7.11587<System.Object>()#"
		call void Generated1291.Generated1291::M.G3_C1763.T<class Generated1291.BaseClass0,class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G2_C715::ClassMethod2794.11588()#G2_C715::ClassMethod2795.11589()#G3_C1763::ClassMethod2796.MI.17859<System.Object>()#G3_C1763::ClassMethod4928.17858()#G3_C1763::Method0.17854()#G3_C1763::Method1.17855()#G3_C1763::Method2.17856<System.Object>()#G3_C1763::Method3.17857<System.Object>()#G1_C13::Method4.4872()#G1_C13::Method5.4873()#G1_C13::Method6.4875<System.Object>()#G2_C715::Method7.11587<System.Object>()#"
		call void Generated1291.Generated1291::M.G3_C1763.A<class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1763::Method0.17854()#G3_C1763::Method1.17855()#G3_C1763::Method2.17856<System.Object>()#G3_C1763::Method3.17857<System.Object>()#"
		call void Generated1291.Generated1291::M.IBase0<class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>>(!!0,string)
		newobj instance void class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G1_C13::Method4.4872()#G1_C13::Method5.4873()#G1_C13::Method6.4875<System.Object>()#G1_C13::Method7.4871<System.Object>()#"
		call void Generated1291.Generated1291::M.G1_C13.T.T<class Generated1291.BaseClass1,class Generated1291.BaseClass0,class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G1_C13::Method4.4872()#G1_C13::Method5.4873()#G1_C13::Method6.4875<System.Object>()#G1_C13::Method7.4871<System.Object>()#"
		call void Generated1291.Generated1291::M.G1_C13.B.T<class Generated1291.BaseClass0,class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G1_C13::Method4.4872()#G1_C13::Method5.4873()#G1_C13::Method6.4875<System.Object>()#G1_C13::Method7.4871<System.Object>()#"
		call void Generated1291.Generated1291::M.G1_C13.B.A<class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C715::Method7.11587<System.Object>()#"
		call void Generated1291.Generated1291::M.IBase2.T.T<class Generated1291.BaseClass1,class Generated1291.BaseClass0,class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C715::Method7.11587<System.Object>()#"
		call void Generated1291.Generated1291::M.IBase2.B.T<class Generated1291.BaseClass0,class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C715::Method7.11587<System.Object>()#"
		call void Generated1291.Generated1291::M.IBase2.B.A<class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C13::Method4.4872()#G1_C13::Method5.MI.4874()#G1_C13::Method6.4875<System.Object>()#"
		call void Generated1291.Generated1291::M.IBase1.T<class Generated1291.BaseClass1,class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C13::Method4.4872()#G1_C13::Method5.MI.4874()#G1_C13::Method6.4875<System.Object>()#"
		call void Generated1291.Generated1291::M.IBase1.B<class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C715::Method7.11587<System.Object>()#"
		call void Generated1291.Generated1291::M.IBase2.T.T<class Generated1291.BaseClass0,class Generated1291.BaseClass0,class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C715::Method7.11587<System.Object>()#"
		call void Generated1291.Generated1291::M.IBase2.A.T<class Generated1291.BaseClass0,class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C715::Method7.11587<System.Object>()#"
		call void Generated1291.Generated1291::M.IBase2.A.A<class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C715::Method7.11587<System.Object>()#"
		call void Generated1291.Generated1291::M.IBase2.T.T<class Generated1291.BaseClass0,class Generated1291.BaseClass1,class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C715::Method7.11587<System.Object>()#"
		call void Generated1291.Generated1291::M.IBase2.A.T<class Generated1291.BaseClass1,class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C715::Method7.11587<System.Object>()#"
		call void Generated1291.Generated1291::M.IBase2.A.B<class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C715::Method7.11587<System.Object>()#"
		call void Generated1291.Generated1291::M.IBase2.T.T<class Generated1291.BaseClass1,class Generated1291.BaseClass1,class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C715::Method7.11587<System.Object>()#"
		call void Generated1291.Generated1291::M.IBase2.B.T<class Generated1291.BaseClass1,class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C715::Method7.11587<System.Object>()#"
		call void Generated1291.Generated1291::M.IBase2.B.B<class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C13::Method4.4872()#G1_C13::Method5.MI.4874()#G1_C13::Method6.4875<System.Object>()#"
		call void Generated1291.Generated1291::M.IBase1.T<class Generated1291.BaseClass0,class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C13::Method4.4872()#G1_C13::Method5.MI.4874()#G1_C13::Method6.4875<System.Object>()#"
		call void Generated1291.Generated1291::M.IBase1.A<class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G2_C715::ClassMethod2794.11588()#G2_C715::ClassMethod2795.11589()#G3_C1763::ClassMethod2796.MI.17859<System.Object>()#G1_C13::Method4.4872()#G1_C13::Method5.4873()#G1_C13::Method6.4875<System.Object>()#G2_C715::Method7.11587<System.Object>()#"
		call void Generated1291.Generated1291::M.G2_C715.T.T<class Generated1291.BaseClass1,class Generated1291.BaseClass0,class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G2_C715::ClassMethod2794.11588()#G2_C715::ClassMethod2795.11589()#G3_C1763::ClassMethod2796.MI.17859<System.Object>()#G1_C13::Method4.4872()#G1_C13::Method5.4873()#G1_C13::Method6.4875<System.Object>()#G2_C715::Method7.11587<System.Object>()#"
		call void Generated1291.Generated1291::M.G2_C715.B.T<class Generated1291.BaseClass0,class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G2_C715::ClassMethod2794.11588()#G2_C715::ClassMethod2795.11589()#G3_C1763::ClassMethod2796.MI.17859<System.Object>()#G1_C13::Method4.4872()#G1_C13::Method5.4873()#G1_C13::Method6.4875<System.Object>()#G2_C715::Method7.11587<System.Object>()#"
		call void Generated1291.Generated1291::M.G2_C715.B.A<class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G2_C715::ClassMethod2794.11588()#G2_C715::ClassMethod2795.11589()#G3_C1763::ClassMethod2796.MI.17859<System.Object>()#G3_C1763::ClassMethod4928.17858()#G3_C1763::Method0.17854()#G3_C1763::Method1.17855()#G3_C1763::Method2.17856<System.Object>()#G3_C1763::Method3.17857<System.Object>()#G1_C13::Method4.4872()#G1_C13::Method5.4873()#G1_C13::Method6.4875<System.Object>()#G2_C715::Method7.11587<System.Object>()#"
		call void Generated1291.Generated1291::M.G3_C1763.T<class Generated1291.BaseClass1,class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G2_C715::ClassMethod2794.11588()#G2_C715::ClassMethod2795.11589()#G3_C1763::ClassMethod2796.MI.17859<System.Object>()#G3_C1763::ClassMethod4928.17858()#G3_C1763::Method0.17854()#G3_C1763::Method1.17855()#G3_C1763::Method2.17856<System.Object>()#G3_C1763::Method3.17857<System.Object>()#G1_C13::Method4.4872()#G1_C13::Method5.4873()#G1_C13::Method6.4875<System.Object>()#G2_C715::Method7.11587<System.Object>()#"
		call void Generated1291.Generated1291::M.G3_C1763.B<class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1763::Method0.17854()#G3_C1763::Method1.17855()#G3_C1763::Method2.17856<System.Object>()#G3_C1763::Method3.17857<System.Object>()#"
		call void Generated1291.Generated1291::M.IBase0<class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>>(!!0,string)
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
		newobj instance void class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1291.G1_C13`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1291.G1_C13`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>::ClassMethod1349<object>()
		calli default string(class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>)
		ldstr "G1_C13::ClassMethod1349.4877<System.Object>()"
		ldstr "class G1_C13`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1291.G1_C13`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1291.G1_C13`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>::ClassMethod1348<object>()
		calli default string(class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>)
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()"
		ldstr "class G1_C13`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1291.G1_C13`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1291.G1_C13`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>::Method6<object>()
		calli default string(class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>)
		ldstr "G1_C13::Method6.4875<System.Object>()"
		ldstr "class G1_C13`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1291.G1_C13`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1291.G1_C13`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>::Method5()
		calli default string(class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>)
		ldstr "G1_C13::Method5.4873()"
		ldstr "class G1_C13`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1291.G1_C13`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1291.G1_C13`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>::Method4()
		calli default string(class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>)
		ldstr "G1_C13::Method4.4872()"
		ldstr "class G1_C13`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1291.G1_C13`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1291.G1_C13`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>::Method7<object>()
		calli default string(class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>)
		ldstr "G1_C13::Method7.4871<System.Object>()"
		ldstr "class G1_C13`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1291.IBase2`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>::Method7<object>()
		calli default string(class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>)
		ldstr "G2_C715::Method7.11587<System.Object>()"
		ldstr "class IBase2`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1291.IBase1`1<class Generated1291.BaseClass1>::Method4()
		calli default string(class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>)
		ldstr "G1_C13::Method4.4872()"
		ldstr "class IBase1`1<class Generated1291.BaseClass1> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1291.IBase1`1<class Generated1291.BaseClass1>::Method5()
		calli default string(class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>)
		ldstr "G1_C13::Method5.MI.4874()"
		ldstr "class IBase1`1<class Generated1291.BaseClass1> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1291.IBase1`1<class Generated1291.BaseClass1>::Method6<object>()
		calli default string(class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>)
		ldstr "G1_C13::Method6.4875<System.Object>()"
		ldstr "class IBase1`1<class Generated1291.BaseClass1> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1291.IBase2`2<class Generated1291.BaseClass0,class Generated1291.BaseClass0>::Method7<object>()
		calli default string(class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>)
		ldstr "G2_C715::Method7.11587<System.Object>()"
		ldstr "class IBase2`2<class Generated1291.BaseClass0,class Generated1291.BaseClass0> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1291.IBase2`2<class Generated1291.BaseClass0,class Generated1291.BaseClass1>::Method7<object>()
		calli default string(class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>)
		ldstr "G2_C715::Method7.11587<System.Object>()"
		ldstr "class IBase2`2<class Generated1291.BaseClass0,class Generated1291.BaseClass1> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1291.IBase2`2<class Generated1291.BaseClass1,class Generated1291.BaseClass1>::Method7<object>()
		calli default string(class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>)
		ldstr "G2_C715::Method7.11587<System.Object>()"
		ldstr "class IBase2`2<class Generated1291.BaseClass1,class Generated1291.BaseClass1> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1291.IBase1`1<class Generated1291.BaseClass0>::Method4()
		calli default string(class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>)
		ldstr "G1_C13::Method4.4872()"
		ldstr "class IBase1`1<class Generated1291.BaseClass0> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1291.IBase1`1<class Generated1291.BaseClass0>::Method5()
		calli default string(class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>)
		ldstr "G1_C13::Method5.MI.4874()"
		ldstr "class IBase1`1<class Generated1291.BaseClass0> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1291.IBase1`1<class Generated1291.BaseClass0>::Method6<object>()
		calli default string(class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>)
		ldstr "G1_C13::Method6.4875<System.Object>()"
		ldstr "class IBase1`1<class Generated1291.BaseClass0> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1291.G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1291.G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>::ClassMethod2796<object>()
		calli default string(class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>)
		ldstr "G3_C1763::ClassMethod2796.MI.17859<System.Object>()"
		ldstr "class G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1291.G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1291.G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>::ClassMethod2795()
		calli default string(class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>)
		ldstr "G2_C715::ClassMethod2795.11589()"
		ldstr "class G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1291.G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1291.G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>::ClassMethod2794()
		calli default string(class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>)
		ldstr "G2_C715::ClassMethod2794.11588()"
		ldstr "class G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1291.G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1291.G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>::Method7<object>()
		calli default string(class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>)
		ldstr "G2_C715::Method7.11587<System.Object>()"
		ldstr "class G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1291.G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1291.G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>::ClassMethod1349<object>()
		calli default string(class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>)
		ldstr "G1_C13::ClassMethod1349.4877<System.Object>()"
		ldstr "class G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1291.G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1291.G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>::ClassMethod1348<object>()
		calli default string(class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>)
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()"
		ldstr "class G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1291.G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1291.G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>::Method6<object>()
		calli default string(class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>)
		ldstr "G1_C13::Method6.4875<System.Object>()"
		ldstr "class G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1291.G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1291.G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>::Method5()
		calli default string(class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>)
		ldstr "G1_C13::Method5.4873()"
		ldstr "class G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1291.G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1291.G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>::Method4()
		calli default string(class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>)
		ldstr "G1_C13::Method4.4872()"
		ldstr "class G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>::ClassMethod4928()
		calli default string(class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>)
		ldstr "G3_C1763::ClassMethod4928.17858()"
		ldstr "class G3_C1763`1<class Generated1291.BaseClass0> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>::Method3<object>()
		calli default string(class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>)
		ldstr "G3_C1763::Method3.17857<System.Object>()"
		ldstr "class G3_C1763`1<class Generated1291.BaseClass0> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>::Method2<object>()
		calli default string(class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>)
		ldstr "G3_C1763::Method2.17856<System.Object>()"
		ldstr "class G3_C1763`1<class Generated1291.BaseClass0> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>::Method1()
		calli default string(class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>)
		ldstr "G3_C1763::Method1.17855()"
		ldstr "class G3_C1763`1<class Generated1291.BaseClass0> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>::Method0()
		calli default string(class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>)
		ldstr "G3_C1763::Method0.17854()"
		ldstr "class G3_C1763`1<class Generated1291.BaseClass0> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>::ClassMethod2796<object>()
		calli default string(class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>)
		ldstr "G3_C1763::ClassMethod2796.MI.17859<System.Object>()"
		ldstr "class G3_C1763`1<class Generated1291.BaseClass0> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>::ClassMethod2795()
		calli default string(class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>)
		ldstr "G2_C715::ClassMethod2795.11589()"
		ldstr "class G3_C1763`1<class Generated1291.BaseClass0> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>::ClassMethod2794()
		calli default string(class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>)
		ldstr "G2_C715::ClassMethod2794.11588()"
		ldstr "class G3_C1763`1<class Generated1291.BaseClass0> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>::Method7<object>()
		calli default string(class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>)
		ldstr "G2_C715::Method7.11587<System.Object>()"
		ldstr "class G3_C1763`1<class Generated1291.BaseClass0> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>::ClassMethod1349<object>()
		calli default string(class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>)
		ldstr "G1_C13::ClassMethod1349.4877<System.Object>()"
		ldstr "class G3_C1763`1<class Generated1291.BaseClass0> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>::ClassMethod1348<object>()
		calli default string(class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>)
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()"
		ldstr "class G3_C1763`1<class Generated1291.BaseClass0> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>::Method6<object>()
		calli default string(class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>)
		ldstr "G1_C13::Method6.4875<System.Object>()"
		ldstr "class G3_C1763`1<class Generated1291.BaseClass0> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>::Method5()
		calli default string(class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>)
		ldstr "G1_C13::Method5.4873()"
		ldstr "class G3_C1763`1<class Generated1291.BaseClass0> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>::Method4()
		calli default string(class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>)
		ldstr "G1_C13::Method4.4872()"
		ldstr "class G3_C1763`1<class Generated1291.BaseClass0> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated1291.IBase0::Method0()
		calli default string(class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>)
		ldstr "G3_C1763::Method0.17854()"
		ldstr "IBase0 on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated1291.IBase0::Method1()
		calli default string(class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>)
		ldstr "G3_C1763::Method1.17855()"
		ldstr "IBase0 on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated1291.IBase0::Method2<object>()
		calli default string(class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>)
		ldstr "G3_C1763::Method2.17856<System.Object>()"
		ldstr "IBase0 on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated1291.IBase0::Method3<object>()
		calli default string(class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>)
		ldstr "G3_C1763::Method3.17857<System.Object>()"
		ldstr "IBase0 on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1291.G1_C13`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1291.G1_C13`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>::ClassMethod1349<object>()
		calli default string(class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>)
		ldstr "G1_C13::ClassMethod1349.4877<System.Object>()"
		ldstr "class G1_C13`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1291.G1_C13`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1291.G1_C13`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>::ClassMethod1348<object>()
		calli default string(class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>)
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()"
		ldstr "class G1_C13`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1291.G1_C13`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1291.G1_C13`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>::Method6<object>()
		calli default string(class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>)
		ldstr "G1_C13::Method6.4875<System.Object>()"
		ldstr "class G1_C13`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1291.G1_C13`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1291.G1_C13`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>::Method5()
		calli default string(class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>)
		ldstr "G1_C13::Method5.4873()"
		ldstr "class G1_C13`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1291.G1_C13`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1291.G1_C13`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>::Method4()
		calli default string(class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>)
		ldstr "G1_C13::Method4.4872()"
		ldstr "class G1_C13`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1291.G1_C13`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1291.G1_C13`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>::Method7<object>()
		calli default string(class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>)
		ldstr "G1_C13::Method7.4871<System.Object>()"
		ldstr "class G1_C13`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1291.IBase2`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>::Method7<object>()
		calli default string(class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>)
		ldstr "G2_C715::Method7.11587<System.Object>()"
		ldstr "class IBase2`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1291.IBase1`1<class Generated1291.BaseClass1>::Method4()
		calli default string(class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>)
		ldstr "G1_C13::Method4.4872()"
		ldstr "class IBase1`1<class Generated1291.BaseClass1> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1291.IBase1`1<class Generated1291.BaseClass1>::Method5()
		calli default string(class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>)
		ldstr "G1_C13::Method5.MI.4874()"
		ldstr "class IBase1`1<class Generated1291.BaseClass1> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1291.IBase1`1<class Generated1291.BaseClass1>::Method6<object>()
		calli default string(class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>)
		ldstr "G1_C13::Method6.4875<System.Object>()"
		ldstr "class IBase1`1<class Generated1291.BaseClass1> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1291.IBase2`2<class Generated1291.BaseClass0,class Generated1291.BaseClass0>::Method7<object>()
		calli default string(class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>)
		ldstr "G2_C715::Method7.11587<System.Object>()"
		ldstr "class IBase2`2<class Generated1291.BaseClass0,class Generated1291.BaseClass0> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1291.IBase2`2<class Generated1291.BaseClass0,class Generated1291.BaseClass1>::Method7<object>()
		calli default string(class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>)
		ldstr "G2_C715::Method7.11587<System.Object>()"
		ldstr "class IBase2`2<class Generated1291.BaseClass0,class Generated1291.BaseClass1> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1291.IBase2`2<class Generated1291.BaseClass1,class Generated1291.BaseClass1>::Method7<object>()
		calli default string(class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>)
		ldstr "G2_C715::Method7.11587<System.Object>()"
		ldstr "class IBase2`2<class Generated1291.BaseClass1,class Generated1291.BaseClass1> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1291.IBase1`1<class Generated1291.BaseClass0>::Method4()
		calli default string(class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>)
		ldstr "G1_C13::Method4.4872()"
		ldstr "class IBase1`1<class Generated1291.BaseClass0> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1291.IBase1`1<class Generated1291.BaseClass0>::Method5()
		calli default string(class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>)
		ldstr "G1_C13::Method5.MI.4874()"
		ldstr "class IBase1`1<class Generated1291.BaseClass0> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1291.IBase1`1<class Generated1291.BaseClass0>::Method6<object>()
		calli default string(class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>)
		ldstr "G1_C13::Method6.4875<System.Object>()"
		ldstr "class IBase1`1<class Generated1291.BaseClass0> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1291.G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1291.G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>::ClassMethod2796<object>()
		calli default string(class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>)
		ldstr "G3_C1763::ClassMethod2796.MI.17859<System.Object>()"
		ldstr "class G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1291.G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1291.G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>::ClassMethod2795()
		calli default string(class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>)
		ldstr "G2_C715::ClassMethod2795.11589()"
		ldstr "class G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1291.G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1291.G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>::ClassMethod2794()
		calli default string(class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>)
		ldstr "G2_C715::ClassMethod2794.11588()"
		ldstr "class G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1291.G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1291.G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>::Method7<object>()
		calli default string(class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>)
		ldstr "G2_C715::Method7.11587<System.Object>()"
		ldstr "class G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1291.G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1291.G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>::ClassMethod1349<object>()
		calli default string(class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>)
		ldstr "G1_C13::ClassMethod1349.4877<System.Object>()"
		ldstr "class G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1291.G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1291.G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>::ClassMethod1348<object>()
		calli default string(class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>)
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()"
		ldstr "class G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1291.G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1291.G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>::Method6<object>()
		calli default string(class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>)
		ldstr "G1_C13::Method6.4875<System.Object>()"
		ldstr "class G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1291.G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1291.G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>::Method5()
		calli default string(class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>)
		ldstr "G1_C13::Method5.4873()"
		ldstr "class G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1291.G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1291.G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0>::Method4()
		calli default string(class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>)
		ldstr "G1_C13::Method4.4872()"
		ldstr "class G2_C715`2<class Generated1291.BaseClass1,class Generated1291.BaseClass0> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>::ClassMethod4928()
		calli default string(class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>)
		ldstr "G3_C1763::ClassMethod4928.17858()"
		ldstr "class G3_C1763`1<class Generated1291.BaseClass1> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>::Method3<object>()
		calli default string(class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>)
		ldstr "G3_C1763::Method3.17857<System.Object>()"
		ldstr "class G3_C1763`1<class Generated1291.BaseClass1> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>::Method2<object>()
		calli default string(class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>)
		ldstr "G3_C1763::Method2.17856<System.Object>()"
		ldstr "class G3_C1763`1<class Generated1291.BaseClass1> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>::Method1()
		calli default string(class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>)
		ldstr "G3_C1763::Method1.17855()"
		ldstr "class G3_C1763`1<class Generated1291.BaseClass1> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>::Method0()
		calli default string(class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>)
		ldstr "G3_C1763::Method0.17854()"
		ldstr "class G3_C1763`1<class Generated1291.BaseClass1> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>::ClassMethod2796<object>()
		calli default string(class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>)
		ldstr "G3_C1763::ClassMethod2796.MI.17859<System.Object>()"
		ldstr "class G3_C1763`1<class Generated1291.BaseClass1> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>::ClassMethod2795()
		calli default string(class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>)
		ldstr "G2_C715::ClassMethod2795.11589()"
		ldstr "class G3_C1763`1<class Generated1291.BaseClass1> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>::ClassMethod2794()
		calli default string(class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>)
		ldstr "G2_C715::ClassMethod2794.11588()"
		ldstr "class G3_C1763`1<class Generated1291.BaseClass1> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>::Method7<object>()
		calli default string(class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>)
		ldstr "G2_C715::Method7.11587<System.Object>()"
		ldstr "class G3_C1763`1<class Generated1291.BaseClass1> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>::ClassMethod1349<object>()
		calli default string(class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>)
		ldstr "G1_C13::ClassMethod1349.4877<System.Object>()"
		ldstr "class G3_C1763`1<class Generated1291.BaseClass1> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>::ClassMethod1348<object>()
		calli default string(class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>)
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()"
		ldstr "class G3_C1763`1<class Generated1291.BaseClass1> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>::Method6<object>()
		calli default string(class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>)
		ldstr "G1_C13::Method6.4875<System.Object>()"
		ldstr "class G3_C1763`1<class Generated1291.BaseClass1> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>::Method5()
		calli default string(class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>)
		ldstr "G1_C13::Method5.4873()"
		ldstr "class G3_C1763`1<class Generated1291.BaseClass1> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>::Method4()
		calli default string(class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>)
		ldstr "G1_C13::Method4.4872()"
		ldstr "class G3_C1763`1<class Generated1291.BaseClass1> on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated1291.IBase0::Method0()
		calli default string(class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>)
		ldstr "G3_C1763::Method0.17854()"
		ldstr "IBase0 on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated1291.IBase0::Method1()
		calli default string(class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>)
		ldstr "G3_C1763::Method1.17855()"
		ldstr "IBase0 on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated1291.IBase0::Method2<object>()
		calli default string(class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>)
		ldstr "G3_C1763::Method2.17856<System.Object>()"
		ldstr "IBase0 on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated1291.IBase0::Method3<object>()
		calli default string(class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>)
		ldstr "G3_C1763::Method3.17857<System.Object>()"
		ldstr "IBase0 on type class Generated1291.G3_C1763`1<class Generated1291.BaseClass1>"
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
		call void Generated1291.Generated1291::MethodCallingTest()
		call void Generated1291.Generated1291::ConstrainedCallsTest()
		call void Generated1291.Generated1291::StructConstrainedInterfaceCallsTest()
		call void Generated1291.Generated1291::CalliTest()
		ldc.i4 100
		ret
	}
}
