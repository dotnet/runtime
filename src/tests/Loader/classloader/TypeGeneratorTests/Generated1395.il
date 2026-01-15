// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.class public Generated1395.BaseClass0 
{
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void [mscorlib]System.Object::.ctor()
		ret
	}
}
.class public Generated1395.BaseClass1 
		extends Generated1395.BaseClass0
{
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void Generated1395.BaseClass0::.ctor()
		ret
	}
}
.class public Generated1395.G3_C1867`1<T0> 
		extends class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>
		implements class Generated1395.IBase2`2<!T0,class Generated1395.BaseClass1> 
{
	.method public hidebysig newslot virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G3_C1867::Method7.18431<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod5125() cil managed noinlining { 
		ldstr "G3_C1867::ClassMethod5125.18432()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod5126() cil managed noinlining { 
		ldstr "G3_C1867::ClassMethod5126.18433()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod5127<M0>() cil managed noinlining { 
		ldstr "G3_C1867::ClassMethod5127.18434<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod5128<M0>() cil managed noinlining { 
		ldstr "G3_C1867::ClassMethod5128.18435<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>::.ctor()
		ret
	}
}
.class public Generated1395.G2_C805`2<T0, T1> 
		extends class Generated1395.G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>
		implements Generated1395.IBase0, class Generated1395.IBase1`1<!T1> 
{
	.method public hidebysig virtual instance string Method0() cil managed noinlining { 
		ldstr "G2_C805::Method0.12437()"
		ret
	}
	.method public hidebysig virtual instance string Method1() cil managed noinlining { 
		ldstr "G2_C805::Method1.12438()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'Generated1395.IBase0.Method1'() cil managed noinlining { 
		.override method instance string Generated1395.IBase0::Method1()
		ldstr "G2_C805::Method1.MI.12439()"
		ret
	}
	.method public hidebysig virtual instance string Method2<M0>() cil managed noinlining { 
		ldstr "G2_C805::Method2.12440<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string Method3<M0>() cil managed noinlining { 
		ldstr "G2_C805::Method3.12441<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'Generated1395.IBase0.Method3'<M0>() cil managed noinlining { 
		.override method instance string Generated1395.IBase0::Method3<[1]>()
		ldstr "G2_C805::Method3.MI.12442<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig virtual instance string Method4() cil managed noinlining { 
		ldstr "G2_C805::Method4.12443()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<T1>.Method4'() cil managed noinlining { 
		.override method instance string class Generated1395.IBase1`1<!T1>::Method4()
		ldstr "G2_C805::Method4.MI.12444()"
		ret
	}
	.method public hidebysig newslot virtual instance string Method5() cil managed noinlining { 
		ldstr "G2_C805::Method5.12445()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<T1>.Method5'() cil managed noinlining { 
		.override method instance string class Generated1395.IBase1`1<!T1>::Method5()
		ldstr "G2_C805::Method5.MI.12446()"
		ret
	}
	.method public hidebysig newslot virtual instance string Method6<M0>() cil managed noinlining { 
		ldstr "G2_C805::Method6.12447<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<T1>.Method6'<M0>() cil managed noinlining { 
		.override method instance string class Generated1395.IBase1`1<!T1>::Method6<[1]>()
		ldstr "G2_C805::Method6.MI.12448<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class Generated1395.G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>::.ctor()
		ret
	}
}
.class interface public abstract Generated1395.IBase2`2<+T0, -T1> 
{
	.method public hidebysig newslot abstract virtual instance string Method7<M0>() cil managed { }
}
.class public abstract Generated1395.G1_C15`2<T0, T1> 
		implements class Generated1395.IBase2`2<!T1,!T1>, class Generated1395.IBase1`1<class Generated1395.BaseClass0> 
{
	.method public hidebysig newslot virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G1_C15::Method7.4885<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase2<T1,T1>.Method7'<M0>() cil managed noinlining { 
		.override method instance string class Generated1395.IBase2`2<!T1,!T1>::Method7<[1]>()
		ldstr "G1_C15::Method7.MI.4886<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig virtual instance string Method4() cil managed noinlining { 
		ldstr "G1_C15::Method4.4887()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<class Generated1395.BaseClass0>.Method4'() cil managed noinlining { 
		.override method instance string class Generated1395.IBase1`1<class Generated1395.BaseClass0>::Method4()
		ldstr "G1_C15::Method4.MI.4888()"
		ret
	}
	.method public hidebysig virtual instance string Method5() cil managed noinlining { 
		ldstr "G1_C15::Method5.4889()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<class Generated1395.BaseClass0>.Method5'() cil managed noinlining { 
		.override method instance string class Generated1395.IBase1`1<class Generated1395.BaseClass0>::Method5()
		ldstr "G1_C15::Method5.MI.4890()"
		ret
	}
	.method public hidebysig virtual instance string Method6<M0>() cil managed noinlining { 
		ldstr "G1_C15::Method6.4891<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<class Generated1395.BaseClass0>.Method6'<M0>() cil managed noinlining { 
		.override method instance string class Generated1395.IBase1`1<class Generated1395.BaseClass0>::Method6<[1]>()
		ldstr "G1_C15::Method6.MI.4892<"
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
.class interface public abstract Generated1395.IBase0 
{
	.method public hidebysig newslot abstract virtual instance string Method0() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method1() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method2<M0>() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method3<M0>() cil managed { }
}
.class interface public abstract Generated1395.IBase1`1<+T0> 
{
	.method public hidebysig newslot abstract virtual instance string Method4() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method5() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method6<M0>() cil managed { }
}
.class public auto ansi beforefieldinit Generated1395.Generated1395 {
	.method static void M.BaseClass0<(Generated1395.BaseClass0)W>(!!W inst, string exp) cil managed {
		.maxstack 5
		.locals init (string[] actualResults)
		ldc.i4.s    0
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.BaseClass0<(Generated1395.BaseClass0)W>(!!W inst, string exp)"
		ldc.i4.s 0
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.BaseClass1<(Generated1395.BaseClass1)W>(!!W inst, string exp) cil managed {
		.maxstack 5
		.locals init (string[] actualResults)
		ldc.i4.s    0
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.BaseClass1<(Generated1395.BaseClass1)W>(!!W inst, string exp)"
		ldc.i4.s 0
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1867.T<T0,(class Generated1395.G3_C1867`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 17
		.locals init (string[] actualResults)
		ldc.i4.s    12
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1867.T<T0,(class Generated1395.G3_C1867`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 12
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.G3_C1867`1<!!T0>::ClassMethod5125()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.G3_C1867`1<!!T0>::ClassMethod5126()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.G3_C1867`1<!!T0>::ClassMethod5127<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.G3_C1867`1<!!T0>::ClassMethod5128<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.G3_C1867`1<!!T0>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.G3_C1867`1<!!T0>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.G3_C1867`1<!!T0>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.G3_C1867`1<!!T0>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.G3_C1867`1<!!T0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.G3_C1867`1<!!T0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.G3_C1867`1<!!T0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.G3_C1867`1<!!T0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1867.A<(class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 17
		.locals init (string[] actualResults)
		ldc.i4.s    12
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1867.A<(class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 12
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>::ClassMethod5125()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>::ClassMethod5126()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>::ClassMethod5127<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>::ClassMethod5128<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1867.B<(class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 17
		.locals init (string[] actualResults)
		ldc.i4.s    12
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1867.B<(class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 12
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>::ClassMethod5125()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>::ClassMethod5126()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>::ClassMethod5127<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>::ClassMethod5128<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C805.T.T<T0,T1,(class Generated1395.G2_C805`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C805.T.T<T0,T1,(class Generated1395.G2_C805`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.G2_C805`2<!!T0,!!T1>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.G2_C805`2<!!T0,!!T1>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.G2_C805`2<!!T0,!!T1>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.G2_C805`2<!!T0,!!T1>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.G2_C805`2<!!T0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.G2_C805`2<!!T0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.G2_C805`2<!!T0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.G2_C805`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C805.A.T<T1,(class Generated1395.G2_C805`2<class Generated1395.BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C805.A.T<T1,(class Generated1395.G2_C805`2<class Generated1395.BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass0,!!T1>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass0,!!T1>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass0,!!T1>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass0,!!T1>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C805.A.A<(class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C805.A.A<(class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C805.A.B<(class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C805.A.B<(class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C805.B.T<T1,(class Generated1395.G2_C805`2<class Generated1395.BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C805.B.T<T1,(class Generated1395.G2_C805`2<class Generated1395.BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass1,!!T1>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass1,!!T1>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass1,!!T1>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass1,!!T1>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass1,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass1,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass1,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C805.B.A<(class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C805.B.A<(class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C805.B.B<(class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C805.B.B<(class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.T.T<T0,T1,(class Generated1395.IBase2`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.T.T<T0,T1,(class Generated1395.IBase2`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.IBase2`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.A.T<T1,(class Generated1395.IBase2`2<class Generated1395.BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.A.T<T1,(class Generated1395.IBase2`2<class Generated1395.BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.IBase2`2<class Generated1395.BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.A.A<(class Generated1395.IBase2`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.A.A<(class Generated1395.IBase2`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.IBase2`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.A.B<(class Generated1395.IBase2`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.A.B<(class Generated1395.IBase2`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.IBase2`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.B.T<T1,(class Generated1395.IBase2`2<class Generated1395.BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.B.T<T1,(class Generated1395.IBase2`2<class Generated1395.BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.IBase2`2<class Generated1395.BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.B.A<(class Generated1395.IBase2`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.B.A<(class Generated1395.IBase2`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.IBase2`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.B.B<(class Generated1395.IBase2`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.B.B<(class Generated1395.IBase2`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.IBase2`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C15.T.T<T0,T1,(class Generated1395.G1_C15`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 9
		.locals init (string[] actualResults)
		ldc.i4.s    4
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C15.T.T<T0,T1,(class Generated1395.G1_C15`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 4
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.G1_C15`2<!!T0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.G1_C15`2<!!T0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.G1_C15`2<!!T0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.G1_C15`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C15.A.T<T1,(class Generated1395.G1_C15`2<class Generated1395.BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 9
		.locals init (string[] actualResults)
		ldc.i4.s    4
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C15.A.T<T1,(class Generated1395.G1_C15`2<class Generated1395.BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 4
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.G1_C15`2<class Generated1395.BaseClass0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.G1_C15`2<class Generated1395.BaseClass0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.G1_C15`2<class Generated1395.BaseClass0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.G1_C15`2<class Generated1395.BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C15.A.A<(class Generated1395.G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 9
		.locals init (string[] actualResults)
		ldc.i4.s    4
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C15.A.A<(class Generated1395.G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 4
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C15.A.B<(class Generated1395.G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 9
		.locals init (string[] actualResults)
		ldc.i4.s    4
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C15.A.B<(class Generated1395.G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 4
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C15.B.T<T1,(class Generated1395.G1_C15`2<class Generated1395.BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 9
		.locals init (string[] actualResults)
		ldc.i4.s    4
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C15.B.T<T1,(class Generated1395.G1_C15`2<class Generated1395.BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 4
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.G1_C15`2<class Generated1395.BaseClass1,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.G1_C15`2<class Generated1395.BaseClass1,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.G1_C15`2<class Generated1395.BaseClass1,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.G1_C15`2<class Generated1395.BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C15.B.A<(class Generated1395.G1_C15`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 9
		.locals init (string[] actualResults)
		ldc.i4.s    4
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C15.B.A<(class Generated1395.G1_C15`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 4
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.G1_C15`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.G1_C15`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.G1_C15`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.G1_C15`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C15.B.B<(class Generated1395.G1_C15`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 9
		.locals init (string[] actualResults)
		ldc.i4.s    4
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C15.B.B<(class Generated1395.G1_C15`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 4
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.G1_C15`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.G1_C15`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.G1_C15`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.G1_C15`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.Generated1395.IBase0<(Generated1395.IBase0)W>(!!W inst, string exp) cil managed {
		.maxstack 9
		.locals init (string[] actualResults)
		ldc.i4.s    4
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.Generated1395.IBase0<(Generated1395.IBase0)W>(!!W inst, string exp)"
		ldc.i4.s 4
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string Generated1395.IBase0::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string Generated1395.IBase0::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string Generated1395.IBase0::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string Generated1395.IBase0::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase1.T<T0,(class Generated1395.IBase1`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase1.T<T0,(class Generated1395.IBase1`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.IBase1`1<!!T0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.IBase1`1<!!T0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.IBase1`1<!!T0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase1.A<(class Generated1395.IBase1`1<class Generated1395.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase1.A<(class Generated1395.IBase1`1<class Generated1395.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.IBase1`1<class Generated1395.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.IBase1`1<class Generated1395.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.IBase1`1<class Generated1395.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase1.B<(class Generated1395.IBase1`1<class Generated1395.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase1.B<(class Generated1395.IBase1`1<class Generated1395.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.IBase1`1<class Generated1395.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.IBase1`1<class Generated1395.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1395.IBase1`1<class Generated1395.BaseClass1>::Method6<object>()
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
		newobj instance void class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1395.G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>
		callvirt instance string class Generated1395.G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>::Method6<object>()
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0> on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1395.G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>
		callvirt instance string class Generated1395.G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>::Method5()
		ldstr "G1_C15::Method5.4889()"
		ldstr "class G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0> on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1395.G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>
		callvirt instance string class Generated1395.G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>::Method4()
		ldstr "G2_C805::Method4.12443()"
		ldstr "class G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0> on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1395.G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>
		callvirt instance string class Generated1395.G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>::Method7<object>()
		ldstr "G1_C15::Method7.4885<System.Object>()"
		ldstr "class G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0> on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1395.IBase2`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>::Method7<object>()
		ldstr "G1_C15::Method7.MI.4886<System.Object>()"
		ldstr "class IBase2`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0> on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1395.IBase1`1<class Generated1395.BaseClass0>::Method4()
		ldstr "G2_C805::Method4.MI.12444()"
		ldstr "class IBase1`1<class Generated1395.BaseClass0> on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1395.IBase1`1<class Generated1395.BaseClass0>::Method5()
		ldstr "G2_C805::Method5.MI.12446()"
		ldstr "class IBase1`1<class Generated1395.BaseClass0> on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1395.IBase1`1<class Generated1395.BaseClass0>::Method6<object>()
		ldstr "G2_C805::Method6.MI.12448<System.Object>()"
		ldstr "class IBase1`1<class Generated1395.BaseClass0> on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1395.IBase2`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>::Method7<object>()
		ldstr "G3_C1867::Method7.18431<System.Object>()"
		ldstr "class IBase2`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1> on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>
		callvirt instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>::Method6<object>()
		ldstr "G2_C805::Method6.12447<System.Object>()"
		ldstr "class G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1> on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>
		callvirt instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>::Method5()
		ldstr "G2_C805::Method5.12445()"
		ldstr "class G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1> on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>
		callvirt instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>::Method4()
		ldstr "G2_C805::Method4.12443()"
		ldstr "class G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1> on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>
		callvirt instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>::Method3<object>()
		ldstr "G2_C805::Method3.12441<System.Object>()"
		ldstr "class G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1> on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>
		callvirt instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>::Method2<object>()
		ldstr "G2_C805::Method2.12440<System.Object>()"
		ldstr "class G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1> on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>
		callvirt instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>::Method1()
		ldstr "G2_C805::Method1.12438()"
		ldstr "class G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1> on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>
		callvirt instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>::Method0()
		ldstr "G2_C805::Method0.12437()"
		ldstr "class G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1> on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>
		callvirt instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>::Method7<object>()
		ldstr "G1_C15::Method7.4885<System.Object>()"
		ldstr "class G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1> on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string Generated1395.IBase0::Method0()
		ldstr "G2_C805::Method0.12437()"
		ldstr "Generated1395.IBase0 on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated1395.IBase0::Method1()
		ldstr "G2_C805::Method1.MI.12439()"
		ldstr "Generated1395.IBase0 on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated1395.IBase0::Method2<object>()
		ldstr "G2_C805::Method2.12440<System.Object>()"
		ldstr "Generated1395.IBase0 on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated1395.IBase0::Method3<object>()
		ldstr "G2_C805::Method3.MI.12442<System.Object>()"
		ldstr "Generated1395.IBase0 on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1395.IBase1`1<class Generated1395.BaseClass1>::Method4()
		ldstr "G2_C805::Method4.MI.12444()"
		ldstr "class IBase1`1<class Generated1395.BaseClass1> on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1395.IBase1`1<class Generated1395.BaseClass1>::Method5()
		ldstr "G2_C805::Method5.MI.12446()"
		ldstr "class IBase1`1<class Generated1395.BaseClass1> on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1395.IBase1`1<class Generated1395.BaseClass1>::Method6<object>()
		ldstr "G2_C805::Method6.MI.12448<System.Object>()"
		ldstr "class IBase1`1<class Generated1395.BaseClass1> on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>
		callvirt instance string class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>::ClassMethod5128<object>()
		ldstr "G3_C1867::ClassMethod5128.18435<System.Object>()"
		ldstr "class G3_C1867`1<class Generated1395.BaseClass0> on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>
		callvirt instance string class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>::ClassMethod5127<object>()
		ldstr "G3_C1867::ClassMethod5127.18434<System.Object>()"
		ldstr "class G3_C1867`1<class Generated1395.BaseClass0> on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>
		callvirt instance string class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>::ClassMethod5126()
		ldstr "G3_C1867::ClassMethod5126.18433()"
		ldstr "class G3_C1867`1<class Generated1395.BaseClass0> on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>
		callvirt instance string class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>::ClassMethod5125()
		ldstr "G3_C1867::ClassMethod5125.18432()"
		ldstr "class G3_C1867`1<class Generated1395.BaseClass0> on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>
		callvirt instance string class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>::Method7<object>()
		ldstr "G3_C1867::Method7.18431<System.Object>()"
		ldstr "class G3_C1867`1<class Generated1395.BaseClass0> on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>
		callvirt instance string class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>::Method6<object>()
		ldstr "G2_C805::Method6.12447<System.Object>()"
		ldstr "class G3_C1867`1<class Generated1395.BaseClass0> on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>
		callvirt instance string class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>::Method5()
		ldstr "G2_C805::Method5.12445()"
		ldstr "class G3_C1867`1<class Generated1395.BaseClass0> on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>
		callvirt instance string class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>::Method4()
		ldstr "G2_C805::Method4.12443()"
		ldstr "class G3_C1867`1<class Generated1395.BaseClass0> on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>
		callvirt instance string class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>::Method3<object>()
		ldstr "G2_C805::Method3.12441<System.Object>()"
		ldstr "class G3_C1867`1<class Generated1395.BaseClass0> on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>
		callvirt instance string class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>::Method2<object>()
		ldstr "G2_C805::Method2.12440<System.Object>()"
		ldstr "class G3_C1867`1<class Generated1395.BaseClass0> on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>
		callvirt instance string class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>::Method1()
		ldstr "G2_C805::Method1.12438()"
		ldstr "class G3_C1867`1<class Generated1395.BaseClass0> on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>
		callvirt instance string class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>::Method0()
		ldstr "G2_C805::Method0.12437()"
		ldstr "class G3_C1867`1<class Generated1395.BaseClass0> on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1395.G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>
		callvirt instance string class Generated1395.G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>::Method6<object>()
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0> on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1395.G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>
		callvirt instance string class Generated1395.G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>::Method5()
		ldstr "G1_C15::Method5.4889()"
		ldstr "class G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0> on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1395.G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>
		callvirt instance string class Generated1395.G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>::Method4()
		ldstr "G2_C805::Method4.12443()"
		ldstr "class G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0> on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1395.G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>
		callvirt instance string class Generated1395.G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>::Method7<object>()
		ldstr "G1_C15::Method7.4885<System.Object>()"
		ldstr "class G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0> on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1395.IBase2`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>::Method7<object>()
		ldstr "G1_C15::Method7.MI.4886<System.Object>()"
		ldstr "class IBase2`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0> on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1395.IBase1`1<class Generated1395.BaseClass0>::Method4()
		ldstr "G2_C805::Method4.MI.12444()"
		ldstr "class IBase1`1<class Generated1395.BaseClass0> on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1395.IBase1`1<class Generated1395.BaseClass0>::Method5()
		ldstr "G2_C805::Method5.MI.12446()"
		ldstr "class IBase1`1<class Generated1395.BaseClass0> on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1395.IBase1`1<class Generated1395.BaseClass0>::Method6<object>()
		ldstr "G2_C805::Method6.MI.12448<System.Object>()"
		ldstr "class IBase1`1<class Generated1395.BaseClass0> on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1395.IBase2`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>::Method7<object>()
		ldstr "G3_C1867::Method7.18431<System.Object>()"
		ldstr "class IBase2`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1> on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>
		callvirt instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>::Method6<object>()
		ldstr "G2_C805::Method6.12447<System.Object>()"
		ldstr "class G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1> on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>
		callvirt instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>::Method5()
		ldstr "G2_C805::Method5.12445()"
		ldstr "class G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1> on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>
		callvirt instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>::Method4()
		ldstr "G2_C805::Method4.12443()"
		ldstr "class G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1> on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>
		callvirt instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>::Method3<object>()
		ldstr "G2_C805::Method3.12441<System.Object>()"
		ldstr "class G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1> on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>
		callvirt instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>::Method2<object>()
		ldstr "G2_C805::Method2.12440<System.Object>()"
		ldstr "class G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1> on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>
		callvirt instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>::Method1()
		ldstr "G2_C805::Method1.12438()"
		ldstr "class G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1> on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>
		callvirt instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>::Method0()
		ldstr "G2_C805::Method0.12437()"
		ldstr "class G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1> on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>
		callvirt instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>::Method7<object>()
		ldstr "G1_C15::Method7.4885<System.Object>()"
		ldstr "class G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1> on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string Generated1395.IBase0::Method0()
		ldstr "G2_C805::Method0.12437()"
		ldstr "Generated1395.IBase0 on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated1395.IBase0::Method1()
		ldstr "G2_C805::Method1.MI.12439()"
		ldstr "Generated1395.IBase0 on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated1395.IBase0::Method2<object>()
		ldstr "G2_C805::Method2.12440<System.Object>()"
		ldstr "Generated1395.IBase0 on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated1395.IBase0::Method3<object>()
		ldstr "G2_C805::Method3.MI.12442<System.Object>()"
		ldstr "Generated1395.IBase0 on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1395.IBase1`1<class Generated1395.BaseClass1>::Method4()
		ldstr "G2_C805::Method4.MI.12444()"
		ldstr "class IBase1`1<class Generated1395.BaseClass1> on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1395.IBase1`1<class Generated1395.BaseClass1>::Method5()
		ldstr "G2_C805::Method5.MI.12446()"
		ldstr "class IBase1`1<class Generated1395.BaseClass1> on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1395.IBase1`1<class Generated1395.BaseClass1>::Method6<object>()
		ldstr "G2_C805::Method6.MI.12448<System.Object>()"
		ldstr "class IBase1`1<class Generated1395.BaseClass1> on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>
		callvirt instance string class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>::ClassMethod5128<object>()
		ldstr "G3_C1867::ClassMethod5128.18435<System.Object>()"
		ldstr "class G3_C1867`1<class Generated1395.BaseClass1> on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>
		callvirt instance string class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>::ClassMethod5127<object>()
		ldstr "G3_C1867::ClassMethod5127.18434<System.Object>()"
		ldstr "class G3_C1867`1<class Generated1395.BaseClass1> on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>
		callvirt instance string class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>::ClassMethod5126()
		ldstr "G3_C1867::ClassMethod5126.18433()"
		ldstr "class G3_C1867`1<class Generated1395.BaseClass1> on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>
		callvirt instance string class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>::ClassMethod5125()
		ldstr "G3_C1867::ClassMethod5125.18432()"
		ldstr "class G3_C1867`1<class Generated1395.BaseClass1> on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>
		callvirt instance string class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>::Method7<object>()
		ldstr "G3_C1867::Method7.18431<System.Object>()"
		ldstr "class G3_C1867`1<class Generated1395.BaseClass1> on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>
		callvirt instance string class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>::Method6<object>()
		ldstr "G2_C805::Method6.12447<System.Object>()"
		ldstr "class G3_C1867`1<class Generated1395.BaseClass1> on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>
		callvirt instance string class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>::Method5()
		ldstr "G2_C805::Method5.12445()"
		ldstr "class G3_C1867`1<class Generated1395.BaseClass1> on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>
		callvirt instance string class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>::Method4()
		ldstr "G2_C805::Method4.12443()"
		ldstr "class G3_C1867`1<class Generated1395.BaseClass1> on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>
		callvirt instance string class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>::Method3<object>()
		ldstr "G2_C805::Method3.12441<System.Object>()"
		ldstr "class G3_C1867`1<class Generated1395.BaseClass1> on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>
		callvirt instance string class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>::Method2<object>()
		ldstr "G2_C805::Method2.12440<System.Object>()"
		ldstr "class G3_C1867`1<class Generated1395.BaseClass1> on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>
		callvirt instance string class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>::Method1()
		ldstr "G2_C805::Method1.12438()"
		ldstr "class G3_C1867`1<class Generated1395.BaseClass1> on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>
		callvirt instance string class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>::Method0()
		ldstr "G2_C805::Method0.12437()"
		ldstr "class G3_C1867`1<class Generated1395.BaseClass1> on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1395.IBase2`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>::Method7<object>()
		ldstr "G3_C1867::Method7.18431<System.Object>()"
		ldstr "class IBase2`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1> on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1395.G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>
		callvirt instance string class Generated1395.G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>::Method6<object>()
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1395.G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>
		callvirt instance string class Generated1395.G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>::Method5()
		ldstr "G1_C15::Method5.4889()"
		ldstr "class G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1395.G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>
		callvirt instance string class Generated1395.G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>::Method4()
		ldstr "G2_C805::Method4.12443()"
		ldstr "class G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1395.G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>
		callvirt instance string class Generated1395.G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>::Method7<object>()
		ldstr "G1_C15::Method7.4885<System.Object>()"
		ldstr "class G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1395.IBase2`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>::Method7<object>()
		ldstr "G1_C15::Method7.MI.4886<System.Object>()"
		ldstr "class IBase2`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1395.IBase1`1<class Generated1395.BaseClass0>::Method4()
		ldstr "G2_C805::Method4.MI.12444()"
		ldstr "class IBase1`1<class Generated1395.BaseClass0> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1395.IBase1`1<class Generated1395.BaseClass0>::Method5()
		ldstr "G2_C805::Method5.MI.12446()"
		ldstr "class IBase1`1<class Generated1395.BaseClass0> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1395.IBase1`1<class Generated1395.BaseClass0>::Method6<object>()
		ldstr "G2_C805::Method6.MI.12448<System.Object>()"
		ldstr "class IBase1`1<class Generated1395.BaseClass0> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1395.IBase2`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>::Method7<object>()
		ldstr "G1_C15::Method7.MI.4886<System.Object>()"
		ldstr "class IBase2`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>
		callvirt instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>::Method6<object>()
		ldstr "G2_C805::Method6.12447<System.Object>()"
		ldstr "class G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>
		callvirt instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>::Method5()
		ldstr "G2_C805::Method5.12445()"
		ldstr "class G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>
		callvirt instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>::Method4()
		ldstr "G2_C805::Method4.12443()"
		ldstr "class G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>
		callvirt instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>::Method3<object>()
		ldstr "G2_C805::Method3.12441<System.Object>()"
		ldstr "class G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>
		callvirt instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>::Method2<object>()
		ldstr "G2_C805::Method2.12440<System.Object>()"
		ldstr "class G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>
		callvirt instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>::Method1()
		ldstr "G2_C805::Method1.12438()"
		ldstr "class G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>
		callvirt instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>::Method0()
		ldstr "G2_C805::Method0.12437()"
		ldstr "class G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>
		callvirt instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>::Method7<object>()
		ldstr "G1_C15::Method7.4885<System.Object>()"
		ldstr "class G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string Generated1395.IBase0::Method0()
		ldstr "G2_C805::Method0.12437()"
		ldstr "Generated1395.IBase0 on type class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated1395.IBase0::Method1()
		ldstr "G2_C805::Method1.MI.12439()"
		ldstr "Generated1395.IBase0 on type class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated1395.IBase0::Method2<object>()
		ldstr "G2_C805::Method2.12440<System.Object>()"
		ldstr "Generated1395.IBase0 on type class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated1395.IBase0::Method3<object>()
		ldstr "G2_C805::Method3.MI.12442<System.Object>()"
		ldstr "Generated1395.IBase0 on type class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1395.G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>
		callvirt instance string class Generated1395.G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>::Method6<object>()
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1395.G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>
		callvirt instance string class Generated1395.G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>::Method5()
		ldstr "G1_C15::Method5.4889()"
		ldstr "class G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1395.G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>
		callvirt instance string class Generated1395.G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>::Method4()
		ldstr "G2_C805::Method4.12443()"
		ldstr "class G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1395.G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>
		callvirt instance string class Generated1395.G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>::Method7<object>()
		ldstr "G1_C15::Method7.4885<System.Object>()"
		ldstr "class G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1395.IBase2`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>::Method7<object>()
		ldstr "G1_C15::Method7.MI.4886<System.Object>()"
		ldstr "class IBase2`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1395.IBase1`1<class Generated1395.BaseClass0>::Method4()
		ldstr "G2_C805::Method4.MI.12444()"
		ldstr "class IBase1`1<class Generated1395.BaseClass0> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1395.IBase1`1<class Generated1395.BaseClass0>::Method5()
		ldstr "G2_C805::Method5.MI.12446()"
		ldstr "class IBase1`1<class Generated1395.BaseClass0> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1395.IBase1`1<class Generated1395.BaseClass0>::Method6<object>()
		ldstr "G2_C805::Method6.MI.12448<System.Object>()"
		ldstr "class IBase1`1<class Generated1395.BaseClass0> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1395.IBase2`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>::Method7<object>()
		ldstr "G1_C15::Method7.MI.4886<System.Object>()"
		ldstr "class IBase2`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>
		callvirt instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>::Method6<object>()
		ldstr "G2_C805::Method6.12447<System.Object>()"
		ldstr "class G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>
		callvirt instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>::Method5()
		ldstr "G2_C805::Method5.12445()"
		ldstr "class G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>
		callvirt instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>::Method4()
		ldstr "G2_C805::Method4.12443()"
		ldstr "class G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>
		callvirt instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>::Method3<object>()
		ldstr "G2_C805::Method3.12441<System.Object>()"
		ldstr "class G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>
		callvirt instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>::Method2<object>()
		ldstr "G2_C805::Method2.12440<System.Object>()"
		ldstr "class G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>
		callvirt instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>::Method1()
		ldstr "G2_C805::Method1.12438()"
		ldstr "class G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>
		callvirt instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>::Method0()
		ldstr "G2_C805::Method0.12437()"
		ldstr "class G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>
		callvirt instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>::Method7<object>()
		ldstr "G1_C15::Method7.4885<System.Object>()"
		ldstr "class G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string Generated1395.IBase0::Method0()
		ldstr "G2_C805::Method0.12437()"
		ldstr "Generated1395.IBase0 on type class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated1395.IBase0::Method1()
		ldstr "G2_C805::Method1.MI.12439()"
		ldstr "Generated1395.IBase0 on type class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated1395.IBase0::Method2<object>()
		ldstr "G2_C805::Method2.12440<System.Object>()"
		ldstr "Generated1395.IBase0 on type class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated1395.IBase0::Method3<object>()
		ldstr "G2_C805::Method3.MI.12442<System.Object>()"
		ldstr "Generated1395.IBase0 on type class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1395.IBase1`1<class Generated1395.BaseClass1>::Method4()
		ldstr "G2_C805::Method4.MI.12444()"
		ldstr "class IBase1`1<class Generated1395.BaseClass1> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1395.IBase1`1<class Generated1395.BaseClass1>::Method5()
		ldstr "G2_C805::Method5.MI.12446()"
		ldstr "class IBase1`1<class Generated1395.BaseClass1> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1395.IBase1`1<class Generated1395.BaseClass1>::Method6<object>()
		ldstr "G2_C805::Method6.MI.12448<System.Object>()"
		ldstr "class IBase1`1<class Generated1395.BaseClass1> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1395.G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>
		callvirt instance string class Generated1395.G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>::Method6<object>()
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1395.G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>
		callvirt instance string class Generated1395.G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>::Method5()
		ldstr "G1_C15::Method5.4889()"
		ldstr "class G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1395.G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>
		callvirt instance string class Generated1395.G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>::Method4()
		ldstr "G2_C805::Method4.12443()"
		ldstr "class G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1395.G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>
		callvirt instance string class Generated1395.G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>::Method7<object>()
		ldstr "G1_C15::Method7.4885<System.Object>()"
		ldstr "class G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1395.IBase2`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>::Method7<object>()
		ldstr "G1_C15::Method7.MI.4886<System.Object>()"
		ldstr "class IBase2`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1395.IBase1`1<class Generated1395.BaseClass0>::Method4()
		ldstr "G2_C805::Method4.MI.12444()"
		ldstr "class IBase1`1<class Generated1395.BaseClass0> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1395.IBase1`1<class Generated1395.BaseClass0>::Method5()
		ldstr "G2_C805::Method5.MI.12446()"
		ldstr "class IBase1`1<class Generated1395.BaseClass0> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1395.IBase1`1<class Generated1395.BaseClass0>::Method6<object>()
		ldstr "G2_C805::Method6.MI.12448<System.Object>()"
		ldstr "class IBase1`1<class Generated1395.BaseClass0> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1395.IBase2`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>::Method7<object>()
		ldstr "G1_C15::Method7.MI.4886<System.Object>()"
		ldstr "class IBase2`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0>
		callvirt instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0>::Method6<object>()
		ldstr "G2_C805::Method6.12447<System.Object>()"
		ldstr "class G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0>
		callvirt instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0>::Method5()
		ldstr "G2_C805::Method5.12445()"
		ldstr "class G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0>
		callvirt instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0>::Method4()
		ldstr "G2_C805::Method4.12443()"
		ldstr "class G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0>
		callvirt instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0>::Method3<object>()
		ldstr "G2_C805::Method3.12441<System.Object>()"
		ldstr "class G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0>
		callvirt instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0>::Method2<object>()
		ldstr "G2_C805::Method2.12440<System.Object>()"
		ldstr "class G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0>
		callvirt instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0>::Method1()
		ldstr "G2_C805::Method1.12438()"
		ldstr "class G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0>
		callvirt instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0>::Method0()
		ldstr "G2_C805::Method0.12437()"
		ldstr "class G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0>
		callvirt instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0>::Method7<object>()
		ldstr "G1_C15::Method7.4885<System.Object>()"
		ldstr "class G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string Generated1395.IBase0::Method0()
		ldstr "G2_C805::Method0.12437()"
		ldstr "Generated1395.IBase0 on type class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated1395.IBase0::Method1()
		ldstr "G2_C805::Method1.MI.12439()"
		ldstr "Generated1395.IBase0 on type class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated1395.IBase0::Method2<object>()
		ldstr "G2_C805::Method2.12440<System.Object>()"
		ldstr "Generated1395.IBase0 on type class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated1395.IBase0::Method3<object>()
		ldstr "G2_C805::Method3.MI.12442<System.Object>()"
		ldstr "Generated1395.IBase0 on type class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1395.G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>
		callvirt instance string class Generated1395.G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>::Method6<object>()
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1395.G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>
		callvirt instance string class Generated1395.G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>::Method5()
		ldstr "G1_C15::Method5.4889()"
		ldstr "class G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1395.G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>
		callvirt instance string class Generated1395.G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>::Method4()
		ldstr "G2_C805::Method4.12443()"
		ldstr "class G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1395.G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>
		callvirt instance string class Generated1395.G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>::Method7<object>()
		ldstr "G1_C15::Method7.4885<System.Object>()"
		ldstr "class G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1395.IBase2`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>::Method7<object>()
		ldstr "G1_C15::Method7.MI.4886<System.Object>()"
		ldstr "class IBase2`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1395.IBase1`1<class Generated1395.BaseClass0>::Method4()
		ldstr "G2_C805::Method4.MI.12444()"
		ldstr "class IBase1`1<class Generated1395.BaseClass0> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1395.IBase1`1<class Generated1395.BaseClass0>::Method5()
		ldstr "G2_C805::Method5.MI.12446()"
		ldstr "class IBase1`1<class Generated1395.BaseClass0> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1395.IBase1`1<class Generated1395.BaseClass0>::Method6<object>()
		ldstr "G2_C805::Method6.MI.12448<System.Object>()"
		ldstr "class IBase1`1<class Generated1395.BaseClass0> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1395.IBase2`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>::Method7<object>()
		ldstr "G1_C15::Method7.MI.4886<System.Object>()"
		ldstr "class IBase2`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>
		callvirt instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>::Method6<object>()
		ldstr "G2_C805::Method6.12447<System.Object>()"
		ldstr "class G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>
		callvirt instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>::Method5()
		ldstr "G2_C805::Method5.12445()"
		ldstr "class G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>
		callvirt instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>::Method4()
		ldstr "G2_C805::Method4.12443()"
		ldstr "class G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>
		callvirt instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>::Method3<object>()
		ldstr "G2_C805::Method3.12441<System.Object>()"
		ldstr "class G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>
		callvirt instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>::Method2<object>()
		ldstr "G2_C805::Method2.12440<System.Object>()"
		ldstr "class G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>
		callvirt instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>::Method1()
		ldstr "G2_C805::Method1.12438()"
		ldstr "class G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>
		callvirt instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>::Method0()
		ldstr "G2_C805::Method0.12437()"
		ldstr "class G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>
		callvirt instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>::Method7<object>()
		ldstr "G1_C15::Method7.4885<System.Object>()"
		ldstr "class G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string Generated1395.IBase0::Method0()
		ldstr "G2_C805::Method0.12437()"
		ldstr "Generated1395.IBase0 on type class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated1395.IBase0::Method1()
		ldstr "G2_C805::Method1.MI.12439()"
		ldstr "Generated1395.IBase0 on type class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated1395.IBase0::Method2<object>()
		ldstr "G2_C805::Method2.12440<System.Object>()"
		ldstr "Generated1395.IBase0 on type class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated1395.IBase0::Method3<object>()
		ldstr "G2_C805::Method3.MI.12442<System.Object>()"
		ldstr "Generated1395.IBase0 on type class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1395.IBase1`1<class Generated1395.BaseClass1>::Method4()
		ldstr "G2_C805::Method4.MI.12444()"
		ldstr "class IBase1`1<class Generated1395.BaseClass1> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1395.IBase1`1<class Generated1395.BaseClass1>::Method5()
		ldstr "G2_C805::Method5.MI.12446()"
		ldstr "class IBase1`1<class Generated1395.BaseClass1> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1395.IBase1`1<class Generated1395.BaseClass1>::Method6<object>()
		ldstr "G2_C805::Method6.MI.12448<System.Object>()"
		ldstr "class IBase1`1<class Generated1395.BaseClass1> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>"
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
		newobj instance void class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C805::Method4.12443()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G1_C15::Method7.4885<System.Object>()#"
		call void Generated1395.Generated1395::M.G1_C15.T.T<class Generated1395.BaseClass0,class Generated1395.BaseClass0,class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C805::Method4.12443()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G1_C15::Method7.4885<System.Object>()#"
		call void Generated1395.Generated1395::M.G1_C15.A.T<class Generated1395.BaseClass0,class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C805::Method4.12443()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G1_C15::Method7.4885<System.Object>()#"
		call void Generated1395.Generated1395::M.G1_C15.A.A<class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C15::Method7.MI.4886<System.Object>()#"
		call void Generated1395.Generated1395::M.IBase2.T.T<class Generated1395.BaseClass0,class Generated1395.BaseClass0,class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C15::Method7.MI.4886<System.Object>()#"
		call void Generated1395.Generated1395::M.IBase2.A.T<class Generated1395.BaseClass0,class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C15::Method7.MI.4886<System.Object>()#"
		call void Generated1395.Generated1395::M.IBase2.A.A<class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C805::Method4.MI.12444()#G2_C805::Method5.MI.12446()#G2_C805::Method6.MI.12448<System.Object>()#"
		call void Generated1395.Generated1395::M.IBase1.T<class Generated1395.BaseClass0,class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C805::Method4.MI.12444()#G2_C805::Method5.MI.12446()#G2_C805::Method6.MI.12448<System.Object>()#"
		call void Generated1395.Generated1395::M.IBase1.A<class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1867::Method7.18431<System.Object>()#"
		call void Generated1395.Generated1395::M.IBase2.T.T<class Generated1395.BaseClass0,class Generated1395.BaseClass1,class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G3_C1867::Method7.18431<System.Object>()#"
		call void Generated1395.Generated1395::M.IBase2.A.T<class Generated1395.BaseClass1,class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G3_C1867::Method7.18431<System.Object>()#"
		call void Generated1395.Generated1395::M.IBase2.A.B<class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C805::Method0.12437()#G2_C805::Method1.12438()#G2_C805::Method2.12440<System.Object>()#G2_C805::Method3.12441<System.Object>()#G2_C805::Method4.12443()#G2_C805::Method5.12445()#G2_C805::Method6.12447<System.Object>()#G1_C15::Method7.4885<System.Object>()#"
		call void Generated1395.Generated1395::M.G2_C805.T.T<class Generated1395.BaseClass1,class Generated1395.BaseClass1,class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C805::Method0.12437()#G2_C805::Method1.12438()#G2_C805::Method2.12440<System.Object>()#G2_C805::Method3.12441<System.Object>()#G2_C805::Method4.12443()#G2_C805::Method5.12445()#G2_C805::Method6.12447<System.Object>()#G1_C15::Method7.4885<System.Object>()#"
		call void Generated1395.Generated1395::M.G2_C805.B.T<class Generated1395.BaseClass1,class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C805::Method0.12437()#G2_C805::Method1.12438()#G2_C805::Method2.12440<System.Object>()#G2_C805::Method3.12441<System.Object>()#G2_C805::Method4.12443()#G2_C805::Method5.12445()#G2_C805::Method6.12447<System.Object>()#G1_C15::Method7.4885<System.Object>()#"
		call void Generated1395.Generated1395::M.G2_C805.B.B<class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C805::Method0.12437()#G2_C805::Method1.MI.12439()#G2_C805::Method2.12440<System.Object>()#G2_C805::Method3.MI.12442<System.Object>()#"
		call void Generated1395.Generated1395::M.Generated1395.IBase0<class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C805::Method4.MI.12444()#G2_C805::Method5.MI.12446()#G2_C805::Method6.MI.12448<System.Object>()#"
		call void Generated1395.Generated1395::M.IBase1.T<class Generated1395.BaseClass1,class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C805::Method4.MI.12444()#G2_C805::Method5.MI.12446()#G2_C805::Method6.MI.12448<System.Object>()#"
		call void Generated1395.Generated1395::M.IBase1.B<class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1867::ClassMethod5125.18432()#G3_C1867::ClassMethod5126.18433()#G3_C1867::ClassMethod5127.18434<System.Object>()#G3_C1867::ClassMethod5128.18435<System.Object>()#G2_C805::Method0.12437()#G2_C805::Method1.12438()#G2_C805::Method2.12440<System.Object>()#G2_C805::Method3.12441<System.Object>()#G2_C805::Method4.12443()#G2_C805::Method5.12445()#G2_C805::Method6.12447<System.Object>()#G3_C1867::Method7.18431<System.Object>()#"
		call void Generated1395.Generated1395::M.G3_C1867.T<class Generated1395.BaseClass0,class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G3_C1867::ClassMethod5125.18432()#G3_C1867::ClassMethod5126.18433()#G3_C1867::ClassMethod5127.18434<System.Object>()#G3_C1867::ClassMethod5128.18435<System.Object>()#G2_C805::Method0.12437()#G2_C805::Method1.12438()#G2_C805::Method2.12440<System.Object>()#G2_C805::Method3.12441<System.Object>()#G2_C805::Method4.12443()#G2_C805::Method5.12445()#G2_C805::Method6.12447<System.Object>()#G3_C1867::Method7.18431<System.Object>()#"
		call void Generated1395.Generated1395::M.G3_C1867.A<class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>>(!!0,string)
		newobj instance void class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C805::Method4.12443()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G1_C15::Method7.4885<System.Object>()#"
		call void Generated1395.Generated1395::M.G1_C15.T.T<class Generated1395.BaseClass0,class Generated1395.BaseClass0,class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C805::Method4.12443()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G1_C15::Method7.4885<System.Object>()#"
		call void Generated1395.Generated1395::M.G1_C15.A.T<class Generated1395.BaseClass0,class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C805::Method4.12443()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G1_C15::Method7.4885<System.Object>()#"
		call void Generated1395.Generated1395::M.G1_C15.A.A<class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C15::Method7.MI.4886<System.Object>()#"
		call void Generated1395.Generated1395::M.IBase2.T.T<class Generated1395.BaseClass0,class Generated1395.BaseClass0,class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C15::Method7.MI.4886<System.Object>()#"
		call void Generated1395.Generated1395::M.IBase2.A.T<class Generated1395.BaseClass0,class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C15::Method7.MI.4886<System.Object>()#"
		call void Generated1395.Generated1395::M.IBase2.A.A<class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C805::Method4.MI.12444()#G2_C805::Method5.MI.12446()#G2_C805::Method6.MI.12448<System.Object>()#"
		call void Generated1395.Generated1395::M.IBase1.T<class Generated1395.BaseClass0,class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C805::Method4.MI.12444()#G2_C805::Method5.MI.12446()#G2_C805::Method6.MI.12448<System.Object>()#"
		call void Generated1395.Generated1395::M.IBase1.A<class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1867::Method7.18431<System.Object>()#"
		call void Generated1395.Generated1395::M.IBase2.T.T<class Generated1395.BaseClass0,class Generated1395.BaseClass1,class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G3_C1867::Method7.18431<System.Object>()#"
		call void Generated1395.Generated1395::M.IBase2.A.T<class Generated1395.BaseClass1,class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1867::Method7.18431<System.Object>()#"
		call void Generated1395.Generated1395::M.IBase2.A.B<class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C805::Method0.12437()#G2_C805::Method1.12438()#G2_C805::Method2.12440<System.Object>()#G2_C805::Method3.12441<System.Object>()#G2_C805::Method4.12443()#G2_C805::Method5.12445()#G2_C805::Method6.12447<System.Object>()#G1_C15::Method7.4885<System.Object>()#"
		call void Generated1395.Generated1395::M.G2_C805.T.T<class Generated1395.BaseClass1,class Generated1395.BaseClass1,class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C805::Method0.12437()#G2_C805::Method1.12438()#G2_C805::Method2.12440<System.Object>()#G2_C805::Method3.12441<System.Object>()#G2_C805::Method4.12443()#G2_C805::Method5.12445()#G2_C805::Method6.12447<System.Object>()#G1_C15::Method7.4885<System.Object>()#"
		call void Generated1395.Generated1395::M.G2_C805.B.T<class Generated1395.BaseClass1,class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C805::Method0.12437()#G2_C805::Method1.12438()#G2_C805::Method2.12440<System.Object>()#G2_C805::Method3.12441<System.Object>()#G2_C805::Method4.12443()#G2_C805::Method5.12445()#G2_C805::Method6.12447<System.Object>()#G1_C15::Method7.4885<System.Object>()#"
		call void Generated1395.Generated1395::M.G2_C805.B.B<class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C805::Method0.12437()#G2_C805::Method1.MI.12439()#G2_C805::Method2.12440<System.Object>()#G2_C805::Method3.MI.12442<System.Object>()#"
		call void Generated1395.Generated1395::M.Generated1395.IBase0<class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C805::Method4.MI.12444()#G2_C805::Method5.MI.12446()#G2_C805::Method6.MI.12448<System.Object>()#"
		call void Generated1395.Generated1395::M.IBase1.T<class Generated1395.BaseClass1,class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C805::Method4.MI.12444()#G2_C805::Method5.MI.12446()#G2_C805::Method6.MI.12448<System.Object>()#"
		call void Generated1395.Generated1395::M.IBase1.B<class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1867::ClassMethod5125.18432()#G3_C1867::ClassMethod5126.18433()#G3_C1867::ClassMethod5127.18434<System.Object>()#G3_C1867::ClassMethod5128.18435<System.Object>()#G2_C805::Method0.12437()#G2_C805::Method1.12438()#G2_C805::Method2.12440<System.Object>()#G2_C805::Method3.12441<System.Object>()#G2_C805::Method4.12443()#G2_C805::Method5.12445()#G2_C805::Method6.12447<System.Object>()#G3_C1867::Method7.18431<System.Object>()#"
		call void Generated1395.Generated1395::M.G3_C1867.T<class Generated1395.BaseClass1,class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1867::ClassMethod5125.18432()#G3_C1867::ClassMethod5126.18433()#G3_C1867::ClassMethod5127.18434<System.Object>()#G3_C1867::ClassMethod5128.18435<System.Object>()#G2_C805::Method0.12437()#G2_C805::Method1.12438()#G2_C805::Method2.12440<System.Object>()#G2_C805::Method3.12441<System.Object>()#G2_C805::Method4.12443()#G2_C805::Method5.12445()#G2_C805::Method6.12447<System.Object>()#G3_C1867::Method7.18431<System.Object>()#"
		call void Generated1395.Generated1395::M.G3_C1867.B<class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1867::Method7.18431<System.Object>()#"
		call void Generated1395.Generated1395::M.IBase2.T.T<class Generated1395.BaseClass1,class Generated1395.BaseClass1,class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G3_C1867::Method7.18431<System.Object>()#"
		call void Generated1395.Generated1395::M.IBase2.B.T<class Generated1395.BaseClass1,class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1867::Method7.18431<System.Object>()#"
		call void Generated1395.Generated1395::M.IBase2.B.B<class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>>(!!0,string)
		newobj instance void class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C805::Method4.12443()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G1_C15::Method7.4885<System.Object>()#"
		call void Generated1395.Generated1395::M.G1_C15.T.T<class Generated1395.BaseClass0,class Generated1395.BaseClass0,class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C805::Method4.12443()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G1_C15::Method7.4885<System.Object>()#"
		call void Generated1395.Generated1395::M.G1_C15.A.T<class Generated1395.BaseClass0,class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C805::Method4.12443()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G1_C15::Method7.4885<System.Object>()#"
		call void Generated1395.Generated1395::M.G1_C15.A.A<class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C15::Method7.MI.4886<System.Object>()#"
		call void Generated1395.Generated1395::M.IBase2.T.T<class Generated1395.BaseClass0,class Generated1395.BaseClass0,class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C15::Method7.MI.4886<System.Object>()#"
		call void Generated1395.Generated1395::M.IBase2.A.T<class Generated1395.BaseClass0,class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C15::Method7.MI.4886<System.Object>()#"
		call void Generated1395.Generated1395::M.IBase2.A.A<class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C805::Method4.MI.12444()#G2_C805::Method5.MI.12446()#G2_C805::Method6.MI.12448<System.Object>()#"
		call void Generated1395.Generated1395::M.IBase1.T<class Generated1395.BaseClass0,class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C805::Method4.MI.12444()#G2_C805::Method5.MI.12446()#G2_C805::Method6.MI.12448<System.Object>()#"
		call void Generated1395.Generated1395::M.IBase1.A<class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C15::Method7.MI.4886<System.Object>()#"
		call void Generated1395.Generated1395::M.IBase2.T.T<class Generated1395.BaseClass0,class Generated1395.BaseClass1,class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C15::Method7.MI.4886<System.Object>()#"
		call void Generated1395.Generated1395::M.IBase2.A.T<class Generated1395.BaseClass1,class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C15::Method7.MI.4886<System.Object>()#"
		call void Generated1395.Generated1395::M.IBase2.A.B<class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C805::Method0.12437()#G2_C805::Method1.12438()#G2_C805::Method2.12440<System.Object>()#G2_C805::Method3.12441<System.Object>()#G2_C805::Method4.12443()#G2_C805::Method5.12445()#G2_C805::Method6.12447<System.Object>()#G1_C15::Method7.4885<System.Object>()#"
		call void Generated1395.Generated1395::M.G2_C805.T.T<class Generated1395.BaseClass0,class Generated1395.BaseClass0,class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C805::Method0.12437()#G2_C805::Method1.12438()#G2_C805::Method2.12440<System.Object>()#G2_C805::Method3.12441<System.Object>()#G2_C805::Method4.12443()#G2_C805::Method5.12445()#G2_C805::Method6.12447<System.Object>()#G1_C15::Method7.4885<System.Object>()#"
		call void Generated1395.Generated1395::M.G2_C805.A.T<class Generated1395.BaseClass0,class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C805::Method0.12437()#G2_C805::Method1.12438()#G2_C805::Method2.12440<System.Object>()#G2_C805::Method3.12441<System.Object>()#G2_C805::Method4.12443()#G2_C805::Method5.12445()#G2_C805::Method6.12447<System.Object>()#G1_C15::Method7.4885<System.Object>()#"
		call void Generated1395.Generated1395::M.G2_C805.A.A<class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C805::Method0.12437()#G2_C805::Method1.MI.12439()#G2_C805::Method2.12440<System.Object>()#G2_C805::Method3.MI.12442<System.Object>()#"
		call void Generated1395.Generated1395::M.Generated1395.IBase0<class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>>(!!0,string)
		newobj instance void class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C805::Method4.12443()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G1_C15::Method7.4885<System.Object>()#"
		call void Generated1395.Generated1395::M.G1_C15.T.T<class Generated1395.BaseClass0,class Generated1395.BaseClass0,class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C805::Method4.12443()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G1_C15::Method7.4885<System.Object>()#"
		call void Generated1395.Generated1395::M.G1_C15.A.T<class Generated1395.BaseClass0,class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C805::Method4.12443()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G1_C15::Method7.4885<System.Object>()#"
		call void Generated1395.Generated1395::M.G1_C15.A.A<class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C15::Method7.MI.4886<System.Object>()#"
		call void Generated1395.Generated1395::M.IBase2.T.T<class Generated1395.BaseClass0,class Generated1395.BaseClass0,class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C15::Method7.MI.4886<System.Object>()#"
		call void Generated1395.Generated1395::M.IBase2.A.T<class Generated1395.BaseClass0,class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C15::Method7.MI.4886<System.Object>()#"
		call void Generated1395.Generated1395::M.IBase2.A.A<class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C805::Method4.MI.12444()#G2_C805::Method5.MI.12446()#G2_C805::Method6.MI.12448<System.Object>()#"
		call void Generated1395.Generated1395::M.IBase1.T<class Generated1395.BaseClass0,class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C805::Method4.MI.12444()#G2_C805::Method5.MI.12446()#G2_C805::Method6.MI.12448<System.Object>()#"
		call void Generated1395.Generated1395::M.IBase1.A<class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C15::Method7.MI.4886<System.Object>()#"
		call void Generated1395.Generated1395::M.IBase2.T.T<class Generated1395.BaseClass0,class Generated1395.BaseClass1,class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C15::Method7.MI.4886<System.Object>()#"
		call void Generated1395.Generated1395::M.IBase2.A.T<class Generated1395.BaseClass1,class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C15::Method7.MI.4886<System.Object>()#"
		call void Generated1395.Generated1395::M.IBase2.A.B<class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C805::Method0.12437()#G2_C805::Method1.12438()#G2_C805::Method2.12440<System.Object>()#G2_C805::Method3.12441<System.Object>()#G2_C805::Method4.12443()#G2_C805::Method5.12445()#G2_C805::Method6.12447<System.Object>()#G1_C15::Method7.4885<System.Object>()#"
		call void Generated1395.Generated1395::M.G2_C805.T.T<class Generated1395.BaseClass0,class Generated1395.BaseClass1,class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C805::Method0.12437()#G2_C805::Method1.12438()#G2_C805::Method2.12440<System.Object>()#G2_C805::Method3.12441<System.Object>()#G2_C805::Method4.12443()#G2_C805::Method5.12445()#G2_C805::Method6.12447<System.Object>()#G1_C15::Method7.4885<System.Object>()#"
		call void Generated1395.Generated1395::M.G2_C805.A.T<class Generated1395.BaseClass1,class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C805::Method0.12437()#G2_C805::Method1.12438()#G2_C805::Method2.12440<System.Object>()#G2_C805::Method3.12441<System.Object>()#G2_C805::Method4.12443()#G2_C805::Method5.12445()#G2_C805::Method6.12447<System.Object>()#G1_C15::Method7.4885<System.Object>()#"
		call void Generated1395.Generated1395::M.G2_C805.A.B<class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C805::Method0.12437()#G2_C805::Method1.MI.12439()#G2_C805::Method2.12440<System.Object>()#G2_C805::Method3.MI.12442<System.Object>()#"
		call void Generated1395.Generated1395::M.Generated1395.IBase0<class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C805::Method4.MI.12444()#G2_C805::Method5.MI.12446()#G2_C805::Method6.MI.12448<System.Object>()#"
		call void Generated1395.Generated1395::M.IBase1.T<class Generated1395.BaseClass1,class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C805::Method4.MI.12444()#G2_C805::Method5.MI.12446()#G2_C805::Method6.MI.12448<System.Object>()#"
		call void Generated1395.Generated1395::M.IBase1.B<class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>>(!!0,string)
		newobj instance void class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C805::Method4.12443()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G1_C15::Method7.4885<System.Object>()#"
		call void Generated1395.Generated1395::M.G1_C15.T.T<class Generated1395.BaseClass0,class Generated1395.BaseClass0,class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C805::Method4.12443()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G1_C15::Method7.4885<System.Object>()#"
		call void Generated1395.Generated1395::M.G1_C15.A.T<class Generated1395.BaseClass0,class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C805::Method4.12443()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G1_C15::Method7.4885<System.Object>()#"
		call void Generated1395.Generated1395::M.G1_C15.A.A<class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C15::Method7.MI.4886<System.Object>()#"
		call void Generated1395.Generated1395::M.IBase2.T.T<class Generated1395.BaseClass0,class Generated1395.BaseClass0,class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C15::Method7.MI.4886<System.Object>()#"
		call void Generated1395.Generated1395::M.IBase2.A.T<class Generated1395.BaseClass0,class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C15::Method7.MI.4886<System.Object>()#"
		call void Generated1395.Generated1395::M.IBase2.A.A<class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C805::Method4.MI.12444()#G2_C805::Method5.MI.12446()#G2_C805::Method6.MI.12448<System.Object>()#"
		call void Generated1395.Generated1395::M.IBase1.T<class Generated1395.BaseClass0,class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C805::Method4.MI.12444()#G2_C805::Method5.MI.12446()#G2_C805::Method6.MI.12448<System.Object>()#"
		call void Generated1395.Generated1395::M.IBase1.A<class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C15::Method7.MI.4886<System.Object>()#"
		call void Generated1395.Generated1395::M.IBase2.T.T<class Generated1395.BaseClass0,class Generated1395.BaseClass1,class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C15::Method7.MI.4886<System.Object>()#"
		call void Generated1395.Generated1395::M.IBase2.A.T<class Generated1395.BaseClass1,class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C15::Method7.MI.4886<System.Object>()#"
		call void Generated1395.Generated1395::M.IBase2.A.B<class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C805::Method0.12437()#G2_C805::Method1.12438()#G2_C805::Method2.12440<System.Object>()#G2_C805::Method3.12441<System.Object>()#G2_C805::Method4.12443()#G2_C805::Method5.12445()#G2_C805::Method6.12447<System.Object>()#G1_C15::Method7.4885<System.Object>()#"
		call void Generated1395.Generated1395::M.G2_C805.T.T<class Generated1395.BaseClass1,class Generated1395.BaseClass0,class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C805::Method0.12437()#G2_C805::Method1.12438()#G2_C805::Method2.12440<System.Object>()#G2_C805::Method3.12441<System.Object>()#G2_C805::Method4.12443()#G2_C805::Method5.12445()#G2_C805::Method6.12447<System.Object>()#G1_C15::Method7.4885<System.Object>()#"
		call void Generated1395.Generated1395::M.G2_C805.B.T<class Generated1395.BaseClass0,class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C805::Method0.12437()#G2_C805::Method1.12438()#G2_C805::Method2.12440<System.Object>()#G2_C805::Method3.12441<System.Object>()#G2_C805::Method4.12443()#G2_C805::Method5.12445()#G2_C805::Method6.12447<System.Object>()#G1_C15::Method7.4885<System.Object>()#"
		call void Generated1395.Generated1395::M.G2_C805.B.A<class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C805::Method0.12437()#G2_C805::Method1.MI.12439()#G2_C805::Method2.12440<System.Object>()#G2_C805::Method3.MI.12442<System.Object>()#"
		call void Generated1395.Generated1395::M.Generated1395.IBase0<class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0>>(!!0,string)
		newobj instance void class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C805::Method4.12443()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G1_C15::Method7.4885<System.Object>()#"
		call void Generated1395.Generated1395::M.G1_C15.T.T<class Generated1395.BaseClass0,class Generated1395.BaseClass0,class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C805::Method4.12443()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G1_C15::Method7.4885<System.Object>()#"
		call void Generated1395.Generated1395::M.G1_C15.A.T<class Generated1395.BaseClass0,class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C805::Method4.12443()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G1_C15::Method7.4885<System.Object>()#"
		call void Generated1395.Generated1395::M.G1_C15.A.A<class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C15::Method7.MI.4886<System.Object>()#"
		call void Generated1395.Generated1395::M.IBase2.T.T<class Generated1395.BaseClass0,class Generated1395.BaseClass0,class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C15::Method7.MI.4886<System.Object>()#"
		call void Generated1395.Generated1395::M.IBase2.A.T<class Generated1395.BaseClass0,class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C15::Method7.MI.4886<System.Object>()#"
		call void Generated1395.Generated1395::M.IBase2.A.A<class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C805::Method4.MI.12444()#G2_C805::Method5.MI.12446()#G2_C805::Method6.MI.12448<System.Object>()#"
		call void Generated1395.Generated1395::M.IBase1.T<class Generated1395.BaseClass0,class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C805::Method4.MI.12444()#G2_C805::Method5.MI.12446()#G2_C805::Method6.MI.12448<System.Object>()#"
		call void Generated1395.Generated1395::M.IBase1.A<class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C15::Method7.MI.4886<System.Object>()#"
		call void Generated1395.Generated1395::M.IBase2.T.T<class Generated1395.BaseClass0,class Generated1395.BaseClass1,class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C15::Method7.MI.4886<System.Object>()#"
		call void Generated1395.Generated1395::M.IBase2.A.T<class Generated1395.BaseClass1,class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C15::Method7.MI.4886<System.Object>()#"
		call void Generated1395.Generated1395::M.IBase2.A.B<class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C805::Method0.12437()#G2_C805::Method1.12438()#G2_C805::Method2.12440<System.Object>()#G2_C805::Method3.12441<System.Object>()#G2_C805::Method4.12443()#G2_C805::Method5.12445()#G2_C805::Method6.12447<System.Object>()#G1_C15::Method7.4885<System.Object>()#"
		call void Generated1395.Generated1395::M.G2_C805.T.T<class Generated1395.BaseClass1,class Generated1395.BaseClass1,class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C805::Method0.12437()#G2_C805::Method1.12438()#G2_C805::Method2.12440<System.Object>()#G2_C805::Method3.12441<System.Object>()#G2_C805::Method4.12443()#G2_C805::Method5.12445()#G2_C805::Method6.12447<System.Object>()#G1_C15::Method7.4885<System.Object>()#"
		call void Generated1395.Generated1395::M.G2_C805.B.T<class Generated1395.BaseClass1,class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C805::Method0.12437()#G2_C805::Method1.12438()#G2_C805::Method2.12440<System.Object>()#G2_C805::Method3.12441<System.Object>()#G2_C805::Method4.12443()#G2_C805::Method5.12445()#G2_C805::Method6.12447<System.Object>()#G1_C15::Method7.4885<System.Object>()#"
		call void Generated1395.Generated1395::M.G2_C805.B.B<class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C805::Method0.12437()#G2_C805::Method1.MI.12439()#G2_C805::Method2.12440<System.Object>()#G2_C805::Method3.MI.12442<System.Object>()#"
		call void Generated1395.Generated1395::M.Generated1395.IBase0<class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C805::Method4.MI.12444()#G2_C805::Method5.MI.12446()#G2_C805::Method6.MI.12448<System.Object>()#"
		call void Generated1395.Generated1395::M.IBase1.T<class Generated1395.BaseClass1,class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C805::Method4.MI.12444()#G2_C805::Method5.MI.12446()#G2_C805::Method6.MI.12448<System.Object>()#"
		call void Generated1395.Generated1395::M.IBase1.B<class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>>(!!0,string)
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
		newobj instance void class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1395.G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1395.G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>::Method6<object>()
		calli default string(class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>)
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0> on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1395.G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1395.G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>::Method5()
		calli default string(class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>)
		ldstr "G1_C15::Method5.4889()"
		ldstr "class G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0> on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1395.G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1395.G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>::Method4()
		calli default string(class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>)
		ldstr "G2_C805::Method4.12443()"
		ldstr "class G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0> on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1395.G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1395.G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>::Method7<object>()
		calli default string(class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>)
		ldstr "G1_C15::Method7.4885<System.Object>()"
		ldstr "class G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0> on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1395.IBase2`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>::Method7<object>()
		calli default string(class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>)
		ldstr "G1_C15::Method7.MI.4886<System.Object>()"
		ldstr "class IBase2`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0> on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1395.IBase1`1<class Generated1395.BaseClass0>::Method4()
		calli default string(class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>)
		ldstr "G2_C805::Method4.MI.12444()"
		ldstr "class IBase1`1<class Generated1395.BaseClass0> on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1395.IBase1`1<class Generated1395.BaseClass0>::Method5()
		calli default string(class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>)
		ldstr "G2_C805::Method5.MI.12446()"
		ldstr "class IBase1`1<class Generated1395.BaseClass0> on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1395.IBase1`1<class Generated1395.BaseClass0>::Method6<object>()
		calli default string(class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>)
		ldstr "G2_C805::Method6.MI.12448<System.Object>()"
		ldstr "class IBase1`1<class Generated1395.BaseClass0> on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1395.IBase2`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>::Method7<object>()
		calli default string(class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>)
		ldstr "G3_C1867::Method7.18431<System.Object>()"
		ldstr "class IBase2`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1> on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>::Method6<object>()
		calli default string(class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>)
		ldstr "G2_C805::Method6.12447<System.Object>()"
		ldstr "class G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1> on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>::Method5()
		calli default string(class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>)
		ldstr "G2_C805::Method5.12445()"
		ldstr "class G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1> on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>::Method4()
		calli default string(class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>)
		ldstr "G2_C805::Method4.12443()"
		ldstr "class G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1> on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>::Method3<object>()
		calli default string(class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>)
		ldstr "G2_C805::Method3.12441<System.Object>()"
		ldstr "class G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1> on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>::Method2<object>()
		calli default string(class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>)
		ldstr "G2_C805::Method2.12440<System.Object>()"
		ldstr "class G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1> on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>::Method1()
		calli default string(class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>)
		ldstr "G2_C805::Method1.12438()"
		ldstr "class G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1> on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>::Method0()
		calli default string(class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>)
		ldstr "G2_C805::Method0.12437()"
		ldstr "class G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1> on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>::Method7<object>()
		calli default string(class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>)
		ldstr "G1_C15::Method7.4885<System.Object>()"
		ldstr "class G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1> on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated1395.IBase0::Method0()
		calli default string(class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>)
		ldstr "G2_C805::Method0.12437()"
		ldstr "Generated1395.IBase0 on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated1395.IBase0::Method1()
		calli default string(class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>)
		ldstr "G2_C805::Method1.MI.12439()"
		ldstr "Generated1395.IBase0 on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated1395.IBase0::Method2<object>()
		calli default string(class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>)
		ldstr "G2_C805::Method2.12440<System.Object>()"
		ldstr "Generated1395.IBase0 on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated1395.IBase0::Method3<object>()
		calli default string(class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>)
		ldstr "G2_C805::Method3.MI.12442<System.Object>()"
		ldstr "Generated1395.IBase0 on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1395.IBase1`1<class Generated1395.BaseClass1>::Method4()
		calli default string(class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>)
		ldstr "G2_C805::Method4.MI.12444()"
		ldstr "class IBase1`1<class Generated1395.BaseClass1> on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1395.IBase1`1<class Generated1395.BaseClass1>::Method5()
		calli default string(class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>)
		ldstr "G2_C805::Method5.MI.12446()"
		ldstr "class IBase1`1<class Generated1395.BaseClass1> on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1395.IBase1`1<class Generated1395.BaseClass1>::Method6<object>()
		calli default string(class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>)
		ldstr "G2_C805::Method6.MI.12448<System.Object>()"
		ldstr "class IBase1`1<class Generated1395.BaseClass1> on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>::ClassMethod5128<object>()
		calli default string(class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>)
		ldstr "G3_C1867::ClassMethod5128.18435<System.Object>()"
		ldstr "class G3_C1867`1<class Generated1395.BaseClass0> on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>::ClassMethod5127<object>()
		calli default string(class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>)
		ldstr "G3_C1867::ClassMethod5127.18434<System.Object>()"
		ldstr "class G3_C1867`1<class Generated1395.BaseClass0> on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>::ClassMethod5126()
		calli default string(class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>)
		ldstr "G3_C1867::ClassMethod5126.18433()"
		ldstr "class G3_C1867`1<class Generated1395.BaseClass0> on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>::ClassMethod5125()
		calli default string(class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>)
		ldstr "G3_C1867::ClassMethod5125.18432()"
		ldstr "class G3_C1867`1<class Generated1395.BaseClass0> on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>::Method7<object>()
		calli default string(class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>)
		ldstr "G3_C1867::Method7.18431<System.Object>()"
		ldstr "class G3_C1867`1<class Generated1395.BaseClass0> on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>::Method6<object>()
		calli default string(class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>)
		ldstr "G2_C805::Method6.12447<System.Object>()"
		ldstr "class G3_C1867`1<class Generated1395.BaseClass0> on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>::Method5()
		calli default string(class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>)
		ldstr "G2_C805::Method5.12445()"
		ldstr "class G3_C1867`1<class Generated1395.BaseClass0> on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>::Method4()
		calli default string(class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>)
		ldstr "G2_C805::Method4.12443()"
		ldstr "class G3_C1867`1<class Generated1395.BaseClass0> on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>::Method3<object>()
		calli default string(class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>)
		ldstr "G2_C805::Method3.12441<System.Object>()"
		ldstr "class G3_C1867`1<class Generated1395.BaseClass0> on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>::Method2<object>()
		calli default string(class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>)
		ldstr "G2_C805::Method2.12440<System.Object>()"
		ldstr "class G3_C1867`1<class Generated1395.BaseClass0> on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>::Method1()
		calli default string(class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>)
		ldstr "G2_C805::Method1.12438()"
		ldstr "class G3_C1867`1<class Generated1395.BaseClass0> on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>::Method0()
		calli default string(class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>)
		ldstr "G2_C805::Method0.12437()"
		ldstr "class G3_C1867`1<class Generated1395.BaseClass0> on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1395.G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1395.G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>::Method6<object>()
		calli default string(class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>)
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0> on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1395.G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1395.G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>::Method5()
		calli default string(class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>)
		ldstr "G1_C15::Method5.4889()"
		ldstr "class G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0> on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1395.G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1395.G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>::Method4()
		calli default string(class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>)
		ldstr "G2_C805::Method4.12443()"
		ldstr "class G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0> on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1395.G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1395.G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>::Method7<object>()
		calli default string(class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>)
		ldstr "G1_C15::Method7.4885<System.Object>()"
		ldstr "class G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0> on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1395.IBase2`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>::Method7<object>()
		calli default string(class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>)
		ldstr "G1_C15::Method7.MI.4886<System.Object>()"
		ldstr "class IBase2`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0> on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1395.IBase1`1<class Generated1395.BaseClass0>::Method4()
		calli default string(class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>)
		ldstr "G2_C805::Method4.MI.12444()"
		ldstr "class IBase1`1<class Generated1395.BaseClass0> on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1395.IBase1`1<class Generated1395.BaseClass0>::Method5()
		calli default string(class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>)
		ldstr "G2_C805::Method5.MI.12446()"
		ldstr "class IBase1`1<class Generated1395.BaseClass0> on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1395.IBase1`1<class Generated1395.BaseClass0>::Method6<object>()
		calli default string(class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>)
		ldstr "G2_C805::Method6.MI.12448<System.Object>()"
		ldstr "class IBase1`1<class Generated1395.BaseClass0> on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1395.IBase2`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>::Method7<object>()
		calli default string(class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>)
		ldstr "G3_C1867::Method7.18431<System.Object>()"
		ldstr "class IBase2`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1> on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>::Method6<object>()
		calli default string(class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>)
		ldstr "G2_C805::Method6.12447<System.Object>()"
		ldstr "class G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1> on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>::Method5()
		calli default string(class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>)
		ldstr "G2_C805::Method5.12445()"
		ldstr "class G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1> on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>::Method4()
		calli default string(class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>)
		ldstr "G2_C805::Method4.12443()"
		ldstr "class G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1> on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>::Method3<object>()
		calli default string(class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>)
		ldstr "G2_C805::Method3.12441<System.Object>()"
		ldstr "class G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1> on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>::Method2<object>()
		calli default string(class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>)
		ldstr "G2_C805::Method2.12440<System.Object>()"
		ldstr "class G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1> on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>::Method1()
		calli default string(class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>)
		ldstr "G2_C805::Method1.12438()"
		ldstr "class G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1> on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>::Method0()
		calli default string(class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>)
		ldstr "G2_C805::Method0.12437()"
		ldstr "class G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1> on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>::Method7<object>()
		calli default string(class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>)
		ldstr "G1_C15::Method7.4885<System.Object>()"
		ldstr "class G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1> on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated1395.IBase0::Method0()
		calli default string(class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>)
		ldstr "G2_C805::Method0.12437()"
		ldstr "Generated1395.IBase0 on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated1395.IBase0::Method1()
		calli default string(class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>)
		ldstr "G2_C805::Method1.MI.12439()"
		ldstr "Generated1395.IBase0 on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated1395.IBase0::Method2<object>()
		calli default string(class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>)
		ldstr "G2_C805::Method2.12440<System.Object>()"
		ldstr "Generated1395.IBase0 on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated1395.IBase0::Method3<object>()
		calli default string(class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>)
		ldstr "G2_C805::Method3.MI.12442<System.Object>()"
		ldstr "Generated1395.IBase0 on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1395.IBase1`1<class Generated1395.BaseClass1>::Method4()
		calli default string(class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>)
		ldstr "G2_C805::Method4.MI.12444()"
		ldstr "class IBase1`1<class Generated1395.BaseClass1> on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1395.IBase1`1<class Generated1395.BaseClass1>::Method5()
		calli default string(class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>)
		ldstr "G2_C805::Method5.MI.12446()"
		ldstr "class IBase1`1<class Generated1395.BaseClass1> on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1395.IBase1`1<class Generated1395.BaseClass1>::Method6<object>()
		calli default string(class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>)
		ldstr "G2_C805::Method6.MI.12448<System.Object>()"
		ldstr "class IBase1`1<class Generated1395.BaseClass1> on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>::ClassMethod5128<object>()
		calli default string(class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>)
		ldstr "G3_C1867::ClassMethod5128.18435<System.Object>()"
		ldstr "class G3_C1867`1<class Generated1395.BaseClass1> on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>::ClassMethod5127<object>()
		calli default string(class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>)
		ldstr "G3_C1867::ClassMethod5127.18434<System.Object>()"
		ldstr "class G3_C1867`1<class Generated1395.BaseClass1> on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>::ClassMethod5126()
		calli default string(class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>)
		ldstr "G3_C1867::ClassMethod5126.18433()"
		ldstr "class G3_C1867`1<class Generated1395.BaseClass1> on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>::ClassMethod5125()
		calli default string(class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>)
		ldstr "G3_C1867::ClassMethod5125.18432()"
		ldstr "class G3_C1867`1<class Generated1395.BaseClass1> on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>::Method7<object>()
		calli default string(class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>)
		ldstr "G3_C1867::Method7.18431<System.Object>()"
		ldstr "class G3_C1867`1<class Generated1395.BaseClass1> on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>::Method6<object>()
		calli default string(class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>)
		ldstr "G2_C805::Method6.12447<System.Object>()"
		ldstr "class G3_C1867`1<class Generated1395.BaseClass1> on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>::Method5()
		calli default string(class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>)
		ldstr "G2_C805::Method5.12445()"
		ldstr "class G3_C1867`1<class Generated1395.BaseClass1> on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>::Method4()
		calli default string(class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>)
		ldstr "G2_C805::Method4.12443()"
		ldstr "class G3_C1867`1<class Generated1395.BaseClass1> on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>::Method3<object>()
		calli default string(class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>)
		ldstr "G2_C805::Method3.12441<System.Object>()"
		ldstr "class G3_C1867`1<class Generated1395.BaseClass1> on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>::Method2<object>()
		calli default string(class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>)
		ldstr "G2_C805::Method2.12440<System.Object>()"
		ldstr "class G3_C1867`1<class Generated1395.BaseClass1> on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>::Method1()
		calli default string(class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>)
		ldstr "G2_C805::Method1.12438()"
		ldstr "class G3_C1867`1<class Generated1395.BaseClass1> on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>::Method0()
		calli default string(class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>)
		ldstr "G2_C805::Method0.12437()"
		ldstr "class G3_C1867`1<class Generated1395.BaseClass1> on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1395.IBase2`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>::Method7<object>()
		calli default string(class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>)
		ldstr "G3_C1867::Method7.18431<System.Object>()"
		ldstr "class IBase2`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1> on type class Generated1395.G3_C1867`1<class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1395.G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1395.G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>::Method6<object>()
		calli default string(class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>)
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1395.G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1395.G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>::Method5()
		calli default string(class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>)
		ldstr "G1_C15::Method5.4889()"
		ldstr "class G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1395.G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1395.G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>::Method4()
		calli default string(class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>)
		ldstr "G2_C805::Method4.12443()"
		ldstr "class G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1395.G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1395.G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>::Method7<object>()
		calli default string(class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>)
		ldstr "G1_C15::Method7.4885<System.Object>()"
		ldstr "class G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1395.IBase2`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>::Method7<object>()
		calli default string(class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>)
		ldstr "G1_C15::Method7.MI.4886<System.Object>()"
		ldstr "class IBase2`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1395.IBase1`1<class Generated1395.BaseClass0>::Method4()
		calli default string(class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>)
		ldstr "G2_C805::Method4.MI.12444()"
		ldstr "class IBase1`1<class Generated1395.BaseClass0> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1395.IBase1`1<class Generated1395.BaseClass0>::Method5()
		calli default string(class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>)
		ldstr "G2_C805::Method5.MI.12446()"
		ldstr "class IBase1`1<class Generated1395.BaseClass0> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1395.IBase1`1<class Generated1395.BaseClass0>::Method6<object>()
		calli default string(class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>)
		ldstr "G2_C805::Method6.MI.12448<System.Object>()"
		ldstr "class IBase1`1<class Generated1395.BaseClass0> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1395.IBase2`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>::Method7<object>()
		calli default string(class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>)
		ldstr "G1_C15::Method7.MI.4886<System.Object>()"
		ldstr "class IBase2`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>::Method6<object>()
		calli default string(class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>)
		ldstr "G2_C805::Method6.12447<System.Object>()"
		ldstr "class G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>::Method5()
		calli default string(class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>)
		ldstr "G2_C805::Method5.12445()"
		ldstr "class G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>::Method4()
		calli default string(class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>)
		ldstr "G2_C805::Method4.12443()"
		ldstr "class G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>::Method3<object>()
		calli default string(class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>)
		ldstr "G2_C805::Method3.12441<System.Object>()"
		ldstr "class G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>::Method2<object>()
		calli default string(class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>)
		ldstr "G2_C805::Method2.12440<System.Object>()"
		ldstr "class G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>::Method1()
		calli default string(class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>)
		ldstr "G2_C805::Method1.12438()"
		ldstr "class G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>::Method0()
		calli default string(class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>)
		ldstr "G2_C805::Method0.12437()"
		ldstr "class G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>::Method7<object>()
		calli default string(class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>)
		ldstr "G1_C15::Method7.4885<System.Object>()"
		ldstr "class G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated1395.IBase0::Method0()
		calli default string(class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>)
		ldstr "G2_C805::Method0.12437()"
		ldstr "Generated1395.IBase0 on type class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated1395.IBase0::Method1()
		calli default string(class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>)
		ldstr "G2_C805::Method1.MI.12439()"
		ldstr "Generated1395.IBase0 on type class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated1395.IBase0::Method2<object>()
		calli default string(class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>)
		ldstr "G2_C805::Method2.12440<System.Object>()"
		ldstr "Generated1395.IBase0 on type class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated1395.IBase0::Method3<object>()
		calli default string(class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>)
		ldstr "G2_C805::Method3.MI.12442<System.Object>()"
		ldstr "Generated1395.IBase0 on type class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1395.G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1395.G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>::Method6<object>()
		calli default string(class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>)
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1395.G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1395.G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>::Method5()
		calli default string(class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>)
		ldstr "G1_C15::Method5.4889()"
		ldstr "class G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1395.G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1395.G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>::Method4()
		calli default string(class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>)
		ldstr "G2_C805::Method4.12443()"
		ldstr "class G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1395.G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1395.G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>::Method7<object>()
		calli default string(class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>)
		ldstr "G1_C15::Method7.4885<System.Object>()"
		ldstr "class G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1395.IBase2`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>::Method7<object>()
		calli default string(class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>)
		ldstr "G1_C15::Method7.MI.4886<System.Object>()"
		ldstr "class IBase2`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1395.IBase1`1<class Generated1395.BaseClass0>::Method4()
		calli default string(class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>)
		ldstr "G2_C805::Method4.MI.12444()"
		ldstr "class IBase1`1<class Generated1395.BaseClass0> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1395.IBase1`1<class Generated1395.BaseClass0>::Method5()
		calli default string(class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>)
		ldstr "G2_C805::Method5.MI.12446()"
		ldstr "class IBase1`1<class Generated1395.BaseClass0> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1395.IBase1`1<class Generated1395.BaseClass0>::Method6<object>()
		calli default string(class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>)
		ldstr "G2_C805::Method6.MI.12448<System.Object>()"
		ldstr "class IBase1`1<class Generated1395.BaseClass0> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1395.IBase2`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>::Method7<object>()
		calli default string(class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>)
		ldstr "G1_C15::Method7.MI.4886<System.Object>()"
		ldstr "class IBase2`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>::Method6<object>()
		calli default string(class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>)
		ldstr "G2_C805::Method6.12447<System.Object>()"
		ldstr "class G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>::Method5()
		calli default string(class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>)
		ldstr "G2_C805::Method5.12445()"
		ldstr "class G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>::Method4()
		calli default string(class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>)
		ldstr "G2_C805::Method4.12443()"
		ldstr "class G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>::Method3<object>()
		calli default string(class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>)
		ldstr "G2_C805::Method3.12441<System.Object>()"
		ldstr "class G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>::Method2<object>()
		calli default string(class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>)
		ldstr "G2_C805::Method2.12440<System.Object>()"
		ldstr "class G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>::Method1()
		calli default string(class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>)
		ldstr "G2_C805::Method1.12438()"
		ldstr "class G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>::Method0()
		calli default string(class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>)
		ldstr "G2_C805::Method0.12437()"
		ldstr "class G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>::Method7<object>()
		calli default string(class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>)
		ldstr "G1_C15::Method7.4885<System.Object>()"
		ldstr "class G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated1395.IBase0::Method0()
		calli default string(class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>)
		ldstr "G2_C805::Method0.12437()"
		ldstr "Generated1395.IBase0 on type class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated1395.IBase0::Method1()
		calli default string(class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>)
		ldstr "G2_C805::Method1.MI.12439()"
		ldstr "Generated1395.IBase0 on type class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated1395.IBase0::Method2<object>()
		calli default string(class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>)
		ldstr "G2_C805::Method2.12440<System.Object>()"
		ldstr "Generated1395.IBase0 on type class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated1395.IBase0::Method3<object>()
		calli default string(class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>)
		ldstr "G2_C805::Method3.MI.12442<System.Object>()"
		ldstr "Generated1395.IBase0 on type class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1395.IBase1`1<class Generated1395.BaseClass1>::Method4()
		calli default string(class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>)
		ldstr "G2_C805::Method4.MI.12444()"
		ldstr "class IBase1`1<class Generated1395.BaseClass1> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1395.IBase1`1<class Generated1395.BaseClass1>::Method5()
		calli default string(class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>)
		ldstr "G2_C805::Method5.MI.12446()"
		ldstr "class IBase1`1<class Generated1395.BaseClass1> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1395.IBase1`1<class Generated1395.BaseClass1>::Method6<object>()
		calli default string(class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>)
		ldstr "G2_C805::Method6.MI.12448<System.Object>()"
		ldstr "class IBase1`1<class Generated1395.BaseClass1> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1395.G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1395.G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>::Method6<object>()
		calli default string(class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0>)
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1395.G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1395.G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>::Method5()
		calli default string(class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0>)
		ldstr "G1_C15::Method5.4889()"
		ldstr "class G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1395.G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1395.G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>::Method4()
		calli default string(class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0>)
		ldstr "G2_C805::Method4.12443()"
		ldstr "class G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1395.G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1395.G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>::Method7<object>()
		calli default string(class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0>)
		ldstr "G1_C15::Method7.4885<System.Object>()"
		ldstr "class G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1395.IBase2`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>::Method7<object>()
		calli default string(class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0>)
		ldstr "G1_C15::Method7.MI.4886<System.Object>()"
		ldstr "class IBase2`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1395.IBase1`1<class Generated1395.BaseClass0>::Method4()
		calli default string(class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0>)
		ldstr "G2_C805::Method4.MI.12444()"
		ldstr "class IBase1`1<class Generated1395.BaseClass0> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1395.IBase1`1<class Generated1395.BaseClass0>::Method5()
		calli default string(class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0>)
		ldstr "G2_C805::Method5.MI.12446()"
		ldstr "class IBase1`1<class Generated1395.BaseClass0> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1395.IBase1`1<class Generated1395.BaseClass0>::Method6<object>()
		calli default string(class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0>)
		ldstr "G2_C805::Method6.MI.12448<System.Object>()"
		ldstr "class IBase1`1<class Generated1395.BaseClass0> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1395.IBase2`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>::Method7<object>()
		calli default string(class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0>)
		ldstr "G1_C15::Method7.MI.4886<System.Object>()"
		ldstr "class IBase2`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0>::Method6<object>()
		calli default string(class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0>)
		ldstr "G2_C805::Method6.12447<System.Object>()"
		ldstr "class G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0>::Method5()
		calli default string(class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0>)
		ldstr "G2_C805::Method5.12445()"
		ldstr "class G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0>::Method4()
		calli default string(class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0>)
		ldstr "G2_C805::Method4.12443()"
		ldstr "class G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0>::Method3<object>()
		calli default string(class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0>)
		ldstr "G2_C805::Method3.12441<System.Object>()"
		ldstr "class G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0>::Method2<object>()
		calli default string(class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0>)
		ldstr "G2_C805::Method2.12440<System.Object>()"
		ldstr "class G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0>::Method1()
		calli default string(class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0>)
		ldstr "G2_C805::Method1.12438()"
		ldstr "class G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0>::Method0()
		calli default string(class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0>)
		ldstr "G2_C805::Method0.12437()"
		ldstr "class G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0>::Method7<object>()
		calli default string(class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0>)
		ldstr "G1_C15::Method7.4885<System.Object>()"
		ldstr "class G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated1395.IBase0::Method0()
		calli default string(class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0>)
		ldstr "G2_C805::Method0.12437()"
		ldstr "Generated1395.IBase0 on type class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated1395.IBase0::Method1()
		calli default string(class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0>)
		ldstr "G2_C805::Method1.MI.12439()"
		ldstr "Generated1395.IBase0 on type class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated1395.IBase0::Method2<object>()
		calli default string(class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0>)
		ldstr "G2_C805::Method2.12440<System.Object>()"
		ldstr "Generated1395.IBase0 on type class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated1395.IBase0::Method3<object>()
		calli default string(class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0>)
		ldstr "G2_C805::Method3.MI.12442<System.Object>()"
		ldstr "Generated1395.IBase0 on type class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1395.G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1395.G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>::Method6<object>()
		calli default string(class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>)
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1395.G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1395.G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>::Method5()
		calli default string(class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>)
		ldstr "G1_C15::Method5.4889()"
		ldstr "class G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1395.G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1395.G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>::Method4()
		calli default string(class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>)
		ldstr "G2_C805::Method4.12443()"
		ldstr "class G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1395.G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1395.G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>::Method7<object>()
		calli default string(class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>)
		ldstr "G1_C15::Method7.4885<System.Object>()"
		ldstr "class G1_C15`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1395.IBase2`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0>::Method7<object>()
		calli default string(class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>)
		ldstr "G1_C15::Method7.MI.4886<System.Object>()"
		ldstr "class IBase2`2<class Generated1395.BaseClass0,class Generated1395.BaseClass0> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1395.IBase1`1<class Generated1395.BaseClass0>::Method4()
		calli default string(class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>)
		ldstr "G2_C805::Method4.MI.12444()"
		ldstr "class IBase1`1<class Generated1395.BaseClass0> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1395.IBase1`1<class Generated1395.BaseClass0>::Method5()
		calli default string(class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>)
		ldstr "G2_C805::Method5.MI.12446()"
		ldstr "class IBase1`1<class Generated1395.BaseClass0> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1395.IBase1`1<class Generated1395.BaseClass0>::Method6<object>()
		calli default string(class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>)
		ldstr "G2_C805::Method6.MI.12448<System.Object>()"
		ldstr "class IBase1`1<class Generated1395.BaseClass0> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1395.IBase2`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1>::Method7<object>()
		calli default string(class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>)
		ldstr "G1_C15::Method7.MI.4886<System.Object>()"
		ldstr "class IBase2`2<class Generated1395.BaseClass0,class Generated1395.BaseClass1> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>::Method6<object>()
		calli default string(class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>)
		ldstr "G2_C805::Method6.12447<System.Object>()"
		ldstr "class G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>::Method5()
		calli default string(class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>)
		ldstr "G2_C805::Method5.12445()"
		ldstr "class G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>::Method4()
		calli default string(class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>)
		ldstr "G2_C805::Method4.12443()"
		ldstr "class G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>::Method3<object>()
		calli default string(class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>)
		ldstr "G2_C805::Method3.12441<System.Object>()"
		ldstr "class G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>::Method2<object>()
		calli default string(class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>)
		ldstr "G2_C805::Method2.12440<System.Object>()"
		ldstr "class G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>::Method1()
		calli default string(class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>)
		ldstr "G2_C805::Method1.12438()"
		ldstr "class G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>::Method0()
		calli default string(class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>)
		ldstr "G2_C805::Method0.12437()"
		ldstr "class G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>::Method7<object>()
		calli default string(class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>)
		ldstr "G1_C15::Method7.4885<System.Object>()"
		ldstr "class G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated1395.IBase0::Method0()
		calli default string(class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>)
		ldstr "G2_C805::Method0.12437()"
		ldstr "Generated1395.IBase0 on type class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated1395.IBase0::Method1()
		calli default string(class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>)
		ldstr "G2_C805::Method1.MI.12439()"
		ldstr "Generated1395.IBase0 on type class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated1395.IBase0::Method2<object>()
		calli default string(class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>)
		ldstr "G2_C805::Method2.12440<System.Object>()"
		ldstr "Generated1395.IBase0 on type class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated1395.IBase0::Method3<object>()
		calli default string(class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>)
		ldstr "G2_C805::Method3.MI.12442<System.Object>()"
		ldstr "Generated1395.IBase0 on type class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1395.IBase1`1<class Generated1395.BaseClass1>::Method4()
		calli default string(class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>)
		ldstr "G2_C805::Method4.MI.12444()"
		ldstr "class IBase1`1<class Generated1395.BaseClass1> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1395.IBase1`1<class Generated1395.BaseClass1>::Method5()
		calli default string(class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>)
		ldstr "G2_C805::Method5.MI.12446()"
		ldstr "class IBase1`1<class Generated1395.BaseClass1> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1395.IBase1`1<class Generated1395.BaseClass1>::Method6<object>()
		calli default string(class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>)
		ldstr "G2_C805::Method6.MI.12448<System.Object>()"
		ldstr "class IBase1`1<class Generated1395.BaseClass1> on type class Generated1395.G2_C805`2<class Generated1395.BaseClass1,class Generated1395.BaseClass1>"
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
		call void Generated1395.Generated1395::MethodCallingTest()
		call void Generated1395.Generated1395::ConstrainedCallsTest()
		call void Generated1395.Generated1395::StructConstrainedInterfaceCallsTest()
		call void Generated1395.Generated1395::CalliTest()
		ldc.i4 100
		ret
	}
}
