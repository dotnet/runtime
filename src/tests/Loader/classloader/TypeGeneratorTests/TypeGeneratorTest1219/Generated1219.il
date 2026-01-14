// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) .ver 4:0:0:0 }
.assembly extern TestFramework { .publickeytoken = ( B0 3F 5F 7F 11 D5 0A 3A )  }

//TYPES IN FORWARDER ASSEMBLIES:

//TEST ASSEMBLY:
.assembly extern xunit.core {}

.class public Generated1219.BaseClass0 
{
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void [mscorlib]System.Object::.ctor()
		ret
	}
}
.class public Generated1219.BaseClass1 
		extends Generated1219.BaseClass0
{
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void Generated1219.BaseClass0::.ctor()
		ret
	}
}
.class public Generated1219.G3_C1691`1<T0> 
		extends class Generated1219.G2_C675`2<class Generated1219.BaseClass1,!T0>
		implements class Generated1219.IBase2`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0> 
{
	.method public hidebysig newslot virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G3_C1691::Method7.17470<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>.Method7'<M0>() cil managed noinlining { 
		.override method instance string class Generated1219.IBase2`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>::Method7<[1]>()
		ldstr "G3_C1691::Method7.MI.17471<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod4779() cil managed noinlining { 
		ldstr "G3_C1691::ClassMethod4779.17472()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod4780<M0>() cil managed noinlining { 
		ldstr "G3_C1691::ClassMethod4780.17473<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod4781<M0>() cil managed noinlining { 
		ldstr "G3_C1691::ClassMethod4781.17474<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'G2_C675<class Generated1219.BaseClass1,T0>.ClassMethod2710'<M0>() cil managed noinlining { 
		.override method instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass1,!T0>::ClassMethod2710<[1]>()
		ldstr "G3_C1691::ClassMethod2710.MI.17475<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class Generated1219.G2_C675`2<class Generated1219.BaseClass1,!T0>::.ctor()
		ret
	}
}
.class public Generated1219.G2_C675`2<T0, T1> 
		extends class Generated1219.G1_C12`2<!T0,!T1>
		implements class Generated1219.IBase1`1<class Generated1219.BaseClass1>, class Generated1219.IBase2`2<class Generated1219.BaseClass0,!T1> 
{
	.method public hidebysig virtual instance string Method4() cil managed noinlining { 
		ldstr "G2_C675::Method4.11203()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<class Generated1219.BaseClass1>.Method4'() cil managed noinlining { 
		.override method instance string class Generated1219.IBase1`1<class Generated1219.BaseClass1>::Method4()
		ldstr "G2_C675::Method4.MI.11204()"
		ret
	}
	.method public hidebysig virtual instance string Method5() cil managed noinlining { 
		ldstr "G2_C675::Method5.11205()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<class Generated1219.BaseClass1>.Method5'() cil managed noinlining { 
		.override method instance string class Generated1219.IBase1`1<class Generated1219.BaseClass1>::Method5()
		ldstr "G2_C675::Method5.MI.11206()"
		ret
	}
	.method public hidebysig virtual instance string Method6<M0>() cil managed noinlining { 
		ldstr "G2_C675::Method6.11207<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<class Generated1219.BaseClass1>.Method6'<M0>() cil managed noinlining { 
		.override method instance string class Generated1219.IBase1`1<class Generated1219.BaseClass1>::Method6<[1]>()
		ldstr "G2_C675::Method6.MI.11208<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G2_C675::Method7.11209<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase2<class Generated1219.BaseClass0,T1>.Method7'<M0>() cil managed noinlining { 
		.override method instance string class Generated1219.IBase2`2<class Generated1219.BaseClass0,!T1>::Method7<[1]>()
		ldstr "G2_C675::Method7.MI.11210<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod2709() cil managed noinlining { 
		ldstr "G2_C675::ClassMethod2709.11211()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod2710<M0>() cil managed noinlining { 
		ldstr "G2_C675::ClassMethod2710.11212<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod2711<M0>() cil managed noinlining { 
		ldstr "G2_C675::ClassMethod2711.11213<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'G1_C12<T0,T1>.ClassMethod1347'<M0>() cil managed noinlining { 
		.override method instance string class Generated1219.G1_C12`2<!T0,!T1>::ClassMethod1347<[1]>()
		ldstr "G2_C675::ClassMethod1347.MI.11214<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class Generated1219.G1_C12`2<!T0,!T1>::.ctor()
		ret
	}
}
.class interface public abstract Generated1219.IBase2`2<+T0, -T1> 
{
	.method public hidebysig newslot abstract virtual instance string Method7<M0>() cil managed { }
}
.class public Generated1219.G1_C12`2<T0, T1> 
		implements class Generated1219.IBase2`2<!T0,!T1>, class Generated1219.IBase1`1<class Generated1219.BaseClass0> 
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
		.override method instance string class Generated1219.IBase2`2<!T0,!T1>::Method7<[1]>()
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
	.method public hidebysig newslot virtual instance string 'IBase1<class Generated1219.BaseClass0>.Method6'<M0>() cil managed noinlining { 
		.override method instance string class Generated1219.IBase1`1<class Generated1219.BaseClass0>::Method6<[1]>()
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
.class interface public abstract Generated1219.IBase1`1<+T0> 
{
	.method public hidebysig newslot abstract virtual instance string Method4() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method5() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method6<M0>() cil managed { }
}
.class public auto ansi beforefieldinit Generated1219.Generated1219 {
	.method static void M.BaseClass0<(Generated1219.BaseClass0)W>(!!W inst, string exp) cil managed {
		.maxstack 5
		.locals init (string[] actualResults)
		ldc.i4.s    0
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.BaseClass0<(Generated1219.BaseClass0)W>(!!W inst, string exp)"
		ldc.i4.s 0
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.BaseClass1<(Generated1219.BaseClass1)W>(!!W inst, string exp) cil managed {
		.maxstack 5
		.locals init (string[] actualResults)
		ldc.i4.s    0
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.BaseClass1<(Generated1219.BaseClass1)W>(!!W inst, string exp)"
		ldc.i4.s 0
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1691.T<T0,(class Generated1219.G3_C1691`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 17
		.locals init (string[] actualResults)
		ldc.i4.s    12
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1691.T<T0,(class Generated1219.G3_C1691`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 12
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G3_C1691`1<!!T0>::ClassMethod1346<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G3_C1691`1<!!T0>::ClassMethod1347<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G3_C1691`1<!!T0>::ClassMethod2709()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G3_C1691`1<!!T0>::ClassMethod2710<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G3_C1691`1<!!T0>::ClassMethod2711<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G3_C1691`1<!!T0>::ClassMethod4779()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G3_C1691`1<!!T0>::ClassMethod4780<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G3_C1691`1<!!T0>::ClassMethod4781<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G3_C1691`1<!!T0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G3_C1691`1<!!T0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G3_C1691`1<!!T0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G3_C1691`1<!!T0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1691.A<(class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 17
		.locals init (string[] actualResults)
		ldc.i4.s    12
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1691.A<(class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 12
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>::ClassMethod1346<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>::ClassMethod1347<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>::ClassMethod2709()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>::ClassMethod2710<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>::ClassMethod2711<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>::ClassMethod4779()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>::ClassMethod4780<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>::ClassMethod4781<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1691.B<(class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 17
		.locals init (string[] actualResults)
		ldc.i4.s    12
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1691.B<(class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 12
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>::ClassMethod1346<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>::ClassMethod1347<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>::ClassMethod2709()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>::ClassMethod2710<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>::ClassMethod2711<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>::ClassMethod4779()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>::ClassMethod4780<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>::ClassMethod4781<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C675.T.T<T0,T1,(class Generated1219.G2_C675`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 14
		.locals init (string[] actualResults)
		ldc.i4.s    9
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C675.T.T<T0,T1,(class Generated1219.G2_C675`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 9
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G2_C675`2<!!T0,!!T1>::ClassMethod1346<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G2_C675`2<!!T0,!!T1>::ClassMethod1347<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G2_C675`2<!!T0,!!T1>::ClassMethod2709()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G2_C675`2<!!T0,!!T1>::ClassMethod2710<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G2_C675`2<!!T0,!!T1>::ClassMethod2711<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G2_C675`2<!!T0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G2_C675`2<!!T0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G2_C675`2<!!T0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G2_C675`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C675.A.T<T1,(class Generated1219.G2_C675`2<class Generated1219.BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 14
		.locals init (string[] actualResults)
		ldc.i4.s    9
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C675.A.T<T1,(class Generated1219.G2_C675`2<class Generated1219.BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 9
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass0,!!T1>::ClassMethod1346<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass0,!!T1>::ClassMethod1347<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass0,!!T1>::ClassMethod2709()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass0,!!T1>::ClassMethod2710<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass0,!!T1>::ClassMethod2711<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C675.A.A<(class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 14
		.locals init (string[] actualResults)
		ldc.i4.s    9
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C675.A.A<(class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 9
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>::ClassMethod1346<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>::ClassMethod1347<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>::ClassMethod2709()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>::ClassMethod2710<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>::ClassMethod2711<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C675.A.B<(class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 14
		.locals init (string[] actualResults)
		ldc.i4.s    9
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C675.A.B<(class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 9
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>::ClassMethod1346<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>::ClassMethod1347<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>::ClassMethod2709()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>::ClassMethod2710<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>::ClassMethod2711<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C675.B.T<T1,(class Generated1219.G2_C675`2<class Generated1219.BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 14
		.locals init (string[] actualResults)
		ldc.i4.s    9
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C675.B.T<T1,(class Generated1219.G2_C675`2<class Generated1219.BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 9
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass1,!!T1>::ClassMethod1346<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass1,!!T1>::ClassMethod1347<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass1,!!T1>::ClassMethod2709()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass1,!!T1>::ClassMethod2710<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass1,!!T1>::ClassMethod2711<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass1,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass1,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass1,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C675.B.A<(class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 14
		.locals init (string[] actualResults)
		ldc.i4.s    9
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C675.B.A<(class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 9
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>::ClassMethod1346<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>::ClassMethod1347<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>::ClassMethod2709()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>::ClassMethod2710<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>::ClassMethod2711<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C675.B.B<(class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 14
		.locals init (string[] actualResults)
		ldc.i4.s    9
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C675.B.B<(class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 9
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>::ClassMethod1346<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>::ClassMethod1347<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>::ClassMethod2709()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>::ClassMethod2710<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>::ClassMethod2711<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.T.T<T0,T1,(class Generated1219.IBase2`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.T.T<T0,T1,(class Generated1219.IBase2`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.IBase2`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.A.T<T1,(class Generated1219.IBase2`2<class Generated1219.BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.A.T<T1,(class Generated1219.IBase2`2<class Generated1219.BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.IBase2`2<class Generated1219.BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.A.A<(class Generated1219.IBase2`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.A.A<(class Generated1219.IBase2`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.IBase2`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.A.B<(class Generated1219.IBase2`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.A.B<(class Generated1219.IBase2`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.IBase2`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.B.T<T1,(class Generated1219.IBase2`2<class Generated1219.BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.B.T<T1,(class Generated1219.IBase2`2<class Generated1219.BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.IBase2`2<class Generated1219.BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.B.A<(class Generated1219.IBase2`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.B.A<(class Generated1219.IBase2`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.IBase2`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.B.B<(class Generated1219.IBase2`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.B.B<(class Generated1219.IBase2`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.IBase2`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C12.T.T<T0,T1,(class Generated1219.G1_C12`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C12.T.T<T0,T1,(class Generated1219.G1_C12`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G1_C12`2<!!T0,!!T1>::ClassMethod1346<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G1_C12`2<!!T0,!!T1>::ClassMethod1347<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G1_C12`2<!!T0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G1_C12`2<!!T0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G1_C12`2<!!T0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G1_C12`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C12.A.T<T1,(class Generated1219.G1_C12`2<class Generated1219.BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C12.A.T<T1,(class Generated1219.G1_C12`2<class Generated1219.BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass0,!!T1>::ClassMethod1346<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass0,!!T1>::ClassMethod1347<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C12.A.A<(class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C12.A.A<(class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>::ClassMethod1346<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>::ClassMethod1347<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C12.A.B<(class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C12.A.B<(class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>::ClassMethod1346<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>::ClassMethod1347<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C12.B.T<T1,(class Generated1219.G1_C12`2<class Generated1219.BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C12.B.T<T1,(class Generated1219.G1_C12`2<class Generated1219.BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass1,!!T1>::ClassMethod1346<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass1,!!T1>::ClassMethod1347<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass1,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass1,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass1,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C12.B.A<(class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C12.B.A<(class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>::ClassMethod1346<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>::ClassMethod1347<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C12.B.B<(class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C12.B.B<(class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>::ClassMethod1346<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>::ClassMethod1347<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase1.T<T0,(class Generated1219.IBase1`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase1.T<T0,(class Generated1219.IBase1`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.IBase1`1<!!T0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.IBase1`1<!!T0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.IBase1`1<!!T0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase1.A<(class Generated1219.IBase1`1<class Generated1219.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase1.A<(class Generated1219.IBase1`1<class Generated1219.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.IBase1`1<class Generated1219.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.IBase1`1<class Generated1219.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.IBase1`1<class Generated1219.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase1.B<(class Generated1219.IBase1`1<class Generated1219.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase1.B<(class Generated1219.IBase1`1<class Generated1219.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.IBase1`1<class Generated1219.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.IBase1`1<class Generated1219.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1219.IBase1`1<class Generated1219.BaseClass1>::Method6<object>()
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
		newobj instance void class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>
		callvirt instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>::ClassMethod1347<object>()
		ldstr "G2_C675::ClassMethod1347.MI.11214<System.Object>()"
		ldstr "class G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>
		callvirt instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>::ClassMethod1346<object>()
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()"
		ldstr "class G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>
		callvirt instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>::Method6<object>()
		ldstr "G2_C675::Method6.11207<System.Object>()"
		ldstr "class G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>
		callvirt instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>::Method5()
		ldstr "G2_C675::Method5.11205()"
		ldstr "class G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>
		callvirt instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>::Method4()
		ldstr "G2_C675::Method4.11203()"
		ldstr "class G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>
		callvirt instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>::Method7<object>()
		ldstr "G2_C675::Method7.11209<System.Object>()"
		ldstr "class G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1219.IBase2`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>::Method7<object>()
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1219.IBase1`1<class Generated1219.BaseClass0>::Method4()
		ldstr "G2_C675::Method4.MI.11204()"
		ldstr "class IBase1`1<class Generated1219.BaseClass0> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1219.IBase1`1<class Generated1219.BaseClass0>::Method5()
		ldstr "G2_C675::Method5.MI.11206()"
		ldstr "class IBase1`1<class Generated1219.BaseClass0> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1219.IBase1`1<class Generated1219.BaseClass0>::Method6<object>()
		ldstr "G2_C675::Method6.MI.11208<System.Object>()"
		ldstr "class IBase1`1<class Generated1219.BaseClass0> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1219.IBase2`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>::Method7<object>()
		ldstr "G3_C1691::Method7.MI.17471<System.Object>()"
		ldstr "class IBase2`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1219.IBase2`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>::Method7<object>()
		ldstr "G3_C1691::Method7.MI.17471<System.Object>()"
		ldstr "class IBase2`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1219.IBase2`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>::Method7<object>()
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>
		callvirt instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>::ClassMethod2711<object>()
		ldstr "G2_C675::ClassMethod2711.11213<System.Object>()"
		ldstr "class G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>
		callvirt instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>::ClassMethod2710<object>()
		ldstr "G3_C1691::ClassMethod2710.MI.17475<System.Object>()"
		ldstr "class G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>
		callvirt instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>::ClassMethod2709()
		ldstr "G2_C675::ClassMethod2709.11211()"
		ldstr "class G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>
		callvirt instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>::Method7<object>()
		ldstr "G2_C675::Method7.11209<System.Object>()"
		ldstr "class G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>
		callvirt instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>::Method6<object>()
		ldstr "G2_C675::Method6.11207<System.Object>()"
		ldstr "class G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>
		callvirt instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>::Method5()
		ldstr "G2_C675::Method5.11205()"
		ldstr "class G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>
		callvirt instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>::Method4()
		ldstr "G2_C675::Method4.11203()"
		ldstr "class G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>
		callvirt instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>::ClassMethod1347<object>()
		ldstr "G2_C675::ClassMethod1347.MI.11214<System.Object>()"
		ldstr "class G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>
		callvirt instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>::ClassMethod1346<object>()
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()"
		ldstr "class G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1219.IBase1`1<class Generated1219.BaseClass1>::Method4()
		ldstr "G2_C675::Method4.MI.11204()"
		ldstr "class IBase1`1<class Generated1219.BaseClass1> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1219.IBase1`1<class Generated1219.BaseClass1>::Method5()
		ldstr "G2_C675::Method5.MI.11206()"
		ldstr "class IBase1`1<class Generated1219.BaseClass1> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1219.IBase1`1<class Generated1219.BaseClass1>::Method6<object>()
		ldstr "G2_C675::Method6.MI.11208<System.Object>()"
		ldstr "class IBase1`1<class Generated1219.BaseClass1> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>
		callvirt instance string class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>::ClassMethod4781<object>()
		ldstr "G3_C1691::ClassMethod4781.17474<System.Object>()"
		ldstr "class G3_C1691`1<class Generated1219.BaseClass0> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>
		callvirt instance string class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>::ClassMethod4780<object>()
		ldstr "G3_C1691::ClassMethod4780.17473<System.Object>()"
		ldstr "class G3_C1691`1<class Generated1219.BaseClass0> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>
		callvirt instance string class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>::ClassMethod4779()
		ldstr "G3_C1691::ClassMethod4779.17472()"
		ldstr "class G3_C1691`1<class Generated1219.BaseClass0> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>
		callvirt instance string class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>::Method7<object>()
		ldstr "G3_C1691::Method7.17470<System.Object>()"
		ldstr "class G3_C1691`1<class Generated1219.BaseClass0> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>
		callvirt instance string class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>::ClassMethod2711<object>()
		ldstr "G2_C675::ClassMethod2711.11213<System.Object>()"
		ldstr "class G3_C1691`1<class Generated1219.BaseClass0> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>
		callvirt instance string class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>::ClassMethod2710<object>()
		ldstr "G3_C1691::ClassMethod2710.MI.17475<System.Object>()"
		ldstr "class G3_C1691`1<class Generated1219.BaseClass0> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>
		callvirt instance string class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>::ClassMethod2709()
		ldstr "G2_C675::ClassMethod2709.11211()"
		ldstr "class G3_C1691`1<class Generated1219.BaseClass0> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>
		callvirt instance string class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>::Method6<object>()
		ldstr "G2_C675::Method6.11207<System.Object>()"
		ldstr "class G3_C1691`1<class Generated1219.BaseClass0> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>
		callvirt instance string class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>::Method5()
		ldstr "G2_C675::Method5.11205()"
		ldstr "class G3_C1691`1<class Generated1219.BaseClass0> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>
		callvirt instance string class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>::Method4()
		ldstr "G2_C675::Method4.11203()"
		ldstr "class G3_C1691`1<class Generated1219.BaseClass0> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>
		callvirt instance string class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>::ClassMethod1347<object>()
		ldstr "G2_C675::ClassMethod1347.MI.11214<System.Object>()"
		ldstr "class G3_C1691`1<class Generated1219.BaseClass0> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>
		callvirt instance string class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>::ClassMethod1346<object>()
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()"
		ldstr "class G3_C1691`1<class Generated1219.BaseClass0> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>
		callvirt instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>::ClassMethod1347<object>()
		ldstr "G2_C675::ClassMethod1347.MI.11214<System.Object>()"
		ldstr "class G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>
		callvirt instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>::ClassMethod1346<object>()
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()"
		ldstr "class G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>
		callvirt instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>::Method6<object>()
		ldstr "G2_C675::Method6.11207<System.Object>()"
		ldstr "class G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>
		callvirt instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>::Method5()
		ldstr "G2_C675::Method5.11205()"
		ldstr "class G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>
		callvirt instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>::Method4()
		ldstr "G2_C675::Method4.11203()"
		ldstr "class G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>
		callvirt instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>::Method7<object>()
		ldstr "G2_C675::Method7.11209<System.Object>()"
		ldstr "class G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1219.IBase2`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>::Method7<object>()
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1219.IBase1`1<class Generated1219.BaseClass0>::Method4()
		ldstr "G2_C675::Method4.MI.11204()"
		ldstr "class IBase1`1<class Generated1219.BaseClass0> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1219.IBase1`1<class Generated1219.BaseClass0>::Method5()
		ldstr "G2_C675::Method5.MI.11206()"
		ldstr "class IBase1`1<class Generated1219.BaseClass0> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1219.IBase1`1<class Generated1219.BaseClass0>::Method6<object>()
		ldstr "G2_C675::Method6.MI.11208<System.Object>()"
		ldstr "class IBase1`1<class Generated1219.BaseClass0> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1219.IBase2`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>::Method7<object>()
		ldstr "G3_C1691::Method7.MI.17471<System.Object>()"
		ldstr "class IBase2`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>
		callvirt instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>::ClassMethod2711<object>()
		ldstr "G2_C675::ClassMethod2711.11213<System.Object>()"
		ldstr "class G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>
		callvirt instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>::ClassMethod2710<object>()
		ldstr "G3_C1691::ClassMethod2710.MI.17475<System.Object>()"
		ldstr "class G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>
		callvirt instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>::ClassMethod2709()
		ldstr "G2_C675::ClassMethod2709.11211()"
		ldstr "class G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>
		callvirt instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>::Method7<object>()
		ldstr "G2_C675::Method7.11209<System.Object>()"
		ldstr "class G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>
		callvirt instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>::Method6<object>()
		ldstr "G2_C675::Method6.11207<System.Object>()"
		ldstr "class G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>
		callvirt instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>::Method5()
		ldstr "G2_C675::Method5.11205()"
		ldstr "class G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>
		callvirt instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>::Method4()
		ldstr "G2_C675::Method4.11203()"
		ldstr "class G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>
		callvirt instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>::ClassMethod1347<object>()
		ldstr "G2_C675::ClassMethod1347.MI.11214<System.Object>()"
		ldstr "class G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>
		callvirt instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>::ClassMethod1346<object>()
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()"
		ldstr "class G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1219.IBase1`1<class Generated1219.BaseClass1>::Method4()
		ldstr "G2_C675::Method4.MI.11204()"
		ldstr "class IBase1`1<class Generated1219.BaseClass1> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1219.IBase1`1<class Generated1219.BaseClass1>::Method5()
		ldstr "G2_C675::Method5.MI.11206()"
		ldstr "class IBase1`1<class Generated1219.BaseClass1> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1219.IBase1`1<class Generated1219.BaseClass1>::Method6<object>()
		ldstr "G2_C675::Method6.MI.11208<System.Object>()"
		ldstr "class IBase1`1<class Generated1219.BaseClass1> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>
		callvirt instance string class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>::ClassMethod4781<object>()
		ldstr "G3_C1691::ClassMethod4781.17474<System.Object>()"
		ldstr "class G3_C1691`1<class Generated1219.BaseClass1> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>
		callvirt instance string class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>::ClassMethod4780<object>()
		ldstr "G3_C1691::ClassMethod4780.17473<System.Object>()"
		ldstr "class G3_C1691`1<class Generated1219.BaseClass1> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>
		callvirt instance string class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>::ClassMethod4779()
		ldstr "G3_C1691::ClassMethod4779.17472()"
		ldstr "class G3_C1691`1<class Generated1219.BaseClass1> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>
		callvirt instance string class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>::Method7<object>()
		ldstr "G3_C1691::Method7.17470<System.Object>()"
		ldstr "class G3_C1691`1<class Generated1219.BaseClass1> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>
		callvirt instance string class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>::ClassMethod2711<object>()
		ldstr "G2_C675::ClassMethod2711.11213<System.Object>()"
		ldstr "class G3_C1691`1<class Generated1219.BaseClass1> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>
		callvirt instance string class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>::ClassMethod2710<object>()
		ldstr "G3_C1691::ClassMethod2710.MI.17475<System.Object>()"
		ldstr "class G3_C1691`1<class Generated1219.BaseClass1> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>
		callvirt instance string class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>::ClassMethod2709()
		ldstr "G2_C675::ClassMethod2709.11211()"
		ldstr "class G3_C1691`1<class Generated1219.BaseClass1> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>
		callvirt instance string class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>::Method6<object>()
		ldstr "G2_C675::Method6.11207<System.Object>()"
		ldstr "class G3_C1691`1<class Generated1219.BaseClass1> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>
		callvirt instance string class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>::Method5()
		ldstr "G2_C675::Method5.11205()"
		ldstr "class G3_C1691`1<class Generated1219.BaseClass1> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>
		callvirt instance string class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>::Method4()
		ldstr "G2_C675::Method4.11203()"
		ldstr "class G3_C1691`1<class Generated1219.BaseClass1> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>
		callvirt instance string class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>::ClassMethod1347<object>()
		ldstr "G2_C675::ClassMethod1347.MI.11214<System.Object>()"
		ldstr "class G3_C1691`1<class Generated1219.BaseClass1> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>
		callvirt instance string class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>::ClassMethod1346<object>()
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()"
		ldstr "class G3_C1691`1<class Generated1219.BaseClass1> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1219.IBase2`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>::Method7<object>()
		ldstr "G3_C1691::Method7.MI.17471<System.Object>()"
		ldstr "class IBase2`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>
		callvirt instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>::ClassMethod1347<object>()
		ldstr "G2_C675::ClassMethod1347.MI.11214<System.Object>()"
		ldstr "class G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>
		callvirt instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>::ClassMethod1346<object>()
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()"
		ldstr "class G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>
		callvirt instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>::Method6<object>()
		ldstr "G2_C675::Method6.11207<System.Object>()"
		ldstr "class G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>
		callvirt instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>::Method5()
		ldstr "G2_C675::Method5.11205()"
		ldstr "class G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>
		callvirt instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>::Method4()
		ldstr "G2_C675::Method4.11203()"
		ldstr "class G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>
		callvirt instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>::Method7<object>()
		ldstr "G2_C675::Method7.11209<System.Object>()"
		ldstr "class G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1219.IBase2`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>::Method7<object>()
		ldstr "G2_C675::Method7.MI.11210<System.Object>()"
		ldstr "class IBase2`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1219.IBase1`1<class Generated1219.BaseClass0>::Method4()
		ldstr "G2_C675::Method4.MI.11204()"
		ldstr "class IBase1`1<class Generated1219.BaseClass0> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1219.IBase1`1<class Generated1219.BaseClass0>::Method5()
		ldstr "G2_C675::Method5.MI.11206()"
		ldstr "class IBase1`1<class Generated1219.BaseClass0> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1219.IBase1`1<class Generated1219.BaseClass0>::Method6<object>()
		ldstr "G2_C675::Method6.MI.11208<System.Object>()"
		ldstr "class IBase1`1<class Generated1219.BaseClass0> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1219.IBase2`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>::Method7<object>()
		ldstr "G2_C675::Method7.MI.11210<System.Object>()"
		ldstr "class IBase2`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>
		callvirt instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>::ClassMethod2711<object>()
		ldstr "G2_C675::ClassMethod2711.11213<System.Object>()"
		ldstr "class G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>
		callvirt instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>::ClassMethod2710<object>()
		ldstr "G2_C675::ClassMethod2710.11212<System.Object>()"
		ldstr "class G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>
		callvirt instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>::ClassMethod2709()
		ldstr "G2_C675::ClassMethod2709.11211()"
		ldstr "class G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>
		callvirt instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>::Method7<object>()
		ldstr "G2_C675::Method7.11209<System.Object>()"
		ldstr "class G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>
		callvirt instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>::Method6<object>()
		ldstr "G2_C675::Method6.11207<System.Object>()"
		ldstr "class G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>
		callvirt instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>::Method5()
		ldstr "G2_C675::Method5.11205()"
		ldstr "class G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>
		callvirt instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>::Method4()
		ldstr "G2_C675::Method4.11203()"
		ldstr "class G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>
		callvirt instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>::ClassMethod1347<object>()
		ldstr "G2_C675::ClassMethod1347.MI.11214<System.Object>()"
		ldstr "class G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>
		callvirt instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>::ClassMethod1346<object>()
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()"
		ldstr "class G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1219.IBase1`1<class Generated1219.BaseClass1>::Method4()
		ldstr "G2_C675::Method4.MI.11204()"
		ldstr "class IBase1`1<class Generated1219.BaseClass1> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1219.IBase1`1<class Generated1219.BaseClass1>::Method5()
		ldstr "G2_C675::Method5.MI.11206()"
		ldstr "class IBase1`1<class Generated1219.BaseClass1> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1219.IBase1`1<class Generated1219.BaseClass1>::Method6<object>()
		ldstr "G2_C675::Method6.MI.11208<System.Object>()"
		ldstr "class IBase1`1<class Generated1219.BaseClass1> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>
		callvirt instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>::ClassMethod1347<object>()
		ldstr "G2_C675::ClassMethod1347.MI.11214<System.Object>()"
		ldstr "class G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>
		callvirt instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>::ClassMethod1346<object>()
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()"
		ldstr "class G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>
		callvirt instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>::Method6<object>()
		ldstr "G2_C675::Method6.11207<System.Object>()"
		ldstr "class G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>
		callvirt instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>::Method5()
		ldstr "G2_C675::Method5.11205()"
		ldstr "class G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>
		callvirt instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>::Method4()
		ldstr "G2_C675::Method4.11203()"
		ldstr "class G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>
		callvirt instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>::Method7<object>()
		ldstr "G2_C675::Method7.11209<System.Object>()"
		ldstr "class G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1219.IBase2`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>::Method7<object>()
		ldstr "G2_C675::Method7.MI.11210<System.Object>()"
		ldstr "class IBase2`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1219.IBase1`1<class Generated1219.BaseClass0>::Method4()
		ldstr "G2_C675::Method4.MI.11204()"
		ldstr "class IBase1`1<class Generated1219.BaseClass0> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1219.IBase1`1<class Generated1219.BaseClass0>::Method5()
		ldstr "G2_C675::Method5.MI.11206()"
		ldstr "class IBase1`1<class Generated1219.BaseClass0> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1219.IBase1`1<class Generated1219.BaseClass0>::Method6<object>()
		ldstr "G2_C675::Method6.MI.11208<System.Object>()"
		ldstr "class IBase1`1<class Generated1219.BaseClass0> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>
		callvirt instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>::ClassMethod2711<object>()
		ldstr "G2_C675::ClassMethod2711.11213<System.Object>()"
		ldstr "class G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>
		callvirt instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>::ClassMethod2710<object>()
		ldstr "G2_C675::ClassMethod2710.11212<System.Object>()"
		ldstr "class G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>
		callvirt instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>::ClassMethod2709()
		ldstr "G2_C675::ClassMethod2709.11211()"
		ldstr "class G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>
		callvirt instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>::Method7<object>()
		ldstr "G2_C675::Method7.11209<System.Object>()"
		ldstr "class G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>
		callvirt instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>::Method6<object>()
		ldstr "G2_C675::Method6.11207<System.Object>()"
		ldstr "class G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>
		callvirt instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>::Method5()
		ldstr "G2_C675::Method5.11205()"
		ldstr "class G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>
		callvirt instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>::Method4()
		ldstr "G2_C675::Method4.11203()"
		ldstr "class G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>
		callvirt instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>::ClassMethod1347<object>()
		ldstr "G2_C675::ClassMethod1347.MI.11214<System.Object>()"
		ldstr "class G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>
		callvirt instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>::ClassMethod1346<object>()
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()"
		ldstr "class G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1219.IBase1`1<class Generated1219.BaseClass1>::Method4()
		ldstr "G2_C675::Method4.MI.11204()"
		ldstr "class IBase1`1<class Generated1219.BaseClass1> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1219.IBase1`1<class Generated1219.BaseClass1>::Method5()
		ldstr "G2_C675::Method5.MI.11206()"
		ldstr "class IBase1`1<class Generated1219.BaseClass1> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1219.IBase1`1<class Generated1219.BaseClass1>::Method6<object>()
		ldstr "G2_C675::Method6.MI.11208<System.Object>()"
		ldstr "class IBase1`1<class Generated1219.BaseClass1> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>
		callvirt instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>::ClassMethod1347<object>()
		ldstr "G2_C675::ClassMethod1347.MI.11214<System.Object>()"
		ldstr "class G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>
		callvirt instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>::ClassMethod1346<object>()
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()"
		ldstr "class G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>
		callvirt instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>::Method6<object>()
		ldstr "G2_C675::Method6.11207<System.Object>()"
		ldstr "class G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>
		callvirt instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>::Method5()
		ldstr "G2_C675::Method5.11205()"
		ldstr "class G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>
		callvirt instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>::Method4()
		ldstr "G2_C675::Method4.11203()"
		ldstr "class G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>
		callvirt instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>::Method7<object>()
		ldstr "G2_C675::Method7.11209<System.Object>()"
		ldstr "class G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1219.IBase2`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>::Method7<object>()
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1219.IBase1`1<class Generated1219.BaseClass0>::Method4()
		ldstr "G2_C675::Method4.MI.11204()"
		ldstr "class IBase1`1<class Generated1219.BaseClass0> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1219.IBase1`1<class Generated1219.BaseClass0>::Method5()
		ldstr "G2_C675::Method5.MI.11206()"
		ldstr "class IBase1`1<class Generated1219.BaseClass0> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1219.IBase1`1<class Generated1219.BaseClass0>::Method6<object>()
		ldstr "G2_C675::Method6.MI.11208<System.Object>()"
		ldstr "class IBase1`1<class Generated1219.BaseClass0> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1219.IBase2`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>::Method7<object>()
		ldstr "G2_C675::Method7.MI.11210<System.Object>()"
		ldstr "class IBase2`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1219.IBase2`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>::Method7<object>()
		ldstr "G2_C675::Method7.MI.11210<System.Object>()"
		ldstr "class IBase2`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1219.IBase2`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>::Method7<object>()
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>
		callvirt instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>::ClassMethod2711<object>()
		ldstr "G2_C675::ClassMethod2711.11213<System.Object>()"
		ldstr "class G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>
		callvirt instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>::ClassMethod2710<object>()
		ldstr "G2_C675::ClassMethod2710.11212<System.Object>()"
		ldstr "class G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>
		callvirt instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>::ClassMethod2709()
		ldstr "G2_C675::ClassMethod2709.11211()"
		ldstr "class G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>
		callvirt instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>::Method7<object>()
		ldstr "G2_C675::Method7.11209<System.Object>()"
		ldstr "class G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>
		callvirt instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>::Method6<object>()
		ldstr "G2_C675::Method6.11207<System.Object>()"
		ldstr "class G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>
		callvirt instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>::Method5()
		ldstr "G2_C675::Method5.11205()"
		ldstr "class G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>
		callvirt instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>::Method4()
		ldstr "G2_C675::Method4.11203()"
		ldstr "class G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>
		callvirt instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>::ClassMethod1347<object>()
		ldstr "G2_C675::ClassMethod1347.MI.11214<System.Object>()"
		ldstr "class G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>
		callvirt instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>::ClassMethod1346<object>()
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()"
		ldstr "class G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1219.IBase1`1<class Generated1219.BaseClass1>::Method4()
		ldstr "G2_C675::Method4.MI.11204()"
		ldstr "class IBase1`1<class Generated1219.BaseClass1> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1219.IBase1`1<class Generated1219.BaseClass1>::Method5()
		ldstr "G2_C675::Method5.MI.11206()"
		ldstr "class IBase1`1<class Generated1219.BaseClass1> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1219.IBase1`1<class Generated1219.BaseClass1>::Method6<object>()
		ldstr "G2_C675::Method6.MI.11208<System.Object>()"
		ldstr "class IBase1`1<class Generated1219.BaseClass1> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>
		callvirt instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>::ClassMethod1347<object>()
		ldstr "G2_C675::ClassMethod1347.MI.11214<System.Object>()"
		ldstr "class G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>
		callvirt instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>::ClassMethod1346<object>()
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()"
		ldstr "class G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>
		callvirt instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>::Method6<object>()
		ldstr "G2_C675::Method6.11207<System.Object>()"
		ldstr "class G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>
		callvirt instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>::Method5()
		ldstr "G2_C675::Method5.11205()"
		ldstr "class G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>
		callvirt instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>::Method4()
		ldstr "G2_C675::Method4.11203()"
		ldstr "class G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>
		callvirt instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>::Method7<object>()
		ldstr "G2_C675::Method7.11209<System.Object>()"
		ldstr "class G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1219.IBase2`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>::Method7<object>()
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1219.IBase1`1<class Generated1219.BaseClass0>::Method4()
		ldstr "G2_C675::Method4.MI.11204()"
		ldstr "class IBase1`1<class Generated1219.BaseClass0> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1219.IBase1`1<class Generated1219.BaseClass0>::Method5()
		ldstr "G2_C675::Method5.MI.11206()"
		ldstr "class IBase1`1<class Generated1219.BaseClass0> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1219.IBase1`1<class Generated1219.BaseClass0>::Method6<object>()
		ldstr "G2_C675::Method6.MI.11208<System.Object>()"
		ldstr "class IBase1`1<class Generated1219.BaseClass0> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1219.IBase2`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>::Method7<object>()
		ldstr "G2_C675::Method7.MI.11210<System.Object>()"
		ldstr "class IBase2`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>
		callvirt instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>::ClassMethod2711<object>()
		ldstr "G2_C675::ClassMethod2711.11213<System.Object>()"
		ldstr "class G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>
		callvirt instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>::ClassMethod2710<object>()
		ldstr "G2_C675::ClassMethod2710.11212<System.Object>()"
		ldstr "class G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>
		callvirt instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>::ClassMethod2709()
		ldstr "G2_C675::ClassMethod2709.11211()"
		ldstr "class G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>
		callvirt instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>::Method7<object>()
		ldstr "G2_C675::Method7.11209<System.Object>()"
		ldstr "class G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>
		callvirt instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>::Method6<object>()
		ldstr "G2_C675::Method6.11207<System.Object>()"
		ldstr "class G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>
		callvirt instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>::Method5()
		ldstr "G2_C675::Method5.11205()"
		ldstr "class G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>
		callvirt instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>::Method4()
		ldstr "G2_C675::Method4.11203()"
		ldstr "class G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>
		callvirt instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>::ClassMethod1347<object>()
		ldstr "G2_C675::ClassMethod1347.MI.11214<System.Object>()"
		ldstr "class G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>
		callvirt instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>::ClassMethod1346<object>()
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()"
		ldstr "class G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1219.IBase1`1<class Generated1219.BaseClass1>::Method4()
		ldstr "G2_C675::Method4.MI.11204()"
		ldstr "class IBase1`1<class Generated1219.BaseClass1> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1219.IBase1`1<class Generated1219.BaseClass1>::Method5()
		ldstr "G2_C675::Method5.MI.11206()"
		ldstr "class IBase1`1<class Generated1219.BaseClass1> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1219.IBase1`1<class Generated1219.BaseClass1>::Method6<object>()
		ldstr "G2_C675::Method6.MI.11208<System.Object>()"
		ldstr "class IBase1`1<class Generated1219.BaseClass1> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>
		callvirt instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>::ClassMethod1347<object>()
		ldstr "G1_C12::ClassMethod1347.4870<System.Object>()"
		ldstr "class G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0> on type class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>
		callvirt instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>::ClassMethod1346<object>()
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()"
		ldstr "class G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0> on type class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>
		callvirt instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>::Method6<object>()
		ldstr "G1_C12::Method6.4867<System.Object>()"
		ldstr "class G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0> on type class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>
		callvirt instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>::Method5()
		ldstr "G1_C12::Method5.4866()"
		ldstr "class G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0> on type class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>
		callvirt instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>::Method4()
		ldstr "G1_C12::Method4.4865()"
		ldstr "class G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0> on type class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>
		callvirt instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>::Method7<object>()
		ldstr "G1_C12::Method7.4863<System.Object>()"
		ldstr "class G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0> on type class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1219.IBase2`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>::Method7<object>()
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0> on type class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1219.IBase1`1<class Generated1219.BaseClass0>::Method4()
		ldstr "G1_C12::Method4.4865()"
		ldstr "class IBase1`1<class Generated1219.BaseClass0> on type class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1219.IBase1`1<class Generated1219.BaseClass0>::Method5()
		ldstr "G1_C12::Method5.4866()"
		ldstr "class IBase1`1<class Generated1219.BaseClass0> on type class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1219.IBase1`1<class Generated1219.BaseClass0>::Method6<object>()
		ldstr "G1_C12::Method6.MI.4868<System.Object>()"
		ldstr "class IBase1`1<class Generated1219.BaseClass0> on type class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1219.IBase2`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>::Method7<object>()
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1> on type class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>
		callvirt instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>::ClassMethod1347<object>()
		ldstr "G1_C12::ClassMethod1347.4870<System.Object>()"
		ldstr "class G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1> on type class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>
		callvirt instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>::ClassMethod1346<object>()
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()"
		ldstr "class G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1> on type class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>
		callvirt instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>::Method6<object>()
		ldstr "G1_C12::Method6.4867<System.Object>()"
		ldstr "class G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1> on type class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>
		callvirt instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>::Method5()
		ldstr "G1_C12::Method5.4866()"
		ldstr "class G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1> on type class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>
		callvirt instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>::Method4()
		ldstr "G1_C12::Method4.4865()"
		ldstr "class G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1> on type class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>
		callvirt instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>::Method7<object>()
		ldstr "G1_C12::Method7.4863<System.Object>()"
		ldstr "class G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1> on type class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1219.IBase2`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>::Method7<object>()
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1> on type class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1219.IBase1`1<class Generated1219.BaseClass0>::Method4()
		ldstr "G1_C12::Method4.4865()"
		ldstr "class IBase1`1<class Generated1219.BaseClass0> on type class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1219.IBase1`1<class Generated1219.BaseClass0>::Method5()
		ldstr "G1_C12::Method5.4866()"
		ldstr "class IBase1`1<class Generated1219.BaseClass0> on type class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1219.IBase1`1<class Generated1219.BaseClass0>::Method6<object>()
		ldstr "G1_C12::Method6.MI.4868<System.Object>()"
		ldstr "class IBase1`1<class Generated1219.BaseClass0> on type class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>
		callvirt instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>::ClassMethod1347<object>()
		ldstr "G1_C12::ClassMethod1347.4870<System.Object>()"
		ldstr "class G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0> on type class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>
		callvirt instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>::ClassMethod1346<object>()
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()"
		ldstr "class G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0> on type class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>
		callvirt instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>::Method6<object>()
		ldstr "G1_C12::Method6.4867<System.Object>()"
		ldstr "class G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0> on type class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>
		callvirt instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>::Method5()
		ldstr "G1_C12::Method5.4866()"
		ldstr "class G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0> on type class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>
		callvirt instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>::Method4()
		ldstr "G1_C12::Method4.4865()"
		ldstr "class G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0> on type class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>
		callvirt instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>::Method7<object>()
		ldstr "G1_C12::Method7.4863<System.Object>()"
		ldstr "class G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0> on type class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1219.IBase2`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>::Method7<object>()
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0> on type class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1219.IBase1`1<class Generated1219.BaseClass0>::Method4()
		ldstr "G1_C12::Method4.4865()"
		ldstr "class IBase1`1<class Generated1219.BaseClass0> on type class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1219.IBase1`1<class Generated1219.BaseClass0>::Method5()
		ldstr "G1_C12::Method5.4866()"
		ldstr "class IBase1`1<class Generated1219.BaseClass0> on type class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1219.IBase1`1<class Generated1219.BaseClass0>::Method6<object>()
		ldstr "G1_C12::Method6.MI.4868<System.Object>()"
		ldstr "class IBase1`1<class Generated1219.BaseClass0> on type class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1219.IBase2`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>::Method7<object>()
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0> on type class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1219.IBase2`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>::Method7<object>()
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1> on type class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1219.IBase2`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>::Method7<object>()
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1> on type class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>
		callvirt instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>::ClassMethod1347<object>()
		ldstr "G1_C12::ClassMethod1347.4870<System.Object>()"
		ldstr "class G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1> on type class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>
		callvirt instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>::ClassMethod1346<object>()
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()"
		ldstr "class G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1> on type class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>
		callvirt instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>::Method6<object>()
		ldstr "G1_C12::Method6.4867<System.Object>()"
		ldstr "class G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1> on type class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>
		callvirt instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>::Method5()
		ldstr "G1_C12::Method5.4866()"
		ldstr "class G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1> on type class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>
		callvirt instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>::Method4()
		ldstr "G1_C12::Method4.4865()"
		ldstr "class G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1> on type class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>
		callvirt instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>::Method7<object>()
		ldstr "G1_C12::Method7.4863<System.Object>()"
		ldstr "class G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1> on type class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1219.IBase2`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>::Method7<object>()
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1> on type class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1219.IBase1`1<class Generated1219.BaseClass0>::Method4()
		ldstr "G1_C12::Method4.4865()"
		ldstr "class IBase1`1<class Generated1219.BaseClass0> on type class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1219.IBase1`1<class Generated1219.BaseClass0>::Method5()
		ldstr "G1_C12::Method5.4866()"
		ldstr "class IBase1`1<class Generated1219.BaseClass0> on type class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1219.IBase1`1<class Generated1219.BaseClass0>::Method6<object>()
		ldstr "G1_C12::Method6.MI.4868<System.Object>()"
		ldstr "class IBase1`1<class Generated1219.BaseClass0> on type class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1219.IBase2`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>::Method7<object>()
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1> on type class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>"
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
		newobj instance void class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()#G2_C675::ClassMethod1347.MI.11214<System.Object>()#G2_C675::Method4.11203()#G2_C675::Method5.11205()#G2_C675::Method6.11207<System.Object>()#G2_C675::Method7.11209<System.Object>()#"
		call void Generated1219.Generated1219::M.G1_C12.T.T<class Generated1219.BaseClass1,class Generated1219.BaseClass0,class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()#G2_C675::ClassMethod1347.MI.11214<System.Object>()#G2_C675::Method4.11203()#G2_C675::Method5.11205()#G2_C675::Method6.11207<System.Object>()#G2_C675::Method7.11209<System.Object>()#"
		call void Generated1219.Generated1219::M.G1_C12.B.T<class Generated1219.BaseClass0,class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()#G2_C675::ClassMethod1347.MI.11214<System.Object>()#G2_C675::Method4.11203()#G2_C675::Method5.11205()#G2_C675::Method6.11207<System.Object>()#G2_C675::Method7.11209<System.Object>()#"
		call void Generated1219.Generated1219::M.G1_C12.B.A<class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated1219.Generated1219::M.IBase2.T.T<class Generated1219.BaseClass1,class Generated1219.BaseClass0,class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated1219.Generated1219::M.IBase2.B.T<class Generated1219.BaseClass0,class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated1219.Generated1219::M.IBase2.B.A<class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C675::Method4.MI.11204()#G2_C675::Method5.MI.11206()#G2_C675::Method6.MI.11208<System.Object>()#"
		call void Generated1219.Generated1219::M.IBase1.T<class Generated1219.BaseClass0,class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C675::Method4.MI.11204()#G2_C675::Method5.MI.11206()#G2_C675::Method6.MI.11208<System.Object>()#"
		call void Generated1219.Generated1219::M.IBase1.A<class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1691::Method7.MI.17471<System.Object>()#"
		call void Generated1219.Generated1219::M.IBase2.T.T<class Generated1219.BaseClass0,class Generated1219.BaseClass0,class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G3_C1691::Method7.MI.17471<System.Object>()#"
		call void Generated1219.Generated1219::M.IBase2.A.T<class Generated1219.BaseClass0,class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G3_C1691::Method7.MI.17471<System.Object>()#"
		call void Generated1219.Generated1219::M.IBase2.A.A<class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1691::Method7.MI.17471<System.Object>()#"
		call void Generated1219.Generated1219::M.IBase2.T.T<class Generated1219.BaseClass0,class Generated1219.BaseClass1,class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G3_C1691::Method7.MI.17471<System.Object>()#"
		call void Generated1219.Generated1219::M.IBase2.A.T<class Generated1219.BaseClass1,class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G3_C1691::Method7.MI.17471<System.Object>()#"
		call void Generated1219.Generated1219::M.IBase2.A.B<class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated1219.Generated1219::M.IBase2.T.T<class Generated1219.BaseClass1,class Generated1219.BaseClass1,class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated1219.Generated1219::M.IBase2.B.T<class Generated1219.BaseClass1,class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated1219.Generated1219::M.IBase2.B.B<class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()#G2_C675::ClassMethod1347.MI.11214<System.Object>()#G2_C675::ClassMethod2709.11211()#G3_C1691::ClassMethod2710.MI.17475<System.Object>()#G2_C675::ClassMethod2711.11213<System.Object>()#G2_C675::Method4.11203()#G2_C675::Method5.11205()#G2_C675::Method6.11207<System.Object>()#G2_C675::Method7.11209<System.Object>()#"
		call void Generated1219.Generated1219::M.G2_C675.T.T<class Generated1219.BaseClass1,class Generated1219.BaseClass0,class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()#G2_C675::ClassMethod1347.MI.11214<System.Object>()#G2_C675::ClassMethod2709.11211()#G3_C1691::ClassMethod2710.MI.17475<System.Object>()#G2_C675::ClassMethod2711.11213<System.Object>()#G2_C675::Method4.11203()#G2_C675::Method5.11205()#G2_C675::Method6.11207<System.Object>()#G2_C675::Method7.11209<System.Object>()#"
		call void Generated1219.Generated1219::M.G2_C675.B.T<class Generated1219.BaseClass0,class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()#G2_C675::ClassMethod1347.MI.11214<System.Object>()#G2_C675::ClassMethod2709.11211()#G3_C1691::ClassMethod2710.MI.17475<System.Object>()#G2_C675::ClassMethod2711.11213<System.Object>()#G2_C675::Method4.11203()#G2_C675::Method5.11205()#G2_C675::Method6.11207<System.Object>()#G2_C675::Method7.11209<System.Object>()#"
		call void Generated1219.Generated1219::M.G2_C675.B.A<class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C675::Method4.MI.11204()#G2_C675::Method5.MI.11206()#G2_C675::Method6.MI.11208<System.Object>()#"
		call void Generated1219.Generated1219::M.IBase1.T<class Generated1219.BaseClass1,class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C675::Method4.MI.11204()#G2_C675::Method5.MI.11206()#G2_C675::Method6.MI.11208<System.Object>()#"
		call void Generated1219.Generated1219::M.IBase1.B<class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()#G2_C675::ClassMethod1347.MI.11214<System.Object>()#G2_C675::ClassMethod2709.11211()#G3_C1691::ClassMethod2710.MI.17475<System.Object>()#G2_C675::ClassMethod2711.11213<System.Object>()#G3_C1691::ClassMethod4779.17472()#G3_C1691::ClassMethod4780.17473<System.Object>()#G3_C1691::ClassMethod4781.17474<System.Object>()#G2_C675::Method4.11203()#G2_C675::Method5.11205()#G2_C675::Method6.11207<System.Object>()#G3_C1691::Method7.17470<System.Object>()#"
		call void Generated1219.Generated1219::M.G3_C1691.T<class Generated1219.BaseClass0,class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()#G2_C675::ClassMethod1347.MI.11214<System.Object>()#G2_C675::ClassMethod2709.11211()#G3_C1691::ClassMethod2710.MI.17475<System.Object>()#G2_C675::ClassMethod2711.11213<System.Object>()#G3_C1691::ClassMethod4779.17472()#G3_C1691::ClassMethod4780.17473<System.Object>()#G3_C1691::ClassMethod4781.17474<System.Object>()#G2_C675::Method4.11203()#G2_C675::Method5.11205()#G2_C675::Method6.11207<System.Object>()#G3_C1691::Method7.17470<System.Object>()#"
		call void Generated1219.Generated1219::M.G3_C1691.A<class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>>(!!0,string)
		newobj instance void class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()#G2_C675::ClassMethod1347.MI.11214<System.Object>()#G2_C675::Method4.11203()#G2_C675::Method5.11205()#G2_C675::Method6.11207<System.Object>()#G2_C675::Method7.11209<System.Object>()#"
		call void Generated1219.Generated1219::M.G1_C12.T.T<class Generated1219.BaseClass1,class Generated1219.BaseClass1,class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()#G2_C675::ClassMethod1347.MI.11214<System.Object>()#G2_C675::Method4.11203()#G2_C675::Method5.11205()#G2_C675::Method6.11207<System.Object>()#G2_C675::Method7.11209<System.Object>()#"
		call void Generated1219.Generated1219::M.G1_C12.B.T<class Generated1219.BaseClass1,class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()#G2_C675::ClassMethod1347.MI.11214<System.Object>()#G2_C675::Method4.11203()#G2_C675::Method5.11205()#G2_C675::Method6.11207<System.Object>()#G2_C675::Method7.11209<System.Object>()#"
		call void Generated1219.Generated1219::M.G1_C12.B.B<class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated1219.Generated1219::M.IBase2.T.T<class Generated1219.BaseClass1,class Generated1219.BaseClass1,class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated1219.Generated1219::M.IBase2.B.T<class Generated1219.BaseClass1,class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated1219.Generated1219::M.IBase2.B.B<class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C675::Method4.MI.11204()#G2_C675::Method5.MI.11206()#G2_C675::Method6.MI.11208<System.Object>()#"
		call void Generated1219.Generated1219::M.IBase1.T<class Generated1219.BaseClass0,class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C675::Method4.MI.11204()#G2_C675::Method5.MI.11206()#G2_C675::Method6.MI.11208<System.Object>()#"
		call void Generated1219.Generated1219::M.IBase1.A<class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1691::Method7.MI.17471<System.Object>()#"
		call void Generated1219.Generated1219::M.IBase2.T.T<class Generated1219.BaseClass0,class Generated1219.BaseClass1,class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G3_C1691::Method7.MI.17471<System.Object>()#"
		call void Generated1219.Generated1219::M.IBase2.A.T<class Generated1219.BaseClass1,class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1691::Method7.MI.17471<System.Object>()#"
		call void Generated1219.Generated1219::M.IBase2.A.B<class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()#G2_C675::ClassMethod1347.MI.11214<System.Object>()#G2_C675::ClassMethod2709.11211()#G3_C1691::ClassMethod2710.MI.17475<System.Object>()#G2_C675::ClassMethod2711.11213<System.Object>()#G2_C675::Method4.11203()#G2_C675::Method5.11205()#G2_C675::Method6.11207<System.Object>()#G2_C675::Method7.11209<System.Object>()#"
		call void Generated1219.Generated1219::M.G2_C675.T.T<class Generated1219.BaseClass1,class Generated1219.BaseClass1,class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()#G2_C675::ClassMethod1347.MI.11214<System.Object>()#G2_C675::ClassMethod2709.11211()#G3_C1691::ClassMethod2710.MI.17475<System.Object>()#G2_C675::ClassMethod2711.11213<System.Object>()#G2_C675::Method4.11203()#G2_C675::Method5.11205()#G2_C675::Method6.11207<System.Object>()#G2_C675::Method7.11209<System.Object>()#"
		call void Generated1219.Generated1219::M.G2_C675.B.T<class Generated1219.BaseClass1,class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()#G2_C675::ClassMethod1347.MI.11214<System.Object>()#G2_C675::ClassMethod2709.11211()#G3_C1691::ClassMethod2710.MI.17475<System.Object>()#G2_C675::ClassMethod2711.11213<System.Object>()#G2_C675::Method4.11203()#G2_C675::Method5.11205()#G2_C675::Method6.11207<System.Object>()#G2_C675::Method7.11209<System.Object>()#"
		call void Generated1219.Generated1219::M.G2_C675.B.B<class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C675::Method4.MI.11204()#G2_C675::Method5.MI.11206()#G2_C675::Method6.MI.11208<System.Object>()#"
		call void Generated1219.Generated1219::M.IBase1.T<class Generated1219.BaseClass1,class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C675::Method4.MI.11204()#G2_C675::Method5.MI.11206()#G2_C675::Method6.MI.11208<System.Object>()#"
		call void Generated1219.Generated1219::M.IBase1.B<class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()#G2_C675::ClassMethod1347.MI.11214<System.Object>()#G2_C675::ClassMethod2709.11211()#G3_C1691::ClassMethod2710.MI.17475<System.Object>()#G2_C675::ClassMethod2711.11213<System.Object>()#G3_C1691::ClassMethod4779.17472()#G3_C1691::ClassMethod4780.17473<System.Object>()#G3_C1691::ClassMethod4781.17474<System.Object>()#G2_C675::Method4.11203()#G2_C675::Method5.11205()#G2_C675::Method6.11207<System.Object>()#G3_C1691::Method7.17470<System.Object>()#"
		call void Generated1219.Generated1219::M.G3_C1691.T<class Generated1219.BaseClass1,class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()#G2_C675::ClassMethod1347.MI.11214<System.Object>()#G2_C675::ClassMethod2709.11211()#G3_C1691::ClassMethod2710.MI.17475<System.Object>()#G2_C675::ClassMethod2711.11213<System.Object>()#G3_C1691::ClassMethod4779.17472()#G3_C1691::ClassMethod4780.17473<System.Object>()#G3_C1691::ClassMethod4781.17474<System.Object>()#G2_C675::Method4.11203()#G2_C675::Method5.11205()#G2_C675::Method6.11207<System.Object>()#G3_C1691::Method7.17470<System.Object>()#"
		call void Generated1219.Generated1219::M.G3_C1691.B<class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1691::Method7.MI.17471<System.Object>()#"
		call void Generated1219.Generated1219::M.IBase2.T.T<class Generated1219.BaseClass0,class Generated1219.BaseClass0,class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G3_C1691::Method7.MI.17471<System.Object>()#"
		call void Generated1219.Generated1219::M.IBase2.A.T<class Generated1219.BaseClass0,class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1691::Method7.MI.17471<System.Object>()#"
		call void Generated1219.Generated1219::M.IBase2.A.A<class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>>(!!0,string)
		newobj instance void class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()#G2_C675::ClassMethod1347.MI.11214<System.Object>()#G2_C675::Method4.11203()#G2_C675::Method5.11205()#G2_C675::Method6.11207<System.Object>()#G2_C675::Method7.11209<System.Object>()#"
		call void Generated1219.Generated1219::M.G1_C12.T.T<class Generated1219.BaseClass0,class Generated1219.BaseClass0,class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()#G2_C675::ClassMethod1347.MI.11214<System.Object>()#G2_C675::Method4.11203()#G2_C675::Method5.11205()#G2_C675::Method6.11207<System.Object>()#G2_C675::Method7.11209<System.Object>()#"
		call void Generated1219.Generated1219::M.G1_C12.A.T<class Generated1219.BaseClass0,class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()#G2_C675::ClassMethod1347.MI.11214<System.Object>()#G2_C675::Method4.11203()#G2_C675::Method5.11205()#G2_C675::Method6.11207<System.Object>()#G2_C675::Method7.11209<System.Object>()#"
		call void Generated1219.Generated1219::M.G1_C12.A.A<class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C675::Method7.MI.11210<System.Object>()#"
		call void Generated1219.Generated1219::M.IBase2.T.T<class Generated1219.BaseClass0,class Generated1219.BaseClass0,class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C675::Method7.MI.11210<System.Object>()#"
		call void Generated1219.Generated1219::M.IBase2.A.T<class Generated1219.BaseClass0,class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C675::Method7.MI.11210<System.Object>()#"
		call void Generated1219.Generated1219::M.IBase2.A.A<class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C675::Method4.MI.11204()#G2_C675::Method5.MI.11206()#G2_C675::Method6.MI.11208<System.Object>()#"
		call void Generated1219.Generated1219::M.IBase1.T<class Generated1219.BaseClass0,class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C675::Method4.MI.11204()#G2_C675::Method5.MI.11206()#G2_C675::Method6.MI.11208<System.Object>()#"
		call void Generated1219.Generated1219::M.IBase1.A<class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C675::Method7.MI.11210<System.Object>()#"
		call void Generated1219.Generated1219::M.IBase2.T.T<class Generated1219.BaseClass0,class Generated1219.BaseClass1,class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C675::Method7.MI.11210<System.Object>()#"
		call void Generated1219.Generated1219::M.IBase2.A.T<class Generated1219.BaseClass1,class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C675::Method7.MI.11210<System.Object>()#"
		call void Generated1219.Generated1219::M.IBase2.A.B<class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()#G2_C675::ClassMethod1347.MI.11214<System.Object>()#G2_C675::ClassMethod2709.11211()#G2_C675::ClassMethod2710.11212<System.Object>()#G2_C675::ClassMethod2711.11213<System.Object>()#G2_C675::Method4.11203()#G2_C675::Method5.11205()#G2_C675::Method6.11207<System.Object>()#G2_C675::Method7.11209<System.Object>()#"
		call void Generated1219.Generated1219::M.G2_C675.T.T<class Generated1219.BaseClass0,class Generated1219.BaseClass0,class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()#G2_C675::ClassMethod1347.MI.11214<System.Object>()#G2_C675::ClassMethod2709.11211()#G2_C675::ClassMethod2710.11212<System.Object>()#G2_C675::ClassMethod2711.11213<System.Object>()#G2_C675::Method4.11203()#G2_C675::Method5.11205()#G2_C675::Method6.11207<System.Object>()#G2_C675::Method7.11209<System.Object>()#"
		call void Generated1219.Generated1219::M.G2_C675.A.T<class Generated1219.BaseClass0,class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()#G2_C675::ClassMethod1347.MI.11214<System.Object>()#G2_C675::ClassMethod2709.11211()#G2_C675::ClassMethod2710.11212<System.Object>()#G2_C675::ClassMethod2711.11213<System.Object>()#G2_C675::Method4.11203()#G2_C675::Method5.11205()#G2_C675::Method6.11207<System.Object>()#G2_C675::Method7.11209<System.Object>()#"
		call void Generated1219.Generated1219::M.G2_C675.A.A<class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C675::Method4.MI.11204()#G2_C675::Method5.MI.11206()#G2_C675::Method6.MI.11208<System.Object>()#"
		call void Generated1219.Generated1219::M.IBase1.T<class Generated1219.BaseClass1,class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C675::Method4.MI.11204()#G2_C675::Method5.MI.11206()#G2_C675::Method6.MI.11208<System.Object>()#"
		call void Generated1219.Generated1219::M.IBase1.B<class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>>(!!0,string)
		newobj instance void class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()#G2_C675::ClassMethod1347.MI.11214<System.Object>()#G2_C675::Method4.11203()#G2_C675::Method5.11205()#G2_C675::Method6.11207<System.Object>()#G2_C675::Method7.11209<System.Object>()#"
		call void Generated1219.Generated1219::M.G1_C12.T.T<class Generated1219.BaseClass0,class Generated1219.BaseClass1,class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()#G2_C675::ClassMethod1347.MI.11214<System.Object>()#G2_C675::Method4.11203()#G2_C675::Method5.11205()#G2_C675::Method6.11207<System.Object>()#G2_C675::Method7.11209<System.Object>()#"
		call void Generated1219.Generated1219::M.G1_C12.A.T<class Generated1219.BaseClass1,class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()#G2_C675::ClassMethod1347.MI.11214<System.Object>()#G2_C675::Method4.11203()#G2_C675::Method5.11205()#G2_C675::Method6.11207<System.Object>()#G2_C675::Method7.11209<System.Object>()#"
		call void Generated1219.Generated1219::M.G1_C12.A.B<class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C675::Method7.MI.11210<System.Object>()#"
		call void Generated1219.Generated1219::M.IBase2.T.T<class Generated1219.BaseClass0,class Generated1219.BaseClass1,class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C675::Method7.MI.11210<System.Object>()#"
		call void Generated1219.Generated1219::M.IBase2.A.T<class Generated1219.BaseClass1,class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C675::Method7.MI.11210<System.Object>()#"
		call void Generated1219.Generated1219::M.IBase2.A.B<class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C675::Method4.MI.11204()#G2_C675::Method5.MI.11206()#G2_C675::Method6.MI.11208<System.Object>()#"
		call void Generated1219.Generated1219::M.IBase1.T<class Generated1219.BaseClass0,class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C675::Method4.MI.11204()#G2_C675::Method5.MI.11206()#G2_C675::Method6.MI.11208<System.Object>()#"
		call void Generated1219.Generated1219::M.IBase1.A<class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()#G2_C675::ClassMethod1347.MI.11214<System.Object>()#G2_C675::ClassMethod2709.11211()#G2_C675::ClassMethod2710.11212<System.Object>()#G2_C675::ClassMethod2711.11213<System.Object>()#G2_C675::Method4.11203()#G2_C675::Method5.11205()#G2_C675::Method6.11207<System.Object>()#G2_C675::Method7.11209<System.Object>()#"
		call void Generated1219.Generated1219::M.G2_C675.T.T<class Generated1219.BaseClass0,class Generated1219.BaseClass1,class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()#G2_C675::ClassMethod1347.MI.11214<System.Object>()#G2_C675::ClassMethod2709.11211()#G2_C675::ClassMethod2710.11212<System.Object>()#G2_C675::ClassMethod2711.11213<System.Object>()#G2_C675::Method4.11203()#G2_C675::Method5.11205()#G2_C675::Method6.11207<System.Object>()#G2_C675::Method7.11209<System.Object>()#"
		call void Generated1219.Generated1219::M.G2_C675.A.T<class Generated1219.BaseClass1,class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()#G2_C675::ClassMethod1347.MI.11214<System.Object>()#G2_C675::ClassMethod2709.11211()#G2_C675::ClassMethod2710.11212<System.Object>()#G2_C675::ClassMethod2711.11213<System.Object>()#G2_C675::Method4.11203()#G2_C675::Method5.11205()#G2_C675::Method6.11207<System.Object>()#G2_C675::Method7.11209<System.Object>()#"
		call void Generated1219.Generated1219::M.G2_C675.A.B<class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C675::Method4.MI.11204()#G2_C675::Method5.MI.11206()#G2_C675::Method6.MI.11208<System.Object>()#"
		call void Generated1219.Generated1219::M.IBase1.T<class Generated1219.BaseClass1,class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C675::Method4.MI.11204()#G2_C675::Method5.MI.11206()#G2_C675::Method6.MI.11208<System.Object>()#"
		call void Generated1219.Generated1219::M.IBase1.B<class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>>(!!0,string)
		newobj instance void class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()#G2_C675::ClassMethod1347.MI.11214<System.Object>()#G2_C675::Method4.11203()#G2_C675::Method5.11205()#G2_C675::Method6.11207<System.Object>()#G2_C675::Method7.11209<System.Object>()#"
		call void Generated1219.Generated1219::M.G1_C12.T.T<class Generated1219.BaseClass1,class Generated1219.BaseClass0,class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()#G2_C675::ClassMethod1347.MI.11214<System.Object>()#G2_C675::Method4.11203()#G2_C675::Method5.11205()#G2_C675::Method6.11207<System.Object>()#G2_C675::Method7.11209<System.Object>()#"
		call void Generated1219.Generated1219::M.G1_C12.B.T<class Generated1219.BaseClass0,class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()#G2_C675::ClassMethod1347.MI.11214<System.Object>()#G2_C675::Method4.11203()#G2_C675::Method5.11205()#G2_C675::Method6.11207<System.Object>()#G2_C675::Method7.11209<System.Object>()#"
		call void Generated1219.Generated1219::M.G1_C12.B.A<class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated1219.Generated1219::M.IBase2.T.T<class Generated1219.BaseClass1,class Generated1219.BaseClass0,class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated1219.Generated1219::M.IBase2.B.T<class Generated1219.BaseClass0,class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated1219.Generated1219::M.IBase2.B.A<class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C675::Method4.MI.11204()#G2_C675::Method5.MI.11206()#G2_C675::Method6.MI.11208<System.Object>()#"
		call void Generated1219.Generated1219::M.IBase1.T<class Generated1219.BaseClass0,class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C675::Method4.MI.11204()#G2_C675::Method5.MI.11206()#G2_C675::Method6.MI.11208<System.Object>()#"
		call void Generated1219.Generated1219::M.IBase1.A<class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C675::Method7.MI.11210<System.Object>()#"
		call void Generated1219.Generated1219::M.IBase2.T.T<class Generated1219.BaseClass0,class Generated1219.BaseClass0,class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C675::Method7.MI.11210<System.Object>()#"
		call void Generated1219.Generated1219::M.IBase2.A.T<class Generated1219.BaseClass0,class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C675::Method7.MI.11210<System.Object>()#"
		call void Generated1219.Generated1219::M.IBase2.A.A<class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C675::Method7.MI.11210<System.Object>()#"
		call void Generated1219.Generated1219::M.IBase2.T.T<class Generated1219.BaseClass0,class Generated1219.BaseClass1,class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C675::Method7.MI.11210<System.Object>()#"
		call void Generated1219.Generated1219::M.IBase2.A.T<class Generated1219.BaseClass1,class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C675::Method7.MI.11210<System.Object>()#"
		call void Generated1219.Generated1219::M.IBase2.A.B<class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated1219.Generated1219::M.IBase2.T.T<class Generated1219.BaseClass1,class Generated1219.BaseClass1,class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated1219.Generated1219::M.IBase2.B.T<class Generated1219.BaseClass1,class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated1219.Generated1219::M.IBase2.B.B<class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()#G2_C675::ClassMethod1347.MI.11214<System.Object>()#G2_C675::ClassMethod2709.11211()#G2_C675::ClassMethod2710.11212<System.Object>()#G2_C675::ClassMethod2711.11213<System.Object>()#G2_C675::Method4.11203()#G2_C675::Method5.11205()#G2_C675::Method6.11207<System.Object>()#G2_C675::Method7.11209<System.Object>()#"
		call void Generated1219.Generated1219::M.G2_C675.T.T<class Generated1219.BaseClass1,class Generated1219.BaseClass0,class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()#G2_C675::ClassMethod1347.MI.11214<System.Object>()#G2_C675::ClassMethod2709.11211()#G2_C675::ClassMethod2710.11212<System.Object>()#G2_C675::ClassMethod2711.11213<System.Object>()#G2_C675::Method4.11203()#G2_C675::Method5.11205()#G2_C675::Method6.11207<System.Object>()#G2_C675::Method7.11209<System.Object>()#"
		call void Generated1219.Generated1219::M.G2_C675.B.T<class Generated1219.BaseClass0,class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()#G2_C675::ClassMethod1347.MI.11214<System.Object>()#G2_C675::ClassMethod2709.11211()#G2_C675::ClassMethod2710.11212<System.Object>()#G2_C675::ClassMethod2711.11213<System.Object>()#G2_C675::Method4.11203()#G2_C675::Method5.11205()#G2_C675::Method6.11207<System.Object>()#G2_C675::Method7.11209<System.Object>()#"
		call void Generated1219.Generated1219::M.G2_C675.B.A<class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C675::Method4.MI.11204()#G2_C675::Method5.MI.11206()#G2_C675::Method6.MI.11208<System.Object>()#"
		call void Generated1219.Generated1219::M.IBase1.T<class Generated1219.BaseClass1,class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C675::Method4.MI.11204()#G2_C675::Method5.MI.11206()#G2_C675::Method6.MI.11208<System.Object>()#"
		call void Generated1219.Generated1219::M.IBase1.B<class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>>(!!0,string)
		newobj instance void class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()#G2_C675::ClassMethod1347.MI.11214<System.Object>()#G2_C675::Method4.11203()#G2_C675::Method5.11205()#G2_C675::Method6.11207<System.Object>()#G2_C675::Method7.11209<System.Object>()#"
		call void Generated1219.Generated1219::M.G1_C12.T.T<class Generated1219.BaseClass1,class Generated1219.BaseClass1,class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()#G2_C675::ClassMethod1347.MI.11214<System.Object>()#G2_C675::Method4.11203()#G2_C675::Method5.11205()#G2_C675::Method6.11207<System.Object>()#G2_C675::Method7.11209<System.Object>()#"
		call void Generated1219.Generated1219::M.G1_C12.B.T<class Generated1219.BaseClass1,class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()#G2_C675::ClassMethod1347.MI.11214<System.Object>()#G2_C675::Method4.11203()#G2_C675::Method5.11205()#G2_C675::Method6.11207<System.Object>()#G2_C675::Method7.11209<System.Object>()#"
		call void Generated1219.Generated1219::M.G1_C12.B.B<class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated1219.Generated1219::M.IBase2.T.T<class Generated1219.BaseClass1,class Generated1219.BaseClass1,class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated1219.Generated1219::M.IBase2.B.T<class Generated1219.BaseClass1,class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated1219.Generated1219::M.IBase2.B.B<class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C675::Method4.MI.11204()#G2_C675::Method5.MI.11206()#G2_C675::Method6.MI.11208<System.Object>()#"
		call void Generated1219.Generated1219::M.IBase1.T<class Generated1219.BaseClass0,class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C675::Method4.MI.11204()#G2_C675::Method5.MI.11206()#G2_C675::Method6.MI.11208<System.Object>()#"
		call void Generated1219.Generated1219::M.IBase1.A<class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C675::Method7.MI.11210<System.Object>()#"
		call void Generated1219.Generated1219::M.IBase2.T.T<class Generated1219.BaseClass0,class Generated1219.BaseClass1,class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C675::Method7.MI.11210<System.Object>()#"
		call void Generated1219.Generated1219::M.IBase2.A.T<class Generated1219.BaseClass1,class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C675::Method7.MI.11210<System.Object>()#"
		call void Generated1219.Generated1219::M.IBase2.A.B<class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()#G2_C675::ClassMethod1347.MI.11214<System.Object>()#G2_C675::ClassMethod2709.11211()#G2_C675::ClassMethod2710.11212<System.Object>()#G2_C675::ClassMethod2711.11213<System.Object>()#G2_C675::Method4.11203()#G2_C675::Method5.11205()#G2_C675::Method6.11207<System.Object>()#G2_C675::Method7.11209<System.Object>()#"
		call void Generated1219.Generated1219::M.G2_C675.T.T<class Generated1219.BaseClass1,class Generated1219.BaseClass1,class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()#G2_C675::ClassMethod1347.MI.11214<System.Object>()#G2_C675::ClassMethod2709.11211()#G2_C675::ClassMethod2710.11212<System.Object>()#G2_C675::ClassMethod2711.11213<System.Object>()#G2_C675::Method4.11203()#G2_C675::Method5.11205()#G2_C675::Method6.11207<System.Object>()#G2_C675::Method7.11209<System.Object>()#"
		call void Generated1219.Generated1219::M.G2_C675.B.T<class Generated1219.BaseClass1,class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()#G2_C675::ClassMethod1347.MI.11214<System.Object>()#G2_C675::ClassMethod2709.11211()#G2_C675::ClassMethod2710.11212<System.Object>()#G2_C675::ClassMethod2711.11213<System.Object>()#G2_C675::Method4.11203()#G2_C675::Method5.11205()#G2_C675::Method6.11207<System.Object>()#G2_C675::Method7.11209<System.Object>()#"
		call void Generated1219.Generated1219::M.G2_C675.B.B<class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C675::Method4.MI.11204()#G2_C675::Method5.MI.11206()#G2_C675::Method6.MI.11208<System.Object>()#"
		call void Generated1219.Generated1219::M.IBase1.T<class Generated1219.BaseClass1,class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C675::Method4.MI.11204()#G2_C675::Method5.MI.11206()#G2_C675::Method6.MI.11208<System.Object>()#"
		call void Generated1219.Generated1219::M.IBase1.B<class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>>(!!0,string)
		newobj instance void class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()#G1_C12::ClassMethod1347.4870<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Generated1219.Generated1219::M.G1_C12.T.T<class Generated1219.BaseClass0,class Generated1219.BaseClass0,class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()#G1_C12::ClassMethod1347.4870<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Generated1219.Generated1219::M.G1_C12.A.T<class Generated1219.BaseClass0,class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()#G1_C12::ClassMethod1347.4870<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Generated1219.Generated1219::M.G1_C12.A.A<class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated1219.Generated1219::M.IBase2.T.T<class Generated1219.BaseClass0,class Generated1219.BaseClass0,class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated1219.Generated1219::M.IBase2.A.T<class Generated1219.BaseClass0,class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated1219.Generated1219::M.IBase2.A.A<class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.MI.4868<System.Object>()#"
		call void Generated1219.Generated1219::M.IBase1.T<class Generated1219.BaseClass0,class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.MI.4868<System.Object>()#"
		call void Generated1219.Generated1219::M.IBase1.A<class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated1219.Generated1219::M.IBase2.T.T<class Generated1219.BaseClass0,class Generated1219.BaseClass1,class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated1219.Generated1219::M.IBase2.A.T<class Generated1219.BaseClass1,class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated1219.Generated1219::M.IBase2.A.B<class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>>(!!0,string)
		newobj instance void class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()#G1_C12::ClassMethod1347.4870<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Generated1219.Generated1219::M.G1_C12.T.T<class Generated1219.BaseClass0,class Generated1219.BaseClass1,class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()#G1_C12::ClassMethod1347.4870<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Generated1219.Generated1219::M.G1_C12.A.T<class Generated1219.BaseClass1,class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()#G1_C12::ClassMethod1347.4870<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Generated1219.Generated1219::M.G1_C12.A.B<class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated1219.Generated1219::M.IBase2.T.T<class Generated1219.BaseClass0,class Generated1219.BaseClass1,class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated1219.Generated1219::M.IBase2.A.T<class Generated1219.BaseClass1,class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated1219.Generated1219::M.IBase2.A.B<class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.MI.4868<System.Object>()#"
		call void Generated1219.Generated1219::M.IBase1.T<class Generated1219.BaseClass0,class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.MI.4868<System.Object>()#"
		call void Generated1219.Generated1219::M.IBase1.A<class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>>(!!0,string)
		newobj instance void class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()#G1_C12::ClassMethod1347.4870<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Generated1219.Generated1219::M.G1_C12.T.T<class Generated1219.BaseClass1,class Generated1219.BaseClass0,class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()#G1_C12::ClassMethod1347.4870<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Generated1219.Generated1219::M.G1_C12.B.T<class Generated1219.BaseClass0,class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()#G1_C12::ClassMethod1347.4870<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Generated1219.Generated1219::M.G1_C12.B.A<class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated1219.Generated1219::M.IBase2.T.T<class Generated1219.BaseClass1,class Generated1219.BaseClass0,class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated1219.Generated1219::M.IBase2.B.T<class Generated1219.BaseClass0,class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated1219.Generated1219::M.IBase2.B.A<class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.MI.4868<System.Object>()#"
		call void Generated1219.Generated1219::M.IBase1.T<class Generated1219.BaseClass0,class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.MI.4868<System.Object>()#"
		call void Generated1219.Generated1219::M.IBase1.A<class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated1219.Generated1219::M.IBase2.T.T<class Generated1219.BaseClass0,class Generated1219.BaseClass0,class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated1219.Generated1219::M.IBase2.A.T<class Generated1219.BaseClass0,class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated1219.Generated1219::M.IBase2.A.A<class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated1219.Generated1219::M.IBase2.T.T<class Generated1219.BaseClass0,class Generated1219.BaseClass1,class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated1219.Generated1219::M.IBase2.A.T<class Generated1219.BaseClass1,class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated1219.Generated1219::M.IBase2.A.B<class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated1219.Generated1219::M.IBase2.T.T<class Generated1219.BaseClass1,class Generated1219.BaseClass1,class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated1219.Generated1219::M.IBase2.B.T<class Generated1219.BaseClass1,class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated1219.Generated1219::M.IBase2.B.B<class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>>(!!0,string)
		newobj instance void class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()#G1_C12::ClassMethod1347.4870<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Generated1219.Generated1219::M.G1_C12.T.T<class Generated1219.BaseClass1,class Generated1219.BaseClass1,class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()#G1_C12::ClassMethod1347.4870<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Generated1219.Generated1219::M.G1_C12.B.T<class Generated1219.BaseClass1,class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()#G1_C12::ClassMethod1347.4870<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Generated1219.Generated1219::M.G1_C12.B.B<class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated1219.Generated1219::M.IBase2.T.T<class Generated1219.BaseClass1,class Generated1219.BaseClass1,class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated1219.Generated1219::M.IBase2.B.T<class Generated1219.BaseClass1,class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated1219.Generated1219::M.IBase2.B.B<class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.MI.4868<System.Object>()#"
		call void Generated1219.Generated1219::M.IBase1.T<class Generated1219.BaseClass0,class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.MI.4868<System.Object>()#"
		call void Generated1219.Generated1219::M.IBase1.A<class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated1219.Generated1219::M.IBase2.T.T<class Generated1219.BaseClass0,class Generated1219.BaseClass1,class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated1219.Generated1219::M.IBase2.A.T<class Generated1219.BaseClass1,class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated1219.Generated1219::M.IBase2.A.B<class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>>(!!0,string)
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
		newobj instance void class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>::ClassMethod1347<object>()
		calli default string(class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>)
		ldstr "G2_C675::ClassMethod1347.MI.11214<System.Object>()"
		ldstr "class G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>::ClassMethod1346<object>()
		calli default string(class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>)
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()"
		ldstr "class G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>::Method6<object>()
		calli default string(class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>)
		ldstr "G2_C675::Method6.11207<System.Object>()"
		ldstr "class G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>::Method5()
		calli default string(class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>)
		ldstr "G2_C675::Method5.11205()"
		ldstr "class G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>::Method4()
		calli default string(class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>)
		ldstr "G2_C675::Method4.11203()"
		ldstr "class G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>::Method7<object>()
		calli default string(class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>)
		ldstr "G2_C675::Method7.11209<System.Object>()"
		ldstr "class G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1219.IBase2`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>::Method7<object>()
		calli default string(class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>)
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1219.IBase1`1<class Generated1219.BaseClass0>::Method4()
		calli default string(class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>)
		ldstr "G2_C675::Method4.MI.11204()"
		ldstr "class IBase1`1<class Generated1219.BaseClass0> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1219.IBase1`1<class Generated1219.BaseClass0>::Method5()
		calli default string(class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>)
		ldstr "G2_C675::Method5.MI.11206()"
		ldstr "class IBase1`1<class Generated1219.BaseClass0> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1219.IBase1`1<class Generated1219.BaseClass0>::Method6<object>()
		calli default string(class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>)
		ldstr "G2_C675::Method6.MI.11208<System.Object>()"
		ldstr "class IBase1`1<class Generated1219.BaseClass0> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1219.IBase2`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>::Method7<object>()
		calli default string(class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>)
		ldstr "G3_C1691::Method7.MI.17471<System.Object>()"
		ldstr "class IBase2`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1219.IBase2`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>::Method7<object>()
		calli default string(class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>)
		ldstr "G3_C1691::Method7.MI.17471<System.Object>()"
		ldstr "class IBase2`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1219.IBase2`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>::Method7<object>()
		calli default string(class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>)
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>::ClassMethod2711<object>()
		calli default string(class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>)
		ldstr "G2_C675::ClassMethod2711.11213<System.Object>()"
		ldstr "class G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>::ClassMethod2710<object>()
		calli default string(class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>)
		ldstr "G3_C1691::ClassMethod2710.MI.17475<System.Object>()"
		ldstr "class G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>::ClassMethod2709()
		calli default string(class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>)
		ldstr "G2_C675::ClassMethod2709.11211()"
		ldstr "class G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>::Method7<object>()
		calli default string(class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>)
		ldstr "G2_C675::Method7.11209<System.Object>()"
		ldstr "class G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>::Method6<object>()
		calli default string(class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>)
		ldstr "G2_C675::Method6.11207<System.Object>()"
		ldstr "class G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>::Method5()
		calli default string(class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>)
		ldstr "G2_C675::Method5.11205()"
		ldstr "class G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>::Method4()
		calli default string(class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>)
		ldstr "G2_C675::Method4.11203()"
		ldstr "class G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>::ClassMethod1347<object>()
		calli default string(class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>)
		ldstr "G2_C675::ClassMethod1347.MI.11214<System.Object>()"
		ldstr "class G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>::ClassMethod1346<object>()
		calli default string(class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>)
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()"
		ldstr "class G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1219.IBase1`1<class Generated1219.BaseClass1>::Method4()
		calli default string(class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>)
		ldstr "G2_C675::Method4.MI.11204()"
		ldstr "class IBase1`1<class Generated1219.BaseClass1> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1219.IBase1`1<class Generated1219.BaseClass1>::Method5()
		calli default string(class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>)
		ldstr "G2_C675::Method5.MI.11206()"
		ldstr "class IBase1`1<class Generated1219.BaseClass1> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1219.IBase1`1<class Generated1219.BaseClass1>::Method6<object>()
		calli default string(class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>)
		ldstr "G2_C675::Method6.MI.11208<System.Object>()"
		ldstr "class IBase1`1<class Generated1219.BaseClass1> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>::ClassMethod4781<object>()
		calli default string(class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>)
		ldstr "G3_C1691::ClassMethod4781.17474<System.Object>()"
		ldstr "class G3_C1691`1<class Generated1219.BaseClass0> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>::ClassMethod4780<object>()
		calli default string(class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>)
		ldstr "G3_C1691::ClassMethod4780.17473<System.Object>()"
		ldstr "class G3_C1691`1<class Generated1219.BaseClass0> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>::ClassMethod4779()
		calli default string(class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>)
		ldstr "G3_C1691::ClassMethod4779.17472()"
		ldstr "class G3_C1691`1<class Generated1219.BaseClass0> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>::Method7<object>()
		calli default string(class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>)
		ldstr "G3_C1691::Method7.17470<System.Object>()"
		ldstr "class G3_C1691`1<class Generated1219.BaseClass0> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>::ClassMethod2711<object>()
		calli default string(class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>)
		ldstr "G2_C675::ClassMethod2711.11213<System.Object>()"
		ldstr "class G3_C1691`1<class Generated1219.BaseClass0> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>::ClassMethod2710<object>()
		calli default string(class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>)
		ldstr "G3_C1691::ClassMethod2710.MI.17475<System.Object>()"
		ldstr "class G3_C1691`1<class Generated1219.BaseClass0> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>::ClassMethod2709()
		calli default string(class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>)
		ldstr "G2_C675::ClassMethod2709.11211()"
		ldstr "class G3_C1691`1<class Generated1219.BaseClass0> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>::Method6<object>()
		calli default string(class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>)
		ldstr "G2_C675::Method6.11207<System.Object>()"
		ldstr "class G3_C1691`1<class Generated1219.BaseClass0> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>::Method5()
		calli default string(class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>)
		ldstr "G2_C675::Method5.11205()"
		ldstr "class G3_C1691`1<class Generated1219.BaseClass0> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>::Method4()
		calli default string(class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>)
		ldstr "G2_C675::Method4.11203()"
		ldstr "class G3_C1691`1<class Generated1219.BaseClass0> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>::ClassMethod1347<object>()
		calli default string(class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>)
		ldstr "G2_C675::ClassMethod1347.MI.11214<System.Object>()"
		ldstr "class G3_C1691`1<class Generated1219.BaseClass0> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>::ClassMethod1346<object>()
		calli default string(class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>)
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()"
		ldstr "class G3_C1691`1<class Generated1219.BaseClass0> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>::ClassMethod1347<object>()
		calli default string(class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>)
		ldstr "G2_C675::ClassMethod1347.MI.11214<System.Object>()"
		ldstr "class G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>::ClassMethod1346<object>()
		calli default string(class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>)
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()"
		ldstr "class G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>::Method6<object>()
		calli default string(class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>)
		ldstr "G2_C675::Method6.11207<System.Object>()"
		ldstr "class G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>::Method5()
		calli default string(class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>)
		ldstr "G2_C675::Method5.11205()"
		ldstr "class G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>::Method4()
		calli default string(class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>)
		ldstr "G2_C675::Method4.11203()"
		ldstr "class G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>::Method7<object>()
		calli default string(class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>)
		ldstr "G2_C675::Method7.11209<System.Object>()"
		ldstr "class G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1219.IBase2`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>::Method7<object>()
		calli default string(class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>)
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1219.IBase1`1<class Generated1219.BaseClass0>::Method4()
		calli default string(class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>)
		ldstr "G2_C675::Method4.MI.11204()"
		ldstr "class IBase1`1<class Generated1219.BaseClass0> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1219.IBase1`1<class Generated1219.BaseClass0>::Method5()
		calli default string(class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>)
		ldstr "G2_C675::Method5.MI.11206()"
		ldstr "class IBase1`1<class Generated1219.BaseClass0> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1219.IBase1`1<class Generated1219.BaseClass0>::Method6<object>()
		calli default string(class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>)
		ldstr "G2_C675::Method6.MI.11208<System.Object>()"
		ldstr "class IBase1`1<class Generated1219.BaseClass0> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1219.IBase2`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>::Method7<object>()
		calli default string(class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>)
		ldstr "G3_C1691::Method7.MI.17471<System.Object>()"
		ldstr "class IBase2`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>::ClassMethod2711<object>()
		calli default string(class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>)
		ldstr "G2_C675::ClassMethod2711.11213<System.Object>()"
		ldstr "class G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>::ClassMethod2710<object>()
		calli default string(class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>)
		ldstr "G3_C1691::ClassMethod2710.MI.17475<System.Object>()"
		ldstr "class G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>::ClassMethod2709()
		calli default string(class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>)
		ldstr "G2_C675::ClassMethod2709.11211()"
		ldstr "class G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>::Method7<object>()
		calli default string(class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>)
		ldstr "G2_C675::Method7.11209<System.Object>()"
		ldstr "class G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>::Method6<object>()
		calli default string(class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>)
		ldstr "G2_C675::Method6.11207<System.Object>()"
		ldstr "class G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>::Method5()
		calli default string(class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>)
		ldstr "G2_C675::Method5.11205()"
		ldstr "class G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>::Method4()
		calli default string(class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>)
		ldstr "G2_C675::Method4.11203()"
		ldstr "class G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>::ClassMethod1347<object>()
		calli default string(class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>)
		ldstr "G2_C675::ClassMethod1347.MI.11214<System.Object>()"
		ldstr "class G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>::ClassMethod1346<object>()
		calli default string(class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>)
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()"
		ldstr "class G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1219.IBase1`1<class Generated1219.BaseClass1>::Method4()
		calli default string(class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>)
		ldstr "G2_C675::Method4.MI.11204()"
		ldstr "class IBase1`1<class Generated1219.BaseClass1> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1219.IBase1`1<class Generated1219.BaseClass1>::Method5()
		calli default string(class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>)
		ldstr "G2_C675::Method5.MI.11206()"
		ldstr "class IBase1`1<class Generated1219.BaseClass1> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1219.IBase1`1<class Generated1219.BaseClass1>::Method6<object>()
		calli default string(class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>)
		ldstr "G2_C675::Method6.MI.11208<System.Object>()"
		ldstr "class IBase1`1<class Generated1219.BaseClass1> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>::ClassMethod4781<object>()
		calli default string(class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>)
		ldstr "G3_C1691::ClassMethod4781.17474<System.Object>()"
		ldstr "class G3_C1691`1<class Generated1219.BaseClass1> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>::ClassMethod4780<object>()
		calli default string(class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>)
		ldstr "G3_C1691::ClassMethod4780.17473<System.Object>()"
		ldstr "class G3_C1691`1<class Generated1219.BaseClass1> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>::ClassMethod4779()
		calli default string(class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>)
		ldstr "G3_C1691::ClassMethod4779.17472()"
		ldstr "class G3_C1691`1<class Generated1219.BaseClass1> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>::Method7<object>()
		calli default string(class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>)
		ldstr "G3_C1691::Method7.17470<System.Object>()"
		ldstr "class G3_C1691`1<class Generated1219.BaseClass1> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>::ClassMethod2711<object>()
		calli default string(class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>)
		ldstr "G2_C675::ClassMethod2711.11213<System.Object>()"
		ldstr "class G3_C1691`1<class Generated1219.BaseClass1> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>::ClassMethod2710<object>()
		calli default string(class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>)
		ldstr "G3_C1691::ClassMethod2710.MI.17475<System.Object>()"
		ldstr "class G3_C1691`1<class Generated1219.BaseClass1> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>::ClassMethod2709()
		calli default string(class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>)
		ldstr "G2_C675::ClassMethod2709.11211()"
		ldstr "class G3_C1691`1<class Generated1219.BaseClass1> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>::Method6<object>()
		calli default string(class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>)
		ldstr "G2_C675::Method6.11207<System.Object>()"
		ldstr "class G3_C1691`1<class Generated1219.BaseClass1> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>::Method5()
		calli default string(class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>)
		ldstr "G2_C675::Method5.11205()"
		ldstr "class G3_C1691`1<class Generated1219.BaseClass1> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>::Method4()
		calli default string(class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>)
		ldstr "G2_C675::Method4.11203()"
		ldstr "class G3_C1691`1<class Generated1219.BaseClass1> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>::ClassMethod1347<object>()
		calli default string(class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>)
		ldstr "G2_C675::ClassMethod1347.MI.11214<System.Object>()"
		ldstr "class G3_C1691`1<class Generated1219.BaseClass1> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>::ClassMethod1346<object>()
		calli default string(class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>)
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()"
		ldstr "class G3_C1691`1<class Generated1219.BaseClass1> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1219.IBase2`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>::Method7<object>()
		calli default string(class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>)
		ldstr "G3_C1691::Method7.MI.17471<System.Object>()"
		ldstr "class IBase2`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0> on type class Generated1219.G3_C1691`1<class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>::ClassMethod1347<object>()
		calli default string(class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>)
		ldstr "G2_C675::ClassMethod1347.MI.11214<System.Object>()"
		ldstr "class G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>::ClassMethod1346<object>()
		calli default string(class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>)
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()"
		ldstr "class G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>::Method6<object>()
		calli default string(class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>)
		ldstr "G2_C675::Method6.11207<System.Object>()"
		ldstr "class G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>::Method5()
		calli default string(class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>)
		ldstr "G2_C675::Method5.11205()"
		ldstr "class G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>::Method4()
		calli default string(class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>)
		ldstr "G2_C675::Method4.11203()"
		ldstr "class G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>::Method7<object>()
		calli default string(class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>)
		ldstr "G2_C675::Method7.11209<System.Object>()"
		ldstr "class G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1219.IBase2`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>::Method7<object>()
		calli default string(class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>)
		ldstr "G2_C675::Method7.MI.11210<System.Object>()"
		ldstr "class IBase2`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1219.IBase1`1<class Generated1219.BaseClass0>::Method4()
		calli default string(class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>)
		ldstr "G2_C675::Method4.MI.11204()"
		ldstr "class IBase1`1<class Generated1219.BaseClass0> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1219.IBase1`1<class Generated1219.BaseClass0>::Method5()
		calli default string(class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>)
		ldstr "G2_C675::Method5.MI.11206()"
		ldstr "class IBase1`1<class Generated1219.BaseClass0> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1219.IBase1`1<class Generated1219.BaseClass0>::Method6<object>()
		calli default string(class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>)
		ldstr "G2_C675::Method6.MI.11208<System.Object>()"
		ldstr "class IBase1`1<class Generated1219.BaseClass0> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1219.IBase2`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>::Method7<object>()
		calli default string(class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>)
		ldstr "G2_C675::Method7.MI.11210<System.Object>()"
		ldstr "class IBase2`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>::ClassMethod2711<object>()
		calli default string(class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>)
		ldstr "G2_C675::ClassMethod2711.11213<System.Object>()"
		ldstr "class G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>::ClassMethod2710<object>()
		calli default string(class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>)
		ldstr "G2_C675::ClassMethod2710.11212<System.Object>()"
		ldstr "class G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>::ClassMethod2709()
		calli default string(class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>)
		ldstr "G2_C675::ClassMethod2709.11211()"
		ldstr "class G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>::Method7<object>()
		calli default string(class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>)
		ldstr "G2_C675::Method7.11209<System.Object>()"
		ldstr "class G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>::Method6<object>()
		calli default string(class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>)
		ldstr "G2_C675::Method6.11207<System.Object>()"
		ldstr "class G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>::Method5()
		calli default string(class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>)
		ldstr "G2_C675::Method5.11205()"
		ldstr "class G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>::Method4()
		calli default string(class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>)
		ldstr "G2_C675::Method4.11203()"
		ldstr "class G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>::ClassMethod1347<object>()
		calli default string(class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>)
		ldstr "G2_C675::ClassMethod1347.MI.11214<System.Object>()"
		ldstr "class G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>::ClassMethod1346<object>()
		calli default string(class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>)
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()"
		ldstr "class G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1219.IBase1`1<class Generated1219.BaseClass1>::Method4()
		calli default string(class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>)
		ldstr "G2_C675::Method4.MI.11204()"
		ldstr "class IBase1`1<class Generated1219.BaseClass1> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1219.IBase1`1<class Generated1219.BaseClass1>::Method5()
		calli default string(class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>)
		ldstr "G2_C675::Method5.MI.11206()"
		ldstr "class IBase1`1<class Generated1219.BaseClass1> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1219.IBase1`1<class Generated1219.BaseClass1>::Method6<object>()
		calli default string(class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>)
		ldstr "G2_C675::Method6.MI.11208<System.Object>()"
		ldstr "class IBase1`1<class Generated1219.BaseClass1> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>::ClassMethod1347<object>()
		calli default string(class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>)
		ldstr "G2_C675::ClassMethod1347.MI.11214<System.Object>()"
		ldstr "class G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>::ClassMethod1346<object>()
		calli default string(class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>)
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()"
		ldstr "class G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>::Method6<object>()
		calli default string(class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>)
		ldstr "G2_C675::Method6.11207<System.Object>()"
		ldstr "class G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>::Method5()
		calli default string(class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>)
		ldstr "G2_C675::Method5.11205()"
		ldstr "class G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>::Method4()
		calli default string(class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>)
		ldstr "G2_C675::Method4.11203()"
		ldstr "class G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>::Method7<object>()
		calli default string(class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>)
		ldstr "G2_C675::Method7.11209<System.Object>()"
		ldstr "class G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1219.IBase2`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>::Method7<object>()
		calli default string(class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>)
		ldstr "G2_C675::Method7.MI.11210<System.Object>()"
		ldstr "class IBase2`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1219.IBase1`1<class Generated1219.BaseClass0>::Method4()
		calli default string(class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>)
		ldstr "G2_C675::Method4.MI.11204()"
		ldstr "class IBase1`1<class Generated1219.BaseClass0> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1219.IBase1`1<class Generated1219.BaseClass0>::Method5()
		calli default string(class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>)
		ldstr "G2_C675::Method5.MI.11206()"
		ldstr "class IBase1`1<class Generated1219.BaseClass0> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1219.IBase1`1<class Generated1219.BaseClass0>::Method6<object>()
		calli default string(class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>)
		ldstr "G2_C675::Method6.MI.11208<System.Object>()"
		ldstr "class IBase1`1<class Generated1219.BaseClass0> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>::ClassMethod2711<object>()
		calli default string(class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>)
		ldstr "G2_C675::ClassMethod2711.11213<System.Object>()"
		ldstr "class G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>::ClassMethod2710<object>()
		calli default string(class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>)
		ldstr "G2_C675::ClassMethod2710.11212<System.Object>()"
		ldstr "class G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>::ClassMethod2709()
		calli default string(class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>)
		ldstr "G2_C675::ClassMethod2709.11211()"
		ldstr "class G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>::Method7<object>()
		calli default string(class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>)
		ldstr "G2_C675::Method7.11209<System.Object>()"
		ldstr "class G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>::Method6<object>()
		calli default string(class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>)
		ldstr "G2_C675::Method6.11207<System.Object>()"
		ldstr "class G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>::Method5()
		calli default string(class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>)
		ldstr "G2_C675::Method5.11205()"
		ldstr "class G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>::Method4()
		calli default string(class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>)
		ldstr "G2_C675::Method4.11203()"
		ldstr "class G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>::ClassMethod1347<object>()
		calli default string(class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>)
		ldstr "G2_C675::ClassMethod1347.MI.11214<System.Object>()"
		ldstr "class G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>::ClassMethod1346<object>()
		calli default string(class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>)
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()"
		ldstr "class G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1219.IBase1`1<class Generated1219.BaseClass1>::Method4()
		calli default string(class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>)
		ldstr "G2_C675::Method4.MI.11204()"
		ldstr "class IBase1`1<class Generated1219.BaseClass1> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1219.IBase1`1<class Generated1219.BaseClass1>::Method5()
		calli default string(class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>)
		ldstr "G2_C675::Method5.MI.11206()"
		ldstr "class IBase1`1<class Generated1219.BaseClass1> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1219.IBase1`1<class Generated1219.BaseClass1>::Method6<object>()
		calli default string(class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>)
		ldstr "G2_C675::Method6.MI.11208<System.Object>()"
		ldstr "class IBase1`1<class Generated1219.BaseClass1> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>::ClassMethod1347<object>()
		calli default string(class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>)
		ldstr "G2_C675::ClassMethod1347.MI.11214<System.Object>()"
		ldstr "class G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>::ClassMethod1346<object>()
		calli default string(class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>)
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()"
		ldstr "class G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>::Method6<object>()
		calli default string(class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>)
		ldstr "G2_C675::Method6.11207<System.Object>()"
		ldstr "class G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>::Method5()
		calli default string(class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>)
		ldstr "G2_C675::Method5.11205()"
		ldstr "class G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>::Method4()
		calli default string(class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>)
		ldstr "G2_C675::Method4.11203()"
		ldstr "class G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>::Method7<object>()
		calli default string(class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>)
		ldstr "G2_C675::Method7.11209<System.Object>()"
		ldstr "class G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1219.IBase2`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>::Method7<object>()
		calli default string(class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>)
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1219.IBase1`1<class Generated1219.BaseClass0>::Method4()
		calli default string(class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>)
		ldstr "G2_C675::Method4.MI.11204()"
		ldstr "class IBase1`1<class Generated1219.BaseClass0> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1219.IBase1`1<class Generated1219.BaseClass0>::Method5()
		calli default string(class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>)
		ldstr "G2_C675::Method5.MI.11206()"
		ldstr "class IBase1`1<class Generated1219.BaseClass0> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1219.IBase1`1<class Generated1219.BaseClass0>::Method6<object>()
		calli default string(class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>)
		ldstr "G2_C675::Method6.MI.11208<System.Object>()"
		ldstr "class IBase1`1<class Generated1219.BaseClass0> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1219.IBase2`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>::Method7<object>()
		calli default string(class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>)
		ldstr "G2_C675::Method7.MI.11210<System.Object>()"
		ldstr "class IBase2`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1219.IBase2`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>::Method7<object>()
		calli default string(class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>)
		ldstr "G2_C675::Method7.MI.11210<System.Object>()"
		ldstr "class IBase2`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1219.IBase2`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>::Method7<object>()
		calli default string(class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>)
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>::ClassMethod2711<object>()
		calli default string(class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>)
		ldstr "G2_C675::ClassMethod2711.11213<System.Object>()"
		ldstr "class G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>::ClassMethod2710<object>()
		calli default string(class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>)
		ldstr "G2_C675::ClassMethod2710.11212<System.Object>()"
		ldstr "class G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>::ClassMethod2709()
		calli default string(class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>)
		ldstr "G2_C675::ClassMethod2709.11211()"
		ldstr "class G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>::Method7<object>()
		calli default string(class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>)
		ldstr "G2_C675::Method7.11209<System.Object>()"
		ldstr "class G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>::Method6<object>()
		calli default string(class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>)
		ldstr "G2_C675::Method6.11207<System.Object>()"
		ldstr "class G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>::Method5()
		calli default string(class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>)
		ldstr "G2_C675::Method5.11205()"
		ldstr "class G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>::Method4()
		calli default string(class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>)
		ldstr "G2_C675::Method4.11203()"
		ldstr "class G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>::ClassMethod1347<object>()
		calli default string(class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>)
		ldstr "G2_C675::ClassMethod1347.MI.11214<System.Object>()"
		ldstr "class G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>::ClassMethod1346<object>()
		calli default string(class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>)
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()"
		ldstr "class G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1219.IBase1`1<class Generated1219.BaseClass1>::Method4()
		calli default string(class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>)
		ldstr "G2_C675::Method4.MI.11204()"
		ldstr "class IBase1`1<class Generated1219.BaseClass1> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1219.IBase1`1<class Generated1219.BaseClass1>::Method5()
		calli default string(class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>)
		ldstr "G2_C675::Method5.MI.11206()"
		ldstr "class IBase1`1<class Generated1219.BaseClass1> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1219.IBase1`1<class Generated1219.BaseClass1>::Method6<object>()
		calli default string(class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>)
		ldstr "G2_C675::Method6.MI.11208<System.Object>()"
		ldstr "class IBase1`1<class Generated1219.BaseClass1> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>::ClassMethod1347<object>()
		calli default string(class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>)
		ldstr "G2_C675::ClassMethod1347.MI.11214<System.Object>()"
		ldstr "class G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>::ClassMethod1346<object>()
		calli default string(class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>)
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()"
		ldstr "class G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>::Method6<object>()
		calli default string(class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>)
		ldstr "G2_C675::Method6.11207<System.Object>()"
		ldstr "class G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>::Method5()
		calli default string(class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>)
		ldstr "G2_C675::Method5.11205()"
		ldstr "class G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>::Method4()
		calli default string(class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>)
		ldstr "G2_C675::Method4.11203()"
		ldstr "class G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>::Method7<object>()
		calli default string(class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>)
		ldstr "G2_C675::Method7.11209<System.Object>()"
		ldstr "class G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1219.IBase2`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>::Method7<object>()
		calli default string(class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>)
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1219.IBase1`1<class Generated1219.BaseClass0>::Method4()
		calli default string(class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>)
		ldstr "G2_C675::Method4.MI.11204()"
		ldstr "class IBase1`1<class Generated1219.BaseClass0> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1219.IBase1`1<class Generated1219.BaseClass0>::Method5()
		calli default string(class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>)
		ldstr "G2_C675::Method5.MI.11206()"
		ldstr "class IBase1`1<class Generated1219.BaseClass0> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1219.IBase1`1<class Generated1219.BaseClass0>::Method6<object>()
		calli default string(class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>)
		ldstr "G2_C675::Method6.MI.11208<System.Object>()"
		ldstr "class IBase1`1<class Generated1219.BaseClass0> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1219.IBase2`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>::Method7<object>()
		calli default string(class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>)
		ldstr "G2_C675::Method7.MI.11210<System.Object>()"
		ldstr "class IBase2`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>::ClassMethod2711<object>()
		calli default string(class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>)
		ldstr "G2_C675::ClassMethod2711.11213<System.Object>()"
		ldstr "class G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>::ClassMethod2710<object>()
		calli default string(class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>)
		ldstr "G2_C675::ClassMethod2710.11212<System.Object>()"
		ldstr "class G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>::ClassMethod2709()
		calli default string(class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>)
		ldstr "G2_C675::ClassMethod2709.11211()"
		ldstr "class G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>::Method7<object>()
		calli default string(class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>)
		ldstr "G2_C675::Method7.11209<System.Object>()"
		ldstr "class G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>::Method6<object>()
		calli default string(class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>)
		ldstr "G2_C675::Method6.11207<System.Object>()"
		ldstr "class G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>::Method5()
		calli default string(class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>)
		ldstr "G2_C675::Method5.11205()"
		ldstr "class G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>::Method4()
		calli default string(class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>)
		ldstr "G2_C675::Method4.11203()"
		ldstr "class G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>::ClassMethod1347<object>()
		calli default string(class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>)
		ldstr "G2_C675::ClassMethod1347.MI.11214<System.Object>()"
		ldstr "class G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>::ClassMethod1346<object>()
		calli default string(class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>)
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()"
		ldstr "class G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1219.IBase1`1<class Generated1219.BaseClass1>::Method4()
		calli default string(class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>)
		ldstr "G2_C675::Method4.MI.11204()"
		ldstr "class IBase1`1<class Generated1219.BaseClass1> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1219.IBase1`1<class Generated1219.BaseClass1>::Method5()
		calli default string(class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>)
		ldstr "G2_C675::Method5.MI.11206()"
		ldstr "class IBase1`1<class Generated1219.BaseClass1> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1219.IBase1`1<class Generated1219.BaseClass1>::Method6<object>()
		calli default string(class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>)
		ldstr "G2_C675::Method6.MI.11208<System.Object>()"
		ldstr "class IBase1`1<class Generated1219.BaseClass1> on type class Generated1219.G2_C675`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>::ClassMethod1347<object>()
		calli default string(class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>)
		ldstr "G1_C12::ClassMethod1347.4870<System.Object>()"
		ldstr "class G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0> on type class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>::ClassMethod1346<object>()
		calli default string(class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>)
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()"
		ldstr "class G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0> on type class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>::Method6<object>()
		calli default string(class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>)
		ldstr "G1_C12::Method6.4867<System.Object>()"
		ldstr "class G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0> on type class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>::Method5()
		calli default string(class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>)
		ldstr "G1_C12::Method5.4866()"
		ldstr "class G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0> on type class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>::Method4()
		calli default string(class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>)
		ldstr "G1_C12::Method4.4865()"
		ldstr "class G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0> on type class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>::Method7<object>()
		calli default string(class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>)
		ldstr "G1_C12::Method7.4863<System.Object>()"
		ldstr "class G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0> on type class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1219.IBase2`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>::Method7<object>()
		calli default string(class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>)
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0> on type class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1219.IBase1`1<class Generated1219.BaseClass0>::Method4()
		calli default string(class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>)
		ldstr "G1_C12::Method4.4865()"
		ldstr "class IBase1`1<class Generated1219.BaseClass0> on type class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1219.IBase1`1<class Generated1219.BaseClass0>::Method5()
		calli default string(class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>)
		ldstr "G1_C12::Method5.4866()"
		ldstr "class IBase1`1<class Generated1219.BaseClass0> on type class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1219.IBase1`1<class Generated1219.BaseClass0>::Method6<object>()
		calli default string(class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>)
		ldstr "G1_C12::Method6.MI.4868<System.Object>()"
		ldstr "class IBase1`1<class Generated1219.BaseClass0> on type class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1219.IBase2`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>::Method7<object>()
		calli default string(class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>)
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1> on type class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>::ClassMethod1347<object>()
		calli default string(class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>)
		ldstr "G1_C12::ClassMethod1347.4870<System.Object>()"
		ldstr "class G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1> on type class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>::ClassMethod1346<object>()
		calli default string(class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>)
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()"
		ldstr "class G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1> on type class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>::Method6<object>()
		calli default string(class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>)
		ldstr "G1_C12::Method6.4867<System.Object>()"
		ldstr "class G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1> on type class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>::Method5()
		calli default string(class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>)
		ldstr "G1_C12::Method5.4866()"
		ldstr "class G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1> on type class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>::Method4()
		calli default string(class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>)
		ldstr "G1_C12::Method4.4865()"
		ldstr "class G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1> on type class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>::Method7<object>()
		calli default string(class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>)
		ldstr "G1_C12::Method7.4863<System.Object>()"
		ldstr "class G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1> on type class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1219.IBase2`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>::Method7<object>()
		calli default string(class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>)
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1> on type class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1219.IBase1`1<class Generated1219.BaseClass0>::Method4()
		calli default string(class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>)
		ldstr "G1_C12::Method4.4865()"
		ldstr "class IBase1`1<class Generated1219.BaseClass0> on type class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1219.IBase1`1<class Generated1219.BaseClass0>::Method5()
		calli default string(class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>)
		ldstr "G1_C12::Method5.4866()"
		ldstr "class IBase1`1<class Generated1219.BaseClass0> on type class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1219.IBase1`1<class Generated1219.BaseClass0>::Method6<object>()
		calli default string(class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>)
		ldstr "G1_C12::Method6.MI.4868<System.Object>()"
		ldstr "class IBase1`1<class Generated1219.BaseClass0> on type class Generated1219.G1_C12`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>::ClassMethod1347<object>()
		calli default string(class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>)
		ldstr "G1_C12::ClassMethod1347.4870<System.Object>()"
		ldstr "class G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0> on type class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>::ClassMethod1346<object>()
		calli default string(class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>)
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()"
		ldstr "class G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0> on type class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>::Method6<object>()
		calli default string(class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>)
		ldstr "G1_C12::Method6.4867<System.Object>()"
		ldstr "class G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0> on type class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>::Method5()
		calli default string(class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>)
		ldstr "G1_C12::Method5.4866()"
		ldstr "class G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0> on type class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>::Method4()
		calli default string(class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>)
		ldstr "G1_C12::Method4.4865()"
		ldstr "class G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0> on type class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>::Method7<object>()
		calli default string(class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>)
		ldstr "G1_C12::Method7.4863<System.Object>()"
		ldstr "class G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0> on type class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1219.IBase2`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>::Method7<object>()
		calli default string(class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>)
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0> on type class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1219.IBase1`1<class Generated1219.BaseClass0>::Method4()
		calli default string(class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>)
		ldstr "G1_C12::Method4.4865()"
		ldstr "class IBase1`1<class Generated1219.BaseClass0> on type class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1219.IBase1`1<class Generated1219.BaseClass0>::Method5()
		calli default string(class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>)
		ldstr "G1_C12::Method5.4866()"
		ldstr "class IBase1`1<class Generated1219.BaseClass0> on type class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1219.IBase1`1<class Generated1219.BaseClass0>::Method6<object>()
		calli default string(class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>)
		ldstr "G1_C12::Method6.MI.4868<System.Object>()"
		ldstr "class IBase1`1<class Generated1219.BaseClass0> on type class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1219.IBase2`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0>::Method7<object>()
		calli default string(class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>)
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class Generated1219.BaseClass0,class Generated1219.BaseClass0> on type class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1219.IBase2`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>::Method7<object>()
		calli default string(class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>)
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1> on type class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1219.IBase2`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>::Method7<object>()
		calli default string(class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>)
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1> on type class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>::ClassMethod1347<object>()
		calli default string(class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>)
		ldstr "G1_C12::ClassMethod1347.4870<System.Object>()"
		ldstr "class G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1> on type class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>::ClassMethod1346<object>()
		calli default string(class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>)
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()"
		ldstr "class G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1> on type class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>::Method6<object>()
		calli default string(class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>)
		ldstr "G1_C12::Method6.4867<System.Object>()"
		ldstr "class G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1> on type class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>::Method5()
		calli default string(class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>)
		ldstr "G1_C12::Method5.4866()"
		ldstr "class G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1> on type class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>::Method4()
		calli default string(class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>)
		ldstr "G1_C12::Method4.4865()"
		ldstr "class G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1> on type class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>::Method7<object>()
		calli default string(class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>)
		ldstr "G1_C12::Method7.4863<System.Object>()"
		ldstr "class G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1> on type class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1219.IBase2`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>::Method7<object>()
		calli default string(class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>)
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1> on type class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1219.IBase1`1<class Generated1219.BaseClass0>::Method4()
		calli default string(class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>)
		ldstr "G1_C12::Method4.4865()"
		ldstr "class IBase1`1<class Generated1219.BaseClass0> on type class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1219.IBase1`1<class Generated1219.BaseClass0>::Method5()
		calli default string(class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>)
		ldstr "G1_C12::Method5.4866()"
		ldstr "class IBase1`1<class Generated1219.BaseClass0> on type class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1219.IBase1`1<class Generated1219.BaseClass0>::Method6<object>()
		calli default string(class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>)
		ldstr "G1_C12::Method6.MI.4868<System.Object>()"
		ldstr "class IBase1`1<class Generated1219.BaseClass0> on type class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1219.IBase2`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1>::Method7<object>()
		calli default string(class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>)
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class Generated1219.BaseClass0,class Generated1219.BaseClass1> on type class Generated1219.G1_C12`2<class Generated1219.BaseClass1,class Generated1219.BaseClass1>"
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
		call void Generated1219.Generated1219::MethodCallingTest()
		call void Generated1219.Generated1219::ConstrainedCallsTest()
		call void Generated1219.Generated1219::StructConstrainedInterfaceCallsTest()
		call void Generated1219.Generated1219::CalliTest()
		ldc.i4 100
		ret
	}
}
