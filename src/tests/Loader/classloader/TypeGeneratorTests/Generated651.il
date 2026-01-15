// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.class public Generated651.BaseClass0 
{
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void [mscorlib]System.Object::.ctor()
		ret
	}
}
.class public Generated651.BaseClass1 
		extends Generated651.BaseClass0
{
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void Generated651.BaseClass0::.ctor()
		ret
	}
}
.class public Generated651.G3_C1123`1<T0> 
		extends class Generated651.G2_C148`1<class Generated651.BaseClass1>
		implements Generated651.IBase0 
{
	.method public hidebysig newslot virtual instance string Method0() cil managed noinlining { 
		ldstr "G3_C1123::Method0.14448()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'Generated651.IBase0.Method0'() cil managed noinlining { 
		.override method instance string Generated651.IBase0::Method0()
		ldstr "G3_C1123::Method0.MI.14449()"
		ret
	}
	.method public hidebysig newslot virtual instance string Method1() cil managed noinlining { 
		ldstr "G3_C1123::Method1.14450()"
		ret
	}
	.method public hidebysig newslot virtual instance string Method2<M0>() cil managed noinlining { 
		ldstr "G3_C1123::Method2.14451<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'Generated651.IBase0.Method2'<M0>() cil managed noinlining { 
		.override method instance string Generated651.IBase0::Method2<[1]>()
		ldstr "G3_C1123::Method2.MI.14452<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig virtual instance string Method3<M0>() cil managed noinlining { 
		ldstr "G3_C1123::Method3.14453<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'Generated651.IBase0.Method3'<M0>() cil managed noinlining { 
		.override method instance string Generated651.IBase0::Method3<[1]>()
		ldstr "G3_C1123::Method3.MI.14454<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod3643<M0>() cil managed noinlining { 
		ldstr "G3_C1123::ClassMethod3643.14455<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'G2_C148<class Generated651.BaseClass1>.ClassMethod1619'() cil managed noinlining { 
		.override method instance string class Generated651.G2_C148`1<class Generated651.BaseClass1>::ClassMethod1619()
		ldstr "G3_C1123::ClassMethod1619.MI.14456()"
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class Generated651.G2_C148`1<class Generated651.BaseClass1>::.ctor()
		ret
	}
}
.class public Generated651.G2_C148`1<T0> 
		extends class Generated651.G1_C11`2<class Generated651.BaseClass0,!T0>
		implements class Generated651.IBase1`1<class Generated651.BaseClass0>, class Generated651.IBase2`2<class Generated651.BaseClass1,class Generated651.BaseClass0> 
{
	.method public hidebysig virtual instance string Method4() cil managed noinlining { 
		ldstr "G2_C148::Method4.6160()"
		ret
	}
	.method public hidebysig newslot virtual instance string Method5() cil managed noinlining { 
		ldstr "G2_C148::Method5.6161()"
		ret
	}
	.method public hidebysig virtual instance string Method6<M0>() cil managed noinlining { 
		ldstr "G2_C148::Method6.6162<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<class Generated651.BaseClass0>.Method6'<M0>() cil managed noinlining { 
		.override method instance string class Generated651.IBase1`1<class Generated651.BaseClass0>::Method6<[1]>()
		ldstr "G2_C148::Method6.MI.6163<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G2_C148::Method7.6164<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1619() cil managed noinlining { 
		ldstr "G2_C148::ClassMethod1619.6165()"
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class Generated651.G1_C11`2<class Generated651.BaseClass0,!T0>::.ctor()
		ret
	}
}
.class interface public abstract Generated651.IBase0 
{
	.method public hidebysig newslot abstract virtual instance string Method0() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method1() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method2<M0>() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method3<M0>() cil managed { }
}
.class public Generated651.G1_C11`2<T0, T1> 
		implements class Generated651.IBase2`2<class Generated651.BaseClass1,!T1>, class Generated651.IBase1`1<class Generated651.BaseClass0> 
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
	.method public hidebysig newslot virtual instance string 'IBase2<class Generated651.BaseClass1,T1>.Method7'<M0>() cil managed noinlining { 
		.override method instance string class Generated651.IBase2`2<class Generated651.BaseClass1,!T1>::Method7<[1]>()
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
	.method public hidebysig newslot virtual instance string 'IBase1<class Generated651.BaseClass0>.Method4'() cil managed noinlining { 
		.override method instance string class Generated651.IBase1`1<class Generated651.BaseClass0>::Method4()
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
	.method public hidebysig newslot virtual instance string 'IBase1<class Generated651.BaseClass0>.Method6'<M0>() cil managed noinlining { 
		.override method instance string class Generated651.IBase1`1<class Generated651.BaseClass0>::Method6<[1]>()
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
.class interface public abstract Generated651.IBase1`1<+T0> 
{
	.method public hidebysig newslot abstract virtual instance string Method4() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method5() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method6<M0>() cil managed { }
}
.class interface public abstract Generated651.IBase2`2<+T0, -T1> 
{
	.method public hidebysig newslot abstract virtual instance string Method7<M0>() cil managed { }
}
.class public auto ansi beforefieldinit Generated651.Generated651 {
	.method static void M.BaseClass0<(Generated651.BaseClass0)W>(!!W inst, string exp) cil managed {
		.maxstack 5
		.locals init (string[] actualResults)
		ldc.i4.s    0
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.BaseClass0<(Generated651.BaseClass0)W>(!!W inst, string exp)"
		ldc.i4.s 0
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.BaseClass1<(Generated651.BaseClass1)W>(!!W inst, string exp) cil managed {
		.maxstack 5
		.locals init (string[] actualResults)
		ldc.i4.s    0
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.BaseClass1<(Generated651.BaseClass1)W>(!!W inst, string exp)"
		ldc.i4.s 0
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1123.T<T0,(class Generated651.G3_C1123`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 17
		.locals init (string[] actualResults)
		ldc.i4.s    12
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1123.T<T0,(class Generated651.G3_C1123`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 12
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated651.G3_C1123`1<!!T0>::ClassMethod1344<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated651.G3_C1123`1<!!T0>::ClassMethod1345<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated651.G3_C1123`1<!!T0>::ClassMethod1619()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated651.G3_C1123`1<!!T0>::ClassMethod3643<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated651.G3_C1123`1<!!T0>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated651.G3_C1123`1<!!T0>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated651.G3_C1123`1<!!T0>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated651.G3_C1123`1<!!T0>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated651.G3_C1123`1<!!T0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated651.G3_C1123`1<!!T0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated651.G3_C1123`1<!!T0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated651.G3_C1123`1<!!T0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1123.A<(class Generated651.G3_C1123`1<class Generated651.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 17
		.locals init (string[] actualResults)
		ldc.i4.s    12
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1123.A<(class Generated651.G3_C1123`1<class Generated651.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 12
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated651.G3_C1123`1<class Generated651.BaseClass0>::ClassMethod1344<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated651.G3_C1123`1<class Generated651.BaseClass0>::ClassMethod1345<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated651.G3_C1123`1<class Generated651.BaseClass0>::ClassMethod1619()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated651.G3_C1123`1<class Generated651.BaseClass0>::ClassMethod3643<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated651.G3_C1123`1<class Generated651.BaseClass0>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated651.G3_C1123`1<class Generated651.BaseClass0>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated651.G3_C1123`1<class Generated651.BaseClass0>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated651.G3_C1123`1<class Generated651.BaseClass0>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated651.G3_C1123`1<class Generated651.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated651.G3_C1123`1<class Generated651.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated651.G3_C1123`1<class Generated651.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated651.G3_C1123`1<class Generated651.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1123.B<(class Generated651.G3_C1123`1<class Generated651.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 17
		.locals init (string[] actualResults)
		ldc.i4.s    12
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1123.B<(class Generated651.G3_C1123`1<class Generated651.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 12
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated651.G3_C1123`1<class Generated651.BaseClass1>::ClassMethod1344<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated651.G3_C1123`1<class Generated651.BaseClass1>::ClassMethod1345<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated651.G3_C1123`1<class Generated651.BaseClass1>::ClassMethod1619()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated651.G3_C1123`1<class Generated651.BaseClass1>::ClassMethod3643<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated651.G3_C1123`1<class Generated651.BaseClass1>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated651.G3_C1123`1<class Generated651.BaseClass1>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated651.G3_C1123`1<class Generated651.BaseClass1>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated651.G3_C1123`1<class Generated651.BaseClass1>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated651.G3_C1123`1<class Generated651.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated651.G3_C1123`1<class Generated651.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated651.G3_C1123`1<class Generated651.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated651.G3_C1123`1<class Generated651.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C148.T<T0,(class Generated651.G2_C148`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 12
		.locals init (string[] actualResults)
		ldc.i4.s    7
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C148.T<T0,(class Generated651.G2_C148`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 7
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated651.G2_C148`1<!!T0>::ClassMethod1344<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated651.G2_C148`1<!!T0>::ClassMethod1345<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated651.G2_C148`1<!!T0>::ClassMethod1619()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated651.G2_C148`1<!!T0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated651.G2_C148`1<!!T0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated651.G2_C148`1<!!T0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated651.G2_C148`1<!!T0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C148.A<(class Generated651.G2_C148`1<class Generated651.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 12
		.locals init (string[] actualResults)
		ldc.i4.s    7
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C148.A<(class Generated651.G2_C148`1<class Generated651.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 7
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated651.G2_C148`1<class Generated651.BaseClass0>::ClassMethod1344<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated651.G2_C148`1<class Generated651.BaseClass0>::ClassMethod1345<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated651.G2_C148`1<class Generated651.BaseClass0>::ClassMethod1619()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated651.G2_C148`1<class Generated651.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated651.G2_C148`1<class Generated651.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated651.G2_C148`1<class Generated651.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated651.G2_C148`1<class Generated651.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C148.B<(class Generated651.G2_C148`1<class Generated651.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 12
		.locals init (string[] actualResults)
		ldc.i4.s    7
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C148.B<(class Generated651.G2_C148`1<class Generated651.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 7
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated651.G2_C148`1<class Generated651.BaseClass1>::ClassMethod1344<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated651.G2_C148`1<class Generated651.BaseClass1>::ClassMethod1345<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated651.G2_C148`1<class Generated651.BaseClass1>::ClassMethod1619()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated651.G2_C148`1<class Generated651.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated651.G2_C148`1<class Generated651.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated651.G2_C148`1<class Generated651.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated651.G2_C148`1<class Generated651.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.Generated651.IBase0<(Generated651.IBase0)W>(!!W inst, string exp) cil managed {
		.maxstack 9
		.locals init (string[] actualResults)
		ldc.i4.s    4
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.Generated651.IBase0<(Generated651.IBase0)W>(!!W inst, string exp)"
		ldc.i4.s 4
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string Generated651.IBase0::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string Generated651.IBase0::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string Generated651.IBase0::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string Generated651.IBase0::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C11.T.T<T0,T1,(class Generated651.G1_C11`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C11.T.T<T0,T1,(class Generated651.G1_C11`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated651.G1_C11`2<!!T0,!!T1>::ClassMethod1344<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated651.G1_C11`2<!!T0,!!T1>::ClassMethod1345<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated651.G1_C11`2<!!T0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated651.G1_C11`2<!!T0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated651.G1_C11`2<!!T0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated651.G1_C11`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C11.A.T<T1,(class Generated651.G1_C11`2<class Generated651.BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C11.A.T<T1,(class Generated651.G1_C11`2<class Generated651.BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated651.G1_C11`2<class Generated651.BaseClass0,!!T1>::ClassMethod1344<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated651.G1_C11`2<class Generated651.BaseClass0,!!T1>::ClassMethod1345<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated651.G1_C11`2<class Generated651.BaseClass0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated651.G1_C11`2<class Generated651.BaseClass0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated651.G1_C11`2<class Generated651.BaseClass0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated651.G1_C11`2<class Generated651.BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C11.A.A<(class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C11.A.A<(class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0>::ClassMethod1344<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0>::ClassMethod1345<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C11.A.B<(class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C11.A.B<(class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>::ClassMethod1344<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>::ClassMethod1345<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C11.B.T<T1,(class Generated651.G1_C11`2<class Generated651.BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C11.B.T<T1,(class Generated651.G1_C11`2<class Generated651.BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated651.G1_C11`2<class Generated651.BaseClass1,!!T1>::ClassMethod1344<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated651.G1_C11`2<class Generated651.BaseClass1,!!T1>::ClassMethod1345<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated651.G1_C11`2<class Generated651.BaseClass1,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated651.G1_C11`2<class Generated651.BaseClass1,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated651.G1_C11`2<class Generated651.BaseClass1,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated651.G1_C11`2<class Generated651.BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C11.B.A<(class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C11.B.A<(class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass0>::ClassMethod1344<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass0>::ClassMethod1345<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C11.B.B<(class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C11.B.B<(class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass1>::ClassMethod1344<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass1>::ClassMethod1345<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase1.T<T0,(class Generated651.IBase1`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase1.T<T0,(class Generated651.IBase1`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated651.IBase1`1<!!T0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated651.IBase1`1<!!T0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated651.IBase1`1<!!T0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase1.A<(class Generated651.IBase1`1<class Generated651.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase1.A<(class Generated651.IBase1`1<class Generated651.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated651.IBase1`1<class Generated651.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated651.IBase1`1<class Generated651.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated651.IBase1`1<class Generated651.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase1.B<(class Generated651.IBase1`1<class Generated651.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase1.B<(class Generated651.IBase1`1<class Generated651.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated651.IBase1`1<class Generated651.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated651.IBase1`1<class Generated651.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated651.IBase1`1<class Generated651.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.T.T<T0,T1,(class Generated651.IBase2`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.T.T<T0,T1,(class Generated651.IBase2`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated651.IBase2`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.A.T<T1,(class Generated651.IBase2`2<class Generated651.BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.A.T<T1,(class Generated651.IBase2`2<class Generated651.BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated651.IBase2`2<class Generated651.BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.A.A<(class Generated651.IBase2`2<class Generated651.BaseClass0,class Generated651.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.A.A<(class Generated651.IBase2`2<class Generated651.BaseClass0,class Generated651.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated651.IBase2`2<class Generated651.BaseClass0,class Generated651.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.A.B<(class Generated651.IBase2`2<class Generated651.BaseClass0,class Generated651.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.A.B<(class Generated651.IBase2`2<class Generated651.BaseClass0,class Generated651.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated651.IBase2`2<class Generated651.BaseClass0,class Generated651.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.B.T<T1,(class Generated651.IBase2`2<class Generated651.BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.B.T<T1,(class Generated651.IBase2`2<class Generated651.BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated651.IBase2`2<class Generated651.BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.B.A<(class Generated651.IBase2`2<class Generated651.BaseClass1,class Generated651.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.B.A<(class Generated651.IBase2`2<class Generated651.BaseClass1,class Generated651.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated651.IBase2`2<class Generated651.BaseClass1,class Generated651.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.B.B<(class Generated651.IBase2`2<class Generated651.BaseClass1,class Generated651.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.B.B<(class Generated651.IBase2`2<class Generated651.BaseClass1,class Generated651.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated651.IBase2`2<class Generated651.BaseClass1,class Generated651.BaseClass1>::Method7<object>()
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
		newobj instance void class Generated651.G3_C1123`1<class Generated651.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>
		callvirt instance string class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>::ClassMethod1345<object>()
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1> on type class Generated651.G3_C1123`1<class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>
		callvirt instance string class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>::ClassMethod1344<object>()
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()"
		ldstr "class G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1> on type class Generated651.G3_C1123`1<class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>
		callvirt instance string class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>::Method6<object>()
		ldstr "G2_C148::Method6.6162<System.Object>()"
		ldstr "class G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1> on type class Generated651.G3_C1123`1<class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>
		callvirt instance string class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>::Method5()
		ldstr "G1_C11::Method5.4858()"
		ldstr "class G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1> on type class Generated651.G3_C1123`1<class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>
		callvirt instance string class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>::Method4()
		ldstr "G2_C148::Method4.6160()"
		ldstr "class G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1> on type class Generated651.G3_C1123`1<class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>
		callvirt instance string class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>::Method7<object>()
		ldstr "G2_C148::Method7.6164<System.Object>()"
		ldstr "class G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1> on type class Generated651.G3_C1123`1<class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated651.IBase2`2<class Generated651.BaseClass1,class Generated651.BaseClass1>::Method7<object>()
		ldstr "G2_C148::Method7.6164<System.Object>()"
		ldstr "class IBase2`2<class Generated651.BaseClass1,class Generated651.BaseClass1> on type class Generated651.G3_C1123`1<class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated651.IBase1`1<class Generated651.BaseClass0>::Method4()
		ldstr "G2_C148::Method4.6160()"
		ldstr "class IBase1`1<class Generated651.BaseClass0> on type class Generated651.G3_C1123`1<class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated651.IBase1`1<class Generated651.BaseClass0>::Method5()
		ldstr "G2_C148::Method5.6161()"
		ldstr "class IBase1`1<class Generated651.BaseClass0> on type class Generated651.G3_C1123`1<class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated651.IBase1`1<class Generated651.BaseClass0>::Method6<object>()
		ldstr "G2_C148::Method6.MI.6163<System.Object>()"
		ldstr "class IBase1`1<class Generated651.BaseClass0> on type class Generated651.G3_C1123`1<class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated651.IBase2`2<class Generated651.BaseClass0,class Generated651.BaseClass1>::Method7<object>()
		ldstr "G2_C148::Method7.6164<System.Object>()"
		ldstr "class IBase2`2<class Generated651.BaseClass0,class Generated651.BaseClass1> on type class Generated651.G3_C1123`1<class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated651.G2_C148`1<class Generated651.BaseClass1>
		callvirt instance string class Generated651.G2_C148`1<class Generated651.BaseClass1>::ClassMethod1619()
		ldstr "G3_C1123::ClassMethod1619.MI.14456()"
		ldstr "class G2_C148`1<class Generated651.BaseClass1> on type class Generated651.G3_C1123`1<class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated651.G2_C148`1<class Generated651.BaseClass1>
		callvirt instance string class Generated651.G2_C148`1<class Generated651.BaseClass1>::Method7<object>()
		ldstr "G2_C148::Method7.6164<System.Object>()"
		ldstr "class G2_C148`1<class Generated651.BaseClass1> on type class Generated651.G3_C1123`1<class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated651.G2_C148`1<class Generated651.BaseClass1>
		callvirt instance string class Generated651.G2_C148`1<class Generated651.BaseClass1>::Method6<object>()
		ldstr "G2_C148::Method6.6162<System.Object>()"
		ldstr "class G2_C148`1<class Generated651.BaseClass1> on type class Generated651.G3_C1123`1<class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated651.G2_C148`1<class Generated651.BaseClass1>
		callvirt instance string class Generated651.G2_C148`1<class Generated651.BaseClass1>::Method5()
		ldstr "G2_C148::Method5.6161()"
		ldstr "class G2_C148`1<class Generated651.BaseClass1> on type class Generated651.G3_C1123`1<class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated651.G2_C148`1<class Generated651.BaseClass1>
		callvirt instance string class Generated651.G2_C148`1<class Generated651.BaseClass1>::Method4()
		ldstr "G2_C148::Method4.6160()"
		ldstr "class G2_C148`1<class Generated651.BaseClass1> on type class Generated651.G3_C1123`1<class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated651.G2_C148`1<class Generated651.BaseClass1>
		callvirt instance string class Generated651.G2_C148`1<class Generated651.BaseClass1>::ClassMethod1345<object>()
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G2_C148`1<class Generated651.BaseClass1> on type class Generated651.G3_C1123`1<class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated651.G2_C148`1<class Generated651.BaseClass1>
		callvirt instance string class Generated651.G2_C148`1<class Generated651.BaseClass1>::ClassMethod1344<object>()
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()"
		ldstr "class G2_C148`1<class Generated651.BaseClass1> on type class Generated651.G3_C1123`1<class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated651.IBase2`2<class Generated651.BaseClass1,class Generated651.BaseClass0>::Method7<object>()
		ldstr "G2_C148::Method7.6164<System.Object>()"
		ldstr "class IBase2`2<class Generated651.BaseClass1,class Generated651.BaseClass0> on type class Generated651.G3_C1123`1<class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated651.IBase2`2<class Generated651.BaseClass0,class Generated651.BaseClass0>::Method7<object>()
		ldstr "G2_C148::Method7.6164<System.Object>()"
		ldstr "class IBase2`2<class Generated651.BaseClass0,class Generated651.BaseClass0> on type class Generated651.G3_C1123`1<class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated651.G3_C1123`1<class Generated651.BaseClass0>
		callvirt instance string class Generated651.G3_C1123`1<class Generated651.BaseClass0>::ClassMethod3643<object>()
		ldstr "G3_C1123::ClassMethod3643.14455<System.Object>()"
		ldstr "class G3_C1123`1<class Generated651.BaseClass0> on type class Generated651.G3_C1123`1<class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated651.G3_C1123`1<class Generated651.BaseClass0>
		callvirt instance string class Generated651.G3_C1123`1<class Generated651.BaseClass0>::Method3<object>()
		ldstr "G3_C1123::Method3.14453<System.Object>()"
		ldstr "class G3_C1123`1<class Generated651.BaseClass0> on type class Generated651.G3_C1123`1<class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated651.G3_C1123`1<class Generated651.BaseClass0>
		callvirt instance string class Generated651.G3_C1123`1<class Generated651.BaseClass0>::Method2<object>()
		ldstr "G3_C1123::Method2.14451<System.Object>()"
		ldstr "class G3_C1123`1<class Generated651.BaseClass0> on type class Generated651.G3_C1123`1<class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated651.G3_C1123`1<class Generated651.BaseClass0>
		callvirt instance string class Generated651.G3_C1123`1<class Generated651.BaseClass0>::Method1()
		ldstr "G3_C1123::Method1.14450()"
		ldstr "class G3_C1123`1<class Generated651.BaseClass0> on type class Generated651.G3_C1123`1<class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated651.G3_C1123`1<class Generated651.BaseClass0>
		callvirt instance string class Generated651.G3_C1123`1<class Generated651.BaseClass0>::Method0()
		ldstr "G3_C1123::Method0.14448()"
		ldstr "class G3_C1123`1<class Generated651.BaseClass0> on type class Generated651.G3_C1123`1<class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated651.G3_C1123`1<class Generated651.BaseClass0>
		callvirt instance string class Generated651.G3_C1123`1<class Generated651.BaseClass0>::ClassMethod1619()
		ldstr "G3_C1123::ClassMethod1619.MI.14456()"
		ldstr "class G3_C1123`1<class Generated651.BaseClass0> on type class Generated651.G3_C1123`1<class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated651.G3_C1123`1<class Generated651.BaseClass0>
		callvirt instance string class Generated651.G3_C1123`1<class Generated651.BaseClass0>::Method7<object>()
		ldstr "G2_C148::Method7.6164<System.Object>()"
		ldstr "class G3_C1123`1<class Generated651.BaseClass0> on type class Generated651.G3_C1123`1<class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated651.G3_C1123`1<class Generated651.BaseClass0>
		callvirt instance string class Generated651.G3_C1123`1<class Generated651.BaseClass0>::Method6<object>()
		ldstr "G2_C148::Method6.6162<System.Object>()"
		ldstr "class G3_C1123`1<class Generated651.BaseClass0> on type class Generated651.G3_C1123`1<class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated651.G3_C1123`1<class Generated651.BaseClass0>
		callvirt instance string class Generated651.G3_C1123`1<class Generated651.BaseClass0>::Method5()
		ldstr "G2_C148::Method5.6161()"
		ldstr "class G3_C1123`1<class Generated651.BaseClass0> on type class Generated651.G3_C1123`1<class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated651.G3_C1123`1<class Generated651.BaseClass0>
		callvirt instance string class Generated651.G3_C1123`1<class Generated651.BaseClass0>::Method4()
		ldstr "G2_C148::Method4.6160()"
		ldstr "class G3_C1123`1<class Generated651.BaseClass0> on type class Generated651.G3_C1123`1<class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated651.G3_C1123`1<class Generated651.BaseClass0>
		callvirt instance string class Generated651.G3_C1123`1<class Generated651.BaseClass0>::ClassMethod1345<object>()
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G3_C1123`1<class Generated651.BaseClass0> on type class Generated651.G3_C1123`1<class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated651.G3_C1123`1<class Generated651.BaseClass0>
		callvirt instance string class Generated651.G3_C1123`1<class Generated651.BaseClass0>::ClassMethod1344<object>()
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()"
		ldstr "class G3_C1123`1<class Generated651.BaseClass0> on type class Generated651.G3_C1123`1<class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string Generated651.IBase0::Method0()
		ldstr "G3_C1123::Method0.MI.14449()"
		ldstr "Generated651.IBase0 on type class Generated651.G3_C1123`1<class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated651.IBase0::Method1()
		ldstr "G3_C1123::Method1.14450()"
		ldstr "Generated651.IBase0 on type class Generated651.G3_C1123`1<class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated651.IBase0::Method2<object>()
		ldstr "G3_C1123::Method2.MI.14452<System.Object>()"
		ldstr "Generated651.IBase0 on type class Generated651.G3_C1123`1<class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated651.IBase0::Method3<object>()
		ldstr "G3_C1123::Method3.MI.14454<System.Object>()"
		ldstr "Generated651.IBase0 on type class Generated651.G3_C1123`1<class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated651.G3_C1123`1<class Generated651.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>
		callvirt instance string class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>::ClassMethod1345<object>()
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1> on type class Generated651.G3_C1123`1<class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>
		callvirt instance string class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>::ClassMethod1344<object>()
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()"
		ldstr "class G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1> on type class Generated651.G3_C1123`1<class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>
		callvirt instance string class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>::Method6<object>()
		ldstr "G2_C148::Method6.6162<System.Object>()"
		ldstr "class G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1> on type class Generated651.G3_C1123`1<class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>
		callvirt instance string class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>::Method5()
		ldstr "G1_C11::Method5.4858()"
		ldstr "class G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1> on type class Generated651.G3_C1123`1<class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>
		callvirt instance string class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>::Method4()
		ldstr "G2_C148::Method4.6160()"
		ldstr "class G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1> on type class Generated651.G3_C1123`1<class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>
		callvirt instance string class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>::Method7<object>()
		ldstr "G2_C148::Method7.6164<System.Object>()"
		ldstr "class G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1> on type class Generated651.G3_C1123`1<class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated651.IBase2`2<class Generated651.BaseClass1,class Generated651.BaseClass1>::Method7<object>()
		ldstr "G2_C148::Method7.6164<System.Object>()"
		ldstr "class IBase2`2<class Generated651.BaseClass1,class Generated651.BaseClass1> on type class Generated651.G3_C1123`1<class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated651.IBase1`1<class Generated651.BaseClass0>::Method4()
		ldstr "G2_C148::Method4.6160()"
		ldstr "class IBase1`1<class Generated651.BaseClass0> on type class Generated651.G3_C1123`1<class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated651.IBase1`1<class Generated651.BaseClass0>::Method5()
		ldstr "G2_C148::Method5.6161()"
		ldstr "class IBase1`1<class Generated651.BaseClass0> on type class Generated651.G3_C1123`1<class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated651.IBase1`1<class Generated651.BaseClass0>::Method6<object>()
		ldstr "G2_C148::Method6.MI.6163<System.Object>()"
		ldstr "class IBase1`1<class Generated651.BaseClass0> on type class Generated651.G3_C1123`1<class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated651.IBase2`2<class Generated651.BaseClass0,class Generated651.BaseClass1>::Method7<object>()
		ldstr "G2_C148::Method7.6164<System.Object>()"
		ldstr "class IBase2`2<class Generated651.BaseClass0,class Generated651.BaseClass1> on type class Generated651.G3_C1123`1<class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated651.G2_C148`1<class Generated651.BaseClass1>
		callvirt instance string class Generated651.G2_C148`1<class Generated651.BaseClass1>::ClassMethod1619()
		ldstr "G3_C1123::ClassMethod1619.MI.14456()"
		ldstr "class G2_C148`1<class Generated651.BaseClass1> on type class Generated651.G3_C1123`1<class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated651.G2_C148`1<class Generated651.BaseClass1>
		callvirt instance string class Generated651.G2_C148`1<class Generated651.BaseClass1>::Method7<object>()
		ldstr "G2_C148::Method7.6164<System.Object>()"
		ldstr "class G2_C148`1<class Generated651.BaseClass1> on type class Generated651.G3_C1123`1<class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated651.G2_C148`1<class Generated651.BaseClass1>
		callvirt instance string class Generated651.G2_C148`1<class Generated651.BaseClass1>::Method6<object>()
		ldstr "G2_C148::Method6.6162<System.Object>()"
		ldstr "class G2_C148`1<class Generated651.BaseClass1> on type class Generated651.G3_C1123`1<class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated651.G2_C148`1<class Generated651.BaseClass1>
		callvirt instance string class Generated651.G2_C148`1<class Generated651.BaseClass1>::Method5()
		ldstr "G2_C148::Method5.6161()"
		ldstr "class G2_C148`1<class Generated651.BaseClass1> on type class Generated651.G3_C1123`1<class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated651.G2_C148`1<class Generated651.BaseClass1>
		callvirt instance string class Generated651.G2_C148`1<class Generated651.BaseClass1>::Method4()
		ldstr "G2_C148::Method4.6160()"
		ldstr "class G2_C148`1<class Generated651.BaseClass1> on type class Generated651.G3_C1123`1<class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated651.G2_C148`1<class Generated651.BaseClass1>
		callvirt instance string class Generated651.G2_C148`1<class Generated651.BaseClass1>::ClassMethod1345<object>()
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G2_C148`1<class Generated651.BaseClass1> on type class Generated651.G3_C1123`1<class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated651.G2_C148`1<class Generated651.BaseClass1>
		callvirt instance string class Generated651.G2_C148`1<class Generated651.BaseClass1>::ClassMethod1344<object>()
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()"
		ldstr "class G2_C148`1<class Generated651.BaseClass1> on type class Generated651.G3_C1123`1<class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated651.IBase2`2<class Generated651.BaseClass1,class Generated651.BaseClass0>::Method7<object>()
		ldstr "G2_C148::Method7.6164<System.Object>()"
		ldstr "class IBase2`2<class Generated651.BaseClass1,class Generated651.BaseClass0> on type class Generated651.G3_C1123`1<class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated651.IBase2`2<class Generated651.BaseClass0,class Generated651.BaseClass0>::Method7<object>()
		ldstr "G2_C148::Method7.6164<System.Object>()"
		ldstr "class IBase2`2<class Generated651.BaseClass0,class Generated651.BaseClass0> on type class Generated651.G3_C1123`1<class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated651.G3_C1123`1<class Generated651.BaseClass1>
		callvirt instance string class Generated651.G3_C1123`1<class Generated651.BaseClass1>::ClassMethod3643<object>()
		ldstr "G3_C1123::ClassMethod3643.14455<System.Object>()"
		ldstr "class G3_C1123`1<class Generated651.BaseClass1> on type class Generated651.G3_C1123`1<class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated651.G3_C1123`1<class Generated651.BaseClass1>
		callvirt instance string class Generated651.G3_C1123`1<class Generated651.BaseClass1>::Method3<object>()
		ldstr "G3_C1123::Method3.14453<System.Object>()"
		ldstr "class G3_C1123`1<class Generated651.BaseClass1> on type class Generated651.G3_C1123`1<class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated651.G3_C1123`1<class Generated651.BaseClass1>
		callvirt instance string class Generated651.G3_C1123`1<class Generated651.BaseClass1>::Method2<object>()
		ldstr "G3_C1123::Method2.14451<System.Object>()"
		ldstr "class G3_C1123`1<class Generated651.BaseClass1> on type class Generated651.G3_C1123`1<class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated651.G3_C1123`1<class Generated651.BaseClass1>
		callvirt instance string class Generated651.G3_C1123`1<class Generated651.BaseClass1>::Method1()
		ldstr "G3_C1123::Method1.14450()"
		ldstr "class G3_C1123`1<class Generated651.BaseClass1> on type class Generated651.G3_C1123`1<class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated651.G3_C1123`1<class Generated651.BaseClass1>
		callvirt instance string class Generated651.G3_C1123`1<class Generated651.BaseClass1>::Method0()
		ldstr "G3_C1123::Method0.14448()"
		ldstr "class G3_C1123`1<class Generated651.BaseClass1> on type class Generated651.G3_C1123`1<class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated651.G3_C1123`1<class Generated651.BaseClass1>
		callvirt instance string class Generated651.G3_C1123`1<class Generated651.BaseClass1>::ClassMethod1619()
		ldstr "G3_C1123::ClassMethod1619.MI.14456()"
		ldstr "class G3_C1123`1<class Generated651.BaseClass1> on type class Generated651.G3_C1123`1<class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated651.G3_C1123`1<class Generated651.BaseClass1>
		callvirt instance string class Generated651.G3_C1123`1<class Generated651.BaseClass1>::Method7<object>()
		ldstr "G2_C148::Method7.6164<System.Object>()"
		ldstr "class G3_C1123`1<class Generated651.BaseClass1> on type class Generated651.G3_C1123`1<class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated651.G3_C1123`1<class Generated651.BaseClass1>
		callvirt instance string class Generated651.G3_C1123`1<class Generated651.BaseClass1>::Method6<object>()
		ldstr "G2_C148::Method6.6162<System.Object>()"
		ldstr "class G3_C1123`1<class Generated651.BaseClass1> on type class Generated651.G3_C1123`1<class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated651.G3_C1123`1<class Generated651.BaseClass1>
		callvirt instance string class Generated651.G3_C1123`1<class Generated651.BaseClass1>::Method5()
		ldstr "G2_C148::Method5.6161()"
		ldstr "class G3_C1123`1<class Generated651.BaseClass1> on type class Generated651.G3_C1123`1<class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated651.G3_C1123`1<class Generated651.BaseClass1>
		callvirt instance string class Generated651.G3_C1123`1<class Generated651.BaseClass1>::Method4()
		ldstr "G2_C148::Method4.6160()"
		ldstr "class G3_C1123`1<class Generated651.BaseClass1> on type class Generated651.G3_C1123`1<class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated651.G3_C1123`1<class Generated651.BaseClass1>
		callvirt instance string class Generated651.G3_C1123`1<class Generated651.BaseClass1>::ClassMethod1345<object>()
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G3_C1123`1<class Generated651.BaseClass1> on type class Generated651.G3_C1123`1<class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated651.G3_C1123`1<class Generated651.BaseClass1>
		callvirt instance string class Generated651.G3_C1123`1<class Generated651.BaseClass1>::ClassMethod1344<object>()
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()"
		ldstr "class G3_C1123`1<class Generated651.BaseClass1> on type class Generated651.G3_C1123`1<class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string Generated651.IBase0::Method0()
		ldstr "G3_C1123::Method0.MI.14449()"
		ldstr "Generated651.IBase0 on type class Generated651.G3_C1123`1<class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated651.IBase0::Method1()
		ldstr "G3_C1123::Method1.14450()"
		ldstr "Generated651.IBase0 on type class Generated651.G3_C1123`1<class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated651.IBase0::Method2<object>()
		ldstr "G3_C1123::Method2.MI.14452<System.Object>()"
		ldstr "Generated651.IBase0 on type class Generated651.G3_C1123`1<class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated651.IBase0::Method3<object>()
		ldstr "G3_C1123::Method3.MI.14454<System.Object>()"
		ldstr "Generated651.IBase0 on type class Generated651.G3_C1123`1<class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated651.G2_C148`1<class Generated651.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0>
		callvirt instance string class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0>::ClassMethod1345<object>()
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0> on type class Generated651.G2_C148`1<class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0>
		callvirt instance string class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0>::ClassMethod1344<object>()
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()"
		ldstr "class G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0> on type class Generated651.G2_C148`1<class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0>
		callvirt instance string class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0>::Method6<object>()
		ldstr "G2_C148::Method6.6162<System.Object>()"
		ldstr "class G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0> on type class Generated651.G2_C148`1<class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0>
		callvirt instance string class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0>::Method5()
		ldstr "G1_C11::Method5.4858()"
		ldstr "class G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0> on type class Generated651.G2_C148`1<class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0>
		callvirt instance string class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0>::Method4()
		ldstr "G2_C148::Method4.6160()"
		ldstr "class G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0> on type class Generated651.G2_C148`1<class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0>
		callvirt instance string class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0>::Method7<object>()
		ldstr "G2_C148::Method7.6164<System.Object>()"
		ldstr "class G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0> on type class Generated651.G2_C148`1<class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated651.IBase2`2<class Generated651.BaseClass1,class Generated651.BaseClass0>::Method7<object>()
		ldstr "G2_C148::Method7.6164<System.Object>()"
		ldstr "class IBase2`2<class Generated651.BaseClass1,class Generated651.BaseClass0> on type class Generated651.G2_C148`1<class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated651.IBase1`1<class Generated651.BaseClass0>::Method4()
		ldstr "G2_C148::Method4.6160()"
		ldstr "class IBase1`1<class Generated651.BaseClass0> on type class Generated651.G2_C148`1<class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated651.IBase1`1<class Generated651.BaseClass0>::Method5()
		ldstr "G2_C148::Method5.6161()"
		ldstr "class IBase1`1<class Generated651.BaseClass0> on type class Generated651.G2_C148`1<class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated651.IBase1`1<class Generated651.BaseClass0>::Method6<object>()
		ldstr "G2_C148::Method6.MI.6163<System.Object>()"
		ldstr "class IBase1`1<class Generated651.BaseClass0> on type class Generated651.G2_C148`1<class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated651.IBase2`2<class Generated651.BaseClass0,class Generated651.BaseClass0>::Method7<object>()
		ldstr "G2_C148::Method7.6164<System.Object>()"
		ldstr "class IBase2`2<class Generated651.BaseClass0,class Generated651.BaseClass0> on type class Generated651.G2_C148`1<class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated651.IBase2`2<class Generated651.BaseClass0,class Generated651.BaseClass1>::Method7<object>()
		ldstr "G2_C148::Method7.6164<System.Object>()"
		ldstr "class IBase2`2<class Generated651.BaseClass0,class Generated651.BaseClass1> on type class Generated651.G2_C148`1<class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated651.IBase2`2<class Generated651.BaseClass1,class Generated651.BaseClass1>::Method7<object>()
		ldstr "G2_C148::Method7.6164<System.Object>()"
		ldstr "class IBase2`2<class Generated651.BaseClass1,class Generated651.BaseClass1> on type class Generated651.G2_C148`1<class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated651.G2_C148`1<class Generated651.BaseClass0>
		callvirt instance string class Generated651.G2_C148`1<class Generated651.BaseClass0>::ClassMethod1619()
		ldstr "G2_C148::ClassMethod1619.6165()"
		ldstr "class G2_C148`1<class Generated651.BaseClass0> on type class Generated651.G2_C148`1<class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated651.G2_C148`1<class Generated651.BaseClass0>
		callvirt instance string class Generated651.G2_C148`1<class Generated651.BaseClass0>::Method7<object>()
		ldstr "G2_C148::Method7.6164<System.Object>()"
		ldstr "class G2_C148`1<class Generated651.BaseClass0> on type class Generated651.G2_C148`1<class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated651.G2_C148`1<class Generated651.BaseClass0>
		callvirt instance string class Generated651.G2_C148`1<class Generated651.BaseClass0>::Method6<object>()
		ldstr "G2_C148::Method6.6162<System.Object>()"
		ldstr "class G2_C148`1<class Generated651.BaseClass0> on type class Generated651.G2_C148`1<class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated651.G2_C148`1<class Generated651.BaseClass0>
		callvirt instance string class Generated651.G2_C148`1<class Generated651.BaseClass0>::Method5()
		ldstr "G2_C148::Method5.6161()"
		ldstr "class G2_C148`1<class Generated651.BaseClass0> on type class Generated651.G2_C148`1<class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated651.G2_C148`1<class Generated651.BaseClass0>
		callvirt instance string class Generated651.G2_C148`1<class Generated651.BaseClass0>::Method4()
		ldstr "G2_C148::Method4.6160()"
		ldstr "class G2_C148`1<class Generated651.BaseClass0> on type class Generated651.G2_C148`1<class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated651.G2_C148`1<class Generated651.BaseClass0>
		callvirt instance string class Generated651.G2_C148`1<class Generated651.BaseClass0>::ClassMethod1345<object>()
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G2_C148`1<class Generated651.BaseClass0> on type class Generated651.G2_C148`1<class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated651.G2_C148`1<class Generated651.BaseClass0>
		callvirt instance string class Generated651.G2_C148`1<class Generated651.BaseClass0>::ClassMethod1344<object>()
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()"
		ldstr "class G2_C148`1<class Generated651.BaseClass0> on type class Generated651.G2_C148`1<class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated651.G2_C148`1<class Generated651.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>
		callvirt instance string class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>::ClassMethod1345<object>()
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1> on type class Generated651.G2_C148`1<class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>
		callvirt instance string class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>::ClassMethod1344<object>()
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()"
		ldstr "class G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1> on type class Generated651.G2_C148`1<class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>
		callvirt instance string class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>::Method6<object>()
		ldstr "G2_C148::Method6.6162<System.Object>()"
		ldstr "class G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1> on type class Generated651.G2_C148`1<class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>
		callvirt instance string class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>::Method5()
		ldstr "G1_C11::Method5.4858()"
		ldstr "class G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1> on type class Generated651.G2_C148`1<class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>
		callvirt instance string class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>::Method4()
		ldstr "G2_C148::Method4.6160()"
		ldstr "class G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1> on type class Generated651.G2_C148`1<class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>
		callvirt instance string class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>::Method7<object>()
		ldstr "G2_C148::Method7.6164<System.Object>()"
		ldstr "class G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1> on type class Generated651.G2_C148`1<class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated651.IBase2`2<class Generated651.BaseClass1,class Generated651.BaseClass1>::Method7<object>()
		ldstr "G2_C148::Method7.6164<System.Object>()"
		ldstr "class IBase2`2<class Generated651.BaseClass1,class Generated651.BaseClass1> on type class Generated651.G2_C148`1<class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated651.IBase1`1<class Generated651.BaseClass0>::Method4()
		ldstr "G2_C148::Method4.6160()"
		ldstr "class IBase1`1<class Generated651.BaseClass0> on type class Generated651.G2_C148`1<class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated651.IBase1`1<class Generated651.BaseClass0>::Method5()
		ldstr "G2_C148::Method5.6161()"
		ldstr "class IBase1`1<class Generated651.BaseClass0> on type class Generated651.G2_C148`1<class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated651.IBase1`1<class Generated651.BaseClass0>::Method6<object>()
		ldstr "G2_C148::Method6.MI.6163<System.Object>()"
		ldstr "class IBase1`1<class Generated651.BaseClass0> on type class Generated651.G2_C148`1<class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated651.IBase2`2<class Generated651.BaseClass0,class Generated651.BaseClass1>::Method7<object>()
		ldstr "G2_C148::Method7.6164<System.Object>()"
		ldstr "class IBase2`2<class Generated651.BaseClass0,class Generated651.BaseClass1> on type class Generated651.G2_C148`1<class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated651.G2_C148`1<class Generated651.BaseClass1>
		callvirt instance string class Generated651.G2_C148`1<class Generated651.BaseClass1>::ClassMethod1619()
		ldstr "G2_C148::ClassMethod1619.6165()"
		ldstr "class G2_C148`1<class Generated651.BaseClass1> on type class Generated651.G2_C148`1<class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated651.G2_C148`1<class Generated651.BaseClass1>
		callvirt instance string class Generated651.G2_C148`1<class Generated651.BaseClass1>::Method7<object>()
		ldstr "G2_C148::Method7.6164<System.Object>()"
		ldstr "class G2_C148`1<class Generated651.BaseClass1> on type class Generated651.G2_C148`1<class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated651.G2_C148`1<class Generated651.BaseClass1>
		callvirt instance string class Generated651.G2_C148`1<class Generated651.BaseClass1>::Method6<object>()
		ldstr "G2_C148::Method6.6162<System.Object>()"
		ldstr "class G2_C148`1<class Generated651.BaseClass1> on type class Generated651.G2_C148`1<class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated651.G2_C148`1<class Generated651.BaseClass1>
		callvirt instance string class Generated651.G2_C148`1<class Generated651.BaseClass1>::Method5()
		ldstr "G2_C148::Method5.6161()"
		ldstr "class G2_C148`1<class Generated651.BaseClass1> on type class Generated651.G2_C148`1<class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated651.G2_C148`1<class Generated651.BaseClass1>
		callvirt instance string class Generated651.G2_C148`1<class Generated651.BaseClass1>::Method4()
		ldstr "G2_C148::Method4.6160()"
		ldstr "class G2_C148`1<class Generated651.BaseClass1> on type class Generated651.G2_C148`1<class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated651.G2_C148`1<class Generated651.BaseClass1>
		callvirt instance string class Generated651.G2_C148`1<class Generated651.BaseClass1>::ClassMethod1345<object>()
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G2_C148`1<class Generated651.BaseClass1> on type class Generated651.G2_C148`1<class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated651.G2_C148`1<class Generated651.BaseClass1>
		callvirt instance string class Generated651.G2_C148`1<class Generated651.BaseClass1>::ClassMethod1344<object>()
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()"
		ldstr "class G2_C148`1<class Generated651.BaseClass1> on type class Generated651.G2_C148`1<class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated651.IBase2`2<class Generated651.BaseClass1,class Generated651.BaseClass0>::Method7<object>()
		ldstr "G2_C148::Method7.6164<System.Object>()"
		ldstr "class IBase2`2<class Generated651.BaseClass1,class Generated651.BaseClass0> on type class Generated651.G2_C148`1<class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated651.IBase2`2<class Generated651.BaseClass0,class Generated651.BaseClass0>::Method7<object>()
		ldstr "G2_C148::Method7.6164<System.Object>()"
		ldstr "class IBase2`2<class Generated651.BaseClass0,class Generated651.BaseClass0> on type class Generated651.G2_C148`1<class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0>
		callvirt instance string class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0>::ClassMethod1345<object>()
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0> on type class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0>
		callvirt instance string class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0>::ClassMethod1344<object>()
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()"
		ldstr "class G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0> on type class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0>
		callvirt instance string class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0>::Method6<object>()
		ldstr "G1_C11::Method6.4859<System.Object>()"
		ldstr "class G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0> on type class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0>
		callvirt instance string class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0>::Method5()
		ldstr "G1_C11::Method5.4858()"
		ldstr "class G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0> on type class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0>
		callvirt instance string class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0>::Method4()
		ldstr "G1_C11::Method4.4856()"
		ldstr "class G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0> on type class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0>
		callvirt instance string class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0>::Method7<object>()
		ldstr "G1_C11::Method7.4854<System.Object>()"
		ldstr "class G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0> on type class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated651.IBase2`2<class Generated651.BaseClass1,class Generated651.BaseClass0>::Method7<object>()
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated651.BaseClass1,class Generated651.BaseClass0> on type class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated651.IBase1`1<class Generated651.BaseClass0>::Method4()
		ldstr "G1_C11::Method4.MI.4857()"
		ldstr "class IBase1`1<class Generated651.BaseClass0> on type class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated651.IBase1`1<class Generated651.BaseClass0>::Method5()
		ldstr "G1_C11::Method5.4858()"
		ldstr "class IBase1`1<class Generated651.BaseClass0> on type class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated651.IBase1`1<class Generated651.BaseClass0>::Method6<object>()
		ldstr "G1_C11::Method6.MI.4860<System.Object>()"
		ldstr "class IBase1`1<class Generated651.BaseClass0> on type class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated651.IBase2`2<class Generated651.BaseClass0,class Generated651.BaseClass0>::Method7<object>()
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated651.BaseClass0,class Generated651.BaseClass0> on type class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated651.IBase2`2<class Generated651.BaseClass0,class Generated651.BaseClass1>::Method7<object>()
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated651.BaseClass0,class Generated651.BaseClass1> on type class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated651.IBase2`2<class Generated651.BaseClass1,class Generated651.BaseClass1>::Method7<object>()
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated651.BaseClass1,class Generated651.BaseClass1> on type class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>
		callvirt instance string class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>::ClassMethod1345<object>()
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1> on type class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>
		callvirt instance string class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>::ClassMethod1344<object>()
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()"
		ldstr "class G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1> on type class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>
		callvirt instance string class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>::Method6<object>()
		ldstr "G1_C11::Method6.4859<System.Object>()"
		ldstr "class G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1> on type class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>
		callvirt instance string class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>::Method5()
		ldstr "G1_C11::Method5.4858()"
		ldstr "class G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1> on type class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>
		callvirt instance string class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>::Method4()
		ldstr "G1_C11::Method4.4856()"
		ldstr "class G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1> on type class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>
		callvirt instance string class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>::Method7<object>()
		ldstr "G1_C11::Method7.4854<System.Object>()"
		ldstr "class G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1> on type class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated651.IBase2`2<class Generated651.BaseClass1,class Generated651.BaseClass1>::Method7<object>()
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated651.BaseClass1,class Generated651.BaseClass1> on type class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated651.IBase1`1<class Generated651.BaseClass0>::Method4()
		ldstr "G1_C11::Method4.MI.4857()"
		ldstr "class IBase1`1<class Generated651.BaseClass0> on type class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated651.IBase1`1<class Generated651.BaseClass0>::Method5()
		ldstr "G1_C11::Method5.4858()"
		ldstr "class IBase1`1<class Generated651.BaseClass0> on type class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated651.IBase1`1<class Generated651.BaseClass0>::Method6<object>()
		ldstr "G1_C11::Method6.MI.4860<System.Object>()"
		ldstr "class IBase1`1<class Generated651.BaseClass0> on type class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated651.IBase2`2<class Generated651.BaseClass0,class Generated651.BaseClass1>::Method7<object>()
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated651.BaseClass0,class Generated651.BaseClass1> on type class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass0>
		callvirt instance string class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass0>::ClassMethod1345<object>()
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass0> on type class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass0>
		callvirt instance string class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass0>::ClassMethod1344<object>()
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()"
		ldstr "class G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass0> on type class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass0>
		callvirt instance string class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass0>::Method6<object>()
		ldstr "G1_C11::Method6.4859<System.Object>()"
		ldstr "class G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass0> on type class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass0>
		callvirt instance string class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass0>::Method5()
		ldstr "G1_C11::Method5.4858()"
		ldstr "class G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass0> on type class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass0>
		callvirt instance string class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass0>::Method4()
		ldstr "G1_C11::Method4.4856()"
		ldstr "class G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass0> on type class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass0>
		callvirt instance string class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass0>::Method7<object>()
		ldstr "G1_C11::Method7.4854<System.Object>()"
		ldstr "class G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass0> on type class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated651.IBase2`2<class Generated651.BaseClass1,class Generated651.BaseClass0>::Method7<object>()
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated651.BaseClass1,class Generated651.BaseClass0> on type class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated651.IBase1`1<class Generated651.BaseClass0>::Method4()
		ldstr "G1_C11::Method4.MI.4857()"
		ldstr "class IBase1`1<class Generated651.BaseClass0> on type class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated651.IBase1`1<class Generated651.BaseClass0>::Method5()
		ldstr "G1_C11::Method5.4858()"
		ldstr "class IBase1`1<class Generated651.BaseClass0> on type class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated651.IBase1`1<class Generated651.BaseClass0>::Method6<object>()
		ldstr "G1_C11::Method6.MI.4860<System.Object>()"
		ldstr "class IBase1`1<class Generated651.BaseClass0> on type class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated651.IBase2`2<class Generated651.BaseClass0,class Generated651.BaseClass0>::Method7<object>()
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated651.BaseClass0,class Generated651.BaseClass0> on type class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated651.IBase2`2<class Generated651.BaseClass0,class Generated651.BaseClass1>::Method7<object>()
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated651.BaseClass0,class Generated651.BaseClass1> on type class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated651.IBase2`2<class Generated651.BaseClass1,class Generated651.BaseClass1>::Method7<object>()
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated651.BaseClass1,class Generated651.BaseClass1> on type class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass1>
		callvirt instance string class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass1>::ClassMethod1345<object>()
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass1> on type class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass1>
		callvirt instance string class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass1>::ClassMethod1344<object>()
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()"
		ldstr "class G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass1> on type class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass1>
		callvirt instance string class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass1>::Method6<object>()
		ldstr "G1_C11::Method6.4859<System.Object>()"
		ldstr "class G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass1> on type class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass1>
		callvirt instance string class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass1>::Method5()
		ldstr "G1_C11::Method5.4858()"
		ldstr "class G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass1> on type class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass1>
		callvirt instance string class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass1>::Method4()
		ldstr "G1_C11::Method4.4856()"
		ldstr "class G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass1> on type class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass1>
		callvirt instance string class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass1>::Method7<object>()
		ldstr "G1_C11::Method7.4854<System.Object>()"
		ldstr "class G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass1> on type class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated651.IBase2`2<class Generated651.BaseClass1,class Generated651.BaseClass1>::Method7<object>()
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated651.BaseClass1,class Generated651.BaseClass1> on type class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated651.IBase1`1<class Generated651.BaseClass0>::Method4()
		ldstr "G1_C11::Method4.MI.4857()"
		ldstr "class IBase1`1<class Generated651.BaseClass0> on type class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated651.IBase1`1<class Generated651.BaseClass0>::Method5()
		ldstr "G1_C11::Method5.4858()"
		ldstr "class IBase1`1<class Generated651.BaseClass0> on type class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated651.IBase1`1<class Generated651.BaseClass0>::Method6<object>()
		ldstr "G1_C11::Method6.MI.4860<System.Object>()"
		ldstr "class IBase1`1<class Generated651.BaseClass0> on type class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated651.IBase2`2<class Generated651.BaseClass0,class Generated651.BaseClass1>::Method7<object>()
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated651.BaseClass0,class Generated651.BaseClass1> on type class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass1>"
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
		newobj instance void class Generated651.G3_C1123`1<class Generated651.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G2_C148::Method4.6160()#G1_C11::Method5.4858()#G2_C148::Method6.6162<System.Object>()#G2_C148::Method7.6164<System.Object>()#"
		call void Generated651.Generated651::M.G1_C11.T.T<class Generated651.BaseClass0,class Generated651.BaseClass1,class Generated651.G3_C1123`1<class Generated651.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G2_C148::Method4.6160()#G1_C11::Method5.4858()#G2_C148::Method6.6162<System.Object>()#G2_C148::Method7.6164<System.Object>()#"
		call void Generated651.Generated651::M.G1_C11.A.T<class Generated651.BaseClass1,class Generated651.G3_C1123`1<class Generated651.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G2_C148::Method4.6160()#G1_C11::Method5.4858()#G2_C148::Method6.6162<System.Object>()#G2_C148::Method7.6164<System.Object>()#"
		call void Generated651.Generated651::M.G1_C11.A.B<class Generated651.G3_C1123`1<class Generated651.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C148::Method7.6164<System.Object>()#"
		call void Generated651.Generated651::M.IBase2.T.T<class Generated651.BaseClass1,class Generated651.BaseClass1,class Generated651.G3_C1123`1<class Generated651.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C148::Method7.6164<System.Object>()#"
		call void Generated651.Generated651::M.IBase2.B.T<class Generated651.BaseClass1,class Generated651.G3_C1123`1<class Generated651.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C148::Method7.6164<System.Object>()#"
		call void Generated651.Generated651::M.IBase2.B.B<class Generated651.G3_C1123`1<class Generated651.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C148::Method4.6160()#G2_C148::Method5.6161()#G2_C148::Method6.MI.6163<System.Object>()#"
		call void Generated651.Generated651::M.IBase1.T<class Generated651.BaseClass0,class Generated651.G3_C1123`1<class Generated651.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C148::Method4.6160()#G2_C148::Method5.6161()#G2_C148::Method6.MI.6163<System.Object>()#"
		call void Generated651.Generated651::M.IBase1.A<class Generated651.G3_C1123`1<class Generated651.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C148::Method7.6164<System.Object>()#"
		call void Generated651.Generated651::M.IBase2.T.T<class Generated651.BaseClass0,class Generated651.BaseClass1,class Generated651.G3_C1123`1<class Generated651.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C148::Method7.6164<System.Object>()#"
		call void Generated651.Generated651::M.IBase2.A.T<class Generated651.BaseClass1,class Generated651.G3_C1123`1<class Generated651.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C148::Method7.6164<System.Object>()#"
		call void Generated651.Generated651::M.IBase2.A.B<class Generated651.G3_C1123`1<class Generated651.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G3_C1123::ClassMethod1619.MI.14456()#G2_C148::Method4.6160()#G2_C148::Method5.6161()#G2_C148::Method6.6162<System.Object>()#G2_C148::Method7.6164<System.Object>()#"
		call void Generated651.Generated651::M.G2_C148.T<class Generated651.BaseClass1,class Generated651.G3_C1123`1<class Generated651.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G3_C1123::ClassMethod1619.MI.14456()#G2_C148::Method4.6160()#G2_C148::Method5.6161()#G2_C148::Method6.6162<System.Object>()#G2_C148::Method7.6164<System.Object>()#"
		call void Generated651.Generated651::M.G2_C148.B<class Generated651.G3_C1123`1<class Generated651.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C148::Method7.6164<System.Object>()#"
		call void Generated651.Generated651::M.IBase2.T.T<class Generated651.BaseClass1,class Generated651.BaseClass0,class Generated651.G3_C1123`1<class Generated651.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C148::Method7.6164<System.Object>()#"
		call void Generated651.Generated651::M.IBase2.B.T<class Generated651.BaseClass0,class Generated651.G3_C1123`1<class Generated651.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C148::Method7.6164<System.Object>()#"
		call void Generated651.Generated651::M.IBase2.B.A<class Generated651.G3_C1123`1<class Generated651.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C148::Method7.6164<System.Object>()#"
		call void Generated651.Generated651::M.IBase2.T.T<class Generated651.BaseClass0,class Generated651.BaseClass0,class Generated651.G3_C1123`1<class Generated651.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C148::Method7.6164<System.Object>()#"
		call void Generated651.Generated651::M.IBase2.A.T<class Generated651.BaseClass0,class Generated651.G3_C1123`1<class Generated651.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C148::Method7.6164<System.Object>()#"
		call void Generated651.Generated651::M.IBase2.A.A<class Generated651.G3_C1123`1<class Generated651.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G3_C1123::ClassMethod1619.MI.14456()#G3_C1123::ClassMethod3643.14455<System.Object>()#G3_C1123::Method0.14448()#G3_C1123::Method1.14450()#G3_C1123::Method2.14451<System.Object>()#G3_C1123::Method3.14453<System.Object>()#G2_C148::Method4.6160()#G2_C148::Method5.6161()#G2_C148::Method6.6162<System.Object>()#G2_C148::Method7.6164<System.Object>()#"
		call void Generated651.Generated651::M.G3_C1123.T<class Generated651.BaseClass0,class Generated651.G3_C1123`1<class Generated651.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G3_C1123::ClassMethod1619.MI.14456()#G3_C1123::ClassMethod3643.14455<System.Object>()#G3_C1123::Method0.14448()#G3_C1123::Method1.14450()#G3_C1123::Method2.14451<System.Object>()#G3_C1123::Method3.14453<System.Object>()#G2_C148::Method4.6160()#G2_C148::Method5.6161()#G2_C148::Method6.6162<System.Object>()#G2_C148::Method7.6164<System.Object>()#"
		call void Generated651.Generated651::M.G3_C1123.A<class Generated651.G3_C1123`1<class Generated651.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1123::Method0.MI.14449()#G3_C1123::Method1.14450()#G3_C1123::Method2.MI.14452<System.Object>()#G3_C1123::Method3.MI.14454<System.Object>()#"
		call void Generated651.Generated651::M.Generated651.IBase0<class Generated651.G3_C1123`1<class Generated651.BaseClass0>>(!!0,string)
		newobj instance void class Generated651.G3_C1123`1<class Generated651.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G2_C148::Method4.6160()#G1_C11::Method5.4858()#G2_C148::Method6.6162<System.Object>()#G2_C148::Method7.6164<System.Object>()#"
		call void Generated651.Generated651::M.G1_C11.T.T<class Generated651.BaseClass0,class Generated651.BaseClass1,class Generated651.G3_C1123`1<class Generated651.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G2_C148::Method4.6160()#G1_C11::Method5.4858()#G2_C148::Method6.6162<System.Object>()#G2_C148::Method7.6164<System.Object>()#"
		call void Generated651.Generated651::M.G1_C11.A.T<class Generated651.BaseClass1,class Generated651.G3_C1123`1<class Generated651.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G2_C148::Method4.6160()#G1_C11::Method5.4858()#G2_C148::Method6.6162<System.Object>()#G2_C148::Method7.6164<System.Object>()#"
		call void Generated651.Generated651::M.G1_C11.A.B<class Generated651.G3_C1123`1<class Generated651.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C148::Method7.6164<System.Object>()#"
		call void Generated651.Generated651::M.IBase2.T.T<class Generated651.BaseClass1,class Generated651.BaseClass1,class Generated651.G3_C1123`1<class Generated651.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C148::Method7.6164<System.Object>()#"
		call void Generated651.Generated651::M.IBase2.B.T<class Generated651.BaseClass1,class Generated651.G3_C1123`1<class Generated651.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C148::Method7.6164<System.Object>()#"
		call void Generated651.Generated651::M.IBase2.B.B<class Generated651.G3_C1123`1<class Generated651.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C148::Method4.6160()#G2_C148::Method5.6161()#G2_C148::Method6.MI.6163<System.Object>()#"
		call void Generated651.Generated651::M.IBase1.T<class Generated651.BaseClass0,class Generated651.G3_C1123`1<class Generated651.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C148::Method4.6160()#G2_C148::Method5.6161()#G2_C148::Method6.MI.6163<System.Object>()#"
		call void Generated651.Generated651::M.IBase1.A<class Generated651.G3_C1123`1<class Generated651.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C148::Method7.6164<System.Object>()#"
		call void Generated651.Generated651::M.IBase2.T.T<class Generated651.BaseClass0,class Generated651.BaseClass1,class Generated651.G3_C1123`1<class Generated651.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C148::Method7.6164<System.Object>()#"
		call void Generated651.Generated651::M.IBase2.A.T<class Generated651.BaseClass1,class Generated651.G3_C1123`1<class Generated651.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C148::Method7.6164<System.Object>()#"
		call void Generated651.Generated651::M.IBase2.A.B<class Generated651.G3_C1123`1<class Generated651.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G3_C1123::ClassMethod1619.MI.14456()#G2_C148::Method4.6160()#G2_C148::Method5.6161()#G2_C148::Method6.6162<System.Object>()#G2_C148::Method7.6164<System.Object>()#"
		call void Generated651.Generated651::M.G2_C148.T<class Generated651.BaseClass1,class Generated651.G3_C1123`1<class Generated651.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G3_C1123::ClassMethod1619.MI.14456()#G2_C148::Method4.6160()#G2_C148::Method5.6161()#G2_C148::Method6.6162<System.Object>()#G2_C148::Method7.6164<System.Object>()#"
		call void Generated651.Generated651::M.G2_C148.B<class Generated651.G3_C1123`1<class Generated651.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C148::Method7.6164<System.Object>()#"
		call void Generated651.Generated651::M.IBase2.T.T<class Generated651.BaseClass1,class Generated651.BaseClass0,class Generated651.G3_C1123`1<class Generated651.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C148::Method7.6164<System.Object>()#"
		call void Generated651.Generated651::M.IBase2.B.T<class Generated651.BaseClass0,class Generated651.G3_C1123`1<class Generated651.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C148::Method7.6164<System.Object>()#"
		call void Generated651.Generated651::M.IBase2.B.A<class Generated651.G3_C1123`1<class Generated651.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C148::Method7.6164<System.Object>()#"
		call void Generated651.Generated651::M.IBase2.T.T<class Generated651.BaseClass0,class Generated651.BaseClass0,class Generated651.G3_C1123`1<class Generated651.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C148::Method7.6164<System.Object>()#"
		call void Generated651.Generated651::M.IBase2.A.T<class Generated651.BaseClass0,class Generated651.G3_C1123`1<class Generated651.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C148::Method7.6164<System.Object>()#"
		call void Generated651.Generated651::M.IBase2.A.A<class Generated651.G3_C1123`1<class Generated651.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G3_C1123::ClassMethod1619.MI.14456()#G3_C1123::ClassMethod3643.14455<System.Object>()#G3_C1123::Method0.14448()#G3_C1123::Method1.14450()#G3_C1123::Method2.14451<System.Object>()#G3_C1123::Method3.14453<System.Object>()#G2_C148::Method4.6160()#G2_C148::Method5.6161()#G2_C148::Method6.6162<System.Object>()#G2_C148::Method7.6164<System.Object>()#"
		call void Generated651.Generated651::M.G3_C1123.T<class Generated651.BaseClass1,class Generated651.G3_C1123`1<class Generated651.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G3_C1123::ClassMethod1619.MI.14456()#G3_C1123::ClassMethod3643.14455<System.Object>()#G3_C1123::Method0.14448()#G3_C1123::Method1.14450()#G3_C1123::Method2.14451<System.Object>()#G3_C1123::Method3.14453<System.Object>()#G2_C148::Method4.6160()#G2_C148::Method5.6161()#G2_C148::Method6.6162<System.Object>()#G2_C148::Method7.6164<System.Object>()#"
		call void Generated651.Generated651::M.G3_C1123.B<class Generated651.G3_C1123`1<class Generated651.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1123::Method0.MI.14449()#G3_C1123::Method1.14450()#G3_C1123::Method2.MI.14452<System.Object>()#G3_C1123::Method3.MI.14454<System.Object>()#"
		call void Generated651.Generated651::M.Generated651.IBase0<class Generated651.G3_C1123`1<class Generated651.BaseClass1>>(!!0,string)
		newobj instance void class Generated651.G2_C148`1<class Generated651.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G2_C148::Method4.6160()#G1_C11::Method5.4858()#G2_C148::Method6.6162<System.Object>()#G2_C148::Method7.6164<System.Object>()#"
		call void Generated651.Generated651::M.G1_C11.T.T<class Generated651.BaseClass0,class Generated651.BaseClass0,class Generated651.G2_C148`1<class Generated651.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G2_C148::Method4.6160()#G1_C11::Method5.4858()#G2_C148::Method6.6162<System.Object>()#G2_C148::Method7.6164<System.Object>()#"
		call void Generated651.Generated651::M.G1_C11.A.T<class Generated651.BaseClass0,class Generated651.G2_C148`1<class Generated651.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G2_C148::Method4.6160()#G1_C11::Method5.4858()#G2_C148::Method6.6162<System.Object>()#G2_C148::Method7.6164<System.Object>()#"
		call void Generated651.Generated651::M.G1_C11.A.A<class Generated651.G2_C148`1<class Generated651.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C148::Method7.6164<System.Object>()#"
		call void Generated651.Generated651::M.IBase2.T.T<class Generated651.BaseClass1,class Generated651.BaseClass0,class Generated651.G2_C148`1<class Generated651.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C148::Method7.6164<System.Object>()#"
		call void Generated651.Generated651::M.IBase2.B.T<class Generated651.BaseClass0,class Generated651.G2_C148`1<class Generated651.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C148::Method7.6164<System.Object>()#"
		call void Generated651.Generated651::M.IBase2.B.A<class Generated651.G2_C148`1<class Generated651.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C148::Method4.6160()#G2_C148::Method5.6161()#G2_C148::Method6.MI.6163<System.Object>()#"
		call void Generated651.Generated651::M.IBase1.T<class Generated651.BaseClass0,class Generated651.G2_C148`1<class Generated651.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C148::Method4.6160()#G2_C148::Method5.6161()#G2_C148::Method6.MI.6163<System.Object>()#"
		call void Generated651.Generated651::M.IBase1.A<class Generated651.G2_C148`1<class Generated651.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C148::Method7.6164<System.Object>()#"
		call void Generated651.Generated651::M.IBase2.T.T<class Generated651.BaseClass0,class Generated651.BaseClass0,class Generated651.G2_C148`1<class Generated651.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C148::Method7.6164<System.Object>()#"
		call void Generated651.Generated651::M.IBase2.A.T<class Generated651.BaseClass0,class Generated651.G2_C148`1<class Generated651.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C148::Method7.6164<System.Object>()#"
		call void Generated651.Generated651::M.IBase2.A.A<class Generated651.G2_C148`1<class Generated651.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C148::Method7.6164<System.Object>()#"
		call void Generated651.Generated651::M.IBase2.T.T<class Generated651.BaseClass0,class Generated651.BaseClass1,class Generated651.G2_C148`1<class Generated651.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C148::Method7.6164<System.Object>()#"
		call void Generated651.Generated651::M.IBase2.A.T<class Generated651.BaseClass1,class Generated651.G2_C148`1<class Generated651.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C148::Method7.6164<System.Object>()#"
		call void Generated651.Generated651::M.IBase2.A.B<class Generated651.G2_C148`1<class Generated651.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C148::Method7.6164<System.Object>()#"
		call void Generated651.Generated651::M.IBase2.T.T<class Generated651.BaseClass1,class Generated651.BaseClass1,class Generated651.G2_C148`1<class Generated651.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C148::Method7.6164<System.Object>()#"
		call void Generated651.Generated651::M.IBase2.B.T<class Generated651.BaseClass1,class Generated651.G2_C148`1<class Generated651.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C148::Method7.6164<System.Object>()#"
		call void Generated651.Generated651::M.IBase2.B.B<class Generated651.G2_C148`1<class Generated651.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G2_C148::ClassMethod1619.6165()#G2_C148::Method4.6160()#G2_C148::Method5.6161()#G2_C148::Method6.6162<System.Object>()#G2_C148::Method7.6164<System.Object>()#"
		call void Generated651.Generated651::M.G2_C148.T<class Generated651.BaseClass0,class Generated651.G2_C148`1<class Generated651.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G2_C148::ClassMethod1619.6165()#G2_C148::Method4.6160()#G2_C148::Method5.6161()#G2_C148::Method6.6162<System.Object>()#G2_C148::Method7.6164<System.Object>()#"
		call void Generated651.Generated651::M.G2_C148.A<class Generated651.G2_C148`1<class Generated651.BaseClass0>>(!!0,string)
		newobj instance void class Generated651.G2_C148`1<class Generated651.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G2_C148::Method4.6160()#G1_C11::Method5.4858()#G2_C148::Method6.6162<System.Object>()#G2_C148::Method7.6164<System.Object>()#"
		call void Generated651.Generated651::M.G1_C11.T.T<class Generated651.BaseClass0,class Generated651.BaseClass1,class Generated651.G2_C148`1<class Generated651.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G2_C148::Method4.6160()#G1_C11::Method5.4858()#G2_C148::Method6.6162<System.Object>()#G2_C148::Method7.6164<System.Object>()#"
		call void Generated651.Generated651::M.G1_C11.A.T<class Generated651.BaseClass1,class Generated651.G2_C148`1<class Generated651.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G2_C148::Method4.6160()#G1_C11::Method5.4858()#G2_C148::Method6.6162<System.Object>()#G2_C148::Method7.6164<System.Object>()#"
		call void Generated651.Generated651::M.G1_C11.A.B<class Generated651.G2_C148`1<class Generated651.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C148::Method7.6164<System.Object>()#"
		call void Generated651.Generated651::M.IBase2.T.T<class Generated651.BaseClass1,class Generated651.BaseClass1,class Generated651.G2_C148`1<class Generated651.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C148::Method7.6164<System.Object>()#"
		call void Generated651.Generated651::M.IBase2.B.T<class Generated651.BaseClass1,class Generated651.G2_C148`1<class Generated651.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C148::Method7.6164<System.Object>()#"
		call void Generated651.Generated651::M.IBase2.B.B<class Generated651.G2_C148`1<class Generated651.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C148::Method4.6160()#G2_C148::Method5.6161()#G2_C148::Method6.MI.6163<System.Object>()#"
		call void Generated651.Generated651::M.IBase1.T<class Generated651.BaseClass0,class Generated651.G2_C148`1<class Generated651.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C148::Method4.6160()#G2_C148::Method5.6161()#G2_C148::Method6.MI.6163<System.Object>()#"
		call void Generated651.Generated651::M.IBase1.A<class Generated651.G2_C148`1<class Generated651.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C148::Method7.6164<System.Object>()#"
		call void Generated651.Generated651::M.IBase2.T.T<class Generated651.BaseClass0,class Generated651.BaseClass1,class Generated651.G2_C148`1<class Generated651.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C148::Method7.6164<System.Object>()#"
		call void Generated651.Generated651::M.IBase2.A.T<class Generated651.BaseClass1,class Generated651.G2_C148`1<class Generated651.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C148::Method7.6164<System.Object>()#"
		call void Generated651.Generated651::M.IBase2.A.B<class Generated651.G2_C148`1<class Generated651.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G2_C148::ClassMethod1619.6165()#G2_C148::Method4.6160()#G2_C148::Method5.6161()#G2_C148::Method6.6162<System.Object>()#G2_C148::Method7.6164<System.Object>()#"
		call void Generated651.Generated651::M.G2_C148.T<class Generated651.BaseClass1,class Generated651.G2_C148`1<class Generated651.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G2_C148::ClassMethod1619.6165()#G2_C148::Method4.6160()#G2_C148::Method5.6161()#G2_C148::Method6.6162<System.Object>()#G2_C148::Method7.6164<System.Object>()#"
		call void Generated651.Generated651::M.G2_C148.B<class Generated651.G2_C148`1<class Generated651.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C148::Method7.6164<System.Object>()#"
		call void Generated651.Generated651::M.IBase2.T.T<class Generated651.BaseClass1,class Generated651.BaseClass0,class Generated651.G2_C148`1<class Generated651.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C148::Method7.6164<System.Object>()#"
		call void Generated651.Generated651::M.IBase2.B.T<class Generated651.BaseClass0,class Generated651.G2_C148`1<class Generated651.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C148::Method7.6164<System.Object>()#"
		call void Generated651.Generated651::M.IBase2.B.A<class Generated651.G2_C148`1<class Generated651.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C148::Method7.6164<System.Object>()#"
		call void Generated651.Generated651::M.IBase2.T.T<class Generated651.BaseClass0,class Generated651.BaseClass0,class Generated651.G2_C148`1<class Generated651.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C148::Method7.6164<System.Object>()#"
		call void Generated651.Generated651::M.IBase2.A.T<class Generated651.BaseClass0,class Generated651.G2_C148`1<class Generated651.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C148::Method7.6164<System.Object>()#"
		call void Generated651.Generated651::M.IBase2.A.A<class Generated651.G2_C148`1<class Generated651.BaseClass1>>(!!0,string)
		newobj instance void class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G1_C11::Method4.4856()#G1_C11::Method5.4858()#G1_C11::Method6.4859<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Generated651.Generated651::M.G1_C11.T.T<class Generated651.BaseClass0,class Generated651.BaseClass0,class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G1_C11::Method4.4856()#G1_C11::Method5.4858()#G1_C11::Method6.4859<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Generated651.Generated651::M.G1_C11.A.T<class Generated651.BaseClass0,class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G1_C11::Method4.4856()#G1_C11::Method5.4858()#G1_C11::Method6.4859<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Generated651.Generated651::M.G1_C11.A.A<class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated651.Generated651::M.IBase2.T.T<class Generated651.BaseClass1,class Generated651.BaseClass0,class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated651.Generated651::M.IBase2.B.T<class Generated651.BaseClass0,class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated651.Generated651::M.IBase2.B.A<class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method4.MI.4857()#G1_C11::Method5.4858()#G1_C11::Method6.MI.4860<System.Object>()#"
		call void Generated651.Generated651::M.IBase1.T<class Generated651.BaseClass0,class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method4.MI.4857()#G1_C11::Method5.4858()#G1_C11::Method6.MI.4860<System.Object>()#"
		call void Generated651.Generated651::M.IBase1.A<class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated651.Generated651::M.IBase2.T.T<class Generated651.BaseClass0,class Generated651.BaseClass0,class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated651.Generated651::M.IBase2.A.T<class Generated651.BaseClass0,class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated651.Generated651::M.IBase2.A.A<class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated651.Generated651::M.IBase2.T.T<class Generated651.BaseClass0,class Generated651.BaseClass1,class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated651.Generated651::M.IBase2.A.T<class Generated651.BaseClass1,class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated651.Generated651::M.IBase2.A.B<class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated651.Generated651::M.IBase2.T.T<class Generated651.BaseClass1,class Generated651.BaseClass1,class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated651.Generated651::M.IBase2.B.T<class Generated651.BaseClass1,class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated651.Generated651::M.IBase2.B.B<class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0>>(!!0,string)
		newobj instance void class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G1_C11::Method4.4856()#G1_C11::Method5.4858()#G1_C11::Method6.4859<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Generated651.Generated651::M.G1_C11.T.T<class Generated651.BaseClass0,class Generated651.BaseClass1,class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G1_C11::Method4.4856()#G1_C11::Method5.4858()#G1_C11::Method6.4859<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Generated651.Generated651::M.G1_C11.A.T<class Generated651.BaseClass1,class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G1_C11::Method4.4856()#G1_C11::Method5.4858()#G1_C11::Method6.4859<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Generated651.Generated651::M.G1_C11.A.B<class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated651.Generated651::M.IBase2.T.T<class Generated651.BaseClass1,class Generated651.BaseClass1,class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated651.Generated651::M.IBase2.B.T<class Generated651.BaseClass1,class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated651.Generated651::M.IBase2.B.B<class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method4.MI.4857()#G1_C11::Method5.4858()#G1_C11::Method6.MI.4860<System.Object>()#"
		call void Generated651.Generated651::M.IBase1.T<class Generated651.BaseClass0,class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method4.MI.4857()#G1_C11::Method5.4858()#G1_C11::Method6.MI.4860<System.Object>()#"
		call void Generated651.Generated651::M.IBase1.A<class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated651.Generated651::M.IBase2.T.T<class Generated651.BaseClass0,class Generated651.BaseClass1,class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated651.Generated651::M.IBase2.A.T<class Generated651.BaseClass1,class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated651.Generated651::M.IBase2.A.B<class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>>(!!0,string)
		newobj instance void class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G1_C11::Method4.4856()#G1_C11::Method5.4858()#G1_C11::Method6.4859<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Generated651.Generated651::M.G1_C11.T.T<class Generated651.BaseClass1,class Generated651.BaseClass0,class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G1_C11::Method4.4856()#G1_C11::Method5.4858()#G1_C11::Method6.4859<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Generated651.Generated651::M.G1_C11.B.T<class Generated651.BaseClass0,class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G1_C11::Method4.4856()#G1_C11::Method5.4858()#G1_C11::Method6.4859<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Generated651.Generated651::M.G1_C11.B.A<class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated651.Generated651::M.IBase2.T.T<class Generated651.BaseClass1,class Generated651.BaseClass0,class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated651.Generated651::M.IBase2.B.T<class Generated651.BaseClass0,class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated651.Generated651::M.IBase2.B.A<class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method4.MI.4857()#G1_C11::Method5.4858()#G1_C11::Method6.MI.4860<System.Object>()#"
		call void Generated651.Generated651::M.IBase1.T<class Generated651.BaseClass0,class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method4.MI.4857()#G1_C11::Method5.4858()#G1_C11::Method6.MI.4860<System.Object>()#"
		call void Generated651.Generated651::M.IBase1.A<class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated651.Generated651::M.IBase2.T.T<class Generated651.BaseClass0,class Generated651.BaseClass0,class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated651.Generated651::M.IBase2.A.T<class Generated651.BaseClass0,class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated651.Generated651::M.IBase2.A.A<class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated651.Generated651::M.IBase2.T.T<class Generated651.BaseClass0,class Generated651.BaseClass1,class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated651.Generated651::M.IBase2.A.T<class Generated651.BaseClass1,class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated651.Generated651::M.IBase2.A.B<class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated651.Generated651::M.IBase2.T.T<class Generated651.BaseClass1,class Generated651.BaseClass1,class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated651.Generated651::M.IBase2.B.T<class Generated651.BaseClass1,class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated651.Generated651::M.IBase2.B.B<class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass0>>(!!0,string)
		newobj instance void class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G1_C11::Method4.4856()#G1_C11::Method5.4858()#G1_C11::Method6.4859<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Generated651.Generated651::M.G1_C11.T.T<class Generated651.BaseClass1,class Generated651.BaseClass1,class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G1_C11::Method4.4856()#G1_C11::Method5.4858()#G1_C11::Method6.4859<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Generated651.Generated651::M.G1_C11.B.T<class Generated651.BaseClass1,class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G1_C11::Method4.4856()#G1_C11::Method5.4858()#G1_C11::Method6.4859<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Generated651.Generated651::M.G1_C11.B.B<class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated651.Generated651::M.IBase2.T.T<class Generated651.BaseClass1,class Generated651.BaseClass1,class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated651.Generated651::M.IBase2.B.T<class Generated651.BaseClass1,class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated651.Generated651::M.IBase2.B.B<class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method4.MI.4857()#G1_C11::Method5.4858()#G1_C11::Method6.MI.4860<System.Object>()#"
		call void Generated651.Generated651::M.IBase1.T<class Generated651.BaseClass0,class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method4.MI.4857()#G1_C11::Method5.4858()#G1_C11::Method6.MI.4860<System.Object>()#"
		call void Generated651.Generated651::M.IBase1.A<class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated651.Generated651::M.IBase2.T.T<class Generated651.BaseClass0,class Generated651.BaseClass1,class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated651.Generated651::M.IBase2.A.T<class Generated651.BaseClass1,class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated651.Generated651::M.IBase2.A.B<class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass1>>(!!0,string)
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
		newobj instance void class Generated651.G3_C1123`1<class Generated651.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>::ClassMethod1345<object>()
		calli default string(class Generated651.G3_C1123`1<class Generated651.BaseClass0>)
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1> on type class Generated651.G3_C1123`1<class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>::ClassMethod1344<object>()
		calli default string(class Generated651.G3_C1123`1<class Generated651.BaseClass0>)
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()"
		ldstr "class G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1> on type class Generated651.G3_C1123`1<class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>::Method6<object>()
		calli default string(class Generated651.G3_C1123`1<class Generated651.BaseClass0>)
		ldstr "G2_C148::Method6.6162<System.Object>()"
		ldstr "class G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1> on type class Generated651.G3_C1123`1<class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>::Method5()
		calli default string(class Generated651.G3_C1123`1<class Generated651.BaseClass0>)
		ldstr "G1_C11::Method5.4858()"
		ldstr "class G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1> on type class Generated651.G3_C1123`1<class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>::Method4()
		calli default string(class Generated651.G3_C1123`1<class Generated651.BaseClass0>)
		ldstr "G2_C148::Method4.6160()"
		ldstr "class G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1> on type class Generated651.G3_C1123`1<class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>::Method7<object>()
		calli default string(class Generated651.G3_C1123`1<class Generated651.BaseClass0>)
		ldstr "G2_C148::Method7.6164<System.Object>()"
		ldstr "class G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1> on type class Generated651.G3_C1123`1<class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated651.IBase2`2<class Generated651.BaseClass1,class Generated651.BaseClass1>::Method7<object>()
		calli default string(class Generated651.G3_C1123`1<class Generated651.BaseClass0>)
		ldstr "G2_C148::Method7.6164<System.Object>()"
		ldstr "class IBase2`2<class Generated651.BaseClass1,class Generated651.BaseClass1> on type class Generated651.G3_C1123`1<class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated651.IBase1`1<class Generated651.BaseClass0>::Method4()
		calli default string(class Generated651.G3_C1123`1<class Generated651.BaseClass0>)
		ldstr "G2_C148::Method4.6160()"
		ldstr "class IBase1`1<class Generated651.BaseClass0> on type class Generated651.G3_C1123`1<class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated651.IBase1`1<class Generated651.BaseClass0>::Method5()
		calli default string(class Generated651.G3_C1123`1<class Generated651.BaseClass0>)
		ldstr "G2_C148::Method5.6161()"
		ldstr "class IBase1`1<class Generated651.BaseClass0> on type class Generated651.G3_C1123`1<class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated651.IBase1`1<class Generated651.BaseClass0>::Method6<object>()
		calli default string(class Generated651.G3_C1123`1<class Generated651.BaseClass0>)
		ldstr "G2_C148::Method6.MI.6163<System.Object>()"
		ldstr "class IBase1`1<class Generated651.BaseClass0> on type class Generated651.G3_C1123`1<class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated651.IBase2`2<class Generated651.BaseClass0,class Generated651.BaseClass1>::Method7<object>()
		calli default string(class Generated651.G3_C1123`1<class Generated651.BaseClass0>)
		ldstr "G2_C148::Method7.6164<System.Object>()"
		ldstr "class IBase2`2<class Generated651.BaseClass0,class Generated651.BaseClass1> on type class Generated651.G3_C1123`1<class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated651.G2_C148`1<class Generated651.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated651.G2_C148`1<class Generated651.BaseClass1>::ClassMethod1619()
		calli default string(class Generated651.G3_C1123`1<class Generated651.BaseClass0>)
		ldstr "G3_C1123::ClassMethod1619.MI.14456()"
		ldstr "class G2_C148`1<class Generated651.BaseClass1> on type class Generated651.G3_C1123`1<class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated651.G2_C148`1<class Generated651.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated651.G2_C148`1<class Generated651.BaseClass1>::Method7<object>()
		calli default string(class Generated651.G3_C1123`1<class Generated651.BaseClass0>)
		ldstr "G2_C148::Method7.6164<System.Object>()"
		ldstr "class G2_C148`1<class Generated651.BaseClass1> on type class Generated651.G3_C1123`1<class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated651.G2_C148`1<class Generated651.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated651.G2_C148`1<class Generated651.BaseClass1>::Method6<object>()
		calli default string(class Generated651.G3_C1123`1<class Generated651.BaseClass0>)
		ldstr "G2_C148::Method6.6162<System.Object>()"
		ldstr "class G2_C148`1<class Generated651.BaseClass1> on type class Generated651.G3_C1123`1<class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated651.G2_C148`1<class Generated651.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated651.G2_C148`1<class Generated651.BaseClass1>::Method5()
		calli default string(class Generated651.G3_C1123`1<class Generated651.BaseClass0>)
		ldstr "G2_C148::Method5.6161()"
		ldstr "class G2_C148`1<class Generated651.BaseClass1> on type class Generated651.G3_C1123`1<class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated651.G2_C148`1<class Generated651.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated651.G2_C148`1<class Generated651.BaseClass1>::Method4()
		calli default string(class Generated651.G3_C1123`1<class Generated651.BaseClass0>)
		ldstr "G2_C148::Method4.6160()"
		ldstr "class G2_C148`1<class Generated651.BaseClass1> on type class Generated651.G3_C1123`1<class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated651.G2_C148`1<class Generated651.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated651.G2_C148`1<class Generated651.BaseClass1>::ClassMethod1345<object>()
		calli default string(class Generated651.G3_C1123`1<class Generated651.BaseClass0>)
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G2_C148`1<class Generated651.BaseClass1> on type class Generated651.G3_C1123`1<class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated651.G2_C148`1<class Generated651.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated651.G2_C148`1<class Generated651.BaseClass1>::ClassMethod1344<object>()
		calli default string(class Generated651.G3_C1123`1<class Generated651.BaseClass0>)
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()"
		ldstr "class G2_C148`1<class Generated651.BaseClass1> on type class Generated651.G3_C1123`1<class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated651.IBase2`2<class Generated651.BaseClass1,class Generated651.BaseClass0>::Method7<object>()
		calli default string(class Generated651.G3_C1123`1<class Generated651.BaseClass0>)
		ldstr "G2_C148::Method7.6164<System.Object>()"
		ldstr "class IBase2`2<class Generated651.BaseClass1,class Generated651.BaseClass0> on type class Generated651.G3_C1123`1<class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated651.IBase2`2<class Generated651.BaseClass0,class Generated651.BaseClass0>::Method7<object>()
		calli default string(class Generated651.G3_C1123`1<class Generated651.BaseClass0>)
		ldstr "G2_C148::Method7.6164<System.Object>()"
		ldstr "class IBase2`2<class Generated651.BaseClass0,class Generated651.BaseClass0> on type class Generated651.G3_C1123`1<class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated651.G3_C1123`1<class Generated651.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated651.G3_C1123`1<class Generated651.BaseClass0>::ClassMethod3643<object>()
		calli default string(class Generated651.G3_C1123`1<class Generated651.BaseClass0>)
		ldstr "G3_C1123::ClassMethod3643.14455<System.Object>()"
		ldstr "class G3_C1123`1<class Generated651.BaseClass0> on type class Generated651.G3_C1123`1<class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated651.G3_C1123`1<class Generated651.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated651.G3_C1123`1<class Generated651.BaseClass0>::Method3<object>()
		calli default string(class Generated651.G3_C1123`1<class Generated651.BaseClass0>)
		ldstr "G3_C1123::Method3.14453<System.Object>()"
		ldstr "class G3_C1123`1<class Generated651.BaseClass0> on type class Generated651.G3_C1123`1<class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated651.G3_C1123`1<class Generated651.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated651.G3_C1123`1<class Generated651.BaseClass0>::Method2<object>()
		calli default string(class Generated651.G3_C1123`1<class Generated651.BaseClass0>)
		ldstr "G3_C1123::Method2.14451<System.Object>()"
		ldstr "class G3_C1123`1<class Generated651.BaseClass0> on type class Generated651.G3_C1123`1<class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated651.G3_C1123`1<class Generated651.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated651.G3_C1123`1<class Generated651.BaseClass0>::Method1()
		calli default string(class Generated651.G3_C1123`1<class Generated651.BaseClass0>)
		ldstr "G3_C1123::Method1.14450()"
		ldstr "class G3_C1123`1<class Generated651.BaseClass0> on type class Generated651.G3_C1123`1<class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated651.G3_C1123`1<class Generated651.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated651.G3_C1123`1<class Generated651.BaseClass0>::Method0()
		calli default string(class Generated651.G3_C1123`1<class Generated651.BaseClass0>)
		ldstr "G3_C1123::Method0.14448()"
		ldstr "class G3_C1123`1<class Generated651.BaseClass0> on type class Generated651.G3_C1123`1<class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated651.G3_C1123`1<class Generated651.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated651.G3_C1123`1<class Generated651.BaseClass0>::ClassMethod1619()
		calli default string(class Generated651.G3_C1123`1<class Generated651.BaseClass0>)
		ldstr "G3_C1123::ClassMethod1619.MI.14456()"
		ldstr "class G3_C1123`1<class Generated651.BaseClass0> on type class Generated651.G3_C1123`1<class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated651.G3_C1123`1<class Generated651.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated651.G3_C1123`1<class Generated651.BaseClass0>::Method7<object>()
		calli default string(class Generated651.G3_C1123`1<class Generated651.BaseClass0>)
		ldstr "G2_C148::Method7.6164<System.Object>()"
		ldstr "class G3_C1123`1<class Generated651.BaseClass0> on type class Generated651.G3_C1123`1<class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated651.G3_C1123`1<class Generated651.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated651.G3_C1123`1<class Generated651.BaseClass0>::Method6<object>()
		calli default string(class Generated651.G3_C1123`1<class Generated651.BaseClass0>)
		ldstr "G2_C148::Method6.6162<System.Object>()"
		ldstr "class G3_C1123`1<class Generated651.BaseClass0> on type class Generated651.G3_C1123`1<class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated651.G3_C1123`1<class Generated651.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated651.G3_C1123`1<class Generated651.BaseClass0>::Method5()
		calli default string(class Generated651.G3_C1123`1<class Generated651.BaseClass0>)
		ldstr "G2_C148::Method5.6161()"
		ldstr "class G3_C1123`1<class Generated651.BaseClass0> on type class Generated651.G3_C1123`1<class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated651.G3_C1123`1<class Generated651.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated651.G3_C1123`1<class Generated651.BaseClass0>::Method4()
		calli default string(class Generated651.G3_C1123`1<class Generated651.BaseClass0>)
		ldstr "G2_C148::Method4.6160()"
		ldstr "class G3_C1123`1<class Generated651.BaseClass0> on type class Generated651.G3_C1123`1<class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated651.G3_C1123`1<class Generated651.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated651.G3_C1123`1<class Generated651.BaseClass0>::ClassMethod1345<object>()
		calli default string(class Generated651.G3_C1123`1<class Generated651.BaseClass0>)
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G3_C1123`1<class Generated651.BaseClass0> on type class Generated651.G3_C1123`1<class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated651.G3_C1123`1<class Generated651.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated651.G3_C1123`1<class Generated651.BaseClass0>::ClassMethod1344<object>()
		calli default string(class Generated651.G3_C1123`1<class Generated651.BaseClass0>)
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()"
		ldstr "class G3_C1123`1<class Generated651.BaseClass0> on type class Generated651.G3_C1123`1<class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated651.IBase0::Method0()
		calli default string(class Generated651.G3_C1123`1<class Generated651.BaseClass0>)
		ldstr "G3_C1123::Method0.MI.14449()"
		ldstr "Generated651.IBase0 on type class Generated651.G3_C1123`1<class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated651.IBase0::Method1()
		calli default string(class Generated651.G3_C1123`1<class Generated651.BaseClass0>)
		ldstr "G3_C1123::Method1.14450()"
		ldstr "Generated651.IBase0 on type class Generated651.G3_C1123`1<class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated651.IBase0::Method2<object>()
		calli default string(class Generated651.G3_C1123`1<class Generated651.BaseClass0>)
		ldstr "G3_C1123::Method2.MI.14452<System.Object>()"
		ldstr "Generated651.IBase0 on type class Generated651.G3_C1123`1<class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated651.IBase0::Method3<object>()
		calli default string(class Generated651.G3_C1123`1<class Generated651.BaseClass0>)
		ldstr "G3_C1123::Method3.MI.14454<System.Object>()"
		ldstr "Generated651.IBase0 on type class Generated651.G3_C1123`1<class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated651.G3_C1123`1<class Generated651.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>::ClassMethod1345<object>()
		calli default string(class Generated651.G3_C1123`1<class Generated651.BaseClass1>)
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1> on type class Generated651.G3_C1123`1<class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>::ClassMethod1344<object>()
		calli default string(class Generated651.G3_C1123`1<class Generated651.BaseClass1>)
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()"
		ldstr "class G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1> on type class Generated651.G3_C1123`1<class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>::Method6<object>()
		calli default string(class Generated651.G3_C1123`1<class Generated651.BaseClass1>)
		ldstr "G2_C148::Method6.6162<System.Object>()"
		ldstr "class G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1> on type class Generated651.G3_C1123`1<class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>::Method5()
		calli default string(class Generated651.G3_C1123`1<class Generated651.BaseClass1>)
		ldstr "G1_C11::Method5.4858()"
		ldstr "class G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1> on type class Generated651.G3_C1123`1<class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>::Method4()
		calli default string(class Generated651.G3_C1123`1<class Generated651.BaseClass1>)
		ldstr "G2_C148::Method4.6160()"
		ldstr "class G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1> on type class Generated651.G3_C1123`1<class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>::Method7<object>()
		calli default string(class Generated651.G3_C1123`1<class Generated651.BaseClass1>)
		ldstr "G2_C148::Method7.6164<System.Object>()"
		ldstr "class G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1> on type class Generated651.G3_C1123`1<class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated651.IBase2`2<class Generated651.BaseClass1,class Generated651.BaseClass1>::Method7<object>()
		calli default string(class Generated651.G3_C1123`1<class Generated651.BaseClass1>)
		ldstr "G2_C148::Method7.6164<System.Object>()"
		ldstr "class IBase2`2<class Generated651.BaseClass1,class Generated651.BaseClass1> on type class Generated651.G3_C1123`1<class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated651.IBase1`1<class Generated651.BaseClass0>::Method4()
		calli default string(class Generated651.G3_C1123`1<class Generated651.BaseClass1>)
		ldstr "G2_C148::Method4.6160()"
		ldstr "class IBase1`1<class Generated651.BaseClass0> on type class Generated651.G3_C1123`1<class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated651.IBase1`1<class Generated651.BaseClass0>::Method5()
		calli default string(class Generated651.G3_C1123`1<class Generated651.BaseClass1>)
		ldstr "G2_C148::Method5.6161()"
		ldstr "class IBase1`1<class Generated651.BaseClass0> on type class Generated651.G3_C1123`1<class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated651.IBase1`1<class Generated651.BaseClass0>::Method6<object>()
		calli default string(class Generated651.G3_C1123`1<class Generated651.BaseClass1>)
		ldstr "G2_C148::Method6.MI.6163<System.Object>()"
		ldstr "class IBase1`1<class Generated651.BaseClass0> on type class Generated651.G3_C1123`1<class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated651.IBase2`2<class Generated651.BaseClass0,class Generated651.BaseClass1>::Method7<object>()
		calli default string(class Generated651.G3_C1123`1<class Generated651.BaseClass1>)
		ldstr "G2_C148::Method7.6164<System.Object>()"
		ldstr "class IBase2`2<class Generated651.BaseClass0,class Generated651.BaseClass1> on type class Generated651.G3_C1123`1<class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated651.G2_C148`1<class Generated651.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated651.G2_C148`1<class Generated651.BaseClass1>::ClassMethod1619()
		calli default string(class Generated651.G3_C1123`1<class Generated651.BaseClass1>)
		ldstr "G3_C1123::ClassMethod1619.MI.14456()"
		ldstr "class G2_C148`1<class Generated651.BaseClass1> on type class Generated651.G3_C1123`1<class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated651.G2_C148`1<class Generated651.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated651.G2_C148`1<class Generated651.BaseClass1>::Method7<object>()
		calli default string(class Generated651.G3_C1123`1<class Generated651.BaseClass1>)
		ldstr "G2_C148::Method7.6164<System.Object>()"
		ldstr "class G2_C148`1<class Generated651.BaseClass1> on type class Generated651.G3_C1123`1<class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated651.G2_C148`1<class Generated651.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated651.G2_C148`1<class Generated651.BaseClass1>::Method6<object>()
		calli default string(class Generated651.G3_C1123`1<class Generated651.BaseClass1>)
		ldstr "G2_C148::Method6.6162<System.Object>()"
		ldstr "class G2_C148`1<class Generated651.BaseClass1> on type class Generated651.G3_C1123`1<class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated651.G2_C148`1<class Generated651.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated651.G2_C148`1<class Generated651.BaseClass1>::Method5()
		calli default string(class Generated651.G3_C1123`1<class Generated651.BaseClass1>)
		ldstr "G2_C148::Method5.6161()"
		ldstr "class G2_C148`1<class Generated651.BaseClass1> on type class Generated651.G3_C1123`1<class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated651.G2_C148`1<class Generated651.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated651.G2_C148`1<class Generated651.BaseClass1>::Method4()
		calli default string(class Generated651.G3_C1123`1<class Generated651.BaseClass1>)
		ldstr "G2_C148::Method4.6160()"
		ldstr "class G2_C148`1<class Generated651.BaseClass1> on type class Generated651.G3_C1123`1<class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated651.G2_C148`1<class Generated651.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated651.G2_C148`1<class Generated651.BaseClass1>::ClassMethod1345<object>()
		calli default string(class Generated651.G3_C1123`1<class Generated651.BaseClass1>)
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G2_C148`1<class Generated651.BaseClass1> on type class Generated651.G3_C1123`1<class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated651.G2_C148`1<class Generated651.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated651.G2_C148`1<class Generated651.BaseClass1>::ClassMethod1344<object>()
		calli default string(class Generated651.G3_C1123`1<class Generated651.BaseClass1>)
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()"
		ldstr "class G2_C148`1<class Generated651.BaseClass1> on type class Generated651.G3_C1123`1<class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated651.IBase2`2<class Generated651.BaseClass1,class Generated651.BaseClass0>::Method7<object>()
		calli default string(class Generated651.G3_C1123`1<class Generated651.BaseClass1>)
		ldstr "G2_C148::Method7.6164<System.Object>()"
		ldstr "class IBase2`2<class Generated651.BaseClass1,class Generated651.BaseClass0> on type class Generated651.G3_C1123`1<class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated651.IBase2`2<class Generated651.BaseClass0,class Generated651.BaseClass0>::Method7<object>()
		calli default string(class Generated651.G3_C1123`1<class Generated651.BaseClass1>)
		ldstr "G2_C148::Method7.6164<System.Object>()"
		ldstr "class IBase2`2<class Generated651.BaseClass0,class Generated651.BaseClass0> on type class Generated651.G3_C1123`1<class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated651.G3_C1123`1<class Generated651.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated651.G3_C1123`1<class Generated651.BaseClass1>::ClassMethod3643<object>()
		calli default string(class Generated651.G3_C1123`1<class Generated651.BaseClass1>)
		ldstr "G3_C1123::ClassMethod3643.14455<System.Object>()"
		ldstr "class G3_C1123`1<class Generated651.BaseClass1> on type class Generated651.G3_C1123`1<class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated651.G3_C1123`1<class Generated651.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated651.G3_C1123`1<class Generated651.BaseClass1>::Method3<object>()
		calli default string(class Generated651.G3_C1123`1<class Generated651.BaseClass1>)
		ldstr "G3_C1123::Method3.14453<System.Object>()"
		ldstr "class G3_C1123`1<class Generated651.BaseClass1> on type class Generated651.G3_C1123`1<class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated651.G3_C1123`1<class Generated651.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated651.G3_C1123`1<class Generated651.BaseClass1>::Method2<object>()
		calli default string(class Generated651.G3_C1123`1<class Generated651.BaseClass1>)
		ldstr "G3_C1123::Method2.14451<System.Object>()"
		ldstr "class G3_C1123`1<class Generated651.BaseClass1> on type class Generated651.G3_C1123`1<class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated651.G3_C1123`1<class Generated651.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated651.G3_C1123`1<class Generated651.BaseClass1>::Method1()
		calli default string(class Generated651.G3_C1123`1<class Generated651.BaseClass1>)
		ldstr "G3_C1123::Method1.14450()"
		ldstr "class G3_C1123`1<class Generated651.BaseClass1> on type class Generated651.G3_C1123`1<class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated651.G3_C1123`1<class Generated651.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated651.G3_C1123`1<class Generated651.BaseClass1>::Method0()
		calli default string(class Generated651.G3_C1123`1<class Generated651.BaseClass1>)
		ldstr "G3_C1123::Method0.14448()"
		ldstr "class G3_C1123`1<class Generated651.BaseClass1> on type class Generated651.G3_C1123`1<class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated651.G3_C1123`1<class Generated651.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated651.G3_C1123`1<class Generated651.BaseClass1>::ClassMethod1619()
		calli default string(class Generated651.G3_C1123`1<class Generated651.BaseClass1>)
		ldstr "G3_C1123::ClassMethod1619.MI.14456()"
		ldstr "class G3_C1123`1<class Generated651.BaseClass1> on type class Generated651.G3_C1123`1<class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated651.G3_C1123`1<class Generated651.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated651.G3_C1123`1<class Generated651.BaseClass1>::Method7<object>()
		calli default string(class Generated651.G3_C1123`1<class Generated651.BaseClass1>)
		ldstr "G2_C148::Method7.6164<System.Object>()"
		ldstr "class G3_C1123`1<class Generated651.BaseClass1> on type class Generated651.G3_C1123`1<class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated651.G3_C1123`1<class Generated651.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated651.G3_C1123`1<class Generated651.BaseClass1>::Method6<object>()
		calli default string(class Generated651.G3_C1123`1<class Generated651.BaseClass1>)
		ldstr "G2_C148::Method6.6162<System.Object>()"
		ldstr "class G3_C1123`1<class Generated651.BaseClass1> on type class Generated651.G3_C1123`1<class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated651.G3_C1123`1<class Generated651.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated651.G3_C1123`1<class Generated651.BaseClass1>::Method5()
		calli default string(class Generated651.G3_C1123`1<class Generated651.BaseClass1>)
		ldstr "G2_C148::Method5.6161()"
		ldstr "class G3_C1123`1<class Generated651.BaseClass1> on type class Generated651.G3_C1123`1<class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated651.G3_C1123`1<class Generated651.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated651.G3_C1123`1<class Generated651.BaseClass1>::Method4()
		calli default string(class Generated651.G3_C1123`1<class Generated651.BaseClass1>)
		ldstr "G2_C148::Method4.6160()"
		ldstr "class G3_C1123`1<class Generated651.BaseClass1> on type class Generated651.G3_C1123`1<class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated651.G3_C1123`1<class Generated651.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated651.G3_C1123`1<class Generated651.BaseClass1>::ClassMethod1345<object>()
		calli default string(class Generated651.G3_C1123`1<class Generated651.BaseClass1>)
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G3_C1123`1<class Generated651.BaseClass1> on type class Generated651.G3_C1123`1<class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated651.G3_C1123`1<class Generated651.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated651.G3_C1123`1<class Generated651.BaseClass1>::ClassMethod1344<object>()
		calli default string(class Generated651.G3_C1123`1<class Generated651.BaseClass1>)
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()"
		ldstr "class G3_C1123`1<class Generated651.BaseClass1> on type class Generated651.G3_C1123`1<class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated651.IBase0::Method0()
		calli default string(class Generated651.G3_C1123`1<class Generated651.BaseClass1>)
		ldstr "G3_C1123::Method0.MI.14449()"
		ldstr "Generated651.IBase0 on type class Generated651.G3_C1123`1<class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated651.IBase0::Method1()
		calli default string(class Generated651.G3_C1123`1<class Generated651.BaseClass1>)
		ldstr "G3_C1123::Method1.14450()"
		ldstr "Generated651.IBase0 on type class Generated651.G3_C1123`1<class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated651.IBase0::Method2<object>()
		calli default string(class Generated651.G3_C1123`1<class Generated651.BaseClass1>)
		ldstr "G3_C1123::Method2.MI.14452<System.Object>()"
		ldstr "Generated651.IBase0 on type class Generated651.G3_C1123`1<class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated651.IBase0::Method3<object>()
		calli default string(class Generated651.G3_C1123`1<class Generated651.BaseClass1>)
		ldstr "G3_C1123::Method3.MI.14454<System.Object>()"
		ldstr "Generated651.IBase0 on type class Generated651.G3_C1123`1<class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated651.G2_C148`1<class Generated651.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0>::ClassMethod1345<object>()
		calli default string(class Generated651.G2_C148`1<class Generated651.BaseClass0>)
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0> on type class Generated651.G2_C148`1<class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0>::ClassMethod1344<object>()
		calli default string(class Generated651.G2_C148`1<class Generated651.BaseClass0>)
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()"
		ldstr "class G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0> on type class Generated651.G2_C148`1<class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0>::Method6<object>()
		calli default string(class Generated651.G2_C148`1<class Generated651.BaseClass0>)
		ldstr "G2_C148::Method6.6162<System.Object>()"
		ldstr "class G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0> on type class Generated651.G2_C148`1<class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0>::Method5()
		calli default string(class Generated651.G2_C148`1<class Generated651.BaseClass0>)
		ldstr "G1_C11::Method5.4858()"
		ldstr "class G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0> on type class Generated651.G2_C148`1<class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0>::Method4()
		calli default string(class Generated651.G2_C148`1<class Generated651.BaseClass0>)
		ldstr "G2_C148::Method4.6160()"
		ldstr "class G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0> on type class Generated651.G2_C148`1<class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0>::Method7<object>()
		calli default string(class Generated651.G2_C148`1<class Generated651.BaseClass0>)
		ldstr "G2_C148::Method7.6164<System.Object>()"
		ldstr "class G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0> on type class Generated651.G2_C148`1<class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated651.IBase2`2<class Generated651.BaseClass1,class Generated651.BaseClass0>::Method7<object>()
		calli default string(class Generated651.G2_C148`1<class Generated651.BaseClass0>)
		ldstr "G2_C148::Method7.6164<System.Object>()"
		ldstr "class IBase2`2<class Generated651.BaseClass1,class Generated651.BaseClass0> on type class Generated651.G2_C148`1<class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated651.IBase1`1<class Generated651.BaseClass0>::Method4()
		calli default string(class Generated651.G2_C148`1<class Generated651.BaseClass0>)
		ldstr "G2_C148::Method4.6160()"
		ldstr "class IBase1`1<class Generated651.BaseClass0> on type class Generated651.G2_C148`1<class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated651.IBase1`1<class Generated651.BaseClass0>::Method5()
		calli default string(class Generated651.G2_C148`1<class Generated651.BaseClass0>)
		ldstr "G2_C148::Method5.6161()"
		ldstr "class IBase1`1<class Generated651.BaseClass0> on type class Generated651.G2_C148`1<class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated651.IBase1`1<class Generated651.BaseClass0>::Method6<object>()
		calli default string(class Generated651.G2_C148`1<class Generated651.BaseClass0>)
		ldstr "G2_C148::Method6.MI.6163<System.Object>()"
		ldstr "class IBase1`1<class Generated651.BaseClass0> on type class Generated651.G2_C148`1<class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated651.IBase2`2<class Generated651.BaseClass0,class Generated651.BaseClass0>::Method7<object>()
		calli default string(class Generated651.G2_C148`1<class Generated651.BaseClass0>)
		ldstr "G2_C148::Method7.6164<System.Object>()"
		ldstr "class IBase2`2<class Generated651.BaseClass0,class Generated651.BaseClass0> on type class Generated651.G2_C148`1<class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated651.IBase2`2<class Generated651.BaseClass0,class Generated651.BaseClass1>::Method7<object>()
		calli default string(class Generated651.G2_C148`1<class Generated651.BaseClass0>)
		ldstr "G2_C148::Method7.6164<System.Object>()"
		ldstr "class IBase2`2<class Generated651.BaseClass0,class Generated651.BaseClass1> on type class Generated651.G2_C148`1<class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated651.IBase2`2<class Generated651.BaseClass1,class Generated651.BaseClass1>::Method7<object>()
		calli default string(class Generated651.G2_C148`1<class Generated651.BaseClass0>)
		ldstr "G2_C148::Method7.6164<System.Object>()"
		ldstr "class IBase2`2<class Generated651.BaseClass1,class Generated651.BaseClass1> on type class Generated651.G2_C148`1<class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated651.G2_C148`1<class Generated651.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated651.G2_C148`1<class Generated651.BaseClass0>::ClassMethod1619()
		calli default string(class Generated651.G2_C148`1<class Generated651.BaseClass0>)
		ldstr "G2_C148::ClassMethod1619.6165()"
		ldstr "class G2_C148`1<class Generated651.BaseClass0> on type class Generated651.G2_C148`1<class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated651.G2_C148`1<class Generated651.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated651.G2_C148`1<class Generated651.BaseClass0>::Method7<object>()
		calli default string(class Generated651.G2_C148`1<class Generated651.BaseClass0>)
		ldstr "G2_C148::Method7.6164<System.Object>()"
		ldstr "class G2_C148`1<class Generated651.BaseClass0> on type class Generated651.G2_C148`1<class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated651.G2_C148`1<class Generated651.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated651.G2_C148`1<class Generated651.BaseClass0>::Method6<object>()
		calli default string(class Generated651.G2_C148`1<class Generated651.BaseClass0>)
		ldstr "G2_C148::Method6.6162<System.Object>()"
		ldstr "class G2_C148`1<class Generated651.BaseClass0> on type class Generated651.G2_C148`1<class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated651.G2_C148`1<class Generated651.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated651.G2_C148`1<class Generated651.BaseClass0>::Method5()
		calli default string(class Generated651.G2_C148`1<class Generated651.BaseClass0>)
		ldstr "G2_C148::Method5.6161()"
		ldstr "class G2_C148`1<class Generated651.BaseClass0> on type class Generated651.G2_C148`1<class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated651.G2_C148`1<class Generated651.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated651.G2_C148`1<class Generated651.BaseClass0>::Method4()
		calli default string(class Generated651.G2_C148`1<class Generated651.BaseClass0>)
		ldstr "G2_C148::Method4.6160()"
		ldstr "class G2_C148`1<class Generated651.BaseClass0> on type class Generated651.G2_C148`1<class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated651.G2_C148`1<class Generated651.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated651.G2_C148`1<class Generated651.BaseClass0>::ClassMethod1345<object>()
		calli default string(class Generated651.G2_C148`1<class Generated651.BaseClass0>)
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G2_C148`1<class Generated651.BaseClass0> on type class Generated651.G2_C148`1<class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated651.G2_C148`1<class Generated651.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated651.G2_C148`1<class Generated651.BaseClass0>::ClassMethod1344<object>()
		calli default string(class Generated651.G2_C148`1<class Generated651.BaseClass0>)
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()"
		ldstr "class G2_C148`1<class Generated651.BaseClass0> on type class Generated651.G2_C148`1<class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated651.G2_C148`1<class Generated651.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>::ClassMethod1345<object>()
		calli default string(class Generated651.G2_C148`1<class Generated651.BaseClass1>)
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1> on type class Generated651.G2_C148`1<class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>::ClassMethod1344<object>()
		calli default string(class Generated651.G2_C148`1<class Generated651.BaseClass1>)
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()"
		ldstr "class G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1> on type class Generated651.G2_C148`1<class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>::Method6<object>()
		calli default string(class Generated651.G2_C148`1<class Generated651.BaseClass1>)
		ldstr "G2_C148::Method6.6162<System.Object>()"
		ldstr "class G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1> on type class Generated651.G2_C148`1<class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>::Method5()
		calli default string(class Generated651.G2_C148`1<class Generated651.BaseClass1>)
		ldstr "G1_C11::Method5.4858()"
		ldstr "class G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1> on type class Generated651.G2_C148`1<class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>::Method4()
		calli default string(class Generated651.G2_C148`1<class Generated651.BaseClass1>)
		ldstr "G2_C148::Method4.6160()"
		ldstr "class G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1> on type class Generated651.G2_C148`1<class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>::Method7<object>()
		calli default string(class Generated651.G2_C148`1<class Generated651.BaseClass1>)
		ldstr "G2_C148::Method7.6164<System.Object>()"
		ldstr "class G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1> on type class Generated651.G2_C148`1<class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated651.IBase2`2<class Generated651.BaseClass1,class Generated651.BaseClass1>::Method7<object>()
		calli default string(class Generated651.G2_C148`1<class Generated651.BaseClass1>)
		ldstr "G2_C148::Method7.6164<System.Object>()"
		ldstr "class IBase2`2<class Generated651.BaseClass1,class Generated651.BaseClass1> on type class Generated651.G2_C148`1<class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated651.IBase1`1<class Generated651.BaseClass0>::Method4()
		calli default string(class Generated651.G2_C148`1<class Generated651.BaseClass1>)
		ldstr "G2_C148::Method4.6160()"
		ldstr "class IBase1`1<class Generated651.BaseClass0> on type class Generated651.G2_C148`1<class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated651.IBase1`1<class Generated651.BaseClass0>::Method5()
		calli default string(class Generated651.G2_C148`1<class Generated651.BaseClass1>)
		ldstr "G2_C148::Method5.6161()"
		ldstr "class IBase1`1<class Generated651.BaseClass0> on type class Generated651.G2_C148`1<class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated651.IBase1`1<class Generated651.BaseClass0>::Method6<object>()
		calli default string(class Generated651.G2_C148`1<class Generated651.BaseClass1>)
		ldstr "G2_C148::Method6.MI.6163<System.Object>()"
		ldstr "class IBase1`1<class Generated651.BaseClass0> on type class Generated651.G2_C148`1<class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated651.IBase2`2<class Generated651.BaseClass0,class Generated651.BaseClass1>::Method7<object>()
		calli default string(class Generated651.G2_C148`1<class Generated651.BaseClass1>)
		ldstr "G2_C148::Method7.6164<System.Object>()"
		ldstr "class IBase2`2<class Generated651.BaseClass0,class Generated651.BaseClass1> on type class Generated651.G2_C148`1<class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated651.G2_C148`1<class Generated651.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated651.G2_C148`1<class Generated651.BaseClass1>::ClassMethod1619()
		calli default string(class Generated651.G2_C148`1<class Generated651.BaseClass1>)
		ldstr "G2_C148::ClassMethod1619.6165()"
		ldstr "class G2_C148`1<class Generated651.BaseClass1> on type class Generated651.G2_C148`1<class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated651.G2_C148`1<class Generated651.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated651.G2_C148`1<class Generated651.BaseClass1>::Method7<object>()
		calli default string(class Generated651.G2_C148`1<class Generated651.BaseClass1>)
		ldstr "G2_C148::Method7.6164<System.Object>()"
		ldstr "class G2_C148`1<class Generated651.BaseClass1> on type class Generated651.G2_C148`1<class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated651.G2_C148`1<class Generated651.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated651.G2_C148`1<class Generated651.BaseClass1>::Method6<object>()
		calli default string(class Generated651.G2_C148`1<class Generated651.BaseClass1>)
		ldstr "G2_C148::Method6.6162<System.Object>()"
		ldstr "class G2_C148`1<class Generated651.BaseClass1> on type class Generated651.G2_C148`1<class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated651.G2_C148`1<class Generated651.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated651.G2_C148`1<class Generated651.BaseClass1>::Method5()
		calli default string(class Generated651.G2_C148`1<class Generated651.BaseClass1>)
		ldstr "G2_C148::Method5.6161()"
		ldstr "class G2_C148`1<class Generated651.BaseClass1> on type class Generated651.G2_C148`1<class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated651.G2_C148`1<class Generated651.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated651.G2_C148`1<class Generated651.BaseClass1>::Method4()
		calli default string(class Generated651.G2_C148`1<class Generated651.BaseClass1>)
		ldstr "G2_C148::Method4.6160()"
		ldstr "class G2_C148`1<class Generated651.BaseClass1> on type class Generated651.G2_C148`1<class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated651.G2_C148`1<class Generated651.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated651.G2_C148`1<class Generated651.BaseClass1>::ClassMethod1345<object>()
		calli default string(class Generated651.G2_C148`1<class Generated651.BaseClass1>)
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G2_C148`1<class Generated651.BaseClass1> on type class Generated651.G2_C148`1<class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated651.G2_C148`1<class Generated651.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated651.G2_C148`1<class Generated651.BaseClass1>::ClassMethod1344<object>()
		calli default string(class Generated651.G2_C148`1<class Generated651.BaseClass1>)
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()"
		ldstr "class G2_C148`1<class Generated651.BaseClass1> on type class Generated651.G2_C148`1<class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated651.IBase2`2<class Generated651.BaseClass1,class Generated651.BaseClass0>::Method7<object>()
		calli default string(class Generated651.G2_C148`1<class Generated651.BaseClass1>)
		ldstr "G2_C148::Method7.6164<System.Object>()"
		ldstr "class IBase2`2<class Generated651.BaseClass1,class Generated651.BaseClass0> on type class Generated651.G2_C148`1<class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated651.IBase2`2<class Generated651.BaseClass0,class Generated651.BaseClass0>::Method7<object>()
		calli default string(class Generated651.G2_C148`1<class Generated651.BaseClass1>)
		ldstr "G2_C148::Method7.6164<System.Object>()"
		ldstr "class IBase2`2<class Generated651.BaseClass0,class Generated651.BaseClass0> on type class Generated651.G2_C148`1<class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0>::ClassMethod1345<object>()
		calli default string(class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0>)
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0> on type class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0>::ClassMethod1344<object>()
		calli default string(class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0>)
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()"
		ldstr "class G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0> on type class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0>::Method6<object>()
		calli default string(class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0>)
		ldstr "G1_C11::Method6.4859<System.Object>()"
		ldstr "class G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0> on type class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0>::Method5()
		calli default string(class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0>)
		ldstr "G1_C11::Method5.4858()"
		ldstr "class G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0> on type class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0>::Method4()
		calli default string(class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0>)
		ldstr "G1_C11::Method4.4856()"
		ldstr "class G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0> on type class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0>::Method7<object>()
		calli default string(class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0>)
		ldstr "G1_C11::Method7.4854<System.Object>()"
		ldstr "class G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0> on type class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated651.IBase2`2<class Generated651.BaseClass1,class Generated651.BaseClass0>::Method7<object>()
		calli default string(class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0>)
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated651.BaseClass1,class Generated651.BaseClass0> on type class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated651.IBase1`1<class Generated651.BaseClass0>::Method4()
		calli default string(class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0>)
		ldstr "G1_C11::Method4.MI.4857()"
		ldstr "class IBase1`1<class Generated651.BaseClass0> on type class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated651.IBase1`1<class Generated651.BaseClass0>::Method5()
		calli default string(class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0>)
		ldstr "G1_C11::Method5.4858()"
		ldstr "class IBase1`1<class Generated651.BaseClass0> on type class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated651.IBase1`1<class Generated651.BaseClass0>::Method6<object>()
		calli default string(class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0>)
		ldstr "G1_C11::Method6.MI.4860<System.Object>()"
		ldstr "class IBase1`1<class Generated651.BaseClass0> on type class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated651.IBase2`2<class Generated651.BaseClass0,class Generated651.BaseClass0>::Method7<object>()
		calli default string(class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0>)
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated651.BaseClass0,class Generated651.BaseClass0> on type class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated651.IBase2`2<class Generated651.BaseClass0,class Generated651.BaseClass1>::Method7<object>()
		calli default string(class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0>)
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated651.BaseClass0,class Generated651.BaseClass1> on type class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated651.IBase2`2<class Generated651.BaseClass1,class Generated651.BaseClass1>::Method7<object>()
		calli default string(class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0>)
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated651.BaseClass1,class Generated651.BaseClass1> on type class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>::ClassMethod1345<object>()
		calli default string(class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>)
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1> on type class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>::ClassMethod1344<object>()
		calli default string(class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>)
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()"
		ldstr "class G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1> on type class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>::Method6<object>()
		calli default string(class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>)
		ldstr "G1_C11::Method6.4859<System.Object>()"
		ldstr "class G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1> on type class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>::Method5()
		calli default string(class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>)
		ldstr "G1_C11::Method5.4858()"
		ldstr "class G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1> on type class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>::Method4()
		calli default string(class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>)
		ldstr "G1_C11::Method4.4856()"
		ldstr "class G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1> on type class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>::Method7<object>()
		calli default string(class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>)
		ldstr "G1_C11::Method7.4854<System.Object>()"
		ldstr "class G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1> on type class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated651.IBase2`2<class Generated651.BaseClass1,class Generated651.BaseClass1>::Method7<object>()
		calli default string(class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>)
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated651.BaseClass1,class Generated651.BaseClass1> on type class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated651.IBase1`1<class Generated651.BaseClass0>::Method4()
		calli default string(class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>)
		ldstr "G1_C11::Method4.MI.4857()"
		ldstr "class IBase1`1<class Generated651.BaseClass0> on type class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated651.IBase1`1<class Generated651.BaseClass0>::Method5()
		calli default string(class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>)
		ldstr "G1_C11::Method5.4858()"
		ldstr "class IBase1`1<class Generated651.BaseClass0> on type class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated651.IBase1`1<class Generated651.BaseClass0>::Method6<object>()
		calli default string(class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>)
		ldstr "G1_C11::Method6.MI.4860<System.Object>()"
		ldstr "class IBase1`1<class Generated651.BaseClass0> on type class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated651.IBase2`2<class Generated651.BaseClass0,class Generated651.BaseClass1>::Method7<object>()
		calli default string(class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>)
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated651.BaseClass0,class Generated651.BaseClass1> on type class Generated651.G1_C11`2<class Generated651.BaseClass0,class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass0>::ClassMethod1345<object>()
		calli default string(class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass0>)
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass0> on type class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass0>::ClassMethod1344<object>()
		calli default string(class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass0>)
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()"
		ldstr "class G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass0> on type class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass0>::Method6<object>()
		calli default string(class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass0>)
		ldstr "G1_C11::Method6.4859<System.Object>()"
		ldstr "class G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass0> on type class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass0>::Method5()
		calli default string(class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass0>)
		ldstr "G1_C11::Method5.4858()"
		ldstr "class G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass0> on type class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass0>::Method4()
		calli default string(class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass0>)
		ldstr "G1_C11::Method4.4856()"
		ldstr "class G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass0> on type class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass0>::Method7<object>()
		calli default string(class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass0>)
		ldstr "G1_C11::Method7.4854<System.Object>()"
		ldstr "class G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass0> on type class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated651.IBase2`2<class Generated651.BaseClass1,class Generated651.BaseClass0>::Method7<object>()
		calli default string(class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass0>)
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated651.BaseClass1,class Generated651.BaseClass0> on type class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated651.IBase1`1<class Generated651.BaseClass0>::Method4()
		calli default string(class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass0>)
		ldstr "G1_C11::Method4.MI.4857()"
		ldstr "class IBase1`1<class Generated651.BaseClass0> on type class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated651.IBase1`1<class Generated651.BaseClass0>::Method5()
		calli default string(class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass0>)
		ldstr "G1_C11::Method5.4858()"
		ldstr "class IBase1`1<class Generated651.BaseClass0> on type class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated651.IBase1`1<class Generated651.BaseClass0>::Method6<object>()
		calli default string(class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass0>)
		ldstr "G1_C11::Method6.MI.4860<System.Object>()"
		ldstr "class IBase1`1<class Generated651.BaseClass0> on type class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated651.IBase2`2<class Generated651.BaseClass0,class Generated651.BaseClass0>::Method7<object>()
		calli default string(class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass0>)
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated651.BaseClass0,class Generated651.BaseClass0> on type class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated651.IBase2`2<class Generated651.BaseClass0,class Generated651.BaseClass1>::Method7<object>()
		calli default string(class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass0>)
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated651.BaseClass0,class Generated651.BaseClass1> on type class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated651.IBase2`2<class Generated651.BaseClass1,class Generated651.BaseClass1>::Method7<object>()
		calli default string(class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass0>)
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated651.BaseClass1,class Generated651.BaseClass1> on type class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass1>::ClassMethod1345<object>()
		calli default string(class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass1>)
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass1> on type class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass1>::ClassMethod1344<object>()
		calli default string(class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass1>)
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()"
		ldstr "class G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass1> on type class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass1>::Method6<object>()
		calli default string(class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass1>)
		ldstr "G1_C11::Method6.4859<System.Object>()"
		ldstr "class G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass1> on type class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass1>::Method5()
		calli default string(class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass1>)
		ldstr "G1_C11::Method5.4858()"
		ldstr "class G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass1> on type class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass1>::Method4()
		calli default string(class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass1>)
		ldstr "G1_C11::Method4.4856()"
		ldstr "class G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass1> on type class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass1>::Method7<object>()
		calli default string(class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass1>)
		ldstr "G1_C11::Method7.4854<System.Object>()"
		ldstr "class G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass1> on type class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated651.IBase2`2<class Generated651.BaseClass1,class Generated651.BaseClass1>::Method7<object>()
		calli default string(class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass1>)
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated651.BaseClass1,class Generated651.BaseClass1> on type class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated651.IBase1`1<class Generated651.BaseClass0>::Method4()
		calli default string(class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass1>)
		ldstr "G1_C11::Method4.MI.4857()"
		ldstr "class IBase1`1<class Generated651.BaseClass0> on type class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated651.IBase1`1<class Generated651.BaseClass0>::Method5()
		calli default string(class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass1>)
		ldstr "G1_C11::Method5.4858()"
		ldstr "class IBase1`1<class Generated651.BaseClass0> on type class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated651.IBase1`1<class Generated651.BaseClass0>::Method6<object>()
		calli default string(class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass1>)
		ldstr "G1_C11::Method6.MI.4860<System.Object>()"
		ldstr "class IBase1`1<class Generated651.BaseClass0> on type class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated651.IBase2`2<class Generated651.BaseClass0,class Generated651.BaseClass1>::Method7<object>()
		calli default string(class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass1>)
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated651.BaseClass0,class Generated651.BaseClass1> on type class Generated651.G1_C11`2<class Generated651.BaseClass1,class Generated651.BaseClass1>"
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
		call void Generated651.Generated651::MethodCallingTest()
		call void Generated651.Generated651::ConstrainedCallsTest()
		call void Generated651.Generated651::StructConstrainedInterfaceCallsTest()
		call void Generated651.Generated651::CalliTest()
		ldc.i4 100
		ret
	}
}
