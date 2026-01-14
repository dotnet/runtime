// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) .ver 4:0:0:0 }
.assembly extern TestFramework { .publickeytoken = ( B0 3F 5F 7F 11 D5 0A 3A )  }

//TYPES IN FORWARDER ASSEMBLIES:

//TEST ASSEMBLY:
.assembly extern xunit.core {}

.class public Generated1118.BaseClass0 
{
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void [mscorlib]System.Object::.ctor()
		ret
	}
}
.class public Generated1118.BaseClass1 
		extends Generated1118.BaseClass0
{
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void Generated1118.BaseClass0::.ctor()
		ret
	}
}
.class public Generated1118.G3_C1590`1<T0> 
		extends class Generated1118.G2_C599`2<class Generated1118.BaseClass0,!T0>
		implements Generated1118.IBase0 
{
	.method public hidebysig virtual instance string Method0() cil managed noinlining { 
		ldstr "G3_C1590::Method0.16953()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase0.Method0'() cil managed noinlining { 
		.override method instance string Generated1118.IBase0::Method0()
		ldstr "G3_C1590::Method0.MI.16954()"
		ret
	}
	.method public hidebysig virtual instance string Method1() cil managed noinlining { 
		ldstr "G3_C1590::Method1.16955()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase0.Method1'() cil managed noinlining { 
		.override method instance string Generated1118.IBase0::Method1()
		ldstr "G3_C1590::Method1.MI.16956()"
		ret
	}
	.method public hidebysig virtual instance string Method2<M0>() cil managed noinlining { 
		ldstr "G3_C1590::Method2.16957<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig virtual instance string Method3<M0>() cil managed noinlining { 
		ldstr "G3_C1590::Method3.16958<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase0.Method3'<M0>() cil managed noinlining { 
		.override method instance string Generated1118.IBase0::Method3<[1]>()
		ldstr "G3_C1590::Method3.MI.16959<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod4568() cil managed noinlining { 
		ldstr "G3_C1590::ClassMethod4568.16960()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'G2_C599<class Generated1118.BaseClass0,T0>.ClassMethod2567'<M0>() cil managed noinlining { 
		.override method instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass0,!T0>::ClassMethod2567<[1]>()
		ldstr "G3_C1590::ClassMethod2567.MI.16961<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class Generated1118.G2_C599`2<class Generated1118.BaseClass0,!T0>::.ctor()
		ret
	}
}
.class public Generated1118.G2_C599`2<T0, T1> 
		extends class Generated1118.G1_C11`2<!T0,class Generated1118.BaseClass1>
		implements class Generated1118.IBase2`2<!T0,!T1>, IBase0 
{
	.method public hidebysig virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G2_C599::Method7.10517<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string Method0() cil managed noinlining { 
		ldstr "G2_C599::Method0.10518()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase0.Method0'() cil managed noinlining { 
		.override method instance string Generated1118.IBase0::Method0()
		ldstr "G2_C599::Method0.MI.10519()"
		ret
	}
	.method public hidebysig virtual instance string Method1() cil managed noinlining { 
		ldstr "G2_C599::Method1.10520()"
		ret
	}
	.method public hidebysig newslot virtual instance string Method2<M0>() cil managed noinlining { 
		ldstr "G2_C599::Method2.10521<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string Method3<M0>() cil managed noinlining { 
		ldstr "G2_C599::Method3.10522<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase0.Method3'<M0>() cil managed noinlining { 
		.override method instance string Generated1118.IBase0::Method3<[1]>()
		ldstr "G2_C599::Method3.MI.10523<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod2567<M0>() cil managed noinlining { 
		ldstr "G2_C599::ClassMethod2567.10524<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'G1_C11<T0,class Generated1118.BaseClass1>.ClassMethod1344'<M0>() cil managed noinlining { 
		.override method instance string class Generated1118.G1_C11`2<!T0,class Generated1118.BaseClass1>::ClassMethod1344<[1]>()
		ldstr "G2_C599::ClassMethod1344.MI.10525<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'G1_C11<T0,class Generated1118.BaseClass1>.ClassMethod1345'<M0>() cil managed noinlining { 
		.override method instance string class Generated1118.G1_C11`2<!T0,class Generated1118.BaseClass1>::ClassMethod1345<[1]>()
		ldstr "G2_C599::ClassMethod1345.MI.10526<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class Generated1118.G1_C11`2<!T0,class Generated1118.BaseClass1>::.ctor()
		ret
	}
}
.class interface public abstract Generated1118.IBase0 
{
	.method public hidebysig newslot abstract virtual instance string Method0() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method1() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method2<M0>() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method3<M0>() cil managed { }
}
.class public Generated1118.G1_C11`2<T0, T1> 
		implements class Generated1118.IBase2`2<class Generated1118.BaseClass1,!T1>, class Generated1118.IBase1`1<class Generated1118.BaseClass0> 
{
	.method public hidebysig newslot virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G1_C11::Method7.4854<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase2<class Generated1118.BaseClass1,T1>.Method7'<M0>() cil managed noinlining { 
		.override method instance string class Generated1118.IBase2`2<class Generated1118.BaseClass1,!T1>::Method7<[1]>()
		ldstr "G1_C11::Method7.MI.4855<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig virtual instance string Method4() cil managed noinlining { 
		ldstr "G1_C11::Method4.4856()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<class Generated1118.BaseClass0>.Method4'() cil managed noinlining { 
		.override method instance string class Generated1118.IBase1`1<class Generated1118.BaseClass0>::Method4()
		ldstr "G1_C11::Method4.MI.4857()"
		ret
	}
	.method public hidebysig virtual instance string Method5() cil managed noinlining { 
		ldstr "G1_C11::Method5.4858()"
		ret
	}
	.method public hidebysig virtual instance string Method6<M0>() cil managed noinlining { 
		ldstr "G1_C11::Method6.4859<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<class Generated1118.BaseClass0>.Method6'<M0>() cil managed noinlining { 
		.override method instance string class Generated1118.IBase1`1<class Generated1118.BaseClass0>::Method6<[1]>()
		ldstr "G1_C11::Method6.MI.4860<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1344<M0>() cil managed noinlining { 
		ldstr "G1_C11::ClassMethod1344.4861<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1345<M0>() cil managed noinlining { 
		ldstr "G1_C11::ClassMethod1345.4862<"
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
.class interface public abstract Generated1118.IBase2`2<+T0, -T1> 
{
	.method public hidebysig newslot abstract virtual instance string Method7<M0>() cil managed { }
}
.class interface public abstract Generated1118.IBase1`1<+T0> 
{
	.method public hidebysig newslot abstract virtual instance string Method4() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method5() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method6<M0>() cil managed { }
}
.class public auto ansi beforefieldinit Generated1118.Generated1118 {
	.method static void M.BaseClass0<(Generated1118.BaseClass0)W>(!!W inst, string exp) cil managed {
		.maxstack 5
		.locals init (string[] actualResults)
		ldc.i4.s    0
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.BaseClass0<(Generated1118.BaseClass0)W>(!!W inst, string exp)"
		ldc.i4.s 0
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.BaseClass1<(Generated1118.BaseClass1)W>(!!W inst, string exp) cil managed {
		.maxstack 5
		.locals init (string[] actualResults)
		ldc.i4.s    0
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.BaseClass1<(Generated1118.BaseClass1)W>(!!W inst, string exp)"
		ldc.i4.s 0
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1590.T<T0,(class Generated1118.G3_C1590`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 17
		.locals init (string[] actualResults)
		ldc.i4.s    12
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1590.T<T0,(class Generated1118.G3_C1590`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 12
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G3_C1590`1<!!T0>::ClassMethod1344<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G3_C1590`1<!!T0>::ClassMethod1345<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G3_C1590`1<!!T0>::ClassMethod2567<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G3_C1590`1<!!T0>::ClassMethod4568()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G3_C1590`1<!!T0>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G3_C1590`1<!!T0>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G3_C1590`1<!!T0>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G3_C1590`1<!!T0>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G3_C1590`1<!!T0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G3_C1590`1<!!T0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G3_C1590`1<!!T0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G3_C1590`1<!!T0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1590.A<(class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 17
		.locals init (string[] actualResults)
		ldc.i4.s    12
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1590.A<(class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 12
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>::ClassMethod1344<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>::ClassMethod1345<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>::ClassMethod2567<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>::ClassMethod4568()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1590.B<(class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 17
		.locals init (string[] actualResults)
		ldc.i4.s    12
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1590.B<(class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 12
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>::ClassMethod1344<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>::ClassMethod1345<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>::ClassMethod2567<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>::ClassMethod4568()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C599.T.T<T0,T1,(class Generated1118.G2_C599`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 16
		.locals init (string[] actualResults)
		ldc.i4.s    11
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C599.T.T<T0,T1,(class Generated1118.G2_C599`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 11
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G2_C599`2<!!T0,!!T1>::ClassMethod1344<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G2_C599`2<!!T0,!!T1>::ClassMethod1345<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G2_C599`2<!!T0,!!T1>::ClassMethod2567<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G2_C599`2<!!T0,!!T1>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G2_C599`2<!!T0,!!T1>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G2_C599`2<!!T0,!!T1>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G2_C599`2<!!T0,!!T1>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G2_C599`2<!!T0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G2_C599`2<!!T0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G2_C599`2<!!T0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G2_C599`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C599.A.T<T1,(class Generated1118.G2_C599`2<class Generated1118.BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 16
		.locals init (string[] actualResults)
		ldc.i4.s    11
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C599.A.T<T1,(class Generated1118.G2_C599`2<class Generated1118.BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 11
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass0,!!T1>::ClassMethod1344<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass0,!!T1>::ClassMethod1345<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass0,!!T1>::ClassMethod2567<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass0,!!T1>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass0,!!T1>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass0,!!T1>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass0,!!T1>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C599.A.A<(class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 16
		.locals init (string[] actualResults)
		ldc.i4.s    11
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C599.A.A<(class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 11
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>::ClassMethod1344<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>::ClassMethod1345<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>::ClassMethod2567<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C599.A.B<(class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 16
		.locals init (string[] actualResults)
		ldc.i4.s    11
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C599.A.B<(class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 11
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::ClassMethod1344<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::ClassMethod1345<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::ClassMethod2567<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C599.B.T<T1,(class Generated1118.G2_C599`2<class Generated1118.BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 16
		.locals init (string[] actualResults)
		ldc.i4.s    11
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C599.B.T<T1,(class Generated1118.G2_C599`2<class Generated1118.BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 11
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass1,!!T1>::ClassMethod1344<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass1,!!T1>::ClassMethod1345<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass1,!!T1>::ClassMethod2567<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass1,!!T1>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass1,!!T1>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass1,!!T1>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass1,!!T1>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass1,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass1,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass1,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C599.B.A<(class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 16
		.locals init (string[] actualResults)
		ldc.i4.s    11
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C599.B.A<(class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 11
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>::ClassMethod1344<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>::ClassMethod1345<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>::ClassMethod2567<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C599.B.B<(class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 16
		.locals init (string[] actualResults)
		ldc.i4.s    11
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C599.B.B<(class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 11
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>::ClassMethod1344<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>::ClassMethod1345<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>::ClassMethod2567<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase0<(Generated1118.IBase0)W>(!!W inst, string exp) cil managed {
		.maxstack 9
		.locals init (string[] actualResults)
		ldc.i4.s    4
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase0<(Generated1118.IBase0)W>(!!W inst, string exp)"
		ldc.i4.s 4
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string Generated1118.IBase0::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string Generated1118.IBase0::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string Generated1118.IBase0::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string Generated1118.IBase0::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C11.T.T<T0,T1,(class Generated1118.G1_C11`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C11.T.T<T0,T1,(class Generated1118.G1_C11`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G1_C11`2<!!T0,!!T1>::ClassMethod1344<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G1_C11`2<!!T0,!!T1>::ClassMethod1345<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G1_C11`2<!!T0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G1_C11`2<!!T0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G1_C11`2<!!T0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G1_C11`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C11.A.T<T1,(class Generated1118.G1_C11`2<class Generated1118.BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C11.A.T<T1,(class Generated1118.G1_C11`2<class Generated1118.BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass0,!!T1>::ClassMethod1344<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass0,!!T1>::ClassMethod1345<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C11.A.A<(class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C11.A.A<(class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>::ClassMethod1344<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>::ClassMethod1345<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C11.A.B<(class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C11.A.B<(class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::ClassMethod1344<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::ClassMethod1345<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C11.B.T<T1,(class Generated1118.G1_C11`2<class Generated1118.BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C11.B.T<T1,(class Generated1118.G1_C11`2<class Generated1118.BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass1,!!T1>::ClassMethod1344<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass1,!!T1>::ClassMethod1345<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass1,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass1,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass1,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C11.B.A<(class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C11.B.A<(class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>::ClassMethod1344<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>::ClassMethod1345<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C11.B.B<(class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C11.B.B<(class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>::ClassMethod1344<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>::ClassMethod1345<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.T.T<T0,T1,(class Generated1118.IBase2`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.T.T<T0,T1,(class Generated1118.IBase2`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.IBase2`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.A.T<T1,(class Generated1118.IBase2`2<class Generated1118.BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.A.T<T1,(class Generated1118.IBase2`2<class Generated1118.BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.IBase2`2<class Generated1118.BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.A.A<(class Generated1118.IBase2`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.A.A<(class Generated1118.IBase2`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.IBase2`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.A.B<(class Generated1118.IBase2`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.A.B<(class Generated1118.IBase2`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.IBase2`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.B.T<T1,(class Generated1118.IBase2`2<class Generated1118.BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.B.T<T1,(class Generated1118.IBase2`2<class Generated1118.BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.IBase2`2<class Generated1118.BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.B.A<(class Generated1118.IBase2`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.B.A<(class Generated1118.IBase2`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.IBase2`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.B.B<(class Generated1118.IBase2`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.B.B<(class Generated1118.IBase2`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.IBase2`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase1.T<T0,(class Generated1118.IBase1`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase1.T<T0,(class Generated1118.IBase1`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.IBase1`1<!!T0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.IBase1`1<!!T0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.IBase1`1<!!T0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase1.A<(class Generated1118.IBase1`1<class Generated1118.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase1.A<(class Generated1118.IBase1`1<class Generated1118.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.IBase1`1<class Generated1118.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.IBase1`1<class Generated1118.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.IBase1`1<class Generated1118.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase1.B<(class Generated1118.IBase1`1<class Generated1118.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase1.B<(class Generated1118.IBase1`1<class Generated1118.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.IBase1`1<class Generated1118.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.IBase1`1<class Generated1118.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1118.IBase1`1<class Generated1118.BaseClass1>::Method6<object>()
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
		newobj instance void class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>
		callvirt instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::ClassMethod1345<object>()
		ldstr "G2_C599::ClassMethod1345.MI.10526<System.Object>()"
		ldstr "class G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>
		callvirt instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::ClassMethod1344<object>()
		ldstr "G2_C599::ClassMethod1344.MI.10525<System.Object>()"
		ldstr "class G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>
		callvirt instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::Method6<object>()
		ldstr "G1_C11::Method6.4859<System.Object>()"
		ldstr "class G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>
		callvirt instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::Method5()
		ldstr "G1_C11::Method5.4858()"
		ldstr "class G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>
		callvirt instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::Method4()
		ldstr "G1_C11::Method4.4856()"
		ldstr "class G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>
		callvirt instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::Method7<object>()
		ldstr "G2_C599::Method7.10517<System.Object>()"
		ldstr "class G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1118.IBase2`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>::Method7<object>()
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1118.IBase1`1<class Generated1118.BaseClass0>::Method4()
		ldstr "G1_C11::Method4.MI.4857()"
		ldstr "class IBase1`1<class Generated1118.BaseClass0> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1118.IBase1`1<class Generated1118.BaseClass0>::Method5()
		ldstr "G1_C11::Method5.4858()"
		ldstr "class IBase1`1<class Generated1118.BaseClass0> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1118.IBase1`1<class Generated1118.BaseClass0>::Method6<object>()
		ldstr "G1_C11::Method6.MI.4860<System.Object>()"
		ldstr "class IBase1`1<class Generated1118.BaseClass0> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1118.IBase2`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::Method7<object>()
		ldstr "G2_C599::Method7.10517<System.Object>()"
		ldstr "class IBase2`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>
		callvirt instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>::ClassMethod2567<object>()
		ldstr "G3_C1590::ClassMethod2567.MI.16961<System.Object>()"
		ldstr "class G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>
		callvirt instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>::Method3<object>()
		ldstr "G3_C1590::Method3.16958<System.Object>()"
		ldstr "class G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>
		callvirt instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>::Method2<object>()
		ldstr "G3_C1590::Method2.16957<System.Object>()"
		ldstr "class G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>
		callvirt instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>::Method1()
		ldstr "G3_C1590::Method1.16955()"
		ldstr "class G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>
		callvirt instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>::Method0()
		ldstr "G3_C1590::Method0.16953()"
		ldstr "class G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>
		callvirt instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>::Method7<object>()
		ldstr "G2_C599::Method7.10517<System.Object>()"
		ldstr "class G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>
		callvirt instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>::ClassMethod1345<object>()
		ldstr "G2_C599::ClassMethod1345.MI.10526<System.Object>()"
		ldstr "class G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>
		callvirt instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>::ClassMethod1344<object>()
		ldstr "G2_C599::ClassMethod1344.MI.10525<System.Object>()"
		ldstr "class G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>
		callvirt instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>::Method6<object>()
		ldstr "G1_C11::Method6.4859<System.Object>()"
		ldstr "class G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>
		callvirt instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>::Method5()
		ldstr "G1_C11::Method5.4858()"
		ldstr "class G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>
		callvirt instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>::Method4()
		ldstr "G1_C11::Method4.4856()"
		ldstr "class G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1118.IBase2`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>::Method7<object>()
		ldstr "G2_C599::Method7.10517<System.Object>()"
		ldstr "class IBase2`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string Generated1118.IBase0::Method0()
		ldstr "G3_C1590::Method0.MI.16954()"
		ldstr "IBase0 on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated1118.IBase0::Method1()
		ldstr "G3_C1590::Method1.MI.16956()"
		ldstr "IBase0 on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated1118.IBase0::Method2<object>()
		ldstr "G3_C1590::Method2.16957<System.Object>()"
		ldstr "IBase0 on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated1118.IBase0::Method3<object>()
		ldstr "G3_C1590::Method3.MI.16959<System.Object>()"
		ldstr "IBase0 on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>
		callvirt instance string class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>::ClassMethod4568()
		ldstr "G3_C1590::ClassMethod4568.16960()"
		ldstr "class G3_C1590`1<class Generated1118.BaseClass0> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>
		callvirt instance string class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>::Method3<object>()
		ldstr "G3_C1590::Method3.16958<System.Object>()"
		ldstr "class G3_C1590`1<class Generated1118.BaseClass0> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>
		callvirt instance string class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>::Method2<object>()
		ldstr "G3_C1590::Method2.16957<System.Object>()"
		ldstr "class G3_C1590`1<class Generated1118.BaseClass0> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>
		callvirt instance string class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>::Method1()
		ldstr "G3_C1590::Method1.16955()"
		ldstr "class G3_C1590`1<class Generated1118.BaseClass0> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>
		callvirt instance string class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>::Method0()
		ldstr "G3_C1590::Method0.16953()"
		ldstr "class G3_C1590`1<class Generated1118.BaseClass0> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>
		callvirt instance string class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>::ClassMethod2567<object>()
		ldstr "G3_C1590::ClassMethod2567.MI.16961<System.Object>()"
		ldstr "class G3_C1590`1<class Generated1118.BaseClass0> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>
		callvirt instance string class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>::Method7<object>()
		ldstr "G2_C599::Method7.10517<System.Object>()"
		ldstr "class G3_C1590`1<class Generated1118.BaseClass0> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>
		callvirt instance string class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>::ClassMethod1345<object>()
		ldstr "G2_C599::ClassMethod1345.MI.10526<System.Object>()"
		ldstr "class G3_C1590`1<class Generated1118.BaseClass0> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>
		callvirt instance string class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>::ClassMethod1344<object>()
		ldstr "G2_C599::ClassMethod1344.MI.10525<System.Object>()"
		ldstr "class G3_C1590`1<class Generated1118.BaseClass0> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>
		callvirt instance string class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>::Method6<object>()
		ldstr "G1_C11::Method6.4859<System.Object>()"
		ldstr "class G3_C1590`1<class Generated1118.BaseClass0> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>
		callvirt instance string class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>::Method5()
		ldstr "G1_C11::Method5.4858()"
		ldstr "class G3_C1590`1<class Generated1118.BaseClass0> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>
		callvirt instance string class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>::Method4()
		ldstr "G1_C11::Method4.4856()"
		ldstr "class G3_C1590`1<class Generated1118.BaseClass0> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>
		callvirt instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::ClassMethod1345<object>()
		ldstr "G2_C599::ClassMethod1345.MI.10526<System.Object>()"
		ldstr "class G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>
		callvirt instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::ClassMethod1344<object>()
		ldstr "G2_C599::ClassMethod1344.MI.10525<System.Object>()"
		ldstr "class G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>
		callvirt instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::Method6<object>()
		ldstr "G1_C11::Method6.4859<System.Object>()"
		ldstr "class G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>
		callvirt instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::Method5()
		ldstr "G1_C11::Method5.4858()"
		ldstr "class G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>
		callvirt instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::Method4()
		ldstr "G1_C11::Method4.4856()"
		ldstr "class G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>
		callvirt instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::Method7<object>()
		ldstr "G2_C599::Method7.10517<System.Object>()"
		ldstr "class G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1118.IBase2`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>::Method7<object>()
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1118.IBase1`1<class Generated1118.BaseClass0>::Method4()
		ldstr "G1_C11::Method4.MI.4857()"
		ldstr "class IBase1`1<class Generated1118.BaseClass0> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1118.IBase1`1<class Generated1118.BaseClass0>::Method5()
		ldstr "G1_C11::Method5.4858()"
		ldstr "class IBase1`1<class Generated1118.BaseClass0> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1118.IBase1`1<class Generated1118.BaseClass0>::Method6<object>()
		ldstr "G1_C11::Method6.MI.4860<System.Object>()"
		ldstr "class IBase1`1<class Generated1118.BaseClass0> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1118.IBase2`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::Method7<object>()
		ldstr "G2_C599::Method7.10517<System.Object>()"
		ldstr "class IBase2`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>
		callvirt instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::ClassMethod2567<object>()
		ldstr "G3_C1590::ClassMethod2567.MI.16961<System.Object>()"
		ldstr "class G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>
		callvirt instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::Method3<object>()
		ldstr "G3_C1590::Method3.16958<System.Object>()"
		ldstr "class G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>
		callvirt instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::Method2<object>()
		ldstr "G3_C1590::Method2.16957<System.Object>()"
		ldstr "class G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>
		callvirt instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::Method1()
		ldstr "G3_C1590::Method1.16955()"
		ldstr "class G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>
		callvirt instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::Method0()
		ldstr "G3_C1590::Method0.16953()"
		ldstr "class G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>
		callvirt instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::Method7<object>()
		ldstr "G2_C599::Method7.10517<System.Object>()"
		ldstr "class G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>
		callvirt instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::ClassMethod1345<object>()
		ldstr "G2_C599::ClassMethod1345.MI.10526<System.Object>()"
		ldstr "class G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>
		callvirt instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::ClassMethod1344<object>()
		ldstr "G2_C599::ClassMethod1344.MI.10525<System.Object>()"
		ldstr "class G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>
		callvirt instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::Method6<object>()
		ldstr "G1_C11::Method6.4859<System.Object>()"
		ldstr "class G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>
		callvirt instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::Method5()
		ldstr "G1_C11::Method5.4858()"
		ldstr "class G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>
		callvirt instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::Method4()
		ldstr "G1_C11::Method4.4856()"
		ldstr "class G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string Generated1118.IBase0::Method0()
		ldstr "G3_C1590::Method0.MI.16954()"
		ldstr "IBase0 on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated1118.IBase0::Method1()
		ldstr "G3_C1590::Method1.MI.16956()"
		ldstr "IBase0 on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated1118.IBase0::Method2<object>()
		ldstr "G3_C1590::Method2.16957<System.Object>()"
		ldstr "IBase0 on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated1118.IBase0::Method3<object>()
		ldstr "G3_C1590::Method3.MI.16959<System.Object>()"
		ldstr "IBase0 on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>
		callvirt instance string class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>::ClassMethod4568()
		ldstr "G3_C1590::ClassMethod4568.16960()"
		ldstr "class G3_C1590`1<class Generated1118.BaseClass1> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>
		callvirt instance string class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>::Method3<object>()
		ldstr "G3_C1590::Method3.16958<System.Object>()"
		ldstr "class G3_C1590`1<class Generated1118.BaseClass1> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>
		callvirt instance string class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>::Method2<object>()
		ldstr "G3_C1590::Method2.16957<System.Object>()"
		ldstr "class G3_C1590`1<class Generated1118.BaseClass1> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>
		callvirt instance string class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>::Method1()
		ldstr "G3_C1590::Method1.16955()"
		ldstr "class G3_C1590`1<class Generated1118.BaseClass1> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>
		callvirt instance string class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>::Method0()
		ldstr "G3_C1590::Method0.16953()"
		ldstr "class G3_C1590`1<class Generated1118.BaseClass1> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>
		callvirt instance string class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>::ClassMethod2567<object>()
		ldstr "G3_C1590::ClassMethod2567.MI.16961<System.Object>()"
		ldstr "class G3_C1590`1<class Generated1118.BaseClass1> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>
		callvirt instance string class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>::Method7<object>()
		ldstr "G2_C599::Method7.10517<System.Object>()"
		ldstr "class G3_C1590`1<class Generated1118.BaseClass1> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>
		callvirt instance string class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>::ClassMethod1345<object>()
		ldstr "G2_C599::ClassMethod1345.MI.10526<System.Object>()"
		ldstr "class G3_C1590`1<class Generated1118.BaseClass1> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>
		callvirt instance string class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>::ClassMethod1344<object>()
		ldstr "G2_C599::ClassMethod1344.MI.10525<System.Object>()"
		ldstr "class G3_C1590`1<class Generated1118.BaseClass1> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>
		callvirt instance string class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>::Method6<object>()
		ldstr "G1_C11::Method6.4859<System.Object>()"
		ldstr "class G3_C1590`1<class Generated1118.BaseClass1> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>
		callvirt instance string class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>::Method5()
		ldstr "G1_C11::Method5.4858()"
		ldstr "class G3_C1590`1<class Generated1118.BaseClass1> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>
		callvirt instance string class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>::Method4()
		ldstr "G1_C11::Method4.4856()"
		ldstr "class G3_C1590`1<class Generated1118.BaseClass1> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>
		callvirt instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::ClassMethod1345<object>()
		ldstr "G2_C599::ClassMethod1345.MI.10526<System.Object>()"
		ldstr "class G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>
		callvirt instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::ClassMethod1344<object>()
		ldstr "G2_C599::ClassMethod1344.MI.10525<System.Object>()"
		ldstr "class G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>
		callvirt instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::Method6<object>()
		ldstr "G1_C11::Method6.4859<System.Object>()"
		ldstr "class G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>
		callvirt instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::Method5()
		ldstr "G1_C11::Method5.4858()"
		ldstr "class G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>
		callvirt instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::Method4()
		ldstr "G1_C11::Method4.4856()"
		ldstr "class G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>
		callvirt instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::Method7<object>()
		ldstr "G2_C599::Method7.10517<System.Object>()"
		ldstr "class G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1118.IBase2`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>::Method7<object>()
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1118.IBase1`1<class Generated1118.BaseClass0>::Method4()
		ldstr "G1_C11::Method4.MI.4857()"
		ldstr "class IBase1`1<class Generated1118.BaseClass0> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1118.IBase1`1<class Generated1118.BaseClass0>::Method5()
		ldstr "G1_C11::Method5.4858()"
		ldstr "class IBase1`1<class Generated1118.BaseClass0> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1118.IBase1`1<class Generated1118.BaseClass0>::Method6<object>()
		ldstr "G1_C11::Method6.MI.4860<System.Object>()"
		ldstr "class IBase1`1<class Generated1118.BaseClass0> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1118.IBase2`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::Method7<object>()
		ldstr "G2_C599::Method7.10517<System.Object>()"
		ldstr "class IBase2`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>
		callvirt instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>::ClassMethod2567<object>()
		ldstr "G2_C599::ClassMethod2567.10524<System.Object>()"
		ldstr "class G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>
		callvirt instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>::Method3<object>()
		ldstr "G2_C599::Method3.10522<System.Object>()"
		ldstr "class G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>
		callvirt instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>::Method2<object>()
		ldstr "G2_C599::Method2.10521<System.Object>()"
		ldstr "class G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>
		callvirt instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>::Method1()
		ldstr "G2_C599::Method1.10520()"
		ldstr "class G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>
		callvirt instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>::Method0()
		ldstr "G2_C599::Method0.10518()"
		ldstr "class G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>
		callvirt instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>::Method7<object>()
		ldstr "G2_C599::Method7.10517<System.Object>()"
		ldstr "class G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>
		callvirt instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>::ClassMethod1345<object>()
		ldstr "G2_C599::ClassMethod1345.MI.10526<System.Object>()"
		ldstr "class G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>
		callvirt instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>::ClassMethod1344<object>()
		ldstr "G2_C599::ClassMethod1344.MI.10525<System.Object>()"
		ldstr "class G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>
		callvirt instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>::Method6<object>()
		ldstr "G1_C11::Method6.4859<System.Object>()"
		ldstr "class G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>
		callvirt instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>::Method5()
		ldstr "G1_C11::Method5.4858()"
		ldstr "class G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>
		callvirt instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>::Method4()
		ldstr "G1_C11::Method4.4856()"
		ldstr "class G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1118.IBase2`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>::Method7<object>()
		ldstr "G2_C599::Method7.10517<System.Object>()"
		ldstr "class IBase2`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string Generated1118.IBase0::Method0()
		ldstr "G2_C599::Method0.MI.10519()"
		ldstr "IBase0 on type class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated1118.IBase0::Method1()
		ldstr "G2_C599::Method1.10520()"
		ldstr "IBase0 on type class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated1118.IBase0::Method2<object>()
		ldstr "G2_C599::Method2.10521<System.Object>()"
		ldstr "IBase0 on type class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated1118.IBase0::Method3<object>()
		ldstr "G2_C599::Method3.MI.10523<System.Object>()"
		ldstr "IBase0 on type class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>
		callvirt instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::ClassMethod1345<object>()
		ldstr "G2_C599::ClassMethod1345.MI.10526<System.Object>()"
		ldstr "class G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>
		callvirt instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::ClassMethod1344<object>()
		ldstr "G2_C599::ClassMethod1344.MI.10525<System.Object>()"
		ldstr "class G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>
		callvirt instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::Method6<object>()
		ldstr "G1_C11::Method6.4859<System.Object>()"
		ldstr "class G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>
		callvirt instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::Method5()
		ldstr "G1_C11::Method5.4858()"
		ldstr "class G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>
		callvirt instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::Method4()
		ldstr "G1_C11::Method4.4856()"
		ldstr "class G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>
		callvirt instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::Method7<object>()
		ldstr "G2_C599::Method7.10517<System.Object>()"
		ldstr "class G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1118.IBase2`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>::Method7<object>()
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1118.IBase1`1<class Generated1118.BaseClass0>::Method4()
		ldstr "G1_C11::Method4.MI.4857()"
		ldstr "class IBase1`1<class Generated1118.BaseClass0> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1118.IBase1`1<class Generated1118.BaseClass0>::Method5()
		ldstr "G1_C11::Method5.4858()"
		ldstr "class IBase1`1<class Generated1118.BaseClass0> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1118.IBase1`1<class Generated1118.BaseClass0>::Method6<object>()
		ldstr "G1_C11::Method6.MI.4860<System.Object>()"
		ldstr "class IBase1`1<class Generated1118.BaseClass0> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1118.IBase2`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::Method7<object>()
		ldstr "G2_C599::Method7.10517<System.Object>()"
		ldstr "class IBase2`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>
		callvirt instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::ClassMethod2567<object>()
		ldstr "G2_C599::ClassMethod2567.10524<System.Object>()"
		ldstr "class G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>
		callvirt instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::Method3<object>()
		ldstr "G2_C599::Method3.10522<System.Object>()"
		ldstr "class G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>
		callvirt instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::Method2<object>()
		ldstr "G2_C599::Method2.10521<System.Object>()"
		ldstr "class G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>
		callvirt instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::Method1()
		ldstr "G2_C599::Method1.10520()"
		ldstr "class G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>
		callvirt instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::Method0()
		ldstr "G2_C599::Method0.10518()"
		ldstr "class G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>
		callvirt instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::Method7<object>()
		ldstr "G2_C599::Method7.10517<System.Object>()"
		ldstr "class G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>
		callvirt instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::ClassMethod1345<object>()
		ldstr "G2_C599::ClassMethod1345.MI.10526<System.Object>()"
		ldstr "class G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>
		callvirt instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::ClassMethod1344<object>()
		ldstr "G2_C599::ClassMethod1344.MI.10525<System.Object>()"
		ldstr "class G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>
		callvirt instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::Method6<object>()
		ldstr "G1_C11::Method6.4859<System.Object>()"
		ldstr "class G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>
		callvirt instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::Method5()
		ldstr "G1_C11::Method5.4858()"
		ldstr "class G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>
		callvirt instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::Method4()
		ldstr "G1_C11::Method4.4856()"
		ldstr "class G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string Generated1118.IBase0::Method0()
		ldstr "G2_C599::Method0.MI.10519()"
		ldstr "IBase0 on type class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated1118.IBase0::Method1()
		ldstr "G2_C599::Method1.10520()"
		ldstr "IBase0 on type class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated1118.IBase0::Method2<object>()
		ldstr "G2_C599::Method2.10521<System.Object>()"
		ldstr "IBase0 on type class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated1118.IBase0::Method3<object>()
		ldstr "G2_C599::Method3.MI.10523<System.Object>()"
		ldstr "IBase0 on type class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>
		callvirt instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>::ClassMethod1345<object>()
		ldstr "G2_C599::ClassMethod1345.MI.10526<System.Object>()"
		ldstr "class G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>
		callvirt instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>::ClassMethod1344<object>()
		ldstr "G2_C599::ClassMethod1344.MI.10525<System.Object>()"
		ldstr "class G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>
		callvirt instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>::Method6<object>()
		ldstr "G1_C11::Method6.4859<System.Object>()"
		ldstr "class G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>
		callvirt instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>::Method5()
		ldstr "G1_C11::Method5.4858()"
		ldstr "class G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>
		callvirt instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>::Method4()
		ldstr "G1_C11::Method4.4856()"
		ldstr "class G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>
		callvirt instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>::Method7<object>()
		ldstr "G2_C599::Method7.10517<System.Object>()"
		ldstr "class G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1118.IBase2`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>::Method7<object>()
		ldstr "G2_C599::Method7.10517<System.Object>()"
		ldstr "class IBase2`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1118.IBase1`1<class Generated1118.BaseClass0>::Method4()
		ldstr "G1_C11::Method4.MI.4857()"
		ldstr "class IBase1`1<class Generated1118.BaseClass0> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1118.IBase1`1<class Generated1118.BaseClass0>::Method5()
		ldstr "G1_C11::Method5.4858()"
		ldstr "class IBase1`1<class Generated1118.BaseClass0> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1118.IBase1`1<class Generated1118.BaseClass0>::Method6<object>()
		ldstr "G1_C11::Method6.MI.4860<System.Object>()"
		ldstr "class IBase1`1<class Generated1118.BaseClass0> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1118.IBase2`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::Method7<object>()
		ldstr "G2_C599::Method7.10517<System.Object>()"
		ldstr "class IBase2`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>
		callvirt instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>::ClassMethod2567<object>()
		ldstr "G2_C599::ClassMethod2567.10524<System.Object>()"
		ldstr "class G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>
		callvirt instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>::Method3<object>()
		ldstr "G2_C599::Method3.10522<System.Object>()"
		ldstr "class G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>
		callvirt instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>::Method2<object>()
		ldstr "G2_C599::Method2.10521<System.Object>()"
		ldstr "class G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>
		callvirt instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>::Method1()
		ldstr "G2_C599::Method1.10520()"
		ldstr "class G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>
		callvirt instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>::Method0()
		ldstr "G2_C599::Method0.10518()"
		ldstr "class G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>
		callvirt instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>::Method7<object>()
		ldstr "G2_C599::Method7.10517<System.Object>()"
		ldstr "class G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>
		callvirt instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>::ClassMethod1345<object>()
		ldstr "G2_C599::ClassMethod1345.MI.10526<System.Object>()"
		ldstr "class G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>
		callvirt instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>::ClassMethod1344<object>()
		ldstr "G2_C599::ClassMethod1344.MI.10525<System.Object>()"
		ldstr "class G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>
		callvirt instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>::Method6<object>()
		ldstr "G1_C11::Method6.4859<System.Object>()"
		ldstr "class G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>
		callvirt instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>::Method5()
		ldstr "G1_C11::Method5.4858()"
		ldstr "class G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>
		callvirt instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>::Method4()
		ldstr "G1_C11::Method4.4856()"
		ldstr "class G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1118.IBase2`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>::Method7<object>()
		ldstr "G2_C599::Method7.10517<System.Object>()"
		ldstr "class IBase2`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string Generated1118.IBase0::Method0()
		ldstr "G2_C599::Method0.MI.10519()"
		ldstr "IBase0 on type class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated1118.IBase0::Method1()
		ldstr "G2_C599::Method1.10520()"
		ldstr "IBase0 on type class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated1118.IBase0::Method2<object>()
		ldstr "G2_C599::Method2.10521<System.Object>()"
		ldstr "IBase0 on type class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated1118.IBase0::Method3<object>()
		ldstr "G2_C599::Method3.MI.10523<System.Object>()"
		ldstr "IBase0 on type class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1118.IBase2`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>::Method7<object>()
		ldstr "G2_C599::Method7.10517<System.Object>()"
		ldstr "class IBase2`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>
		callvirt instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>::ClassMethod1345<object>()
		ldstr "G2_C599::ClassMethod1345.MI.10526<System.Object>()"
		ldstr "class G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>
		callvirt instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>::ClassMethod1344<object>()
		ldstr "G2_C599::ClassMethod1344.MI.10525<System.Object>()"
		ldstr "class G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>
		callvirt instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>::Method6<object>()
		ldstr "G1_C11::Method6.4859<System.Object>()"
		ldstr "class G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>
		callvirt instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>::Method5()
		ldstr "G1_C11::Method5.4858()"
		ldstr "class G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>
		callvirt instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>::Method4()
		ldstr "G1_C11::Method4.4856()"
		ldstr "class G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>
		callvirt instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>::Method7<object>()
		ldstr "G2_C599::Method7.10517<System.Object>()"
		ldstr "class G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1118.IBase2`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>::Method7<object>()
		ldstr "G2_C599::Method7.10517<System.Object>()"
		ldstr "class IBase2`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1118.IBase1`1<class Generated1118.BaseClass0>::Method4()
		ldstr "G1_C11::Method4.MI.4857()"
		ldstr "class IBase1`1<class Generated1118.BaseClass0> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1118.IBase1`1<class Generated1118.BaseClass0>::Method5()
		ldstr "G1_C11::Method5.4858()"
		ldstr "class IBase1`1<class Generated1118.BaseClass0> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1118.IBase1`1<class Generated1118.BaseClass0>::Method6<object>()
		ldstr "G1_C11::Method6.MI.4860<System.Object>()"
		ldstr "class IBase1`1<class Generated1118.BaseClass0> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1118.IBase2`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::Method7<object>()
		ldstr "G2_C599::Method7.10517<System.Object>()"
		ldstr "class IBase2`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>
		callvirt instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>::ClassMethod2567<object>()
		ldstr "G2_C599::ClassMethod2567.10524<System.Object>()"
		ldstr "class G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>
		callvirt instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>::Method3<object>()
		ldstr "G2_C599::Method3.10522<System.Object>()"
		ldstr "class G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>
		callvirt instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>::Method2<object>()
		ldstr "G2_C599::Method2.10521<System.Object>()"
		ldstr "class G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>
		callvirt instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>::Method1()
		ldstr "G2_C599::Method1.10520()"
		ldstr "class G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>
		callvirt instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>::Method0()
		ldstr "G2_C599::Method0.10518()"
		ldstr "class G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>
		callvirt instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>::Method7<object>()
		ldstr "G2_C599::Method7.10517<System.Object>()"
		ldstr "class G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>
		callvirt instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>::ClassMethod1345<object>()
		ldstr "G2_C599::ClassMethod1345.MI.10526<System.Object>()"
		ldstr "class G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>
		callvirt instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>::ClassMethod1344<object>()
		ldstr "G2_C599::ClassMethod1344.MI.10525<System.Object>()"
		ldstr "class G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>
		callvirt instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>::Method6<object>()
		ldstr "G1_C11::Method6.4859<System.Object>()"
		ldstr "class G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>
		callvirt instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>::Method5()
		ldstr "G1_C11::Method5.4858()"
		ldstr "class G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>
		callvirt instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>::Method4()
		ldstr "G1_C11::Method4.4856()"
		ldstr "class G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string Generated1118.IBase0::Method0()
		ldstr "G2_C599::Method0.MI.10519()"
		ldstr "IBase0 on type class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated1118.IBase0::Method1()
		ldstr "G2_C599::Method1.10520()"
		ldstr "IBase0 on type class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated1118.IBase0::Method2<object>()
		ldstr "G2_C599::Method2.10521<System.Object>()"
		ldstr "IBase0 on type class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated1118.IBase0::Method3<object>()
		ldstr "G2_C599::Method3.MI.10523<System.Object>()"
		ldstr "IBase0 on type class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>
		callvirt instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>::ClassMethod1345<object>()
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0> on type class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>
		callvirt instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>::ClassMethod1344<object>()
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()"
		ldstr "class G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0> on type class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>
		callvirt instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>::Method6<object>()
		ldstr "G1_C11::Method6.4859<System.Object>()"
		ldstr "class G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0> on type class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>
		callvirt instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>::Method5()
		ldstr "G1_C11::Method5.4858()"
		ldstr "class G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0> on type class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>
		callvirt instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>::Method4()
		ldstr "G1_C11::Method4.4856()"
		ldstr "class G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0> on type class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>
		callvirt instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>::Method7<object>()
		ldstr "G1_C11::Method7.4854<System.Object>()"
		ldstr "class G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0> on type class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1118.IBase2`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>::Method7<object>()
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0> on type class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1118.IBase1`1<class Generated1118.BaseClass0>::Method4()
		ldstr "G1_C11::Method4.MI.4857()"
		ldstr "class IBase1`1<class Generated1118.BaseClass0> on type class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1118.IBase1`1<class Generated1118.BaseClass0>::Method5()
		ldstr "G1_C11::Method5.4858()"
		ldstr "class IBase1`1<class Generated1118.BaseClass0> on type class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1118.IBase1`1<class Generated1118.BaseClass0>::Method6<object>()
		ldstr "G1_C11::Method6.MI.4860<System.Object>()"
		ldstr "class IBase1`1<class Generated1118.BaseClass0> on type class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1118.IBase2`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>::Method7<object>()
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0> on type class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1118.IBase2`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::Method7<object>()
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1> on type class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1118.IBase2`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>::Method7<object>()
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1> on type class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>
		callvirt instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::ClassMethod1345<object>()
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1> on type class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>
		callvirt instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::ClassMethod1344<object>()
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()"
		ldstr "class G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1> on type class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>
		callvirt instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::Method6<object>()
		ldstr "G1_C11::Method6.4859<System.Object>()"
		ldstr "class G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1> on type class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>
		callvirt instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::Method5()
		ldstr "G1_C11::Method5.4858()"
		ldstr "class G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1> on type class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>
		callvirt instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::Method4()
		ldstr "G1_C11::Method4.4856()"
		ldstr "class G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1> on type class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>
		callvirt instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::Method7<object>()
		ldstr "G1_C11::Method7.4854<System.Object>()"
		ldstr "class G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1> on type class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1118.IBase2`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>::Method7<object>()
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1> on type class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1118.IBase1`1<class Generated1118.BaseClass0>::Method4()
		ldstr "G1_C11::Method4.MI.4857()"
		ldstr "class IBase1`1<class Generated1118.BaseClass0> on type class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1118.IBase1`1<class Generated1118.BaseClass0>::Method5()
		ldstr "G1_C11::Method5.4858()"
		ldstr "class IBase1`1<class Generated1118.BaseClass0> on type class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1118.IBase1`1<class Generated1118.BaseClass0>::Method6<object>()
		ldstr "G1_C11::Method6.MI.4860<System.Object>()"
		ldstr "class IBase1`1<class Generated1118.BaseClass0> on type class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1118.IBase2`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::Method7<object>()
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1> on type class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>
		callvirt instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>::ClassMethod1345<object>()
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0> on type class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>
		callvirt instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>::ClassMethod1344<object>()
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()"
		ldstr "class G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0> on type class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>
		callvirt instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>::Method6<object>()
		ldstr "G1_C11::Method6.4859<System.Object>()"
		ldstr "class G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0> on type class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>
		callvirt instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>::Method5()
		ldstr "G1_C11::Method5.4858()"
		ldstr "class G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0> on type class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>
		callvirt instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>::Method4()
		ldstr "G1_C11::Method4.4856()"
		ldstr "class G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0> on type class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>
		callvirt instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>::Method7<object>()
		ldstr "G1_C11::Method7.4854<System.Object>()"
		ldstr "class G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0> on type class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1118.IBase2`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>::Method7<object>()
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0> on type class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1118.IBase1`1<class Generated1118.BaseClass0>::Method4()
		ldstr "G1_C11::Method4.MI.4857()"
		ldstr "class IBase1`1<class Generated1118.BaseClass0> on type class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1118.IBase1`1<class Generated1118.BaseClass0>::Method5()
		ldstr "G1_C11::Method5.4858()"
		ldstr "class IBase1`1<class Generated1118.BaseClass0> on type class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1118.IBase1`1<class Generated1118.BaseClass0>::Method6<object>()
		ldstr "G1_C11::Method6.MI.4860<System.Object>()"
		ldstr "class IBase1`1<class Generated1118.BaseClass0> on type class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1118.IBase2`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>::Method7<object>()
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0> on type class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1118.IBase2`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::Method7<object>()
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1> on type class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1118.IBase2`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>::Method7<object>()
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1> on type class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>
		callvirt instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>::ClassMethod1345<object>()
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1> on type class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>
		callvirt instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>::ClassMethod1344<object>()
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()"
		ldstr "class G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1> on type class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>
		callvirt instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>::Method6<object>()
		ldstr "G1_C11::Method6.4859<System.Object>()"
		ldstr "class G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1> on type class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>
		callvirt instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>::Method5()
		ldstr "G1_C11::Method5.4858()"
		ldstr "class G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1> on type class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>
		callvirt instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>::Method4()
		ldstr "G1_C11::Method4.4856()"
		ldstr "class G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1> on type class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>
		callvirt instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>::Method7<object>()
		ldstr "G1_C11::Method7.4854<System.Object>()"
		ldstr "class G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1> on type class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1118.IBase2`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>::Method7<object>()
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1> on type class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1118.IBase1`1<class Generated1118.BaseClass0>::Method4()
		ldstr "G1_C11::Method4.MI.4857()"
		ldstr "class IBase1`1<class Generated1118.BaseClass0> on type class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1118.IBase1`1<class Generated1118.BaseClass0>::Method5()
		ldstr "G1_C11::Method5.4858()"
		ldstr "class IBase1`1<class Generated1118.BaseClass0> on type class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1118.IBase1`1<class Generated1118.BaseClass0>::Method6<object>()
		ldstr "G1_C11::Method6.MI.4860<System.Object>()"
		ldstr "class IBase1`1<class Generated1118.BaseClass0> on type class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1118.IBase2`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::Method7<object>()
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1> on type class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>"
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
		newobj instance void class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C599::ClassMethod1344.MI.10525<System.Object>()#G2_C599::ClassMethod1345.MI.10526<System.Object>()#G1_C11::Method4.4856()#G1_C11::Method5.4858()#G1_C11::Method6.4859<System.Object>()#G2_C599::Method7.10517<System.Object>()#"
		call void Generated1118.Generated1118::M.G1_C11.T.T<class Generated1118.BaseClass0,class Generated1118.BaseClass1,class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C599::ClassMethod1344.MI.10525<System.Object>()#G2_C599::ClassMethod1345.MI.10526<System.Object>()#G1_C11::Method4.4856()#G1_C11::Method5.4858()#G1_C11::Method6.4859<System.Object>()#G2_C599::Method7.10517<System.Object>()#"
		call void Generated1118.Generated1118::M.G1_C11.A.T<class Generated1118.BaseClass1,class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C599::ClassMethod1344.MI.10525<System.Object>()#G2_C599::ClassMethod1345.MI.10526<System.Object>()#G1_C11::Method4.4856()#G1_C11::Method5.4858()#G1_C11::Method6.4859<System.Object>()#G2_C599::Method7.10517<System.Object>()#"
		call void Generated1118.Generated1118::M.G1_C11.A.B<class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1118.Generated1118::M.IBase2.T.T<class Generated1118.BaseClass1,class Generated1118.BaseClass1,class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1118.Generated1118::M.IBase2.B.T<class Generated1118.BaseClass1,class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1118.Generated1118::M.IBase2.B.B<class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method4.MI.4857()#G1_C11::Method5.4858()#G1_C11::Method6.MI.4860<System.Object>()#"
		call void Generated1118.Generated1118::M.IBase1.T<class Generated1118.BaseClass0,class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method4.MI.4857()#G1_C11::Method5.4858()#G1_C11::Method6.MI.4860<System.Object>()#"
		call void Generated1118.Generated1118::M.IBase1.A<class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C599::Method7.10517<System.Object>()#"
		call void Generated1118.Generated1118::M.IBase2.T.T<class Generated1118.BaseClass0,class Generated1118.BaseClass1,class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C599::Method7.10517<System.Object>()#"
		call void Generated1118.Generated1118::M.IBase2.A.T<class Generated1118.BaseClass1,class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C599::Method7.10517<System.Object>()#"
		call void Generated1118.Generated1118::M.IBase2.A.B<class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C599::ClassMethod1344.MI.10525<System.Object>()#G2_C599::ClassMethod1345.MI.10526<System.Object>()#G3_C1590::ClassMethod2567.MI.16961<System.Object>()#G3_C1590::Method0.16953()#G3_C1590::Method1.16955()#G3_C1590::Method2.16957<System.Object>()#G3_C1590::Method3.16958<System.Object>()#G1_C11::Method4.4856()#G1_C11::Method5.4858()#G1_C11::Method6.4859<System.Object>()#G2_C599::Method7.10517<System.Object>()#"
		call void Generated1118.Generated1118::M.G2_C599.T.T<class Generated1118.BaseClass0,class Generated1118.BaseClass0,class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C599::ClassMethod1344.MI.10525<System.Object>()#G2_C599::ClassMethod1345.MI.10526<System.Object>()#G3_C1590::ClassMethod2567.MI.16961<System.Object>()#G3_C1590::Method0.16953()#G3_C1590::Method1.16955()#G3_C1590::Method2.16957<System.Object>()#G3_C1590::Method3.16958<System.Object>()#G1_C11::Method4.4856()#G1_C11::Method5.4858()#G1_C11::Method6.4859<System.Object>()#G2_C599::Method7.10517<System.Object>()#"
		call void Generated1118.Generated1118::M.G2_C599.A.T<class Generated1118.BaseClass0,class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C599::ClassMethod1344.MI.10525<System.Object>()#G2_C599::ClassMethod1345.MI.10526<System.Object>()#G3_C1590::ClassMethod2567.MI.16961<System.Object>()#G3_C1590::Method0.16953()#G3_C1590::Method1.16955()#G3_C1590::Method2.16957<System.Object>()#G3_C1590::Method3.16958<System.Object>()#G1_C11::Method4.4856()#G1_C11::Method5.4858()#G1_C11::Method6.4859<System.Object>()#G2_C599::Method7.10517<System.Object>()#"
		call void Generated1118.Generated1118::M.G2_C599.A.A<class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C599::Method7.10517<System.Object>()#"
		call void Generated1118.Generated1118::M.IBase2.T.T<class Generated1118.BaseClass0,class Generated1118.BaseClass0,class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C599::Method7.10517<System.Object>()#"
		call void Generated1118.Generated1118::M.IBase2.A.T<class Generated1118.BaseClass0,class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C599::Method7.10517<System.Object>()#"
		call void Generated1118.Generated1118::M.IBase2.A.A<class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1590::Method0.MI.16954()#G3_C1590::Method1.MI.16956()#G3_C1590::Method2.16957<System.Object>()#G3_C1590::Method3.MI.16959<System.Object>()#"
		call void Generated1118.Generated1118::M.IBase0<class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C599::ClassMethod1344.MI.10525<System.Object>()#G2_C599::ClassMethod1345.MI.10526<System.Object>()#G3_C1590::ClassMethod2567.MI.16961<System.Object>()#G3_C1590::ClassMethod4568.16960()#G3_C1590::Method0.16953()#G3_C1590::Method1.16955()#G3_C1590::Method2.16957<System.Object>()#G3_C1590::Method3.16958<System.Object>()#G1_C11::Method4.4856()#G1_C11::Method5.4858()#G1_C11::Method6.4859<System.Object>()#G2_C599::Method7.10517<System.Object>()#"
		call void Generated1118.Generated1118::M.G3_C1590.T<class Generated1118.BaseClass0,class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C599::ClassMethod1344.MI.10525<System.Object>()#G2_C599::ClassMethod1345.MI.10526<System.Object>()#G3_C1590::ClassMethod2567.MI.16961<System.Object>()#G3_C1590::ClassMethod4568.16960()#G3_C1590::Method0.16953()#G3_C1590::Method1.16955()#G3_C1590::Method2.16957<System.Object>()#G3_C1590::Method3.16958<System.Object>()#G1_C11::Method4.4856()#G1_C11::Method5.4858()#G1_C11::Method6.4859<System.Object>()#G2_C599::Method7.10517<System.Object>()#"
		call void Generated1118.Generated1118::M.G3_C1590.A<class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>>(!!0,string)
		newobj instance void class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C599::ClassMethod1344.MI.10525<System.Object>()#G2_C599::ClassMethod1345.MI.10526<System.Object>()#G1_C11::Method4.4856()#G1_C11::Method5.4858()#G1_C11::Method6.4859<System.Object>()#G2_C599::Method7.10517<System.Object>()#"
		call void Generated1118.Generated1118::M.G1_C11.T.T<class Generated1118.BaseClass0,class Generated1118.BaseClass1,class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C599::ClassMethod1344.MI.10525<System.Object>()#G2_C599::ClassMethod1345.MI.10526<System.Object>()#G1_C11::Method4.4856()#G1_C11::Method5.4858()#G1_C11::Method6.4859<System.Object>()#G2_C599::Method7.10517<System.Object>()#"
		call void Generated1118.Generated1118::M.G1_C11.A.T<class Generated1118.BaseClass1,class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C599::ClassMethod1344.MI.10525<System.Object>()#G2_C599::ClassMethod1345.MI.10526<System.Object>()#G1_C11::Method4.4856()#G1_C11::Method5.4858()#G1_C11::Method6.4859<System.Object>()#G2_C599::Method7.10517<System.Object>()#"
		call void Generated1118.Generated1118::M.G1_C11.A.B<class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1118.Generated1118::M.IBase2.T.T<class Generated1118.BaseClass1,class Generated1118.BaseClass1,class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1118.Generated1118::M.IBase2.B.T<class Generated1118.BaseClass1,class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1118.Generated1118::M.IBase2.B.B<class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method4.MI.4857()#G1_C11::Method5.4858()#G1_C11::Method6.MI.4860<System.Object>()#"
		call void Generated1118.Generated1118::M.IBase1.T<class Generated1118.BaseClass0,class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method4.MI.4857()#G1_C11::Method5.4858()#G1_C11::Method6.MI.4860<System.Object>()#"
		call void Generated1118.Generated1118::M.IBase1.A<class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C599::Method7.10517<System.Object>()#"
		call void Generated1118.Generated1118::M.IBase2.T.T<class Generated1118.BaseClass0,class Generated1118.BaseClass1,class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C599::Method7.10517<System.Object>()#"
		call void Generated1118.Generated1118::M.IBase2.A.T<class Generated1118.BaseClass1,class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C599::Method7.10517<System.Object>()#"
		call void Generated1118.Generated1118::M.IBase2.A.B<class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C599::ClassMethod1344.MI.10525<System.Object>()#G2_C599::ClassMethod1345.MI.10526<System.Object>()#G3_C1590::ClassMethod2567.MI.16961<System.Object>()#G3_C1590::Method0.16953()#G3_C1590::Method1.16955()#G3_C1590::Method2.16957<System.Object>()#G3_C1590::Method3.16958<System.Object>()#G1_C11::Method4.4856()#G1_C11::Method5.4858()#G1_C11::Method6.4859<System.Object>()#G2_C599::Method7.10517<System.Object>()#"
		call void Generated1118.Generated1118::M.G2_C599.T.T<class Generated1118.BaseClass0,class Generated1118.BaseClass1,class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C599::ClassMethod1344.MI.10525<System.Object>()#G2_C599::ClassMethod1345.MI.10526<System.Object>()#G3_C1590::ClassMethod2567.MI.16961<System.Object>()#G3_C1590::Method0.16953()#G3_C1590::Method1.16955()#G3_C1590::Method2.16957<System.Object>()#G3_C1590::Method3.16958<System.Object>()#G1_C11::Method4.4856()#G1_C11::Method5.4858()#G1_C11::Method6.4859<System.Object>()#G2_C599::Method7.10517<System.Object>()#"
		call void Generated1118.Generated1118::M.G2_C599.A.T<class Generated1118.BaseClass1,class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C599::ClassMethod1344.MI.10525<System.Object>()#G2_C599::ClassMethod1345.MI.10526<System.Object>()#G3_C1590::ClassMethod2567.MI.16961<System.Object>()#G3_C1590::Method0.16953()#G3_C1590::Method1.16955()#G3_C1590::Method2.16957<System.Object>()#G3_C1590::Method3.16958<System.Object>()#G1_C11::Method4.4856()#G1_C11::Method5.4858()#G1_C11::Method6.4859<System.Object>()#G2_C599::Method7.10517<System.Object>()#"
		call void Generated1118.Generated1118::M.G2_C599.A.B<class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1590::Method0.MI.16954()#G3_C1590::Method1.MI.16956()#G3_C1590::Method2.16957<System.Object>()#G3_C1590::Method3.MI.16959<System.Object>()#"
		call void Generated1118.Generated1118::M.IBase0<class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C599::ClassMethod1344.MI.10525<System.Object>()#G2_C599::ClassMethod1345.MI.10526<System.Object>()#G3_C1590::ClassMethod2567.MI.16961<System.Object>()#G3_C1590::ClassMethod4568.16960()#G3_C1590::Method0.16953()#G3_C1590::Method1.16955()#G3_C1590::Method2.16957<System.Object>()#G3_C1590::Method3.16958<System.Object>()#G1_C11::Method4.4856()#G1_C11::Method5.4858()#G1_C11::Method6.4859<System.Object>()#G2_C599::Method7.10517<System.Object>()#"
		call void Generated1118.Generated1118::M.G3_C1590.T<class Generated1118.BaseClass1,class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C599::ClassMethod1344.MI.10525<System.Object>()#G2_C599::ClassMethod1345.MI.10526<System.Object>()#G3_C1590::ClassMethod2567.MI.16961<System.Object>()#G3_C1590::ClassMethod4568.16960()#G3_C1590::Method0.16953()#G3_C1590::Method1.16955()#G3_C1590::Method2.16957<System.Object>()#G3_C1590::Method3.16958<System.Object>()#G1_C11::Method4.4856()#G1_C11::Method5.4858()#G1_C11::Method6.4859<System.Object>()#G2_C599::Method7.10517<System.Object>()#"
		call void Generated1118.Generated1118::M.G3_C1590.B<class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>>(!!0,string)
		newobj instance void class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C599::ClassMethod1344.MI.10525<System.Object>()#G2_C599::ClassMethod1345.MI.10526<System.Object>()#G1_C11::Method4.4856()#G1_C11::Method5.4858()#G1_C11::Method6.4859<System.Object>()#G2_C599::Method7.10517<System.Object>()#"
		call void Generated1118.Generated1118::M.G1_C11.T.T<class Generated1118.BaseClass0,class Generated1118.BaseClass1,class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C599::ClassMethod1344.MI.10525<System.Object>()#G2_C599::ClassMethod1345.MI.10526<System.Object>()#G1_C11::Method4.4856()#G1_C11::Method5.4858()#G1_C11::Method6.4859<System.Object>()#G2_C599::Method7.10517<System.Object>()#"
		call void Generated1118.Generated1118::M.G1_C11.A.T<class Generated1118.BaseClass1,class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C599::ClassMethod1344.MI.10525<System.Object>()#G2_C599::ClassMethod1345.MI.10526<System.Object>()#G1_C11::Method4.4856()#G1_C11::Method5.4858()#G1_C11::Method6.4859<System.Object>()#G2_C599::Method7.10517<System.Object>()#"
		call void Generated1118.Generated1118::M.G1_C11.A.B<class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1118.Generated1118::M.IBase2.T.T<class Generated1118.BaseClass1,class Generated1118.BaseClass1,class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1118.Generated1118::M.IBase2.B.T<class Generated1118.BaseClass1,class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1118.Generated1118::M.IBase2.B.B<class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method4.MI.4857()#G1_C11::Method5.4858()#G1_C11::Method6.MI.4860<System.Object>()#"
		call void Generated1118.Generated1118::M.IBase1.T<class Generated1118.BaseClass0,class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method4.MI.4857()#G1_C11::Method5.4858()#G1_C11::Method6.MI.4860<System.Object>()#"
		call void Generated1118.Generated1118::M.IBase1.A<class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C599::Method7.10517<System.Object>()#"
		call void Generated1118.Generated1118::M.IBase2.T.T<class Generated1118.BaseClass0,class Generated1118.BaseClass1,class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C599::Method7.10517<System.Object>()#"
		call void Generated1118.Generated1118::M.IBase2.A.T<class Generated1118.BaseClass1,class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C599::Method7.10517<System.Object>()#"
		call void Generated1118.Generated1118::M.IBase2.A.B<class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C599::ClassMethod1344.MI.10525<System.Object>()#G2_C599::ClassMethod1345.MI.10526<System.Object>()#G2_C599::ClassMethod2567.10524<System.Object>()#G2_C599::Method0.10518()#G2_C599::Method1.10520()#G2_C599::Method2.10521<System.Object>()#G2_C599::Method3.10522<System.Object>()#G1_C11::Method4.4856()#G1_C11::Method5.4858()#G1_C11::Method6.4859<System.Object>()#G2_C599::Method7.10517<System.Object>()#"
		call void Generated1118.Generated1118::M.G2_C599.T.T<class Generated1118.BaseClass0,class Generated1118.BaseClass0,class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C599::ClassMethod1344.MI.10525<System.Object>()#G2_C599::ClassMethod1345.MI.10526<System.Object>()#G2_C599::ClassMethod2567.10524<System.Object>()#G2_C599::Method0.10518()#G2_C599::Method1.10520()#G2_C599::Method2.10521<System.Object>()#G2_C599::Method3.10522<System.Object>()#G1_C11::Method4.4856()#G1_C11::Method5.4858()#G1_C11::Method6.4859<System.Object>()#G2_C599::Method7.10517<System.Object>()#"
		call void Generated1118.Generated1118::M.G2_C599.A.T<class Generated1118.BaseClass0,class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C599::ClassMethod1344.MI.10525<System.Object>()#G2_C599::ClassMethod1345.MI.10526<System.Object>()#G2_C599::ClassMethod2567.10524<System.Object>()#G2_C599::Method0.10518()#G2_C599::Method1.10520()#G2_C599::Method2.10521<System.Object>()#G2_C599::Method3.10522<System.Object>()#G1_C11::Method4.4856()#G1_C11::Method5.4858()#G1_C11::Method6.4859<System.Object>()#G2_C599::Method7.10517<System.Object>()#"
		call void Generated1118.Generated1118::M.G2_C599.A.A<class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C599::Method7.10517<System.Object>()#"
		call void Generated1118.Generated1118::M.IBase2.T.T<class Generated1118.BaseClass0,class Generated1118.BaseClass0,class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C599::Method7.10517<System.Object>()#"
		call void Generated1118.Generated1118::M.IBase2.A.T<class Generated1118.BaseClass0,class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C599::Method7.10517<System.Object>()#"
		call void Generated1118.Generated1118::M.IBase2.A.A<class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C599::Method0.MI.10519()#G2_C599::Method1.10520()#G2_C599::Method2.10521<System.Object>()#G2_C599::Method3.MI.10523<System.Object>()#"
		call void Generated1118.Generated1118::M.IBase0<class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>>(!!0,string)
		newobj instance void class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C599::ClassMethod1344.MI.10525<System.Object>()#G2_C599::ClassMethod1345.MI.10526<System.Object>()#G1_C11::Method4.4856()#G1_C11::Method5.4858()#G1_C11::Method6.4859<System.Object>()#G2_C599::Method7.10517<System.Object>()#"
		call void Generated1118.Generated1118::M.G1_C11.T.T<class Generated1118.BaseClass0,class Generated1118.BaseClass1,class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C599::ClassMethod1344.MI.10525<System.Object>()#G2_C599::ClassMethod1345.MI.10526<System.Object>()#G1_C11::Method4.4856()#G1_C11::Method5.4858()#G1_C11::Method6.4859<System.Object>()#G2_C599::Method7.10517<System.Object>()#"
		call void Generated1118.Generated1118::M.G1_C11.A.T<class Generated1118.BaseClass1,class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C599::ClassMethod1344.MI.10525<System.Object>()#G2_C599::ClassMethod1345.MI.10526<System.Object>()#G1_C11::Method4.4856()#G1_C11::Method5.4858()#G1_C11::Method6.4859<System.Object>()#G2_C599::Method7.10517<System.Object>()#"
		call void Generated1118.Generated1118::M.G1_C11.A.B<class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1118.Generated1118::M.IBase2.T.T<class Generated1118.BaseClass1,class Generated1118.BaseClass1,class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1118.Generated1118::M.IBase2.B.T<class Generated1118.BaseClass1,class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1118.Generated1118::M.IBase2.B.B<class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method4.MI.4857()#G1_C11::Method5.4858()#G1_C11::Method6.MI.4860<System.Object>()#"
		call void Generated1118.Generated1118::M.IBase1.T<class Generated1118.BaseClass0,class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method4.MI.4857()#G1_C11::Method5.4858()#G1_C11::Method6.MI.4860<System.Object>()#"
		call void Generated1118.Generated1118::M.IBase1.A<class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C599::Method7.10517<System.Object>()#"
		call void Generated1118.Generated1118::M.IBase2.T.T<class Generated1118.BaseClass0,class Generated1118.BaseClass1,class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C599::Method7.10517<System.Object>()#"
		call void Generated1118.Generated1118::M.IBase2.A.T<class Generated1118.BaseClass1,class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C599::Method7.10517<System.Object>()#"
		call void Generated1118.Generated1118::M.IBase2.A.B<class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C599::ClassMethod1344.MI.10525<System.Object>()#G2_C599::ClassMethod1345.MI.10526<System.Object>()#G2_C599::ClassMethod2567.10524<System.Object>()#G2_C599::Method0.10518()#G2_C599::Method1.10520()#G2_C599::Method2.10521<System.Object>()#G2_C599::Method3.10522<System.Object>()#G1_C11::Method4.4856()#G1_C11::Method5.4858()#G1_C11::Method6.4859<System.Object>()#G2_C599::Method7.10517<System.Object>()#"
		call void Generated1118.Generated1118::M.G2_C599.T.T<class Generated1118.BaseClass0,class Generated1118.BaseClass1,class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C599::ClassMethod1344.MI.10525<System.Object>()#G2_C599::ClassMethod1345.MI.10526<System.Object>()#G2_C599::ClassMethod2567.10524<System.Object>()#G2_C599::Method0.10518()#G2_C599::Method1.10520()#G2_C599::Method2.10521<System.Object>()#G2_C599::Method3.10522<System.Object>()#G1_C11::Method4.4856()#G1_C11::Method5.4858()#G1_C11::Method6.4859<System.Object>()#G2_C599::Method7.10517<System.Object>()#"
		call void Generated1118.Generated1118::M.G2_C599.A.T<class Generated1118.BaseClass1,class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C599::ClassMethod1344.MI.10525<System.Object>()#G2_C599::ClassMethod1345.MI.10526<System.Object>()#G2_C599::ClassMethod2567.10524<System.Object>()#G2_C599::Method0.10518()#G2_C599::Method1.10520()#G2_C599::Method2.10521<System.Object>()#G2_C599::Method3.10522<System.Object>()#G1_C11::Method4.4856()#G1_C11::Method5.4858()#G1_C11::Method6.4859<System.Object>()#G2_C599::Method7.10517<System.Object>()#"
		call void Generated1118.Generated1118::M.G2_C599.A.B<class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C599::Method0.MI.10519()#G2_C599::Method1.10520()#G2_C599::Method2.10521<System.Object>()#G2_C599::Method3.MI.10523<System.Object>()#"
		call void Generated1118.Generated1118::M.IBase0<class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>>(!!0,string)
		newobj instance void class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C599::ClassMethod1344.MI.10525<System.Object>()#G2_C599::ClassMethod1345.MI.10526<System.Object>()#G1_C11::Method4.4856()#G1_C11::Method5.4858()#G1_C11::Method6.4859<System.Object>()#G2_C599::Method7.10517<System.Object>()#"
		call void Generated1118.Generated1118::M.G1_C11.T.T<class Generated1118.BaseClass1,class Generated1118.BaseClass1,class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C599::ClassMethod1344.MI.10525<System.Object>()#G2_C599::ClassMethod1345.MI.10526<System.Object>()#G1_C11::Method4.4856()#G1_C11::Method5.4858()#G1_C11::Method6.4859<System.Object>()#G2_C599::Method7.10517<System.Object>()#"
		call void Generated1118.Generated1118::M.G1_C11.B.T<class Generated1118.BaseClass1,class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C599::ClassMethod1344.MI.10525<System.Object>()#G2_C599::ClassMethod1345.MI.10526<System.Object>()#G1_C11::Method4.4856()#G1_C11::Method5.4858()#G1_C11::Method6.4859<System.Object>()#G2_C599::Method7.10517<System.Object>()#"
		call void Generated1118.Generated1118::M.G1_C11.B.B<class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C599::Method7.10517<System.Object>()#"
		call void Generated1118.Generated1118::M.IBase2.T.T<class Generated1118.BaseClass1,class Generated1118.BaseClass1,class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C599::Method7.10517<System.Object>()#"
		call void Generated1118.Generated1118::M.IBase2.B.T<class Generated1118.BaseClass1,class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C599::Method7.10517<System.Object>()#"
		call void Generated1118.Generated1118::M.IBase2.B.B<class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method4.MI.4857()#G1_C11::Method5.4858()#G1_C11::Method6.MI.4860<System.Object>()#"
		call void Generated1118.Generated1118::M.IBase1.T<class Generated1118.BaseClass0,class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method4.MI.4857()#G1_C11::Method5.4858()#G1_C11::Method6.MI.4860<System.Object>()#"
		call void Generated1118.Generated1118::M.IBase1.A<class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C599::Method7.10517<System.Object>()#"
		call void Generated1118.Generated1118::M.IBase2.T.T<class Generated1118.BaseClass0,class Generated1118.BaseClass1,class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C599::Method7.10517<System.Object>()#"
		call void Generated1118.Generated1118::M.IBase2.A.T<class Generated1118.BaseClass1,class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C599::Method7.10517<System.Object>()#"
		call void Generated1118.Generated1118::M.IBase2.A.B<class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C599::ClassMethod1344.MI.10525<System.Object>()#G2_C599::ClassMethod1345.MI.10526<System.Object>()#G2_C599::ClassMethod2567.10524<System.Object>()#G2_C599::Method0.10518()#G2_C599::Method1.10520()#G2_C599::Method2.10521<System.Object>()#G2_C599::Method3.10522<System.Object>()#G1_C11::Method4.4856()#G1_C11::Method5.4858()#G1_C11::Method6.4859<System.Object>()#G2_C599::Method7.10517<System.Object>()#"
		call void Generated1118.Generated1118::M.G2_C599.T.T<class Generated1118.BaseClass1,class Generated1118.BaseClass0,class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C599::ClassMethod1344.MI.10525<System.Object>()#G2_C599::ClassMethod1345.MI.10526<System.Object>()#G2_C599::ClassMethod2567.10524<System.Object>()#G2_C599::Method0.10518()#G2_C599::Method1.10520()#G2_C599::Method2.10521<System.Object>()#G2_C599::Method3.10522<System.Object>()#G1_C11::Method4.4856()#G1_C11::Method5.4858()#G1_C11::Method6.4859<System.Object>()#G2_C599::Method7.10517<System.Object>()#"
		call void Generated1118.Generated1118::M.G2_C599.B.T<class Generated1118.BaseClass0,class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C599::ClassMethod1344.MI.10525<System.Object>()#G2_C599::ClassMethod1345.MI.10526<System.Object>()#G2_C599::ClassMethod2567.10524<System.Object>()#G2_C599::Method0.10518()#G2_C599::Method1.10520()#G2_C599::Method2.10521<System.Object>()#G2_C599::Method3.10522<System.Object>()#G1_C11::Method4.4856()#G1_C11::Method5.4858()#G1_C11::Method6.4859<System.Object>()#G2_C599::Method7.10517<System.Object>()#"
		call void Generated1118.Generated1118::M.G2_C599.B.A<class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C599::Method7.10517<System.Object>()#"
		call void Generated1118.Generated1118::M.IBase2.T.T<class Generated1118.BaseClass1,class Generated1118.BaseClass0,class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C599::Method7.10517<System.Object>()#"
		call void Generated1118.Generated1118::M.IBase2.B.T<class Generated1118.BaseClass0,class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C599::Method7.10517<System.Object>()#"
		call void Generated1118.Generated1118::M.IBase2.B.A<class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C599::Method0.MI.10519()#G2_C599::Method1.10520()#G2_C599::Method2.10521<System.Object>()#G2_C599::Method3.MI.10523<System.Object>()#"
		call void Generated1118.Generated1118::M.IBase0<class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C599::Method7.10517<System.Object>()#"
		call void Generated1118.Generated1118::M.IBase2.T.T<class Generated1118.BaseClass0,class Generated1118.BaseClass0,class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C599::Method7.10517<System.Object>()#"
		call void Generated1118.Generated1118::M.IBase2.A.T<class Generated1118.BaseClass0,class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C599::Method7.10517<System.Object>()#"
		call void Generated1118.Generated1118::M.IBase2.A.A<class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>>(!!0,string)
		newobj instance void class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C599::ClassMethod1344.MI.10525<System.Object>()#G2_C599::ClassMethod1345.MI.10526<System.Object>()#G1_C11::Method4.4856()#G1_C11::Method5.4858()#G1_C11::Method6.4859<System.Object>()#G2_C599::Method7.10517<System.Object>()#"
		call void Generated1118.Generated1118::M.G1_C11.T.T<class Generated1118.BaseClass1,class Generated1118.BaseClass1,class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C599::ClassMethod1344.MI.10525<System.Object>()#G2_C599::ClassMethod1345.MI.10526<System.Object>()#G1_C11::Method4.4856()#G1_C11::Method5.4858()#G1_C11::Method6.4859<System.Object>()#G2_C599::Method7.10517<System.Object>()#"
		call void Generated1118.Generated1118::M.G1_C11.B.T<class Generated1118.BaseClass1,class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C599::ClassMethod1344.MI.10525<System.Object>()#G2_C599::ClassMethod1345.MI.10526<System.Object>()#G1_C11::Method4.4856()#G1_C11::Method5.4858()#G1_C11::Method6.4859<System.Object>()#G2_C599::Method7.10517<System.Object>()#"
		call void Generated1118.Generated1118::M.G1_C11.B.B<class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C599::Method7.10517<System.Object>()#"
		call void Generated1118.Generated1118::M.IBase2.T.T<class Generated1118.BaseClass1,class Generated1118.BaseClass1,class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C599::Method7.10517<System.Object>()#"
		call void Generated1118.Generated1118::M.IBase2.B.T<class Generated1118.BaseClass1,class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C599::Method7.10517<System.Object>()#"
		call void Generated1118.Generated1118::M.IBase2.B.B<class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method4.MI.4857()#G1_C11::Method5.4858()#G1_C11::Method6.MI.4860<System.Object>()#"
		call void Generated1118.Generated1118::M.IBase1.T<class Generated1118.BaseClass0,class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method4.MI.4857()#G1_C11::Method5.4858()#G1_C11::Method6.MI.4860<System.Object>()#"
		call void Generated1118.Generated1118::M.IBase1.A<class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C599::Method7.10517<System.Object>()#"
		call void Generated1118.Generated1118::M.IBase2.T.T<class Generated1118.BaseClass0,class Generated1118.BaseClass1,class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C599::Method7.10517<System.Object>()#"
		call void Generated1118.Generated1118::M.IBase2.A.T<class Generated1118.BaseClass1,class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C599::Method7.10517<System.Object>()#"
		call void Generated1118.Generated1118::M.IBase2.A.B<class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C599::ClassMethod1344.MI.10525<System.Object>()#G2_C599::ClassMethod1345.MI.10526<System.Object>()#G2_C599::ClassMethod2567.10524<System.Object>()#G2_C599::Method0.10518()#G2_C599::Method1.10520()#G2_C599::Method2.10521<System.Object>()#G2_C599::Method3.10522<System.Object>()#G1_C11::Method4.4856()#G1_C11::Method5.4858()#G1_C11::Method6.4859<System.Object>()#G2_C599::Method7.10517<System.Object>()#"
		call void Generated1118.Generated1118::M.G2_C599.T.T<class Generated1118.BaseClass1,class Generated1118.BaseClass1,class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C599::ClassMethod1344.MI.10525<System.Object>()#G2_C599::ClassMethod1345.MI.10526<System.Object>()#G2_C599::ClassMethod2567.10524<System.Object>()#G2_C599::Method0.10518()#G2_C599::Method1.10520()#G2_C599::Method2.10521<System.Object>()#G2_C599::Method3.10522<System.Object>()#G1_C11::Method4.4856()#G1_C11::Method5.4858()#G1_C11::Method6.4859<System.Object>()#G2_C599::Method7.10517<System.Object>()#"
		call void Generated1118.Generated1118::M.G2_C599.B.T<class Generated1118.BaseClass1,class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C599::ClassMethod1344.MI.10525<System.Object>()#G2_C599::ClassMethod1345.MI.10526<System.Object>()#G2_C599::ClassMethod2567.10524<System.Object>()#G2_C599::Method0.10518()#G2_C599::Method1.10520()#G2_C599::Method2.10521<System.Object>()#G2_C599::Method3.10522<System.Object>()#G1_C11::Method4.4856()#G1_C11::Method5.4858()#G1_C11::Method6.4859<System.Object>()#G2_C599::Method7.10517<System.Object>()#"
		call void Generated1118.Generated1118::M.G2_C599.B.B<class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C599::Method0.MI.10519()#G2_C599::Method1.10520()#G2_C599::Method2.10521<System.Object>()#G2_C599::Method3.MI.10523<System.Object>()#"
		call void Generated1118.Generated1118::M.IBase0<class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>>(!!0,string)
		newobj instance void class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G1_C11::Method4.4856()#G1_C11::Method5.4858()#G1_C11::Method6.4859<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Generated1118.Generated1118::M.G1_C11.T.T<class Generated1118.BaseClass0,class Generated1118.BaseClass0,class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G1_C11::Method4.4856()#G1_C11::Method5.4858()#G1_C11::Method6.4859<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Generated1118.Generated1118::M.G1_C11.A.T<class Generated1118.BaseClass0,class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G1_C11::Method4.4856()#G1_C11::Method5.4858()#G1_C11::Method6.4859<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Generated1118.Generated1118::M.G1_C11.A.A<class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1118.Generated1118::M.IBase2.T.T<class Generated1118.BaseClass1,class Generated1118.BaseClass0,class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1118.Generated1118::M.IBase2.B.T<class Generated1118.BaseClass0,class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1118.Generated1118::M.IBase2.B.A<class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method4.MI.4857()#G1_C11::Method5.4858()#G1_C11::Method6.MI.4860<System.Object>()#"
		call void Generated1118.Generated1118::M.IBase1.T<class Generated1118.BaseClass0,class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method4.MI.4857()#G1_C11::Method5.4858()#G1_C11::Method6.MI.4860<System.Object>()#"
		call void Generated1118.Generated1118::M.IBase1.A<class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1118.Generated1118::M.IBase2.T.T<class Generated1118.BaseClass0,class Generated1118.BaseClass0,class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1118.Generated1118::M.IBase2.A.T<class Generated1118.BaseClass0,class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1118.Generated1118::M.IBase2.A.A<class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1118.Generated1118::M.IBase2.T.T<class Generated1118.BaseClass0,class Generated1118.BaseClass1,class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1118.Generated1118::M.IBase2.A.T<class Generated1118.BaseClass1,class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1118.Generated1118::M.IBase2.A.B<class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1118.Generated1118::M.IBase2.T.T<class Generated1118.BaseClass1,class Generated1118.BaseClass1,class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1118.Generated1118::M.IBase2.B.T<class Generated1118.BaseClass1,class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1118.Generated1118::M.IBase2.B.B<class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>>(!!0,string)
		newobj instance void class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G1_C11::Method4.4856()#G1_C11::Method5.4858()#G1_C11::Method6.4859<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Generated1118.Generated1118::M.G1_C11.T.T<class Generated1118.BaseClass0,class Generated1118.BaseClass1,class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G1_C11::Method4.4856()#G1_C11::Method5.4858()#G1_C11::Method6.4859<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Generated1118.Generated1118::M.G1_C11.A.T<class Generated1118.BaseClass1,class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G1_C11::Method4.4856()#G1_C11::Method5.4858()#G1_C11::Method6.4859<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Generated1118.Generated1118::M.G1_C11.A.B<class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1118.Generated1118::M.IBase2.T.T<class Generated1118.BaseClass1,class Generated1118.BaseClass1,class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1118.Generated1118::M.IBase2.B.T<class Generated1118.BaseClass1,class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1118.Generated1118::M.IBase2.B.B<class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method4.MI.4857()#G1_C11::Method5.4858()#G1_C11::Method6.MI.4860<System.Object>()#"
		call void Generated1118.Generated1118::M.IBase1.T<class Generated1118.BaseClass0,class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method4.MI.4857()#G1_C11::Method5.4858()#G1_C11::Method6.MI.4860<System.Object>()#"
		call void Generated1118.Generated1118::M.IBase1.A<class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1118.Generated1118::M.IBase2.T.T<class Generated1118.BaseClass0,class Generated1118.BaseClass1,class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1118.Generated1118::M.IBase2.A.T<class Generated1118.BaseClass1,class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1118.Generated1118::M.IBase2.A.B<class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>>(!!0,string)
		newobj instance void class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G1_C11::Method4.4856()#G1_C11::Method5.4858()#G1_C11::Method6.4859<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Generated1118.Generated1118::M.G1_C11.T.T<class Generated1118.BaseClass1,class Generated1118.BaseClass0,class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G1_C11::Method4.4856()#G1_C11::Method5.4858()#G1_C11::Method6.4859<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Generated1118.Generated1118::M.G1_C11.B.T<class Generated1118.BaseClass0,class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G1_C11::Method4.4856()#G1_C11::Method5.4858()#G1_C11::Method6.4859<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Generated1118.Generated1118::M.G1_C11.B.A<class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1118.Generated1118::M.IBase2.T.T<class Generated1118.BaseClass1,class Generated1118.BaseClass0,class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1118.Generated1118::M.IBase2.B.T<class Generated1118.BaseClass0,class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1118.Generated1118::M.IBase2.B.A<class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method4.MI.4857()#G1_C11::Method5.4858()#G1_C11::Method6.MI.4860<System.Object>()#"
		call void Generated1118.Generated1118::M.IBase1.T<class Generated1118.BaseClass0,class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method4.MI.4857()#G1_C11::Method5.4858()#G1_C11::Method6.MI.4860<System.Object>()#"
		call void Generated1118.Generated1118::M.IBase1.A<class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1118.Generated1118::M.IBase2.T.T<class Generated1118.BaseClass0,class Generated1118.BaseClass0,class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1118.Generated1118::M.IBase2.A.T<class Generated1118.BaseClass0,class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1118.Generated1118::M.IBase2.A.A<class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1118.Generated1118::M.IBase2.T.T<class Generated1118.BaseClass0,class Generated1118.BaseClass1,class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1118.Generated1118::M.IBase2.A.T<class Generated1118.BaseClass1,class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1118.Generated1118::M.IBase2.A.B<class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1118.Generated1118::M.IBase2.T.T<class Generated1118.BaseClass1,class Generated1118.BaseClass1,class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1118.Generated1118::M.IBase2.B.T<class Generated1118.BaseClass1,class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1118.Generated1118::M.IBase2.B.B<class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>>(!!0,string)
		newobj instance void class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G1_C11::Method4.4856()#G1_C11::Method5.4858()#G1_C11::Method6.4859<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Generated1118.Generated1118::M.G1_C11.T.T<class Generated1118.BaseClass1,class Generated1118.BaseClass1,class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G1_C11::Method4.4856()#G1_C11::Method5.4858()#G1_C11::Method6.4859<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Generated1118.Generated1118::M.G1_C11.B.T<class Generated1118.BaseClass1,class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G1_C11::Method4.4856()#G1_C11::Method5.4858()#G1_C11::Method6.4859<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Generated1118.Generated1118::M.G1_C11.B.B<class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1118.Generated1118::M.IBase2.T.T<class Generated1118.BaseClass1,class Generated1118.BaseClass1,class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1118.Generated1118::M.IBase2.B.T<class Generated1118.BaseClass1,class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1118.Generated1118::M.IBase2.B.B<class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method4.MI.4857()#G1_C11::Method5.4858()#G1_C11::Method6.MI.4860<System.Object>()#"
		call void Generated1118.Generated1118::M.IBase1.T<class Generated1118.BaseClass0,class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method4.MI.4857()#G1_C11::Method5.4858()#G1_C11::Method6.MI.4860<System.Object>()#"
		call void Generated1118.Generated1118::M.IBase1.A<class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1118.Generated1118::M.IBase2.T.T<class Generated1118.BaseClass0,class Generated1118.BaseClass1,class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1118.Generated1118::M.IBase2.A.T<class Generated1118.BaseClass1,class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1118.Generated1118::M.IBase2.A.B<class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>>(!!0,string)
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
		newobj instance void class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::ClassMethod1345<object>()
		calli default string(class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>)
		ldstr "G2_C599::ClassMethod1345.MI.10526<System.Object>()"
		ldstr "class G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::ClassMethod1344<object>()
		calli default string(class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>)
		ldstr "G2_C599::ClassMethod1344.MI.10525<System.Object>()"
		ldstr "class G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::Method6<object>()
		calli default string(class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>)
		ldstr "G1_C11::Method6.4859<System.Object>()"
		ldstr "class G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::Method5()
		calli default string(class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>)
		ldstr "G1_C11::Method5.4858()"
		ldstr "class G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::Method4()
		calli default string(class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>)
		ldstr "G1_C11::Method4.4856()"
		ldstr "class G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::Method7<object>()
		calli default string(class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>)
		ldstr "G2_C599::Method7.10517<System.Object>()"
		ldstr "class G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1118.IBase2`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>::Method7<object>()
		calli default string(class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>)
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1118.IBase1`1<class Generated1118.BaseClass0>::Method4()
		calli default string(class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>)
		ldstr "G1_C11::Method4.MI.4857()"
		ldstr "class IBase1`1<class Generated1118.BaseClass0> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1118.IBase1`1<class Generated1118.BaseClass0>::Method5()
		calli default string(class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>)
		ldstr "G1_C11::Method5.4858()"
		ldstr "class IBase1`1<class Generated1118.BaseClass0> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1118.IBase1`1<class Generated1118.BaseClass0>::Method6<object>()
		calli default string(class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>)
		ldstr "G1_C11::Method6.MI.4860<System.Object>()"
		ldstr "class IBase1`1<class Generated1118.BaseClass0> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1118.IBase2`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::Method7<object>()
		calli default string(class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>)
		ldstr "G2_C599::Method7.10517<System.Object>()"
		ldstr "class IBase2`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>::ClassMethod2567<object>()
		calli default string(class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>)
		ldstr "G3_C1590::ClassMethod2567.MI.16961<System.Object>()"
		ldstr "class G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>::Method3<object>()
		calli default string(class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>)
		ldstr "G3_C1590::Method3.16958<System.Object>()"
		ldstr "class G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>::Method2<object>()
		calli default string(class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>)
		ldstr "G3_C1590::Method2.16957<System.Object>()"
		ldstr "class G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>::Method1()
		calli default string(class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>)
		ldstr "G3_C1590::Method1.16955()"
		ldstr "class G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>::Method0()
		calli default string(class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>)
		ldstr "G3_C1590::Method0.16953()"
		ldstr "class G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>::Method7<object>()
		calli default string(class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>)
		ldstr "G2_C599::Method7.10517<System.Object>()"
		ldstr "class G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>::ClassMethod1345<object>()
		calli default string(class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>)
		ldstr "G2_C599::ClassMethod1345.MI.10526<System.Object>()"
		ldstr "class G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>::ClassMethod1344<object>()
		calli default string(class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>)
		ldstr "G2_C599::ClassMethod1344.MI.10525<System.Object>()"
		ldstr "class G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>::Method6<object>()
		calli default string(class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>)
		ldstr "G1_C11::Method6.4859<System.Object>()"
		ldstr "class G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>::Method5()
		calli default string(class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>)
		ldstr "G1_C11::Method5.4858()"
		ldstr "class G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>::Method4()
		calli default string(class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>)
		ldstr "G1_C11::Method4.4856()"
		ldstr "class G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1118.IBase2`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>::Method7<object>()
		calli default string(class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>)
		ldstr "G2_C599::Method7.10517<System.Object>()"
		ldstr "class IBase2`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated1118.IBase0::Method0()
		calli default string(class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>)
		ldstr "G3_C1590::Method0.MI.16954()"
		ldstr "IBase0 on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated1118.IBase0::Method1()
		calli default string(class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>)
		ldstr "G3_C1590::Method1.MI.16956()"
		ldstr "IBase0 on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated1118.IBase0::Method2<object>()
		calli default string(class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>)
		ldstr "G3_C1590::Method2.16957<System.Object>()"
		ldstr "IBase0 on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated1118.IBase0::Method3<object>()
		calli default string(class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>)
		ldstr "G3_C1590::Method3.MI.16959<System.Object>()"
		ldstr "IBase0 on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>::ClassMethod4568()
		calli default string(class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>)
		ldstr "G3_C1590::ClassMethod4568.16960()"
		ldstr "class G3_C1590`1<class Generated1118.BaseClass0> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>::Method3<object>()
		calli default string(class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>)
		ldstr "G3_C1590::Method3.16958<System.Object>()"
		ldstr "class G3_C1590`1<class Generated1118.BaseClass0> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>::Method2<object>()
		calli default string(class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>)
		ldstr "G3_C1590::Method2.16957<System.Object>()"
		ldstr "class G3_C1590`1<class Generated1118.BaseClass0> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>::Method1()
		calli default string(class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>)
		ldstr "G3_C1590::Method1.16955()"
		ldstr "class G3_C1590`1<class Generated1118.BaseClass0> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>::Method0()
		calli default string(class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>)
		ldstr "G3_C1590::Method0.16953()"
		ldstr "class G3_C1590`1<class Generated1118.BaseClass0> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>::ClassMethod2567<object>()
		calli default string(class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>)
		ldstr "G3_C1590::ClassMethod2567.MI.16961<System.Object>()"
		ldstr "class G3_C1590`1<class Generated1118.BaseClass0> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>::Method7<object>()
		calli default string(class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>)
		ldstr "G2_C599::Method7.10517<System.Object>()"
		ldstr "class G3_C1590`1<class Generated1118.BaseClass0> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>::ClassMethod1345<object>()
		calli default string(class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>)
		ldstr "G2_C599::ClassMethod1345.MI.10526<System.Object>()"
		ldstr "class G3_C1590`1<class Generated1118.BaseClass0> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>::ClassMethod1344<object>()
		calli default string(class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>)
		ldstr "G2_C599::ClassMethod1344.MI.10525<System.Object>()"
		ldstr "class G3_C1590`1<class Generated1118.BaseClass0> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>::Method6<object>()
		calli default string(class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>)
		ldstr "G1_C11::Method6.4859<System.Object>()"
		ldstr "class G3_C1590`1<class Generated1118.BaseClass0> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>::Method5()
		calli default string(class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>)
		ldstr "G1_C11::Method5.4858()"
		ldstr "class G3_C1590`1<class Generated1118.BaseClass0> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>::Method4()
		calli default string(class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>)
		ldstr "G1_C11::Method4.4856()"
		ldstr "class G3_C1590`1<class Generated1118.BaseClass0> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::ClassMethod1345<object>()
		calli default string(class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>)
		ldstr "G2_C599::ClassMethod1345.MI.10526<System.Object>()"
		ldstr "class G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::ClassMethod1344<object>()
		calli default string(class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>)
		ldstr "G2_C599::ClassMethod1344.MI.10525<System.Object>()"
		ldstr "class G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::Method6<object>()
		calli default string(class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>)
		ldstr "G1_C11::Method6.4859<System.Object>()"
		ldstr "class G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::Method5()
		calli default string(class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>)
		ldstr "G1_C11::Method5.4858()"
		ldstr "class G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::Method4()
		calli default string(class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>)
		ldstr "G1_C11::Method4.4856()"
		ldstr "class G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::Method7<object>()
		calli default string(class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>)
		ldstr "G2_C599::Method7.10517<System.Object>()"
		ldstr "class G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1118.IBase2`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>::Method7<object>()
		calli default string(class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>)
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1118.IBase1`1<class Generated1118.BaseClass0>::Method4()
		calli default string(class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>)
		ldstr "G1_C11::Method4.MI.4857()"
		ldstr "class IBase1`1<class Generated1118.BaseClass0> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1118.IBase1`1<class Generated1118.BaseClass0>::Method5()
		calli default string(class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>)
		ldstr "G1_C11::Method5.4858()"
		ldstr "class IBase1`1<class Generated1118.BaseClass0> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1118.IBase1`1<class Generated1118.BaseClass0>::Method6<object>()
		calli default string(class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>)
		ldstr "G1_C11::Method6.MI.4860<System.Object>()"
		ldstr "class IBase1`1<class Generated1118.BaseClass0> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1118.IBase2`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::Method7<object>()
		calli default string(class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>)
		ldstr "G2_C599::Method7.10517<System.Object>()"
		ldstr "class IBase2`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::ClassMethod2567<object>()
		calli default string(class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>)
		ldstr "G3_C1590::ClassMethod2567.MI.16961<System.Object>()"
		ldstr "class G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::Method3<object>()
		calli default string(class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>)
		ldstr "G3_C1590::Method3.16958<System.Object>()"
		ldstr "class G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::Method2<object>()
		calli default string(class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>)
		ldstr "G3_C1590::Method2.16957<System.Object>()"
		ldstr "class G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::Method1()
		calli default string(class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>)
		ldstr "G3_C1590::Method1.16955()"
		ldstr "class G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::Method0()
		calli default string(class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>)
		ldstr "G3_C1590::Method0.16953()"
		ldstr "class G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::Method7<object>()
		calli default string(class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>)
		ldstr "G2_C599::Method7.10517<System.Object>()"
		ldstr "class G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::ClassMethod1345<object>()
		calli default string(class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>)
		ldstr "G2_C599::ClassMethod1345.MI.10526<System.Object>()"
		ldstr "class G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::ClassMethod1344<object>()
		calli default string(class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>)
		ldstr "G2_C599::ClassMethod1344.MI.10525<System.Object>()"
		ldstr "class G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::Method6<object>()
		calli default string(class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>)
		ldstr "G1_C11::Method6.4859<System.Object>()"
		ldstr "class G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::Method5()
		calli default string(class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>)
		ldstr "G1_C11::Method5.4858()"
		ldstr "class G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::Method4()
		calli default string(class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>)
		ldstr "G1_C11::Method4.4856()"
		ldstr "class G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated1118.IBase0::Method0()
		calli default string(class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>)
		ldstr "G3_C1590::Method0.MI.16954()"
		ldstr "IBase0 on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated1118.IBase0::Method1()
		calli default string(class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>)
		ldstr "G3_C1590::Method1.MI.16956()"
		ldstr "IBase0 on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated1118.IBase0::Method2<object>()
		calli default string(class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>)
		ldstr "G3_C1590::Method2.16957<System.Object>()"
		ldstr "IBase0 on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated1118.IBase0::Method3<object>()
		calli default string(class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>)
		ldstr "G3_C1590::Method3.MI.16959<System.Object>()"
		ldstr "IBase0 on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>::ClassMethod4568()
		calli default string(class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>)
		ldstr "G3_C1590::ClassMethod4568.16960()"
		ldstr "class G3_C1590`1<class Generated1118.BaseClass1> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>::Method3<object>()
		calli default string(class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>)
		ldstr "G3_C1590::Method3.16958<System.Object>()"
		ldstr "class G3_C1590`1<class Generated1118.BaseClass1> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>::Method2<object>()
		calli default string(class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>)
		ldstr "G3_C1590::Method2.16957<System.Object>()"
		ldstr "class G3_C1590`1<class Generated1118.BaseClass1> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>::Method1()
		calli default string(class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>)
		ldstr "G3_C1590::Method1.16955()"
		ldstr "class G3_C1590`1<class Generated1118.BaseClass1> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>::Method0()
		calli default string(class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>)
		ldstr "G3_C1590::Method0.16953()"
		ldstr "class G3_C1590`1<class Generated1118.BaseClass1> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>::ClassMethod2567<object>()
		calli default string(class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>)
		ldstr "G3_C1590::ClassMethod2567.MI.16961<System.Object>()"
		ldstr "class G3_C1590`1<class Generated1118.BaseClass1> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>::Method7<object>()
		calli default string(class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>)
		ldstr "G2_C599::Method7.10517<System.Object>()"
		ldstr "class G3_C1590`1<class Generated1118.BaseClass1> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>::ClassMethod1345<object>()
		calli default string(class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>)
		ldstr "G2_C599::ClassMethod1345.MI.10526<System.Object>()"
		ldstr "class G3_C1590`1<class Generated1118.BaseClass1> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>::ClassMethod1344<object>()
		calli default string(class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>)
		ldstr "G2_C599::ClassMethod1344.MI.10525<System.Object>()"
		ldstr "class G3_C1590`1<class Generated1118.BaseClass1> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>::Method6<object>()
		calli default string(class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>)
		ldstr "G1_C11::Method6.4859<System.Object>()"
		ldstr "class G3_C1590`1<class Generated1118.BaseClass1> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>::Method5()
		calli default string(class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>)
		ldstr "G1_C11::Method5.4858()"
		ldstr "class G3_C1590`1<class Generated1118.BaseClass1> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>::Method4()
		calli default string(class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>)
		ldstr "G1_C11::Method4.4856()"
		ldstr "class G3_C1590`1<class Generated1118.BaseClass1> on type class Generated1118.G3_C1590`1<class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::ClassMethod1345<object>()
		calli default string(class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>)
		ldstr "G2_C599::ClassMethod1345.MI.10526<System.Object>()"
		ldstr "class G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::ClassMethod1344<object>()
		calli default string(class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>)
		ldstr "G2_C599::ClassMethod1344.MI.10525<System.Object>()"
		ldstr "class G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::Method6<object>()
		calli default string(class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>)
		ldstr "G1_C11::Method6.4859<System.Object>()"
		ldstr "class G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::Method5()
		calli default string(class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>)
		ldstr "G1_C11::Method5.4858()"
		ldstr "class G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::Method4()
		calli default string(class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>)
		ldstr "G1_C11::Method4.4856()"
		ldstr "class G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::Method7<object>()
		calli default string(class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>)
		ldstr "G2_C599::Method7.10517<System.Object>()"
		ldstr "class G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1118.IBase2`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>::Method7<object>()
		calli default string(class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>)
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1118.IBase1`1<class Generated1118.BaseClass0>::Method4()
		calli default string(class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>)
		ldstr "G1_C11::Method4.MI.4857()"
		ldstr "class IBase1`1<class Generated1118.BaseClass0> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1118.IBase1`1<class Generated1118.BaseClass0>::Method5()
		calli default string(class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>)
		ldstr "G1_C11::Method5.4858()"
		ldstr "class IBase1`1<class Generated1118.BaseClass0> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1118.IBase1`1<class Generated1118.BaseClass0>::Method6<object>()
		calli default string(class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>)
		ldstr "G1_C11::Method6.MI.4860<System.Object>()"
		ldstr "class IBase1`1<class Generated1118.BaseClass0> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1118.IBase2`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::Method7<object>()
		calli default string(class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>)
		ldstr "G2_C599::Method7.10517<System.Object>()"
		ldstr "class IBase2`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>::ClassMethod2567<object>()
		calli default string(class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>)
		ldstr "G2_C599::ClassMethod2567.10524<System.Object>()"
		ldstr "class G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>::Method3<object>()
		calli default string(class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>)
		ldstr "G2_C599::Method3.10522<System.Object>()"
		ldstr "class G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>::Method2<object>()
		calli default string(class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>)
		ldstr "G2_C599::Method2.10521<System.Object>()"
		ldstr "class G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>::Method1()
		calli default string(class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>)
		ldstr "G2_C599::Method1.10520()"
		ldstr "class G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>::Method0()
		calli default string(class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>)
		ldstr "G2_C599::Method0.10518()"
		ldstr "class G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>::Method7<object>()
		calli default string(class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>)
		ldstr "G2_C599::Method7.10517<System.Object>()"
		ldstr "class G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>::ClassMethod1345<object>()
		calli default string(class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>)
		ldstr "G2_C599::ClassMethod1345.MI.10526<System.Object>()"
		ldstr "class G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>::ClassMethod1344<object>()
		calli default string(class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>)
		ldstr "G2_C599::ClassMethod1344.MI.10525<System.Object>()"
		ldstr "class G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>::Method6<object>()
		calli default string(class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>)
		ldstr "G1_C11::Method6.4859<System.Object>()"
		ldstr "class G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>::Method5()
		calli default string(class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>)
		ldstr "G1_C11::Method5.4858()"
		ldstr "class G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>::Method4()
		calli default string(class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>)
		ldstr "G1_C11::Method4.4856()"
		ldstr "class G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1118.IBase2`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>::Method7<object>()
		calli default string(class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>)
		ldstr "G2_C599::Method7.10517<System.Object>()"
		ldstr "class IBase2`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated1118.IBase0::Method0()
		calli default string(class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>)
		ldstr "G2_C599::Method0.MI.10519()"
		ldstr "IBase0 on type class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated1118.IBase0::Method1()
		calli default string(class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>)
		ldstr "G2_C599::Method1.10520()"
		ldstr "IBase0 on type class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated1118.IBase0::Method2<object>()
		calli default string(class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>)
		ldstr "G2_C599::Method2.10521<System.Object>()"
		ldstr "IBase0 on type class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated1118.IBase0::Method3<object>()
		calli default string(class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>)
		ldstr "G2_C599::Method3.MI.10523<System.Object>()"
		ldstr "IBase0 on type class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::ClassMethod1345<object>()
		calli default string(class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>)
		ldstr "G2_C599::ClassMethod1345.MI.10526<System.Object>()"
		ldstr "class G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::ClassMethod1344<object>()
		calli default string(class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>)
		ldstr "G2_C599::ClassMethod1344.MI.10525<System.Object>()"
		ldstr "class G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::Method6<object>()
		calli default string(class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>)
		ldstr "G1_C11::Method6.4859<System.Object>()"
		ldstr "class G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::Method5()
		calli default string(class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>)
		ldstr "G1_C11::Method5.4858()"
		ldstr "class G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::Method4()
		calli default string(class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>)
		ldstr "G1_C11::Method4.4856()"
		ldstr "class G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::Method7<object>()
		calli default string(class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>)
		ldstr "G2_C599::Method7.10517<System.Object>()"
		ldstr "class G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1118.IBase2`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>::Method7<object>()
		calli default string(class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>)
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1118.IBase1`1<class Generated1118.BaseClass0>::Method4()
		calli default string(class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>)
		ldstr "G1_C11::Method4.MI.4857()"
		ldstr "class IBase1`1<class Generated1118.BaseClass0> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1118.IBase1`1<class Generated1118.BaseClass0>::Method5()
		calli default string(class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>)
		ldstr "G1_C11::Method5.4858()"
		ldstr "class IBase1`1<class Generated1118.BaseClass0> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1118.IBase1`1<class Generated1118.BaseClass0>::Method6<object>()
		calli default string(class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>)
		ldstr "G1_C11::Method6.MI.4860<System.Object>()"
		ldstr "class IBase1`1<class Generated1118.BaseClass0> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1118.IBase2`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::Method7<object>()
		calli default string(class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>)
		ldstr "G2_C599::Method7.10517<System.Object>()"
		ldstr "class IBase2`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::ClassMethod2567<object>()
		calli default string(class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>)
		ldstr "G2_C599::ClassMethod2567.10524<System.Object>()"
		ldstr "class G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::Method3<object>()
		calli default string(class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>)
		ldstr "G2_C599::Method3.10522<System.Object>()"
		ldstr "class G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::Method2<object>()
		calli default string(class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>)
		ldstr "G2_C599::Method2.10521<System.Object>()"
		ldstr "class G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::Method1()
		calli default string(class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>)
		ldstr "G2_C599::Method1.10520()"
		ldstr "class G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::Method0()
		calli default string(class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>)
		ldstr "G2_C599::Method0.10518()"
		ldstr "class G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::Method7<object>()
		calli default string(class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>)
		ldstr "G2_C599::Method7.10517<System.Object>()"
		ldstr "class G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::ClassMethod1345<object>()
		calli default string(class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>)
		ldstr "G2_C599::ClassMethod1345.MI.10526<System.Object>()"
		ldstr "class G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::ClassMethod1344<object>()
		calli default string(class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>)
		ldstr "G2_C599::ClassMethod1344.MI.10525<System.Object>()"
		ldstr "class G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::Method6<object>()
		calli default string(class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>)
		ldstr "G1_C11::Method6.4859<System.Object>()"
		ldstr "class G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::Method5()
		calli default string(class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>)
		ldstr "G1_C11::Method5.4858()"
		ldstr "class G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::Method4()
		calli default string(class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>)
		ldstr "G1_C11::Method4.4856()"
		ldstr "class G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated1118.IBase0::Method0()
		calli default string(class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>)
		ldstr "G2_C599::Method0.MI.10519()"
		ldstr "IBase0 on type class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated1118.IBase0::Method1()
		calli default string(class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>)
		ldstr "G2_C599::Method1.10520()"
		ldstr "IBase0 on type class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated1118.IBase0::Method2<object>()
		calli default string(class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>)
		ldstr "G2_C599::Method2.10521<System.Object>()"
		ldstr "IBase0 on type class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated1118.IBase0::Method3<object>()
		calli default string(class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>)
		ldstr "G2_C599::Method3.MI.10523<System.Object>()"
		ldstr "IBase0 on type class Generated1118.G2_C599`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>::ClassMethod1345<object>()
		calli default string(class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>)
		ldstr "G2_C599::ClassMethod1345.MI.10526<System.Object>()"
		ldstr "class G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>::ClassMethod1344<object>()
		calli default string(class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>)
		ldstr "G2_C599::ClassMethod1344.MI.10525<System.Object>()"
		ldstr "class G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>::Method6<object>()
		calli default string(class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>)
		ldstr "G1_C11::Method6.4859<System.Object>()"
		ldstr "class G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>::Method5()
		calli default string(class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>)
		ldstr "G1_C11::Method5.4858()"
		ldstr "class G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>::Method4()
		calli default string(class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>)
		ldstr "G1_C11::Method4.4856()"
		ldstr "class G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>::Method7<object>()
		calli default string(class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>)
		ldstr "G2_C599::Method7.10517<System.Object>()"
		ldstr "class G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1118.IBase2`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>::Method7<object>()
		calli default string(class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>)
		ldstr "G2_C599::Method7.10517<System.Object>()"
		ldstr "class IBase2`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1118.IBase1`1<class Generated1118.BaseClass0>::Method4()
		calli default string(class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>)
		ldstr "G1_C11::Method4.MI.4857()"
		ldstr "class IBase1`1<class Generated1118.BaseClass0> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1118.IBase1`1<class Generated1118.BaseClass0>::Method5()
		calli default string(class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>)
		ldstr "G1_C11::Method5.4858()"
		ldstr "class IBase1`1<class Generated1118.BaseClass0> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1118.IBase1`1<class Generated1118.BaseClass0>::Method6<object>()
		calli default string(class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>)
		ldstr "G1_C11::Method6.MI.4860<System.Object>()"
		ldstr "class IBase1`1<class Generated1118.BaseClass0> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1118.IBase2`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::Method7<object>()
		calli default string(class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>)
		ldstr "G2_C599::Method7.10517<System.Object>()"
		ldstr "class IBase2`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>::ClassMethod2567<object>()
		calli default string(class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>)
		ldstr "G2_C599::ClassMethod2567.10524<System.Object>()"
		ldstr "class G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>::Method3<object>()
		calli default string(class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>)
		ldstr "G2_C599::Method3.10522<System.Object>()"
		ldstr "class G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>::Method2<object>()
		calli default string(class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>)
		ldstr "G2_C599::Method2.10521<System.Object>()"
		ldstr "class G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>::Method1()
		calli default string(class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>)
		ldstr "G2_C599::Method1.10520()"
		ldstr "class G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>::Method0()
		calli default string(class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>)
		ldstr "G2_C599::Method0.10518()"
		ldstr "class G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>::Method7<object>()
		calli default string(class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>)
		ldstr "G2_C599::Method7.10517<System.Object>()"
		ldstr "class G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>::ClassMethod1345<object>()
		calli default string(class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>)
		ldstr "G2_C599::ClassMethod1345.MI.10526<System.Object>()"
		ldstr "class G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>::ClassMethod1344<object>()
		calli default string(class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>)
		ldstr "G2_C599::ClassMethod1344.MI.10525<System.Object>()"
		ldstr "class G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>::Method6<object>()
		calli default string(class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>)
		ldstr "G1_C11::Method6.4859<System.Object>()"
		ldstr "class G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>::Method5()
		calli default string(class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>)
		ldstr "G1_C11::Method5.4858()"
		ldstr "class G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>::Method4()
		calli default string(class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>)
		ldstr "G1_C11::Method4.4856()"
		ldstr "class G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1118.IBase2`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>::Method7<object>()
		calli default string(class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>)
		ldstr "G2_C599::Method7.10517<System.Object>()"
		ldstr "class IBase2`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated1118.IBase0::Method0()
		calli default string(class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>)
		ldstr "G2_C599::Method0.MI.10519()"
		ldstr "IBase0 on type class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated1118.IBase0::Method1()
		calli default string(class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>)
		ldstr "G2_C599::Method1.10520()"
		ldstr "IBase0 on type class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated1118.IBase0::Method2<object>()
		calli default string(class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>)
		ldstr "G2_C599::Method2.10521<System.Object>()"
		ldstr "IBase0 on type class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated1118.IBase0::Method3<object>()
		calli default string(class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>)
		ldstr "G2_C599::Method3.MI.10523<System.Object>()"
		ldstr "IBase0 on type class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1118.IBase2`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>::Method7<object>()
		calli default string(class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>)
		ldstr "G2_C599::Method7.10517<System.Object>()"
		ldstr "class IBase2`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>::ClassMethod1345<object>()
		calli default string(class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>)
		ldstr "G2_C599::ClassMethod1345.MI.10526<System.Object>()"
		ldstr "class G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>::ClassMethod1344<object>()
		calli default string(class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>)
		ldstr "G2_C599::ClassMethod1344.MI.10525<System.Object>()"
		ldstr "class G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>::Method6<object>()
		calli default string(class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>)
		ldstr "G1_C11::Method6.4859<System.Object>()"
		ldstr "class G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>::Method5()
		calli default string(class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>)
		ldstr "G1_C11::Method5.4858()"
		ldstr "class G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>::Method4()
		calli default string(class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>)
		ldstr "G1_C11::Method4.4856()"
		ldstr "class G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>::Method7<object>()
		calli default string(class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>)
		ldstr "G2_C599::Method7.10517<System.Object>()"
		ldstr "class G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1118.IBase2`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>::Method7<object>()
		calli default string(class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>)
		ldstr "G2_C599::Method7.10517<System.Object>()"
		ldstr "class IBase2`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1118.IBase1`1<class Generated1118.BaseClass0>::Method4()
		calli default string(class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>)
		ldstr "G1_C11::Method4.MI.4857()"
		ldstr "class IBase1`1<class Generated1118.BaseClass0> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1118.IBase1`1<class Generated1118.BaseClass0>::Method5()
		calli default string(class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>)
		ldstr "G1_C11::Method5.4858()"
		ldstr "class IBase1`1<class Generated1118.BaseClass0> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1118.IBase1`1<class Generated1118.BaseClass0>::Method6<object>()
		calli default string(class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>)
		ldstr "G1_C11::Method6.MI.4860<System.Object>()"
		ldstr "class IBase1`1<class Generated1118.BaseClass0> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1118.IBase2`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::Method7<object>()
		calli default string(class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>)
		ldstr "G2_C599::Method7.10517<System.Object>()"
		ldstr "class IBase2`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>::ClassMethod2567<object>()
		calli default string(class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>)
		ldstr "G2_C599::ClassMethod2567.10524<System.Object>()"
		ldstr "class G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>::Method3<object>()
		calli default string(class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>)
		ldstr "G2_C599::Method3.10522<System.Object>()"
		ldstr "class G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>::Method2<object>()
		calli default string(class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>)
		ldstr "G2_C599::Method2.10521<System.Object>()"
		ldstr "class G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>::Method1()
		calli default string(class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>)
		ldstr "G2_C599::Method1.10520()"
		ldstr "class G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>::Method0()
		calli default string(class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>)
		ldstr "G2_C599::Method0.10518()"
		ldstr "class G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>::Method7<object>()
		calli default string(class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>)
		ldstr "G2_C599::Method7.10517<System.Object>()"
		ldstr "class G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>::ClassMethod1345<object>()
		calli default string(class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>)
		ldstr "G2_C599::ClassMethod1345.MI.10526<System.Object>()"
		ldstr "class G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>::ClassMethod1344<object>()
		calli default string(class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>)
		ldstr "G2_C599::ClassMethod1344.MI.10525<System.Object>()"
		ldstr "class G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>::Method6<object>()
		calli default string(class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>)
		ldstr "G1_C11::Method6.4859<System.Object>()"
		ldstr "class G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>::Method5()
		calli default string(class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>)
		ldstr "G1_C11::Method5.4858()"
		ldstr "class G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>::Method4()
		calli default string(class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>)
		ldstr "G1_C11::Method4.4856()"
		ldstr "class G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1> on type class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated1118.IBase0::Method0()
		calli default string(class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>)
		ldstr "G2_C599::Method0.MI.10519()"
		ldstr "IBase0 on type class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated1118.IBase0::Method1()
		calli default string(class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>)
		ldstr "G2_C599::Method1.10520()"
		ldstr "IBase0 on type class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated1118.IBase0::Method2<object>()
		calli default string(class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>)
		ldstr "G2_C599::Method2.10521<System.Object>()"
		ldstr "IBase0 on type class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated1118.IBase0::Method3<object>()
		calli default string(class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>)
		ldstr "G2_C599::Method3.MI.10523<System.Object>()"
		ldstr "IBase0 on type class Generated1118.G2_C599`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>::ClassMethod1345<object>()
		calli default string(class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>)
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0> on type class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>::ClassMethod1344<object>()
		calli default string(class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>)
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()"
		ldstr "class G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0> on type class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>::Method6<object>()
		calli default string(class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>)
		ldstr "G1_C11::Method6.4859<System.Object>()"
		ldstr "class G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0> on type class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>::Method5()
		calli default string(class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>)
		ldstr "G1_C11::Method5.4858()"
		ldstr "class G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0> on type class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>::Method4()
		calli default string(class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>)
		ldstr "G1_C11::Method4.4856()"
		ldstr "class G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0> on type class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>::Method7<object>()
		calli default string(class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>)
		ldstr "G1_C11::Method7.4854<System.Object>()"
		ldstr "class G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0> on type class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1118.IBase2`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>::Method7<object>()
		calli default string(class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>)
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0> on type class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1118.IBase1`1<class Generated1118.BaseClass0>::Method4()
		calli default string(class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>)
		ldstr "G1_C11::Method4.MI.4857()"
		ldstr "class IBase1`1<class Generated1118.BaseClass0> on type class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1118.IBase1`1<class Generated1118.BaseClass0>::Method5()
		calli default string(class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>)
		ldstr "G1_C11::Method5.4858()"
		ldstr "class IBase1`1<class Generated1118.BaseClass0> on type class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1118.IBase1`1<class Generated1118.BaseClass0>::Method6<object>()
		calli default string(class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>)
		ldstr "G1_C11::Method6.MI.4860<System.Object>()"
		ldstr "class IBase1`1<class Generated1118.BaseClass0> on type class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1118.IBase2`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>::Method7<object>()
		calli default string(class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>)
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0> on type class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1118.IBase2`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::Method7<object>()
		calli default string(class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>)
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1> on type class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1118.IBase2`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>::Method7<object>()
		calli default string(class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>)
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1> on type class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::ClassMethod1345<object>()
		calli default string(class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>)
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1> on type class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::ClassMethod1344<object>()
		calli default string(class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>)
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()"
		ldstr "class G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1> on type class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::Method6<object>()
		calli default string(class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>)
		ldstr "G1_C11::Method6.4859<System.Object>()"
		ldstr "class G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1> on type class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::Method5()
		calli default string(class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>)
		ldstr "G1_C11::Method5.4858()"
		ldstr "class G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1> on type class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::Method4()
		calli default string(class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>)
		ldstr "G1_C11::Method4.4856()"
		ldstr "class G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1> on type class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::Method7<object>()
		calli default string(class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>)
		ldstr "G1_C11::Method7.4854<System.Object>()"
		ldstr "class G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1> on type class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1118.IBase2`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>::Method7<object>()
		calli default string(class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>)
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1> on type class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1118.IBase1`1<class Generated1118.BaseClass0>::Method4()
		calli default string(class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>)
		ldstr "G1_C11::Method4.MI.4857()"
		ldstr "class IBase1`1<class Generated1118.BaseClass0> on type class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1118.IBase1`1<class Generated1118.BaseClass0>::Method5()
		calli default string(class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>)
		ldstr "G1_C11::Method5.4858()"
		ldstr "class IBase1`1<class Generated1118.BaseClass0> on type class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1118.IBase1`1<class Generated1118.BaseClass0>::Method6<object>()
		calli default string(class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>)
		ldstr "G1_C11::Method6.MI.4860<System.Object>()"
		ldstr "class IBase1`1<class Generated1118.BaseClass0> on type class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1118.IBase2`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::Method7<object>()
		calli default string(class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>)
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1> on type class Generated1118.G1_C11`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>::ClassMethod1345<object>()
		calli default string(class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>)
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0> on type class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>::ClassMethod1344<object>()
		calli default string(class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>)
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()"
		ldstr "class G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0> on type class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>::Method6<object>()
		calli default string(class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>)
		ldstr "G1_C11::Method6.4859<System.Object>()"
		ldstr "class G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0> on type class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>::Method5()
		calli default string(class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>)
		ldstr "G1_C11::Method5.4858()"
		ldstr "class G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0> on type class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>::Method4()
		calli default string(class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>)
		ldstr "G1_C11::Method4.4856()"
		ldstr "class G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0> on type class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>::Method7<object>()
		calli default string(class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>)
		ldstr "G1_C11::Method7.4854<System.Object>()"
		ldstr "class G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0> on type class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1118.IBase2`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>::Method7<object>()
		calli default string(class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>)
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0> on type class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1118.IBase1`1<class Generated1118.BaseClass0>::Method4()
		calli default string(class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>)
		ldstr "G1_C11::Method4.MI.4857()"
		ldstr "class IBase1`1<class Generated1118.BaseClass0> on type class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1118.IBase1`1<class Generated1118.BaseClass0>::Method5()
		calli default string(class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>)
		ldstr "G1_C11::Method5.4858()"
		ldstr "class IBase1`1<class Generated1118.BaseClass0> on type class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1118.IBase1`1<class Generated1118.BaseClass0>::Method6<object>()
		calli default string(class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>)
		ldstr "G1_C11::Method6.MI.4860<System.Object>()"
		ldstr "class IBase1`1<class Generated1118.BaseClass0> on type class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1118.IBase2`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0>::Method7<object>()
		calli default string(class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>)
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated1118.BaseClass0,class Generated1118.BaseClass0> on type class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1118.IBase2`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::Method7<object>()
		calli default string(class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>)
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1> on type class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1118.IBase2`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>::Method7<object>()
		calli default string(class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>)
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1> on type class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>::ClassMethod1345<object>()
		calli default string(class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>)
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1> on type class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>::ClassMethod1344<object>()
		calli default string(class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>)
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()"
		ldstr "class G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1> on type class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>::Method6<object>()
		calli default string(class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>)
		ldstr "G1_C11::Method6.4859<System.Object>()"
		ldstr "class G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1> on type class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>::Method5()
		calli default string(class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>)
		ldstr "G1_C11::Method5.4858()"
		ldstr "class G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1> on type class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>::Method4()
		calli default string(class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>)
		ldstr "G1_C11::Method4.4856()"
		ldstr "class G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1> on type class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>::Method7<object>()
		calli default string(class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>)
		ldstr "G1_C11::Method7.4854<System.Object>()"
		ldstr "class G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1> on type class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1118.IBase2`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>::Method7<object>()
		calli default string(class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>)
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1> on type class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1118.IBase1`1<class Generated1118.BaseClass0>::Method4()
		calli default string(class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>)
		ldstr "G1_C11::Method4.MI.4857()"
		ldstr "class IBase1`1<class Generated1118.BaseClass0> on type class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1118.IBase1`1<class Generated1118.BaseClass0>::Method5()
		calli default string(class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>)
		ldstr "G1_C11::Method5.4858()"
		ldstr "class IBase1`1<class Generated1118.BaseClass0> on type class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1118.IBase1`1<class Generated1118.BaseClass0>::Method6<object>()
		calli default string(class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>)
		ldstr "G1_C11::Method6.MI.4860<System.Object>()"
		ldstr "class IBase1`1<class Generated1118.BaseClass0> on type class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1118.IBase2`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1>::Method7<object>()
		calli default string(class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>)
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated1118.BaseClass0,class Generated1118.BaseClass1> on type class Generated1118.G1_C11`2<class Generated1118.BaseClass1,class Generated1118.BaseClass1>"
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
		call void Generated1118.Generated1118::MethodCallingTest()
		call void Generated1118.Generated1118::ConstrainedCallsTest()
		call void Generated1118.Generated1118::StructConstrainedInterfaceCallsTest()
		call void Generated1118.Generated1118::CalliTest()
		ldc.i4 100
		ret
	}
}
