// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) .ver 4:0:0:0 }
.assembly extern TestFramework { .publickeytoken = ( B0 3F 5F 7F 11 D5 0A 3A )  }

//TYPES IN FORWARDER ASSEMBLIES:

//TEST ASSEMBLY:
.assembly extern xunit.core {}

.class public Generated1242.BaseClass0 
{
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void [mscorlib]System.Object::.ctor()
		ret
	}
}
.class public Generated1242.BaseClass1 
		extends Generated1242.BaseClass0
{
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void Generated1242.BaseClass0::.ctor()
		ret
	}
}
.class public Generated1242.G3_C1714`1<T0> 
		extends class Generated1242.G2_C688`2<class Generated1242.BaseClass1,!T0>
		implements class Generated1242.IBase2`2<class Generated1242.BaseClass1,!T0> 
{
	.method public hidebysig newslot virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G3_C1714::Method7.17596<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod4826<M0>() cil managed noinlining { 
		ldstr "G3_C1714::ClassMethod4826.17597<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod4827<M0>() cil managed noinlining { 
		ldstr "G3_C1714::ClassMethod4827.17598<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class Generated1242.G2_C688`2<class Generated1242.BaseClass1,!T0>::.ctor()
		ret
	}
}
.class public Generated1242.G2_C688`2<T0, T1> 
		extends class Generated1242.G1_C12`2<!T1,!T1>
{
	.method public hidebysig newslot virtual instance string ClassMethod2736() cil managed noinlining { 
		ldstr "G2_C688::ClassMethod2736.11336()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod2737<M0>() cil managed noinlining { 
		ldstr "G2_C688::ClassMethod2737.11337<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod2738<M0>() cil managed noinlining { 
		ldstr "G2_C688::ClassMethod2738.11338<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'G1_C12<T1,T1>.ClassMethod1346'<M0>() cil managed noinlining { 
		.override method instance string class Generated1242.G1_C12`2<!T1,!T1>::ClassMethod1346<[1]>()
		ldstr "G2_C688::ClassMethod1346.MI.11339<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'G1_C12<T1,T1>.ClassMethod1347'<M0>() cil managed noinlining { 
		.override method instance string class Generated1242.G1_C12`2<!T1,!T1>::ClassMethod1347<[1]>()
		ldstr "G2_C688::ClassMethod1347.MI.11340<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class Generated1242.G1_C12`2<!T1,!T1>::.ctor()
		ret
	}
}
.class interface public abstract Generated1242.IBase2`2<+T0, -T1> 
{
	.method public hidebysig newslot abstract virtual instance string Method7<M0>() cil managed { }
}
.class public Generated1242.G1_C12`2<T0, T1> 
		implements class Generated1242.IBase2`2<!T0,!T1>, class Generated1242.IBase1`1<class Generated1242.BaseClass0> 
{
	.method public hidebysig virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G1_C12::Method7.4863<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase2<T0,T1>.Method7'<M0>() cil managed noinlining { 
		.override method instance string class Generated1242.IBase2`2<!T0,!T1>::Method7<[1]>()
		ldstr "G1_C12::Method7.MI.4864<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string Method4() cil managed noinlining { 
		ldstr "G1_C12::Method4.4865()"
		ret
	}
	.method public hidebysig virtual instance string Method5() cil managed noinlining { 
		ldstr "G1_C12::Method5.4866()"
		ret
	}
	.method public hidebysig virtual instance string Method6<M0>() cil managed noinlining { 
		ldstr "G1_C12::Method6.4867<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<class Generated1242.BaseClass0>.Method6'<M0>() cil managed noinlining { 
		.override method instance string class Generated1242.IBase1`1<class Generated1242.BaseClass0>::Method6<[1]>()
		ldstr "G1_C12::Method6.MI.4868<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1346<M0>() cil managed noinlining { 
		ldstr "G1_C12::ClassMethod1346.4869<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1347<M0>() cil managed noinlining { 
		ldstr "G1_C12::ClassMethod1347.4870<"
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
.class interface public abstract Generated1242.IBase1`1<+T0> 
{
	.method public hidebysig newslot abstract virtual instance string Method4() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method5() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method6<M0>() cil managed { }
}
.class public auto ansi beforefieldinit Generated1242.Generated1242 {
	.method static void M.BaseClass0<(Generated1242.BaseClass0)W>(!!W inst, string exp) cil managed {
		.maxstack 5
		.locals init (string[] actualResults)
		ldc.i4.s    0
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.BaseClass0<(Generated1242.BaseClass0)W>(!!W inst, string exp)"
		ldc.i4.s 0
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.BaseClass1<(Generated1242.BaseClass1)W>(!!W inst, string exp) cil managed {
		.maxstack 5
		.locals init (string[] actualResults)
		ldc.i4.s    0
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.BaseClass1<(Generated1242.BaseClass1)W>(!!W inst, string exp)"
		ldc.i4.s 0
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1714.T<T0,(class Generated1242.G3_C1714`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 16
		.locals init (string[] actualResults)
		ldc.i4.s    11
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1714.T<T0,(class Generated1242.G3_C1714`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 11
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G3_C1714`1<!!T0>::ClassMethod1346<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G3_C1714`1<!!T0>::ClassMethod1347<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G3_C1714`1<!!T0>::ClassMethod2736()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G3_C1714`1<!!T0>::ClassMethod2737<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G3_C1714`1<!!T0>::ClassMethod2738<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G3_C1714`1<!!T0>::ClassMethod4826<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G3_C1714`1<!!T0>::ClassMethod4827<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G3_C1714`1<!!T0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G3_C1714`1<!!T0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G3_C1714`1<!!T0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G3_C1714`1<!!T0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1714.A<(class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 16
		.locals init (string[] actualResults)
		ldc.i4.s    11
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1714.A<(class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 11
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>::ClassMethod1346<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>::ClassMethod1347<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>::ClassMethod2736()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>::ClassMethod2737<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>::ClassMethod2738<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>::ClassMethod4826<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>::ClassMethod4827<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1714.B<(class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 16
		.locals init (string[] actualResults)
		ldc.i4.s    11
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1714.B<(class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 11
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>::ClassMethod1346<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>::ClassMethod1347<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>::ClassMethod2736()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>::ClassMethod2737<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>::ClassMethod2738<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>::ClassMethod4826<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>::ClassMethod4827<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C688.T.T<T0,T1,(class Generated1242.G2_C688`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 14
		.locals init (string[] actualResults)
		ldc.i4.s    9
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C688.T.T<T0,T1,(class Generated1242.G2_C688`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 9
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G2_C688`2<!!T0,!!T1>::ClassMethod1346<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G2_C688`2<!!T0,!!T1>::ClassMethod1347<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G2_C688`2<!!T0,!!T1>::ClassMethod2736()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G2_C688`2<!!T0,!!T1>::ClassMethod2737<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G2_C688`2<!!T0,!!T1>::ClassMethod2738<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G2_C688`2<!!T0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G2_C688`2<!!T0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G2_C688`2<!!T0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G2_C688`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C688.A.T<T1,(class Generated1242.G2_C688`2<class Generated1242.BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 14
		.locals init (string[] actualResults)
		ldc.i4.s    9
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C688.A.T<T1,(class Generated1242.G2_C688`2<class Generated1242.BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 9
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass0,!!T1>::ClassMethod1346<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass0,!!T1>::ClassMethod1347<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass0,!!T1>::ClassMethod2736()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass0,!!T1>::ClassMethod2737<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass0,!!T1>::ClassMethod2738<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C688.A.A<(class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 14
		.locals init (string[] actualResults)
		ldc.i4.s    9
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C688.A.A<(class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 9
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>::ClassMethod1346<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>::ClassMethod1347<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>::ClassMethod2736()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>::ClassMethod2737<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>::ClassMethod2738<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C688.A.B<(class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 14
		.locals init (string[] actualResults)
		ldc.i4.s    9
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C688.A.B<(class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 9
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>::ClassMethod1346<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>::ClassMethod1347<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>::ClassMethod2736()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>::ClassMethod2737<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>::ClassMethod2738<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C688.B.T<T1,(class Generated1242.G2_C688`2<class Generated1242.BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 14
		.locals init (string[] actualResults)
		ldc.i4.s    9
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C688.B.T<T1,(class Generated1242.G2_C688`2<class Generated1242.BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 9
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass1,!!T1>::ClassMethod1346<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass1,!!T1>::ClassMethod1347<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass1,!!T1>::ClassMethod2736()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass1,!!T1>::ClassMethod2737<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass1,!!T1>::ClassMethod2738<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass1,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass1,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass1,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C688.B.A<(class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 14
		.locals init (string[] actualResults)
		ldc.i4.s    9
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C688.B.A<(class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 9
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>::ClassMethod1346<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>::ClassMethod1347<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>::ClassMethod2736()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>::ClassMethod2737<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>::ClassMethod2738<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C688.B.B<(class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 14
		.locals init (string[] actualResults)
		ldc.i4.s    9
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C688.B.B<(class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 9
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>::ClassMethod1346<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>::ClassMethod1347<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>::ClassMethod2736()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>::ClassMethod2737<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>::ClassMethod2738<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.T.T<T0,T1,(class Generated1242.IBase2`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.T.T<T0,T1,(class Generated1242.IBase2`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.IBase2`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.A.T<T1,(class Generated1242.IBase2`2<class Generated1242.BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.A.T<T1,(class Generated1242.IBase2`2<class Generated1242.BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.IBase2`2<class Generated1242.BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.A.A<(class Generated1242.IBase2`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.A.A<(class Generated1242.IBase2`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.IBase2`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.A.B<(class Generated1242.IBase2`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.A.B<(class Generated1242.IBase2`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.IBase2`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.B.T<T1,(class Generated1242.IBase2`2<class Generated1242.BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.B.T<T1,(class Generated1242.IBase2`2<class Generated1242.BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.IBase2`2<class Generated1242.BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.B.A<(class Generated1242.IBase2`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.B.A<(class Generated1242.IBase2`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.IBase2`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.B.B<(class Generated1242.IBase2`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.B.B<(class Generated1242.IBase2`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.IBase2`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C12.T.T<T0,T1,(class Generated1242.G1_C12`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C12.T.T<T0,T1,(class Generated1242.G1_C12`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G1_C12`2<!!T0,!!T1>::ClassMethod1346<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G1_C12`2<!!T0,!!T1>::ClassMethod1347<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G1_C12`2<!!T0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G1_C12`2<!!T0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G1_C12`2<!!T0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G1_C12`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C12.A.T<T1,(class Generated1242.G1_C12`2<class Generated1242.BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C12.A.T<T1,(class Generated1242.G1_C12`2<class Generated1242.BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass0,!!T1>::ClassMethod1346<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass0,!!T1>::ClassMethod1347<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C12.A.A<(class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C12.A.A<(class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>::ClassMethod1346<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>::ClassMethod1347<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C12.A.B<(class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C12.A.B<(class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>::ClassMethod1346<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>::ClassMethod1347<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C12.B.T<T1,(class Generated1242.G1_C12`2<class Generated1242.BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C12.B.T<T1,(class Generated1242.G1_C12`2<class Generated1242.BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass1,!!T1>::ClassMethod1346<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass1,!!T1>::ClassMethod1347<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass1,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass1,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass1,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C12.B.A<(class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C12.B.A<(class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>::ClassMethod1346<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>::ClassMethod1347<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C12.B.B<(class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C12.B.B<(class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>::ClassMethod1346<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>::ClassMethod1347<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase1.T<T0,(class Generated1242.IBase1`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase1.T<T0,(class Generated1242.IBase1`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.IBase1`1<!!T0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.IBase1`1<!!T0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.IBase1`1<!!T0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase1.A<(class Generated1242.IBase1`1<class Generated1242.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase1.A<(class Generated1242.IBase1`1<class Generated1242.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.IBase1`1<class Generated1242.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.IBase1`1<class Generated1242.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.IBase1`1<class Generated1242.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase1.B<(class Generated1242.IBase1`1<class Generated1242.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase1.B<(class Generated1242.IBase1`1<class Generated1242.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.IBase1`1<class Generated1242.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.IBase1`1<class Generated1242.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1242.IBase1`1<class Generated1242.BaseClass1>::Method6<object>()
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
		newobj instance void class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>
		callvirt instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>::ClassMethod1347<object>()
		ldstr "G2_C688::ClassMethod1347.MI.11340<System.Object>()"
		ldstr "class G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0> on type class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>
		callvirt instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>::ClassMethod1346<object>()
		ldstr "G2_C688::ClassMethod1346.MI.11339<System.Object>()"
		ldstr "class G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0> on type class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>
		callvirt instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>::Method6<object>()
		ldstr "G1_C12::Method6.4867<System.Object>()"
		ldstr "class G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0> on type class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>
		callvirt instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>::Method5()
		ldstr "G1_C12::Method5.4866()"
		ldstr "class G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0> on type class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>
		callvirt instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>::Method4()
		ldstr "G1_C12::Method4.4865()"
		ldstr "class G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0> on type class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>
		callvirt instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>::Method7<object>()
		ldstr "G1_C12::Method7.4863<System.Object>()"
		ldstr "class G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0> on type class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1242.IBase2`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>::Method7<object>()
		ldstr "G3_C1714::Method7.17596<System.Object>()"
		ldstr "class IBase2`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0> on type class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1242.IBase1`1<class Generated1242.BaseClass0>::Method4()
		ldstr "G1_C12::Method4.4865()"
		ldstr "class IBase1`1<class Generated1242.BaseClass0> on type class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1242.IBase1`1<class Generated1242.BaseClass0>::Method5()
		ldstr "G1_C12::Method5.4866()"
		ldstr "class IBase1`1<class Generated1242.BaseClass0> on type class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1242.IBase1`1<class Generated1242.BaseClass0>::Method6<object>()
		ldstr "G1_C12::Method6.MI.4868<System.Object>()"
		ldstr "class IBase1`1<class Generated1242.BaseClass0> on type class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1242.IBase2`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>::Method7<object>()
		ldstr "G3_C1714::Method7.17596<System.Object>()"
		ldstr "class IBase2`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1> on type class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>
		callvirt instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>::ClassMethod2738<object>()
		ldstr "G2_C688::ClassMethod2738.11338<System.Object>()"
		ldstr "class G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0> on type class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>
		callvirt instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>::ClassMethod2737<object>()
		ldstr "G2_C688::ClassMethod2737.11337<System.Object>()"
		ldstr "class G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0> on type class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>
		callvirt instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>::ClassMethod2736()
		ldstr "G2_C688::ClassMethod2736.11336()"
		ldstr "class G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0> on type class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>
		callvirt instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>::ClassMethod1347<object>()
		ldstr "G2_C688::ClassMethod1347.MI.11340<System.Object>()"
		ldstr "class G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0> on type class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>
		callvirt instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>::ClassMethod1346<object>()
		ldstr "G2_C688::ClassMethod1346.MI.11339<System.Object>()"
		ldstr "class G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0> on type class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>
		callvirt instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>::Method6<object>()
		ldstr "G1_C12::Method6.4867<System.Object>()"
		ldstr "class G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0> on type class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>
		callvirt instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>::Method5()
		ldstr "G1_C12::Method5.4866()"
		ldstr "class G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0> on type class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>
		callvirt instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>::Method4()
		ldstr "G1_C12::Method4.4865()"
		ldstr "class G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0> on type class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>
		callvirt instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>::Method7<object>()
		ldstr "G1_C12::Method7.4863<System.Object>()"
		ldstr "class G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0> on type class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>
		callvirt instance string class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>::ClassMethod4827<object>()
		ldstr "G3_C1714::ClassMethod4827.17598<System.Object>()"
		ldstr "class G3_C1714`1<class Generated1242.BaseClass0> on type class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>
		callvirt instance string class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>::ClassMethod4826<object>()
		ldstr "G3_C1714::ClassMethod4826.17597<System.Object>()"
		ldstr "class G3_C1714`1<class Generated1242.BaseClass0> on type class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>
		callvirt instance string class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>::Method7<object>()
		ldstr "G3_C1714::Method7.17596<System.Object>()"
		ldstr "class G3_C1714`1<class Generated1242.BaseClass0> on type class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>
		callvirt instance string class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>::ClassMethod2738<object>()
		ldstr "G2_C688::ClassMethod2738.11338<System.Object>()"
		ldstr "class G3_C1714`1<class Generated1242.BaseClass0> on type class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>
		callvirt instance string class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>::ClassMethod2737<object>()
		ldstr "G2_C688::ClassMethod2737.11337<System.Object>()"
		ldstr "class G3_C1714`1<class Generated1242.BaseClass0> on type class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>
		callvirt instance string class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>::ClassMethod2736()
		ldstr "G2_C688::ClassMethod2736.11336()"
		ldstr "class G3_C1714`1<class Generated1242.BaseClass0> on type class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>
		callvirt instance string class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>::ClassMethod1347<object>()
		ldstr "G2_C688::ClassMethod1347.MI.11340<System.Object>()"
		ldstr "class G3_C1714`1<class Generated1242.BaseClass0> on type class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>
		callvirt instance string class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>::ClassMethod1346<object>()
		ldstr "G2_C688::ClassMethod1346.MI.11339<System.Object>()"
		ldstr "class G3_C1714`1<class Generated1242.BaseClass0> on type class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>
		callvirt instance string class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>::Method6<object>()
		ldstr "G1_C12::Method6.4867<System.Object>()"
		ldstr "class G3_C1714`1<class Generated1242.BaseClass0> on type class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>
		callvirt instance string class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>::Method5()
		ldstr "G1_C12::Method5.4866()"
		ldstr "class G3_C1714`1<class Generated1242.BaseClass0> on type class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>
		callvirt instance string class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>::Method4()
		ldstr "G1_C12::Method4.4865()"
		ldstr "class G3_C1714`1<class Generated1242.BaseClass0> on type class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1242.IBase2`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>::Method7<object>()
		ldstr "G3_C1714::Method7.17596<System.Object>()"
		ldstr "class IBase2`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0> on type class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1242.IBase2`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>::Method7<object>()
		ldstr "G3_C1714::Method7.17596<System.Object>()"
		ldstr "class IBase2`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1> on type class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>
		callvirt instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>::ClassMethod1347<object>()
		ldstr "G2_C688::ClassMethod1347.MI.11340<System.Object>()"
		ldstr "class G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1> on type class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>
		callvirt instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>::ClassMethod1346<object>()
		ldstr "G2_C688::ClassMethod1346.MI.11339<System.Object>()"
		ldstr "class G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1> on type class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>
		callvirt instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>::Method6<object>()
		ldstr "G1_C12::Method6.4867<System.Object>()"
		ldstr "class G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1> on type class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>
		callvirt instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>::Method5()
		ldstr "G1_C12::Method5.4866()"
		ldstr "class G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1> on type class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>
		callvirt instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>::Method4()
		ldstr "G1_C12::Method4.4865()"
		ldstr "class G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1> on type class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>
		callvirt instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>::Method7<object>()
		ldstr "G1_C12::Method7.4863<System.Object>()"
		ldstr "class G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1> on type class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1242.IBase2`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>::Method7<object>()
		ldstr "G3_C1714::Method7.17596<System.Object>()"
		ldstr "class IBase2`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1> on type class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1242.IBase1`1<class Generated1242.BaseClass0>::Method4()
		ldstr "G1_C12::Method4.4865()"
		ldstr "class IBase1`1<class Generated1242.BaseClass0> on type class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1242.IBase1`1<class Generated1242.BaseClass0>::Method5()
		ldstr "G1_C12::Method5.4866()"
		ldstr "class IBase1`1<class Generated1242.BaseClass0> on type class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1242.IBase1`1<class Generated1242.BaseClass0>::Method6<object>()
		ldstr "G1_C12::Method6.MI.4868<System.Object>()"
		ldstr "class IBase1`1<class Generated1242.BaseClass0> on type class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1242.IBase2`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>::Method7<object>()
		ldstr "G3_C1714::Method7.17596<System.Object>()"
		ldstr "class IBase2`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1> on type class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>
		callvirt instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>::ClassMethod2738<object>()
		ldstr "G2_C688::ClassMethod2738.11338<System.Object>()"
		ldstr "class G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1> on type class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>
		callvirt instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>::ClassMethod2737<object>()
		ldstr "G2_C688::ClassMethod2737.11337<System.Object>()"
		ldstr "class G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1> on type class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>
		callvirt instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>::ClassMethod2736()
		ldstr "G2_C688::ClassMethod2736.11336()"
		ldstr "class G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1> on type class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>
		callvirt instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>::ClassMethod1347<object>()
		ldstr "G2_C688::ClassMethod1347.MI.11340<System.Object>()"
		ldstr "class G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1> on type class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>
		callvirt instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>::ClassMethod1346<object>()
		ldstr "G2_C688::ClassMethod1346.MI.11339<System.Object>()"
		ldstr "class G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1> on type class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>
		callvirt instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>::Method6<object>()
		ldstr "G1_C12::Method6.4867<System.Object>()"
		ldstr "class G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1> on type class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>
		callvirt instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>::Method5()
		ldstr "G1_C12::Method5.4866()"
		ldstr "class G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1> on type class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>
		callvirt instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>::Method4()
		ldstr "G1_C12::Method4.4865()"
		ldstr "class G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1> on type class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>
		callvirt instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>::Method7<object>()
		ldstr "G1_C12::Method7.4863<System.Object>()"
		ldstr "class G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1> on type class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>
		callvirt instance string class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>::ClassMethod4827<object>()
		ldstr "G3_C1714::ClassMethod4827.17598<System.Object>()"
		ldstr "class G3_C1714`1<class Generated1242.BaseClass1> on type class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>
		callvirt instance string class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>::ClassMethod4826<object>()
		ldstr "G3_C1714::ClassMethod4826.17597<System.Object>()"
		ldstr "class G3_C1714`1<class Generated1242.BaseClass1> on type class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>
		callvirt instance string class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>::Method7<object>()
		ldstr "G3_C1714::Method7.17596<System.Object>()"
		ldstr "class G3_C1714`1<class Generated1242.BaseClass1> on type class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>
		callvirt instance string class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>::ClassMethod2738<object>()
		ldstr "G2_C688::ClassMethod2738.11338<System.Object>()"
		ldstr "class G3_C1714`1<class Generated1242.BaseClass1> on type class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>
		callvirt instance string class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>::ClassMethod2737<object>()
		ldstr "G2_C688::ClassMethod2737.11337<System.Object>()"
		ldstr "class G3_C1714`1<class Generated1242.BaseClass1> on type class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>
		callvirt instance string class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>::ClassMethod2736()
		ldstr "G2_C688::ClassMethod2736.11336()"
		ldstr "class G3_C1714`1<class Generated1242.BaseClass1> on type class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>
		callvirt instance string class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>::ClassMethod1347<object>()
		ldstr "G2_C688::ClassMethod1347.MI.11340<System.Object>()"
		ldstr "class G3_C1714`1<class Generated1242.BaseClass1> on type class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>
		callvirt instance string class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>::ClassMethod1346<object>()
		ldstr "G2_C688::ClassMethod1346.MI.11339<System.Object>()"
		ldstr "class G3_C1714`1<class Generated1242.BaseClass1> on type class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>
		callvirt instance string class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>::Method6<object>()
		ldstr "G1_C12::Method6.4867<System.Object>()"
		ldstr "class G3_C1714`1<class Generated1242.BaseClass1> on type class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>
		callvirt instance string class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>::Method5()
		ldstr "G1_C12::Method5.4866()"
		ldstr "class G3_C1714`1<class Generated1242.BaseClass1> on type class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>
		callvirt instance string class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>::Method4()
		ldstr "G1_C12::Method4.4865()"
		ldstr "class G3_C1714`1<class Generated1242.BaseClass1> on type class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>
		callvirt instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>::ClassMethod1347<object>()
		ldstr "G2_C688::ClassMethod1347.MI.11340<System.Object>()"
		ldstr "class G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>
		callvirt instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>::ClassMethod1346<object>()
		ldstr "G2_C688::ClassMethod1346.MI.11339<System.Object>()"
		ldstr "class G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>
		callvirt instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>::Method6<object>()
		ldstr "G1_C12::Method6.4867<System.Object>()"
		ldstr "class G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>
		callvirt instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>::Method5()
		ldstr "G1_C12::Method5.4866()"
		ldstr "class G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>
		callvirt instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>::Method4()
		ldstr "G1_C12::Method4.4865()"
		ldstr "class G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>
		callvirt instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>::Method7<object>()
		ldstr "G1_C12::Method7.4863<System.Object>()"
		ldstr "class G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1242.IBase2`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>::Method7<object>()
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1242.IBase1`1<class Generated1242.BaseClass0>::Method4()
		ldstr "G1_C12::Method4.4865()"
		ldstr "class IBase1`1<class Generated1242.BaseClass0> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1242.IBase1`1<class Generated1242.BaseClass0>::Method5()
		ldstr "G1_C12::Method5.4866()"
		ldstr "class IBase1`1<class Generated1242.BaseClass0> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1242.IBase1`1<class Generated1242.BaseClass0>::Method6<object>()
		ldstr "G1_C12::Method6.MI.4868<System.Object>()"
		ldstr "class IBase1`1<class Generated1242.BaseClass0> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1242.IBase2`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>::Method7<object>()
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>
		callvirt instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>::ClassMethod2738<object>()
		ldstr "G2_C688::ClassMethod2738.11338<System.Object>()"
		ldstr "class G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>
		callvirt instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>::ClassMethod2737<object>()
		ldstr "G2_C688::ClassMethod2737.11337<System.Object>()"
		ldstr "class G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>
		callvirt instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>::ClassMethod2736()
		ldstr "G2_C688::ClassMethod2736.11336()"
		ldstr "class G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>
		callvirt instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>::ClassMethod1347<object>()
		ldstr "G2_C688::ClassMethod1347.MI.11340<System.Object>()"
		ldstr "class G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>
		callvirt instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>::ClassMethod1346<object>()
		ldstr "G2_C688::ClassMethod1346.MI.11339<System.Object>()"
		ldstr "class G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>
		callvirt instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>::Method6<object>()
		ldstr "G1_C12::Method6.4867<System.Object>()"
		ldstr "class G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>
		callvirt instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>::Method5()
		ldstr "G1_C12::Method5.4866()"
		ldstr "class G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>
		callvirt instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>::Method4()
		ldstr "G1_C12::Method4.4865()"
		ldstr "class G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>
		callvirt instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>::Method7<object>()
		ldstr "G1_C12::Method7.4863<System.Object>()"
		ldstr "class G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>
		callvirt instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>::ClassMethod1347<object>()
		ldstr "G2_C688::ClassMethod1347.MI.11340<System.Object>()"
		ldstr "class G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>
		callvirt instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>::ClassMethod1346<object>()
		ldstr "G2_C688::ClassMethod1346.MI.11339<System.Object>()"
		ldstr "class G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>
		callvirt instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>::Method6<object>()
		ldstr "G1_C12::Method6.4867<System.Object>()"
		ldstr "class G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>
		callvirt instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>::Method5()
		ldstr "G1_C12::Method5.4866()"
		ldstr "class G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>
		callvirt instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>::Method4()
		ldstr "G1_C12::Method4.4865()"
		ldstr "class G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>
		callvirt instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>::Method7<object>()
		ldstr "G1_C12::Method7.4863<System.Object>()"
		ldstr "class G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1242.IBase2`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>::Method7<object>()
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1242.IBase1`1<class Generated1242.BaseClass0>::Method4()
		ldstr "G1_C12::Method4.4865()"
		ldstr "class IBase1`1<class Generated1242.BaseClass0> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1242.IBase1`1<class Generated1242.BaseClass0>::Method5()
		ldstr "G1_C12::Method5.4866()"
		ldstr "class IBase1`1<class Generated1242.BaseClass0> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1242.IBase1`1<class Generated1242.BaseClass0>::Method6<object>()
		ldstr "G1_C12::Method6.MI.4868<System.Object>()"
		ldstr "class IBase1`1<class Generated1242.BaseClass0> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1242.IBase2`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>::Method7<object>()
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>
		callvirt instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>::ClassMethod2738<object>()
		ldstr "G2_C688::ClassMethod2738.11338<System.Object>()"
		ldstr "class G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>
		callvirt instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>::ClassMethod2737<object>()
		ldstr "G2_C688::ClassMethod2737.11337<System.Object>()"
		ldstr "class G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>
		callvirt instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>::ClassMethod2736()
		ldstr "G2_C688::ClassMethod2736.11336()"
		ldstr "class G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>
		callvirt instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>::ClassMethod1347<object>()
		ldstr "G2_C688::ClassMethod1347.MI.11340<System.Object>()"
		ldstr "class G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>
		callvirt instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>::ClassMethod1346<object>()
		ldstr "G2_C688::ClassMethod1346.MI.11339<System.Object>()"
		ldstr "class G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>
		callvirt instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>::Method6<object>()
		ldstr "G1_C12::Method6.4867<System.Object>()"
		ldstr "class G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>
		callvirt instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>::Method5()
		ldstr "G1_C12::Method5.4866()"
		ldstr "class G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>
		callvirt instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>::Method4()
		ldstr "G1_C12::Method4.4865()"
		ldstr "class G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>
		callvirt instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>::Method7<object>()
		ldstr "G1_C12::Method7.4863<System.Object>()"
		ldstr "class G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>
		callvirt instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>::ClassMethod1347<object>()
		ldstr "G2_C688::ClassMethod1347.MI.11340<System.Object>()"
		ldstr "class G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>
		callvirt instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>::ClassMethod1346<object>()
		ldstr "G2_C688::ClassMethod1346.MI.11339<System.Object>()"
		ldstr "class G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>
		callvirt instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>::Method6<object>()
		ldstr "G1_C12::Method6.4867<System.Object>()"
		ldstr "class G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>
		callvirt instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>::Method5()
		ldstr "G1_C12::Method5.4866()"
		ldstr "class G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>
		callvirt instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>::Method4()
		ldstr "G1_C12::Method4.4865()"
		ldstr "class G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>
		callvirt instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>::Method7<object>()
		ldstr "G1_C12::Method7.4863<System.Object>()"
		ldstr "class G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1242.IBase2`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>::Method7<object>()
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1242.IBase1`1<class Generated1242.BaseClass0>::Method4()
		ldstr "G1_C12::Method4.4865()"
		ldstr "class IBase1`1<class Generated1242.BaseClass0> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1242.IBase1`1<class Generated1242.BaseClass0>::Method5()
		ldstr "G1_C12::Method5.4866()"
		ldstr "class IBase1`1<class Generated1242.BaseClass0> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1242.IBase1`1<class Generated1242.BaseClass0>::Method6<object>()
		ldstr "G1_C12::Method6.MI.4868<System.Object>()"
		ldstr "class IBase1`1<class Generated1242.BaseClass0> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1242.IBase2`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>::Method7<object>()
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>
		callvirt instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>::ClassMethod2738<object>()
		ldstr "G2_C688::ClassMethod2738.11338<System.Object>()"
		ldstr "class G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>
		callvirt instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>::ClassMethod2737<object>()
		ldstr "G2_C688::ClassMethod2737.11337<System.Object>()"
		ldstr "class G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>
		callvirt instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>::ClassMethod2736()
		ldstr "G2_C688::ClassMethod2736.11336()"
		ldstr "class G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>
		callvirt instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>::ClassMethod1347<object>()
		ldstr "G2_C688::ClassMethod1347.MI.11340<System.Object>()"
		ldstr "class G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>
		callvirt instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>::ClassMethod1346<object>()
		ldstr "G2_C688::ClassMethod1346.MI.11339<System.Object>()"
		ldstr "class G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>
		callvirt instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>::Method6<object>()
		ldstr "G1_C12::Method6.4867<System.Object>()"
		ldstr "class G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>
		callvirt instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>::Method5()
		ldstr "G1_C12::Method5.4866()"
		ldstr "class G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>
		callvirt instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>::Method4()
		ldstr "G1_C12::Method4.4865()"
		ldstr "class G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>
		callvirt instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>::Method7<object>()
		ldstr "G1_C12::Method7.4863<System.Object>()"
		ldstr "class G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>
		callvirt instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>::ClassMethod1347<object>()
		ldstr "G2_C688::ClassMethod1347.MI.11340<System.Object>()"
		ldstr "class G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>
		callvirt instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>::ClassMethod1346<object>()
		ldstr "G2_C688::ClassMethod1346.MI.11339<System.Object>()"
		ldstr "class G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>
		callvirt instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>::Method6<object>()
		ldstr "G1_C12::Method6.4867<System.Object>()"
		ldstr "class G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>
		callvirt instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>::Method5()
		ldstr "G1_C12::Method5.4866()"
		ldstr "class G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>
		callvirt instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>::Method4()
		ldstr "G1_C12::Method4.4865()"
		ldstr "class G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>
		callvirt instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>::Method7<object>()
		ldstr "G1_C12::Method7.4863<System.Object>()"
		ldstr "class G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1242.IBase2`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>::Method7<object>()
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1242.IBase1`1<class Generated1242.BaseClass0>::Method4()
		ldstr "G1_C12::Method4.4865()"
		ldstr "class IBase1`1<class Generated1242.BaseClass0> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1242.IBase1`1<class Generated1242.BaseClass0>::Method5()
		ldstr "G1_C12::Method5.4866()"
		ldstr "class IBase1`1<class Generated1242.BaseClass0> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1242.IBase1`1<class Generated1242.BaseClass0>::Method6<object>()
		ldstr "G1_C12::Method6.MI.4868<System.Object>()"
		ldstr "class IBase1`1<class Generated1242.BaseClass0> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1242.IBase2`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>::Method7<object>()
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>
		callvirt instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>::ClassMethod2738<object>()
		ldstr "G2_C688::ClassMethod2738.11338<System.Object>()"
		ldstr "class G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>
		callvirt instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>::ClassMethod2737<object>()
		ldstr "G2_C688::ClassMethod2737.11337<System.Object>()"
		ldstr "class G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>
		callvirt instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>::ClassMethod2736()
		ldstr "G2_C688::ClassMethod2736.11336()"
		ldstr "class G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>
		callvirt instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>::ClassMethod1347<object>()
		ldstr "G2_C688::ClassMethod1347.MI.11340<System.Object>()"
		ldstr "class G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>
		callvirt instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>::ClassMethod1346<object>()
		ldstr "G2_C688::ClassMethod1346.MI.11339<System.Object>()"
		ldstr "class G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>
		callvirt instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>::Method6<object>()
		ldstr "G1_C12::Method6.4867<System.Object>()"
		ldstr "class G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>
		callvirt instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>::Method5()
		ldstr "G1_C12::Method5.4866()"
		ldstr "class G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>
		callvirt instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>::Method4()
		ldstr "G1_C12::Method4.4865()"
		ldstr "class G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>
		callvirt instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>::Method7<object>()
		ldstr "G1_C12::Method7.4863<System.Object>()"
		ldstr "class G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>
		callvirt instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>::ClassMethod1347<object>()
		ldstr "G1_C12::ClassMethod1347.4870<System.Object>()"
		ldstr "class G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0> on type class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>
		callvirt instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>::ClassMethod1346<object>()
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()"
		ldstr "class G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0> on type class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>
		callvirt instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>::Method6<object>()
		ldstr "G1_C12::Method6.4867<System.Object>()"
		ldstr "class G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0> on type class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>
		callvirt instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>::Method5()
		ldstr "G1_C12::Method5.4866()"
		ldstr "class G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0> on type class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>
		callvirt instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>::Method4()
		ldstr "G1_C12::Method4.4865()"
		ldstr "class G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0> on type class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>
		callvirt instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>::Method7<object>()
		ldstr "G1_C12::Method7.4863<System.Object>()"
		ldstr "class G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0> on type class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1242.IBase2`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>::Method7<object>()
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0> on type class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1242.IBase1`1<class Generated1242.BaseClass0>::Method4()
		ldstr "G1_C12::Method4.4865()"
		ldstr "class IBase1`1<class Generated1242.BaseClass0> on type class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1242.IBase1`1<class Generated1242.BaseClass0>::Method5()
		ldstr "G1_C12::Method5.4866()"
		ldstr "class IBase1`1<class Generated1242.BaseClass0> on type class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1242.IBase1`1<class Generated1242.BaseClass0>::Method6<object>()
		ldstr "G1_C12::Method6.MI.4868<System.Object>()"
		ldstr "class IBase1`1<class Generated1242.BaseClass0> on type class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1242.IBase2`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>::Method7<object>()
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1> on type class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>
		callvirt instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>::ClassMethod1347<object>()
		ldstr "G1_C12::ClassMethod1347.4870<System.Object>()"
		ldstr "class G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1> on type class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>
		callvirt instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>::ClassMethod1346<object>()
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()"
		ldstr "class G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1> on type class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>
		callvirt instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>::Method6<object>()
		ldstr "G1_C12::Method6.4867<System.Object>()"
		ldstr "class G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1> on type class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>
		callvirt instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>::Method5()
		ldstr "G1_C12::Method5.4866()"
		ldstr "class G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1> on type class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>
		callvirt instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>::Method4()
		ldstr "G1_C12::Method4.4865()"
		ldstr "class G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1> on type class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>
		callvirt instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>::Method7<object>()
		ldstr "G1_C12::Method7.4863<System.Object>()"
		ldstr "class G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1> on type class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1242.IBase2`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>::Method7<object>()
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1> on type class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1242.IBase1`1<class Generated1242.BaseClass0>::Method4()
		ldstr "G1_C12::Method4.4865()"
		ldstr "class IBase1`1<class Generated1242.BaseClass0> on type class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1242.IBase1`1<class Generated1242.BaseClass0>::Method5()
		ldstr "G1_C12::Method5.4866()"
		ldstr "class IBase1`1<class Generated1242.BaseClass0> on type class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1242.IBase1`1<class Generated1242.BaseClass0>::Method6<object>()
		ldstr "G1_C12::Method6.MI.4868<System.Object>()"
		ldstr "class IBase1`1<class Generated1242.BaseClass0> on type class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>
		callvirt instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>::ClassMethod1347<object>()
		ldstr "G1_C12::ClassMethod1347.4870<System.Object>()"
		ldstr "class G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0> on type class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>
		callvirt instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>::ClassMethod1346<object>()
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()"
		ldstr "class G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0> on type class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>
		callvirt instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>::Method6<object>()
		ldstr "G1_C12::Method6.4867<System.Object>()"
		ldstr "class G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0> on type class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>
		callvirt instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>::Method5()
		ldstr "G1_C12::Method5.4866()"
		ldstr "class G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0> on type class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>
		callvirt instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>::Method4()
		ldstr "G1_C12::Method4.4865()"
		ldstr "class G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0> on type class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>
		callvirt instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>::Method7<object>()
		ldstr "G1_C12::Method7.4863<System.Object>()"
		ldstr "class G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0> on type class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1242.IBase2`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>::Method7<object>()
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0> on type class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1242.IBase1`1<class Generated1242.BaseClass0>::Method4()
		ldstr "G1_C12::Method4.4865()"
		ldstr "class IBase1`1<class Generated1242.BaseClass0> on type class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1242.IBase1`1<class Generated1242.BaseClass0>::Method5()
		ldstr "G1_C12::Method5.4866()"
		ldstr "class IBase1`1<class Generated1242.BaseClass0> on type class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1242.IBase1`1<class Generated1242.BaseClass0>::Method6<object>()
		ldstr "G1_C12::Method6.MI.4868<System.Object>()"
		ldstr "class IBase1`1<class Generated1242.BaseClass0> on type class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1242.IBase2`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>::Method7<object>()
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0> on type class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1242.IBase2`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>::Method7<object>()
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1> on type class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1242.IBase2`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>::Method7<object>()
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1> on type class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>
		callvirt instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>::ClassMethod1347<object>()
		ldstr "G1_C12::ClassMethod1347.4870<System.Object>()"
		ldstr "class G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1> on type class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>
		callvirt instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>::ClassMethod1346<object>()
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()"
		ldstr "class G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1> on type class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>
		callvirt instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>::Method6<object>()
		ldstr "G1_C12::Method6.4867<System.Object>()"
		ldstr "class G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1> on type class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>
		callvirt instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>::Method5()
		ldstr "G1_C12::Method5.4866()"
		ldstr "class G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1> on type class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>
		callvirt instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>::Method4()
		ldstr "G1_C12::Method4.4865()"
		ldstr "class G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1> on type class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>
		callvirt instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>::Method7<object>()
		ldstr "G1_C12::Method7.4863<System.Object>()"
		ldstr "class G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1> on type class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1242.IBase2`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>::Method7<object>()
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1> on type class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1242.IBase1`1<class Generated1242.BaseClass0>::Method4()
		ldstr "G1_C12::Method4.4865()"
		ldstr "class IBase1`1<class Generated1242.BaseClass0> on type class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1242.IBase1`1<class Generated1242.BaseClass0>::Method5()
		ldstr "G1_C12::Method5.4866()"
		ldstr "class IBase1`1<class Generated1242.BaseClass0> on type class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1242.IBase1`1<class Generated1242.BaseClass0>::Method6<object>()
		ldstr "G1_C12::Method6.MI.4868<System.Object>()"
		ldstr "class IBase1`1<class Generated1242.BaseClass0> on type class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1242.IBase2`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>::Method7<object>()
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1> on type class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>"
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
		newobj instance void class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C688::ClassMethod1346.MI.11339<System.Object>()#G2_C688::ClassMethod1347.MI.11340<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Generated1242.Generated1242::M.G1_C12.T.T<class Generated1242.BaseClass0,class Generated1242.BaseClass0,class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C688::ClassMethod1346.MI.11339<System.Object>()#G2_C688::ClassMethod1347.MI.11340<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Generated1242.Generated1242::M.G1_C12.A.T<class Generated1242.BaseClass0,class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C688::ClassMethod1346.MI.11339<System.Object>()#G2_C688::ClassMethod1347.MI.11340<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Generated1242.Generated1242::M.G1_C12.A.A<class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1714::Method7.17596<System.Object>()#"
		call void Generated1242.Generated1242::M.IBase2.T.T<class Generated1242.BaseClass0,class Generated1242.BaseClass0,class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G3_C1714::Method7.17596<System.Object>()#"
		call void Generated1242.Generated1242::M.IBase2.A.T<class Generated1242.BaseClass0,class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G3_C1714::Method7.17596<System.Object>()#"
		call void Generated1242.Generated1242::M.IBase2.A.A<class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.MI.4868<System.Object>()#"
		call void Generated1242.Generated1242::M.IBase1.T<class Generated1242.BaseClass0,class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.MI.4868<System.Object>()#"
		call void Generated1242.Generated1242::M.IBase1.A<class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1714::Method7.17596<System.Object>()#"
		call void Generated1242.Generated1242::M.IBase2.T.T<class Generated1242.BaseClass0,class Generated1242.BaseClass1,class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G3_C1714::Method7.17596<System.Object>()#"
		call void Generated1242.Generated1242::M.IBase2.A.T<class Generated1242.BaseClass1,class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G3_C1714::Method7.17596<System.Object>()#"
		call void Generated1242.Generated1242::M.IBase2.A.B<class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C688::ClassMethod1346.MI.11339<System.Object>()#G2_C688::ClassMethod1347.MI.11340<System.Object>()#G2_C688::ClassMethod2736.11336()#G2_C688::ClassMethod2737.11337<System.Object>()#G2_C688::ClassMethod2738.11338<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Generated1242.Generated1242::M.G2_C688.T.T<class Generated1242.BaseClass1,class Generated1242.BaseClass0,class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C688::ClassMethod1346.MI.11339<System.Object>()#G2_C688::ClassMethod1347.MI.11340<System.Object>()#G2_C688::ClassMethod2736.11336()#G2_C688::ClassMethod2737.11337<System.Object>()#G2_C688::ClassMethod2738.11338<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Generated1242.Generated1242::M.G2_C688.B.T<class Generated1242.BaseClass0,class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C688::ClassMethod1346.MI.11339<System.Object>()#G2_C688::ClassMethod1347.MI.11340<System.Object>()#G2_C688::ClassMethod2736.11336()#G2_C688::ClassMethod2737.11337<System.Object>()#G2_C688::ClassMethod2738.11338<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Generated1242.Generated1242::M.G2_C688.B.A<class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C688::ClassMethod1346.MI.11339<System.Object>()#G2_C688::ClassMethod1347.MI.11340<System.Object>()#G2_C688::ClassMethod2736.11336()#G2_C688::ClassMethod2737.11337<System.Object>()#G2_C688::ClassMethod2738.11338<System.Object>()#G3_C1714::ClassMethod4826.17597<System.Object>()#G3_C1714::ClassMethod4827.17598<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G3_C1714::Method7.17596<System.Object>()#"
		call void Generated1242.Generated1242::M.G3_C1714.T<class Generated1242.BaseClass0,class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C688::ClassMethod1346.MI.11339<System.Object>()#G2_C688::ClassMethod1347.MI.11340<System.Object>()#G2_C688::ClassMethod2736.11336()#G2_C688::ClassMethod2737.11337<System.Object>()#G2_C688::ClassMethod2738.11338<System.Object>()#G3_C1714::ClassMethod4826.17597<System.Object>()#G3_C1714::ClassMethod4827.17598<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G3_C1714::Method7.17596<System.Object>()#"
		call void Generated1242.Generated1242::M.G3_C1714.A<class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1714::Method7.17596<System.Object>()#"
		call void Generated1242.Generated1242::M.IBase2.T.T<class Generated1242.BaseClass1,class Generated1242.BaseClass0,class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G3_C1714::Method7.17596<System.Object>()#"
		call void Generated1242.Generated1242::M.IBase2.B.T<class Generated1242.BaseClass0,class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G3_C1714::Method7.17596<System.Object>()#"
		call void Generated1242.Generated1242::M.IBase2.B.A<class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1714::Method7.17596<System.Object>()#"
		call void Generated1242.Generated1242::M.IBase2.T.T<class Generated1242.BaseClass1,class Generated1242.BaseClass1,class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G3_C1714::Method7.17596<System.Object>()#"
		call void Generated1242.Generated1242::M.IBase2.B.T<class Generated1242.BaseClass1,class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G3_C1714::Method7.17596<System.Object>()#"
		call void Generated1242.Generated1242::M.IBase2.B.B<class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>>(!!0,string)
		newobj instance void class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C688::ClassMethod1346.MI.11339<System.Object>()#G2_C688::ClassMethod1347.MI.11340<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Generated1242.Generated1242::M.G1_C12.T.T<class Generated1242.BaseClass1,class Generated1242.BaseClass1,class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C688::ClassMethod1346.MI.11339<System.Object>()#G2_C688::ClassMethod1347.MI.11340<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Generated1242.Generated1242::M.G1_C12.B.T<class Generated1242.BaseClass1,class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C688::ClassMethod1346.MI.11339<System.Object>()#G2_C688::ClassMethod1347.MI.11340<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Generated1242.Generated1242::M.G1_C12.B.B<class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1714::Method7.17596<System.Object>()#"
		call void Generated1242.Generated1242::M.IBase2.T.T<class Generated1242.BaseClass1,class Generated1242.BaseClass1,class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G3_C1714::Method7.17596<System.Object>()#"
		call void Generated1242.Generated1242::M.IBase2.B.T<class Generated1242.BaseClass1,class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1714::Method7.17596<System.Object>()#"
		call void Generated1242.Generated1242::M.IBase2.B.B<class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.MI.4868<System.Object>()#"
		call void Generated1242.Generated1242::M.IBase1.T<class Generated1242.BaseClass0,class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.MI.4868<System.Object>()#"
		call void Generated1242.Generated1242::M.IBase1.A<class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1714::Method7.17596<System.Object>()#"
		call void Generated1242.Generated1242::M.IBase2.T.T<class Generated1242.BaseClass0,class Generated1242.BaseClass1,class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G3_C1714::Method7.17596<System.Object>()#"
		call void Generated1242.Generated1242::M.IBase2.A.T<class Generated1242.BaseClass1,class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1714::Method7.17596<System.Object>()#"
		call void Generated1242.Generated1242::M.IBase2.A.B<class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C688::ClassMethod1346.MI.11339<System.Object>()#G2_C688::ClassMethod1347.MI.11340<System.Object>()#G2_C688::ClassMethod2736.11336()#G2_C688::ClassMethod2737.11337<System.Object>()#G2_C688::ClassMethod2738.11338<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Generated1242.Generated1242::M.G2_C688.T.T<class Generated1242.BaseClass1,class Generated1242.BaseClass1,class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C688::ClassMethod1346.MI.11339<System.Object>()#G2_C688::ClassMethod1347.MI.11340<System.Object>()#G2_C688::ClassMethod2736.11336()#G2_C688::ClassMethod2737.11337<System.Object>()#G2_C688::ClassMethod2738.11338<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Generated1242.Generated1242::M.G2_C688.B.T<class Generated1242.BaseClass1,class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C688::ClassMethod1346.MI.11339<System.Object>()#G2_C688::ClassMethod1347.MI.11340<System.Object>()#G2_C688::ClassMethod2736.11336()#G2_C688::ClassMethod2737.11337<System.Object>()#G2_C688::ClassMethod2738.11338<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Generated1242.Generated1242::M.G2_C688.B.B<class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C688::ClassMethod1346.MI.11339<System.Object>()#G2_C688::ClassMethod1347.MI.11340<System.Object>()#G2_C688::ClassMethod2736.11336()#G2_C688::ClassMethod2737.11337<System.Object>()#G2_C688::ClassMethod2738.11338<System.Object>()#G3_C1714::ClassMethod4826.17597<System.Object>()#G3_C1714::ClassMethod4827.17598<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G3_C1714::Method7.17596<System.Object>()#"
		call void Generated1242.Generated1242::M.G3_C1714.T<class Generated1242.BaseClass1,class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C688::ClassMethod1346.MI.11339<System.Object>()#G2_C688::ClassMethod1347.MI.11340<System.Object>()#G2_C688::ClassMethod2736.11336()#G2_C688::ClassMethod2737.11337<System.Object>()#G2_C688::ClassMethod2738.11338<System.Object>()#G3_C1714::ClassMethod4826.17597<System.Object>()#G3_C1714::ClassMethod4827.17598<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G3_C1714::Method7.17596<System.Object>()#"
		call void Generated1242.Generated1242::M.G3_C1714.B<class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>>(!!0,string)
		newobj instance void class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C688::ClassMethod1346.MI.11339<System.Object>()#G2_C688::ClassMethod1347.MI.11340<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Generated1242.Generated1242::M.G1_C12.T.T<class Generated1242.BaseClass0,class Generated1242.BaseClass0,class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C688::ClassMethod1346.MI.11339<System.Object>()#G2_C688::ClassMethod1347.MI.11340<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Generated1242.Generated1242::M.G1_C12.A.T<class Generated1242.BaseClass0,class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C688::ClassMethod1346.MI.11339<System.Object>()#G2_C688::ClassMethod1347.MI.11340<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Generated1242.Generated1242::M.G1_C12.A.A<class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated1242.Generated1242::M.IBase2.T.T<class Generated1242.BaseClass0,class Generated1242.BaseClass0,class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated1242.Generated1242::M.IBase2.A.T<class Generated1242.BaseClass0,class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated1242.Generated1242::M.IBase2.A.A<class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.MI.4868<System.Object>()#"
		call void Generated1242.Generated1242::M.IBase1.T<class Generated1242.BaseClass0,class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.MI.4868<System.Object>()#"
		call void Generated1242.Generated1242::M.IBase1.A<class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated1242.Generated1242::M.IBase2.T.T<class Generated1242.BaseClass0,class Generated1242.BaseClass1,class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated1242.Generated1242::M.IBase2.A.T<class Generated1242.BaseClass1,class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated1242.Generated1242::M.IBase2.A.B<class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C688::ClassMethod1346.MI.11339<System.Object>()#G2_C688::ClassMethod1347.MI.11340<System.Object>()#G2_C688::ClassMethod2736.11336()#G2_C688::ClassMethod2737.11337<System.Object>()#G2_C688::ClassMethod2738.11338<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Generated1242.Generated1242::M.G2_C688.T.T<class Generated1242.BaseClass0,class Generated1242.BaseClass0,class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C688::ClassMethod1346.MI.11339<System.Object>()#G2_C688::ClassMethod1347.MI.11340<System.Object>()#G2_C688::ClassMethod2736.11336()#G2_C688::ClassMethod2737.11337<System.Object>()#G2_C688::ClassMethod2738.11338<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Generated1242.Generated1242::M.G2_C688.A.T<class Generated1242.BaseClass0,class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C688::ClassMethod1346.MI.11339<System.Object>()#G2_C688::ClassMethod1347.MI.11340<System.Object>()#G2_C688::ClassMethod2736.11336()#G2_C688::ClassMethod2737.11337<System.Object>()#G2_C688::ClassMethod2738.11338<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Generated1242.Generated1242::M.G2_C688.A.A<class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>>(!!0,string)
		newobj instance void class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C688::ClassMethod1346.MI.11339<System.Object>()#G2_C688::ClassMethod1347.MI.11340<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Generated1242.Generated1242::M.G1_C12.T.T<class Generated1242.BaseClass1,class Generated1242.BaseClass1,class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C688::ClassMethod1346.MI.11339<System.Object>()#G2_C688::ClassMethod1347.MI.11340<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Generated1242.Generated1242::M.G1_C12.B.T<class Generated1242.BaseClass1,class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C688::ClassMethod1346.MI.11339<System.Object>()#G2_C688::ClassMethod1347.MI.11340<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Generated1242.Generated1242::M.G1_C12.B.B<class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated1242.Generated1242::M.IBase2.T.T<class Generated1242.BaseClass1,class Generated1242.BaseClass1,class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated1242.Generated1242::M.IBase2.B.T<class Generated1242.BaseClass1,class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated1242.Generated1242::M.IBase2.B.B<class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.MI.4868<System.Object>()#"
		call void Generated1242.Generated1242::M.IBase1.T<class Generated1242.BaseClass0,class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.MI.4868<System.Object>()#"
		call void Generated1242.Generated1242::M.IBase1.A<class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated1242.Generated1242::M.IBase2.T.T<class Generated1242.BaseClass0,class Generated1242.BaseClass1,class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated1242.Generated1242::M.IBase2.A.T<class Generated1242.BaseClass1,class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated1242.Generated1242::M.IBase2.A.B<class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C688::ClassMethod1346.MI.11339<System.Object>()#G2_C688::ClassMethod1347.MI.11340<System.Object>()#G2_C688::ClassMethod2736.11336()#G2_C688::ClassMethod2737.11337<System.Object>()#G2_C688::ClassMethod2738.11338<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Generated1242.Generated1242::M.G2_C688.T.T<class Generated1242.BaseClass0,class Generated1242.BaseClass1,class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C688::ClassMethod1346.MI.11339<System.Object>()#G2_C688::ClassMethod1347.MI.11340<System.Object>()#G2_C688::ClassMethod2736.11336()#G2_C688::ClassMethod2737.11337<System.Object>()#G2_C688::ClassMethod2738.11338<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Generated1242.Generated1242::M.G2_C688.A.T<class Generated1242.BaseClass1,class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C688::ClassMethod1346.MI.11339<System.Object>()#G2_C688::ClassMethod1347.MI.11340<System.Object>()#G2_C688::ClassMethod2736.11336()#G2_C688::ClassMethod2737.11337<System.Object>()#G2_C688::ClassMethod2738.11338<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Generated1242.Generated1242::M.G2_C688.A.B<class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>>(!!0,string)
		newobj instance void class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C688::ClassMethod1346.MI.11339<System.Object>()#G2_C688::ClassMethod1347.MI.11340<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Generated1242.Generated1242::M.G1_C12.T.T<class Generated1242.BaseClass0,class Generated1242.BaseClass0,class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C688::ClassMethod1346.MI.11339<System.Object>()#G2_C688::ClassMethod1347.MI.11340<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Generated1242.Generated1242::M.G1_C12.A.T<class Generated1242.BaseClass0,class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C688::ClassMethod1346.MI.11339<System.Object>()#G2_C688::ClassMethod1347.MI.11340<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Generated1242.Generated1242::M.G1_C12.A.A<class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated1242.Generated1242::M.IBase2.T.T<class Generated1242.BaseClass0,class Generated1242.BaseClass0,class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated1242.Generated1242::M.IBase2.A.T<class Generated1242.BaseClass0,class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated1242.Generated1242::M.IBase2.A.A<class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.MI.4868<System.Object>()#"
		call void Generated1242.Generated1242::M.IBase1.T<class Generated1242.BaseClass0,class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.MI.4868<System.Object>()#"
		call void Generated1242.Generated1242::M.IBase1.A<class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated1242.Generated1242::M.IBase2.T.T<class Generated1242.BaseClass0,class Generated1242.BaseClass1,class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated1242.Generated1242::M.IBase2.A.T<class Generated1242.BaseClass1,class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated1242.Generated1242::M.IBase2.A.B<class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C688::ClassMethod1346.MI.11339<System.Object>()#G2_C688::ClassMethod1347.MI.11340<System.Object>()#G2_C688::ClassMethod2736.11336()#G2_C688::ClassMethod2737.11337<System.Object>()#G2_C688::ClassMethod2738.11338<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Generated1242.Generated1242::M.G2_C688.T.T<class Generated1242.BaseClass1,class Generated1242.BaseClass0,class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C688::ClassMethod1346.MI.11339<System.Object>()#G2_C688::ClassMethod1347.MI.11340<System.Object>()#G2_C688::ClassMethod2736.11336()#G2_C688::ClassMethod2737.11337<System.Object>()#G2_C688::ClassMethod2738.11338<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Generated1242.Generated1242::M.G2_C688.B.T<class Generated1242.BaseClass0,class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C688::ClassMethod1346.MI.11339<System.Object>()#G2_C688::ClassMethod1347.MI.11340<System.Object>()#G2_C688::ClassMethod2736.11336()#G2_C688::ClassMethod2737.11337<System.Object>()#G2_C688::ClassMethod2738.11338<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Generated1242.Generated1242::M.G2_C688.B.A<class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>>(!!0,string)
		newobj instance void class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C688::ClassMethod1346.MI.11339<System.Object>()#G2_C688::ClassMethod1347.MI.11340<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Generated1242.Generated1242::M.G1_C12.T.T<class Generated1242.BaseClass1,class Generated1242.BaseClass1,class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C688::ClassMethod1346.MI.11339<System.Object>()#G2_C688::ClassMethod1347.MI.11340<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Generated1242.Generated1242::M.G1_C12.B.T<class Generated1242.BaseClass1,class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C688::ClassMethod1346.MI.11339<System.Object>()#G2_C688::ClassMethod1347.MI.11340<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Generated1242.Generated1242::M.G1_C12.B.B<class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated1242.Generated1242::M.IBase2.T.T<class Generated1242.BaseClass1,class Generated1242.BaseClass1,class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated1242.Generated1242::M.IBase2.B.T<class Generated1242.BaseClass1,class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated1242.Generated1242::M.IBase2.B.B<class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.MI.4868<System.Object>()#"
		call void Generated1242.Generated1242::M.IBase1.T<class Generated1242.BaseClass0,class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.MI.4868<System.Object>()#"
		call void Generated1242.Generated1242::M.IBase1.A<class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated1242.Generated1242::M.IBase2.T.T<class Generated1242.BaseClass0,class Generated1242.BaseClass1,class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated1242.Generated1242::M.IBase2.A.T<class Generated1242.BaseClass1,class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated1242.Generated1242::M.IBase2.A.B<class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C688::ClassMethod1346.MI.11339<System.Object>()#G2_C688::ClassMethod1347.MI.11340<System.Object>()#G2_C688::ClassMethod2736.11336()#G2_C688::ClassMethod2737.11337<System.Object>()#G2_C688::ClassMethod2738.11338<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Generated1242.Generated1242::M.G2_C688.T.T<class Generated1242.BaseClass1,class Generated1242.BaseClass1,class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C688::ClassMethod1346.MI.11339<System.Object>()#G2_C688::ClassMethod1347.MI.11340<System.Object>()#G2_C688::ClassMethod2736.11336()#G2_C688::ClassMethod2737.11337<System.Object>()#G2_C688::ClassMethod2738.11338<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Generated1242.Generated1242::M.G2_C688.B.T<class Generated1242.BaseClass1,class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C688::ClassMethod1346.MI.11339<System.Object>()#G2_C688::ClassMethod1347.MI.11340<System.Object>()#G2_C688::ClassMethod2736.11336()#G2_C688::ClassMethod2737.11337<System.Object>()#G2_C688::ClassMethod2738.11338<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Generated1242.Generated1242::M.G2_C688.B.B<class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>>(!!0,string)
		newobj instance void class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()#G1_C12::ClassMethod1347.4870<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Generated1242.Generated1242::M.G1_C12.T.T<class Generated1242.BaseClass0,class Generated1242.BaseClass0,class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()#G1_C12::ClassMethod1347.4870<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Generated1242.Generated1242::M.G1_C12.A.T<class Generated1242.BaseClass0,class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()#G1_C12::ClassMethod1347.4870<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Generated1242.Generated1242::M.G1_C12.A.A<class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated1242.Generated1242::M.IBase2.T.T<class Generated1242.BaseClass0,class Generated1242.BaseClass0,class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated1242.Generated1242::M.IBase2.A.T<class Generated1242.BaseClass0,class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated1242.Generated1242::M.IBase2.A.A<class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.MI.4868<System.Object>()#"
		call void Generated1242.Generated1242::M.IBase1.T<class Generated1242.BaseClass0,class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.MI.4868<System.Object>()#"
		call void Generated1242.Generated1242::M.IBase1.A<class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated1242.Generated1242::M.IBase2.T.T<class Generated1242.BaseClass0,class Generated1242.BaseClass1,class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated1242.Generated1242::M.IBase2.A.T<class Generated1242.BaseClass1,class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated1242.Generated1242::M.IBase2.A.B<class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>>(!!0,string)
		newobj instance void class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()#G1_C12::ClassMethod1347.4870<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Generated1242.Generated1242::M.G1_C12.T.T<class Generated1242.BaseClass0,class Generated1242.BaseClass1,class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()#G1_C12::ClassMethod1347.4870<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Generated1242.Generated1242::M.G1_C12.A.T<class Generated1242.BaseClass1,class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()#G1_C12::ClassMethod1347.4870<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Generated1242.Generated1242::M.G1_C12.A.B<class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated1242.Generated1242::M.IBase2.T.T<class Generated1242.BaseClass0,class Generated1242.BaseClass1,class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated1242.Generated1242::M.IBase2.A.T<class Generated1242.BaseClass1,class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated1242.Generated1242::M.IBase2.A.B<class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.MI.4868<System.Object>()#"
		call void Generated1242.Generated1242::M.IBase1.T<class Generated1242.BaseClass0,class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.MI.4868<System.Object>()#"
		call void Generated1242.Generated1242::M.IBase1.A<class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>>(!!0,string)
		newobj instance void class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()#G1_C12::ClassMethod1347.4870<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Generated1242.Generated1242::M.G1_C12.T.T<class Generated1242.BaseClass1,class Generated1242.BaseClass0,class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()#G1_C12::ClassMethod1347.4870<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Generated1242.Generated1242::M.G1_C12.B.T<class Generated1242.BaseClass0,class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()#G1_C12::ClassMethod1347.4870<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Generated1242.Generated1242::M.G1_C12.B.A<class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated1242.Generated1242::M.IBase2.T.T<class Generated1242.BaseClass1,class Generated1242.BaseClass0,class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated1242.Generated1242::M.IBase2.B.T<class Generated1242.BaseClass0,class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated1242.Generated1242::M.IBase2.B.A<class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.MI.4868<System.Object>()#"
		call void Generated1242.Generated1242::M.IBase1.T<class Generated1242.BaseClass0,class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.MI.4868<System.Object>()#"
		call void Generated1242.Generated1242::M.IBase1.A<class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated1242.Generated1242::M.IBase2.T.T<class Generated1242.BaseClass0,class Generated1242.BaseClass0,class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated1242.Generated1242::M.IBase2.A.T<class Generated1242.BaseClass0,class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated1242.Generated1242::M.IBase2.A.A<class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated1242.Generated1242::M.IBase2.T.T<class Generated1242.BaseClass0,class Generated1242.BaseClass1,class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated1242.Generated1242::M.IBase2.A.T<class Generated1242.BaseClass1,class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated1242.Generated1242::M.IBase2.A.B<class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated1242.Generated1242::M.IBase2.T.T<class Generated1242.BaseClass1,class Generated1242.BaseClass1,class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated1242.Generated1242::M.IBase2.B.T<class Generated1242.BaseClass1,class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated1242.Generated1242::M.IBase2.B.B<class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>>(!!0,string)
		newobj instance void class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()#G1_C12::ClassMethod1347.4870<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Generated1242.Generated1242::M.G1_C12.T.T<class Generated1242.BaseClass1,class Generated1242.BaseClass1,class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()#G1_C12::ClassMethod1347.4870<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Generated1242.Generated1242::M.G1_C12.B.T<class Generated1242.BaseClass1,class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()#G1_C12::ClassMethod1347.4870<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Generated1242.Generated1242::M.G1_C12.B.B<class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated1242.Generated1242::M.IBase2.T.T<class Generated1242.BaseClass1,class Generated1242.BaseClass1,class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated1242.Generated1242::M.IBase2.B.T<class Generated1242.BaseClass1,class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated1242.Generated1242::M.IBase2.B.B<class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.MI.4868<System.Object>()#"
		call void Generated1242.Generated1242::M.IBase1.T<class Generated1242.BaseClass0,class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.MI.4868<System.Object>()#"
		call void Generated1242.Generated1242::M.IBase1.A<class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated1242.Generated1242::M.IBase2.T.T<class Generated1242.BaseClass0,class Generated1242.BaseClass1,class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated1242.Generated1242::M.IBase2.A.T<class Generated1242.BaseClass1,class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated1242.Generated1242::M.IBase2.A.B<class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>>(!!0,string)
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
		newobj instance void class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>::ClassMethod1347<object>()
		calli default string(class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>)
		ldstr "G2_C688::ClassMethod1347.MI.11340<System.Object>()"
		ldstr "class G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0> on type class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>::ClassMethod1346<object>()
		calli default string(class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>)
		ldstr "G2_C688::ClassMethod1346.MI.11339<System.Object>()"
		ldstr "class G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0> on type class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>::Method6<object>()
		calli default string(class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>)
		ldstr "G1_C12::Method6.4867<System.Object>()"
		ldstr "class G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0> on type class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>::Method5()
		calli default string(class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>)
		ldstr "G1_C12::Method5.4866()"
		ldstr "class G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0> on type class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>::Method4()
		calli default string(class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>)
		ldstr "G1_C12::Method4.4865()"
		ldstr "class G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0> on type class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>::Method7<object>()
		calli default string(class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>)
		ldstr "G1_C12::Method7.4863<System.Object>()"
		ldstr "class G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0> on type class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1242.IBase2`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>::Method7<object>()
		calli default string(class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>)
		ldstr "G3_C1714::Method7.17596<System.Object>()"
		ldstr "class IBase2`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0> on type class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1242.IBase1`1<class Generated1242.BaseClass0>::Method4()
		calli default string(class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>)
		ldstr "G1_C12::Method4.4865()"
		ldstr "class IBase1`1<class Generated1242.BaseClass0> on type class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1242.IBase1`1<class Generated1242.BaseClass0>::Method5()
		calli default string(class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>)
		ldstr "G1_C12::Method5.4866()"
		ldstr "class IBase1`1<class Generated1242.BaseClass0> on type class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1242.IBase1`1<class Generated1242.BaseClass0>::Method6<object>()
		calli default string(class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>)
		ldstr "G1_C12::Method6.MI.4868<System.Object>()"
		ldstr "class IBase1`1<class Generated1242.BaseClass0> on type class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1242.IBase2`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>::Method7<object>()
		calli default string(class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>)
		ldstr "G3_C1714::Method7.17596<System.Object>()"
		ldstr "class IBase2`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1> on type class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>::ClassMethod2738<object>()
		calli default string(class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>)
		ldstr "G2_C688::ClassMethod2738.11338<System.Object>()"
		ldstr "class G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0> on type class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>::ClassMethod2737<object>()
		calli default string(class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>)
		ldstr "G2_C688::ClassMethod2737.11337<System.Object>()"
		ldstr "class G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0> on type class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>::ClassMethod2736()
		calli default string(class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>)
		ldstr "G2_C688::ClassMethod2736.11336()"
		ldstr "class G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0> on type class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>::ClassMethod1347<object>()
		calli default string(class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>)
		ldstr "G2_C688::ClassMethod1347.MI.11340<System.Object>()"
		ldstr "class G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0> on type class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>::ClassMethod1346<object>()
		calli default string(class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>)
		ldstr "G2_C688::ClassMethod1346.MI.11339<System.Object>()"
		ldstr "class G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0> on type class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>::Method6<object>()
		calli default string(class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>)
		ldstr "G1_C12::Method6.4867<System.Object>()"
		ldstr "class G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0> on type class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>::Method5()
		calli default string(class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>)
		ldstr "G1_C12::Method5.4866()"
		ldstr "class G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0> on type class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>::Method4()
		calli default string(class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>)
		ldstr "G1_C12::Method4.4865()"
		ldstr "class G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0> on type class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>::Method7<object>()
		calli default string(class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>)
		ldstr "G1_C12::Method7.4863<System.Object>()"
		ldstr "class G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0> on type class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>::ClassMethod4827<object>()
		calli default string(class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>)
		ldstr "G3_C1714::ClassMethod4827.17598<System.Object>()"
		ldstr "class G3_C1714`1<class Generated1242.BaseClass0> on type class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>::ClassMethod4826<object>()
		calli default string(class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>)
		ldstr "G3_C1714::ClassMethod4826.17597<System.Object>()"
		ldstr "class G3_C1714`1<class Generated1242.BaseClass0> on type class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>::Method7<object>()
		calli default string(class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>)
		ldstr "G3_C1714::Method7.17596<System.Object>()"
		ldstr "class G3_C1714`1<class Generated1242.BaseClass0> on type class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>::ClassMethod2738<object>()
		calli default string(class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>)
		ldstr "G2_C688::ClassMethod2738.11338<System.Object>()"
		ldstr "class G3_C1714`1<class Generated1242.BaseClass0> on type class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>::ClassMethod2737<object>()
		calli default string(class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>)
		ldstr "G2_C688::ClassMethod2737.11337<System.Object>()"
		ldstr "class G3_C1714`1<class Generated1242.BaseClass0> on type class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>::ClassMethod2736()
		calli default string(class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>)
		ldstr "G2_C688::ClassMethod2736.11336()"
		ldstr "class G3_C1714`1<class Generated1242.BaseClass0> on type class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>::ClassMethod1347<object>()
		calli default string(class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>)
		ldstr "G2_C688::ClassMethod1347.MI.11340<System.Object>()"
		ldstr "class G3_C1714`1<class Generated1242.BaseClass0> on type class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>::ClassMethod1346<object>()
		calli default string(class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>)
		ldstr "G2_C688::ClassMethod1346.MI.11339<System.Object>()"
		ldstr "class G3_C1714`1<class Generated1242.BaseClass0> on type class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>::Method6<object>()
		calli default string(class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>)
		ldstr "G1_C12::Method6.4867<System.Object>()"
		ldstr "class G3_C1714`1<class Generated1242.BaseClass0> on type class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>::Method5()
		calli default string(class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>)
		ldstr "G1_C12::Method5.4866()"
		ldstr "class G3_C1714`1<class Generated1242.BaseClass0> on type class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>::Method4()
		calli default string(class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>)
		ldstr "G1_C12::Method4.4865()"
		ldstr "class G3_C1714`1<class Generated1242.BaseClass0> on type class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1242.IBase2`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>::Method7<object>()
		calli default string(class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>)
		ldstr "G3_C1714::Method7.17596<System.Object>()"
		ldstr "class IBase2`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0> on type class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1242.IBase2`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>::Method7<object>()
		calli default string(class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>)
		ldstr "G3_C1714::Method7.17596<System.Object>()"
		ldstr "class IBase2`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1> on type class Generated1242.G3_C1714`1<class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>::ClassMethod1347<object>()
		calli default string(class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>)
		ldstr "G2_C688::ClassMethod1347.MI.11340<System.Object>()"
		ldstr "class G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1> on type class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>::ClassMethod1346<object>()
		calli default string(class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>)
		ldstr "G2_C688::ClassMethod1346.MI.11339<System.Object>()"
		ldstr "class G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1> on type class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>::Method6<object>()
		calli default string(class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>)
		ldstr "G1_C12::Method6.4867<System.Object>()"
		ldstr "class G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1> on type class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>::Method5()
		calli default string(class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>)
		ldstr "G1_C12::Method5.4866()"
		ldstr "class G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1> on type class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>::Method4()
		calli default string(class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>)
		ldstr "G1_C12::Method4.4865()"
		ldstr "class G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1> on type class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>::Method7<object>()
		calli default string(class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>)
		ldstr "G1_C12::Method7.4863<System.Object>()"
		ldstr "class G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1> on type class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1242.IBase2`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>::Method7<object>()
		calli default string(class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>)
		ldstr "G3_C1714::Method7.17596<System.Object>()"
		ldstr "class IBase2`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1> on type class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1242.IBase1`1<class Generated1242.BaseClass0>::Method4()
		calli default string(class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>)
		ldstr "G1_C12::Method4.4865()"
		ldstr "class IBase1`1<class Generated1242.BaseClass0> on type class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1242.IBase1`1<class Generated1242.BaseClass0>::Method5()
		calli default string(class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>)
		ldstr "G1_C12::Method5.4866()"
		ldstr "class IBase1`1<class Generated1242.BaseClass0> on type class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1242.IBase1`1<class Generated1242.BaseClass0>::Method6<object>()
		calli default string(class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>)
		ldstr "G1_C12::Method6.MI.4868<System.Object>()"
		ldstr "class IBase1`1<class Generated1242.BaseClass0> on type class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1242.IBase2`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>::Method7<object>()
		calli default string(class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>)
		ldstr "G3_C1714::Method7.17596<System.Object>()"
		ldstr "class IBase2`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1> on type class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>::ClassMethod2738<object>()
		calli default string(class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>)
		ldstr "G2_C688::ClassMethod2738.11338<System.Object>()"
		ldstr "class G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1> on type class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>::ClassMethod2737<object>()
		calli default string(class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>)
		ldstr "G2_C688::ClassMethod2737.11337<System.Object>()"
		ldstr "class G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1> on type class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>::ClassMethod2736()
		calli default string(class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>)
		ldstr "G2_C688::ClassMethod2736.11336()"
		ldstr "class G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1> on type class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>::ClassMethod1347<object>()
		calli default string(class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>)
		ldstr "G2_C688::ClassMethod1347.MI.11340<System.Object>()"
		ldstr "class G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1> on type class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>::ClassMethod1346<object>()
		calli default string(class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>)
		ldstr "G2_C688::ClassMethod1346.MI.11339<System.Object>()"
		ldstr "class G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1> on type class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>::Method6<object>()
		calli default string(class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>)
		ldstr "G1_C12::Method6.4867<System.Object>()"
		ldstr "class G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1> on type class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>::Method5()
		calli default string(class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>)
		ldstr "G1_C12::Method5.4866()"
		ldstr "class G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1> on type class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>::Method4()
		calli default string(class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>)
		ldstr "G1_C12::Method4.4865()"
		ldstr "class G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1> on type class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>::Method7<object>()
		calli default string(class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>)
		ldstr "G1_C12::Method7.4863<System.Object>()"
		ldstr "class G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1> on type class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>::ClassMethod4827<object>()
		calli default string(class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>)
		ldstr "G3_C1714::ClassMethod4827.17598<System.Object>()"
		ldstr "class G3_C1714`1<class Generated1242.BaseClass1> on type class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>::ClassMethod4826<object>()
		calli default string(class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>)
		ldstr "G3_C1714::ClassMethod4826.17597<System.Object>()"
		ldstr "class G3_C1714`1<class Generated1242.BaseClass1> on type class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>::Method7<object>()
		calli default string(class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>)
		ldstr "G3_C1714::Method7.17596<System.Object>()"
		ldstr "class G3_C1714`1<class Generated1242.BaseClass1> on type class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>::ClassMethod2738<object>()
		calli default string(class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>)
		ldstr "G2_C688::ClassMethod2738.11338<System.Object>()"
		ldstr "class G3_C1714`1<class Generated1242.BaseClass1> on type class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>::ClassMethod2737<object>()
		calli default string(class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>)
		ldstr "G2_C688::ClassMethod2737.11337<System.Object>()"
		ldstr "class G3_C1714`1<class Generated1242.BaseClass1> on type class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>::ClassMethod2736()
		calli default string(class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>)
		ldstr "G2_C688::ClassMethod2736.11336()"
		ldstr "class G3_C1714`1<class Generated1242.BaseClass1> on type class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>::ClassMethod1347<object>()
		calli default string(class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>)
		ldstr "G2_C688::ClassMethod1347.MI.11340<System.Object>()"
		ldstr "class G3_C1714`1<class Generated1242.BaseClass1> on type class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>::ClassMethod1346<object>()
		calli default string(class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>)
		ldstr "G2_C688::ClassMethod1346.MI.11339<System.Object>()"
		ldstr "class G3_C1714`1<class Generated1242.BaseClass1> on type class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>::Method6<object>()
		calli default string(class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>)
		ldstr "G1_C12::Method6.4867<System.Object>()"
		ldstr "class G3_C1714`1<class Generated1242.BaseClass1> on type class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>::Method5()
		calli default string(class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>)
		ldstr "G1_C12::Method5.4866()"
		ldstr "class G3_C1714`1<class Generated1242.BaseClass1> on type class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>::Method4()
		calli default string(class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>)
		ldstr "G1_C12::Method4.4865()"
		ldstr "class G3_C1714`1<class Generated1242.BaseClass1> on type class Generated1242.G3_C1714`1<class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>::ClassMethod1347<object>()
		calli default string(class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>)
		ldstr "G2_C688::ClassMethod1347.MI.11340<System.Object>()"
		ldstr "class G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>::ClassMethod1346<object>()
		calli default string(class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>)
		ldstr "G2_C688::ClassMethod1346.MI.11339<System.Object>()"
		ldstr "class G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>::Method6<object>()
		calli default string(class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>)
		ldstr "G1_C12::Method6.4867<System.Object>()"
		ldstr "class G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>::Method5()
		calli default string(class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>)
		ldstr "G1_C12::Method5.4866()"
		ldstr "class G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>::Method4()
		calli default string(class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>)
		ldstr "G1_C12::Method4.4865()"
		ldstr "class G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>::Method7<object>()
		calli default string(class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>)
		ldstr "G1_C12::Method7.4863<System.Object>()"
		ldstr "class G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1242.IBase2`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>::Method7<object>()
		calli default string(class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>)
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1242.IBase1`1<class Generated1242.BaseClass0>::Method4()
		calli default string(class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>)
		ldstr "G1_C12::Method4.4865()"
		ldstr "class IBase1`1<class Generated1242.BaseClass0> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1242.IBase1`1<class Generated1242.BaseClass0>::Method5()
		calli default string(class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>)
		ldstr "G1_C12::Method5.4866()"
		ldstr "class IBase1`1<class Generated1242.BaseClass0> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1242.IBase1`1<class Generated1242.BaseClass0>::Method6<object>()
		calli default string(class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>)
		ldstr "G1_C12::Method6.MI.4868<System.Object>()"
		ldstr "class IBase1`1<class Generated1242.BaseClass0> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1242.IBase2`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>::Method7<object>()
		calli default string(class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>)
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>::ClassMethod2738<object>()
		calli default string(class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>)
		ldstr "G2_C688::ClassMethod2738.11338<System.Object>()"
		ldstr "class G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>::ClassMethod2737<object>()
		calli default string(class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>)
		ldstr "G2_C688::ClassMethod2737.11337<System.Object>()"
		ldstr "class G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>::ClassMethod2736()
		calli default string(class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>)
		ldstr "G2_C688::ClassMethod2736.11336()"
		ldstr "class G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>::ClassMethod1347<object>()
		calli default string(class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>)
		ldstr "G2_C688::ClassMethod1347.MI.11340<System.Object>()"
		ldstr "class G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>::ClassMethod1346<object>()
		calli default string(class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>)
		ldstr "G2_C688::ClassMethod1346.MI.11339<System.Object>()"
		ldstr "class G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>::Method6<object>()
		calli default string(class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>)
		ldstr "G1_C12::Method6.4867<System.Object>()"
		ldstr "class G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>::Method5()
		calli default string(class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>)
		ldstr "G1_C12::Method5.4866()"
		ldstr "class G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>::Method4()
		calli default string(class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>)
		ldstr "G1_C12::Method4.4865()"
		ldstr "class G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>::Method7<object>()
		calli default string(class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>)
		ldstr "G1_C12::Method7.4863<System.Object>()"
		ldstr "class G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>::ClassMethod1347<object>()
		calli default string(class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>)
		ldstr "G2_C688::ClassMethod1347.MI.11340<System.Object>()"
		ldstr "class G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>::ClassMethod1346<object>()
		calli default string(class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>)
		ldstr "G2_C688::ClassMethod1346.MI.11339<System.Object>()"
		ldstr "class G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>::Method6<object>()
		calli default string(class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>)
		ldstr "G1_C12::Method6.4867<System.Object>()"
		ldstr "class G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>::Method5()
		calli default string(class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>)
		ldstr "G1_C12::Method5.4866()"
		ldstr "class G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>::Method4()
		calli default string(class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>)
		ldstr "G1_C12::Method4.4865()"
		ldstr "class G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>::Method7<object>()
		calli default string(class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>)
		ldstr "G1_C12::Method7.4863<System.Object>()"
		ldstr "class G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1242.IBase2`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>::Method7<object>()
		calli default string(class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>)
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1242.IBase1`1<class Generated1242.BaseClass0>::Method4()
		calli default string(class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>)
		ldstr "G1_C12::Method4.4865()"
		ldstr "class IBase1`1<class Generated1242.BaseClass0> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1242.IBase1`1<class Generated1242.BaseClass0>::Method5()
		calli default string(class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>)
		ldstr "G1_C12::Method5.4866()"
		ldstr "class IBase1`1<class Generated1242.BaseClass0> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1242.IBase1`1<class Generated1242.BaseClass0>::Method6<object>()
		calli default string(class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>)
		ldstr "G1_C12::Method6.MI.4868<System.Object>()"
		ldstr "class IBase1`1<class Generated1242.BaseClass0> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1242.IBase2`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>::Method7<object>()
		calli default string(class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>)
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>::ClassMethod2738<object>()
		calli default string(class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>)
		ldstr "G2_C688::ClassMethod2738.11338<System.Object>()"
		ldstr "class G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>::ClassMethod2737<object>()
		calli default string(class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>)
		ldstr "G2_C688::ClassMethod2737.11337<System.Object>()"
		ldstr "class G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>::ClassMethod2736()
		calli default string(class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>)
		ldstr "G2_C688::ClassMethod2736.11336()"
		ldstr "class G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>::ClassMethod1347<object>()
		calli default string(class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>)
		ldstr "G2_C688::ClassMethod1347.MI.11340<System.Object>()"
		ldstr "class G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>::ClassMethod1346<object>()
		calli default string(class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>)
		ldstr "G2_C688::ClassMethod1346.MI.11339<System.Object>()"
		ldstr "class G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>::Method6<object>()
		calli default string(class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>)
		ldstr "G1_C12::Method6.4867<System.Object>()"
		ldstr "class G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>::Method5()
		calli default string(class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>)
		ldstr "G1_C12::Method5.4866()"
		ldstr "class G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>::Method4()
		calli default string(class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>)
		ldstr "G1_C12::Method4.4865()"
		ldstr "class G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>::Method7<object>()
		calli default string(class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>)
		ldstr "G1_C12::Method7.4863<System.Object>()"
		ldstr "class G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>::ClassMethod1347<object>()
		calli default string(class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>)
		ldstr "G2_C688::ClassMethod1347.MI.11340<System.Object>()"
		ldstr "class G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>::ClassMethod1346<object>()
		calli default string(class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>)
		ldstr "G2_C688::ClassMethod1346.MI.11339<System.Object>()"
		ldstr "class G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>::Method6<object>()
		calli default string(class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>)
		ldstr "G1_C12::Method6.4867<System.Object>()"
		ldstr "class G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>::Method5()
		calli default string(class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>)
		ldstr "G1_C12::Method5.4866()"
		ldstr "class G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>::Method4()
		calli default string(class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>)
		ldstr "G1_C12::Method4.4865()"
		ldstr "class G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>::Method7<object>()
		calli default string(class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>)
		ldstr "G1_C12::Method7.4863<System.Object>()"
		ldstr "class G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1242.IBase2`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>::Method7<object>()
		calli default string(class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>)
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1242.IBase1`1<class Generated1242.BaseClass0>::Method4()
		calli default string(class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>)
		ldstr "G1_C12::Method4.4865()"
		ldstr "class IBase1`1<class Generated1242.BaseClass0> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1242.IBase1`1<class Generated1242.BaseClass0>::Method5()
		calli default string(class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>)
		ldstr "G1_C12::Method5.4866()"
		ldstr "class IBase1`1<class Generated1242.BaseClass0> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1242.IBase1`1<class Generated1242.BaseClass0>::Method6<object>()
		calli default string(class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>)
		ldstr "G1_C12::Method6.MI.4868<System.Object>()"
		ldstr "class IBase1`1<class Generated1242.BaseClass0> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1242.IBase2`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>::Method7<object>()
		calli default string(class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>)
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>::ClassMethod2738<object>()
		calli default string(class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>)
		ldstr "G2_C688::ClassMethod2738.11338<System.Object>()"
		ldstr "class G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>::ClassMethod2737<object>()
		calli default string(class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>)
		ldstr "G2_C688::ClassMethod2737.11337<System.Object>()"
		ldstr "class G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>::ClassMethod2736()
		calli default string(class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>)
		ldstr "G2_C688::ClassMethod2736.11336()"
		ldstr "class G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>::ClassMethod1347<object>()
		calli default string(class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>)
		ldstr "G2_C688::ClassMethod1347.MI.11340<System.Object>()"
		ldstr "class G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>::ClassMethod1346<object>()
		calli default string(class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>)
		ldstr "G2_C688::ClassMethod1346.MI.11339<System.Object>()"
		ldstr "class G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>::Method6<object>()
		calli default string(class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>)
		ldstr "G1_C12::Method6.4867<System.Object>()"
		ldstr "class G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>::Method5()
		calli default string(class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>)
		ldstr "G1_C12::Method5.4866()"
		ldstr "class G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>::Method4()
		calli default string(class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>)
		ldstr "G1_C12::Method4.4865()"
		ldstr "class G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>::Method7<object>()
		calli default string(class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>)
		ldstr "G1_C12::Method7.4863<System.Object>()"
		ldstr "class G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>::ClassMethod1347<object>()
		calli default string(class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>)
		ldstr "G2_C688::ClassMethod1347.MI.11340<System.Object>()"
		ldstr "class G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>::ClassMethod1346<object>()
		calli default string(class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>)
		ldstr "G2_C688::ClassMethod1346.MI.11339<System.Object>()"
		ldstr "class G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>::Method6<object>()
		calli default string(class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>)
		ldstr "G1_C12::Method6.4867<System.Object>()"
		ldstr "class G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>::Method5()
		calli default string(class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>)
		ldstr "G1_C12::Method5.4866()"
		ldstr "class G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>::Method4()
		calli default string(class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>)
		ldstr "G1_C12::Method4.4865()"
		ldstr "class G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>::Method7<object>()
		calli default string(class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>)
		ldstr "G1_C12::Method7.4863<System.Object>()"
		ldstr "class G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1242.IBase2`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>::Method7<object>()
		calli default string(class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>)
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1242.IBase1`1<class Generated1242.BaseClass0>::Method4()
		calli default string(class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>)
		ldstr "G1_C12::Method4.4865()"
		ldstr "class IBase1`1<class Generated1242.BaseClass0> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1242.IBase1`1<class Generated1242.BaseClass0>::Method5()
		calli default string(class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>)
		ldstr "G1_C12::Method5.4866()"
		ldstr "class IBase1`1<class Generated1242.BaseClass0> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1242.IBase1`1<class Generated1242.BaseClass0>::Method6<object>()
		calli default string(class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>)
		ldstr "G1_C12::Method6.MI.4868<System.Object>()"
		ldstr "class IBase1`1<class Generated1242.BaseClass0> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1242.IBase2`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>::Method7<object>()
		calli default string(class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>)
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>::ClassMethod2738<object>()
		calli default string(class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>)
		ldstr "G2_C688::ClassMethod2738.11338<System.Object>()"
		ldstr "class G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>::ClassMethod2737<object>()
		calli default string(class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>)
		ldstr "G2_C688::ClassMethod2737.11337<System.Object>()"
		ldstr "class G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>::ClassMethod2736()
		calli default string(class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>)
		ldstr "G2_C688::ClassMethod2736.11336()"
		ldstr "class G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>::ClassMethod1347<object>()
		calli default string(class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>)
		ldstr "G2_C688::ClassMethod1347.MI.11340<System.Object>()"
		ldstr "class G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>::ClassMethod1346<object>()
		calli default string(class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>)
		ldstr "G2_C688::ClassMethod1346.MI.11339<System.Object>()"
		ldstr "class G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>::Method6<object>()
		calli default string(class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>)
		ldstr "G1_C12::Method6.4867<System.Object>()"
		ldstr "class G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>::Method5()
		calli default string(class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>)
		ldstr "G1_C12::Method5.4866()"
		ldstr "class G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>::Method4()
		calli default string(class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>)
		ldstr "G1_C12::Method4.4865()"
		ldstr "class G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>::Method7<object>()
		calli default string(class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>)
		ldstr "G1_C12::Method7.4863<System.Object>()"
		ldstr "class G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1> on type class Generated1242.G2_C688`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>::ClassMethod1347<object>()
		calli default string(class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>)
		ldstr "G1_C12::ClassMethod1347.4870<System.Object>()"
		ldstr "class G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0> on type class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>::ClassMethod1346<object>()
		calli default string(class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>)
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()"
		ldstr "class G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0> on type class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>::Method6<object>()
		calli default string(class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>)
		ldstr "G1_C12::Method6.4867<System.Object>()"
		ldstr "class G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0> on type class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>::Method5()
		calli default string(class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>)
		ldstr "G1_C12::Method5.4866()"
		ldstr "class G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0> on type class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>::Method4()
		calli default string(class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>)
		ldstr "G1_C12::Method4.4865()"
		ldstr "class G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0> on type class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>::Method7<object>()
		calli default string(class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>)
		ldstr "G1_C12::Method7.4863<System.Object>()"
		ldstr "class G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0> on type class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1242.IBase2`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>::Method7<object>()
		calli default string(class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>)
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0> on type class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1242.IBase1`1<class Generated1242.BaseClass0>::Method4()
		calli default string(class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>)
		ldstr "G1_C12::Method4.4865()"
		ldstr "class IBase1`1<class Generated1242.BaseClass0> on type class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1242.IBase1`1<class Generated1242.BaseClass0>::Method5()
		calli default string(class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>)
		ldstr "G1_C12::Method5.4866()"
		ldstr "class IBase1`1<class Generated1242.BaseClass0> on type class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1242.IBase1`1<class Generated1242.BaseClass0>::Method6<object>()
		calli default string(class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>)
		ldstr "G1_C12::Method6.MI.4868<System.Object>()"
		ldstr "class IBase1`1<class Generated1242.BaseClass0> on type class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1242.IBase2`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>::Method7<object>()
		calli default string(class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>)
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1> on type class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>::ClassMethod1347<object>()
		calli default string(class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>)
		ldstr "G1_C12::ClassMethod1347.4870<System.Object>()"
		ldstr "class G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1> on type class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>::ClassMethod1346<object>()
		calli default string(class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>)
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()"
		ldstr "class G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1> on type class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>::Method6<object>()
		calli default string(class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>)
		ldstr "G1_C12::Method6.4867<System.Object>()"
		ldstr "class G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1> on type class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>::Method5()
		calli default string(class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>)
		ldstr "G1_C12::Method5.4866()"
		ldstr "class G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1> on type class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>::Method4()
		calli default string(class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>)
		ldstr "G1_C12::Method4.4865()"
		ldstr "class G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1> on type class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>::Method7<object>()
		calli default string(class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>)
		ldstr "G1_C12::Method7.4863<System.Object>()"
		ldstr "class G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1> on type class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1242.IBase2`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>::Method7<object>()
		calli default string(class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>)
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1> on type class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1242.IBase1`1<class Generated1242.BaseClass0>::Method4()
		calli default string(class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>)
		ldstr "G1_C12::Method4.4865()"
		ldstr "class IBase1`1<class Generated1242.BaseClass0> on type class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1242.IBase1`1<class Generated1242.BaseClass0>::Method5()
		calli default string(class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>)
		ldstr "G1_C12::Method5.4866()"
		ldstr "class IBase1`1<class Generated1242.BaseClass0> on type class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1242.IBase1`1<class Generated1242.BaseClass0>::Method6<object>()
		calli default string(class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>)
		ldstr "G1_C12::Method6.MI.4868<System.Object>()"
		ldstr "class IBase1`1<class Generated1242.BaseClass0> on type class Generated1242.G1_C12`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>::ClassMethod1347<object>()
		calli default string(class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>)
		ldstr "G1_C12::ClassMethod1347.4870<System.Object>()"
		ldstr "class G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0> on type class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>::ClassMethod1346<object>()
		calli default string(class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>)
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()"
		ldstr "class G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0> on type class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>::Method6<object>()
		calli default string(class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>)
		ldstr "G1_C12::Method6.4867<System.Object>()"
		ldstr "class G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0> on type class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>::Method5()
		calli default string(class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>)
		ldstr "G1_C12::Method5.4866()"
		ldstr "class G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0> on type class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>::Method4()
		calli default string(class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>)
		ldstr "G1_C12::Method4.4865()"
		ldstr "class G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0> on type class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>::Method7<object>()
		calli default string(class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>)
		ldstr "G1_C12::Method7.4863<System.Object>()"
		ldstr "class G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0> on type class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1242.IBase2`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>::Method7<object>()
		calli default string(class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>)
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0> on type class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1242.IBase1`1<class Generated1242.BaseClass0>::Method4()
		calli default string(class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>)
		ldstr "G1_C12::Method4.4865()"
		ldstr "class IBase1`1<class Generated1242.BaseClass0> on type class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1242.IBase1`1<class Generated1242.BaseClass0>::Method5()
		calli default string(class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>)
		ldstr "G1_C12::Method5.4866()"
		ldstr "class IBase1`1<class Generated1242.BaseClass0> on type class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1242.IBase1`1<class Generated1242.BaseClass0>::Method6<object>()
		calli default string(class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>)
		ldstr "G1_C12::Method6.MI.4868<System.Object>()"
		ldstr "class IBase1`1<class Generated1242.BaseClass0> on type class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1242.IBase2`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0>::Method7<object>()
		calli default string(class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>)
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class Generated1242.BaseClass0,class Generated1242.BaseClass0> on type class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1242.IBase2`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>::Method7<object>()
		calli default string(class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>)
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1> on type class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1242.IBase2`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>::Method7<object>()
		calli default string(class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>)
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1> on type class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>::ClassMethod1347<object>()
		calli default string(class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>)
		ldstr "G1_C12::ClassMethod1347.4870<System.Object>()"
		ldstr "class G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1> on type class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>::ClassMethod1346<object>()
		calli default string(class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>)
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()"
		ldstr "class G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1> on type class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>::Method6<object>()
		calli default string(class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>)
		ldstr "G1_C12::Method6.4867<System.Object>()"
		ldstr "class G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1> on type class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>::Method5()
		calli default string(class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>)
		ldstr "G1_C12::Method5.4866()"
		ldstr "class G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1> on type class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>::Method4()
		calli default string(class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>)
		ldstr "G1_C12::Method4.4865()"
		ldstr "class G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1> on type class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>::Method7<object>()
		calli default string(class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>)
		ldstr "G1_C12::Method7.4863<System.Object>()"
		ldstr "class G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1> on type class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1242.IBase2`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>::Method7<object>()
		calli default string(class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>)
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1> on type class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1242.IBase1`1<class Generated1242.BaseClass0>::Method4()
		calli default string(class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>)
		ldstr "G1_C12::Method4.4865()"
		ldstr "class IBase1`1<class Generated1242.BaseClass0> on type class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1242.IBase1`1<class Generated1242.BaseClass0>::Method5()
		calli default string(class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>)
		ldstr "G1_C12::Method5.4866()"
		ldstr "class IBase1`1<class Generated1242.BaseClass0> on type class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1242.IBase1`1<class Generated1242.BaseClass0>::Method6<object>()
		calli default string(class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>)
		ldstr "G1_C12::Method6.MI.4868<System.Object>()"
		ldstr "class IBase1`1<class Generated1242.BaseClass0> on type class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1242.IBase2`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1>::Method7<object>()
		calli default string(class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>)
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class Generated1242.BaseClass0,class Generated1242.BaseClass1> on type class Generated1242.G1_C12`2<class Generated1242.BaseClass1,class Generated1242.BaseClass1>"
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
		call void Generated1242.Generated1242::MethodCallingTest()
		call void Generated1242.Generated1242::ConstrainedCallsTest()
		call void Generated1242.Generated1242::StructConstrainedInterfaceCallsTest()
		call void Generated1242.Generated1242::CalliTest()
		ldc.i4 100
		ret
	}
}
