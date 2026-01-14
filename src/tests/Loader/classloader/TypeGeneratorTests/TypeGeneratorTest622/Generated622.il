// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) .ver 4:0:0:0 }
.assembly extern TestFramework { .publickeytoken = ( B0 3F 5F 7F 11 D5 0A 3A )  }

//TYPES IN FORWARDER ASSEMBLIES:

//TEST ASSEMBLY:
.assembly extern xunit.core {}

.class public Generated622.BaseClass0 
{
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void [mscorlib]System.Object::.ctor()
		ret
	}
}
.class public Generated622.BaseClass1 
		extends Generated622.BaseClass0
{
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void Generated622.BaseClass0::.ctor()
		ret
	}
}
.class public Generated622.G3_C1094`1<T0> 
		extends class Generated622.G2_C78`1<class Generated622.BaseClass0>
		implements class Generated622.IBase2`2<class Generated622.BaseClass0,class Generated622.BaseClass0> 
{
	.method public hidebysig virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G3_C1094::Method7.14267<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase2<class Generated622.BaseClass0,class Generated622.BaseClass0>.Method7'<M0>() cil managed noinlining { 
		.override method instance string class Generated622.IBase2`2<class Generated622.BaseClass0,class Generated622.BaseClass0>::Method7<[1]>()
		ldstr "G3_C1094::Method7.MI.14268<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod3581<M0>() cil managed noinlining { 
		ldstr "G3_C1094::ClassMethod3581.14269<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'G2_C78<class Generated622.BaseClass0>.ClassMethod1472'<M0>() cil managed noinlining { 
		.override method instance string class Generated622.G2_C78`1<class Generated622.BaseClass0>::ClassMethod1472<[1]>()
		ldstr "G3_C1094::ClassMethod1472.MI.14270<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class Generated622.G2_C78`1<class Generated622.BaseClass0>::.ctor()
		ret
	}
}
.class public Generated622.G2_C78`1<T0> 
		extends class Generated622.G1_C6`2<!T0,class Generated622.BaseClass1>
		implements class Generated622.IBase1`1<!T0>, class Generated622.IBase2`2<!T0,class Generated622.BaseClass1> 
{
	.method public hidebysig newslot virtual instance string Method4() cil managed noinlining { 
		ldstr "G2_C78::Method4.5454()"
		ret
	}
	.method public hidebysig virtual instance string Method5() cil managed noinlining { 
		ldstr "G2_C78::Method5.5455()"
		ret
	}
	.method public hidebysig virtual instance string Method6<M0>() cil managed noinlining { 
		ldstr "G2_C78::Method6.5456<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<T0>.Method6'<M0>() cil managed noinlining { 
		.override method instance string class Generated622.IBase1`1<!T0>::Method6<[1]>()
		ldstr "G2_C78::Method6.MI.5457<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G2_C78::Method7.5458<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1471() cil managed noinlining { 
		ldstr "G2_C78::ClassMethod1471.5459()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1472<M0>() cil managed noinlining { 
		ldstr "G2_C78::ClassMethod1472.5460<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'G1_C6<T0,class Generated622.BaseClass1>.ClassMethod1326'() cil managed noinlining { 
		.override method instance string class Generated622.G1_C6`2<!T0,class Generated622.BaseClass1>::ClassMethod1326()
		ldstr "G2_C78::ClassMethod1326.MI.5461()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'G1_C6<T0,class Generated622.BaseClass1>.ClassMethod1327'<M0>() cil managed noinlining { 
		.override method instance string class Generated622.G1_C6`2<!T0,class Generated622.BaseClass1>::ClassMethod1327<[1]>()
		ldstr "G2_C78::ClassMethod1327.MI.5462<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class Generated622.G1_C6`2<!T0,class Generated622.BaseClass1>::.ctor()
		ret
	}
}
.class interface public abstract Generated622.IBase2`2<+T0, -T1> 
{
	.method public hidebysig newslot abstract virtual instance string Method7<M0>() cil managed { }
}
.class public Generated622.G1_C6`2<T0, T1> 
		implements class Generated622.IBase2`2<class Generated622.BaseClass1,!T0> 
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
.class interface public abstract Generated622.IBase1`1<+T0> 
{
	.method public hidebysig newslot abstract virtual instance string Method4() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method5() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method6<M0>() cil managed { }
}
.class public auto ansi beforefieldinit Generated622.Generated622 {
	.method static void M.BaseClass0<(Generated622.BaseClass0)W>(!!W inst, string exp) cil managed {
		.maxstack 5
		.locals init (string[] actualResults)
		ldc.i4.s    0
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.BaseClass0<(Generated622.BaseClass0)W>(!!W inst, string exp)"
		ldc.i4.s 0
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.BaseClass1<(Generated622.BaseClass1)W>(!!W inst, string exp) cil managed {
		.maxstack 5
		.locals init (string[] actualResults)
		ldc.i4.s    0
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.BaseClass1<(Generated622.BaseClass1)W>(!!W inst, string exp)"
		ldc.i4.s 0
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1094.T<T0,(class Generated622.G3_C1094`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 14
		.locals init (string[] actualResults)
		ldc.i4.s    9
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1094.T<T0,(class Generated622.G3_C1094`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 9
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated622.G3_C1094`1<!!T0>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated622.G3_C1094`1<!!T0>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated622.G3_C1094`1<!!T0>::ClassMethod1471()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated622.G3_C1094`1<!!T0>::ClassMethod1472<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated622.G3_C1094`1<!!T0>::ClassMethod3581<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated622.G3_C1094`1<!!T0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated622.G3_C1094`1<!!T0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated622.G3_C1094`1<!!T0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated622.G3_C1094`1<!!T0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1094.A<(class Generated622.G3_C1094`1<class Generated622.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 14
		.locals init (string[] actualResults)
		ldc.i4.s    9
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1094.A<(class Generated622.G3_C1094`1<class Generated622.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 9
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated622.G3_C1094`1<class Generated622.BaseClass0>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated622.G3_C1094`1<class Generated622.BaseClass0>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated622.G3_C1094`1<class Generated622.BaseClass0>::ClassMethod1471()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated622.G3_C1094`1<class Generated622.BaseClass0>::ClassMethod1472<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated622.G3_C1094`1<class Generated622.BaseClass0>::ClassMethod3581<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated622.G3_C1094`1<class Generated622.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated622.G3_C1094`1<class Generated622.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated622.G3_C1094`1<class Generated622.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated622.G3_C1094`1<class Generated622.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1094.B<(class Generated622.G3_C1094`1<class Generated622.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 14
		.locals init (string[] actualResults)
		ldc.i4.s    9
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1094.B<(class Generated622.G3_C1094`1<class Generated622.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 9
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated622.G3_C1094`1<class Generated622.BaseClass1>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated622.G3_C1094`1<class Generated622.BaseClass1>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated622.G3_C1094`1<class Generated622.BaseClass1>::ClassMethod1471()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated622.G3_C1094`1<class Generated622.BaseClass1>::ClassMethod1472<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated622.G3_C1094`1<class Generated622.BaseClass1>::ClassMethod3581<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated622.G3_C1094`1<class Generated622.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated622.G3_C1094`1<class Generated622.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated622.G3_C1094`1<class Generated622.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated622.G3_C1094`1<class Generated622.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C78.T<T0,(class Generated622.G2_C78`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C78.T<T0,(class Generated622.G2_C78`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated622.G2_C78`1<!!T0>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated622.G2_C78`1<!!T0>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated622.G2_C78`1<!!T0>::ClassMethod1471()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated622.G2_C78`1<!!T0>::ClassMethod1472<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated622.G2_C78`1<!!T0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated622.G2_C78`1<!!T0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated622.G2_C78`1<!!T0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated622.G2_C78`1<!!T0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C78.A<(class Generated622.G2_C78`1<class Generated622.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C78.A<(class Generated622.G2_C78`1<class Generated622.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated622.G2_C78`1<class Generated622.BaseClass0>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated622.G2_C78`1<class Generated622.BaseClass0>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated622.G2_C78`1<class Generated622.BaseClass0>::ClassMethod1471()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated622.G2_C78`1<class Generated622.BaseClass0>::ClassMethod1472<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated622.G2_C78`1<class Generated622.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated622.G2_C78`1<class Generated622.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated622.G2_C78`1<class Generated622.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated622.G2_C78`1<class Generated622.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C78.B<(class Generated622.G2_C78`1<class Generated622.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C78.B<(class Generated622.G2_C78`1<class Generated622.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated622.G2_C78`1<class Generated622.BaseClass1>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated622.G2_C78`1<class Generated622.BaseClass1>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated622.G2_C78`1<class Generated622.BaseClass1>::ClassMethod1471()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated622.G2_C78`1<class Generated622.BaseClass1>::ClassMethod1472<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated622.G2_C78`1<class Generated622.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated622.G2_C78`1<class Generated622.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated622.G2_C78`1<class Generated622.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated622.G2_C78`1<class Generated622.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.T.T<T0,T1,(class Generated622.IBase2`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.T.T<T0,T1,(class Generated622.IBase2`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated622.IBase2`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.A.T<T1,(class Generated622.IBase2`2<class Generated622.BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.A.T<T1,(class Generated622.IBase2`2<class Generated622.BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated622.IBase2`2<class Generated622.BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.A.A<(class Generated622.IBase2`2<class Generated622.BaseClass0,class Generated622.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.A.A<(class Generated622.IBase2`2<class Generated622.BaseClass0,class Generated622.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated622.IBase2`2<class Generated622.BaseClass0,class Generated622.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.A.B<(class Generated622.IBase2`2<class Generated622.BaseClass0,class Generated622.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.A.B<(class Generated622.IBase2`2<class Generated622.BaseClass0,class Generated622.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated622.IBase2`2<class Generated622.BaseClass0,class Generated622.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.B.T<T1,(class Generated622.IBase2`2<class Generated622.BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.B.T<T1,(class Generated622.IBase2`2<class Generated622.BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated622.IBase2`2<class Generated622.BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.B.A<(class Generated622.IBase2`2<class Generated622.BaseClass1,class Generated622.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.B.A<(class Generated622.IBase2`2<class Generated622.BaseClass1,class Generated622.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated622.IBase2`2<class Generated622.BaseClass1,class Generated622.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.B.B<(class Generated622.IBase2`2<class Generated622.BaseClass1,class Generated622.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.B.B<(class Generated622.IBase2`2<class Generated622.BaseClass1,class Generated622.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated622.IBase2`2<class Generated622.BaseClass1,class Generated622.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C6.T.T<T0,T1,(class Generated622.G1_C6`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C6.T.T<T0,T1,(class Generated622.G1_C6`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated622.G1_C6`2<!!T0,!!T1>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated622.G1_C6`2<!!T0,!!T1>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated622.G1_C6`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C6.A.T<T1,(class Generated622.G1_C6`2<class Generated622.BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C6.A.T<T1,(class Generated622.G1_C6`2<class Generated622.BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated622.G1_C6`2<class Generated622.BaseClass0,!!T1>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated622.G1_C6`2<class Generated622.BaseClass0,!!T1>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated622.G1_C6`2<class Generated622.BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C6.A.A<(class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C6.A.A<(class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass0>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass0>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C6.A.B<(class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C6.A.B<(class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass1>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass1>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C6.B.T<T1,(class Generated622.G1_C6`2<class Generated622.BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C6.B.T<T1,(class Generated622.G1_C6`2<class Generated622.BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated622.G1_C6`2<class Generated622.BaseClass1,!!T1>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated622.G1_C6`2<class Generated622.BaseClass1,!!T1>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated622.G1_C6`2<class Generated622.BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C6.B.A<(class Generated622.G1_C6`2<class Generated622.BaseClass1,class Generated622.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C6.B.A<(class Generated622.G1_C6`2<class Generated622.BaseClass1,class Generated622.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated622.G1_C6`2<class Generated622.BaseClass1,class Generated622.BaseClass0>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated622.G1_C6`2<class Generated622.BaseClass1,class Generated622.BaseClass0>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated622.G1_C6`2<class Generated622.BaseClass1,class Generated622.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C6.B.B<(class Generated622.G1_C6`2<class Generated622.BaseClass1,class Generated622.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C6.B.B<(class Generated622.G1_C6`2<class Generated622.BaseClass1,class Generated622.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated622.G1_C6`2<class Generated622.BaseClass1,class Generated622.BaseClass1>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated622.G1_C6`2<class Generated622.BaseClass1,class Generated622.BaseClass1>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated622.G1_C6`2<class Generated622.BaseClass1,class Generated622.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase1.T<T0,(class Generated622.IBase1`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase1.T<T0,(class Generated622.IBase1`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated622.IBase1`1<!!T0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated622.IBase1`1<!!T0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated622.IBase1`1<!!T0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase1.A<(class Generated622.IBase1`1<class Generated622.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase1.A<(class Generated622.IBase1`1<class Generated622.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated622.IBase1`1<class Generated622.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated622.IBase1`1<class Generated622.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated622.IBase1`1<class Generated622.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase1.B<(class Generated622.IBase1`1<class Generated622.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase1.B<(class Generated622.IBase1`1<class Generated622.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated622.IBase1`1<class Generated622.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated622.IBase1`1<class Generated622.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated622.IBase1`1<class Generated622.BaseClass1>::Method6<object>()
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
		newobj instance void class Generated622.G3_C1094`1<class Generated622.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass1>
		callvirt instance string class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass1>::ClassMethod1327<object>()
		ldstr "G2_C78::ClassMethod1327.MI.5462<System.Object>()"
		ldstr "class G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass1> on type class Generated622.G3_C1094`1<class Generated622.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass1>
		callvirt instance string class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass1>::ClassMethod1326()
		ldstr "G2_C78::ClassMethod1326.MI.5461()"
		ldstr "class G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass1> on type class Generated622.G3_C1094`1<class Generated622.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass1>
		callvirt instance string class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass1>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass1> on type class Generated622.G3_C1094`1<class Generated622.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated622.IBase2`2<class Generated622.BaseClass1,class Generated622.BaseClass0>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated622.BaseClass1,class Generated622.BaseClass0> on type class Generated622.G3_C1094`1<class Generated622.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated622.IBase2`2<class Generated622.BaseClass0,class Generated622.BaseClass0>::Method7<object>()
		ldstr "G3_C1094::Method7.MI.14268<System.Object>()"
		ldstr "class IBase2`2<class Generated622.BaseClass0,class Generated622.BaseClass0> on type class Generated622.G3_C1094`1<class Generated622.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated622.IBase2`2<class Generated622.BaseClass0,class Generated622.BaseClass1>::Method7<object>()
		ldstr "G3_C1094::Method7.MI.14268<System.Object>()"
		ldstr "class IBase2`2<class Generated622.BaseClass0,class Generated622.BaseClass1> on type class Generated622.G3_C1094`1<class Generated622.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated622.IBase2`2<class Generated622.BaseClass1,class Generated622.BaseClass1>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated622.BaseClass1,class Generated622.BaseClass1> on type class Generated622.G3_C1094`1<class Generated622.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated622.G2_C78`1<class Generated622.BaseClass0>
		callvirt instance string class Generated622.G2_C78`1<class Generated622.BaseClass0>::ClassMethod1472<object>()
		ldstr "G3_C1094::ClassMethod1472.MI.14270<System.Object>()"
		ldstr "class G2_C78`1<class Generated622.BaseClass0> on type class Generated622.G3_C1094`1<class Generated622.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated622.G2_C78`1<class Generated622.BaseClass0>
		callvirt instance string class Generated622.G2_C78`1<class Generated622.BaseClass0>::ClassMethod1471()
		ldstr "G2_C78::ClassMethod1471.5459()"
		ldstr "class G2_C78`1<class Generated622.BaseClass0> on type class Generated622.G3_C1094`1<class Generated622.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated622.G2_C78`1<class Generated622.BaseClass0>
		callvirt instance string class Generated622.G2_C78`1<class Generated622.BaseClass0>::Method7<object>()
		ldstr "G3_C1094::Method7.14267<System.Object>()"
		ldstr "class G2_C78`1<class Generated622.BaseClass0> on type class Generated622.G3_C1094`1<class Generated622.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated622.G2_C78`1<class Generated622.BaseClass0>
		callvirt instance string class Generated622.G2_C78`1<class Generated622.BaseClass0>::Method6<object>()
		ldstr "G2_C78::Method6.5456<System.Object>()"
		ldstr "class G2_C78`1<class Generated622.BaseClass0> on type class Generated622.G3_C1094`1<class Generated622.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated622.G2_C78`1<class Generated622.BaseClass0>
		callvirt instance string class Generated622.G2_C78`1<class Generated622.BaseClass0>::Method5()
		ldstr "G2_C78::Method5.5455()"
		ldstr "class G2_C78`1<class Generated622.BaseClass0> on type class Generated622.G3_C1094`1<class Generated622.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated622.G2_C78`1<class Generated622.BaseClass0>
		callvirt instance string class Generated622.G2_C78`1<class Generated622.BaseClass0>::Method4()
		ldstr "G2_C78::Method4.5454()"
		ldstr "class G2_C78`1<class Generated622.BaseClass0> on type class Generated622.G3_C1094`1<class Generated622.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated622.G2_C78`1<class Generated622.BaseClass0>
		callvirt instance string class Generated622.G2_C78`1<class Generated622.BaseClass0>::ClassMethod1327<object>()
		ldstr "G2_C78::ClassMethod1327.MI.5462<System.Object>()"
		ldstr "class G2_C78`1<class Generated622.BaseClass0> on type class Generated622.G3_C1094`1<class Generated622.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated622.G2_C78`1<class Generated622.BaseClass0>
		callvirt instance string class Generated622.G2_C78`1<class Generated622.BaseClass0>::ClassMethod1326()
		ldstr "G2_C78::ClassMethod1326.MI.5461()"
		ldstr "class G2_C78`1<class Generated622.BaseClass0> on type class Generated622.G3_C1094`1<class Generated622.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated622.IBase1`1<class Generated622.BaseClass0>::Method4()
		ldstr "G2_C78::Method4.5454()"
		ldstr "class IBase1`1<class Generated622.BaseClass0> on type class Generated622.G3_C1094`1<class Generated622.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated622.IBase1`1<class Generated622.BaseClass0>::Method5()
		ldstr "G2_C78::Method5.5455()"
		ldstr "class IBase1`1<class Generated622.BaseClass0> on type class Generated622.G3_C1094`1<class Generated622.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated622.IBase1`1<class Generated622.BaseClass0>::Method6<object>()
		ldstr "G2_C78::Method6.MI.5457<System.Object>()"
		ldstr "class IBase1`1<class Generated622.BaseClass0> on type class Generated622.G3_C1094`1<class Generated622.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated622.G3_C1094`1<class Generated622.BaseClass0>
		callvirt instance string class Generated622.G3_C1094`1<class Generated622.BaseClass0>::ClassMethod3581<object>()
		ldstr "G3_C1094::ClassMethod3581.14269<System.Object>()"
		ldstr "class G3_C1094`1<class Generated622.BaseClass0> on type class Generated622.G3_C1094`1<class Generated622.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated622.G3_C1094`1<class Generated622.BaseClass0>
		callvirt instance string class Generated622.G3_C1094`1<class Generated622.BaseClass0>::Method7<object>()
		ldstr "G3_C1094::Method7.14267<System.Object>()"
		ldstr "class G3_C1094`1<class Generated622.BaseClass0> on type class Generated622.G3_C1094`1<class Generated622.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated622.G3_C1094`1<class Generated622.BaseClass0>
		callvirt instance string class Generated622.G3_C1094`1<class Generated622.BaseClass0>::ClassMethod1472<object>()
		ldstr "G3_C1094::ClassMethod1472.MI.14270<System.Object>()"
		ldstr "class G3_C1094`1<class Generated622.BaseClass0> on type class Generated622.G3_C1094`1<class Generated622.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated622.G3_C1094`1<class Generated622.BaseClass0>
		callvirt instance string class Generated622.G3_C1094`1<class Generated622.BaseClass0>::ClassMethod1471()
		ldstr "G2_C78::ClassMethod1471.5459()"
		ldstr "class G3_C1094`1<class Generated622.BaseClass0> on type class Generated622.G3_C1094`1<class Generated622.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated622.G3_C1094`1<class Generated622.BaseClass0>
		callvirt instance string class Generated622.G3_C1094`1<class Generated622.BaseClass0>::Method6<object>()
		ldstr "G2_C78::Method6.5456<System.Object>()"
		ldstr "class G3_C1094`1<class Generated622.BaseClass0> on type class Generated622.G3_C1094`1<class Generated622.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated622.G3_C1094`1<class Generated622.BaseClass0>
		callvirt instance string class Generated622.G3_C1094`1<class Generated622.BaseClass0>::Method5()
		ldstr "G2_C78::Method5.5455()"
		ldstr "class G3_C1094`1<class Generated622.BaseClass0> on type class Generated622.G3_C1094`1<class Generated622.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated622.G3_C1094`1<class Generated622.BaseClass0>
		callvirt instance string class Generated622.G3_C1094`1<class Generated622.BaseClass0>::Method4()
		ldstr "G2_C78::Method4.5454()"
		ldstr "class G3_C1094`1<class Generated622.BaseClass0> on type class Generated622.G3_C1094`1<class Generated622.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated622.G3_C1094`1<class Generated622.BaseClass0>
		callvirt instance string class Generated622.G3_C1094`1<class Generated622.BaseClass0>::ClassMethod1327<object>()
		ldstr "G2_C78::ClassMethod1327.MI.5462<System.Object>()"
		ldstr "class G3_C1094`1<class Generated622.BaseClass0> on type class Generated622.G3_C1094`1<class Generated622.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated622.G3_C1094`1<class Generated622.BaseClass0>
		callvirt instance string class Generated622.G3_C1094`1<class Generated622.BaseClass0>::ClassMethod1326()
		ldstr "G2_C78::ClassMethod1326.MI.5461()"
		ldstr "class G3_C1094`1<class Generated622.BaseClass0> on type class Generated622.G3_C1094`1<class Generated622.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated622.G3_C1094`1<class Generated622.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass1>
		callvirt instance string class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass1>::ClassMethod1327<object>()
		ldstr "G2_C78::ClassMethod1327.MI.5462<System.Object>()"
		ldstr "class G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass1> on type class Generated622.G3_C1094`1<class Generated622.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass1>
		callvirt instance string class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass1>::ClassMethod1326()
		ldstr "G2_C78::ClassMethod1326.MI.5461()"
		ldstr "class G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass1> on type class Generated622.G3_C1094`1<class Generated622.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass1>
		callvirt instance string class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass1>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass1> on type class Generated622.G3_C1094`1<class Generated622.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated622.IBase2`2<class Generated622.BaseClass1,class Generated622.BaseClass0>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated622.BaseClass1,class Generated622.BaseClass0> on type class Generated622.G3_C1094`1<class Generated622.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated622.IBase2`2<class Generated622.BaseClass0,class Generated622.BaseClass0>::Method7<object>()
		ldstr "G3_C1094::Method7.MI.14268<System.Object>()"
		ldstr "class IBase2`2<class Generated622.BaseClass0,class Generated622.BaseClass0> on type class Generated622.G3_C1094`1<class Generated622.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated622.IBase2`2<class Generated622.BaseClass0,class Generated622.BaseClass1>::Method7<object>()
		ldstr "G3_C1094::Method7.MI.14268<System.Object>()"
		ldstr "class IBase2`2<class Generated622.BaseClass0,class Generated622.BaseClass1> on type class Generated622.G3_C1094`1<class Generated622.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated622.IBase2`2<class Generated622.BaseClass1,class Generated622.BaseClass1>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated622.BaseClass1,class Generated622.BaseClass1> on type class Generated622.G3_C1094`1<class Generated622.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated622.G2_C78`1<class Generated622.BaseClass0>
		callvirt instance string class Generated622.G2_C78`1<class Generated622.BaseClass0>::ClassMethod1472<object>()
		ldstr "G3_C1094::ClassMethod1472.MI.14270<System.Object>()"
		ldstr "class G2_C78`1<class Generated622.BaseClass0> on type class Generated622.G3_C1094`1<class Generated622.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated622.G2_C78`1<class Generated622.BaseClass0>
		callvirt instance string class Generated622.G2_C78`1<class Generated622.BaseClass0>::ClassMethod1471()
		ldstr "G2_C78::ClassMethod1471.5459()"
		ldstr "class G2_C78`1<class Generated622.BaseClass0> on type class Generated622.G3_C1094`1<class Generated622.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated622.G2_C78`1<class Generated622.BaseClass0>
		callvirt instance string class Generated622.G2_C78`1<class Generated622.BaseClass0>::Method7<object>()
		ldstr "G3_C1094::Method7.14267<System.Object>()"
		ldstr "class G2_C78`1<class Generated622.BaseClass0> on type class Generated622.G3_C1094`1<class Generated622.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated622.G2_C78`1<class Generated622.BaseClass0>
		callvirt instance string class Generated622.G2_C78`1<class Generated622.BaseClass0>::Method6<object>()
		ldstr "G2_C78::Method6.5456<System.Object>()"
		ldstr "class G2_C78`1<class Generated622.BaseClass0> on type class Generated622.G3_C1094`1<class Generated622.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated622.G2_C78`1<class Generated622.BaseClass0>
		callvirt instance string class Generated622.G2_C78`1<class Generated622.BaseClass0>::Method5()
		ldstr "G2_C78::Method5.5455()"
		ldstr "class G2_C78`1<class Generated622.BaseClass0> on type class Generated622.G3_C1094`1<class Generated622.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated622.G2_C78`1<class Generated622.BaseClass0>
		callvirt instance string class Generated622.G2_C78`1<class Generated622.BaseClass0>::Method4()
		ldstr "G2_C78::Method4.5454()"
		ldstr "class G2_C78`1<class Generated622.BaseClass0> on type class Generated622.G3_C1094`1<class Generated622.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated622.G2_C78`1<class Generated622.BaseClass0>
		callvirt instance string class Generated622.G2_C78`1<class Generated622.BaseClass0>::ClassMethod1327<object>()
		ldstr "G2_C78::ClassMethod1327.MI.5462<System.Object>()"
		ldstr "class G2_C78`1<class Generated622.BaseClass0> on type class Generated622.G3_C1094`1<class Generated622.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated622.G2_C78`1<class Generated622.BaseClass0>
		callvirt instance string class Generated622.G2_C78`1<class Generated622.BaseClass0>::ClassMethod1326()
		ldstr "G2_C78::ClassMethod1326.MI.5461()"
		ldstr "class G2_C78`1<class Generated622.BaseClass0> on type class Generated622.G3_C1094`1<class Generated622.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated622.IBase1`1<class Generated622.BaseClass0>::Method4()
		ldstr "G2_C78::Method4.5454()"
		ldstr "class IBase1`1<class Generated622.BaseClass0> on type class Generated622.G3_C1094`1<class Generated622.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated622.IBase1`1<class Generated622.BaseClass0>::Method5()
		ldstr "G2_C78::Method5.5455()"
		ldstr "class IBase1`1<class Generated622.BaseClass0> on type class Generated622.G3_C1094`1<class Generated622.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated622.IBase1`1<class Generated622.BaseClass0>::Method6<object>()
		ldstr "G2_C78::Method6.MI.5457<System.Object>()"
		ldstr "class IBase1`1<class Generated622.BaseClass0> on type class Generated622.G3_C1094`1<class Generated622.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated622.G3_C1094`1<class Generated622.BaseClass1>
		callvirt instance string class Generated622.G3_C1094`1<class Generated622.BaseClass1>::ClassMethod3581<object>()
		ldstr "G3_C1094::ClassMethod3581.14269<System.Object>()"
		ldstr "class G3_C1094`1<class Generated622.BaseClass1> on type class Generated622.G3_C1094`1<class Generated622.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated622.G3_C1094`1<class Generated622.BaseClass1>
		callvirt instance string class Generated622.G3_C1094`1<class Generated622.BaseClass1>::Method7<object>()
		ldstr "G3_C1094::Method7.14267<System.Object>()"
		ldstr "class G3_C1094`1<class Generated622.BaseClass1> on type class Generated622.G3_C1094`1<class Generated622.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated622.G3_C1094`1<class Generated622.BaseClass1>
		callvirt instance string class Generated622.G3_C1094`1<class Generated622.BaseClass1>::ClassMethod1472<object>()
		ldstr "G3_C1094::ClassMethod1472.MI.14270<System.Object>()"
		ldstr "class G3_C1094`1<class Generated622.BaseClass1> on type class Generated622.G3_C1094`1<class Generated622.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated622.G3_C1094`1<class Generated622.BaseClass1>
		callvirt instance string class Generated622.G3_C1094`1<class Generated622.BaseClass1>::ClassMethod1471()
		ldstr "G2_C78::ClassMethod1471.5459()"
		ldstr "class G3_C1094`1<class Generated622.BaseClass1> on type class Generated622.G3_C1094`1<class Generated622.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated622.G3_C1094`1<class Generated622.BaseClass1>
		callvirt instance string class Generated622.G3_C1094`1<class Generated622.BaseClass1>::Method6<object>()
		ldstr "G2_C78::Method6.5456<System.Object>()"
		ldstr "class G3_C1094`1<class Generated622.BaseClass1> on type class Generated622.G3_C1094`1<class Generated622.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated622.G3_C1094`1<class Generated622.BaseClass1>
		callvirt instance string class Generated622.G3_C1094`1<class Generated622.BaseClass1>::Method5()
		ldstr "G2_C78::Method5.5455()"
		ldstr "class G3_C1094`1<class Generated622.BaseClass1> on type class Generated622.G3_C1094`1<class Generated622.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated622.G3_C1094`1<class Generated622.BaseClass1>
		callvirt instance string class Generated622.G3_C1094`1<class Generated622.BaseClass1>::Method4()
		ldstr "G2_C78::Method4.5454()"
		ldstr "class G3_C1094`1<class Generated622.BaseClass1> on type class Generated622.G3_C1094`1<class Generated622.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated622.G3_C1094`1<class Generated622.BaseClass1>
		callvirt instance string class Generated622.G3_C1094`1<class Generated622.BaseClass1>::ClassMethod1327<object>()
		ldstr "G2_C78::ClassMethod1327.MI.5462<System.Object>()"
		ldstr "class G3_C1094`1<class Generated622.BaseClass1> on type class Generated622.G3_C1094`1<class Generated622.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated622.G3_C1094`1<class Generated622.BaseClass1>
		callvirt instance string class Generated622.G3_C1094`1<class Generated622.BaseClass1>::ClassMethod1326()
		ldstr "G2_C78::ClassMethod1326.MI.5461()"
		ldstr "class G3_C1094`1<class Generated622.BaseClass1> on type class Generated622.G3_C1094`1<class Generated622.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated622.G2_C78`1<class Generated622.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass1>
		callvirt instance string class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass1>::ClassMethod1327<object>()
		ldstr "G2_C78::ClassMethod1327.MI.5462<System.Object>()"
		ldstr "class G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass1> on type class Generated622.G2_C78`1<class Generated622.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass1>
		callvirt instance string class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass1>::ClassMethod1326()
		ldstr "G2_C78::ClassMethod1326.MI.5461()"
		ldstr "class G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass1> on type class Generated622.G2_C78`1<class Generated622.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass1>
		callvirt instance string class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass1>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass1> on type class Generated622.G2_C78`1<class Generated622.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated622.IBase2`2<class Generated622.BaseClass1,class Generated622.BaseClass0>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated622.BaseClass1,class Generated622.BaseClass0> on type class Generated622.G2_C78`1<class Generated622.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated622.IBase2`2<class Generated622.BaseClass0,class Generated622.BaseClass0>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated622.BaseClass0,class Generated622.BaseClass0> on type class Generated622.G2_C78`1<class Generated622.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated622.IBase2`2<class Generated622.BaseClass0,class Generated622.BaseClass1>::Method7<object>()
		ldstr "G2_C78::Method7.5458<System.Object>()"
		ldstr "class IBase2`2<class Generated622.BaseClass0,class Generated622.BaseClass1> on type class Generated622.G2_C78`1<class Generated622.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated622.IBase2`2<class Generated622.BaseClass1,class Generated622.BaseClass1>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated622.BaseClass1,class Generated622.BaseClass1> on type class Generated622.G2_C78`1<class Generated622.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated622.G2_C78`1<class Generated622.BaseClass0>
		callvirt instance string class Generated622.G2_C78`1<class Generated622.BaseClass0>::ClassMethod1472<object>()
		ldstr "G2_C78::ClassMethod1472.5460<System.Object>()"
		ldstr "class G2_C78`1<class Generated622.BaseClass0> on type class Generated622.G2_C78`1<class Generated622.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated622.G2_C78`1<class Generated622.BaseClass0>
		callvirt instance string class Generated622.G2_C78`1<class Generated622.BaseClass0>::ClassMethod1471()
		ldstr "G2_C78::ClassMethod1471.5459()"
		ldstr "class G2_C78`1<class Generated622.BaseClass0> on type class Generated622.G2_C78`1<class Generated622.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated622.G2_C78`1<class Generated622.BaseClass0>
		callvirt instance string class Generated622.G2_C78`1<class Generated622.BaseClass0>::Method7<object>()
		ldstr "G2_C78::Method7.5458<System.Object>()"
		ldstr "class G2_C78`1<class Generated622.BaseClass0> on type class Generated622.G2_C78`1<class Generated622.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated622.G2_C78`1<class Generated622.BaseClass0>
		callvirt instance string class Generated622.G2_C78`1<class Generated622.BaseClass0>::Method6<object>()
		ldstr "G2_C78::Method6.5456<System.Object>()"
		ldstr "class G2_C78`1<class Generated622.BaseClass0> on type class Generated622.G2_C78`1<class Generated622.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated622.G2_C78`1<class Generated622.BaseClass0>
		callvirt instance string class Generated622.G2_C78`1<class Generated622.BaseClass0>::Method5()
		ldstr "G2_C78::Method5.5455()"
		ldstr "class G2_C78`1<class Generated622.BaseClass0> on type class Generated622.G2_C78`1<class Generated622.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated622.G2_C78`1<class Generated622.BaseClass0>
		callvirt instance string class Generated622.G2_C78`1<class Generated622.BaseClass0>::Method4()
		ldstr "G2_C78::Method4.5454()"
		ldstr "class G2_C78`1<class Generated622.BaseClass0> on type class Generated622.G2_C78`1<class Generated622.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated622.G2_C78`1<class Generated622.BaseClass0>
		callvirt instance string class Generated622.G2_C78`1<class Generated622.BaseClass0>::ClassMethod1327<object>()
		ldstr "G2_C78::ClassMethod1327.MI.5462<System.Object>()"
		ldstr "class G2_C78`1<class Generated622.BaseClass0> on type class Generated622.G2_C78`1<class Generated622.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated622.G2_C78`1<class Generated622.BaseClass0>
		callvirt instance string class Generated622.G2_C78`1<class Generated622.BaseClass0>::ClassMethod1326()
		ldstr "G2_C78::ClassMethod1326.MI.5461()"
		ldstr "class G2_C78`1<class Generated622.BaseClass0> on type class Generated622.G2_C78`1<class Generated622.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated622.IBase1`1<class Generated622.BaseClass0>::Method4()
		ldstr "G2_C78::Method4.5454()"
		ldstr "class IBase1`1<class Generated622.BaseClass0> on type class Generated622.G2_C78`1<class Generated622.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated622.IBase1`1<class Generated622.BaseClass0>::Method5()
		ldstr "G2_C78::Method5.5455()"
		ldstr "class IBase1`1<class Generated622.BaseClass0> on type class Generated622.G2_C78`1<class Generated622.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated622.IBase1`1<class Generated622.BaseClass0>::Method6<object>()
		ldstr "G2_C78::Method6.MI.5457<System.Object>()"
		ldstr "class IBase1`1<class Generated622.BaseClass0> on type class Generated622.G2_C78`1<class Generated622.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated622.G2_C78`1<class Generated622.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated622.G1_C6`2<class Generated622.BaseClass1,class Generated622.BaseClass1>
		callvirt instance string class Generated622.G1_C6`2<class Generated622.BaseClass1,class Generated622.BaseClass1>::ClassMethod1327<object>()
		ldstr "G2_C78::ClassMethod1327.MI.5462<System.Object>()"
		ldstr "class G1_C6`2<class Generated622.BaseClass1,class Generated622.BaseClass1> on type class Generated622.G2_C78`1<class Generated622.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated622.G1_C6`2<class Generated622.BaseClass1,class Generated622.BaseClass1>
		callvirt instance string class Generated622.G1_C6`2<class Generated622.BaseClass1,class Generated622.BaseClass1>::ClassMethod1326()
		ldstr "G2_C78::ClassMethod1326.MI.5461()"
		ldstr "class G1_C6`2<class Generated622.BaseClass1,class Generated622.BaseClass1> on type class Generated622.G2_C78`1<class Generated622.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated622.G1_C6`2<class Generated622.BaseClass1,class Generated622.BaseClass1>
		callvirt instance string class Generated622.G1_C6`2<class Generated622.BaseClass1,class Generated622.BaseClass1>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class G1_C6`2<class Generated622.BaseClass1,class Generated622.BaseClass1> on type class Generated622.G2_C78`1<class Generated622.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated622.IBase2`2<class Generated622.BaseClass1,class Generated622.BaseClass1>::Method7<object>()
		ldstr "G2_C78::Method7.5458<System.Object>()"
		ldstr "class IBase2`2<class Generated622.BaseClass1,class Generated622.BaseClass1> on type class Generated622.G2_C78`1<class Generated622.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated622.IBase2`2<class Generated622.BaseClass0,class Generated622.BaseClass1>::Method7<object>()
		ldstr "G2_C78::Method7.5458<System.Object>()"
		ldstr "class IBase2`2<class Generated622.BaseClass0,class Generated622.BaseClass1> on type class Generated622.G2_C78`1<class Generated622.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated622.G2_C78`1<class Generated622.BaseClass1>
		callvirt instance string class Generated622.G2_C78`1<class Generated622.BaseClass1>::ClassMethod1472<object>()
		ldstr "G2_C78::ClassMethod1472.5460<System.Object>()"
		ldstr "class G2_C78`1<class Generated622.BaseClass1> on type class Generated622.G2_C78`1<class Generated622.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated622.G2_C78`1<class Generated622.BaseClass1>
		callvirt instance string class Generated622.G2_C78`1<class Generated622.BaseClass1>::ClassMethod1471()
		ldstr "G2_C78::ClassMethod1471.5459()"
		ldstr "class G2_C78`1<class Generated622.BaseClass1> on type class Generated622.G2_C78`1<class Generated622.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated622.G2_C78`1<class Generated622.BaseClass1>
		callvirt instance string class Generated622.G2_C78`1<class Generated622.BaseClass1>::Method7<object>()
		ldstr "G2_C78::Method7.5458<System.Object>()"
		ldstr "class G2_C78`1<class Generated622.BaseClass1> on type class Generated622.G2_C78`1<class Generated622.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated622.G2_C78`1<class Generated622.BaseClass1>
		callvirt instance string class Generated622.G2_C78`1<class Generated622.BaseClass1>::Method6<object>()
		ldstr "G2_C78::Method6.5456<System.Object>()"
		ldstr "class G2_C78`1<class Generated622.BaseClass1> on type class Generated622.G2_C78`1<class Generated622.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated622.G2_C78`1<class Generated622.BaseClass1>
		callvirt instance string class Generated622.G2_C78`1<class Generated622.BaseClass1>::Method5()
		ldstr "G2_C78::Method5.5455()"
		ldstr "class G2_C78`1<class Generated622.BaseClass1> on type class Generated622.G2_C78`1<class Generated622.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated622.G2_C78`1<class Generated622.BaseClass1>
		callvirt instance string class Generated622.G2_C78`1<class Generated622.BaseClass1>::Method4()
		ldstr "G2_C78::Method4.5454()"
		ldstr "class G2_C78`1<class Generated622.BaseClass1> on type class Generated622.G2_C78`1<class Generated622.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated622.G2_C78`1<class Generated622.BaseClass1>
		callvirt instance string class Generated622.G2_C78`1<class Generated622.BaseClass1>::ClassMethod1327<object>()
		ldstr "G2_C78::ClassMethod1327.MI.5462<System.Object>()"
		ldstr "class G2_C78`1<class Generated622.BaseClass1> on type class Generated622.G2_C78`1<class Generated622.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated622.G2_C78`1<class Generated622.BaseClass1>
		callvirt instance string class Generated622.G2_C78`1<class Generated622.BaseClass1>::ClassMethod1326()
		ldstr "G2_C78::ClassMethod1326.MI.5461()"
		ldstr "class G2_C78`1<class Generated622.BaseClass1> on type class Generated622.G2_C78`1<class Generated622.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated622.IBase1`1<class Generated622.BaseClass1>::Method4()
		ldstr "G2_C78::Method4.5454()"
		ldstr "class IBase1`1<class Generated622.BaseClass1> on type class Generated622.G2_C78`1<class Generated622.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated622.IBase1`1<class Generated622.BaseClass1>::Method5()
		ldstr "G2_C78::Method5.5455()"
		ldstr "class IBase1`1<class Generated622.BaseClass1> on type class Generated622.G2_C78`1<class Generated622.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated622.IBase1`1<class Generated622.BaseClass1>::Method6<object>()
		ldstr "G2_C78::Method6.MI.5457<System.Object>()"
		ldstr "class IBase1`1<class Generated622.BaseClass1> on type class Generated622.G2_C78`1<class Generated622.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated622.IBase1`1<class Generated622.BaseClass0>::Method4()
		ldstr "G2_C78::Method4.5454()"
		ldstr "class IBase1`1<class Generated622.BaseClass0> on type class Generated622.G2_C78`1<class Generated622.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated622.IBase1`1<class Generated622.BaseClass0>::Method5()
		ldstr "G2_C78::Method5.5455()"
		ldstr "class IBase1`1<class Generated622.BaseClass0> on type class Generated622.G2_C78`1<class Generated622.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated622.IBase1`1<class Generated622.BaseClass0>::Method6<object>()
		ldstr "G2_C78::Method6.MI.5457<System.Object>()"
		ldstr "class IBase1`1<class Generated622.BaseClass0> on type class Generated622.G2_C78`1<class Generated622.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass0>
		callvirt instance string class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass0>::ClassMethod1327<object>()
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass0> on type class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass0>
		callvirt instance string class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass0>::ClassMethod1326()
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass0> on type class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass0>
		callvirt instance string class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass0>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass0> on type class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated622.IBase2`2<class Generated622.BaseClass1,class Generated622.BaseClass0>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated622.BaseClass1,class Generated622.BaseClass0> on type class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated622.IBase2`2<class Generated622.BaseClass0,class Generated622.BaseClass0>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated622.BaseClass0,class Generated622.BaseClass0> on type class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated622.IBase2`2<class Generated622.BaseClass0,class Generated622.BaseClass1>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated622.BaseClass0,class Generated622.BaseClass1> on type class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated622.IBase2`2<class Generated622.BaseClass1,class Generated622.BaseClass1>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated622.BaseClass1,class Generated622.BaseClass1> on type class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass1>
		callvirt instance string class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass1>::ClassMethod1327<object>()
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass1> on type class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass1>
		callvirt instance string class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass1>::ClassMethod1326()
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass1> on type class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass1>
		callvirt instance string class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass1>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass1> on type class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated622.IBase2`2<class Generated622.BaseClass1,class Generated622.BaseClass0>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated622.BaseClass1,class Generated622.BaseClass0> on type class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated622.IBase2`2<class Generated622.BaseClass0,class Generated622.BaseClass0>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated622.BaseClass0,class Generated622.BaseClass0> on type class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated622.IBase2`2<class Generated622.BaseClass0,class Generated622.BaseClass1>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated622.BaseClass0,class Generated622.BaseClass1> on type class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated622.IBase2`2<class Generated622.BaseClass1,class Generated622.BaseClass1>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated622.BaseClass1,class Generated622.BaseClass1> on type class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated622.G1_C6`2<class Generated622.BaseClass1,class Generated622.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated622.G1_C6`2<class Generated622.BaseClass1,class Generated622.BaseClass0>
		callvirt instance string class Generated622.G1_C6`2<class Generated622.BaseClass1,class Generated622.BaseClass0>::ClassMethod1327<object>()
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G1_C6`2<class Generated622.BaseClass1,class Generated622.BaseClass0> on type class Generated622.G1_C6`2<class Generated622.BaseClass1,class Generated622.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated622.G1_C6`2<class Generated622.BaseClass1,class Generated622.BaseClass0>
		callvirt instance string class Generated622.G1_C6`2<class Generated622.BaseClass1,class Generated622.BaseClass0>::ClassMethod1326()
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G1_C6`2<class Generated622.BaseClass1,class Generated622.BaseClass0> on type class Generated622.G1_C6`2<class Generated622.BaseClass1,class Generated622.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated622.G1_C6`2<class Generated622.BaseClass1,class Generated622.BaseClass0>
		callvirt instance string class Generated622.G1_C6`2<class Generated622.BaseClass1,class Generated622.BaseClass0>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class G1_C6`2<class Generated622.BaseClass1,class Generated622.BaseClass0> on type class Generated622.G1_C6`2<class Generated622.BaseClass1,class Generated622.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated622.IBase2`2<class Generated622.BaseClass1,class Generated622.BaseClass1>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated622.BaseClass1,class Generated622.BaseClass1> on type class Generated622.G1_C6`2<class Generated622.BaseClass1,class Generated622.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated622.IBase2`2<class Generated622.BaseClass0,class Generated622.BaseClass1>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated622.BaseClass0,class Generated622.BaseClass1> on type class Generated622.G1_C6`2<class Generated622.BaseClass1,class Generated622.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated622.G1_C6`2<class Generated622.BaseClass1,class Generated622.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated622.G1_C6`2<class Generated622.BaseClass1,class Generated622.BaseClass1>
		callvirt instance string class Generated622.G1_C6`2<class Generated622.BaseClass1,class Generated622.BaseClass1>::ClassMethod1327<object>()
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G1_C6`2<class Generated622.BaseClass1,class Generated622.BaseClass1> on type class Generated622.G1_C6`2<class Generated622.BaseClass1,class Generated622.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated622.G1_C6`2<class Generated622.BaseClass1,class Generated622.BaseClass1>
		callvirt instance string class Generated622.G1_C6`2<class Generated622.BaseClass1,class Generated622.BaseClass1>::ClassMethod1326()
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G1_C6`2<class Generated622.BaseClass1,class Generated622.BaseClass1> on type class Generated622.G1_C6`2<class Generated622.BaseClass1,class Generated622.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated622.G1_C6`2<class Generated622.BaseClass1,class Generated622.BaseClass1>
		callvirt instance string class Generated622.G1_C6`2<class Generated622.BaseClass1,class Generated622.BaseClass1>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class G1_C6`2<class Generated622.BaseClass1,class Generated622.BaseClass1> on type class Generated622.G1_C6`2<class Generated622.BaseClass1,class Generated622.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated622.IBase2`2<class Generated622.BaseClass1,class Generated622.BaseClass1>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated622.BaseClass1,class Generated622.BaseClass1> on type class Generated622.G1_C6`2<class Generated622.BaseClass1,class Generated622.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated622.IBase2`2<class Generated622.BaseClass0,class Generated622.BaseClass1>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated622.BaseClass0,class Generated622.BaseClass1> on type class Generated622.G1_C6`2<class Generated622.BaseClass1,class Generated622.BaseClass1>"
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
		newobj instance void class Generated622.G3_C1094`1<class Generated622.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C78::ClassMethod1326.MI.5461()#G2_C78::ClassMethod1327.MI.5462<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated622.Generated622::M.G1_C6.T.T<class Generated622.BaseClass0,class Generated622.BaseClass1,class Generated622.G3_C1094`1<class Generated622.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C78::ClassMethod1326.MI.5461()#G2_C78::ClassMethod1327.MI.5462<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated622.Generated622::M.G1_C6.A.T<class Generated622.BaseClass1,class Generated622.G3_C1094`1<class Generated622.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C78::ClassMethod1326.MI.5461()#G2_C78::ClassMethod1327.MI.5462<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated622.Generated622::M.G1_C6.A.B<class Generated622.G3_C1094`1<class Generated622.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated622.Generated622::M.IBase2.T.T<class Generated622.BaseClass1,class Generated622.BaseClass0,class Generated622.G3_C1094`1<class Generated622.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated622.Generated622::M.IBase2.B.T<class Generated622.BaseClass0,class Generated622.G3_C1094`1<class Generated622.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated622.Generated622::M.IBase2.B.A<class Generated622.G3_C1094`1<class Generated622.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1094::Method7.MI.14268<System.Object>()#"
		call void Generated622.Generated622::M.IBase2.T.T<class Generated622.BaseClass0,class Generated622.BaseClass0,class Generated622.G3_C1094`1<class Generated622.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G3_C1094::Method7.MI.14268<System.Object>()#"
		call void Generated622.Generated622::M.IBase2.A.T<class Generated622.BaseClass0,class Generated622.G3_C1094`1<class Generated622.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G3_C1094::Method7.MI.14268<System.Object>()#"
		call void Generated622.Generated622::M.IBase2.A.A<class Generated622.G3_C1094`1<class Generated622.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1094::Method7.MI.14268<System.Object>()#"
		call void Generated622.Generated622::M.IBase2.T.T<class Generated622.BaseClass0,class Generated622.BaseClass1,class Generated622.G3_C1094`1<class Generated622.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G3_C1094::Method7.MI.14268<System.Object>()#"
		call void Generated622.Generated622::M.IBase2.A.T<class Generated622.BaseClass1,class Generated622.G3_C1094`1<class Generated622.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G3_C1094::Method7.MI.14268<System.Object>()#"
		call void Generated622.Generated622::M.IBase2.A.B<class Generated622.G3_C1094`1<class Generated622.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated622.Generated622::M.IBase2.T.T<class Generated622.BaseClass1,class Generated622.BaseClass1,class Generated622.G3_C1094`1<class Generated622.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated622.Generated622::M.IBase2.B.T<class Generated622.BaseClass1,class Generated622.G3_C1094`1<class Generated622.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated622.Generated622::M.IBase2.B.B<class Generated622.G3_C1094`1<class Generated622.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C78::ClassMethod1326.MI.5461()#G2_C78::ClassMethod1327.MI.5462<System.Object>()#G2_C78::ClassMethod1471.5459()#G3_C1094::ClassMethod1472.MI.14270<System.Object>()#G2_C78::Method4.5454()#G2_C78::Method5.5455()#G2_C78::Method6.5456<System.Object>()#G3_C1094::Method7.14267<System.Object>()#"
		call void Generated622.Generated622::M.G2_C78.T<class Generated622.BaseClass0,class Generated622.G3_C1094`1<class Generated622.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C78::ClassMethod1326.MI.5461()#G2_C78::ClassMethod1327.MI.5462<System.Object>()#G2_C78::ClassMethod1471.5459()#G3_C1094::ClassMethod1472.MI.14270<System.Object>()#G2_C78::Method4.5454()#G2_C78::Method5.5455()#G2_C78::Method6.5456<System.Object>()#G3_C1094::Method7.14267<System.Object>()#"
		call void Generated622.Generated622::M.G2_C78.A<class Generated622.G3_C1094`1<class Generated622.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C78::Method4.5454()#G2_C78::Method5.5455()#G2_C78::Method6.MI.5457<System.Object>()#"
		call void Generated622.Generated622::M.IBase1.T<class Generated622.BaseClass0,class Generated622.G3_C1094`1<class Generated622.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C78::Method4.5454()#G2_C78::Method5.5455()#G2_C78::Method6.MI.5457<System.Object>()#"
		call void Generated622.Generated622::M.IBase1.A<class Generated622.G3_C1094`1<class Generated622.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C78::ClassMethod1326.MI.5461()#G2_C78::ClassMethod1327.MI.5462<System.Object>()#G2_C78::ClassMethod1471.5459()#G3_C1094::ClassMethod1472.MI.14270<System.Object>()#G3_C1094::ClassMethod3581.14269<System.Object>()#G2_C78::Method4.5454()#G2_C78::Method5.5455()#G2_C78::Method6.5456<System.Object>()#G3_C1094::Method7.14267<System.Object>()#"
		call void Generated622.Generated622::M.G3_C1094.T<class Generated622.BaseClass0,class Generated622.G3_C1094`1<class Generated622.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C78::ClassMethod1326.MI.5461()#G2_C78::ClassMethod1327.MI.5462<System.Object>()#G2_C78::ClassMethod1471.5459()#G3_C1094::ClassMethod1472.MI.14270<System.Object>()#G3_C1094::ClassMethod3581.14269<System.Object>()#G2_C78::Method4.5454()#G2_C78::Method5.5455()#G2_C78::Method6.5456<System.Object>()#G3_C1094::Method7.14267<System.Object>()#"
		call void Generated622.Generated622::M.G3_C1094.A<class Generated622.G3_C1094`1<class Generated622.BaseClass0>>(!!0,string)
		newobj instance void class Generated622.G3_C1094`1<class Generated622.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C78::ClassMethod1326.MI.5461()#G2_C78::ClassMethod1327.MI.5462<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated622.Generated622::M.G1_C6.T.T<class Generated622.BaseClass0,class Generated622.BaseClass1,class Generated622.G3_C1094`1<class Generated622.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C78::ClassMethod1326.MI.5461()#G2_C78::ClassMethod1327.MI.5462<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated622.Generated622::M.G1_C6.A.T<class Generated622.BaseClass1,class Generated622.G3_C1094`1<class Generated622.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C78::ClassMethod1326.MI.5461()#G2_C78::ClassMethod1327.MI.5462<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated622.Generated622::M.G1_C6.A.B<class Generated622.G3_C1094`1<class Generated622.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated622.Generated622::M.IBase2.T.T<class Generated622.BaseClass1,class Generated622.BaseClass0,class Generated622.G3_C1094`1<class Generated622.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated622.Generated622::M.IBase2.B.T<class Generated622.BaseClass0,class Generated622.G3_C1094`1<class Generated622.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated622.Generated622::M.IBase2.B.A<class Generated622.G3_C1094`1<class Generated622.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1094::Method7.MI.14268<System.Object>()#"
		call void Generated622.Generated622::M.IBase2.T.T<class Generated622.BaseClass0,class Generated622.BaseClass0,class Generated622.G3_C1094`1<class Generated622.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G3_C1094::Method7.MI.14268<System.Object>()#"
		call void Generated622.Generated622::M.IBase2.A.T<class Generated622.BaseClass0,class Generated622.G3_C1094`1<class Generated622.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1094::Method7.MI.14268<System.Object>()#"
		call void Generated622.Generated622::M.IBase2.A.A<class Generated622.G3_C1094`1<class Generated622.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1094::Method7.MI.14268<System.Object>()#"
		call void Generated622.Generated622::M.IBase2.T.T<class Generated622.BaseClass0,class Generated622.BaseClass1,class Generated622.G3_C1094`1<class Generated622.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G3_C1094::Method7.MI.14268<System.Object>()#"
		call void Generated622.Generated622::M.IBase2.A.T<class Generated622.BaseClass1,class Generated622.G3_C1094`1<class Generated622.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1094::Method7.MI.14268<System.Object>()#"
		call void Generated622.Generated622::M.IBase2.A.B<class Generated622.G3_C1094`1<class Generated622.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated622.Generated622::M.IBase2.T.T<class Generated622.BaseClass1,class Generated622.BaseClass1,class Generated622.G3_C1094`1<class Generated622.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated622.Generated622::M.IBase2.B.T<class Generated622.BaseClass1,class Generated622.G3_C1094`1<class Generated622.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated622.Generated622::M.IBase2.B.B<class Generated622.G3_C1094`1<class Generated622.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C78::ClassMethod1326.MI.5461()#G2_C78::ClassMethod1327.MI.5462<System.Object>()#G2_C78::ClassMethod1471.5459()#G3_C1094::ClassMethod1472.MI.14270<System.Object>()#G2_C78::Method4.5454()#G2_C78::Method5.5455()#G2_C78::Method6.5456<System.Object>()#G3_C1094::Method7.14267<System.Object>()#"
		call void Generated622.Generated622::M.G2_C78.T<class Generated622.BaseClass0,class Generated622.G3_C1094`1<class Generated622.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C78::ClassMethod1326.MI.5461()#G2_C78::ClassMethod1327.MI.5462<System.Object>()#G2_C78::ClassMethod1471.5459()#G3_C1094::ClassMethod1472.MI.14270<System.Object>()#G2_C78::Method4.5454()#G2_C78::Method5.5455()#G2_C78::Method6.5456<System.Object>()#G3_C1094::Method7.14267<System.Object>()#"
		call void Generated622.Generated622::M.G2_C78.A<class Generated622.G3_C1094`1<class Generated622.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C78::Method4.5454()#G2_C78::Method5.5455()#G2_C78::Method6.MI.5457<System.Object>()#"
		call void Generated622.Generated622::M.IBase1.T<class Generated622.BaseClass0,class Generated622.G3_C1094`1<class Generated622.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C78::Method4.5454()#G2_C78::Method5.5455()#G2_C78::Method6.MI.5457<System.Object>()#"
		call void Generated622.Generated622::M.IBase1.A<class Generated622.G3_C1094`1<class Generated622.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C78::ClassMethod1326.MI.5461()#G2_C78::ClassMethod1327.MI.5462<System.Object>()#G2_C78::ClassMethod1471.5459()#G3_C1094::ClassMethod1472.MI.14270<System.Object>()#G3_C1094::ClassMethod3581.14269<System.Object>()#G2_C78::Method4.5454()#G2_C78::Method5.5455()#G2_C78::Method6.5456<System.Object>()#G3_C1094::Method7.14267<System.Object>()#"
		call void Generated622.Generated622::M.G3_C1094.T<class Generated622.BaseClass1,class Generated622.G3_C1094`1<class Generated622.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C78::ClassMethod1326.MI.5461()#G2_C78::ClassMethod1327.MI.5462<System.Object>()#G2_C78::ClassMethod1471.5459()#G3_C1094::ClassMethod1472.MI.14270<System.Object>()#G3_C1094::ClassMethod3581.14269<System.Object>()#G2_C78::Method4.5454()#G2_C78::Method5.5455()#G2_C78::Method6.5456<System.Object>()#G3_C1094::Method7.14267<System.Object>()#"
		call void Generated622.Generated622::M.G3_C1094.B<class Generated622.G3_C1094`1<class Generated622.BaseClass1>>(!!0,string)
		newobj instance void class Generated622.G2_C78`1<class Generated622.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C78::ClassMethod1326.MI.5461()#G2_C78::ClassMethod1327.MI.5462<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated622.Generated622::M.G1_C6.T.T<class Generated622.BaseClass0,class Generated622.BaseClass1,class Generated622.G2_C78`1<class Generated622.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C78::ClassMethod1326.MI.5461()#G2_C78::ClassMethod1327.MI.5462<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated622.Generated622::M.G1_C6.A.T<class Generated622.BaseClass1,class Generated622.G2_C78`1<class Generated622.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C78::ClassMethod1326.MI.5461()#G2_C78::ClassMethod1327.MI.5462<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated622.Generated622::M.G1_C6.A.B<class Generated622.G2_C78`1<class Generated622.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated622.Generated622::M.IBase2.T.T<class Generated622.BaseClass1,class Generated622.BaseClass0,class Generated622.G2_C78`1<class Generated622.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated622.Generated622::M.IBase2.B.T<class Generated622.BaseClass0,class Generated622.G2_C78`1<class Generated622.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated622.Generated622::M.IBase2.B.A<class Generated622.G2_C78`1<class Generated622.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated622.Generated622::M.IBase2.T.T<class Generated622.BaseClass0,class Generated622.BaseClass0,class Generated622.G2_C78`1<class Generated622.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated622.Generated622::M.IBase2.A.T<class Generated622.BaseClass0,class Generated622.G2_C78`1<class Generated622.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated622.Generated622::M.IBase2.A.A<class Generated622.G2_C78`1<class Generated622.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C78::Method7.5458<System.Object>()#"
		call void Generated622.Generated622::M.IBase2.T.T<class Generated622.BaseClass0,class Generated622.BaseClass1,class Generated622.G2_C78`1<class Generated622.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C78::Method7.5458<System.Object>()#"
		call void Generated622.Generated622::M.IBase2.A.T<class Generated622.BaseClass1,class Generated622.G2_C78`1<class Generated622.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C78::Method7.5458<System.Object>()#"
		call void Generated622.Generated622::M.IBase2.A.B<class Generated622.G2_C78`1<class Generated622.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated622.Generated622::M.IBase2.T.T<class Generated622.BaseClass1,class Generated622.BaseClass1,class Generated622.G2_C78`1<class Generated622.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated622.Generated622::M.IBase2.B.T<class Generated622.BaseClass1,class Generated622.G2_C78`1<class Generated622.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated622.Generated622::M.IBase2.B.B<class Generated622.G2_C78`1<class Generated622.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C78::ClassMethod1326.MI.5461()#G2_C78::ClassMethod1327.MI.5462<System.Object>()#G2_C78::ClassMethod1471.5459()#G2_C78::ClassMethod1472.5460<System.Object>()#G2_C78::Method4.5454()#G2_C78::Method5.5455()#G2_C78::Method6.5456<System.Object>()#G2_C78::Method7.5458<System.Object>()#"
		call void Generated622.Generated622::M.G2_C78.T<class Generated622.BaseClass0,class Generated622.G2_C78`1<class Generated622.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C78::ClassMethod1326.MI.5461()#G2_C78::ClassMethod1327.MI.5462<System.Object>()#G2_C78::ClassMethod1471.5459()#G2_C78::ClassMethod1472.5460<System.Object>()#G2_C78::Method4.5454()#G2_C78::Method5.5455()#G2_C78::Method6.5456<System.Object>()#G2_C78::Method7.5458<System.Object>()#"
		call void Generated622.Generated622::M.G2_C78.A<class Generated622.G2_C78`1<class Generated622.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C78::Method4.5454()#G2_C78::Method5.5455()#G2_C78::Method6.MI.5457<System.Object>()#"
		call void Generated622.Generated622::M.IBase1.T<class Generated622.BaseClass0,class Generated622.G2_C78`1<class Generated622.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C78::Method4.5454()#G2_C78::Method5.5455()#G2_C78::Method6.MI.5457<System.Object>()#"
		call void Generated622.Generated622::M.IBase1.A<class Generated622.G2_C78`1<class Generated622.BaseClass0>>(!!0,string)
		newobj instance void class Generated622.G2_C78`1<class Generated622.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C78::ClassMethod1326.MI.5461()#G2_C78::ClassMethod1327.MI.5462<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated622.Generated622::M.G1_C6.T.T<class Generated622.BaseClass1,class Generated622.BaseClass1,class Generated622.G2_C78`1<class Generated622.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C78::ClassMethod1326.MI.5461()#G2_C78::ClassMethod1327.MI.5462<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated622.Generated622::M.G1_C6.B.T<class Generated622.BaseClass1,class Generated622.G2_C78`1<class Generated622.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C78::ClassMethod1326.MI.5461()#G2_C78::ClassMethod1327.MI.5462<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated622.Generated622::M.G1_C6.B.B<class Generated622.G2_C78`1<class Generated622.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C78::Method7.5458<System.Object>()#"
		call void Generated622.Generated622::M.IBase2.T.T<class Generated622.BaseClass1,class Generated622.BaseClass1,class Generated622.G2_C78`1<class Generated622.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C78::Method7.5458<System.Object>()#"
		call void Generated622.Generated622::M.IBase2.B.T<class Generated622.BaseClass1,class Generated622.G2_C78`1<class Generated622.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C78::Method7.5458<System.Object>()#"
		call void Generated622.Generated622::M.IBase2.B.B<class Generated622.G2_C78`1<class Generated622.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C78::Method7.5458<System.Object>()#"
		call void Generated622.Generated622::M.IBase2.T.T<class Generated622.BaseClass0,class Generated622.BaseClass1,class Generated622.G2_C78`1<class Generated622.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C78::Method7.5458<System.Object>()#"
		call void Generated622.Generated622::M.IBase2.A.T<class Generated622.BaseClass1,class Generated622.G2_C78`1<class Generated622.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C78::Method7.5458<System.Object>()#"
		call void Generated622.Generated622::M.IBase2.A.B<class Generated622.G2_C78`1<class Generated622.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C78::ClassMethod1326.MI.5461()#G2_C78::ClassMethod1327.MI.5462<System.Object>()#G2_C78::ClassMethod1471.5459()#G2_C78::ClassMethod1472.5460<System.Object>()#G2_C78::Method4.5454()#G2_C78::Method5.5455()#G2_C78::Method6.5456<System.Object>()#G2_C78::Method7.5458<System.Object>()#"
		call void Generated622.Generated622::M.G2_C78.T<class Generated622.BaseClass1,class Generated622.G2_C78`1<class Generated622.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C78::ClassMethod1326.MI.5461()#G2_C78::ClassMethod1327.MI.5462<System.Object>()#G2_C78::ClassMethod1471.5459()#G2_C78::ClassMethod1472.5460<System.Object>()#G2_C78::Method4.5454()#G2_C78::Method5.5455()#G2_C78::Method6.5456<System.Object>()#G2_C78::Method7.5458<System.Object>()#"
		call void Generated622.Generated622::M.G2_C78.B<class Generated622.G2_C78`1<class Generated622.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C78::Method4.5454()#G2_C78::Method5.5455()#G2_C78::Method6.MI.5457<System.Object>()#"
		call void Generated622.Generated622::M.IBase1.T<class Generated622.BaseClass1,class Generated622.G2_C78`1<class Generated622.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C78::Method4.5454()#G2_C78::Method5.5455()#G2_C78::Method6.MI.5457<System.Object>()#"
		call void Generated622.Generated622::M.IBase1.B<class Generated622.G2_C78`1<class Generated622.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C78::Method4.5454()#G2_C78::Method5.5455()#G2_C78::Method6.MI.5457<System.Object>()#"
		call void Generated622.Generated622::M.IBase1.T<class Generated622.BaseClass0,class Generated622.G2_C78`1<class Generated622.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C78::Method4.5454()#G2_C78::Method5.5455()#G2_C78::Method6.MI.5457<System.Object>()#"
		call void Generated622.Generated622::M.IBase1.A<class Generated622.G2_C78`1<class Generated622.BaseClass1>>(!!0,string)
		newobj instance void class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated622.Generated622::M.G1_C6.T.T<class Generated622.BaseClass0,class Generated622.BaseClass0,class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated622.Generated622::M.G1_C6.A.T<class Generated622.BaseClass0,class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated622.Generated622::M.G1_C6.A.A<class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated622.Generated622::M.IBase2.T.T<class Generated622.BaseClass1,class Generated622.BaseClass0,class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated622.Generated622::M.IBase2.B.T<class Generated622.BaseClass0,class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated622.Generated622::M.IBase2.B.A<class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated622.Generated622::M.IBase2.T.T<class Generated622.BaseClass0,class Generated622.BaseClass0,class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated622.Generated622::M.IBase2.A.T<class Generated622.BaseClass0,class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated622.Generated622::M.IBase2.A.A<class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated622.Generated622::M.IBase2.T.T<class Generated622.BaseClass0,class Generated622.BaseClass1,class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated622.Generated622::M.IBase2.A.T<class Generated622.BaseClass1,class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated622.Generated622::M.IBase2.A.B<class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated622.Generated622::M.IBase2.T.T<class Generated622.BaseClass1,class Generated622.BaseClass1,class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated622.Generated622::M.IBase2.B.T<class Generated622.BaseClass1,class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated622.Generated622::M.IBase2.B.B<class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass0>>(!!0,string)
		newobj instance void class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated622.Generated622::M.G1_C6.T.T<class Generated622.BaseClass0,class Generated622.BaseClass1,class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated622.Generated622::M.G1_C6.A.T<class Generated622.BaseClass1,class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated622.Generated622::M.G1_C6.A.B<class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated622.Generated622::M.IBase2.T.T<class Generated622.BaseClass1,class Generated622.BaseClass0,class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated622.Generated622::M.IBase2.B.T<class Generated622.BaseClass0,class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated622.Generated622::M.IBase2.B.A<class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated622.Generated622::M.IBase2.T.T<class Generated622.BaseClass0,class Generated622.BaseClass0,class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated622.Generated622::M.IBase2.A.T<class Generated622.BaseClass0,class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated622.Generated622::M.IBase2.A.A<class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated622.Generated622::M.IBase2.T.T<class Generated622.BaseClass0,class Generated622.BaseClass1,class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated622.Generated622::M.IBase2.A.T<class Generated622.BaseClass1,class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated622.Generated622::M.IBase2.A.B<class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated622.Generated622::M.IBase2.T.T<class Generated622.BaseClass1,class Generated622.BaseClass1,class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated622.Generated622::M.IBase2.B.T<class Generated622.BaseClass1,class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated622.Generated622::M.IBase2.B.B<class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass1>>(!!0,string)
		newobj instance void class Generated622.G1_C6`2<class Generated622.BaseClass1,class Generated622.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated622.Generated622::M.G1_C6.T.T<class Generated622.BaseClass1,class Generated622.BaseClass0,class Generated622.G1_C6`2<class Generated622.BaseClass1,class Generated622.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated622.Generated622::M.G1_C6.B.T<class Generated622.BaseClass0,class Generated622.G1_C6`2<class Generated622.BaseClass1,class Generated622.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated622.Generated622::M.G1_C6.B.A<class Generated622.G1_C6`2<class Generated622.BaseClass1,class Generated622.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated622.Generated622::M.IBase2.T.T<class Generated622.BaseClass1,class Generated622.BaseClass1,class Generated622.G1_C6`2<class Generated622.BaseClass1,class Generated622.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated622.Generated622::M.IBase2.B.T<class Generated622.BaseClass1,class Generated622.G1_C6`2<class Generated622.BaseClass1,class Generated622.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated622.Generated622::M.IBase2.B.B<class Generated622.G1_C6`2<class Generated622.BaseClass1,class Generated622.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated622.Generated622::M.IBase2.T.T<class Generated622.BaseClass0,class Generated622.BaseClass1,class Generated622.G1_C6`2<class Generated622.BaseClass1,class Generated622.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated622.Generated622::M.IBase2.A.T<class Generated622.BaseClass1,class Generated622.G1_C6`2<class Generated622.BaseClass1,class Generated622.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated622.Generated622::M.IBase2.A.B<class Generated622.G1_C6`2<class Generated622.BaseClass1,class Generated622.BaseClass0>>(!!0,string)
		newobj instance void class Generated622.G1_C6`2<class Generated622.BaseClass1,class Generated622.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated622.Generated622::M.G1_C6.T.T<class Generated622.BaseClass1,class Generated622.BaseClass1,class Generated622.G1_C6`2<class Generated622.BaseClass1,class Generated622.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated622.Generated622::M.G1_C6.B.T<class Generated622.BaseClass1,class Generated622.G1_C6`2<class Generated622.BaseClass1,class Generated622.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated622.Generated622::M.G1_C6.B.B<class Generated622.G1_C6`2<class Generated622.BaseClass1,class Generated622.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated622.Generated622::M.IBase2.T.T<class Generated622.BaseClass1,class Generated622.BaseClass1,class Generated622.G1_C6`2<class Generated622.BaseClass1,class Generated622.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated622.Generated622::M.IBase2.B.T<class Generated622.BaseClass1,class Generated622.G1_C6`2<class Generated622.BaseClass1,class Generated622.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated622.Generated622::M.IBase2.B.B<class Generated622.G1_C6`2<class Generated622.BaseClass1,class Generated622.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated622.Generated622::M.IBase2.T.T<class Generated622.BaseClass0,class Generated622.BaseClass1,class Generated622.G1_C6`2<class Generated622.BaseClass1,class Generated622.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated622.Generated622::M.IBase2.A.T<class Generated622.BaseClass1,class Generated622.G1_C6`2<class Generated622.BaseClass1,class Generated622.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated622.Generated622::M.IBase2.A.B<class Generated622.G1_C6`2<class Generated622.BaseClass1,class Generated622.BaseClass1>>(!!0,string)
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
		newobj instance void class Generated622.G3_C1094`1<class Generated622.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass1>::ClassMethod1327<object>()
		calli default string(class Generated622.G3_C1094`1<class Generated622.BaseClass0>)
		ldstr "G2_C78::ClassMethod1327.MI.5462<System.Object>()"
		ldstr "class G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass1> on type class Generated622.G3_C1094`1<class Generated622.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass1>::ClassMethod1326()
		calli default string(class Generated622.G3_C1094`1<class Generated622.BaseClass0>)
		ldstr "G2_C78::ClassMethod1326.MI.5461()"
		ldstr "class G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass1> on type class Generated622.G3_C1094`1<class Generated622.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass1>::Method7<object>()
		calli default string(class Generated622.G3_C1094`1<class Generated622.BaseClass0>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass1> on type class Generated622.G3_C1094`1<class Generated622.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated622.IBase2`2<class Generated622.BaseClass1,class Generated622.BaseClass0>::Method7<object>()
		calli default string(class Generated622.G3_C1094`1<class Generated622.BaseClass0>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated622.BaseClass1,class Generated622.BaseClass0> on type class Generated622.G3_C1094`1<class Generated622.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated622.IBase2`2<class Generated622.BaseClass0,class Generated622.BaseClass0>::Method7<object>()
		calli default string(class Generated622.G3_C1094`1<class Generated622.BaseClass0>)
		ldstr "G3_C1094::Method7.MI.14268<System.Object>()"
		ldstr "class IBase2`2<class Generated622.BaseClass0,class Generated622.BaseClass0> on type class Generated622.G3_C1094`1<class Generated622.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated622.IBase2`2<class Generated622.BaseClass0,class Generated622.BaseClass1>::Method7<object>()
		calli default string(class Generated622.G3_C1094`1<class Generated622.BaseClass0>)
		ldstr "G3_C1094::Method7.MI.14268<System.Object>()"
		ldstr "class IBase2`2<class Generated622.BaseClass0,class Generated622.BaseClass1> on type class Generated622.G3_C1094`1<class Generated622.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated622.IBase2`2<class Generated622.BaseClass1,class Generated622.BaseClass1>::Method7<object>()
		calli default string(class Generated622.G3_C1094`1<class Generated622.BaseClass0>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated622.BaseClass1,class Generated622.BaseClass1> on type class Generated622.G3_C1094`1<class Generated622.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated622.G2_C78`1<class Generated622.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated622.G2_C78`1<class Generated622.BaseClass0>::ClassMethod1472<object>()
		calli default string(class Generated622.G3_C1094`1<class Generated622.BaseClass0>)
		ldstr "G3_C1094::ClassMethod1472.MI.14270<System.Object>()"
		ldstr "class G2_C78`1<class Generated622.BaseClass0> on type class Generated622.G3_C1094`1<class Generated622.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated622.G2_C78`1<class Generated622.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated622.G2_C78`1<class Generated622.BaseClass0>::ClassMethod1471()
		calli default string(class Generated622.G3_C1094`1<class Generated622.BaseClass0>)
		ldstr "G2_C78::ClassMethod1471.5459()"
		ldstr "class G2_C78`1<class Generated622.BaseClass0> on type class Generated622.G3_C1094`1<class Generated622.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated622.G2_C78`1<class Generated622.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated622.G2_C78`1<class Generated622.BaseClass0>::Method7<object>()
		calli default string(class Generated622.G3_C1094`1<class Generated622.BaseClass0>)
		ldstr "G3_C1094::Method7.14267<System.Object>()"
		ldstr "class G2_C78`1<class Generated622.BaseClass0> on type class Generated622.G3_C1094`1<class Generated622.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated622.G2_C78`1<class Generated622.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated622.G2_C78`1<class Generated622.BaseClass0>::Method6<object>()
		calli default string(class Generated622.G3_C1094`1<class Generated622.BaseClass0>)
		ldstr "G2_C78::Method6.5456<System.Object>()"
		ldstr "class G2_C78`1<class Generated622.BaseClass0> on type class Generated622.G3_C1094`1<class Generated622.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated622.G2_C78`1<class Generated622.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated622.G2_C78`1<class Generated622.BaseClass0>::Method5()
		calli default string(class Generated622.G3_C1094`1<class Generated622.BaseClass0>)
		ldstr "G2_C78::Method5.5455()"
		ldstr "class G2_C78`1<class Generated622.BaseClass0> on type class Generated622.G3_C1094`1<class Generated622.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated622.G2_C78`1<class Generated622.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated622.G2_C78`1<class Generated622.BaseClass0>::Method4()
		calli default string(class Generated622.G3_C1094`1<class Generated622.BaseClass0>)
		ldstr "G2_C78::Method4.5454()"
		ldstr "class G2_C78`1<class Generated622.BaseClass0> on type class Generated622.G3_C1094`1<class Generated622.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated622.G2_C78`1<class Generated622.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated622.G2_C78`1<class Generated622.BaseClass0>::ClassMethod1327<object>()
		calli default string(class Generated622.G3_C1094`1<class Generated622.BaseClass0>)
		ldstr "G2_C78::ClassMethod1327.MI.5462<System.Object>()"
		ldstr "class G2_C78`1<class Generated622.BaseClass0> on type class Generated622.G3_C1094`1<class Generated622.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated622.G2_C78`1<class Generated622.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated622.G2_C78`1<class Generated622.BaseClass0>::ClassMethod1326()
		calli default string(class Generated622.G3_C1094`1<class Generated622.BaseClass0>)
		ldstr "G2_C78::ClassMethod1326.MI.5461()"
		ldstr "class G2_C78`1<class Generated622.BaseClass0> on type class Generated622.G3_C1094`1<class Generated622.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated622.IBase1`1<class Generated622.BaseClass0>::Method4()
		calli default string(class Generated622.G3_C1094`1<class Generated622.BaseClass0>)
		ldstr "G2_C78::Method4.5454()"
		ldstr "class IBase1`1<class Generated622.BaseClass0> on type class Generated622.G3_C1094`1<class Generated622.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated622.IBase1`1<class Generated622.BaseClass0>::Method5()
		calli default string(class Generated622.G3_C1094`1<class Generated622.BaseClass0>)
		ldstr "G2_C78::Method5.5455()"
		ldstr "class IBase1`1<class Generated622.BaseClass0> on type class Generated622.G3_C1094`1<class Generated622.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated622.IBase1`1<class Generated622.BaseClass0>::Method6<object>()
		calli default string(class Generated622.G3_C1094`1<class Generated622.BaseClass0>)
		ldstr "G2_C78::Method6.MI.5457<System.Object>()"
		ldstr "class IBase1`1<class Generated622.BaseClass0> on type class Generated622.G3_C1094`1<class Generated622.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated622.G3_C1094`1<class Generated622.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated622.G3_C1094`1<class Generated622.BaseClass0>::ClassMethod3581<object>()
		calli default string(class Generated622.G3_C1094`1<class Generated622.BaseClass0>)
		ldstr "G3_C1094::ClassMethod3581.14269<System.Object>()"
		ldstr "class G3_C1094`1<class Generated622.BaseClass0> on type class Generated622.G3_C1094`1<class Generated622.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated622.G3_C1094`1<class Generated622.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated622.G3_C1094`1<class Generated622.BaseClass0>::Method7<object>()
		calli default string(class Generated622.G3_C1094`1<class Generated622.BaseClass0>)
		ldstr "G3_C1094::Method7.14267<System.Object>()"
		ldstr "class G3_C1094`1<class Generated622.BaseClass0> on type class Generated622.G3_C1094`1<class Generated622.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated622.G3_C1094`1<class Generated622.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated622.G3_C1094`1<class Generated622.BaseClass0>::ClassMethod1472<object>()
		calli default string(class Generated622.G3_C1094`1<class Generated622.BaseClass0>)
		ldstr "G3_C1094::ClassMethod1472.MI.14270<System.Object>()"
		ldstr "class G3_C1094`1<class Generated622.BaseClass0> on type class Generated622.G3_C1094`1<class Generated622.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated622.G3_C1094`1<class Generated622.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated622.G3_C1094`1<class Generated622.BaseClass0>::ClassMethod1471()
		calli default string(class Generated622.G3_C1094`1<class Generated622.BaseClass0>)
		ldstr "G2_C78::ClassMethod1471.5459()"
		ldstr "class G3_C1094`1<class Generated622.BaseClass0> on type class Generated622.G3_C1094`1<class Generated622.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated622.G3_C1094`1<class Generated622.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated622.G3_C1094`1<class Generated622.BaseClass0>::Method6<object>()
		calli default string(class Generated622.G3_C1094`1<class Generated622.BaseClass0>)
		ldstr "G2_C78::Method6.5456<System.Object>()"
		ldstr "class G3_C1094`1<class Generated622.BaseClass0> on type class Generated622.G3_C1094`1<class Generated622.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated622.G3_C1094`1<class Generated622.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated622.G3_C1094`1<class Generated622.BaseClass0>::Method5()
		calli default string(class Generated622.G3_C1094`1<class Generated622.BaseClass0>)
		ldstr "G2_C78::Method5.5455()"
		ldstr "class G3_C1094`1<class Generated622.BaseClass0> on type class Generated622.G3_C1094`1<class Generated622.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated622.G3_C1094`1<class Generated622.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated622.G3_C1094`1<class Generated622.BaseClass0>::Method4()
		calli default string(class Generated622.G3_C1094`1<class Generated622.BaseClass0>)
		ldstr "G2_C78::Method4.5454()"
		ldstr "class G3_C1094`1<class Generated622.BaseClass0> on type class Generated622.G3_C1094`1<class Generated622.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated622.G3_C1094`1<class Generated622.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated622.G3_C1094`1<class Generated622.BaseClass0>::ClassMethod1327<object>()
		calli default string(class Generated622.G3_C1094`1<class Generated622.BaseClass0>)
		ldstr "G2_C78::ClassMethod1327.MI.5462<System.Object>()"
		ldstr "class G3_C1094`1<class Generated622.BaseClass0> on type class Generated622.G3_C1094`1<class Generated622.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated622.G3_C1094`1<class Generated622.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated622.G3_C1094`1<class Generated622.BaseClass0>::ClassMethod1326()
		calli default string(class Generated622.G3_C1094`1<class Generated622.BaseClass0>)
		ldstr "G2_C78::ClassMethod1326.MI.5461()"
		ldstr "class G3_C1094`1<class Generated622.BaseClass0> on type class Generated622.G3_C1094`1<class Generated622.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated622.G3_C1094`1<class Generated622.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass1>::ClassMethod1327<object>()
		calli default string(class Generated622.G3_C1094`1<class Generated622.BaseClass1>)
		ldstr "G2_C78::ClassMethod1327.MI.5462<System.Object>()"
		ldstr "class G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass1> on type class Generated622.G3_C1094`1<class Generated622.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass1>::ClassMethod1326()
		calli default string(class Generated622.G3_C1094`1<class Generated622.BaseClass1>)
		ldstr "G2_C78::ClassMethod1326.MI.5461()"
		ldstr "class G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass1> on type class Generated622.G3_C1094`1<class Generated622.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass1>::Method7<object>()
		calli default string(class Generated622.G3_C1094`1<class Generated622.BaseClass1>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass1> on type class Generated622.G3_C1094`1<class Generated622.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated622.IBase2`2<class Generated622.BaseClass1,class Generated622.BaseClass0>::Method7<object>()
		calli default string(class Generated622.G3_C1094`1<class Generated622.BaseClass1>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated622.BaseClass1,class Generated622.BaseClass0> on type class Generated622.G3_C1094`1<class Generated622.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated622.IBase2`2<class Generated622.BaseClass0,class Generated622.BaseClass0>::Method7<object>()
		calli default string(class Generated622.G3_C1094`1<class Generated622.BaseClass1>)
		ldstr "G3_C1094::Method7.MI.14268<System.Object>()"
		ldstr "class IBase2`2<class Generated622.BaseClass0,class Generated622.BaseClass0> on type class Generated622.G3_C1094`1<class Generated622.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated622.IBase2`2<class Generated622.BaseClass0,class Generated622.BaseClass1>::Method7<object>()
		calli default string(class Generated622.G3_C1094`1<class Generated622.BaseClass1>)
		ldstr "G3_C1094::Method7.MI.14268<System.Object>()"
		ldstr "class IBase2`2<class Generated622.BaseClass0,class Generated622.BaseClass1> on type class Generated622.G3_C1094`1<class Generated622.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated622.IBase2`2<class Generated622.BaseClass1,class Generated622.BaseClass1>::Method7<object>()
		calli default string(class Generated622.G3_C1094`1<class Generated622.BaseClass1>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated622.BaseClass1,class Generated622.BaseClass1> on type class Generated622.G3_C1094`1<class Generated622.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated622.G2_C78`1<class Generated622.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated622.G2_C78`1<class Generated622.BaseClass0>::ClassMethod1472<object>()
		calli default string(class Generated622.G3_C1094`1<class Generated622.BaseClass1>)
		ldstr "G3_C1094::ClassMethod1472.MI.14270<System.Object>()"
		ldstr "class G2_C78`1<class Generated622.BaseClass0> on type class Generated622.G3_C1094`1<class Generated622.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated622.G2_C78`1<class Generated622.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated622.G2_C78`1<class Generated622.BaseClass0>::ClassMethod1471()
		calli default string(class Generated622.G3_C1094`1<class Generated622.BaseClass1>)
		ldstr "G2_C78::ClassMethod1471.5459()"
		ldstr "class G2_C78`1<class Generated622.BaseClass0> on type class Generated622.G3_C1094`1<class Generated622.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated622.G2_C78`1<class Generated622.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated622.G2_C78`1<class Generated622.BaseClass0>::Method7<object>()
		calli default string(class Generated622.G3_C1094`1<class Generated622.BaseClass1>)
		ldstr "G3_C1094::Method7.14267<System.Object>()"
		ldstr "class G2_C78`1<class Generated622.BaseClass0> on type class Generated622.G3_C1094`1<class Generated622.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated622.G2_C78`1<class Generated622.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated622.G2_C78`1<class Generated622.BaseClass0>::Method6<object>()
		calli default string(class Generated622.G3_C1094`1<class Generated622.BaseClass1>)
		ldstr "G2_C78::Method6.5456<System.Object>()"
		ldstr "class G2_C78`1<class Generated622.BaseClass0> on type class Generated622.G3_C1094`1<class Generated622.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated622.G2_C78`1<class Generated622.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated622.G2_C78`1<class Generated622.BaseClass0>::Method5()
		calli default string(class Generated622.G3_C1094`1<class Generated622.BaseClass1>)
		ldstr "G2_C78::Method5.5455()"
		ldstr "class G2_C78`1<class Generated622.BaseClass0> on type class Generated622.G3_C1094`1<class Generated622.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated622.G2_C78`1<class Generated622.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated622.G2_C78`1<class Generated622.BaseClass0>::Method4()
		calli default string(class Generated622.G3_C1094`1<class Generated622.BaseClass1>)
		ldstr "G2_C78::Method4.5454()"
		ldstr "class G2_C78`1<class Generated622.BaseClass0> on type class Generated622.G3_C1094`1<class Generated622.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated622.G2_C78`1<class Generated622.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated622.G2_C78`1<class Generated622.BaseClass0>::ClassMethod1327<object>()
		calli default string(class Generated622.G3_C1094`1<class Generated622.BaseClass1>)
		ldstr "G2_C78::ClassMethod1327.MI.5462<System.Object>()"
		ldstr "class G2_C78`1<class Generated622.BaseClass0> on type class Generated622.G3_C1094`1<class Generated622.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated622.G2_C78`1<class Generated622.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated622.G2_C78`1<class Generated622.BaseClass0>::ClassMethod1326()
		calli default string(class Generated622.G3_C1094`1<class Generated622.BaseClass1>)
		ldstr "G2_C78::ClassMethod1326.MI.5461()"
		ldstr "class G2_C78`1<class Generated622.BaseClass0> on type class Generated622.G3_C1094`1<class Generated622.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated622.IBase1`1<class Generated622.BaseClass0>::Method4()
		calli default string(class Generated622.G3_C1094`1<class Generated622.BaseClass1>)
		ldstr "G2_C78::Method4.5454()"
		ldstr "class IBase1`1<class Generated622.BaseClass0> on type class Generated622.G3_C1094`1<class Generated622.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated622.IBase1`1<class Generated622.BaseClass0>::Method5()
		calli default string(class Generated622.G3_C1094`1<class Generated622.BaseClass1>)
		ldstr "G2_C78::Method5.5455()"
		ldstr "class IBase1`1<class Generated622.BaseClass0> on type class Generated622.G3_C1094`1<class Generated622.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated622.IBase1`1<class Generated622.BaseClass0>::Method6<object>()
		calli default string(class Generated622.G3_C1094`1<class Generated622.BaseClass1>)
		ldstr "G2_C78::Method6.MI.5457<System.Object>()"
		ldstr "class IBase1`1<class Generated622.BaseClass0> on type class Generated622.G3_C1094`1<class Generated622.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated622.G3_C1094`1<class Generated622.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated622.G3_C1094`1<class Generated622.BaseClass1>::ClassMethod3581<object>()
		calli default string(class Generated622.G3_C1094`1<class Generated622.BaseClass1>)
		ldstr "G3_C1094::ClassMethod3581.14269<System.Object>()"
		ldstr "class G3_C1094`1<class Generated622.BaseClass1> on type class Generated622.G3_C1094`1<class Generated622.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated622.G3_C1094`1<class Generated622.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated622.G3_C1094`1<class Generated622.BaseClass1>::Method7<object>()
		calli default string(class Generated622.G3_C1094`1<class Generated622.BaseClass1>)
		ldstr "G3_C1094::Method7.14267<System.Object>()"
		ldstr "class G3_C1094`1<class Generated622.BaseClass1> on type class Generated622.G3_C1094`1<class Generated622.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated622.G3_C1094`1<class Generated622.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated622.G3_C1094`1<class Generated622.BaseClass1>::ClassMethod1472<object>()
		calli default string(class Generated622.G3_C1094`1<class Generated622.BaseClass1>)
		ldstr "G3_C1094::ClassMethod1472.MI.14270<System.Object>()"
		ldstr "class G3_C1094`1<class Generated622.BaseClass1> on type class Generated622.G3_C1094`1<class Generated622.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated622.G3_C1094`1<class Generated622.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated622.G3_C1094`1<class Generated622.BaseClass1>::ClassMethod1471()
		calli default string(class Generated622.G3_C1094`1<class Generated622.BaseClass1>)
		ldstr "G2_C78::ClassMethod1471.5459()"
		ldstr "class G3_C1094`1<class Generated622.BaseClass1> on type class Generated622.G3_C1094`1<class Generated622.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated622.G3_C1094`1<class Generated622.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated622.G3_C1094`1<class Generated622.BaseClass1>::Method6<object>()
		calli default string(class Generated622.G3_C1094`1<class Generated622.BaseClass1>)
		ldstr "G2_C78::Method6.5456<System.Object>()"
		ldstr "class G3_C1094`1<class Generated622.BaseClass1> on type class Generated622.G3_C1094`1<class Generated622.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated622.G3_C1094`1<class Generated622.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated622.G3_C1094`1<class Generated622.BaseClass1>::Method5()
		calli default string(class Generated622.G3_C1094`1<class Generated622.BaseClass1>)
		ldstr "G2_C78::Method5.5455()"
		ldstr "class G3_C1094`1<class Generated622.BaseClass1> on type class Generated622.G3_C1094`1<class Generated622.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated622.G3_C1094`1<class Generated622.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated622.G3_C1094`1<class Generated622.BaseClass1>::Method4()
		calli default string(class Generated622.G3_C1094`1<class Generated622.BaseClass1>)
		ldstr "G2_C78::Method4.5454()"
		ldstr "class G3_C1094`1<class Generated622.BaseClass1> on type class Generated622.G3_C1094`1<class Generated622.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated622.G3_C1094`1<class Generated622.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated622.G3_C1094`1<class Generated622.BaseClass1>::ClassMethod1327<object>()
		calli default string(class Generated622.G3_C1094`1<class Generated622.BaseClass1>)
		ldstr "G2_C78::ClassMethod1327.MI.5462<System.Object>()"
		ldstr "class G3_C1094`1<class Generated622.BaseClass1> on type class Generated622.G3_C1094`1<class Generated622.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated622.G3_C1094`1<class Generated622.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated622.G3_C1094`1<class Generated622.BaseClass1>::ClassMethod1326()
		calli default string(class Generated622.G3_C1094`1<class Generated622.BaseClass1>)
		ldstr "G2_C78::ClassMethod1326.MI.5461()"
		ldstr "class G3_C1094`1<class Generated622.BaseClass1> on type class Generated622.G3_C1094`1<class Generated622.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated622.G2_C78`1<class Generated622.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass1>::ClassMethod1327<object>()
		calli default string(class Generated622.G2_C78`1<class Generated622.BaseClass0>)
		ldstr "G2_C78::ClassMethod1327.MI.5462<System.Object>()"
		ldstr "class G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass1> on type class Generated622.G2_C78`1<class Generated622.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass1>::ClassMethod1326()
		calli default string(class Generated622.G2_C78`1<class Generated622.BaseClass0>)
		ldstr "G2_C78::ClassMethod1326.MI.5461()"
		ldstr "class G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass1> on type class Generated622.G2_C78`1<class Generated622.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass1>::Method7<object>()
		calli default string(class Generated622.G2_C78`1<class Generated622.BaseClass0>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass1> on type class Generated622.G2_C78`1<class Generated622.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated622.IBase2`2<class Generated622.BaseClass1,class Generated622.BaseClass0>::Method7<object>()
		calli default string(class Generated622.G2_C78`1<class Generated622.BaseClass0>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated622.BaseClass1,class Generated622.BaseClass0> on type class Generated622.G2_C78`1<class Generated622.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated622.IBase2`2<class Generated622.BaseClass0,class Generated622.BaseClass0>::Method7<object>()
		calli default string(class Generated622.G2_C78`1<class Generated622.BaseClass0>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated622.BaseClass0,class Generated622.BaseClass0> on type class Generated622.G2_C78`1<class Generated622.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated622.IBase2`2<class Generated622.BaseClass0,class Generated622.BaseClass1>::Method7<object>()
		calli default string(class Generated622.G2_C78`1<class Generated622.BaseClass0>)
		ldstr "G2_C78::Method7.5458<System.Object>()"
		ldstr "class IBase2`2<class Generated622.BaseClass0,class Generated622.BaseClass1> on type class Generated622.G2_C78`1<class Generated622.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated622.IBase2`2<class Generated622.BaseClass1,class Generated622.BaseClass1>::Method7<object>()
		calli default string(class Generated622.G2_C78`1<class Generated622.BaseClass0>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated622.BaseClass1,class Generated622.BaseClass1> on type class Generated622.G2_C78`1<class Generated622.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated622.G2_C78`1<class Generated622.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated622.G2_C78`1<class Generated622.BaseClass0>::ClassMethod1472<object>()
		calli default string(class Generated622.G2_C78`1<class Generated622.BaseClass0>)
		ldstr "G2_C78::ClassMethod1472.5460<System.Object>()"
		ldstr "class G2_C78`1<class Generated622.BaseClass0> on type class Generated622.G2_C78`1<class Generated622.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated622.G2_C78`1<class Generated622.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated622.G2_C78`1<class Generated622.BaseClass0>::ClassMethod1471()
		calli default string(class Generated622.G2_C78`1<class Generated622.BaseClass0>)
		ldstr "G2_C78::ClassMethod1471.5459()"
		ldstr "class G2_C78`1<class Generated622.BaseClass0> on type class Generated622.G2_C78`1<class Generated622.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated622.G2_C78`1<class Generated622.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated622.G2_C78`1<class Generated622.BaseClass0>::Method7<object>()
		calli default string(class Generated622.G2_C78`1<class Generated622.BaseClass0>)
		ldstr "G2_C78::Method7.5458<System.Object>()"
		ldstr "class G2_C78`1<class Generated622.BaseClass0> on type class Generated622.G2_C78`1<class Generated622.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated622.G2_C78`1<class Generated622.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated622.G2_C78`1<class Generated622.BaseClass0>::Method6<object>()
		calli default string(class Generated622.G2_C78`1<class Generated622.BaseClass0>)
		ldstr "G2_C78::Method6.5456<System.Object>()"
		ldstr "class G2_C78`1<class Generated622.BaseClass0> on type class Generated622.G2_C78`1<class Generated622.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated622.G2_C78`1<class Generated622.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated622.G2_C78`1<class Generated622.BaseClass0>::Method5()
		calli default string(class Generated622.G2_C78`1<class Generated622.BaseClass0>)
		ldstr "G2_C78::Method5.5455()"
		ldstr "class G2_C78`1<class Generated622.BaseClass0> on type class Generated622.G2_C78`1<class Generated622.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated622.G2_C78`1<class Generated622.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated622.G2_C78`1<class Generated622.BaseClass0>::Method4()
		calli default string(class Generated622.G2_C78`1<class Generated622.BaseClass0>)
		ldstr "G2_C78::Method4.5454()"
		ldstr "class G2_C78`1<class Generated622.BaseClass0> on type class Generated622.G2_C78`1<class Generated622.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated622.G2_C78`1<class Generated622.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated622.G2_C78`1<class Generated622.BaseClass0>::ClassMethod1327<object>()
		calli default string(class Generated622.G2_C78`1<class Generated622.BaseClass0>)
		ldstr "G2_C78::ClassMethod1327.MI.5462<System.Object>()"
		ldstr "class G2_C78`1<class Generated622.BaseClass0> on type class Generated622.G2_C78`1<class Generated622.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated622.G2_C78`1<class Generated622.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated622.G2_C78`1<class Generated622.BaseClass0>::ClassMethod1326()
		calli default string(class Generated622.G2_C78`1<class Generated622.BaseClass0>)
		ldstr "G2_C78::ClassMethod1326.MI.5461()"
		ldstr "class G2_C78`1<class Generated622.BaseClass0> on type class Generated622.G2_C78`1<class Generated622.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated622.IBase1`1<class Generated622.BaseClass0>::Method4()
		calli default string(class Generated622.G2_C78`1<class Generated622.BaseClass0>)
		ldstr "G2_C78::Method4.5454()"
		ldstr "class IBase1`1<class Generated622.BaseClass0> on type class Generated622.G2_C78`1<class Generated622.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated622.IBase1`1<class Generated622.BaseClass0>::Method5()
		calli default string(class Generated622.G2_C78`1<class Generated622.BaseClass0>)
		ldstr "G2_C78::Method5.5455()"
		ldstr "class IBase1`1<class Generated622.BaseClass0> on type class Generated622.G2_C78`1<class Generated622.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated622.IBase1`1<class Generated622.BaseClass0>::Method6<object>()
		calli default string(class Generated622.G2_C78`1<class Generated622.BaseClass0>)
		ldstr "G2_C78::Method6.MI.5457<System.Object>()"
		ldstr "class IBase1`1<class Generated622.BaseClass0> on type class Generated622.G2_C78`1<class Generated622.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated622.G2_C78`1<class Generated622.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated622.G1_C6`2<class Generated622.BaseClass1,class Generated622.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated622.G1_C6`2<class Generated622.BaseClass1,class Generated622.BaseClass1>::ClassMethod1327<object>()
		calli default string(class Generated622.G2_C78`1<class Generated622.BaseClass1>)
		ldstr "G2_C78::ClassMethod1327.MI.5462<System.Object>()"
		ldstr "class G1_C6`2<class Generated622.BaseClass1,class Generated622.BaseClass1> on type class Generated622.G2_C78`1<class Generated622.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated622.G1_C6`2<class Generated622.BaseClass1,class Generated622.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated622.G1_C6`2<class Generated622.BaseClass1,class Generated622.BaseClass1>::ClassMethod1326()
		calli default string(class Generated622.G2_C78`1<class Generated622.BaseClass1>)
		ldstr "G2_C78::ClassMethod1326.MI.5461()"
		ldstr "class G1_C6`2<class Generated622.BaseClass1,class Generated622.BaseClass1> on type class Generated622.G2_C78`1<class Generated622.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated622.G1_C6`2<class Generated622.BaseClass1,class Generated622.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated622.G1_C6`2<class Generated622.BaseClass1,class Generated622.BaseClass1>::Method7<object>()
		calli default string(class Generated622.G2_C78`1<class Generated622.BaseClass1>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class G1_C6`2<class Generated622.BaseClass1,class Generated622.BaseClass1> on type class Generated622.G2_C78`1<class Generated622.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated622.IBase2`2<class Generated622.BaseClass1,class Generated622.BaseClass1>::Method7<object>()
		calli default string(class Generated622.G2_C78`1<class Generated622.BaseClass1>)
		ldstr "G2_C78::Method7.5458<System.Object>()"
		ldstr "class IBase2`2<class Generated622.BaseClass1,class Generated622.BaseClass1> on type class Generated622.G2_C78`1<class Generated622.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated622.IBase2`2<class Generated622.BaseClass0,class Generated622.BaseClass1>::Method7<object>()
		calli default string(class Generated622.G2_C78`1<class Generated622.BaseClass1>)
		ldstr "G2_C78::Method7.5458<System.Object>()"
		ldstr "class IBase2`2<class Generated622.BaseClass0,class Generated622.BaseClass1> on type class Generated622.G2_C78`1<class Generated622.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated622.G2_C78`1<class Generated622.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated622.G2_C78`1<class Generated622.BaseClass1>::ClassMethod1472<object>()
		calli default string(class Generated622.G2_C78`1<class Generated622.BaseClass1>)
		ldstr "G2_C78::ClassMethod1472.5460<System.Object>()"
		ldstr "class G2_C78`1<class Generated622.BaseClass1> on type class Generated622.G2_C78`1<class Generated622.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated622.G2_C78`1<class Generated622.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated622.G2_C78`1<class Generated622.BaseClass1>::ClassMethod1471()
		calli default string(class Generated622.G2_C78`1<class Generated622.BaseClass1>)
		ldstr "G2_C78::ClassMethod1471.5459()"
		ldstr "class G2_C78`1<class Generated622.BaseClass1> on type class Generated622.G2_C78`1<class Generated622.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated622.G2_C78`1<class Generated622.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated622.G2_C78`1<class Generated622.BaseClass1>::Method7<object>()
		calli default string(class Generated622.G2_C78`1<class Generated622.BaseClass1>)
		ldstr "G2_C78::Method7.5458<System.Object>()"
		ldstr "class G2_C78`1<class Generated622.BaseClass1> on type class Generated622.G2_C78`1<class Generated622.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated622.G2_C78`1<class Generated622.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated622.G2_C78`1<class Generated622.BaseClass1>::Method6<object>()
		calli default string(class Generated622.G2_C78`1<class Generated622.BaseClass1>)
		ldstr "G2_C78::Method6.5456<System.Object>()"
		ldstr "class G2_C78`1<class Generated622.BaseClass1> on type class Generated622.G2_C78`1<class Generated622.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated622.G2_C78`1<class Generated622.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated622.G2_C78`1<class Generated622.BaseClass1>::Method5()
		calli default string(class Generated622.G2_C78`1<class Generated622.BaseClass1>)
		ldstr "G2_C78::Method5.5455()"
		ldstr "class G2_C78`1<class Generated622.BaseClass1> on type class Generated622.G2_C78`1<class Generated622.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated622.G2_C78`1<class Generated622.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated622.G2_C78`1<class Generated622.BaseClass1>::Method4()
		calli default string(class Generated622.G2_C78`1<class Generated622.BaseClass1>)
		ldstr "G2_C78::Method4.5454()"
		ldstr "class G2_C78`1<class Generated622.BaseClass1> on type class Generated622.G2_C78`1<class Generated622.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated622.G2_C78`1<class Generated622.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated622.G2_C78`1<class Generated622.BaseClass1>::ClassMethod1327<object>()
		calli default string(class Generated622.G2_C78`1<class Generated622.BaseClass1>)
		ldstr "G2_C78::ClassMethod1327.MI.5462<System.Object>()"
		ldstr "class G2_C78`1<class Generated622.BaseClass1> on type class Generated622.G2_C78`1<class Generated622.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated622.G2_C78`1<class Generated622.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated622.G2_C78`1<class Generated622.BaseClass1>::ClassMethod1326()
		calli default string(class Generated622.G2_C78`1<class Generated622.BaseClass1>)
		ldstr "G2_C78::ClassMethod1326.MI.5461()"
		ldstr "class G2_C78`1<class Generated622.BaseClass1> on type class Generated622.G2_C78`1<class Generated622.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated622.IBase1`1<class Generated622.BaseClass1>::Method4()
		calli default string(class Generated622.G2_C78`1<class Generated622.BaseClass1>)
		ldstr "G2_C78::Method4.5454()"
		ldstr "class IBase1`1<class Generated622.BaseClass1> on type class Generated622.G2_C78`1<class Generated622.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated622.IBase1`1<class Generated622.BaseClass1>::Method5()
		calli default string(class Generated622.G2_C78`1<class Generated622.BaseClass1>)
		ldstr "G2_C78::Method5.5455()"
		ldstr "class IBase1`1<class Generated622.BaseClass1> on type class Generated622.G2_C78`1<class Generated622.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated622.IBase1`1<class Generated622.BaseClass1>::Method6<object>()
		calli default string(class Generated622.G2_C78`1<class Generated622.BaseClass1>)
		ldstr "G2_C78::Method6.MI.5457<System.Object>()"
		ldstr "class IBase1`1<class Generated622.BaseClass1> on type class Generated622.G2_C78`1<class Generated622.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated622.IBase1`1<class Generated622.BaseClass0>::Method4()
		calli default string(class Generated622.G2_C78`1<class Generated622.BaseClass1>)
		ldstr "G2_C78::Method4.5454()"
		ldstr "class IBase1`1<class Generated622.BaseClass0> on type class Generated622.G2_C78`1<class Generated622.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated622.IBase1`1<class Generated622.BaseClass0>::Method5()
		calli default string(class Generated622.G2_C78`1<class Generated622.BaseClass1>)
		ldstr "G2_C78::Method5.5455()"
		ldstr "class IBase1`1<class Generated622.BaseClass0> on type class Generated622.G2_C78`1<class Generated622.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated622.IBase1`1<class Generated622.BaseClass0>::Method6<object>()
		calli default string(class Generated622.G2_C78`1<class Generated622.BaseClass1>)
		ldstr "G2_C78::Method6.MI.5457<System.Object>()"
		ldstr "class IBase1`1<class Generated622.BaseClass0> on type class Generated622.G2_C78`1<class Generated622.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass0>::ClassMethod1327<object>()
		calli default string(class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass0>)
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass0> on type class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass0>::ClassMethod1326()
		calli default string(class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass0>)
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass0> on type class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass0>::Method7<object>()
		calli default string(class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass0>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass0> on type class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated622.IBase2`2<class Generated622.BaseClass1,class Generated622.BaseClass0>::Method7<object>()
		calli default string(class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass0>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated622.BaseClass1,class Generated622.BaseClass0> on type class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated622.IBase2`2<class Generated622.BaseClass0,class Generated622.BaseClass0>::Method7<object>()
		calli default string(class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass0>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated622.BaseClass0,class Generated622.BaseClass0> on type class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated622.IBase2`2<class Generated622.BaseClass0,class Generated622.BaseClass1>::Method7<object>()
		calli default string(class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass0>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated622.BaseClass0,class Generated622.BaseClass1> on type class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated622.IBase2`2<class Generated622.BaseClass1,class Generated622.BaseClass1>::Method7<object>()
		calli default string(class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass0>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated622.BaseClass1,class Generated622.BaseClass1> on type class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass1>::ClassMethod1327<object>()
		calli default string(class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass1>)
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass1> on type class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass1>::ClassMethod1326()
		calli default string(class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass1>)
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass1> on type class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass1>::Method7<object>()
		calli default string(class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass1>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass1> on type class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated622.IBase2`2<class Generated622.BaseClass1,class Generated622.BaseClass0>::Method7<object>()
		calli default string(class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass1>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated622.BaseClass1,class Generated622.BaseClass0> on type class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated622.IBase2`2<class Generated622.BaseClass0,class Generated622.BaseClass0>::Method7<object>()
		calli default string(class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass1>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated622.BaseClass0,class Generated622.BaseClass0> on type class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated622.IBase2`2<class Generated622.BaseClass0,class Generated622.BaseClass1>::Method7<object>()
		calli default string(class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass1>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated622.BaseClass0,class Generated622.BaseClass1> on type class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated622.IBase2`2<class Generated622.BaseClass1,class Generated622.BaseClass1>::Method7<object>()
		calli default string(class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass1>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated622.BaseClass1,class Generated622.BaseClass1> on type class Generated622.G1_C6`2<class Generated622.BaseClass0,class Generated622.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated622.G1_C6`2<class Generated622.BaseClass1,class Generated622.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated622.G1_C6`2<class Generated622.BaseClass1,class Generated622.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated622.G1_C6`2<class Generated622.BaseClass1,class Generated622.BaseClass0>::ClassMethod1327<object>()
		calli default string(class Generated622.G1_C6`2<class Generated622.BaseClass1,class Generated622.BaseClass0>)
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G1_C6`2<class Generated622.BaseClass1,class Generated622.BaseClass0> on type class Generated622.G1_C6`2<class Generated622.BaseClass1,class Generated622.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated622.G1_C6`2<class Generated622.BaseClass1,class Generated622.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated622.G1_C6`2<class Generated622.BaseClass1,class Generated622.BaseClass0>::ClassMethod1326()
		calli default string(class Generated622.G1_C6`2<class Generated622.BaseClass1,class Generated622.BaseClass0>)
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G1_C6`2<class Generated622.BaseClass1,class Generated622.BaseClass0> on type class Generated622.G1_C6`2<class Generated622.BaseClass1,class Generated622.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated622.G1_C6`2<class Generated622.BaseClass1,class Generated622.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated622.G1_C6`2<class Generated622.BaseClass1,class Generated622.BaseClass0>::Method7<object>()
		calli default string(class Generated622.G1_C6`2<class Generated622.BaseClass1,class Generated622.BaseClass0>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class G1_C6`2<class Generated622.BaseClass1,class Generated622.BaseClass0> on type class Generated622.G1_C6`2<class Generated622.BaseClass1,class Generated622.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated622.IBase2`2<class Generated622.BaseClass1,class Generated622.BaseClass1>::Method7<object>()
		calli default string(class Generated622.G1_C6`2<class Generated622.BaseClass1,class Generated622.BaseClass0>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated622.BaseClass1,class Generated622.BaseClass1> on type class Generated622.G1_C6`2<class Generated622.BaseClass1,class Generated622.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated622.IBase2`2<class Generated622.BaseClass0,class Generated622.BaseClass1>::Method7<object>()
		calli default string(class Generated622.G1_C6`2<class Generated622.BaseClass1,class Generated622.BaseClass0>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated622.BaseClass0,class Generated622.BaseClass1> on type class Generated622.G1_C6`2<class Generated622.BaseClass1,class Generated622.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated622.G1_C6`2<class Generated622.BaseClass1,class Generated622.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated622.G1_C6`2<class Generated622.BaseClass1,class Generated622.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated622.G1_C6`2<class Generated622.BaseClass1,class Generated622.BaseClass1>::ClassMethod1327<object>()
		calli default string(class Generated622.G1_C6`2<class Generated622.BaseClass1,class Generated622.BaseClass1>)
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G1_C6`2<class Generated622.BaseClass1,class Generated622.BaseClass1> on type class Generated622.G1_C6`2<class Generated622.BaseClass1,class Generated622.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated622.G1_C6`2<class Generated622.BaseClass1,class Generated622.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated622.G1_C6`2<class Generated622.BaseClass1,class Generated622.BaseClass1>::ClassMethod1326()
		calli default string(class Generated622.G1_C6`2<class Generated622.BaseClass1,class Generated622.BaseClass1>)
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G1_C6`2<class Generated622.BaseClass1,class Generated622.BaseClass1> on type class Generated622.G1_C6`2<class Generated622.BaseClass1,class Generated622.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated622.G1_C6`2<class Generated622.BaseClass1,class Generated622.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated622.G1_C6`2<class Generated622.BaseClass1,class Generated622.BaseClass1>::Method7<object>()
		calli default string(class Generated622.G1_C6`2<class Generated622.BaseClass1,class Generated622.BaseClass1>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class G1_C6`2<class Generated622.BaseClass1,class Generated622.BaseClass1> on type class Generated622.G1_C6`2<class Generated622.BaseClass1,class Generated622.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated622.IBase2`2<class Generated622.BaseClass1,class Generated622.BaseClass1>::Method7<object>()
		calli default string(class Generated622.G1_C6`2<class Generated622.BaseClass1,class Generated622.BaseClass1>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated622.BaseClass1,class Generated622.BaseClass1> on type class Generated622.G1_C6`2<class Generated622.BaseClass1,class Generated622.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated622.IBase2`2<class Generated622.BaseClass0,class Generated622.BaseClass1>::Method7<object>()
		calli default string(class Generated622.G1_C6`2<class Generated622.BaseClass1,class Generated622.BaseClass1>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated622.BaseClass0,class Generated622.BaseClass1> on type class Generated622.G1_C6`2<class Generated622.BaseClass1,class Generated622.BaseClass1>"
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
		call void Generated622.Generated622::MethodCallingTest()
		call void Generated622.Generated622::ConstrainedCallsTest()
		call void Generated622.Generated622::StructConstrainedInterfaceCallsTest()
		call void Generated622.Generated622::CalliTest()
		ldc.i4 100
		ret
	}
}
