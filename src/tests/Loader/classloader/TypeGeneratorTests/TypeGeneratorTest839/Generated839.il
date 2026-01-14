// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) .ver 4:0:0:0 }
.assembly extern TestFramework { .publickeytoken = ( B0 3F 5F 7F 11 D5 0A 3A )  }

//TYPES IN FORWARDER ASSEMBLIES:

//TEST ASSEMBLY:
.assembly extern xunit.core {}

.class public Generated839.BaseClass0 
{
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void [mscorlib]System.Object::.ctor()
		ret
	}
}
.class public Generated839.BaseClass1 
		extends Generated839.BaseClass0
{
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void Generated839.BaseClass0::.ctor()
		ret
	}
}
.class public Generated839.G3_C1311`1<T0> 
		extends class Generated839.G2_C350`2<class Generated839.BaseClass1,!T0>
		implements class Generated839.IBase2`2<class Generated839.BaseClass0,class Generated839.BaseClass1> 
{
	.method public hidebysig virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G3_C1311::Method7.15471<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase2<class Generated839.BaseClass0,class Generated839.BaseClass1>.Method7'<M0>() cil managed noinlining { 
		.override method instance string class Generated839.IBase2`2<class Generated839.BaseClass0,class Generated839.BaseClass1>::Method7<[1]>()
		ldstr "G3_C1311::Method7.MI.15472<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod4000() cil managed noinlining { 
		ldstr "G3_C1311::ClassMethod4000.15473()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod4001() cil managed noinlining { 
		ldstr "G3_C1311::ClassMethod4001.15474()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod4002<M0>() cil managed noinlining { 
		ldstr "G3_C1311::ClassMethod4002.15475<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod4003<M0>() cil managed noinlining { 
		ldstr "G3_C1311::ClassMethod4003.15476<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class Generated839.G2_C350`2<class Generated839.BaseClass1,!T0>::.ctor()
		ret
	}
}
.class public Generated839.G2_C350`2<T0, T1> 
		extends class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>
		implements class Generated839.IBase2`2<!T1,class Generated839.BaseClass1>, class Generated839.IBase1`1<class Generated839.BaseClass1> 
{
	.method public hidebysig newslot virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G2_C350::Method7.8034<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase2<T1,class Generated839.BaseClass1>.Method7'<M0>() cil managed noinlining { 
		.override method instance string class Generated839.IBase2`2<!T1,class Generated839.BaseClass1>::Method7<[1]>()
		ldstr "G2_C350::Method7.MI.8035<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string Method4() cil managed noinlining { 
		ldstr "G2_C350::Method4.8036()"
		ret
	}
	.method public hidebysig virtual instance string Method5() cil managed noinlining { 
		ldstr "G2_C350::Method5.8037()"
		ret
	}
	.method public hidebysig newslot virtual instance string Method6<M0>() cil managed noinlining { 
		ldstr "G2_C350::Method6.8038<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<class Generated839.BaseClass1>.Method6'<M0>() cil managed noinlining { 
		.override method instance string class Generated839.IBase1`1<class Generated839.BaseClass1>::Method6<[1]>()
		ldstr "G2_C350::Method6.MI.8039<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'G1_C7<class Generated839.BaseClass1,class Generated839.BaseClass0>.ClassMethod1329'() cil managed noinlining { 
		.override method instance string class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::ClassMethod1329()
		ldstr "G2_C350::ClassMethod1329.MI.8040()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'G1_C7<class Generated839.BaseClass1,class Generated839.BaseClass0>.ClassMethod1330'<M0>() cil managed noinlining { 
		.override method instance string class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::ClassMethod1330<[1]>()
		ldstr "G2_C350::ClassMethod1330.MI.8041<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'G1_C7<class Generated839.BaseClass1,class Generated839.BaseClass0>.ClassMethod1331'<M0>() cil managed noinlining { 
		.override method instance string class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::ClassMethod1331<[1]>()
		ldstr "G2_C350::ClassMethod1331.MI.8042<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::.ctor()
		ret
	}
}
.class interface public abstract Generated839.IBase2`2<+T0, -T1> 
{
	.method public hidebysig newslot abstract virtual instance string Method7<M0>() cil managed { }
}
.class public abstract Generated839.G1_C7`2<T0, T1> 
		implements class Generated839.IBase1`1<!T0>, class Generated839.IBase2`2<class Generated839.BaseClass1,!T0> 
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
		.override method instance string class Generated839.IBase1`1<!T0>::Method6<[1]>()
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
	.method public hidebysig newslot virtual instance string 'IBase2<class Generated839.BaseClass1,T0>.Method7'<M0>() cil managed noinlining { 
		.override method instance string class Generated839.IBase2`2<class Generated839.BaseClass1,!T0>::Method7<[1]>()
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
.class interface public abstract Generated839.IBase1`1<+T0> 
{
	.method public hidebysig newslot abstract virtual instance string Method4() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method5() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method6<M0>() cil managed { }
}
.class public auto ansi beforefieldinit Generated839.Generated839 {
	.method static void M.BaseClass0<(Generated839.BaseClass0)W>(!!W inst, string exp) cil managed {
		.maxstack 5
		.locals init (string[] actualResults)
		ldc.i4.s    0
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.BaseClass0<(Generated839.BaseClass0)W>(!!W inst, string exp)"
		ldc.i4.s 0
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.BaseClass1<(Generated839.BaseClass1)W>(!!W inst, string exp) cil managed {
		.maxstack 5
		.locals init (string[] actualResults)
		ldc.i4.s    0
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.BaseClass1<(Generated839.BaseClass1)W>(!!W inst, string exp)"
		ldc.i4.s 0
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1311.T<T0,(class Generated839.G3_C1311`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 17
		.locals init (string[] actualResults)
		ldc.i4.s    12
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1311.T<T0,(class Generated839.G3_C1311`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 12
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G3_C1311`1<!!T0>::ClassMethod1328()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G3_C1311`1<!!T0>::ClassMethod1329()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G3_C1311`1<!!T0>::ClassMethod1330<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G3_C1311`1<!!T0>::ClassMethod1331<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G3_C1311`1<!!T0>::ClassMethod4000()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G3_C1311`1<!!T0>::ClassMethod4001()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G3_C1311`1<!!T0>::ClassMethod4002<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G3_C1311`1<!!T0>::ClassMethod4003<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G3_C1311`1<!!T0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G3_C1311`1<!!T0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G3_C1311`1<!!T0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G3_C1311`1<!!T0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1311.A<(class Generated839.G3_C1311`1<class Generated839.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 17
		.locals init (string[] actualResults)
		ldc.i4.s    12
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1311.A<(class Generated839.G3_C1311`1<class Generated839.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 12
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G3_C1311`1<class Generated839.BaseClass0>::ClassMethod1328()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G3_C1311`1<class Generated839.BaseClass0>::ClassMethod1329()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G3_C1311`1<class Generated839.BaseClass0>::ClassMethod1330<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G3_C1311`1<class Generated839.BaseClass0>::ClassMethod1331<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G3_C1311`1<class Generated839.BaseClass0>::ClassMethod4000()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G3_C1311`1<class Generated839.BaseClass0>::ClassMethod4001()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G3_C1311`1<class Generated839.BaseClass0>::ClassMethod4002<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G3_C1311`1<class Generated839.BaseClass0>::ClassMethod4003<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G3_C1311`1<class Generated839.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G3_C1311`1<class Generated839.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G3_C1311`1<class Generated839.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G3_C1311`1<class Generated839.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1311.B<(class Generated839.G3_C1311`1<class Generated839.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 17
		.locals init (string[] actualResults)
		ldc.i4.s    12
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1311.B<(class Generated839.G3_C1311`1<class Generated839.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 12
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G3_C1311`1<class Generated839.BaseClass1>::ClassMethod1328()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G3_C1311`1<class Generated839.BaseClass1>::ClassMethod1329()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G3_C1311`1<class Generated839.BaseClass1>::ClassMethod1330<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G3_C1311`1<class Generated839.BaseClass1>::ClassMethod1331<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G3_C1311`1<class Generated839.BaseClass1>::ClassMethod4000()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G3_C1311`1<class Generated839.BaseClass1>::ClassMethod4001()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G3_C1311`1<class Generated839.BaseClass1>::ClassMethod4002<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G3_C1311`1<class Generated839.BaseClass1>::ClassMethod4003<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G3_C1311`1<class Generated839.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G3_C1311`1<class Generated839.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G3_C1311`1<class Generated839.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G3_C1311`1<class Generated839.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C350.T.T<T0,T1,(class Generated839.G2_C350`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C350.T.T<T0,T1,(class Generated839.G2_C350`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G2_C350`2<!!T0,!!T1>::ClassMethod1328()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G2_C350`2<!!T0,!!T1>::ClassMethod1329()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G2_C350`2<!!T0,!!T1>::ClassMethod1330<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G2_C350`2<!!T0,!!T1>::ClassMethod1331<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G2_C350`2<!!T0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G2_C350`2<!!T0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G2_C350`2<!!T0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G2_C350`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C350.A.T<T1,(class Generated839.G2_C350`2<class Generated839.BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C350.A.T<T1,(class Generated839.G2_C350`2<class Generated839.BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G2_C350`2<class Generated839.BaseClass0,!!T1>::ClassMethod1328()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G2_C350`2<class Generated839.BaseClass0,!!T1>::ClassMethod1329()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G2_C350`2<class Generated839.BaseClass0,!!T1>::ClassMethod1330<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G2_C350`2<class Generated839.BaseClass0,!!T1>::ClassMethod1331<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G2_C350`2<class Generated839.BaseClass0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G2_C350`2<class Generated839.BaseClass0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G2_C350`2<class Generated839.BaseClass0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G2_C350`2<class Generated839.BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C350.A.A<(class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C350.A.A<(class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>::ClassMethod1328()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>::ClassMethod1329()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>::ClassMethod1330<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>::ClassMethod1331<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C350.A.B<(class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C350.A.B<(class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>::ClassMethod1328()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>::ClassMethod1329()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>::ClassMethod1330<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>::ClassMethod1331<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C350.B.T<T1,(class Generated839.G2_C350`2<class Generated839.BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C350.B.T<T1,(class Generated839.G2_C350`2<class Generated839.BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G2_C350`2<class Generated839.BaseClass1,!!T1>::ClassMethod1328()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G2_C350`2<class Generated839.BaseClass1,!!T1>::ClassMethod1329()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G2_C350`2<class Generated839.BaseClass1,!!T1>::ClassMethod1330<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G2_C350`2<class Generated839.BaseClass1,!!T1>::ClassMethod1331<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G2_C350`2<class Generated839.BaseClass1,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G2_C350`2<class Generated839.BaseClass1,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G2_C350`2<class Generated839.BaseClass1,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G2_C350`2<class Generated839.BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C350.B.A<(class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C350.B.A<(class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::ClassMethod1328()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::ClassMethod1329()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::ClassMethod1330<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::ClassMethod1331<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C350.B.B<(class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C350.B.B<(class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>::ClassMethod1328()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>::ClassMethod1329()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>::ClassMethod1330<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>::ClassMethod1331<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.T.T<T0,T1,(class Generated839.IBase2`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.T.T<T0,T1,(class Generated839.IBase2`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.IBase2`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.A.T<T1,(class Generated839.IBase2`2<class Generated839.BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.A.T<T1,(class Generated839.IBase2`2<class Generated839.BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.IBase2`2<class Generated839.BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.A.A<(class Generated839.IBase2`2<class Generated839.BaseClass0,class Generated839.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.A.A<(class Generated839.IBase2`2<class Generated839.BaseClass0,class Generated839.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.IBase2`2<class Generated839.BaseClass0,class Generated839.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.A.B<(class Generated839.IBase2`2<class Generated839.BaseClass0,class Generated839.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.A.B<(class Generated839.IBase2`2<class Generated839.BaseClass0,class Generated839.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.IBase2`2<class Generated839.BaseClass0,class Generated839.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.B.T<T1,(class Generated839.IBase2`2<class Generated839.BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.B.T<T1,(class Generated839.IBase2`2<class Generated839.BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.IBase2`2<class Generated839.BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.B.A<(class Generated839.IBase2`2<class Generated839.BaseClass1,class Generated839.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.B.A<(class Generated839.IBase2`2<class Generated839.BaseClass1,class Generated839.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.IBase2`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.B.B<(class Generated839.IBase2`2<class Generated839.BaseClass1,class Generated839.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.B.B<(class Generated839.IBase2`2<class Generated839.BaseClass1,class Generated839.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.IBase2`2<class Generated839.BaseClass1,class Generated839.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C7.T.T<T0,T1,(class Generated839.G1_C7`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C7.T.T<T0,T1,(class Generated839.G1_C7`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G1_C7`2<!!T0,!!T1>::ClassMethod1328()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G1_C7`2<!!T0,!!T1>::ClassMethod1329()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G1_C7`2<!!T0,!!T1>::ClassMethod1330<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G1_C7`2<!!T0,!!T1>::ClassMethod1331<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G1_C7`2<!!T0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G1_C7`2<!!T0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G1_C7`2<!!T0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G1_C7`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C7.A.T<T1,(class Generated839.G1_C7`2<class Generated839.BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C7.A.T<T1,(class Generated839.G1_C7`2<class Generated839.BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G1_C7`2<class Generated839.BaseClass0,!!T1>::ClassMethod1328()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G1_C7`2<class Generated839.BaseClass0,!!T1>::ClassMethod1329()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G1_C7`2<class Generated839.BaseClass0,!!T1>::ClassMethod1330<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G1_C7`2<class Generated839.BaseClass0,!!T1>::ClassMethod1331<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G1_C7`2<class Generated839.BaseClass0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G1_C7`2<class Generated839.BaseClass0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G1_C7`2<class Generated839.BaseClass0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G1_C7`2<class Generated839.BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C7.A.A<(class Generated839.G1_C7`2<class Generated839.BaseClass0,class Generated839.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C7.A.A<(class Generated839.G1_C7`2<class Generated839.BaseClass0,class Generated839.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G1_C7`2<class Generated839.BaseClass0,class Generated839.BaseClass0>::ClassMethod1328()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G1_C7`2<class Generated839.BaseClass0,class Generated839.BaseClass0>::ClassMethod1329()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G1_C7`2<class Generated839.BaseClass0,class Generated839.BaseClass0>::ClassMethod1330<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G1_C7`2<class Generated839.BaseClass0,class Generated839.BaseClass0>::ClassMethod1331<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G1_C7`2<class Generated839.BaseClass0,class Generated839.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G1_C7`2<class Generated839.BaseClass0,class Generated839.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G1_C7`2<class Generated839.BaseClass0,class Generated839.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G1_C7`2<class Generated839.BaseClass0,class Generated839.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C7.A.B<(class Generated839.G1_C7`2<class Generated839.BaseClass0,class Generated839.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C7.A.B<(class Generated839.G1_C7`2<class Generated839.BaseClass0,class Generated839.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G1_C7`2<class Generated839.BaseClass0,class Generated839.BaseClass1>::ClassMethod1328()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G1_C7`2<class Generated839.BaseClass0,class Generated839.BaseClass1>::ClassMethod1329()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G1_C7`2<class Generated839.BaseClass0,class Generated839.BaseClass1>::ClassMethod1330<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G1_C7`2<class Generated839.BaseClass0,class Generated839.BaseClass1>::ClassMethod1331<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G1_C7`2<class Generated839.BaseClass0,class Generated839.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G1_C7`2<class Generated839.BaseClass0,class Generated839.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G1_C7`2<class Generated839.BaseClass0,class Generated839.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G1_C7`2<class Generated839.BaseClass0,class Generated839.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C7.B.T<T1,(class Generated839.G1_C7`2<class Generated839.BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C7.B.T<T1,(class Generated839.G1_C7`2<class Generated839.BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G1_C7`2<class Generated839.BaseClass1,!!T1>::ClassMethod1328()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G1_C7`2<class Generated839.BaseClass1,!!T1>::ClassMethod1329()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G1_C7`2<class Generated839.BaseClass1,!!T1>::ClassMethod1330<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G1_C7`2<class Generated839.BaseClass1,!!T1>::ClassMethod1331<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G1_C7`2<class Generated839.BaseClass1,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G1_C7`2<class Generated839.BaseClass1,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G1_C7`2<class Generated839.BaseClass1,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G1_C7`2<class Generated839.BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C7.B.A<(class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C7.B.A<(class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::ClassMethod1328()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::ClassMethod1329()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::ClassMethod1330<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::ClassMethod1331<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C7.B.B<(class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C7.B.B<(class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass1>::ClassMethod1328()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass1>::ClassMethod1329()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass1>::ClassMethod1330<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass1>::ClassMethod1331<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase1.T<T0,(class Generated839.IBase1`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase1.T<T0,(class Generated839.IBase1`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.IBase1`1<!!T0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.IBase1`1<!!T0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.IBase1`1<!!T0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase1.A<(class Generated839.IBase1`1<class Generated839.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase1.A<(class Generated839.IBase1`1<class Generated839.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.IBase1`1<class Generated839.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.IBase1`1<class Generated839.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.IBase1`1<class Generated839.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase1.B<(class Generated839.IBase1`1<class Generated839.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase1.B<(class Generated839.IBase1`1<class Generated839.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.IBase1`1<class Generated839.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.IBase1`1<class Generated839.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated839.IBase1`1<class Generated839.BaseClass1>::Method6<object>()
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
		newobj instance void class Generated839.G3_C1311`1<class Generated839.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>
		callvirt instance string class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::ClassMethod1331<object>()
		ldstr "G2_C350::ClassMethod1331.MI.8042<System.Object>()"
		ldstr "class G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0> on type class Generated839.G3_C1311`1<class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>
		callvirt instance string class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::ClassMethod1330<object>()
		ldstr "G2_C350::ClassMethod1330.MI.8041<System.Object>()"
		ldstr "class G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0> on type class Generated839.G3_C1311`1<class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>
		callvirt instance string class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::ClassMethod1329()
		ldstr "G2_C350::ClassMethod1329.MI.8040()"
		ldstr "class G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0> on type class Generated839.G3_C1311`1<class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>
		callvirt instance string class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::ClassMethod1328()
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0> on type class Generated839.G3_C1311`1<class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>
		callvirt instance string class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::Method7<object>()
		ldstr "G1_C7::Method7.4815<System.Object>()"
		ldstr "class G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0> on type class Generated839.G3_C1311`1<class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>
		callvirt instance string class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::Method6<object>()
		ldstr "G1_C7::Method6.4813<System.Object>()"
		ldstr "class G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0> on type class Generated839.G3_C1311`1<class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>
		callvirt instance string class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::Method5()
		ldstr "G2_C350::Method5.8037()"
		ldstr "class G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0> on type class Generated839.G3_C1311`1<class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>
		callvirt instance string class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::Method4()
		ldstr "G1_C7::Method4.4811()"
		ldstr "class G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0> on type class Generated839.G3_C1311`1<class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated839.IBase1`1<class Generated839.BaseClass1>::Method4()
		ldstr "G2_C350::Method4.8036()"
		ldstr "class IBase1`1<class Generated839.BaseClass1> on type class Generated839.G3_C1311`1<class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated839.IBase1`1<class Generated839.BaseClass1>::Method5()
		ldstr "G2_C350::Method5.8037()"
		ldstr "class IBase1`1<class Generated839.BaseClass1> on type class Generated839.G3_C1311`1<class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated839.IBase1`1<class Generated839.BaseClass1>::Method6<object>()
		ldstr "G2_C350::Method6.MI.8039<System.Object>()"
		ldstr "class IBase1`1<class Generated839.BaseClass1> on type class Generated839.G3_C1311`1<class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated839.IBase2`2<class Generated839.BaseClass1,class Generated839.BaseClass1>::Method7<object>()
		ldstr "G1_C7::Method7.MI.4816<System.Object>()"
		ldstr "class IBase2`2<class Generated839.BaseClass1,class Generated839.BaseClass1> on type class Generated839.G3_C1311`1<class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated839.IBase1`1<class Generated839.BaseClass0>::Method4()
		ldstr "G2_C350::Method4.8036()"
		ldstr "class IBase1`1<class Generated839.BaseClass0> on type class Generated839.G3_C1311`1<class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated839.IBase1`1<class Generated839.BaseClass0>::Method5()
		ldstr "G2_C350::Method5.8037()"
		ldstr "class IBase1`1<class Generated839.BaseClass0> on type class Generated839.G3_C1311`1<class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated839.IBase1`1<class Generated839.BaseClass0>::Method6<object>()
		ldstr "G2_C350::Method6.MI.8039<System.Object>()"
		ldstr "class IBase1`1<class Generated839.BaseClass0> on type class Generated839.G3_C1311`1<class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated839.IBase2`2<class Generated839.BaseClass0,class Generated839.BaseClass1>::Method7<object>()
		ldstr "G3_C1311::Method7.MI.15472<System.Object>()"
		ldstr "class IBase2`2<class Generated839.BaseClass0,class Generated839.BaseClass1> on type class Generated839.G3_C1311`1<class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>
		callvirt instance string class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::Method6<object>()
		ldstr "G2_C350::Method6.8038<System.Object>()"
		ldstr "class G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0> on type class Generated839.G3_C1311`1<class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>
		callvirt instance string class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::Method5()
		ldstr "G2_C350::Method5.8037()"
		ldstr "class G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0> on type class Generated839.G3_C1311`1<class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>
		callvirt instance string class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::Method4()
		ldstr "G2_C350::Method4.8036()"
		ldstr "class G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0> on type class Generated839.G3_C1311`1<class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>
		callvirt instance string class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::Method7<object>()
		ldstr "G3_C1311::Method7.15471<System.Object>()"
		ldstr "class G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0> on type class Generated839.G3_C1311`1<class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>
		callvirt instance string class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::ClassMethod1331<object>()
		ldstr "G2_C350::ClassMethod1331.MI.8042<System.Object>()"
		ldstr "class G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0> on type class Generated839.G3_C1311`1<class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>
		callvirt instance string class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::ClassMethod1330<object>()
		ldstr "G2_C350::ClassMethod1330.MI.8041<System.Object>()"
		ldstr "class G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0> on type class Generated839.G3_C1311`1<class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>
		callvirt instance string class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::ClassMethod1329()
		ldstr "G2_C350::ClassMethod1329.MI.8040()"
		ldstr "class G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0> on type class Generated839.G3_C1311`1<class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>
		callvirt instance string class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::ClassMethod1328()
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0> on type class Generated839.G3_C1311`1<class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated839.G3_C1311`1<class Generated839.BaseClass0>
		callvirt instance string class Generated839.G3_C1311`1<class Generated839.BaseClass0>::ClassMethod4003<object>()
		ldstr "G3_C1311::ClassMethod4003.15476<System.Object>()"
		ldstr "class G3_C1311`1<class Generated839.BaseClass0> on type class Generated839.G3_C1311`1<class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated839.G3_C1311`1<class Generated839.BaseClass0>
		callvirt instance string class Generated839.G3_C1311`1<class Generated839.BaseClass0>::ClassMethod4002<object>()
		ldstr "G3_C1311::ClassMethod4002.15475<System.Object>()"
		ldstr "class G3_C1311`1<class Generated839.BaseClass0> on type class Generated839.G3_C1311`1<class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated839.G3_C1311`1<class Generated839.BaseClass0>
		callvirt instance string class Generated839.G3_C1311`1<class Generated839.BaseClass0>::ClassMethod4001()
		ldstr "G3_C1311::ClassMethod4001.15474()"
		ldstr "class G3_C1311`1<class Generated839.BaseClass0> on type class Generated839.G3_C1311`1<class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated839.G3_C1311`1<class Generated839.BaseClass0>
		callvirt instance string class Generated839.G3_C1311`1<class Generated839.BaseClass0>::ClassMethod4000()
		ldstr "G3_C1311::ClassMethod4000.15473()"
		ldstr "class G3_C1311`1<class Generated839.BaseClass0> on type class Generated839.G3_C1311`1<class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated839.G3_C1311`1<class Generated839.BaseClass0>
		callvirt instance string class Generated839.G3_C1311`1<class Generated839.BaseClass0>::Method7<object>()
		ldstr "G3_C1311::Method7.15471<System.Object>()"
		ldstr "class G3_C1311`1<class Generated839.BaseClass0> on type class Generated839.G3_C1311`1<class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated839.G3_C1311`1<class Generated839.BaseClass0>
		callvirt instance string class Generated839.G3_C1311`1<class Generated839.BaseClass0>::Method6<object>()
		ldstr "G2_C350::Method6.8038<System.Object>()"
		ldstr "class G3_C1311`1<class Generated839.BaseClass0> on type class Generated839.G3_C1311`1<class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated839.G3_C1311`1<class Generated839.BaseClass0>
		callvirt instance string class Generated839.G3_C1311`1<class Generated839.BaseClass0>::Method5()
		ldstr "G2_C350::Method5.8037()"
		ldstr "class G3_C1311`1<class Generated839.BaseClass0> on type class Generated839.G3_C1311`1<class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated839.G3_C1311`1<class Generated839.BaseClass0>
		callvirt instance string class Generated839.G3_C1311`1<class Generated839.BaseClass0>::Method4()
		ldstr "G2_C350::Method4.8036()"
		ldstr "class G3_C1311`1<class Generated839.BaseClass0> on type class Generated839.G3_C1311`1<class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated839.G3_C1311`1<class Generated839.BaseClass0>
		callvirt instance string class Generated839.G3_C1311`1<class Generated839.BaseClass0>::ClassMethod1331<object>()
		ldstr "G2_C350::ClassMethod1331.MI.8042<System.Object>()"
		ldstr "class G3_C1311`1<class Generated839.BaseClass0> on type class Generated839.G3_C1311`1<class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated839.G3_C1311`1<class Generated839.BaseClass0>
		callvirt instance string class Generated839.G3_C1311`1<class Generated839.BaseClass0>::ClassMethod1330<object>()
		ldstr "G2_C350::ClassMethod1330.MI.8041<System.Object>()"
		ldstr "class G3_C1311`1<class Generated839.BaseClass0> on type class Generated839.G3_C1311`1<class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated839.G3_C1311`1<class Generated839.BaseClass0>
		callvirt instance string class Generated839.G3_C1311`1<class Generated839.BaseClass0>::ClassMethod1329()
		ldstr "G2_C350::ClassMethod1329.MI.8040()"
		ldstr "class G3_C1311`1<class Generated839.BaseClass0> on type class Generated839.G3_C1311`1<class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated839.G3_C1311`1<class Generated839.BaseClass0>
		callvirt instance string class Generated839.G3_C1311`1<class Generated839.BaseClass0>::ClassMethod1328()
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G3_C1311`1<class Generated839.BaseClass0> on type class Generated839.G3_C1311`1<class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated839.G3_C1311`1<class Generated839.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>
		callvirt instance string class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::ClassMethod1331<object>()
		ldstr "G2_C350::ClassMethod1331.MI.8042<System.Object>()"
		ldstr "class G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0> on type class Generated839.G3_C1311`1<class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>
		callvirt instance string class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::ClassMethod1330<object>()
		ldstr "G2_C350::ClassMethod1330.MI.8041<System.Object>()"
		ldstr "class G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0> on type class Generated839.G3_C1311`1<class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>
		callvirt instance string class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::ClassMethod1329()
		ldstr "G2_C350::ClassMethod1329.MI.8040()"
		ldstr "class G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0> on type class Generated839.G3_C1311`1<class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>
		callvirt instance string class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::ClassMethod1328()
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0> on type class Generated839.G3_C1311`1<class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>
		callvirt instance string class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::Method7<object>()
		ldstr "G1_C7::Method7.4815<System.Object>()"
		ldstr "class G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0> on type class Generated839.G3_C1311`1<class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>
		callvirt instance string class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::Method6<object>()
		ldstr "G1_C7::Method6.4813<System.Object>()"
		ldstr "class G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0> on type class Generated839.G3_C1311`1<class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>
		callvirt instance string class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::Method5()
		ldstr "G2_C350::Method5.8037()"
		ldstr "class G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0> on type class Generated839.G3_C1311`1<class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>
		callvirt instance string class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::Method4()
		ldstr "G1_C7::Method4.4811()"
		ldstr "class G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0> on type class Generated839.G3_C1311`1<class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated839.IBase1`1<class Generated839.BaseClass1>::Method4()
		ldstr "G2_C350::Method4.8036()"
		ldstr "class IBase1`1<class Generated839.BaseClass1> on type class Generated839.G3_C1311`1<class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated839.IBase1`1<class Generated839.BaseClass1>::Method5()
		ldstr "G2_C350::Method5.8037()"
		ldstr "class IBase1`1<class Generated839.BaseClass1> on type class Generated839.G3_C1311`1<class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated839.IBase1`1<class Generated839.BaseClass1>::Method6<object>()
		ldstr "G2_C350::Method6.MI.8039<System.Object>()"
		ldstr "class IBase1`1<class Generated839.BaseClass1> on type class Generated839.G3_C1311`1<class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated839.IBase2`2<class Generated839.BaseClass1,class Generated839.BaseClass1>::Method7<object>()
		ldstr "G2_C350::Method7.MI.8035<System.Object>()"
		ldstr "class IBase2`2<class Generated839.BaseClass1,class Generated839.BaseClass1> on type class Generated839.G3_C1311`1<class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated839.IBase1`1<class Generated839.BaseClass0>::Method4()
		ldstr "G2_C350::Method4.8036()"
		ldstr "class IBase1`1<class Generated839.BaseClass0> on type class Generated839.G3_C1311`1<class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated839.IBase1`1<class Generated839.BaseClass0>::Method5()
		ldstr "G2_C350::Method5.8037()"
		ldstr "class IBase1`1<class Generated839.BaseClass0> on type class Generated839.G3_C1311`1<class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated839.IBase1`1<class Generated839.BaseClass0>::Method6<object>()
		ldstr "G2_C350::Method6.MI.8039<System.Object>()"
		ldstr "class IBase1`1<class Generated839.BaseClass0> on type class Generated839.G3_C1311`1<class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated839.IBase2`2<class Generated839.BaseClass0,class Generated839.BaseClass1>::Method7<object>()
		ldstr "G3_C1311::Method7.MI.15472<System.Object>()"
		ldstr "class IBase2`2<class Generated839.BaseClass0,class Generated839.BaseClass1> on type class Generated839.G3_C1311`1<class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>
		callvirt instance string class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>::Method6<object>()
		ldstr "G2_C350::Method6.8038<System.Object>()"
		ldstr "class G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1> on type class Generated839.G3_C1311`1<class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>
		callvirt instance string class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>::Method5()
		ldstr "G2_C350::Method5.8037()"
		ldstr "class G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1> on type class Generated839.G3_C1311`1<class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>
		callvirt instance string class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>::Method4()
		ldstr "G2_C350::Method4.8036()"
		ldstr "class G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1> on type class Generated839.G3_C1311`1<class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>
		callvirt instance string class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>::Method7<object>()
		ldstr "G3_C1311::Method7.15471<System.Object>()"
		ldstr "class G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1> on type class Generated839.G3_C1311`1<class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>
		callvirt instance string class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>::ClassMethod1331<object>()
		ldstr "G2_C350::ClassMethod1331.MI.8042<System.Object>()"
		ldstr "class G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1> on type class Generated839.G3_C1311`1<class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>
		callvirt instance string class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>::ClassMethod1330<object>()
		ldstr "G2_C350::ClassMethod1330.MI.8041<System.Object>()"
		ldstr "class G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1> on type class Generated839.G3_C1311`1<class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>
		callvirt instance string class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>::ClassMethod1329()
		ldstr "G2_C350::ClassMethod1329.MI.8040()"
		ldstr "class G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1> on type class Generated839.G3_C1311`1<class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>
		callvirt instance string class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>::ClassMethod1328()
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1> on type class Generated839.G3_C1311`1<class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated839.G3_C1311`1<class Generated839.BaseClass1>
		callvirt instance string class Generated839.G3_C1311`1<class Generated839.BaseClass1>::ClassMethod4003<object>()
		ldstr "G3_C1311::ClassMethod4003.15476<System.Object>()"
		ldstr "class G3_C1311`1<class Generated839.BaseClass1> on type class Generated839.G3_C1311`1<class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated839.G3_C1311`1<class Generated839.BaseClass1>
		callvirt instance string class Generated839.G3_C1311`1<class Generated839.BaseClass1>::ClassMethod4002<object>()
		ldstr "G3_C1311::ClassMethod4002.15475<System.Object>()"
		ldstr "class G3_C1311`1<class Generated839.BaseClass1> on type class Generated839.G3_C1311`1<class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated839.G3_C1311`1<class Generated839.BaseClass1>
		callvirt instance string class Generated839.G3_C1311`1<class Generated839.BaseClass1>::ClassMethod4001()
		ldstr "G3_C1311::ClassMethod4001.15474()"
		ldstr "class G3_C1311`1<class Generated839.BaseClass1> on type class Generated839.G3_C1311`1<class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated839.G3_C1311`1<class Generated839.BaseClass1>
		callvirt instance string class Generated839.G3_C1311`1<class Generated839.BaseClass1>::ClassMethod4000()
		ldstr "G3_C1311::ClassMethod4000.15473()"
		ldstr "class G3_C1311`1<class Generated839.BaseClass1> on type class Generated839.G3_C1311`1<class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated839.G3_C1311`1<class Generated839.BaseClass1>
		callvirt instance string class Generated839.G3_C1311`1<class Generated839.BaseClass1>::Method7<object>()
		ldstr "G3_C1311::Method7.15471<System.Object>()"
		ldstr "class G3_C1311`1<class Generated839.BaseClass1> on type class Generated839.G3_C1311`1<class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated839.G3_C1311`1<class Generated839.BaseClass1>
		callvirt instance string class Generated839.G3_C1311`1<class Generated839.BaseClass1>::Method6<object>()
		ldstr "G2_C350::Method6.8038<System.Object>()"
		ldstr "class G3_C1311`1<class Generated839.BaseClass1> on type class Generated839.G3_C1311`1<class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated839.G3_C1311`1<class Generated839.BaseClass1>
		callvirt instance string class Generated839.G3_C1311`1<class Generated839.BaseClass1>::Method5()
		ldstr "G2_C350::Method5.8037()"
		ldstr "class G3_C1311`1<class Generated839.BaseClass1> on type class Generated839.G3_C1311`1<class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated839.G3_C1311`1<class Generated839.BaseClass1>
		callvirt instance string class Generated839.G3_C1311`1<class Generated839.BaseClass1>::Method4()
		ldstr "G2_C350::Method4.8036()"
		ldstr "class G3_C1311`1<class Generated839.BaseClass1> on type class Generated839.G3_C1311`1<class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated839.G3_C1311`1<class Generated839.BaseClass1>
		callvirt instance string class Generated839.G3_C1311`1<class Generated839.BaseClass1>::ClassMethod1331<object>()
		ldstr "G2_C350::ClassMethod1331.MI.8042<System.Object>()"
		ldstr "class G3_C1311`1<class Generated839.BaseClass1> on type class Generated839.G3_C1311`1<class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated839.G3_C1311`1<class Generated839.BaseClass1>
		callvirt instance string class Generated839.G3_C1311`1<class Generated839.BaseClass1>::ClassMethod1330<object>()
		ldstr "G2_C350::ClassMethod1330.MI.8041<System.Object>()"
		ldstr "class G3_C1311`1<class Generated839.BaseClass1> on type class Generated839.G3_C1311`1<class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated839.G3_C1311`1<class Generated839.BaseClass1>
		callvirt instance string class Generated839.G3_C1311`1<class Generated839.BaseClass1>::ClassMethod1329()
		ldstr "G2_C350::ClassMethod1329.MI.8040()"
		ldstr "class G3_C1311`1<class Generated839.BaseClass1> on type class Generated839.G3_C1311`1<class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated839.G3_C1311`1<class Generated839.BaseClass1>
		callvirt instance string class Generated839.G3_C1311`1<class Generated839.BaseClass1>::ClassMethod1328()
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G3_C1311`1<class Generated839.BaseClass1> on type class Generated839.G3_C1311`1<class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>
		callvirt instance string class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::ClassMethod1331<object>()
		ldstr "G2_C350::ClassMethod1331.MI.8042<System.Object>()"
		ldstr "class G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0> on type class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>
		callvirt instance string class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::ClassMethod1330<object>()
		ldstr "G2_C350::ClassMethod1330.MI.8041<System.Object>()"
		ldstr "class G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0> on type class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>
		callvirt instance string class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::ClassMethod1329()
		ldstr "G2_C350::ClassMethod1329.MI.8040()"
		ldstr "class G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0> on type class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>
		callvirt instance string class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::ClassMethod1328()
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0> on type class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>
		callvirt instance string class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::Method7<object>()
		ldstr "G1_C7::Method7.4815<System.Object>()"
		ldstr "class G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0> on type class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>
		callvirt instance string class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::Method6<object>()
		ldstr "G1_C7::Method6.4813<System.Object>()"
		ldstr "class G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0> on type class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>
		callvirt instance string class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::Method5()
		ldstr "G2_C350::Method5.8037()"
		ldstr "class G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0> on type class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>
		callvirt instance string class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::Method4()
		ldstr "G1_C7::Method4.4811()"
		ldstr "class G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0> on type class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated839.IBase1`1<class Generated839.BaseClass1>::Method4()
		ldstr "G2_C350::Method4.8036()"
		ldstr "class IBase1`1<class Generated839.BaseClass1> on type class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated839.IBase1`1<class Generated839.BaseClass1>::Method5()
		ldstr "G2_C350::Method5.8037()"
		ldstr "class IBase1`1<class Generated839.BaseClass1> on type class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated839.IBase1`1<class Generated839.BaseClass1>::Method6<object>()
		ldstr "G2_C350::Method6.MI.8039<System.Object>()"
		ldstr "class IBase1`1<class Generated839.BaseClass1> on type class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated839.IBase2`2<class Generated839.BaseClass1,class Generated839.BaseClass1>::Method7<object>()
		ldstr "G1_C7::Method7.MI.4816<System.Object>()"
		ldstr "class IBase2`2<class Generated839.BaseClass1,class Generated839.BaseClass1> on type class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated839.IBase1`1<class Generated839.BaseClass0>::Method4()
		ldstr "G2_C350::Method4.8036()"
		ldstr "class IBase1`1<class Generated839.BaseClass0> on type class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated839.IBase1`1<class Generated839.BaseClass0>::Method5()
		ldstr "G2_C350::Method5.8037()"
		ldstr "class IBase1`1<class Generated839.BaseClass0> on type class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated839.IBase1`1<class Generated839.BaseClass0>::Method6<object>()
		ldstr "G2_C350::Method6.MI.8039<System.Object>()"
		ldstr "class IBase1`1<class Generated839.BaseClass0> on type class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated839.IBase2`2<class Generated839.BaseClass0,class Generated839.BaseClass1>::Method7<object>()
		ldstr "G2_C350::Method7.MI.8035<System.Object>()"
		ldstr "class IBase2`2<class Generated839.BaseClass0,class Generated839.BaseClass1> on type class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>
		callvirt instance string class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>::Method6<object>()
		ldstr "G2_C350::Method6.8038<System.Object>()"
		ldstr "class G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0> on type class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>
		callvirt instance string class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>::Method5()
		ldstr "G2_C350::Method5.8037()"
		ldstr "class G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0> on type class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>
		callvirt instance string class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>::Method4()
		ldstr "G2_C350::Method4.8036()"
		ldstr "class G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0> on type class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>
		callvirt instance string class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>::Method7<object>()
		ldstr "G2_C350::Method7.8034<System.Object>()"
		ldstr "class G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0> on type class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>
		callvirt instance string class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>::ClassMethod1331<object>()
		ldstr "G2_C350::ClassMethod1331.MI.8042<System.Object>()"
		ldstr "class G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0> on type class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>
		callvirt instance string class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>::ClassMethod1330<object>()
		ldstr "G2_C350::ClassMethod1330.MI.8041<System.Object>()"
		ldstr "class G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0> on type class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>
		callvirt instance string class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>::ClassMethod1329()
		ldstr "G2_C350::ClassMethod1329.MI.8040()"
		ldstr "class G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0> on type class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>
		callvirt instance string class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>::ClassMethod1328()
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0> on type class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>
		callvirt instance string class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::ClassMethod1331<object>()
		ldstr "G2_C350::ClassMethod1331.MI.8042<System.Object>()"
		ldstr "class G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0> on type class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>
		callvirt instance string class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::ClassMethod1330<object>()
		ldstr "G2_C350::ClassMethod1330.MI.8041<System.Object>()"
		ldstr "class G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0> on type class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>
		callvirt instance string class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::ClassMethod1329()
		ldstr "G2_C350::ClassMethod1329.MI.8040()"
		ldstr "class G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0> on type class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>
		callvirt instance string class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::ClassMethod1328()
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0> on type class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>
		callvirt instance string class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::Method7<object>()
		ldstr "G1_C7::Method7.4815<System.Object>()"
		ldstr "class G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0> on type class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>
		callvirt instance string class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::Method6<object>()
		ldstr "G1_C7::Method6.4813<System.Object>()"
		ldstr "class G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0> on type class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>
		callvirt instance string class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::Method5()
		ldstr "G2_C350::Method5.8037()"
		ldstr "class G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0> on type class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>
		callvirt instance string class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::Method4()
		ldstr "G1_C7::Method4.4811()"
		ldstr "class G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0> on type class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated839.IBase1`1<class Generated839.BaseClass1>::Method4()
		ldstr "G2_C350::Method4.8036()"
		ldstr "class IBase1`1<class Generated839.BaseClass1> on type class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated839.IBase1`1<class Generated839.BaseClass1>::Method5()
		ldstr "G2_C350::Method5.8037()"
		ldstr "class IBase1`1<class Generated839.BaseClass1> on type class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated839.IBase1`1<class Generated839.BaseClass1>::Method6<object>()
		ldstr "G2_C350::Method6.MI.8039<System.Object>()"
		ldstr "class IBase1`1<class Generated839.BaseClass1> on type class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated839.IBase2`2<class Generated839.BaseClass1,class Generated839.BaseClass1>::Method7<object>()
		ldstr "G2_C350::Method7.MI.8035<System.Object>()"
		ldstr "class IBase2`2<class Generated839.BaseClass1,class Generated839.BaseClass1> on type class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated839.IBase1`1<class Generated839.BaseClass0>::Method4()
		ldstr "G2_C350::Method4.8036()"
		ldstr "class IBase1`1<class Generated839.BaseClass0> on type class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated839.IBase1`1<class Generated839.BaseClass0>::Method5()
		ldstr "G2_C350::Method5.8037()"
		ldstr "class IBase1`1<class Generated839.BaseClass0> on type class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated839.IBase1`1<class Generated839.BaseClass0>::Method6<object>()
		ldstr "G2_C350::Method6.MI.8039<System.Object>()"
		ldstr "class IBase1`1<class Generated839.BaseClass0> on type class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated839.IBase2`2<class Generated839.BaseClass0,class Generated839.BaseClass1>::Method7<object>()
		ldstr "G2_C350::Method7.MI.8035<System.Object>()"
		ldstr "class IBase2`2<class Generated839.BaseClass0,class Generated839.BaseClass1> on type class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>
		callvirt instance string class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>::Method6<object>()
		ldstr "G2_C350::Method6.8038<System.Object>()"
		ldstr "class G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1> on type class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>
		callvirt instance string class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>::Method5()
		ldstr "G2_C350::Method5.8037()"
		ldstr "class G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1> on type class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>
		callvirt instance string class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>::Method4()
		ldstr "G2_C350::Method4.8036()"
		ldstr "class G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1> on type class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>
		callvirt instance string class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>::Method7<object>()
		ldstr "G2_C350::Method7.8034<System.Object>()"
		ldstr "class G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1> on type class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>
		callvirt instance string class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>::ClassMethod1331<object>()
		ldstr "G2_C350::ClassMethod1331.MI.8042<System.Object>()"
		ldstr "class G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1> on type class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>
		callvirt instance string class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>::ClassMethod1330<object>()
		ldstr "G2_C350::ClassMethod1330.MI.8041<System.Object>()"
		ldstr "class G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1> on type class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>
		callvirt instance string class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>::ClassMethod1329()
		ldstr "G2_C350::ClassMethod1329.MI.8040()"
		ldstr "class G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1> on type class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>
		callvirt instance string class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>::ClassMethod1328()
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1> on type class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>
		callvirt instance string class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::ClassMethod1331<object>()
		ldstr "G2_C350::ClassMethod1331.MI.8042<System.Object>()"
		ldstr "class G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0> on type class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>
		callvirt instance string class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::ClassMethod1330<object>()
		ldstr "G2_C350::ClassMethod1330.MI.8041<System.Object>()"
		ldstr "class G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0> on type class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>
		callvirt instance string class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::ClassMethod1329()
		ldstr "G2_C350::ClassMethod1329.MI.8040()"
		ldstr "class G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0> on type class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>
		callvirt instance string class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::ClassMethod1328()
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0> on type class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>
		callvirt instance string class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::Method7<object>()
		ldstr "G1_C7::Method7.4815<System.Object>()"
		ldstr "class G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0> on type class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>
		callvirt instance string class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::Method6<object>()
		ldstr "G1_C7::Method6.4813<System.Object>()"
		ldstr "class G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0> on type class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>
		callvirt instance string class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::Method5()
		ldstr "G2_C350::Method5.8037()"
		ldstr "class G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0> on type class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>
		callvirt instance string class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::Method4()
		ldstr "G1_C7::Method4.4811()"
		ldstr "class G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0> on type class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated839.IBase1`1<class Generated839.BaseClass1>::Method4()
		ldstr "G2_C350::Method4.8036()"
		ldstr "class IBase1`1<class Generated839.BaseClass1> on type class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated839.IBase1`1<class Generated839.BaseClass1>::Method5()
		ldstr "G2_C350::Method5.8037()"
		ldstr "class IBase1`1<class Generated839.BaseClass1> on type class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated839.IBase1`1<class Generated839.BaseClass1>::Method6<object>()
		ldstr "G2_C350::Method6.MI.8039<System.Object>()"
		ldstr "class IBase1`1<class Generated839.BaseClass1> on type class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated839.IBase2`2<class Generated839.BaseClass1,class Generated839.BaseClass1>::Method7<object>()
		ldstr "G1_C7::Method7.MI.4816<System.Object>()"
		ldstr "class IBase2`2<class Generated839.BaseClass1,class Generated839.BaseClass1> on type class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated839.IBase1`1<class Generated839.BaseClass0>::Method4()
		ldstr "G2_C350::Method4.8036()"
		ldstr "class IBase1`1<class Generated839.BaseClass0> on type class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated839.IBase1`1<class Generated839.BaseClass0>::Method5()
		ldstr "G2_C350::Method5.8037()"
		ldstr "class IBase1`1<class Generated839.BaseClass0> on type class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated839.IBase1`1<class Generated839.BaseClass0>::Method6<object>()
		ldstr "G2_C350::Method6.MI.8039<System.Object>()"
		ldstr "class IBase1`1<class Generated839.BaseClass0> on type class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated839.IBase2`2<class Generated839.BaseClass0,class Generated839.BaseClass1>::Method7<object>()
		ldstr "G2_C350::Method7.MI.8035<System.Object>()"
		ldstr "class IBase2`2<class Generated839.BaseClass0,class Generated839.BaseClass1> on type class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>
		callvirt instance string class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::Method6<object>()
		ldstr "G2_C350::Method6.8038<System.Object>()"
		ldstr "class G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0> on type class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>
		callvirt instance string class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::Method5()
		ldstr "G2_C350::Method5.8037()"
		ldstr "class G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0> on type class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>
		callvirt instance string class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::Method4()
		ldstr "G2_C350::Method4.8036()"
		ldstr "class G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0> on type class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>
		callvirt instance string class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::Method7<object>()
		ldstr "G2_C350::Method7.8034<System.Object>()"
		ldstr "class G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0> on type class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>
		callvirt instance string class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::ClassMethod1331<object>()
		ldstr "G2_C350::ClassMethod1331.MI.8042<System.Object>()"
		ldstr "class G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0> on type class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>
		callvirt instance string class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::ClassMethod1330<object>()
		ldstr "G2_C350::ClassMethod1330.MI.8041<System.Object>()"
		ldstr "class G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0> on type class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>
		callvirt instance string class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::ClassMethod1329()
		ldstr "G2_C350::ClassMethod1329.MI.8040()"
		ldstr "class G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0> on type class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>
		callvirt instance string class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::ClassMethod1328()
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0> on type class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>
		callvirt instance string class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::ClassMethod1331<object>()
		ldstr "G2_C350::ClassMethod1331.MI.8042<System.Object>()"
		ldstr "class G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0> on type class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>
		callvirt instance string class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::ClassMethod1330<object>()
		ldstr "G2_C350::ClassMethod1330.MI.8041<System.Object>()"
		ldstr "class G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0> on type class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>
		callvirt instance string class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::ClassMethod1329()
		ldstr "G2_C350::ClassMethod1329.MI.8040()"
		ldstr "class G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0> on type class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>
		callvirt instance string class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::ClassMethod1328()
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0> on type class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>
		callvirt instance string class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::Method7<object>()
		ldstr "G1_C7::Method7.4815<System.Object>()"
		ldstr "class G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0> on type class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>
		callvirt instance string class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::Method6<object>()
		ldstr "G1_C7::Method6.4813<System.Object>()"
		ldstr "class G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0> on type class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>
		callvirt instance string class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::Method5()
		ldstr "G2_C350::Method5.8037()"
		ldstr "class G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0> on type class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>
		callvirt instance string class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::Method4()
		ldstr "G1_C7::Method4.4811()"
		ldstr "class G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0> on type class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated839.IBase1`1<class Generated839.BaseClass1>::Method4()
		ldstr "G2_C350::Method4.8036()"
		ldstr "class IBase1`1<class Generated839.BaseClass1> on type class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated839.IBase1`1<class Generated839.BaseClass1>::Method5()
		ldstr "G2_C350::Method5.8037()"
		ldstr "class IBase1`1<class Generated839.BaseClass1> on type class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated839.IBase1`1<class Generated839.BaseClass1>::Method6<object>()
		ldstr "G2_C350::Method6.MI.8039<System.Object>()"
		ldstr "class IBase1`1<class Generated839.BaseClass1> on type class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated839.IBase2`2<class Generated839.BaseClass1,class Generated839.BaseClass1>::Method7<object>()
		ldstr "G2_C350::Method7.MI.8035<System.Object>()"
		ldstr "class IBase2`2<class Generated839.BaseClass1,class Generated839.BaseClass1> on type class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated839.IBase1`1<class Generated839.BaseClass0>::Method4()
		ldstr "G2_C350::Method4.8036()"
		ldstr "class IBase1`1<class Generated839.BaseClass0> on type class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated839.IBase1`1<class Generated839.BaseClass0>::Method5()
		ldstr "G2_C350::Method5.8037()"
		ldstr "class IBase1`1<class Generated839.BaseClass0> on type class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated839.IBase1`1<class Generated839.BaseClass0>::Method6<object>()
		ldstr "G2_C350::Method6.MI.8039<System.Object>()"
		ldstr "class IBase1`1<class Generated839.BaseClass0> on type class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated839.IBase2`2<class Generated839.BaseClass0,class Generated839.BaseClass1>::Method7<object>()
		ldstr "G2_C350::Method7.MI.8035<System.Object>()"
		ldstr "class IBase2`2<class Generated839.BaseClass0,class Generated839.BaseClass1> on type class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>
		callvirt instance string class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>::Method6<object>()
		ldstr "G2_C350::Method6.8038<System.Object>()"
		ldstr "class G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1> on type class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>
		callvirt instance string class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>::Method5()
		ldstr "G2_C350::Method5.8037()"
		ldstr "class G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1> on type class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>
		callvirt instance string class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>::Method4()
		ldstr "G2_C350::Method4.8036()"
		ldstr "class G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1> on type class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>
		callvirt instance string class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>::Method7<object>()
		ldstr "G2_C350::Method7.8034<System.Object>()"
		ldstr "class G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1> on type class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>
		callvirt instance string class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>::ClassMethod1331<object>()
		ldstr "G2_C350::ClassMethod1331.MI.8042<System.Object>()"
		ldstr "class G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1> on type class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>
		callvirt instance string class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>::ClassMethod1330<object>()
		ldstr "G2_C350::ClassMethod1330.MI.8041<System.Object>()"
		ldstr "class G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1> on type class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>
		callvirt instance string class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>::ClassMethod1329()
		ldstr "G2_C350::ClassMethod1329.MI.8040()"
		ldstr "class G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1> on type class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>
		callvirt instance string class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>::ClassMethod1328()
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1> on type class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>"
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
		newobj instance void class Generated839.G3_C1311`1<class Generated839.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C350::ClassMethod1329.MI.8040()#G2_C350::ClassMethod1330.MI.8041<System.Object>()#G2_C350::ClassMethod1331.MI.8042<System.Object>()#G1_C7::Method4.4811()#G2_C350::Method5.8037()#G1_C7::Method6.4813<System.Object>()#G1_C7::Method7.4815<System.Object>()#"
		call void Generated839.Generated839::M.G1_C7.T.T<class Generated839.BaseClass1,class Generated839.BaseClass0,class Generated839.G3_C1311`1<class Generated839.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C350::ClassMethod1329.MI.8040()#G2_C350::ClassMethod1330.MI.8041<System.Object>()#G2_C350::ClassMethod1331.MI.8042<System.Object>()#G1_C7::Method4.4811()#G2_C350::Method5.8037()#G1_C7::Method6.4813<System.Object>()#G1_C7::Method7.4815<System.Object>()#"
		call void Generated839.Generated839::M.G1_C7.B.T<class Generated839.BaseClass0,class Generated839.G3_C1311`1<class Generated839.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C350::ClassMethod1329.MI.8040()#G2_C350::ClassMethod1330.MI.8041<System.Object>()#G2_C350::ClassMethod1331.MI.8042<System.Object>()#G1_C7::Method4.4811()#G2_C350::Method5.8037()#G1_C7::Method6.4813<System.Object>()#G1_C7::Method7.4815<System.Object>()#"
		call void Generated839.Generated839::M.G1_C7.B.A<class Generated839.G3_C1311`1<class Generated839.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C350::Method4.8036()#G2_C350::Method5.8037()#G2_C350::Method6.MI.8039<System.Object>()#"
		call void Generated839.Generated839::M.IBase1.T<class Generated839.BaseClass1,class Generated839.G3_C1311`1<class Generated839.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C350::Method4.8036()#G2_C350::Method5.8037()#G2_C350::Method6.MI.8039<System.Object>()#"
		call void Generated839.Generated839::M.IBase1.B<class Generated839.G3_C1311`1<class Generated839.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C7::Method7.MI.4816<System.Object>()#"
		call void Generated839.Generated839::M.IBase2.T.T<class Generated839.BaseClass1,class Generated839.BaseClass1,class Generated839.G3_C1311`1<class Generated839.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C7::Method7.MI.4816<System.Object>()#"
		call void Generated839.Generated839::M.IBase2.B.T<class Generated839.BaseClass1,class Generated839.G3_C1311`1<class Generated839.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::Method7.MI.4816<System.Object>()#"
		call void Generated839.Generated839::M.IBase2.B.B<class Generated839.G3_C1311`1<class Generated839.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C350::Method4.8036()#G2_C350::Method5.8037()#G2_C350::Method6.MI.8039<System.Object>()#"
		call void Generated839.Generated839::M.IBase1.T<class Generated839.BaseClass0,class Generated839.G3_C1311`1<class Generated839.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C350::Method4.8036()#G2_C350::Method5.8037()#G2_C350::Method6.MI.8039<System.Object>()#"
		call void Generated839.Generated839::M.IBase1.A<class Generated839.G3_C1311`1<class Generated839.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1311::Method7.MI.15472<System.Object>()#"
		call void Generated839.Generated839::M.IBase2.T.T<class Generated839.BaseClass0,class Generated839.BaseClass1,class Generated839.G3_C1311`1<class Generated839.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G3_C1311::Method7.MI.15472<System.Object>()#"
		call void Generated839.Generated839::M.IBase2.A.T<class Generated839.BaseClass1,class Generated839.G3_C1311`1<class Generated839.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G3_C1311::Method7.MI.15472<System.Object>()#"
		call void Generated839.Generated839::M.IBase2.A.B<class Generated839.G3_C1311`1<class Generated839.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C350::ClassMethod1329.MI.8040()#G2_C350::ClassMethod1330.MI.8041<System.Object>()#G2_C350::ClassMethod1331.MI.8042<System.Object>()#G2_C350::Method4.8036()#G2_C350::Method5.8037()#G2_C350::Method6.8038<System.Object>()#G3_C1311::Method7.15471<System.Object>()#"
		call void Generated839.Generated839::M.G2_C350.T.T<class Generated839.BaseClass1,class Generated839.BaseClass0,class Generated839.G3_C1311`1<class Generated839.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C350::ClassMethod1329.MI.8040()#G2_C350::ClassMethod1330.MI.8041<System.Object>()#G2_C350::ClassMethod1331.MI.8042<System.Object>()#G2_C350::Method4.8036()#G2_C350::Method5.8037()#G2_C350::Method6.8038<System.Object>()#G3_C1311::Method7.15471<System.Object>()#"
		call void Generated839.Generated839::M.G2_C350.B.T<class Generated839.BaseClass0,class Generated839.G3_C1311`1<class Generated839.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C350::ClassMethod1329.MI.8040()#G2_C350::ClassMethod1330.MI.8041<System.Object>()#G2_C350::ClassMethod1331.MI.8042<System.Object>()#G2_C350::Method4.8036()#G2_C350::Method5.8037()#G2_C350::Method6.8038<System.Object>()#G3_C1311::Method7.15471<System.Object>()#"
		call void Generated839.Generated839::M.G2_C350.B.A<class Generated839.G3_C1311`1<class Generated839.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C350::ClassMethod1329.MI.8040()#G2_C350::ClassMethod1330.MI.8041<System.Object>()#G2_C350::ClassMethod1331.MI.8042<System.Object>()#G3_C1311::ClassMethod4000.15473()#G3_C1311::ClassMethod4001.15474()#G3_C1311::ClassMethod4002.15475<System.Object>()#G3_C1311::ClassMethod4003.15476<System.Object>()#G2_C350::Method4.8036()#G2_C350::Method5.8037()#G2_C350::Method6.8038<System.Object>()#G3_C1311::Method7.15471<System.Object>()#"
		call void Generated839.Generated839::M.G3_C1311.T<class Generated839.BaseClass0,class Generated839.G3_C1311`1<class Generated839.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C350::ClassMethod1329.MI.8040()#G2_C350::ClassMethod1330.MI.8041<System.Object>()#G2_C350::ClassMethod1331.MI.8042<System.Object>()#G3_C1311::ClassMethod4000.15473()#G3_C1311::ClassMethod4001.15474()#G3_C1311::ClassMethod4002.15475<System.Object>()#G3_C1311::ClassMethod4003.15476<System.Object>()#G2_C350::Method4.8036()#G2_C350::Method5.8037()#G2_C350::Method6.8038<System.Object>()#G3_C1311::Method7.15471<System.Object>()#"
		call void Generated839.Generated839::M.G3_C1311.A<class Generated839.G3_C1311`1<class Generated839.BaseClass0>>(!!0,string)
		newobj instance void class Generated839.G3_C1311`1<class Generated839.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C350::ClassMethod1329.MI.8040()#G2_C350::ClassMethod1330.MI.8041<System.Object>()#G2_C350::ClassMethod1331.MI.8042<System.Object>()#G1_C7::Method4.4811()#G2_C350::Method5.8037()#G1_C7::Method6.4813<System.Object>()#G1_C7::Method7.4815<System.Object>()#"
		call void Generated839.Generated839::M.G1_C7.T.T<class Generated839.BaseClass1,class Generated839.BaseClass0,class Generated839.G3_C1311`1<class Generated839.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C350::ClassMethod1329.MI.8040()#G2_C350::ClassMethod1330.MI.8041<System.Object>()#G2_C350::ClassMethod1331.MI.8042<System.Object>()#G1_C7::Method4.4811()#G2_C350::Method5.8037()#G1_C7::Method6.4813<System.Object>()#G1_C7::Method7.4815<System.Object>()#"
		call void Generated839.Generated839::M.G1_C7.B.T<class Generated839.BaseClass0,class Generated839.G3_C1311`1<class Generated839.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C350::ClassMethod1329.MI.8040()#G2_C350::ClassMethod1330.MI.8041<System.Object>()#G2_C350::ClassMethod1331.MI.8042<System.Object>()#G1_C7::Method4.4811()#G2_C350::Method5.8037()#G1_C7::Method6.4813<System.Object>()#G1_C7::Method7.4815<System.Object>()#"
		call void Generated839.Generated839::M.G1_C7.B.A<class Generated839.G3_C1311`1<class Generated839.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C350::Method4.8036()#G2_C350::Method5.8037()#G2_C350::Method6.MI.8039<System.Object>()#"
		call void Generated839.Generated839::M.IBase1.T<class Generated839.BaseClass1,class Generated839.G3_C1311`1<class Generated839.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C350::Method4.8036()#G2_C350::Method5.8037()#G2_C350::Method6.MI.8039<System.Object>()#"
		call void Generated839.Generated839::M.IBase1.B<class Generated839.G3_C1311`1<class Generated839.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C350::Method7.MI.8035<System.Object>()#"
		call void Generated839.Generated839::M.IBase2.T.T<class Generated839.BaseClass1,class Generated839.BaseClass1,class Generated839.G3_C1311`1<class Generated839.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C350::Method7.MI.8035<System.Object>()#"
		call void Generated839.Generated839::M.IBase2.B.T<class Generated839.BaseClass1,class Generated839.G3_C1311`1<class Generated839.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C350::Method7.MI.8035<System.Object>()#"
		call void Generated839.Generated839::M.IBase2.B.B<class Generated839.G3_C1311`1<class Generated839.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C350::Method4.8036()#G2_C350::Method5.8037()#G2_C350::Method6.MI.8039<System.Object>()#"
		call void Generated839.Generated839::M.IBase1.T<class Generated839.BaseClass0,class Generated839.G3_C1311`1<class Generated839.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C350::Method4.8036()#G2_C350::Method5.8037()#G2_C350::Method6.MI.8039<System.Object>()#"
		call void Generated839.Generated839::M.IBase1.A<class Generated839.G3_C1311`1<class Generated839.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1311::Method7.MI.15472<System.Object>()#"
		call void Generated839.Generated839::M.IBase2.T.T<class Generated839.BaseClass0,class Generated839.BaseClass1,class Generated839.G3_C1311`1<class Generated839.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G3_C1311::Method7.MI.15472<System.Object>()#"
		call void Generated839.Generated839::M.IBase2.A.T<class Generated839.BaseClass1,class Generated839.G3_C1311`1<class Generated839.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1311::Method7.MI.15472<System.Object>()#"
		call void Generated839.Generated839::M.IBase2.A.B<class Generated839.G3_C1311`1<class Generated839.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C350::ClassMethod1329.MI.8040()#G2_C350::ClassMethod1330.MI.8041<System.Object>()#G2_C350::ClassMethod1331.MI.8042<System.Object>()#G2_C350::Method4.8036()#G2_C350::Method5.8037()#G2_C350::Method6.8038<System.Object>()#G3_C1311::Method7.15471<System.Object>()#"
		call void Generated839.Generated839::M.G2_C350.T.T<class Generated839.BaseClass1,class Generated839.BaseClass1,class Generated839.G3_C1311`1<class Generated839.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C350::ClassMethod1329.MI.8040()#G2_C350::ClassMethod1330.MI.8041<System.Object>()#G2_C350::ClassMethod1331.MI.8042<System.Object>()#G2_C350::Method4.8036()#G2_C350::Method5.8037()#G2_C350::Method6.8038<System.Object>()#G3_C1311::Method7.15471<System.Object>()#"
		call void Generated839.Generated839::M.G2_C350.B.T<class Generated839.BaseClass1,class Generated839.G3_C1311`1<class Generated839.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C350::ClassMethod1329.MI.8040()#G2_C350::ClassMethod1330.MI.8041<System.Object>()#G2_C350::ClassMethod1331.MI.8042<System.Object>()#G2_C350::Method4.8036()#G2_C350::Method5.8037()#G2_C350::Method6.8038<System.Object>()#G3_C1311::Method7.15471<System.Object>()#"
		call void Generated839.Generated839::M.G2_C350.B.B<class Generated839.G3_C1311`1<class Generated839.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C350::ClassMethod1329.MI.8040()#G2_C350::ClassMethod1330.MI.8041<System.Object>()#G2_C350::ClassMethod1331.MI.8042<System.Object>()#G3_C1311::ClassMethod4000.15473()#G3_C1311::ClassMethod4001.15474()#G3_C1311::ClassMethod4002.15475<System.Object>()#G3_C1311::ClassMethod4003.15476<System.Object>()#G2_C350::Method4.8036()#G2_C350::Method5.8037()#G2_C350::Method6.8038<System.Object>()#G3_C1311::Method7.15471<System.Object>()#"
		call void Generated839.Generated839::M.G3_C1311.T<class Generated839.BaseClass1,class Generated839.G3_C1311`1<class Generated839.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C350::ClassMethod1329.MI.8040()#G2_C350::ClassMethod1330.MI.8041<System.Object>()#G2_C350::ClassMethod1331.MI.8042<System.Object>()#G3_C1311::ClassMethod4000.15473()#G3_C1311::ClassMethod4001.15474()#G3_C1311::ClassMethod4002.15475<System.Object>()#G3_C1311::ClassMethod4003.15476<System.Object>()#G2_C350::Method4.8036()#G2_C350::Method5.8037()#G2_C350::Method6.8038<System.Object>()#G3_C1311::Method7.15471<System.Object>()#"
		call void Generated839.Generated839::M.G3_C1311.B<class Generated839.G3_C1311`1<class Generated839.BaseClass1>>(!!0,string)
		newobj instance void class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C350::ClassMethod1329.MI.8040()#G2_C350::ClassMethod1330.MI.8041<System.Object>()#G2_C350::ClassMethod1331.MI.8042<System.Object>()#G1_C7::Method4.4811()#G2_C350::Method5.8037()#G1_C7::Method6.4813<System.Object>()#G1_C7::Method7.4815<System.Object>()#"
		call void Generated839.Generated839::M.G1_C7.T.T<class Generated839.BaseClass1,class Generated839.BaseClass0,class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C350::ClassMethod1329.MI.8040()#G2_C350::ClassMethod1330.MI.8041<System.Object>()#G2_C350::ClassMethod1331.MI.8042<System.Object>()#G1_C7::Method4.4811()#G2_C350::Method5.8037()#G1_C7::Method6.4813<System.Object>()#G1_C7::Method7.4815<System.Object>()#"
		call void Generated839.Generated839::M.G1_C7.B.T<class Generated839.BaseClass0,class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C350::ClassMethod1329.MI.8040()#G2_C350::ClassMethod1330.MI.8041<System.Object>()#G2_C350::ClassMethod1331.MI.8042<System.Object>()#G1_C7::Method4.4811()#G2_C350::Method5.8037()#G1_C7::Method6.4813<System.Object>()#G1_C7::Method7.4815<System.Object>()#"
		call void Generated839.Generated839::M.G1_C7.B.A<class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C350::Method4.8036()#G2_C350::Method5.8037()#G2_C350::Method6.MI.8039<System.Object>()#"
		call void Generated839.Generated839::M.IBase1.T<class Generated839.BaseClass1,class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C350::Method4.8036()#G2_C350::Method5.8037()#G2_C350::Method6.MI.8039<System.Object>()#"
		call void Generated839.Generated839::M.IBase1.B<class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C7::Method7.MI.4816<System.Object>()#"
		call void Generated839.Generated839::M.IBase2.T.T<class Generated839.BaseClass1,class Generated839.BaseClass1,class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C7::Method7.MI.4816<System.Object>()#"
		call void Generated839.Generated839::M.IBase2.B.T<class Generated839.BaseClass1,class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::Method7.MI.4816<System.Object>()#"
		call void Generated839.Generated839::M.IBase2.B.B<class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C350::Method4.8036()#G2_C350::Method5.8037()#G2_C350::Method6.MI.8039<System.Object>()#"
		call void Generated839.Generated839::M.IBase1.T<class Generated839.BaseClass0,class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C350::Method4.8036()#G2_C350::Method5.8037()#G2_C350::Method6.MI.8039<System.Object>()#"
		call void Generated839.Generated839::M.IBase1.A<class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C350::Method7.MI.8035<System.Object>()#"
		call void Generated839.Generated839::M.IBase2.T.T<class Generated839.BaseClass0,class Generated839.BaseClass1,class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C350::Method7.MI.8035<System.Object>()#"
		call void Generated839.Generated839::M.IBase2.A.T<class Generated839.BaseClass1,class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C350::Method7.MI.8035<System.Object>()#"
		call void Generated839.Generated839::M.IBase2.A.B<class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C350::ClassMethod1329.MI.8040()#G2_C350::ClassMethod1330.MI.8041<System.Object>()#G2_C350::ClassMethod1331.MI.8042<System.Object>()#G2_C350::Method4.8036()#G2_C350::Method5.8037()#G2_C350::Method6.8038<System.Object>()#G2_C350::Method7.8034<System.Object>()#"
		call void Generated839.Generated839::M.G2_C350.T.T<class Generated839.BaseClass0,class Generated839.BaseClass0,class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C350::ClassMethod1329.MI.8040()#G2_C350::ClassMethod1330.MI.8041<System.Object>()#G2_C350::ClassMethod1331.MI.8042<System.Object>()#G2_C350::Method4.8036()#G2_C350::Method5.8037()#G2_C350::Method6.8038<System.Object>()#G2_C350::Method7.8034<System.Object>()#"
		call void Generated839.Generated839::M.G2_C350.A.T<class Generated839.BaseClass0,class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C350::ClassMethod1329.MI.8040()#G2_C350::ClassMethod1330.MI.8041<System.Object>()#G2_C350::ClassMethod1331.MI.8042<System.Object>()#G2_C350::Method4.8036()#G2_C350::Method5.8037()#G2_C350::Method6.8038<System.Object>()#G2_C350::Method7.8034<System.Object>()#"
		call void Generated839.Generated839::M.G2_C350.A.A<class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>>(!!0,string)
		newobj instance void class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C350::ClassMethod1329.MI.8040()#G2_C350::ClassMethod1330.MI.8041<System.Object>()#G2_C350::ClassMethod1331.MI.8042<System.Object>()#G1_C7::Method4.4811()#G2_C350::Method5.8037()#G1_C7::Method6.4813<System.Object>()#G1_C7::Method7.4815<System.Object>()#"
		call void Generated839.Generated839::M.G1_C7.T.T<class Generated839.BaseClass1,class Generated839.BaseClass0,class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C350::ClassMethod1329.MI.8040()#G2_C350::ClassMethod1330.MI.8041<System.Object>()#G2_C350::ClassMethod1331.MI.8042<System.Object>()#G1_C7::Method4.4811()#G2_C350::Method5.8037()#G1_C7::Method6.4813<System.Object>()#G1_C7::Method7.4815<System.Object>()#"
		call void Generated839.Generated839::M.G1_C7.B.T<class Generated839.BaseClass0,class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C350::ClassMethod1329.MI.8040()#G2_C350::ClassMethod1330.MI.8041<System.Object>()#G2_C350::ClassMethod1331.MI.8042<System.Object>()#G1_C7::Method4.4811()#G2_C350::Method5.8037()#G1_C7::Method6.4813<System.Object>()#G1_C7::Method7.4815<System.Object>()#"
		call void Generated839.Generated839::M.G1_C7.B.A<class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C350::Method4.8036()#G2_C350::Method5.8037()#G2_C350::Method6.MI.8039<System.Object>()#"
		call void Generated839.Generated839::M.IBase1.T<class Generated839.BaseClass1,class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C350::Method4.8036()#G2_C350::Method5.8037()#G2_C350::Method6.MI.8039<System.Object>()#"
		call void Generated839.Generated839::M.IBase1.B<class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C350::Method7.MI.8035<System.Object>()#"
		call void Generated839.Generated839::M.IBase2.T.T<class Generated839.BaseClass1,class Generated839.BaseClass1,class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C350::Method7.MI.8035<System.Object>()#"
		call void Generated839.Generated839::M.IBase2.B.T<class Generated839.BaseClass1,class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C350::Method7.MI.8035<System.Object>()#"
		call void Generated839.Generated839::M.IBase2.B.B<class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C350::Method4.8036()#G2_C350::Method5.8037()#G2_C350::Method6.MI.8039<System.Object>()#"
		call void Generated839.Generated839::M.IBase1.T<class Generated839.BaseClass0,class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C350::Method4.8036()#G2_C350::Method5.8037()#G2_C350::Method6.MI.8039<System.Object>()#"
		call void Generated839.Generated839::M.IBase1.A<class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C350::Method7.MI.8035<System.Object>()#"
		call void Generated839.Generated839::M.IBase2.T.T<class Generated839.BaseClass0,class Generated839.BaseClass1,class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C350::Method7.MI.8035<System.Object>()#"
		call void Generated839.Generated839::M.IBase2.A.T<class Generated839.BaseClass1,class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C350::Method7.MI.8035<System.Object>()#"
		call void Generated839.Generated839::M.IBase2.A.B<class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C350::ClassMethod1329.MI.8040()#G2_C350::ClassMethod1330.MI.8041<System.Object>()#G2_C350::ClassMethod1331.MI.8042<System.Object>()#G2_C350::Method4.8036()#G2_C350::Method5.8037()#G2_C350::Method6.8038<System.Object>()#G2_C350::Method7.8034<System.Object>()#"
		call void Generated839.Generated839::M.G2_C350.T.T<class Generated839.BaseClass0,class Generated839.BaseClass1,class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C350::ClassMethod1329.MI.8040()#G2_C350::ClassMethod1330.MI.8041<System.Object>()#G2_C350::ClassMethod1331.MI.8042<System.Object>()#G2_C350::Method4.8036()#G2_C350::Method5.8037()#G2_C350::Method6.8038<System.Object>()#G2_C350::Method7.8034<System.Object>()#"
		call void Generated839.Generated839::M.G2_C350.A.T<class Generated839.BaseClass1,class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C350::ClassMethod1329.MI.8040()#G2_C350::ClassMethod1330.MI.8041<System.Object>()#G2_C350::ClassMethod1331.MI.8042<System.Object>()#G2_C350::Method4.8036()#G2_C350::Method5.8037()#G2_C350::Method6.8038<System.Object>()#G2_C350::Method7.8034<System.Object>()#"
		call void Generated839.Generated839::M.G2_C350.A.B<class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>>(!!0,string)
		newobj instance void class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C350::ClassMethod1329.MI.8040()#G2_C350::ClassMethod1330.MI.8041<System.Object>()#G2_C350::ClassMethod1331.MI.8042<System.Object>()#G1_C7::Method4.4811()#G2_C350::Method5.8037()#G1_C7::Method6.4813<System.Object>()#G1_C7::Method7.4815<System.Object>()#"
		call void Generated839.Generated839::M.G1_C7.T.T<class Generated839.BaseClass1,class Generated839.BaseClass0,class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C350::ClassMethod1329.MI.8040()#G2_C350::ClassMethod1330.MI.8041<System.Object>()#G2_C350::ClassMethod1331.MI.8042<System.Object>()#G1_C7::Method4.4811()#G2_C350::Method5.8037()#G1_C7::Method6.4813<System.Object>()#G1_C7::Method7.4815<System.Object>()#"
		call void Generated839.Generated839::M.G1_C7.B.T<class Generated839.BaseClass0,class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C350::ClassMethod1329.MI.8040()#G2_C350::ClassMethod1330.MI.8041<System.Object>()#G2_C350::ClassMethod1331.MI.8042<System.Object>()#G1_C7::Method4.4811()#G2_C350::Method5.8037()#G1_C7::Method6.4813<System.Object>()#G1_C7::Method7.4815<System.Object>()#"
		call void Generated839.Generated839::M.G1_C7.B.A<class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C350::Method4.8036()#G2_C350::Method5.8037()#G2_C350::Method6.MI.8039<System.Object>()#"
		call void Generated839.Generated839::M.IBase1.T<class Generated839.BaseClass1,class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C350::Method4.8036()#G2_C350::Method5.8037()#G2_C350::Method6.MI.8039<System.Object>()#"
		call void Generated839.Generated839::M.IBase1.B<class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C7::Method7.MI.4816<System.Object>()#"
		call void Generated839.Generated839::M.IBase2.T.T<class Generated839.BaseClass1,class Generated839.BaseClass1,class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C7::Method7.MI.4816<System.Object>()#"
		call void Generated839.Generated839::M.IBase2.B.T<class Generated839.BaseClass1,class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::Method7.MI.4816<System.Object>()#"
		call void Generated839.Generated839::M.IBase2.B.B<class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C350::Method4.8036()#G2_C350::Method5.8037()#G2_C350::Method6.MI.8039<System.Object>()#"
		call void Generated839.Generated839::M.IBase1.T<class Generated839.BaseClass0,class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C350::Method4.8036()#G2_C350::Method5.8037()#G2_C350::Method6.MI.8039<System.Object>()#"
		call void Generated839.Generated839::M.IBase1.A<class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C350::Method7.MI.8035<System.Object>()#"
		call void Generated839.Generated839::M.IBase2.T.T<class Generated839.BaseClass0,class Generated839.BaseClass1,class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C350::Method7.MI.8035<System.Object>()#"
		call void Generated839.Generated839::M.IBase2.A.T<class Generated839.BaseClass1,class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C350::Method7.MI.8035<System.Object>()#"
		call void Generated839.Generated839::M.IBase2.A.B<class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C350::ClassMethod1329.MI.8040()#G2_C350::ClassMethod1330.MI.8041<System.Object>()#G2_C350::ClassMethod1331.MI.8042<System.Object>()#G2_C350::Method4.8036()#G2_C350::Method5.8037()#G2_C350::Method6.8038<System.Object>()#G2_C350::Method7.8034<System.Object>()#"
		call void Generated839.Generated839::M.G2_C350.T.T<class Generated839.BaseClass1,class Generated839.BaseClass0,class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C350::ClassMethod1329.MI.8040()#G2_C350::ClassMethod1330.MI.8041<System.Object>()#G2_C350::ClassMethod1331.MI.8042<System.Object>()#G2_C350::Method4.8036()#G2_C350::Method5.8037()#G2_C350::Method6.8038<System.Object>()#G2_C350::Method7.8034<System.Object>()#"
		call void Generated839.Generated839::M.G2_C350.B.T<class Generated839.BaseClass0,class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C350::ClassMethod1329.MI.8040()#G2_C350::ClassMethod1330.MI.8041<System.Object>()#G2_C350::ClassMethod1331.MI.8042<System.Object>()#G2_C350::Method4.8036()#G2_C350::Method5.8037()#G2_C350::Method6.8038<System.Object>()#G2_C350::Method7.8034<System.Object>()#"
		call void Generated839.Generated839::M.G2_C350.B.A<class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>>(!!0,string)
		newobj instance void class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C350::ClassMethod1329.MI.8040()#G2_C350::ClassMethod1330.MI.8041<System.Object>()#G2_C350::ClassMethod1331.MI.8042<System.Object>()#G1_C7::Method4.4811()#G2_C350::Method5.8037()#G1_C7::Method6.4813<System.Object>()#G1_C7::Method7.4815<System.Object>()#"
		call void Generated839.Generated839::M.G1_C7.T.T<class Generated839.BaseClass1,class Generated839.BaseClass0,class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C350::ClassMethod1329.MI.8040()#G2_C350::ClassMethod1330.MI.8041<System.Object>()#G2_C350::ClassMethod1331.MI.8042<System.Object>()#G1_C7::Method4.4811()#G2_C350::Method5.8037()#G1_C7::Method6.4813<System.Object>()#G1_C7::Method7.4815<System.Object>()#"
		call void Generated839.Generated839::M.G1_C7.B.T<class Generated839.BaseClass0,class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C350::ClassMethod1329.MI.8040()#G2_C350::ClassMethod1330.MI.8041<System.Object>()#G2_C350::ClassMethod1331.MI.8042<System.Object>()#G1_C7::Method4.4811()#G2_C350::Method5.8037()#G1_C7::Method6.4813<System.Object>()#G1_C7::Method7.4815<System.Object>()#"
		call void Generated839.Generated839::M.G1_C7.B.A<class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C350::Method4.8036()#G2_C350::Method5.8037()#G2_C350::Method6.MI.8039<System.Object>()#"
		call void Generated839.Generated839::M.IBase1.T<class Generated839.BaseClass1,class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C350::Method4.8036()#G2_C350::Method5.8037()#G2_C350::Method6.MI.8039<System.Object>()#"
		call void Generated839.Generated839::M.IBase1.B<class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C350::Method7.MI.8035<System.Object>()#"
		call void Generated839.Generated839::M.IBase2.T.T<class Generated839.BaseClass1,class Generated839.BaseClass1,class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C350::Method7.MI.8035<System.Object>()#"
		call void Generated839.Generated839::M.IBase2.B.T<class Generated839.BaseClass1,class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C350::Method7.MI.8035<System.Object>()#"
		call void Generated839.Generated839::M.IBase2.B.B<class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C350::Method4.8036()#G2_C350::Method5.8037()#G2_C350::Method6.MI.8039<System.Object>()#"
		call void Generated839.Generated839::M.IBase1.T<class Generated839.BaseClass0,class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C350::Method4.8036()#G2_C350::Method5.8037()#G2_C350::Method6.MI.8039<System.Object>()#"
		call void Generated839.Generated839::M.IBase1.A<class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C350::Method7.MI.8035<System.Object>()#"
		call void Generated839.Generated839::M.IBase2.T.T<class Generated839.BaseClass0,class Generated839.BaseClass1,class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C350::Method7.MI.8035<System.Object>()#"
		call void Generated839.Generated839::M.IBase2.A.T<class Generated839.BaseClass1,class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C350::Method7.MI.8035<System.Object>()#"
		call void Generated839.Generated839::M.IBase2.A.B<class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C350::ClassMethod1329.MI.8040()#G2_C350::ClassMethod1330.MI.8041<System.Object>()#G2_C350::ClassMethod1331.MI.8042<System.Object>()#G2_C350::Method4.8036()#G2_C350::Method5.8037()#G2_C350::Method6.8038<System.Object>()#G2_C350::Method7.8034<System.Object>()#"
		call void Generated839.Generated839::M.G2_C350.T.T<class Generated839.BaseClass1,class Generated839.BaseClass1,class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C350::ClassMethod1329.MI.8040()#G2_C350::ClassMethod1330.MI.8041<System.Object>()#G2_C350::ClassMethod1331.MI.8042<System.Object>()#G2_C350::Method4.8036()#G2_C350::Method5.8037()#G2_C350::Method6.8038<System.Object>()#G2_C350::Method7.8034<System.Object>()#"
		call void Generated839.Generated839::M.G2_C350.B.T<class Generated839.BaseClass1,class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C350::ClassMethod1329.MI.8040()#G2_C350::ClassMethod1330.MI.8041<System.Object>()#G2_C350::ClassMethod1331.MI.8042<System.Object>()#G2_C350::Method4.8036()#G2_C350::Method5.8037()#G2_C350::Method6.8038<System.Object>()#G2_C350::Method7.8034<System.Object>()#"
		call void Generated839.Generated839::M.G2_C350.B.B<class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>>(!!0,string)
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
		newobj instance void class Generated839.G3_C1311`1<class Generated839.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::ClassMethod1331<object>()
		calli default string(class Generated839.G3_C1311`1<class Generated839.BaseClass0>)
		ldstr "G2_C350::ClassMethod1331.MI.8042<System.Object>()"
		ldstr "class G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0> on type class Generated839.G3_C1311`1<class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::ClassMethod1330<object>()
		calli default string(class Generated839.G3_C1311`1<class Generated839.BaseClass0>)
		ldstr "G2_C350::ClassMethod1330.MI.8041<System.Object>()"
		ldstr "class G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0> on type class Generated839.G3_C1311`1<class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::ClassMethod1329()
		calli default string(class Generated839.G3_C1311`1<class Generated839.BaseClass0>)
		ldstr "G2_C350::ClassMethod1329.MI.8040()"
		ldstr "class G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0> on type class Generated839.G3_C1311`1<class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::ClassMethod1328()
		calli default string(class Generated839.G3_C1311`1<class Generated839.BaseClass0>)
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0> on type class Generated839.G3_C1311`1<class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::Method7<object>()
		calli default string(class Generated839.G3_C1311`1<class Generated839.BaseClass0>)
		ldstr "G1_C7::Method7.4815<System.Object>()"
		ldstr "class G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0> on type class Generated839.G3_C1311`1<class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::Method6<object>()
		calli default string(class Generated839.G3_C1311`1<class Generated839.BaseClass0>)
		ldstr "G1_C7::Method6.4813<System.Object>()"
		ldstr "class G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0> on type class Generated839.G3_C1311`1<class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::Method5()
		calli default string(class Generated839.G3_C1311`1<class Generated839.BaseClass0>)
		ldstr "G2_C350::Method5.8037()"
		ldstr "class G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0> on type class Generated839.G3_C1311`1<class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::Method4()
		calli default string(class Generated839.G3_C1311`1<class Generated839.BaseClass0>)
		ldstr "G1_C7::Method4.4811()"
		ldstr "class G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0> on type class Generated839.G3_C1311`1<class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated839.IBase1`1<class Generated839.BaseClass1>::Method4()
		calli default string(class Generated839.G3_C1311`1<class Generated839.BaseClass0>)
		ldstr "G2_C350::Method4.8036()"
		ldstr "class IBase1`1<class Generated839.BaseClass1> on type class Generated839.G3_C1311`1<class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated839.IBase1`1<class Generated839.BaseClass1>::Method5()
		calli default string(class Generated839.G3_C1311`1<class Generated839.BaseClass0>)
		ldstr "G2_C350::Method5.8037()"
		ldstr "class IBase1`1<class Generated839.BaseClass1> on type class Generated839.G3_C1311`1<class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated839.IBase1`1<class Generated839.BaseClass1>::Method6<object>()
		calli default string(class Generated839.G3_C1311`1<class Generated839.BaseClass0>)
		ldstr "G2_C350::Method6.MI.8039<System.Object>()"
		ldstr "class IBase1`1<class Generated839.BaseClass1> on type class Generated839.G3_C1311`1<class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated839.IBase2`2<class Generated839.BaseClass1,class Generated839.BaseClass1>::Method7<object>()
		calli default string(class Generated839.G3_C1311`1<class Generated839.BaseClass0>)
		ldstr "G1_C7::Method7.MI.4816<System.Object>()"
		ldstr "class IBase2`2<class Generated839.BaseClass1,class Generated839.BaseClass1> on type class Generated839.G3_C1311`1<class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated839.IBase1`1<class Generated839.BaseClass0>::Method4()
		calli default string(class Generated839.G3_C1311`1<class Generated839.BaseClass0>)
		ldstr "G2_C350::Method4.8036()"
		ldstr "class IBase1`1<class Generated839.BaseClass0> on type class Generated839.G3_C1311`1<class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated839.IBase1`1<class Generated839.BaseClass0>::Method5()
		calli default string(class Generated839.G3_C1311`1<class Generated839.BaseClass0>)
		ldstr "G2_C350::Method5.8037()"
		ldstr "class IBase1`1<class Generated839.BaseClass0> on type class Generated839.G3_C1311`1<class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated839.IBase1`1<class Generated839.BaseClass0>::Method6<object>()
		calli default string(class Generated839.G3_C1311`1<class Generated839.BaseClass0>)
		ldstr "G2_C350::Method6.MI.8039<System.Object>()"
		ldstr "class IBase1`1<class Generated839.BaseClass0> on type class Generated839.G3_C1311`1<class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated839.IBase2`2<class Generated839.BaseClass0,class Generated839.BaseClass1>::Method7<object>()
		calli default string(class Generated839.G3_C1311`1<class Generated839.BaseClass0>)
		ldstr "G3_C1311::Method7.MI.15472<System.Object>()"
		ldstr "class IBase2`2<class Generated839.BaseClass0,class Generated839.BaseClass1> on type class Generated839.G3_C1311`1<class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::Method6<object>()
		calli default string(class Generated839.G3_C1311`1<class Generated839.BaseClass0>)
		ldstr "G2_C350::Method6.8038<System.Object>()"
		ldstr "class G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0> on type class Generated839.G3_C1311`1<class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::Method5()
		calli default string(class Generated839.G3_C1311`1<class Generated839.BaseClass0>)
		ldstr "G2_C350::Method5.8037()"
		ldstr "class G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0> on type class Generated839.G3_C1311`1<class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::Method4()
		calli default string(class Generated839.G3_C1311`1<class Generated839.BaseClass0>)
		ldstr "G2_C350::Method4.8036()"
		ldstr "class G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0> on type class Generated839.G3_C1311`1<class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::Method7<object>()
		calli default string(class Generated839.G3_C1311`1<class Generated839.BaseClass0>)
		ldstr "G3_C1311::Method7.15471<System.Object>()"
		ldstr "class G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0> on type class Generated839.G3_C1311`1<class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::ClassMethod1331<object>()
		calli default string(class Generated839.G3_C1311`1<class Generated839.BaseClass0>)
		ldstr "G2_C350::ClassMethod1331.MI.8042<System.Object>()"
		ldstr "class G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0> on type class Generated839.G3_C1311`1<class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::ClassMethod1330<object>()
		calli default string(class Generated839.G3_C1311`1<class Generated839.BaseClass0>)
		ldstr "G2_C350::ClassMethod1330.MI.8041<System.Object>()"
		ldstr "class G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0> on type class Generated839.G3_C1311`1<class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::ClassMethod1329()
		calli default string(class Generated839.G3_C1311`1<class Generated839.BaseClass0>)
		ldstr "G2_C350::ClassMethod1329.MI.8040()"
		ldstr "class G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0> on type class Generated839.G3_C1311`1<class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::ClassMethod1328()
		calli default string(class Generated839.G3_C1311`1<class Generated839.BaseClass0>)
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0> on type class Generated839.G3_C1311`1<class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated839.G3_C1311`1<class Generated839.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated839.G3_C1311`1<class Generated839.BaseClass0>::ClassMethod4003<object>()
		calli default string(class Generated839.G3_C1311`1<class Generated839.BaseClass0>)
		ldstr "G3_C1311::ClassMethod4003.15476<System.Object>()"
		ldstr "class G3_C1311`1<class Generated839.BaseClass0> on type class Generated839.G3_C1311`1<class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated839.G3_C1311`1<class Generated839.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated839.G3_C1311`1<class Generated839.BaseClass0>::ClassMethod4002<object>()
		calli default string(class Generated839.G3_C1311`1<class Generated839.BaseClass0>)
		ldstr "G3_C1311::ClassMethod4002.15475<System.Object>()"
		ldstr "class G3_C1311`1<class Generated839.BaseClass0> on type class Generated839.G3_C1311`1<class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated839.G3_C1311`1<class Generated839.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated839.G3_C1311`1<class Generated839.BaseClass0>::ClassMethod4001()
		calli default string(class Generated839.G3_C1311`1<class Generated839.BaseClass0>)
		ldstr "G3_C1311::ClassMethod4001.15474()"
		ldstr "class G3_C1311`1<class Generated839.BaseClass0> on type class Generated839.G3_C1311`1<class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated839.G3_C1311`1<class Generated839.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated839.G3_C1311`1<class Generated839.BaseClass0>::ClassMethod4000()
		calli default string(class Generated839.G3_C1311`1<class Generated839.BaseClass0>)
		ldstr "G3_C1311::ClassMethod4000.15473()"
		ldstr "class G3_C1311`1<class Generated839.BaseClass0> on type class Generated839.G3_C1311`1<class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated839.G3_C1311`1<class Generated839.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated839.G3_C1311`1<class Generated839.BaseClass0>::Method7<object>()
		calli default string(class Generated839.G3_C1311`1<class Generated839.BaseClass0>)
		ldstr "G3_C1311::Method7.15471<System.Object>()"
		ldstr "class G3_C1311`1<class Generated839.BaseClass0> on type class Generated839.G3_C1311`1<class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated839.G3_C1311`1<class Generated839.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated839.G3_C1311`1<class Generated839.BaseClass0>::Method6<object>()
		calli default string(class Generated839.G3_C1311`1<class Generated839.BaseClass0>)
		ldstr "G2_C350::Method6.8038<System.Object>()"
		ldstr "class G3_C1311`1<class Generated839.BaseClass0> on type class Generated839.G3_C1311`1<class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated839.G3_C1311`1<class Generated839.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated839.G3_C1311`1<class Generated839.BaseClass0>::Method5()
		calli default string(class Generated839.G3_C1311`1<class Generated839.BaseClass0>)
		ldstr "G2_C350::Method5.8037()"
		ldstr "class G3_C1311`1<class Generated839.BaseClass0> on type class Generated839.G3_C1311`1<class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated839.G3_C1311`1<class Generated839.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated839.G3_C1311`1<class Generated839.BaseClass0>::Method4()
		calli default string(class Generated839.G3_C1311`1<class Generated839.BaseClass0>)
		ldstr "G2_C350::Method4.8036()"
		ldstr "class G3_C1311`1<class Generated839.BaseClass0> on type class Generated839.G3_C1311`1<class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated839.G3_C1311`1<class Generated839.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated839.G3_C1311`1<class Generated839.BaseClass0>::ClassMethod1331<object>()
		calli default string(class Generated839.G3_C1311`1<class Generated839.BaseClass0>)
		ldstr "G2_C350::ClassMethod1331.MI.8042<System.Object>()"
		ldstr "class G3_C1311`1<class Generated839.BaseClass0> on type class Generated839.G3_C1311`1<class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated839.G3_C1311`1<class Generated839.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated839.G3_C1311`1<class Generated839.BaseClass0>::ClassMethod1330<object>()
		calli default string(class Generated839.G3_C1311`1<class Generated839.BaseClass0>)
		ldstr "G2_C350::ClassMethod1330.MI.8041<System.Object>()"
		ldstr "class G3_C1311`1<class Generated839.BaseClass0> on type class Generated839.G3_C1311`1<class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated839.G3_C1311`1<class Generated839.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated839.G3_C1311`1<class Generated839.BaseClass0>::ClassMethod1329()
		calli default string(class Generated839.G3_C1311`1<class Generated839.BaseClass0>)
		ldstr "G2_C350::ClassMethod1329.MI.8040()"
		ldstr "class G3_C1311`1<class Generated839.BaseClass0> on type class Generated839.G3_C1311`1<class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated839.G3_C1311`1<class Generated839.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated839.G3_C1311`1<class Generated839.BaseClass0>::ClassMethod1328()
		calli default string(class Generated839.G3_C1311`1<class Generated839.BaseClass0>)
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G3_C1311`1<class Generated839.BaseClass0> on type class Generated839.G3_C1311`1<class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated839.G3_C1311`1<class Generated839.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::ClassMethod1331<object>()
		calli default string(class Generated839.G3_C1311`1<class Generated839.BaseClass1>)
		ldstr "G2_C350::ClassMethod1331.MI.8042<System.Object>()"
		ldstr "class G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0> on type class Generated839.G3_C1311`1<class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::ClassMethod1330<object>()
		calli default string(class Generated839.G3_C1311`1<class Generated839.BaseClass1>)
		ldstr "G2_C350::ClassMethod1330.MI.8041<System.Object>()"
		ldstr "class G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0> on type class Generated839.G3_C1311`1<class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::ClassMethod1329()
		calli default string(class Generated839.G3_C1311`1<class Generated839.BaseClass1>)
		ldstr "G2_C350::ClassMethod1329.MI.8040()"
		ldstr "class G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0> on type class Generated839.G3_C1311`1<class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::ClassMethod1328()
		calli default string(class Generated839.G3_C1311`1<class Generated839.BaseClass1>)
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0> on type class Generated839.G3_C1311`1<class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::Method7<object>()
		calli default string(class Generated839.G3_C1311`1<class Generated839.BaseClass1>)
		ldstr "G1_C7::Method7.4815<System.Object>()"
		ldstr "class G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0> on type class Generated839.G3_C1311`1<class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::Method6<object>()
		calli default string(class Generated839.G3_C1311`1<class Generated839.BaseClass1>)
		ldstr "G1_C7::Method6.4813<System.Object>()"
		ldstr "class G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0> on type class Generated839.G3_C1311`1<class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::Method5()
		calli default string(class Generated839.G3_C1311`1<class Generated839.BaseClass1>)
		ldstr "G2_C350::Method5.8037()"
		ldstr "class G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0> on type class Generated839.G3_C1311`1<class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::Method4()
		calli default string(class Generated839.G3_C1311`1<class Generated839.BaseClass1>)
		ldstr "G1_C7::Method4.4811()"
		ldstr "class G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0> on type class Generated839.G3_C1311`1<class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated839.IBase1`1<class Generated839.BaseClass1>::Method4()
		calli default string(class Generated839.G3_C1311`1<class Generated839.BaseClass1>)
		ldstr "G2_C350::Method4.8036()"
		ldstr "class IBase1`1<class Generated839.BaseClass1> on type class Generated839.G3_C1311`1<class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated839.IBase1`1<class Generated839.BaseClass1>::Method5()
		calli default string(class Generated839.G3_C1311`1<class Generated839.BaseClass1>)
		ldstr "G2_C350::Method5.8037()"
		ldstr "class IBase1`1<class Generated839.BaseClass1> on type class Generated839.G3_C1311`1<class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated839.IBase1`1<class Generated839.BaseClass1>::Method6<object>()
		calli default string(class Generated839.G3_C1311`1<class Generated839.BaseClass1>)
		ldstr "G2_C350::Method6.MI.8039<System.Object>()"
		ldstr "class IBase1`1<class Generated839.BaseClass1> on type class Generated839.G3_C1311`1<class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated839.IBase2`2<class Generated839.BaseClass1,class Generated839.BaseClass1>::Method7<object>()
		calli default string(class Generated839.G3_C1311`1<class Generated839.BaseClass1>)
		ldstr "G2_C350::Method7.MI.8035<System.Object>()"
		ldstr "class IBase2`2<class Generated839.BaseClass1,class Generated839.BaseClass1> on type class Generated839.G3_C1311`1<class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated839.IBase1`1<class Generated839.BaseClass0>::Method4()
		calli default string(class Generated839.G3_C1311`1<class Generated839.BaseClass1>)
		ldstr "G2_C350::Method4.8036()"
		ldstr "class IBase1`1<class Generated839.BaseClass0> on type class Generated839.G3_C1311`1<class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated839.IBase1`1<class Generated839.BaseClass0>::Method5()
		calli default string(class Generated839.G3_C1311`1<class Generated839.BaseClass1>)
		ldstr "G2_C350::Method5.8037()"
		ldstr "class IBase1`1<class Generated839.BaseClass0> on type class Generated839.G3_C1311`1<class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated839.IBase1`1<class Generated839.BaseClass0>::Method6<object>()
		calli default string(class Generated839.G3_C1311`1<class Generated839.BaseClass1>)
		ldstr "G2_C350::Method6.MI.8039<System.Object>()"
		ldstr "class IBase1`1<class Generated839.BaseClass0> on type class Generated839.G3_C1311`1<class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated839.IBase2`2<class Generated839.BaseClass0,class Generated839.BaseClass1>::Method7<object>()
		calli default string(class Generated839.G3_C1311`1<class Generated839.BaseClass1>)
		ldstr "G3_C1311::Method7.MI.15472<System.Object>()"
		ldstr "class IBase2`2<class Generated839.BaseClass0,class Generated839.BaseClass1> on type class Generated839.G3_C1311`1<class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>::Method6<object>()
		calli default string(class Generated839.G3_C1311`1<class Generated839.BaseClass1>)
		ldstr "G2_C350::Method6.8038<System.Object>()"
		ldstr "class G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1> on type class Generated839.G3_C1311`1<class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>::Method5()
		calli default string(class Generated839.G3_C1311`1<class Generated839.BaseClass1>)
		ldstr "G2_C350::Method5.8037()"
		ldstr "class G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1> on type class Generated839.G3_C1311`1<class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>::Method4()
		calli default string(class Generated839.G3_C1311`1<class Generated839.BaseClass1>)
		ldstr "G2_C350::Method4.8036()"
		ldstr "class G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1> on type class Generated839.G3_C1311`1<class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>::Method7<object>()
		calli default string(class Generated839.G3_C1311`1<class Generated839.BaseClass1>)
		ldstr "G3_C1311::Method7.15471<System.Object>()"
		ldstr "class G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1> on type class Generated839.G3_C1311`1<class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>::ClassMethod1331<object>()
		calli default string(class Generated839.G3_C1311`1<class Generated839.BaseClass1>)
		ldstr "G2_C350::ClassMethod1331.MI.8042<System.Object>()"
		ldstr "class G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1> on type class Generated839.G3_C1311`1<class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>::ClassMethod1330<object>()
		calli default string(class Generated839.G3_C1311`1<class Generated839.BaseClass1>)
		ldstr "G2_C350::ClassMethod1330.MI.8041<System.Object>()"
		ldstr "class G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1> on type class Generated839.G3_C1311`1<class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>::ClassMethod1329()
		calli default string(class Generated839.G3_C1311`1<class Generated839.BaseClass1>)
		ldstr "G2_C350::ClassMethod1329.MI.8040()"
		ldstr "class G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1> on type class Generated839.G3_C1311`1<class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>::ClassMethod1328()
		calli default string(class Generated839.G3_C1311`1<class Generated839.BaseClass1>)
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1> on type class Generated839.G3_C1311`1<class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated839.G3_C1311`1<class Generated839.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated839.G3_C1311`1<class Generated839.BaseClass1>::ClassMethod4003<object>()
		calli default string(class Generated839.G3_C1311`1<class Generated839.BaseClass1>)
		ldstr "G3_C1311::ClassMethod4003.15476<System.Object>()"
		ldstr "class G3_C1311`1<class Generated839.BaseClass1> on type class Generated839.G3_C1311`1<class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated839.G3_C1311`1<class Generated839.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated839.G3_C1311`1<class Generated839.BaseClass1>::ClassMethod4002<object>()
		calli default string(class Generated839.G3_C1311`1<class Generated839.BaseClass1>)
		ldstr "G3_C1311::ClassMethod4002.15475<System.Object>()"
		ldstr "class G3_C1311`1<class Generated839.BaseClass1> on type class Generated839.G3_C1311`1<class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated839.G3_C1311`1<class Generated839.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated839.G3_C1311`1<class Generated839.BaseClass1>::ClassMethod4001()
		calli default string(class Generated839.G3_C1311`1<class Generated839.BaseClass1>)
		ldstr "G3_C1311::ClassMethod4001.15474()"
		ldstr "class G3_C1311`1<class Generated839.BaseClass1> on type class Generated839.G3_C1311`1<class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated839.G3_C1311`1<class Generated839.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated839.G3_C1311`1<class Generated839.BaseClass1>::ClassMethod4000()
		calli default string(class Generated839.G3_C1311`1<class Generated839.BaseClass1>)
		ldstr "G3_C1311::ClassMethod4000.15473()"
		ldstr "class G3_C1311`1<class Generated839.BaseClass1> on type class Generated839.G3_C1311`1<class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated839.G3_C1311`1<class Generated839.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated839.G3_C1311`1<class Generated839.BaseClass1>::Method7<object>()
		calli default string(class Generated839.G3_C1311`1<class Generated839.BaseClass1>)
		ldstr "G3_C1311::Method7.15471<System.Object>()"
		ldstr "class G3_C1311`1<class Generated839.BaseClass1> on type class Generated839.G3_C1311`1<class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated839.G3_C1311`1<class Generated839.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated839.G3_C1311`1<class Generated839.BaseClass1>::Method6<object>()
		calli default string(class Generated839.G3_C1311`1<class Generated839.BaseClass1>)
		ldstr "G2_C350::Method6.8038<System.Object>()"
		ldstr "class G3_C1311`1<class Generated839.BaseClass1> on type class Generated839.G3_C1311`1<class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated839.G3_C1311`1<class Generated839.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated839.G3_C1311`1<class Generated839.BaseClass1>::Method5()
		calli default string(class Generated839.G3_C1311`1<class Generated839.BaseClass1>)
		ldstr "G2_C350::Method5.8037()"
		ldstr "class G3_C1311`1<class Generated839.BaseClass1> on type class Generated839.G3_C1311`1<class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated839.G3_C1311`1<class Generated839.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated839.G3_C1311`1<class Generated839.BaseClass1>::Method4()
		calli default string(class Generated839.G3_C1311`1<class Generated839.BaseClass1>)
		ldstr "G2_C350::Method4.8036()"
		ldstr "class G3_C1311`1<class Generated839.BaseClass1> on type class Generated839.G3_C1311`1<class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated839.G3_C1311`1<class Generated839.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated839.G3_C1311`1<class Generated839.BaseClass1>::ClassMethod1331<object>()
		calli default string(class Generated839.G3_C1311`1<class Generated839.BaseClass1>)
		ldstr "G2_C350::ClassMethod1331.MI.8042<System.Object>()"
		ldstr "class G3_C1311`1<class Generated839.BaseClass1> on type class Generated839.G3_C1311`1<class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated839.G3_C1311`1<class Generated839.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated839.G3_C1311`1<class Generated839.BaseClass1>::ClassMethod1330<object>()
		calli default string(class Generated839.G3_C1311`1<class Generated839.BaseClass1>)
		ldstr "G2_C350::ClassMethod1330.MI.8041<System.Object>()"
		ldstr "class G3_C1311`1<class Generated839.BaseClass1> on type class Generated839.G3_C1311`1<class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated839.G3_C1311`1<class Generated839.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated839.G3_C1311`1<class Generated839.BaseClass1>::ClassMethod1329()
		calli default string(class Generated839.G3_C1311`1<class Generated839.BaseClass1>)
		ldstr "G2_C350::ClassMethod1329.MI.8040()"
		ldstr "class G3_C1311`1<class Generated839.BaseClass1> on type class Generated839.G3_C1311`1<class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated839.G3_C1311`1<class Generated839.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated839.G3_C1311`1<class Generated839.BaseClass1>::ClassMethod1328()
		calli default string(class Generated839.G3_C1311`1<class Generated839.BaseClass1>)
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G3_C1311`1<class Generated839.BaseClass1> on type class Generated839.G3_C1311`1<class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::ClassMethod1331<object>()
		calli default string(class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>)
		ldstr "G2_C350::ClassMethod1331.MI.8042<System.Object>()"
		ldstr "class G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0> on type class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::ClassMethod1330<object>()
		calli default string(class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>)
		ldstr "G2_C350::ClassMethod1330.MI.8041<System.Object>()"
		ldstr "class G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0> on type class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::ClassMethod1329()
		calli default string(class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>)
		ldstr "G2_C350::ClassMethod1329.MI.8040()"
		ldstr "class G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0> on type class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::ClassMethod1328()
		calli default string(class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>)
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0> on type class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::Method7<object>()
		calli default string(class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>)
		ldstr "G1_C7::Method7.4815<System.Object>()"
		ldstr "class G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0> on type class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::Method6<object>()
		calli default string(class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>)
		ldstr "G1_C7::Method6.4813<System.Object>()"
		ldstr "class G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0> on type class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::Method5()
		calli default string(class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>)
		ldstr "G2_C350::Method5.8037()"
		ldstr "class G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0> on type class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::Method4()
		calli default string(class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>)
		ldstr "G1_C7::Method4.4811()"
		ldstr "class G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0> on type class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated839.IBase1`1<class Generated839.BaseClass1>::Method4()
		calli default string(class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>)
		ldstr "G2_C350::Method4.8036()"
		ldstr "class IBase1`1<class Generated839.BaseClass1> on type class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated839.IBase1`1<class Generated839.BaseClass1>::Method5()
		calli default string(class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>)
		ldstr "G2_C350::Method5.8037()"
		ldstr "class IBase1`1<class Generated839.BaseClass1> on type class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated839.IBase1`1<class Generated839.BaseClass1>::Method6<object>()
		calli default string(class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>)
		ldstr "G2_C350::Method6.MI.8039<System.Object>()"
		ldstr "class IBase1`1<class Generated839.BaseClass1> on type class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated839.IBase2`2<class Generated839.BaseClass1,class Generated839.BaseClass1>::Method7<object>()
		calli default string(class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>)
		ldstr "G1_C7::Method7.MI.4816<System.Object>()"
		ldstr "class IBase2`2<class Generated839.BaseClass1,class Generated839.BaseClass1> on type class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated839.IBase1`1<class Generated839.BaseClass0>::Method4()
		calli default string(class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>)
		ldstr "G2_C350::Method4.8036()"
		ldstr "class IBase1`1<class Generated839.BaseClass0> on type class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated839.IBase1`1<class Generated839.BaseClass0>::Method5()
		calli default string(class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>)
		ldstr "G2_C350::Method5.8037()"
		ldstr "class IBase1`1<class Generated839.BaseClass0> on type class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated839.IBase1`1<class Generated839.BaseClass0>::Method6<object>()
		calli default string(class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>)
		ldstr "G2_C350::Method6.MI.8039<System.Object>()"
		ldstr "class IBase1`1<class Generated839.BaseClass0> on type class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated839.IBase2`2<class Generated839.BaseClass0,class Generated839.BaseClass1>::Method7<object>()
		calli default string(class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>)
		ldstr "G2_C350::Method7.MI.8035<System.Object>()"
		ldstr "class IBase2`2<class Generated839.BaseClass0,class Generated839.BaseClass1> on type class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>::Method6<object>()
		calli default string(class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>)
		ldstr "G2_C350::Method6.8038<System.Object>()"
		ldstr "class G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0> on type class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>::Method5()
		calli default string(class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>)
		ldstr "G2_C350::Method5.8037()"
		ldstr "class G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0> on type class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>::Method4()
		calli default string(class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>)
		ldstr "G2_C350::Method4.8036()"
		ldstr "class G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0> on type class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>::Method7<object>()
		calli default string(class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>)
		ldstr "G2_C350::Method7.8034<System.Object>()"
		ldstr "class G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0> on type class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>::ClassMethod1331<object>()
		calli default string(class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>)
		ldstr "G2_C350::ClassMethod1331.MI.8042<System.Object>()"
		ldstr "class G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0> on type class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>::ClassMethod1330<object>()
		calli default string(class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>)
		ldstr "G2_C350::ClassMethod1330.MI.8041<System.Object>()"
		ldstr "class G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0> on type class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>::ClassMethod1329()
		calli default string(class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>)
		ldstr "G2_C350::ClassMethod1329.MI.8040()"
		ldstr "class G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0> on type class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>::ClassMethod1328()
		calli default string(class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>)
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0> on type class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::ClassMethod1331<object>()
		calli default string(class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>)
		ldstr "G2_C350::ClassMethod1331.MI.8042<System.Object>()"
		ldstr "class G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0> on type class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::ClassMethod1330<object>()
		calli default string(class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>)
		ldstr "G2_C350::ClassMethod1330.MI.8041<System.Object>()"
		ldstr "class G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0> on type class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::ClassMethod1329()
		calli default string(class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>)
		ldstr "G2_C350::ClassMethod1329.MI.8040()"
		ldstr "class G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0> on type class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::ClassMethod1328()
		calli default string(class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>)
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0> on type class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::Method7<object>()
		calli default string(class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>)
		ldstr "G1_C7::Method7.4815<System.Object>()"
		ldstr "class G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0> on type class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::Method6<object>()
		calli default string(class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>)
		ldstr "G1_C7::Method6.4813<System.Object>()"
		ldstr "class G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0> on type class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::Method5()
		calli default string(class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>)
		ldstr "G2_C350::Method5.8037()"
		ldstr "class G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0> on type class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::Method4()
		calli default string(class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>)
		ldstr "G1_C7::Method4.4811()"
		ldstr "class G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0> on type class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated839.IBase1`1<class Generated839.BaseClass1>::Method4()
		calli default string(class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>)
		ldstr "G2_C350::Method4.8036()"
		ldstr "class IBase1`1<class Generated839.BaseClass1> on type class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated839.IBase1`1<class Generated839.BaseClass1>::Method5()
		calli default string(class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>)
		ldstr "G2_C350::Method5.8037()"
		ldstr "class IBase1`1<class Generated839.BaseClass1> on type class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated839.IBase1`1<class Generated839.BaseClass1>::Method6<object>()
		calli default string(class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>)
		ldstr "G2_C350::Method6.MI.8039<System.Object>()"
		ldstr "class IBase1`1<class Generated839.BaseClass1> on type class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated839.IBase2`2<class Generated839.BaseClass1,class Generated839.BaseClass1>::Method7<object>()
		calli default string(class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>)
		ldstr "G2_C350::Method7.MI.8035<System.Object>()"
		ldstr "class IBase2`2<class Generated839.BaseClass1,class Generated839.BaseClass1> on type class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated839.IBase1`1<class Generated839.BaseClass0>::Method4()
		calli default string(class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>)
		ldstr "G2_C350::Method4.8036()"
		ldstr "class IBase1`1<class Generated839.BaseClass0> on type class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated839.IBase1`1<class Generated839.BaseClass0>::Method5()
		calli default string(class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>)
		ldstr "G2_C350::Method5.8037()"
		ldstr "class IBase1`1<class Generated839.BaseClass0> on type class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated839.IBase1`1<class Generated839.BaseClass0>::Method6<object>()
		calli default string(class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>)
		ldstr "G2_C350::Method6.MI.8039<System.Object>()"
		ldstr "class IBase1`1<class Generated839.BaseClass0> on type class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated839.IBase2`2<class Generated839.BaseClass0,class Generated839.BaseClass1>::Method7<object>()
		calli default string(class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>)
		ldstr "G2_C350::Method7.MI.8035<System.Object>()"
		ldstr "class IBase2`2<class Generated839.BaseClass0,class Generated839.BaseClass1> on type class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>::Method6<object>()
		calli default string(class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>)
		ldstr "G2_C350::Method6.8038<System.Object>()"
		ldstr "class G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1> on type class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>::Method5()
		calli default string(class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>)
		ldstr "G2_C350::Method5.8037()"
		ldstr "class G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1> on type class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>::Method4()
		calli default string(class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>)
		ldstr "G2_C350::Method4.8036()"
		ldstr "class G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1> on type class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>::Method7<object>()
		calli default string(class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>)
		ldstr "G2_C350::Method7.8034<System.Object>()"
		ldstr "class G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1> on type class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>::ClassMethod1331<object>()
		calli default string(class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>)
		ldstr "G2_C350::ClassMethod1331.MI.8042<System.Object>()"
		ldstr "class G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1> on type class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>::ClassMethod1330<object>()
		calli default string(class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>)
		ldstr "G2_C350::ClassMethod1330.MI.8041<System.Object>()"
		ldstr "class G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1> on type class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>::ClassMethod1329()
		calli default string(class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>)
		ldstr "G2_C350::ClassMethod1329.MI.8040()"
		ldstr "class G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1> on type class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>::ClassMethod1328()
		calli default string(class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>)
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1> on type class Generated839.G2_C350`2<class Generated839.BaseClass0,class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::ClassMethod1331<object>()
		calli default string(class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>)
		ldstr "G2_C350::ClassMethod1331.MI.8042<System.Object>()"
		ldstr "class G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0> on type class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::ClassMethod1330<object>()
		calli default string(class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>)
		ldstr "G2_C350::ClassMethod1330.MI.8041<System.Object>()"
		ldstr "class G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0> on type class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::ClassMethod1329()
		calli default string(class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>)
		ldstr "G2_C350::ClassMethod1329.MI.8040()"
		ldstr "class G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0> on type class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::ClassMethod1328()
		calli default string(class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>)
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0> on type class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::Method7<object>()
		calli default string(class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>)
		ldstr "G1_C7::Method7.4815<System.Object>()"
		ldstr "class G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0> on type class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::Method6<object>()
		calli default string(class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>)
		ldstr "G1_C7::Method6.4813<System.Object>()"
		ldstr "class G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0> on type class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::Method5()
		calli default string(class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>)
		ldstr "G2_C350::Method5.8037()"
		ldstr "class G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0> on type class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::Method4()
		calli default string(class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>)
		ldstr "G1_C7::Method4.4811()"
		ldstr "class G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0> on type class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated839.IBase1`1<class Generated839.BaseClass1>::Method4()
		calli default string(class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>)
		ldstr "G2_C350::Method4.8036()"
		ldstr "class IBase1`1<class Generated839.BaseClass1> on type class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated839.IBase1`1<class Generated839.BaseClass1>::Method5()
		calli default string(class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>)
		ldstr "G2_C350::Method5.8037()"
		ldstr "class IBase1`1<class Generated839.BaseClass1> on type class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated839.IBase1`1<class Generated839.BaseClass1>::Method6<object>()
		calli default string(class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>)
		ldstr "G2_C350::Method6.MI.8039<System.Object>()"
		ldstr "class IBase1`1<class Generated839.BaseClass1> on type class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated839.IBase2`2<class Generated839.BaseClass1,class Generated839.BaseClass1>::Method7<object>()
		calli default string(class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>)
		ldstr "G1_C7::Method7.MI.4816<System.Object>()"
		ldstr "class IBase2`2<class Generated839.BaseClass1,class Generated839.BaseClass1> on type class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated839.IBase1`1<class Generated839.BaseClass0>::Method4()
		calli default string(class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>)
		ldstr "G2_C350::Method4.8036()"
		ldstr "class IBase1`1<class Generated839.BaseClass0> on type class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated839.IBase1`1<class Generated839.BaseClass0>::Method5()
		calli default string(class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>)
		ldstr "G2_C350::Method5.8037()"
		ldstr "class IBase1`1<class Generated839.BaseClass0> on type class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated839.IBase1`1<class Generated839.BaseClass0>::Method6<object>()
		calli default string(class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>)
		ldstr "G2_C350::Method6.MI.8039<System.Object>()"
		ldstr "class IBase1`1<class Generated839.BaseClass0> on type class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated839.IBase2`2<class Generated839.BaseClass0,class Generated839.BaseClass1>::Method7<object>()
		calli default string(class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>)
		ldstr "G2_C350::Method7.MI.8035<System.Object>()"
		ldstr "class IBase2`2<class Generated839.BaseClass0,class Generated839.BaseClass1> on type class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::Method6<object>()
		calli default string(class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>)
		ldstr "G2_C350::Method6.8038<System.Object>()"
		ldstr "class G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0> on type class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::Method5()
		calli default string(class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>)
		ldstr "G2_C350::Method5.8037()"
		ldstr "class G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0> on type class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::Method4()
		calli default string(class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>)
		ldstr "G2_C350::Method4.8036()"
		ldstr "class G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0> on type class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::Method7<object>()
		calli default string(class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>)
		ldstr "G2_C350::Method7.8034<System.Object>()"
		ldstr "class G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0> on type class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::ClassMethod1331<object>()
		calli default string(class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>)
		ldstr "G2_C350::ClassMethod1331.MI.8042<System.Object>()"
		ldstr "class G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0> on type class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::ClassMethod1330<object>()
		calli default string(class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>)
		ldstr "G2_C350::ClassMethod1330.MI.8041<System.Object>()"
		ldstr "class G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0> on type class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::ClassMethod1329()
		calli default string(class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>)
		ldstr "G2_C350::ClassMethod1329.MI.8040()"
		ldstr "class G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0> on type class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::ClassMethod1328()
		calli default string(class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>)
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0> on type class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::ClassMethod1331<object>()
		calli default string(class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>)
		ldstr "G2_C350::ClassMethod1331.MI.8042<System.Object>()"
		ldstr "class G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0> on type class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::ClassMethod1330<object>()
		calli default string(class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>)
		ldstr "G2_C350::ClassMethod1330.MI.8041<System.Object>()"
		ldstr "class G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0> on type class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::ClassMethod1329()
		calli default string(class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>)
		ldstr "G2_C350::ClassMethod1329.MI.8040()"
		ldstr "class G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0> on type class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::ClassMethod1328()
		calli default string(class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>)
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0> on type class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::Method7<object>()
		calli default string(class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>)
		ldstr "G1_C7::Method7.4815<System.Object>()"
		ldstr "class G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0> on type class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::Method6<object>()
		calli default string(class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>)
		ldstr "G1_C7::Method6.4813<System.Object>()"
		ldstr "class G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0> on type class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::Method5()
		calli default string(class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>)
		ldstr "G2_C350::Method5.8037()"
		ldstr "class G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0> on type class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated839.G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0>::Method4()
		calli default string(class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>)
		ldstr "G1_C7::Method4.4811()"
		ldstr "class G1_C7`2<class Generated839.BaseClass1,class Generated839.BaseClass0> on type class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated839.IBase1`1<class Generated839.BaseClass1>::Method4()
		calli default string(class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>)
		ldstr "G2_C350::Method4.8036()"
		ldstr "class IBase1`1<class Generated839.BaseClass1> on type class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated839.IBase1`1<class Generated839.BaseClass1>::Method5()
		calli default string(class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>)
		ldstr "G2_C350::Method5.8037()"
		ldstr "class IBase1`1<class Generated839.BaseClass1> on type class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated839.IBase1`1<class Generated839.BaseClass1>::Method6<object>()
		calli default string(class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>)
		ldstr "G2_C350::Method6.MI.8039<System.Object>()"
		ldstr "class IBase1`1<class Generated839.BaseClass1> on type class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated839.IBase2`2<class Generated839.BaseClass1,class Generated839.BaseClass1>::Method7<object>()
		calli default string(class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>)
		ldstr "G2_C350::Method7.MI.8035<System.Object>()"
		ldstr "class IBase2`2<class Generated839.BaseClass1,class Generated839.BaseClass1> on type class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated839.IBase1`1<class Generated839.BaseClass0>::Method4()
		calli default string(class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>)
		ldstr "G2_C350::Method4.8036()"
		ldstr "class IBase1`1<class Generated839.BaseClass0> on type class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated839.IBase1`1<class Generated839.BaseClass0>::Method5()
		calli default string(class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>)
		ldstr "G2_C350::Method5.8037()"
		ldstr "class IBase1`1<class Generated839.BaseClass0> on type class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated839.IBase1`1<class Generated839.BaseClass0>::Method6<object>()
		calli default string(class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>)
		ldstr "G2_C350::Method6.MI.8039<System.Object>()"
		ldstr "class IBase1`1<class Generated839.BaseClass0> on type class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated839.IBase2`2<class Generated839.BaseClass0,class Generated839.BaseClass1>::Method7<object>()
		calli default string(class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>)
		ldstr "G2_C350::Method7.MI.8035<System.Object>()"
		ldstr "class IBase2`2<class Generated839.BaseClass0,class Generated839.BaseClass1> on type class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>::Method6<object>()
		calli default string(class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>)
		ldstr "G2_C350::Method6.8038<System.Object>()"
		ldstr "class G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1> on type class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>::Method5()
		calli default string(class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>)
		ldstr "G2_C350::Method5.8037()"
		ldstr "class G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1> on type class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>::Method4()
		calli default string(class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>)
		ldstr "G2_C350::Method4.8036()"
		ldstr "class G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1> on type class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>::Method7<object>()
		calli default string(class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>)
		ldstr "G2_C350::Method7.8034<System.Object>()"
		ldstr "class G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1> on type class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>::ClassMethod1331<object>()
		calli default string(class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>)
		ldstr "G2_C350::ClassMethod1331.MI.8042<System.Object>()"
		ldstr "class G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1> on type class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>::ClassMethod1330<object>()
		calli default string(class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>)
		ldstr "G2_C350::ClassMethod1330.MI.8041<System.Object>()"
		ldstr "class G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1> on type class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>::ClassMethod1329()
		calli default string(class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>)
		ldstr "G2_C350::ClassMethod1329.MI.8040()"
		ldstr "class G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1> on type class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>::ClassMethod1328()
		calli default string(class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>)
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1> on type class Generated839.G2_C350`2<class Generated839.BaseClass1,class Generated839.BaseClass1>"
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
		call void Generated839.Generated839::MethodCallingTest()
		call void Generated839.Generated839::ConstrainedCallsTest()
		call void Generated839.Generated839::StructConstrainedInterfaceCallsTest()
		call void Generated839.Generated839::CalliTest()
		ldc.i4 100
		ret
	}
}
