// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) .ver 4:0:0:0 }
.assembly extern TestFramework { .publickeytoken = ( B0 3F 5F 7F 11 D5 0A 3A )  }

//TYPES IN FORWARDER ASSEMBLIES:

//TEST ASSEMBLY:
.assembly extern xunit.core {}

.class public Generated1248.BaseClass0 
{
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void [mscorlib]System.Object::.ctor()
		ret
	}
}
.class public Generated1248.BaseClass1 
		extends Generated1248.BaseClass0
{
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void Generated1248.BaseClass0::.ctor()
		ret
	}
}
.class public Generated1248.G3_C1720`1<T0> 
		extends class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		implements class Generated1248.IBase2`2<class Generated1248.BaseClass0,!T0> 
{
	.method public hidebysig newslot virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G3_C1720::Method7.17628<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase2<class Generated1248.BaseClass0,T0>.Method7'<M0>() cil managed noinlining { 
		.override method instance string class Generated1248.IBase2`2<class Generated1248.BaseClass0,!T0>::Method7<[1]>()
		ldstr "G3_C1720::Method7.MI.17629<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod4839() cil managed noinlining { 
		ldstr "G3_C1720::ClassMethod4839.17630()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod4840() cil managed noinlining { 
		ldstr "G3_C1720::ClassMethod4840.17631()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod4841<M0>() cil managed noinlining { 
		ldstr "G3_C1720::ClassMethod4841.17632<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'G2_C693<class Generated1248.BaseClass0,class Generated1248.BaseClass1>.ClassMethod2748'() cil managed noinlining { 
		.override method instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::ClassMethod2748()
		ldstr "G3_C1720::ClassMethod2748.MI.17633()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'G2_C693<class Generated1248.BaseClass0,class Generated1248.BaseClass1>.ClassMethod2749'() cil managed noinlining { 
		.override method instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::ClassMethod2749()
		ldstr "G3_C1720::ClassMethod2749.MI.17634()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'G2_C693<class Generated1248.BaseClass0,class Generated1248.BaseClass1>.ClassMethod2750'<M0>() cil managed noinlining { 
		.override method instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::ClassMethod2750<[1]>()
		ldstr "G3_C1720::ClassMethod2750.MI.17635<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'G2_C693<class Generated1248.BaseClass0,class Generated1248.BaseClass1>.ClassMethod2751'<M0>() cil managed noinlining { 
		.override method instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::ClassMethod2751<[1]>()
		ldstr "G3_C1720::ClassMethod2751.MI.17636<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::.ctor()
		ret
	}
}
.class public Generated1248.G2_C693`2<T0, T1> 
		extends class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		implements class Generated1248.IBase1`1<class Generated1248.BaseClass0>, class Generated1248.IBase2`2<class Generated1248.BaseClass1,!T0> 
{
	.method public hidebysig newslot virtual instance string Method4() cil managed noinlining { 
		ldstr "G2_C693::Method4.11387()"
		ret
	}
	.method public hidebysig newslot virtual instance string Method5() cil managed noinlining { 
		ldstr "G2_C693::Method5.11388()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<class Generated1248.BaseClass0>.Method5'() cil managed noinlining { 
		.override method instance string class Generated1248.IBase1`1<class Generated1248.BaseClass0>::Method5()
		ldstr "G2_C693::Method5.MI.11389()"
		ret
	}
	.method public hidebysig newslot virtual instance string Method6<M0>() cil managed noinlining { 
		ldstr "G2_C693::Method6.11390<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G2_C693::Method7.11391<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod2748() cil managed noinlining { 
		ldstr "G2_C693::ClassMethod2748.11392()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod2749() cil managed noinlining { 
		ldstr "G2_C693::ClassMethod2749.11393()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod2750<M0>() cil managed noinlining { 
		ldstr "G2_C693::ClassMethod2750.11394<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod2751<M0>() cil managed noinlining { 
		ldstr "G2_C693::ClassMethod2751.11395<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::.ctor()
		ret
	}
}
.class interface public abstract Generated1248.IBase2`2<+T0, -T1> 
{
	.method public hidebysig newslot abstract virtual instance string Method7<M0>() cil managed { }
}
.class public abstract Generated1248.G1_C13`2<T0, T1> 
		implements class Generated1248.IBase2`2<!T0,!T1>, class Generated1248.IBase1`1<!T0> 
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
		.override method instance string class Generated1248.IBase1`1<!T0>::Method5()
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
.class interface public abstract Generated1248.IBase1`1<+T0> 
{
	.method public hidebysig newslot abstract virtual instance string Method4() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method5() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method6<M0>() cil managed { }
}
.class public auto ansi beforefieldinit Generated1248.Generated1248 {
	.method static void M.BaseClass0<(Generated1248.BaseClass0)W>(!!W inst, string exp) cil managed {
		.maxstack 5
		.locals init (string[] actualResults)
		ldc.i4.s    0
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.BaseClass0<(Generated1248.BaseClass0)W>(!!W inst, string exp)"
		ldc.i4.s 0
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.BaseClass1<(Generated1248.BaseClass1)W>(!!W inst, string exp) cil managed {
		.maxstack 5
		.locals init (string[] actualResults)
		ldc.i4.s    0
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.BaseClass1<(Generated1248.BaseClass1)W>(!!W inst, string exp)"
		ldc.i4.s 0
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1720.T<T0,(class Generated1248.G3_C1720`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 18
		.locals init (string[] actualResults)
		ldc.i4.s    13
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1720.T<T0,(class Generated1248.G3_C1720`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 13
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G3_C1720`1<!!T0>::ClassMethod1348<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G3_C1720`1<!!T0>::ClassMethod1349<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G3_C1720`1<!!T0>::ClassMethod2748()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G3_C1720`1<!!T0>::ClassMethod2749()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G3_C1720`1<!!T0>::ClassMethod2750<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G3_C1720`1<!!T0>::ClassMethod2751<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G3_C1720`1<!!T0>::ClassMethod4839()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G3_C1720`1<!!T0>::ClassMethod4840()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G3_C1720`1<!!T0>::ClassMethod4841<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G3_C1720`1<!!T0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G3_C1720`1<!!T0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G3_C1720`1<!!T0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G3_C1720`1<!!T0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1720.A<(class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 18
		.locals init (string[] actualResults)
		ldc.i4.s    13
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1720.A<(class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 13
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>::ClassMethod1348<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>::ClassMethod1349<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>::ClassMethod2748()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>::ClassMethod2749()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>::ClassMethod2750<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>::ClassMethod2751<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>::ClassMethod4839()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>::ClassMethod4840()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>::ClassMethod4841<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1720.B<(class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 18
		.locals init (string[] actualResults)
		ldc.i4.s    13
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1720.B<(class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 13
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>::ClassMethod1348<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>::ClassMethod1349<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>::ClassMethod2748()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>::ClassMethod2749()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>::ClassMethod2750<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>::ClassMethod2751<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>::ClassMethod4839()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>::ClassMethod4840()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>::ClassMethod4841<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C693.T.T<T0,T1,(class Generated1248.G2_C693`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 15
		.locals init (string[] actualResults)
		ldc.i4.s    10
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C693.T.T<T0,T1,(class Generated1248.G2_C693`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 10
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G2_C693`2<!!T0,!!T1>::ClassMethod1348<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G2_C693`2<!!T0,!!T1>::ClassMethod1349<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G2_C693`2<!!T0,!!T1>::ClassMethod2748()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G2_C693`2<!!T0,!!T1>::ClassMethod2749()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G2_C693`2<!!T0,!!T1>::ClassMethod2750<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G2_C693`2<!!T0,!!T1>::ClassMethod2751<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G2_C693`2<!!T0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G2_C693`2<!!T0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G2_C693`2<!!T0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G2_C693`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C693.A.T<T1,(class Generated1248.G2_C693`2<class Generated1248.BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 15
		.locals init (string[] actualResults)
		ldc.i4.s    10
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C693.A.T<T1,(class Generated1248.G2_C693`2<class Generated1248.BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 10
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass0,!!T1>::ClassMethod1348<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass0,!!T1>::ClassMethod1349<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass0,!!T1>::ClassMethod2748()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass0,!!T1>::ClassMethod2749()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass0,!!T1>::ClassMethod2750<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass0,!!T1>::ClassMethod2751<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C693.A.A<(class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 15
		.locals init (string[] actualResults)
		ldc.i4.s    10
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C693.A.A<(class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 10
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>::ClassMethod1348<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>::ClassMethod1349<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>::ClassMethod2748()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>::ClassMethod2749()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>::ClassMethod2750<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>::ClassMethod2751<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C693.A.B<(class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 15
		.locals init (string[] actualResults)
		ldc.i4.s    10
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C693.A.B<(class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 10
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::ClassMethod1348<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::ClassMethod1349<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::ClassMethod2748()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::ClassMethod2749()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::ClassMethod2750<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::ClassMethod2751<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C693.B.T<T1,(class Generated1248.G2_C693`2<class Generated1248.BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 15
		.locals init (string[] actualResults)
		ldc.i4.s    10
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C693.B.T<T1,(class Generated1248.G2_C693`2<class Generated1248.BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 10
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass1,!!T1>::ClassMethod1348<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass1,!!T1>::ClassMethod1349<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass1,!!T1>::ClassMethod2748()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass1,!!T1>::ClassMethod2749()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass1,!!T1>::ClassMethod2750<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass1,!!T1>::ClassMethod2751<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass1,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass1,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass1,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C693.B.A<(class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 15
		.locals init (string[] actualResults)
		ldc.i4.s    10
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C693.B.A<(class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 10
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>::ClassMethod1348<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>::ClassMethod1349<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>::ClassMethod2748()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>::ClassMethod2749()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>::ClassMethod2750<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>::ClassMethod2751<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C693.B.B<(class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 15
		.locals init (string[] actualResults)
		ldc.i4.s    10
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C693.B.B<(class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 10
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>::ClassMethod1348<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>::ClassMethod1349<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>::ClassMethod2748()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>::ClassMethod2749()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>::ClassMethod2750<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>::ClassMethod2751<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.T.T<T0,T1,(class Generated1248.IBase2`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.T.T<T0,T1,(class Generated1248.IBase2`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.IBase2`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.A.T<T1,(class Generated1248.IBase2`2<class Generated1248.BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.A.T<T1,(class Generated1248.IBase2`2<class Generated1248.BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.IBase2`2<class Generated1248.BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.A.A<(class Generated1248.IBase2`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.A.A<(class Generated1248.IBase2`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.IBase2`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.A.B<(class Generated1248.IBase2`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.A.B<(class Generated1248.IBase2`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.IBase2`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.B.T<T1,(class Generated1248.IBase2`2<class Generated1248.BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.B.T<T1,(class Generated1248.IBase2`2<class Generated1248.BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.IBase2`2<class Generated1248.BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.B.A<(class Generated1248.IBase2`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.B.A<(class Generated1248.IBase2`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.IBase2`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.B.B<(class Generated1248.IBase2`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.B.B<(class Generated1248.IBase2`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.IBase2`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C13.T.T<T0,T1,(class Generated1248.G1_C13`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C13.T.T<T0,T1,(class Generated1248.G1_C13`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G1_C13`2<!!T0,!!T1>::ClassMethod1348<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G1_C13`2<!!T0,!!T1>::ClassMethod1349<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G1_C13`2<!!T0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G1_C13`2<!!T0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G1_C13`2<!!T0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G1_C13`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C13.A.T<T1,(class Generated1248.G1_C13`2<class Generated1248.BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C13.A.T<T1,(class Generated1248.G1_C13`2<class Generated1248.BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G1_C13`2<class Generated1248.BaseClass0,!!T1>::ClassMethod1348<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G1_C13`2<class Generated1248.BaseClass0,!!T1>::ClassMethod1349<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G1_C13`2<class Generated1248.BaseClass0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G1_C13`2<class Generated1248.BaseClass0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G1_C13`2<class Generated1248.BaseClass0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G1_C13`2<class Generated1248.BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C13.A.A<(class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C13.A.A<(class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>::ClassMethod1348<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>::ClassMethod1349<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C13.A.B<(class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C13.A.B<(class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::ClassMethod1348<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::ClassMethod1349<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C13.B.T<T1,(class Generated1248.G1_C13`2<class Generated1248.BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C13.B.T<T1,(class Generated1248.G1_C13`2<class Generated1248.BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G1_C13`2<class Generated1248.BaseClass1,!!T1>::ClassMethod1348<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G1_C13`2<class Generated1248.BaseClass1,!!T1>::ClassMethod1349<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G1_C13`2<class Generated1248.BaseClass1,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G1_C13`2<class Generated1248.BaseClass1,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G1_C13`2<class Generated1248.BaseClass1,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G1_C13`2<class Generated1248.BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C13.B.A<(class Generated1248.G1_C13`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C13.B.A<(class Generated1248.G1_C13`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G1_C13`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>::ClassMethod1348<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G1_C13`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>::ClassMethod1349<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G1_C13`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G1_C13`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G1_C13`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G1_C13`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C13.B.B<(class Generated1248.G1_C13`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C13.B.B<(class Generated1248.G1_C13`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G1_C13`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>::ClassMethod1348<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G1_C13`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>::ClassMethod1349<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G1_C13`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G1_C13`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G1_C13`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.G1_C13`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase1.T<T0,(class Generated1248.IBase1`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase1.T<T0,(class Generated1248.IBase1`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.IBase1`1<!!T0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.IBase1`1<!!T0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.IBase1`1<!!T0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase1.A<(class Generated1248.IBase1`1<class Generated1248.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase1.A<(class Generated1248.IBase1`1<class Generated1248.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.IBase1`1<class Generated1248.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.IBase1`1<class Generated1248.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.IBase1`1<class Generated1248.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase1.B<(class Generated1248.IBase1`1<class Generated1248.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase1.B<(class Generated1248.IBase1`1<class Generated1248.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.IBase1`1<class Generated1248.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.IBase1`1<class Generated1248.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1248.IBase1`1<class Generated1248.BaseClass1>::Method6<object>()
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
		newobj instance void class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		callvirt instance string class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::ClassMethod1349<object>()
		ldstr "G1_C13::ClassMethod1349.4877<System.Object>()"
		ldstr "class G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		callvirt instance string class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::ClassMethod1348<object>()
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()"
		ldstr "class G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		callvirt instance string class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::Method6<object>()
		ldstr "G1_C13::Method6.4875<System.Object>()"
		ldstr "class G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		callvirt instance string class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::Method5()
		ldstr "G1_C13::Method5.4873()"
		ldstr "class G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		callvirt instance string class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::Method4()
		ldstr "G1_C13::Method4.4872()"
		ldstr "class G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		callvirt instance string class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::Method7<object>()
		ldstr "G2_C693::Method7.11391<System.Object>()"
		ldstr "class G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1248.IBase2`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::Method7<object>()
		ldstr "G3_C1720::Method7.MI.17629<System.Object>()"
		ldstr "class IBase2`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1248.IBase1`1<class Generated1248.BaseClass0>::Method4()
		ldstr "G2_C693::Method4.11387()"
		ldstr "class IBase1`1<class Generated1248.BaseClass0> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1248.IBase1`1<class Generated1248.BaseClass0>::Method5()
		ldstr "G2_C693::Method5.MI.11389()"
		ldstr "class IBase1`1<class Generated1248.BaseClass0> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1248.IBase1`1<class Generated1248.BaseClass0>::Method6<object>()
		ldstr "G2_C693::Method6.11390<System.Object>()"
		ldstr "class IBase1`1<class Generated1248.BaseClass0> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		callvirt instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::ClassMethod2751<object>()
		ldstr "G3_C1720::ClassMethod2751.MI.17636<System.Object>()"
		ldstr "class G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		callvirt instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::ClassMethod2750<object>()
		ldstr "G3_C1720::ClassMethod2750.MI.17635<System.Object>()"
		ldstr "class G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		callvirt instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::ClassMethod2749()
		ldstr "G3_C1720::ClassMethod2749.MI.17634()"
		ldstr "class G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		callvirt instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::ClassMethod2748()
		ldstr "G3_C1720::ClassMethod2748.MI.17633()"
		ldstr "class G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		callvirt instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::Method7<object>()
		ldstr "G2_C693::Method7.11391<System.Object>()"
		ldstr "class G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		callvirt instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::Method6<object>()
		ldstr "G2_C693::Method6.11390<System.Object>()"
		ldstr "class G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		callvirt instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::Method5()
		ldstr "G2_C693::Method5.11388()"
		ldstr "class G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		callvirt instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::Method4()
		ldstr "G2_C693::Method4.11387()"
		ldstr "class G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		callvirt instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::ClassMethod1349<object>()
		ldstr "G1_C13::ClassMethod1349.4877<System.Object>()"
		ldstr "class G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		callvirt instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::ClassMethod1348<object>()
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()"
		ldstr "class G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1248.IBase2`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>::Method7<object>()
		ldstr "G2_C693::Method7.11391<System.Object>()"
		ldstr "class IBase2`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1248.IBase2`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>::Method7<object>()
		ldstr "G3_C1720::Method7.MI.17629<System.Object>()"
		ldstr "class IBase2`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1248.IBase2`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>::Method7<object>()
		ldstr "G2_C693::Method7.11391<System.Object>()"
		ldstr "class IBase2`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>
		callvirt instance string class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>::ClassMethod4841<object>()
		ldstr "G3_C1720::ClassMethod4841.17632<System.Object>()"
		ldstr "class G3_C1720`1<class Generated1248.BaseClass0> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>
		callvirt instance string class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>::ClassMethod4840()
		ldstr "G3_C1720::ClassMethod4840.17631()"
		ldstr "class G3_C1720`1<class Generated1248.BaseClass0> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>
		callvirt instance string class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>::ClassMethod4839()
		ldstr "G3_C1720::ClassMethod4839.17630()"
		ldstr "class G3_C1720`1<class Generated1248.BaseClass0> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>
		callvirt instance string class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>::Method7<object>()
		ldstr "G3_C1720::Method7.17628<System.Object>()"
		ldstr "class G3_C1720`1<class Generated1248.BaseClass0> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>
		callvirt instance string class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>::ClassMethod2751<object>()
		ldstr "G3_C1720::ClassMethod2751.MI.17636<System.Object>()"
		ldstr "class G3_C1720`1<class Generated1248.BaseClass0> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>
		callvirt instance string class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>::ClassMethod2750<object>()
		ldstr "G3_C1720::ClassMethod2750.MI.17635<System.Object>()"
		ldstr "class G3_C1720`1<class Generated1248.BaseClass0> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>
		callvirt instance string class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>::ClassMethod2749()
		ldstr "G3_C1720::ClassMethod2749.MI.17634()"
		ldstr "class G3_C1720`1<class Generated1248.BaseClass0> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>
		callvirt instance string class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>::ClassMethod2748()
		ldstr "G3_C1720::ClassMethod2748.MI.17633()"
		ldstr "class G3_C1720`1<class Generated1248.BaseClass0> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>
		callvirt instance string class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>::Method6<object>()
		ldstr "G2_C693::Method6.11390<System.Object>()"
		ldstr "class G3_C1720`1<class Generated1248.BaseClass0> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>
		callvirt instance string class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>::Method5()
		ldstr "G2_C693::Method5.11388()"
		ldstr "class G3_C1720`1<class Generated1248.BaseClass0> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>
		callvirt instance string class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>::Method4()
		ldstr "G2_C693::Method4.11387()"
		ldstr "class G3_C1720`1<class Generated1248.BaseClass0> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>
		callvirt instance string class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>::ClassMethod1349<object>()
		ldstr "G1_C13::ClassMethod1349.4877<System.Object>()"
		ldstr "class G3_C1720`1<class Generated1248.BaseClass0> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>
		callvirt instance string class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>::ClassMethod1348<object>()
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()"
		ldstr "class G3_C1720`1<class Generated1248.BaseClass0> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		callvirt instance string class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::ClassMethod1349<object>()
		ldstr "G1_C13::ClassMethod1349.4877<System.Object>()"
		ldstr "class G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		callvirt instance string class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::ClassMethod1348<object>()
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()"
		ldstr "class G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		callvirt instance string class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::Method6<object>()
		ldstr "G1_C13::Method6.4875<System.Object>()"
		ldstr "class G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		callvirt instance string class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::Method5()
		ldstr "G1_C13::Method5.4873()"
		ldstr "class G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		callvirt instance string class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::Method4()
		ldstr "G1_C13::Method4.4872()"
		ldstr "class G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		callvirt instance string class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::Method7<object>()
		ldstr "G2_C693::Method7.11391<System.Object>()"
		ldstr "class G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1248.IBase2`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::Method7<object>()
		ldstr "G3_C1720::Method7.MI.17629<System.Object>()"
		ldstr "class IBase2`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1248.IBase1`1<class Generated1248.BaseClass0>::Method4()
		ldstr "G2_C693::Method4.11387()"
		ldstr "class IBase1`1<class Generated1248.BaseClass0> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1248.IBase1`1<class Generated1248.BaseClass0>::Method5()
		ldstr "G2_C693::Method5.MI.11389()"
		ldstr "class IBase1`1<class Generated1248.BaseClass0> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1248.IBase1`1<class Generated1248.BaseClass0>::Method6<object>()
		ldstr "G2_C693::Method6.11390<System.Object>()"
		ldstr "class IBase1`1<class Generated1248.BaseClass0> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		callvirt instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::ClassMethod2751<object>()
		ldstr "G3_C1720::ClassMethod2751.MI.17636<System.Object>()"
		ldstr "class G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		callvirt instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::ClassMethod2750<object>()
		ldstr "G3_C1720::ClassMethod2750.MI.17635<System.Object>()"
		ldstr "class G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		callvirt instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::ClassMethod2749()
		ldstr "G3_C1720::ClassMethod2749.MI.17634()"
		ldstr "class G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		callvirt instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::ClassMethod2748()
		ldstr "G3_C1720::ClassMethod2748.MI.17633()"
		ldstr "class G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		callvirt instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::Method7<object>()
		ldstr "G2_C693::Method7.11391<System.Object>()"
		ldstr "class G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		callvirt instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::Method6<object>()
		ldstr "G2_C693::Method6.11390<System.Object>()"
		ldstr "class G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		callvirt instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::Method5()
		ldstr "G2_C693::Method5.11388()"
		ldstr "class G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		callvirt instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::Method4()
		ldstr "G2_C693::Method4.11387()"
		ldstr "class G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		callvirt instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::ClassMethod1349<object>()
		ldstr "G1_C13::ClassMethod1349.4877<System.Object>()"
		ldstr "class G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		callvirt instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::ClassMethod1348<object>()
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()"
		ldstr "class G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1248.IBase2`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>::Method7<object>()
		ldstr "G2_C693::Method7.11391<System.Object>()"
		ldstr "class IBase2`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1248.IBase2`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>::Method7<object>()
		ldstr "G2_C693::Method7.11391<System.Object>()"
		ldstr "class IBase2`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1248.IBase2`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>::Method7<object>()
		ldstr "G2_C693::Method7.11391<System.Object>()"
		ldstr "class IBase2`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>
		callvirt instance string class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>::ClassMethod4841<object>()
		ldstr "G3_C1720::ClassMethod4841.17632<System.Object>()"
		ldstr "class G3_C1720`1<class Generated1248.BaseClass1> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>
		callvirt instance string class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>::ClassMethod4840()
		ldstr "G3_C1720::ClassMethod4840.17631()"
		ldstr "class G3_C1720`1<class Generated1248.BaseClass1> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>
		callvirt instance string class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>::ClassMethod4839()
		ldstr "G3_C1720::ClassMethod4839.17630()"
		ldstr "class G3_C1720`1<class Generated1248.BaseClass1> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>
		callvirt instance string class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>::Method7<object>()
		ldstr "G3_C1720::Method7.17628<System.Object>()"
		ldstr "class G3_C1720`1<class Generated1248.BaseClass1> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>
		callvirt instance string class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>::ClassMethod2751<object>()
		ldstr "G3_C1720::ClassMethod2751.MI.17636<System.Object>()"
		ldstr "class G3_C1720`1<class Generated1248.BaseClass1> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>
		callvirt instance string class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>::ClassMethod2750<object>()
		ldstr "G3_C1720::ClassMethod2750.MI.17635<System.Object>()"
		ldstr "class G3_C1720`1<class Generated1248.BaseClass1> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>
		callvirt instance string class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>::ClassMethod2749()
		ldstr "G3_C1720::ClassMethod2749.MI.17634()"
		ldstr "class G3_C1720`1<class Generated1248.BaseClass1> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>
		callvirt instance string class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>::ClassMethod2748()
		ldstr "G3_C1720::ClassMethod2748.MI.17633()"
		ldstr "class G3_C1720`1<class Generated1248.BaseClass1> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>
		callvirt instance string class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>::Method6<object>()
		ldstr "G2_C693::Method6.11390<System.Object>()"
		ldstr "class G3_C1720`1<class Generated1248.BaseClass1> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>
		callvirt instance string class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>::Method5()
		ldstr "G2_C693::Method5.11388()"
		ldstr "class G3_C1720`1<class Generated1248.BaseClass1> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>
		callvirt instance string class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>::Method4()
		ldstr "G2_C693::Method4.11387()"
		ldstr "class G3_C1720`1<class Generated1248.BaseClass1> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>
		callvirt instance string class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>::ClassMethod1349<object>()
		ldstr "G1_C13::ClassMethod1349.4877<System.Object>()"
		ldstr "class G3_C1720`1<class Generated1248.BaseClass1> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>
		callvirt instance string class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>::ClassMethod1348<object>()
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()"
		ldstr "class G3_C1720`1<class Generated1248.BaseClass1> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		callvirt instance string class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::ClassMethod1349<object>()
		ldstr "G1_C13::ClassMethod1349.4877<System.Object>()"
		ldstr "class G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		callvirt instance string class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::ClassMethod1348<object>()
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()"
		ldstr "class G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		callvirt instance string class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::Method6<object>()
		ldstr "G1_C13::Method6.4875<System.Object>()"
		ldstr "class G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		callvirt instance string class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::Method5()
		ldstr "G1_C13::Method5.4873()"
		ldstr "class G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		callvirt instance string class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::Method4()
		ldstr "G1_C13::Method4.4872()"
		ldstr "class G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		callvirt instance string class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::Method7<object>()
		ldstr "G2_C693::Method7.11391<System.Object>()"
		ldstr "class G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1248.IBase2`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::Method7<object>()
		ldstr "G2_C693::Method7.11391<System.Object>()"
		ldstr "class IBase2`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1248.IBase1`1<class Generated1248.BaseClass0>::Method4()
		ldstr "G2_C693::Method4.11387()"
		ldstr "class IBase1`1<class Generated1248.BaseClass0> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1248.IBase1`1<class Generated1248.BaseClass0>::Method5()
		ldstr "G2_C693::Method5.MI.11389()"
		ldstr "class IBase1`1<class Generated1248.BaseClass0> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1248.IBase1`1<class Generated1248.BaseClass0>::Method6<object>()
		ldstr "G2_C693::Method6.11390<System.Object>()"
		ldstr "class IBase1`1<class Generated1248.BaseClass0> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>
		callvirt instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>::ClassMethod2751<object>()
		ldstr "G2_C693::ClassMethod2751.11395<System.Object>()"
		ldstr "class G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>
		callvirt instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>::ClassMethod2750<object>()
		ldstr "G2_C693::ClassMethod2750.11394<System.Object>()"
		ldstr "class G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>
		callvirt instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>::ClassMethod2749()
		ldstr "G2_C693::ClassMethod2749.11393()"
		ldstr "class G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>
		callvirt instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>::ClassMethod2748()
		ldstr "G2_C693::ClassMethod2748.11392()"
		ldstr "class G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>
		callvirt instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>::Method7<object>()
		ldstr "G2_C693::Method7.11391<System.Object>()"
		ldstr "class G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>
		callvirt instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>::Method6<object>()
		ldstr "G2_C693::Method6.11390<System.Object>()"
		ldstr "class G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>
		callvirt instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>::Method5()
		ldstr "G2_C693::Method5.11388()"
		ldstr "class G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>
		callvirt instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>::Method4()
		ldstr "G2_C693::Method4.11387()"
		ldstr "class G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>
		callvirt instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>::ClassMethod1349<object>()
		ldstr "G1_C13::ClassMethod1349.4877<System.Object>()"
		ldstr "class G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>
		callvirt instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>::ClassMethod1348<object>()
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()"
		ldstr "class G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1248.IBase2`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>::Method7<object>()
		ldstr "G2_C693::Method7.11391<System.Object>()"
		ldstr "class IBase2`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1248.IBase2`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>::Method7<object>()
		ldstr "G2_C693::Method7.11391<System.Object>()"
		ldstr "class IBase2`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1248.IBase2`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>::Method7<object>()
		ldstr "G2_C693::Method7.11391<System.Object>()"
		ldstr "class IBase2`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		callvirt instance string class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::ClassMethod1349<object>()
		ldstr "G1_C13::ClassMethod1349.4877<System.Object>()"
		ldstr "class G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		callvirt instance string class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::ClassMethod1348<object>()
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()"
		ldstr "class G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		callvirt instance string class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::Method6<object>()
		ldstr "G1_C13::Method6.4875<System.Object>()"
		ldstr "class G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		callvirt instance string class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::Method5()
		ldstr "G1_C13::Method5.4873()"
		ldstr "class G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		callvirt instance string class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::Method4()
		ldstr "G1_C13::Method4.4872()"
		ldstr "class G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		callvirt instance string class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::Method7<object>()
		ldstr "G2_C693::Method7.11391<System.Object>()"
		ldstr "class G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1248.IBase2`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::Method7<object>()
		ldstr "G2_C693::Method7.11391<System.Object>()"
		ldstr "class IBase2`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1248.IBase1`1<class Generated1248.BaseClass0>::Method4()
		ldstr "G2_C693::Method4.11387()"
		ldstr "class IBase1`1<class Generated1248.BaseClass0> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1248.IBase1`1<class Generated1248.BaseClass0>::Method5()
		ldstr "G2_C693::Method5.MI.11389()"
		ldstr "class IBase1`1<class Generated1248.BaseClass0> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1248.IBase1`1<class Generated1248.BaseClass0>::Method6<object>()
		ldstr "G2_C693::Method6.11390<System.Object>()"
		ldstr "class IBase1`1<class Generated1248.BaseClass0> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		callvirt instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::ClassMethod2751<object>()
		ldstr "G2_C693::ClassMethod2751.11395<System.Object>()"
		ldstr "class G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		callvirt instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::ClassMethod2750<object>()
		ldstr "G2_C693::ClassMethod2750.11394<System.Object>()"
		ldstr "class G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		callvirt instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::ClassMethod2749()
		ldstr "G2_C693::ClassMethod2749.11393()"
		ldstr "class G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		callvirt instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::ClassMethod2748()
		ldstr "G2_C693::ClassMethod2748.11392()"
		ldstr "class G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		callvirt instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::Method7<object>()
		ldstr "G2_C693::Method7.11391<System.Object>()"
		ldstr "class G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		callvirt instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::Method6<object>()
		ldstr "G2_C693::Method6.11390<System.Object>()"
		ldstr "class G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		callvirt instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::Method5()
		ldstr "G2_C693::Method5.11388()"
		ldstr "class G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		callvirt instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::Method4()
		ldstr "G2_C693::Method4.11387()"
		ldstr "class G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		callvirt instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::ClassMethod1349<object>()
		ldstr "G1_C13::ClassMethod1349.4877<System.Object>()"
		ldstr "class G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		callvirt instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::ClassMethod1348<object>()
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()"
		ldstr "class G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1248.IBase2`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>::Method7<object>()
		ldstr "G2_C693::Method7.11391<System.Object>()"
		ldstr "class IBase2`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1248.IBase2`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>::Method7<object>()
		ldstr "G2_C693::Method7.11391<System.Object>()"
		ldstr "class IBase2`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1248.IBase2`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>::Method7<object>()
		ldstr "G2_C693::Method7.11391<System.Object>()"
		ldstr "class IBase2`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		callvirt instance string class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::ClassMethod1349<object>()
		ldstr "G1_C13::ClassMethod1349.4877<System.Object>()"
		ldstr "class G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		callvirt instance string class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::ClassMethod1348<object>()
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()"
		ldstr "class G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		callvirt instance string class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::Method6<object>()
		ldstr "G1_C13::Method6.4875<System.Object>()"
		ldstr "class G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		callvirt instance string class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::Method5()
		ldstr "G1_C13::Method5.4873()"
		ldstr "class G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		callvirt instance string class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::Method4()
		ldstr "G1_C13::Method4.4872()"
		ldstr "class G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		callvirt instance string class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::Method7<object>()
		ldstr "G2_C693::Method7.11391<System.Object>()"
		ldstr "class G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1248.IBase2`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::Method7<object>()
		ldstr "G2_C693::Method7.11391<System.Object>()"
		ldstr "class IBase2`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1248.IBase1`1<class Generated1248.BaseClass0>::Method4()
		ldstr "G2_C693::Method4.11387()"
		ldstr "class IBase1`1<class Generated1248.BaseClass0> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1248.IBase1`1<class Generated1248.BaseClass0>::Method5()
		ldstr "G2_C693::Method5.MI.11389()"
		ldstr "class IBase1`1<class Generated1248.BaseClass0> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1248.IBase1`1<class Generated1248.BaseClass0>::Method6<object>()
		ldstr "G2_C693::Method6.11390<System.Object>()"
		ldstr "class IBase1`1<class Generated1248.BaseClass0> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>
		callvirt instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>::ClassMethod2751<object>()
		ldstr "G2_C693::ClassMethod2751.11395<System.Object>()"
		ldstr "class G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>
		callvirt instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>::ClassMethod2750<object>()
		ldstr "G2_C693::ClassMethod2750.11394<System.Object>()"
		ldstr "class G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>
		callvirt instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>::ClassMethod2749()
		ldstr "G2_C693::ClassMethod2749.11393()"
		ldstr "class G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>
		callvirt instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>::ClassMethod2748()
		ldstr "G2_C693::ClassMethod2748.11392()"
		ldstr "class G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>
		callvirt instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>::Method7<object>()
		ldstr "G2_C693::Method7.11391<System.Object>()"
		ldstr "class G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>
		callvirt instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>::Method6<object>()
		ldstr "G2_C693::Method6.11390<System.Object>()"
		ldstr "class G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>
		callvirt instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>::Method5()
		ldstr "G2_C693::Method5.11388()"
		ldstr "class G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>
		callvirt instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>::Method4()
		ldstr "G2_C693::Method4.11387()"
		ldstr "class G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>
		callvirt instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>::ClassMethod1349<object>()
		ldstr "G1_C13::ClassMethod1349.4877<System.Object>()"
		ldstr "class G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>
		callvirt instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>::ClassMethod1348<object>()
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()"
		ldstr "class G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1248.IBase2`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>::Method7<object>()
		ldstr "G2_C693::Method7.11391<System.Object>()"
		ldstr "class IBase2`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		callvirt instance string class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::ClassMethod1349<object>()
		ldstr "G1_C13::ClassMethod1349.4877<System.Object>()"
		ldstr "class G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		callvirt instance string class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::ClassMethod1348<object>()
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()"
		ldstr "class G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		callvirt instance string class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::Method6<object>()
		ldstr "G1_C13::Method6.4875<System.Object>()"
		ldstr "class G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		callvirt instance string class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::Method5()
		ldstr "G1_C13::Method5.4873()"
		ldstr "class G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		callvirt instance string class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::Method4()
		ldstr "G1_C13::Method4.4872()"
		ldstr "class G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		callvirt instance string class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::Method7<object>()
		ldstr "G2_C693::Method7.11391<System.Object>()"
		ldstr "class G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1248.IBase2`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::Method7<object>()
		ldstr "G2_C693::Method7.11391<System.Object>()"
		ldstr "class IBase2`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1248.IBase1`1<class Generated1248.BaseClass0>::Method4()
		ldstr "G2_C693::Method4.11387()"
		ldstr "class IBase1`1<class Generated1248.BaseClass0> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1248.IBase1`1<class Generated1248.BaseClass0>::Method5()
		ldstr "G2_C693::Method5.MI.11389()"
		ldstr "class IBase1`1<class Generated1248.BaseClass0> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1248.IBase1`1<class Generated1248.BaseClass0>::Method6<object>()
		ldstr "G2_C693::Method6.11390<System.Object>()"
		ldstr "class IBase1`1<class Generated1248.BaseClass0> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>
		callvirt instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>::ClassMethod2751<object>()
		ldstr "G2_C693::ClassMethod2751.11395<System.Object>()"
		ldstr "class G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>
		callvirt instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>::ClassMethod2750<object>()
		ldstr "G2_C693::ClassMethod2750.11394<System.Object>()"
		ldstr "class G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>
		callvirt instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>::ClassMethod2749()
		ldstr "G2_C693::ClassMethod2749.11393()"
		ldstr "class G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>
		callvirt instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>::ClassMethod2748()
		ldstr "G2_C693::ClassMethod2748.11392()"
		ldstr "class G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>
		callvirt instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>::Method7<object>()
		ldstr "G2_C693::Method7.11391<System.Object>()"
		ldstr "class G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>
		callvirt instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>::Method6<object>()
		ldstr "G2_C693::Method6.11390<System.Object>()"
		ldstr "class G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>
		callvirt instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>::Method5()
		ldstr "G2_C693::Method5.11388()"
		ldstr "class G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>
		callvirt instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>::Method4()
		ldstr "G2_C693::Method4.11387()"
		ldstr "class G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>
		callvirt instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>::ClassMethod1349<object>()
		ldstr "G1_C13::ClassMethod1349.4877<System.Object>()"
		ldstr "class G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>
		callvirt instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>::ClassMethod1348<object>()
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()"
		ldstr "class G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1248.IBase2`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>::Method7<object>()
		ldstr "G2_C693::Method7.11391<System.Object>()"
		ldstr "class IBase2`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>"
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
		newobj instance void class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G1_C13::Method4.4872()#G1_C13::Method5.4873()#G1_C13::Method6.4875<System.Object>()#G2_C693::Method7.11391<System.Object>()#"
		call void Generated1248.Generated1248::M.G1_C13.T.T<class Generated1248.BaseClass0,class Generated1248.BaseClass1,class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G1_C13::Method4.4872()#G1_C13::Method5.4873()#G1_C13::Method6.4875<System.Object>()#G2_C693::Method7.11391<System.Object>()#"
		call void Generated1248.Generated1248::M.G1_C13.A.T<class Generated1248.BaseClass1,class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G1_C13::Method4.4872()#G1_C13::Method5.4873()#G1_C13::Method6.4875<System.Object>()#G2_C693::Method7.11391<System.Object>()#"
		call void Generated1248.Generated1248::M.G1_C13.A.B<class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1720::Method7.MI.17629<System.Object>()#"
		call void Generated1248.Generated1248::M.IBase2.T.T<class Generated1248.BaseClass0,class Generated1248.BaseClass1,class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G3_C1720::Method7.MI.17629<System.Object>()#"
		call void Generated1248.Generated1248::M.IBase2.A.T<class Generated1248.BaseClass1,class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G3_C1720::Method7.MI.17629<System.Object>()#"
		call void Generated1248.Generated1248::M.IBase2.A.B<class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C693::Method4.11387()#G2_C693::Method5.MI.11389()#G2_C693::Method6.11390<System.Object>()#"
		call void Generated1248.Generated1248::M.IBase1.T<class Generated1248.BaseClass0,class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C693::Method4.11387()#G2_C693::Method5.MI.11389()#G2_C693::Method6.11390<System.Object>()#"
		call void Generated1248.Generated1248::M.IBase1.A<class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G3_C1720::ClassMethod2748.MI.17633()#G3_C1720::ClassMethod2749.MI.17634()#G3_C1720::ClassMethod2750.MI.17635<System.Object>()#G3_C1720::ClassMethod2751.MI.17636<System.Object>()#G2_C693::Method4.11387()#G2_C693::Method5.11388()#G2_C693::Method6.11390<System.Object>()#G2_C693::Method7.11391<System.Object>()#"
		call void Generated1248.Generated1248::M.G2_C693.T.T<class Generated1248.BaseClass0,class Generated1248.BaseClass1,class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G3_C1720::ClassMethod2748.MI.17633()#G3_C1720::ClassMethod2749.MI.17634()#G3_C1720::ClassMethod2750.MI.17635<System.Object>()#G3_C1720::ClassMethod2751.MI.17636<System.Object>()#G2_C693::Method4.11387()#G2_C693::Method5.11388()#G2_C693::Method6.11390<System.Object>()#G2_C693::Method7.11391<System.Object>()#"
		call void Generated1248.Generated1248::M.G2_C693.A.T<class Generated1248.BaseClass1,class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G3_C1720::ClassMethod2748.MI.17633()#G3_C1720::ClassMethod2749.MI.17634()#G3_C1720::ClassMethod2750.MI.17635<System.Object>()#G3_C1720::ClassMethod2751.MI.17636<System.Object>()#G2_C693::Method4.11387()#G2_C693::Method5.11388()#G2_C693::Method6.11390<System.Object>()#G2_C693::Method7.11391<System.Object>()#"
		call void Generated1248.Generated1248::M.G2_C693.A.B<class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C693::Method7.11391<System.Object>()#"
		call void Generated1248.Generated1248::M.IBase2.T.T<class Generated1248.BaseClass1,class Generated1248.BaseClass0,class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C693::Method7.11391<System.Object>()#"
		call void Generated1248.Generated1248::M.IBase2.B.T<class Generated1248.BaseClass0,class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C693::Method7.11391<System.Object>()#"
		call void Generated1248.Generated1248::M.IBase2.B.A<class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1720::Method7.MI.17629<System.Object>()#"
		call void Generated1248.Generated1248::M.IBase2.T.T<class Generated1248.BaseClass0,class Generated1248.BaseClass0,class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G3_C1720::Method7.MI.17629<System.Object>()#"
		call void Generated1248.Generated1248::M.IBase2.A.T<class Generated1248.BaseClass0,class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G3_C1720::Method7.MI.17629<System.Object>()#"
		call void Generated1248.Generated1248::M.IBase2.A.A<class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C693::Method7.11391<System.Object>()#"
		call void Generated1248.Generated1248::M.IBase2.T.T<class Generated1248.BaseClass1,class Generated1248.BaseClass1,class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C693::Method7.11391<System.Object>()#"
		call void Generated1248.Generated1248::M.IBase2.B.T<class Generated1248.BaseClass1,class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C693::Method7.11391<System.Object>()#"
		call void Generated1248.Generated1248::M.IBase2.B.B<class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G3_C1720::ClassMethod2748.MI.17633()#G3_C1720::ClassMethod2749.MI.17634()#G3_C1720::ClassMethod2750.MI.17635<System.Object>()#G3_C1720::ClassMethod2751.MI.17636<System.Object>()#G3_C1720::ClassMethod4839.17630()#G3_C1720::ClassMethod4840.17631()#G3_C1720::ClassMethod4841.17632<System.Object>()#G2_C693::Method4.11387()#G2_C693::Method5.11388()#G2_C693::Method6.11390<System.Object>()#G3_C1720::Method7.17628<System.Object>()#"
		call void Generated1248.Generated1248::M.G3_C1720.T<class Generated1248.BaseClass0,class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G3_C1720::ClassMethod2748.MI.17633()#G3_C1720::ClassMethod2749.MI.17634()#G3_C1720::ClassMethod2750.MI.17635<System.Object>()#G3_C1720::ClassMethod2751.MI.17636<System.Object>()#G3_C1720::ClassMethod4839.17630()#G3_C1720::ClassMethod4840.17631()#G3_C1720::ClassMethod4841.17632<System.Object>()#G2_C693::Method4.11387()#G2_C693::Method5.11388()#G2_C693::Method6.11390<System.Object>()#G3_C1720::Method7.17628<System.Object>()#"
		call void Generated1248.Generated1248::M.G3_C1720.A<class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>>(!!0,string)
		newobj instance void class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G1_C13::Method4.4872()#G1_C13::Method5.4873()#G1_C13::Method6.4875<System.Object>()#G2_C693::Method7.11391<System.Object>()#"
		call void Generated1248.Generated1248::M.G1_C13.T.T<class Generated1248.BaseClass0,class Generated1248.BaseClass1,class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G1_C13::Method4.4872()#G1_C13::Method5.4873()#G1_C13::Method6.4875<System.Object>()#G2_C693::Method7.11391<System.Object>()#"
		call void Generated1248.Generated1248::M.G1_C13.A.T<class Generated1248.BaseClass1,class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G1_C13::Method4.4872()#G1_C13::Method5.4873()#G1_C13::Method6.4875<System.Object>()#G2_C693::Method7.11391<System.Object>()#"
		call void Generated1248.Generated1248::M.G1_C13.A.B<class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1720::Method7.MI.17629<System.Object>()#"
		call void Generated1248.Generated1248::M.IBase2.T.T<class Generated1248.BaseClass0,class Generated1248.BaseClass1,class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G3_C1720::Method7.MI.17629<System.Object>()#"
		call void Generated1248.Generated1248::M.IBase2.A.T<class Generated1248.BaseClass1,class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1720::Method7.MI.17629<System.Object>()#"
		call void Generated1248.Generated1248::M.IBase2.A.B<class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C693::Method4.11387()#G2_C693::Method5.MI.11389()#G2_C693::Method6.11390<System.Object>()#"
		call void Generated1248.Generated1248::M.IBase1.T<class Generated1248.BaseClass0,class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C693::Method4.11387()#G2_C693::Method5.MI.11389()#G2_C693::Method6.11390<System.Object>()#"
		call void Generated1248.Generated1248::M.IBase1.A<class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G3_C1720::ClassMethod2748.MI.17633()#G3_C1720::ClassMethod2749.MI.17634()#G3_C1720::ClassMethod2750.MI.17635<System.Object>()#G3_C1720::ClassMethod2751.MI.17636<System.Object>()#G2_C693::Method4.11387()#G2_C693::Method5.11388()#G2_C693::Method6.11390<System.Object>()#G2_C693::Method7.11391<System.Object>()#"
		call void Generated1248.Generated1248::M.G2_C693.T.T<class Generated1248.BaseClass0,class Generated1248.BaseClass1,class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G3_C1720::ClassMethod2748.MI.17633()#G3_C1720::ClassMethod2749.MI.17634()#G3_C1720::ClassMethod2750.MI.17635<System.Object>()#G3_C1720::ClassMethod2751.MI.17636<System.Object>()#G2_C693::Method4.11387()#G2_C693::Method5.11388()#G2_C693::Method6.11390<System.Object>()#G2_C693::Method7.11391<System.Object>()#"
		call void Generated1248.Generated1248::M.G2_C693.A.T<class Generated1248.BaseClass1,class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G3_C1720::ClassMethod2748.MI.17633()#G3_C1720::ClassMethod2749.MI.17634()#G3_C1720::ClassMethod2750.MI.17635<System.Object>()#G3_C1720::ClassMethod2751.MI.17636<System.Object>()#G2_C693::Method4.11387()#G2_C693::Method5.11388()#G2_C693::Method6.11390<System.Object>()#G2_C693::Method7.11391<System.Object>()#"
		call void Generated1248.Generated1248::M.G2_C693.A.B<class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C693::Method7.11391<System.Object>()#"
		call void Generated1248.Generated1248::M.IBase2.T.T<class Generated1248.BaseClass1,class Generated1248.BaseClass0,class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C693::Method7.11391<System.Object>()#"
		call void Generated1248.Generated1248::M.IBase2.B.T<class Generated1248.BaseClass0,class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C693::Method7.11391<System.Object>()#"
		call void Generated1248.Generated1248::M.IBase2.B.A<class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C693::Method7.11391<System.Object>()#"
		call void Generated1248.Generated1248::M.IBase2.T.T<class Generated1248.BaseClass0,class Generated1248.BaseClass0,class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C693::Method7.11391<System.Object>()#"
		call void Generated1248.Generated1248::M.IBase2.A.T<class Generated1248.BaseClass0,class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C693::Method7.11391<System.Object>()#"
		call void Generated1248.Generated1248::M.IBase2.A.A<class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C693::Method7.11391<System.Object>()#"
		call void Generated1248.Generated1248::M.IBase2.T.T<class Generated1248.BaseClass1,class Generated1248.BaseClass1,class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C693::Method7.11391<System.Object>()#"
		call void Generated1248.Generated1248::M.IBase2.B.T<class Generated1248.BaseClass1,class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C693::Method7.11391<System.Object>()#"
		call void Generated1248.Generated1248::M.IBase2.B.B<class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G3_C1720::ClassMethod2748.MI.17633()#G3_C1720::ClassMethod2749.MI.17634()#G3_C1720::ClassMethod2750.MI.17635<System.Object>()#G3_C1720::ClassMethod2751.MI.17636<System.Object>()#G3_C1720::ClassMethod4839.17630()#G3_C1720::ClassMethod4840.17631()#G3_C1720::ClassMethod4841.17632<System.Object>()#G2_C693::Method4.11387()#G2_C693::Method5.11388()#G2_C693::Method6.11390<System.Object>()#G3_C1720::Method7.17628<System.Object>()#"
		call void Generated1248.Generated1248::M.G3_C1720.T<class Generated1248.BaseClass1,class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G3_C1720::ClassMethod2748.MI.17633()#G3_C1720::ClassMethod2749.MI.17634()#G3_C1720::ClassMethod2750.MI.17635<System.Object>()#G3_C1720::ClassMethod2751.MI.17636<System.Object>()#G3_C1720::ClassMethod4839.17630()#G3_C1720::ClassMethod4840.17631()#G3_C1720::ClassMethod4841.17632<System.Object>()#G2_C693::Method4.11387()#G2_C693::Method5.11388()#G2_C693::Method6.11390<System.Object>()#G3_C1720::Method7.17628<System.Object>()#"
		call void Generated1248.Generated1248::M.G3_C1720.B<class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>>(!!0,string)
		newobj instance void class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G1_C13::Method4.4872()#G1_C13::Method5.4873()#G1_C13::Method6.4875<System.Object>()#G2_C693::Method7.11391<System.Object>()#"
		call void Generated1248.Generated1248::M.G1_C13.T.T<class Generated1248.BaseClass0,class Generated1248.BaseClass1,class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G1_C13::Method4.4872()#G1_C13::Method5.4873()#G1_C13::Method6.4875<System.Object>()#G2_C693::Method7.11391<System.Object>()#"
		call void Generated1248.Generated1248::M.G1_C13.A.T<class Generated1248.BaseClass1,class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G1_C13::Method4.4872()#G1_C13::Method5.4873()#G1_C13::Method6.4875<System.Object>()#G2_C693::Method7.11391<System.Object>()#"
		call void Generated1248.Generated1248::M.G1_C13.A.B<class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C693::Method7.11391<System.Object>()#"
		call void Generated1248.Generated1248::M.IBase2.T.T<class Generated1248.BaseClass0,class Generated1248.BaseClass1,class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C693::Method7.11391<System.Object>()#"
		call void Generated1248.Generated1248::M.IBase2.A.T<class Generated1248.BaseClass1,class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C693::Method7.11391<System.Object>()#"
		call void Generated1248.Generated1248::M.IBase2.A.B<class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C693::Method4.11387()#G2_C693::Method5.MI.11389()#G2_C693::Method6.11390<System.Object>()#"
		call void Generated1248.Generated1248::M.IBase1.T<class Generated1248.BaseClass0,class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C693::Method4.11387()#G2_C693::Method5.MI.11389()#G2_C693::Method6.11390<System.Object>()#"
		call void Generated1248.Generated1248::M.IBase1.A<class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G2_C693::ClassMethod2748.11392()#G2_C693::ClassMethod2749.11393()#G2_C693::ClassMethod2750.11394<System.Object>()#G2_C693::ClassMethod2751.11395<System.Object>()#G2_C693::Method4.11387()#G2_C693::Method5.11388()#G2_C693::Method6.11390<System.Object>()#G2_C693::Method7.11391<System.Object>()#"
		call void Generated1248.Generated1248::M.G2_C693.T.T<class Generated1248.BaseClass0,class Generated1248.BaseClass0,class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G2_C693::ClassMethod2748.11392()#G2_C693::ClassMethod2749.11393()#G2_C693::ClassMethod2750.11394<System.Object>()#G2_C693::ClassMethod2751.11395<System.Object>()#G2_C693::Method4.11387()#G2_C693::Method5.11388()#G2_C693::Method6.11390<System.Object>()#G2_C693::Method7.11391<System.Object>()#"
		call void Generated1248.Generated1248::M.G2_C693.A.T<class Generated1248.BaseClass0,class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G2_C693::ClassMethod2748.11392()#G2_C693::ClassMethod2749.11393()#G2_C693::ClassMethod2750.11394<System.Object>()#G2_C693::ClassMethod2751.11395<System.Object>()#G2_C693::Method4.11387()#G2_C693::Method5.11388()#G2_C693::Method6.11390<System.Object>()#G2_C693::Method7.11391<System.Object>()#"
		call void Generated1248.Generated1248::M.G2_C693.A.A<class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C693::Method7.11391<System.Object>()#"
		call void Generated1248.Generated1248::M.IBase2.T.T<class Generated1248.BaseClass1,class Generated1248.BaseClass0,class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C693::Method7.11391<System.Object>()#"
		call void Generated1248.Generated1248::M.IBase2.B.T<class Generated1248.BaseClass0,class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C693::Method7.11391<System.Object>()#"
		call void Generated1248.Generated1248::M.IBase2.B.A<class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C693::Method7.11391<System.Object>()#"
		call void Generated1248.Generated1248::M.IBase2.T.T<class Generated1248.BaseClass0,class Generated1248.BaseClass0,class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C693::Method7.11391<System.Object>()#"
		call void Generated1248.Generated1248::M.IBase2.A.T<class Generated1248.BaseClass0,class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C693::Method7.11391<System.Object>()#"
		call void Generated1248.Generated1248::M.IBase2.A.A<class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C693::Method7.11391<System.Object>()#"
		call void Generated1248.Generated1248::M.IBase2.T.T<class Generated1248.BaseClass1,class Generated1248.BaseClass1,class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C693::Method7.11391<System.Object>()#"
		call void Generated1248.Generated1248::M.IBase2.B.T<class Generated1248.BaseClass1,class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C693::Method7.11391<System.Object>()#"
		call void Generated1248.Generated1248::M.IBase2.B.B<class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>>(!!0,string)
		newobj instance void class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G1_C13::Method4.4872()#G1_C13::Method5.4873()#G1_C13::Method6.4875<System.Object>()#G2_C693::Method7.11391<System.Object>()#"
		call void Generated1248.Generated1248::M.G1_C13.T.T<class Generated1248.BaseClass0,class Generated1248.BaseClass1,class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G1_C13::Method4.4872()#G1_C13::Method5.4873()#G1_C13::Method6.4875<System.Object>()#G2_C693::Method7.11391<System.Object>()#"
		call void Generated1248.Generated1248::M.G1_C13.A.T<class Generated1248.BaseClass1,class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G1_C13::Method4.4872()#G1_C13::Method5.4873()#G1_C13::Method6.4875<System.Object>()#G2_C693::Method7.11391<System.Object>()#"
		call void Generated1248.Generated1248::M.G1_C13.A.B<class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C693::Method7.11391<System.Object>()#"
		call void Generated1248.Generated1248::M.IBase2.T.T<class Generated1248.BaseClass0,class Generated1248.BaseClass1,class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C693::Method7.11391<System.Object>()#"
		call void Generated1248.Generated1248::M.IBase2.A.T<class Generated1248.BaseClass1,class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C693::Method7.11391<System.Object>()#"
		call void Generated1248.Generated1248::M.IBase2.A.B<class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C693::Method4.11387()#G2_C693::Method5.MI.11389()#G2_C693::Method6.11390<System.Object>()#"
		call void Generated1248.Generated1248::M.IBase1.T<class Generated1248.BaseClass0,class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C693::Method4.11387()#G2_C693::Method5.MI.11389()#G2_C693::Method6.11390<System.Object>()#"
		call void Generated1248.Generated1248::M.IBase1.A<class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G2_C693::ClassMethod2748.11392()#G2_C693::ClassMethod2749.11393()#G2_C693::ClassMethod2750.11394<System.Object>()#G2_C693::ClassMethod2751.11395<System.Object>()#G2_C693::Method4.11387()#G2_C693::Method5.11388()#G2_C693::Method6.11390<System.Object>()#G2_C693::Method7.11391<System.Object>()#"
		call void Generated1248.Generated1248::M.G2_C693.T.T<class Generated1248.BaseClass0,class Generated1248.BaseClass1,class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G2_C693::ClassMethod2748.11392()#G2_C693::ClassMethod2749.11393()#G2_C693::ClassMethod2750.11394<System.Object>()#G2_C693::ClassMethod2751.11395<System.Object>()#G2_C693::Method4.11387()#G2_C693::Method5.11388()#G2_C693::Method6.11390<System.Object>()#G2_C693::Method7.11391<System.Object>()#"
		call void Generated1248.Generated1248::M.G2_C693.A.T<class Generated1248.BaseClass1,class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G2_C693::ClassMethod2748.11392()#G2_C693::ClassMethod2749.11393()#G2_C693::ClassMethod2750.11394<System.Object>()#G2_C693::ClassMethod2751.11395<System.Object>()#G2_C693::Method4.11387()#G2_C693::Method5.11388()#G2_C693::Method6.11390<System.Object>()#G2_C693::Method7.11391<System.Object>()#"
		call void Generated1248.Generated1248::M.G2_C693.A.B<class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C693::Method7.11391<System.Object>()#"
		call void Generated1248.Generated1248::M.IBase2.T.T<class Generated1248.BaseClass1,class Generated1248.BaseClass0,class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C693::Method7.11391<System.Object>()#"
		call void Generated1248.Generated1248::M.IBase2.B.T<class Generated1248.BaseClass0,class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C693::Method7.11391<System.Object>()#"
		call void Generated1248.Generated1248::M.IBase2.B.A<class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C693::Method7.11391<System.Object>()#"
		call void Generated1248.Generated1248::M.IBase2.T.T<class Generated1248.BaseClass0,class Generated1248.BaseClass0,class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C693::Method7.11391<System.Object>()#"
		call void Generated1248.Generated1248::M.IBase2.A.T<class Generated1248.BaseClass0,class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C693::Method7.11391<System.Object>()#"
		call void Generated1248.Generated1248::M.IBase2.A.A<class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C693::Method7.11391<System.Object>()#"
		call void Generated1248.Generated1248::M.IBase2.T.T<class Generated1248.BaseClass1,class Generated1248.BaseClass1,class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C693::Method7.11391<System.Object>()#"
		call void Generated1248.Generated1248::M.IBase2.B.T<class Generated1248.BaseClass1,class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C693::Method7.11391<System.Object>()#"
		call void Generated1248.Generated1248::M.IBase2.B.B<class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>>(!!0,string)
		newobj instance void class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G1_C13::Method4.4872()#G1_C13::Method5.4873()#G1_C13::Method6.4875<System.Object>()#G2_C693::Method7.11391<System.Object>()#"
		call void Generated1248.Generated1248::M.G1_C13.T.T<class Generated1248.BaseClass0,class Generated1248.BaseClass1,class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G1_C13::Method4.4872()#G1_C13::Method5.4873()#G1_C13::Method6.4875<System.Object>()#G2_C693::Method7.11391<System.Object>()#"
		call void Generated1248.Generated1248::M.G1_C13.A.T<class Generated1248.BaseClass1,class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G1_C13::Method4.4872()#G1_C13::Method5.4873()#G1_C13::Method6.4875<System.Object>()#G2_C693::Method7.11391<System.Object>()#"
		call void Generated1248.Generated1248::M.G1_C13.A.B<class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C693::Method7.11391<System.Object>()#"
		call void Generated1248.Generated1248::M.IBase2.T.T<class Generated1248.BaseClass0,class Generated1248.BaseClass1,class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C693::Method7.11391<System.Object>()#"
		call void Generated1248.Generated1248::M.IBase2.A.T<class Generated1248.BaseClass1,class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C693::Method7.11391<System.Object>()#"
		call void Generated1248.Generated1248::M.IBase2.A.B<class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C693::Method4.11387()#G2_C693::Method5.MI.11389()#G2_C693::Method6.11390<System.Object>()#"
		call void Generated1248.Generated1248::M.IBase1.T<class Generated1248.BaseClass0,class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C693::Method4.11387()#G2_C693::Method5.MI.11389()#G2_C693::Method6.11390<System.Object>()#"
		call void Generated1248.Generated1248::M.IBase1.A<class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G2_C693::ClassMethod2748.11392()#G2_C693::ClassMethod2749.11393()#G2_C693::ClassMethod2750.11394<System.Object>()#G2_C693::ClassMethod2751.11395<System.Object>()#G2_C693::Method4.11387()#G2_C693::Method5.11388()#G2_C693::Method6.11390<System.Object>()#G2_C693::Method7.11391<System.Object>()#"
		call void Generated1248.Generated1248::M.G2_C693.T.T<class Generated1248.BaseClass1,class Generated1248.BaseClass0,class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G2_C693::ClassMethod2748.11392()#G2_C693::ClassMethod2749.11393()#G2_C693::ClassMethod2750.11394<System.Object>()#G2_C693::ClassMethod2751.11395<System.Object>()#G2_C693::Method4.11387()#G2_C693::Method5.11388()#G2_C693::Method6.11390<System.Object>()#G2_C693::Method7.11391<System.Object>()#"
		call void Generated1248.Generated1248::M.G2_C693.B.T<class Generated1248.BaseClass0,class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G2_C693::ClassMethod2748.11392()#G2_C693::ClassMethod2749.11393()#G2_C693::ClassMethod2750.11394<System.Object>()#G2_C693::ClassMethod2751.11395<System.Object>()#G2_C693::Method4.11387()#G2_C693::Method5.11388()#G2_C693::Method6.11390<System.Object>()#G2_C693::Method7.11391<System.Object>()#"
		call void Generated1248.Generated1248::M.G2_C693.B.A<class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C693::Method7.11391<System.Object>()#"
		call void Generated1248.Generated1248::M.IBase2.T.T<class Generated1248.BaseClass1,class Generated1248.BaseClass1,class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C693::Method7.11391<System.Object>()#"
		call void Generated1248.Generated1248::M.IBase2.B.T<class Generated1248.BaseClass1,class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C693::Method7.11391<System.Object>()#"
		call void Generated1248.Generated1248::M.IBase2.B.B<class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>>(!!0,string)
		newobj instance void class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G1_C13::Method4.4872()#G1_C13::Method5.4873()#G1_C13::Method6.4875<System.Object>()#G2_C693::Method7.11391<System.Object>()#"
		call void Generated1248.Generated1248::M.G1_C13.T.T<class Generated1248.BaseClass0,class Generated1248.BaseClass1,class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G1_C13::Method4.4872()#G1_C13::Method5.4873()#G1_C13::Method6.4875<System.Object>()#G2_C693::Method7.11391<System.Object>()#"
		call void Generated1248.Generated1248::M.G1_C13.A.T<class Generated1248.BaseClass1,class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G1_C13::Method4.4872()#G1_C13::Method5.4873()#G1_C13::Method6.4875<System.Object>()#G2_C693::Method7.11391<System.Object>()#"
		call void Generated1248.Generated1248::M.G1_C13.A.B<class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C693::Method7.11391<System.Object>()#"
		call void Generated1248.Generated1248::M.IBase2.T.T<class Generated1248.BaseClass0,class Generated1248.BaseClass1,class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C693::Method7.11391<System.Object>()#"
		call void Generated1248.Generated1248::M.IBase2.A.T<class Generated1248.BaseClass1,class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C693::Method7.11391<System.Object>()#"
		call void Generated1248.Generated1248::M.IBase2.A.B<class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C693::Method4.11387()#G2_C693::Method5.MI.11389()#G2_C693::Method6.11390<System.Object>()#"
		call void Generated1248.Generated1248::M.IBase1.T<class Generated1248.BaseClass0,class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C693::Method4.11387()#G2_C693::Method5.MI.11389()#G2_C693::Method6.11390<System.Object>()#"
		call void Generated1248.Generated1248::M.IBase1.A<class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G2_C693::ClassMethod2748.11392()#G2_C693::ClassMethod2749.11393()#G2_C693::ClassMethod2750.11394<System.Object>()#G2_C693::ClassMethod2751.11395<System.Object>()#G2_C693::Method4.11387()#G2_C693::Method5.11388()#G2_C693::Method6.11390<System.Object>()#G2_C693::Method7.11391<System.Object>()#"
		call void Generated1248.Generated1248::M.G2_C693.T.T<class Generated1248.BaseClass1,class Generated1248.BaseClass1,class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G2_C693::ClassMethod2748.11392()#G2_C693::ClassMethod2749.11393()#G2_C693::ClassMethod2750.11394<System.Object>()#G2_C693::ClassMethod2751.11395<System.Object>()#G2_C693::Method4.11387()#G2_C693::Method5.11388()#G2_C693::Method6.11390<System.Object>()#G2_C693::Method7.11391<System.Object>()#"
		call void Generated1248.Generated1248::M.G2_C693.B.T<class Generated1248.BaseClass1,class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G2_C693::ClassMethod2748.11392()#G2_C693::ClassMethod2749.11393()#G2_C693::ClassMethod2750.11394<System.Object>()#G2_C693::ClassMethod2751.11395<System.Object>()#G2_C693::Method4.11387()#G2_C693::Method5.11388()#G2_C693::Method6.11390<System.Object>()#G2_C693::Method7.11391<System.Object>()#"
		call void Generated1248.Generated1248::M.G2_C693.B.B<class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C693::Method7.11391<System.Object>()#"
		call void Generated1248.Generated1248::M.IBase2.T.T<class Generated1248.BaseClass1,class Generated1248.BaseClass1,class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C693::Method7.11391<System.Object>()#"
		call void Generated1248.Generated1248::M.IBase2.B.T<class Generated1248.BaseClass1,class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C693::Method7.11391<System.Object>()#"
		call void Generated1248.Generated1248::M.IBase2.B.B<class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>>(!!0,string)
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
		newobj instance void class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::ClassMethod1349<object>()
		calli default string(class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>)
		ldstr "G1_C13::ClassMethod1349.4877<System.Object>()"
		ldstr "class G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::ClassMethod1348<object>()
		calli default string(class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>)
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()"
		ldstr "class G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::Method6<object>()
		calli default string(class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>)
		ldstr "G1_C13::Method6.4875<System.Object>()"
		ldstr "class G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::Method5()
		calli default string(class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>)
		ldstr "G1_C13::Method5.4873()"
		ldstr "class G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::Method4()
		calli default string(class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>)
		ldstr "G1_C13::Method4.4872()"
		ldstr "class G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::Method7<object>()
		calli default string(class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>)
		ldstr "G2_C693::Method7.11391<System.Object>()"
		ldstr "class G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1248.IBase2`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::Method7<object>()
		calli default string(class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>)
		ldstr "G3_C1720::Method7.MI.17629<System.Object>()"
		ldstr "class IBase2`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1248.IBase1`1<class Generated1248.BaseClass0>::Method4()
		calli default string(class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>)
		ldstr "G2_C693::Method4.11387()"
		ldstr "class IBase1`1<class Generated1248.BaseClass0> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1248.IBase1`1<class Generated1248.BaseClass0>::Method5()
		calli default string(class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>)
		ldstr "G2_C693::Method5.MI.11389()"
		ldstr "class IBase1`1<class Generated1248.BaseClass0> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1248.IBase1`1<class Generated1248.BaseClass0>::Method6<object>()
		calli default string(class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>)
		ldstr "G2_C693::Method6.11390<System.Object>()"
		ldstr "class IBase1`1<class Generated1248.BaseClass0> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::ClassMethod2751<object>()
		calli default string(class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>)
		ldstr "G3_C1720::ClassMethod2751.MI.17636<System.Object>()"
		ldstr "class G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::ClassMethod2750<object>()
		calli default string(class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>)
		ldstr "G3_C1720::ClassMethod2750.MI.17635<System.Object>()"
		ldstr "class G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::ClassMethod2749()
		calli default string(class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>)
		ldstr "G3_C1720::ClassMethod2749.MI.17634()"
		ldstr "class G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::ClassMethod2748()
		calli default string(class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>)
		ldstr "G3_C1720::ClassMethod2748.MI.17633()"
		ldstr "class G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::Method7<object>()
		calli default string(class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>)
		ldstr "G2_C693::Method7.11391<System.Object>()"
		ldstr "class G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::Method6<object>()
		calli default string(class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>)
		ldstr "G2_C693::Method6.11390<System.Object>()"
		ldstr "class G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::Method5()
		calli default string(class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>)
		ldstr "G2_C693::Method5.11388()"
		ldstr "class G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::Method4()
		calli default string(class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>)
		ldstr "G2_C693::Method4.11387()"
		ldstr "class G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::ClassMethod1349<object>()
		calli default string(class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>)
		ldstr "G1_C13::ClassMethod1349.4877<System.Object>()"
		ldstr "class G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::ClassMethod1348<object>()
		calli default string(class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>)
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()"
		ldstr "class G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1248.IBase2`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>::Method7<object>()
		calli default string(class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>)
		ldstr "G2_C693::Method7.11391<System.Object>()"
		ldstr "class IBase2`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1248.IBase2`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>::Method7<object>()
		calli default string(class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>)
		ldstr "G3_C1720::Method7.MI.17629<System.Object>()"
		ldstr "class IBase2`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1248.IBase2`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>::Method7<object>()
		calli default string(class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>)
		ldstr "G2_C693::Method7.11391<System.Object>()"
		ldstr "class IBase2`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>::ClassMethod4841<object>()
		calli default string(class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>)
		ldstr "G3_C1720::ClassMethod4841.17632<System.Object>()"
		ldstr "class G3_C1720`1<class Generated1248.BaseClass0> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>::ClassMethod4840()
		calli default string(class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>)
		ldstr "G3_C1720::ClassMethod4840.17631()"
		ldstr "class G3_C1720`1<class Generated1248.BaseClass0> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>::ClassMethod4839()
		calli default string(class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>)
		ldstr "G3_C1720::ClassMethod4839.17630()"
		ldstr "class G3_C1720`1<class Generated1248.BaseClass0> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>::Method7<object>()
		calli default string(class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>)
		ldstr "G3_C1720::Method7.17628<System.Object>()"
		ldstr "class G3_C1720`1<class Generated1248.BaseClass0> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>::ClassMethod2751<object>()
		calli default string(class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>)
		ldstr "G3_C1720::ClassMethod2751.MI.17636<System.Object>()"
		ldstr "class G3_C1720`1<class Generated1248.BaseClass0> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>::ClassMethod2750<object>()
		calli default string(class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>)
		ldstr "G3_C1720::ClassMethod2750.MI.17635<System.Object>()"
		ldstr "class G3_C1720`1<class Generated1248.BaseClass0> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>::ClassMethod2749()
		calli default string(class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>)
		ldstr "G3_C1720::ClassMethod2749.MI.17634()"
		ldstr "class G3_C1720`1<class Generated1248.BaseClass0> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>::ClassMethod2748()
		calli default string(class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>)
		ldstr "G3_C1720::ClassMethod2748.MI.17633()"
		ldstr "class G3_C1720`1<class Generated1248.BaseClass0> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>::Method6<object>()
		calli default string(class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>)
		ldstr "G2_C693::Method6.11390<System.Object>()"
		ldstr "class G3_C1720`1<class Generated1248.BaseClass0> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>::Method5()
		calli default string(class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>)
		ldstr "G2_C693::Method5.11388()"
		ldstr "class G3_C1720`1<class Generated1248.BaseClass0> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>::Method4()
		calli default string(class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>)
		ldstr "G2_C693::Method4.11387()"
		ldstr "class G3_C1720`1<class Generated1248.BaseClass0> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>::ClassMethod1349<object>()
		calli default string(class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>)
		ldstr "G1_C13::ClassMethod1349.4877<System.Object>()"
		ldstr "class G3_C1720`1<class Generated1248.BaseClass0> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>::ClassMethod1348<object>()
		calli default string(class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>)
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()"
		ldstr "class G3_C1720`1<class Generated1248.BaseClass0> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::ClassMethod1349<object>()
		calli default string(class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>)
		ldstr "G1_C13::ClassMethod1349.4877<System.Object>()"
		ldstr "class G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::ClassMethod1348<object>()
		calli default string(class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>)
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()"
		ldstr "class G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::Method6<object>()
		calli default string(class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>)
		ldstr "G1_C13::Method6.4875<System.Object>()"
		ldstr "class G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::Method5()
		calli default string(class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>)
		ldstr "G1_C13::Method5.4873()"
		ldstr "class G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::Method4()
		calli default string(class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>)
		ldstr "G1_C13::Method4.4872()"
		ldstr "class G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::Method7<object>()
		calli default string(class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>)
		ldstr "G2_C693::Method7.11391<System.Object>()"
		ldstr "class G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1248.IBase2`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::Method7<object>()
		calli default string(class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>)
		ldstr "G3_C1720::Method7.MI.17629<System.Object>()"
		ldstr "class IBase2`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1248.IBase1`1<class Generated1248.BaseClass0>::Method4()
		calli default string(class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>)
		ldstr "G2_C693::Method4.11387()"
		ldstr "class IBase1`1<class Generated1248.BaseClass0> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1248.IBase1`1<class Generated1248.BaseClass0>::Method5()
		calli default string(class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>)
		ldstr "G2_C693::Method5.MI.11389()"
		ldstr "class IBase1`1<class Generated1248.BaseClass0> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1248.IBase1`1<class Generated1248.BaseClass0>::Method6<object>()
		calli default string(class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>)
		ldstr "G2_C693::Method6.11390<System.Object>()"
		ldstr "class IBase1`1<class Generated1248.BaseClass0> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::ClassMethod2751<object>()
		calli default string(class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>)
		ldstr "G3_C1720::ClassMethod2751.MI.17636<System.Object>()"
		ldstr "class G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::ClassMethod2750<object>()
		calli default string(class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>)
		ldstr "G3_C1720::ClassMethod2750.MI.17635<System.Object>()"
		ldstr "class G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::ClassMethod2749()
		calli default string(class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>)
		ldstr "G3_C1720::ClassMethod2749.MI.17634()"
		ldstr "class G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::ClassMethod2748()
		calli default string(class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>)
		ldstr "G3_C1720::ClassMethod2748.MI.17633()"
		ldstr "class G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::Method7<object>()
		calli default string(class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>)
		ldstr "G2_C693::Method7.11391<System.Object>()"
		ldstr "class G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::Method6<object>()
		calli default string(class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>)
		ldstr "G2_C693::Method6.11390<System.Object>()"
		ldstr "class G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::Method5()
		calli default string(class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>)
		ldstr "G2_C693::Method5.11388()"
		ldstr "class G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::Method4()
		calli default string(class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>)
		ldstr "G2_C693::Method4.11387()"
		ldstr "class G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::ClassMethod1349<object>()
		calli default string(class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>)
		ldstr "G1_C13::ClassMethod1349.4877<System.Object>()"
		ldstr "class G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::ClassMethod1348<object>()
		calli default string(class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>)
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()"
		ldstr "class G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1248.IBase2`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>::Method7<object>()
		calli default string(class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>)
		ldstr "G2_C693::Method7.11391<System.Object>()"
		ldstr "class IBase2`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1248.IBase2`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>::Method7<object>()
		calli default string(class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>)
		ldstr "G2_C693::Method7.11391<System.Object>()"
		ldstr "class IBase2`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1248.IBase2`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>::Method7<object>()
		calli default string(class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>)
		ldstr "G2_C693::Method7.11391<System.Object>()"
		ldstr "class IBase2`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>::ClassMethod4841<object>()
		calli default string(class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>)
		ldstr "G3_C1720::ClassMethod4841.17632<System.Object>()"
		ldstr "class G3_C1720`1<class Generated1248.BaseClass1> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>::ClassMethod4840()
		calli default string(class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>)
		ldstr "G3_C1720::ClassMethod4840.17631()"
		ldstr "class G3_C1720`1<class Generated1248.BaseClass1> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>::ClassMethod4839()
		calli default string(class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>)
		ldstr "G3_C1720::ClassMethod4839.17630()"
		ldstr "class G3_C1720`1<class Generated1248.BaseClass1> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>::Method7<object>()
		calli default string(class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>)
		ldstr "G3_C1720::Method7.17628<System.Object>()"
		ldstr "class G3_C1720`1<class Generated1248.BaseClass1> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>::ClassMethod2751<object>()
		calli default string(class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>)
		ldstr "G3_C1720::ClassMethod2751.MI.17636<System.Object>()"
		ldstr "class G3_C1720`1<class Generated1248.BaseClass1> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>::ClassMethod2750<object>()
		calli default string(class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>)
		ldstr "G3_C1720::ClassMethod2750.MI.17635<System.Object>()"
		ldstr "class G3_C1720`1<class Generated1248.BaseClass1> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>::ClassMethod2749()
		calli default string(class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>)
		ldstr "G3_C1720::ClassMethod2749.MI.17634()"
		ldstr "class G3_C1720`1<class Generated1248.BaseClass1> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>::ClassMethod2748()
		calli default string(class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>)
		ldstr "G3_C1720::ClassMethod2748.MI.17633()"
		ldstr "class G3_C1720`1<class Generated1248.BaseClass1> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>::Method6<object>()
		calli default string(class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>)
		ldstr "G2_C693::Method6.11390<System.Object>()"
		ldstr "class G3_C1720`1<class Generated1248.BaseClass1> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>::Method5()
		calli default string(class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>)
		ldstr "G2_C693::Method5.11388()"
		ldstr "class G3_C1720`1<class Generated1248.BaseClass1> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>::Method4()
		calli default string(class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>)
		ldstr "G2_C693::Method4.11387()"
		ldstr "class G3_C1720`1<class Generated1248.BaseClass1> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>::ClassMethod1349<object>()
		calli default string(class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>)
		ldstr "G1_C13::ClassMethod1349.4877<System.Object>()"
		ldstr "class G3_C1720`1<class Generated1248.BaseClass1> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>::ClassMethod1348<object>()
		calli default string(class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>)
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()"
		ldstr "class G3_C1720`1<class Generated1248.BaseClass1> on type class Generated1248.G3_C1720`1<class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::ClassMethod1349<object>()
		calli default string(class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>)
		ldstr "G1_C13::ClassMethod1349.4877<System.Object>()"
		ldstr "class G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::ClassMethod1348<object>()
		calli default string(class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>)
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()"
		ldstr "class G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::Method6<object>()
		calli default string(class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>)
		ldstr "G1_C13::Method6.4875<System.Object>()"
		ldstr "class G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::Method5()
		calli default string(class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>)
		ldstr "G1_C13::Method5.4873()"
		ldstr "class G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::Method4()
		calli default string(class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>)
		ldstr "G1_C13::Method4.4872()"
		ldstr "class G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::Method7<object>()
		calli default string(class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>)
		ldstr "G2_C693::Method7.11391<System.Object>()"
		ldstr "class G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1248.IBase2`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::Method7<object>()
		calli default string(class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>)
		ldstr "G2_C693::Method7.11391<System.Object>()"
		ldstr "class IBase2`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1248.IBase1`1<class Generated1248.BaseClass0>::Method4()
		calli default string(class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>)
		ldstr "G2_C693::Method4.11387()"
		ldstr "class IBase1`1<class Generated1248.BaseClass0> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1248.IBase1`1<class Generated1248.BaseClass0>::Method5()
		calli default string(class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>)
		ldstr "G2_C693::Method5.MI.11389()"
		ldstr "class IBase1`1<class Generated1248.BaseClass0> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1248.IBase1`1<class Generated1248.BaseClass0>::Method6<object>()
		calli default string(class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>)
		ldstr "G2_C693::Method6.11390<System.Object>()"
		ldstr "class IBase1`1<class Generated1248.BaseClass0> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>::ClassMethod2751<object>()
		calli default string(class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>)
		ldstr "G2_C693::ClassMethod2751.11395<System.Object>()"
		ldstr "class G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>::ClassMethod2750<object>()
		calli default string(class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>)
		ldstr "G2_C693::ClassMethod2750.11394<System.Object>()"
		ldstr "class G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>::ClassMethod2749()
		calli default string(class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>)
		ldstr "G2_C693::ClassMethod2749.11393()"
		ldstr "class G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>::ClassMethod2748()
		calli default string(class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>)
		ldstr "G2_C693::ClassMethod2748.11392()"
		ldstr "class G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>::Method7<object>()
		calli default string(class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>)
		ldstr "G2_C693::Method7.11391<System.Object>()"
		ldstr "class G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>::Method6<object>()
		calli default string(class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>)
		ldstr "G2_C693::Method6.11390<System.Object>()"
		ldstr "class G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>::Method5()
		calli default string(class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>)
		ldstr "G2_C693::Method5.11388()"
		ldstr "class G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>::Method4()
		calli default string(class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>)
		ldstr "G2_C693::Method4.11387()"
		ldstr "class G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>::ClassMethod1349<object>()
		calli default string(class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>)
		ldstr "G1_C13::ClassMethod1349.4877<System.Object>()"
		ldstr "class G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>::ClassMethod1348<object>()
		calli default string(class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>)
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()"
		ldstr "class G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1248.IBase2`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>::Method7<object>()
		calli default string(class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>)
		ldstr "G2_C693::Method7.11391<System.Object>()"
		ldstr "class IBase2`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1248.IBase2`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>::Method7<object>()
		calli default string(class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>)
		ldstr "G2_C693::Method7.11391<System.Object>()"
		ldstr "class IBase2`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1248.IBase2`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>::Method7<object>()
		calli default string(class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>)
		ldstr "G2_C693::Method7.11391<System.Object>()"
		ldstr "class IBase2`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::ClassMethod1349<object>()
		calli default string(class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>)
		ldstr "G1_C13::ClassMethod1349.4877<System.Object>()"
		ldstr "class G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::ClassMethod1348<object>()
		calli default string(class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>)
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()"
		ldstr "class G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::Method6<object>()
		calli default string(class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>)
		ldstr "G1_C13::Method6.4875<System.Object>()"
		ldstr "class G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::Method5()
		calli default string(class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>)
		ldstr "G1_C13::Method5.4873()"
		ldstr "class G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::Method4()
		calli default string(class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>)
		ldstr "G1_C13::Method4.4872()"
		ldstr "class G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::Method7<object>()
		calli default string(class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>)
		ldstr "G2_C693::Method7.11391<System.Object>()"
		ldstr "class G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1248.IBase2`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::Method7<object>()
		calli default string(class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>)
		ldstr "G2_C693::Method7.11391<System.Object>()"
		ldstr "class IBase2`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1248.IBase1`1<class Generated1248.BaseClass0>::Method4()
		calli default string(class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>)
		ldstr "G2_C693::Method4.11387()"
		ldstr "class IBase1`1<class Generated1248.BaseClass0> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1248.IBase1`1<class Generated1248.BaseClass0>::Method5()
		calli default string(class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>)
		ldstr "G2_C693::Method5.MI.11389()"
		ldstr "class IBase1`1<class Generated1248.BaseClass0> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1248.IBase1`1<class Generated1248.BaseClass0>::Method6<object>()
		calli default string(class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>)
		ldstr "G2_C693::Method6.11390<System.Object>()"
		ldstr "class IBase1`1<class Generated1248.BaseClass0> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::ClassMethod2751<object>()
		calli default string(class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>)
		ldstr "G2_C693::ClassMethod2751.11395<System.Object>()"
		ldstr "class G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::ClassMethod2750<object>()
		calli default string(class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>)
		ldstr "G2_C693::ClassMethod2750.11394<System.Object>()"
		ldstr "class G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::ClassMethod2749()
		calli default string(class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>)
		ldstr "G2_C693::ClassMethod2749.11393()"
		ldstr "class G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::ClassMethod2748()
		calli default string(class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>)
		ldstr "G2_C693::ClassMethod2748.11392()"
		ldstr "class G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::Method7<object>()
		calli default string(class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>)
		ldstr "G2_C693::Method7.11391<System.Object>()"
		ldstr "class G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::Method6<object>()
		calli default string(class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>)
		ldstr "G2_C693::Method6.11390<System.Object>()"
		ldstr "class G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::Method5()
		calli default string(class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>)
		ldstr "G2_C693::Method5.11388()"
		ldstr "class G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::Method4()
		calli default string(class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>)
		ldstr "G2_C693::Method4.11387()"
		ldstr "class G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::ClassMethod1349<object>()
		calli default string(class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>)
		ldstr "G1_C13::ClassMethod1349.4877<System.Object>()"
		ldstr "class G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::ClassMethod1348<object>()
		calli default string(class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>)
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()"
		ldstr "class G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1248.IBase2`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>::Method7<object>()
		calli default string(class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>)
		ldstr "G2_C693::Method7.11391<System.Object>()"
		ldstr "class IBase2`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1248.IBase2`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0>::Method7<object>()
		calli default string(class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>)
		ldstr "G2_C693::Method7.11391<System.Object>()"
		ldstr "class IBase2`2<class Generated1248.BaseClass0,class Generated1248.BaseClass0> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1248.IBase2`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>::Method7<object>()
		calli default string(class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>)
		ldstr "G2_C693::Method7.11391<System.Object>()"
		ldstr "class IBase2`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::ClassMethod1349<object>()
		calli default string(class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>)
		ldstr "G1_C13::ClassMethod1349.4877<System.Object>()"
		ldstr "class G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::ClassMethod1348<object>()
		calli default string(class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>)
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()"
		ldstr "class G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::Method6<object>()
		calli default string(class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>)
		ldstr "G1_C13::Method6.4875<System.Object>()"
		ldstr "class G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::Method5()
		calli default string(class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>)
		ldstr "G1_C13::Method5.4873()"
		ldstr "class G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::Method4()
		calli default string(class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>)
		ldstr "G1_C13::Method4.4872()"
		ldstr "class G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::Method7<object>()
		calli default string(class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>)
		ldstr "G2_C693::Method7.11391<System.Object>()"
		ldstr "class G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1248.IBase2`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::Method7<object>()
		calli default string(class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>)
		ldstr "G2_C693::Method7.11391<System.Object>()"
		ldstr "class IBase2`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1248.IBase1`1<class Generated1248.BaseClass0>::Method4()
		calli default string(class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>)
		ldstr "G2_C693::Method4.11387()"
		ldstr "class IBase1`1<class Generated1248.BaseClass0> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1248.IBase1`1<class Generated1248.BaseClass0>::Method5()
		calli default string(class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>)
		ldstr "G2_C693::Method5.MI.11389()"
		ldstr "class IBase1`1<class Generated1248.BaseClass0> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1248.IBase1`1<class Generated1248.BaseClass0>::Method6<object>()
		calli default string(class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>)
		ldstr "G2_C693::Method6.11390<System.Object>()"
		ldstr "class IBase1`1<class Generated1248.BaseClass0> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>::ClassMethod2751<object>()
		calli default string(class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>)
		ldstr "G2_C693::ClassMethod2751.11395<System.Object>()"
		ldstr "class G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>::ClassMethod2750<object>()
		calli default string(class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>)
		ldstr "G2_C693::ClassMethod2750.11394<System.Object>()"
		ldstr "class G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>::ClassMethod2749()
		calli default string(class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>)
		ldstr "G2_C693::ClassMethod2749.11393()"
		ldstr "class G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>::ClassMethod2748()
		calli default string(class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>)
		ldstr "G2_C693::ClassMethod2748.11392()"
		ldstr "class G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>::Method7<object>()
		calli default string(class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>)
		ldstr "G2_C693::Method7.11391<System.Object>()"
		ldstr "class G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>::Method6<object>()
		calli default string(class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>)
		ldstr "G2_C693::Method6.11390<System.Object>()"
		ldstr "class G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>::Method5()
		calli default string(class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>)
		ldstr "G2_C693::Method5.11388()"
		ldstr "class G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>::Method4()
		calli default string(class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>)
		ldstr "G2_C693::Method4.11387()"
		ldstr "class G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>::ClassMethod1349<object>()
		calli default string(class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>)
		ldstr "G1_C13::ClassMethod1349.4877<System.Object>()"
		ldstr "class G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>::ClassMethod1348<object>()
		calli default string(class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>)
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()"
		ldstr "class G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1248.IBase2`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>::Method7<object>()
		calli default string(class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>)
		ldstr "G2_C693::Method7.11391<System.Object>()"
		ldstr "class IBase2`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::ClassMethod1349<object>()
		calli default string(class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>)
		ldstr "G1_C13::ClassMethod1349.4877<System.Object>()"
		ldstr "class G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::ClassMethod1348<object>()
		calli default string(class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>)
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()"
		ldstr "class G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::Method6<object>()
		calli default string(class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>)
		ldstr "G1_C13::Method6.4875<System.Object>()"
		ldstr "class G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::Method5()
		calli default string(class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>)
		ldstr "G1_C13::Method5.4873()"
		ldstr "class G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::Method4()
		calli default string(class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>)
		ldstr "G1_C13::Method4.4872()"
		ldstr "class G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1248.G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::Method7<object>()
		calli default string(class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>)
		ldstr "G2_C693::Method7.11391<System.Object>()"
		ldstr "class G1_C13`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1248.IBase2`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1>::Method7<object>()
		calli default string(class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>)
		ldstr "G2_C693::Method7.11391<System.Object>()"
		ldstr "class IBase2`2<class Generated1248.BaseClass0,class Generated1248.BaseClass1> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1248.IBase1`1<class Generated1248.BaseClass0>::Method4()
		calli default string(class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>)
		ldstr "G2_C693::Method4.11387()"
		ldstr "class IBase1`1<class Generated1248.BaseClass0> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1248.IBase1`1<class Generated1248.BaseClass0>::Method5()
		calli default string(class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>)
		ldstr "G2_C693::Method5.MI.11389()"
		ldstr "class IBase1`1<class Generated1248.BaseClass0> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1248.IBase1`1<class Generated1248.BaseClass0>::Method6<object>()
		calli default string(class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>)
		ldstr "G2_C693::Method6.11390<System.Object>()"
		ldstr "class IBase1`1<class Generated1248.BaseClass0> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>::ClassMethod2751<object>()
		calli default string(class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>)
		ldstr "G2_C693::ClassMethod2751.11395<System.Object>()"
		ldstr "class G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>::ClassMethod2750<object>()
		calli default string(class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>)
		ldstr "G2_C693::ClassMethod2750.11394<System.Object>()"
		ldstr "class G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>::ClassMethod2749()
		calli default string(class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>)
		ldstr "G2_C693::ClassMethod2749.11393()"
		ldstr "class G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>::ClassMethod2748()
		calli default string(class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>)
		ldstr "G2_C693::ClassMethod2748.11392()"
		ldstr "class G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>::Method7<object>()
		calli default string(class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>)
		ldstr "G2_C693::Method7.11391<System.Object>()"
		ldstr "class G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>::Method6<object>()
		calli default string(class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>)
		ldstr "G2_C693::Method6.11390<System.Object>()"
		ldstr "class G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>::Method5()
		calli default string(class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>)
		ldstr "G2_C693::Method5.11388()"
		ldstr "class G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>::Method4()
		calli default string(class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>)
		ldstr "G2_C693::Method4.11387()"
		ldstr "class G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>::ClassMethod1349<object>()
		calli default string(class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>)
		ldstr "G1_C13::ClassMethod1349.4877<System.Object>()"
		ldstr "class G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>::ClassMethod1348<object>()
		calli default string(class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>)
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()"
		ldstr "class G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1248.IBase2`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>::Method7<object>()
		calli default string(class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>)
		ldstr "G2_C693::Method7.11391<System.Object>()"
		ldstr "class IBase2`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1> on type class Generated1248.G2_C693`2<class Generated1248.BaseClass1,class Generated1248.BaseClass1>"
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
		call void Generated1248.Generated1248::MethodCallingTest()
		call void Generated1248.Generated1248::ConstrainedCallsTest()
		call void Generated1248.Generated1248::StructConstrainedInterfaceCallsTest()
		call void Generated1248.Generated1248::CalliTest()
		ldc.i4 100
		ret
	}
}
