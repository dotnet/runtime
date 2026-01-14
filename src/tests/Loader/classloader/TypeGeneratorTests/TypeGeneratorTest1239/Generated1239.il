// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) .ver 4:0:0:0 }
.assembly extern TestFramework { .publickeytoken = ( B0 3F 5F 7F 11 D5 0A 3A )  }

//TYPES IN FORWARDER ASSEMBLIES:

//TEST ASSEMBLY:
.assembly extern xunit.core {}

.class public Generated1239.BaseClass0
{
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed {
		ldarg.0
		call instance void [mscorlib]System.Object::.ctor()
		ret
	}
}
.class public Generated1239.BaseClass1
		extends Generated1239.BaseClass0
{
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed {
		ldarg.0
		call instance void Generated1239.BaseClass0::.ctor()
		ret
	}
}
.class public Generated1239.G3_C1711`1<T0>
		extends class Generated1239.G2_C686`2<class Generated1239.BaseClass1,!T0>
		implements class Generated1239.IBase1`1<class Generated1239.BaseClass0>
{
	.method public hidebysig virtual instance string Method4() cil managed noinlining {
		ldstr "G3_C1711::Method4.17581()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<class Generated1239.BaseClass0>.Method4'() cil managed noinlining {
		.override method instance string class Generated1239.IBase1`1<class Generated1239.BaseClass0>::Method4()
		ldstr "G3_C1711::Method4.MI.17582()"
		ret
	}
	.method public hidebysig virtual instance string Method5() cil managed noinlining {
		ldstr "G3_C1711::Method5.17583()"
		ret
	}
	.method public hidebysig virtual instance string Method6<M0>() cil managed noinlining {
		ldstr "G3_C1711::Method6.17584<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<class Generated1239.BaseClass0>.Method6'<M0>() cil managed noinlining {
		.override method instance string class Generated1239.IBase1`1<class Generated1239.BaseClass0>::Method6<[1]>()
		ldstr "G3_C1711::Method6.MI.17585<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod4821() cil managed noinlining {
		ldstr "G3_C1711::ClassMethod4821.17586()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod4822() cil managed noinlining {
		ldstr "G3_C1711::ClassMethod4822.17587()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod4823<M0>() cil managed noinlining {
		ldstr "G3_C1711::ClassMethod4823.17588<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod4824<M0>() cil managed noinlining {
		ldstr "G3_C1711::ClassMethod4824.17589<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'G2_C686<class Generated1239.BaseClass1,T0>.ClassMethod2733'<M0>() cil managed noinlining {
		.override method instance string class Generated1239.G2_C686`2<class Generated1239.BaseClass1,!T0>::ClassMethod2733<[1]>()
		ldstr "G3_C1711::ClassMethod2733.MI.17590<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed {
		ldarg.0
		call instance void class Generated1239.G2_C686`2<class Generated1239.BaseClass1,!T0>::.ctor()
		ret
	}
}
.class public Generated1239.G2_C686`2<T0, T1>
		extends class Generated1239.G1_C12`2<!T1,!T0>
		implements class Generated1239.IBase1`1<!T0>, class Generated1239.IBase2`2<!T1,!T0>
{
	.method public hidebysig newslot virtual instance string Method4() cil managed noinlining {
		ldstr "G2_C686::Method4.11317()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<T0>.Method4'() cil managed noinlining {
		.override method instance string class Generated1239.IBase1`1<!T0>::Method4()
		ldstr "G2_C686::Method4.MI.11318()"
		ret
	}
	.method public hidebysig newslot virtual instance string Method5() cil managed noinlining {
		ldstr "G2_C686::Method5.11319()"
		ret
	}
	.method public hidebysig virtual instance string Method6<M0>() cil managed noinlining {
		ldstr "G2_C686::Method6.11320<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<T0>.Method6'<M0>() cil managed noinlining {
		.override method instance string class Generated1239.IBase1`1<!T0>::Method6<[1]>()
		ldstr "G2_C686::Method6.MI.11321<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string Method7<M0>() cil managed noinlining {
		ldstr "G2_C686::Method7.11322<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod2733<M0>() cil managed noinlining {
		ldstr "G2_C686::ClassMethod2733.11323<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'G1_C12<T1,T0>.ClassMethod1346'<M0>() cil managed noinlining {
		.override method instance string class Generated1239.G1_C12`2<!T1,!T0>::ClassMethod1346<[1]>()
		ldstr "G2_C686::ClassMethod1346.MI.11324<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'G1_C12<T1,T0>.ClassMethod1347'<M0>() cil managed noinlining {
		.override method instance string class Generated1239.G1_C12`2<!T1,!T0>::ClassMethod1347<[1]>()
		ldstr "G2_C686::ClassMethod1347.MI.11325<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed {
		ldarg.0
		call instance void class Generated1239.G1_C12`2<!T1,!T0>::.ctor()
		ret
	}
}
.class interface public abstract Generated1239.IBase1`1<+T0>
{
	.method public hidebysig newslot abstract virtual instance string Method4() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method5() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method6<M0>() cil managed { }
}
.class public Generated1239.G1_C12`2<T0, T1>
		implements class Generated1239.IBase2`2<!T0,!T1>, class Generated1239.IBase1`1<class Generated1239.BaseClass0>
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
		.override method instance string class Generated1239.IBase2`2<!T0,!T1>::Method7<[1]>()
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
	.method public hidebysig newslot virtual instance string 'IBase1<class Generated1239.BaseClass0>.Method6'<M0>() cil managed noinlining {
		.override method instance string class Generated1239.IBase1`1<class Generated1239.BaseClass0>::Method6<[1]>()
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
.class interface public abstract Generated1239.IBase2`2<+T0, -T1>
{
	.method public hidebysig newslot abstract virtual instance string Method7<M0>() cil managed { }
}
.class public auto ansi beforefieldinit Generated1239.Generated1239 {
	.method static void M.BaseClass0<(Generated1239.BaseClass0)W>(!!W inst, string exp) cil managed {
		.maxstack 5
		.locals init (string[] actualResults)
		ldc.i4.s    0
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.BaseClass0<(Generated1239.BaseClass0)W>(!!W inst, string exp)"
		ldc.i4.s 0
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.BaseClass1<(Generated1239.BaseClass1)W>(!!W inst, string exp) cil managed {
		.maxstack 5
		.locals init (string[] actualResults)
		ldc.i4.s    0
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.BaseClass1<(Generated1239.BaseClass1)W>(!!W inst, string exp)"
		ldc.i4.s 0
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1711.T<T0,(class Generated1239.G3_C1711`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 16
		.locals init (string[] actualResults)
		ldc.i4.s    11
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1711.T<T0,(class Generated1239.G3_C1711`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 11
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.G3_C1711`1<!!T0>::ClassMethod1346<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.G3_C1711`1<!!T0>::ClassMethod1347<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.G3_C1711`1<!!T0>::ClassMethod2733<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.G3_C1711`1<!!T0>::ClassMethod4821()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.G3_C1711`1<!!T0>::ClassMethod4822()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.G3_C1711`1<!!T0>::ClassMethod4823<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.G3_C1711`1<!!T0>::ClassMethod4824<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.G3_C1711`1<!!T0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.G3_C1711`1<!!T0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.G3_C1711`1<!!T0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.G3_C1711`1<!!T0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1711.A<(class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 16
		.locals init (string[] actualResults)
		ldc.i4.s    11
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1711.A<(class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 11
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>::ClassMethod1346<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>::ClassMethod1347<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>::ClassMethod2733<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>::ClassMethod4821()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>::ClassMethod4822()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>::ClassMethod4823<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>::ClassMethod4824<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1711.B<(class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 16
		.locals init (string[] actualResults)
		ldc.i4.s    11
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1711.B<(class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 11
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>::ClassMethod1346<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>::ClassMethod1347<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>::ClassMethod2733<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>::ClassMethod4821()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>::ClassMethod4822()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>::ClassMethod4823<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>::ClassMethod4824<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C686.T.T<T0,T1,(class Generated1239.G2_C686`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 12
		.locals init (string[] actualResults)
		ldc.i4.s    7
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C686.T.T<T0,T1,(class Generated1239.G2_C686`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 7
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.G2_C686`2<!!T0,!!T1>::ClassMethod1346<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.G2_C686`2<!!T0,!!T1>::ClassMethod1347<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.G2_C686`2<!!T0,!!T1>::ClassMethod2733<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.G2_C686`2<!!T0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.G2_C686`2<!!T0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.G2_C686`2<!!T0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.G2_C686`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C686.A.T<T1,(class Generated1239.G2_C686`2<class Generated1239.BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 12
		.locals init (string[] actualResults)
		ldc.i4.s    7
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C686.A.T<T1,(class Generated1239.G2_C686`2<class Generated1239.BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 7
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.G2_C686`2<class Generated1239.BaseClass0,!!T1>::ClassMethod1346<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.G2_C686`2<class Generated1239.BaseClass0,!!T1>::ClassMethod1347<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.G2_C686`2<class Generated1239.BaseClass0,!!T1>::ClassMethod2733<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.G2_C686`2<class Generated1239.BaseClass0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.G2_C686`2<class Generated1239.BaseClass0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.G2_C686`2<class Generated1239.BaseClass0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.G2_C686`2<class Generated1239.BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C686.A.A<(class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 12
		.locals init (string[] actualResults)
		ldc.i4.s    7
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C686.A.A<(class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 7
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>::ClassMethod1346<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>::ClassMethod1347<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>::ClassMethod2733<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C686.A.B<(class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 12
		.locals init (string[] actualResults)
		ldc.i4.s    7
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C686.A.B<(class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 7
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>::ClassMethod1346<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>::ClassMethod1347<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>::ClassMethod2733<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C686.B.T<T1,(class Generated1239.G2_C686`2<class Generated1239.BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 12
		.locals init (string[] actualResults)
		ldc.i4.s    7
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C686.B.T<T1,(class Generated1239.G2_C686`2<class Generated1239.BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 7
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.G2_C686`2<class Generated1239.BaseClass1,!!T1>::ClassMethod1346<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.G2_C686`2<class Generated1239.BaseClass1,!!T1>::ClassMethod1347<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.G2_C686`2<class Generated1239.BaseClass1,!!T1>::ClassMethod2733<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.G2_C686`2<class Generated1239.BaseClass1,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.G2_C686`2<class Generated1239.BaseClass1,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.G2_C686`2<class Generated1239.BaseClass1,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.G2_C686`2<class Generated1239.BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C686.B.A<(class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 12
		.locals init (string[] actualResults)
		ldc.i4.s    7
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C686.B.A<(class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 7
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>::ClassMethod1346<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>::ClassMethod1347<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>::ClassMethod2733<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C686.B.B<(class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 12
		.locals init (string[] actualResults)
		ldc.i4.s    7
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C686.B.B<(class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 7
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>::ClassMethod1346<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>::ClassMethod1347<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>::ClassMethod2733<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase1.T<T0,(class Generated1239.IBase1`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase1.T<T0,(class Generated1239.IBase1`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.IBase1`1<!!T0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.IBase1`1<!!T0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.IBase1`1<!!T0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase1.A<(class Generated1239.IBase1`1<class Generated1239.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase1.A<(class Generated1239.IBase1`1<class Generated1239.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.IBase1`1<class Generated1239.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.IBase1`1<class Generated1239.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.IBase1`1<class Generated1239.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase1.B<(class Generated1239.IBase1`1<class Generated1239.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase1.B<(class Generated1239.IBase1`1<class Generated1239.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.IBase1`1<class Generated1239.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.IBase1`1<class Generated1239.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.IBase1`1<class Generated1239.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C12.T.T<T0,T1,(class Generated1239.G1_C12`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C12.T.T<T0,T1,(class Generated1239.G1_C12`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.G1_C12`2<!!T0,!!T1>::ClassMethod1346<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.G1_C12`2<!!T0,!!T1>::ClassMethod1347<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.G1_C12`2<!!T0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.G1_C12`2<!!T0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.G1_C12`2<!!T0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.G1_C12`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C12.A.T<T1,(class Generated1239.G1_C12`2<class Generated1239.BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C12.A.T<T1,(class Generated1239.G1_C12`2<class Generated1239.BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass0,!!T1>::ClassMethod1346<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass0,!!T1>::ClassMethod1347<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C12.A.A<(class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C12.A.A<(class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>::ClassMethod1346<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>::ClassMethod1347<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C12.A.B<(class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C12.A.B<(class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>::ClassMethod1346<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>::ClassMethod1347<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C12.B.T<T1,(class Generated1239.G1_C12`2<class Generated1239.BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C12.B.T<T1,(class Generated1239.G1_C12`2<class Generated1239.BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass1,!!T1>::ClassMethod1346<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass1,!!T1>::ClassMethod1347<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass1,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass1,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass1,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C12.B.A<(class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C12.B.A<(class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>::ClassMethod1346<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>::ClassMethod1347<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C12.B.B<(class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C12.B.B<(class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>::ClassMethod1346<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>::ClassMethod1347<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.T.T<T0,T1,(class Generated1239.IBase2`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.T.T<T0,T1,(class Generated1239.IBase2`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.IBase2`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.A.T<T1,(class Generated1239.IBase2`2<class Generated1239.BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.A.T<T1,(class Generated1239.IBase2`2<class Generated1239.BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.IBase2`2<class Generated1239.BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.A.A<(class Generated1239.IBase2`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.A.A<(class Generated1239.IBase2`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.IBase2`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.A.B<(class Generated1239.IBase2`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.A.B<(class Generated1239.IBase2`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.IBase2`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.B.T<T1,(class Generated1239.IBase2`2<class Generated1239.BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.B.T<T1,(class Generated1239.IBase2`2<class Generated1239.BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.IBase2`2<class Generated1239.BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.B.A<(class Generated1239.IBase2`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.B.A<(class Generated1239.IBase2`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.IBase2`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.B.B<(class Generated1239.IBase2`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.B.B<(class Generated1239.IBase2`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1239.IBase2`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>::Method7<object>()
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
		newobj instance void class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>
		callvirt instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>::ClassMethod1347<object>()
		ldstr "G2_C686::ClassMethod1347.MI.11325<System.Object>()"
		ldstr "class G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1> on type class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>
		callvirt instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>::ClassMethod1346<object>()
		ldstr "G2_C686::ClassMethod1346.MI.11324<System.Object>()"
		ldstr "class G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1> on type class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>
		callvirt instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>::Method6<object>()
		ldstr "G3_C1711::Method6.17584<System.Object>()"
		ldstr "class G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1> on type class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>
		callvirt instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>::Method5()
		ldstr "G1_C12::Method5.4866()"
		ldstr "class G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1> on type class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>
		callvirt instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>::Method4()
		ldstr "G1_C12::Method4.4865()"
		ldstr "class G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1> on type class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>
		callvirt instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>::Method7<object>()
		ldstr "G1_C12::Method7.4863<System.Object>()"
		ldstr "class G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1> on type class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1239.IBase2`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>::Method7<object>()
		ldstr "G2_C686::Method7.11322<System.Object>()"
		ldstr "class IBase2`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1> on type class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1239.IBase1`1<class Generated1239.BaseClass0>::Method4()
		ldstr "G3_C1711::Method4.MI.17582()"
		ldstr "class IBase1`1<class Generated1239.BaseClass0> on type class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1239.IBase1`1<class Generated1239.BaseClass0>::Method5()
		ldstr "G3_C1711::Method5.17583()"
		ldstr "class IBase1`1<class Generated1239.BaseClass0> on type class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1239.IBase1`1<class Generated1239.BaseClass0>::Method6<object>()
		ldstr "G3_C1711::Method6.MI.17585<System.Object>()"
		ldstr "class IBase1`1<class Generated1239.BaseClass0> on type class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>
		callvirt instance string class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>::ClassMethod2733<object>()
		ldstr "G3_C1711::ClassMethod2733.MI.17590<System.Object>()"
		ldstr "class G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0> on type class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>
		callvirt instance string class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>::Method7<object>()
		ldstr "G2_C686::Method7.11322<System.Object>()"
		ldstr "class G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0> on type class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>
		callvirt instance string class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>::Method6<object>()
		ldstr "G3_C1711::Method6.17584<System.Object>()"
		ldstr "class G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0> on type class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>
		callvirt instance string class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>::Method5()
		ldstr "G3_C1711::Method5.17583()"
		ldstr "class G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0> on type class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>
		callvirt instance string class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>::Method4()
		ldstr "G3_C1711::Method4.17581()"
		ldstr "class G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0> on type class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>
		callvirt instance string class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>::ClassMethod1347<object>()
		ldstr "G2_C686::ClassMethod1347.MI.11325<System.Object>()"
		ldstr "class G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0> on type class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>
		callvirt instance string class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>::ClassMethod1346<object>()
		ldstr "G2_C686::ClassMethod1346.MI.11324<System.Object>()"
		ldstr "class G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0> on type class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1239.IBase1`1<class Generated1239.BaseClass1>::Method4()
		ldstr "G2_C686::Method4.MI.11318()"
		ldstr "class IBase1`1<class Generated1239.BaseClass1> on type class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1239.IBase1`1<class Generated1239.BaseClass1>::Method5()
		ldstr "G3_C1711::Method5.17583()"
		ldstr "class IBase1`1<class Generated1239.BaseClass1> on type class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1239.IBase1`1<class Generated1239.BaseClass1>::Method6<object>()
		ldstr "G2_C686::Method6.MI.11321<System.Object>()"
		ldstr "class IBase1`1<class Generated1239.BaseClass1> on type class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>
		callvirt instance string class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>::ClassMethod4824<object>()
		ldstr "G3_C1711::ClassMethod4824.17589<System.Object>()"
		ldstr "class G3_C1711`1<class Generated1239.BaseClass0> on type class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>
		callvirt instance string class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>::ClassMethod4823<object>()
		ldstr "G3_C1711::ClassMethod4823.17588<System.Object>()"
		ldstr "class G3_C1711`1<class Generated1239.BaseClass0> on type class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>
		callvirt instance string class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>::ClassMethod4822()
		ldstr "G3_C1711::ClassMethod4822.17587()"
		ldstr "class G3_C1711`1<class Generated1239.BaseClass0> on type class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>
		callvirt instance string class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>::ClassMethod4821()
		ldstr "G3_C1711::ClassMethod4821.17586()"
		ldstr "class G3_C1711`1<class Generated1239.BaseClass0> on type class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>
		callvirt instance string class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>::Method6<object>()
		ldstr "G3_C1711::Method6.17584<System.Object>()"
		ldstr "class G3_C1711`1<class Generated1239.BaseClass0> on type class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>
		callvirt instance string class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>::Method5()
		ldstr "G3_C1711::Method5.17583()"
		ldstr "class G3_C1711`1<class Generated1239.BaseClass0> on type class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>
		callvirt instance string class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>::Method4()
		ldstr "G3_C1711::Method4.17581()"
		ldstr "class G3_C1711`1<class Generated1239.BaseClass0> on type class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>
		callvirt instance string class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>::ClassMethod2733<object>()
		ldstr "G3_C1711::ClassMethod2733.MI.17590<System.Object>()"
		ldstr "class G3_C1711`1<class Generated1239.BaseClass0> on type class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>
		callvirt instance string class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>::Method7<object>()
		ldstr "G2_C686::Method7.11322<System.Object>()"
		ldstr "class G3_C1711`1<class Generated1239.BaseClass0> on type class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>
		callvirt instance string class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>::ClassMethod1347<object>()
		ldstr "G2_C686::ClassMethod1347.MI.11325<System.Object>()"
		ldstr "class G3_C1711`1<class Generated1239.BaseClass0> on type class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>
		callvirt instance string class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>::ClassMethod1346<object>()
		ldstr "G2_C686::ClassMethod1346.MI.11324<System.Object>()"
		ldstr "class G3_C1711`1<class Generated1239.BaseClass0> on type class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>
		callvirt instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>::ClassMethod1347<object>()
		ldstr "G2_C686::ClassMethod1347.MI.11325<System.Object>()"
		ldstr "class G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1> on type class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>
		callvirt instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>::ClassMethod1346<object>()
		ldstr "G2_C686::ClassMethod1346.MI.11324<System.Object>()"
		ldstr "class G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1> on type class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>
		callvirt instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>::Method6<object>()
		ldstr "G3_C1711::Method6.17584<System.Object>()"
		ldstr "class G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1> on type class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>
		callvirt instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>::Method5()
		ldstr "G1_C12::Method5.4866()"
		ldstr "class G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1> on type class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>
		callvirt instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>::Method4()
		ldstr "G1_C12::Method4.4865()"
		ldstr "class G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1> on type class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>
		callvirt instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>::Method7<object>()
		ldstr "G1_C12::Method7.4863<System.Object>()"
		ldstr "class G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1> on type class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1239.IBase2`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>::Method7<object>()
		ldstr "G2_C686::Method7.11322<System.Object>()"
		ldstr "class IBase2`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1> on type class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1239.IBase1`1<class Generated1239.BaseClass0>::Method4()
		ldstr "G3_C1711::Method4.MI.17582()"
		ldstr "class IBase1`1<class Generated1239.BaseClass0> on type class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1239.IBase1`1<class Generated1239.BaseClass0>::Method5()
		ldstr "G3_C1711::Method5.17583()"
		ldstr "class IBase1`1<class Generated1239.BaseClass0> on type class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1239.IBase1`1<class Generated1239.BaseClass0>::Method6<object>()
		ldstr "G3_C1711::Method6.MI.17585<System.Object>()"
		ldstr "class IBase1`1<class Generated1239.BaseClass0> on type class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1239.IBase2`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>::Method7<object>()
		ldstr "G2_C686::Method7.11322<System.Object>()"
		ldstr "class IBase2`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1> on type class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>
		callvirt instance string class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>::ClassMethod2733<object>()
		ldstr "G3_C1711::ClassMethod2733.MI.17590<System.Object>()"
		ldstr "class G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1> on type class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>
		callvirt instance string class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>::Method7<object>()
		ldstr "G2_C686::Method7.11322<System.Object>()"
		ldstr "class G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1> on type class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>
		callvirt instance string class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>::Method6<object>()
		ldstr "G3_C1711::Method6.17584<System.Object>()"
		ldstr "class G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1> on type class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>
		callvirt instance string class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>::Method5()
		ldstr "G3_C1711::Method5.17583()"
		ldstr "class G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1> on type class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>
		callvirt instance string class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>::Method4()
		ldstr "G3_C1711::Method4.17581()"
		ldstr "class G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1> on type class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>
		callvirt instance string class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>::ClassMethod1347<object>()
		ldstr "G2_C686::ClassMethod1347.MI.11325<System.Object>()"
		ldstr "class G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1> on type class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>
		callvirt instance string class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>::ClassMethod1346<object>()
		ldstr "G2_C686::ClassMethod1346.MI.11324<System.Object>()"
		ldstr "class G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1> on type class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1239.IBase1`1<class Generated1239.BaseClass1>::Method4()
		ldstr "G2_C686::Method4.MI.11318()"
		ldstr "class IBase1`1<class Generated1239.BaseClass1> on type class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1239.IBase1`1<class Generated1239.BaseClass1>::Method5()
		ldstr "G3_C1711::Method5.17583()"
		ldstr "class IBase1`1<class Generated1239.BaseClass1> on type class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1239.IBase1`1<class Generated1239.BaseClass1>::Method6<object>()
		ldstr "G2_C686::Method6.MI.11321<System.Object>()"
		ldstr "class IBase1`1<class Generated1239.BaseClass1> on type class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>
		callvirt instance string class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>::ClassMethod4824<object>()
		ldstr "G3_C1711::ClassMethod4824.17589<System.Object>()"
		ldstr "class G3_C1711`1<class Generated1239.BaseClass1> on type class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>
		callvirt instance string class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>::ClassMethod4823<object>()
		ldstr "G3_C1711::ClassMethod4823.17588<System.Object>()"
		ldstr "class G3_C1711`1<class Generated1239.BaseClass1> on type class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>
		callvirt instance string class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>::ClassMethod4822()
		ldstr "G3_C1711::ClassMethod4822.17587()"
		ldstr "class G3_C1711`1<class Generated1239.BaseClass1> on type class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>
		callvirt instance string class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>::ClassMethod4821()
		ldstr "G3_C1711::ClassMethod4821.17586()"
		ldstr "class G3_C1711`1<class Generated1239.BaseClass1> on type class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>
		callvirt instance string class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>::Method6<object>()
		ldstr "G3_C1711::Method6.17584<System.Object>()"
		ldstr "class G3_C1711`1<class Generated1239.BaseClass1> on type class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>
		callvirt instance string class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>::Method5()
		ldstr "G3_C1711::Method5.17583()"
		ldstr "class G3_C1711`1<class Generated1239.BaseClass1> on type class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>
		callvirt instance string class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>::Method4()
		ldstr "G3_C1711::Method4.17581()"
		ldstr "class G3_C1711`1<class Generated1239.BaseClass1> on type class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>
		callvirt instance string class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>::ClassMethod2733<object>()
		ldstr "G3_C1711::ClassMethod2733.MI.17590<System.Object>()"
		ldstr "class G3_C1711`1<class Generated1239.BaseClass1> on type class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>
		callvirt instance string class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>::Method7<object>()
		ldstr "G2_C686::Method7.11322<System.Object>()"
		ldstr "class G3_C1711`1<class Generated1239.BaseClass1> on type class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>
		callvirt instance string class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>::ClassMethod1347<object>()
		ldstr "G2_C686::ClassMethod1347.MI.11325<System.Object>()"
		ldstr "class G3_C1711`1<class Generated1239.BaseClass1> on type class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>
		callvirt instance string class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>::ClassMethod1346<object>()
		ldstr "G2_C686::ClassMethod1346.MI.11324<System.Object>()"
		ldstr "class G3_C1711`1<class Generated1239.BaseClass1> on type class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>
		callvirt instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>::ClassMethod1347<object>()
		ldstr "G2_C686::ClassMethod1347.MI.11325<System.Object>()"
		ldstr "class G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>
		callvirt instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>::ClassMethod1346<object>()
		ldstr "G2_C686::ClassMethod1346.MI.11324<System.Object>()"
		ldstr "class G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>
		callvirt instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>::Method6<object>()
		ldstr "G2_C686::Method6.11320<System.Object>()"
		ldstr "class G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>
		callvirt instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>::Method5()
		ldstr "G1_C12::Method5.4866()"
		ldstr "class G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>
		callvirt instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>::Method4()
		ldstr "G1_C12::Method4.4865()"
		ldstr "class G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>
		callvirt instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>::Method7<object>()
		ldstr "G1_C12::Method7.4863<System.Object>()"
		ldstr "class G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1239.IBase2`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>::Method7<object>()
		ldstr "G2_C686::Method7.11322<System.Object>()"
		ldstr "class IBase2`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1239.IBase1`1<class Generated1239.BaseClass0>::Method4()
		ldstr "G2_C686::Method4.MI.11318()"
		ldstr "class IBase1`1<class Generated1239.BaseClass0> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1239.IBase1`1<class Generated1239.BaseClass0>::Method5()
		ldstr "G2_C686::Method5.11319()"
		ldstr "class IBase1`1<class Generated1239.BaseClass0> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1239.IBase1`1<class Generated1239.BaseClass0>::Method6<object>()
		ldstr "G2_C686::Method6.MI.11321<System.Object>()"
		ldstr "class IBase1`1<class Generated1239.BaseClass0> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1239.IBase2`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>::Method7<object>()
		ldstr "G2_C686::Method7.11322<System.Object>()"
		ldstr "class IBase2`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>
		callvirt instance string class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>::ClassMethod2733<object>()
		ldstr "G2_C686::ClassMethod2733.11323<System.Object>()"
		ldstr "class G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>
		callvirt instance string class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>::Method7<object>()
		ldstr "G2_C686::Method7.11322<System.Object>()"
		ldstr "class G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>
		callvirt instance string class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>::Method6<object>()
		ldstr "G2_C686::Method6.11320<System.Object>()"
		ldstr "class G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>
		callvirt instance string class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>::Method5()
		ldstr "G2_C686::Method5.11319()"
		ldstr "class G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>
		callvirt instance string class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>::Method4()
		ldstr "G2_C686::Method4.11317()"
		ldstr "class G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>
		callvirt instance string class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>::ClassMethod1347<object>()
		ldstr "G2_C686::ClassMethod1347.MI.11325<System.Object>()"
		ldstr "class G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>
		callvirt instance string class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>::ClassMethod1346<object>()
		ldstr "G2_C686::ClassMethod1346.MI.11324<System.Object>()"
		ldstr "class G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>
		callvirt instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>::ClassMethod1347<object>()
		ldstr "G2_C686::ClassMethod1347.MI.11325<System.Object>()"
		ldstr "class G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>
		callvirt instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>::ClassMethod1346<object>()
		ldstr "G2_C686::ClassMethod1346.MI.11324<System.Object>()"
		ldstr "class G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>
		callvirt instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>::Method6<object>()
		ldstr "G2_C686::Method6.11320<System.Object>()"
		ldstr "class G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>
		callvirt instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>::Method5()
		ldstr "G1_C12::Method5.4866()"
		ldstr "class G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>
		callvirt instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>::Method4()
		ldstr "G1_C12::Method4.4865()"
		ldstr "class G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>
		callvirt instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>::Method7<object>()
		ldstr "G1_C12::Method7.4863<System.Object>()"
		ldstr "class G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1239.IBase2`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>::Method7<object>()
		ldstr "G2_C686::Method7.11322<System.Object>()"
		ldstr "class IBase2`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1239.IBase1`1<class Generated1239.BaseClass0>::Method4()
		ldstr "G2_C686::Method4.MI.11318()"
		ldstr "class IBase1`1<class Generated1239.BaseClass0> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1239.IBase1`1<class Generated1239.BaseClass0>::Method5()
		ldstr "G2_C686::Method5.11319()"
		ldstr "class IBase1`1<class Generated1239.BaseClass0> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1239.IBase1`1<class Generated1239.BaseClass0>::Method6<object>()
		ldstr "G2_C686::Method6.MI.11321<System.Object>()"
		ldstr "class IBase1`1<class Generated1239.BaseClass0> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1239.IBase2`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>::Method7<object>()
		ldstr "G2_C686::Method7.11322<System.Object>()"
		ldstr "class IBase2`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1239.IBase2`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>::Method7<object>()
		ldstr "G2_C686::Method7.11322<System.Object>()"
		ldstr "class IBase2`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1239.IBase2`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>::Method7<object>()
		ldstr "G2_C686::Method7.11322<System.Object>()"
		ldstr "class IBase2`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>
		callvirt instance string class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>::ClassMethod2733<object>()
		ldstr "G2_C686::ClassMethod2733.11323<System.Object>()"
		ldstr "class G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>
		callvirt instance string class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>::Method7<object>()
		ldstr "G2_C686::Method7.11322<System.Object>()"
		ldstr "class G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>
		callvirt instance string class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>::Method6<object>()
		ldstr "G2_C686::Method6.11320<System.Object>()"
		ldstr "class G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>
		callvirt instance string class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>::Method5()
		ldstr "G2_C686::Method5.11319()"
		ldstr "class G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>
		callvirt instance string class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>::Method4()
		ldstr "G2_C686::Method4.11317()"
		ldstr "class G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>
		callvirt instance string class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>::ClassMethod1347<object>()
		ldstr "G2_C686::ClassMethod1347.MI.11325<System.Object>()"
		ldstr "class G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>
		callvirt instance string class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>::ClassMethod1346<object>()
		ldstr "G2_C686::ClassMethod1346.MI.11324<System.Object>()"
		ldstr "class G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>
		callvirt instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>::ClassMethod1347<object>()
		ldstr "G2_C686::ClassMethod1347.MI.11325<System.Object>()"
		ldstr "class G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>
		callvirt instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>::ClassMethod1346<object>()
		ldstr "G2_C686::ClassMethod1346.MI.11324<System.Object>()"
		ldstr "class G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>
		callvirt instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>::Method6<object>()
		ldstr "G2_C686::Method6.11320<System.Object>()"
		ldstr "class G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>
		callvirt instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>::Method5()
		ldstr "G1_C12::Method5.4866()"
		ldstr "class G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>
		callvirt instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>::Method4()
		ldstr "G1_C12::Method4.4865()"
		ldstr "class G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>
		callvirt instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>::Method7<object>()
		ldstr "G1_C12::Method7.4863<System.Object>()"
		ldstr "class G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1239.IBase2`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>::Method7<object>()
		ldstr "G2_C686::Method7.11322<System.Object>()"
		ldstr "class IBase2`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1239.IBase1`1<class Generated1239.BaseClass0>::Method4()
		ldstr "G2_C686::Method4.MI.11318()"
		ldstr "class IBase1`1<class Generated1239.BaseClass0> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1239.IBase1`1<class Generated1239.BaseClass0>::Method5()
		ldstr "G2_C686::Method5.11319()"
		ldstr "class IBase1`1<class Generated1239.BaseClass0> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1239.IBase1`1<class Generated1239.BaseClass0>::Method6<object>()
		ldstr "G2_C686::Method6.MI.11321<System.Object>()"
		ldstr "class IBase1`1<class Generated1239.BaseClass0> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>
		callvirt instance string class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>::ClassMethod2733<object>()
		ldstr "G2_C686::ClassMethod2733.11323<System.Object>()"
		ldstr "class G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>
		callvirt instance string class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>::Method7<object>()
		ldstr "G2_C686::Method7.11322<System.Object>()"
		ldstr "class G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>
		callvirt instance string class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>::Method6<object>()
		ldstr "G2_C686::Method6.11320<System.Object>()"
		ldstr "class G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>
		callvirt instance string class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>::Method5()
		ldstr "G2_C686::Method5.11319()"
		ldstr "class G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>
		callvirt instance string class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>::Method4()
		ldstr "G2_C686::Method4.11317()"
		ldstr "class G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>
		callvirt instance string class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>::ClassMethod1347<object>()
		ldstr "G2_C686::ClassMethod1347.MI.11325<System.Object>()"
		ldstr "class G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>
		callvirt instance string class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>::ClassMethod1346<object>()
		ldstr "G2_C686::ClassMethod1346.MI.11324<System.Object>()"
		ldstr "class G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1239.IBase1`1<class Generated1239.BaseClass1>::Method4()
		ldstr "G2_C686::Method4.MI.11318()"
		ldstr "class IBase1`1<class Generated1239.BaseClass1> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1239.IBase1`1<class Generated1239.BaseClass1>::Method5()
		ldstr "G2_C686::Method5.11319()"
		ldstr "class IBase1`1<class Generated1239.BaseClass1> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1239.IBase1`1<class Generated1239.BaseClass1>::Method6<object>()
		ldstr "G2_C686::Method6.MI.11321<System.Object>()"
		ldstr "class IBase1`1<class Generated1239.BaseClass1> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>
		callvirt instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>::ClassMethod1347<object>()
		ldstr "G2_C686::ClassMethod1347.MI.11325<System.Object>()"
		ldstr "class G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>
		callvirt instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>::ClassMethod1346<object>()
		ldstr "G2_C686::ClassMethod1346.MI.11324<System.Object>()"
		ldstr "class G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>
		callvirt instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>::Method6<object>()
		ldstr "G2_C686::Method6.11320<System.Object>()"
		ldstr "class G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>
		callvirt instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>::Method5()
		ldstr "G1_C12::Method5.4866()"
		ldstr "class G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>
		callvirt instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>::Method4()
		ldstr "G1_C12::Method4.4865()"
		ldstr "class G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>
		callvirt instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>::Method7<object>()
		ldstr "G1_C12::Method7.4863<System.Object>()"
		ldstr "class G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1239.IBase2`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>::Method7<object>()
		ldstr "G2_C686::Method7.11322<System.Object>()"
		ldstr "class IBase2`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1239.IBase1`1<class Generated1239.BaseClass0>::Method4()
		ldstr "G2_C686::Method4.MI.11318()"
		ldstr "class IBase1`1<class Generated1239.BaseClass0> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1239.IBase1`1<class Generated1239.BaseClass0>::Method5()
		ldstr "G2_C686::Method5.11319()"
		ldstr "class IBase1`1<class Generated1239.BaseClass0> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1239.IBase1`1<class Generated1239.BaseClass0>::Method6<object>()
		ldstr "G2_C686::Method6.MI.11321<System.Object>()"
		ldstr "class IBase1`1<class Generated1239.BaseClass0> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1239.IBase2`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>::Method7<object>()
		ldstr "G2_C686::Method7.11322<System.Object>()"
		ldstr "class IBase2`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>
		callvirt instance string class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>::ClassMethod2733<object>()
		ldstr "G2_C686::ClassMethod2733.11323<System.Object>()"
		ldstr "class G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>
		callvirt instance string class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>::Method7<object>()
		ldstr "G2_C686::Method7.11322<System.Object>()"
		ldstr "class G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>
		callvirt instance string class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>::Method6<object>()
		ldstr "G2_C686::Method6.11320<System.Object>()"
		ldstr "class G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>
		callvirt instance string class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>::Method5()
		ldstr "G2_C686::Method5.11319()"
		ldstr "class G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>
		callvirt instance string class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>::Method4()
		ldstr "G2_C686::Method4.11317()"
		ldstr "class G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>
		callvirt instance string class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>::ClassMethod1347<object>()
		ldstr "G2_C686::ClassMethod1347.MI.11325<System.Object>()"
		ldstr "class G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>
		callvirt instance string class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>::ClassMethod1346<object>()
		ldstr "G2_C686::ClassMethod1346.MI.11324<System.Object>()"
		ldstr "class G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1239.IBase1`1<class Generated1239.BaseClass1>::Method4()
		ldstr "G2_C686::Method4.MI.11318()"
		ldstr "class IBase1`1<class Generated1239.BaseClass1> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1239.IBase1`1<class Generated1239.BaseClass1>::Method5()
		ldstr "G2_C686::Method5.11319()"
		ldstr "class IBase1`1<class Generated1239.BaseClass1> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1239.IBase1`1<class Generated1239.BaseClass1>::Method6<object>()
		ldstr "G2_C686::Method6.MI.11321<System.Object>()"
		ldstr "class IBase1`1<class Generated1239.BaseClass1> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>
		callvirt instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>::ClassMethod1347<object>()
		ldstr "G1_C12::ClassMethod1347.4870<System.Object>()"
		ldstr "class G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0> on type class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>
		callvirt instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>::ClassMethod1346<object>()
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()"
		ldstr "class G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0> on type class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>
		callvirt instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>::Method6<object>()
		ldstr "G1_C12::Method6.4867<System.Object>()"
		ldstr "class G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0> on type class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>
		callvirt instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>::Method5()
		ldstr "G1_C12::Method5.4866()"
		ldstr "class G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0> on type class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>
		callvirt instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>::Method4()
		ldstr "G1_C12::Method4.4865()"
		ldstr "class G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0> on type class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>
		callvirt instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>::Method7<object>()
		ldstr "G1_C12::Method7.4863<System.Object>()"
		ldstr "class G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0> on type class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1239.IBase2`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>::Method7<object>()
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0> on type class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1239.IBase1`1<class Generated1239.BaseClass0>::Method4()
		ldstr "G1_C12::Method4.4865()"
		ldstr "class IBase1`1<class Generated1239.BaseClass0> on type class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1239.IBase1`1<class Generated1239.BaseClass0>::Method5()
		ldstr "G1_C12::Method5.4866()"
		ldstr "class IBase1`1<class Generated1239.BaseClass0> on type class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1239.IBase1`1<class Generated1239.BaseClass0>::Method6<object>()
		ldstr "G1_C12::Method6.MI.4868<System.Object>()"
		ldstr "class IBase1`1<class Generated1239.BaseClass0> on type class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1239.IBase2`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>::Method7<object>()
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1> on type class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>
		callvirt instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>::ClassMethod1347<object>()
		ldstr "G1_C12::ClassMethod1347.4870<System.Object>()"
		ldstr "class G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1> on type class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>
		callvirt instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>::ClassMethod1346<object>()
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()"
		ldstr "class G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1> on type class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>
		callvirt instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>::Method6<object>()
		ldstr "G1_C12::Method6.4867<System.Object>()"
		ldstr "class G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1> on type class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>
		callvirt instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>::Method5()
		ldstr "G1_C12::Method5.4866()"
		ldstr "class G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1> on type class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>
		callvirt instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>::Method4()
		ldstr "G1_C12::Method4.4865()"
		ldstr "class G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1> on type class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>
		callvirt instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>::Method7<object>()
		ldstr "G1_C12::Method7.4863<System.Object>()"
		ldstr "class G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1> on type class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1239.IBase2`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>::Method7<object>()
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1> on type class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1239.IBase1`1<class Generated1239.BaseClass0>::Method4()
		ldstr "G1_C12::Method4.4865()"
		ldstr "class IBase1`1<class Generated1239.BaseClass0> on type class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1239.IBase1`1<class Generated1239.BaseClass0>::Method5()
		ldstr "G1_C12::Method5.4866()"
		ldstr "class IBase1`1<class Generated1239.BaseClass0> on type class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1239.IBase1`1<class Generated1239.BaseClass0>::Method6<object>()
		ldstr "G1_C12::Method6.MI.4868<System.Object>()"
		ldstr "class IBase1`1<class Generated1239.BaseClass0> on type class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>
		callvirt instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>::ClassMethod1347<object>()
		ldstr "G1_C12::ClassMethod1347.4870<System.Object>()"
		ldstr "class G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0> on type class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>
		callvirt instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>::ClassMethod1346<object>()
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()"
		ldstr "class G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0> on type class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>
		callvirt instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>::Method6<object>()
		ldstr "G1_C12::Method6.4867<System.Object>()"
		ldstr "class G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0> on type class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>
		callvirt instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>::Method5()
		ldstr "G1_C12::Method5.4866()"
		ldstr "class G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0> on type class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>
		callvirt instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>::Method4()
		ldstr "G1_C12::Method4.4865()"
		ldstr "class G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0> on type class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>
		callvirt instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>::Method7<object>()
		ldstr "G1_C12::Method7.4863<System.Object>()"
		ldstr "class G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0> on type class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1239.IBase2`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>::Method7<object>()
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0> on type class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1239.IBase1`1<class Generated1239.BaseClass0>::Method4()
		ldstr "G1_C12::Method4.4865()"
		ldstr "class IBase1`1<class Generated1239.BaseClass0> on type class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1239.IBase1`1<class Generated1239.BaseClass0>::Method5()
		ldstr "G1_C12::Method5.4866()"
		ldstr "class IBase1`1<class Generated1239.BaseClass0> on type class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1239.IBase1`1<class Generated1239.BaseClass0>::Method6<object>()
		ldstr "G1_C12::Method6.MI.4868<System.Object>()"
		ldstr "class IBase1`1<class Generated1239.BaseClass0> on type class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1239.IBase2`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>::Method7<object>()
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0> on type class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1239.IBase2`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>::Method7<object>()
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1> on type class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1239.IBase2`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>::Method7<object>()
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1> on type class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>
		callvirt instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>::ClassMethod1347<object>()
		ldstr "G1_C12::ClassMethod1347.4870<System.Object>()"
		ldstr "class G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1> on type class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>
		callvirt instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>::ClassMethod1346<object>()
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()"
		ldstr "class G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1> on type class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>
		callvirt instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>::Method6<object>()
		ldstr "G1_C12::Method6.4867<System.Object>()"
		ldstr "class G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1> on type class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>
		callvirt instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>::Method5()
		ldstr "G1_C12::Method5.4866()"
		ldstr "class G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1> on type class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>
		callvirt instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>::Method4()
		ldstr "G1_C12::Method4.4865()"
		ldstr "class G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1> on type class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>
		callvirt instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>::Method7<object>()
		ldstr "G1_C12::Method7.4863<System.Object>()"
		ldstr "class G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1> on type class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1239.IBase2`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>::Method7<object>()
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1> on type class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1239.IBase1`1<class Generated1239.BaseClass0>::Method4()
		ldstr "G1_C12::Method4.4865()"
		ldstr "class IBase1`1<class Generated1239.BaseClass0> on type class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1239.IBase1`1<class Generated1239.BaseClass0>::Method5()
		ldstr "G1_C12::Method5.4866()"
		ldstr "class IBase1`1<class Generated1239.BaseClass0> on type class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1239.IBase1`1<class Generated1239.BaseClass0>::Method6<object>()
		ldstr "G1_C12::Method6.MI.4868<System.Object>()"
		ldstr "class IBase1`1<class Generated1239.BaseClass0> on type class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1239.IBase2`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>::Method7<object>()
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1> on type class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>"
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
		newobj instance void class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C686::ClassMethod1346.MI.11324<System.Object>()#G2_C686::ClassMethod1347.MI.11325<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G3_C1711::Method6.17584<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Generated1239.Generated1239::M.G1_C12.T.T<class Generated1239.BaseClass0,class Generated1239.BaseClass1,class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C686::ClassMethod1346.MI.11324<System.Object>()#G2_C686::ClassMethod1347.MI.11325<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G3_C1711::Method6.17584<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Generated1239.Generated1239::M.G1_C12.A.T<class Generated1239.BaseClass1,class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C686::ClassMethod1346.MI.11324<System.Object>()#G2_C686::ClassMethod1347.MI.11325<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G3_C1711::Method6.17584<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Generated1239.Generated1239::M.G1_C12.A.B<class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C686::Method7.11322<System.Object>()#"
		call void Generated1239.Generated1239::M.IBase2.T.T<class Generated1239.BaseClass0,class Generated1239.BaseClass1,class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C686::Method7.11322<System.Object>()#"
		call void Generated1239.Generated1239::M.IBase2.A.T<class Generated1239.BaseClass1,class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C686::Method7.11322<System.Object>()#"
		call void Generated1239.Generated1239::M.IBase2.A.B<class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1711::Method4.MI.17582()#G3_C1711::Method5.17583()#G3_C1711::Method6.MI.17585<System.Object>()#"
		call void Generated1239.Generated1239::M.IBase1.T<class Generated1239.BaseClass0,class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G3_C1711::Method4.MI.17582()#G3_C1711::Method5.17583()#G3_C1711::Method6.MI.17585<System.Object>()#"
		call void Generated1239.Generated1239::M.IBase1.A<class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C686::ClassMethod1346.MI.11324<System.Object>()#G2_C686::ClassMethod1347.MI.11325<System.Object>()#G3_C1711::ClassMethod2733.MI.17590<System.Object>()#G3_C1711::Method4.17581()#G3_C1711::Method5.17583()#G3_C1711::Method6.17584<System.Object>()#G2_C686::Method7.11322<System.Object>()#"
		call void Generated1239.Generated1239::M.G2_C686.T.T<class Generated1239.BaseClass1,class Generated1239.BaseClass0,class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C686::ClassMethod1346.MI.11324<System.Object>()#G2_C686::ClassMethod1347.MI.11325<System.Object>()#G3_C1711::ClassMethod2733.MI.17590<System.Object>()#G3_C1711::Method4.17581()#G3_C1711::Method5.17583()#G3_C1711::Method6.17584<System.Object>()#G2_C686::Method7.11322<System.Object>()#"
		call void Generated1239.Generated1239::M.G2_C686.B.T<class Generated1239.BaseClass0,class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C686::ClassMethod1346.MI.11324<System.Object>()#G2_C686::ClassMethod1347.MI.11325<System.Object>()#G3_C1711::ClassMethod2733.MI.17590<System.Object>()#G3_C1711::Method4.17581()#G3_C1711::Method5.17583()#G3_C1711::Method6.17584<System.Object>()#G2_C686::Method7.11322<System.Object>()#"
		call void Generated1239.Generated1239::M.G2_C686.B.A<class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C686::Method4.MI.11318()#G3_C1711::Method5.17583()#G2_C686::Method6.MI.11321<System.Object>()#"
		call void Generated1239.Generated1239::M.IBase1.T<class Generated1239.BaseClass1,class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C686::Method4.MI.11318()#G3_C1711::Method5.17583()#G2_C686::Method6.MI.11321<System.Object>()#"
		call void Generated1239.Generated1239::M.IBase1.B<class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C686::ClassMethod1346.MI.11324<System.Object>()#G2_C686::ClassMethod1347.MI.11325<System.Object>()#G3_C1711::ClassMethod2733.MI.17590<System.Object>()#G3_C1711::ClassMethod4821.17586()#G3_C1711::ClassMethod4822.17587()#G3_C1711::ClassMethod4823.17588<System.Object>()#G3_C1711::ClassMethod4824.17589<System.Object>()#G3_C1711::Method4.17581()#G3_C1711::Method5.17583()#G3_C1711::Method6.17584<System.Object>()#G2_C686::Method7.11322<System.Object>()#"
		call void Generated1239.Generated1239::M.G3_C1711.T<class Generated1239.BaseClass0,class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C686::ClassMethod1346.MI.11324<System.Object>()#G2_C686::ClassMethod1347.MI.11325<System.Object>()#G3_C1711::ClassMethod2733.MI.17590<System.Object>()#G3_C1711::ClassMethod4821.17586()#G3_C1711::ClassMethod4822.17587()#G3_C1711::ClassMethod4823.17588<System.Object>()#G3_C1711::ClassMethod4824.17589<System.Object>()#G3_C1711::Method4.17581()#G3_C1711::Method5.17583()#G3_C1711::Method6.17584<System.Object>()#G2_C686::Method7.11322<System.Object>()#"
		call void Generated1239.Generated1239::M.G3_C1711.A<class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>>(!!0,string)
		newobj instance void class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C686::ClassMethod1346.MI.11324<System.Object>()#G2_C686::ClassMethod1347.MI.11325<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G3_C1711::Method6.17584<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Generated1239.Generated1239::M.G1_C12.T.T<class Generated1239.BaseClass1,class Generated1239.BaseClass1,class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C686::ClassMethod1346.MI.11324<System.Object>()#G2_C686::ClassMethod1347.MI.11325<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G3_C1711::Method6.17584<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Generated1239.Generated1239::M.G1_C12.B.T<class Generated1239.BaseClass1,class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C686::ClassMethod1346.MI.11324<System.Object>()#G2_C686::ClassMethod1347.MI.11325<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G3_C1711::Method6.17584<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Generated1239.Generated1239::M.G1_C12.B.B<class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C686::Method7.11322<System.Object>()#"
		call void Generated1239.Generated1239::M.IBase2.T.T<class Generated1239.BaseClass1,class Generated1239.BaseClass1,class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C686::Method7.11322<System.Object>()#"
		call void Generated1239.Generated1239::M.IBase2.B.T<class Generated1239.BaseClass1,class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C686::Method7.11322<System.Object>()#"
		call void Generated1239.Generated1239::M.IBase2.B.B<class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1711::Method4.MI.17582()#G3_C1711::Method5.17583()#G3_C1711::Method6.MI.17585<System.Object>()#"
		call void Generated1239.Generated1239::M.IBase1.T<class Generated1239.BaseClass0,class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1711::Method4.MI.17582()#G3_C1711::Method5.17583()#G3_C1711::Method6.MI.17585<System.Object>()#"
		call void Generated1239.Generated1239::M.IBase1.A<class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C686::Method7.11322<System.Object>()#"
		call void Generated1239.Generated1239::M.IBase2.T.T<class Generated1239.BaseClass0,class Generated1239.BaseClass1,class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C686::Method7.11322<System.Object>()#"
		call void Generated1239.Generated1239::M.IBase2.A.T<class Generated1239.BaseClass1,class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C686::Method7.11322<System.Object>()#"
		call void Generated1239.Generated1239::M.IBase2.A.B<class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C686::ClassMethod1346.MI.11324<System.Object>()#G2_C686::ClassMethod1347.MI.11325<System.Object>()#G3_C1711::ClassMethod2733.MI.17590<System.Object>()#G3_C1711::Method4.17581()#G3_C1711::Method5.17583()#G3_C1711::Method6.17584<System.Object>()#G2_C686::Method7.11322<System.Object>()#"
		call void Generated1239.Generated1239::M.G2_C686.T.T<class Generated1239.BaseClass1,class Generated1239.BaseClass1,class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C686::ClassMethod1346.MI.11324<System.Object>()#G2_C686::ClassMethod1347.MI.11325<System.Object>()#G3_C1711::ClassMethod2733.MI.17590<System.Object>()#G3_C1711::Method4.17581()#G3_C1711::Method5.17583()#G3_C1711::Method6.17584<System.Object>()#G2_C686::Method7.11322<System.Object>()#"
		call void Generated1239.Generated1239::M.G2_C686.B.T<class Generated1239.BaseClass1,class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C686::ClassMethod1346.MI.11324<System.Object>()#G2_C686::ClassMethod1347.MI.11325<System.Object>()#G3_C1711::ClassMethod2733.MI.17590<System.Object>()#G3_C1711::Method4.17581()#G3_C1711::Method5.17583()#G3_C1711::Method6.17584<System.Object>()#G2_C686::Method7.11322<System.Object>()#"
		call void Generated1239.Generated1239::M.G2_C686.B.B<class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C686::Method4.MI.11318()#G3_C1711::Method5.17583()#G2_C686::Method6.MI.11321<System.Object>()#"
		call void Generated1239.Generated1239::M.IBase1.T<class Generated1239.BaseClass1,class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C686::Method4.MI.11318()#G3_C1711::Method5.17583()#G2_C686::Method6.MI.11321<System.Object>()#"
		call void Generated1239.Generated1239::M.IBase1.B<class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C686::ClassMethod1346.MI.11324<System.Object>()#G2_C686::ClassMethod1347.MI.11325<System.Object>()#G3_C1711::ClassMethod2733.MI.17590<System.Object>()#G3_C1711::ClassMethod4821.17586()#G3_C1711::ClassMethod4822.17587()#G3_C1711::ClassMethod4823.17588<System.Object>()#G3_C1711::ClassMethod4824.17589<System.Object>()#G3_C1711::Method4.17581()#G3_C1711::Method5.17583()#G3_C1711::Method6.17584<System.Object>()#G2_C686::Method7.11322<System.Object>()#"
		call void Generated1239.Generated1239::M.G3_C1711.T<class Generated1239.BaseClass1,class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C686::ClassMethod1346.MI.11324<System.Object>()#G2_C686::ClassMethod1347.MI.11325<System.Object>()#G3_C1711::ClassMethod2733.MI.17590<System.Object>()#G3_C1711::ClassMethod4821.17586()#G3_C1711::ClassMethod4822.17587()#G3_C1711::ClassMethod4823.17588<System.Object>()#G3_C1711::ClassMethod4824.17589<System.Object>()#G3_C1711::Method4.17581()#G3_C1711::Method5.17583()#G3_C1711::Method6.17584<System.Object>()#G2_C686::Method7.11322<System.Object>()#"
		call void Generated1239.Generated1239::M.G3_C1711.B<class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>>(!!0,string)
		newobj instance void class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C686::ClassMethod1346.MI.11324<System.Object>()#G2_C686::ClassMethod1347.MI.11325<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G2_C686::Method6.11320<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Generated1239.Generated1239::M.G1_C12.T.T<class Generated1239.BaseClass0,class Generated1239.BaseClass0,class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C686::ClassMethod1346.MI.11324<System.Object>()#G2_C686::ClassMethod1347.MI.11325<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G2_C686::Method6.11320<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Generated1239.Generated1239::M.G1_C12.A.T<class Generated1239.BaseClass0,class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C686::ClassMethod1346.MI.11324<System.Object>()#G2_C686::ClassMethod1347.MI.11325<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G2_C686::Method6.11320<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Generated1239.Generated1239::M.G1_C12.A.A<class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C686::Method7.11322<System.Object>()#"
		call void Generated1239.Generated1239::M.IBase2.T.T<class Generated1239.BaseClass0,class Generated1239.BaseClass0,class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C686::Method7.11322<System.Object>()#"
		call void Generated1239.Generated1239::M.IBase2.A.T<class Generated1239.BaseClass0,class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C686::Method7.11322<System.Object>()#"
		call void Generated1239.Generated1239::M.IBase2.A.A<class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C686::Method4.MI.11318()#G2_C686::Method5.11319()#G2_C686::Method6.MI.11321<System.Object>()#"
		call void Generated1239.Generated1239::M.IBase1.T<class Generated1239.BaseClass0,class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C686::Method4.MI.11318()#G2_C686::Method5.11319()#G2_C686::Method6.MI.11321<System.Object>()#"
		call void Generated1239.Generated1239::M.IBase1.A<class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C686::Method7.11322<System.Object>()#"
		call void Generated1239.Generated1239::M.IBase2.T.T<class Generated1239.BaseClass0,class Generated1239.BaseClass1,class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C686::Method7.11322<System.Object>()#"
		call void Generated1239.Generated1239::M.IBase2.A.T<class Generated1239.BaseClass1,class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C686::Method7.11322<System.Object>()#"
		call void Generated1239.Generated1239::M.IBase2.A.B<class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C686::ClassMethod1346.MI.11324<System.Object>()#G2_C686::ClassMethod1347.MI.11325<System.Object>()#G2_C686::ClassMethod2733.11323<System.Object>()#G2_C686::Method4.11317()#G2_C686::Method5.11319()#G2_C686::Method6.11320<System.Object>()#G2_C686::Method7.11322<System.Object>()#"
		call void Generated1239.Generated1239::M.G2_C686.T.T<class Generated1239.BaseClass0,class Generated1239.BaseClass0,class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C686::ClassMethod1346.MI.11324<System.Object>()#G2_C686::ClassMethod1347.MI.11325<System.Object>()#G2_C686::ClassMethod2733.11323<System.Object>()#G2_C686::Method4.11317()#G2_C686::Method5.11319()#G2_C686::Method6.11320<System.Object>()#G2_C686::Method7.11322<System.Object>()#"
		call void Generated1239.Generated1239::M.G2_C686.A.T<class Generated1239.BaseClass0,class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C686::ClassMethod1346.MI.11324<System.Object>()#G2_C686::ClassMethod1347.MI.11325<System.Object>()#G2_C686::ClassMethod2733.11323<System.Object>()#G2_C686::Method4.11317()#G2_C686::Method5.11319()#G2_C686::Method6.11320<System.Object>()#G2_C686::Method7.11322<System.Object>()#"
		call void Generated1239.Generated1239::M.G2_C686.A.A<class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>>(!!0,string)
		newobj instance void class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C686::ClassMethod1346.MI.11324<System.Object>()#G2_C686::ClassMethod1347.MI.11325<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G2_C686::Method6.11320<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Generated1239.Generated1239::M.G1_C12.T.T<class Generated1239.BaseClass1,class Generated1239.BaseClass0,class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C686::ClassMethod1346.MI.11324<System.Object>()#G2_C686::ClassMethod1347.MI.11325<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G2_C686::Method6.11320<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Generated1239.Generated1239::M.G1_C12.B.T<class Generated1239.BaseClass0,class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C686::ClassMethod1346.MI.11324<System.Object>()#G2_C686::ClassMethod1347.MI.11325<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G2_C686::Method6.11320<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Generated1239.Generated1239::M.G1_C12.B.A<class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C686::Method7.11322<System.Object>()#"
		call void Generated1239.Generated1239::M.IBase2.T.T<class Generated1239.BaseClass1,class Generated1239.BaseClass0,class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C686::Method7.11322<System.Object>()#"
		call void Generated1239.Generated1239::M.IBase2.B.T<class Generated1239.BaseClass0,class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C686::Method7.11322<System.Object>()#"
		call void Generated1239.Generated1239::M.IBase2.B.A<class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C686::Method4.MI.11318()#G2_C686::Method5.11319()#G2_C686::Method6.MI.11321<System.Object>()#"
		call void Generated1239.Generated1239::M.IBase1.T<class Generated1239.BaseClass0,class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C686::Method4.MI.11318()#G2_C686::Method5.11319()#G2_C686::Method6.MI.11321<System.Object>()#"
		call void Generated1239.Generated1239::M.IBase1.A<class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C686::Method7.11322<System.Object>()#"
		call void Generated1239.Generated1239::M.IBase2.T.T<class Generated1239.BaseClass0,class Generated1239.BaseClass0,class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C686::Method7.11322<System.Object>()#"
		call void Generated1239.Generated1239::M.IBase2.A.T<class Generated1239.BaseClass0,class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C686::Method7.11322<System.Object>()#"
		call void Generated1239.Generated1239::M.IBase2.A.A<class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C686::Method7.11322<System.Object>()#"
		call void Generated1239.Generated1239::M.IBase2.T.T<class Generated1239.BaseClass0,class Generated1239.BaseClass1,class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C686::Method7.11322<System.Object>()#"
		call void Generated1239.Generated1239::M.IBase2.A.T<class Generated1239.BaseClass1,class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C686::Method7.11322<System.Object>()#"
		call void Generated1239.Generated1239::M.IBase2.A.B<class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C686::Method7.11322<System.Object>()#"
		call void Generated1239.Generated1239::M.IBase2.T.T<class Generated1239.BaseClass1,class Generated1239.BaseClass1,class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C686::Method7.11322<System.Object>()#"
		call void Generated1239.Generated1239::M.IBase2.B.T<class Generated1239.BaseClass1,class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C686::Method7.11322<System.Object>()#"
		call void Generated1239.Generated1239::M.IBase2.B.B<class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C686::ClassMethod1346.MI.11324<System.Object>()#G2_C686::ClassMethod1347.MI.11325<System.Object>()#G2_C686::ClassMethod2733.11323<System.Object>()#G2_C686::Method4.11317()#G2_C686::Method5.11319()#G2_C686::Method6.11320<System.Object>()#G2_C686::Method7.11322<System.Object>()#"
		call void Generated1239.Generated1239::M.G2_C686.T.T<class Generated1239.BaseClass0,class Generated1239.BaseClass1,class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C686::ClassMethod1346.MI.11324<System.Object>()#G2_C686::ClassMethod1347.MI.11325<System.Object>()#G2_C686::ClassMethod2733.11323<System.Object>()#G2_C686::Method4.11317()#G2_C686::Method5.11319()#G2_C686::Method6.11320<System.Object>()#G2_C686::Method7.11322<System.Object>()#"
		call void Generated1239.Generated1239::M.G2_C686.A.T<class Generated1239.BaseClass1,class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C686::ClassMethod1346.MI.11324<System.Object>()#G2_C686::ClassMethod1347.MI.11325<System.Object>()#G2_C686::ClassMethod2733.11323<System.Object>()#G2_C686::Method4.11317()#G2_C686::Method5.11319()#G2_C686::Method6.11320<System.Object>()#G2_C686::Method7.11322<System.Object>()#"
		call void Generated1239.Generated1239::M.G2_C686.A.B<class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>>(!!0,string)
		newobj instance void class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C686::ClassMethod1346.MI.11324<System.Object>()#G2_C686::ClassMethod1347.MI.11325<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G2_C686::Method6.11320<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Generated1239.Generated1239::M.G1_C12.T.T<class Generated1239.BaseClass0,class Generated1239.BaseClass1,class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C686::ClassMethod1346.MI.11324<System.Object>()#G2_C686::ClassMethod1347.MI.11325<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G2_C686::Method6.11320<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Generated1239.Generated1239::M.G1_C12.A.T<class Generated1239.BaseClass1,class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C686::ClassMethod1346.MI.11324<System.Object>()#G2_C686::ClassMethod1347.MI.11325<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G2_C686::Method6.11320<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Generated1239.Generated1239::M.G1_C12.A.B<class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C686::Method7.11322<System.Object>()#"
		call void Generated1239.Generated1239::M.IBase2.T.T<class Generated1239.BaseClass0,class Generated1239.BaseClass1,class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C686::Method7.11322<System.Object>()#"
		call void Generated1239.Generated1239::M.IBase2.A.T<class Generated1239.BaseClass1,class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C686::Method7.11322<System.Object>()#"
		call void Generated1239.Generated1239::M.IBase2.A.B<class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C686::Method4.MI.11318()#G2_C686::Method5.11319()#G2_C686::Method6.MI.11321<System.Object>()#"
		call void Generated1239.Generated1239::M.IBase1.T<class Generated1239.BaseClass0,class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C686::Method4.MI.11318()#G2_C686::Method5.11319()#G2_C686::Method6.MI.11321<System.Object>()#"
		call void Generated1239.Generated1239::M.IBase1.A<class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C686::ClassMethod1346.MI.11324<System.Object>()#G2_C686::ClassMethod1347.MI.11325<System.Object>()#G2_C686::ClassMethod2733.11323<System.Object>()#G2_C686::Method4.11317()#G2_C686::Method5.11319()#G2_C686::Method6.11320<System.Object>()#G2_C686::Method7.11322<System.Object>()#"
		call void Generated1239.Generated1239::M.G2_C686.T.T<class Generated1239.BaseClass1,class Generated1239.BaseClass0,class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C686::ClassMethod1346.MI.11324<System.Object>()#G2_C686::ClassMethod1347.MI.11325<System.Object>()#G2_C686::ClassMethod2733.11323<System.Object>()#G2_C686::Method4.11317()#G2_C686::Method5.11319()#G2_C686::Method6.11320<System.Object>()#G2_C686::Method7.11322<System.Object>()#"
		call void Generated1239.Generated1239::M.G2_C686.B.T<class Generated1239.BaseClass0,class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C686::ClassMethod1346.MI.11324<System.Object>()#G2_C686::ClassMethod1347.MI.11325<System.Object>()#G2_C686::ClassMethod2733.11323<System.Object>()#G2_C686::Method4.11317()#G2_C686::Method5.11319()#G2_C686::Method6.11320<System.Object>()#G2_C686::Method7.11322<System.Object>()#"
		call void Generated1239.Generated1239::M.G2_C686.B.A<class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C686::Method4.MI.11318()#G2_C686::Method5.11319()#G2_C686::Method6.MI.11321<System.Object>()#"
		call void Generated1239.Generated1239::M.IBase1.T<class Generated1239.BaseClass1,class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C686::Method4.MI.11318()#G2_C686::Method5.11319()#G2_C686::Method6.MI.11321<System.Object>()#"
		call void Generated1239.Generated1239::M.IBase1.B<class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>>(!!0,string)
		newobj instance void class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C686::ClassMethod1346.MI.11324<System.Object>()#G2_C686::ClassMethod1347.MI.11325<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G2_C686::Method6.11320<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Generated1239.Generated1239::M.G1_C12.T.T<class Generated1239.BaseClass1,class Generated1239.BaseClass1,class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C686::ClassMethod1346.MI.11324<System.Object>()#G2_C686::ClassMethod1347.MI.11325<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G2_C686::Method6.11320<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Generated1239.Generated1239::M.G1_C12.B.T<class Generated1239.BaseClass1,class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C686::ClassMethod1346.MI.11324<System.Object>()#G2_C686::ClassMethod1347.MI.11325<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G2_C686::Method6.11320<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Generated1239.Generated1239::M.G1_C12.B.B<class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C686::Method7.11322<System.Object>()#"
		call void Generated1239.Generated1239::M.IBase2.T.T<class Generated1239.BaseClass1,class Generated1239.BaseClass1,class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C686::Method7.11322<System.Object>()#"
		call void Generated1239.Generated1239::M.IBase2.B.T<class Generated1239.BaseClass1,class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C686::Method7.11322<System.Object>()#"
		call void Generated1239.Generated1239::M.IBase2.B.B<class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C686::Method4.MI.11318()#G2_C686::Method5.11319()#G2_C686::Method6.MI.11321<System.Object>()#"
		call void Generated1239.Generated1239::M.IBase1.T<class Generated1239.BaseClass0,class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C686::Method4.MI.11318()#G2_C686::Method5.11319()#G2_C686::Method6.MI.11321<System.Object>()#"
		call void Generated1239.Generated1239::M.IBase1.A<class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C686::Method7.11322<System.Object>()#"
		call void Generated1239.Generated1239::M.IBase2.T.T<class Generated1239.BaseClass0,class Generated1239.BaseClass1,class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C686::Method7.11322<System.Object>()#"
		call void Generated1239.Generated1239::M.IBase2.A.T<class Generated1239.BaseClass1,class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C686::Method7.11322<System.Object>()#"
		call void Generated1239.Generated1239::M.IBase2.A.B<class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C686::ClassMethod1346.MI.11324<System.Object>()#G2_C686::ClassMethod1347.MI.11325<System.Object>()#G2_C686::ClassMethod2733.11323<System.Object>()#G2_C686::Method4.11317()#G2_C686::Method5.11319()#G2_C686::Method6.11320<System.Object>()#G2_C686::Method7.11322<System.Object>()#"
		call void Generated1239.Generated1239::M.G2_C686.T.T<class Generated1239.BaseClass1,class Generated1239.BaseClass1,class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C686::ClassMethod1346.MI.11324<System.Object>()#G2_C686::ClassMethod1347.MI.11325<System.Object>()#G2_C686::ClassMethod2733.11323<System.Object>()#G2_C686::Method4.11317()#G2_C686::Method5.11319()#G2_C686::Method6.11320<System.Object>()#G2_C686::Method7.11322<System.Object>()#"
		call void Generated1239.Generated1239::M.G2_C686.B.T<class Generated1239.BaseClass1,class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C686::ClassMethod1346.MI.11324<System.Object>()#G2_C686::ClassMethod1347.MI.11325<System.Object>()#G2_C686::ClassMethod2733.11323<System.Object>()#G2_C686::Method4.11317()#G2_C686::Method5.11319()#G2_C686::Method6.11320<System.Object>()#G2_C686::Method7.11322<System.Object>()#"
		call void Generated1239.Generated1239::M.G2_C686.B.B<class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C686::Method4.MI.11318()#G2_C686::Method5.11319()#G2_C686::Method6.MI.11321<System.Object>()#"
		call void Generated1239.Generated1239::M.IBase1.T<class Generated1239.BaseClass1,class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C686::Method4.MI.11318()#G2_C686::Method5.11319()#G2_C686::Method6.MI.11321<System.Object>()#"
		call void Generated1239.Generated1239::M.IBase1.B<class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>>(!!0,string)
		newobj instance void class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()#G1_C12::ClassMethod1347.4870<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Generated1239.Generated1239::M.G1_C12.T.T<class Generated1239.BaseClass0,class Generated1239.BaseClass0,class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()#G1_C12::ClassMethod1347.4870<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Generated1239.Generated1239::M.G1_C12.A.T<class Generated1239.BaseClass0,class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()#G1_C12::ClassMethod1347.4870<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Generated1239.Generated1239::M.G1_C12.A.A<class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated1239.Generated1239::M.IBase2.T.T<class Generated1239.BaseClass0,class Generated1239.BaseClass0,class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated1239.Generated1239::M.IBase2.A.T<class Generated1239.BaseClass0,class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated1239.Generated1239::M.IBase2.A.A<class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.MI.4868<System.Object>()#"
		call void Generated1239.Generated1239::M.IBase1.T<class Generated1239.BaseClass0,class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.MI.4868<System.Object>()#"
		call void Generated1239.Generated1239::M.IBase1.A<class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated1239.Generated1239::M.IBase2.T.T<class Generated1239.BaseClass0,class Generated1239.BaseClass1,class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated1239.Generated1239::M.IBase2.A.T<class Generated1239.BaseClass1,class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated1239.Generated1239::M.IBase2.A.B<class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>>(!!0,string)
		newobj instance void class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()#G1_C12::ClassMethod1347.4870<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Generated1239.Generated1239::M.G1_C12.T.T<class Generated1239.BaseClass0,class Generated1239.BaseClass1,class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()#G1_C12::ClassMethod1347.4870<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Generated1239.Generated1239::M.G1_C12.A.T<class Generated1239.BaseClass1,class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()#G1_C12::ClassMethod1347.4870<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Generated1239.Generated1239::M.G1_C12.A.B<class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated1239.Generated1239::M.IBase2.T.T<class Generated1239.BaseClass0,class Generated1239.BaseClass1,class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated1239.Generated1239::M.IBase2.A.T<class Generated1239.BaseClass1,class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated1239.Generated1239::M.IBase2.A.B<class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.MI.4868<System.Object>()#"
		call void Generated1239.Generated1239::M.IBase1.T<class Generated1239.BaseClass0,class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.MI.4868<System.Object>()#"
		call void Generated1239.Generated1239::M.IBase1.A<class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>>(!!0,string)
		newobj instance void class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()#G1_C12::ClassMethod1347.4870<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Generated1239.Generated1239::M.G1_C12.T.T<class Generated1239.BaseClass1,class Generated1239.BaseClass0,class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()#G1_C12::ClassMethod1347.4870<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Generated1239.Generated1239::M.G1_C12.B.T<class Generated1239.BaseClass0,class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()#G1_C12::ClassMethod1347.4870<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Generated1239.Generated1239::M.G1_C12.B.A<class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated1239.Generated1239::M.IBase2.T.T<class Generated1239.BaseClass1,class Generated1239.BaseClass0,class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated1239.Generated1239::M.IBase2.B.T<class Generated1239.BaseClass0,class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated1239.Generated1239::M.IBase2.B.A<class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.MI.4868<System.Object>()#"
		call void Generated1239.Generated1239::M.IBase1.T<class Generated1239.BaseClass0,class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.MI.4868<System.Object>()#"
		call void Generated1239.Generated1239::M.IBase1.A<class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated1239.Generated1239::M.IBase2.T.T<class Generated1239.BaseClass0,class Generated1239.BaseClass0,class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated1239.Generated1239::M.IBase2.A.T<class Generated1239.BaseClass0,class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated1239.Generated1239::M.IBase2.A.A<class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated1239.Generated1239::M.IBase2.T.T<class Generated1239.BaseClass0,class Generated1239.BaseClass1,class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated1239.Generated1239::M.IBase2.A.T<class Generated1239.BaseClass1,class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated1239.Generated1239::M.IBase2.A.B<class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated1239.Generated1239::M.IBase2.T.T<class Generated1239.BaseClass1,class Generated1239.BaseClass1,class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated1239.Generated1239::M.IBase2.B.T<class Generated1239.BaseClass1,class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated1239.Generated1239::M.IBase2.B.B<class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>>(!!0,string)
		newobj instance void class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()#G1_C12::ClassMethod1347.4870<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Generated1239.Generated1239::M.G1_C12.T.T<class Generated1239.BaseClass1,class Generated1239.BaseClass1,class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()#G1_C12::ClassMethod1347.4870<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Generated1239.Generated1239::M.G1_C12.B.T<class Generated1239.BaseClass1,class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()#G1_C12::ClassMethod1347.4870<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Generated1239.Generated1239::M.G1_C12.B.B<class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated1239.Generated1239::M.IBase2.T.T<class Generated1239.BaseClass1,class Generated1239.BaseClass1,class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated1239.Generated1239::M.IBase2.B.T<class Generated1239.BaseClass1,class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated1239.Generated1239::M.IBase2.B.B<class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.MI.4868<System.Object>()#"
		call void Generated1239.Generated1239::M.IBase1.T<class Generated1239.BaseClass0,class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.MI.4868<System.Object>()#"
		call void Generated1239.Generated1239::M.IBase1.A<class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated1239.Generated1239::M.IBase2.T.T<class Generated1239.BaseClass0,class Generated1239.BaseClass1,class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated1239.Generated1239::M.IBase2.A.T<class Generated1239.BaseClass1,class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated1239.Generated1239::M.IBase2.A.B<class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>>(!!0,string)
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
		newobj instance void class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>::ClassMethod1347<object>()
		calli default string(class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>)
		ldstr "G2_C686::ClassMethod1347.MI.11325<System.Object>()"
		ldstr "class G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1> on type class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>::ClassMethod1346<object>()
		calli default string(class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>)
		ldstr "G2_C686::ClassMethod1346.MI.11324<System.Object>()"
		ldstr "class G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1> on type class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>::Method6<object>()
		calli default string(class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>)
		ldstr "G3_C1711::Method6.17584<System.Object>()"
		ldstr "class G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1> on type class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>::Method5()
		calli default string(class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>)
		ldstr "G1_C12::Method5.4866()"
		ldstr "class G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1> on type class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>::Method4()
		calli default string(class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>)
		ldstr "G1_C12::Method4.4865()"
		ldstr "class G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1> on type class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>::Method7<object>()
		calli default string(class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>)
		ldstr "G1_C12::Method7.4863<System.Object>()"
		ldstr "class G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1> on type class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1239.IBase2`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>::Method7<object>()
		calli default string(class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>)
		ldstr "G2_C686::Method7.11322<System.Object>()"
		ldstr "class IBase2`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1> on type class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1239.IBase1`1<class Generated1239.BaseClass0>::Method4()
		calli default string(class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>)
		ldstr "G3_C1711::Method4.MI.17582()"
		ldstr "class IBase1`1<class Generated1239.BaseClass0> on type class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1239.IBase1`1<class Generated1239.BaseClass0>::Method5()
		calli default string(class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>)
		ldstr "G3_C1711::Method5.17583()"
		ldstr "class IBase1`1<class Generated1239.BaseClass0> on type class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1239.IBase1`1<class Generated1239.BaseClass0>::Method6<object>()
		calli default string(class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>)
		ldstr "G3_C1711::Method6.MI.17585<System.Object>()"
		ldstr "class IBase1`1<class Generated1239.BaseClass0> on type class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>::ClassMethod2733<object>()
		calli default string(class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>)
		ldstr "G3_C1711::ClassMethod2733.MI.17590<System.Object>()"
		ldstr "class G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0> on type class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>::Method7<object>()
		calli default string(class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>)
		ldstr "G2_C686::Method7.11322<System.Object>()"
		ldstr "class G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0> on type class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>::Method6<object>()
		calli default string(class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>)
		ldstr "G3_C1711::Method6.17584<System.Object>()"
		ldstr "class G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0> on type class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>::Method5()
		calli default string(class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>)
		ldstr "G3_C1711::Method5.17583()"
		ldstr "class G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0> on type class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>::Method4()
		calli default string(class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>)
		ldstr "G3_C1711::Method4.17581()"
		ldstr "class G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0> on type class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>::ClassMethod1347<object>()
		calli default string(class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>)
		ldstr "G2_C686::ClassMethod1347.MI.11325<System.Object>()"
		ldstr "class G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0> on type class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>::ClassMethod1346<object>()
		calli default string(class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>)
		ldstr "G2_C686::ClassMethod1346.MI.11324<System.Object>()"
		ldstr "class G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0> on type class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1239.IBase1`1<class Generated1239.BaseClass1>::Method4()
		calli default string(class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>)
		ldstr "G2_C686::Method4.MI.11318()"
		ldstr "class IBase1`1<class Generated1239.BaseClass1> on type class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1239.IBase1`1<class Generated1239.BaseClass1>::Method5()
		calli default string(class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>)
		ldstr "G3_C1711::Method5.17583()"
		ldstr "class IBase1`1<class Generated1239.BaseClass1> on type class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1239.IBase1`1<class Generated1239.BaseClass1>::Method6<object>()
		calli default string(class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>)
		ldstr "G2_C686::Method6.MI.11321<System.Object>()"
		ldstr "class IBase1`1<class Generated1239.BaseClass1> on type class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>::ClassMethod4824<object>()
		calli default string(class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>)
		ldstr "G3_C1711::ClassMethod4824.17589<System.Object>()"
		ldstr "class G3_C1711`1<class Generated1239.BaseClass0> on type class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>::ClassMethod4823<object>()
		calli default string(class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>)
		ldstr "G3_C1711::ClassMethod4823.17588<System.Object>()"
		ldstr "class G3_C1711`1<class Generated1239.BaseClass0> on type class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>::ClassMethod4822()
		calli default string(class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>)
		ldstr "G3_C1711::ClassMethod4822.17587()"
		ldstr "class G3_C1711`1<class Generated1239.BaseClass0> on type class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>::ClassMethod4821()
		calli default string(class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>)
		ldstr "G3_C1711::ClassMethod4821.17586()"
		ldstr "class G3_C1711`1<class Generated1239.BaseClass0> on type class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>::Method6<object>()
		calli default string(class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>)
		ldstr "G3_C1711::Method6.17584<System.Object>()"
		ldstr "class G3_C1711`1<class Generated1239.BaseClass0> on type class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>::Method5()
		calli default string(class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>)
		ldstr "G3_C1711::Method5.17583()"
		ldstr "class G3_C1711`1<class Generated1239.BaseClass0> on type class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>::Method4()
		calli default string(class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>)
		ldstr "G3_C1711::Method4.17581()"
		ldstr "class G3_C1711`1<class Generated1239.BaseClass0> on type class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>::ClassMethod2733<object>()
		calli default string(class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>)
		ldstr "G3_C1711::ClassMethod2733.MI.17590<System.Object>()"
		ldstr "class G3_C1711`1<class Generated1239.BaseClass0> on type class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>::Method7<object>()
		calli default string(class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>)
		ldstr "G2_C686::Method7.11322<System.Object>()"
		ldstr "class G3_C1711`1<class Generated1239.BaseClass0> on type class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>::ClassMethod1347<object>()
		calli default string(class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>)
		ldstr "G2_C686::ClassMethod1347.MI.11325<System.Object>()"
		ldstr "class G3_C1711`1<class Generated1239.BaseClass0> on type class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>::ClassMethod1346<object>()
		calli default string(class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>)
		ldstr "G2_C686::ClassMethod1346.MI.11324<System.Object>()"
		ldstr "class G3_C1711`1<class Generated1239.BaseClass0> on type class Generated1239.G3_C1711`1<class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>::ClassMethod1347<object>()
		calli default string(class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>)
		ldstr "G2_C686::ClassMethod1347.MI.11325<System.Object>()"
		ldstr "class G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1> on type class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>::ClassMethod1346<object>()
		calli default string(class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>)
		ldstr "G2_C686::ClassMethod1346.MI.11324<System.Object>()"
		ldstr "class G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1> on type class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>::Method6<object>()
		calli default string(class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>)
		ldstr "G3_C1711::Method6.17584<System.Object>()"
		ldstr "class G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1> on type class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>::Method5()
		calli default string(class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>)
		ldstr "G1_C12::Method5.4866()"
		ldstr "class G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1> on type class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>::Method4()
		calli default string(class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>)
		ldstr "G1_C12::Method4.4865()"
		ldstr "class G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1> on type class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>::Method7<object>()
		calli default string(class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>)
		ldstr "G1_C12::Method7.4863<System.Object>()"
		ldstr "class G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1> on type class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1239.IBase2`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>::Method7<object>()
		calli default string(class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>)
		ldstr "G2_C686::Method7.11322<System.Object>()"
		ldstr "class IBase2`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1> on type class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1239.IBase1`1<class Generated1239.BaseClass0>::Method4()
		calli default string(class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>)
		ldstr "G3_C1711::Method4.MI.17582()"
		ldstr "class IBase1`1<class Generated1239.BaseClass0> on type class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1239.IBase1`1<class Generated1239.BaseClass0>::Method5()
		calli default string(class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>)
		ldstr "G3_C1711::Method5.17583()"
		ldstr "class IBase1`1<class Generated1239.BaseClass0> on type class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1239.IBase1`1<class Generated1239.BaseClass0>::Method6<object>()
		calli default string(class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>)
		ldstr "G3_C1711::Method6.MI.17585<System.Object>()"
		ldstr "class IBase1`1<class Generated1239.BaseClass0> on type class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1239.IBase2`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>::Method7<object>()
		calli default string(class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>)
		ldstr "G2_C686::Method7.11322<System.Object>()"
		ldstr "class IBase2`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1> on type class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>::ClassMethod2733<object>()
		calli default string(class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>)
		ldstr "G3_C1711::ClassMethod2733.MI.17590<System.Object>()"
		ldstr "class G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1> on type class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>::Method7<object>()
		calli default string(class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>)
		ldstr "G2_C686::Method7.11322<System.Object>()"
		ldstr "class G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1> on type class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>::Method6<object>()
		calli default string(class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>)
		ldstr "G3_C1711::Method6.17584<System.Object>()"
		ldstr "class G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1> on type class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>::Method5()
		calli default string(class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>)
		ldstr "G3_C1711::Method5.17583()"
		ldstr "class G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1> on type class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>::Method4()
		calli default string(class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>)
		ldstr "G3_C1711::Method4.17581()"
		ldstr "class G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1> on type class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>::ClassMethod1347<object>()
		calli default string(class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>)
		ldstr "G2_C686::ClassMethod1347.MI.11325<System.Object>()"
		ldstr "class G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1> on type class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>::ClassMethod1346<object>()
		calli default string(class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>)
		ldstr "G2_C686::ClassMethod1346.MI.11324<System.Object>()"
		ldstr "class G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1> on type class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1239.IBase1`1<class Generated1239.BaseClass1>::Method4()
		calli default string(class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>)
		ldstr "G2_C686::Method4.MI.11318()"
		ldstr "class IBase1`1<class Generated1239.BaseClass1> on type class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1239.IBase1`1<class Generated1239.BaseClass1>::Method5()
		calli default string(class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>)
		ldstr "G3_C1711::Method5.17583()"
		ldstr "class IBase1`1<class Generated1239.BaseClass1> on type class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1239.IBase1`1<class Generated1239.BaseClass1>::Method6<object>()
		calli default string(class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>)
		ldstr "G2_C686::Method6.MI.11321<System.Object>()"
		ldstr "class IBase1`1<class Generated1239.BaseClass1> on type class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>::ClassMethod4824<object>()
		calli default string(class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>)
		ldstr "G3_C1711::ClassMethod4824.17589<System.Object>()"
		ldstr "class G3_C1711`1<class Generated1239.BaseClass1> on type class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>::ClassMethod4823<object>()
		calli default string(class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>)
		ldstr "G3_C1711::ClassMethod4823.17588<System.Object>()"
		ldstr "class G3_C1711`1<class Generated1239.BaseClass1> on type class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>::ClassMethod4822()
		calli default string(class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>)
		ldstr "G3_C1711::ClassMethod4822.17587()"
		ldstr "class G3_C1711`1<class Generated1239.BaseClass1> on type class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>::ClassMethod4821()
		calli default string(class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>)
		ldstr "G3_C1711::ClassMethod4821.17586()"
		ldstr "class G3_C1711`1<class Generated1239.BaseClass1> on type class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>::Method6<object>()
		calli default string(class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>)
		ldstr "G3_C1711::Method6.17584<System.Object>()"
		ldstr "class G3_C1711`1<class Generated1239.BaseClass1> on type class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>::Method5()
		calli default string(class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>)
		ldstr "G3_C1711::Method5.17583()"
		ldstr "class G3_C1711`1<class Generated1239.BaseClass1> on type class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>::Method4()
		calli default string(class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>)
		ldstr "G3_C1711::Method4.17581()"
		ldstr "class G3_C1711`1<class Generated1239.BaseClass1> on type class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>::ClassMethod2733<object>()
		calli default string(class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>)
		ldstr "G3_C1711::ClassMethod2733.MI.17590<System.Object>()"
		ldstr "class G3_C1711`1<class Generated1239.BaseClass1> on type class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>::Method7<object>()
		calli default string(class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>)
		ldstr "G2_C686::Method7.11322<System.Object>()"
		ldstr "class G3_C1711`1<class Generated1239.BaseClass1> on type class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>::ClassMethod1347<object>()
		calli default string(class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>)
		ldstr "G2_C686::ClassMethod1347.MI.11325<System.Object>()"
		ldstr "class G3_C1711`1<class Generated1239.BaseClass1> on type class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>::ClassMethod1346<object>()
		calli default string(class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>)
		ldstr "G2_C686::ClassMethod1346.MI.11324<System.Object>()"
		ldstr "class G3_C1711`1<class Generated1239.BaseClass1> on type class Generated1239.G3_C1711`1<class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>::ClassMethod1347<object>()
		calli default string(class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>)
		ldstr "G2_C686::ClassMethod1347.MI.11325<System.Object>()"
		ldstr "class G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>::ClassMethod1346<object>()
		calli default string(class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>)
		ldstr "G2_C686::ClassMethod1346.MI.11324<System.Object>()"
		ldstr "class G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>::Method6<object>()
		calli default string(class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>)
		ldstr "G2_C686::Method6.11320<System.Object>()"
		ldstr "class G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>::Method5()
		calli default string(class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>)
		ldstr "G1_C12::Method5.4866()"
		ldstr "class G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>::Method4()
		calli default string(class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>)
		ldstr "G1_C12::Method4.4865()"
		ldstr "class G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>::Method7<object>()
		calli default string(class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>)
		ldstr "G1_C12::Method7.4863<System.Object>()"
		ldstr "class G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1239.IBase2`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>::Method7<object>()
		calli default string(class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>)
		ldstr "G2_C686::Method7.11322<System.Object>()"
		ldstr "class IBase2`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1239.IBase1`1<class Generated1239.BaseClass0>::Method4()
		calli default string(class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>)
		ldstr "G2_C686::Method4.MI.11318()"
		ldstr "class IBase1`1<class Generated1239.BaseClass0> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1239.IBase1`1<class Generated1239.BaseClass0>::Method5()
		calli default string(class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>)
		ldstr "G2_C686::Method5.11319()"
		ldstr "class IBase1`1<class Generated1239.BaseClass0> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1239.IBase1`1<class Generated1239.BaseClass0>::Method6<object>()
		calli default string(class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>)
		ldstr "G2_C686::Method6.MI.11321<System.Object>()"
		ldstr "class IBase1`1<class Generated1239.BaseClass0> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1239.IBase2`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>::Method7<object>()
		calli default string(class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>)
		ldstr "G2_C686::Method7.11322<System.Object>()"
		ldstr "class IBase2`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>::ClassMethod2733<object>()
		calli default string(class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>)
		ldstr "G2_C686::ClassMethod2733.11323<System.Object>()"
		ldstr "class G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>::Method7<object>()
		calli default string(class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>)
		ldstr "G2_C686::Method7.11322<System.Object>()"
		ldstr "class G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>::Method6<object>()
		calli default string(class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>)
		ldstr "G2_C686::Method6.11320<System.Object>()"
		ldstr "class G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>::Method5()
		calli default string(class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>)
		ldstr "G2_C686::Method5.11319()"
		ldstr "class G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>::Method4()
		calli default string(class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>)
		ldstr "G2_C686::Method4.11317()"
		ldstr "class G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>::ClassMethod1347<object>()
		calli default string(class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>)
		ldstr "G2_C686::ClassMethod1347.MI.11325<System.Object>()"
		ldstr "class G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>::ClassMethod1346<object>()
		calli default string(class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>)
		ldstr "G2_C686::ClassMethod1346.MI.11324<System.Object>()"
		ldstr "class G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>::ClassMethod1347<object>()
		calli default string(class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>)
		ldstr "G2_C686::ClassMethod1347.MI.11325<System.Object>()"
		ldstr "class G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>::ClassMethod1346<object>()
		calli default string(class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>)
		ldstr "G2_C686::ClassMethod1346.MI.11324<System.Object>()"
		ldstr "class G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>::Method6<object>()
		calli default string(class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>)
		ldstr "G2_C686::Method6.11320<System.Object>()"
		ldstr "class G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>::Method5()
		calli default string(class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>)
		ldstr "G1_C12::Method5.4866()"
		ldstr "class G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>::Method4()
		calli default string(class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>)
		ldstr "G1_C12::Method4.4865()"
		ldstr "class G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>::Method7<object>()
		calli default string(class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>)
		ldstr "G1_C12::Method7.4863<System.Object>()"
		ldstr "class G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1239.IBase2`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>::Method7<object>()
		calli default string(class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>)
		ldstr "G2_C686::Method7.11322<System.Object>()"
		ldstr "class IBase2`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1239.IBase1`1<class Generated1239.BaseClass0>::Method4()
		calli default string(class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>)
		ldstr "G2_C686::Method4.MI.11318()"
		ldstr "class IBase1`1<class Generated1239.BaseClass0> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1239.IBase1`1<class Generated1239.BaseClass0>::Method5()
		calli default string(class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>)
		ldstr "G2_C686::Method5.11319()"
		ldstr "class IBase1`1<class Generated1239.BaseClass0> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1239.IBase1`1<class Generated1239.BaseClass0>::Method6<object>()
		calli default string(class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>)
		ldstr "G2_C686::Method6.MI.11321<System.Object>()"
		ldstr "class IBase1`1<class Generated1239.BaseClass0> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1239.IBase2`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>::Method7<object>()
		calli default string(class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>)
		ldstr "G2_C686::Method7.11322<System.Object>()"
		ldstr "class IBase2`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1239.IBase2`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>::Method7<object>()
		calli default string(class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>)
		ldstr "G2_C686::Method7.11322<System.Object>()"
		ldstr "class IBase2`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1239.IBase2`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>::Method7<object>()
		calli default string(class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>)
		ldstr "G2_C686::Method7.11322<System.Object>()"
		ldstr "class IBase2`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>::ClassMethod2733<object>()
		calli default string(class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>)
		ldstr "G2_C686::ClassMethod2733.11323<System.Object>()"
		ldstr "class G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>::Method7<object>()
		calli default string(class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>)
		ldstr "G2_C686::Method7.11322<System.Object>()"
		ldstr "class G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>::Method6<object>()
		calli default string(class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>)
		ldstr "G2_C686::Method6.11320<System.Object>()"
		ldstr "class G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>::Method5()
		calli default string(class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>)
		ldstr "G2_C686::Method5.11319()"
		ldstr "class G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>::Method4()
		calli default string(class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>)
		ldstr "G2_C686::Method4.11317()"
		ldstr "class G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>::ClassMethod1347<object>()
		calli default string(class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>)
		ldstr "G2_C686::ClassMethod1347.MI.11325<System.Object>()"
		ldstr "class G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>::ClassMethod1346<object>()
		calli default string(class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>)
		ldstr "G2_C686::ClassMethod1346.MI.11324<System.Object>()"
		ldstr "class G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>::ClassMethod1347<object>()
		calli default string(class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>)
		ldstr "G2_C686::ClassMethod1347.MI.11325<System.Object>()"
		ldstr "class G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>::ClassMethod1346<object>()
		calli default string(class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>)
		ldstr "G2_C686::ClassMethod1346.MI.11324<System.Object>()"
		ldstr "class G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>::Method6<object>()
		calli default string(class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>)
		ldstr "G2_C686::Method6.11320<System.Object>()"
		ldstr "class G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>::Method5()
		calli default string(class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>)
		ldstr "G1_C12::Method5.4866()"
		ldstr "class G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>::Method4()
		calli default string(class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>)
		ldstr "G1_C12::Method4.4865()"
		ldstr "class G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>::Method7<object>()
		calli default string(class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>)
		ldstr "G1_C12::Method7.4863<System.Object>()"
		ldstr "class G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1239.IBase2`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>::Method7<object>()
		calli default string(class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>)
		ldstr "G2_C686::Method7.11322<System.Object>()"
		ldstr "class IBase2`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1239.IBase1`1<class Generated1239.BaseClass0>::Method4()
		calli default string(class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>)
		ldstr "G2_C686::Method4.MI.11318()"
		ldstr "class IBase1`1<class Generated1239.BaseClass0> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1239.IBase1`1<class Generated1239.BaseClass0>::Method5()
		calli default string(class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>)
		ldstr "G2_C686::Method5.11319()"
		ldstr "class IBase1`1<class Generated1239.BaseClass0> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1239.IBase1`1<class Generated1239.BaseClass0>::Method6<object>()
		calli default string(class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>)
		ldstr "G2_C686::Method6.MI.11321<System.Object>()"
		ldstr "class IBase1`1<class Generated1239.BaseClass0> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>::ClassMethod2733<object>()
		calli default string(class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>)
		ldstr "G2_C686::ClassMethod2733.11323<System.Object>()"
		ldstr "class G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>::Method7<object>()
		calli default string(class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>)
		ldstr "G2_C686::Method7.11322<System.Object>()"
		ldstr "class G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>::Method6<object>()
		calli default string(class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>)
		ldstr "G2_C686::Method6.11320<System.Object>()"
		ldstr "class G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>::Method5()
		calli default string(class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>)
		ldstr "G2_C686::Method5.11319()"
		ldstr "class G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>::Method4()
		calli default string(class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>)
		ldstr "G2_C686::Method4.11317()"
		ldstr "class G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>::ClassMethod1347<object>()
		calli default string(class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>)
		ldstr "G2_C686::ClassMethod1347.MI.11325<System.Object>()"
		ldstr "class G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>::ClassMethod1346<object>()
		calli default string(class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>)
		ldstr "G2_C686::ClassMethod1346.MI.11324<System.Object>()"
		ldstr "class G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1239.IBase1`1<class Generated1239.BaseClass1>::Method4()
		calli default string(class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>)
		ldstr "G2_C686::Method4.MI.11318()"
		ldstr "class IBase1`1<class Generated1239.BaseClass1> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1239.IBase1`1<class Generated1239.BaseClass1>::Method5()
		calli default string(class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>)
		ldstr "G2_C686::Method5.11319()"
		ldstr "class IBase1`1<class Generated1239.BaseClass1> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1239.IBase1`1<class Generated1239.BaseClass1>::Method6<object>()
		calli default string(class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>)
		ldstr "G2_C686::Method6.MI.11321<System.Object>()"
		ldstr "class IBase1`1<class Generated1239.BaseClass1> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>::ClassMethod1347<object>()
		calli default string(class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>)
		ldstr "G2_C686::ClassMethod1347.MI.11325<System.Object>()"
		ldstr "class G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>::ClassMethod1346<object>()
		calli default string(class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>)
		ldstr "G2_C686::ClassMethod1346.MI.11324<System.Object>()"
		ldstr "class G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>::Method6<object>()
		calli default string(class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>)
		ldstr "G2_C686::Method6.11320<System.Object>()"
		ldstr "class G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>::Method5()
		calli default string(class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>)
		ldstr "G1_C12::Method5.4866()"
		ldstr "class G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>::Method4()
		calli default string(class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>)
		ldstr "G1_C12::Method4.4865()"
		ldstr "class G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>::Method7<object>()
		calli default string(class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>)
		ldstr "G1_C12::Method7.4863<System.Object>()"
		ldstr "class G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1239.IBase2`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>::Method7<object>()
		calli default string(class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>)
		ldstr "G2_C686::Method7.11322<System.Object>()"
		ldstr "class IBase2`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1239.IBase1`1<class Generated1239.BaseClass0>::Method4()
		calli default string(class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>)
		ldstr "G2_C686::Method4.MI.11318()"
		ldstr "class IBase1`1<class Generated1239.BaseClass0> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1239.IBase1`1<class Generated1239.BaseClass0>::Method5()
		calli default string(class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>)
		ldstr "G2_C686::Method5.11319()"
		ldstr "class IBase1`1<class Generated1239.BaseClass0> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1239.IBase1`1<class Generated1239.BaseClass0>::Method6<object>()
		calli default string(class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>)
		ldstr "G2_C686::Method6.MI.11321<System.Object>()"
		ldstr "class IBase1`1<class Generated1239.BaseClass0> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1239.IBase2`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>::Method7<object>()
		calli default string(class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>)
		ldstr "G2_C686::Method7.11322<System.Object>()"
		ldstr "class IBase2`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>::ClassMethod2733<object>()
		calli default string(class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>)
		ldstr "G2_C686::ClassMethod2733.11323<System.Object>()"
		ldstr "class G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>::Method7<object>()
		calli default string(class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>)
		ldstr "G2_C686::Method7.11322<System.Object>()"
		ldstr "class G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>::Method6<object>()
		calli default string(class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>)
		ldstr "G2_C686::Method6.11320<System.Object>()"
		ldstr "class G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>::Method5()
		calli default string(class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>)
		ldstr "G2_C686::Method5.11319()"
		ldstr "class G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>::Method4()
		calli default string(class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>)
		ldstr "G2_C686::Method4.11317()"
		ldstr "class G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>::ClassMethod1347<object>()
		calli default string(class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>)
		ldstr "G2_C686::ClassMethod1347.MI.11325<System.Object>()"
		ldstr "class G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>::ClassMethod1346<object>()
		calli default string(class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>)
		ldstr "G2_C686::ClassMethod1346.MI.11324<System.Object>()"
		ldstr "class G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1239.IBase1`1<class Generated1239.BaseClass1>::Method4()
		calli default string(class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>)
		ldstr "G2_C686::Method4.MI.11318()"
		ldstr "class IBase1`1<class Generated1239.BaseClass1> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1239.IBase1`1<class Generated1239.BaseClass1>::Method5()
		calli default string(class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>)
		ldstr "G2_C686::Method5.11319()"
		ldstr "class IBase1`1<class Generated1239.BaseClass1> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1239.IBase1`1<class Generated1239.BaseClass1>::Method6<object>()
		calli default string(class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>)
		ldstr "G2_C686::Method6.MI.11321<System.Object>()"
		ldstr "class IBase1`1<class Generated1239.BaseClass1> on type class Generated1239.G2_C686`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>::ClassMethod1347<object>()
		calli default string(class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>)
		ldstr "G1_C12::ClassMethod1347.4870<System.Object>()"
		ldstr "class G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0> on type class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>::ClassMethod1346<object>()
		calli default string(class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>)
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()"
		ldstr "class G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0> on type class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>::Method6<object>()
		calli default string(class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>)
		ldstr "G1_C12::Method6.4867<System.Object>()"
		ldstr "class G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0> on type class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>::Method5()
		calli default string(class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>)
		ldstr "G1_C12::Method5.4866()"
		ldstr "class G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0> on type class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>::Method4()
		calli default string(class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>)
		ldstr "G1_C12::Method4.4865()"
		ldstr "class G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0> on type class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>::Method7<object>()
		calli default string(class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>)
		ldstr "G1_C12::Method7.4863<System.Object>()"
		ldstr "class G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0> on type class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1239.IBase2`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>::Method7<object>()
		calli default string(class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>)
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0> on type class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1239.IBase1`1<class Generated1239.BaseClass0>::Method4()
		calli default string(class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>)
		ldstr "G1_C12::Method4.4865()"
		ldstr "class IBase1`1<class Generated1239.BaseClass0> on type class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1239.IBase1`1<class Generated1239.BaseClass0>::Method5()
		calli default string(class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>)
		ldstr "G1_C12::Method5.4866()"
		ldstr "class IBase1`1<class Generated1239.BaseClass0> on type class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1239.IBase1`1<class Generated1239.BaseClass0>::Method6<object>()
		calli default string(class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>)
		ldstr "G1_C12::Method6.MI.4868<System.Object>()"
		ldstr "class IBase1`1<class Generated1239.BaseClass0> on type class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1239.IBase2`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>::Method7<object>()
		calli default string(class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>)
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1> on type class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>::ClassMethod1347<object>()
		calli default string(class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>)
		ldstr "G1_C12::ClassMethod1347.4870<System.Object>()"
		ldstr "class G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1> on type class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>::ClassMethod1346<object>()
		calli default string(class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>)
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()"
		ldstr "class G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1> on type class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>::Method6<object>()
		calli default string(class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>)
		ldstr "G1_C12::Method6.4867<System.Object>()"
		ldstr "class G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1> on type class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>::Method5()
		calli default string(class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>)
		ldstr "G1_C12::Method5.4866()"
		ldstr "class G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1> on type class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>::Method4()
		calli default string(class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>)
		ldstr "G1_C12::Method4.4865()"
		ldstr "class G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1> on type class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>::Method7<object>()
		calli default string(class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>)
		ldstr "G1_C12::Method7.4863<System.Object>()"
		ldstr "class G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1> on type class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1239.IBase2`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>::Method7<object>()
		calli default string(class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>)
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1> on type class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1239.IBase1`1<class Generated1239.BaseClass0>::Method4()
		calli default string(class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>)
		ldstr "G1_C12::Method4.4865()"
		ldstr "class IBase1`1<class Generated1239.BaseClass0> on type class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1239.IBase1`1<class Generated1239.BaseClass0>::Method5()
		calli default string(class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>)
		ldstr "G1_C12::Method5.4866()"
		ldstr "class IBase1`1<class Generated1239.BaseClass0> on type class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1239.IBase1`1<class Generated1239.BaseClass0>::Method6<object>()
		calli default string(class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>)
		ldstr "G1_C12::Method6.MI.4868<System.Object>()"
		ldstr "class IBase1`1<class Generated1239.BaseClass0> on type class Generated1239.G1_C12`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>::ClassMethod1347<object>()
		calli default string(class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>)
		ldstr "G1_C12::ClassMethod1347.4870<System.Object>()"
		ldstr "class G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0> on type class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>::ClassMethod1346<object>()
		calli default string(class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>)
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()"
		ldstr "class G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0> on type class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>::Method6<object>()
		calli default string(class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>)
		ldstr "G1_C12::Method6.4867<System.Object>()"
		ldstr "class G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0> on type class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>::Method5()
		calli default string(class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>)
		ldstr "G1_C12::Method5.4866()"
		ldstr "class G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0> on type class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>::Method4()
		calli default string(class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>)
		ldstr "G1_C12::Method4.4865()"
		ldstr "class G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0> on type class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>::Method7<object>()
		calli default string(class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>)
		ldstr "G1_C12::Method7.4863<System.Object>()"
		ldstr "class G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0> on type class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1239.IBase2`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>::Method7<object>()
		calli default string(class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>)
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0> on type class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1239.IBase1`1<class Generated1239.BaseClass0>::Method4()
		calli default string(class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>)
		ldstr "G1_C12::Method4.4865()"
		ldstr "class IBase1`1<class Generated1239.BaseClass0> on type class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1239.IBase1`1<class Generated1239.BaseClass0>::Method5()
		calli default string(class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>)
		ldstr "G1_C12::Method5.4866()"
		ldstr "class IBase1`1<class Generated1239.BaseClass0> on type class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1239.IBase1`1<class Generated1239.BaseClass0>::Method6<object>()
		calli default string(class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>)
		ldstr "G1_C12::Method6.MI.4868<System.Object>()"
		ldstr "class IBase1`1<class Generated1239.BaseClass0> on type class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1239.IBase2`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0>::Method7<object>()
		calli default string(class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>)
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class Generated1239.BaseClass0,class Generated1239.BaseClass0> on type class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1239.IBase2`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>::Method7<object>()
		calli default string(class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>)
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1> on type class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1239.IBase2`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>::Method7<object>()
		calli default string(class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>)
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1> on type class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>::ClassMethod1347<object>()
		calli default string(class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>)
		ldstr "G1_C12::ClassMethod1347.4870<System.Object>()"
		ldstr "class G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1> on type class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>::ClassMethod1346<object>()
		calli default string(class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>)
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()"
		ldstr "class G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1> on type class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>::Method6<object>()
		calli default string(class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>)
		ldstr "G1_C12::Method6.4867<System.Object>()"
		ldstr "class G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1> on type class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>::Method5()
		calli default string(class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>)
		ldstr "G1_C12::Method5.4866()"
		ldstr "class G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1> on type class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>::Method4()
		calli default string(class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>)
		ldstr "G1_C12::Method4.4865()"
		ldstr "class G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1> on type class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>::Method7<object>()
		calli default string(class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>)
		ldstr "G1_C12::Method7.4863<System.Object>()"
		ldstr "class G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1> on type class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1239.IBase2`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>::Method7<object>()
		calli default string(class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>)
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1> on type class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1239.IBase1`1<class Generated1239.BaseClass0>::Method4()
		calli default string(class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>)
		ldstr "G1_C12::Method4.4865()"
		ldstr "class IBase1`1<class Generated1239.BaseClass0> on type class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1239.IBase1`1<class Generated1239.BaseClass0>::Method5()
		calli default string(class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>)
		ldstr "G1_C12::Method5.4866()"
		ldstr "class IBase1`1<class Generated1239.BaseClass0> on type class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1239.IBase1`1<class Generated1239.BaseClass0>::Method6<object>()
		calli default string(class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>)
		ldstr "G1_C12::Method6.MI.4868<System.Object>()"
		ldstr "class IBase1`1<class Generated1239.BaseClass0> on type class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1239.IBase2`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1>::Method7<object>()
		calli default string(class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>)
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class Generated1239.BaseClass0,class Generated1239.BaseClass1> on type class Generated1239.G1_C12`2<class Generated1239.BaseClass1,class Generated1239.BaseClass1>"
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
		call void Generated1239.Generated1239::MethodCallingTest()
		call void Generated1239.Generated1239::ConstrainedCallsTest()
		call void Generated1239.Generated1239::StructConstrainedInterfaceCallsTest()
		call void Generated1239.Generated1239::CalliTest()
		ldc.i4 100
		ret
	}
}
