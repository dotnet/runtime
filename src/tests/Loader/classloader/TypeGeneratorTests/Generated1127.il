// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.class public Generated1127.BaseClass0 
{
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void [mscorlib]System.Object::.ctor()
		ret
	}
}
.class public Generated1127.BaseClass1 
		extends Generated1127.BaseClass0
{
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void Generated1127.BaseClass0::.ctor()
		ret
	}
}
.class public Generated1127.G3_C1599`1<T0> 
		extends class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>
		implements class Generated1127.IBase2`2<class Generated1127.BaseClass0,!T0> 
{
	.method public hidebysig newslot virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G3_C1599::Method7.17011<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod4588() cil managed noinlining { 
		ldstr "G3_C1599::ClassMethod4588.17012()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod4589() cil managed noinlining { 
		ldstr "G3_C1599::ClassMethod4589.17013()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod4590<M0>() cil managed noinlining { 
		ldstr "G3_C1599::ClassMethod4590.17014<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod4591<M0>() cil managed noinlining { 
		ldstr "G3_C1599::ClassMethod4591.17015<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::.ctor()
		ret
	}
}
.class public Generated1127.G2_C607`2<T0, T1> 
		extends class Generated1127.G1_C11`2<!T0,!T1>
		implements class Generated1127.IBase1`1<!T1>, Generated1127.IBase0 
{
	.method public hidebysig virtual instance string Method4() cil managed noinlining { 
		ldstr "G2_C607::Method4.10584()"
		ret
	}
	.method public hidebysig newslot virtual instance string Method5() cil managed noinlining { 
		ldstr "G2_C607::Method5.10585()"
		ret
	}
	.method public hidebysig virtual instance string Method6<M0>() cil managed noinlining { 
		ldstr "G2_C607::Method6.10586<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string Method0() cil managed noinlining { 
		ldstr "G2_C607::Method0.10587()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'Generated1127.IBase0.Method0'() cil managed noinlining { 
		.override method instance string Generated1127.IBase0::Method0()
		ldstr "G2_C607::Method0.MI.10588()"
		ret
	}
	.method public hidebysig newslot virtual instance string Method1() cil managed noinlining { 
		ldstr "G2_C607::Method1.10589()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'Generated1127.IBase0.Method1'() cil managed noinlining { 
		.override method instance string Generated1127.IBase0::Method1()
		ldstr "G2_C607::Method1.MI.10590()"
		ret
	}
	.method public hidebysig newslot virtual instance string Method2<M0>() cil managed noinlining { 
		ldstr "G2_C607::Method2.10591<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'Generated1127.IBase0.Method2'<M0>() cil managed noinlining { 
		.override method instance string Generated1127.IBase0::Method2<[1]>()
		ldstr "G2_C607::Method2.MI.10592<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig virtual instance string Method3<M0>() cil managed noinlining { 
		ldstr "G2_C607::Method3.10593<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'Generated1127.IBase0.Method3'<M0>() cil managed noinlining { 
		.override method instance string Generated1127.IBase0::Method3<[1]>()
		ldstr "G2_C607::Method3.MI.10594<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod2579() cil managed noinlining { 
		ldstr "G2_C607::ClassMethod2579.10595()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod2580<M0>() cil managed noinlining { 
		ldstr "G2_C607::ClassMethod2580.10596<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'G1_C11<T0,T1>.ClassMethod1344'<M0>() cil managed noinlining { 
		.override method instance string class Generated1127.G1_C11`2<!T0,!T1>::ClassMethod1344<[1]>()
		ldstr "G2_C607::ClassMethod1344.MI.10597<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class Generated1127.G1_C11`2<!T0,!T1>::.ctor()
		ret
	}
}
.class interface public abstract Generated1127.IBase2`2<+T0, -T1> 
{
	.method public hidebysig newslot abstract virtual instance string Method7<M0>() cil managed { }
}
.class public Generated1127.G1_C11`2<T0, T1> 
		implements class Generated1127.IBase2`2<class Generated1127.BaseClass1,!T1>, class Generated1127.IBase1`1<class Generated1127.BaseClass0> 
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
	.method public hidebysig newslot virtual instance string 'IBase2<class Generated1127.BaseClass1,T1>.Method7'<M0>() cil managed noinlining { 
		.override method instance string class Generated1127.IBase2`2<class Generated1127.BaseClass1,!T1>::Method7<[1]>()
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
	.method public hidebysig newslot virtual instance string 'IBase1<class Generated1127.BaseClass0>.Method4'() cil managed noinlining { 
		.override method instance string class Generated1127.IBase1`1<class Generated1127.BaseClass0>::Method4()
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
	.method public hidebysig newslot virtual instance string 'IBase1<class Generated1127.BaseClass0>.Method6'<M0>() cil managed noinlining { 
		.override method instance string class Generated1127.IBase1`1<class Generated1127.BaseClass0>::Method6<[1]>()
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
.class interface public abstract Generated1127.IBase1`1<+T0> 
{
	.method public hidebysig newslot abstract virtual instance string Method4() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method5() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method6<M0>() cil managed { }
}
.class interface public abstract Generated1127.IBase0 
{
	.method public hidebysig newslot abstract virtual instance string Method0() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method1() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method2<M0>() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method3<M0>() cil managed { }
}
.class public auto ansi beforefieldinit Generated1127.Generated1127 {
	.method static void M.BaseClass0<(Generated1127.BaseClass0)W>(!!W inst, string exp) cil managed {
		.maxstack 5
		.locals init (string[] actualResults)
		ldc.i4.s    0
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.BaseClass0<(Generated1127.BaseClass0)W>(!!W inst, string exp)"
		ldc.i4.s 0
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.BaseClass1<(Generated1127.BaseClass1)W>(!!W inst, string exp) cil managed {
		.maxstack 5
		.locals init (string[] actualResults)
		ldc.i4.s    0
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.BaseClass1<(Generated1127.BaseClass1)W>(!!W inst, string exp)"
		ldc.i4.s 0
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1599.T<T0,(class Generated1127.G3_C1599`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 21
		.locals init (string[] actualResults)
		ldc.i4.s    16
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1599.T<T0,(class Generated1127.G3_C1599`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 16
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G3_C1599`1<!!T0>::ClassMethod1344<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G3_C1599`1<!!T0>::ClassMethod1345<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G3_C1599`1<!!T0>::ClassMethod2579()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G3_C1599`1<!!T0>::ClassMethod2580<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G3_C1599`1<!!T0>::ClassMethod4588()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G3_C1599`1<!!T0>::ClassMethod4589()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G3_C1599`1<!!T0>::ClassMethod4590<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G3_C1599`1<!!T0>::ClassMethod4591<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G3_C1599`1<!!T0>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G3_C1599`1<!!T0>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G3_C1599`1<!!T0>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G3_C1599`1<!!T0>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G3_C1599`1<!!T0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     13
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G3_C1599`1<!!T0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     14
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G3_C1599`1<!!T0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     15
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G3_C1599`1<!!T0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1599.A<(class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 21
		.locals init (string[] actualResults)
		ldc.i4.s    16
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1599.A<(class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 16
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>::ClassMethod1344<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>::ClassMethod1345<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>::ClassMethod2579()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>::ClassMethod2580<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>::ClassMethod4588()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>::ClassMethod4589()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>::ClassMethod4590<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>::ClassMethod4591<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     13
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     14
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     15
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1599.B<(class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 21
		.locals init (string[] actualResults)
		ldc.i4.s    16
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1599.B<(class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 16
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>::ClassMethod1344<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>::ClassMethod1345<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>::ClassMethod2579()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>::ClassMethod2580<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>::ClassMethod4588()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>::ClassMethod4589()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>::ClassMethod4590<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>::ClassMethod4591<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     13
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     14
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     15
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C607.T.T<T0,T1,(class Generated1127.G2_C607`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 17
		.locals init (string[] actualResults)
		ldc.i4.s    12
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C607.T.T<T0,T1,(class Generated1127.G2_C607`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 12
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G2_C607`2<!!T0,!!T1>::ClassMethod1344<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G2_C607`2<!!T0,!!T1>::ClassMethod1345<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G2_C607`2<!!T0,!!T1>::ClassMethod2579()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G2_C607`2<!!T0,!!T1>::ClassMethod2580<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G2_C607`2<!!T0,!!T1>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G2_C607`2<!!T0,!!T1>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G2_C607`2<!!T0,!!T1>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G2_C607`2<!!T0,!!T1>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G2_C607`2<!!T0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G2_C607`2<!!T0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G2_C607`2<!!T0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G2_C607`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C607.A.T<T1,(class Generated1127.G2_C607`2<class Generated1127.BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 17
		.locals init (string[] actualResults)
		ldc.i4.s    12
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C607.A.T<T1,(class Generated1127.G2_C607`2<class Generated1127.BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 12
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass0,!!T1>::ClassMethod1344<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass0,!!T1>::ClassMethod1345<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass0,!!T1>::ClassMethod2579()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass0,!!T1>::ClassMethod2580<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass0,!!T1>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass0,!!T1>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass0,!!T1>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass0,!!T1>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C607.A.A<(class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 17
		.locals init (string[] actualResults)
		ldc.i4.s    12
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C607.A.A<(class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 12
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>::ClassMethod1344<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>::ClassMethod1345<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>::ClassMethod2579()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>::ClassMethod2580<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C607.A.B<(class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 17
		.locals init (string[] actualResults)
		ldc.i4.s    12
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C607.A.B<(class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 12
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>::ClassMethod1344<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>::ClassMethod1345<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>::ClassMethod2579()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>::ClassMethod2580<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C607.B.T<T1,(class Generated1127.G2_C607`2<class Generated1127.BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 17
		.locals init (string[] actualResults)
		ldc.i4.s    12
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C607.B.T<T1,(class Generated1127.G2_C607`2<class Generated1127.BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 12
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass1,!!T1>::ClassMethod1344<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass1,!!T1>::ClassMethod1345<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass1,!!T1>::ClassMethod2579()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass1,!!T1>::ClassMethod2580<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass1,!!T1>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass1,!!T1>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass1,!!T1>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass1,!!T1>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass1,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass1,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass1,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C607.B.A<(class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 17
		.locals init (string[] actualResults)
		ldc.i4.s    12
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C607.B.A<(class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 12
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>::ClassMethod1344<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>::ClassMethod1345<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>::ClassMethod2579()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>::ClassMethod2580<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C607.B.B<(class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 17
		.locals init (string[] actualResults)
		ldc.i4.s    12
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C607.B.B<(class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 12
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::ClassMethod1344<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::ClassMethod1345<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::ClassMethod2579()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::ClassMethod2580<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.T.T<T0,T1,(class Generated1127.IBase2`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.T.T<T0,T1,(class Generated1127.IBase2`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.IBase2`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.A.T<T1,(class Generated1127.IBase2`2<class Generated1127.BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.A.T<T1,(class Generated1127.IBase2`2<class Generated1127.BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.IBase2`2<class Generated1127.BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.A.A<(class Generated1127.IBase2`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.A.A<(class Generated1127.IBase2`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.IBase2`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.A.B<(class Generated1127.IBase2`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.A.B<(class Generated1127.IBase2`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.IBase2`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.B.T<T1,(class Generated1127.IBase2`2<class Generated1127.BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.B.T<T1,(class Generated1127.IBase2`2<class Generated1127.BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.IBase2`2<class Generated1127.BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.B.A<(class Generated1127.IBase2`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.B.A<(class Generated1127.IBase2`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.IBase2`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.B.B<(class Generated1127.IBase2`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.B.B<(class Generated1127.IBase2`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.IBase2`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C11.T.T<T0,T1,(class Generated1127.G1_C11`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C11.T.T<T0,T1,(class Generated1127.G1_C11`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G1_C11`2<!!T0,!!T1>::ClassMethod1344<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G1_C11`2<!!T0,!!T1>::ClassMethod1345<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G1_C11`2<!!T0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G1_C11`2<!!T0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G1_C11`2<!!T0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G1_C11`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C11.A.T<T1,(class Generated1127.G1_C11`2<class Generated1127.BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C11.A.T<T1,(class Generated1127.G1_C11`2<class Generated1127.BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass0,!!T1>::ClassMethod1344<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass0,!!T1>::ClassMethod1345<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C11.A.A<(class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C11.A.A<(class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>::ClassMethod1344<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>::ClassMethod1345<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C11.A.B<(class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C11.A.B<(class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>::ClassMethod1344<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>::ClassMethod1345<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C11.B.T<T1,(class Generated1127.G1_C11`2<class Generated1127.BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C11.B.T<T1,(class Generated1127.G1_C11`2<class Generated1127.BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass1,!!T1>::ClassMethod1344<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass1,!!T1>::ClassMethod1345<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass1,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass1,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass1,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C11.B.A<(class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C11.B.A<(class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>::ClassMethod1344<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>::ClassMethod1345<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C11.B.B<(class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C11.B.B<(class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::ClassMethod1344<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::ClassMethod1345<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase1.T<T0,(class Generated1127.IBase1`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase1.T<T0,(class Generated1127.IBase1`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.IBase1`1<!!T0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.IBase1`1<!!T0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.IBase1`1<!!T0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase1.A<(class Generated1127.IBase1`1<class Generated1127.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase1.A<(class Generated1127.IBase1`1<class Generated1127.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.IBase1`1<class Generated1127.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.IBase1`1<class Generated1127.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.IBase1`1<class Generated1127.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase1.B<(class Generated1127.IBase1`1<class Generated1127.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase1.B<(class Generated1127.IBase1`1<class Generated1127.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.IBase1`1<class Generated1127.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.IBase1`1<class Generated1127.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1127.IBase1`1<class Generated1127.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.Generated1127.IBase0<(Generated1127.IBase0)W>(!!W inst, string exp) cil managed {
		.maxstack 9
		.locals init (string[] actualResults)
		ldc.i4.s    4
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.Generated1127.IBase0<(Generated1127.IBase0)W>(!!W inst, string exp)"
		ldc.i4.s 4
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string Generated1127.IBase0::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string Generated1127.IBase0::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string Generated1127.IBase0::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string Generated1127.IBase0::Method3<object>()
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
		newobj instance void class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>
		callvirt instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::ClassMethod1345<object>()
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>
		callvirt instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::ClassMethod1344<object>()
		ldstr "G2_C607::ClassMethod1344.MI.10597<System.Object>()"
		ldstr "class G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>
		callvirt instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::Method6<object>()
		ldstr "G2_C607::Method6.10586<System.Object>()"
		ldstr "class G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>
		callvirt instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::Method5()
		ldstr "G1_C11::Method5.4858()"
		ldstr "class G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>
		callvirt instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::Method4()
		ldstr "G2_C607::Method4.10584()"
		ldstr "class G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>
		callvirt instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::Method7<object>()
		ldstr "G1_C11::Method7.4854<System.Object>()"
		ldstr "class G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1127.IBase2`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::Method7<object>()
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1127.IBase1`1<class Generated1127.BaseClass0>::Method4()
		ldstr "G2_C607::Method4.10584()"
		ldstr "class IBase1`1<class Generated1127.BaseClass0> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1127.IBase1`1<class Generated1127.BaseClass0>::Method5()
		ldstr "G2_C607::Method5.10585()"
		ldstr "class IBase1`1<class Generated1127.BaseClass0> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1127.IBase1`1<class Generated1127.BaseClass0>::Method6<object>()
		ldstr "G2_C607::Method6.10586<System.Object>()"
		ldstr "class IBase1`1<class Generated1127.BaseClass0> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1127.IBase2`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>::Method7<object>()
		ldstr "G3_C1599::Method7.17011<System.Object>()"
		ldstr "class IBase2`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>
		callvirt instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::ClassMethod2580<object>()
		ldstr "G2_C607::ClassMethod2580.10596<System.Object>()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>
		callvirt instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::ClassMethod2579()
		ldstr "G2_C607::ClassMethod2579.10595()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>
		callvirt instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::Method3<object>()
		ldstr "G2_C607::Method3.10593<System.Object>()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>
		callvirt instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::Method2<object>()
		ldstr "G2_C607::Method2.10591<System.Object>()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>
		callvirt instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::Method1()
		ldstr "G2_C607::Method1.10589()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>
		callvirt instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::Method0()
		ldstr "G2_C607::Method0.10587()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>
		callvirt instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::Method6<object>()
		ldstr "G2_C607::Method6.10586<System.Object>()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>
		callvirt instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::Method5()
		ldstr "G2_C607::Method5.10585()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>
		callvirt instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::Method4()
		ldstr "G2_C607::Method4.10584()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>
		callvirt instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::ClassMethod1345<object>()
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>
		callvirt instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::ClassMethod1344<object>()
		ldstr "G2_C607::ClassMethod1344.MI.10597<System.Object>()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>
		callvirt instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::Method7<object>()
		ldstr "G1_C11::Method7.4854<System.Object>()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1127.IBase1`1<class Generated1127.BaseClass1>::Method4()
		ldstr "G2_C607::Method4.10584()"
		ldstr "class IBase1`1<class Generated1127.BaseClass1> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1127.IBase1`1<class Generated1127.BaseClass1>::Method5()
		ldstr "G2_C607::Method5.10585()"
		ldstr "class IBase1`1<class Generated1127.BaseClass1> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1127.IBase1`1<class Generated1127.BaseClass1>::Method6<object>()
		ldstr "G2_C607::Method6.10586<System.Object>()"
		ldstr "class IBase1`1<class Generated1127.BaseClass1> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string Generated1127.IBase0::Method0()
		ldstr "G2_C607::Method0.MI.10588()"
		ldstr "Generated1127.IBase0 on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated1127.IBase0::Method1()
		ldstr "G2_C607::Method1.MI.10590()"
		ldstr "Generated1127.IBase0 on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated1127.IBase0::Method2<object>()
		ldstr "G2_C607::Method2.MI.10592<System.Object>()"
		ldstr "Generated1127.IBase0 on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated1127.IBase0::Method3<object>()
		ldstr "G2_C607::Method3.MI.10594<System.Object>()"
		ldstr "Generated1127.IBase0 on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>
		callvirt instance string class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>::ClassMethod4591<object>()
		ldstr "G3_C1599::ClassMethod4591.17015<System.Object>()"
		ldstr "class G3_C1599`1<class Generated1127.BaseClass0> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>
		callvirt instance string class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>::ClassMethod4590<object>()
		ldstr "G3_C1599::ClassMethod4590.17014<System.Object>()"
		ldstr "class G3_C1599`1<class Generated1127.BaseClass0> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>
		callvirt instance string class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>::ClassMethod4589()
		ldstr "G3_C1599::ClassMethod4589.17013()"
		ldstr "class G3_C1599`1<class Generated1127.BaseClass0> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>
		callvirt instance string class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>::ClassMethod4588()
		ldstr "G3_C1599::ClassMethod4588.17012()"
		ldstr "class G3_C1599`1<class Generated1127.BaseClass0> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>
		callvirt instance string class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>::Method7<object>()
		ldstr "G3_C1599::Method7.17011<System.Object>()"
		ldstr "class G3_C1599`1<class Generated1127.BaseClass0> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>
		callvirt instance string class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>::ClassMethod2580<object>()
		ldstr "G2_C607::ClassMethod2580.10596<System.Object>()"
		ldstr "class G3_C1599`1<class Generated1127.BaseClass0> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>
		callvirt instance string class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>::ClassMethod2579()
		ldstr "G2_C607::ClassMethod2579.10595()"
		ldstr "class G3_C1599`1<class Generated1127.BaseClass0> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>
		callvirt instance string class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>::Method3<object>()
		ldstr "G2_C607::Method3.10593<System.Object>()"
		ldstr "class G3_C1599`1<class Generated1127.BaseClass0> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>
		callvirt instance string class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>::Method2<object>()
		ldstr "G2_C607::Method2.10591<System.Object>()"
		ldstr "class G3_C1599`1<class Generated1127.BaseClass0> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>
		callvirt instance string class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>::Method1()
		ldstr "G2_C607::Method1.10589()"
		ldstr "class G3_C1599`1<class Generated1127.BaseClass0> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>
		callvirt instance string class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>::Method0()
		ldstr "G2_C607::Method0.10587()"
		ldstr "class G3_C1599`1<class Generated1127.BaseClass0> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>
		callvirt instance string class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>::Method6<object>()
		ldstr "G2_C607::Method6.10586<System.Object>()"
		ldstr "class G3_C1599`1<class Generated1127.BaseClass0> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>
		callvirt instance string class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>::Method5()
		ldstr "G2_C607::Method5.10585()"
		ldstr "class G3_C1599`1<class Generated1127.BaseClass0> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>
		callvirt instance string class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>::Method4()
		ldstr "G2_C607::Method4.10584()"
		ldstr "class G3_C1599`1<class Generated1127.BaseClass0> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>
		callvirt instance string class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>::ClassMethod1345<object>()
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G3_C1599`1<class Generated1127.BaseClass0> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>
		callvirt instance string class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>::ClassMethod1344<object>()
		ldstr "G2_C607::ClassMethod1344.MI.10597<System.Object>()"
		ldstr "class G3_C1599`1<class Generated1127.BaseClass0> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1127.IBase2`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>::Method7<object>()
		ldstr "G3_C1599::Method7.17011<System.Object>()"
		ldstr "class IBase2`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>
		callvirt instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::ClassMethod1345<object>()
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>
		callvirt instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::ClassMethod1344<object>()
		ldstr "G2_C607::ClassMethod1344.MI.10597<System.Object>()"
		ldstr "class G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>
		callvirt instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::Method6<object>()
		ldstr "G2_C607::Method6.10586<System.Object>()"
		ldstr "class G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>
		callvirt instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::Method5()
		ldstr "G1_C11::Method5.4858()"
		ldstr "class G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>
		callvirt instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::Method4()
		ldstr "G2_C607::Method4.10584()"
		ldstr "class G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>
		callvirt instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::Method7<object>()
		ldstr "G1_C11::Method7.4854<System.Object>()"
		ldstr "class G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1127.IBase2`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::Method7<object>()
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1127.IBase1`1<class Generated1127.BaseClass0>::Method4()
		ldstr "G2_C607::Method4.10584()"
		ldstr "class IBase1`1<class Generated1127.BaseClass0> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1127.IBase1`1<class Generated1127.BaseClass0>::Method5()
		ldstr "G2_C607::Method5.10585()"
		ldstr "class IBase1`1<class Generated1127.BaseClass0> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1127.IBase1`1<class Generated1127.BaseClass0>::Method6<object>()
		ldstr "G2_C607::Method6.10586<System.Object>()"
		ldstr "class IBase1`1<class Generated1127.BaseClass0> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1127.IBase2`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>::Method7<object>()
		ldstr "G3_C1599::Method7.17011<System.Object>()"
		ldstr "class IBase2`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>
		callvirt instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::ClassMethod2580<object>()
		ldstr "G2_C607::ClassMethod2580.10596<System.Object>()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>
		callvirt instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::ClassMethod2579()
		ldstr "G2_C607::ClassMethod2579.10595()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>
		callvirt instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::Method3<object>()
		ldstr "G2_C607::Method3.10593<System.Object>()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>
		callvirt instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::Method2<object>()
		ldstr "G2_C607::Method2.10591<System.Object>()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>
		callvirt instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::Method1()
		ldstr "G2_C607::Method1.10589()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>
		callvirt instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::Method0()
		ldstr "G2_C607::Method0.10587()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>
		callvirt instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::Method6<object>()
		ldstr "G2_C607::Method6.10586<System.Object>()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>
		callvirt instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::Method5()
		ldstr "G2_C607::Method5.10585()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>
		callvirt instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::Method4()
		ldstr "G2_C607::Method4.10584()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>
		callvirt instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::ClassMethod1345<object>()
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>
		callvirt instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::ClassMethod1344<object>()
		ldstr "G2_C607::ClassMethod1344.MI.10597<System.Object>()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>
		callvirt instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::Method7<object>()
		ldstr "G1_C11::Method7.4854<System.Object>()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1127.IBase1`1<class Generated1127.BaseClass1>::Method4()
		ldstr "G2_C607::Method4.10584()"
		ldstr "class IBase1`1<class Generated1127.BaseClass1> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1127.IBase1`1<class Generated1127.BaseClass1>::Method5()
		ldstr "G2_C607::Method5.10585()"
		ldstr "class IBase1`1<class Generated1127.BaseClass1> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1127.IBase1`1<class Generated1127.BaseClass1>::Method6<object>()
		ldstr "G2_C607::Method6.10586<System.Object>()"
		ldstr "class IBase1`1<class Generated1127.BaseClass1> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string Generated1127.IBase0::Method0()
		ldstr "G2_C607::Method0.MI.10588()"
		ldstr "Generated1127.IBase0 on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated1127.IBase0::Method1()
		ldstr "G2_C607::Method1.MI.10590()"
		ldstr "Generated1127.IBase0 on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated1127.IBase0::Method2<object>()
		ldstr "G2_C607::Method2.MI.10592<System.Object>()"
		ldstr "Generated1127.IBase0 on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated1127.IBase0::Method3<object>()
		ldstr "G2_C607::Method3.MI.10594<System.Object>()"
		ldstr "Generated1127.IBase0 on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>
		callvirt instance string class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>::ClassMethod4591<object>()
		ldstr "G3_C1599::ClassMethod4591.17015<System.Object>()"
		ldstr "class G3_C1599`1<class Generated1127.BaseClass1> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>
		callvirt instance string class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>::ClassMethod4590<object>()
		ldstr "G3_C1599::ClassMethod4590.17014<System.Object>()"
		ldstr "class G3_C1599`1<class Generated1127.BaseClass1> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>
		callvirt instance string class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>::ClassMethod4589()
		ldstr "G3_C1599::ClassMethod4589.17013()"
		ldstr "class G3_C1599`1<class Generated1127.BaseClass1> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>
		callvirt instance string class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>::ClassMethod4588()
		ldstr "G3_C1599::ClassMethod4588.17012()"
		ldstr "class G3_C1599`1<class Generated1127.BaseClass1> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>
		callvirt instance string class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>::Method7<object>()
		ldstr "G3_C1599::Method7.17011<System.Object>()"
		ldstr "class G3_C1599`1<class Generated1127.BaseClass1> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>
		callvirt instance string class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>::ClassMethod2580<object>()
		ldstr "G2_C607::ClassMethod2580.10596<System.Object>()"
		ldstr "class G3_C1599`1<class Generated1127.BaseClass1> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>
		callvirt instance string class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>::ClassMethod2579()
		ldstr "G2_C607::ClassMethod2579.10595()"
		ldstr "class G3_C1599`1<class Generated1127.BaseClass1> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>
		callvirt instance string class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>::Method3<object>()
		ldstr "G2_C607::Method3.10593<System.Object>()"
		ldstr "class G3_C1599`1<class Generated1127.BaseClass1> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>
		callvirt instance string class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>::Method2<object>()
		ldstr "G2_C607::Method2.10591<System.Object>()"
		ldstr "class G3_C1599`1<class Generated1127.BaseClass1> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>
		callvirt instance string class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>::Method1()
		ldstr "G2_C607::Method1.10589()"
		ldstr "class G3_C1599`1<class Generated1127.BaseClass1> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>
		callvirt instance string class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>::Method0()
		ldstr "G2_C607::Method0.10587()"
		ldstr "class G3_C1599`1<class Generated1127.BaseClass1> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>
		callvirt instance string class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>::Method6<object>()
		ldstr "G2_C607::Method6.10586<System.Object>()"
		ldstr "class G3_C1599`1<class Generated1127.BaseClass1> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>
		callvirt instance string class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>::Method5()
		ldstr "G2_C607::Method5.10585()"
		ldstr "class G3_C1599`1<class Generated1127.BaseClass1> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>
		callvirt instance string class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>::Method4()
		ldstr "G2_C607::Method4.10584()"
		ldstr "class G3_C1599`1<class Generated1127.BaseClass1> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>
		callvirt instance string class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>::ClassMethod1345<object>()
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G3_C1599`1<class Generated1127.BaseClass1> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>
		callvirt instance string class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>::ClassMethod1344<object>()
		ldstr "G2_C607::ClassMethod1344.MI.10597<System.Object>()"
		ldstr "class G3_C1599`1<class Generated1127.BaseClass1> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>
		callvirt instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>::ClassMethod1345<object>()
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>
		callvirt instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>::ClassMethod1344<object>()
		ldstr "G2_C607::ClassMethod1344.MI.10597<System.Object>()"
		ldstr "class G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>
		callvirt instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>::Method6<object>()
		ldstr "G2_C607::Method6.10586<System.Object>()"
		ldstr "class G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>
		callvirt instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>::Method5()
		ldstr "G1_C11::Method5.4858()"
		ldstr "class G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>
		callvirt instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>::Method4()
		ldstr "G2_C607::Method4.10584()"
		ldstr "class G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>
		callvirt instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>::Method7<object>()
		ldstr "G1_C11::Method7.4854<System.Object>()"
		ldstr "class G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1127.IBase2`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>::Method7<object>()
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1127.IBase1`1<class Generated1127.BaseClass0>::Method4()
		ldstr "G2_C607::Method4.10584()"
		ldstr "class IBase1`1<class Generated1127.BaseClass0> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1127.IBase1`1<class Generated1127.BaseClass0>::Method5()
		ldstr "G2_C607::Method5.10585()"
		ldstr "class IBase1`1<class Generated1127.BaseClass0> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1127.IBase1`1<class Generated1127.BaseClass0>::Method6<object>()
		ldstr "G2_C607::Method6.10586<System.Object>()"
		ldstr "class IBase1`1<class Generated1127.BaseClass0> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1127.IBase2`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>::Method7<object>()
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1127.IBase2`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>::Method7<object>()
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1127.IBase2`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::Method7<object>()
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>
		callvirt instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>::ClassMethod2580<object>()
		ldstr "G2_C607::ClassMethod2580.10596<System.Object>()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>
		callvirt instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>::ClassMethod2579()
		ldstr "G2_C607::ClassMethod2579.10595()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>
		callvirt instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>::Method3<object>()
		ldstr "G2_C607::Method3.10593<System.Object>()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>
		callvirt instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>::Method2<object>()
		ldstr "G2_C607::Method2.10591<System.Object>()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>
		callvirt instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>::Method1()
		ldstr "G2_C607::Method1.10589()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>
		callvirt instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>::Method0()
		ldstr "G2_C607::Method0.10587()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>
		callvirt instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>::Method6<object>()
		ldstr "G2_C607::Method6.10586<System.Object>()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>
		callvirt instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>::Method5()
		ldstr "G2_C607::Method5.10585()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>
		callvirt instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>::Method4()
		ldstr "G2_C607::Method4.10584()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>
		callvirt instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>::ClassMethod1345<object>()
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>
		callvirt instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>::ClassMethod1344<object>()
		ldstr "G2_C607::ClassMethod1344.MI.10597<System.Object>()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>
		callvirt instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>::Method7<object>()
		ldstr "G1_C11::Method7.4854<System.Object>()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string Generated1127.IBase0::Method0()
		ldstr "G2_C607::Method0.MI.10588()"
		ldstr "Generated1127.IBase0 on type class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated1127.IBase0::Method1()
		ldstr "G2_C607::Method1.MI.10590()"
		ldstr "Generated1127.IBase0 on type class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated1127.IBase0::Method2<object>()
		ldstr "G2_C607::Method2.MI.10592<System.Object>()"
		ldstr "Generated1127.IBase0 on type class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated1127.IBase0::Method3<object>()
		ldstr "G2_C607::Method3.MI.10594<System.Object>()"
		ldstr "Generated1127.IBase0 on type class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>
		callvirt instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>::ClassMethod1345<object>()
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>
		callvirt instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>::ClassMethod1344<object>()
		ldstr "G2_C607::ClassMethod1344.MI.10597<System.Object>()"
		ldstr "class G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>
		callvirt instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>::Method6<object>()
		ldstr "G2_C607::Method6.10586<System.Object>()"
		ldstr "class G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>
		callvirt instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>::Method5()
		ldstr "G1_C11::Method5.4858()"
		ldstr "class G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>
		callvirt instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>::Method4()
		ldstr "G2_C607::Method4.10584()"
		ldstr "class G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>
		callvirt instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>::Method7<object>()
		ldstr "G1_C11::Method7.4854<System.Object>()"
		ldstr "class G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1127.IBase2`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::Method7<object>()
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1127.IBase1`1<class Generated1127.BaseClass0>::Method4()
		ldstr "G2_C607::Method4.10584()"
		ldstr "class IBase1`1<class Generated1127.BaseClass0> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1127.IBase1`1<class Generated1127.BaseClass0>::Method5()
		ldstr "G2_C607::Method5.10585()"
		ldstr "class IBase1`1<class Generated1127.BaseClass0> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1127.IBase1`1<class Generated1127.BaseClass0>::Method6<object>()
		ldstr "G2_C607::Method6.10586<System.Object>()"
		ldstr "class IBase1`1<class Generated1127.BaseClass0> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1127.IBase2`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>::Method7<object>()
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>
		callvirt instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>::ClassMethod2580<object>()
		ldstr "G2_C607::ClassMethod2580.10596<System.Object>()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>
		callvirt instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>::ClassMethod2579()
		ldstr "G2_C607::ClassMethod2579.10595()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>
		callvirt instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>::Method3<object>()
		ldstr "G2_C607::Method3.10593<System.Object>()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>
		callvirt instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>::Method2<object>()
		ldstr "G2_C607::Method2.10591<System.Object>()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>
		callvirt instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>::Method1()
		ldstr "G2_C607::Method1.10589()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>
		callvirt instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>::Method0()
		ldstr "G2_C607::Method0.10587()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>
		callvirt instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>::Method6<object>()
		ldstr "G2_C607::Method6.10586<System.Object>()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>
		callvirt instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>::Method5()
		ldstr "G2_C607::Method5.10585()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>
		callvirt instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>::Method4()
		ldstr "G2_C607::Method4.10584()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>
		callvirt instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>::ClassMethod1345<object>()
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>
		callvirt instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>::ClassMethod1344<object>()
		ldstr "G2_C607::ClassMethod1344.MI.10597<System.Object>()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>
		callvirt instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>::Method7<object>()
		ldstr "G1_C11::Method7.4854<System.Object>()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1127.IBase1`1<class Generated1127.BaseClass1>::Method4()
		ldstr "G2_C607::Method4.10584()"
		ldstr "class IBase1`1<class Generated1127.BaseClass1> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1127.IBase1`1<class Generated1127.BaseClass1>::Method5()
		ldstr "G2_C607::Method5.10585()"
		ldstr "class IBase1`1<class Generated1127.BaseClass1> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1127.IBase1`1<class Generated1127.BaseClass1>::Method6<object>()
		ldstr "G2_C607::Method6.10586<System.Object>()"
		ldstr "class IBase1`1<class Generated1127.BaseClass1> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string Generated1127.IBase0::Method0()
		ldstr "G2_C607::Method0.MI.10588()"
		ldstr "Generated1127.IBase0 on type class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated1127.IBase0::Method1()
		ldstr "G2_C607::Method1.MI.10590()"
		ldstr "Generated1127.IBase0 on type class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated1127.IBase0::Method2<object>()
		ldstr "G2_C607::Method2.MI.10592<System.Object>()"
		ldstr "Generated1127.IBase0 on type class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated1127.IBase0::Method3<object>()
		ldstr "G2_C607::Method3.MI.10594<System.Object>()"
		ldstr "Generated1127.IBase0 on type class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>
		callvirt instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>::ClassMethod1345<object>()
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>
		callvirt instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>::ClassMethod1344<object>()
		ldstr "G2_C607::ClassMethod1344.MI.10597<System.Object>()"
		ldstr "class G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>
		callvirt instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>::Method6<object>()
		ldstr "G2_C607::Method6.10586<System.Object>()"
		ldstr "class G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>
		callvirt instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>::Method5()
		ldstr "G1_C11::Method5.4858()"
		ldstr "class G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>
		callvirt instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>::Method4()
		ldstr "G2_C607::Method4.10584()"
		ldstr "class G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>
		callvirt instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>::Method7<object>()
		ldstr "G1_C11::Method7.4854<System.Object>()"
		ldstr "class G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1127.IBase2`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>::Method7<object>()
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1127.IBase1`1<class Generated1127.BaseClass0>::Method4()
		ldstr "G2_C607::Method4.10584()"
		ldstr "class IBase1`1<class Generated1127.BaseClass0> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1127.IBase1`1<class Generated1127.BaseClass0>::Method5()
		ldstr "G2_C607::Method5.10585()"
		ldstr "class IBase1`1<class Generated1127.BaseClass0> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1127.IBase1`1<class Generated1127.BaseClass0>::Method6<object>()
		ldstr "G2_C607::Method6.10586<System.Object>()"
		ldstr "class IBase1`1<class Generated1127.BaseClass0> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1127.IBase2`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>::Method7<object>()
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1127.IBase2`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>::Method7<object>()
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1127.IBase2`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::Method7<object>()
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>
		callvirt instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>::ClassMethod2580<object>()
		ldstr "G2_C607::ClassMethod2580.10596<System.Object>()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>
		callvirt instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>::ClassMethod2579()
		ldstr "G2_C607::ClassMethod2579.10595()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>
		callvirt instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>::Method3<object>()
		ldstr "G2_C607::Method3.10593<System.Object>()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>
		callvirt instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>::Method2<object>()
		ldstr "G2_C607::Method2.10591<System.Object>()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>
		callvirt instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>::Method1()
		ldstr "G2_C607::Method1.10589()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>
		callvirt instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>::Method0()
		ldstr "G2_C607::Method0.10587()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>
		callvirt instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>::Method6<object>()
		ldstr "G2_C607::Method6.10586<System.Object>()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>
		callvirt instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>::Method5()
		ldstr "G2_C607::Method5.10585()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>
		callvirt instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>::Method4()
		ldstr "G2_C607::Method4.10584()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>
		callvirt instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>::ClassMethod1345<object>()
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>
		callvirt instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>::ClassMethod1344<object>()
		ldstr "G2_C607::ClassMethod1344.MI.10597<System.Object>()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>
		callvirt instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>::Method7<object>()
		ldstr "G1_C11::Method7.4854<System.Object>()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string Generated1127.IBase0::Method0()
		ldstr "G2_C607::Method0.MI.10588()"
		ldstr "Generated1127.IBase0 on type class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated1127.IBase0::Method1()
		ldstr "G2_C607::Method1.MI.10590()"
		ldstr "Generated1127.IBase0 on type class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated1127.IBase0::Method2<object>()
		ldstr "G2_C607::Method2.MI.10592<System.Object>()"
		ldstr "Generated1127.IBase0 on type class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated1127.IBase0::Method3<object>()
		ldstr "G2_C607::Method3.MI.10594<System.Object>()"
		ldstr "Generated1127.IBase0 on type class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>
		callvirt instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::ClassMethod1345<object>()
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>
		callvirt instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::ClassMethod1344<object>()
		ldstr "G2_C607::ClassMethod1344.MI.10597<System.Object>()"
		ldstr "class G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>
		callvirt instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::Method6<object>()
		ldstr "G2_C607::Method6.10586<System.Object>()"
		ldstr "class G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>
		callvirt instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::Method5()
		ldstr "G1_C11::Method5.4858()"
		ldstr "class G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>
		callvirt instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::Method4()
		ldstr "G2_C607::Method4.10584()"
		ldstr "class G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>
		callvirt instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::Method7<object>()
		ldstr "G1_C11::Method7.4854<System.Object>()"
		ldstr "class G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1127.IBase2`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::Method7<object>()
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1127.IBase1`1<class Generated1127.BaseClass0>::Method4()
		ldstr "G2_C607::Method4.10584()"
		ldstr "class IBase1`1<class Generated1127.BaseClass0> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1127.IBase1`1<class Generated1127.BaseClass0>::Method5()
		ldstr "G2_C607::Method5.10585()"
		ldstr "class IBase1`1<class Generated1127.BaseClass0> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1127.IBase1`1<class Generated1127.BaseClass0>::Method6<object>()
		ldstr "G2_C607::Method6.10586<System.Object>()"
		ldstr "class IBase1`1<class Generated1127.BaseClass0> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1127.IBase2`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>::Method7<object>()
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>
		callvirt instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::ClassMethod2580<object>()
		ldstr "G2_C607::ClassMethod2580.10596<System.Object>()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>
		callvirt instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::ClassMethod2579()
		ldstr "G2_C607::ClassMethod2579.10595()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>
		callvirt instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::Method3<object>()
		ldstr "G2_C607::Method3.10593<System.Object>()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>
		callvirt instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::Method2<object>()
		ldstr "G2_C607::Method2.10591<System.Object>()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>
		callvirt instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::Method1()
		ldstr "G2_C607::Method1.10589()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>
		callvirt instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::Method0()
		ldstr "G2_C607::Method0.10587()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>
		callvirt instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::Method6<object>()
		ldstr "G2_C607::Method6.10586<System.Object>()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>
		callvirt instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::Method5()
		ldstr "G2_C607::Method5.10585()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>
		callvirt instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::Method4()
		ldstr "G2_C607::Method4.10584()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>
		callvirt instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::ClassMethod1345<object>()
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>
		callvirt instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::ClassMethod1344<object>()
		ldstr "G2_C607::ClassMethod1344.MI.10597<System.Object>()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>
		callvirt instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::Method7<object>()
		ldstr "G1_C11::Method7.4854<System.Object>()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1127.IBase1`1<class Generated1127.BaseClass1>::Method4()
		ldstr "G2_C607::Method4.10584()"
		ldstr "class IBase1`1<class Generated1127.BaseClass1> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1127.IBase1`1<class Generated1127.BaseClass1>::Method5()
		ldstr "G2_C607::Method5.10585()"
		ldstr "class IBase1`1<class Generated1127.BaseClass1> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1127.IBase1`1<class Generated1127.BaseClass1>::Method6<object>()
		ldstr "G2_C607::Method6.10586<System.Object>()"
		ldstr "class IBase1`1<class Generated1127.BaseClass1> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string Generated1127.IBase0::Method0()
		ldstr "G2_C607::Method0.MI.10588()"
		ldstr "Generated1127.IBase0 on type class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated1127.IBase0::Method1()
		ldstr "G2_C607::Method1.MI.10590()"
		ldstr "Generated1127.IBase0 on type class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated1127.IBase0::Method2<object>()
		ldstr "G2_C607::Method2.MI.10592<System.Object>()"
		ldstr "Generated1127.IBase0 on type class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated1127.IBase0::Method3<object>()
		ldstr "G2_C607::Method3.MI.10594<System.Object>()"
		ldstr "Generated1127.IBase0 on type class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>
		callvirt instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>::ClassMethod1345<object>()
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0> on type class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>
		callvirt instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>::ClassMethod1344<object>()
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()"
		ldstr "class G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0> on type class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>
		callvirt instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>::Method6<object>()
		ldstr "G1_C11::Method6.4859<System.Object>()"
		ldstr "class G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0> on type class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>
		callvirt instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>::Method5()
		ldstr "G1_C11::Method5.4858()"
		ldstr "class G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0> on type class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>
		callvirt instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>::Method4()
		ldstr "G1_C11::Method4.4856()"
		ldstr "class G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0> on type class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>
		callvirt instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>::Method7<object>()
		ldstr "G1_C11::Method7.4854<System.Object>()"
		ldstr "class G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0> on type class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1127.IBase2`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>::Method7<object>()
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0> on type class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1127.IBase1`1<class Generated1127.BaseClass0>::Method4()
		ldstr "G1_C11::Method4.MI.4857()"
		ldstr "class IBase1`1<class Generated1127.BaseClass0> on type class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1127.IBase1`1<class Generated1127.BaseClass0>::Method5()
		ldstr "G1_C11::Method5.4858()"
		ldstr "class IBase1`1<class Generated1127.BaseClass0> on type class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1127.IBase1`1<class Generated1127.BaseClass0>::Method6<object>()
		ldstr "G1_C11::Method6.MI.4860<System.Object>()"
		ldstr "class IBase1`1<class Generated1127.BaseClass0> on type class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1127.IBase2`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>::Method7<object>()
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0> on type class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1127.IBase2`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>::Method7<object>()
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1> on type class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1127.IBase2`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::Method7<object>()
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>
		callvirt instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>::ClassMethod1345<object>()
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1> on type class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>
		callvirt instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>::ClassMethod1344<object>()
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()"
		ldstr "class G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1> on type class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>
		callvirt instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>::Method6<object>()
		ldstr "G1_C11::Method6.4859<System.Object>()"
		ldstr "class G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1> on type class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>
		callvirt instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>::Method5()
		ldstr "G1_C11::Method5.4858()"
		ldstr "class G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1> on type class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>
		callvirt instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>::Method4()
		ldstr "G1_C11::Method4.4856()"
		ldstr "class G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1> on type class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>
		callvirt instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>::Method7<object>()
		ldstr "G1_C11::Method7.4854<System.Object>()"
		ldstr "class G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1> on type class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1127.IBase2`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::Method7<object>()
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1127.IBase1`1<class Generated1127.BaseClass0>::Method4()
		ldstr "G1_C11::Method4.MI.4857()"
		ldstr "class IBase1`1<class Generated1127.BaseClass0> on type class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1127.IBase1`1<class Generated1127.BaseClass0>::Method5()
		ldstr "G1_C11::Method5.4858()"
		ldstr "class IBase1`1<class Generated1127.BaseClass0> on type class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1127.IBase1`1<class Generated1127.BaseClass0>::Method6<object>()
		ldstr "G1_C11::Method6.MI.4860<System.Object>()"
		ldstr "class IBase1`1<class Generated1127.BaseClass0> on type class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1127.IBase2`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>::Method7<object>()
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1> on type class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>
		callvirt instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>::ClassMethod1345<object>()
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0> on type class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>
		callvirt instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>::ClassMethod1344<object>()
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()"
		ldstr "class G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0> on type class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>
		callvirt instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>::Method6<object>()
		ldstr "G1_C11::Method6.4859<System.Object>()"
		ldstr "class G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0> on type class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>
		callvirt instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>::Method5()
		ldstr "G1_C11::Method5.4858()"
		ldstr "class G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0> on type class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>
		callvirt instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>::Method4()
		ldstr "G1_C11::Method4.4856()"
		ldstr "class G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0> on type class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>
		callvirt instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>::Method7<object>()
		ldstr "G1_C11::Method7.4854<System.Object>()"
		ldstr "class G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0> on type class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1127.IBase2`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>::Method7<object>()
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0> on type class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1127.IBase1`1<class Generated1127.BaseClass0>::Method4()
		ldstr "G1_C11::Method4.MI.4857()"
		ldstr "class IBase1`1<class Generated1127.BaseClass0> on type class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1127.IBase1`1<class Generated1127.BaseClass0>::Method5()
		ldstr "G1_C11::Method5.4858()"
		ldstr "class IBase1`1<class Generated1127.BaseClass0> on type class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1127.IBase1`1<class Generated1127.BaseClass0>::Method6<object>()
		ldstr "G1_C11::Method6.MI.4860<System.Object>()"
		ldstr "class IBase1`1<class Generated1127.BaseClass0> on type class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1127.IBase2`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>::Method7<object>()
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0> on type class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1127.IBase2`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>::Method7<object>()
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1> on type class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1127.IBase2`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::Method7<object>()
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>
		callvirt instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::ClassMethod1345<object>()
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>
		callvirt instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::ClassMethod1344<object>()
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()"
		ldstr "class G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>
		callvirt instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::Method6<object>()
		ldstr "G1_C11::Method6.4859<System.Object>()"
		ldstr "class G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>
		callvirt instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::Method5()
		ldstr "G1_C11::Method5.4858()"
		ldstr "class G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>
		callvirt instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::Method4()
		ldstr "G1_C11::Method4.4856()"
		ldstr "class G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>
		callvirt instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::Method7<object>()
		ldstr "G1_C11::Method7.4854<System.Object>()"
		ldstr "class G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1127.IBase2`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::Method7<object>()
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1127.IBase1`1<class Generated1127.BaseClass0>::Method4()
		ldstr "G1_C11::Method4.MI.4857()"
		ldstr "class IBase1`1<class Generated1127.BaseClass0> on type class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1127.IBase1`1<class Generated1127.BaseClass0>::Method5()
		ldstr "G1_C11::Method5.4858()"
		ldstr "class IBase1`1<class Generated1127.BaseClass0> on type class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1127.IBase1`1<class Generated1127.BaseClass0>::Method6<object>()
		ldstr "G1_C11::Method6.MI.4860<System.Object>()"
		ldstr "class IBase1`1<class Generated1127.BaseClass0> on type class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1127.IBase2`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>::Method7<object>()
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1> on type class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>"
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
		newobj instance void class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C607::ClassMethod1344.MI.10597<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G2_C607::Method4.10584()#G1_C11::Method5.4858()#G2_C607::Method6.10586<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Generated1127.Generated1127::M.G1_C11.T.T<class Generated1127.BaseClass1,class Generated1127.BaseClass1,class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C607::ClassMethod1344.MI.10597<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G2_C607::Method4.10584()#G1_C11::Method5.4858()#G2_C607::Method6.10586<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Generated1127.Generated1127::M.G1_C11.B.T<class Generated1127.BaseClass1,class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C607::ClassMethod1344.MI.10597<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G2_C607::Method4.10584()#G1_C11::Method5.4858()#G2_C607::Method6.10586<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Generated1127.Generated1127::M.G1_C11.B.B<class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1127.Generated1127::M.IBase2.T.T<class Generated1127.BaseClass1,class Generated1127.BaseClass1,class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1127.Generated1127::M.IBase2.B.T<class Generated1127.BaseClass1,class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1127.Generated1127::M.IBase2.B.B<class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C607::Method4.10584()#G2_C607::Method5.10585()#G2_C607::Method6.10586<System.Object>()#"
		call void Generated1127.Generated1127::M.IBase1.T<class Generated1127.BaseClass0,class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C607::Method4.10584()#G2_C607::Method5.10585()#G2_C607::Method6.10586<System.Object>()#"
		call void Generated1127.Generated1127::M.IBase1.A<class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1599::Method7.17011<System.Object>()#"
		call void Generated1127.Generated1127::M.IBase2.T.T<class Generated1127.BaseClass0,class Generated1127.BaseClass1,class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G3_C1599::Method7.17011<System.Object>()#"
		call void Generated1127.Generated1127::M.IBase2.A.T<class Generated1127.BaseClass1,class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G3_C1599::Method7.17011<System.Object>()#"
		call void Generated1127.Generated1127::M.IBase2.A.B<class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C607::ClassMethod1344.MI.10597<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G2_C607::ClassMethod2579.10595()#G2_C607::ClassMethod2580.10596<System.Object>()#G2_C607::Method0.10587()#G2_C607::Method1.10589()#G2_C607::Method2.10591<System.Object>()#G2_C607::Method3.10593<System.Object>()#G2_C607::Method4.10584()#G2_C607::Method5.10585()#G2_C607::Method6.10586<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Generated1127.Generated1127::M.G2_C607.T.T<class Generated1127.BaseClass1,class Generated1127.BaseClass1,class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C607::ClassMethod1344.MI.10597<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G2_C607::ClassMethod2579.10595()#G2_C607::ClassMethod2580.10596<System.Object>()#G2_C607::Method0.10587()#G2_C607::Method1.10589()#G2_C607::Method2.10591<System.Object>()#G2_C607::Method3.10593<System.Object>()#G2_C607::Method4.10584()#G2_C607::Method5.10585()#G2_C607::Method6.10586<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Generated1127.Generated1127::M.G2_C607.B.T<class Generated1127.BaseClass1,class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C607::ClassMethod1344.MI.10597<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G2_C607::ClassMethod2579.10595()#G2_C607::ClassMethod2580.10596<System.Object>()#G2_C607::Method0.10587()#G2_C607::Method1.10589()#G2_C607::Method2.10591<System.Object>()#G2_C607::Method3.10593<System.Object>()#G2_C607::Method4.10584()#G2_C607::Method5.10585()#G2_C607::Method6.10586<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Generated1127.Generated1127::M.G2_C607.B.B<class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C607::Method4.10584()#G2_C607::Method5.10585()#G2_C607::Method6.10586<System.Object>()#"
		call void Generated1127.Generated1127::M.IBase1.T<class Generated1127.BaseClass1,class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C607::Method4.10584()#G2_C607::Method5.10585()#G2_C607::Method6.10586<System.Object>()#"
		call void Generated1127.Generated1127::M.IBase1.B<class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C607::Method0.MI.10588()#G2_C607::Method1.MI.10590()#G2_C607::Method2.MI.10592<System.Object>()#G2_C607::Method3.MI.10594<System.Object>()#"
		call void Generated1127.Generated1127::M.Generated1127.IBase0<class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C607::ClassMethod1344.MI.10597<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G2_C607::ClassMethod2579.10595()#G2_C607::ClassMethod2580.10596<System.Object>()#G3_C1599::ClassMethod4588.17012()#G3_C1599::ClassMethod4589.17013()#G3_C1599::ClassMethod4590.17014<System.Object>()#G3_C1599::ClassMethod4591.17015<System.Object>()#G2_C607::Method0.10587()#G2_C607::Method1.10589()#G2_C607::Method2.10591<System.Object>()#G2_C607::Method3.10593<System.Object>()#G2_C607::Method4.10584()#G2_C607::Method5.10585()#G2_C607::Method6.10586<System.Object>()#G3_C1599::Method7.17011<System.Object>()#"
		call void Generated1127.Generated1127::M.G3_C1599.T<class Generated1127.BaseClass0,class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C607::ClassMethod1344.MI.10597<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G2_C607::ClassMethod2579.10595()#G2_C607::ClassMethod2580.10596<System.Object>()#G3_C1599::ClassMethod4588.17012()#G3_C1599::ClassMethod4589.17013()#G3_C1599::ClassMethod4590.17014<System.Object>()#G3_C1599::ClassMethod4591.17015<System.Object>()#G2_C607::Method0.10587()#G2_C607::Method1.10589()#G2_C607::Method2.10591<System.Object>()#G2_C607::Method3.10593<System.Object>()#G2_C607::Method4.10584()#G2_C607::Method5.10585()#G2_C607::Method6.10586<System.Object>()#G3_C1599::Method7.17011<System.Object>()#"
		call void Generated1127.Generated1127::M.G3_C1599.A<class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1599::Method7.17011<System.Object>()#"
		call void Generated1127.Generated1127::M.IBase2.T.T<class Generated1127.BaseClass0,class Generated1127.BaseClass0,class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G3_C1599::Method7.17011<System.Object>()#"
		call void Generated1127.Generated1127::M.IBase2.A.T<class Generated1127.BaseClass0,class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G3_C1599::Method7.17011<System.Object>()#"
		call void Generated1127.Generated1127::M.IBase2.A.A<class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>>(!!0,string)
		newobj instance void class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C607::ClassMethod1344.MI.10597<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G2_C607::Method4.10584()#G1_C11::Method5.4858()#G2_C607::Method6.10586<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Generated1127.Generated1127::M.G1_C11.T.T<class Generated1127.BaseClass1,class Generated1127.BaseClass1,class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C607::ClassMethod1344.MI.10597<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G2_C607::Method4.10584()#G1_C11::Method5.4858()#G2_C607::Method6.10586<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Generated1127.Generated1127::M.G1_C11.B.T<class Generated1127.BaseClass1,class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C607::ClassMethod1344.MI.10597<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G2_C607::Method4.10584()#G1_C11::Method5.4858()#G2_C607::Method6.10586<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Generated1127.Generated1127::M.G1_C11.B.B<class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1127.Generated1127::M.IBase2.T.T<class Generated1127.BaseClass1,class Generated1127.BaseClass1,class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1127.Generated1127::M.IBase2.B.T<class Generated1127.BaseClass1,class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1127.Generated1127::M.IBase2.B.B<class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C607::Method4.10584()#G2_C607::Method5.10585()#G2_C607::Method6.10586<System.Object>()#"
		call void Generated1127.Generated1127::M.IBase1.T<class Generated1127.BaseClass0,class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C607::Method4.10584()#G2_C607::Method5.10585()#G2_C607::Method6.10586<System.Object>()#"
		call void Generated1127.Generated1127::M.IBase1.A<class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1599::Method7.17011<System.Object>()#"
		call void Generated1127.Generated1127::M.IBase2.T.T<class Generated1127.BaseClass0,class Generated1127.BaseClass1,class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G3_C1599::Method7.17011<System.Object>()#"
		call void Generated1127.Generated1127::M.IBase2.A.T<class Generated1127.BaseClass1,class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1599::Method7.17011<System.Object>()#"
		call void Generated1127.Generated1127::M.IBase2.A.B<class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C607::ClassMethod1344.MI.10597<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G2_C607::ClassMethod2579.10595()#G2_C607::ClassMethod2580.10596<System.Object>()#G2_C607::Method0.10587()#G2_C607::Method1.10589()#G2_C607::Method2.10591<System.Object>()#G2_C607::Method3.10593<System.Object>()#G2_C607::Method4.10584()#G2_C607::Method5.10585()#G2_C607::Method6.10586<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Generated1127.Generated1127::M.G2_C607.T.T<class Generated1127.BaseClass1,class Generated1127.BaseClass1,class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C607::ClassMethod1344.MI.10597<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G2_C607::ClassMethod2579.10595()#G2_C607::ClassMethod2580.10596<System.Object>()#G2_C607::Method0.10587()#G2_C607::Method1.10589()#G2_C607::Method2.10591<System.Object>()#G2_C607::Method3.10593<System.Object>()#G2_C607::Method4.10584()#G2_C607::Method5.10585()#G2_C607::Method6.10586<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Generated1127.Generated1127::M.G2_C607.B.T<class Generated1127.BaseClass1,class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C607::ClassMethod1344.MI.10597<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G2_C607::ClassMethod2579.10595()#G2_C607::ClassMethod2580.10596<System.Object>()#G2_C607::Method0.10587()#G2_C607::Method1.10589()#G2_C607::Method2.10591<System.Object>()#G2_C607::Method3.10593<System.Object>()#G2_C607::Method4.10584()#G2_C607::Method5.10585()#G2_C607::Method6.10586<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Generated1127.Generated1127::M.G2_C607.B.B<class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C607::Method4.10584()#G2_C607::Method5.10585()#G2_C607::Method6.10586<System.Object>()#"
		call void Generated1127.Generated1127::M.IBase1.T<class Generated1127.BaseClass1,class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C607::Method4.10584()#G2_C607::Method5.10585()#G2_C607::Method6.10586<System.Object>()#"
		call void Generated1127.Generated1127::M.IBase1.B<class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C607::Method0.MI.10588()#G2_C607::Method1.MI.10590()#G2_C607::Method2.MI.10592<System.Object>()#G2_C607::Method3.MI.10594<System.Object>()#"
		call void Generated1127.Generated1127::M.Generated1127.IBase0<class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C607::ClassMethod1344.MI.10597<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G2_C607::ClassMethod2579.10595()#G2_C607::ClassMethod2580.10596<System.Object>()#G3_C1599::ClassMethod4588.17012()#G3_C1599::ClassMethod4589.17013()#G3_C1599::ClassMethod4590.17014<System.Object>()#G3_C1599::ClassMethod4591.17015<System.Object>()#G2_C607::Method0.10587()#G2_C607::Method1.10589()#G2_C607::Method2.10591<System.Object>()#G2_C607::Method3.10593<System.Object>()#G2_C607::Method4.10584()#G2_C607::Method5.10585()#G2_C607::Method6.10586<System.Object>()#G3_C1599::Method7.17011<System.Object>()#"
		call void Generated1127.Generated1127::M.G3_C1599.T<class Generated1127.BaseClass1,class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C607::ClassMethod1344.MI.10597<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G2_C607::ClassMethod2579.10595()#G2_C607::ClassMethod2580.10596<System.Object>()#G3_C1599::ClassMethod4588.17012()#G3_C1599::ClassMethod4589.17013()#G3_C1599::ClassMethod4590.17014<System.Object>()#G3_C1599::ClassMethod4591.17015<System.Object>()#G2_C607::Method0.10587()#G2_C607::Method1.10589()#G2_C607::Method2.10591<System.Object>()#G2_C607::Method3.10593<System.Object>()#G2_C607::Method4.10584()#G2_C607::Method5.10585()#G2_C607::Method6.10586<System.Object>()#G3_C1599::Method7.17011<System.Object>()#"
		call void Generated1127.Generated1127::M.G3_C1599.B<class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>>(!!0,string)
		newobj instance void class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C607::ClassMethod1344.MI.10597<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G2_C607::Method4.10584()#G1_C11::Method5.4858()#G2_C607::Method6.10586<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Generated1127.Generated1127::M.G1_C11.T.T<class Generated1127.BaseClass0,class Generated1127.BaseClass0,class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C607::ClassMethod1344.MI.10597<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G2_C607::Method4.10584()#G1_C11::Method5.4858()#G2_C607::Method6.10586<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Generated1127.Generated1127::M.G1_C11.A.T<class Generated1127.BaseClass0,class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C607::ClassMethod1344.MI.10597<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G2_C607::Method4.10584()#G1_C11::Method5.4858()#G2_C607::Method6.10586<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Generated1127.Generated1127::M.G1_C11.A.A<class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1127.Generated1127::M.IBase2.T.T<class Generated1127.BaseClass1,class Generated1127.BaseClass0,class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1127.Generated1127::M.IBase2.B.T<class Generated1127.BaseClass0,class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1127.Generated1127::M.IBase2.B.A<class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C607::Method4.10584()#G2_C607::Method5.10585()#G2_C607::Method6.10586<System.Object>()#"
		call void Generated1127.Generated1127::M.IBase1.T<class Generated1127.BaseClass0,class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C607::Method4.10584()#G2_C607::Method5.10585()#G2_C607::Method6.10586<System.Object>()#"
		call void Generated1127.Generated1127::M.IBase1.A<class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1127.Generated1127::M.IBase2.T.T<class Generated1127.BaseClass0,class Generated1127.BaseClass0,class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1127.Generated1127::M.IBase2.A.T<class Generated1127.BaseClass0,class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1127.Generated1127::M.IBase2.A.A<class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1127.Generated1127::M.IBase2.T.T<class Generated1127.BaseClass0,class Generated1127.BaseClass1,class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1127.Generated1127::M.IBase2.A.T<class Generated1127.BaseClass1,class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1127.Generated1127::M.IBase2.A.B<class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1127.Generated1127::M.IBase2.T.T<class Generated1127.BaseClass1,class Generated1127.BaseClass1,class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1127.Generated1127::M.IBase2.B.T<class Generated1127.BaseClass1,class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1127.Generated1127::M.IBase2.B.B<class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C607::ClassMethod1344.MI.10597<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G2_C607::ClassMethod2579.10595()#G2_C607::ClassMethod2580.10596<System.Object>()#G2_C607::Method0.10587()#G2_C607::Method1.10589()#G2_C607::Method2.10591<System.Object>()#G2_C607::Method3.10593<System.Object>()#G2_C607::Method4.10584()#G2_C607::Method5.10585()#G2_C607::Method6.10586<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Generated1127.Generated1127::M.G2_C607.T.T<class Generated1127.BaseClass0,class Generated1127.BaseClass0,class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C607::ClassMethod1344.MI.10597<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G2_C607::ClassMethod2579.10595()#G2_C607::ClassMethod2580.10596<System.Object>()#G2_C607::Method0.10587()#G2_C607::Method1.10589()#G2_C607::Method2.10591<System.Object>()#G2_C607::Method3.10593<System.Object>()#G2_C607::Method4.10584()#G2_C607::Method5.10585()#G2_C607::Method6.10586<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Generated1127.Generated1127::M.G2_C607.A.T<class Generated1127.BaseClass0,class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C607::ClassMethod1344.MI.10597<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G2_C607::ClassMethod2579.10595()#G2_C607::ClassMethod2580.10596<System.Object>()#G2_C607::Method0.10587()#G2_C607::Method1.10589()#G2_C607::Method2.10591<System.Object>()#G2_C607::Method3.10593<System.Object>()#G2_C607::Method4.10584()#G2_C607::Method5.10585()#G2_C607::Method6.10586<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Generated1127.Generated1127::M.G2_C607.A.A<class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C607::Method0.MI.10588()#G2_C607::Method1.MI.10590()#G2_C607::Method2.MI.10592<System.Object>()#G2_C607::Method3.MI.10594<System.Object>()#"
		call void Generated1127.Generated1127::M.Generated1127.IBase0<class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>>(!!0,string)
		newobj instance void class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C607::ClassMethod1344.MI.10597<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G2_C607::Method4.10584()#G1_C11::Method5.4858()#G2_C607::Method6.10586<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Generated1127.Generated1127::M.G1_C11.T.T<class Generated1127.BaseClass0,class Generated1127.BaseClass1,class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C607::ClassMethod1344.MI.10597<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G2_C607::Method4.10584()#G1_C11::Method5.4858()#G2_C607::Method6.10586<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Generated1127.Generated1127::M.G1_C11.A.T<class Generated1127.BaseClass1,class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C607::ClassMethod1344.MI.10597<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G2_C607::Method4.10584()#G1_C11::Method5.4858()#G2_C607::Method6.10586<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Generated1127.Generated1127::M.G1_C11.A.B<class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1127.Generated1127::M.IBase2.T.T<class Generated1127.BaseClass1,class Generated1127.BaseClass1,class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1127.Generated1127::M.IBase2.B.T<class Generated1127.BaseClass1,class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1127.Generated1127::M.IBase2.B.B<class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C607::Method4.10584()#G2_C607::Method5.10585()#G2_C607::Method6.10586<System.Object>()#"
		call void Generated1127.Generated1127::M.IBase1.T<class Generated1127.BaseClass0,class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C607::Method4.10584()#G2_C607::Method5.10585()#G2_C607::Method6.10586<System.Object>()#"
		call void Generated1127.Generated1127::M.IBase1.A<class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1127.Generated1127::M.IBase2.T.T<class Generated1127.BaseClass0,class Generated1127.BaseClass1,class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1127.Generated1127::M.IBase2.A.T<class Generated1127.BaseClass1,class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1127.Generated1127::M.IBase2.A.B<class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C607::ClassMethod1344.MI.10597<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G2_C607::ClassMethod2579.10595()#G2_C607::ClassMethod2580.10596<System.Object>()#G2_C607::Method0.10587()#G2_C607::Method1.10589()#G2_C607::Method2.10591<System.Object>()#G2_C607::Method3.10593<System.Object>()#G2_C607::Method4.10584()#G2_C607::Method5.10585()#G2_C607::Method6.10586<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Generated1127.Generated1127::M.G2_C607.T.T<class Generated1127.BaseClass0,class Generated1127.BaseClass1,class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C607::ClassMethod1344.MI.10597<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G2_C607::ClassMethod2579.10595()#G2_C607::ClassMethod2580.10596<System.Object>()#G2_C607::Method0.10587()#G2_C607::Method1.10589()#G2_C607::Method2.10591<System.Object>()#G2_C607::Method3.10593<System.Object>()#G2_C607::Method4.10584()#G2_C607::Method5.10585()#G2_C607::Method6.10586<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Generated1127.Generated1127::M.G2_C607.A.T<class Generated1127.BaseClass1,class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C607::ClassMethod1344.MI.10597<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G2_C607::ClassMethod2579.10595()#G2_C607::ClassMethod2580.10596<System.Object>()#G2_C607::Method0.10587()#G2_C607::Method1.10589()#G2_C607::Method2.10591<System.Object>()#G2_C607::Method3.10593<System.Object>()#G2_C607::Method4.10584()#G2_C607::Method5.10585()#G2_C607::Method6.10586<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Generated1127.Generated1127::M.G2_C607.A.B<class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C607::Method4.10584()#G2_C607::Method5.10585()#G2_C607::Method6.10586<System.Object>()#"
		call void Generated1127.Generated1127::M.IBase1.T<class Generated1127.BaseClass1,class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C607::Method4.10584()#G2_C607::Method5.10585()#G2_C607::Method6.10586<System.Object>()#"
		call void Generated1127.Generated1127::M.IBase1.B<class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C607::Method0.MI.10588()#G2_C607::Method1.MI.10590()#G2_C607::Method2.MI.10592<System.Object>()#G2_C607::Method3.MI.10594<System.Object>()#"
		call void Generated1127.Generated1127::M.Generated1127.IBase0<class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>>(!!0,string)
		newobj instance void class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C607::ClassMethod1344.MI.10597<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G2_C607::Method4.10584()#G1_C11::Method5.4858()#G2_C607::Method6.10586<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Generated1127.Generated1127::M.G1_C11.T.T<class Generated1127.BaseClass1,class Generated1127.BaseClass0,class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C607::ClassMethod1344.MI.10597<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G2_C607::Method4.10584()#G1_C11::Method5.4858()#G2_C607::Method6.10586<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Generated1127.Generated1127::M.G1_C11.B.T<class Generated1127.BaseClass0,class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C607::ClassMethod1344.MI.10597<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G2_C607::Method4.10584()#G1_C11::Method5.4858()#G2_C607::Method6.10586<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Generated1127.Generated1127::M.G1_C11.B.A<class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1127.Generated1127::M.IBase2.T.T<class Generated1127.BaseClass1,class Generated1127.BaseClass0,class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1127.Generated1127::M.IBase2.B.T<class Generated1127.BaseClass0,class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1127.Generated1127::M.IBase2.B.A<class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C607::Method4.10584()#G2_C607::Method5.10585()#G2_C607::Method6.10586<System.Object>()#"
		call void Generated1127.Generated1127::M.IBase1.T<class Generated1127.BaseClass0,class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C607::Method4.10584()#G2_C607::Method5.10585()#G2_C607::Method6.10586<System.Object>()#"
		call void Generated1127.Generated1127::M.IBase1.A<class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1127.Generated1127::M.IBase2.T.T<class Generated1127.BaseClass0,class Generated1127.BaseClass0,class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1127.Generated1127::M.IBase2.A.T<class Generated1127.BaseClass0,class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1127.Generated1127::M.IBase2.A.A<class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1127.Generated1127::M.IBase2.T.T<class Generated1127.BaseClass0,class Generated1127.BaseClass1,class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1127.Generated1127::M.IBase2.A.T<class Generated1127.BaseClass1,class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1127.Generated1127::M.IBase2.A.B<class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1127.Generated1127::M.IBase2.T.T<class Generated1127.BaseClass1,class Generated1127.BaseClass1,class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1127.Generated1127::M.IBase2.B.T<class Generated1127.BaseClass1,class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1127.Generated1127::M.IBase2.B.B<class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C607::ClassMethod1344.MI.10597<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G2_C607::ClassMethod2579.10595()#G2_C607::ClassMethod2580.10596<System.Object>()#G2_C607::Method0.10587()#G2_C607::Method1.10589()#G2_C607::Method2.10591<System.Object>()#G2_C607::Method3.10593<System.Object>()#G2_C607::Method4.10584()#G2_C607::Method5.10585()#G2_C607::Method6.10586<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Generated1127.Generated1127::M.G2_C607.T.T<class Generated1127.BaseClass1,class Generated1127.BaseClass0,class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C607::ClassMethod1344.MI.10597<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G2_C607::ClassMethod2579.10595()#G2_C607::ClassMethod2580.10596<System.Object>()#G2_C607::Method0.10587()#G2_C607::Method1.10589()#G2_C607::Method2.10591<System.Object>()#G2_C607::Method3.10593<System.Object>()#G2_C607::Method4.10584()#G2_C607::Method5.10585()#G2_C607::Method6.10586<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Generated1127.Generated1127::M.G2_C607.B.T<class Generated1127.BaseClass0,class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C607::ClassMethod1344.MI.10597<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G2_C607::ClassMethod2579.10595()#G2_C607::ClassMethod2580.10596<System.Object>()#G2_C607::Method0.10587()#G2_C607::Method1.10589()#G2_C607::Method2.10591<System.Object>()#G2_C607::Method3.10593<System.Object>()#G2_C607::Method4.10584()#G2_C607::Method5.10585()#G2_C607::Method6.10586<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Generated1127.Generated1127::M.G2_C607.B.A<class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C607::Method0.MI.10588()#G2_C607::Method1.MI.10590()#G2_C607::Method2.MI.10592<System.Object>()#G2_C607::Method3.MI.10594<System.Object>()#"
		call void Generated1127.Generated1127::M.Generated1127.IBase0<class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>>(!!0,string)
		newobj instance void class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C607::ClassMethod1344.MI.10597<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G2_C607::Method4.10584()#G1_C11::Method5.4858()#G2_C607::Method6.10586<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Generated1127.Generated1127::M.G1_C11.T.T<class Generated1127.BaseClass1,class Generated1127.BaseClass1,class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C607::ClassMethod1344.MI.10597<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G2_C607::Method4.10584()#G1_C11::Method5.4858()#G2_C607::Method6.10586<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Generated1127.Generated1127::M.G1_C11.B.T<class Generated1127.BaseClass1,class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C607::ClassMethod1344.MI.10597<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G2_C607::Method4.10584()#G1_C11::Method5.4858()#G2_C607::Method6.10586<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Generated1127.Generated1127::M.G1_C11.B.B<class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1127.Generated1127::M.IBase2.T.T<class Generated1127.BaseClass1,class Generated1127.BaseClass1,class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1127.Generated1127::M.IBase2.B.T<class Generated1127.BaseClass1,class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1127.Generated1127::M.IBase2.B.B<class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C607::Method4.10584()#G2_C607::Method5.10585()#G2_C607::Method6.10586<System.Object>()#"
		call void Generated1127.Generated1127::M.IBase1.T<class Generated1127.BaseClass0,class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C607::Method4.10584()#G2_C607::Method5.10585()#G2_C607::Method6.10586<System.Object>()#"
		call void Generated1127.Generated1127::M.IBase1.A<class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1127.Generated1127::M.IBase2.T.T<class Generated1127.BaseClass0,class Generated1127.BaseClass1,class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1127.Generated1127::M.IBase2.A.T<class Generated1127.BaseClass1,class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1127.Generated1127::M.IBase2.A.B<class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C607::ClassMethod1344.MI.10597<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G2_C607::ClassMethod2579.10595()#G2_C607::ClassMethod2580.10596<System.Object>()#G2_C607::Method0.10587()#G2_C607::Method1.10589()#G2_C607::Method2.10591<System.Object>()#G2_C607::Method3.10593<System.Object>()#G2_C607::Method4.10584()#G2_C607::Method5.10585()#G2_C607::Method6.10586<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Generated1127.Generated1127::M.G2_C607.T.T<class Generated1127.BaseClass1,class Generated1127.BaseClass1,class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C607::ClassMethod1344.MI.10597<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G2_C607::ClassMethod2579.10595()#G2_C607::ClassMethod2580.10596<System.Object>()#G2_C607::Method0.10587()#G2_C607::Method1.10589()#G2_C607::Method2.10591<System.Object>()#G2_C607::Method3.10593<System.Object>()#G2_C607::Method4.10584()#G2_C607::Method5.10585()#G2_C607::Method6.10586<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Generated1127.Generated1127::M.G2_C607.B.T<class Generated1127.BaseClass1,class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C607::ClassMethod1344.MI.10597<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G2_C607::ClassMethod2579.10595()#G2_C607::ClassMethod2580.10596<System.Object>()#G2_C607::Method0.10587()#G2_C607::Method1.10589()#G2_C607::Method2.10591<System.Object>()#G2_C607::Method3.10593<System.Object>()#G2_C607::Method4.10584()#G2_C607::Method5.10585()#G2_C607::Method6.10586<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Generated1127.Generated1127::M.G2_C607.B.B<class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C607::Method4.10584()#G2_C607::Method5.10585()#G2_C607::Method6.10586<System.Object>()#"
		call void Generated1127.Generated1127::M.IBase1.T<class Generated1127.BaseClass1,class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C607::Method4.10584()#G2_C607::Method5.10585()#G2_C607::Method6.10586<System.Object>()#"
		call void Generated1127.Generated1127::M.IBase1.B<class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C607::Method0.MI.10588()#G2_C607::Method1.MI.10590()#G2_C607::Method2.MI.10592<System.Object>()#G2_C607::Method3.MI.10594<System.Object>()#"
		call void Generated1127.Generated1127::M.Generated1127.IBase0<class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>>(!!0,string)
		newobj instance void class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G1_C11::Method4.4856()#G1_C11::Method5.4858()#G1_C11::Method6.4859<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Generated1127.Generated1127::M.G1_C11.T.T<class Generated1127.BaseClass0,class Generated1127.BaseClass0,class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G1_C11::Method4.4856()#G1_C11::Method5.4858()#G1_C11::Method6.4859<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Generated1127.Generated1127::M.G1_C11.A.T<class Generated1127.BaseClass0,class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G1_C11::Method4.4856()#G1_C11::Method5.4858()#G1_C11::Method6.4859<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Generated1127.Generated1127::M.G1_C11.A.A<class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1127.Generated1127::M.IBase2.T.T<class Generated1127.BaseClass1,class Generated1127.BaseClass0,class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1127.Generated1127::M.IBase2.B.T<class Generated1127.BaseClass0,class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1127.Generated1127::M.IBase2.B.A<class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method4.MI.4857()#G1_C11::Method5.4858()#G1_C11::Method6.MI.4860<System.Object>()#"
		call void Generated1127.Generated1127::M.IBase1.T<class Generated1127.BaseClass0,class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method4.MI.4857()#G1_C11::Method5.4858()#G1_C11::Method6.MI.4860<System.Object>()#"
		call void Generated1127.Generated1127::M.IBase1.A<class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1127.Generated1127::M.IBase2.T.T<class Generated1127.BaseClass0,class Generated1127.BaseClass0,class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1127.Generated1127::M.IBase2.A.T<class Generated1127.BaseClass0,class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1127.Generated1127::M.IBase2.A.A<class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1127.Generated1127::M.IBase2.T.T<class Generated1127.BaseClass0,class Generated1127.BaseClass1,class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1127.Generated1127::M.IBase2.A.T<class Generated1127.BaseClass1,class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1127.Generated1127::M.IBase2.A.B<class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1127.Generated1127::M.IBase2.T.T<class Generated1127.BaseClass1,class Generated1127.BaseClass1,class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1127.Generated1127::M.IBase2.B.T<class Generated1127.BaseClass1,class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1127.Generated1127::M.IBase2.B.B<class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>>(!!0,string)
		newobj instance void class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G1_C11::Method4.4856()#G1_C11::Method5.4858()#G1_C11::Method6.4859<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Generated1127.Generated1127::M.G1_C11.T.T<class Generated1127.BaseClass0,class Generated1127.BaseClass1,class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G1_C11::Method4.4856()#G1_C11::Method5.4858()#G1_C11::Method6.4859<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Generated1127.Generated1127::M.G1_C11.A.T<class Generated1127.BaseClass1,class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G1_C11::Method4.4856()#G1_C11::Method5.4858()#G1_C11::Method6.4859<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Generated1127.Generated1127::M.G1_C11.A.B<class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1127.Generated1127::M.IBase2.T.T<class Generated1127.BaseClass1,class Generated1127.BaseClass1,class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1127.Generated1127::M.IBase2.B.T<class Generated1127.BaseClass1,class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1127.Generated1127::M.IBase2.B.B<class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method4.MI.4857()#G1_C11::Method5.4858()#G1_C11::Method6.MI.4860<System.Object>()#"
		call void Generated1127.Generated1127::M.IBase1.T<class Generated1127.BaseClass0,class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method4.MI.4857()#G1_C11::Method5.4858()#G1_C11::Method6.MI.4860<System.Object>()#"
		call void Generated1127.Generated1127::M.IBase1.A<class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1127.Generated1127::M.IBase2.T.T<class Generated1127.BaseClass0,class Generated1127.BaseClass1,class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1127.Generated1127::M.IBase2.A.T<class Generated1127.BaseClass1,class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1127.Generated1127::M.IBase2.A.B<class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>>(!!0,string)
		newobj instance void class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G1_C11::Method4.4856()#G1_C11::Method5.4858()#G1_C11::Method6.4859<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Generated1127.Generated1127::M.G1_C11.T.T<class Generated1127.BaseClass1,class Generated1127.BaseClass0,class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G1_C11::Method4.4856()#G1_C11::Method5.4858()#G1_C11::Method6.4859<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Generated1127.Generated1127::M.G1_C11.B.T<class Generated1127.BaseClass0,class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G1_C11::Method4.4856()#G1_C11::Method5.4858()#G1_C11::Method6.4859<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Generated1127.Generated1127::M.G1_C11.B.A<class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1127.Generated1127::M.IBase2.T.T<class Generated1127.BaseClass1,class Generated1127.BaseClass0,class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1127.Generated1127::M.IBase2.B.T<class Generated1127.BaseClass0,class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1127.Generated1127::M.IBase2.B.A<class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method4.MI.4857()#G1_C11::Method5.4858()#G1_C11::Method6.MI.4860<System.Object>()#"
		call void Generated1127.Generated1127::M.IBase1.T<class Generated1127.BaseClass0,class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method4.MI.4857()#G1_C11::Method5.4858()#G1_C11::Method6.MI.4860<System.Object>()#"
		call void Generated1127.Generated1127::M.IBase1.A<class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1127.Generated1127::M.IBase2.T.T<class Generated1127.BaseClass0,class Generated1127.BaseClass0,class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1127.Generated1127::M.IBase2.A.T<class Generated1127.BaseClass0,class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1127.Generated1127::M.IBase2.A.A<class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1127.Generated1127::M.IBase2.T.T<class Generated1127.BaseClass0,class Generated1127.BaseClass1,class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1127.Generated1127::M.IBase2.A.T<class Generated1127.BaseClass1,class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1127.Generated1127::M.IBase2.A.B<class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1127.Generated1127::M.IBase2.T.T<class Generated1127.BaseClass1,class Generated1127.BaseClass1,class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1127.Generated1127::M.IBase2.B.T<class Generated1127.BaseClass1,class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1127.Generated1127::M.IBase2.B.B<class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>>(!!0,string)
		newobj instance void class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G1_C11::Method4.4856()#G1_C11::Method5.4858()#G1_C11::Method6.4859<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Generated1127.Generated1127::M.G1_C11.T.T<class Generated1127.BaseClass1,class Generated1127.BaseClass1,class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G1_C11::Method4.4856()#G1_C11::Method5.4858()#G1_C11::Method6.4859<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Generated1127.Generated1127::M.G1_C11.B.T<class Generated1127.BaseClass1,class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G1_C11::Method4.4856()#G1_C11::Method5.4858()#G1_C11::Method6.4859<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Generated1127.Generated1127::M.G1_C11.B.B<class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1127.Generated1127::M.IBase2.T.T<class Generated1127.BaseClass1,class Generated1127.BaseClass1,class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1127.Generated1127::M.IBase2.B.T<class Generated1127.BaseClass1,class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1127.Generated1127::M.IBase2.B.B<class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method4.MI.4857()#G1_C11::Method5.4858()#G1_C11::Method6.MI.4860<System.Object>()#"
		call void Generated1127.Generated1127::M.IBase1.T<class Generated1127.BaseClass0,class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method4.MI.4857()#G1_C11::Method5.4858()#G1_C11::Method6.MI.4860<System.Object>()#"
		call void Generated1127.Generated1127::M.IBase1.A<class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1127.Generated1127::M.IBase2.T.T<class Generated1127.BaseClass0,class Generated1127.BaseClass1,class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1127.Generated1127::M.IBase2.A.T<class Generated1127.BaseClass1,class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1127.Generated1127::M.IBase2.A.B<class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>>(!!0,string)
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
		newobj instance void class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::ClassMethod1345<object>()
		calli default string(class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>)
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::ClassMethod1344<object>()
		calli default string(class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>)
		ldstr "G2_C607::ClassMethod1344.MI.10597<System.Object>()"
		ldstr "class G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::Method6<object>()
		calli default string(class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>)
		ldstr "G2_C607::Method6.10586<System.Object>()"
		ldstr "class G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::Method5()
		calli default string(class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>)
		ldstr "G1_C11::Method5.4858()"
		ldstr "class G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::Method4()
		calli default string(class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>)
		ldstr "G2_C607::Method4.10584()"
		ldstr "class G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::Method7<object>()
		calli default string(class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>)
		ldstr "G1_C11::Method7.4854<System.Object>()"
		ldstr "class G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1127.IBase2`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::Method7<object>()
		calli default string(class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>)
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1127.IBase1`1<class Generated1127.BaseClass0>::Method4()
		calli default string(class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>)
		ldstr "G2_C607::Method4.10584()"
		ldstr "class IBase1`1<class Generated1127.BaseClass0> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1127.IBase1`1<class Generated1127.BaseClass0>::Method5()
		calli default string(class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>)
		ldstr "G2_C607::Method5.10585()"
		ldstr "class IBase1`1<class Generated1127.BaseClass0> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1127.IBase1`1<class Generated1127.BaseClass0>::Method6<object>()
		calli default string(class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>)
		ldstr "G2_C607::Method6.10586<System.Object>()"
		ldstr "class IBase1`1<class Generated1127.BaseClass0> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1127.IBase2`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>::Method7<object>()
		calli default string(class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>)
		ldstr "G3_C1599::Method7.17011<System.Object>()"
		ldstr "class IBase2`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::ClassMethod2580<object>()
		calli default string(class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>)
		ldstr "G2_C607::ClassMethod2580.10596<System.Object>()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::ClassMethod2579()
		calli default string(class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>)
		ldstr "G2_C607::ClassMethod2579.10595()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::Method3<object>()
		calli default string(class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>)
		ldstr "G2_C607::Method3.10593<System.Object>()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::Method2<object>()
		calli default string(class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>)
		ldstr "G2_C607::Method2.10591<System.Object>()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::Method1()
		calli default string(class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>)
		ldstr "G2_C607::Method1.10589()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::Method0()
		calli default string(class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>)
		ldstr "G2_C607::Method0.10587()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::Method6<object>()
		calli default string(class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>)
		ldstr "G2_C607::Method6.10586<System.Object>()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::Method5()
		calli default string(class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>)
		ldstr "G2_C607::Method5.10585()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::Method4()
		calli default string(class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>)
		ldstr "G2_C607::Method4.10584()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::ClassMethod1345<object>()
		calli default string(class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>)
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::ClassMethod1344<object>()
		calli default string(class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>)
		ldstr "G2_C607::ClassMethod1344.MI.10597<System.Object>()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::Method7<object>()
		calli default string(class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>)
		ldstr "G1_C11::Method7.4854<System.Object>()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1127.IBase1`1<class Generated1127.BaseClass1>::Method4()
		calli default string(class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>)
		ldstr "G2_C607::Method4.10584()"
		ldstr "class IBase1`1<class Generated1127.BaseClass1> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1127.IBase1`1<class Generated1127.BaseClass1>::Method5()
		calli default string(class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>)
		ldstr "G2_C607::Method5.10585()"
		ldstr "class IBase1`1<class Generated1127.BaseClass1> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1127.IBase1`1<class Generated1127.BaseClass1>::Method6<object>()
		calli default string(class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>)
		ldstr "G2_C607::Method6.10586<System.Object>()"
		ldstr "class IBase1`1<class Generated1127.BaseClass1> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated1127.IBase0::Method0()
		calli default string(class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>)
		ldstr "G2_C607::Method0.MI.10588()"
		ldstr "Generated1127.IBase0 on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated1127.IBase0::Method1()
		calli default string(class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>)
		ldstr "G2_C607::Method1.MI.10590()"
		ldstr "Generated1127.IBase0 on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated1127.IBase0::Method2<object>()
		calli default string(class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>)
		ldstr "G2_C607::Method2.MI.10592<System.Object>()"
		ldstr "Generated1127.IBase0 on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated1127.IBase0::Method3<object>()
		calli default string(class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>)
		ldstr "G2_C607::Method3.MI.10594<System.Object>()"
		ldstr "Generated1127.IBase0 on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>::ClassMethod4591<object>()
		calli default string(class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>)
		ldstr "G3_C1599::ClassMethod4591.17015<System.Object>()"
		ldstr "class G3_C1599`1<class Generated1127.BaseClass0> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>::ClassMethod4590<object>()
		calli default string(class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>)
		ldstr "G3_C1599::ClassMethod4590.17014<System.Object>()"
		ldstr "class G3_C1599`1<class Generated1127.BaseClass0> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>::ClassMethod4589()
		calli default string(class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>)
		ldstr "G3_C1599::ClassMethod4589.17013()"
		ldstr "class G3_C1599`1<class Generated1127.BaseClass0> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>::ClassMethod4588()
		calli default string(class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>)
		ldstr "G3_C1599::ClassMethod4588.17012()"
		ldstr "class G3_C1599`1<class Generated1127.BaseClass0> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>::Method7<object>()
		calli default string(class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>)
		ldstr "G3_C1599::Method7.17011<System.Object>()"
		ldstr "class G3_C1599`1<class Generated1127.BaseClass0> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>::ClassMethod2580<object>()
		calli default string(class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>)
		ldstr "G2_C607::ClassMethod2580.10596<System.Object>()"
		ldstr "class G3_C1599`1<class Generated1127.BaseClass0> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>::ClassMethod2579()
		calli default string(class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>)
		ldstr "G2_C607::ClassMethod2579.10595()"
		ldstr "class G3_C1599`1<class Generated1127.BaseClass0> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>::Method3<object>()
		calli default string(class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>)
		ldstr "G2_C607::Method3.10593<System.Object>()"
		ldstr "class G3_C1599`1<class Generated1127.BaseClass0> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>::Method2<object>()
		calli default string(class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>)
		ldstr "G2_C607::Method2.10591<System.Object>()"
		ldstr "class G3_C1599`1<class Generated1127.BaseClass0> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>::Method1()
		calli default string(class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>)
		ldstr "G2_C607::Method1.10589()"
		ldstr "class G3_C1599`1<class Generated1127.BaseClass0> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>::Method0()
		calli default string(class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>)
		ldstr "G2_C607::Method0.10587()"
		ldstr "class G3_C1599`1<class Generated1127.BaseClass0> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>::Method6<object>()
		calli default string(class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>)
		ldstr "G2_C607::Method6.10586<System.Object>()"
		ldstr "class G3_C1599`1<class Generated1127.BaseClass0> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>::Method5()
		calli default string(class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>)
		ldstr "G2_C607::Method5.10585()"
		ldstr "class G3_C1599`1<class Generated1127.BaseClass0> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>::Method4()
		calli default string(class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>)
		ldstr "G2_C607::Method4.10584()"
		ldstr "class G3_C1599`1<class Generated1127.BaseClass0> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>::ClassMethod1345<object>()
		calli default string(class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>)
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G3_C1599`1<class Generated1127.BaseClass0> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>::ClassMethod1344<object>()
		calli default string(class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>)
		ldstr "G2_C607::ClassMethod1344.MI.10597<System.Object>()"
		ldstr "class G3_C1599`1<class Generated1127.BaseClass0> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1127.IBase2`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>::Method7<object>()
		calli default string(class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>)
		ldstr "G3_C1599::Method7.17011<System.Object>()"
		ldstr "class IBase2`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::ClassMethod1345<object>()
		calli default string(class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>)
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::ClassMethod1344<object>()
		calli default string(class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>)
		ldstr "G2_C607::ClassMethod1344.MI.10597<System.Object>()"
		ldstr "class G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::Method6<object>()
		calli default string(class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>)
		ldstr "G2_C607::Method6.10586<System.Object>()"
		ldstr "class G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::Method5()
		calli default string(class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>)
		ldstr "G1_C11::Method5.4858()"
		ldstr "class G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::Method4()
		calli default string(class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>)
		ldstr "G2_C607::Method4.10584()"
		ldstr "class G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::Method7<object>()
		calli default string(class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>)
		ldstr "G1_C11::Method7.4854<System.Object>()"
		ldstr "class G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1127.IBase2`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::Method7<object>()
		calli default string(class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>)
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1127.IBase1`1<class Generated1127.BaseClass0>::Method4()
		calli default string(class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>)
		ldstr "G2_C607::Method4.10584()"
		ldstr "class IBase1`1<class Generated1127.BaseClass0> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1127.IBase1`1<class Generated1127.BaseClass0>::Method5()
		calli default string(class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>)
		ldstr "G2_C607::Method5.10585()"
		ldstr "class IBase1`1<class Generated1127.BaseClass0> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1127.IBase1`1<class Generated1127.BaseClass0>::Method6<object>()
		calli default string(class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>)
		ldstr "G2_C607::Method6.10586<System.Object>()"
		ldstr "class IBase1`1<class Generated1127.BaseClass0> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1127.IBase2`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>::Method7<object>()
		calli default string(class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>)
		ldstr "G3_C1599::Method7.17011<System.Object>()"
		ldstr "class IBase2`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::ClassMethod2580<object>()
		calli default string(class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>)
		ldstr "G2_C607::ClassMethod2580.10596<System.Object>()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::ClassMethod2579()
		calli default string(class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>)
		ldstr "G2_C607::ClassMethod2579.10595()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::Method3<object>()
		calli default string(class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>)
		ldstr "G2_C607::Method3.10593<System.Object>()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::Method2<object>()
		calli default string(class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>)
		ldstr "G2_C607::Method2.10591<System.Object>()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::Method1()
		calli default string(class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>)
		ldstr "G2_C607::Method1.10589()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::Method0()
		calli default string(class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>)
		ldstr "G2_C607::Method0.10587()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::Method6<object>()
		calli default string(class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>)
		ldstr "G2_C607::Method6.10586<System.Object>()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::Method5()
		calli default string(class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>)
		ldstr "G2_C607::Method5.10585()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::Method4()
		calli default string(class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>)
		ldstr "G2_C607::Method4.10584()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::ClassMethod1345<object>()
		calli default string(class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>)
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::ClassMethod1344<object>()
		calli default string(class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>)
		ldstr "G2_C607::ClassMethod1344.MI.10597<System.Object>()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::Method7<object>()
		calli default string(class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>)
		ldstr "G1_C11::Method7.4854<System.Object>()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1127.IBase1`1<class Generated1127.BaseClass1>::Method4()
		calli default string(class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>)
		ldstr "G2_C607::Method4.10584()"
		ldstr "class IBase1`1<class Generated1127.BaseClass1> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1127.IBase1`1<class Generated1127.BaseClass1>::Method5()
		calli default string(class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>)
		ldstr "G2_C607::Method5.10585()"
		ldstr "class IBase1`1<class Generated1127.BaseClass1> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1127.IBase1`1<class Generated1127.BaseClass1>::Method6<object>()
		calli default string(class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>)
		ldstr "G2_C607::Method6.10586<System.Object>()"
		ldstr "class IBase1`1<class Generated1127.BaseClass1> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated1127.IBase0::Method0()
		calli default string(class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>)
		ldstr "G2_C607::Method0.MI.10588()"
		ldstr "Generated1127.IBase0 on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated1127.IBase0::Method1()
		calli default string(class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>)
		ldstr "G2_C607::Method1.MI.10590()"
		ldstr "Generated1127.IBase0 on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated1127.IBase0::Method2<object>()
		calli default string(class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>)
		ldstr "G2_C607::Method2.MI.10592<System.Object>()"
		ldstr "Generated1127.IBase0 on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated1127.IBase0::Method3<object>()
		calli default string(class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>)
		ldstr "G2_C607::Method3.MI.10594<System.Object>()"
		ldstr "Generated1127.IBase0 on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>::ClassMethod4591<object>()
		calli default string(class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>)
		ldstr "G3_C1599::ClassMethod4591.17015<System.Object>()"
		ldstr "class G3_C1599`1<class Generated1127.BaseClass1> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>::ClassMethod4590<object>()
		calli default string(class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>)
		ldstr "G3_C1599::ClassMethod4590.17014<System.Object>()"
		ldstr "class G3_C1599`1<class Generated1127.BaseClass1> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>::ClassMethod4589()
		calli default string(class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>)
		ldstr "G3_C1599::ClassMethod4589.17013()"
		ldstr "class G3_C1599`1<class Generated1127.BaseClass1> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>::ClassMethod4588()
		calli default string(class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>)
		ldstr "G3_C1599::ClassMethod4588.17012()"
		ldstr "class G3_C1599`1<class Generated1127.BaseClass1> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>::Method7<object>()
		calli default string(class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>)
		ldstr "G3_C1599::Method7.17011<System.Object>()"
		ldstr "class G3_C1599`1<class Generated1127.BaseClass1> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>::ClassMethod2580<object>()
		calli default string(class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>)
		ldstr "G2_C607::ClassMethod2580.10596<System.Object>()"
		ldstr "class G3_C1599`1<class Generated1127.BaseClass1> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>::ClassMethod2579()
		calli default string(class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>)
		ldstr "G2_C607::ClassMethod2579.10595()"
		ldstr "class G3_C1599`1<class Generated1127.BaseClass1> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>::Method3<object>()
		calli default string(class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>)
		ldstr "G2_C607::Method3.10593<System.Object>()"
		ldstr "class G3_C1599`1<class Generated1127.BaseClass1> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>::Method2<object>()
		calli default string(class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>)
		ldstr "G2_C607::Method2.10591<System.Object>()"
		ldstr "class G3_C1599`1<class Generated1127.BaseClass1> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>::Method1()
		calli default string(class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>)
		ldstr "G2_C607::Method1.10589()"
		ldstr "class G3_C1599`1<class Generated1127.BaseClass1> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>::Method0()
		calli default string(class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>)
		ldstr "G2_C607::Method0.10587()"
		ldstr "class G3_C1599`1<class Generated1127.BaseClass1> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>::Method6<object>()
		calli default string(class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>)
		ldstr "G2_C607::Method6.10586<System.Object>()"
		ldstr "class G3_C1599`1<class Generated1127.BaseClass1> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>::Method5()
		calli default string(class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>)
		ldstr "G2_C607::Method5.10585()"
		ldstr "class G3_C1599`1<class Generated1127.BaseClass1> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>::Method4()
		calli default string(class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>)
		ldstr "G2_C607::Method4.10584()"
		ldstr "class G3_C1599`1<class Generated1127.BaseClass1> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>::ClassMethod1345<object>()
		calli default string(class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>)
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G3_C1599`1<class Generated1127.BaseClass1> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>::ClassMethod1344<object>()
		calli default string(class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>)
		ldstr "G2_C607::ClassMethod1344.MI.10597<System.Object>()"
		ldstr "class G3_C1599`1<class Generated1127.BaseClass1> on type class Generated1127.G3_C1599`1<class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>::ClassMethod1345<object>()
		calli default string(class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>)
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>::ClassMethod1344<object>()
		calli default string(class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>)
		ldstr "G2_C607::ClassMethod1344.MI.10597<System.Object>()"
		ldstr "class G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>::Method6<object>()
		calli default string(class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>)
		ldstr "G2_C607::Method6.10586<System.Object>()"
		ldstr "class G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>::Method5()
		calli default string(class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>)
		ldstr "G1_C11::Method5.4858()"
		ldstr "class G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>::Method4()
		calli default string(class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>)
		ldstr "G2_C607::Method4.10584()"
		ldstr "class G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>::Method7<object>()
		calli default string(class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>)
		ldstr "G1_C11::Method7.4854<System.Object>()"
		ldstr "class G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1127.IBase2`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>::Method7<object>()
		calli default string(class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>)
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1127.IBase1`1<class Generated1127.BaseClass0>::Method4()
		calli default string(class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>)
		ldstr "G2_C607::Method4.10584()"
		ldstr "class IBase1`1<class Generated1127.BaseClass0> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1127.IBase1`1<class Generated1127.BaseClass0>::Method5()
		calli default string(class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>)
		ldstr "G2_C607::Method5.10585()"
		ldstr "class IBase1`1<class Generated1127.BaseClass0> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1127.IBase1`1<class Generated1127.BaseClass0>::Method6<object>()
		calli default string(class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>)
		ldstr "G2_C607::Method6.10586<System.Object>()"
		ldstr "class IBase1`1<class Generated1127.BaseClass0> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1127.IBase2`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>::Method7<object>()
		calli default string(class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>)
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1127.IBase2`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>::Method7<object>()
		calli default string(class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>)
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1127.IBase2`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::Method7<object>()
		calli default string(class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>)
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>::ClassMethod2580<object>()
		calli default string(class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>)
		ldstr "G2_C607::ClassMethod2580.10596<System.Object>()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>::ClassMethod2579()
		calli default string(class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>)
		ldstr "G2_C607::ClassMethod2579.10595()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>::Method3<object>()
		calli default string(class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>)
		ldstr "G2_C607::Method3.10593<System.Object>()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>::Method2<object>()
		calli default string(class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>)
		ldstr "G2_C607::Method2.10591<System.Object>()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>::Method1()
		calli default string(class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>)
		ldstr "G2_C607::Method1.10589()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>::Method0()
		calli default string(class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>)
		ldstr "G2_C607::Method0.10587()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>::Method6<object>()
		calli default string(class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>)
		ldstr "G2_C607::Method6.10586<System.Object>()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>::Method5()
		calli default string(class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>)
		ldstr "G2_C607::Method5.10585()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>::Method4()
		calli default string(class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>)
		ldstr "G2_C607::Method4.10584()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>::ClassMethod1345<object>()
		calli default string(class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>)
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>::ClassMethod1344<object>()
		calli default string(class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>)
		ldstr "G2_C607::ClassMethod1344.MI.10597<System.Object>()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>::Method7<object>()
		calli default string(class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>)
		ldstr "G1_C11::Method7.4854<System.Object>()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated1127.IBase0::Method0()
		calli default string(class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>)
		ldstr "G2_C607::Method0.MI.10588()"
		ldstr "Generated1127.IBase0 on type class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated1127.IBase0::Method1()
		calli default string(class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>)
		ldstr "G2_C607::Method1.MI.10590()"
		ldstr "Generated1127.IBase0 on type class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated1127.IBase0::Method2<object>()
		calli default string(class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>)
		ldstr "G2_C607::Method2.MI.10592<System.Object>()"
		ldstr "Generated1127.IBase0 on type class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated1127.IBase0::Method3<object>()
		calli default string(class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>)
		ldstr "G2_C607::Method3.MI.10594<System.Object>()"
		ldstr "Generated1127.IBase0 on type class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>::ClassMethod1345<object>()
		calli default string(class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>)
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>::ClassMethod1344<object>()
		calli default string(class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>)
		ldstr "G2_C607::ClassMethod1344.MI.10597<System.Object>()"
		ldstr "class G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>::Method6<object>()
		calli default string(class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>)
		ldstr "G2_C607::Method6.10586<System.Object>()"
		ldstr "class G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>::Method5()
		calli default string(class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>)
		ldstr "G1_C11::Method5.4858()"
		ldstr "class G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>::Method4()
		calli default string(class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>)
		ldstr "G2_C607::Method4.10584()"
		ldstr "class G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>::Method7<object>()
		calli default string(class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>)
		ldstr "G1_C11::Method7.4854<System.Object>()"
		ldstr "class G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1127.IBase2`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::Method7<object>()
		calli default string(class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>)
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1127.IBase1`1<class Generated1127.BaseClass0>::Method4()
		calli default string(class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>)
		ldstr "G2_C607::Method4.10584()"
		ldstr "class IBase1`1<class Generated1127.BaseClass0> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1127.IBase1`1<class Generated1127.BaseClass0>::Method5()
		calli default string(class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>)
		ldstr "G2_C607::Method5.10585()"
		ldstr "class IBase1`1<class Generated1127.BaseClass0> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1127.IBase1`1<class Generated1127.BaseClass0>::Method6<object>()
		calli default string(class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>)
		ldstr "G2_C607::Method6.10586<System.Object>()"
		ldstr "class IBase1`1<class Generated1127.BaseClass0> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1127.IBase2`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>::Method7<object>()
		calli default string(class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>)
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>::ClassMethod2580<object>()
		calli default string(class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>)
		ldstr "G2_C607::ClassMethod2580.10596<System.Object>()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>::ClassMethod2579()
		calli default string(class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>)
		ldstr "G2_C607::ClassMethod2579.10595()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>::Method3<object>()
		calli default string(class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>)
		ldstr "G2_C607::Method3.10593<System.Object>()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>::Method2<object>()
		calli default string(class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>)
		ldstr "G2_C607::Method2.10591<System.Object>()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>::Method1()
		calli default string(class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>)
		ldstr "G2_C607::Method1.10589()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>::Method0()
		calli default string(class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>)
		ldstr "G2_C607::Method0.10587()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>::Method6<object>()
		calli default string(class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>)
		ldstr "G2_C607::Method6.10586<System.Object>()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>::Method5()
		calli default string(class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>)
		ldstr "G2_C607::Method5.10585()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>::Method4()
		calli default string(class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>)
		ldstr "G2_C607::Method4.10584()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>::ClassMethod1345<object>()
		calli default string(class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>)
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>::ClassMethod1344<object>()
		calli default string(class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>)
		ldstr "G2_C607::ClassMethod1344.MI.10597<System.Object>()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>::Method7<object>()
		calli default string(class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>)
		ldstr "G1_C11::Method7.4854<System.Object>()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1127.IBase1`1<class Generated1127.BaseClass1>::Method4()
		calli default string(class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>)
		ldstr "G2_C607::Method4.10584()"
		ldstr "class IBase1`1<class Generated1127.BaseClass1> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1127.IBase1`1<class Generated1127.BaseClass1>::Method5()
		calli default string(class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>)
		ldstr "G2_C607::Method5.10585()"
		ldstr "class IBase1`1<class Generated1127.BaseClass1> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1127.IBase1`1<class Generated1127.BaseClass1>::Method6<object>()
		calli default string(class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>)
		ldstr "G2_C607::Method6.10586<System.Object>()"
		ldstr "class IBase1`1<class Generated1127.BaseClass1> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated1127.IBase0::Method0()
		calli default string(class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>)
		ldstr "G2_C607::Method0.MI.10588()"
		ldstr "Generated1127.IBase0 on type class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated1127.IBase0::Method1()
		calli default string(class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>)
		ldstr "G2_C607::Method1.MI.10590()"
		ldstr "Generated1127.IBase0 on type class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated1127.IBase0::Method2<object>()
		calli default string(class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>)
		ldstr "G2_C607::Method2.MI.10592<System.Object>()"
		ldstr "Generated1127.IBase0 on type class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated1127.IBase0::Method3<object>()
		calli default string(class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>)
		ldstr "G2_C607::Method3.MI.10594<System.Object>()"
		ldstr "Generated1127.IBase0 on type class Generated1127.G2_C607`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>::ClassMethod1345<object>()
		calli default string(class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>)
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>::ClassMethod1344<object>()
		calli default string(class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>)
		ldstr "G2_C607::ClassMethod1344.MI.10597<System.Object>()"
		ldstr "class G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>::Method6<object>()
		calli default string(class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>)
		ldstr "G2_C607::Method6.10586<System.Object>()"
		ldstr "class G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>::Method5()
		calli default string(class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>)
		ldstr "G1_C11::Method5.4858()"
		ldstr "class G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>::Method4()
		calli default string(class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>)
		ldstr "G2_C607::Method4.10584()"
		ldstr "class G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>::Method7<object>()
		calli default string(class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>)
		ldstr "G1_C11::Method7.4854<System.Object>()"
		ldstr "class G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1127.IBase2`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>::Method7<object>()
		calli default string(class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>)
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1127.IBase1`1<class Generated1127.BaseClass0>::Method4()
		calli default string(class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>)
		ldstr "G2_C607::Method4.10584()"
		ldstr "class IBase1`1<class Generated1127.BaseClass0> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1127.IBase1`1<class Generated1127.BaseClass0>::Method5()
		calli default string(class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>)
		ldstr "G2_C607::Method5.10585()"
		ldstr "class IBase1`1<class Generated1127.BaseClass0> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1127.IBase1`1<class Generated1127.BaseClass0>::Method6<object>()
		calli default string(class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>)
		ldstr "G2_C607::Method6.10586<System.Object>()"
		ldstr "class IBase1`1<class Generated1127.BaseClass0> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1127.IBase2`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>::Method7<object>()
		calli default string(class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>)
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1127.IBase2`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>::Method7<object>()
		calli default string(class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>)
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1127.IBase2`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::Method7<object>()
		calli default string(class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>)
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>::ClassMethod2580<object>()
		calli default string(class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>)
		ldstr "G2_C607::ClassMethod2580.10596<System.Object>()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>::ClassMethod2579()
		calli default string(class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>)
		ldstr "G2_C607::ClassMethod2579.10595()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>::Method3<object>()
		calli default string(class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>)
		ldstr "G2_C607::Method3.10593<System.Object>()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>::Method2<object>()
		calli default string(class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>)
		ldstr "G2_C607::Method2.10591<System.Object>()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>::Method1()
		calli default string(class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>)
		ldstr "G2_C607::Method1.10589()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>::Method0()
		calli default string(class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>)
		ldstr "G2_C607::Method0.10587()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>::Method6<object>()
		calli default string(class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>)
		ldstr "G2_C607::Method6.10586<System.Object>()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>::Method5()
		calli default string(class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>)
		ldstr "G2_C607::Method5.10585()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>::Method4()
		calli default string(class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>)
		ldstr "G2_C607::Method4.10584()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>::ClassMethod1345<object>()
		calli default string(class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>)
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>::ClassMethod1344<object>()
		calli default string(class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>)
		ldstr "G2_C607::ClassMethod1344.MI.10597<System.Object>()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>::Method7<object>()
		calli default string(class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>)
		ldstr "G1_C11::Method7.4854<System.Object>()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated1127.IBase0::Method0()
		calli default string(class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>)
		ldstr "G2_C607::Method0.MI.10588()"
		ldstr "Generated1127.IBase0 on type class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated1127.IBase0::Method1()
		calli default string(class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>)
		ldstr "G2_C607::Method1.MI.10590()"
		ldstr "Generated1127.IBase0 on type class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated1127.IBase0::Method2<object>()
		calli default string(class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>)
		ldstr "G2_C607::Method2.MI.10592<System.Object>()"
		ldstr "Generated1127.IBase0 on type class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated1127.IBase0::Method3<object>()
		calli default string(class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>)
		ldstr "G2_C607::Method3.MI.10594<System.Object>()"
		ldstr "Generated1127.IBase0 on type class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::ClassMethod1345<object>()
		calli default string(class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>)
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::ClassMethod1344<object>()
		calli default string(class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>)
		ldstr "G2_C607::ClassMethod1344.MI.10597<System.Object>()"
		ldstr "class G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::Method6<object>()
		calli default string(class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>)
		ldstr "G2_C607::Method6.10586<System.Object>()"
		ldstr "class G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::Method5()
		calli default string(class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>)
		ldstr "G1_C11::Method5.4858()"
		ldstr "class G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::Method4()
		calli default string(class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>)
		ldstr "G2_C607::Method4.10584()"
		ldstr "class G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::Method7<object>()
		calli default string(class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>)
		ldstr "G1_C11::Method7.4854<System.Object>()"
		ldstr "class G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1127.IBase2`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::Method7<object>()
		calli default string(class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>)
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1127.IBase1`1<class Generated1127.BaseClass0>::Method4()
		calli default string(class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>)
		ldstr "G2_C607::Method4.10584()"
		ldstr "class IBase1`1<class Generated1127.BaseClass0> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1127.IBase1`1<class Generated1127.BaseClass0>::Method5()
		calli default string(class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>)
		ldstr "G2_C607::Method5.10585()"
		ldstr "class IBase1`1<class Generated1127.BaseClass0> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1127.IBase1`1<class Generated1127.BaseClass0>::Method6<object>()
		calli default string(class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>)
		ldstr "G2_C607::Method6.10586<System.Object>()"
		ldstr "class IBase1`1<class Generated1127.BaseClass0> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1127.IBase2`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>::Method7<object>()
		calli default string(class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>)
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::ClassMethod2580<object>()
		calli default string(class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>)
		ldstr "G2_C607::ClassMethod2580.10596<System.Object>()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::ClassMethod2579()
		calli default string(class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>)
		ldstr "G2_C607::ClassMethod2579.10595()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::Method3<object>()
		calli default string(class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>)
		ldstr "G2_C607::Method3.10593<System.Object>()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::Method2<object>()
		calli default string(class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>)
		ldstr "G2_C607::Method2.10591<System.Object>()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::Method1()
		calli default string(class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>)
		ldstr "G2_C607::Method1.10589()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::Method0()
		calli default string(class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>)
		ldstr "G2_C607::Method0.10587()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::Method6<object>()
		calli default string(class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>)
		ldstr "G2_C607::Method6.10586<System.Object>()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::Method5()
		calli default string(class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>)
		ldstr "G2_C607::Method5.10585()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::Method4()
		calli default string(class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>)
		ldstr "G2_C607::Method4.10584()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::ClassMethod1345<object>()
		calli default string(class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>)
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::ClassMethod1344<object>()
		calli default string(class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>)
		ldstr "G2_C607::ClassMethod1344.MI.10597<System.Object>()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::Method7<object>()
		calli default string(class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>)
		ldstr "G1_C11::Method7.4854<System.Object>()"
		ldstr "class G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1127.IBase1`1<class Generated1127.BaseClass1>::Method4()
		calli default string(class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>)
		ldstr "G2_C607::Method4.10584()"
		ldstr "class IBase1`1<class Generated1127.BaseClass1> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1127.IBase1`1<class Generated1127.BaseClass1>::Method5()
		calli default string(class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>)
		ldstr "G2_C607::Method5.10585()"
		ldstr "class IBase1`1<class Generated1127.BaseClass1> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1127.IBase1`1<class Generated1127.BaseClass1>::Method6<object>()
		calli default string(class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>)
		ldstr "G2_C607::Method6.10586<System.Object>()"
		ldstr "class IBase1`1<class Generated1127.BaseClass1> on type class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated1127.IBase0::Method0()
		calli default string(class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>)
		ldstr "G2_C607::Method0.MI.10588()"
		ldstr "Generated1127.IBase0 on type class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated1127.IBase0::Method1()
		calli default string(class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>)
		ldstr "G2_C607::Method1.MI.10590()"
		ldstr "Generated1127.IBase0 on type class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated1127.IBase0::Method2<object>()
		calli default string(class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>)
		ldstr "G2_C607::Method2.MI.10592<System.Object>()"
		ldstr "Generated1127.IBase0 on type class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated1127.IBase0::Method3<object>()
		calli default string(class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>)
		ldstr "G2_C607::Method3.MI.10594<System.Object>()"
		ldstr "Generated1127.IBase0 on type class Generated1127.G2_C607`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>::ClassMethod1345<object>()
		calli default string(class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>)
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0> on type class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>::ClassMethod1344<object>()
		calli default string(class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>)
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()"
		ldstr "class G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0> on type class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>::Method6<object>()
		calli default string(class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>)
		ldstr "G1_C11::Method6.4859<System.Object>()"
		ldstr "class G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0> on type class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>::Method5()
		calli default string(class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>)
		ldstr "G1_C11::Method5.4858()"
		ldstr "class G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0> on type class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>::Method4()
		calli default string(class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>)
		ldstr "G1_C11::Method4.4856()"
		ldstr "class G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0> on type class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>::Method7<object>()
		calli default string(class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>)
		ldstr "G1_C11::Method7.4854<System.Object>()"
		ldstr "class G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0> on type class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1127.IBase2`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>::Method7<object>()
		calli default string(class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>)
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0> on type class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1127.IBase1`1<class Generated1127.BaseClass0>::Method4()
		calli default string(class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>)
		ldstr "G1_C11::Method4.MI.4857()"
		ldstr "class IBase1`1<class Generated1127.BaseClass0> on type class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1127.IBase1`1<class Generated1127.BaseClass0>::Method5()
		calli default string(class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>)
		ldstr "G1_C11::Method5.4858()"
		ldstr "class IBase1`1<class Generated1127.BaseClass0> on type class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1127.IBase1`1<class Generated1127.BaseClass0>::Method6<object>()
		calli default string(class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>)
		ldstr "G1_C11::Method6.MI.4860<System.Object>()"
		ldstr "class IBase1`1<class Generated1127.BaseClass0> on type class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1127.IBase2`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>::Method7<object>()
		calli default string(class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>)
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0> on type class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1127.IBase2`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>::Method7<object>()
		calli default string(class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>)
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1> on type class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1127.IBase2`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::Method7<object>()
		calli default string(class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>)
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>::ClassMethod1345<object>()
		calli default string(class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>)
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1> on type class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>::ClassMethod1344<object>()
		calli default string(class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>)
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()"
		ldstr "class G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1> on type class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>::Method6<object>()
		calli default string(class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>)
		ldstr "G1_C11::Method6.4859<System.Object>()"
		ldstr "class G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1> on type class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>::Method5()
		calli default string(class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>)
		ldstr "G1_C11::Method5.4858()"
		ldstr "class G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1> on type class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>::Method4()
		calli default string(class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>)
		ldstr "G1_C11::Method4.4856()"
		ldstr "class G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1> on type class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>::Method7<object>()
		calli default string(class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>)
		ldstr "G1_C11::Method7.4854<System.Object>()"
		ldstr "class G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1> on type class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1127.IBase2`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::Method7<object>()
		calli default string(class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>)
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1127.IBase1`1<class Generated1127.BaseClass0>::Method4()
		calli default string(class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>)
		ldstr "G1_C11::Method4.MI.4857()"
		ldstr "class IBase1`1<class Generated1127.BaseClass0> on type class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1127.IBase1`1<class Generated1127.BaseClass0>::Method5()
		calli default string(class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>)
		ldstr "G1_C11::Method5.4858()"
		ldstr "class IBase1`1<class Generated1127.BaseClass0> on type class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1127.IBase1`1<class Generated1127.BaseClass0>::Method6<object>()
		calli default string(class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>)
		ldstr "G1_C11::Method6.MI.4860<System.Object>()"
		ldstr "class IBase1`1<class Generated1127.BaseClass0> on type class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1127.IBase2`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>::Method7<object>()
		calli default string(class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>)
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1> on type class Generated1127.G1_C11`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>::ClassMethod1345<object>()
		calli default string(class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>)
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0> on type class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>::ClassMethod1344<object>()
		calli default string(class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>)
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()"
		ldstr "class G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0> on type class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>::Method6<object>()
		calli default string(class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>)
		ldstr "G1_C11::Method6.4859<System.Object>()"
		ldstr "class G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0> on type class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>::Method5()
		calli default string(class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>)
		ldstr "G1_C11::Method5.4858()"
		ldstr "class G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0> on type class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>::Method4()
		calli default string(class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>)
		ldstr "G1_C11::Method4.4856()"
		ldstr "class G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0> on type class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>::Method7<object>()
		calli default string(class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>)
		ldstr "G1_C11::Method7.4854<System.Object>()"
		ldstr "class G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0> on type class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1127.IBase2`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>::Method7<object>()
		calli default string(class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>)
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0> on type class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1127.IBase1`1<class Generated1127.BaseClass0>::Method4()
		calli default string(class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>)
		ldstr "G1_C11::Method4.MI.4857()"
		ldstr "class IBase1`1<class Generated1127.BaseClass0> on type class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1127.IBase1`1<class Generated1127.BaseClass0>::Method5()
		calli default string(class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>)
		ldstr "G1_C11::Method5.4858()"
		ldstr "class IBase1`1<class Generated1127.BaseClass0> on type class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1127.IBase1`1<class Generated1127.BaseClass0>::Method6<object>()
		calli default string(class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>)
		ldstr "G1_C11::Method6.MI.4860<System.Object>()"
		ldstr "class IBase1`1<class Generated1127.BaseClass0> on type class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1127.IBase2`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0>::Method7<object>()
		calli default string(class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>)
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated1127.BaseClass0,class Generated1127.BaseClass0> on type class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1127.IBase2`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>::Method7<object>()
		calli default string(class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>)
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1> on type class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1127.IBase2`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::Method7<object>()
		calli default string(class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>)
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::ClassMethod1345<object>()
		calli default string(class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>)
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::ClassMethod1344<object>()
		calli default string(class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>)
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()"
		ldstr "class G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::Method6<object>()
		calli default string(class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>)
		ldstr "G1_C11::Method6.4859<System.Object>()"
		ldstr "class G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::Method5()
		calli default string(class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>)
		ldstr "G1_C11::Method5.4858()"
		ldstr "class G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::Method4()
		calli default string(class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>)
		ldstr "G1_C11::Method4.4856()"
		ldstr "class G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::Method7<object>()
		calli default string(class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>)
		ldstr "G1_C11::Method7.4854<System.Object>()"
		ldstr "class G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1127.IBase2`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>::Method7<object>()
		calli default string(class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>)
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1> on type class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1127.IBase1`1<class Generated1127.BaseClass0>::Method4()
		calli default string(class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>)
		ldstr "G1_C11::Method4.MI.4857()"
		ldstr "class IBase1`1<class Generated1127.BaseClass0> on type class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1127.IBase1`1<class Generated1127.BaseClass0>::Method5()
		calli default string(class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>)
		ldstr "G1_C11::Method5.4858()"
		ldstr "class IBase1`1<class Generated1127.BaseClass0> on type class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1127.IBase1`1<class Generated1127.BaseClass0>::Method6<object>()
		calli default string(class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>)
		ldstr "G1_C11::Method6.MI.4860<System.Object>()"
		ldstr "class IBase1`1<class Generated1127.BaseClass0> on type class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1127.IBase2`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1>::Method7<object>()
		calli default string(class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>)
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated1127.BaseClass0,class Generated1127.BaseClass1> on type class Generated1127.G1_C11`2<class Generated1127.BaseClass1,class Generated1127.BaseClass1>"
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
		call void Generated1127.Generated1127::MethodCallingTest()
		call void Generated1127.Generated1127::ConstrainedCallsTest()
		call void Generated1127.Generated1127::StructConstrainedInterfaceCallsTest()
		call void Generated1127.Generated1127::CalliTest()
		ldc.i4 100
		ret
	}
}
