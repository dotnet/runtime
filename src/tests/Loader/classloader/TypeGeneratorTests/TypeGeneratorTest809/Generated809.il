// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) .ver 4:0:0:0 }
.assembly extern TestFramework { .publickeytoken = ( B0 3F 5F 7F 11 D5 0A 3A )  }

//TYPES IN FORWARDER ASSEMBLIES:

//TEST ASSEMBLY:
.assembly extern xunit.core {}

.class public Generated809.BaseClass0 
{
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void [mscorlib]System.Object::.ctor()
		ret
	}
}
.class public Generated809.BaseClass1 
		extends Generated809.BaseClass0
{
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void Generated809.BaseClass0::.ctor()
		ret
	}
}
.class public Generated809.G3_C1281`1<T0> 
		extends class Generated809.G2_C327`2<class Generated809.BaseClass0,!T0>
		implements class Generated809.IBase1`1<!T0> 
{
	.method public hidebysig virtual instance string Method4() cil managed noinlining { 
		ldstr "G3_C1281::Method4.15314()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<T0>.Method4'() cil managed noinlining { 
		.override method instance string class Generated809.IBase1`1<!T0>::Method4()
		ldstr "G3_C1281::Method4.MI.15315()"
		ret
	}
	.method public hidebysig virtual instance string Method5() cil managed noinlining { 
		ldstr "G3_C1281::Method5.15316()"
		ret
	}
	.method public hidebysig newslot virtual instance string Method6<M0>() cil managed noinlining { 
		ldstr "G3_C1281::Method6.15317<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<T0>.Method6'<M0>() cil managed noinlining { 
		.override method instance string class Generated809.IBase1`1<!T0>::Method6<[1]>()
		ldstr "G3_C1281::Method6.MI.15318<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod3938() cil managed noinlining { 
		ldstr "G3_C1281::ClassMethod3938.15319()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod3939<M0>() cil managed noinlining { 
		ldstr "G3_C1281::ClassMethod3939.15320<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'G2_C327<class Generated809.BaseClass0,T0>.ClassMethod1993'() cil managed noinlining { 
		.override method instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,!T0>::ClassMethod1993()
		ldstr "G3_C1281::ClassMethod1993.MI.15321()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'G2_C327<class Generated809.BaseClass0,T0>.ClassMethod1994'() cil managed noinlining { 
		.override method instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,!T0>::ClassMethod1994()
		ldstr "G3_C1281::ClassMethod1994.MI.15322()"
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class Generated809.G2_C327`2<class Generated809.BaseClass0,!T0>::.ctor()
		ret
	}
}
.class public Generated809.G2_C327`2<T0, T1> 
		extends class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		implements class Generated809.IBase2`2<class Generated809.BaseClass1,class Generated809.BaseClass1>, class Generated809.IBase1`1<class Generated809.BaseClass0> 
{
	.method public hidebysig newslot virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G2_C327::Method7.7798<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string Method4() cil managed noinlining { 
		ldstr "G2_C327::Method4.7799()"
		ret
	}
	.method public hidebysig virtual instance string Method5() cil managed noinlining { 
		ldstr "G2_C327::Method5.7800()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<class Generated809.BaseClass0>.Method5'() cil managed noinlining { 
		.override method instance string class Generated809.IBase1`1<class Generated809.BaseClass0>::Method5()
		ldstr "G2_C327::Method5.MI.7801()"
		ret
	}
	.method public hidebysig virtual instance string Method6<M0>() cil managed noinlining { 
		ldstr "G2_C327::Method6.7802<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<class Generated809.BaseClass0>.Method6'<M0>() cil managed noinlining { 
		.override method instance string class Generated809.IBase1`1<class Generated809.BaseClass0>::Method6<[1]>()
		ldstr "G2_C327::Method6.MI.7803<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1993() cil managed noinlining { 
		ldstr "G2_C327::ClassMethod1993.7804()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1994() cil managed noinlining { 
		ldstr "G2_C327::ClassMethod1994.7805()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1995<M0>() cil managed noinlining { 
		ldstr "G2_C327::ClassMethod1995.7806<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1996<M0>() cil managed noinlining { 
		ldstr "G2_C327::ClassMethod1996.7807<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'G1_C7<class Generated809.BaseClass0,class Generated809.BaseClass0>.ClassMethod1331'<M0>() cil managed noinlining { 
		.override method instance string class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::ClassMethod1331<[1]>()
		ldstr "G2_C327::ClassMethod1331.MI.7808<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::.ctor()
		ret
	}
}
.class interface public abstract Generated809.IBase1`1<+T0> 
{
	.method public hidebysig newslot abstract virtual instance string Method4() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method5() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method6<M0>() cil managed { }
}
.class public abstract Generated809.G1_C7`2<T0, T1> 
		implements class Generated809.IBase1`1<!T0>, class Generated809.IBase2`2<class Generated809.BaseClass1,!T0> 
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
		.override method instance string class Generated809.IBase1`1<!T0>::Method6<[1]>()
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
	.method public hidebysig newslot virtual instance string 'IBase2<class Generated809.BaseClass1,T0>.Method7'<M0>() cil managed noinlining { 
		.override method instance string class Generated809.IBase2`2<class Generated809.BaseClass1,!T0>::Method7<[1]>()
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
.class interface public abstract Generated809.IBase2`2<+T0, -T1> 
{
	.method public hidebysig newslot abstract virtual instance string Method7<M0>() cil managed { }
}
.class public auto ansi beforefieldinit Generated809.Generated809 {
	.method static void M.BaseClass0<(Generated809.BaseClass0)W>(!!W inst, string exp) cil managed {
		.maxstack 5
		.locals init (string[] actualResults)
		ldc.i4.s    0
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.BaseClass0<(Generated809.BaseClass0)W>(!!W inst, string exp)"
		ldc.i4.s 0
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.BaseClass1<(Generated809.BaseClass1)W>(!!W inst, string exp) cil managed {
		.maxstack 5
		.locals init (string[] actualResults)
		ldc.i4.s    0
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.BaseClass1<(Generated809.BaseClass1)W>(!!W inst, string exp)"
		ldc.i4.s 0
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1281.T<T0,(class Generated809.G3_C1281`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 19
		.locals init (string[] actualResults)
		ldc.i4.s    14
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1281.T<T0,(class Generated809.G3_C1281`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 14
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G3_C1281`1<!!T0>::ClassMethod1328()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G3_C1281`1<!!T0>::ClassMethod1329()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G3_C1281`1<!!T0>::ClassMethod1330<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G3_C1281`1<!!T0>::ClassMethod1331<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G3_C1281`1<!!T0>::ClassMethod1993()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G3_C1281`1<!!T0>::ClassMethod1994()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G3_C1281`1<!!T0>::ClassMethod1995<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G3_C1281`1<!!T0>::ClassMethod1996<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G3_C1281`1<!!T0>::ClassMethod3938()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G3_C1281`1<!!T0>::ClassMethod3939<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G3_C1281`1<!!T0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G3_C1281`1<!!T0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G3_C1281`1<!!T0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     13
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G3_C1281`1<!!T0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1281.A<(class Generated809.G3_C1281`1<class Generated809.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 19
		.locals init (string[] actualResults)
		ldc.i4.s    14
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1281.A<(class Generated809.G3_C1281`1<class Generated809.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 14
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G3_C1281`1<class Generated809.BaseClass0>::ClassMethod1328()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G3_C1281`1<class Generated809.BaseClass0>::ClassMethod1329()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G3_C1281`1<class Generated809.BaseClass0>::ClassMethod1330<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G3_C1281`1<class Generated809.BaseClass0>::ClassMethod1331<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G3_C1281`1<class Generated809.BaseClass0>::ClassMethod1993()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G3_C1281`1<class Generated809.BaseClass0>::ClassMethod1994()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G3_C1281`1<class Generated809.BaseClass0>::ClassMethod1995<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G3_C1281`1<class Generated809.BaseClass0>::ClassMethod1996<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G3_C1281`1<class Generated809.BaseClass0>::ClassMethod3938()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G3_C1281`1<class Generated809.BaseClass0>::ClassMethod3939<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G3_C1281`1<class Generated809.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G3_C1281`1<class Generated809.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G3_C1281`1<class Generated809.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     13
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G3_C1281`1<class Generated809.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1281.B<(class Generated809.G3_C1281`1<class Generated809.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 19
		.locals init (string[] actualResults)
		ldc.i4.s    14
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1281.B<(class Generated809.G3_C1281`1<class Generated809.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 14
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G3_C1281`1<class Generated809.BaseClass1>::ClassMethod1328()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G3_C1281`1<class Generated809.BaseClass1>::ClassMethod1329()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G3_C1281`1<class Generated809.BaseClass1>::ClassMethod1330<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G3_C1281`1<class Generated809.BaseClass1>::ClassMethod1331<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G3_C1281`1<class Generated809.BaseClass1>::ClassMethod1993()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G3_C1281`1<class Generated809.BaseClass1>::ClassMethod1994()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G3_C1281`1<class Generated809.BaseClass1>::ClassMethod1995<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G3_C1281`1<class Generated809.BaseClass1>::ClassMethod1996<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G3_C1281`1<class Generated809.BaseClass1>::ClassMethod3938()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G3_C1281`1<class Generated809.BaseClass1>::ClassMethod3939<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G3_C1281`1<class Generated809.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G3_C1281`1<class Generated809.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G3_C1281`1<class Generated809.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     13
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G3_C1281`1<class Generated809.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C327.T.T<T0,T1,(class Generated809.G2_C327`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 17
		.locals init (string[] actualResults)
		ldc.i4.s    12
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C327.T.T<T0,T1,(class Generated809.G2_C327`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 12
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G2_C327`2<!!T0,!!T1>::ClassMethod1328()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G2_C327`2<!!T0,!!T1>::ClassMethod1329()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G2_C327`2<!!T0,!!T1>::ClassMethod1330<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G2_C327`2<!!T0,!!T1>::ClassMethod1331<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G2_C327`2<!!T0,!!T1>::ClassMethod1993()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G2_C327`2<!!T0,!!T1>::ClassMethod1994()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G2_C327`2<!!T0,!!T1>::ClassMethod1995<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G2_C327`2<!!T0,!!T1>::ClassMethod1996<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G2_C327`2<!!T0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G2_C327`2<!!T0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G2_C327`2<!!T0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G2_C327`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C327.A.T<T1,(class Generated809.G2_C327`2<class Generated809.BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 17
		.locals init (string[] actualResults)
		ldc.i4.s    12
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C327.A.T<T1,(class Generated809.G2_C327`2<class Generated809.BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 12
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,!!T1>::ClassMethod1328()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,!!T1>::ClassMethod1329()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,!!T1>::ClassMethod1330<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,!!T1>::ClassMethod1331<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,!!T1>::ClassMethod1993()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,!!T1>::ClassMethod1994()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,!!T1>::ClassMethod1995<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,!!T1>::ClassMethod1996<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C327.A.A<(class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 17
		.locals init (string[] actualResults)
		ldc.i4.s    12
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C327.A.A<(class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 12
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::ClassMethod1328()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::ClassMethod1329()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::ClassMethod1330<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::ClassMethod1331<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::ClassMethod1993()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::ClassMethod1994()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::ClassMethod1995<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::ClassMethod1996<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C327.A.B<(class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 17
		.locals init (string[] actualResults)
		ldc.i4.s    12
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C327.A.B<(class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 12
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>::ClassMethod1328()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>::ClassMethod1329()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>::ClassMethod1330<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>::ClassMethod1331<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>::ClassMethod1993()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>::ClassMethod1994()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>::ClassMethod1995<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>::ClassMethod1996<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C327.B.T<T1,(class Generated809.G2_C327`2<class Generated809.BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 17
		.locals init (string[] actualResults)
		ldc.i4.s    12
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C327.B.T<T1,(class Generated809.G2_C327`2<class Generated809.BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 12
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G2_C327`2<class Generated809.BaseClass1,!!T1>::ClassMethod1328()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G2_C327`2<class Generated809.BaseClass1,!!T1>::ClassMethod1329()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G2_C327`2<class Generated809.BaseClass1,!!T1>::ClassMethod1330<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G2_C327`2<class Generated809.BaseClass1,!!T1>::ClassMethod1331<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G2_C327`2<class Generated809.BaseClass1,!!T1>::ClassMethod1993()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G2_C327`2<class Generated809.BaseClass1,!!T1>::ClassMethod1994()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G2_C327`2<class Generated809.BaseClass1,!!T1>::ClassMethod1995<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G2_C327`2<class Generated809.BaseClass1,!!T1>::ClassMethod1996<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G2_C327`2<class Generated809.BaseClass1,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G2_C327`2<class Generated809.BaseClass1,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G2_C327`2<class Generated809.BaseClass1,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G2_C327`2<class Generated809.BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C327.B.A<(class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 17
		.locals init (string[] actualResults)
		ldc.i4.s    12
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C327.B.A<(class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 12
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>::ClassMethod1328()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>::ClassMethod1329()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>::ClassMethod1330<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>::ClassMethod1331<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>::ClassMethod1993()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>::ClassMethod1994()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>::ClassMethod1995<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>::ClassMethod1996<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C327.B.B<(class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 17
		.locals init (string[] actualResults)
		ldc.i4.s    12
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C327.B.B<(class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 12
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>::ClassMethod1328()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>::ClassMethod1329()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>::ClassMethod1330<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>::ClassMethod1331<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>::ClassMethod1993()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>::ClassMethod1994()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>::ClassMethod1995<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>::ClassMethod1996<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase1.T<T0,(class Generated809.IBase1`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase1.T<T0,(class Generated809.IBase1`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.IBase1`1<!!T0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.IBase1`1<!!T0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.IBase1`1<!!T0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase1.A<(class Generated809.IBase1`1<class Generated809.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase1.A<(class Generated809.IBase1`1<class Generated809.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.IBase1`1<class Generated809.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.IBase1`1<class Generated809.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.IBase1`1<class Generated809.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase1.B<(class Generated809.IBase1`1<class Generated809.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase1.B<(class Generated809.IBase1`1<class Generated809.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.IBase1`1<class Generated809.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.IBase1`1<class Generated809.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.IBase1`1<class Generated809.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C7.T.T<T0,T1,(class Generated809.G1_C7`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C7.T.T<T0,T1,(class Generated809.G1_C7`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G1_C7`2<!!T0,!!T1>::ClassMethod1328()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G1_C7`2<!!T0,!!T1>::ClassMethod1329()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G1_C7`2<!!T0,!!T1>::ClassMethod1330<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G1_C7`2<!!T0,!!T1>::ClassMethod1331<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G1_C7`2<!!T0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G1_C7`2<!!T0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G1_C7`2<!!T0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G1_C7`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C7.A.T<T1,(class Generated809.G1_C7`2<class Generated809.BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C7.A.T<T1,(class Generated809.G1_C7`2<class Generated809.BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G1_C7`2<class Generated809.BaseClass0,!!T1>::ClassMethod1328()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G1_C7`2<class Generated809.BaseClass0,!!T1>::ClassMethod1329()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G1_C7`2<class Generated809.BaseClass0,!!T1>::ClassMethod1330<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G1_C7`2<class Generated809.BaseClass0,!!T1>::ClassMethod1331<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G1_C7`2<class Generated809.BaseClass0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G1_C7`2<class Generated809.BaseClass0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G1_C7`2<class Generated809.BaseClass0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G1_C7`2<class Generated809.BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C7.A.A<(class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C7.A.A<(class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::ClassMethod1328()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::ClassMethod1329()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::ClassMethod1330<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::ClassMethod1331<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C7.A.B<(class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C7.A.B<(class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass1>::ClassMethod1328()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass1>::ClassMethod1329()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass1>::ClassMethod1330<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass1>::ClassMethod1331<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C7.B.T<T1,(class Generated809.G1_C7`2<class Generated809.BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C7.B.T<T1,(class Generated809.G1_C7`2<class Generated809.BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G1_C7`2<class Generated809.BaseClass1,!!T1>::ClassMethod1328()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G1_C7`2<class Generated809.BaseClass1,!!T1>::ClassMethod1329()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G1_C7`2<class Generated809.BaseClass1,!!T1>::ClassMethod1330<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G1_C7`2<class Generated809.BaseClass1,!!T1>::ClassMethod1331<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G1_C7`2<class Generated809.BaseClass1,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G1_C7`2<class Generated809.BaseClass1,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G1_C7`2<class Generated809.BaseClass1,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G1_C7`2<class Generated809.BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C7.B.A<(class Generated809.G1_C7`2<class Generated809.BaseClass1,class Generated809.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C7.B.A<(class Generated809.G1_C7`2<class Generated809.BaseClass1,class Generated809.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G1_C7`2<class Generated809.BaseClass1,class Generated809.BaseClass0>::ClassMethod1328()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G1_C7`2<class Generated809.BaseClass1,class Generated809.BaseClass0>::ClassMethod1329()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G1_C7`2<class Generated809.BaseClass1,class Generated809.BaseClass0>::ClassMethod1330<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G1_C7`2<class Generated809.BaseClass1,class Generated809.BaseClass0>::ClassMethod1331<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G1_C7`2<class Generated809.BaseClass1,class Generated809.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G1_C7`2<class Generated809.BaseClass1,class Generated809.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G1_C7`2<class Generated809.BaseClass1,class Generated809.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G1_C7`2<class Generated809.BaseClass1,class Generated809.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C7.B.B<(class Generated809.G1_C7`2<class Generated809.BaseClass1,class Generated809.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C7.B.B<(class Generated809.G1_C7`2<class Generated809.BaseClass1,class Generated809.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G1_C7`2<class Generated809.BaseClass1,class Generated809.BaseClass1>::ClassMethod1328()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G1_C7`2<class Generated809.BaseClass1,class Generated809.BaseClass1>::ClassMethod1329()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G1_C7`2<class Generated809.BaseClass1,class Generated809.BaseClass1>::ClassMethod1330<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G1_C7`2<class Generated809.BaseClass1,class Generated809.BaseClass1>::ClassMethod1331<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G1_C7`2<class Generated809.BaseClass1,class Generated809.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G1_C7`2<class Generated809.BaseClass1,class Generated809.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G1_C7`2<class Generated809.BaseClass1,class Generated809.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.G1_C7`2<class Generated809.BaseClass1,class Generated809.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.T.T<T0,T1,(class Generated809.IBase2`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.T.T<T0,T1,(class Generated809.IBase2`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.IBase2`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.A.T<T1,(class Generated809.IBase2`2<class Generated809.BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.A.T<T1,(class Generated809.IBase2`2<class Generated809.BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.IBase2`2<class Generated809.BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.A.A<(class Generated809.IBase2`2<class Generated809.BaseClass0,class Generated809.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.A.A<(class Generated809.IBase2`2<class Generated809.BaseClass0,class Generated809.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.IBase2`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.A.B<(class Generated809.IBase2`2<class Generated809.BaseClass0,class Generated809.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.A.B<(class Generated809.IBase2`2<class Generated809.BaseClass0,class Generated809.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.IBase2`2<class Generated809.BaseClass0,class Generated809.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.B.T<T1,(class Generated809.IBase2`2<class Generated809.BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.B.T<T1,(class Generated809.IBase2`2<class Generated809.BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.IBase2`2<class Generated809.BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.B.A<(class Generated809.IBase2`2<class Generated809.BaseClass1,class Generated809.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.B.A<(class Generated809.IBase2`2<class Generated809.BaseClass1,class Generated809.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.IBase2`2<class Generated809.BaseClass1,class Generated809.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.B.B<(class Generated809.IBase2`2<class Generated809.BaseClass1,class Generated809.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.B.B<(class Generated809.IBase2`2<class Generated809.BaseClass1,class Generated809.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated809.IBase2`2<class Generated809.BaseClass1,class Generated809.BaseClass1>::Method7<object>()
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
		newobj instance void class Generated809.G3_C1281`1<class Generated809.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		callvirt instance string class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::ClassMethod1331<object>()
		ldstr "G2_C327::ClassMethod1331.MI.7808<System.Object>()"
		ldstr "class G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G3_C1281`1<class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		callvirt instance string class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::ClassMethod1330<object>()
		ldstr "G1_C7::ClassMethod1330.4819<System.Object>()"
		ldstr "class G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G3_C1281`1<class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		callvirt instance string class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::ClassMethod1329()
		ldstr "G1_C7::ClassMethod1329.4818()"
		ldstr "class G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G3_C1281`1<class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		callvirt instance string class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::ClassMethod1328()
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G3_C1281`1<class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		callvirt instance string class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::Method7<object>()
		ldstr "G1_C7::Method7.4815<System.Object>()"
		ldstr "class G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G3_C1281`1<class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		callvirt instance string class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::Method6<object>()
		ldstr "G2_C327::Method6.7802<System.Object>()"
		ldstr "class G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G3_C1281`1<class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		callvirt instance string class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::Method5()
		ldstr "G3_C1281::Method5.15316()"
		ldstr "class G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G3_C1281`1<class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		callvirt instance string class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::Method4()
		ldstr "G1_C7::Method4.4811()"
		ldstr "class G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G3_C1281`1<class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated809.IBase1`1<class Generated809.BaseClass0>::Method4()
		ldstr "G3_C1281::Method4.MI.15315()"
		ldstr "class IBase1`1<class Generated809.BaseClass0> on type class Generated809.G3_C1281`1<class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated809.IBase1`1<class Generated809.BaseClass0>::Method5()
		ldstr "G3_C1281::Method5.15316()"
		ldstr "class IBase1`1<class Generated809.BaseClass0> on type class Generated809.G3_C1281`1<class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated809.IBase1`1<class Generated809.BaseClass0>::Method6<object>()
		ldstr "G3_C1281::Method6.MI.15318<System.Object>()"
		ldstr "class IBase1`1<class Generated809.BaseClass0> on type class Generated809.G3_C1281`1<class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated809.IBase2`2<class Generated809.BaseClass1,class Generated809.BaseClass0>::Method7<object>()
		ldstr "G1_C7::Method7.MI.4816<System.Object>()"
		ldstr "class IBase2`2<class Generated809.BaseClass1,class Generated809.BaseClass0> on type class Generated809.G3_C1281`1<class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated809.IBase2`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::Method7<object>()
		ldstr "G1_C7::Method7.MI.4816<System.Object>()"
		ldstr "class IBase2`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G3_C1281`1<class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated809.IBase2`2<class Generated809.BaseClass0,class Generated809.BaseClass1>::Method7<object>()
		ldstr "G2_C327::Method7.7798<System.Object>()"
		ldstr "class IBase2`2<class Generated809.BaseClass0,class Generated809.BaseClass1> on type class Generated809.G3_C1281`1<class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated809.IBase2`2<class Generated809.BaseClass1,class Generated809.BaseClass1>::Method7<object>()
		ldstr "G2_C327::Method7.7798<System.Object>()"
		ldstr "class IBase2`2<class Generated809.BaseClass1,class Generated809.BaseClass1> on type class Generated809.G3_C1281`1<class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		callvirt instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::ClassMethod1996<object>()
		ldstr "G2_C327::ClassMethod1996.7807<System.Object>()"
		ldstr "class G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G3_C1281`1<class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		callvirt instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::ClassMethod1995<object>()
		ldstr "G2_C327::ClassMethod1995.7806<System.Object>()"
		ldstr "class G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G3_C1281`1<class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		callvirt instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::ClassMethod1994()
		ldstr "G3_C1281::ClassMethod1994.MI.15322()"
		ldstr "class G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G3_C1281`1<class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		callvirt instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::ClassMethod1993()
		ldstr "G3_C1281::ClassMethod1993.MI.15321()"
		ldstr "class G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G3_C1281`1<class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		callvirt instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::Method6<object>()
		ldstr "G2_C327::Method6.7802<System.Object>()"
		ldstr "class G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G3_C1281`1<class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		callvirt instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::Method5()
		ldstr "G3_C1281::Method5.15316()"
		ldstr "class G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G3_C1281`1<class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		callvirt instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::Method4()
		ldstr "G3_C1281::Method4.15314()"
		ldstr "class G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G3_C1281`1<class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		callvirt instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::Method7<object>()
		ldstr "G2_C327::Method7.7798<System.Object>()"
		ldstr "class G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G3_C1281`1<class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		callvirt instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::ClassMethod1331<object>()
		ldstr "G2_C327::ClassMethod1331.MI.7808<System.Object>()"
		ldstr "class G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G3_C1281`1<class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		callvirt instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::ClassMethod1330<object>()
		ldstr "G1_C7::ClassMethod1330.4819<System.Object>()"
		ldstr "class G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G3_C1281`1<class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		callvirt instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::ClassMethod1329()
		ldstr "G1_C7::ClassMethod1329.4818()"
		ldstr "class G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G3_C1281`1<class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		callvirt instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::ClassMethod1328()
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G3_C1281`1<class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated809.G3_C1281`1<class Generated809.BaseClass0>
		callvirt instance string class Generated809.G3_C1281`1<class Generated809.BaseClass0>::ClassMethod3939<object>()
		ldstr "G3_C1281::ClassMethod3939.15320<System.Object>()"
		ldstr "class G3_C1281`1<class Generated809.BaseClass0> on type class Generated809.G3_C1281`1<class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G3_C1281`1<class Generated809.BaseClass0>
		callvirt instance string class Generated809.G3_C1281`1<class Generated809.BaseClass0>::ClassMethod3938()
		ldstr "G3_C1281::ClassMethod3938.15319()"
		ldstr "class G3_C1281`1<class Generated809.BaseClass0> on type class Generated809.G3_C1281`1<class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G3_C1281`1<class Generated809.BaseClass0>
		callvirt instance string class Generated809.G3_C1281`1<class Generated809.BaseClass0>::Method6<object>()
		ldstr "G3_C1281::Method6.15317<System.Object>()"
		ldstr "class G3_C1281`1<class Generated809.BaseClass0> on type class Generated809.G3_C1281`1<class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G3_C1281`1<class Generated809.BaseClass0>
		callvirt instance string class Generated809.G3_C1281`1<class Generated809.BaseClass0>::Method5()
		ldstr "G3_C1281::Method5.15316()"
		ldstr "class G3_C1281`1<class Generated809.BaseClass0> on type class Generated809.G3_C1281`1<class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G3_C1281`1<class Generated809.BaseClass0>
		callvirt instance string class Generated809.G3_C1281`1<class Generated809.BaseClass0>::Method4()
		ldstr "G3_C1281::Method4.15314()"
		ldstr "class G3_C1281`1<class Generated809.BaseClass0> on type class Generated809.G3_C1281`1<class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G3_C1281`1<class Generated809.BaseClass0>
		callvirt instance string class Generated809.G3_C1281`1<class Generated809.BaseClass0>::ClassMethod1996<object>()
		ldstr "G2_C327::ClassMethod1996.7807<System.Object>()"
		ldstr "class G3_C1281`1<class Generated809.BaseClass0> on type class Generated809.G3_C1281`1<class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G3_C1281`1<class Generated809.BaseClass0>
		callvirt instance string class Generated809.G3_C1281`1<class Generated809.BaseClass0>::ClassMethod1995<object>()
		ldstr "G2_C327::ClassMethod1995.7806<System.Object>()"
		ldstr "class G3_C1281`1<class Generated809.BaseClass0> on type class Generated809.G3_C1281`1<class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G3_C1281`1<class Generated809.BaseClass0>
		callvirt instance string class Generated809.G3_C1281`1<class Generated809.BaseClass0>::ClassMethod1994()
		ldstr "G3_C1281::ClassMethod1994.MI.15322()"
		ldstr "class G3_C1281`1<class Generated809.BaseClass0> on type class Generated809.G3_C1281`1<class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G3_C1281`1<class Generated809.BaseClass0>
		callvirt instance string class Generated809.G3_C1281`1<class Generated809.BaseClass0>::ClassMethod1993()
		ldstr "G3_C1281::ClassMethod1993.MI.15321()"
		ldstr "class G3_C1281`1<class Generated809.BaseClass0> on type class Generated809.G3_C1281`1<class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G3_C1281`1<class Generated809.BaseClass0>
		callvirt instance string class Generated809.G3_C1281`1<class Generated809.BaseClass0>::Method7<object>()
		ldstr "G2_C327::Method7.7798<System.Object>()"
		ldstr "class G3_C1281`1<class Generated809.BaseClass0> on type class Generated809.G3_C1281`1<class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G3_C1281`1<class Generated809.BaseClass0>
		callvirt instance string class Generated809.G3_C1281`1<class Generated809.BaseClass0>::ClassMethod1331<object>()
		ldstr "G2_C327::ClassMethod1331.MI.7808<System.Object>()"
		ldstr "class G3_C1281`1<class Generated809.BaseClass0> on type class Generated809.G3_C1281`1<class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G3_C1281`1<class Generated809.BaseClass0>
		callvirt instance string class Generated809.G3_C1281`1<class Generated809.BaseClass0>::ClassMethod1330<object>()
		ldstr "G1_C7::ClassMethod1330.4819<System.Object>()"
		ldstr "class G3_C1281`1<class Generated809.BaseClass0> on type class Generated809.G3_C1281`1<class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G3_C1281`1<class Generated809.BaseClass0>
		callvirt instance string class Generated809.G3_C1281`1<class Generated809.BaseClass0>::ClassMethod1329()
		ldstr "G1_C7::ClassMethod1329.4818()"
		ldstr "class G3_C1281`1<class Generated809.BaseClass0> on type class Generated809.G3_C1281`1<class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G3_C1281`1<class Generated809.BaseClass0>
		callvirt instance string class Generated809.G3_C1281`1<class Generated809.BaseClass0>::ClassMethod1328()
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G3_C1281`1<class Generated809.BaseClass0> on type class Generated809.G3_C1281`1<class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated809.G3_C1281`1<class Generated809.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		callvirt instance string class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::ClassMethod1331<object>()
		ldstr "G2_C327::ClassMethod1331.MI.7808<System.Object>()"
		ldstr "class G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G3_C1281`1<class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		callvirt instance string class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::ClassMethod1330<object>()
		ldstr "G1_C7::ClassMethod1330.4819<System.Object>()"
		ldstr "class G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G3_C1281`1<class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		callvirt instance string class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::ClassMethod1329()
		ldstr "G1_C7::ClassMethod1329.4818()"
		ldstr "class G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G3_C1281`1<class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		callvirt instance string class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::ClassMethod1328()
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G3_C1281`1<class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		callvirt instance string class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::Method7<object>()
		ldstr "G1_C7::Method7.4815<System.Object>()"
		ldstr "class G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G3_C1281`1<class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		callvirt instance string class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::Method6<object>()
		ldstr "G2_C327::Method6.7802<System.Object>()"
		ldstr "class G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G3_C1281`1<class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		callvirt instance string class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::Method5()
		ldstr "G3_C1281::Method5.15316()"
		ldstr "class G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G3_C1281`1<class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		callvirt instance string class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::Method4()
		ldstr "G1_C7::Method4.4811()"
		ldstr "class G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G3_C1281`1<class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated809.IBase1`1<class Generated809.BaseClass0>::Method4()
		ldstr "G3_C1281::Method4.MI.15315()"
		ldstr "class IBase1`1<class Generated809.BaseClass0> on type class Generated809.G3_C1281`1<class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated809.IBase1`1<class Generated809.BaseClass0>::Method5()
		ldstr "G3_C1281::Method5.15316()"
		ldstr "class IBase1`1<class Generated809.BaseClass0> on type class Generated809.G3_C1281`1<class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated809.IBase1`1<class Generated809.BaseClass0>::Method6<object>()
		ldstr "G3_C1281::Method6.MI.15318<System.Object>()"
		ldstr "class IBase1`1<class Generated809.BaseClass0> on type class Generated809.G3_C1281`1<class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated809.IBase2`2<class Generated809.BaseClass1,class Generated809.BaseClass0>::Method7<object>()
		ldstr "G1_C7::Method7.MI.4816<System.Object>()"
		ldstr "class IBase2`2<class Generated809.BaseClass1,class Generated809.BaseClass0> on type class Generated809.G3_C1281`1<class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated809.IBase2`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::Method7<object>()
		ldstr "G1_C7::Method7.MI.4816<System.Object>()"
		ldstr "class IBase2`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G3_C1281`1<class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated809.IBase2`2<class Generated809.BaseClass0,class Generated809.BaseClass1>::Method7<object>()
		ldstr "G2_C327::Method7.7798<System.Object>()"
		ldstr "class IBase2`2<class Generated809.BaseClass0,class Generated809.BaseClass1> on type class Generated809.G3_C1281`1<class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated809.IBase2`2<class Generated809.BaseClass1,class Generated809.BaseClass1>::Method7<object>()
		ldstr "G2_C327::Method7.7798<System.Object>()"
		ldstr "class IBase2`2<class Generated809.BaseClass1,class Generated809.BaseClass1> on type class Generated809.G3_C1281`1<class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>
		callvirt instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>::ClassMethod1996<object>()
		ldstr "G2_C327::ClassMethod1996.7807<System.Object>()"
		ldstr "class G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1> on type class Generated809.G3_C1281`1<class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>
		callvirt instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>::ClassMethod1995<object>()
		ldstr "G2_C327::ClassMethod1995.7806<System.Object>()"
		ldstr "class G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1> on type class Generated809.G3_C1281`1<class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>
		callvirt instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>::ClassMethod1994()
		ldstr "G3_C1281::ClassMethod1994.MI.15322()"
		ldstr "class G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1> on type class Generated809.G3_C1281`1<class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>
		callvirt instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>::ClassMethod1993()
		ldstr "G3_C1281::ClassMethod1993.MI.15321()"
		ldstr "class G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1> on type class Generated809.G3_C1281`1<class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>
		callvirt instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>::Method6<object>()
		ldstr "G2_C327::Method6.7802<System.Object>()"
		ldstr "class G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1> on type class Generated809.G3_C1281`1<class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>
		callvirt instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>::Method5()
		ldstr "G3_C1281::Method5.15316()"
		ldstr "class G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1> on type class Generated809.G3_C1281`1<class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>
		callvirt instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>::Method4()
		ldstr "G3_C1281::Method4.15314()"
		ldstr "class G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1> on type class Generated809.G3_C1281`1<class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>
		callvirt instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>::Method7<object>()
		ldstr "G2_C327::Method7.7798<System.Object>()"
		ldstr "class G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1> on type class Generated809.G3_C1281`1<class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>
		callvirt instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>::ClassMethod1331<object>()
		ldstr "G2_C327::ClassMethod1331.MI.7808<System.Object>()"
		ldstr "class G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1> on type class Generated809.G3_C1281`1<class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>
		callvirt instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>::ClassMethod1330<object>()
		ldstr "G1_C7::ClassMethod1330.4819<System.Object>()"
		ldstr "class G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1> on type class Generated809.G3_C1281`1<class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>
		callvirt instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>::ClassMethod1329()
		ldstr "G1_C7::ClassMethod1329.4818()"
		ldstr "class G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1> on type class Generated809.G3_C1281`1<class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>
		callvirt instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>::ClassMethod1328()
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1> on type class Generated809.G3_C1281`1<class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated809.G3_C1281`1<class Generated809.BaseClass1>
		callvirt instance string class Generated809.G3_C1281`1<class Generated809.BaseClass1>::ClassMethod3939<object>()
		ldstr "G3_C1281::ClassMethod3939.15320<System.Object>()"
		ldstr "class G3_C1281`1<class Generated809.BaseClass1> on type class Generated809.G3_C1281`1<class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G3_C1281`1<class Generated809.BaseClass1>
		callvirt instance string class Generated809.G3_C1281`1<class Generated809.BaseClass1>::ClassMethod3938()
		ldstr "G3_C1281::ClassMethod3938.15319()"
		ldstr "class G3_C1281`1<class Generated809.BaseClass1> on type class Generated809.G3_C1281`1<class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G3_C1281`1<class Generated809.BaseClass1>
		callvirt instance string class Generated809.G3_C1281`1<class Generated809.BaseClass1>::Method6<object>()
		ldstr "G3_C1281::Method6.15317<System.Object>()"
		ldstr "class G3_C1281`1<class Generated809.BaseClass1> on type class Generated809.G3_C1281`1<class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G3_C1281`1<class Generated809.BaseClass1>
		callvirt instance string class Generated809.G3_C1281`1<class Generated809.BaseClass1>::Method5()
		ldstr "G3_C1281::Method5.15316()"
		ldstr "class G3_C1281`1<class Generated809.BaseClass1> on type class Generated809.G3_C1281`1<class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G3_C1281`1<class Generated809.BaseClass1>
		callvirt instance string class Generated809.G3_C1281`1<class Generated809.BaseClass1>::Method4()
		ldstr "G3_C1281::Method4.15314()"
		ldstr "class G3_C1281`1<class Generated809.BaseClass1> on type class Generated809.G3_C1281`1<class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G3_C1281`1<class Generated809.BaseClass1>
		callvirt instance string class Generated809.G3_C1281`1<class Generated809.BaseClass1>::ClassMethod1996<object>()
		ldstr "G2_C327::ClassMethod1996.7807<System.Object>()"
		ldstr "class G3_C1281`1<class Generated809.BaseClass1> on type class Generated809.G3_C1281`1<class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G3_C1281`1<class Generated809.BaseClass1>
		callvirt instance string class Generated809.G3_C1281`1<class Generated809.BaseClass1>::ClassMethod1995<object>()
		ldstr "G2_C327::ClassMethod1995.7806<System.Object>()"
		ldstr "class G3_C1281`1<class Generated809.BaseClass1> on type class Generated809.G3_C1281`1<class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G3_C1281`1<class Generated809.BaseClass1>
		callvirt instance string class Generated809.G3_C1281`1<class Generated809.BaseClass1>::ClassMethod1994()
		ldstr "G3_C1281::ClassMethod1994.MI.15322()"
		ldstr "class G3_C1281`1<class Generated809.BaseClass1> on type class Generated809.G3_C1281`1<class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G3_C1281`1<class Generated809.BaseClass1>
		callvirt instance string class Generated809.G3_C1281`1<class Generated809.BaseClass1>::ClassMethod1993()
		ldstr "G3_C1281::ClassMethod1993.MI.15321()"
		ldstr "class G3_C1281`1<class Generated809.BaseClass1> on type class Generated809.G3_C1281`1<class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G3_C1281`1<class Generated809.BaseClass1>
		callvirt instance string class Generated809.G3_C1281`1<class Generated809.BaseClass1>::Method7<object>()
		ldstr "G2_C327::Method7.7798<System.Object>()"
		ldstr "class G3_C1281`1<class Generated809.BaseClass1> on type class Generated809.G3_C1281`1<class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G3_C1281`1<class Generated809.BaseClass1>
		callvirt instance string class Generated809.G3_C1281`1<class Generated809.BaseClass1>::ClassMethod1331<object>()
		ldstr "G2_C327::ClassMethod1331.MI.7808<System.Object>()"
		ldstr "class G3_C1281`1<class Generated809.BaseClass1> on type class Generated809.G3_C1281`1<class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G3_C1281`1<class Generated809.BaseClass1>
		callvirt instance string class Generated809.G3_C1281`1<class Generated809.BaseClass1>::ClassMethod1330<object>()
		ldstr "G1_C7::ClassMethod1330.4819<System.Object>()"
		ldstr "class G3_C1281`1<class Generated809.BaseClass1> on type class Generated809.G3_C1281`1<class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G3_C1281`1<class Generated809.BaseClass1>
		callvirt instance string class Generated809.G3_C1281`1<class Generated809.BaseClass1>::ClassMethod1329()
		ldstr "G1_C7::ClassMethod1329.4818()"
		ldstr "class G3_C1281`1<class Generated809.BaseClass1> on type class Generated809.G3_C1281`1<class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G3_C1281`1<class Generated809.BaseClass1>
		callvirt instance string class Generated809.G3_C1281`1<class Generated809.BaseClass1>::ClassMethod1328()
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G3_C1281`1<class Generated809.BaseClass1> on type class Generated809.G3_C1281`1<class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated809.IBase1`1<class Generated809.BaseClass1>::Method4()
		ldstr "G3_C1281::Method4.MI.15315()"
		ldstr "class IBase1`1<class Generated809.BaseClass1> on type class Generated809.G3_C1281`1<class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated809.IBase1`1<class Generated809.BaseClass1>::Method5()
		ldstr "G3_C1281::Method5.15316()"
		ldstr "class IBase1`1<class Generated809.BaseClass1> on type class Generated809.G3_C1281`1<class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated809.IBase1`1<class Generated809.BaseClass1>::Method6<object>()
		ldstr "G3_C1281::Method6.MI.15318<System.Object>()"
		ldstr "class IBase1`1<class Generated809.BaseClass1> on type class Generated809.G3_C1281`1<class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		callvirt instance string class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::ClassMethod1331<object>()
		ldstr "G2_C327::ClassMethod1331.MI.7808<System.Object>()"
		ldstr "class G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		callvirt instance string class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::ClassMethod1330<object>()
		ldstr "G1_C7::ClassMethod1330.4819<System.Object>()"
		ldstr "class G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		callvirt instance string class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::ClassMethod1329()
		ldstr "G1_C7::ClassMethod1329.4818()"
		ldstr "class G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		callvirt instance string class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::ClassMethod1328()
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		callvirt instance string class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::Method7<object>()
		ldstr "G1_C7::Method7.4815<System.Object>()"
		ldstr "class G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		callvirt instance string class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::Method6<object>()
		ldstr "G2_C327::Method6.7802<System.Object>()"
		ldstr "class G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		callvirt instance string class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::Method5()
		ldstr "G2_C327::Method5.7800()"
		ldstr "class G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		callvirt instance string class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::Method4()
		ldstr "G1_C7::Method4.4811()"
		ldstr "class G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated809.IBase1`1<class Generated809.BaseClass0>::Method4()
		ldstr "G2_C327::Method4.7799()"
		ldstr "class IBase1`1<class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated809.IBase1`1<class Generated809.BaseClass0>::Method5()
		ldstr "G2_C327::Method5.MI.7801()"
		ldstr "class IBase1`1<class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated809.IBase1`1<class Generated809.BaseClass0>::Method6<object>()
		ldstr "G2_C327::Method6.MI.7803<System.Object>()"
		ldstr "class IBase1`1<class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated809.IBase2`2<class Generated809.BaseClass1,class Generated809.BaseClass0>::Method7<object>()
		ldstr "G1_C7::Method7.MI.4816<System.Object>()"
		ldstr "class IBase2`2<class Generated809.BaseClass1,class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated809.IBase2`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::Method7<object>()
		ldstr "G1_C7::Method7.MI.4816<System.Object>()"
		ldstr "class IBase2`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated809.IBase2`2<class Generated809.BaseClass0,class Generated809.BaseClass1>::Method7<object>()
		ldstr "G2_C327::Method7.7798<System.Object>()"
		ldstr "class IBase2`2<class Generated809.BaseClass0,class Generated809.BaseClass1> on type class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated809.IBase2`2<class Generated809.BaseClass1,class Generated809.BaseClass1>::Method7<object>()
		ldstr "G2_C327::Method7.7798<System.Object>()"
		ldstr "class IBase2`2<class Generated809.BaseClass1,class Generated809.BaseClass1> on type class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		callvirt instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::ClassMethod1996<object>()
		ldstr "G2_C327::ClassMethod1996.7807<System.Object>()"
		ldstr "class G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		callvirt instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::ClassMethod1995<object>()
		ldstr "G2_C327::ClassMethod1995.7806<System.Object>()"
		ldstr "class G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		callvirt instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::ClassMethod1994()
		ldstr "G2_C327::ClassMethod1994.7805()"
		ldstr "class G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		callvirt instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::ClassMethod1993()
		ldstr "G2_C327::ClassMethod1993.7804()"
		ldstr "class G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		callvirt instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::Method6<object>()
		ldstr "G2_C327::Method6.7802<System.Object>()"
		ldstr "class G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		callvirt instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::Method5()
		ldstr "G2_C327::Method5.7800()"
		ldstr "class G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		callvirt instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::Method4()
		ldstr "G2_C327::Method4.7799()"
		ldstr "class G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		callvirt instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::Method7<object>()
		ldstr "G2_C327::Method7.7798<System.Object>()"
		ldstr "class G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		callvirt instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::ClassMethod1331<object>()
		ldstr "G2_C327::ClassMethod1331.MI.7808<System.Object>()"
		ldstr "class G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		callvirt instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::ClassMethod1330<object>()
		ldstr "G1_C7::ClassMethod1330.4819<System.Object>()"
		ldstr "class G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		callvirt instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::ClassMethod1329()
		ldstr "G1_C7::ClassMethod1329.4818()"
		ldstr "class G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		callvirt instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::ClassMethod1328()
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		callvirt instance string class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::ClassMethod1331<object>()
		ldstr "G2_C327::ClassMethod1331.MI.7808<System.Object>()"
		ldstr "class G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		callvirt instance string class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::ClassMethod1330<object>()
		ldstr "G1_C7::ClassMethod1330.4819<System.Object>()"
		ldstr "class G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		callvirt instance string class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::ClassMethod1329()
		ldstr "G1_C7::ClassMethod1329.4818()"
		ldstr "class G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		callvirt instance string class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::ClassMethod1328()
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		callvirt instance string class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::Method7<object>()
		ldstr "G1_C7::Method7.4815<System.Object>()"
		ldstr "class G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		callvirt instance string class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::Method6<object>()
		ldstr "G2_C327::Method6.7802<System.Object>()"
		ldstr "class G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		callvirt instance string class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::Method5()
		ldstr "G2_C327::Method5.7800()"
		ldstr "class G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		callvirt instance string class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::Method4()
		ldstr "G1_C7::Method4.4811()"
		ldstr "class G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated809.IBase1`1<class Generated809.BaseClass0>::Method4()
		ldstr "G2_C327::Method4.7799()"
		ldstr "class IBase1`1<class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated809.IBase1`1<class Generated809.BaseClass0>::Method5()
		ldstr "G2_C327::Method5.MI.7801()"
		ldstr "class IBase1`1<class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated809.IBase1`1<class Generated809.BaseClass0>::Method6<object>()
		ldstr "G2_C327::Method6.MI.7803<System.Object>()"
		ldstr "class IBase1`1<class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated809.IBase2`2<class Generated809.BaseClass1,class Generated809.BaseClass0>::Method7<object>()
		ldstr "G1_C7::Method7.MI.4816<System.Object>()"
		ldstr "class IBase2`2<class Generated809.BaseClass1,class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated809.IBase2`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::Method7<object>()
		ldstr "G1_C7::Method7.MI.4816<System.Object>()"
		ldstr "class IBase2`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated809.IBase2`2<class Generated809.BaseClass0,class Generated809.BaseClass1>::Method7<object>()
		ldstr "G2_C327::Method7.7798<System.Object>()"
		ldstr "class IBase2`2<class Generated809.BaseClass0,class Generated809.BaseClass1> on type class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated809.IBase2`2<class Generated809.BaseClass1,class Generated809.BaseClass1>::Method7<object>()
		ldstr "G2_C327::Method7.7798<System.Object>()"
		ldstr "class IBase2`2<class Generated809.BaseClass1,class Generated809.BaseClass1> on type class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>
		callvirt instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>::ClassMethod1996<object>()
		ldstr "G2_C327::ClassMethod1996.7807<System.Object>()"
		ldstr "class G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1> on type class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>
		callvirt instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>::ClassMethod1995<object>()
		ldstr "G2_C327::ClassMethod1995.7806<System.Object>()"
		ldstr "class G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1> on type class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>
		callvirt instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>::ClassMethod1994()
		ldstr "G2_C327::ClassMethod1994.7805()"
		ldstr "class G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1> on type class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>
		callvirt instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>::ClassMethod1993()
		ldstr "G2_C327::ClassMethod1993.7804()"
		ldstr "class G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1> on type class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>
		callvirt instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>::Method6<object>()
		ldstr "G2_C327::Method6.7802<System.Object>()"
		ldstr "class G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1> on type class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>
		callvirt instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>::Method5()
		ldstr "G2_C327::Method5.7800()"
		ldstr "class G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1> on type class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>
		callvirt instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>::Method4()
		ldstr "G2_C327::Method4.7799()"
		ldstr "class G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1> on type class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>
		callvirt instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>::Method7<object>()
		ldstr "G2_C327::Method7.7798<System.Object>()"
		ldstr "class G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1> on type class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>
		callvirt instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>::ClassMethod1331<object>()
		ldstr "G2_C327::ClassMethod1331.MI.7808<System.Object>()"
		ldstr "class G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1> on type class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>
		callvirt instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>::ClassMethod1330<object>()
		ldstr "G1_C7::ClassMethod1330.4819<System.Object>()"
		ldstr "class G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1> on type class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>
		callvirt instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>::ClassMethod1329()
		ldstr "G1_C7::ClassMethod1329.4818()"
		ldstr "class G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1> on type class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>
		callvirt instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>::ClassMethod1328()
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1> on type class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		callvirt instance string class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::ClassMethod1331<object>()
		ldstr "G2_C327::ClassMethod1331.MI.7808<System.Object>()"
		ldstr "class G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		callvirt instance string class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::ClassMethod1330<object>()
		ldstr "G1_C7::ClassMethod1330.4819<System.Object>()"
		ldstr "class G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		callvirt instance string class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::ClassMethod1329()
		ldstr "G1_C7::ClassMethod1329.4818()"
		ldstr "class G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		callvirt instance string class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::ClassMethod1328()
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		callvirt instance string class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::Method7<object>()
		ldstr "G1_C7::Method7.4815<System.Object>()"
		ldstr "class G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		callvirt instance string class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::Method6<object>()
		ldstr "G2_C327::Method6.7802<System.Object>()"
		ldstr "class G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		callvirt instance string class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::Method5()
		ldstr "G2_C327::Method5.7800()"
		ldstr "class G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		callvirt instance string class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::Method4()
		ldstr "G1_C7::Method4.4811()"
		ldstr "class G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated809.IBase1`1<class Generated809.BaseClass0>::Method4()
		ldstr "G2_C327::Method4.7799()"
		ldstr "class IBase1`1<class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated809.IBase1`1<class Generated809.BaseClass0>::Method5()
		ldstr "G2_C327::Method5.MI.7801()"
		ldstr "class IBase1`1<class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated809.IBase1`1<class Generated809.BaseClass0>::Method6<object>()
		ldstr "G2_C327::Method6.MI.7803<System.Object>()"
		ldstr "class IBase1`1<class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated809.IBase2`2<class Generated809.BaseClass1,class Generated809.BaseClass0>::Method7<object>()
		ldstr "G1_C7::Method7.MI.4816<System.Object>()"
		ldstr "class IBase2`2<class Generated809.BaseClass1,class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated809.IBase2`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::Method7<object>()
		ldstr "G1_C7::Method7.MI.4816<System.Object>()"
		ldstr "class IBase2`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated809.IBase2`2<class Generated809.BaseClass0,class Generated809.BaseClass1>::Method7<object>()
		ldstr "G2_C327::Method7.7798<System.Object>()"
		ldstr "class IBase2`2<class Generated809.BaseClass0,class Generated809.BaseClass1> on type class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated809.IBase2`2<class Generated809.BaseClass1,class Generated809.BaseClass1>::Method7<object>()
		ldstr "G2_C327::Method7.7798<System.Object>()"
		ldstr "class IBase2`2<class Generated809.BaseClass1,class Generated809.BaseClass1> on type class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>
		callvirt instance string class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>::ClassMethod1996<object>()
		ldstr "G2_C327::ClassMethod1996.7807<System.Object>()"
		ldstr "class G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>
		callvirt instance string class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>::ClassMethod1995<object>()
		ldstr "G2_C327::ClassMethod1995.7806<System.Object>()"
		ldstr "class G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>
		callvirt instance string class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>::ClassMethod1994()
		ldstr "G2_C327::ClassMethod1994.7805()"
		ldstr "class G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>
		callvirt instance string class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>::ClassMethod1993()
		ldstr "G2_C327::ClassMethod1993.7804()"
		ldstr "class G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>
		callvirt instance string class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>::Method6<object>()
		ldstr "G2_C327::Method6.7802<System.Object>()"
		ldstr "class G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>
		callvirt instance string class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>::Method5()
		ldstr "G2_C327::Method5.7800()"
		ldstr "class G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>
		callvirt instance string class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>::Method4()
		ldstr "G2_C327::Method4.7799()"
		ldstr "class G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>
		callvirt instance string class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>::Method7<object>()
		ldstr "G2_C327::Method7.7798<System.Object>()"
		ldstr "class G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>
		callvirt instance string class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>::ClassMethod1331<object>()
		ldstr "G2_C327::ClassMethod1331.MI.7808<System.Object>()"
		ldstr "class G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>
		callvirt instance string class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>::ClassMethod1330<object>()
		ldstr "G1_C7::ClassMethod1330.4819<System.Object>()"
		ldstr "class G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>
		callvirt instance string class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>::ClassMethod1329()
		ldstr "G1_C7::ClassMethod1329.4818()"
		ldstr "class G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>
		callvirt instance string class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>::ClassMethod1328()
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		callvirt instance string class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::ClassMethod1331<object>()
		ldstr "G2_C327::ClassMethod1331.MI.7808<System.Object>()"
		ldstr "class G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		callvirt instance string class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::ClassMethod1330<object>()
		ldstr "G1_C7::ClassMethod1330.4819<System.Object>()"
		ldstr "class G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		callvirt instance string class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::ClassMethod1329()
		ldstr "G1_C7::ClassMethod1329.4818()"
		ldstr "class G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		callvirt instance string class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::ClassMethod1328()
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		callvirt instance string class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::Method7<object>()
		ldstr "G1_C7::Method7.4815<System.Object>()"
		ldstr "class G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		callvirt instance string class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::Method6<object>()
		ldstr "G2_C327::Method6.7802<System.Object>()"
		ldstr "class G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		callvirt instance string class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::Method5()
		ldstr "G2_C327::Method5.7800()"
		ldstr "class G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		callvirt instance string class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::Method4()
		ldstr "G1_C7::Method4.4811()"
		ldstr "class G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated809.IBase1`1<class Generated809.BaseClass0>::Method4()
		ldstr "G2_C327::Method4.7799()"
		ldstr "class IBase1`1<class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated809.IBase1`1<class Generated809.BaseClass0>::Method5()
		ldstr "G2_C327::Method5.MI.7801()"
		ldstr "class IBase1`1<class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated809.IBase1`1<class Generated809.BaseClass0>::Method6<object>()
		ldstr "G2_C327::Method6.MI.7803<System.Object>()"
		ldstr "class IBase1`1<class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated809.IBase2`2<class Generated809.BaseClass1,class Generated809.BaseClass0>::Method7<object>()
		ldstr "G1_C7::Method7.MI.4816<System.Object>()"
		ldstr "class IBase2`2<class Generated809.BaseClass1,class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated809.IBase2`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::Method7<object>()
		ldstr "G1_C7::Method7.MI.4816<System.Object>()"
		ldstr "class IBase2`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated809.IBase2`2<class Generated809.BaseClass0,class Generated809.BaseClass1>::Method7<object>()
		ldstr "G2_C327::Method7.7798<System.Object>()"
		ldstr "class IBase2`2<class Generated809.BaseClass0,class Generated809.BaseClass1> on type class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated809.IBase2`2<class Generated809.BaseClass1,class Generated809.BaseClass1>::Method7<object>()
		ldstr "G2_C327::Method7.7798<System.Object>()"
		ldstr "class IBase2`2<class Generated809.BaseClass1,class Generated809.BaseClass1> on type class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>
		callvirt instance string class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>::ClassMethod1996<object>()
		ldstr "G2_C327::ClassMethod1996.7807<System.Object>()"
		ldstr "class G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1> on type class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>
		callvirt instance string class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>::ClassMethod1995<object>()
		ldstr "G2_C327::ClassMethod1995.7806<System.Object>()"
		ldstr "class G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1> on type class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>
		callvirt instance string class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>::ClassMethod1994()
		ldstr "G2_C327::ClassMethod1994.7805()"
		ldstr "class G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1> on type class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>
		callvirt instance string class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>::ClassMethod1993()
		ldstr "G2_C327::ClassMethod1993.7804()"
		ldstr "class G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1> on type class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>
		callvirt instance string class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>::Method6<object>()
		ldstr "G2_C327::Method6.7802<System.Object>()"
		ldstr "class G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1> on type class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>
		callvirt instance string class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>::Method5()
		ldstr "G2_C327::Method5.7800()"
		ldstr "class G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1> on type class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>
		callvirt instance string class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>::Method4()
		ldstr "G2_C327::Method4.7799()"
		ldstr "class G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1> on type class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>
		callvirt instance string class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>::Method7<object>()
		ldstr "G2_C327::Method7.7798<System.Object>()"
		ldstr "class G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1> on type class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>
		callvirt instance string class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>::ClassMethod1331<object>()
		ldstr "G2_C327::ClassMethod1331.MI.7808<System.Object>()"
		ldstr "class G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1> on type class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>
		callvirt instance string class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>::ClassMethod1330<object>()
		ldstr "G1_C7::ClassMethod1330.4819<System.Object>()"
		ldstr "class G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1> on type class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>
		callvirt instance string class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>::ClassMethod1329()
		ldstr "G1_C7::ClassMethod1329.4818()"
		ldstr "class G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1> on type class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>
		callvirt instance string class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>::ClassMethod1328()
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1> on type class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>"
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
		newobj instance void class Generated809.G3_C1281`1<class Generated809.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G1_C7::ClassMethod1329.4818()#G1_C7::ClassMethod1330.4819<System.Object>()#G2_C327::ClassMethod1331.MI.7808<System.Object>()#G1_C7::Method4.4811()#G3_C1281::Method5.15316()#G2_C327::Method6.7802<System.Object>()#G1_C7::Method7.4815<System.Object>()#"
		call void Generated809.Generated809::M.G1_C7.T.T<class Generated809.BaseClass0,class Generated809.BaseClass0,class Generated809.G3_C1281`1<class Generated809.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G1_C7::ClassMethod1329.4818()#G1_C7::ClassMethod1330.4819<System.Object>()#G2_C327::ClassMethod1331.MI.7808<System.Object>()#G1_C7::Method4.4811()#G3_C1281::Method5.15316()#G2_C327::Method6.7802<System.Object>()#G1_C7::Method7.4815<System.Object>()#"
		call void Generated809.Generated809::M.G1_C7.A.T<class Generated809.BaseClass0,class Generated809.G3_C1281`1<class Generated809.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G1_C7::ClassMethod1329.4818()#G1_C7::ClassMethod1330.4819<System.Object>()#G2_C327::ClassMethod1331.MI.7808<System.Object>()#G1_C7::Method4.4811()#G3_C1281::Method5.15316()#G2_C327::Method6.7802<System.Object>()#G1_C7::Method7.4815<System.Object>()#"
		call void Generated809.Generated809::M.G1_C7.A.A<class Generated809.G3_C1281`1<class Generated809.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1281::Method4.MI.15315()#G3_C1281::Method5.15316()#G3_C1281::Method6.MI.15318<System.Object>()#"
		call void Generated809.Generated809::M.IBase1.T<class Generated809.BaseClass0,class Generated809.G3_C1281`1<class Generated809.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G3_C1281::Method4.MI.15315()#G3_C1281::Method5.15316()#G3_C1281::Method6.MI.15318<System.Object>()#"
		call void Generated809.Generated809::M.IBase1.A<class Generated809.G3_C1281`1<class Generated809.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C7::Method7.MI.4816<System.Object>()#"
		call void Generated809.Generated809::M.IBase2.T.T<class Generated809.BaseClass1,class Generated809.BaseClass0,class Generated809.G3_C1281`1<class Generated809.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C7::Method7.MI.4816<System.Object>()#"
		call void Generated809.Generated809::M.IBase2.B.T<class Generated809.BaseClass0,class Generated809.G3_C1281`1<class Generated809.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::Method7.MI.4816<System.Object>()#"
		call void Generated809.Generated809::M.IBase2.B.A<class Generated809.G3_C1281`1<class Generated809.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C7::Method7.MI.4816<System.Object>()#"
		call void Generated809.Generated809::M.IBase2.T.T<class Generated809.BaseClass0,class Generated809.BaseClass0,class Generated809.G3_C1281`1<class Generated809.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C7::Method7.MI.4816<System.Object>()#"
		call void Generated809.Generated809::M.IBase2.A.T<class Generated809.BaseClass0,class Generated809.G3_C1281`1<class Generated809.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::Method7.MI.4816<System.Object>()#"
		call void Generated809.Generated809::M.IBase2.A.A<class Generated809.G3_C1281`1<class Generated809.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C327::Method7.7798<System.Object>()#"
		call void Generated809.Generated809::M.IBase2.T.T<class Generated809.BaseClass0,class Generated809.BaseClass1,class Generated809.G3_C1281`1<class Generated809.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C327::Method7.7798<System.Object>()#"
		call void Generated809.Generated809::M.IBase2.A.T<class Generated809.BaseClass1,class Generated809.G3_C1281`1<class Generated809.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C327::Method7.7798<System.Object>()#"
		call void Generated809.Generated809::M.IBase2.A.B<class Generated809.G3_C1281`1<class Generated809.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C327::Method7.7798<System.Object>()#"
		call void Generated809.Generated809::M.IBase2.T.T<class Generated809.BaseClass1,class Generated809.BaseClass1,class Generated809.G3_C1281`1<class Generated809.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C327::Method7.7798<System.Object>()#"
		call void Generated809.Generated809::M.IBase2.B.T<class Generated809.BaseClass1,class Generated809.G3_C1281`1<class Generated809.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C327::Method7.7798<System.Object>()#"
		call void Generated809.Generated809::M.IBase2.B.B<class Generated809.G3_C1281`1<class Generated809.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G1_C7::ClassMethod1329.4818()#G1_C7::ClassMethod1330.4819<System.Object>()#G2_C327::ClassMethod1331.MI.7808<System.Object>()#G3_C1281::ClassMethod1993.MI.15321()#G3_C1281::ClassMethod1994.MI.15322()#G2_C327::ClassMethod1995.7806<System.Object>()#G2_C327::ClassMethod1996.7807<System.Object>()#G3_C1281::Method4.15314()#G3_C1281::Method5.15316()#G2_C327::Method6.7802<System.Object>()#G2_C327::Method7.7798<System.Object>()#"
		call void Generated809.Generated809::M.G2_C327.T.T<class Generated809.BaseClass0,class Generated809.BaseClass0,class Generated809.G3_C1281`1<class Generated809.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G1_C7::ClassMethod1329.4818()#G1_C7::ClassMethod1330.4819<System.Object>()#G2_C327::ClassMethod1331.MI.7808<System.Object>()#G3_C1281::ClassMethod1993.MI.15321()#G3_C1281::ClassMethod1994.MI.15322()#G2_C327::ClassMethod1995.7806<System.Object>()#G2_C327::ClassMethod1996.7807<System.Object>()#G3_C1281::Method4.15314()#G3_C1281::Method5.15316()#G2_C327::Method6.7802<System.Object>()#G2_C327::Method7.7798<System.Object>()#"
		call void Generated809.Generated809::M.G2_C327.A.T<class Generated809.BaseClass0,class Generated809.G3_C1281`1<class Generated809.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G1_C7::ClassMethod1329.4818()#G1_C7::ClassMethod1330.4819<System.Object>()#G2_C327::ClassMethod1331.MI.7808<System.Object>()#G3_C1281::ClassMethod1993.MI.15321()#G3_C1281::ClassMethod1994.MI.15322()#G2_C327::ClassMethod1995.7806<System.Object>()#G2_C327::ClassMethod1996.7807<System.Object>()#G3_C1281::Method4.15314()#G3_C1281::Method5.15316()#G2_C327::Method6.7802<System.Object>()#G2_C327::Method7.7798<System.Object>()#"
		call void Generated809.Generated809::M.G2_C327.A.A<class Generated809.G3_C1281`1<class Generated809.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G1_C7::ClassMethod1329.4818()#G1_C7::ClassMethod1330.4819<System.Object>()#G2_C327::ClassMethod1331.MI.7808<System.Object>()#G3_C1281::ClassMethod1993.MI.15321()#G3_C1281::ClassMethod1994.MI.15322()#G2_C327::ClassMethod1995.7806<System.Object>()#G2_C327::ClassMethod1996.7807<System.Object>()#G3_C1281::ClassMethod3938.15319()#G3_C1281::ClassMethod3939.15320<System.Object>()#G3_C1281::Method4.15314()#G3_C1281::Method5.15316()#G3_C1281::Method6.15317<System.Object>()#G2_C327::Method7.7798<System.Object>()#"
		call void Generated809.Generated809::M.G3_C1281.T<class Generated809.BaseClass0,class Generated809.G3_C1281`1<class Generated809.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G1_C7::ClassMethod1329.4818()#G1_C7::ClassMethod1330.4819<System.Object>()#G2_C327::ClassMethod1331.MI.7808<System.Object>()#G3_C1281::ClassMethod1993.MI.15321()#G3_C1281::ClassMethod1994.MI.15322()#G2_C327::ClassMethod1995.7806<System.Object>()#G2_C327::ClassMethod1996.7807<System.Object>()#G3_C1281::ClassMethod3938.15319()#G3_C1281::ClassMethod3939.15320<System.Object>()#G3_C1281::Method4.15314()#G3_C1281::Method5.15316()#G3_C1281::Method6.15317<System.Object>()#G2_C327::Method7.7798<System.Object>()#"
		call void Generated809.Generated809::M.G3_C1281.A<class Generated809.G3_C1281`1<class Generated809.BaseClass0>>(!!0,string)
		newobj instance void class Generated809.G3_C1281`1<class Generated809.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G1_C7::ClassMethod1329.4818()#G1_C7::ClassMethod1330.4819<System.Object>()#G2_C327::ClassMethod1331.MI.7808<System.Object>()#G1_C7::Method4.4811()#G3_C1281::Method5.15316()#G2_C327::Method6.7802<System.Object>()#G1_C7::Method7.4815<System.Object>()#"
		call void Generated809.Generated809::M.G1_C7.T.T<class Generated809.BaseClass0,class Generated809.BaseClass0,class Generated809.G3_C1281`1<class Generated809.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G1_C7::ClassMethod1329.4818()#G1_C7::ClassMethod1330.4819<System.Object>()#G2_C327::ClassMethod1331.MI.7808<System.Object>()#G1_C7::Method4.4811()#G3_C1281::Method5.15316()#G2_C327::Method6.7802<System.Object>()#G1_C7::Method7.4815<System.Object>()#"
		call void Generated809.Generated809::M.G1_C7.A.T<class Generated809.BaseClass0,class Generated809.G3_C1281`1<class Generated809.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G1_C7::ClassMethod1329.4818()#G1_C7::ClassMethod1330.4819<System.Object>()#G2_C327::ClassMethod1331.MI.7808<System.Object>()#G1_C7::Method4.4811()#G3_C1281::Method5.15316()#G2_C327::Method6.7802<System.Object>()#G1_C7::Method7.4815<System.Object>()#"
		call void Generated809.Generated809::M.G1_C7.A.A<class Generated809.G3_C1281`1<class Generated809.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1281::Method4.MI.15315()#G3_C1281::Method5.15316()#G3_C1281::Method6.MI.15318<System.Object>()#"
		call void Generated809.Generated809::M.IBase1.T<class Generated809.BaseClass0,class Generated809.G3_C1281`1<class Generated809.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1281::Method4.MI.15315()#G3_C1281::Method5.15316()#G3_C1281::Method6.MI.15318<System.Object>()#"
		call void Generated809.Generated809::M.IBase1.A<class Generated809.G3_C1281`1<class Generated809.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C7::Method7.MI.4816<System.Object>()#"
		call void Generated809.Generated809::M.IBase2.T.T<class Generated809.BaseClass1,class Generated809.BaseClass0,class Generated809.G3_C1281`1<class Generated809.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C7::Method7.MI.4816<System.Object>()#"
		call void Generated809.Generated809::M.IBase2.B.T<class Generated809.BaseClass0,class Generated809.G3_C1281`1<class Generated809.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::Method7.MI.4816<System.Object>()#"
		call void Generated809.Generated809::M.IBase2.B.A<class Generated809.G3_C1281`1<class Generated809.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C7::Method7.MI.4816<System.Object>()#"
		call void Generated809.Generated809::M.IBase2.T.T<class Generated809.BaseClass0,class Generated809.BaseClass0,class Generated809.G3_C1281`1<class Generated809.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C7::Method7.MI.4816<System.Object>()#"
		call void Generated809.Generated809::M.IBase2.A.T<class Generated809.BaseClass0,class Generated809.G3_C1281`1<class Generated809.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::Method7.MI.4816<System.Object>()#"
		call void Generated809.Generated809::M.IBase2.A.A<class Generated809.G3_C1281`1<class Generated809.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C327::Method7.7798<System.Object>()#"
		call void Generated809.Generated809::M.IBase2.T.T<class Generated809.BaseClass0,class Generated809.BaseClass1,class Generated809.G3_C1281`1<class Generated809.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C327::Method7.7798<System.Object>()#"
		call void Generated809.Generated809::M.IBase2.A.T<class Generated809.BaseClass1,class Generated809.G3_C1281`1<class Generated809.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C327::Method7.7798<System.Object>()#"
		call void Generated809.Generated809::M.IBase2.A.B<class Generated809.G3_C1281`1<class Generated809.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C327::Method7.7798<System.Object>()#"
		call void Generated809.Generated809::M.IBase2.T.T<class Generated809.BaseClass1,class Generated809.BaseClass1,class Generated809.G3_C1281`1<class Generated809.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C327::Method7.7798<System.Object>()#"
		call void Generated809.Generated809::M.IBase2.B.T<class Generated809.BaseClass1,class Generated809.G3_C1281`1<class Generated809.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C327::Method7.7798<System.Object>()#"
		call void Generated809.Generated809::M.IBase2.B.B<class Generated809.G3_C1281`1<class Generated809.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G1_C7::ClassMethod1329.4818()#G1_C7::ClassMethod1330.4819<System.Object>()#G2_C327::ClassMethod1331.MI.7808<System.Object>()#G3_C1281::ClassMethod1993.MI.15321()#G3_C1281::ClassMethod1994.MI.15322()#G2_C327::ClassMethod1995.7806<System.Object>()#G2_C327::ClassMethod1996.7807<System.Object>()#G3_C1281::Method4.15314()#G3_C1281::Method5.15316()#G2_C327::Method6.7802<System.Object>()#G2_C327::Method7.7798<System.Object>()#"
		call void Generated809.Generated809::M.G2_C327.T.T<class Generated809.BaseClass0,class Generated809.BaseClass1,class Generated809.G3_C1281`1<class Generated809.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G1_C7::ClassMethod1329.4818()#G1_C7::ClassMethod1330.4819<System.Object>()#G2_C327::ClassMethod1331.MI.7808<System.Object>()#G3_C1281::ClassMethod1993.MI.15321()#G3_C1281::ClassMethod1994.MI.15322()#G2_C327::ClassMethod1995.7806<System.Object>()#G2_C327::ClassMethod1996.7807<System.Object>()#G3_C1281::Method4.15314()#G3_C1281::Method5.15316()#G2_C327::Method6.7802<System.Object>()#G2_C327::Method7.7798<System.Object>()#"
		call void Generated809.Generated809::M.G2_C327.A.T<class Generated809.BaseClass1,class Generated809.G3_C1281`1<class Generated809.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G1_C7::ClassMethod1329.4818()#G1_C7::ClassMethod1330.4819<System.Object>()#G2_C327::ClassMethod1331.MI.7808<System.Object>()#G3_C1281::ClassMethod1993.MI.15321()#G3_C1281::ClassMethod1994.MI.15322()#G2_C327::ClassMethod1995.7806<System.Object>()#G2_C327::ClassMethod1996.7807<System.Object>()#G3_C1281::Method4.15314()#G3_C1281::Method5.15316()#G2_C327::Method6.7802<System.Object>()#G2_C327::Method7.7798<System.Object>()#"
		call void Generated809.Generated809::M.G2_C327.A.B<class Generated809.G3_C1281`1<class Generated809.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G1_C7::ClassMethod1329.4818()#G1_C7::ClassMethod1330.4819<System.Object>()#G2_C327::ClassMethod1331.MI.7808<System.Object>()#G3_C1281::ClassMethod1993.MI.15321()#G3_C1281::ClassMethod1994.MI.15322()#G2_C327::ClassMethod1995.7806<System.Object>()#G2_C327::ClassMethod1996.7807<System.Object>()#G3_C1281::ClassMethod3938.15319()#G3_C1281::ClassMethod3939.15320<System.Object>()#G3_C1281::Method4.15314()#G3_C1281::Method5.15316()#G3_C1281::Method6.15317<System.Object>()#G2_C327::Method7.7798<System.Object>()#"
		call void Generated809.Generated809::M.G3_C1281.T<class Generated809.BaseClass1,class Generated809.G3_C1281`1<class Generated809.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G1_C7::ClassMethod1329.4818()#G1_C7::ClassMethod1330.4819<System.Object>()#G2_C327::ClassMethod1331.MI.7808<System.Object>()#G3_C1281::ClassMethod1993.MI.15321()#G3_C1281::ClassMethod1994.MI.15322()#G2_C327::ClassMethod1995.7806<System.Object>()#G2_C327::ClassMethod1996.7807<System.Object>()#G3_C1281::ClassMethod3938.15319()#G3_C1281::ClassMethod3939.15320<System.Object>()#G3_C1281::Method4.15314()#G3_C1281::Method5.15316()#G3_C1281::Method6.15317<System.Object>()#G2_C327::Method7.7798<System.Object>()#"
		call void Generated809.Generated809::M.G3_C1281.B<class Generated809.G3_C1281`1<class Generated809.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1281::Method4.MI.15315()#G3_C1281::Method5.15316()#G3_C1281::Method6.MI.15318<System.Object>()#"
		call void Generated809.Generated809::M.IBase1.T<class Generated809.BaseClass1,class Generated809.G3_C1281`1<class Generated809.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1281::Method4.MI.15315()#G3_C1281::Method5.15316()#G3_C1281::Method6.MI.15318<System.Object>()#"
		call void Generated809.Generated809::M.IBase1.B<class Generated809.G3_C1281`1<class Generated809.BaseClass1>>(!!0,string)
		newobj instance void class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G1_C7::ClassMethod1329.4818()#G1_C7::ClassMethod1330.4819<System.Object>()#G2_C327::ClassMethod1331.MI.7808<System.Object>()#G1_C7::Method4.4811()#G2_C327::Method5.7800()#G2_C327::Method6.7802<System.Object>()#G1_C7::Method7.4815<System.Object>()#"
		call void Generated809.Generated809::M.G1_C7.T.T<class Generated809.BaseClass0,class Generated809.BaseClass0,class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G1_C7::ClassMethod1329.4818()#G1_C7::ClassMethod1330.4819<System.Object>()#G2_C327::ClassMethod1331.MI.7808<System.Object>()#G1_C7::Method4.4811()#G2_C327::Method5.7800()#G2_C327::Method6.7802<System.Object>()#G1_C7::Method7.4815<System.Object>()#"
		call void Generated809.Generated809::M.G1_C7.A.T<class Generated809.BaseClass0,class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G1_C7::ClassMethod1329.4818()#G1_C7::ClassMethod1330.4819<System.Object>()#G2_C327::ClassMethod1331.MI.7808<System.Object>()#G1_C7::Method4.4811()#G2_C327::Method5.7800()#G2_C327::Method6.7802<System.Object>()#G1_C7::Method7.4815<System.Object>()#"
		call void Generated809.Generated809::M.G1_C7.A.A<class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C327::Method4.7799()#G2_C327::Method5.MI.7801()#G2_C327::Method6.MI.7803<System.Object>()#"
		call void Generated809.Generated809::M.IBase1.T<class Generated809.BaseClass0,class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C327::Method4.7799()#G2_C327::Method5.MI.7801()#G2_C327::Method6.MI.7803<System.Object>()#"
		call void Generated809.Generated809::M.IBase1.A<class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C7::Method7.MI.4816<System.Object>()#"
		call void Generated809.Generated809::M.IBase2.T.T<class Generated809.BaseClass1,class Generated809.BaseClass0,class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C7::Method7.MI.4816<System.Object>()#"
		call void Generated809.Generated809::M.IBase2.B.T<class Generated809.BaseClass0,class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::Method7.MI.4816<System.Object>()#"
		call void Generated809.Generated809::M.IBase2.B.A<class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C7::Method7.MI.4816<System.Object>()#"
		call void Generated809.Generated809::M.IBase2.T.T<class Generated809.BaseClass0,class Generated809.BaseClass0,class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C7::Method7.MI.4816<System.Object>()#"
		call void Generated809.Generated809::M.IBase2.A.T<class Generated809.BaseClass0,class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::Method7.MI.4816<System.Object>()#"
		call void Generated809.Generated809::M.IBase2.A.A<class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C327::Method7.7798<System.Object>()#"
		call void Generated809.Generated809::M.IBase2.T.T<class Generated809.BaseClass0,class Generated809.BaseClass1,class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C327::Method7.7798<System.Object>()#"
		call void Generated809.Generated809::M.IBase2.A.T<class Generated809.BaseClass1,class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C327::Method7.7798<System.Object>()#"
		call void Generated809.Generated809::M.IBase2.A.B<class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C327::Method7.7798<System.Object>()#"
		call void Generated809.Generated809::M.IBase2.T.T<class Generated809.BaseClass1,class Generated809.BaseClass1,class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C327::Method7.7798<System.Object>()#"
		call void Generated809.Generated809::M.IBase2.B.T<class Generated809.BaseClass1,class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C327::Method7.7798<System.Object>()#"
		call void Generated809.Generated809::M.IBase2.B.B<class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G1_C7::ClassMethod1329.4818()#G1_C7::ClassMethod1330.4819<System.Object>()#G2_C327::ClassMethod1331.MI.7808<System.Object>()#G2_C327::ClassMethod1993.7804()#G2_C327::ClassMethod1994.7805()#G2_C327::ClassMethod1995.7806<System.Object>()#G2_C327::ClassMethod1996.7807<System.Object>()#G2_C327::Method4.7799()#G2_C327::Method5.7800()#G2_C327::Method6.7802<System.Object>()#G2_C327::Method7.7798<System.Object>()#"
		call void Generated809.Generated809::M.G2_C327.T.T<class Generated809.BaseClass0,class Generated809.BaseClass0,class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G1_C7::ClassMethod1329.4818()#G1_C7::ClassMethod1330.4819<System.Object>()#G2_C327::ClassMethod1331.MI.7808<System.Object>()#G2_C327::ClassMethod1993.7804()#G2_C327::ClassMethod1994.7805()#G2_C327::ClassMethod1995.7806<System.Object>()#G2_C327::ClassMethod1996.7807<System.Object>()#G2_C327::Method4.7799()#G2_C327::Method5.7800()#G2_C327::Method6.7802<System.Object>()#G2_C327::Method7.7798<System.Object>()#"
		call void Generated809.Generated809::M.G2_C327.A.T<class Generated809.BaseClass0,class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G1_C7::ClassMethod1329.4818()#G1_C7::ClassMethod1330.4819<System.Object>()#G2_C327::ClassMethod1331.MI.7808<System.Object>()#G2_C327::ClassMethod1993.7804()#G2_C327::ClassMethod1994.7805()#G2_C327::ClassMethod1995.7806<System.Object>()#G2_C327::ClassMethod1996.7807<System.Object>()#G2_C327::Method4.7799()#G2_C327::Method5.7800()#G2_C327::Method6.7802<System.Object>()#G2_C327::Method7.7798<System.Object>()#"
		call void Generated809.Generated809::M.G2_C327.A.A<class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>>(!!0,string)
		newobj instance void class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G1_C7::ClassMethod1329.4818()#G1_C7::ClassMethod1330.4819<System.Object>()#G2_C327::ClassMethod1331.MI.7808<System.Object>()#G1_C7::Method4.4811()#G2_C327::Method5.7800()#G2_C327::Method6.7802<System.Object>()#G1_C7::Method7.4815<System.Object>()#"
		call void Generated809.Generated809::M.G1_C7.T.T<class Generated809.BaseClass0,class Generated809.BaseClass0,class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G1_C7::ClassMethod1329.4818()#G1_C7::ClassMethod1330.4819<System.Object>()#G2_C327::ClassMethod1331.MI.7808<System.Object>()#G1_C7::Method4.4811()#G2_C327::Method5.7800()#G2_C327::Method6.7802<System.Object>()#G1_C7::Method7.4815<System.Object>()#"
		call void Generated809.Generated809::M.G1_C7.A.T<class Generated809.BaseClass0,class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G1_C7::ClassMethod1329.4818()#G1_C7::ClassMethod1330.4819<System.Object>()#G2_C327::ClassMethod1331.MI.7808<System.Object>()#G1_C7::Method4.4811()#G2_C327::Method5.7800()#G2_C327::Method6.7802<System.Object>()#G1_C7::Method7.4815<System.Object>()#"
		call void Generated809.Generated809::M.G1_C7.A.A<class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C327::Method4.7799()#G2_C327::Method5.MI.7801()#G2_C327::Method6.MI.7803<System.Object>()#"
		call void Generated809.Generated809::M.IBase1.T<class Generated809.BaseClass0,class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C327::Method4.7799()#G2_C327::Method5.MI.7801()#G2_C327::Method6.MI.7803<System.Object>()#"
		call void Generated809.Generated809::M.IBase1.A<class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C7::Method7.MI.4816<System.Object>()#"
		call void Generated809.Generated809::M.IBase2.T.T<class Generated809.BaseClass1,class Generated809.BaseClass0,class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C7::Method7.MI.4816<System.Object>()#"
		call void Generated809.Generated809::M.IBase2.B.T<class Generated809.BaseClass0,class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::Method7.MI.4816<System.Object>()#"
		call void Generated809.Generated809::M.IBase2.B.A<class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C7::Method7.MI.4816<System.Object>()#"
		call void Generated809.Generated809::M.IBase2.T.T<class Generated809.BaseClass0,class Generated809.BaseClass0,class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C7::Method7.MI.4816<System.Object>()#"
		call void Generated809.Generated809::M.IBase2.A.T<class Generated809.BaseClass0,class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::Method7.MI.4816<System.Object>()#"
		call void Generated809.Generated809::M.IBase2.A.A<class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C327::Method7.7798<System.Object>()#"
		call void Generated809.Generated809::M.IBase2.T.T<class Generated809.BaseClass0,class Generated809.BaseClass1,class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C327::Method7.7798<System.Object>()#"
		call void Generated809.Generated809::M.IBase2.A.T<class Generated809.BaseClass1,class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C327::Method7.7798<System.Object>()#"
		call void Generated809.Generated809::M.IBase2.A.B<class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C327::Method7.7798<System.Object>()#"
		call void Generated809.Generated809::M.IBase2.T.T<class Generated809.BaseClass1,class Generated809.BaseClass1,class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C327::Method7.7798<System.Object>()#"
		call void Generated809.Generated809::M.IBase2.B.T<class Generated809.BaseClass1,class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C327::Method7.7798<System.Object>()#"
		call void Generated809.Generated809::M.IBase2.B.B<class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G1_C7::ClassMethod1329.4818()#G1_C7::ClassMethod1330.4819<System.Object>()#G2_C327::ClassMethod1331.MI.7808<System.Object>()#G2_C327::ClassMethod1993.7804()#G2_C327::ClassMethod1994.7805()#G2_C327::ClassMethod1995.7806<System.Object>()#G2_C327::ClassMethod1996.7807<System.Object>()#G2_C327::Method4.7799()#G2_C327::Method5.7800()#G2_C327::Method6.7802<System.Object>()#G2_C327::Method7.7798<System.Object>()#"
		call void Generated809.Generated809::M.G2_C327.T.T<class Generated809.BaseClass0,class Generated809.BaseClass1,class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G1_C7::ClassMethod1329.4818()#G1_C7::ClassMethod1330.4819<System.Object>()#G2_C327::ClassMethod1331.MI.7808<System.Object>()#G2_C327::ClassMethod1993.7804()#G2_C327::ClassMethod1994.7805()#G2_C327::ClassMethod1995.7806<System.Object>()#G2_C327::ClassMethod1996.7807<System.Object>()#G2_C327::Method4.7799()#G2_C327::Method5.7800()#G2_C327::Method6.7802<System.Object>()#G2_C327::Method7.7798<System.Object>()#"
		call void Generated809.Generated809::M.G2_C327.A.T<class Generated809.BaseClass1,class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G1_C7::ClassMethod1329.4818()#G1_C7::ClassMethod1330.4819<System.Object>()#G2_C327::ClassMethod1331.MI.7808<System.Object>()#G2_C327::ClassMethod1993.7804()#G2_C327::ClassMethod1994.7805()#G2_C327::ClassMethod1995.7806<System.Object>()#G2_C327::ClassMethod1996.7807<System.Object>()#G2_C327::Method4.7799()#G2_C327::Method5.7800()#G2_C327::Method6.7802<System.Object>()#G2_C327::Method7.7798<System.Object>()#"
		call void Generated809.Generated809::M.G2_C327.A.B<class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>>(!!0,string)
		newobj instance void class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G1_C7::ClassMethod1329.4818()#G1_C7::ClassMethod1330.4819<System.Object>()#G2_C327::ClassMethod1331.MI.7808<System.Object>()#G1_C7::Method4.4811()#G2_C327::Method5.7800()#G2_C327::Method6.7802<System.Object>()#G1_C7::Method7.4815<System.Object>()#"
		call void Generated809.Generated809::M.G1_C7.T.T<class Generated809.BaseClass0,class Generated809.BaseClass0,class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G1_C7::ClassMethod1329.4818()#G1_C7::ClassMethod1330.4819<System.Object>()#G2_C327::ClassMethod1331.MI.7808<System.Object>()#G1_C7::Method4.4811()#G2_C327::Method5.7800()#G2_C327::Method6.7802<System.Object>()#G1_C7::Method7.4815<System.Object>()#"
		call void Generated809.Generated809::M.G1_C7.A.T<class Generated809.BaseClass0,class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G1_C7::ClassMethod1329.4818()#G1_C7::ClassMethod1330.4819<System.Object>()#G2_C327::ClassMethod1331.MI.7808<System.Object>()#G1_C7::Method4.4811()#G2_C327::Method5.7800()#G2_C327::Method6.7802<System.Object>()#G1_C7::Method7.4815<System.Object>()#"
		call void Generated809.Generated809::M.G1_C7.A.A<class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C327::Method4.7799()#G2_C327::Method5.MI.7801()#G2_C327::Method6.MI.7803<System.Object>()#"
		call void Generated809.Generated809::M.IBase1.T<class Generated809.BaseClass0,class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C327::Method4.7799()#G2_C327::Method5.MI.7801()#G2_C327::Method6.MI.7803<System.Object>()#"
		call void Generated809.Generated809::M.IBase1.A<class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C7::Method7.MI.4816<System.Object>()#"
		call void Generated809.Generated809::M.IBase2.T.T<class Generated809.BaseClass1,class Generated809.BaseClass0,class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C7::Method7.MI.4816<System.Object>()#"
		call void Generated809.Generated809::M.IBase2.B.T<class Generated809.BaseClass0,class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::Method7.MI.4816<System.Object>()#"
		call void Generated809.Generated809::M.IBase2.B.A<class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C7::Method7.MI.4816<System.Object>()#"
		call void Generated809.Generated809::M.IBase2.T.T<class Generated809.BaseClass0,class Generated809.BaseClass0,class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C7::Method7.MI.4816<System.Object>()#"
		call void Generated809.Generated809::M.IBase2.A.T<class Generated809.BaseClass0,class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::Method7.MI.4816<System.Object>()#"
		call void Generated809.Generated809::M.IBase2.A.A<class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C327::Method7.7798<System.Object>()#"
		call void Generated809.Generated809::M.IBase2.T.T<class Generated809.BaseClass0,class Generated809.BaseClass1,class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C327::Method7.7798<System.Object>()#"
		call void Generated809.Generated809::M.IBase2.A.T<class Generated809.BaseClass1,class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C327::Method7.7798<System.Object>()#"
		call void Generated809.Generated809::M.IBase2.A.B<class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C327::Method7.7798<System.Object>()#"
		call void Generated809.Generated809::M.IBase2.T.T<class Generated809.BaseClass1,class Generated809.BaseClass1,class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C327::Method7.7798<System.Object>()#"
		call void Generated809.Generated809::M.IBase2.B.T<class Generated809.BaseClass1,class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C327::Method7.7798<System.Object>()#"
		call void Generated809.Generated809::M.IBase2.B.B<class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G1_C7::ClassMethod1329.4818()#G1_C7::ClassMethod1330.4819<System.Object>()#G2_C327::ClassMethod1331.MI.7808<System.Object>()#G2_C327::ClassMethod1993.7804()#G2_C327::ClassMethod1994.7805()#G2_C327::ClassMethod1995.7806<System.Object>()#G2_C327::ClassMethod1996.7807<System.Object>()#G2_C327::Method4.7799()#G2_C327::Method5.7800()#G2_C327::Method6.7802<System.Object>()#G2_C327::Method7.7798<System.Object>()#"
		call void Generated809.Generated809::M.G2_C327.T.T<class Generated809.BaseClass1,class Generated809.BaseClass0,class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G1_C7::ClassMethod1329.4818()#G1_C7::ClassMethod1330.4819<System.Object>()#G2_C327::ClassMethod1331.MI.7808<System.Object>()#G2_C327::ClassMethod1993.7804()#G2_C327::ClassMethod1994.7805()#G2_C327::ClassMethod1995.7806<System.Object>()#G2_C327::ClassMethod1996.7807<System.Object>()#G2_C327::Method4.7799()#G2_C327::Method5.7800()#G2_C327::Method6.7802<System.Object>()#G2_C327::Method7.7798<System.Object>()#"
		call void Generated809.Generated809::M.G2_C327.B.T<class Generated809.BaseClass0,class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G1_C7::ClassMethod1329.4818()#G1_C7::ClassMethod1330.4819<System.Object>()#G2_C327::ClassMethod1331.MI.7808<System.Object>()#G2_C327::ClassMethod1993.7804()#G2_C327::ClassMethod1994.7805()#G2_C327::ClassMethod1995.7806<System.Object>()#G2_C327::ClassMethod1996.7807<System.Object>()#G2_C327::Method4.7799()#G2_C327::Method5.7800()#G2_C327::Method6.7802<System.Object>()#G2_C327::Method7.7798<System.Object>()#"
		call void Generated809.Generated809::M.G2_C327.B.A<class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>>(!!0,string)
		newobj instance void class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G1_C7::ClassMethod1329.4818()#G1_C7::ClassMethod1330.4819<System.Object>()#G2_C327::ClassMethod1331.MI.7808<System.Object>()#G1_C7::Method4.4811()#G2_C327::Method5.7800()#G2_C327::Method6.7802<System.Object>()#G1_C7::Method7.4815<System.Object>()#"
		call void Generated809.Generated809::M.G1_C7.T.T<class Generated809.BaseClass0,class Generated809.BaseClass0,class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G1_C7::ClassMethod1329.4818()#G1_C7::ClassMethod1330.4819<System.Object>()#G2_C327::ClassMethod1331.MI.7808<System.Object>()#G1_C7::Method4.4811()#G2_C327::Method5.7800()#G2_C327::Method6.7802<System.Object>()#G1_C7::Method7.4815<System.Object>()#"
		call void Generated809.Generated809::M.G1_C7.A.T<class Generated809.BaseClass0,class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G1_C7::ClassMethod1329.4818()#G1_C7::ClassMethod1330.4819<System.Object>()#G2_C327::ClassMethod1331.MI.7808<System.Object>()#G1_C7::Method4.4811()#G2_C327::Method5.7800()#G2_C327::Method6.7802<System.Object>()#G1_C7::Method7.4815<System.Object>()#"
		call void Generated809.Generated809::M.G1_C7.A.A<class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C327::Method4.7799()#G2_C327::Method5.MI.7801()#G2_C327::Method6.MI.7803<System.Object>()#"
		call void Generated809.Generated809::M.IBase1.T<class Generated809.BaseClass0,class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C327::Method4.7799()#G2_C327::Method5.MI.7801()#G2_C327::Method6.MI.7803<System.Object>()#"
		call void Generated809.Generated809::M.IBase1.A<class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C7::Method7.MI.4816<System.Object>()#"
		call void Generated809.Generated809::M.IBase2.T.T<class Generated809.BaseClass1,class Generated809.BaseClass0,class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C7::Method7.MI.4816<System.Object>()#"
		call void Generated809.Generated809::M.IBase2.B.T<class Generated809.BaseClass0,class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::Method7.MI.4816<System.Object>()#"
		call void Generated809.Generated809::M.IBase2.B.A<class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C7::Method7.MI.4816<System.Object>()#"
		call void Generated809.Generated809::M.IBase2.T.T<class Generated809.BaseClass0,class Generated809.BaseClass0,class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C7::Method7.MI.4816<System.Object>()#"
		call void Generated809.Generated809::M.IBase2.A.T<class Generated809.BaseClass0,class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::Method7.MI.4816<System.Object>()#"
		call void Generated809.Generated809::M.IBase2.A.A<class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C327::Method7.7798<System.Object>()#"
		call void Generated809.Generated809::M.IBase2.T.T<class Generated809.BaseClass0,class Generated809.BaseClass1,class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C327::Method7.7798<System.Object>()#"
		call void Generated809.Generated809::M.IBase2.A.T<class Generated809.BaseClass1,class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C327::Method7.7798<System.Object>()#"
		call void Generated809.Generated809::M.IBase2.A.B<class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C327::Method7.7798<System.Object>()#"
		call void Generated809.Generated809::M.IBase2.T.T<class Generated809.BaseClass1,class Generated809.BaseClass1,class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C327::Method7.7798<System.Object>()#"
		call void Generated809.Generated809::M.IBase2.B.T<class Generated809.BaseClass1,class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C327::Method7.7798<System.Object>()#"
		call void Generated809.Generated809::M.IBase2.B.B<class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G1_C7::ClassMethod1329.4818()#G1_C7::ClassMethod1330.4819<System.Object>()#G2_C327::ClassMethod1331.MI.7808<System.Object>()#G2_C327::ClassMethod1993.7804()#G2_C327::ClassMethod1994.7805()#G2_C327::ClassMethod1995.7806<System.Object>()#G2_C327::ClassMethod1996.7807<System.Object>()#G2_C327::Method4.7799()#G2_C327::Method5.7800()#G2_C327::Method6.7802<System.Object>()#G2_C327::Method7.7798<System.Object>()#"
		call void Generated809.Generated809::M.G2_C327.T.T<class Generated809.BaseClass1,class Generated809.BaseClass1,class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G1_C7::ClassMethod1329.4818()#G1_C7::ClassMethod1330.4819<System.Object>()#G2_C327::ClassMethod1331.MI.7808<System.Object>()#G2_C327::ClassMethod1993.7804()#G2_C327::ClassMethod1994.7805()#G2_C327::ClassMethod1995.7806<System.Object>()#G2_C327::ClassMethod1996.7807<System.Object>()#G2_C327::Method4.7799()#G2_C327::Method5.7800()#G2_C327::Method6.7802<System.Object>()#G2_C327::Method7.7798<System.Object>()#"
		call void Generated809.Generated809::M.G2_C327.B.T<class Generated809.BaseClass1,class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G1_C7::ClassMethod1329.4818()#G1_C7::ClassMethod1330.4819<System.Object>()#G2_C327::ClassMethod1331.MI.7808<System.Object>()#G2_C327::ClassMethod1993.7804()#G2_C327::ClassMethod1994.7805()#G2_C327::ClassMethod1995.7806<System.Object>()#G2_C327::ClassMethod1996.7807<System.Object>()#G2_C327::Method4.7799()#G2_C327::Method5.7800()#G2_C327::Method6.7802<System.Object>()#G2_C327::Method7.7798<System.Object>()#"
		call void Generated809.Generated809::M.G2_C327.B.B<class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>>(!!0,string)
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
		newobj instance void class Generated809.G3_C1281`1<class Generated809.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::ClassMethod1331<object>()
		calli default string(class Generated809.G3_C1281`1<class Generated809.BaseClass0>)
		ldstr "G2_C327::ClassMethod1331.MI.7808<System.Object>()"
		ldstr "class G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G3_C1281`1<class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::ClassMethod1330<object>()
		calli default string(class Generated809.G3_C1281`1<class Generated809.BaseClass0>)
		ldstr "G1_C7::ClassMethod1330.4819<System.Object>()"
		ldstr "class G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G3_C1281`1<class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::ClassMethod1329()
		calli default string(class Generated809.G3_C1281`1<class Generated809.BaseClass0>)
		ldstr "G1_C7::ClassMethod1329.4818()"
		ldstr "class G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G3_C1281`1<class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::ClassMethod1328()
		calli default string(class Generated809.G3_C1281`1<class Generated809.BaseClass0>)
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G3_C1281`1<class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::Method7<object>()
		calli default string(class Generated809.G3_C1281`1<class Generated809.BaseClass0>)
		ldstr "G1_C7::Method7.4815<System.Object>()"
		ldstr "class G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G3_C1281`1<class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::Method6<object>()
		calli default string(class Generated809.G3_C1281`1<class Generated809.BaseClass0>)
		ldstr "G2_C327::Method6.7802<System.Object>()"
		ldstr "class G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G3_C1281`1<class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::Method5()
		calli default string(class Generated809.G3_C1281`1<class Generated809.BaseClass0>)
		ldstr "G3_C1281::Method5.15316()"
		ldstr "class G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G3_C1281`1<class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::Method4()
		calli default string(class Generated809.G3_C1281`1<class Generated809.BaseClass0>)
		ldstr "G1_C7::Method4.4811()"
		ldstr "class G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G3_C1281`1<class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated809.IBase1`1<class Generated809.BaseClass0>::Method4()
		calli default string(class Generated809.G3_C1281`1<class Generated809.BaseClass0>)
		ldstr "G3_C1281::Method4.MI.15315()"
		ldstr "class IBase1`1<class Generated809.BaseClass0> on type class Generated809.G3_C1281`1<class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated809.IBase1`1<class Generated809.BaseClass0>::Method5()
		calli default string(class Generated809.G3_C1281`1<class Generated809.BaseClass0>)
		ldstr "G3_C1281::Method5.15316()"
		ldstr "class IBase1`1<class Generated809.BaseClass0> on type class Generated809.G3_C1281`1<class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated809.IBase1`1<class Generated809.BaseClass0>::Method6<object>()
		calli default string(class Generated809.G3_C1281`1<class Generated809.BaseClass0>)
		ldstr "G3_C1281::Method6.MI.15318<System.Object>()"
		ldstr "class IBase1`1<class Generated809.BaseClass0> on type class Generated809.G3_C1281`1<class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated809.IBase2`2<class Generated809.BaseClass1,class Generated809.BaseClass0>::Method7<object>()
		calli default string(class Generated809.G3_C1281`1<class Generated809.BaseClass0>)
		ldstr "G1_C7::Method7.MI.4816<System.Object>()"
		ldstr "class IBase2`2<class Generated809.BaseClass1,class Generated809.BaseClass0> on type class Generated809.G3_C1281`1<class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated809.IBase2`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::Method7<object>()
		calli default string(class Generated809.G3_C1281`1<class Generated809.BaseClass0>)
		ldstr "G1_C7::Method7.MI.4816<System.Object>()"
		ldstr "class IBase2`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G3_C1281`1<class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated809.IBase2`2<class Generated809.BaseClass0,class Generated809.BaseClass1>::Method7<object>()
		calli default string(class Generated809.G3_C1281`1<class Generated809.BaseClass0>)
		ldstr "G2_C327::Method7.7798<System.Object>()"
		ldstr "class IBase2`2<class Generated809.BaseClass0,class Generated809.BaseClass1> on type class Generated809.G3_C1281`1<class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated809.IBase2`2<class Generated809.BaseClass1,class Generated809.BaseClass1>::Method7<object>()
		calli default string(class Generated809.G3_C1281`1<class Generated809.BaseClass0>)
		ldstr "G2_C327::Method7.7798<System.Object>()"
		ldstr "class IBase2`2<class Generated809.BaseClass1,class Generated809.BaseClass1> on type class Generated809.G3_C1281`1<class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::ClassMethod1996<object>()
		calli default string(class Generated809.G3_C1281`1<class Generated809.BaseClass0>)
		ldstr "G2_C327::ClassMethod1996.7807<System.Object>()"
		ldstr "class G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G3_C1281`1<class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::ClassMethod1995<object>()
		calli default string(class Generated809.G3_C1281`1<class Generated809.BaseClass0>)
		ldstr "G2_C327::ClassMethod1995.7806<System.Object>()"
		ldstr "class G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G3_C1281`1<class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::ClassMethod1994()
		calli default string(class Generated809.G3_C1281`1<class Generated809.BaseClass0>)
		ldstr "G3_C1281::ClassMethod1994.MI.15322()"
		ldstr "class G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G3_C1281`1<class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::ClassMethod1993()
		calli default string(class Generated809.G3_C1281`1<class Generated809.BaseClass0>)
		ldstr "G3_C1281::ClassMethod1993.MI.15321()"
		ldstr "class G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G3_C1281`1<class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::Method6<object>()
		calli default string(class Generated809.G3_C1281`1<class Generated809.BaseClass0>)
		ldstr "G2_C327::Method6.7802<System.Object>()"
		ldstr "class G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G3_C1281`1<class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::Method5()
		calli default string(class Generated809.G3_C1281`1<class Generated809.BaseClass0>)
		ldstr "G3_C1281::Method5.15316()"
		ldstr "class G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G3_C1281`1<class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::Method4()
		calli default string(class Generated809.G3_C1281`1<class Generated809.BaseClass0>)
		ldstr "G3_C1281::Method4.15314()"
		ldstr "class G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G3_C1281`1<class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::Method7<object>()
		calli default string(class Generated809.G3_C1281`1<class Generated809.BaseClass0>)
		ldstr "G2_C327::Method7.7798<System.Object>()"
		ldstr "class G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G3_C1281`1<class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::ClassMethod1331<object>()
		calli default string(class Generated809.G3_C1281`1<class Generated809.BaseClass0>)
		ldstr "G2_C327::ClassMethod1331.MI.7808<System.Object>()"
		ldstr "class G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G3_C1281`1<class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::ClassMethod1330<object>()
		calli default string(class Generated809.G3_C1281`1<class Generated809.BaseClass0>)
		ldstr "G1_C7::ClassMethod1330.4819<System.Object>()"
		ldstr "class G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G3_C1281`1<class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::ClassMethod1329()
		calli default string(class Generated809.G3_C1281`1<class Generated809.BaseClass0>)
		ldstr "G1_C7::ClassMethod1329.4818()"
		ldstr "class G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G3_C1281`1<class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::ClassMethod1328()
		calli default string(class Generated809.G3_C1281`1<class Generated809.BaseClass0>)
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G3_C1281`1<class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G3_C1281`1<class Generated809.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated809.G3_C1281`1<class Generated809.BaseClass0>::ClassMethod3939<object>()
		calli default string(class Generated809.G3_C1281`1<class Generated809.BaseClass0>)
		ldstr "G3_C1281::ClassMethod3939.15320<System.Object>()"
		ldstr "class G3_C1281`1<class Generated809.BaseClass0> on type class Generated809.G3_C1281`1<class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G3_C1281`1<class Generated809.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated809.G3_C1281`1<class Generated809.BaseClass0>::ClassMethod3938()
		calli default string(class Generated809.G3_C1281`1<class Generated809.BaseClass0>)
		ldstr "G3_C1281::ClassMethod3938.15319()"
		ldstr "class G3_C1281`1<class Generated809.BaseClass0> on type class Generated809.G3_C1281`1<class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G3_C1281`1<class Generated809.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated809.G3_C1281`1<class Generated809.BaseClass0>::Method6<object>()
		calli default string(class Generated809.G3_C1281`1<class Generated809.BaseClass0>)
		ldstr "G3_C1281::Method6.15317<System.Object>()"
		ldstr "class G3_C1281`1<class Generated809.BaseClass0> on type class Generated809.G3_C1281`1<class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G3_C1281`1<class Generated809.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated809.G3_C1281`1<class Generated809.BaseClass0>::Method5()
		calli default string(class Generated809.G3_C1281`1<class Generated809.BaseClass0>)
		ldstr "G3_C1281::Method5.15316()"
		ldstr "class G3_C1281`1<class Generated809.BaseClass0> on type class Generated809.G3_C1281`1<class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G3_C1281`1<class Generated809.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated809.G3_C1281`1<class Generated809.BaseClass0>::Method4()
		calli default string(class Generated809.G3_C1281`1<class Generated809.BaseClass0>)
		ldstr "G3_C1281::Method4.15314()"
		ldstr "class G3_C1281`1<class Generated809.BaseClass0> on type class Generated809.G3_C1281`1<class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G3_C1281`1<class Generated809.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated809.G3_C1281`1<class Generated809.BaseClass0>::ClassMethod1996<object>()
		calli default string(class Generated809.G3_C1281`1<class Generated809.BaseClass0>)
		ldstr "G2_C327::ClassMethod1996.7807<System.Object>()"
		ldstr "class G3_C1281`1<class Generated809.BaseClass0> on type class Generated809.G3_C1281`1<class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G3_C1281`1<class Generated809.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated809.G3_C1281`1<class Generated809.BaseClass0>::ClassMethod1995<object>()
		calli default string(class Generated809.G3_C1281`1<class Generated809.BaseClass0>)
		ldstr "G2_C327::ClassMethod1995.7806<System.Object>()"
		ldstr "class G3_C1281`1<class Generated809.BaseClass0> on type class Generated809.G3_C1281`1<class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G3_C1281`1<class Generated809.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated809.G3_C1281`1<class Generated809.BaseClass0>::ClassMethod1994()
		calli default string(class Generated809.G3_C1281`1<class Generated809.BaseClass0>)
		ldstr "G3_C1281::ClassMethod1994.MI.15322()"
		ldstr "class G3_C1281`1<class Generated809.BaseClass0> on type class Generated809.G3_C1281`1<class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G3_C1281`1<class Generated809.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated809.G3_C1281`1<class Generated809.BaseClass0>::ClassMethod1993()
		calli default string(class Generated809.G3_C1281`1<class Generated809.BaseClass0>)
		ldstr "G3_C1281::ClassMethod1993.MI.15321()"
		ldstr "class G3_C1281`1<class Generated809.BaseClass0> on type class Generated809.G3_C1281`1<class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G3_C1281`1<class Generated809.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated809.G3_C1281`1<class Generated809.BaseClass0>::Method7<object>()
		calli default string(class Generated809.G3_C1281`1<class Generated809.BaseClass0>)
		ldstr "G2_C327::Method7.7798<System.Object>()"
		ldstr "class G3_C1281`1<class Generated809.BaseClass0> on type class Generated809.G3_C1281`1<class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G3_C1281`1<class Generated809.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated809.G3_C1281`1<class Generated809.BaseClass0>::ClassMethod1331<object>()
		calli default string(class Generated809.G3_C1281`1<class Generated809.BaseClass0>)
		ldstr "G2_C327::ClassMethod1331.MI.7808<System.Object>()"
		ldstr "class G3_C1281`1<class Generated809.BaseClass0> on type class Generated809.G3_C1281`1<class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G3_C1281`1<class Generated809.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated809.G3_C1281`1<class Generated809.BaseClass0>::ClassMethod1330<object>()
		calli default string(class Generated809.G3_C1281`1<class Generated809.BaseClass0>)
		ldstr "G1_C7::ClassMethod1330.4819<System.Object>()"
		ldstr "class G3_C1281`1<class Generated809.BaseClass0> on type class Generated809.G3_C1281`1<class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G3_C1281`1<class Generated809.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated809.G3_C1281`1<class Generated809.BaseClass0>::ClassMethod1329()
		calli default string(class Generated809.G3_C1281`1<class Generated809.BaseClass0>)
		ldstr "G1_C7::ClassMethod1329.4818()"
		ldstr "class G3_C1281`1<class Generated809.BaseClass0> on type class Generated809.G3_C1281`1<class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G3_C1281`1<class Generated809.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated809.G3_C1281`1<class Generated809.BaseClass0>::ClassMethod1328()
		calli default string(class Generated809.G3_C1281`1<class Generated809.BaseClass0>)
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G3_C1281`1<class Generated809.BaseClass0> on type class Generated809.G3_C1281`1<class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated809.G3_C1281`1<class Generated809.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::ClassMethod1331<object>()
		calli default string(class Generated809.G3_C1281`1<class Generated809.BaseClass1>)
		ldstr "G2_C327::ClassMethod1331.MI.7808<System.Object>()"
		ldstr "class G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G3_C1281`1<class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::ClassMethod1330<object>()
		calli default string(class Generated809.G3_C1281`1<class Generated809.BaseClass1>)
		ldstr "G1_C7::ClassMethod1330.4819<System.Object>()"
		ldstr "class G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G3_C1281`1<class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::ClassMethod1329()
		calli default string(class Generated809.G3_C1281`1<class Generated809.BaseClass1>)
		ldstr "G1_C7::ClassMethod1329.4818()"
		ldstr "class G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G3_C1281`1<class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::ClassMethod1328()
		calli default string(class Generated809.G3_C1281`1<class Generated809.BaseClass1>)
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G3_C1281`1<class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::Method7<object>()
		calli default string(class Generated809.G3_C1281`1<class Generated809.BaseClass1>)
		ldstr "G1_C7::Method7.4815<System.Object>()"
		ldstr "class G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G3_C1281`1<class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::Method6<object>()
		calli default string(class Generated809.G3_C1281`1<class Generated809.BaseClass1>)
		ldstr "G2_C327::Method6.7802<System.Object>()"
		ldstr "class G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G3_C1281`1<class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::Method5()
		calli default string(class Generated809.G3_C1281`1<class Generated809.BaseClass1>)
		ldstr "G3_C1281::Method5.15316()"
		ldstr "class G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G3_C1281`1<class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::Method4()
		calli default string(class Generated809.G3_C1281`1<class Generated809.BaseClass1>)
		ldstr "G1_C7::Method4.4811()"
		ldstr "class G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G3_C1281`1<class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated809.IBase1`1<class Generated809.BaseClass0>::Method4()
		calli default string(class Generated809.G3_C1281`1<class Generated809.BaseClass1>)
		ldstr "G3_C1281::Method4.MI.15315()"
		ldstr "class IBase1`1<class Generated809.BaseClass0> on type class Generated809.G3_C1281`1<class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated809.IBase1`1<class Generated809.BaseClass0>::Method5()
		calli default string(class Generated809.G3_C1281`1<class Generated809.BaseClass1>)
		ldstr "G3_C1281::Method5.15316()"
		ldstr "class IBase1`1<class Generated809.BaseClass0> on type class Generated809.G3_C1281`1<class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated809.IBase1`1<class Generated809.BaseClass0>::Method6<object>()
		calli default string(class Generated809.G3_C1281`1<class Generated809.BaseClass1>)
		ldstr "G3_C1281::Method6.MI.15318<System.Object>()"
		ldstr "class IBase1`1<class Generated809.BaseClass0> on type class Generated809.G3_C1281`1<class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated809.IBase2`2<class Generated809.BaseClass1,class Generated809.BaseClass0>::Method7<object>()
		calli default string(class Generated809.G3_C1281`1<class Generated809.BaseClass1>)
		ldstr "G1_C7::Method7.MI.4816<System.Object>()"
		ldstr "class IBase2`2<class Generated809.BaseClass1,class Generated809.BaseClass0> on type class Generated809.G3_C1281`1<class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated809.IBase2`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::Method7<object>()
		calli default string(class Generated809.G3_C1281`1<class Generated809.BaseClass1>)
		ldstr "G1_C7::Method7.MI.4816<System.Object>()"
		ldstr "class IBase2`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G3_C1281`1<class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated809.IBase2`2<class Generated809.BaseClass0,class Generated809.BaseClass1>::Method7<object>()
		calli default string(class Generated809.G3_C1281`1<class Generated809.BaseClass1>)
		ldstr "G2_C327::Method7.7798<System.Object>()"
		ldstr "class IBase2`2<class Generated809.BaseClass0,class Generated809.BaseClass1> on type class Generated809.G3_C1281`1<class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated809.IBase2`2<class Generated809.BaseClass1,class Generated809.BaseClass1>::Method7<object>()
		calli default string(class Generated809.G3_C1281`1<class Generated809.BaseClass1>)
		ldstr "G2_C327::Method7.7798<System.Object>()"
		ldstr "class IBase2`2<class Generated809.BaseClass1,class Generated809.BaseClass1> on type class Generated809.G3_C1281`1<class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>::ClassMethod1996<object>()
		calli default string(class Generated809.G3_C1281`1<class Generated809.BaseClass1>)
		ldstr "G2_C327::ClassMethod1996.7807<System.Object>()"
		ldstr "class G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1> on type class Generated809.G3_C1281`1<class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>::ClassMethod1995<object>()
		calli default string(class Generated809.G3_C1281`1<class Generated809.BaseClass1>)
		ldstr "G2_C327::ClassMethod1995.7806<System.Object>()"
		ldstr "class G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1> on type class Generated809.G3_C1281`1<class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>::ClassMethod1994()
		calli default string(class Generated809.G3_C1281`1<class Generated809.BaseClass1>)
		ldstr "G3_C1281::ClassMethod1994.MI.15322()"
		ldstr "class G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1> on type class Generated809.G3_C1281`1<class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>::ClassMethod1993()
		calli default string(class Generated809.G3_C1281`1<class Generated809.BaseClass1>)
		ldstr "G3_C1281::ClassMethod1993.MI.15321()"
		ldstr "class G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1> on type class Generated809.G3_C1281`1<class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>::Method6<object>()
		calli default string(class Generated809.G3_C1281`1<class Generated809.BaseClass1>)
		ldstr "G2_C327::Method6.7802<System.Object>()"
		ldstr "class G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1> on type class Generated809.G3_C1281`1<class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>::Method5()
		calli default string(class Generated809.G3_C1281`1<class Generated809.BaseClass1>)
		ldstr "G3_C1281::Method5.15316()"
		ldstr "class G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1> on type class Generated809.G3_C1281`1<class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>::Method4()
		calli default string(class Generated809.G3_C1281`1<class Generated809.BaseClass1>)
		ldstr "G3_C1281::Method4.15314()"
		ldstr "class G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1> on type class Generated809.G3_C1281`1<class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>::Method7<object>()
		calli default string(class Generated809.G3_C1281`1<class Generated809.BaseClass1>)
		ldstr "G2_C327::Method7.7798<System.Object>()"
		ldstr "class G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1> on type class Generated809.G3_C1281`1<class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>::ClassMethod1331<object>()
		calli default string(class Generated809.G3_C1281`1<class Generated809.BaseClass1>)
		ldstr "G2_C327::ClassMethod1331.MI.7808<System.Object>()"
		ldstr "class G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1> on type class Generated809.G3_C1281`1<class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>::ClassMethod1330<object>()
		calli default string(class Generated809.G3_C1281`1<class Generated809.BaseClass1>)
		ldstr "G1_C7::ClassMethod1330.4819<System.Object>()"
		ldstr "class G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1> on type class Generated809.G3_C1281`1<class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>::ClassMethod1329()
		calli default string(class Generated809.G3_C1281`1<class Generated809.BaseClass1>)
		ldstr "G1_C7::ClassMethod1329.4818()"
		ldstr "class G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1> on type class Generated809.G3_C1281`1<class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>::ClassMethod1328()
		calli default string(class Generated809.G3_C1281`1<class Generated809.BaseClass1>)
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1> on type class Generated809.G3_C1281`1<class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G3_C1281`1<class Generated809.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated809.G3_C1281`1<class Generated809.BaseClass1>::ClassMethod3939<object>()
		calli default string(class Generated809.G3_C1281`1<class Generated809.BaseClass1>)
		ldstr "G3_C1281::ClassMethod3939.15320<System.Object>()"
		ldstr "class G3_C1281`1<class Generated809.BaseClass1> on type class Generated809.G3_C1281`1<class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G3_C1281`1<class Generated809.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated809.G3_C1281`1<class Generated809.BaseClass1>::ClassMethod3938()
		calli default string(class Generated809.G3_C1281`1<class Generated809.BaseClass1>)
		ldstr "G3_C1281::ClassMethod3938.15319()"
		ldstr "class G3_C1281`1<class Generated809.BaseClass1> on type class Generated809.G3_C1281`1<class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G3_C1281`1<class Generated809.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated809.G3_C1281`1<class Generated809.BaseClass1>::Method6<object>()
		calli default string(class Generated809.G3_C1281`1<class Generated809.BaseClass1>)
		ldstr "G3_C1281::Method6.15317<System.Object>()"
		ldstr "class G3_C1281`1<class Generated809.BaseClass1> on type class Generated809.G3_C1281`1<class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G3_C1281`1<class Generated809.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated809.G3_C1281`1<class Generated809.BaseClass1>::Method5()
		calli default string(class Generated809.G3_C1281`1<class Generated809.BaseClass1>)
		ldstr "G3_C1281::Method5.15316()"
		ldstr "class G3_C1281`1<class Generated809.BaseClass1> on type class Generated809.G3_C1281`1<class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G3_C1281`1<class Generated809.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated809.G3_C1281`1<class Generated809.BaseClass1>::Method4()
		calli default string(class Generated809.G3_C1281`1<class Generated809.BaseClass1>)
		ldstr "G3_C1281::Method4.15314()"
		ldstr "class G3_C1281`1<class Generated809.BaseClass1> on type class Generated809.G3_C1281`1<class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G3_C1281`1<class Generated809.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated809.G3_C1281`1<class Generated809.BaseClass1>::ClassMethod1996<object>()
		calli default string(class Generated809.G3_C1281`1<class Generated809.BaseClass1>)
		ldstr "G2_C327::ClassMethod1996.7807<System.Object>()"
		ldstr "class G3_C1281`1<class Generated809.BaseClass1> on type class Generated809.G3_C1281`1<class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G3_C1281`1<class Generated809.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated809.G3_C1281`1<class Generated809.BaseClass1>::ClassMethod1995<object>()
		calli default string(class Generated809.G3_C1281`1<class Generated809.BaseClass1>)
		ldstr "G2_C327::ClassMethod1995.7806<System.Object>()"
		ldstr "class G3_C1281`1<class Generated809.BaseClass1> on type class Generated809.G3_C1281`1<class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G3_C1281`1<class Generated809.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated809.G3_C1281`1<class Generated809.BaseClass1>::ClassMethod1994()
		calli default string(class Generated809.G3_C1281`1<class Generated809.BaseClass1>)
		ldstr "G3_C1281::ClassMethod1994.MI.15322()"
		ldstr "class G3_C1281`1<class Generated809.BaseClass1> on type class Generated809.G3_C1281`1<class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G3_C1281`1<class Generated809.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated809.G3_C1281`1<class Generated809.BaseClass1>::ClassMethod1993()
		calli default string(class Generated809.G3_C1281`1<class Generated809.BaseClass1>)
		ldstr "G3_C1281::ClassMethod1993.MI.15321()"
		ldstr "class G3_C1281`1<class Generated809.BaseClass1> on type class Generated809.G3_C1281`1<class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G3_C1281`1<class Generated809.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated809.G3_C1281`1<class Generated809.BaseClass1>::Method7<object>()
		calli default string(class Generated809.G3_C1281`1<class Generated809.BaseClass1>)
		ldstr "G2_C327::Method7.7798<System.Object>()"
		ldstr "class G3_C1281`1<class Generated809.BaseClass1> on type class Generated809.G3_C1281`1<class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G3_C1281`1<class Generated809.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated809.G3_C1281`1<class Generated809.BaseClass1>::ClassMethod1331<object>()
		calli default string(class Generated809.G3_C1281`1<class Generated809.BaseClass1>)
		ldstr "G2_C327::ClassMethod1331.MI.7808<System.Object>()"
		ldstr "class G3_C1281`1<class Generated809.BaseClass1> on type class Generated809.G3_C1281`1<class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G3_C1281`1<class Generated809.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated809.G3_C1281`1<class Generated809.BaseClass1>::ClassMethod1330<object>()
		calli default string(class Generated809.G3_C1281`1<class Generated809.BaseClass1>)
		ldstr "G1_C7::ClassMethod1330.4819<System.Object>()"
		ldstr "class G3_C1281`1<class Generated809.BaseClass1> on type class Generated809.G3_C1281`1<class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G3_C1281`1<class Generated809.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated809.G3_C1281`1<class Generated809.BaseClass1>::ClassMethod1329()
		calli default string(class Generated809.G3_C1281`1<class Generated809.BaseClass1>)
		ldstr "G1_C7::ClassMethod1329.4818()"
		ldstr "class G3_C1281`1<class Generated809.BaseClass1> on type class Generated809.G3_C1281`1<class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G3_C1281`1<class Generated809.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated809.G3_C1281`1<class Generated809.BaseClass1>::ClassMethod1328()
		calli default string(class Generated809.G3_C1281`1<class Generated809.BaseClass1>)
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G3_C1281`1<class Generated809.BaseClass1> on type class Generated809.G3_C1281`1<class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated809.IBase1`1<class Generated809.BaseClass1>::Method4()
		calli default string(class Generated809.G3_C1281`1<class Generated809.BaseClass1>)
		ldstr "G3_C1281::Method4.MI.15315()"
		ldstr "class IBase1`1<class Generated809.BaseClass1> on type class Generated809.G3_C1281`1<class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated809.IBase1`1<class Generated809.BaseClass1>::Method5()
		calli default string(class Generated809.G3_C1281`1<class Generated809.BaseClass1>)
		ldstr "G3_C1281::Method5.15316()"
		ldstr "class IBase1`1<class Generated809.BaseClass1> on type class Generated809.G3_C1281`1<class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated809.IBase1`1<class Generated809.BaseClass1>::Method6<object>()
		calli default string(class Generated809.G3_C1281`1<class Generated809.BaseClass1>)
		ldstr "G3_C1281::Method6.MI.15318<System.Object>()"
		ldstr "class IBase1`1<class Generated809.BaseClass1> on type class Generated809.G3_C1281`1<class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::ClassMethod1331<object>()
		calli default string(class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>)
		ldstr "G2_C327::ClassMethod1331.MI.7808<System.Object>()"
		ldstr "class G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::ClassMethod1330<object>()
		calli default string(class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>)
		ldstr "G1_C7::ClassMethod1330.4819<System.Object>()"
		ldstr "class G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::ClassMethod1329()
		calli default string(class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>)
		ldstr "G1_C7::ClassMethod1329.4818()"
		ldstr "class G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::ClassMethod1328()
		calli default string(class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>)
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::Method7<object>()
		calli default string(class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>)
		ldstr "G1_C7::Method7.4815<System.Object>()"
		ldstr "class G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::Method6<object>()
		calli default string(class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>)
		ldstr "G2_C327::Method6.7802<System.Object>()"
		ldstr "class G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::Method5()
		calli default string(class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>)
		ldstr "G2_C327::Method5.7800()"
		ldstr "class G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::Method4()
		calli default string(class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>)
		ldstr "G1_C7::Method4.4811()"
		ldstr "class G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated809.IBase1`1<class Generated809.BaseClass0>::Method4()
		calli default string(class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>)
		ldstr "G2_C327::Method4.7799()"
		ldstr "class IBase1`1<class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated809.IBase1`1<class Generated809.BaseClass0>::Method5()
		calli default string(class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>)
		ldstr "G2_C327::Method5.MI.7801()"
		ldstr "class IBase1`1<class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated809.IBase1`1<class Generated809.BaseClass0>::Method6<object>()
		calli default string(class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>)
		ldstr "G2_C327::Method6.MI.7803<System.Object>()"
		ldstr "class IBase1`1<class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated809.IBase2`2<class Generated809.BaseClass1,class Generated809.BaseClass0>::Method7<object>()
		calli default string(class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>)
		ldstr "G1_C7::Method7.MI.4816<System.Object>()"
		ldstr "class IBase2`2<class Generated809.BaseClass1,class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated809.IBase2`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::Method7<object>()
		calli default string(class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>)
		ldstr "G1_C7::Method7.MI.4816<System.Object>()"
		ldstr "class IBase2`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated809.IBase2`2<class Generated809.BaseClass0,class Generated809.BaseClass1>::Method7<object>()
		calli default string(class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>)
		ldstr "G2_C327::Method7.7798<System.Object>()"
		ldstr "class IBase2`2<class Generated809.BaseClass0,class Generated809.BaseClass1> on type class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated809.IBase2`2<class Generated809.BaseClass1,class Generated809.BaseClass1>::Method7<object>()
		calli default string(class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>)
		ldstr "G2_C327::Method7.7798<System.Object>()"
		ldstr "class IBase2`2<class Generated809.BaseClass1,class Generated809.BaseClass1> on type class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::ClassMethod1996<object>()
		calli default string(class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>)
		ldstr "G2_C327::ClassMethod1996.7807<System.Object>()"
		ldstr "class G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::ClassMethod1995<object>()
		calli default string(class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>)
		ldstr "G2_C327::ClassMethod1995.7806<System.Object>()"
		ldstr "class G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::ClassMethod1994()
		calli default string(class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>)
		ldstr "G2_C327::ClassMethod1994.7805()"
		ldstr "class G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::ClassMethod1993()
		calli default string(class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>)
		ldstr "G2_C327::ClassMethod1993.7804()"
		ldstr "class G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::Method6<object>()
		calli default string(class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>)
		ldstr "G2_C327::Method6.7802<System.Object>()"
		ldstr "class G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::Method5()
		calli default string(class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>)
		ldstr "G2_C327::Method5.7800()"
		ldstr "class G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::Method4()
		calli default string(class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>)
		ldstr "G2_C327::Method4.7799()"
		ldstr "class G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::Method7<object>()
		calli default string(class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>)
		ldstr "G2_C327::Method7.7798<System.Object>()"
		ldstr "class G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::ClassMethod1331<object>()
		calli default string(class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>)
		ldstr "G2_C327::ClassMethod1331.MI.7808<System.Object>()"
		ldstr "class G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::ClassMethod1330<object>()
		calli default string(class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>)
		ldstr "G1_C7::ClassMethod1330.4819<System.Object>()"
		ldstr "class G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::ClassMethod1329()
		calli default string(class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>)
		ldstr "G1_C7::ClassMethod1329.4818()"
		ldstr "class G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::ClassMethod1328()
		calli default string(class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>)
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::ClassMethod1331<object>()
		calli default string(class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>)
		ldstr "G2_C327::ClassMethod1331.MI.7808<System.Object>()"
		ldstr "class G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::ClassMethod1330<object>()
		calli default string(class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>)
		ldstr "G1_C7::ClassMethod1330.4819<System.Object>()"
		ldstr "class G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::ClassMethod1329()
		calli default string(class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>)
		ldstr "G1_C7::ClassMethod1329.4818()"
		ldstr "class G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::ClassMethod1328()
		calli default string(class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>)
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::Method7<object>()
		calli default string(class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>)
		ldstr "G1_C7::Method7.4815<System.Object>()"
		ldstr "class G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::Method6<object>()
		calli default string(class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>)
		ldstr "G2_C327::Method6.7802<System.Object>()"
		ldstr "class G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::Method5()
		calli default string(class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>)
		ldstr "G2_C327::Method5.7800()"
		ldstr "class G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::Method4()
		calli default string(class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>)
		ldstr "G1_C7::Method4.4811()"
		ldstr "class G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated809.IBase1`1<class Generated809.BaseClass0>::Method4()
		calli default string(class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>)
		ldstr "G2_C327::Method4.7799()"
		ldstr "class IBase1`1<class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated809.IBase1`1<class Generated809.BaseClass0>::Method5()
		calli default string(class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>)
		ldstr "G2_C327::Method5.MI.7801()"
		ldstr "class IBase1`1<class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated809.IBase1`1<class Generated809.BaseClass0>::Method6<object>()
		calli default string(class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>)
		ldstr "G2_C327::Method6.MI.7803<System.Object>()"
		ldstr "class IBase1`1<class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated809.IBase2`2<class Generated809.BaseClass1,class Generated809.BaseClass0>::Method7<object>()
		calli default string(class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>)
		ldstr "G1_C7::Method7.MI.4816<System.Object>()"
		ldstr "class IBase2`2<class Generated809.BaseClass1,class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated809.IBase2`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::Method7<object>()
		calli default string(class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>)
		ldstr "G1_C7::Method7.MI.4816<System.Object>()"
		ldstr "class IBase2`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated809.IBase2`2<class Generated809.BaseClass0,class Generated809.BaseClass1>::Method7<object>()
		calli default string(class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>)
		ldstr "G2_C327::Method7.7798<System.Object>()"
		ldstr "class IBase2`2<class Generated809.BaseClass0,class Generated809.BaseClass1> on type class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated809.IBase2`2<class Generated809.BaseClass1,class Generated809.BaseClass1>::Method7<object>()
		calli default string(class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>)
		ldstr "G2_C327::Method7.7798<System.Object>()"
		ldstr "class IBase2`2<class Generated809.BaseClass1,class Generated809.BaseClass1> on type class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>::ClassMethod1996<object>()
		calli default string(class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>)
		ldstr "G2_C327::ClassMethod1996.7807<System.Object>()"
		ldstr "class G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1> on type class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>::ClassMethod1995<object>()
		calli default string(class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>)
		ldstr "G2_C327::ClassMethod1995.7806<System.Object>()"
		ldstr "class G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1> on type class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>::ClassMethod1994()
		calli default string(class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>)
		ldstr "G2_C327::ClassMethod1994.7805()"
		ldstr "class G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1> on type class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>::ClassMethod1993()
		calli default string(class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>)
		ldstr "G2_C327::ClassMethod1993.7804()"
		ldstr "class G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1> on type class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>::Method6<object>()
		calli default string(class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>)
		ldstr "G2_C327::Method6.7802<System.Object>()"
		ldstr "class G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1> on type class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>::Method5()
		calli default string(class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>)
		ldstr "G2_C327::Method5.7800()"
		ldstr "class G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1> on type class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>::Method4()
		calli default string(class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>)
		ldstr "G2_C327::Method4.7799()"
		ldstr "class G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1> on type class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>::Method7<object>()
		calli default string(class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>)
		ldstr "G2_C327::Method7.7798<System.Object>()"
		ldstr "class G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1> on type class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>::ClassMethod1331<object>()
		calli default string(class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>)
		ldstr "G2_C327::ClassMethod1331.MI.7808<System.Object>()"
		ldstr "class G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1> on type class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>::ClassMethod1330<object>()
		calli default string(class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>)
		ldstr "G1_C7::ClassMethod1330.4819<System.Object>()"
		ldstr "class G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1> on type class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>::ClassMethod1329()
		calli default string(class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>)
		ldstr "G1_C7::ClassMethod1329.4818()"
		ldstr "class G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1> on type class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>::ClassMethod1328()
		calli default string(class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>)
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1> on type class Generated809.G2_C327`2<class Generated809.BaseClass0,class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::ClassMethod1331<object>()
		calli default string(class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>)
		ldstr "G2_C327::ClassMethod1331.MI.7808<System.Object>()"
		ldstr "class G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::ClassMethod1330<object>()
		calli default string(class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>)
		ldstr "G1_C7::ClassMethod1330.4819<System.Object>()"
		ldstr "class G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::ClassMethod1329()
		calli default string(class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>)
		ldstr "G1_C7::ClassMethod1329.4818()"
		ldstr "class G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::ClassMethod1328()
		calli default string(class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>)
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::Method7<object>()
		calli default string(class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>)
		ldstr "G1_C7::Method7.4815<System.Object>()"
		ldstr "class G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::Method6<object>()
		calli default string(class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>)
		ldstr "G2_C327::Method6.7802<System.Object>()"
		ldstr "class G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::Method5()
		calli default string(class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>)
		ldstr "G2_C327::Method5.7800()"
		ldstr "class G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::Method4()
		calli default string(class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>)
		ldstr "G1_C7::Method4.4811()"
		ldstr "class G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated809.IBase1`1<class Generated809.BaseClass0>::Method4()
		calli default string(class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>)
		ldstr "G2_C327::Method4.7799()"
		ldstr "class IBase1`1<class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated809.IBase1`1<class Generated809.BaseClass0>::Method5()
		calli default string(class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>)
		ldstr "G2_C327::Method5.MI.7801()"
		ldstr "class IBase1`1<class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated809.IBase1`1<class Generated809.BaseClass0>::Method6<object>()
		calli default string(class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>)
		ldstr "G2_C327::Method6.MI.7803<System.Object>()"
		ldstr "class IBase1`1<class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated809.IBase2`2<class Generated809.BaseClass1,class Generated809.BaseClass0>::Method7<object>()
		calli default string(class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>)
		ldstr "G1_C7::Method7.MI.4816<System.Object>()"
		ldstr "class IBase2`2<class Generated809.BaseClass1,class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated809.IBase2`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::Method7<object>()
		calli default string(class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>)
		ldstr "G1_C7::Method7.MI.4816<System.Object>()"
		ldstr "class IBase2`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated809.IBase2`2<class Generated809.BaseClass0,class Generated809.BaseClass1>::Method7<object>()
		calli default string(class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>)
		ldstr "G2_C327::Method7.7798<System.Object>()"
		ldstr "class IBase2`2<class Generated809.BaseClass0,class Generated809.BaseClass1> on type class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated809.IBase2`2<class Generated809.BaseClass1,class Generated809.BaseClass1>::Method7<object>()
		calli default string(class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>)
		ldstr "G2_C327::Method7.7798<System.Object>()"
		ldstr "class IBase2`2<class Generated809.BaseClass1,class Generated809.BaseClass1> on type class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>::ClassMethod1996<object>()
		calli default string(class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>)
		ldstr "G2_C327::ClassMethod1996.7807<System.Object>()"
		ldstr "class G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>::ClassMethod1995<object>()
		calli default string(class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>)
		ldstr "G2_C327::ClassMethod1995.7806<System.Object>()"
		ldstr "class G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>::ClassMethod1994()
		calli default string(class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>)
		ldstr "G2_C327::ClassMethod1994.7805()"
		ldstr "class G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>::ClassMethod1993()
		calli default string(class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>)
		ldstr "G2_C327::ClassMethod1993.7804()"
		ldstr "class G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>::Method6<object>()
		calli default string(class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>)
		ldstr "G2_C327::Method6.7802<System.Object>()"
		ldstr "class G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>::Method5()
		calli default string(class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>)
		ldstr "G2_C327::Method5.7800()"
		ldstr "class G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>::Method4()
		calli default string(class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>)
		ldstr "G2_C327::Method4.7799()"
		ldstr "class G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>::Method7<object>()
		calli default string(class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>)
		ldstr "G2_C327::Method7.7798<System.Object>()"
		ldstr "class G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>::ClassMethod1331<object>()
		calli default string(class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>)
		ldstr "G2_C327::ClassMethod1331.MI.7808<System.Object>()"
		ldstr "class G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>::ClassMethod1330<object>()
		calli default string(class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>)
		ldstr "G1_C7::ClassMethod1330.4819<System.Object>()"
		ldstr "class G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>::ClassMethod1329()
		calli default string(class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>)
		ldstr "G1_C7::ClassMethod1329.4818()"
		ldstr "class G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>::ClassMethod1328()
		calli default string(class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>)
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::ClassMethod1331<object>()
		calli default string(class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>)
		ldstr "G2_C327::ClassMethod1331.MI.7808<System.Object>()"
		ldstr "class G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::ClassMethod1330<object>()
		calli default string(class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>)
		ldstr "G1_C7::ClassMethod1330.4819<System.Object>()"
		ldstr "class G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::ClassMethod1329()
		calli default string(class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>)
		ldstr "G1_C7::ClassMethod1329.4818()"
		ldstr "class G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::ClassMethod1328()
		calli default string(class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>)
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::Method7<object>()
		calli default string(class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>)
		ldstr "G1_C7::Method7.4815<System.Object>()"
		ldstr "class G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::Method6<object>()
		calli default string(class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>)
		ldstr "G2_C327::Method6.7802<System.Object>()"
		ldstr "class G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::Method5()
		calli default string(class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>)
		ldstr "G2_C327::Method5.7800()"
		ldstr "class G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated809.G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::Method4()
		calli default string(class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>)
		ldstr "G1_C7::Method4.4811()"
		ldstr "class G1_C7`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated809.IBase1`1<class Generated809.BaseClass0>::Method4()
		calli default string(class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>)
		ldstr "G2_C327::Method4.7799()"
		ldstr "class IBase1`1<class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated809.IBase1`1<class Generated809.BaseClass0>::Method5()
		calli default string(class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>)
		ldstr "G2_C327::Method5.MI.7801()"
		ldstr "class IBase1`1<class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated809.IBase1`1<class Generated809.BaseClass0>::Method6<object>()
		calli default string(class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>)
		ldstr "G2_C327::Method6.MI.7803<System.Object>()"
		ldstr "class IBase1`1<class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated809.IBase2`2<class Generated809.BaseClass1,class Generated809.BaseClass0>::Method7<object>()
		calli default string(class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>)
		ldstr "G1_C7::Method7.MI.4816<System.Object>()"
		ldstr "class IBase2`2<class Generated809.BaseClass1,class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated809.IBase2`2<class Generated809.BaseClass0,class Generated809.BaseClass0>::Method7<object>()
		calli default string(class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>)
		ldstr "G1_C7::Method7.MI.4816<System.Object>()"
		ldstr "class IBase2`2<class Generated809.BaseClass0,class Generated809.BaseClass0> on type class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated809.IBase2`2<class Generated809.BaseClass0,class Generated809.BaseClass1>::Method7<object>()
		calli default string(class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>)
		ldstr "G2_C327::Method7.7798<System.Object>()"
		ldstr "class IBase2`2<class Generated809.BaseClass0,class Generated809.BaseClass1> on type class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated809.IBase2`2<class Generated809.BaseClass1,class Generated809.BaseClass1>::Method7<object>()
		calli default string(class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>)
		ldstr "G2_C327::Method7.7798<System.Object>()"
		ldstr "class IBase2`2<class Generated809.BaseClass1,class Generated809.BaseClass1> on type class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>::ClassMethod1996<object>()
		calli default string(class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>)
		ldstr "G2_C327::ClassMethod1996.7807<System.Object>()"
		ldstr "class G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1> on type class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>::ClassMethod1995<object>()
		calli default string(class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>)
		ldstr "G2_C327::ClassMethod1995.7806<System.Object>()"
		ldstr "class G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1> on type class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>::ClassMethod1994()
		calli default string(class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>)
		ldstr "G2_C327::ClassMethod1994.7805()"
		ldstr "class G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1> on type class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>::ClassMethod1993()
		calli default string(class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>)
		ldstr "G2_C327::ClassMethod1993.7804()"
		ldstr "class G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1> on type class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>::Method6<object>()
		calli default string(class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>)
		ldstr "G2_C327::Method6.7802<System.Object>()"
		ldstr "class G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1> on type class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>::Method5()
		calli default string(class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>)
		ldstr "G2_C327::Method5.7800()"
		ldstr "class G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1> on type class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>::Method4()
		calli default string(class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>)
		ldstr "G2_C327::Method4.7799()"
		ldstr "class G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1> on type class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>::Method7<object>()
		calli default string(class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>)
		ldstr "G2_C327::Method7.7798<System.Object>()"
		ldstr "class G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1> on type class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>::ClassMethod1331<object>()
		calli default string(class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>)
		ldstr "G2_C327::ClassMethod1331.MI.7808<System.Object>()"
		ldstr "class G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1> on type class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>::ClassMethod1330<object>()
		calli default string(class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>)
		ldstr "G1_C7::ClassMethod1330.4819<System.Object>()"
		ldstr "class G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1> on type class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>::ClassMethod1329()
		calli default string(class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>)
		ldstr "G1_C7::ClassMethod1329.4818()"
		ldstr "class G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1> on type class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>::ClassMethod1328()
		calli default string(class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>)
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1> on type class Generated809.G2_C327`2<class Generated809.BaseClass1,class Generated809.BaseClass1>"
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
		call void Generated809.Generated809::MethodCallingTest()
		call void Generated809.Generated809::ConstrainedCallsTest()
		call void Generated809.Generated809::StructConstrainedInterfaceCallsTest()
		call void Generated809.Generated809::CalliTest()
		ldc.i4 100
		ret
	}
}
