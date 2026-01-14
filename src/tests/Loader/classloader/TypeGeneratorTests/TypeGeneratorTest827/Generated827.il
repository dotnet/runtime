// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) .ver 4:0:0:0 }
.assembly extern TestFramework { .publickeytoken = ( B0 3F 5F 7F 11 D5 0A 3A )  }

//TYPES IN FORWARDER ASSEMBLIES:

//TEST ASSEMBLY:
.assembly extern xunit.core {}

.class public Generated827.BaseClass0 
{
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void [mscorlib]System.Object::.ctor()
		ret
	}
}
.class public Generated827.BaseClass1 
		extends Generated827.BaseClass0
{
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void Generated827.BaseClass0::.ctor()
		ret
	}
}
.class public Generated827.G3_C1299`1<T0> 
		extends class Generated827.G2_C336`2<class Generated827.BaseClass0,!T0>
{
	.method public hidebysig newslot virtual instance string ClassMethod3975() cil managed noinlining { 
		ldstr "G3_C1299::ClassMethod3975.15413()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod3976() cil managed noinlining { 
		ldstr "G3_C1299::ClassMethod3976.15414()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod3977<M0>() cil managed noinlining { 
		ldstr "G3_C1299::ClassMethod3977.15415<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod3978<M0>() cil managed noinlining { 
		ldstr "G3_C1299::ClassMethod3978.15416<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'G2_C336<class Generated827.BaseClass0,T0>.ClassMethod2015'() cil managed noinlining { 
		.override method instance string class Generated827.G2_C336`2<class Generated827.BaseClass0,!T0>::ClassMethod2015()
		ldstr "G3_C1299::ClassMethod2015.MI.15417()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'G2_C336<class Generated827.BaseClass0,T0>.ClassMethod2016'<M0>() cil managed noinlining { 
		.override method instance string class Generated827.G2_C336`2<class Generated827.BaseClass0,!T0>::ClassMethod2016<[1]>()
		ldstr "G3_C1299::ClassMethod2016.MI.15418<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'G2_C336<class Generated827.BaseClass0,T0>.ClassMethod2017'<M0>() cil managed noinlining { 
		.override method instance string class Generated827.G2_C336`2<class Generated827.BaseClass0,!T0>::ClassMethod2017<[1]>()
		ldstr "G3_C1299::ClassMethod2017.MI.15419<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class Generated827.G2_C336`2<class Generated827.BaseClass0,!T0>::.ctor()
		ret
	}
}
.class public Generated827.G2_C336`2<T0, T1> 
		extends class Generated827.G1_C7`2<class Generated827.BaseClass0,!T0>
		implements class Generated827.IBase1`1<class Generated827.BaseClass0>, class Generated827.IBase2`2<class Generated827.BaseClass1,!T0> 
{
	.method public hidebysig newslot virtual instance string Method4() cil managed noinlining { 
		ldstr "G2_C336::Method4.7890()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<class Generated827.BaseClass0>.Method4'() cil managed noinlining { 
		.override method instance string class Generated827.IBase1`1<class Generated827.BaseClass0>::Method4()
		ldstr "G2_C336::Method4.MI.7891()"
		ret
	}
	.method public hidebysig newslot virtual instance string Method5() cil managed noinlining { 
		ldstr "G2_C336::Method5.7892()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<class Generated827.BaseClass0>.Method5'() cil managed noinlining { 
		.override method instance string class Generated827.IBase1`1<class Generated827.BaseClass0>::Method5()
		ldstr "G2_C336::Method5.MI.7893()"
		ret
	}
	.method public hidebysig newslot virtual instance string Method6<M0>() cil managed noinlining { 
		ldstr "G2_C336::Method6.7894<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G2_C336::Method7.7895<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod2015() cil managed noinlining { 
		ldstr "G2_C336::ClassMethod2015.7896()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod2016<M0>() cil managed noinlining { 
		ldstr "G2_C336::ClassMethod2016.7897<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod2017<M0>() cil managed noinlining { 
		ldstr "G2_C336::ClassMethod2017.7898<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'G1_C7<class Generated827.BaseClass0,T0>.ClassMethod1329'() cil managed noinlining { 
		.override method instance string class Generated827.G1_C7`2<class Generated827.BaseClass0,!T0>::ClassMethod1329()
		ldstr "G2_C336::ClassMethod1329.MI.7899()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'G1_C7<class Generated827.BaseClass0,T0>.ClassMethod1330'<M0>() cil managed noinlining { 
		.override method instance string class Generated827.G1_C7`2<class Generated827.BaseClass0,!T0>::ClassMethod1330<[1]>()
		ldstr "G2_C336::ClassMethod1330.MI.7900<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class Generated827.G1_C7`2<class Generated827.BaseClass0,!T0>::.ctor()
		ret
	}
}
.class public abstract Generated827.G1_C7`2<T0, T1> 
		implements class Generated827.IBase1`1<!T0>, class Generated827.IBase2`2<class Generated827.BaseClass1,!T0> 
{
	.method public hidebysig virtual instance string Method4() cil managed noinlining { 
		ldstr "G1_C7::Method4.4811()"
		ret
	}
	.method public hidebysig newslot virtual instance string Method5() cil managed noinlining { 
		ldstr "G1_C7::Method5.4812()"
		ret
	}
	.method public hidebysig newslot virtual instance string Method6<M0>() cil managed noinlining { 
		ldstr "G1_C7::Method6.4813<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<T0>.Method6'<M0>() cil managed noinlining { 
		.override method instance string class Generated827.IBase1`1<!T0>::Method6<[1]>()
		ldstr "G1_C7::Method6.MI.4814<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G1_C7::Method7.4815<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase2<class Generated827.BaseClass1,T0>.Method7'<M0>() cil managed noinlining { 
		.override method instance string class Generated827.IBase2`2<class Generated827.BaseClass1,!T0>::Method7<[1]>()
		ldstr "G1_C7::Method7.MI.4816<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1328() cil managed noinlining { 
		ldstr "G1_C7::ClassMethod1328.4817()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1329() cil managed noinlining { 
		ldstr "G1_C7::ClassMethod1329.4818()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1330<M0>() cil managed noinlining { 
		ldstr "G1_C7::ClassMethod1330.4819<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1331<M0>() cil managed noinlining { 
		ldstr "G1_C7::ClassMethod1331.4820<"
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
.class interface public abstract Generated827.IBase1`1<+T0> 
{
	.method public hidebysig newslot abstract virtual instance string Method4() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method5() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method6<M0>() cil managed { }
}
.class interface public abstract Generated827.IBase2`2<+T0, -T1> 
{
	.method public hidebysig newslot abstract virtual instance string Method7<M0>() cil managed { }
}
.class public auto ansi beforefieldinit Generated827.Generated827 {
	.method static void M.BaseClass0<(Generated827.BaseClass0)W>(!!W inst, string exp) cil managed {
		.maxstack 5
		.locals init (string[] actualResults)
		ldc.i4.s    0
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.BaseClass0<(Generated827.BaseClass0)W>(!!W inst, string exp)"
		ldc.i4.s 0
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.BaseClass1<(Generated827.BaseClass1)W>(!!W inst, string exp) cil managed {
		.maxstack 5
		.locals init (string[] actualResults)
		ldc.i4.s    0
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.BaseClass1<(Generated827.BaseClass1)W>(!!W inst, string exp)"
		ldc.i4.s 0
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1299.T<T0,(class Generated827.G3_C1299`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 20
		.locals init (string[] actualResults)
		ldc.i4.s    15
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1299.T<T0,(class Generated827.G3_C1299`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 15
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G3_C1299`1<!!T0>::ClassMethod1328()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G3_C1299`1<!!T0>::ClassMethod1329()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G3_C1299`1<!!T0>::ClassMethod1330<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G3_C1299`1<!!T0>::ClassMethod1331<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G3_C1299`1<!!T0>::ClassMethod2015()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G3_C1299`1<!!T0>::ClassMethod2016<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G3_C1299`1<!!T0>::ClassMethod2017<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G3_C1299`1<!!T0>::ClassMethod3975()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G3_C1299`1<!!T0>::ClassMethod3976()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G3_C1299`1<!!T0>::ClassMethod3977<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G3_C1299`1<!!T0>::ClassMethod3978<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G3_C1299`1<!!T0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G3_C1299`1<!!T0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     13
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G3_C1299`1<!!T0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     14
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G3_C1299`1<!!T0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1299.A<(class Generated827.G3_C1299`1<class Generated827.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 20
		.locals init (string[] actualResults)
		ldc.i4.s    15
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1299.A<(class Generated827.G3_C1299`1<class Generated827.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 15
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G3_C1299`1<class Generated827.BaseClass0>::ClassMethod1328()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G3_C1299`1<class Generated827.BaseClass0>::ClassMethod1329()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G3_C1299`1<class Generated827.BaseClass0>::ClassMethod1330<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G3_C1299`1<class Generated827.BaseClass0>::ClassMethod1331<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G3_C1299`1<class Generated827.BaseClass0>::ClassMethod2015()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G3_C1299`1<class Generated827.BaseClass0>::ClassMethod2016<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G3_C1299`1<class Generated827.BaseClass0>::ClassMethod2017<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G3_C1299`1<class Generated827.BaseClass0>::ClassMethod3975()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G3_C1299`1<class Generated827.BaseClass0>::ClassMethod3976()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G3_C1299`1<class Generated827.BaseClass0>::ClassMethod3977<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G3_C1299`1<class Generated827.BaseClass0>::ClassMethod3978<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G3_C1299`1<class Generated827.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G3_C1299`1<class Generated827.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     13
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G3_C1299`1<class Generated827.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     14
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G3_C1299`1<class Generated827.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1299.B<(class Generated827.G3_C1299`1<class Generated827.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 20
		.locals init (string[] actualResults)
		ldc.i4.s    15
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1299.B<(class Generated827.G3_C1299`1<class Generated827.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 15
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G3_C1299`1<class Generated827.BaseClass1>::ClassMethod1328()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G3_C1299`1<class Generated827.BaseClass1>::ClassMethod1329()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G3_C1299`1<class Generated827.BaseClass1>::ClassMethod1330<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G3_C1299`1<class Generated827.BaseClass1>::ClassMethod1331<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G3_C1299`1<class Generated827.BaseClass1>::ClassMethod2015()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G3_C1299`1<class Generated827.BaseClass1>::ClassMethod2016<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G3_C1299`1<class Generated827.BaseClass1>::ClassMethod2017<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G3_C1299`1<class Generated827.BaseClass1>::ClassMethod3975()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G3_C1299`1<class Generated827.BaseClass1>::ClassMethod3976()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G3_C1299`1<class Generated827.BaseClass1>::ClassMethod3977<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G3_C1299`1<class Generated827.BaseClass1>::ClassMethod3978<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G3_C1299`1<class Generated827.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G3_C1299`1<class Generated827.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     13
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G3_C1299`1<class Generated827.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     14
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G3_C1299`1<class Generated827.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C336.T.T<T0,T1,(class Generated827.G2_C336`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 16
		.locals init (string[] actualResults)
		ldc.i4.s    11
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C336.T.T<T0,T1,(class Generated827.G2_C336`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 11
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G2_C336`2<!!T0,!!T1>::ClassMethod1328()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G2_C336`2<!!T0,!!T1>::ClassMethod1329()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G2_C336`2<!!T0,!!T1>::ClassMethod1330<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G2_C336`2<!!T0,!!T1>::ClassMethod1331<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G2_C336`2<!!T0,!!T1>::ClassMethod2015()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G2_C336`2<!!T0,!!T1>::ClassMethod2016<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G2_C336`2<!!T0,!!T1>::ClassMethod2017<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G2_C336`2<!!T0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G2_C336`2<!!T0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G2_C336`2<!!T0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G2_C336`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C336.A.T<T1,(class Generated827.G2_C336`2<class Generated827.BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 16
		.locals init (string[] actualResults)
		ldc.i4.s    11
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C336.A.T<T1,(class Generated827.G2_C336`2<class Generated827.BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 11
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G2_C336`2<class Generated827.BaseClass0,!!T1>::ClassMethod1328()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G2_C336`2<class Generated827.BaseClass0,!!T1>::ClassMethod1329()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G2_C336`2<class Generated827.BaseClass0,!!T1>::ClassMethod1330<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G2_C336`2<class Generated827.BaseClass0,!!T1>::ClassMethod1331<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G2_C336`2<class Generated827.BaseClass0,!!T1>::ClassMethod2015()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G2_C336`2<class Generated827.BaseClass0,!!T1>::ClassMethod2016<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G2_C336`2<class Generated827.BaseClass0,!!T1>::ClassMethod2017<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G2_C336`2<class Generated827.BaseClass0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G2_C336`2<class Generated827.BaseClass0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G2_C336`2<class Generated827.BaseClass0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G2_C336`2<class Generated827.BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C336.A.A<(class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 16
		.locals init (string[] actualResults)
		ldc.i4.s    11
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C336.A.A<(class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 11
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::ClassMethod1328()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::ClassMethod1329()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::ClassMethod1330<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::ClassMethod1331<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::ClassMethod2015()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::ClassMethod2016<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::ClassMethod2017<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C336.A.B<(class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 16
		.locals init (string[] actualResults)
		ldc.i4.s    11
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C336.A.B<(class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 11
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>::ClassMethod1328()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>::ClassMethod1329()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>::ClassMethod1330<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>::ClassMethod1331<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>::ClassMethod2015()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>::ClassMethod2016<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>::ClassMethod2017<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C336.B.T<T1,(class Generated827.G2_C336`2<class Generated827.BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 16
		.locals init (string[] actualResults)
		ldc.i4.s    11
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C336.B.T<T1,(class Generated827.G2_C336`2<class Generated827.BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 11
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G2_C336`2<class Generated827.BaseClass1,!!T1>::ClassMethod1328()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G2_C336`2<class Generated827.BaseClass1,!!T1>::ClassMethod1329()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G2_C336`2<class Generated827.BaseClass1,!!T1>::ClassMethod1330<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G2_C336`2<class Generated827.BaseClass1,!!T1>::ClassMethod1331<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G2_C336`2<class Generated827.BaseClass1,!!T1>::ClassMethod2015()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G2_C336`2<class Generated827.BaseClass1,!!T1>::ClassMethod2016<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G2_C336`2<class Generated827.BaseClass1,!!T1>::ClassMethod2017<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G2_C336`2<class Generated827.BaseClass1,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G2_C336`2<class Generated827.BaseClass1,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G2_C336`2<class Generated827.BaseClass1,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G2_C336`2<class Generated827.BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C336.B.A<(class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 16
		.locals init (string[] actualResults)
		ldc.i4.s    11
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C336.B.A<(class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 11
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>::ClassMethod1328()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>::ClassMethod1329()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>::ClassMethod1330<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>::ClassMethod1331<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>::ClassMethod2015()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>::ClassMethod2016<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>::ClassMethod2017<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C336.B.B<(class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 16
		.locals init (string[] actualResults)
		ldc.i4.s    11
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C336.B.B<(class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 11
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>::ClassMethod1328()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>::ClassMethod1329()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>::ClassMethod1330<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>::ClassMethod1331<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>::ClassMethod2015()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>::ClassMethod2016<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>::ClassMethod2017<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C7.T.T<T0,T1,(class Generated827.G1_C7`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C7.T.T<T0,T1,(class Generated827.G1_C7`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G1_C7`2<!!T0,!!T1>::ClassMethod1328()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G1_C7`2<!!T0,!!T1>::ClassMethod1329()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G1_C7`2<!!T0,!!T1>::ClassMethod1330<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G1_C7`2<!!T0,!!T1>::ClassMethod1331<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G1_C7`2<!!T0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G1_C7`2<!!T0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G1_C7`2<!!T0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G1_C7`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C7.A.T<T1,(class Generated827.G1_C7`2<class Generated827.BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C7.A.T<T1,(class Generated827.G1_C7`2<class Generated827.BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G1_C7`2<class Generated827.BaseClass0,!!T1>::ClassMethod1328()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G1_C7`2<class Generated827.BaseClass0,!!T1>::ClassMethod1329()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G1_C7`2<class Generated827.BaseClass0,!!T1>::ClassMethod1330<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G1_C7`2<class Generated827.BaseClass0,!!T1>::ClassMethod1331<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G1_C7`2<class Generated827.BaseClass0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G1_C7`2<class Generated827.BaseClass0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G1_C7`2<class Generated827.BaseClass0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G1_C7`2<class Generated827.BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C7.A.A<(class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C7.A.A<(class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::ClassMethod1328()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::ClassMethod1329()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::ClassMethod1330<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::ClassMethod1331<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C7.A.B<(class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C7.A.B<(class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass1>::ClassMethod1328()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass1>::ClassMethod1329()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass1>::ClassMethod1330<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass1>::ClassMethod1331<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C7.B.T<T1,(class Generated827.G1_C7`2<class Generated827.BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C7.B.T<T1,(class Generated827.G1_C7`2<class Generated827.BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G1_C7`2<class Generated827.BaseClass1,!!T1>::ClassMethod1328()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G1_C7`2<class Generated827.BaseClass1,!!T1>::ClassMethod1329()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G1_C7`2<class Generated827.BaseClass1,!!T1>::ClassMethod1330<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G1_C7`2<class Generated827.BaseClass1,!!T1>::ClassMethod1331<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G1_C7`2<class Generated827.BaseClass1,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G1_C7`2<class Generated827.BaseClass1,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G1_C7`2<class Generated827.BaseClass1,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G1_C7`2<class Generated827.BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C7.B.A<(class Generated827.G1_C7`2<class Generated827.BaseClass1,class Generated827.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C7.B.A<(class Generated827.G1_C7`2<class Generated827.BaseClass1,class Generated827.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G1_C7`2<class Generated827.BaseClass1,class Generated827.BaseClass0>::ClassMethod1328()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G1_C7`2<class Generated827.BaseClass1,class Generated827.BaseClass0>::ClassMethod1329()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G1_C7`2<class Generated827.BaseClass1,class Generated827.BaseClass0>::ClassMethod1330<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G1_C7`2<class Generated827.BaseClass1,class Generated827.BaseClass0>::ClassMethod1331<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G1_C7`2<class Generated827.BaseClass1,class Generated827.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G1_C7`2<class Generated827.BaseClass1,class Generated827.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G1_C7`2<class Generated827.BaseClass1,class Generated827.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G1_C7`2<class Generated827.BaseClass1,class Generated827.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C7.B.B<(class Generated827.G1_C7`2<class Generated827.BaseClass1,class Generated827.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C7.B.B<(class Generated827.G1_C7`2<class Generated827.BaseClass1,class Generated827.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G1_C7`2<class Generated827.BaseClass1,class Generated827.BaseClass1>::ClassMethod1328()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G1_C7`2<class Generated827.BaseClass1,class Generated827.BaseClass1>::ClassMethod1329()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G1_C7`2<class Generated827.BaseClass1,class Generated827.BaseClass1>::ClassMethod1330<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G1_C7`2<class Generated827.BaseClass1,class Generated827.BaseClass1>::ClassMethod1331<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G1_C7`2<class Generated827.BaseClass1,class Generated827.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G1_C7`2<class Generated827.BaseClass1,class Generated827.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G1_C7`2<class Generated827.BaseClass1,class Generated827.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.G1_C7`2<class Generated827.BaseClass1,class Generated827.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase1.T<T0,(class Generated827.IBase1`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase1.T<T0,(class Generated827.IBase1`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.IBase1`1<!!T0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.IBase1`1<!!T0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.IBase1`1<!!T0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase1.A<(class Generated827.IBase1`1<class Generated827.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase1.A<(class Generated827.IBase1`1<class Generated827.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.IBase1`1<class Generated827.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.IBase1`1<class Generated827.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.IBase1`1<class Generated827.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase1.B<(class Generated827.IBase1`1<class Generated827.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase1.B<(class Generated827.IBase1`1<class Generated827.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.IBase1`1<class Generated827.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.IBase1`1<class Generated827.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.IBase1`1<class Generated827.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.T.T<T0,T1,(class Generated827.IBase2`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.T.T<T0,T1,(class Generated827.IBase2`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.IBase2`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.A.T<T1,(class Generated827.IBase2`2<class Generated827.BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.A.T<T1,(class Generated827.IBase2`2<class Generated827.BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.IBase2`2<class Generated827.BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.A.A<(class Generated827.IBase2`2<class Generated827.BaseClass0,class Generated827.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.A.A<(class Generated827.IBase2`2<class Generated827.BaseClass0,class Generated827.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.IBase2`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.A.B<(class Generated827.IBase2`2<class Generated827.BaseClass0,class Generated827.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.A.B<(class Generated827.IBase2`2<class Generated827.BaseClass0,class Generated827.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.IBase2`2<class Generated827.BaseClass0,class Generated827.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.B.T<T1,(class Generated827.IBase2`2<class Generated827.BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.B.T<T1,(class Generated827.IBase2`2<class Generated827.BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.IBase2`2<class Generated827.BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.B.A<(class Generated827.IBase2`2<class Generated827.BaseClass1,class Generated827.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.B.A<(class Generated827.IBase2`2<class Generated827.BaseClass1,class Generated827.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.IBase2`2<class Generated827.BaseClass1,class Generated827.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.B.B<(class Generated827.IBase2`2<class Generated827.BaseClass1,class Generated827.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.B.B<(class Generated827.IBase2`2<class Generated827.BaseClass1,class Generated827.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated827.IBase2`2<class Generated827.BaseClass1,class Generated827.BaseClass1>::Method7<object>()
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
		newobj instance void class Generated827.G3_C1299`1<class Generated827.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>
		callvirt instance string class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::ClassMethod1331<object>()
		ldstr "G1_C7::ClassMethod1331.4820<System.Object>()"
		ldstr "class G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0> on type class Generated827.G3_C1299`1<class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>
		callvirt instance string class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::ClassMethod1330<object>()
		ldstr "G2_C336::ClassMethod1330.MI.7900<System.Object>()"
		ldstr "class G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0> on type class Generated827.G3_C1299`1<class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>
		callvirt instance string class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::ClassMethod1329()
		ldstr "G2_C336::ClassMethod1329.MI.7899()"
		ldstr "class G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0> on type class Generated827.G3_C1299`1<class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>
		callvirt instance string class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::ClassMethod1328()
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0> on type class Generated827.G3_C1299`1<class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>
		callvirt instance string class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::Method7<object>()
		ldstr "G2_C336::Method7.7895<System.Object>()"
		ldstr "class G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0> on type class Generated827.G3_C1299`1<class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>
		callvirt instance string class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::Method6<object>()
		ldstr "G1_C7::Method6.4813<System.Object>()"
		ldstr "class G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0> on type class Generated827.G3_C1299`1<class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>
		callvirt instance string class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::Method5()
		ldstr "G1_C7::Method5.4812()"
		ldstr "class G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0> on type class Generated827.G3_C1299`1<class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>
		callvirt instance string class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::Method4()
		ldstr "G1_C7::Method4.4811()"
		ldstr "class G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0> on type class Generated827.G3_C1299`1<class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated827.IBase1`1<class Generated827.BaseClass0>::Method4()
		ldstr "G2_C336::Method4.MI.7891()"
		ldstr "class IBase1`1<class Generated827.BaseClass0> on type class Generated827.G3_C1299`1<class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated827.IBase1`1<class Generated827.BaseClass0>::Method5()
		ldstr "G2_C336::Method5.MI.7893()"
		ldstr "class IBase1`1<class Generated827.BaseClass0> on type class Generated827.G3_C1299`1<class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated827.IBase1`1<class Generated827.BaseClass0>::Method6<object>()
		ldstr "G2_C336::Method6.7894<System.Object>()"
		ldstr "class IBase1`1<class Generated827.BaseClass0> on type class Generated827.G3_C1299`1<class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated827.IBase2`2<class Generated827.BaseClass1,class Generated827.BaseClass0>::Method7<object>()
		ldstr "G2_C336::Method7.7895<System.Object>()"
		ldstr "class IBase2`2<class Generated827.BaseClass1,class Generated827.BaseClass0> on type class Generated827.G3_C1299`1<class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated827.IBase2`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::Method7<object>()
		ldstr "G2_C336::Method7.7895<System.Object>()"
		ldstr "class IBase2`2<class Generated827.BaseClass0,class Generated827.BaseClass0> on type class Generated827.G3_C1299`1<class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated827.IBase2`2<class Generated827.BaseClass0,class Generated827.BaseClass1>::Method7<object>()
		ldstr "G2_C336::Method7.7895<System.Object>()"
		ldstr "class IBase2`2<class Generated827.BaseClass0,class Generated827.BaseClass1> on type class Generated827.G3_C1299`1<class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated827.IBase2`2<class Generated827.BaseClass1,class Generated827.BaseClass1>::Method7<object>()
		ldstr "G2_C336::Method7.7895<System.Object>()"
		ldstr "class IBase2`2<class Generated827.BaseClass1,class Generated827.BaseClass1> on type class Generated827.G3_C1299`1<class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>
		callvirt instance string class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::ClassMethod2017<object>()
		ldstr "G3_C1299::ClassMethod2017.MI.15419<System.Object>()"
		ldstr "class G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0> on type class Generated827.G3_C1299`1<class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>
		callvirt instance string class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::ClassMethod2016<object>()
		ldstr "G3_C1299::ClassMethod2016.MI.15418<System.Object>()"
		ldstr "class G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0> on type class Generated827.G3_C1299`1<class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>
		callvirt instance string class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::ClassMethod2015()
		ldstr "G3_C1299::ClassMethod2015.MI.15417()"
		ldstr "class G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0> on type class Generated827.G3_C1299`1<class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>
		callvirt instance string class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::Method7<object>()
		ldstr "G2_C336::Method7.7895<System.Object>()"
		ldstr "class G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0> on type class Generated827.G3_C1299`1<class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>
		callvirt instance string class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::Method6<object>()
		ldstr "G2_C336::Method6.7894<System.Object>()"
		ldstr "class G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0> on type class Generated827.G3_C1299`1<class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>
		callvirt instance string class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::Method5()
		ldstr "G2_C336::Method5.7892()"
		ldstr "class G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0> on type class Generated827.G3_C1299`1<class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>
		callvirt instance string class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::Method4()
		ldstr "G2_C336::Method4.7890()"
		ldstr "class G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0> on type class Generated827.G3_C1299`1<class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>
		callvirt instance string class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::ClassMethod1331<object>()
		ldstr "G1_C7::ClassMethod1331.4820<System.Object>()"
		ldstr "class G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0> on type class Generated827.G3_C1299`1<class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>
		callvirt instance string class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::ClassMethod1330<object>()
		ldstr "G2_C336::ClassMethod1330.MI.7900<System.Object>()"
		ldstr "class G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0> on type class Generated827.G3_C1299`1<class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>
		callvirt instance string class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::ClassMethod1329()
		ldstr "G2_C336::ClassMethod1329.MI.7899()"
		ldstr "class G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0> on type class Generated827.G3_C1299`1<class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>
		callvirt instance string class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::ClassMethod1328()
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0> on type class Generated827.G3_C1299`1<class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated827.G3_C1299`1<class Generated827.BaseClass0>
		callvirt instance string class Generated827.G3_C1299`1<class Generated827.BaseClass0>::ClassMethod3978<object>()
		ldstr "G3_C1299::ClassMethod3978.15416<System.Object>()"
		ldstr "class G3_C1299`1<class Generated827.BaseClass0> on type class Generated827.G3_C1299`1<class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated827.G3_C1299`1<class Generated827.BaseClass0>
		callvirt instance string class Generated827.G3_C1299`1<class Generated827.BaseClass0>::ClassMethod3977<object>()
		ldstr "G3_C1299::ClassMethod3977.15415<System.Object>()"
		ldstr "class G3_C1299`1<class Generated827.BaseClass0> on type class Generated827.G3_C1299`1<class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated827.G3_C1299`1<class Generated827.BaseClass0>
		callvirt instance string class Generated827.G3_C1299`1<class Generated827.BaseClass0>::ClassMethod3976()
		ldstr "G3_C1299::ClassMethod3976.15414()"
		ldstr "class G3_C1299`1<class Generated827.BaseClass0> on type class Generated827.G3_C1299`1<class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated827.G3_C1299`1<class Generated827.BaseClass0>
		callvirt instance string class Generated827.G3_C1299`1<class Generated827.BaseClass0>::ClassMethod3975()
		ldstr "G3_C1299::ClassMethod3975.15413()"
		ldstr "class G3_C1299`1<class Generated827.BaseClass0> on type class Generated827.G3_C1299`1<class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated827.G3_C1299`1<class Generated827.BaseClass0>
		callvirt instance string class Generated827.G3_C1299`1<class Generated827.BaseClass0>::ClassMethod2017<object>()
		ldstr "G3_C1299::ClassMethod2017.MI.15419<System.Object>()"
		ldstr "class G3_C1299`1<class Generated827.BaseClass0> on type class Generated827.G3_C1299`1<class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated827.G3_C1299`1<class Generated827.BaseClass0>
		callvirt instance string class Generated827.G3_C1299`1<class Generated827.BaseClass0>::ClassMethod2016<object>()
		ldstr "G3_C1299::ClassMethod2016.MI.15418<System.Object>()"
		ldstr "class G3_C1299`1<class Generated827.BaseClass0> on type class Generated827.G3_C1299`1<class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated827.G3_C1299`1<class Generated827.BaseClass0>
		callvirt instance string class Generated827.G3_C1299`1<class Generated827.BaseClass0>::ClassMethod2015()
		ldstr "G3_C1299::ClassMethod2015.MI.15417()"
		ldstr "class G3_C1299`1<class Generated827.BaseClass0> on type class Generated827.G3_C1299`1<class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated827.G3_C1299`1<class Generated827.BaseClass0>
		callvirt instance string class Generated827.G3_C1299`1<class Generated827.BaseClass0>::Method7<object>()
		ldstr "G2_C336::Method7.7895<System.Object>()"
		ldstr "class G3_C1299`1<class Generated827.BaseClass0> on type class Generated827.G3_C1299`1<class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated827.G3_C1299`1<class Generated827.BaseClass0>
		callvirt instance string class Generated827.G3_C1299`1<class Generated827.BaseClass0>::Method6<object>()
		ldstr "G2_C336::Method6.7894<System.Object>()"
		ldstr "class G3_C1299`1<class Generated827.BaseClass0> on type class Generated827.G3_C1299`1<class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated827.G3_C1299`1<class Generated827.BaseClass0>
		callvirt instance string class Generated827.G3_C1299`1<class Generated827.BaseClass0>::Method5()
		ldstr "G2_C336::Method5.7892()"
		ldstr "class G3_C1299`1<class Generated827.BaseClass0> on type class Generated827.G3_C1299`1<class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated827.G3_C1299`1<class Generated827.BaseClass0>
		callvirt instance string class Generated827.G3_C1299`1<class Generated827.BaseClass0>::Method4()
		ldstr "G2_C336::Method4.7890()"
		ldstr "class G3_C1299`1<class Generated827.BaseClass0> on type class Generated827.G3_C1299`1<class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated827.G3_C1299`1<class Generated827.BaseClass0>
		callvirt instance string class Generated827.G3_C1299`1<class Generated827.BaseClass0>::ClassMethod1331<object>()
		ldstr "G1_C7::ClassMethod1331.4820<System.Object>()"
		ldstr "class G3_C1299`1<class Generated827.BaseClass0> on type class Generated827.G3_C1299`1<class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated827.G3_C1299`1<class Generated827.BaseClass0>
		callvirt instance string class Generated827.G3_C1299`1<class Generated827.BaseClass0>::ClassMethod1330<object>()
		ldstr "G2_C336::ClassMethod1330.MI.7900<System.Object>()"
		ldstr "class G3_C1299`1<class Generated827.BaseClass0> on type class Generated827.G3_C1299`1<class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated827.G3_C1299`1<class Generated827.BaseClass0>
		callvirt instance string class Generated827.G3_C1299`1<class Generated827.BaseClass0>::ClassMethod1329()
		ldstr "G2_C336::ClassMethod1329.MI.7899()"
		ldstr "class G3_C1299`1<class Generated827.BaseClass0> on type class Generated827.G3_C1299`1<class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated827.G3_C1299`1<class Generated827.BaseClass0>
		callvirt instance string class Generated827.G3_C1299`1<class Generated827.BaseClass0>::ClassMethod1328()
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G3_C1299`1<class Generated827.BaseClass0> on type class Generated827.G3_C1299`1<class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated827.G3_C1299`1<class Generated827.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>
		callvirt instance string class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::ClassMethod1331<object>()
		ldstr "G1_C7::ClassMethod1331.4820<System.Object>()"
		ldstr "class G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0> on type class Generated827.G3_C1299`1<class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>
		callvirt instance string class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::ClassMethod1330<object>()
		ldstr "G2_C336::ClassMethod1330.MI.7900<System.Object>()"
		ldstr "class G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0> on type class Generated827.G3_C1299`1<class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>
		callvirt instance string class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::ClassMethod1329()
		ldstr "G2_C336::ClassMethod1329.MI.7899()"
		ldstr "class G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0> on type class Generated827.G3_C1299`1<class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>
		callvirt instance string class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::ClassMethod1328()
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0> on type class Generated827.G3_C1299`1<class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>
		callvirt instance string class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::Method7<object>()
		ldstr "G2_C336::Method7.7895<System.Object>()"
		ldstr "class G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0> on type class Generated827.G3_C1299`1<class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>
		callvirt instance string class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::Method6<object>()
		ldstr "G1_C7::Method6.4813<System.Object>()"
		ldstr "class G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0> on type class Generated827.G3_C1299`1<class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>
		callvirt instance string class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::Method5()
		ldstr "G1_C7::Method5.4812()"
		ldstr "class G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0> on type class Generated827.G3_C1299`1<class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>
		callvirt instance string class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::Method4()
		ldstr "G1_C7::Method4.4811()"
		ldstr "class G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0> on type class Generated827.G3_C1299`1<class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated827.IBase1`1<class Generated827.BaseClass0>::Method4()
		ldstr "G2_C336::Method4.MI.7891()"
		ldstr "class IBase1`1<class Generated827.BaseClass0> on type class Generated827.G3_C1299`1<class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated827.IBase1`1<class Generated827.BaseClass0>::Method5()
		ldstr "G2_C336::Method5.MI.7893()"
		ldstr "class IBase1`1<class Generated827.BaseClass0> on type class Generated827.G3_C1299`1<class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated827.IBase1`1<class Generated827.BaseClass0>::Method6<object>()
		ldstr "G2_C336::Method6.7894<System.Object>()"
		ldstr "class IBase1`1<class Generated827.BaseClass0> on type class Generated827.G3_C1299`1<class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated827.IBase2`2<class Generated827.BaseClass1,class Generated827.BaseClass0>::Method7<object>()
		ldstr "G2_C336::Method7.7895<System.Object>()"
		ldstr "class IBase2`2<class Generated827.BaseClass1,class Generated827.BaseClass0> on type class Generated827.G3_C1299`1<class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated827.IBase2`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::Method7<object>()
		ldstr "G2_C336::Method7.7895<System.Object>()"
		ldstr "class IBase2`2<class Generated827.BaseClass0,class Generated827.BaseClass0> on type class Generated827.G3_C1299`1<class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated827.IBase2`2<class Generated827.BaseClass0,class Generated827.BaseClass1>::Method7<object>()
		ldstr "G2_C336::Method7.7895<System.Object>()"
		ldstr "class IBase2`2<class Generated827.BaseClass0,class Generated827.BaseClass1> on type class Generated827.G3_C1299`1<class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated827.IBase2`2<class Generated827.BaseClass1,class Generated827.BaseClass1>::Method7<object>()
		ldstr "G2_C336::Method7.7895<System.Object>()"
		ldstr "class IBase2`2<class Generated827.BaseClass1,class Generated827.BaseClass1> on type class Generated827.G3_C1299`1<class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>
		callvirt instance string class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>::ClassMethod2017<object>()
		ldstr "G3_C1299::ClassMethod2017.MI.15419<System.Object>()"
		ldstr "class G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1> on type class Generated827.G3_C1299`1<class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>
		callvirt instance string class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>::ClassMethod2016<object>()
		ldstr "G3_C1299::ClassMethod2016.MI.15418<System.Object>()"
		ldstr "class G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1> on type class Generated827.G3_C1299`1<class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>
		callvirt instance string class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>::ClassMethod2015()
		ldstr "G3_C1299::ClassMethod2015.MI.15417()"
		ldstr "class G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1> on type class Generated827.G3_C1299`1<class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>
		callvirt instance string class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>::Method7<object>()
		ldstr "G2_C336::Method7.7895<System.Object>()"
		ldstr "class G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1> on type class Generated827.G3_C1299`1<class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>
		callvirt instance string class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>::Method6<object>()
		ldstr "G2_C336::Method6.7894<System.Object>()"
		ldstr "class G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1> on type class Generated827.G3_C1299`1<class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>
		callvirt instance string class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>::Method5()
		ldstr "G2_C336::Method5.7892()"
		ldstr "class G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1> on type class Generated827.G3_C1299`1<class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>
		callvirt instance string class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>::Method4()
		ldstr "G2_C336::Method4.7890()"
		ldstr "class G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1> on type class Generated827.G3_C1299`1<class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>
		callvirt instance string class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>::ClassMethod1331<object>()
		ldstr "G1_C7::ClassMethod1331.4820<System.Object>()"
		ldstr "class G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1> on type class Generated827.G3_C1299`1<class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>
		callvirt instance string class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>::ClassMethod1330<object>()
		ldstr "G2_C336::ClassMethod1330.MI.7900<System.Object>()"
		ldstr "class G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1> on type class Generated827.G3_C1299`1<class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>
		callvirt instance string class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>::ClassMethod1329()
		ldstr "G2_C336::ClassMethod1329.MI.7899()"
		ldstr "class G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1> on type class Generated827.G3_C1299`1<class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>
		callvirt instance string class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>::ClassMethod1328()
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1> on type class Generated827.G3_C1299`1<class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated827.G3_C1299`1<class Generated827.BaseClass1>
		callvirt instance string class Generated827.G3_C1299`1<class Generated827.BaseClass1>::ClassMethod3978<object>()
		ldstr "G3_C1299::ClassMethod3978.15416<System.Object>()"
		ldstr "class G3_C1299`1<class Generated827.BaseClass1> on type class Generated827.G3_C1299`1<class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated827.G3_C1299`1<class Generated827.BaseClass1>
		callvirt instance string class Generated827.G3_C1299`1<class Generated827.BaseClass1>::ClassMethod3977<object>()
		ldstr "G3_C1299::ClassMethod3977.15415<System.Object>()"
		ldstr "class G3_C1299`1<class Generated827.BaseClass1> on type class Generated827.G3_C1299`1<class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated827.G3_C1299`1<class Generated827.BaseClass1>
		callvirt instance string class Generated827.G3_C1299`1<class Generated827.BaseClass1>::ClassMethod3976()
		ldstr "G3_C1299::ClassMethod3976.15414()"
		ldstr "class G3_C1299`1<class Generated827.BaseClass1> on type class Generated827.G3_C1299`1<class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated827.G3_C1299`1<class Generated827.BaseClass1>
		callvirt instance string class Generated827.G3_C1299`1<class Generated827.BaseClass1>::ClassMethod3975()
		ldstr "G3_C1299::ClassMethod3975.15413()"
		ldstr "class G3_C1299`1<class Generated827.BaseClass1> on type class Generated827.G3_C1299`1<class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated827.G3_C1299`1<class Generated827.BaseClass1>
		callvirt instance string class Generated827.G3_C1299`1<class Generated827.BaseClass1>::ClassMethod2017<object>()
		ldstr "G3_C1299::ClassMethod2017.MI.15419<System.Object>()"
		ldstr "class G3_C1299`1<class Generated827.BaseClass1> on type class Generated827.G3_C1299`1<class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated827.G3_C1299`1<class Generated827.BaseClass1>
		callvirt instance string class Generated827.G3_C1299`1<class Generated827.BaseClass1>::ClassMethod2016<object>()
		ldstr "G3_C1299::ClassMethod2016.MI.15418<System.Object>()"
		ldstr "class G3_C1299`1<class Generated827.BaseClass1> on type class Generated827.G3_C1299`1<class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated827.G3_C1299`1<class Generated827.BaseClass1>
		callvirt instance string class Generated827.G3_C1299`1<class Generated827.BaseClass1>::ClassMethod2015()
		ldstr "G3_C1299::ClassMethod2015.MI.15417()"
		ldstr "class G3_C1299`1<class Generated827.BaseClass1> on type class Generated827.G3_C1299`1<class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated827.G3_C1299`1<class Generated827.BaseClass1>
		callvirt instance string class Generated827.G3_C1299`1<class Generated827.BaseClass1>::Method7<object>()
		ldstr "G2_C336::Method7.7895<System.Object>()"
		ldstr "class G3_C1299`1<class Generated827.BaseClass1> on type class Generated827.G3_C1299`1<class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated827.G3_C1299`1<class Generated827.BaseClass1>
		callvirt instance string class Generated827.G3_C1299`1<class Generated827.BaseClass1>::Method6<object>()
		ldstr "G2_C336::Method6.7894<System.Object>()"
		ldstr "class G3_C1299`1<class Generated827.BaseClass1> on type class Generated827.G3_C1299`1<class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated827.G3_C1299`1<class Generated827.BaseClass1>
		callvirt instance string class Generated827.G3_C1299`1<class Generated827.BaseClass1>::Method5()
		ldstr "G2_C336::Method5.7892()"
		ldstr "class G3_C1299`1<class Generated827.BaseClass1> on type class Generated827.G3_C1299`1<class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated827.G3_C1299`1<class Generated827.BaseClass1>
		callvirt instance string class Generated827.G3_C1299`1<class Generated827.BaseClass1>::Method4()
		ldstr "G2_C336::Method4.7890()"
		ldstr "class G3_C1299`1<class Generated827.BaseClass1> on type class Generated827.G3_C1299`1<class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated827.G3_C1299`1<class Generated827.BaseClass1>
		callvirt instance string class Generated827.G3_C1299`1<class Generated827.BaseClass1>::ClassMethod1331<object>()
		ldstr "G1_C7::ClassMethod1331.4820<System.Object>()"
		ldstr "class G3_C1299`1<class Generated827.BaseClass1> on type class Generated827.G3_C1299`1<class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated827.G3_C1299`1<class Generated827.BaseClass1>
		callvirt instance string class Generated827.G3_C1299`1<class Generated827.BaseClass1>::ClassMethod1330<object>()
		ldstr "G2_C336::ClassMethod1330.MI.7900<System.Object>()"
		ldstr "class G3_C1299`1<class Generated827.BaseClass1> on type class Generated827.G3_C1299`1<class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated827.G3_C1299`1<class Generated827.BaseClass1>
		callvirt instance string class Generated827.G3_C1299`1<class Generated827.BaseClass1>::ClassMethod1329()
		ldstr "G2_C336::ClassMethod1329.MI.7899()"
		ldstr "class G3_C1299`1<class Generated827.BaseClass1> on type class Generated827.G3_C1299`1<class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated827.G3_C1299`1<class Generated827.BaseClass1>
		callvirt instance string class Generated827.G3_C1299`1<class Generated827.BaseClass1>::ClassMethod1328()
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G3_C1299`1<class Generated827.BaseClass1> on type class Generated827.G3_C1299`1<class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>
		callvirt instance string class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::ClassMethod1331<object>()
		ldstr "G1_C7::ClassMethod1331.4820<System.Object>()"
		ldstr "class G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0> on type class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>
		callvirt instance string class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::ClassMethod1330<object>()
		ldstr "G2_C336::ClassMethod1330.MI.7900<System.Object>()"
		ldstr "class G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0> on type class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>
		callvirt instance string class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::ClassMethod1329()
		ldstr "G2_C336::ClassMethod1329.MI.7899()"
		ldstr "class G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0> on type class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>
		callvirt instance string class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::ClassMethod1328()
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0> on type class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>
		callvirt instance string class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::Method7<object>()
		ldstr "G2_C336::Method7.7895<System.Object>()"
		ldstr "class G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0> on type class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>
		callvirt instance string class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::Method6<object>()
		ldstr "G1_C7::Method6.4813<System.Object>()"
		ldstr "class G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0> on type class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>
		callvirt instance string class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::Method5()
		ldstr "G1_C7::Method5.4812()"
		ldstr "class G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0> on type class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>
		callvirt instance string class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::Method4()
		ldstr "G1_C7::Method4.4811()"
		ldstr "class G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0> on type class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated827.IBase1`1<class Generated827.BaseClass0>::Method4()
		ldstr "G2_C336::Method4.MI.7891()"
		ldstr "class IBase1`1<class Generated827.BaseClass0> on type class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated827.IBase1`1<class Generated827.BaseClass0>::Method5()
		ldstr "G2_C336::Method5.MI.7893()"
		ldstr "class IBase1`1<class Generated827.BaseClass0> on type class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated827.IBase1`1<class Generated827.BaseClass0>::Method6<object>()
		ldstr "G2_C336::Method6.7894<System.Object>()"
		ldstr "class IBase1`1<class Generated827.BaseClass0> on type class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated827.IBase2`2<class Generated827.BaseClass1,class Generated827.BaseClass0>::Method7<object>()
		ldstr "G2_C336::Method7.7895<System.Object>()"
		ldstr "class IBase2`2<class Generated827.BaseClass1,class Generated827.BaseClass0> on type class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated827.IBase2`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::Method7<object>()
		ldstr "G2_C336::Method7.7895<System.Object>()"
		ldstr "class IBase2`2<class Generated827.BaseClass0,class Generated827.BaseClass0> on type class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated827.IBase2`2<class Generated827.BaseClass0,class Generated827.BaseClass1>::Method7<object>()
		ldstr "G2_C336::Method7.7895<System.Object>()"
		ldstr "class IBase2`2<class Generated827.BaseClass0,class Generated827.BaseClass1> on type class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated827.IBase2`2<class Generated827.BaseClass1,class Generated827.BaseClass1>::Method7<object>()
		ldstr "G2_C336::Method7.7895<System.Object>()"
		ldstr "class IBase2`2<class Generated827.BaseClass1,class Generated827.BaseClass1> on type class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>
		callvirt instance string class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::ClassMethod2017<object>()
		ldstr "G2_C336::ClassMethod2017.7898<System.Object>()"
		ldstr "class G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0> on type class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>
		callvirt instance string class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::ClassMethod2016<object>()
		ldstr "G2_C336::ClassMethod2016.7897<System.Object>()"
		ldstr "class G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0> on type class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>
		callvirt instance string class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::ClassMethod2015()
		ldstr "G2_C336::ClassMethod2015.7896()"
		ldstr "class G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0> on type class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>
		callvirt instance string class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::Method7<object>()
		ldstr "G2_C336::Method7.7895<System.Object>()"
		ldstr "class G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0> on type class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>
		callvirt instance string class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::Method6<object>()
		ldstr "G2_C336::Method6.7894<System.Object>()"
		ldstr "class G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0> on type class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>
		callvirt instance string class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::Method5()
		ldstr "G2_C336::Method5.7892()"
		ldstr "class G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0> on type class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>
		callvirt instance string class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::Method4()
		ldstr "G2_C336::Method4.7890()"
		ldstr "class G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0> on type class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>
		callvirt instance string class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::ClassMethod1331<object>()
		ldstr "G1_C7::ClassMethod1331.4820<System.Object>()"
		ldstr "class G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0> on type class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>
		callvirt instance string class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::ClassMethod1330<object>()
		ldstr "G2_C336::ClassMethod1330.MI.7900<System.Object>()"
		ldstr "class G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0> on type class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>
		callvirt instance string class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::ClassMethod1329()
		ldstr "G2_C336::ClassMethod1329.MI.7899()"
		ldstr "class G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0> on type class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>
		callvirt instance string class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::ClassMethod1328()
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0> on type class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>
		callvirt instance string class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::ClassMethod1331<object>()
		ldstr "G1_C7::ClassMethod1331.4820<System.Object>()"
		ldstr "class G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0> on type class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>
		callvirt instance string class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::ClassMethod1330<object>()
		ldstr "G2_C336::ClassMethod1330.MI.7900<System.Object>()"
		ldstr "class G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0> on type class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>
		callvirt instance string class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::ClassMethod1329()
		ldstr "G2_C336::ClassMethod1329.MI.7899()"
		ldstr "class G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0> on type class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>
		callvirt instance string class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::ClassMethod1328()
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0> on type class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>
		callvirt instance string class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::Method7<object>()
		ldstr "G2_C336::Method7.7895<System.Object>()"
		ldstr "class G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0> on type class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>
		callvirt instance string class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::Method6<object>()
		ldstr "G1_C7::Method6.4813<System.Object>()"
		ldstr "class G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0> on type class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>
		callvirt instance string class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::Method5()
		ldstr "G1_C7::Method5.4812()"
		ldstr "class G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0> on type class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>
		callvirt instance string class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::Method4()
		ldstr "G1_C7::Method4.4811()"
		ldstr "class G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0> on type class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated827.IBase1`1<class Generated827.BaseClass0>::Method4()
		ldstr "G2_C336::Method4.MI.7891()"
		ldstr "class IBase1`1<class Generated827.BaseClass0> on type class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated827.IBase1`1<class Generated827.BaseClass0>::Method5()
		ldstr "G2_C336::Method5.MI.7893()"
		ldstr "class IBase1`1<class Generated827.BaseClass0> on type class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated827.IBase1`1<class Generated827.BaseClass0>::Method6<object>()
		ldstr "G2_C336::Method6.7894<System.Object>()"
		ldstr "class IBase1`1<class Generated827.BaseClass0> on type class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated827.IBase2`2<class Generated827.BaseClass1,class Generated827.BaseClass0>::Method7<object>()
		ldstr "G2_C336::Method7.7895<System.Object>()"
		ldstr "class IBase2`2<class Generated827.BaseClass1,class Generated827.BaseClass0> on type class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated827.IBase2`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::Method7<object>()
		ldstr "G2_C336::Method7.7895<System.Object>()"
		ldstr "class IBase2`2<class Generated827.BaseClass0,class Generated827.BaseClass0> on type class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated827.IBase2`2<class Generated827.BaseClass0,class Generated827.BaseClass1>::Method7<object>()
		ldstr "G2_C336::Method7.7895<System.Object>()"
		ldstr "class IBase2`2<class Generated827.BaseClass0,class Generated827.BaseClass1> on type class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated827.IBase2`2<class Generated827.BaseClass1,class Generated827.BaseClass1>::Method7<object>()
		ldstr "G2_C336::Method7.7895<System.Object>()"
		ldstr "class IBase2`2<class Generated827.BaseClass1,class Generated827.BaseClass1> on type class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>
		callvirt instance string class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>::ClassMethod2017<object>()
		ldstr "G2_C336::ClassMethod2017.7898<System.Object>()"
		ldstr "class G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1> on type class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>
		callvirt instance string class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>::ClassMethod2016<object>()
		ldstr "G2_C336::ClassMethod2016.7897<System.Object>()"
		ldstr "class G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1> on type class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>
		callvirt instance string class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>::ClassMethod2015()
		ldstr "G2_C336::ClassMethod2015.7896()"
		ldstr "class G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1> on type class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>
		callvirt instance string class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>::Method7<object>()
		ldstr "G2_C336::Method7.7895<System.Object>()"
		ldstr "class G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1> on type class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>
		callvirt instance string class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>::Method6<object>()
		ldstr "G2_C336::Method6.7894<System.Object>()"
		ldstr "class G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1> on type class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>
		callvirt instance string class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>::Method5()
		ldstr "G2_C336::Method5.7892()"
		ldstr "class G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1> on type class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>
		callvirt instance string class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>::Method4()
		ldstr "G2_C336::Method4.7890()"
		ldstr "class G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1> on type class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>
		callvirt instance string class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>::ClassMethod1331<object>()
		ldstr "G1_C7::ClassMethod1331.4820<System.Object>()"
		ldstr "class G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1> on type class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>
		callvirt instance string class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>::ClassMethod1330<object>()
		ldstr "G2_C336::ClassMethod1330.MI.7900<System.Object>()"
		ldstr "class G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1> on type class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>
		callvirt instance string class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>::ClassMethod1329()
		ldstr "G2_C336::ClassMethod1329.MI.7899()"
		ldstr "class G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1> on type class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>
		callvirt instance string class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>::ClassMethod1328()
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1> on type class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass1>
		callvirt instance string class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass1>::ClassMethod1331<object>()
		ldstr "G1_C7::ClassMethod1331.4820<System.Object>()"
		ldstr "class G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass1> on type class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass1>
		callvirt instance string class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass1>::ClassMethod1330<object>()
		ldstr "G2_C336::ClassMethod1330.MI.7900<System.Object>()"
		ldstr "class G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass1> on type class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass1>
		callvirt instance string class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass1>::ClassMethod1329()
		ldstr "G2_C336::ClassMethod1329.MI.7899()"
		ldstr "class G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass1> on type class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass1>
		callvirt instance string class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass1>::ClassMethod1328()
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass1> on type class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass1>
		callvirt instance string class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass1>::Method7<object>()
		ldstr "G2_C336::Method7.7895<System.Object>()"
		ldstr "class G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass1> on type class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass1>
		callvirt instance string class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass1>::Method6<object>()
		ldstr "G1_C7::Method6.4813<System.Object>()"
		ldstr "class G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass1> on type class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass1>
		callvirt instance string class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass1>::Method5()
		ldstr "G1_C7::Method5.4812()"
		ldstr "class G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass1> on type class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass1>
		callvirt instance string class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass1>::Method4()
		ldstr "G1_C7::Method4.4811()"
		ldstr "class G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass1> on type class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated827.IBase1`1<class Generated827.BaseClass0>::Method4()
		ldstr "G2_C336::Method4.MI.7891()"
		ldstr "class IBase1`1<class Generated827.BaseClass0> on type class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated827.IBase1`1<class Generated827.BaseClass0>::Method5()
		ldstr "G2_C336::Method5.MI.7893()"
		ldstr "class IBase1`1<class Generated827.BaseClass0> on type class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated827.IBase1`1<class Generated827.BaseClass0>::Method6<object>()
		ldstr "G2_C336::Method6.7894<System.Object>()"
		ldstr "class IBase1`1<class Generated827.BaseClass0> on type class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated827.IBase2`2<class Generated827.BaseClass1,class Generated827.BaseClass0>::Method7<object>()
		ldstr "G1_C7::Method7.MI.4816<System.Object>()"
		ldstr "class IBase2`2<class Generated827.BaseClass1,class Generated827.BaseClass0> on type class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated827.IBase2`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::Method7<object>()
		ldstr "G1_C7::Method7.MI.4816<System.Object>()"
		ldstr "class IBase2`2<class Generated827.BaseClass0,class Generated827.BaseClass0> on type class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated827.IBase2`2<class Generated827.BaseClass0,class Generated827.BaseClass1>::Method7<object>()
		ldstr "G2_C336::Method7.7895<System.Object>()"
		ldstr "class IBase2`2<class Generated827.BaseClass0,class Generated827.BaseClass1> on type class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated827.IBase2`2<class Generated827.BaseClass1,class Generated827.BaseClass1>::Method7<object>()
		ldstr "G2_C336::Method7.7895<System.Object>()"
		ldstr "class IBase2`2<class Generated827.BaseClass1,class Generated827.BaseClass1> on type class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>
		callvirt instance string class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>::ClassMethod2017<object>()
		ldstr "G2_C336::ClassMethod2017.7898<System.Object>()"
		ldstr "class G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0> on type class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>
		callvirt instance string class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>::ClassMethod2016<object>()
		ldstr "G2_C336::ClassMethod2016.7897<System.Object>()"
		ldstr "class G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0> on type class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>
		callvirt instance string class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>::ClassMethod2015()
		ldstr "G2_C336::ClassMethod2015.7896()"
		ldstr "class G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0> on type class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>
		callvirt instance string class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>::Method7<object>()
		ldstr "G2_C336::Method7.7895<System.Object>()"
		ldstr "class G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0> on type class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>
		callvirt instance string class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>::Method6<object>()
		ldstr "G2_C336::Method6.7894<System.Object>()"
		ldstr "class G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0> on type class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>
		callvirt instance string class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>::Method5()
		ldstr "G2_C336::Method5.7892()"
		ldstr "class G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0> on type class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>
		callvirt instance string class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>::Method4()
		ldstr "G2_C336::Method4.7890()"
		ldstr "class G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0> on type class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>
		callvirt instance string class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>::ClassMethod1331<object>()
		ldstr "G1_C7::ClassMethod1331.4820<System.Object>()"
		ldstr "class G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0> on type class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>
		callvirt instance string class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>::ClassMethod1330<object>()
		ldstr "G2_C336::ClassMethod1330.MI.7900<System.Object>()"
		ldstr "class G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0> on type class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>
		callvirt instance string class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>::ClassMethod1329()
		ldstr "G2_C336::ClassMethod1329.MI.7899()"
		ldstr "class G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0> on type class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>
		callvirt instance string class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>::ClassMethod1328()
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0> on type class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass1>
		callvirt instance string class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass1>::ClassMethod1331<object>()
		ldstr "G1_C7::ClassMethod1331.4820<System.Object>()"
		ldstr "class G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass1> on type class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass1>
		callvirt instance string class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass1>::ClassMethod1330<object>()
		ldstr "G2_C336::ClassMethod1330.MI.7900<System.Object>()"
		ldstr "class G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass1> on type class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass1>
		callvirt instance string class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass1>::ClassMethod1329()
		ldstr "G2_C336::ClassMethod1329.MI.7899()"
		ldstr "class G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass1> on type class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass1>
		callvirt instance string class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass1>::ClassMethod1328()
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass1> on type class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass1>
		callvirt instance string class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass1>::Method7<object>()
		ldstr "G2_C336::Method7.7895<System.Object>()"
		ldstr "class G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass1> on type class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass1>
		callvirt instance string class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass1>::Method6<object>()
		ldstr "G1_C7::Method6.4813<System.Object>()"
		ldstr "class G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass1> on type class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass1>
		callvirt instance string class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass1>::Method5()
		ldstr "G1_C7::Method5.4812()"
		ldstr "class G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass1> on type class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass1>
		callvirt instance string class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass1>::Method4()
		ldstr "G1_C7::Method4.4811()"
		ldstr "class G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass1> on type class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated827.IBase1`1<class Generated827.BaseClass0>::Method4()
		ldstr "G2_C336::Method4.MI.7891()"
		ldstr "class IBase1`1<class Generated827.BaseClass0> on type class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated827.IBase1`1<class Generated827.BaseClass0>::Method5()
		ldstr "G2_C336::Method5.MI.7893()"
		ldstr "class IBase1`1<class Generated827.BaseClass0> on type class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated827.IBase1`1<class Generated827.BaseClass0>::Method6<object>()
		ldstr "G2_C336::Method6.7894<System.Object>()"
		ldstr "class IBase1`1<class Generated827.BaseClass0> on type class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated827.IBase2`2<class Generated827.BaseClass1,class Generated827.BaseClass0>::Method7<object>()
		ldstr "G1_C7::Method7.MI.4816<System.Object>()"
		ldstr "class IBase2`2<class Generated827.BaseClass1,class Generated827.BaseClass0> on type class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated827.IBase2`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::Method7<object>()
		ldstr "G1_C7::Method7.MI.4816<System.Object>()"
		ldstr "class IBase2`2<class Generated827.BaseClass0,class Generated827.BaseClass0> on type class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated827.IBase2`2<class Generated827.BaseClass0,class Generated827.BaseClass1>::Method7<object>()
		ldstr "G2_C336::Method7.7895<System.Object>()"
		ldstr "class IBase2`2<class Generated827.BaseClass0,class Generated827.BaseClass1> on type class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated827.IBase2`2<class Generated827.BaseClass1,class Generated827.BaseClass1>::Method7<object>()
		ldstr "G2_C336::Method7.7895<System.Object>()"
		ldstr "class IBase2`2<class Generated827.BaseClass1,class Generated827.BaseClass1> on type class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>
		callvirt instance string class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>::ClassMethod2017<object>()
		ldstr "G2_C336::ClassMethod2017.7898<System.Object>()"
		ldstr "class G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1> on type class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>
		callvirt instance string class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>::ClassMethod2016<object>()
		ldstr "G2_C336::ClassMethod2016.7897<System.Object>()"
		ldstr "class G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1> on type class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>
		callvirt instance string class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>::ClassMethod2015()
		ldstr "G2_C336::ClassMethod2015.7896()"
		ldstr "class G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1> on type class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>
		callvirt instance string class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>::Method7<object>()
		ldstr "G2_C336::Method7.7895<System.Object>()"
		ldstr "class G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1> on type class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>
		callvirt instance string class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>::Method6<object>()
		ldstr "G2_C336::Method6.7894<System.Object>()"
		ldstr "class G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1> on type class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>
		callvirt instance string class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>::Method5()
		ldstr "G2_C336::Method5.7892()"
		ldstr "class G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1> on type class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>
		callvirt instance string class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>::Method4()
		ldstr "G2_C336::Method4.7890()"
		ldstr "class G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1> on type class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>
		callvirt instance string class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>::ClassMethod1331<object>()
		ldstr "G1_C7::ClassMethod1331.4820<System.Object>()"
		ldstr "class G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1> on type class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>
		callvirt instance string class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>::ClassMethod1330<object>()
		ldstr "G2_C336::ClassMethod1330.MI.7900<System.Object>()"
		ldstr "class G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1> on type class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>
		callvirt instance string class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>::ClassMethod1329()
		ldstr "G2_C336::ClassMethod1329.MI.7899()"
		ldstr "class G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1> on type class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>
		callvirt instance string class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>::ClassMethod1328()
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1> on type class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>"
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
		newobj instance void class Generated827.G3_C1299`1<class Generated827.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C336::ClassMethod1329.MI.7899()#G2_C336::ClassMethod1330.MI.7900<System.Object>()#G1_C7::ClassMethod1331.4820<System.Object>()#G1_C7::Method4.4811()#G1_C7::Method5.4812()#G1_C7::Method6.4813<System.Object>()#G2_C336::Method7.7895<System.Object>()#"
		call void Generated827.Generated827::M.G1_C7.T.T<class Generated827.BaseClass0,class Generated827.BaseClass0,class Generated827.G3_C1299`1<class Generated827.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C336::ClassMethod1329.MI.7899()#G2_C336::ClassMethod1330.MI.7900<System.Object>()#G1_C7::ClassMethod1331.4820<System.Object>()#G1_C7::Method4.4811()#G1_C7::Method5.4812()#G1_C7::Method6.4813<System.Object>()#G2_C336::Method7.7895<System.Object>()#"
		call void Generated827.Generated827::M.G1_C7.A.T<class Generated827.BaseClass0,class Generated827.G3_C1299`1<class Generated827.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C336::ClassMethod1329.MI.7899()#G2_C336::ClassMethod1330.MI.7900<System.Object>()#G1_C7::ClassMethod1331.4820<System.Object>()#G1_C7::Method4.4811()#G1_C7::Method5.4812()#G1_C7::Method6.4813<System.Object>()#G2_C336::Method7.7895<System.Object>()#"
		call void Generated827.Generated827::M.G1_C7.A.A<class Generated827.G3_C1299`1<class Generated827.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C336::Method4.MI.7891()#G2_C336::Method5.MI.7893()#G2_C336::Method6.7894<System.Object>()#"
		call void Generated827.Generated827::M.IBase1.T<class Generated827.BaseClass0,class Generated827.G3_C1299`1<class Generated827.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C336::Method4.MI.7891()#G2_C336::Method5.MI.7893()#G2_C336::Method6.7894<System.Object>()#"
		call void Generated827.Generated827::M.IBase1.A<class Generated827.G3_C1299`1<class Generated827.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C336::Method7.7895<System.Object>()#"
		call void Generated827.Generated827::M.IBase2.T.T<class Generated827.BaseClass1,class Generated827.BaseClass0,class Generated827.G3_C1299`1<class Generated827.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C336::Method7.7895<System.Object>()#"
		call void Generated827.Generated827::M.IBase2.B.T<class Generated827.BaseClass0,class Generated827.G3_C1299`1<class Generated827.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C336::Method7.7895<System.Object>()#"
		call void Generated827.Generated827::M.IBase2.B.A<class Generated827.G3_C1299`1<class Generated827.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C336::Method7.7895<System.Object>()#"
		call void Generated827.Generated827::M.IBase2.T.T<class Generated827.BaseClass0,class Generated827.BaseClass0,class Generated827.G3_C1299`1<class Generated827.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C336::Method7.7895<System.Object>()#"
		call void Generated827.Generated827::M.IBase2.A.T<class Generated827.BaseClass0,class Generated827.G3_C1299`1<class Generated827.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C336::Method7.7895<System.Object>()#"
		call void Generated827.Generated827::M.IBase2.A.A<class Generated827.G3_C1299`1<class Generated827.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C336::Method7.7895<System.Object>()#"
		call void Generated827.Generated827::M.IBase2.T.T<class Generated827.BaseClass0,class Generated827.BaseClass1,class Generated827.G3_C1299`1<class Generated827.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C336::Method7.7895<System.Object>()#"
		call void Generated827.Generated827::M.IBase2.A.T<class Generated827.BaseClass1,class Generated827.G3_C1299`1<class Generated827.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C336::Method7.7895<System.Object>()#"
		call void Generated827.Generated827::M.IBase2.A.B<class Generated827.G3_C1299`1<class Generated827.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C336::Method7.7895<System.Object>()#"
		call void Generated827.Generated827::M.IBase2.T.T<class Generated827.BaseClass1,class Generated827.BaseClass1,class Generated827.G3_C1299`1<class Generated827.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C336::Method7.7895<System.Object>()#"
		call void Generated827.Generated827::M.IBase2.B.T<class Generated827.BaseClass1,class Generated827.G3_C1299`1<class Generated827.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C336::Method7.7895<System.Object>()#"
		call void Generated827.Generated827::M.IBase2.B.B<class Generated827.G3_C1299`1<class Generated827.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C336::ClassMethod1329.MI.7899()#G2_C336::ClassMethod1330.MI.7900<System.Object>()#G1_C7::ClassMethod1331.4820<System.Object>()#G3_C1299::ClassMethod2015.MI.15417()#G3_C1299::ClassMethod2016.MI.15418<System.Object>()#G3_C1299::ClassMethod2017.MI.15419<System.Object>()#G2_C336::Method4.7890()#G2_C336::Method5.7892()#G2_C336::Method6.7894<System.Object>()#G2_C336::Method7.7895<System.Object>()#"
		call void Generated827.Generated827::M.G2_C336.T.T<class Generated827.BaseClass0,class Generated827.BaseClass0,class Generated827.G3_C1299`1<class Generated827.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C336::ClassMethod1329.MI.7899()#G2_C336::ClassMethod1330.MI.7900<System.Object>()#G1_C7::ClassMethod1331.4820<System.Object>()#G3_C1299::ClassMethod2015.MI.15417()#G3_C1299::ClassMethod2016.MI.15418<System.Object>()#G3_C1299::ClassMethod2017.MI.15419<System.Object>()#G2_C336::Method4.7890()#G2_C336::Method5.7892()#G2_C336::Method6.7894<System.Object>()#G2_C336::Method7.7895<System.Object>()#"
		call void Generated827.Generated827::M.G2_C336.A.T<class Generated827.BaseClass0,class Generated827.G3_C1299`1<class Generated827.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C336::ClassMethod1329.MI.7899()#G2_C336::ClassMethod1330.MI.7900<System.Object>()#G1_C7::ClassMethod1331.4820<System.Object>()#G3_C1299::ClassMethod2015.MI.15417()#G3_C1299::ClassMethod2016.MI.15418<System.Object>()#G3_C1299::ClassMethod2017.MI.15419<System.Object>()#G2_C336::Method4.7890()#G2_C336::Method5.7892()#G2_C336::Method6.7894<System.Object>()#G2_C336::Method7.7895<System.Object>()#"
		call void Generated827.Generated827::M.G2_C336.A.A<class Generated827.G3_C1299`1<class Generated827.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C336::ClassMethod1329.MI.7899()#G2_C336::ClassMethod1330.MI.7900<System.Object>()#G1_C7::ClassMethod1331.4820<System.Object>()#G3_C1299::ClassMethod2015.MI.15417()#G3_C1299::ClassMethod2016.MI.15418<System.Object>()#G3_C1299::ClassMethod2017.MI.15419<System.Object>()#G3_C1299::ClassMethod3975.15413()#G3_C1299::ClassMethod3976.15414()#G3_C1299::ClassMethod3977.15415<System.Object>()#G3_C1299::ClassMethod3978.15416<System.Object>()#G2_C336::Method4.7890()#G2_C336::Method5.7892()#G2_C336::Method6.7894<System.Object>()#G2_C336::Method7.7895<System.Object>()#"
		call void Generated827.Generated827::M.G3_C1299.T<class Generated827.BaseClass0,class Generated827.G3_C1299`1<class Generated827.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C336::ClassMethod1329.MI.7899()#G2_C336::ClassMethod1330.MI.7900<System.Object>()#G1_C7::ClassMethod1331.4820<System.Object>()#G3_C1299::ClassMethod2015.MI.15417()#G3_C1299::ClassMethod2016.MI.15418<System.Object>()#G3_C1299::ClassMethod2017.MI.15419<System.Object>()#G3_C1299::ClassMethod3975.15413()#G3_C1299::ClassMethod3976.15414()#G3_C1299::ClassMethod3977.15415<System.Object>()#G3_C1299::ClassMethod3978.15416<System.Object>()#G2_C336::Method4.7890()#G2_C336::Method5.7892()#G2_C336::Method6.7894<System.Object>()#G2_C336::Method7.7895<System.Object>()#"
		call void Generated827.Generated827::M.G3_C1299.A<class Generated827.G3_C1299`1<class Generated827.BaseClass0>>(!!0,string)
		newobj instance void class Generated827.G3_C1299`1<class Generated827.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C336::ClassMethod1329.MI.7899()#G2_C336::ClassMethod1330.MI.7900<System.Object>()#G1_C7::ClassMethod1331.4820<System.Object>()#G1_C7::Method4.4811()#G1_C7::Method5.4812()#G1_C7::Method6.4813<System.Object>()#G2_C336::Method7.7895<System.Object>()#"
		call void Generated827.Generated827::M.G1_C7.T.T<class Generated827.BaseClass0,class Generated827.BaseClass0,class Generated827.G3_C1299`1<class Generated827.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C336::ClassMethod1329.MI.7899()#G2_C336::ClassMethod1330.MI.7900<System.Object>()#G1_C7::ClassMethod1331.4820<System.Object>()#G1_C7::Method4.4811()#G1_C7::Method5.4812()#G1_C7::Method6.4813<System.Object>()#G2_C336::Method7.7895<System.Object>()#"
		call void Generated827.Generated827::M.G1_C7.A.T<class Generated827.BaseClass0,class Generated827.G3_C1299`1<class Generated827.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C336::ClassMethod1329.MI.7899()#G2_C336::ClassMethod1330.MI.7900<System.Object>()#G1_C7::ClassMethod1331.4820<System.Object>()#G1_C7::Method4.4811()#G1_C7::Method5.4812()#G1_C7::Method6.4813<System.Object>()#G2_C336::Method7.7895<System.Object>()#"
		call void Generated827.Generated827::M.G1_C7.A.A<class Generated827.G3_C1299`1<class Generated827.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C336::Method4.MI.7891()#G2_C336::Method5.MI.7893()#G2_C336::Method6.7894<System.Object>()#"
		call void Generated827.Generated827::M.IBase1.T<class Generated827.BaseClass0,class Generated827.G3_C1299`1<class Generated827.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C336::Method4.MI.7891()#G2_C336::Method5.MI.7893()#G2_C336::Method6.7894<System.Object>()#"
		call void Generated827.Generated827::M.IBase1.A<class Generated827.G3_C1299`1<class Generated827.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C336::Method7.7895<System.Object>()#"
		call void Generated827.Generated827::M.IBase2.T.T<class Generated827.BaseClass1,class Generated827.BaseClass0,class Generated827.G3_C1299`1<class Generated827.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C336::Method7.7895<System.Object>()#"
		call void Generated827.Generated827::M.IBase2.B.T<class Generated827.BaseClass0,class Generated827.G3_C1299`1<class Generated827.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C336::Method7.7895<System.Object>()#"
		call void Generated827.Generated827::M.IBase2.B.A<class Generated827.G3_C1299`1<class Generated827.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C336::Method7.7895<System.Object>()#"
		call void Generated827.Generated827::M.IBase2.T.T<class Generated827.BaseClass0,class Generated827.BaseClass0,class Generated827.G3_C1299`1<class Generated827.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C336::Method7.7895<System.Object>()#"
		call void Generated827.Generated827::M.IBase2.A.T<class Generated827.BaseClass0,class Generated827.G3_C1299`1<class Generated827.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C336::Method7.7895<System.Object>()#"
		call void Generated827.Generated827::M.IBase2.A.A<class Generated827.G3_C1299`1<class Generated827.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C336::Method7.7895<System.Object>()#"
		call void Generated827.Generated827::M.IBase2.T.T<class Generated827.BaseClass0,class Generated827.BaseClass1,class Generated827.G3_C1299`1<class Generated827.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C336::Method7.7895<System.Object>()#"
		call void Generated827.Generated827::M.IBase2.A.T<class Generated827.BaseClass1,class Generated827.G3_C1299`1<class Generated827.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C336::Method7.7895<System.Object>()#"
		call void Generated827.Generated827::M.IBase2.A.B<class Generated827.G3_C1299`1<class Generated827.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C336::Method7.7895<System.Object>()#"
		call void Generated827.Generated827::M.IBase2.T.T<class Generated827.BaseClass1,class Generated827.BaseClass1,class Generated827.G3_C1299`1<class Generated827.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C336::Method7.7895<System.Object>()#"
		call void Generated827.Generated827::M.IBase2.B.T<class Generated827.BaseClass1,class Generated827.G3_C1299`1<class Generated827.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C336::Method7.7895<System.Object>()#"
		call void Generated827.Generated827::M.IBase2.B.B<class Generated827.G3_C1299`1<class Generated827.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C336::ClassMethod1329.MI.7899()#G2_C336::ClassMethod1330.MI.7900<System.Object>()#G1_C7::ClassMethod1331.4820<System.Object>()#G3_C1299::ClassMethod2015.MI.15417()#G3_C1299::ClassMethod2016.MI.15418<System.Object>()#G3_C1299::ClassMethod2017.MI.15419<System.Object>()#G2_C336::Method4.7890()#G2_C336::Method5.7892()#G2_C336::Method6.7894<System.Object>()#G2_C336::Method7.7895<System.Object>()#"
		call void Generated827.Generated827::M.G2_C336.T.T<class Generated827.BaseClass0,class Generated827.BaseClass1,class Generated827.G3_C1299`1<class Generated827.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C336::ClassMethod1329.MI.7899()#G2_C336::ClassMethod1330.MI.7900<System.Object>()#G1_C7::ClassMethod1331.4820<System.Object>()#G3_C1299::ClassMethod2015.MI.15417()#G3_C1299::ClassMethod2016.MI.15418<System.Object>()#G3_C1299::ClassMethod2017.MI.15419<System.Object>()#G2_C336::Method4.7890()#G2_C336::Method5.7892()#G2_C336::Method6.7894<System.Object>()#G2_C336::Method7.7895<System.Object>()#"
		call void Generated827.Generated827::M.G2_C336.A.T<class Generated827.BaseClass1,class Generated827.G3_C1299`1<class Generated827.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C336::ClassMethod1329.MI.7899()#G2_C336::ClassMethod1330.MI.7900<System.Object>()#G1_C7::ClassMethod1331.4820<System.Object>()#G3_C1299::ClassMethod2015.MI.15417()#G3_C1299::ClassMethod2016.MI.15418<System.Object>()#G3_C1299::ClassMethod2017.MI.15419<System.Object>()#G2_C336::Method4.7890()#G2_C336::Method5.7892()#G2_C336::Method6.7894<System.Object>()#G2_C336::Method7.7895<System.Object>()#"
		call void Generated827.Generated827::M.G2_C336.A.B<class Generated827.G3_C1299`1<class Generated827.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C336::ClassMethod1329.MI.7899()#G2_C336::ClassMethod1330.MI.7900<System.Object>()#G1_C7::ClassMethod1331.4820<System.Object>()#G3_C1299::ClassMethod2015.MI.15417()#G3_C1299::ClassMethod2016.MI.15418<System.Object>()#G3_C1299::ClassMethod2017.MI.15419<System.Object>()#G3_C1299::ClassMethod3975.15413()#G3_C1299::ClassMethod3976.15414()#G3_C1299::ClassMethod3977.15415<System.Object>()#G3_C1299::ClassMethod3978.15416<System.Object>()#G2_C336::Method4.7890()#G2_C336::Method5.7892()#G2_C336::Method6.7894<System.Object>()#G2_C336::Method7.7895<System.Object>()#"
		call void Generated827.Generated827::M.G3_C1299.T<class Generated827.BaseClass1,class Generated827.G3_C1299`1<class Generated827.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C336::ClassMethod1329.MI.7899()#G2_C336::ClassMethod1330.MI.7900<System.Object>()#G1_C7::ClassMethod1331.4820<System.Object>()#G3_C1299::ClassMethod2015.MI.15417()#G3_C1299::ClassMethod2016.MI.15418<System.Object>()#G3_C1299::ClassMethod2017.MI.15419<System.Object>()#G3_C1299::ClassMethod3975.15413()#G3_C1299::ClassMethod3976.15414()#G3_C1299::ClassMethod3977.15415<System.Object>()#G3_C1299::ClassMethod3978.15416<System.Object>()#G2_C336::Method4.7890()#G2_C336::Method5.7892()#G2_C336::Method6.7894<System.Object>()#G2_C336::Method7.7895<System.Object>()#"
		call void Generated827.Generated827::M.G3_C1299.B<class Generated827.G3_C1299`1<class Generated827.BaseClass1>>(!!0,string)
		newobj instance void class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C336::ClassMethod1329.MI.7899()#G2_C336::ClassMethod1330.MI.7900<System.Object>()#G1_C7::ClassMethod1331.4820<System.Object>()#G1_C7::Method4.4811()#G1_C7::Method5.4812()#G1_C7::Method6.4813<System.Object>()#G2_C336::Method7.7895<System.Object>()#"
		call void Generated827.Generated827::M.G1_C7.T.T<class Generated827.BaseClass0,class Generated827.BaseClass0,class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C336::ClassMethod1329.MI.7899()#G2_C336::ClassMethod1330.MI.7900<System.Object>()#G1_C7::ClassMethod1331.4820<System.Object>()#G1_C7::Method4.4811()#G1_C7::Method5.4812()#G1_C7::Method6.4813<System.Object>()#G2_C336::Method7.7895<System.Object>()#"
		call void Generated827.Generated827::M.G1_C7.A.T<class Generated827.BaseClass0,class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C336::ClassMethod1329.MI.7899()#G2_C336::ClassMethod1330.MI.7900<System.Object>()#G1_C7::ClassMethod1331.4820<System.Object>()#G1_C7::Method4.4811()#G1_C7::Method5.4812()#G1_C7::Method6.4813<System.Object>()#G2_C336::Method7.7895<System.Object>()#"
		call void Generated827.Generated827::M.G1_C7.A.A<class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C336::Method4.MI.7891()#G2_C336::Method5.MI.7893()#G2_C336::Method6.7894<System.Object>()#"
		call void Generated827.Generated827::M.IBase1.T<class Generated827.BaseClass0,class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C336::Method4.MI.7891()#G2_C336::Method5.MI.7893()#G2_C336::Method6.7894<System.Object>()#"
		call void Generated827.Generated827::M.IBase1.A<class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C336::Method7.7895<System.Object>()#"
		call void Generated827.Generated827::M.IBase2.T.T<class Generated827.BaseClass1,class Generated827.BaseClass0,class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C336::Method7.7895<System.Object>()#"
		call void Generated827.Generated827::M.IBase2.B.T<class Generated827.BaseClass0,class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C336::Method7.7895<System.Object>()#"
		call void Generated827.Generated827::M.IBase2.B.A<class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C336::Method7.7895<System.Object>()#"
		call void Generated827.Generated827::M.IBase2.T.T<class Generated827.BaseClass0,class Generated827.BaseClass0,class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C336::Method7.7895<System.Object>()#"
		call void Generated827.Generated827::M.IBase2.A.T<class Generated827.BaseClass0,class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C336::Method7.7895<System.Object>()#"
		call void Generated827.Generated827::M.IBase2.A.A<class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C336::Method7.7895<System.Object>()#"
		call void Generated827.Generated827::M.IBase2.T.T<class Generated827.BaseClass0,class Generated827.BaseClass1,class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C336::Method7.7895<System.Object>()#"
		call void Generated827.Generated827::M.IBase2.A.T<class Generated827.BaseClass1,class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C336::Method7.7895<System.Object>()#"
		call void Generated827.Generated827::M.IBase2.A.B<class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C336::Method7.7895<System.Object>()#"
		call void Generated827.Generated827::M.IBase2.T.T<class Generated827.BaseClass1,class Generated827.BaseClass1,class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C336::Method7.7895<System.Object>()#"
		call void Generated827.Generated827::M.IBase2.B.T<class Generated827.BaseClass1,class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C336::Method7.7895<System.Object>()#"
		call void Generated827.Generated827::M.IBase2.B.B<class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C336::ClassMethod1329.MI.7899()#G2_C336::ClassMethod1330.MI.7900<System.Object>()#G1_C7::ClassMethod1331.4820<System.Object>()#G2_C336::ClassMethod2015.7896()#G2_C336::ClassMethod2016.7897<System.Object>()#G2_C336::ClassMethod2017.7898<System.Object>()#G2_C336::Method4.7890()#G2_C336::Method5.7892()#G2_C336::Method6.7894<System.Object>()#G2_C336::Method7.7895<System.Object>()#"
		call void Generated827.Generated827::M.G2_C336.T.T<class Generated827.BaseClass0,class Generated827.BaseClass0,class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C336::ClassMethod1329.MI.7899()#G2_C336::ClassMethod1330.MI.7900<System.Object>()#G1_C7::ClassMethod1331.4820<System.Object>()#G2_C336::ClassMethod2015.7896()#G2_C336::ClassMethod2016.7897<System.Object>()#G2_C336::ClassMethod2017.7898<System.Object>()#G2_C336::Method4.7890()#G2_C336::Method5.7892()#G2_C336::Method6.7894<System.Object>()#G2_C336::Method7.7895<System.Object>()#"
		call void Generated827.Generated827::M.G2_C336.A.T<class Generated827.BaseClass0,class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C336::ClassMethod1329.MI.7899()#G2_C336::ClassMethod1330.MI.7900<System.Object>()#G1_C7::ClassMethod1331.4820<System.Object>()#G2_C336::ClassMethod2015.7896()#G2_C336::ClassMethod2016.7897<System.Object>()#G2_C336::ClassMethod2017.7898<System.Object>()#G2_C336::Method4.7890()#G2_C336::Method5.7892()#G2_C336::Method6.7894<System.Object>()#G2_C336::Method7.7895<System.Object>()#"
		call void Generated827.Generated827::M.G2_C336.A.A<class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>>(!!0,string)
		newobj instance void class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C336::ClassMethod1329.MI.7899()#G2_C336::ClassMethod1330.MI.7900<System.Object>()#G1_C7::ClassMethod1331.4820<System.Object>()#G1_C7::Method4.4811()#G1_C7::Method5.4812()#G1_C7::Method6.4813<System.Object>()#G2_C336::Method7.7895<System.Object>()#"
		call void Generated827.Generated827::M.G1_C7.T.T<class Generated827.BaseClass0,class Generated827.BaseClass0,class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C336::ClassMethod1329.MI.7899()#G2_C336::ClassMethod1330.MI.7900<System.Object>()#G1_C7::ClassMethod1331.4820<System.Object>()#G1_C7::Method4.4811()#G1_C7::Method5.4812()#G1_C7::Method6.4813<System.Object>()#G2_C336::Method7.7895<System.Object>()#"
		call void Generated827.Generated827::M.G1_C7.A.T<class Generated827.BaseClass0,class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C336::ClassMethod1329.MI.7899()#G2_C336::ClassMethod1330.MI.7900<System.Object>()#G1_C7::ClassMethod1331.4820<System.Object>()#G1_C7::Method4.4811()#G1_C7::Method5.4812()#G1_C7::Method6.4813<System.Object>()#G2_C336::Method7.7895<System.Object>()#"
		call void Generated827.Generated827::M.G1_C7.A.A<class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C336::Method4.MI.7891()#G2_C336::Method5.MI.7893()#G2_C336::Method6.7894<System.Object>()#"
		call void Generated827.Generated827::M.IBase1.T<class Generated827.BaseClass0,class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C336::Method4.MI.7891()#G2_C336::Method5.MI.7893()#G2_C336::Method6.7894<System.Object>()#"
		call void Generated827.Generated827::M.IBase1.A<class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C336::Method7.7895<System.Object>()#"
		call void Generated827.Generated827::M.IBase2.T.T<class Generated827.BaseClass1,class Generated827.BaseClass0,class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C336::Method7.7895<System.Object>()#"
		call void Generated827.Generated827::M.IBase2.B.T<class Generated827.BaseClass0,class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C336::Method7.7895<System.Object>()#"
		call void Generated827.Generated827::M.IBase2.B.A<class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C336::Method7.7895<System.Object>()#"
		call void Generated827.Generated827::M.IBase2.T.T<class Generated827.BaseClass0,class Generated827.BaseClass0,class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C336::Method7.7895<System.Object>()#"
		call void Generated827.Generated827::M.IBase2.A.T<class Generated827.BaseClass0,class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C336::Method7.7895<System.Object>()#"
		call void Generated827.Generated827::M.IBase2.A.A<class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C336::Method7.7895<System.Object>()#"
		call void Generated827.Generated827::M.IBase2.T.T<class Generated827.BaseClass0,class Generated827.BaseClass1,class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C336::Method7.7895<System.Object>()#"
		call void Generated827.Generated827::M.IBase2.A.T<class Generated827.BaseClass1,class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C336::Method7.7895<System.Object>()#"
		call void Generated827.Generated827::M.IBase2.A.B<class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C336::Method7.7895<System.Object>()#"
		call void Generated827.Generated827::M.IBase2.T.T<class Generated827.BaseClass1,class Generated827.BaseClass1,class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C336::Method7.7895<System.Object>()#"
		call void Generated827.Generated827::M.IBase2.B.T<class Generated827.BaseClass1,class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C336::Method7.7895<System.Object>()#"
		call void Generated827.Generated827::M.IBase2.B.B<class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C336::ClassMethod1329.MI.7899()#G2_C336::ClassMethod1330.MI.7900<System.Object>()#G1_C7::ClassMethod1331.4820<System.Object>()#G2_C336::ClassMethod2015.7896()#G2_C336::ClassMethod2016.7897<System.Object>()#G2_C336::ClassMethod2017.7898<System.Object>()#G2_C336::Method4.7890()#G2_C336::Method5.7892()#G2_C336::Method6.7894<System.Object>()#G2_C336::Method7.7895<System.Object>()#"
		call void Generated827.Generated827::M.G2_C336.T.T<class Generated827.BaseClass0,class Generated827.BaseClass1,class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C336::ClassMethod1329.MI.7899()#G2_C336::ClassMethod1330.MI.7900<System.Object>()#G1_C7::ClassMethod1331.4820<System.Object>()#G2_C336::ClassMethod2015.7896()#G2_C336::ClassMethod2016.7897<System.Object>()#G2_C336::ClassMethod2017.7898<System.Object>()#G2_C336::Method4.7890()#G2_C336::Method5.7892()#G2_C336::Method6.7894<System.Object>()#G2_C336::Method7.7895<System.Object>()#"
		call void Generated827.Generated827::M.G2_C336.A.T<class Generated827.BaseClass1,class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C336::ClassMethod1329.MI.7899()#G2_C336::ClassMethod1330.MI.7900<System.Object>()#G1_C7::ClassMethod1331.4820<System.Object>()#G2_C336::ClassMethod2015.7896()#G2_C336::ClassMethod2016.7897<System.Object>()#G2_C336::ClassMethod2017.7898<System.Object>()#G2_C336::Method4.7890()#G2_C336::Method5.7892()#G2_C336::Method6.7894<System.Object>()#G2_C336::Method7.7895<System.Object>()#"
		call void Generated827.Generated827::M.G2_C336.A.B<class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>>(!!0,string)
		newobj instance void class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C336::ClassMethod1329.MI.7899()#G2_C336::ClassMethod1330.MI.7900<System.Object>()#G1_C7::ClassMethod1331.4820<System.Object>()#G1_C7::Method4.4811()#G1_C7::Method5.4812()#G1_C7::Method6.4813<System.Object>()#G2_C336::Method7.7895<System.Object>()#"
		call void Generated827.Generated827::M.G1_C7.T.T<class Generated827.BaseClass0,class Generated827.BaseClass1,class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C336::ClassMethod1329.MI.7899()#G2_C336::ClassMethod1330.MI.7900<System.Object>()#G1_C7::ClassMethod1331.4820<System.Object>()#G1_C7::Method4.4811()#G1_C7::Method5.4812()#G1_C7::Method6.4813<System.Object>()#G2_C336::Method7.7895<System.Object>()#"
		call void Generated827.Generated827::M.G1_C7.A.T<class Generated827.BaseClass1,class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C336::ClassMethod1329.MI.7899()#G2_C336::ClassMethod1330.MI.7900<System.Object>()#G1_C7::ClassMethod1331.4820<System.Object>()#G1_C7::Method4.4811()#G1_C7::Method5.4812()#G1_C7::Method6.4813<System.Object>()#G2_C336::Method7.7895<System.Object>()#"
		call void Generated827.Generated827::M.G1_C7.A.B<class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C336::Method4.MI.7891()#G2_C336::Method5.MI.7893()#G2_C336::Method6.7894<System.Object>()#"
		call void Generated827.Generated827::M.IBase1.T<class Generated827.BaseClass0,class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C336::Method4.MI.7891()#G2_C336::Method5.MI.7893()#G2_C336::Method6.7894<System.Object>()#"
		call void Generated827.Generated827::M.IBase1.A<class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C7::Method7.MI.4816<System.Object>()#"
		call void Generated827.Generated827::M.IBase2.T.T<class Generated827.BaseClass1,class Generated827.BaseClass0,class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C7::Method7.MI.4816<System.Object>()#"
		call void Generated827.Generated827::M.IBase2.B.T<class Generated827.BaseClass0,class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::Method7.MI.4816<System.Object>()#"
		call void Generated827.Generated827::M.IBase2.B.A<class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C7::Method7.MI.4816<System.Object>()#"
		call void Generated827.Generated827::M.IBase2.T.T<class Generated827.BaseClass0,class Generated827.BaseClass0,class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C7::Method7.MI.4816<System.Object>()#"
		call void Generated827.Generated827::M.IBase2.A.T<class Generated827.BaseClass0,class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::Method7.MI.4816<System.Object>()#"
		call void Generated827.Generated827::M.IBase2.A.A<class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C336::Method7.7895<System.Object>()#"
		call void Generated827.Generated827::M.IBase2.T.T<class Generated827.BaseClass0,class Generated827.BaseClass1,class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C336::Method7.7895<System.Object>()#"
		call void Generated827.Generated827::M.IBase2.A.T<class Generated827.BaseClass1,class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C336::Method7.7895<System.Object>()#"
		call void Generated827.Generated827::M.IBase2.A.B<class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C336::Method7.7895<System.Object>()#"
		call void Generated827.Generated827::M.IBase2.T.T<class Generated827.BaseClass1,class Generated827.BaseClass1,class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C336::Method7.7895<System.Object>()#"
		call void Generated827.Generated827::M.IBase2.B.T<class Generated827.BaseClass1,class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C336::Method7.7895<System.Object>()#"
		call void Generated827.Generated827::M.IBase2.B.B<class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C336::ClassMethod1329.MI.7899()#G2_C336::ClassMethod1330.MI.7900<System.Object>()#G1_C7::ClassMethod1331.4820<System.Object>()#G2_C336::ClassMethod2015.7896()#G2_C336::ClassMethod2016.7897<System.Object>()#G2_C336::ClassMethod2017.7898<System.Object>()#G2_C336::Method4.7890()#G2_C336::Method5.7892()#G2_C336::Method6.7894<System.Object>()#G2_C336::Method7.7895<System.Object>()#"
		call void Generated827.Generated827::M.G2_C336.T.T<class Generated827.BaseClass1,class Generated827.BaseClass0,class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C336::ClassMethod1329.MI.7899()#G2_C336::ClassMethod1330.MI.7900<System.Object>()#G1_C7::ClassMethod1331.4820<System.Object>()#G2_C336::ClassMethod2015.7896()#G2_C336::ClassMethod2016.7897<System.Object>()#G2_C336::ClassMethod2017.7898<System.Object>()#G2_C336::Method4.7890()#G2_C336::Method5.7892()#G2_C336::Method6.7894<System.Object>()#G2_C336::Method7.7895<System.Object>()#"
		call void Generated827.Generated827::M.G2_C336.B.T<class Generated827.BaseClass0,class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C336::ClassMethod1329.MI.7899()#G2_C336::ClassMethod1330.MI.7900<System.Object>()#G1_C7::ClassMethod1331.4820<System.Object>()#G2_C336::ClassMethod2015.7896()#G2_C336::ClassMethod2016.7897<System.Object>()#G2_C336::ClassMethod2017.7898<System.Object>()#G2_C336::Method4.7890()#G2_C336::Method5.7892()#G2_C336::Method6.7894<System.Object>()#G2_C336::Method7.7895<System.Object>()#"
		call void Generated827.Generated827::M.G2_C336.B.A<class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>>(!!0,string)
		newobj instance void class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C336::ClassMethod1329.MI.7899()#G2_C336::ClassMethod1330.MI.7900<System.Object>()#G1_C7::ClassMethod1331.4820<System.Object>()#G1_C7::Method4.4811()#G1_C7::Method5.4812()#G1_C7::Method6.4813<System.Object>()#G2_C336::Method7.7895<System.Object>()#"
		call void Generated827.Generated827::M.G1_C7.T.T<class Generated827.BaseClass0,class Generated827.BaseClass1,class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C336::ClassMethod1329.MI.7899()#G2_C336::ClassMethod1330.MI.7900<System.Object>()#G1_C7::ClassMethod1331.4820<System.Object>()#G1_C7::Method4.4811()#G1_C7::Method5.4812()#G1_C7::Method6.4813<System.Object>()#G2_C336::Method7.7895<System.Object>()#"
		call void Generated827.Generated827::M.G1_C7.A.T<class Generated827.BaseClass1,class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C336::ClassMethod1329.MI.7899()#G2_C336::ClassMethod1330.MI.7900<System.Object>()#G1_C7::ClassMethod1331.4820<System.Object>()#G1_C7::Method4.4811()#G1_C7::Method5.4812()#G1_C7::Method6.4813<System.Object>()#G2_C336::Method7.7895<System.Object>()#"
		call void Generated827.Generated827::M.G1_C7.A.B<class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C336::Method4.MI.7891()#G2_C336::Method5.MI.7893()#G2_C336::Method6.7894<System.Object>()#"
		call void Generated827.Generated827::M.IBase1.T<class Generated827.BaseClass0,class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C336::Method4.MI.7891()#G2_C336::Method5.MI.7893()#G2_C336::Method6.7894<System.Object>()#"
		call void Generated827.Generated827::M.IBase1.A<class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C7::Method7.MI.4816<System.Object>()#"
		call void Generated827.Generated827::M.IBase2.T.T<class Generated827.BaseClass1,class Generated827.BaseClass0,class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C7::Method7.MI.4816<System.Object>()#"
		call void Generated827.Generated827::M.IBase2.B.T<class Generated827.BaseClass0,class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::Method7.MI.4816<System.Object>()#"
		call void Generated827.Generated827::M.IBase2.B.A<class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C7::Method7.MI.4816<System.Object>()#"
		call void Generated827.Generated827::M.IBase2.T.T<class Generated827.BaseClass0,class Generated827.BaseClass0,class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C7::Method7.MI.4816<System.Object>()#"
		call void Generated827.Generated827::M.IBase2.A.T<class Generated827.BaseClass0,class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::Method7.MI.4816<System.Object>()#"
		call void Generated827.Generated827::M.IBase2.A.A<class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C336::Method7.7895<System.Object>()#"
		call void Generated827.Generated827::M.IBase2.T.T<class Generated827.BaseClass0,class Generated827.BaseClass1,class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C336::Method7.7895<System.Object>()#"
		call void Generated827.Generated827::M.IBase2.A.T<class Generated827.BaseClass1,class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C336::Method7.7895<System.Object>()#"
		call void Generated827.Generated827::M.IBase2.A.B<class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C336::Method7.7895<System.Object>()#"
		call void Generated827.Generated827::M.IBase2.T.T<class Generated827.BaseClass1,class Generated827.BaseClass1,class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C336::Method7.7895<System.Object>()#"
		call void Generated827.Generated827::M.IBase2.B.T<class Generated827.BaseClass1,class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C336::Method7.7895<System.Object>()#"
		call void Generated827.Generated827::M.IBase2.B.B<class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C336::ClassMethod1329.MI.7899()#G2_C336::ClassMethod1330.MI.7900<System.Object>()#G1_C7::ClassMethod1331.4820<System.Object>()#G2_C336::ClassMethod2015.7896()#G2_C336::ClassMethod2016.7897<System.Object>()#G2_C336::ClassMethod2017.7898<System.Object>()#G2_C336::Method4.7890()#G2_C336::Method5.7892()#G2_C336::Method6.7894<System.Object>()#G2_C336::Method7.7895<System.Object>()#"
		call void Generated827.Generated827::M.G2_C336.T.T<class Generated827.BaseClass1,class Generated827.BaseClass1,class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C336::ClassMethod1329.MI.7899()#G2_C336::ClassMethod1330.MI.7900<System.Object>()#G1_C7::ClassMethod1331.4820<System.Object>()#G2_C336::ClassMethod2015.7896()#G2_C336::ClassMethod2016.7897<System.Object>()#G2_C336::ClassMethod2017.7898<System.Object>()#G2_C336::Method4.7890()#G2_C336::Method5.7892()#G2_C336::Method6.7894<System.Object>()#G2_C336::Method7.7895<System.Object>()#"
		call void Generated827.Generated827::M.G2_C336.B.T<class Generated827.BaseClass1,class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C336::ClassMethod1329.MI.7899()#G2_C336::ClassMethod1330.MI.7900<System.Object>()#G1_C7::ClassMethod1331.4820<System.Object>()#G2_C336::ClassMethod2015.7896()#G2_C336::ClassMethod2016.7897<System.Object>()#G2_C336::ClassMethod2017.7898<System.Object>()#G2_C336::Method4.7890()#G2_C336::Method5.7892()#G2_C336::Method6.7894<System.Object>()#G2_C336::Method7.7895<System.Object>()#"
		call void Generated827.Generated827::M.G2_C336.B.B<class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>>(!!0,string)
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
		newobj instance void class Generated827.G3_C1299`1<class Generated827.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::ClassMethod1331<object>()
		calli default string(class Generated827.G3_C1299`1<class Generated827.BaseClass0>)
		ldstr "G1_C7::ClassMethod1331.4820<System.Object>()"
		ldstr "class G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0> on type class Generated827.G3_C1299`1<class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::ClassMethod1330<object>()
		calli default string(class Generated827.G3_C1299`1<class Generated827.BaseClass0>)
		ldstr "G2_C336::ClassMethod1330.MI.7900<System.Object>()"
		ldstr "class G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0> on type class Generated827.G3_C1299`1<class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::ClassMethod1329()
		calli default string(class Generated827.G3_C1299`1<class Generated827.BaseClass0>)
		ldstr "G2_C336::ClassMethod1329.MI.7899()"
		ldstr "class G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0> on type class Generated827.G3_C1299`1<class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::ClassMethod1328()
		calli default string(class Generated827.G3_C1299`1<class Generated827.BaseClass0>)
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0> on type class Generated827.G3_C1299`1<class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::Method7<object>()
		calli default string(class Generated827.G3_C1299`1<class Generated827.BaseClass0>)
		ldstr "G2_C336::Method7.7895<System.Object>()"
		ldstr "class G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0> on type class Generated827.G3_C1299`1<class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::Method6<object>()
		calli default string(class Generated827.G3_C1299`1<class Generated827.BaseClass0>)
		ldstr "G1_C7::Method6.4813<System.Object>()"
		ldstr "class G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0> on type class Generated827.G3_C1299`1<class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::Method5()
		calli default string(class Generated827.G3_C1299`1<class Generated827.BaseClass0>)
		ldstr "G1_C7::Method5.4812()"
		ldstr "class G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0> on type class Generated827.G3_C1299`1<class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::Method4()
		calli default string(class Generated827.G3_C1299`1<class Generated827.BaseClass0>)
		ldstr "G1_C7::Method4.4811()"
		ldstr "class G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0> on type class Generated827.G3_C1299`1<class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated827.IBase1`1<class Generated827.BaseClass0>::Method4()
		calli default string(class Generated827.G3_C1299`1<class Generated827.BaseClass0>)
		ldstr "G2_C336::Method4.MI.7891()"
		ldstr "class IBase1`1<class Generated827.BaseClass0> on type class Generated827.G3_C1299`1<class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated827.IBase1`1<class Generated827.BaseClass0>::Method5()
		calli default string(class Generated827.G3_C1299`1<class Generated827.BaseClass0>)
		ldstr "G2_C336::Method5.MI.7893()"
		ldstr "class IBase1`1<class Generated827.BaseClass0> on type class Generated827.G3_C1299`1<class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated827.IBase1`1<class Generated827.BaseClass0>::Method6<object>()
		calli default string(class Generated827.G3_C1299`1<class Generated827.BaseClass0>)
		ldstr "G2_C336::Method6.7894<System.Object>()"
		ldstr "class IBase1`1<class Generated827.BaseClass0> on type class Generated827.G3_C1299`1<class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated827.IBase2`2<class Generated827.BaseClass1,class Generated827.BaseClass0>::Method7<object>()
		calli default string(class Generated827.G3_C1299`1<class Generated827.BaseClass0>)
		ldstr "G2_C336::Method7.7895<System.Object>()"
		ldstr "class IBase2`2<class Generated827.BaseClass1,class Generated827.BaseClass0> on type class Generated827.G3_C1299`1<class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated827.IBase2`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::Method7<object>()
		calli default string(class Generated827.G3_C1299`1<class Generated827.BaseClass0>)
		ldstr "G2_C336::Method7.7895<System.Object>()"
		ldstr "class IBase2`2<class Generated827.BaseClass0,class Generated827.BaseClass0> on type class Generated827.G3_C1299`1<class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated827.IBase2`2<class Generated827.BaseClass0,class Generated827.BaseClass1>::Method7<object>()
		calli default string(class Generated827.G3_C1299`1<class Generated827.BaseClass0>)
		ldstr "G2_C336::Method7.7895<System.Object>()"
		ldstr "class IBase2`2<class Generated827.BaseClass0,class Generated827.BaseClass1> on type class Generated827.G3_C1299`1<class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated827.IBase2`2<class Generated827.BaseClass1,class Generated827.BaseClass1>::Method7<object>()
		calli default string(class Generated827.G3_C1299`1<class Generated827.BaseClass0>)
		ldstr "G2_C336::Method7.7895<System.Object>()"
		ldstr "class IBase2`2<class Generated827.BaseClass1,class Generated827.BaseClass1> on type class Generated827.G3_C1299`1<class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::ClassMethod2017<object>()
		calli default string(class Generated827.G3_C1299`1<class Generated827.BaseClass0>)
		ldstr "G3_C1299::ClassMethod2017.MI.15419<System.Object>()"
		ldstr "class G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0> on type class Generated827.G3_C1299`1<class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::ClassMethod2016<object>()
		calli default string(class Generated827.G3_C1299`1<class Generated827.BaseClass0>)
		ldstr "G3_C1299::ClassMethod2016.MI.15418<System.Object>()"
		ldstr "class G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0> on type class Generated827.G3_C1299`1<class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::ClassMethod2015()
		calli default string(class Generated827.G3_C1299`1<class Generated827.BaseClass0>)
		ldstr "G3_C1299::ClassMethod2015.MI.15417()"
		ldstr "class G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0> on type class Generated827.G3_C1299`1<class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::Method7<object>()
		calli default string(class Generated827.G3_C1299`1<class Generated827.BaseClass0>)
		ldstr "G2_C336::Method7.7895<System.Object>()"
		ldstr "class G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0> on type class Generated827.G3_C1299`1<class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::Method6<object>()
		calli default string(class Generated827.G3_C1299`1<class Generated827.BaseClass0>)
		ldstr "G2_C336::Method6.7894<System.Object>()"
		ldstr "class G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0> on type class Generated827.G3_C1299`1<class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::Method5()
		calli default string(class Generated827.G3_C1299`1<class Generated827.BaseClass0>)
		ldstr "G2_C336::Method5.7892()"
		ldstr "class G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0> on type class Generated827.G3_C1299`1<class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::Method4()
		calli default string(class Generated827.G3_C1299`1<class Generated827.BaseClass0>)
		ldstr "G2_C336::Method4.7890()"
		ldstr "class G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0> on type class Generated827.G3_C1299`1<class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::ClassMethod1331<object>()
		calli default string(class Generated827.G3_C1299`1<class Generated827.BaseClass0>)
		ldstr "G1_C7::ClassMethod1331.4820<System.Object>()"
		ldstr "class G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0> on type class Generated827.G3_C1299`1<class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::ClassMethod1330<object>()
		calli default string(class Generated827.G3_C1299`1<class Generated827.BaseClass0>)
		ldstr "G2_C336::ClassMethod1330.MI.7900<System.Object>()"
		ldstr "class G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0> on type class Generated827.G3_C1299`1<class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::ClassMethod1329()
		calli default string(class Generated827.G3_C1299`1<class Generated827.BaseClass0>)
		ldstr "G2_C336::ClassMethod1329.MI.7899()"
		ldstr "class G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0> on type class Generated827.G3_C1299`1<class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::ClassMethod1328()
		calli default string(class Generated827.G3_C1299`1<class Generated827.BaseClass0>)
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0> on type class Generated827.G3_C1299`1<class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G3_C1299`1<class Generated827.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated827.G3_C1299`1<class Generated827.BaseClass0>::ClassMethod3978<object>()
		calli default string(class Generated827.G3_C1299`1<class Generated827.BaseClass0>)
		ldstr "G3_C1299::ClassMethod3978.15416<System.Object>()"
		ldstr "class G3_C1299`1<class Generated827.BaseClass0> on type class Generated827.G3_C1299`1<class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G3_C1299`1<class Generated827.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated827.G3_C1299`1<class Generated827.BaseClass0>::ClassMethod3977<object>()
		calli default string(class Generated827.G3_C1299`1<class Generated827.BaseClass0>)
		ldstr "G3_C1299::ClassMethod3977.15415<System.Object>()"
		ldstr "class G3_C1299`1<class Generated827.BaseClass0> on type class Generated827.G3_C1299`1<class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G3_C1299`1<class Generated827.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated827.G3_C1299`1<class Generated827.BaseClass0>::ClassMethod3976()
		calli default string(class Generated827.G3_C1299`1<class Generated827.BaseClass0>)
		ldstr "G3_C1299::ClassMethod3976.15414()"
		ldstr "class G3_C1299`1<class Generated827.BaseClass0> on type class Generated827.G3_C1299`1<class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G3_C1299`1<class Generated827.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated827.G3_C1299`1<class Generated827.BaseClass0>::ClassMethod3975()
		calli default string(class Generated827.G3_C1299`1<class Generated827.BaseClass0>)
		ldstr "G3_C1299::ClassMethod3975.15413()"
		ldstr "class G3_C1299`1<class Generated827.BaseClass0> on type class Generated827.G3_C1299`1<class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G3_C1299`1<class Generated827.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated827.G3_C1299`1<class Generated827.BaseClass0>::ClassMethod2017<object>()
		calli default string(class Generated827.G3_C1299`1<class Generated827.BaseClass0>)
		ldstr "G3_C1299::ClassMethod2017.MI.15419<System.Object>()"
		ldstr "class G3_C1299`1<class Generated827.BaseClass0> on type class Generated827.G3_C1299`1<class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G3_C1299`1<class Generated827.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated827.G3_C1299`1<class Generated827.BaseClass0>::ClassMethod2016<object>()
		calli default string(class Generated827.G3_C1299`1<class Generated827.BaseClass0>)
		ldstr "G3_C1299::ClassMethod2016.MI.15418<System.Object>()"
		ldstr "class G3_C1299`1<class Generated827.BaseClass0> on type class Generated827.G3_C1299`1<class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G3_C1299`1<class Generated827.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated827.G3_C1299`1<class Generated827.BaseClass0>::ClassMethod2015()
		calli default string(class Generated827.G3_C1299`1<class Generated827.BaseClass0>)
		ldstr "G3_C1299::ClassMethod2015.MI.15417()"
		ldstr "class G3_C1299`1<class Generated827.BaseClass0> on type class Generated827.G3_C1299`1<class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G3_C1299`1<class Generated827.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated827.G3_C1299`1<class Generated827.BaseClass0>::Method7<object>()
		calli default string(class Generated827.G3_C1299`1<class Generated827.BaseClass0>)
		ldstr "G2_C336::Method7.7895<System.Object>()"
		ldstr "class G3_C1299`1<class Generated827.BaseClass0> on type class Generated827.G3_C1299`1<class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G3_C1299`1<class Generated827.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated827.G3_C1299`1<class Generated827.BaseClass0>::Method6<object>()
		calli default string(class Generated827.G3_C1299`1<class Generated827.BaseClass0>)
		ldstr "G2_C336::Method6.7894<System.Object>()"
		ldstr "class G3_C1299`1<class Generated827.BaseClass0> on type class Generated827.G3_C1299`1<class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G3_C1299`1<class Generated827.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated827.G3_C1299`1<class Generated827.BaseClass0>::Method5()
		calli default string(class Generated827.G3_C1299`1<class Generated827.BaseClass0>)
		ldstr "G2_C336::Method5.7892()"
		ldstr "class G3_C1299`1<class Generated827.BaseClass0> on type class Generated827.G3_C1299`1<class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G3_C1299`1<class Generated827.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated827.G3_C1299`1<class Generated827.BaseClass0>::Method4()
		calli default string(class Generated827.G3_C1299`1<class Generated827.BaseClass0>)
		ldstr "G2_C336::Method4.7890()"
		ldstr "class G3_C1299`1<class Generated827.BaseClass0> on type class Generated827.G3_C1299`1<class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G3_C1299`1<class Generated827.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated827.G3_C1299`1<class Generated827.BaseClass0>::ClassMethod1331<object>()
		calli default string(class Generated827.G3_C1299`1<class Generated827.BaseClass0>)
		ldstr "G1_C7::ClassMethod1331.4820<System.Object>()"
		ldstr "class G3_C1299`1<class Generated827.BaseClass0> on type class Generated827.G3_C1299`1<class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G3_C1299`1<class Generated827.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated827.G3_C1299`1<class Generated827.BaseClass0>::ClassMethod1330<object>()
		calli default string(class Generated827.G3_C1299`1<class Generated827.BaseClass0>)
		ldstr "G2_C336::ClassMethod1330.MI.7900<System.Object>()"
		ldstr "class G3_C1299`1<class Generated827.BaseClass0> on type class Generated827.G3_C1299`1<class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G3_C1299`1<class Generated827.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated827.G3_C1299`1<class Generated827.BaseClass0>::ClassMethod1329()
		calli default string(class Generated827.G3_C1299`1<class Generated827.BaseClass0>)
		ldstr "G2_C336::ClassMethod1329.MI.7899()"
		ldstr "class G3_C1299`1<class Generated827.BaseClass0> on type class Generated827.G3_C1299`1<class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G3_C1299`1<class Generated827.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated827.G3_C1299`1<class Generated827.BaseClass0>::ClassMethod1328()
		calli default string(class Generated827.G3_C1299`1<class Generated827.BaseClass0>)
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G3_C1299`1<class Generated827.BaseClass0> on type class Generated827.G3_C1299`1<class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated827.G3_C1299`1<class Generated827.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::ClassMethod1331<object>()
		calli default string(class Generated827.G3_C1299`1<class Generated827.BaseClass1>)
		ldstr "G1_C7::ClassMethod1331.4820<System.Object>()"
		ldstr "class G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0> on type class Generated827.G3_C1299`1<class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::ClassMethod1330<object>()
		calli default string(class Generated827.G3_C1299`1<class Generated827.BaseClass1>)
		ldstr "G2_C336::ClassMethod1330.MI.7900<System.Object>()"
		ldstr "class G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0> on type class Generated827.G3_C1299`1<class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::ClassMethod1329()
		calli default string(class Generated827.G3_C1299`1<class Generated827.BaseClass1>)
		ldstr "G2_C336::ClassMethod1329.MI.7899()"
		ldstr "class G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0> on type class Generated827.G3_C1299`1<class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::ClassMethod1328()
		calli default string(class Generated827.G3_C1299`1<class Generated827.BaseClass1>)
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0> on type class Generated827.G3_C1299`1<class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::Method7<object>()
		calli default string(class Generated827.G3_C1299`1<class Generated827.BaseClass1>)
		ldstr "G2_C336::Method7.7895<System.Object>()"
		ldstr "class G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0> on type class Generated827.G3_C1299`1<class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::Method6<object>()
		calli default string(class Generated827.G3_C1299`1<class Generated827.BaseClass1>)
		ldstr "G1_C7::Method6.4813<System.Object>()"
		ldstr "class G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0> on type class Generated827.G3_C1299`1<class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::Method5()
		calli default string(class Generated827.G3_C1299`1<class Generated827.BaseClass1>)
		ldstr "G1_C7::Method5.4812()"
		ldstr "class G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0> on type class Generated827.G3_C1299`1<class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::Method4()
		calli default string(class Generated827.G3_C1299`1<class Generated827.BaseClass1>)
		ldstr "G1_C7::Method4.4811()"
		ldstr "class G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0> on type class Generated827.G3_C1299`1<class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated827.IBase1`1<class Generated827.BaseClass0>::Method4()
		calli default string(class Generated827.G3_C1299`1<class Generated827.BaseClass1>)
		ldstr "G2_C336::Method4.MI.7891()"
		ldstr "class IBase1`1<class Generated827.BaseClass0> on type class Generated827.G3_C1299`1<class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated827.IBase1`1<class Generated827.BaseClass0>::Method5()
		calli default string(class Generated827.G3_C1299`1<class Generated827.BaseClass1>)
		ldstr "G2_C336::Method5.MI.7893()"
		ldstr "class IBase1`1<class Generated827.BaseClass0> on type class Generated827.G3_C1299`1<class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated827.IBase1`1<class Generated827.BaseClass0>::Method6<object>()
		calli default string(class Generated827.G3_C1299`1<class Generated827.BaseClass1>)
		ldstr "G2_C336::Method6.7894<System.Object>()"
		ldstr "class IBase1`1<class Generated827.BaseClass0> on type class Generated827.G3_C1299`1<class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated827.IBase2`2<class Generated827.BaseClass1,class Generated827.BaseClass0>::Method7<object>()
		calli default string(class Generated827.G3_C1299`1<class Generated827.BaseClass1>)
		ldstr "G2_C336::Method7.7895<System.Object>()"
		ldstr "class IBase2`2<class Generated827.BaseClass1,class Generated827.BaseClass0> on type class Generated827.G3_C1299`1<class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated827.IBase2`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::Method7<object>()
		calli default string(class Generated827.G3_C1299`1<class Generated827.BaseClass1>)
		ldstr "G2_C336::Method7.7895<System.Object>()"
		ldstr "class IBase2`2<class Generated827.BaseClass0,class Generated827.BaseClass0> on type class Generated827.G3_C1299`1<class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated827.IBase2`2<class Generated827.BaseClass0,class Generated827.BaseClass1>::Method7<object>()
		calli default string(class Generated827.G3_C1299`1<class Generated827.BaseClass1>)
		ldstr "G2_C336::Method7.7895<System.Object>()"
		ldstr "class IBase2`2<class Generated827.BaseClass0,class Generated827.BaseClass1> on type class Generated827.G3_C1299`1<class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated827.IBase2`2<class Generated827.BaseClass1,class Generated827.BaseClass1>::Method7<object>()
		calli default string(class Generated827.G3_C1299`1<class Generated827.BaseClass1>)
		ldstr "G2_C336::Method7.7895<System.Object>()"
		ldstr "class IBase2`2<class Generated827.BaseClass1,class Generated827.BaseClass1> on type class Generated827.G3_C1299`1<class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>::ClassMethod2017<object>()
		calli default string(class Generated827.G3_C1299`1<class Generated827.BaseClass1>)
		ldstr "G3_C1299::ClassMethod2017.MI.15419<System.Object>()"
		ldstr "class G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1> on type class Generated827.G3_C1299`1<class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>::ClassMethod2016<object>()
		calli default string(class Generated827.G3_C1299`1<class Generated827.BaseClass1>)
		ldstr "G3_C1299::ClassMethod2016.MI.15418<System.Object>()"
		ldstr "class G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1> on type class Generated827.G3_C1299`1<class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>::ClassMethod2015()
		calli default string(class Generated827.G3_C1299`1<class Generated827.BaseClass1>)
		ldstr "G3_C1299::ClassMethod2015.MI.15417()"
		ldstr "class G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1> on type class Generated827.G3_C1299`1<class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>::Method7<object>()
		calli default string(class Generated827.G3_C1299`1<class Generated827.BaseClass1>)
		ldstr "G2_C336::Method7.7895<System.Object>()"
		ldstr "class G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1> on type class Generated827.G3_C1299`1<class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>::Method6<object>()
		calli default string(class Generated827.G3_C1299`1<class Generated827.BaseClass1>)
		ldstr "G2_C336::Method6.7894<System.Object>()"
		ldstr "class G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1> on type class Generated827.G3_C1299`1<class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>::Method5()
		calli default string(class Generated827.G3_C1299`1<class Generated827.BaseClass1>)
		ldstr "G2_C336::Method5.7892()"
		ldstr "class G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1> on type class Generated827.G3_C1299`1<class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>::Method4()
		calli default string(class Generated827.G3_C1299`1<class Generated827.BaseClass1>)
		ldstr "G2_C336::Method4.7890()"
		ldstr "class G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1> on type class Generated827.G3_C1299`1<class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>::ClassMethod1331<object>()
		calli default string(class Generated827.G3_C1299`1<class Generated827.BaseClass1>)
		ldstr "G1_C7::ClassMethod1331.4820<System.Object>()"
		ldstr "class G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1> on type class Generated827.G3_C1299`1<class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>::ClassMethod1330<object>()
		calli default string(class Generated827.G3_C1299`1<class Generated827.BaseClass1>)
		ldstr "G2_C336::ClassMethod1330.MI.7900<System.Object>()"
		ldstr "class G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1> on type class Generated827.G3_C1299`1<class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>::ClassMethod1329()
		calli default string(class Generated827.G3_C1299`1<class Generated827.BaseClass1>)
		ldstr "G2_C336::ClassMethod1329.MI.7899()"
		ldstr "class G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1> on type class Generated827.G3_C1299`1<class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>::ClassMethod1328()
		calli default string(class Generated827.G3_C1299`1<class Generated827.BaseClass1>)
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1> on type class Generated827.G3_C1299`1<class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G3_C1299`1<class Generated827.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated827.G3_C1299`1<class Generated827.BaseClass1>::ClassMethod3978<object>()
		calli default string(class Generated827.G3_C1299`1<class Generated827.BaseClass1>)
		ldstr "G3_C1299::ClassMethod3978.15416<System.Object>()"
		ldstr "class G3_C1299`1<class Generated827.BaseClass1> on type class Generated827.G3_C1299`1<class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G3_C1299`1<class Generated827.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated827.G3_C1299`1<class Generated827.BaseClass1>::ClassMethod3977<object>()
		calli default string(class Generated827.G3_C1299`1<class Generated827.BaseClass1>)
		ldstr "G3_C1299::ClassMethod3977.15415<System.Object>()"
		ldstr "class G3_C1299`1<class Generated827.BaseClass1> on type class Generated827.G3_C1299`1<class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G3_C1299`1<class Generated827.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated827.G3_C1299`1<class Generated827.BaseClass1>::ClassMethod3976()
		calli default string(class Generated827.G3_C1299`1<class Generated827.BaseClass1>)
		ldstr "G3_C1299::ClassMethod3976.15414()"
		ldstr "class G3_C1299`1<class Generated827.BaseClass1> on type class Generated827.G3_C1299`1<class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G3_C1299`1<class Generated827.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated827.G3_C1299`1<class Generated827.BaseClass1>::ClassMethod3975()
		calli default string(class Generated827.G3_C1299`1<class Generated827.BaseClass1>)
		ldstr "G3_C1299::ClassMethod3975.15413()"
		ldstr "class G3_C1299`1<class Generated827.BaseClass1> on type class Generated827.G3_C1299`1<class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G3_C1299`1<class Generated827.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated827.G3_C1299`1<class Generated827.BaseClass1>::ClassMethod2017<object>()
		calli default string(class Generated827.G3_C1299`1<class Generated827.BaseClass1>)
		ldstr "G3_C1299::ClassMethod2017.MI.15419<System.Object>()"
		ldstr "class G3_C1299`1<class Generated827.BaseClass1> on type class Generated827.G3_C1299`1<class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G3_C1299`1<class Generated827.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated827.G3_C1299`1<class Generated827.BaseClass1>::ClassMethod2016<object>()
		calli default string(class Generated827.G3_C1299`1<class Generated827.BaseClass1>)
		ldstr "G3_C1299::ClassMethod2016.MI.15418<System.Object>()"
		ldstr "class G3_C1299`1<class Generated827.BaseClass1> on type class Generated827.G3_C1299`1<class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G3_C1299`1<class Generated827.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated827.G3_C1299`1<class Generated827.BaseClass1>::ClassMethod2015()
		calli default string(class Generated827.G3_C1299`1<class Generated827.BaseClass1>)
		ldstr "G3_C1299::ClassMethod2015.MI.15417()"
		ldstr "class G3_C1299`1<class Generated827.BaseClass1> on type class Generated827.G3_C1299`1<class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G3_C1299`1<class Generated827.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated827.G3_C1299`1<class Generated827.BaseClass1>::Method7<object>()
		calli default string(class Generated827.G3_C1299`1<class Generated827.BaseClass1>)
		ldstr "G2_C336::Method7.7895<System.Object>()"
		ldstr "class G3_C1299`1<class Generated827.BaseClass1> on type class Generated827.G3_C1299`1<class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G3_C1299`1<class Generated827.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated827.G3_C1299`1<class Generated827.BaseClass1>::Method6<object>()
		calli default string(class Generated827.G3_C1299`1<class Generated827.BaseClass1>)
		ldstr "G2_C336::Method6.7894<System.Object>()"
		ldstr "class G3_C1299`1<class Generated827.BaseClass1> on type class Generated827.G3_C1299`1<class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G3_C1299`1<class Generated827.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated827.G3_C1299`1<class Generated827.BaseClass1>::Method5()
		calli default string(class Generated827.G3_C1299`1<class Generated827.BaseClass1>)
		ldstr "G2_C336::Method5.7892()"
		ldstr "class G3_C1299`1<class Generated827.BaseClass1> on type class Generated827.G3_C1299`1<class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G3_C1299`1<class Generated827.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated827.G3_C1299`1<class Generated827.BaseClass1>::Method4()
		calli default string(class Generated827.G3_C1299`1<class Generated827.BaseClass1>)
		ldstr "G2_C336::Method4.7890()"
		ldstr "class G3_C1299`1<class Generated827.BaseClass1> on type class Generated827.G3_C1299`1<class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G3_C1299`1<class Generated827.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated827.G3_C1299`1<class Generated827.BaseClass1>::ClassMethod1331<object>()
		calli default string(class Generated827.G3_C1299`1<class Generated827.BaseClass1>)
		ldstr "G1_C7::ClassMethod1331.4820<System.Object>()"
		ldstr "class G3_C1299`1<class Generated827.BaseClass1> on type class Generated827.G3_C1299`1<class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G3_C1299`1<class Generated827.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated827.G3_C1299`1<class Generated827.BaseClass1>::ClassMethod1330<object>()
		calli default string(class Generated827.G3_C1299`1<class Generated827.BaseClass1>)
		ldstr "G2_C336::ClassMethod1330.MI.7900<System.Object>()"
		ldstr "class G3_C1299`1<class Generated827.BaseClass1> on type class Generated827.G3_C1299`1<class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G3_C1299`1<class Generated827.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated827.G3_C1299`1<class Generated827.BaseClass1>::ClassMethod1329()
		calli default string(class Generated827.G3_C1299`1<class Generated827.BaseClass1>)
		ldstr "G2_C336::ClassMethod1329.MI.7899()"
		ldstr "class G3_C1299`1<class Generated827.BaseClass1> on type class Generated827.G3_C1299`1<class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G3_C1299`1<class Generated827.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated827.G3_C1299`1<class Generated827.BaseClass1>::ClassMethod1328()
		calli default string(class Generated827.G3_C1299`1<class Generated827.BaseClass1>)
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G3_C1299`1<class Generated827.BaseClass1> on type class Generated827.G3_C1299`1<class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::ClassMethod1331<object>()
		calli default string(class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>)
		ldstr "G1_C7::ClassMethod1331.4820<System.Object>()"
		ldstr "class G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0> on type class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::ClassMethod1330<object>()
		calli default string(class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>)
		ldstr "G2_C336::ClassMethod1330.MI.7900<System.Object>()"
		ldstr "class G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0> on type class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::ClassMethod1329()
		calli default string(class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>)
		ldstr "G2_C336::ClassMethod1329.MI.7899()"
		ldstr "class G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0> on type class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::ClassMethod1328()
		calli default string(class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>)
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0> on type class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::Method7<object>()
		calli default string(class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>)
		ldstr "G2_C336::Method7.7895<System.Object>()"
		ldstr "class G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0> on type class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::Method6<object>()
		calli default string(class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>)
		ldstr "G1_C7::Method6.4813<System.Object>()"
		ldstr "class G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0> on type class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::Method5()
		calli default string(class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>)
		ldstr "G1_C7::Method5.4812()"
		ldstr "class G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0> on type class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::Method4()
		calli default string(class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>)
		ldstr "G1_C7::Method4.4811()"
		ldstr "class G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0> on type class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated827.IBase1`1<class Generated827.BaseClass0>::Method4()
		calli default string(class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>)
		ldstr "G2_C336::Method4.MI.7891()"
		ldstr "class IBase1`1<class Generated827.BaseClass0> on type class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated827.IBase1`1<class Generated827.BaseClass0>::Method5()
		calli default string(class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>)
		ldstr "G2_C336::Method5.MI.7893()"
		ldstr "class IBase1`1<class Generated827.BaseClass0> on type class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated827.IBase1`1<class Generated827.BaseClass0>::Method6<object>()
		calli default string(class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>)
		ldstr "G2_C336::Method6.7894<System.Object>()"
		ldstr "class IBase1`1<class Generated827.BaseClass0> on type class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated827.IBase2`2<class Generated827.BaseClass1,class Generated827.BaseClass0>::Method7<object>()
		calli default string(class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>)
		ldstr "G2_C336::Method7.7895<System.Object>()"
		ldstr "class IBase2`2<class Generated827.BaseClass1,class Generated827.BaseClass0> on type class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated827.IBase2`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::Method7<object>()
		calli default string(class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>)
		ldstr "G2_C336::Method7.7895<System.Object>()"
		ldstr "class IBase2`2<class Generated827.BaseClass0,class Generated827.BaseClass0> on type class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated827.IBase2`2<class Generated827.BaseClass0,class Generated827.BaseClass1>::Method7<object>()
		calli default string(class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>)
		ldstr "G2_C336::Method7.7895<System.Object>()"
		ldstr "class IBase2`2<class Generated827.BaseClass0,class Generated827.BaseClass1> on type class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated827.IBase2`2<class Generated827.BaseClass1,class Generated827.BaseClass1>::Method7<object>()
		calli default string(class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>)
		ldstr "G2_C336::Method7.7895<System.Object>()"
		ldstr "class IBase2`2<class Generated827.BaseClass1,class Generated827.BaseClass1> on type class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::ClassMethod2017<object>()
		calli default string(class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>)
		ldstr "G2_C336::ClassMethod2017.7898<System.Object>()"
		ldstr "class G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0> on type class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::ClassMethod2016<object>()
		calli default string(class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>)
		ldstr "G2_C336::ClassMethod2016.7897<System.Object>()"
		ldstr "class G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0> on type class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::ClassMethod2015()
		calli default string(class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>)
		ldstr "G2_C336::ClassMethod2015.7896()"
		ldstr "class G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0> on type class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::Method7<object>()
		calli default string(class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>)
		ldstr "G2_C336::Method7.7895<System.Object>()"
		ldstr "class G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0> on type class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::Method6<object>()
		calli default string(class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>)
		ldstr "G2_C336::Method6.7894<System.Object>()"
		ldstr "class G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0> on type class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::Method5()
		calli default string(class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>)
		ldstr "G2_C336::Method5.7892()"
		ldstr "class G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0> on type class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::Method4()
		calli default string(class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>)
		ldstr "G2_C336::Method4.7890()"
		ldstr "class G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0> on type class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::ClassMethod1331<object>()
		calli default string(class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>)
		ldstr "G1_C7::ClassMethod1331.4820<System.Object>()"
		ldstr "class G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0> on type class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::ClassMethod1330<object>()
		calli default string(class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>)
		ldstr "G2_C336::ClassMethod1330.MI.7900<System.Object>()"
		ldstr "class G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0> on type class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::ClassMethod1329()
		calli default string(class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>)
		ldstr "G2_C336::ClassMethod1329.MI.7899()"
		ldstr "class G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0> on type class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::ClassMethod1328()
		calli default string(class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>)
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0> on type class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::ClassMethod1331<object>()
		calli default string(class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>)
		ldstr "G1_C7::ClassMethod1331.4820<System.Object>()"
		ldstr "class G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0> on type class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::ClassMethod1330<object>()
		calli default string(class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>)
		ldstr "G2_C336::ClassMethod1330.MI.7900<System.Object>()"
		ldstr "class G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0> on type class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::ClassMethod1329()
		calli default string(class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>)
		ldstr "G2_C336::ClassMethod1329.MI.7899()"
		ldstr "class G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0> on type class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::ClassMethod1328()
		calli default string(class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>)
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0> on type class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::Method7<object>()
		calli default string(class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>)
		ldstr "G2_C336::Method7.7895<System.Object>()"
		ldstr "class G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0> on type class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::Method6<object>()
		calli default string(class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>)
		ldstr "G1_C7::Method6.4813<System.Object>()"
		ldstr "class G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0> on type class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::Method5()
		calli default string(class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>)
		ldstr "G1_C7::Method5.4812()"
		ldstr "class G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0> on type class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::Method4()
		calli default string(class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>)
		ldstr "G1_C7::Method4.4811()"
		ldstr "class G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass0> on type class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated827.IBase1`1<class Generated827.BaseClass0>::Method4()
		calli default string(class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>)
		ldstr "G2_C336::Method4.MI.7891()"
		ldstr "class IBase1`1<class Generated827.BaseClass0> on type class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated827.IBase1`1<class Generated827.BaseClass0>::Method5()
		calli default string(class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>)
		ldstr "G2_C336::Method5.MI.7893()"
		ldstr "class IBase1`1<class Generated827.BaseClass0> on type class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated827.IBase1`1<class Generated827.BaseClass0>::Method6<object>()
		calli default string(class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>)
		ldstr "G2_C336::Method6.7894<System.Object>()"
		ldstr "class IBase1`1<class Generated827.BaseClass0> on type class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated827.IBase2`2<class Generated827.BaseClass1,class Generated827.BaseClass0>::Method7<object>()
		calli default string(class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>)
		ldstr "G2_C336::Method7.7895<System.Object>()"
		ldstr "class IBase2`2<class Generated827.BaseClass1,class Generated827.BaseClass0> on type class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated827.IBase2`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::Method7<object>()
		calli default string(class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>)
		ldstr "G2_C336::Method7.7895<System.Object>()"
		ldstr "class IBase2`2<class Generated827.BaseClass0,class Generated827.BaseClass0> on type class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated827.IBase2`2<class Generated827.BaseClass0,class Generated827.BaseClass1>::Method7<object>()
		calli default string(class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>)
		ldstr "G2_C336::Method7.7895<System.Object>()"
		ldstr "class IBase2`2<class Generated827.BaseClass0,class Generated827.BaseClass1> on type class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated827.IBase2`2<class Generated827.BaseClass1,class Generated827.BaseClass1>::Method7<object>()
		calli default string(class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>)
		ldstr "G2_C336::Method7.7895<System.Object>()"
		ldstr "class IBase2`2<class Generated827.BaseClass1,class Generated827.BaseClass1> on type class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>::ClassMethod2017<object>()
		calli default string(class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>)
		ldstr "G2_C336::ClassMethod2017.7898<System.Object>()"
		ldstr "class G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1> on type class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>::ClassMethod2016<object>()
		calli default string(class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>)
		ldstr "G2_C336::ClassMethod2016.7897<System.Object>()"
		ldstr "class G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1> on type class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>::ClassMethod2015()
		calli default string(class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>)
		ldstr "G2_C336::ClassMethod2015.7896()"
		ldstr "class G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1> on type class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>::Method7<object>()
		calli default string(class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>)
		ldstr "G2_C336::Method7.7895<System.Object>()"
		ldstr "class G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1> on type class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>::Method6<object>()
		calli default string(class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>)
		ldstr "G2_C336::Method6.7894<System.Object>()"
		ldstr "class G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1> on type class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>::Method5()
		calli default string(class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>)
		ldstr "G2_C336::Method5.7892()"
		ldstr "class G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1> on type class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>::Method4()
		calli default string(class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>)
		ldstr "G2_C336::Method4.7890()"
		ldstr "class G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1> on type class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>::ClassMethod1331<object>()
		calli default string(class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>)
		ldstr "G1_C7::ClassMethod1331.4820<System.Object>()"
		ldstr "class G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1> on type class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>::ClassMethod1330<object>()
		calli default string(class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>)
		ldstr "G2_C336::ClassMethod1330.MI.7900<System.Object>()"
		ldstr "class G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1> on type class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>::ClassMethod1329()
		calli default string(class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>)
		ldstr "G2_C336::ClassMethod1329.MI.7899()"
		ldstr "class G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1> on type class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>::ClassMethod1328()
		calli default string(class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>)
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1> on type class Generated827.G2_C336`2<class Generated827.BaseClass0,class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass1>::ClassMethod1331<object>()
		calli default string(class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>)
		ldstr "G1_C7::ClassMethod1331.4820<System.Object>()"
		ldstr "class G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass1> on type class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass1>::ClassMethod1330<object>()
		calli default string(class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>)
		ldstr "G2_C336::ClassMethod1330.MI.7900<System.Object>()"
		ldstr "class G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass1> on type class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass1>::ClassMethod1329()
		calli default string(class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>)
		ldstr "G2_C336::ClassMethod1329.MI.7899()"
		ldstr "class G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass1> on type class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass1>::ClassMethod1328()
		calli default string(class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>)
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass1> on type class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass1>::Method7<object>()
		calli default string(class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>)
		ldstr "G2_C336::Method7.7895<System.Object>()"
		ldstr "class G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass1> on type class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass1>::Method6<object>()
		calli default string(class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>)
		ldstr "G1_C7::Method6.4813<System.Object>()"
		ldstr "class G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass1> on type class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass1>::Method5()
		calli default string(class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>)
		ldstr "G1_C7::Method5.4812()"
		ldstr "class G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass1> on type class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass1>::Method4()
		calli default string(class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>)
		ldstr "G1_C7::Method4.4811()"
		ldstr "class G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass1> on type class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated827.IBase1`1<class Generated827.BaseClass0>::Method4()
		calli default string(class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>)
		ldstr "G2_C336::Method4.MI.7891()"
		ldstr "class IBase1`1<class Generated827.BaseClass0> on type class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated827.IBase1`1<class Generated827.BaseClass0>::Method5()
		calli default string(class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>)
		ldstr "G2_C336::Method5.MI.7893()"
		ldstr "class IBase1`1<class Generated827.BaseClass0> on type class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated827.IBase1`1<class Generated827.BaseClass0>::Method6<object>()
		calli default string(class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>)
		ldstr "G2_C336::Method6.7894<System.Object>()"
		ldstr "class IBase1`1<class Generated827.BaseClass0> on type class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated827.IBase2`2<class Generated827.BaseClass1,class Generated827.BaseClass0>::Method7<object>()
		calli default string(class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>)
		ldstr "G1_C7::Method7.MI.4816<System.Object>()"
		ldstr "class IBase2`2<class Generated827.BaseClass1,class Generated827.BaseClass0> on type class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated827.IBase2`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::Method7<object>()
		calli default string(class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>)
		ldstr "G1_C7::Method7.MI.4816<System.Object>()"
		ldstr "class IBase2`2<class Generated827.BaseClass0,class Generated827.BaseClass0> on type class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated827.IBase2`2<class Generated827.BaseClass0,class Generated827.BaseClass1>::Method7<object>()
		calli default string(class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>)
		ldstr "G2_C336::Method7.7895<System.Object>()"
		ldstr "class IBase2`2<class Generated827.BaseClass0,class Generated827.BaseClass1> on type class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated827.IBase2`2<class Generated827.BaseClass1,class Generated827.BaseClass1>::Method7<object>()
		calli default string(class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>)
		ldstr "G2_C336::Method7.7895<System.Object>()"
		ldstr "class IBase2`2<class Generated827.BaseClass1,class Generated827.BaseClass1> on type class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>::ClassMethod2017<object>()
		calli default string(class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>)
		ldstr "G2_C336::ClassMethod2017.7898<System.Object>()"
		ldstr "class G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0> on type class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>::ClassMethod2016<object>()
		calli default string(class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>)
		ldstr "G2_C336::ClassMethod2016.7897<System.Object>()"
		ldstr "class G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0> on type class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>::ClassMethod2015()
		calli default string(class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>)
		ldstr "G2_C336::ClassMethod2015.7896()"
		ldstr "class G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0> on type class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>::Method7<object>()
		calli default string(class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>)
		ldstr "G2_C336::Method7.7895<System.Object>()"
		ldstr "class G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0> on type class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>::Method6<object>()
		calli default string(class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>)
		ldstr "G2_C336::Method6.7894<System.Object>()"
		ldstr "class G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0> on type class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>::Method5()
		calli default string(class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>)
		ldstr "G2_C336::Method5.7892()"
		ldstr "class G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0> on type class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>::Method4()
		calli default string(class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>)
		ldstr "G2_C336::Method4.7890()"
		ldstr "class G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0> on type class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>::ClassMethod1331<object>()
		calli default string(class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>)
		ldstr "G1_C7::ClassMethod1331.4820<System.Object>()"
		ldstr "class G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0> on type class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>::ClassMethod1330<object>()
		calli default string(class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>)
		ldstr "G2_C336::ClassMethod1330.MI.7900<System.Object>()"
		ldstr "class G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0> on type class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>::ClassMethod1329()
		calli default string(class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>)
		ldstr "G2_C336::ClassMethod1329.MI.7899()"
		ldstr "class G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0> on type class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>::ClassMethod1328()
		calli default string(class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>)
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0> on type class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass1>::ClassMethod1331<object>()
		calli default string(class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>)
		ldstr "G1_C7::ClassMethod1331.4820<System.Object>()"
		ldstr "class G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass1> on type class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass1>::ClassMethod1330<object>()
		calli default string(class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>)
		ldstr "G2_C336::ClassMethod1330.MI.7900<System.Object>()"
		ldstr "class G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass1> on type class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass1>::ClassMethod1329()
		calli default string(class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>)
		ldstr "G2_C336::ClassMethod1329.MI.7899()"
		ldstr "class G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass1> on type class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass1>::ClassMethod1328()
		calli default string(class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>)
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass1> on type class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass1>::Method7<object>()
		calli default string(class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>)
		ldstr "G2_C336::Method7.7895<System.Object>()"
		ldstr "class G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass1> on type class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass1>::Method6<object>()
		calli default string(class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>)
		ldstr "G1_C7::Method6.4813<System.Object>()"
		ldstr "class G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass1> on type class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass1>::Method5()
		calli default string(class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>)
		ldstr "G1_C7::Method5.4812()"
		ldstr "class G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass1> on type class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated827.G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass1>::Method4()
		calli default string(class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>)
		ldstr "G1_C7::Method4.4811()"
		ldstr "class G1_C7`2<class Generated827.BaseClass0,class Generated827.BaseClass1> on type class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated827.IBase1`1<class Generated827.BaseClass0>::Method4()
		calli default string(class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>)
		ldstr "G2_C336::Method4.MI.7891()"
		ldstr "class IBase1`1<class Generated827.BaseClass0> on type class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated827.IBase1`1<class Generated827.BaseClass0>::Method5()
		calli default string(class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>)
		ldstr "G2_C336::Method5.MI.7893()"
		ldstr "class IBase1`1<class Generated827.BaseClass0> on type class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated827.IBase1`1<class Generated827.BaseClass0>::Method6<object>()
		calli default string(class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>)
		ldstr "G2_C336::Method6.7894<System.Object>()"
		ldstr "class IBase1`1<class Generated827.BaseClass0> on type class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated827.IBase2`2<class Generated827.BaseClass1,class Generated827.BaseClass0>::Method7<object>()
		calli default string(class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>)
		ldstr "G1_C7::Method7.MI.4816<System.Object>()"
		ldstr "class IBase2`2<class Generated827.BaseClass1,class Generated827.BaseClass0> on type class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated827.IBase2`2<class Generated827.BaseClass0,class Generated827.BaseClass0>::Method7<object>()
		calli default string(class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>)
		ldstr "G1_C7::Method7.MI.4816<System.Object>()"
		ldstr "class IBase2`2<class Generated827.BaseClass0,class Generated827.BaseClass0> on type class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated827.IBase2`2<class Generated827.BaseClass0,class Generated827.BaseClass1>::Method7<object>()
		calli default string(class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>)
		ldstr "G2_C336::Method7.7895<System.Object>()"
		ldstr "class IBase2`2<class Generated827.BaseClass0,class Generated827.BaseClass1> on type class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated827.IBase2`2<class Generated827.BaseClass1,class Generated827.BaseClass1>::Method7<object>()
		calli default string(class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>)
		ldstr "G2_C336::Method7.7895<System.Object>()"
		ldstr "class IBase2`2<class Generated827.BaseClass1,class Generated827.BaseClass1> on type class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>::ClassMethod2017<object>()
		calli default string(class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>)
		ldstr "G2_C336::ClassMethod2017.7898<System.Object>()"
		ldstr "class G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1> on type class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>::ClassMethod2016<object>()
		calli default string(class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>)
		ldstr "G2_C336::ClassMethod2016.7897<System.Object>()"
		ldstr "class G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1> on type class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>::ClassMethod2015()
		calli default string(class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>)
		ldstr "G2_C336::ClassMethod2015.7896()"
		ldstr "class G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1> on type class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>::Method7<object>()
		calli default string(class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>)
		ldstr "G2_C336::Method7.7895<System.Object>()"
		ldstr "class G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1> on type class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>::Method6<object>()
		calli default string(class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>)
		ldstr "G2_C336::Method6.7894<System.Object>()"
		ldstr "class G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1> on type class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>::Method5()
		calli default string(class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>)
		ldstr "G2_C336::Method5.7892()"
		ldstr "class G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1> on type class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>::Method4()
		calli default string(class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>)
		ldstr "G2_C336::Method4.7890()"
		ldstr "class G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1> on type class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>::ClassMethod1331<object>()
		calli default string(class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>)
		ldstr "G1_C7::ClassMethod1331.4820<System.Object>()"
		ldstr "class G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1> on type class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>::ClassMethod1330<object>()
		calli default string(class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>)
		ldstr "G2_C336::ClassMethod1330.MI.7900<System.Object>()"
		ldstr "class G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1> on type class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>::ClassMethod1329()
		calli default string(class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>)
		ldstr "G2_C336::ClassMethod1329.MI.7899()"
		ldstr "class G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1> on type class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>::ClassMethod1328()
		calli default string(class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>)
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1> on type class Generated827.G2_C336`2<class Generated827.BaseClass1,class Generated827.BaseClass1>"
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
		call void Generated827.Generated827::MethodCallingTest()
		call void Generated827.Generated827::ConstrainedCallsTest()
		call void Generated827.Generated827::StructConstrainedInterfaceCallsTest()
		call void Generated827.Generated827::CalliTest()
		ldc.i4 100
		ret
	}
}
