// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) .ver 4:0:0:0 }
.assembly extern TestFramework { .publickeytoken = ( B0 3F 5F 7F 11 D5 0A 3A )  }

//TYPES IN FORWARDER ASSEMBLIES:

//TEST ASSEMBLY:
.assembly extern xunit.core {}

.class public Generated1419.BaseClass0 
{
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void [mscorlib]System.Object::.ctor()
		ret
	}
}
.class public Generated1419.BaseClass1 
		extends Generated1419.BaseClass0
{
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void Generated1419.BaseClass0::.ctor()
		ret
	}
}
.class public Generated1419.G3_C1891`1<T0> 
		extends class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>
		implements class Generated1419.IBase1`1<class Generated1419.BaseClass0> 
{
	.method public hidebysig virtual instance string Method4() cil managed noinlining { 
		ldstr "G3_C1891::Method4.18566()"
		ret
	}
	.method public hidebysig newslot virtual instance string Method5() cil managed noinlining { 
		ldstr "G3_C1891::Method5.18567()"
		ret
	}
	.method public hidebysig newslot virtual instance string Method6<M0>() cil managed noinlining { 
		ldstr "G3_C1891::Method6.18568<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<class Generated1419.BaseClass0>.Method6'<M0>() cil managed noinlining { 
		.override method instance string class Generated1419.IBase1`1<class Generated1419.BaseClass0>::Method6<[1]>()
		ldstr "G3_C1891::Method6.MI.18569<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod5177() cil managed noinlining { 
		ldstr "G3_C1891::ClassMethod5177.18570()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod5178<M0>() cil managed noinlining { 
		ldstr "G3_C1891::ClassMethod5178.18571<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>::.ctor()
		ret
	}
}
.class public Generated1419.G2_C818`2<T0, T1> 
		extends class Generated1419.G1_C15`2<class Generated1419.BaseClass0,!T1>
		implements class Generated1419.IBase2`2<!T0,!T1> 
{
	.method public hidebysig newslot virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G2_C818::Method7.12541<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod3017() cil managed noinlining { 
		ldstr "G2_C818::ClassMethod3017.12542()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod3018<M0>() cil managed noinlining { 
		ldstr "G2_C818::ClassMethod3018.12543<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class Generated1419.G1_C15`2<class Generated1419.BaseClass0,!T1>::.ctor()
		ret
	}
}
.class interface public abstract Generated1419.IBase1`1<+T0> 
{
	.method public hidebysig newslot abstract virtual instance string Method4() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method5() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method6<M0>() cil managed { }
}
.class public abstract Generated1419.G1_C15`2<T0, T1> 
		implements class Generated1419.IBase2`2<!T1,!T1>, class Generated1419.IBase1`1<class Generated1419.BaseClass0> 
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
		.override method instance string class Generated1419.IBase2`2<!T1,!T1>::Method7<[1]>()
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
	.method public hidebysig newslot virtual instance string 'IBase1<class Generated1419.BaseClass0>.Method4'() cil managed noinlining { 
		.override method instance string class Generated1419.IBase1`1<class Generated1419.BaseClass0>::Method4()
		ldstr "G1_C15::Method4.MI.4888()"
		ret
	}
	.method public hidebysig virtual instance string Method5() cil managed noinlining { 
		ldstr "G1_C15::Method5.4889()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<class Generated1419.BaseClass0>.Method5'() cil managed noinlining { 
		.override method instance string class Generated1419.IBase1`1<class Generated1419.BaseClass0>::Method5()
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
	.method public hidebysig newslot virtual instance string 'IBase1<class Generated1419.BaseClass0>.Method6'<M0>() cil managed noinlining { 
		.override method instance string class Generated1419.IBase1`1<class Generated1419.BaseClass0>::Method6<[1]>()
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
.class interface public abstract Generated1419.IBase2`2<+T0, -T1> 
{
	.method public hidebysig newslot abstract virtual instance string Method7<M0>() cil managed { }
}
.class public auto ansi beforefieldinit Generated1419.Generated1419 {
	.method static void M.BaseClass0<(Generated1419.BaseClass0)W>(!!W inst, string exp) cil managed {
		.maxstack 5
		.locals init (string[] actualResults)
		ldc.i4.s    0
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.BaseClass0<(Generated1419.BaseClass0)W>(!!W inst, string exp)"
		ldc.i4.s 0
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.BaseClass1<(Generated1419.BaseClass1)W>(!!W inst, string exp) cil managed {
		.maxstack 5
		.locals init (string[] actualResults)
		ldc.i4.s    0
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.BaseClass1<(Generated1419.BaseClass1)W>(!!W inst, string exp)"
		ldc.i4.s 0
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1891.T<T0,(class Generated1419.G3_C1891`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1891.T<T0,(class Generated1419.G3_C1891`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1419.G3_C1891`1<!!T0>::ClassMethod3017()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1419.G3_C1891`1<!!T0>::ClassMethod3018<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1419.G3_C1891`1<!!T0>::ClassMethod5177()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1419.G3_C1891`1<!!T0>::ClassMethod5178<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1419.G3_C1891`1<!!T0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1419.G3_C1891`1<!!T0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1419.G3_C1891`1<!!T0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1419.G3_C1891`1<!!T0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1891.A<(class Generated1419.G3_C1891`1<class Generated1419.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1891.A<(class Generated1419.G3_C1891`1<class Generated1419.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1419.G3_C1891`1<class Generated1419.BaseClass0>::ClassMethod3017()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1419.G3_C1891`1<class Generated1419.BaseClass0>::ClassMethod3018<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1419.G3_C1891`1<class Generated1419.BaseClass0>::ClassMethod5177()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1419.G3_C1891`1<class Generated1419.BaseClass0>::ClassMethod5178<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1419.G3_C1891`1<class Generated1419.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1419.G3_C1891`1<class Generated1419.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1419.G3_C1891`1<class Generated1419.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1419.G3_C1891`1<class Generated1419.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1891.B<(class Generated1419.G3_C1891`1<class Generated1419.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1891.B<(class Generated1419.G3_C1891`1<class Generated1419.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1419.G3_C1891`1<class Generated1419.BaseClass1>::ClassMethod3017()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1419.G3_C1891`1<class Generated1419.BaseClass1>::ClassMethod3018<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1419.G3_C1891`1<class Generated1419.BaseClass1>::ClassMethod5177()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1419.G3_C1891`1<class Generated1419.BaseClass1>::ClassMethod5178<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1419.G3_C1891`1<class Generated1419.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1419.G3_C1891`1<class Generated1419.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1419.G3_C1891`1<class Generated1419.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1419.G3_C1891`1<class Generated1419.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C818.T.T<T0,T1,(class Generated1419.G2_C818`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C818.T.T<T0,T1,(class Generated1419.G2_C818`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1419.G2_C818`2<!!T0,!!T1>::ClassMethod3017()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1419.G2_C818`2<!!T0,!!T1>::ClassMethod3018<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1419.G2_C818`2<!!T0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1419.G2_C818`2<!!T0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1419.G2_C818`2<!!T0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1419.G2_C818`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C818.A.T<T1,(class Generated1419.G2_C818`2<class Generated1419.BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C818.A.T<T1,(class Generated1419.G2_C818`2<class Generated1419.BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1419.G2_C818`2<class Generated1419.BaseClass0,!!T1>::ClassMethod3017()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1419.G2_C818`2<class Generated1419.BaseClass0,!!T1>::ClassMethod3018<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1419.G2_C818`2<class Generated1419.BaseClass0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1419.G2_C818`2<class Generated1419.BaseClass0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1419.G2_C818`2<class Generated1419.BaseClass0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1419.G2_C818`2<class Generated1419.BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C818.A.A<(class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C818.A.A<(class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>::ClassMethod3017()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>::ClassMethod3018<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C818.A.B<(class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C818.A.B<(class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>::ClassMethod3017()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>::ClassMethod3018<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C818.B.T<T1,(class Generated1419.G2_C818`2<class Generated1419.BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C818.B.T<T1,(class Generated1419.G2_C818`2<class Generated1419.BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1419.G2_C818`2<class Generated1419.BaseClass1,!!T1>::ClassMethod3017()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1419.G2_C818`2<class Generated1419.BaseClass1,!!T1>::ClassMethod3018<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1419.G2_C818`2<class Generated1419.BaseClass1,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1419.G2_C818`2<class Generated1419.BaseClass1,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1419.G2_C818`2<class Generated1419.BaseClass1,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1419.G2_C818`2<class Generated1419.BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C818.B.A<(class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C818.B.A<(class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass0>::ClassMethod3017()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass0>::ClassMethod3018<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C818.B.B<(class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C818.B.B<(class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1>::ClassMethod3017()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1>::ClassMethod3018<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase1.T<T0,(class Generated1419.IBase1`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase1.T<T0,(class Generated1419.IBase1`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1419.IBase1`1<!!T0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1419.IBase1`1<!!T0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1419.IBase1`1<!!T0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase1.A<(class Generated1419.IBase1`1<class Generated1419.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase1.A<(class Generated1419.IBase1`1<class Generated1419.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1419.IBase1`1<class Generated1419.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1419.IBase1`1<class Generated1419.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1419.IBase1`1<class Generated1419.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase1.B<(class Generated1419.IBase1`1<class Generated1419.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase1.B<(class Generated1419.IBase1`1<class Generated1419.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1419.IBase1`1<class Generated1419.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1419.IBase1`1<class Generated1419.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1419.IBase1`1<class Generated1419.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C15.T.T<T0,T1,(class Generated1419.G1_C15`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 9
		.locals init (string[] actualResults)
		ldc.i4.s    4
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C15.T.T<T0,T1,(class Generated1419.G1_C15`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 4
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1419.G1_C15`2<!!T0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1419.G1_C15`2<!!T0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1419.G1_C15`2<!!T0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1419.G1_C15`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C15.A.T<T1,(class Generated1419.G1_C15`2<class Generated1419.BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 9
		.locals init (string[] actualResults)
		ldc.i4.s    4
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C15.A.T<T1,(class Generated1419.G1_C15`2<class Generated1419.BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 4
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1419.G1_C15`2<class Generated1419.BaseClass0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1419.G1_C15`2<class Generated1419.BaseClass0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1419.G1_C15`2<class Generated1419.BaseClass0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1419.G1_C15`2<class Generated1419.BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C15.A.A<(class Generated1419.G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 9
		.locals init (string[] actualResults)
		ldc.i4.s    4
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C15.A.A<(class Generated1419.G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 4
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1419.G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1419.G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1419.G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1419.G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C15.A.B<(class Generated1419.G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 9
		.locals init (string[] actualResults)
		ldc.i4.s    4
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C15.A.B<(class Generated1419.G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 4
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1419.G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1419.G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1419.G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1419.G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C15.B.T<T1,(class Generated1419.G1_C15`2<class Generated1419.BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 9
		.locals init (string[] actualResults)
		ldc.i4.s    4
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C15.B.T<T1,(class Generated1419.G1_C15`2<class Generated1419.BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 4
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1419.G1_C15`2<class Generated1419.BaseClass1,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1419.G1_C15`2<class Generated1419.BaseClass1,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1419.G1_C15`2<class Generated1419.BaseClass1,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1419.G1_C15`2<class Generated1419.BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C15.B.A<(class Generated1419.G1_C15`2<class Generated1419.BaseClass1,class Generated1419.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 9
		.locals init (string[] actualResults)
		ldc.i4.s    4
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C15.B.A<(class Generated1419.G1_C15`2<class Generated1419.BaseClass1,class Generated1419.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 4
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1419.G1_C15`2<class Generated1419.BaseClass1,class Generated1419.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1419.G1_C15`2<class Generated1419.BaseClass1,class Generated1419.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1419.G1_C15`2<class Generated1419.BaseClass1,class Generated1419.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1419.G1_C15`2<class Generated1419.BaseClass1,class Generated1419.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C15.B.B<(class Generated1419.G1_C15`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 9
		.locals init (string[] actualResults)
		ldc.i4.s    4
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C15.B.B<(class Generated1419.G1_C15`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 4
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1419.G1_C15`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1419.G1_C15`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1419.G1_C15`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1419.G1_C15`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.T.T<T0,T1,(class Generated1419.IBase2`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.T.T<T0,T1,(class Generated1419.IBase2`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1419.IBase2`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.A.T<T1,(class Generated1419.IBase2`2<class Generated1419.BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.A.T<T1,(class Generated1419.IBase2`2<class Generated1419.BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1419.IBase2`2<class Generated1419.BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.A.A<(class Generated1419.IBase2`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.A.A<(class Generated1419.IBase2`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1419.IBase2`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.A.B<(class Generated1419.IBase2`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.A.B<(class Generated1419.IBase2`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1419.IBase2`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.B.T<T1,(class Generated1419.IBase2`2<class Generated1419.BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.B.T<T1,(class Generated1419.IBase2`2<class Generated1419.BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1419.IBase2`2<class Generated1419.BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.B.A<(class Generated1419.IBase2`2<class Generated1419.BaseClass1,class Generated1419.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.B.A<(class Generated1419.IBase2`2<class Generated1419.BaseClass1,class Generated1419.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1419.IBase2`2<class Generated1419.BaseClass1,class Generated1419.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.B.B<(class Generated1419.IBase2`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.B.B<(class Generated1419.IBase2`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1419.IBase2`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1>::Method7<object>()
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
		newobj instance void class Generated1419.G3_C1891`1<class Generated1419.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1419.G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>
		callvirt instance string class Generated1419.G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>::Method6<object>()
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1> on type class Generated1419.G3_C1891`1<class Generated1419.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1419.G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>
		callvirt instance string class Generated1419.G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>::Method5()
		ldstr "G1_C15::Method5.4889()"
		ldstr "class G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1> on type class Generated1419.G3_C1891`1<class Generated1419.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1419.G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>
		callvirt instance string class Generated1419.G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>::Method4()
		ldstr "G3_C1891::Method4.18566()"
		ldstr "class G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1> on type class Generated1419.G3_C1891`1<class Generated1419.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1419.G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>
		callvirt instance string class Generated1419.G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>::Method7<object>()
		ldstr "G1_C15::Method7.4885<System.Object>()"
		ldstr "class G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1> on type class Generated1419.G3_C1891`1<class Generated1419.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1419.IBase2`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1>::Method7<object>()
		ldstr "G1_C15::Method7.MI.4886<System.Object>()"
		ldstr "class IBase2`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1> on type class Generated1419.G3_C1891`1<class Generated1419.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1419.IBase1`1<class Generated1419.BaseClass0>::Method4()
		ldstr "G3_C1891::Method4.18566()"
		ldstr "class IBase1`1<class Generated1419.BaseClass0> on type class Generated1419.G3_C1891`1<class Generated1419.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1419.IBase1`1<class Generated1419.BaseClass0>::Method5()
		ldstr "G3_C1891::Method5.18567()"
		ldstr "class IBase1`1<class Generated1419.BaseClass0> on type class Generated1419.G3_C1891`1<class Generated1419.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1419.IBase1`1<class Generated1419.BaseClass0>::Method6<object>()
		ldstr "G3_C1891::Method6.MI.18569<System.Object>()"
		ldstr "class IBase1`1<class Generated1419.BaseClass0> on type class Generated1419.G3_C1891`1<class Generated1419.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1419.IBase2`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>::Method7<object>()
		ldstr "G2_C818::Method7.12541<System.Object>()"
		ldstr "class IBase2`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1> on type class Generated1419.G3_C1891`1<class Generated1419.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>
		callvirt instance string class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>::ClassMethod3018<object>()
		ldstr "G2_C818::ClassMethod3018.12543<System.Object>()"
		ldstr "class G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1> on type class Generated1419.G3_C1891`1<class Generated1419.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>
		callvirt instance string class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>::ClassMethod3017()
		ldstr "G2_C818::ClassMethod3017.12542()"
		ldstr "class G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1> on type class Generated1419.G3_C1891`1<class Generated1419.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>
		callvirt instance string class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>::Method7<object>()
		ldstr "G2_C818::Method7.12541<System.Object>()"
		ldstr "class G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1> on type class Generated1419.G3_C1891`1<class Generated1419.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>
		callvirt instance string class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>::Method6<object>()
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1> on type class Generated1419.G3_C1891`1<class Generated1419.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>
		callvirt instance string class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>::Method5()
		ldstr "G1_C15::Method5.4889()"
		ldstr "class G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1> on type class Generated1419.G3_C1891`1<class Generated1419.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>
		callvirt instance string class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>::Method4()
		ldstr "G3_C1891::Method4.18566()"
		ldstr "class G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1> on type class Generated1419.G3_C1891`1<class Generated1419.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1419.G3_C1891`1<class Generated1419.BaseClass0>
		callvirt instance string class Generated1419.G3_C1891`1<class Generated1419.BaseClass0>::ClassMethod5178<object>()
		ldstr "G3_C1891::ClassMethod5178.18571<System.Object>()"
		ldstr "class G3_C1891`1<class Generated1419.BaseClass0> on type class Generated1419.G3_C1891`1<class Generated1419.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1419.G3_C1891`1<class Generated1419.BaseClass0>
		callvirt instance string class Generated1419.G3_C1891`1<class Generated1419.BaseClass0>::ClassMethod5177()
		ldstr "G3_C1891::ClassMethod5177.18570()"
		ldstr "class G3_C1891`1<class Generated1419.BaseClass0> on type class Generated1419.G3_C1891`1<class Generated1419.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1419.G3_C1891`1<class Generated1419.BaseClass0>
		callvirt instance string class Generated1419.G3_C1891`1<class Generated1419.BaseClass0>::Method6<object>()
		ldstr "G3_C1891::Method6.18568<System.Object>()"
		ldstr "class G3_C1891`1<class Generated1419.BaseClass0> on type class Generated1419.G3_C1891`1<class Generated1419.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1419.G3_C1891`1<class Generated1419.BaseClass0>
		callvirt instance string class Generated1419.G3_C1891`1<class Generated1419.BaseClass0>::Method5()
		ldstr "G3_C1891::Method5.18567()"
		ldstr "class G3_C1891`1<class Generated1419.BaseClass0> on type class Generated1419.G3_C1891`1<class Generated1419.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1419.G3_C1891`1<class Generated1419.BaseClass0>
		callvirt instance string class Generated1419.G3_C1891`1<class Generated1419.BaseClass0>::Method4()
		ldstr "G3_C1891::Method4.18566()"
		ldstr "class G3_C1891`1<class Generated1419.BaseClass0> on type class Generated1419.G3_C1891`1<class Generated1419.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1419.G3_C1891`1<class Generated1419.BaseClass0>
		callvirt instance string class Generated1419.G3_C1891`1<class Generated1419.BaseClass0>::ClassMethod3018<object>()
		ldstr "G2_C818::ClassMethod3018.12543<System.Object>()"
		ldstr "class G3_C1891`1<class Generated1419.BaseClass0> on type class Generated1419.G3_C1891`1<class Generated1419.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1419.G3_C1891`1<class Generated1419.BaseClass0>
		callvirt instance string class Generated1419.G3_C1891`1<class Generated1419.BaseClass0>::ClassMethod3017()
		ldstr "G2_C818::ClassMethod3017.12542()"
		ldstr "class G3_C1891`1<class Generated1419.BaseClass0> on type class Generated1419.G3_C1891`1<class Generated1419.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1419.G3_C1891`1<class Generated1419.BaseClass0>
		callvirt instance string class Generated1419.G3_C1891`1<class Generated1419.BaseClass0>::Method7<object>()
		ldstr "G2_C818::Method7.12541<System.Object>()"
		ldstr "class G3_C1891`1<class Generated1419.BaseClass0> on type class Generated1419.G3_C1891`1<class Generated1419.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated1419.G3_C1891`1<class Generated1419.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1419.G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>
		callvirt instance string class Generated1419.G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>::Method6<object>()
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1> on type class Generated1419.G3_C1891`1<class Generated1419.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1419.G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>
		callvirt instance string class Generated1419.G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>::Method5()
		ldstr "G1_C15::Method5.4889()"
		ldstr "class G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1> on type class Generated1419.G3_C1891`1<class Generated1419.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1419.G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>
		callvirt instance string class Generated1419.G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>::Method4()
		ldstr "G3_C1891::Method4.18566()"
		ldstr "class G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1> on type class Generated1419.G3_C1891`1<class Generated1419.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1419.G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>
		callvirt instance string class Generated1419.G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>::Method7<object>()
		ldstr "G1_C15::Method7.4885<System.Object>()"
		ldstr "class G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1> on type class Generated1419.G3_C1891`1<class Generated1419.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1419.IBase2`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1>::Method7<object>()
		ldstr "G1_C15::Method7.MI.4886<System.Object>()"
		ldstr "class IBase2`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1> on type class Generated1419.G3_C1891`1<class Generated1419.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1419.IBase1`1<class Generated1419.BaseClass0>::Method4()
		ldstr "G3_C1891::Method4.18566()"
		ldstr "class IBase1`1<class Generated1419.BaseClass0> on type class Generated1419.G3_C1891`1<class Generated1419.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1419.IBase1`1<class Generated1419.BaseClass0>::Method5()
		ldstr "G3_C1891::Method5.18567()"
		ldstr "class IBase1`1<class Generated1419.BaseClass0> on type class Generated1419.G3_C1891`1<class Generated1419.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1419.IBase1`1<class Generated1419.BaseClass0>::Method6<object>()
		ldstr "G3_C1891::Method6.MI.18569<System.Object>()"
		ldstr "class IBase1`1<class Generated1419.BaseClass0> on type class Generated1419.G3_C1891`1<class Generated1419.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1419.IBase2`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>::Method7<object>()
		ldstr "G2_C818::Method7.12541<System.Object>()"
		ldstr "class IBase2`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1> on type class Generated1419.G3_C1891`1<class Generated1419.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>
		callvirt instance string class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>::ClassMethod3018<object>()
		ldstr "G2_C818::ClassMethod3018.12543<System.Object>()"
		ldstr "class G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1> on type class Generated1419.G3_C1891`1<class Generated1419.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>
		callvirt instance string class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>::ClassMethod3017()
		ldstr "G2_C818::ClassMethod3017.12542()"
		ldstr "class G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1> on type class Generated1419.G3_C1891`1<class Generated1419.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>
		callvirt instance string class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>::Method7<object>()
		ldstr "G2_C818::Method7.12541<System.Object>()"
		ldstr "class G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1> on type class Generated1419.G3_C1891`1<class Generated1419.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>
		callvirt instance string class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>::Method6<object>()
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1> on type class Generated1419.G3_C1891`1<class Generated1419.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>
		callvirt instance string class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>::Method5()
		ldstr "G1_C15::Method5.4889()"
		ldstr "class G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1> on type class Generated1419.G3_C1891`1<class Generated1419.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>
		callvirt instance string class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>::Method4()
		ldstr "G3_C1891::Method4.18566()"
		ldstr "class G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1> on type class Generated1419.G3_C1891`1<class Generated1419.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1419.G3_C1891`1<class Generated1419.BaseClass1>
		callvirt instance string class Generated1419.G3_C1891`1<class Generated1419.BaseClass1>::ClassMethod5178<object>()
		ldstr "G3_C1891::ClassMethod5178.18571<System.Object>()"
		ldstr "class G3_C1891`1<class Generated1419.BaseClass1> on type class Generated1419.G3_C1891`1<class Generated1419.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1419.G3_C1891`1<class Generated1419.BaseClass1>
		callvirt instance string class Generated1419.G3_C1891`1<class Generated1419.BaseClass1>::ClassMethod5177()
		ldstr "G3_C1891::ClassMethod5177.18570()"
		ldstr "class G3_C1891`1<class Generated1419.BaseClass1> on type class Generated1419.G3_C1891`1<class Generated1419.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1419.G3_C1891`1<class Generated1419.BaseClass1>
		callvirt instance string class Generated1419.G3_C1891`1<class Generated1419.BaseClass1>::Method6<object>()
		ldstr "G3_C1891::Method6.18568<System.Object>()"
		ldstr "class G3_C1891`1<class Generated1419.BaseClass1> on type class Generated1419.G3_C1891`1<class Generated1419.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1419.G3_C1891`1<class Generated1419.BaseClass1>
		callvirt instance string class Generated1419.G3_C1891`1<class Generated1419.BaseClass1>::Method5()
		ldstr "G3_C1891::Method5.18567()"
		ldstr "class G3_C1891`1<class Generated1419.BaseClass1> on type class Generated1419.G3_C1891`1<class Generated1419.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1419.G3_C1891`1<class Generated1419.BaseClass1>
		callvirt instance string class Generated1419.G3_C1891`1<class Generated1419.BaseClass1>::Method4()
		ldstr "G3_C1891::Method4.18566()"
		ldstr "class G3_C1891`1<class Generated1419.BaseClass1> on type class Generated1419.G3_C1891`1<class Generated1419.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1419.G3_C1891`1<class Generated1419.BaseClass1>
		callvirt instance string class Generated1419.G3_C1891`1<class Generated1419.BaseClass1>::ClassMethod3018<object>()
		ldstr "G2_C818::ClassMethod3018.12543<System.Object>()"
		ldstr "class G3_C1891`1<class Generated1419.BaseClass1> on type class Generated1419.G3_C1891`1<class Generated1419.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1419.G3_C1891`1<class Generated1419.BaseClass1>
		callvirt instance string class Generated1419.G3_C1891`1<class Generated1419.BaseClass1>::ClassMethod3017()
		ldstr "G2_C818::ClassMethod3017.12542()"
		ldstr "class G3_C1891`1<class Generated1419.BaseClass1> on type class Generated1419.G3_C1891`1<class Generated1419.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1419.G3_C1891`1<class Generated1419.BaseClass1>
		callvirt instance string class Generated1419.G3_C1891`1<class Generated1419.BaseClass1>::Method7<object>()
		ldstr "G2_C818::Method7.12541<System.Object>()"
		ldstr "class G3_C1891`1<class Generated1419.BaseClass1> on type class Generated1419.G3_C1891`1<class Generated1419.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1419.G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>
		callvirt instance string class Generated1419.G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>::Method6<object>()
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0> on type class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1419.G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>
		callvirt instance string class Generated1419.G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>::Method5()
		ldstr "G1_C15::Method5.4889()"
		ldstr "class G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0> on type class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1419.G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>
		callvirt instance string class Generated1419.G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>::Method4()
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0> on type class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1419.G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>
		callvirt instance string class Generated1419.G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>::Method7<object>()
		ldstr "G1_C15::Method7.4885<System.Object>()"
		ldstr "class G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0> on type class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1419.IBase2`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>::Method7<object>()
		ldstr "G2_C818::Method7.12541<System.Object>()"
		ldstr "class IBase2`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0> on type class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1419.IBase1`1<class Generated1419.BaseClass0>::Method4()
		ldstr "G1_C15::Method4.MI.4888()"
		ldstr "class IBase1`1<class Generated1419.BaseClass0> on type class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1419.IBase1`1<class Generated1419.BaseClass0>::Method5()
		ldstr "G1_C15::Method5.MI.4890()"
		ldstr "class IBase1`1<class Generated1419.BaseClass0> on type class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1419.IBase1`1<class Generated1419.BaseClass0>::Method6<object>()
		ldstr "G1_C15::Method6.MI.4892<System.Object>()"
		ldstr "class IBase1`1<class Generated1419.BaseClass0> on type class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1419.IBase2`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>::Method7<object>()
		ldstr "G2_C818::Method7.12541<System.Object>()"
		ldstr "class IBase2`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1> on type class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>
		callvirt instance string class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>::ClassMethod3018<object>()
		ldstr "G2_C818::ClassMethod3018.12543<System.Object>()"
		ldstr "class G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0> on type class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>
		callvirt instance string class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>::ClassMethod3017()
		ldstr "G2_C818::ClassMethod3017.12542()"
		ldstr "class G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0> on type class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>
		callvirt instance string class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>::Method7<object>()
		ldstr "G2_C818::Method7.12541<System.Object>()"
		ldstr "class G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0> on type class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>
		callvirt instance string class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>::Method6<object>()
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0> on type class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>
		callvirt instance string class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>::Method5()
		ldstr "G1_C15::Method5.4889()"
		ldstr "class G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0> on type class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>
		callvirt instance string class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>::Method4()
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0> on type class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1419.G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>
		callvirt instance string class Generated1419.G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>::Method6<object>()
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1> on type class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1419.G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>
		callvirt instance string class Generated1419.G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>::Method5()
		ldstr "G1_C15::Method5.4889()"
		ldstr "class G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1> on type class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1419.G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>
		callvirt instance string class Generated1419.G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>::Method4()
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1> on type class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1419.G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>
		callvirt instance string class Generated1419.G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>::Method7<object>()
		ldstr "G1_C15::Method7.4885<System.Object>()"
		ldstr "class G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1> on type class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1419.IBase2`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1>::Method7<object>()
		ldstr "G1_C15::Method7.MI.4886<System.Object>()"
		ldstr "class IBase2`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1> on type class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1419.IBase1`1<class Generated1419.BaseClass0>::Method4()
		ldstr "G1_C15::Method4.MI.4888()"
		ldstr "class IBase1`1<class Generated1419.BaseClass0> on type class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1419.IBase1`1<class Generated1419.BaseClass0>::Method5()
		ldstr "G1_C15::Method5.MI.4890()"
		ldstr "class IBase1`1<class Generated1419.BaseClass0> on type class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1419.IBase1`1<class Generated1419.BaseClass0>::Method6<object>()
		ldstr "G1_C15::Method6.MI.4892<System.Object>()"
		ldstr "class IBase1`1<class Generated1419.BaseClass0> on type class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1419.IBase2`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>::Method7<object>()
		ldstr "G2_C818::Method7.12541<System.Object>()"
		ldstr "class IBase2`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1> on type class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>
		callvirt instance string class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>::ClassMethod3018<object>()
		ldstr "G2_C818::ClassMethod3018.12543<System.Object>()"
		ldstr "class G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1> on type class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>
		callvirt instance string class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>::ClassMethod3017()
		ldstr "G2_C818::ClassMethod3017.12542()"
		ldstr "class G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1> on type class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>
		callvirt instance string class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>::Method7<object>()
		ldstr "G2_C818::Method7.12541<System.Object>()"
		ldstr "class G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1> on type class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>
		callvirt instance string class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>::Method6<object>()
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1> on type class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>
		callvirt instance string class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>::Method5()
		ldstr "G1_C15::Method5.4889()"
		ldstr "class G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1> on type class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>
		callvirt instance string class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>::Method4()
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1> on type class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1419.G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>
		callvirt instance string class Generated1419.G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>::Method6<object>()
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0> on type class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1419.G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>
		callvirt instance string class Generated1419.G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>::Method5()
		ldstr "G1_C15::Method5.4889()"
		ldstr "class G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0> on type class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1419.G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>
		callvirt instance string class Generated1419.G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>::Method4()
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0> on type class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1419.G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>
		callvirt instance string class Generated1419.G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>::Method7<object>()
		ldstr "G1_C15::Method7.4885<System.Object>()"
		ldstr "class G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0> on type class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1419.IBase2`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>::Method7<object>()
		ldstr "G2_C818::Method7.12541<System.Object>()"
		ldstr "class IBase2`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0> on type class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1419.IBase1`1<class Generated1419.BaseClass0>::Method4()
		ldstr "G1_C15::Method4.MI.4888()"
		ldstr "class IBase1`1<class Generated1419.BaseClass0> on type class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1419.IBase1`1<class Generated1419.BaseClass0>::Method5()
		ldstr "G1_C15::Method5.MI.4890()"
		ldstr "class IBase1`1<class Generated1419.BaseClass0> on type class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1419.IBase1`1<class Generated1419.BaseClass0>::Method6<object>()
		ldstr "G1_C15::Method6.MI.4892<System.Object>()"
		ldstr "class IBase1`1<class Generated1419.BaseClass0> on type class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1419.IBase2`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>::Method7<object>()
		ldstr "G2_C818::Method7.12541<System.Object>()"
		ldstr "class IBase2`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1> on type class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass0>
		callvirt instance string class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass0>::ClassMethod3018<object>()
		ldstr "G2_C818::ClassMethod3018.12543<System.Object>()"
		ldstr "class G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass0> on type class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass0>
		callvirt instance string class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass0>::ClassMethod3017()
		ldstr "G2_C818::ClassMethod3017.12542()"
		ldstr "class G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass0> on type class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass0>
		callvirt instance string class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass0>::Method7<object>()
		ldstr "G2_C818::Method7.12541<System.Object>()"
		ldstr "class G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass0> on type class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass0>
		callvirt instance string class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass0>::Method6<object>()
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass0> on type class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass0>
		callvirt instance string class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass0>::Method5()
		ldstr "G1_C15::Method5.4889()"
		ldstr "class G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass0> on type class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass0>
		callvirt instance string class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass0>::Method4()
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass0> on type class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1419.IBase2`2<class Generated1419.BaseClass1,class Generated1419.BaseClass0>::Method7<object>()
		ldstr "G2_C818::Method7.12541<System.Object>()"
		ldstr "class IBase2`2<class Generated1419.BaseClass1,class Generated1419.BaseClass0> on type class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1419.IBase2`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1>::Method7<object>()
		ldstr "G2_C818::Method7.12541<System.Object>()"
		ldstr "class IBase2`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1> on type class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1419.G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>
		callvirt instance string class Generated1419.G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>::Method6<object>()
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1> on type class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1419.G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>
		callvirt instance string class Generated1419.G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>::Method5()
		ldstr "G1_C15::Method5.4889()"
		ldstr "class G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1> on type class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1419.G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>
		callvirt instance string class Generated1419.G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>::Method4()
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1> on type class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1419.G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>
		callvirt instance string class Generated1419.G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>::Method7<object>()
		ldstr "G1_C15::Method7.4885<System.Object>()"
		ldstr "class G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1> on type class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1419.IBase2`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1>::Method7<object>()
		ldstr "G2_C818::Method7.12541<System.Object>()"
		ldstr "class IBase2`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1> on type class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1419.IBase1`1<class Generated1419.BaseClass0>::Method4()
		ldstr "G1_C15::Method4.MI.4888()"
		ldstr "class IBase1`1<class Generated1419.BaseClass0> on type class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1419.IBase1`1<class Generated1419.BaseClass0>::Method5()
		ldstr "G1_C15::Method5.MI.4890()"
		ldstr "class IBase1`1<class Generated1419.BaseClass0> on type class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1419.IBase1`1<class Generated1419.BaseClass0>::Method6<object>()
		ldstr "G1_C15::Method6.MI.4892<System.Object>()"
		ldstr "class IBase1`1<class Generated1419.BaseClass0> on type class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1419.IBase2`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>::Method7<object>()
		ldstr "G2_C818::Method7.12541<System.Object>()"
		ldstr "class IBase2`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1> on type class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1>
		callvirt instance string class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1>::ClassMethod3018<object>()
		ldstr "G2_C818::ClassMethod3018.12543<System.Object>()"
		ldstr "class G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1> on type class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1>
		callvirt instance string class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1>::ClassMethod3017()
		ldstr "G2_C818::ClassMethod3017.12542()"
		ldstr "class G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1> on type class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1>
		callvirt instance string class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1>::Method7<object>()
		ldstr "G2_C818::Method7.12541<System.Object>()"
		ldstr "class G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1> on type class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1>
		callvirt instance string class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1>::Method6<object>()
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1> on type class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1>
		callvirt instance string class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1>::Method5()
		ldstr "G1_C15::Method5.4889()"
		ldstr "class G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1> on type class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1>
		callvirt instance string class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1>::Method4()
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1> on type class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1>"
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
		newobj instance void class Generated1419.G3_C1891`1<class Generated1419.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G3_C1891::Method4.18566()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G1_C15::Method7.4885<System.Object>()#"
		call void Generated1419.Generated1419::M.G1_C15.T.T<class Generated1419.BaseClass0,class Generated1419.BaseClass1,class Generated1419.G3_C1891`1<class Generated1419.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G3_C1891::Method4.18566()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G1_C15::Method7.4885<System.Object>()#"
		call void Generated1419.Generated1419::M.G1_C15.A.T<class Generated1419.BaseClass1,class Generated1419.G3_C1891`1<class Generated1419.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G3_C1891::Method4.18566()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G1_C15::Method7.4885<System.Object>()#"
		call void Generated1419.Generated1419::M.G1_C15.A.B<class Generated1419.G3_C1891`1<class Generated1419.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C15::Method7.MI.4886<System.Object>()#"
		call void Generated1419.Generated1419::M.IBase2.T.T<class Generated1419.BaseClass1,class Generated1419.BaseClass1,class Generated1419.G3_C1891`1<class Generated1419.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C15::Method7.MI.4886<System.Object>()#"
		call void Generated1419.Generated1419::M.IBase2.B.T<class Generated1419.BaseClass1,class Generated1419.G3_C1891`1<class Generated1419.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C15::Method7.MI.4886<System.Object>()#"
		call void Generated1419.Generated1419::M.IBase2.B.B<class Generated1419.G3_C1891`1<class Generated1419.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1891::Method4.18566()#G3_C1891::Method5.18567()#G3_C1891::Method6.MI.18569<System.Object>()#"
		call void Generated1419.Generated1419::M.IBase1.T<class Generated1419.BaseClass0,class Generated1419.G3_C1891`1<class Generated1419.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G3_C1891::Method4.18566()#G3_C1891::Method5.18567()#G3_C1891::Method6.MI.18569<System.Object>()#"
		call void Generated1419.Generated1419::M.IBase1.A<class Generated1419.G3_C1891`1<class Generated1419.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C818::Method7.12541<System.Object>()#"
		call void Generated1419.Generated1419::M.IBase2.T.T<class Generated1419.BaseClass0,class Generated1419.BaseClass1,class Generated1419.G3_C1891`1<class Generated1419.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C818::Method7.12541<System.Object>()#"
		call void Generated1419.Generated1419::M.IBase2.A.T<class Generated1419.BaseClass1,class Generated1419.G3_C1891`1<class Generated1419.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C818::Method7.12541<System.Object>()#"
		call void Generated1419.Generated1419::M.IBase2.A.B<class Generated1419.G3_C1891`1<class Generated1419.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C818::ClassMethod3017.12542()#G2_C818::ClassMethod3018.12543<System.Object>()#G3_C1891::Method4.18566()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C818::Method7.12541<System.Object>()#"
		call void Generated1419.Generated1419::M.G2_C818.T.T<class Generated1419.BaseClass0,class Generated1419.BaseClass1,class Generated1419.G3_C1891`1<class Generated1419.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C818::ClassMethod3017.12542()#G2_C818::ClassMethod3018.12543<System.Object>()#G3_C1891::Method4.18566()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C818::Method7.12541<System.Object>()#"
		call void Generated1419.Generated1419::M.G2_C818.A.T<class Generated1419.BaseClass1,class Generated1419.G3_C1891`1<class Generated1419.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C818::ClassMethod3017.12542()#G2_C818::ClassMethod3018.12543<System.Object>()#G3_C1891::Method4.18566()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C818::Method7.12541<System.Object>()#"
		call void Generated1419.Generated1419::M.G2_C818.A.B<class Generated1419.G3_C1891`1<class Generated1419.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C818::ClassMethod3017.12542()#G2_C818::ClassMethod3018.12543<System.Object>()#G3_C1891::ClassMethod5177.18570()#G3_C1891::ClassMethod5178.18571<System.Object>()#G3_C1891::Method4.18566()#G3_C1891::Method5.18567()#G3_C1891::Method6.18568<System.Object>()#G2_C818::Method7.12541<System.Object>()#"
		call void Generated1419.Generated1419::M.G3_C1891.T<class Generated1419.BaseClass0,class Generated1419.G3_C1891`1<class Generated1419.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C818::ClassMethod3017.12542()#G2_C818::ClassMethod3018.12543<System.Object>()#G3_C1891::ClassMethod5177.18570()#G3_C1891::ClassMethod5178.18571<System.Object>()#G3_C1891::Method4.18566()#G3_C1891::Method5.18567()#G3_C1891::Method6.18568<System.Object>()#G2_C818::Method7.12541<System.Object>()#"
		call void Generated1419.Generated1419::M.G3_C1891.A<class Generated1419.G3_C1891`1<class Generated1419.BaseClass0>>(!!0,string)
		newobj instance void class Generated1419.G3_C1891`1<class Generated1419.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G3_C1891::Method4.18566()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G1_C15::Method7.4885<System.Object>()#"
		call void Generated1419.Generated1419::M.G1_C15.T.T<class Generated1419.BaseClass0,class Generated1419.BaseClass1,class Generated1419.G3_C1891`1<class Generated1419.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G3_C1891::Method4.18566()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G1_C15::Method7.4885<System.Object>()#"
		call void Generated1419.Generated1419::M.G1_C15.A.T<class Generated1419.BaseClass1,class Generated1419.G3_C1891`1<class Generated1419.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1891::Method4.18566()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G1_C15::Method7.4885<System.Object>()#"
		call void Generated1419.Generated1419::M.G1_C15.A.B<class Generated1419.G3_C1891`1<class Generated1419.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C15::Method7.MI.4886<System.Object>()#"
		call void Generated1419.Generated1419::M.IBase2.T.T<class Generated1419.BaseClass1,class Generated1419.BaseClass1,class Generated1419.G3_C1891`1<class Generated1419.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C15::Method7.MI.4886<System.Object>()#"
		call void Generated1419.Generated1419::M.IBase2.B.T<class Generated1419.BaseClass1,class Generated1419.G3_C1891`1<class Generated1419.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C15::Method7.MI.4886<System.Object>()#"
		call void Generated1419.Generated1419::M.IBase2.B.B<class Generated1419.G3_C1891`1<class Generated1419.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1891::Method4.18566()#G3_C1891::Method5.18567()#G3_C1891::Method6.MI.18569<System.Object>()#"
		call void Generated1419.Generated1419::M.IBase1.T<class Generated1419.BaseClass0,class Generated1419.G3_C1891`1<class Generated1419.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1891::Method4.18566()#G3_C1891::Method5.18567()#G3_C1891::Method6.MI.18569<System.Object>()#"
		call void Generated1419.Generated1419::M.IBase1.A<class Generated1419.G3_C1891`1<class Generated1419.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C818::Method7.12541<System.Object>()#"
		call void Generated1419.Generated1419::M.IBase2.T.T<class Generated1419.BaseClass0,class Generated1419.BaseClass1,class Generated1419.G3_C1891`1<class Generated1419.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C818::Method7.12541<System.Object>()#"
		call void Generated1419.Generated1419::M.IBase2.A.T<class Generated1419.BaseClass1,class Generated1419.G3_C1891`1<class Generated1419.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C818::Method7.12541<System.Object>()#"
		call void Generated1419.Generated1419::M.IBase2.A.B<class Generated1419.G3_C1891`1<class Generated1419.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C818::ClassMethod3017.12542()#G2_C818::ClassMethod3018.12543<System.Object>()#G3_C1891::Method4.18566()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C818::Method7.12541<System.Object>()#"
		call void Generated1419.Generated1419::M.G2_C818.T.T<class Generated1419.BaseClass0,class Generated1419.BaseClass1,class Generated1419.G3_C1891`1<class Generated1419.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C818::ClassMethod3017.12542()#G2_C818::ClassMethod3018.12543<System.Object>()#G3_C1891::Method4.18566()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C818::Method7.12541<System.Object>()#"
		call void Generated1419.Generated1419::M.G2_C818.A.T<class Generated1419.BaseClass1,class Generated1419.G3_C1891`1<class Generated1419.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C818::ClassMethod3017.12542()#G2_C818::ClassMethod3018.12543<System.Object>()#G3_C1891::Method4.18566()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C818::Method7.12541<System.Object>()#"
		call void Generated1419.Generated1419::M.G2_C818.A.B<class Generated1419.G3_C1891`1<class Generated1419.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C818::ClassMethod3017.12542()#G2_C818::ClassMethod3018.12543<System.Object>()#G3_C1891::ClassMethod5177.18570()#G3_C1891::ClassMethod5178.18571<System.Object>()#G3_C1891::Method4.18566()#G3_C1891::Method5.18567()#G3_C1891::Method6.18568<System.Object>()#G2_C818::Method7.12541<System.Object>()#"
		call void Generated1419.Generated1419::M.G3_C1891.T<class Generated1419.BaseClass1,class Generated1419.G3_C1891`1<class Generated1419.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C818::ClassMethod3017.12542()#G2_C818::ClassMethod3018.12543<System.Object>()#G3_C1891::ClassMethod5177.18570()#G3_C1891::ClassMethod5178.18571<System.Object>()#G3_C1891::Method4.18566()#G3_C1891::Method5.18567()#G3_C1891::Method6.18568<System.Object>()#G2_C818::Method7.12541<System.Object>()#"
		call void Generated1419.Generated1419::M.G3_C1891.B<class Generated1419.G3_C1891`1<class Generated1419.BaseClass1>>(!!0,string)
		newobj instance void class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G1_C15::Method7.4885<System.Object>()#"
		call void Generated1419.Generated1419::M.G1_C15.T.T<class Generated1419.BaseClass0,class Generated1419.BaseClass0,class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G1_C15::Method7.4885<System.Object>()#"
		call void Generated1419.Generated1419::M.G1_C15.A.T<class Generated1419.BaseClass0,class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G1_C15::Method7.4885<System.Object>()#"
		call void Generated1419.Generated1419::M.G1_C15.A.A<class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C818::Method7.12541<System.Object>()#"
		call void Generated1419.Generated1419::M.IBase2.T.T<class Generated1419.BaseClass0,class Generated1419.BaseClass0,class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C818::Method7.12541<System.Object>()#"
		call void Generated1419.Generated1419::M.IBase2.A.T<class Generated1419.BaseClass0,class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C818::Method7.12541<System.Object>()#"
		call void Generated1419.Generated1419::M.IBase2.A.A<class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C15::Method4.MI.4888()#G1_C15::Method5.MI.4890()#G1_C15::Method6.MI.4892<System.Object>()#"
		call void Generated1419.Generated1419::M.IBase1.T<class Generated1419.BaseClass0,class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C15::Method4.MI.4888()#G1_C15::Method5.MI.4890()#G1_C15::Method6.MI.4892<System.Object>()#"
		call void Generated1419.Generated1419::M.IBase1.A<class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C818::Method7.12541<System.Object>()#"
		call void Generated1419.Generated1419::M.IBase2.T.T<class Generated1419.BaseClass0,class Generated1419.BaseClass1,class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C818::Method7.12541<System.Object>()#"
		call void Generated1419.Generated1419::M.IBase2.A.T<class Generated1419.BaseClass1,class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C818::Method7.12541<System.Object>()#"
		call void Generated1419.Generated1419::M.IBase2.A.B<class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C818::ClassMethod3017.12542()#G2_C818::ClassMethod3018.12543<System.Object>()#G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C818::Method7.12541<System.Object>()#"
		call void Generated1419.Generated1419::M.G2_C818.T.T<class Generated1419.BaseClass0,class Generated1419.BaseClass0,class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C818::ClassMethod3017.12542()#G2_C818::ClassMethod3018.12543<System.Object>()#G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C818::Method7.12541<System.Object>()#"
		call void Generated1419.Generated1419::M.G2_C818.A.T<class Generated1419.BaseClass0,class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C818::ClassMethod3017.12542()#G2_C818::ClassMethod3018.12543<System.Object>()#G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C818::Method7.12541<System.Object>()#"
		call void Generated1419.Generated1419::M.G2_C818.A.A<class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>>(!!0,string)
		newobj instance void class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G1_C15::Method7.4885<System.Object>()#"
		call void Generated1419.Generated1419::M.G1_C15.T.T<class Generated1419.BaseClass0,class Generated1419.BaseClass1,class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G1_C15::Method7.4885<System.Object>()#"
		call void Generated1419.Generated1419::M.G1_C15.A.T<class Generated1419.BaseClass1,class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G1_C15::Method7.4885<System.Object>()#"
		call void Generated1419.Generated1419::M.G1_C15.A.B<class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C15::Method7.MI.4886<System.Object>()#"
		call void Generated1419.Generated1419::M.IBase2.T.T<class Generated1419.BaseClass1,class Generated1419.BaseClass1,class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C15::Method7.MI.4886<System.Object>()#"
		call void Generated1419.Generated1419::M.IBase2.B.T<class Generated1419.BaseClass1,class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C15::Method7.MI.4886<System.Object>()#"
		call void Generated1419.Generated1419::M.IBase2.B.B<class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C15::Method4.MI.4888()#G1_C15::Method5.MI.4890()#G1_C15::Method6.MI.4892<System.Object>()#"
		call void Generated1419.Generated1419::M.IBase1.T<class Generated1419.BaseClass0,class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C15::Method4.MI.4888()#G1_C15::Method5.MI.4890()#G1_C15::Method6.MI.4892<System.Object>()#"
		call void Generated1419.Generated1419::M.IBase1.A<class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C818::Method7.12541<System.Object>()#"
		call void Generated1419.Generated1419::M.IBase2.T.T<class Generated1419.BaseClass0,class Generated1419.BaseClass1,class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C818::Method7.12541<System.Object>()#"
		call void Generated1419.Generated1419::M.IBase2.A.T<class Generated1419.BaseClass1,class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C818::Method7.12541<System.Object>()#"
		call void Generated1419.Generated1419::M.IBase2.A.B<class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C818::ClassMethod3017.12542()#G2_C818::ClassMethod3018.12543<System.Object>()#G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C818::Method7.12541<System.Object>()#"
		call void Generated1419.Generated1419::M.G2_C818.T.T<class Generated1419.BaseClass0,class Generated1419.BaseClass1,class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C818::ClassMethod3017.12542()#G2_C818::ClassMethod3018.12543<System.Object>()#G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C818::Method7.12541<System.Object>()#"
		call void Generated1419.Generated1419::M.G2_C818.A.T<class Generated1419.BaseClass1,class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C818::ClassMethod3017.12542()#G2_C818::ClassMethod3018.12543<System.Object>()#G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C818::Method7.12541<System.Object>()#"
		call void Generated1419.Generated1419::M.G2_C818.A.B<class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>>(!!0,string)
		newobj instance void class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G1_C15::Method7.4885<System.Object>()#"
		call void Generated1419.Generated1419::M.G1_C15.T.T<class Generated1419.BaseClass0,class Generated1419.BaseClass0,class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G1_C15::Method7.4885<System.Object>()#"
		call void Generated1419.Generated1419::M.G1_C15.A.T<class Generated1419.BaseClass0,class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G1_C15::Method7.4885<System.Object>()#"
		call void Generated1419.Generated1419::M.G1_C15.A.A<class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C818::Method7.12541<System.Object>()#"
		call void Generated1419.Generated1419::M.IBase2.T.T<class Generated1419.BaseClass0,class Generated1419.BaseClass0,class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C818::Method7.12541<System.Object>()#"
		call void Generated1419.Generated1419::M.IBase2.A.T<class Generated1419.BaseClass0,class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C818::Method7.12541<System.Object>()#"
		call void Generated1419.Generated1419::M.IBase2.A.A<class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C15::Method4.MI.4888()#G1_C15::Method5.MI.4890()#G1_C15::Method6.MI.4892<System.Object>()#"
		call void Generated1419.Generated1419::M.IBase1.T<class Generated1419.BaseClass0,class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C15::Method4.MI.4888()#G1_C15::Method5.MI.4890()#G1_C15::Method6.MI.4892<System.Object>()#"
		call void Generated1419.Generated1419::M.IBase1.A<class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C818::Method7.12541<System.Object>()#"
		call void Generated1419.Generated1419::M.IBase2.T.T<class Generated1419.BaseClass0,class Generated1419.BaseClass1,class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C818::Method7.12541<System.Object>()#"
		call void Generated1419.Generated1419::M.IBase2.A.T<class Generated1419.BaseClass1,class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C818::Method7.12541<System.Object>()#"
		call void Generated1419.Generated1419::M.IBase2.A.B<class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C818::ClassMethod3017.12542()#G2_C818::ClassMethod3018.12543<System.Object>()#G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C818::Method7.12541<System.Object>()#"
		call void Generated1419.Generated1419::M.G2_C818.T.T<class Generated1419.BaseClass1,class Generated1419.BaseClass0,class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C818::ClassMethod3017.12542()#G2_C818::ClassMethod3018.12543<System.Object>()#G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C818::Method7.12541<System.Object>()#"
		call void Generated1419.Generated1419::M.G2_C818.B.T<class Generated1419.BaseClass0,class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C818::ClassMethod3017.12542()#G2_C818::ClassMethod3018.12543<System.Object>()#G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C818::Method7.12541<System.Object>()#"
		call void Generated1419.Generated1419::M.G2_C818.B.A<class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C818::Method7.12541<System.Object>()#"
		call void Generated1419.Generated1419::M.IBase2.T.T<class Generated1419.BaseClass1,class Generated1419.BaseClass0,class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C818::Method7.12541<System.Object>()#"
		call void Generated1419.Generated1419::M.IBase2.B.T<class Generated1419.BaseClass0,class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C818::Method7.12541<System.Object>()#"
		call void Generated1419.Generated1419::M.IBase2.B.A<class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C818::Method7.12541<System.Object>()#"
		call void Generated1419.Generated1419::M.IBase2.T.T<class Generated1419.BaseClass1,class Generated1419.BaseClass1,class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C818::Method7.12541<System.Object>()#"
		call void Generated1419.Generated1419::M.IBase2.B.T<class Generated1419.BaseClass1,class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C818::Method7.12541<System.Object>()#"
		call void Generated1419.Generated1419::M.IBase2.B.B<class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass0>>(!!0,string)
		newobj instance void class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G1_C15::Method7.4885<System.Object>()#"
		call void Generated1419.Generated1419::M.G1_C15.T.T<class Generated1419.BaseClass0,class Generated1419.BaseClass1,class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G1_C15::Method7.4885<System.Object>()#"
		call void Generated1419.Generated1419::M.G1_C15.A.T<class Generated1419.BaseClass1,class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G1_C15::Method7.4885<System.Object>()#"
		call void Generated1419.Generated1419::M.G1_C15.A.B<class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C818::Method7.12541<System.Object>()#"
		call void Generated1419.Generated1419::M.IBase2.T.T<class Generated1419.BaseClass1,class Generated1419.BaseClass1,class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C818::Method7.12541<System.Object>()#"
		call void Generated1419.Generated1419::M.IBase2.B.T<class Generated1419.BaseClass1,class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C818::Method7.12541<System.Object>()#"
		call void Generated1419.Generated1419::M.IBase2.B.B<class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C15::Method4.MI.4888()#G1_C15::Method5.MI.4890()#G1_C15::Method6.MI.4892<System.Object>()#"
		call void Generated1419.Generated1419::M.IBase1.T<class Generated1419.BaseClass0,class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C15::Method4.MI.4888()#G1_C15::Method5.MI.4890()#G1_C15::Method6.MI.4892<System.Object>()#"
		call void Generated1419.Generated1419::M.IBase1.A<class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C818::Method7.12541<System.Object>()#"
		call void Generated1419.Generated1419::M.IBase2.T.T<class Generated1419.BaseClass0,class Generated1419.BaseClass1,class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C818::Method7.12541<System.Object>()#"
		call void Generated1419.Generated1419::M.IBase2.A.T<class Generated1419.BaseClass1,class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C818::Method7.12541<System.Object>()#"
		call void Generated1419.Generated1419::M.IBase2.A.B<class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C818::ClassMethod3017.12542()#G2_C818::ClassMethod3018.12543<System.Object>()#G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C818::Method7.12541<System.Object>()#"
		call void Generated1419.Generated1419::M.G2_C818.T.T<class Generated1419.BaseClass1,class Generated1419.BaseClass1,class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C818::ClassMethod3017.12542()#G2_C818::ClassMethod3018.12543<System.Object>()#G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C818::Method7.12541<System.Object>()#"
		call void Generated1419.Generated1419::M.G2_C818.B.T<class Generated1419.BaseClass1,class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C818::ClassMethod3017.12542()#G2_C818::ClassMethod3018.12543<System.Object>()#G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C818::Method7.12541<System.Object>()#"
		call void Generated1419.Generated1419::M.G2_C818.B.B<class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1>>(!!0,string)
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
		newobj instance void class Generated1419.G3_C1891`1<class Generated1419.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1419.G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1419.G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>::Method6<object>()
		calli default string(class Generated1419.G3_C1891`1<class Generated1419.BaseClass0>)
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1> on type class Generated1419.G3_C1891`1<class Generated1419.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1419.G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1419.G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>::Method5()
		calli default string(class Generated1419.G3_C1891`1<class Generated1419.BaseClass0>)
		ldstr "G1_C15::Method5.4889()"
		ldstr "class G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1> on type class Generated1419.G3_C1891`1<class Generated1419.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1419.G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1419.G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>::Method4()
		calli default string(class Generated1419.G3_C1891`1<class Generated1419.BaseClass0>)
		ldstr "G3_C1891::Method4.18566()"
		ldstr "class G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1> on type class Generated1419.G3_C1891`1<class Generated1419.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1419.G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1419.G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>::Method7<object>()
		calli default string(class Generated1419.G3_C1891`1<class Generated1419.BaseClass0>)
		ldstr "G1_C15::Method7.4885<System.Object>()"
		ldstr "class G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1> on type class Generated1419.G3_C1891`1<class Generated1419.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1419.IBase2`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1>::Method7<object>()
		calli default string(class Generated1419.G3_C1891`1<class Generated1419.BaseClass0>)
		ldstr "G1_C15::Method7.MI.4886<System.Object>()"
		ldstr "class IBase2`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1> on type class Generated1419.G3_C1891`1<class Generated1419.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1419.IBase1`1<class Generated1419.BaseClass0>::Method4()
		calli default string(class Generated1419.G3_C1891`1<class Generated1419.BaseClass0>)
		ldstr "G3_C1891::Method4.18566()"
		ldstr "class IBase1`1<class Generated1419.BaseClass0> on type class Generated1419.G3_C1891`1<class Generated1419.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1419.IBase1`1<class Generated1419.BaseClass0>::Method5()
		calli default string(class Generated1419.G3_C1891`1<class Generated1419.BaseClass0>)
		ldstr "G3_C1891::Method5.18567()"
		ldstr "class IBase1`1<class Generated1419.BaseClass0> on type class Generated1419.G3_C1891`1<class Generated1419.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1419.IBase1`1<class Generated1419.BaseClass0>::Method6<object>()
		calli default string(class Generated1419.G3_C1891`1<class Generated1419.BaseClass0>)
		ldstr "G3_C1891::Method6.MI.18569<System.Object>()"
		ldstr "class IBase1`1<class Generated1419.BaseClass0> on type class Generated1419.G3_C1891`1<class Generated1419.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1419.IBase2`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>::Method7<object>()
		calli default string(class Generated1419.G3_C1891`1<class Generated1419.BaseClass0>)
		ldstr "G2_C818::Method7.12541<System.Object>()"
		ldstr "class IBase2`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1> on type class Generated1419.G3_C1891`1<class Generated1419.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>::ClassMethod3018<object>()
		calli default string(class Generated1419.G3_C1891`1<class Generated1419.BaseClass0>)
		ldstr "G2_C818::ClassMethod3018.12543<System.Object>()"
		ldstr "class G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1> on type class Generated1419.G3_C1891`1<class Generated1419.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>::ClassMethod3017()
		calli default string(class Generated1419.G3_C1891`1<class Generated1419.BaseClass0>)
		ldstr "G2_C818::ClassMethod3017.12542()"
		ldstr "class G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1> on type class Generated1419.G3_C1891`1<class Generated1419.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>::Method7<object>()
		calli default string(class Generated1419.G3_C1891`1<class Generated1419.BaseClass0>)
		ldstr "G2_C818::Method7.12541<System.Object>()"
		ldstr "class G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1> on type class Generated1419.G3_C1891`1<class Generated1419.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>::Method6<object>()
		calli default string(class Generated1419.G3_C1891`1<class Generated1419.BaseClass0>)
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1> on type class Generated1419.G3_C1891`1<class Generated1419.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>::Method5()
		calli default string(class Generated1419.G3_C1891`1<class Generated1419.BaseClass0>)
		ldstr "G1_C15::Method5.4889()"
		ldstr "class G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1> on type class Generated1419.G3_C1891`1<class Generated1419.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>::Method4()
		calli default string(class Generated1419.G3_C1891`1<class Generated1419.BaseClass0>)
		ldstr "G3_C1891::Method4.18566()"
		ldstr "class G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1> on type class Generated1419.G3_C1891`1<class Generated1419.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1419.G3_C1891`1<class Generated1419.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1419.G3_C1891`1<class Generated1419.BaseClass0>::ClassMethod5178<object>()
		calli default string(class Generated1419.G3_C1891`1<class Generated1419.BaseClass0>)
		ldstr "G3_C1891::ClassMethod5178.18571<System.Object>()"
		ldstr "class G3_C1891`1<class Generated1419.BaseClass0> on type class Generated1419.G3_C1891`1<class Generated1419.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1419.G3_C1891`1<class Generated1419.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1419.G3_C1891`1<class Generated1419.BaseClass0>::ClassMethod5177()
		calli default string(class Generated1419.G3_C1891`1<class Generated1419.BaseClass0>)
		ldstr "G3_C1891::ClassMethod5177.18570()"
		ldstr "class G3_C1891`1<class Generated1419.BaseClass0> on type class Generated1419.G3_C1891`1<class Generated1419.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1419.G3_C1891`1<class Generated1419.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1419.G3_C1891`1<class Generated1419.BaseClass0>::Method6<object>()
		calli default string(class Generated1419.G3_C1891`1<class Generated1419.BaseClass0>)
		ldstr "G3_C1891::Method6.18568<System.Object>()"
		ldstr "class G3_C1891`1<class Generated1419.BaseClass0> on type class Generated1419.G3_C1891`1<class Generated1419.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1419.G3_C1891`1<class Generated1419.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1419.G3_C1891`1<class Generated1419.BaseClass0>::Method5()
		calli default string(class Generated1419.G3_C1891`1<class Generated1419.BaseClass0>)
		ldstr "G3_C1891::Method5.18567()"
		ldstr "class G3_C1891`1<class Generated1419.BaseClass0> on type class Generated1419.G3_C1891`1<class Generated1419.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1419.G3_C1891`1<class Generated1419.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1419.G3_C1891`1<class Generated1419.BaseClass0>::Method4()
		calli default string(class Generated1419.G3_C1891`1<class Generated1419.BaseClass0>)
		ldstr "G3_C1891::Method4.18566()"
		ldstr "class G3_C1891`1<class Generated1419.BaseClass0> on type class Generated1419.G3_C1891`1<class Generated1419.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1419.G3_C1891`1<class Generated1419.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1419.G3_C1891`1<class Generated1419.BaseClass0>::ClassMethod3018<object>()
		calli default string(class Generated1419.G3_C1891`1<class Generated1419.BaseClass0>)
		ldstr "G2_C818::ClassMethod3018.12543<System.Object>()"
		ldstr "class G3_C1891`1<class Generated1419.BaseClass0> on type class Generated1419.G3_C1891`1<class Generated1419.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1419.G3_C1891`1<class Generated1419.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1419.G3_C1891`1<class Generated1419.BaseClass0>::ClassMethod3017()
		calli default string(class Generated1419.G3_C1891`1<class Generated1419.BaseClass0>)
		ldstr "G2_C818::ClassMethod3017.12542()"
		ldstr "class G3_C1891`1<class Generated1419.BaseClass0> on type class Generated1419.G3_C1891`1<class Generated1419.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1419.G3_C1891`1<class Generated1419.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1419.G3_C1891`1<class Generated1419.BaseClass0>::Method7<object>()
		calli default string(class Generated1419.G3_C1891`1<class Generated1419.BaseClass0>)
		ldstr "G2_C818::Method7.12541<System.Object>()"
		ldstr "class G3_C1891`1<class Generated1419.BaseClass0> on type class Generated1419.G3_C1891`1<class Generated1419.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated1419.G3_C1891`1<class Generated1419.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1419.G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1419.G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>::Method6<object>()
		calli default string(class Generated1419.G3_C1891`1<class Generated1419.BaseClass1>)
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1> on type class Generated1419.G3_C1891`1<class Generated1419.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1419.G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1419.G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>::Method5()
		calli default string(class Generated1419.G3_C1891`1<class Generated1419.BaseClass1>)
		ldstr "G1_C15::Method5.4889()"
		ldstr "class G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1> on type class Generated1419.G3_C1891`1<class Generated1419.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1419.G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1419.G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>::Method4()
		calli default string(class Generated1419.G3_C1891`1<class Generated1419.BaseClass1>)
		ldstr "G3_C1891::Method4.18566()"
		ldstr "class G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1> on type class Generated1419.G3_C1891`1<class Generated1419.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1419.G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1419.G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>::Method7<object>()
		calli default string(class Generated1419.G3_C1891`1<class Generated1419.BaseClass1>)
		ldstr "G1_C15::Method7.4885<System.Object>()"
		ldstr "class G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1> on type class Generated1419.G3_C1891`1<class Generated1419.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1419.IBase2`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1>::Method7<object>()
		calli default string(class Generated1419.G3_C1891`1<class Generated1419.BaseClass1>)
		ldstr "G1_C15::Method7.MI.4886<System.Object>()"
		ldstr "class IBase2`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1> on type class Generated1419.G3_C1891`1<class Generated1419.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1419.IBase1`1<class Generated1419.BaseClass0>::Method4()
		calli default string(class Generated1419.G3_C1891`1<class Generated1419.BaseClass1>)
		ldstr "G3_C1891::Method4.18566()"
		ldstr "class IBase1`1<class Generated1419.BaseClass0> on type class Generated1419.G3_C1891`1<class Generated1419.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1419.IBase1`1<class Generated1419.BaseClass0>::Method5()
		calli default string(class Generated1419.G3_C1891`1<class Generated1419.BaseClass1>)
		ldstr "G3_C1891::Method5.18567()"
		ldstr "class IBase1`1<class Generated1419.BaseClass0> on type class Generated1419.G3_C1891`1<class Generated1419.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1419.IBase1`1<class Generated1419.BaseClass0>::Method6<object>()
		calli default string(class Generated1419.G3_C1891`1<class Generated1419.BaseClass1>)
		ldstr "G3_C1891::Method6.MI.18569<System.Object>()"
		ldstr "class IBase1`1<class Generated1419.BaseClass0> on type class Generated1419.G3_C1891`1<class Generated1419.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1419.IBase2`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>::Method7<object>()
		calli default string(class Generated1419.G3_C1891`1<class Generated1419.BaseClass1>)
		ldstr "G2_C818::Method7.12541<System.Object>()"
		ldstr "class IBase2`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1> on type class Generated1419.G3_C1891`1<class Generated1419.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>::ClassMethod3018<object>()
		calli default string(class Generated1419.G3_C1891`1<class Generated1419.BaseClass1>)
		ldstr "G2_C818::ClassMethod3018.12543<System.Object>()"
		ldstr "class G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1> on type class Generated1419.G3_C1891`1<class Generated1419.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>::ClassMethod3017()
		calli default string(class Generated1419.G3_C1891`1<class Generated1419.BaseClass1>)
		ldstr "G2_C818::ClassMethod3017.12542()"
		ldstr "class G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1> on type class Generated1419.G3_C1891`1<class Generated1419.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>::Method7<object>()
		calli default string(class Generated1419.G3_C1891`1<class Generated1419.BaseClass1>)
		ldstr "G2_C818::Method7.12541<System.Object>()"
		ldstr "class G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1> on type class Generated1419.G3_C1891`1<class Generated1419.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>::Method6<object>()
		calli default string(class Generated1419.G3_C1891`1<class Generated1419.BaseClass1>)
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1> on type class Generated1419.G3_C1891`1<class Generated1419.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>::Method5()
		calli default string(class Generated1419.G3_C1891`1<class Generated1419.BaseClass1>)
		ldstr "G1_C15::Method5.4889()"
		ldstr "class G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1> on type class Generated1419.G3_C1891`1<class Generated1419.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>::Method4()
		calli default string(class Generated1419.G3_C1891`1<class Generated1419.BaseClass1>)
		ldstr "G3_C1891::Method4.18566()"
		ldstr "class G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1> on type class Generated1419.G3_C1891`1<class Generated1419.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1419.G3_C1891`1<class Generated1419.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1419.G3_C1891`1<class Generated1419.BaseClass1>::ClassMethod5178<object>()
		calli default string(class Generated1419.G3_C1891`1<class Generated1419.BaseClass1>)
		ldstr "G3_C1891::ClassMethod5178.18571<System.Object>()"
		ldstr "class G3_C1891`1<class Generated1419.BaseClass1> on type class Generated1419.G3_C1891`1<class Generated1419.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1419.G3_C1891`1<class Generated1419.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1419.G3_C1891`1<class Generated1419.BaseClass1>::ClassMethod5177()
		calli default string(class Generated1419.G3_C1891`1<class Generated1419.BaseClass1>)
		ldstr "G3_C1891::ClassMethod5177.18570()"
		ldstr "class G3_C1891`1<class Generated1419.BaseClass1> on type class Generated1419.G3_C1891`1<class Generated1419.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1419.G3_C1891`1<class Generated1419.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1419.G3_C1891`1<class Generated1419.BaseClass1>::Method6<object>()
		calli default string(class Generated1419.G3_C1891`1<class Generated1419.BaseClass1>)
		ldstr "G3_C1891::Method6.18568<System.Object>()"
		ldstr "class G3_C1891`1<class Generated1419.BaseClass1> on type class Generated1419.G3_C1891`1<class Generated1419.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1419.G3_C1891`1<class Generated1419.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1419.G3_C1891`1<class Generated1419.BaseClass1>::Method5()
		calli default string(class Generated1419.G3_C1891`1<class Generated1419.BaseClass1>)
		ldstr "G3_C1891::Method5.18567()"
		ldstr "class G3_C1891`1<class Generated1419.BaseClass1> on type class Generated1419.G3_C1891`1<class Generated1419.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1419.G3_C1891`1<class Generated1419.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1419.G3_C1891`1<class Generated1419.BaseClass1>::Method4()
		calli default string(class Generated1419.G3_C1891`1<class Generated1419.BaseClass1>)
		ldstr "G3_C1891::Method4.18566()"
		ldstr "class G3_C1891`1<class Generated1419.BaseClass1> on type class Generated1419.G3_C1891`1<class Generated1419.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1419.G3_C1891`1<class Generated1419.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1419.G3_C1891`1<class Generated1419.BaseClass1>::ClassMethod3018<object>()
		calli default string(class Generated1419.G3_C1891`1<class Generated1419.BaseClass1>)
		ldstr "G2_C818::ClassMethod3018.12543<System.Object>()"
		ldstr "class G3_C1891`1<class Generated1419.BaseClass1> on type class Generated1419.G3_C1891`1<class Generated1419.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1419.G3_C1891`1<class Generated1419.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1419.G3_C1891`1<class Generated1419.BaseClass1>::ClassMethod3017()
		calli default string(class Generated1419.G3_C1891`1<class Generated1419.BaseClass1>)
		ldstr "G2_C818::ClassMethod3017.12542()"
		ldstr "class G3_C1891`1<class Generated1419.BaseClass1> on type class Generated1419.G3_C1891`1<class Generated1419.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1419.G3_C1891`1<class Generated1419.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1419.G3_C1891`1<class Generated1419.BaseClass1>::Method7<object>()
		calli default string(class Generated1419.G3_C1891`1<class Generated1419.BaseClass1>)
		ldstr "G2_C818::Method7.12541<System.Object>()"
		ldstr "class G3_C1891`1<class Generated1419.BaseClass1> on type class Generated1419.G3_C1891`1<class Generated1419.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1419.G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1419.G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>::Method6<object>()
		calli default string(class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>)
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0> on type class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1419.G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1419.G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>::Method5()
		calli default string(class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>)
		ldstr "G1_C15::Method5.4889()"
		ldstr "class G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0> on type class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1419.G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1419.G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>::Method4()
		calli default string(class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>)
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0> on type class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1419.G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1419.G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>::Method7<object>()
		calli default string(class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>)
		ldstr "G1_C15::Method7.4885<System.Object>()"
		ldstr "class G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0> on type class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1419.IBase2`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>::Method7<object>()
		calli default string(class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>)
		ldstr "G2_C818::Method7.12541<System.Object>()"
		ldstr "class IBase2`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0> on type class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1419.IBase1`1<class Generated1419.BaseClass0>::Method4()
		calli default string(class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>)
		ldstr "G1_C15::Method4.MI.4888()"
		ldstr "class IBase1`1<class Generated1419.BaseClass0> on type class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1419.IBase1`1<class Generated1419.BaseClass0>::Method5()
		calli default string(class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>)
		ldstr "G1_C15::Method5.MI.4890()"
		ldstr "class IBase1`1<class Generated1419.BaseClass0> on type class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1419.IBase1`1<class Generated1419.BaseClass0>::Method6<object>()
		calli default string(class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>)
		ldstr "G1_C15::Method6.MI.4892<System.Object>()"
		ldstr "class IBase1`1<class Generated1419.BaseClass0> on type class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1419.IBase2`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>::Method7<object>()
		calli default string(class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>)
		ldstr "G2_C818::Method7.12541<System.Object>()"
		ldstr "class IBase2`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1> on type class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>::ClassMethod3018<object>()
		calli default string(class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>)
		ldstr "G2_C818::ClassMethod3018.12543<System.Object>()"
		ldstr "class G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0> on type class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>::ClassMethod3017()
		calli default string(class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>)
		ldstr "G2_C818::ClassMethod3017.12542()"
		ldstr "class G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0> on type class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>::Method7<object>()
		calli default string(class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>)
		ldstr "G2_C818::Method7.12541<System.Object>()"
		ldstr "class G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0> on type class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>::Method6<object>()
		calli default string(class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>)
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0> on type class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>::Method5()
		calli default string(class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>)
		ldstr "G1_C15::Method5.4889()"
		ldstr "class G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0> on type class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>::Method4()
		calli default string(class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>)
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0> on type class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1419.G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1419.G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>::Method6<object>()
		calli default string(class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>)
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1> on type class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1419.G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1419.G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>::Method5()
		calli default string(class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>)
		ldstr "G1_C15::Method5.4889()"
		ldstr "class G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1> on type class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1419.G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1419.G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>::Method4()
		calli default string(class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>)
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1> on type class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1419.G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1419.G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>::Method7<object>()
		calli default string(class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>)
		ldstr "G1_C15::Method7.4885<System.Object>()"
		ldstr "class G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1> on type class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1419.IBase2`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1>::Method7<object>()
		calli default string(class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>)
		ldstr "G1_C15::Method7.MI.4886<System.Object>()"
		ldstr "class IBase2`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1> on type class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1419.IBase1`1<class Generated1419.BaseClass0>::Method4()
		calli default string(class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>)
		ldstr "G1_C15::Method4.MI.4888()"
		ldstr "class IBase1`1<class Generated1419.BaseClass0> on type class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1419.IBase1`1<class Generated1419.BaseClass0>::Method5()
		calli default string(class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>)
		ldstr "G1_C15::Method5.MI.4890()"
		ldstr "class IBase1`1<class Generated1419.BaseClass0> on type class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1419.IBase1`1<class Generated1419.BaseClass0>::Method6<object>()
		calli default string(class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>)
		ldstr "G1_C15::Method6.MI.4892<System.Object>()"
		ldstr "class IBase1`1<class Generated1419.BaseClass0> on type class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1419.IBase2`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>::Method7<object>()
		calli default string(class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>)
		ldstr "G2_C818::Method7.12541<System.Object>()"
		ldstr "class IBase2`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1> on type class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>::ClassMethod3018<object>()
		calli default string(class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>)
		ldstr "G2_C818::ClassMethod3018.12543<System.Object>()"
		ldstr "class G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1> on type class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>::ClassMethod3017()
		calli default string(class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>)
		ldstr "G2_C818::ClassMethod3017.12542()"
		ldstr "class G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1> on type class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>::Method7<object>()
		calli default string(class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>)
		ldstr "G2_C818::Method7.12541<System.Object>()"
		ldstr "class G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1> on type class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>::Method6<object>()
		calli default string(class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>)
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1> on type class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>::Method5()
		calli default string(class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>)
		ldstr "G1_C15::Method5.4889()"
		ldstr "class G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1> on type class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>::Method4()
		calli default string(class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>)
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1> on type class Generated1419.G2_C818`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1419.G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1419.G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>::Method6<object>()
		calli default string(class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass0>)
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0> on type class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1419.G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1419.G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>::Method5()
		calli default string(class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass0>)
		ldstr "G1_C15::Method5.4889()"
		ldstr "class G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0> on type class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1419.G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1419.G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>::Method4()
		calli default string(class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass0>)
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0> on type class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1419.G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1419.G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>::Method7<object>()
		calli default string(class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass0>)
		ldstr "G1_C15::Method7.4885<System.Object>()"
		ldstr "class G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0> on type class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1419.IBase2`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0>::Method7<object>()
		calli default string(class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass0>)
		ldstr "G2_C818::Method7.12541<System.Object>()"
		ldstr "class IBase2`2<class Generated1419.BaseClass0,class Generated1419.BaseClass0> on type class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1419.IBase1`1<class Generated1419.BaseClass0>::Method4()
		calli default string(class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass0>)
		ldstr "G1_C15::Method4.MI.4888()"
		ldstr "class IBase1`1<class Generated1419.BaseClass0> on type class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1419.IBase1`1<class Generated1419.BaseClass0>::Method5()
		calli default string(class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass0>)
		ldstr "G1_C15::Method5.MI.4890()"
		ldstr "class IBase1`1<class Generated1419.BaseClass0> on type class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1419.IBase1`1<class Generated1419.BaseClass0>::Method6<object>()
		calli default string(class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass0>)
		ldstr "G1_C15::Method6.MI.4892<System.Object>()"
		ldstr "class IBase1`1<class Generated1419.BaseClass0> on type class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1419.IBase2`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>::Method7<object>()
		calli default string(class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass0>)
		ldstr "G2_C818::Method7.12541<System.Object>()"
		ldstr "class IBase2`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1> on type class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass0>::ClassMethod3018<object>()
		calli default string(class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass0>)
		ldstr "G2_C818::ClassMethod3018.12543<System.Object>()"
		ldstr "class G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass0> on type class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass0>::ClassMethod3017()
		calli default string(class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass0>)
		ldstr "G2_C818::ClassMethod3017.12542()"
		ldstr "class G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass0> on type class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass0>::Method7<object>()
		calli default string(class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass0>)
		ldstr "G2_C818::Method7.12541<System.Object>()"
		ldstr "class G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass0> on type class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass0>::Method6<object>()
		calli default string(class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass0>)
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass0> on type class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass0>::Method5()
		calli default string(class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass0>)
		ldstr "G1_C15::Method5.4889()"
		ldstr "class G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass0> on type class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass0>::Method4()
		calli default string(class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass0>)
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass0> on type class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1419.IBase2`2<class Generated1419.BaseClass1,class Generated1419.BaseClass0>::Method7<object>()
		calli default string(class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass0>)
		ldstr "G2_C818::Method7.12541<System.Object>()"
		ldstr "class IBase2`2<class Generated1419.BaseClass1,class Generated1419.BaseClass0> on type class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1419.IBase2`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1>::Method7<object>()
		calli default string(class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass0>)
		ldstr "G2_C818::Method7.12541<System.Object>()"
		ldstr "class IBase2`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1> on type class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1419.G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1419.G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>::Method6<object>()
		calli default string(class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1>)
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1> on type class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1419.G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1419.G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>::Method5()
		calli default string(class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1>)
		ldstr "G1_C15::Method5.4889()"
		ldstr "class G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1> on type class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1419.G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1419.G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>::Method4()
		calli default string(class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1>)
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1> on type class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1419.G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1419.G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>::Method7<object>()
		calli default string(class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1>)
		ldstr "G1_C15::Method7.4885<System.Object>()"
		ldstr "class G1_C15`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1> on type class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1419.IBase2`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1>::Method7<object>()
		calli default string(class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1>)
		ldstr "G2_C818::Method7.12541<System.Object>()"
		ldstr "class IBase2`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1> on type class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1419.IBase1`1<class Generated1419.BaseClass0>::Method4()
		calli default string(class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1>)
		ldstr "G1_C15::Method4.MI.4888()"
		ldstr "class IBase1`1<class Generated1419.BaseClass0> on type class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1419.IBase1`1<class Generated1419.BaseClass0>::Method5()
		calli default string(class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1>)
		ldstr "G1_C15::Method5.MI.4890()"
		ldstr "class IBase1`1<class Generated1419.BaseClass0> on type class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1419.IBase1`1<class Generated1419.BaseClass0>::Method6<object>()
		calli default string(class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1>)
		ldstr "G1_C15::Method6.MI.4892<System.Object>()"
		ldstr "class IBase1`1<class Generated1419.BaseClass0> on type class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1419.IBase2`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1>::Method7<object>()
		calli default string(class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1>)
		ldstr "G2_C818::Method7.12541<System.Object>()"
		ldstr "class IBase2`2<class Generated1419.BaseClass0,class Generated1419.BaseClass1> on type class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1>::ClassMethod3018<object>()
		calli default string(class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1>)
		ldstr "G2_C818::ClassMethod3018.12543<System.Object>()"
		ldstr "class G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1> on type class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1>::ClassMethod3017()
		calli default string(class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1>)
		ldstr "G2_C818::ClassMethod3017.12542()"
		ldstr "class G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1> on type class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1>::Method7<object>()
		calli default string(class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1>)
		ldstr "G2_C818::Method7.12541<System.Object>()"
		ldstr "class G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1> on type class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1>::Method6<object>()
		calli default string(class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1>)
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1> on type class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1>::Method5()
		calli default string(class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1>)
		ldstr "G1_C15::Method5.4889()"
		ldstr "class G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1> on type class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1>::Method4()
		calli default string(class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1>)
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1> on type class Generated1419.G2_C818`2<class Generated1419.BaseClass1,class Generated1419.BaseClass1>"
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
		call void Generated1419.Generated1419::MethodCallingTest()
		call void Generated1419.Generated1419::ConstrainedCallsTest()
		call void Generated1419.Generated1419::StructConstrainedInterfaceCallsTest()
		call void Generated1419.Generated1419::CalliTest()
		ldc.i4 100
		ret
	}
}
