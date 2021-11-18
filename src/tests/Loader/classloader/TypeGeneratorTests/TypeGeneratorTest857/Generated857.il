// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) .ver 4:0:0:0 }
.assembly extern TestFramework { .publickeytoken = ( B0 3F 5F 7F 11 D5 0A 3A )  }

//TYPES IN FORWARDER ASSEMBLIES:

//TEST ASSEMBLY:
.assembly Generated857 { .hash algorithm 0x00008004 }
.assembly extern xunit.core {}

.class public BaseClass0 
{
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void [mscorlib]System.Object::.ctor()
		ret
	}
}
.class public BaseClass1 
		extends BaseClass0
{
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void BaseClass0::.ctor()
		ret
	}
}
.class public G3_C1329`1<T0> 
		extends class G2_C362`2<class BaseClass0,!T0>
		implements class IBase1`1<class BaseClass0> 
{
	.method public hidebysig newslot virtual instance string Method4() cil managed noinlining { 
		ldstr "G3_C1329::Method4.15577()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<class BaseClass0>.Method4'() cil managed noinlining { 
		.override method instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G3_C1329::Method4.MI.15578()"
		ret
	}
	.method public hidebysig virtual instance string Method5() cil managed noinlining { 
		ldstr "G3_C1329::Method5.15579()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<class BaseClass0>.Method5'() cil managed noinlining { 
		.override method instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G3_C1329::Method5.MI.15580()"
		ret
	}
	.method public hidebysig newslot virtual instance string Method6<M0>() cil managed noinlining { 
		ldstr "G3_C1329::Method6.15581<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod4039() cil managed noinlining { 
		ldstr "G3_C1329::ClassMethod4039.15582()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod4040() cil managed noinlining { 
		ldstr "G3_C1329::ClassMethod4040.15583()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod4041<M0>() cil managed noinlining { 
		ldstr "G3_C1329::ClassMethod4041.15584<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod4042<M0>() cil managed noinlining { 
		ldstr "G3_C1329::ClassMethod4042.15585<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'G2_C362<class BaseClass0,T0>.ClassMethod2065'<M0>() cil managed noinlining { 
		.override method instance string class G2_C362`2<class BaseClass0,!T0>::ClassMethod2065<[1]>()
		ldstr "G3_C1329::ClassMethod2065.MI.15586<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'G2_C362<class BaseClass0,T0>.ClassMethod2066'<M0>() cil managed noinlining { 
		.override method instance string class G2_C362`2<class BaseClass0,!T0>::ClassMethod2066<[1]>()
		ldstr "G3_C1329::ClassMethod2066.MI.15587<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class G2_C362`2<class BaseClass0,!T0>::.ctor()
		ret
	}
}
.class public G2_C362`2<T0, T1> 
		extends class G1_C7`2<class BaseClass1,!T1>
		implements IBase0, class IBase2`2<class BaseClass1,class BaseClass1> 
{
	.method public hidebysig newslot virtual instance string Method0() cil managed noinlining { 
		ldstr "G2_C362::Method0.8157()"
		ret
	}
	.method public hidebysig newslot virtual instance string Method1() cil managed noinlining { 
		ldstr "G2_C362::Method1.8158()"
		ret
	}
	.method public hidebysig newslot virtual instance string Method2<M0>() cil managed noinlining { 
		ldstr "G2_C362::Method2.8159<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase0.Method2'<M0>() cil managed noinlining { 
		.override method instance string IBase0::Method2<[1]>()
		ldstr "G2_C362::Method2.MI.8160<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig virtual instance string Method3<M0>() cil managed noinlining { 
		ldstr "G2_C362::Method3.8161<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G2_C362::Method7.8162<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod2065<M0>() cil managed noinlining { 
		ldstr "G2_C362::ClassMethod2065.8163<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod2066<M0>() cil managed noinlining { 
		ldstr "G2_C362::ClassMethod2066.8164<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'G1_C7<class BaseClass1,T1>.ClassMethod1329'() cil managed noinlining { 
		.override method instance string class G1_C7`2<class BaseClass1,!T1>::ClassMethod1329()
		ldstr "G2_C362::ClassMethod1329.MI.8165()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'G1_C7<class BaseClass1,T1>.ClassMethod1330'<M0>() cil managed noinlining { 
		.override method instance string class G1_C7`2<class BaseClass1,!T1>::ClassMethod1330<[1]>()
		ldstr "G2_C362::ClassMethod1330.MI.8166<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'G1_C7<class BaseClass1,T1>.ClassMethod1331'<M0>() cil managed noinlining { 
		.override method instance string class G1_C7`2<class BaseClass1,!T1>::ClassMethod1331<[1]>()
		ldstr "G2_C362::ClassMethod1331.MI.8167<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class G1_C7`2<class BaseClass1,!T1>::.ctor()
		ret
	}
}
.class interface public abstract IBase1`1<+T0> 
{
	.method public hidebysig newslot abstract virtual instance string Method4() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method5() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method6<M0>() cil managed { }
}
.class public abstract G1_C7`2<T0, T1> 
		implements class IBase1`1<!T0>, class IBase2`2<class BaseClass1,!T0> 
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
		.override method instance string class IBase1`1<!T0>::Method6<[1]>()
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
	.method public hidebysig newslot virtual instance string 'IBase2<class BaseClass1,T0>.Method7'<M0>() cil managed noinlining { 
		.override method instance string class IBase2`2<class BaseClass1,!T0>::Method7<[1]>()
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
.class interface public abstract IBase0 
{
	.method public hidebysig newslot abstract virtual instance string Method0() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method1() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method2<M0>() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method3<M0>() cil managed { }
}
.class interface public abstract IBase2`2<+T0, -T1> 
{
	.method public hidebysig newslot abstract virtual instance string Method7<M0>() cil managed { }
}
.class public auto ansi beforefieldinit Generated857 {
	.method static void M.BaseClass0<(BaseClass0)W>(!!W inst, string exp) cil managed {
		.maxstack 5
		.locals init (string[] actualResults)
		ldc.i4.s    0
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.BaseClass0<(BaseClass0)W>(!!W inst, string exp)"
		ldc.i4.s 0
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.BaseClass1<(BaseClass1)W>(!!W inst, string exp) cil managed {
		.maxstack 5
		.locals init (string[] actualResults)
		ldc.i4.s    0
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.BaseClass1<(BaseClass1)W>(!!W inst, string exp)"
		ldc.i4.s 0
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1329.T<T0,(class G3_C1329`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 23
		.locals init (string[] actualResults)
		ldc.i4.s    18
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1329.T<T0,(class G3_C1329`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 18
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1329`1<!!T0>::ClassMethod1328()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1329`1<!!T0>::ClassMethod1329()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1329`1<!!T0>::ClassMethod1330<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1329`1<!!T0>::ClassMethod1331<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1329`1<!!T0>::ClassMethod2065<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1329`1<!!T0>::ClassMethod2066<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1329`1<!!T0>::ClassMethod4039()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1329`1<!!T0>::ClassMethod4040()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1329`1<!!T0>::ClassMethod4041<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1329`1<!!T0>::ClassMethod4042<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1329`1<!!T0>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1329`1<!!T0>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1329`1<!!T0>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     13
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1329`1<!!T0>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     14
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1329`1<!!T0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     15
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1329`1<!!T0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     16
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1329`1<!!T0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     17
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1329`1<!!T0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1329.A<(class G3_C1329`1<class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 23
		.locals init (string[] actualResults)
		ldc.i4.s    18
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1329.A<(class G3_C1329`1<class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 18
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1329`1<class BaseClass0>::ClassMethod1328()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1329`1<class BaseClass0>::ClassMethod1329()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1329`1<class BaseClass0>::ClassMethod1330<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1329`1<class BaseClass0>::ClassMethod1331<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1329`1<class BaseClass0>::ClassMethod2065<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1329`1<class BaseClass0>::ClassMethod2066<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1329`1<class BaseClass0>::ClassMethod4039()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1329`1<class BaseClass0>::ClassMethod4040()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1329`1<class BaseClass0>::ClassMethod4041<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1329`1<class BaseClass0>::ClassMethod4042<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1329`1<class BaseClass0>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1329`1<class BaseClass0>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1329`1<class BaseClass0>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     13
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1329`1<class BaseClass0>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     14
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1329`1<class BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     15
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1329`1<class BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     16
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1329`1<class BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     17
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1329`1<class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1329.B<(class G3_C1329`1<class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 23
		.locals init (string[] actualResults)
		ldc.i4.s    18
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1329.B<(class G3_C1329`1<class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 18
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1329`1<class BaseClass1>::ClassMethod1328()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1329`1<class BaseClass1>::ClassMethod1329()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1329`1<class BaseClass1>::ClassMethod1330<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1329`1<class BaseClass1>::ClassMethod1331<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1329`1<class BaseClass1>::ClassMethod2065<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1329`1<class BaseClass1>::ClassMethod2066<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1329`1<class BaseClass1>::ClassMethod4039()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1329`1<class BaseClass1>::ClassMethod4040()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1329`1<class BaseClass1>::ClassMethod4041<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1329`1<class BaseClass1>::ClassMethod4042<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1329`1<class BaseClass1>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1329`1<class BaseClass1>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1329`1<class BaseClass1>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     13
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1329`1<class BaseClass1>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     14
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1329`1<class BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     15
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1329`1<class BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     16
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1329`1<class BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     17
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1329`1<class BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C362.T.T<T0,T1,(class G2_C362`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 19
		.locals init (string[] actualResults)
		ldc.i4.s    14
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C362.T.T<T0,T1,(class G2_C362`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 14
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C362`2<!!T0,!!T1>::ClassMethod1328()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C362`2<!!T0,!!T1>::ClassMethod1329()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C362`2<!!T0,!!T1>::ClassMethod1330<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C362`2<!!T0,!!T1>::ClassMethod1331<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C362`2<!!T0,!!T1>::ClassMethod2065<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C362`2<!!T0,!!T1>::ClassMethod2066<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C362`2<!!T0,!!T1>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C362`2<!!T0,!!T1>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C362`2<!!T0,!!T1>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C362`2<!!T0,!!T1>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C362`2<!!T0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C362`2<!!T0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C362`2<!!T0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     13
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C362`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C362.A.T<T1,(class G2_C362`2<class BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 19
		.locals init (string[] actualResults)
		ldc.i4.s    14
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C362.A.T<T1,(class G2_C362`2<class BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 14
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C362`2<class BaseClass0,!!T1>::ClassMethod1328()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C362`2<class BaseClass0,!!T1>::ClassMethod1329()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C362`2<class BaseClass0,!!T1>::ClassMethod1330<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C362`2<class BaseClass0,!!T1>::ClassMethod1331<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C362`2<class BaseClass0,!!T1>::ClassMethod2065<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C362`2<class BaseClass0,!!T1>::ClassMethod2066<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C362`2<class BaseClass0,!!T1>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C362`2<class BaseClass0,!!T1>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C362`2<class BaseClass0,!!T1>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C362`2<class BaseClass0,!!T1>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C362`2<class BaseClass0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C362`2<class BaseClass0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C362`2<class BaseClass0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     13
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C362`2<class BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C362.A.A<(class G2_C362`2<class BaseClass0,class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 19
		.locals init (string[] actualResults)
		ldc.i4.s    14
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C362.A.A<(class G2_C362`2<class BaseClass0,class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 14
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C362`2<class BaseClass0,class BaseClass0>::ClassMethod1328()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C362`2<class BaseClass0,class BaseClass0>::ClassMethod1329()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C362`2<class BaseClass0,class BaseClass0>::ClassMethod1330<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C362`2<class BaseClass0,class BaseClass0>::ClassMethod1331<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C362`2<class BaseClass0,class BaseClass0>::ClassMethod2065<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C362`2<class BaseClass0,class BaseClass0>::ClassMethod2066<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C362`2<class BaseClass0,class BaseClass0>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C362`2<class BaseClass0,class BaseClass0>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C362`2<class BaseClass0,class BaseClass0>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C362`2<class BaseClass0,class BaseClass0>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C362`2<class BaseClass0,class BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C362`2<class BaseClass0,class BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C362`2<class BaseClass0,class BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     13
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C362`2<class BaseClass0,class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C362.A.B<(class G2_C362`2<class BaseClass0,class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 19
		.locals init (string[] actualResults)
		ldc.i4.s    14
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C362.A.B<(class G2_C362`2<class BaseClass0,class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 14
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C362`2<class BaseClass0,class BaseClass1>::ClassMethod1328()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C362`2<class BaseClass0,class BaseClass1>::ClassMethod1329()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C362`2<class BaseClass0,class BaseClass1>::ClassMethod1330<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C362`2<class BaseClass0,class BaseClass1>::ClassMethod1331<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C362`2<class BaseClass0,class BaseClass1>::ClassMethod2065<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C362`2<class BaseClass0,class BaseClass1>::ClassMethod2066<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C362`2<class BaseClass0,class BaseClass1>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C362`2<class BaseClass0,class BaseClass1>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C362`2<class BaseClass0,class BaseClass1>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C362`2<class BaseClass0,class BaseClass1>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C362`2<class BaseClass0,class BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C362`2<class BaseClass0,class BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C362`2<class BaseClass0,class BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     13
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C362`2<class BaseClass0,class BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C362.B.T<T1,(class G2_C362`2<class BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 19
		.locals init (string[] actualResults)
		ldc.i4.s    14
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C362.B.T<T1,(class G2_C362`2<class BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 14
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C362`2<class BaseClass1,!!T1>::ClassMethod1328()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C362`2<class BaseClass1,!!T1>::ClassMethod1329()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C362`2<class BaseClass1,!!T1>::ClassMethod1330<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C362`2<class BaseClass1,!!T1>::ClassMethod1331<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C362`2<class BaseClass1,!!T1>::ClassMethod2065<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C362`2<class BaseClass1,!!T1>::ClassMethod2066<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C362`2<class BaseClass1,!!T1>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C362`2<class BaseClass1,!!T1>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C362`2<class BaseClass1,!!T1>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C362`2<class BaseClass1,!!T1>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C362`2<class BaseClass1,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C362`2<class BaseClass1,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C362`2<class BaseClass1,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     13
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C362`2<class BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C362.B.A<(class G2_C362`2<class BaseClass1,class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 19
		.locals init (string[] actualResults)
		ldc.i4.s    14
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C362.B.A<(class G2_C362`2<class BaseClass1,class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 14
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C362`2<class BaseClass1,class BaseClass0>::ClassMethod1328()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C362`2<class BaseClass1,class BaseClass0>::ClassMethod1329()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C362`2<class BaseClass1,class BaseClass0>::ClassMethod1330<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C362`2<class BaseClass1,class BaseClass0>::ClassMethod1331<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C362`2<class BaseClass1,class BaseClass0>::ClassMethod2065<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C362`2<class BaseClass1,class BaseClass0>::ClassMethod2066<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C362`2<class BaseClass1,class BaseClass0>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C362`2<class BaseClass1,class BaseClass0>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C362`2<class BaseClass1,class BaseClass0>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C362`2<class BaseClass1,class BaseClass0>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C362`2<class BaseClass1,class BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C362`2<class BaseClass1,class BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C362`2<class BaseClass1,class BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     13
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C362`2<class BaseClass1,class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C362.B.B<(class G2_C362`2<class BaseClass1,class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 19
		.locals init (string[] actualResults)
		ldc.i4.s    14
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C362.B.B<(class G2_C362`2<class BaseClass1,class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 14
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C362`2<class BaseClass1,class BaseClass1>::ClassMethod1328()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C362`2<class BaseClass1,class BaseClass1>::ClassMethod1329()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C362`2<class BaseClass1,class BaseClass1>::ClassMethod1330<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C362`2<class BaseClass1,class BaseClass1>::ClassMethod1331<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C362`2<class BaseClass1,class BaseClass1>::ClassMethod2065<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C362`2<class BaseClass1,class BaseClass1>::ClassMethod2066<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C362`2<class BaseClass1,class BaseClass1>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C362`2<class BaseClass1,class BaseClass1>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C362`2<class BaseClass1,class BaseClass1>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C362`2<class BaseClass1,class BaseClass1>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C362`2<class BaseClass1,class BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C362`2<class BaseClass1,class BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C362`2<class BaseClass1,class BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     13
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C362`2<class BaseClass1,class BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase1.T<T0,(class IBase1`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase1.T<T0,(class IBase1`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class IBase1`1<!!T0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class IBase1`1<!!T0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class IBase1`1<!!T0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase1.A<(class IBase1`1<class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase1.A<(class IBase1`1<class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class IBase1`1<class BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class IBase1`1<class BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class IBase1`1<class BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase1.B<(class IBase1`1<class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase1.B<(class IBase1`1<class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class IBase1`1<class BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class IBase1`1<class BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class IBase1`1<class BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C7.T.T<T0,T1,(class G1_C7`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C7.T.T<T0,T1,(class G1_C7`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C7`2<!!T0,!!T1>::ClassMethod1328()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C7`2<!!T0,!!T1>::ClassMethod1329()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C7`2<!!T0,!!T1>::ClassMethod1330<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C7`2<!!T0,!!T1>::ClassMethod1331<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C7`2<!!T0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C7`2<!!T0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C7`2<!!T0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C7`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C7.A.T<T1,(class G1_C7`2<class BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C7.A.T<T1,(class G1_C7`2<class BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C7`2<class BaseClass0,!!T1>::ClassMethod1328()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C7`2<class BaseClass0,!!T1>::ClassMethod1329()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C7`2<class BaseClass0,!!T1>::ClassMethod1330<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C7`2<class BaseClass0,!!T1>::ClassMethod1331<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C7`2<class BaseClass0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C7`2<class BaseClass0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C7`2<class BaseClass0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C7`2<class BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C7.A.A<(class G1_C7`2<class BaseClass0,class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C7.A.A<(class G1_C7`2<class BaseClass0,class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C7`2<class BaseClass0,class BaseClass0>::ClassMethod1328()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C7`2<class BaseClass0,class BaseClass0>::ClassMethod1329()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C7`2<class BaseClass0,class BaseClass0>::ClassMethod1330<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C7`2<class BaseClass0,class BaseClass0>::ClassMethod1331<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C7`2<class BaseClass0,class BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C7`2<class BaseClass0,class BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C7`2<class BaseClass0,class BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C7`2<class BaseClass0,class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C7.A.B<(class G1_C7`2<class BaseClass0,class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C7.A.B<(class G1_C7`2<class BaseClass0,class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C7`2<class BaseClass0,class BaseClass1>::ClassMethod1328()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C7`2<class BaseClass0,class BaseClass1>::ClassMethod1329()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C7`2<class BaseClass0,class BaseClass1>::ClassMethod1330<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C7`2<class BaseClass0,class BaseClass1>::ClassMethod1331<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C7`2<class BaseClass0,class BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C7`2<class BaseClass0,class BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C7`2<class BaseClass0,class BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C7`2<class BaseClass0,class BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C7.B.T<T1,(class G1_C7`2<class BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C7.B.T<T1,(class G1_C7`2<class BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C7`2<class BaseClass1,!!T1>::ClassMethod1328()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C7`2<class BaseClass1,!!T1>::ClassMethod1329()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C7`2<class BaseClass1,!!T1>::ClassMethod1330<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C7`2<class BaseClass1,!!T1>::ClassMethod1331<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C7`2<class BaseClass1,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C7`2<class BaseClass1,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C7`2<class BaseClass1,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C7`2<class BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C7.B.A<(class G1_C7`2<class BaseClass1,class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C7.B.A<(class G1_C7`2<class BaseClass1,class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C7`2<class BaseClass1,class BaseClass0>::ClassMethod1328()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C7`2<class BaseClass1,class BaseClass0>::ClassMethod1329()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C7`2<class BaseClass1,class BaseClass0>::ClassMethod1330<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C7`2<class BaseClass1,class BaseClass0>::ClassMethod1331<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C7`2<class BaseClass1,class BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C7`2<class BaseClass1,class BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C7`2<class BaseClass1,class BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C7`2<class BaseClass1,class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C7.B.B<(class G1_C7`2<class BaseClass1,class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C7.B.B<(class G1_C7`2<class BaseClass1,class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C7`2<class BaseClass1,class BaseClass1>::ClassMethod1328()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C7`2<class BaseClass1,class BaseClass1>::ClassMethod1329()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C7`2<class BaseClass1,class BaseClass1>::ClassMethod1330<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C7`2<class BaseClass1,class BaseClass1>::ClassMethod1331<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C7`2<class BaseClass1,class BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C7`2<class BaseClass1,class BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C7`2<class BaseClass1,class BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C7`2<class BaseClass1,class BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase0<(IBase0)W>(!!W inst, string exp) cil managed {
		.maxstack 9
		.locals init (string[] actualResults)
		ldc.i4.s    4
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase0<(IBase0)W>(!!W inst, string exp)"
		ldc.i4.s 4
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string IBase0::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string IBase0::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string IBase0::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string IBase0::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.T.T<T0,T1,(class IBase2`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.T.T<T0,T1,(class IBase2`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class IBase2`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.A.T<T1,(class IBase2`2<class BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.A.T<T1,(class IBase2`2<class BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class IBase2`2<class BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.A.A<(class IBase2`2<class BaseClass0,class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.A.A<(class IBase2`2<class BaseClass0,class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.A.B<(class IBase2`2<class BaseClass0,class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.A.B<(class IBase2`2<class BaseClass0,class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.B.T<T1,(class IBase2`2<class BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.B.T<T1,(class IBase2`2<class BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class IBase2`2<class BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.B.A<(class IBase2`2<class BaseClass1,class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.B.A<(class IBase2`2<class BaseClass1,class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.B.B<(class IBase2`2<class BaseClass1,class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.B.B<(class IBase2`2<class BaseClass1,class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
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
		newobj instance void class G3_C1329`1<class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C7`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C7`2<class BaseClass1,class BaseClass0>::ClassMethod1331<object>()
		ldstr "G2_C362::ClassMethod1331.MI.8167<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass0> on type class G3_C1329`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C7`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C7`2<class BaseClass1,class BaseClass0>::ClassMethod1330<object>()
		ldstr "G2_C362::ClassMethod1330.MI.8166<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass0> on type class G3_C1329`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C7`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C7`2<class BaseClass1,class BaseClass0>::ClassMethod1329()
		ldstr "G2_C362::ClassMethod1329.MI.8165()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass0> on type class G3_C1329`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C7`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C7`2<class BaseClass1,class BaseClass0>::ClassMethod1328()
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass0> on type class G3_C1329`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C7`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C7`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G1_C7::Method7.4815<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass0> on type class G3_C1329`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C7`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C7`2<class BaseClass1,class BaseClass0>::Method6<object>()
		ldstr "G1_C7::Method6.4813<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass0> on type class G3_C1329`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C7`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C7`2<class BaseClass1,class BaseClass0>::Method5()
		ldstr "G3_C1329::Method5.15579()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass0> on type class G3_C1329`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C7`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C7`2<class BaseClass1,class BaseClass0>::Method4()
		ldstr "G1_C7::Method4.4811()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass0> on type class G3_C1329`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method4()
		ldstr "G1_C7::Method4.4811()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1329`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method5()
		ldstr "G3_C1329::Method5.15579()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1329`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method6<object>()
		ldstr "G1_C7::Method6.MI.4814<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1329`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G2_C362::Method7.8162<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G3_C1329`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G3_C1329::Method4.MI.15578()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1329`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G3_C1329::Method5.MI.15580()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1329`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G3_C1329::Method6.15581<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1329`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C362::Method7.8162<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1329`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C362`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C362`2<class BaseClass0,class BaseClass0>::ClassMethod2066<object>()
		ldstr "G3_C1329::ClassMethod2066.MI.15587<System.Object>()"
		ldstr "class G2_C362`2<class BaseClass0,class BaseClass0> on type class G3_C1329`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C362`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C362`2<class BaseClass0,class BaseClass0>::ClassMethod2065<object>()
		ldstr "G3_C1329::ClassMethod2065.MI.15586<System.Object>()"
		ldstr "class G2_C362`2<class BaseClass0,class BaseClass0> on type class G3_C1329`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C362`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C362`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G2_C362::Method7.8162<System.Object>()"
		ldstr "class G2_C362`2<class BaseClass0,class BaseClass0> on type class G3_C1329`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C362`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C362`2<class BaseClass0,class BaseClass0>::Method3<object>()
		ldstr "G2_C362::Method3.8161<System.Object>()"
		ldstr "class G2_C362`2<class BaseClass0,class BaseClass0> on type class G3_C1329`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C362`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C362`2<class BaseClass0,class BaseClass0>::Method2<object>()
		ldstr "G2_C362::Method2.8159<System.Object>()"
		ldstr "class G2_C362`2<class BaseClass0,class BaseClass0> on type class G3_C1329`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C362`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C362`2<class BaseClass0,class BaseClass0>::Method1()
		ldstr "G2_C362::Method1.8158()"
		ldstr "class G2_C362`2<class BaseClass0,class BaseClass0> on type class G3_C1329`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C362`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C362`2<class BaseClass0,class BaseClass0>::Method0()
		ldstr "G2_C362::Method0.8157()"
		ldstr "class G2_C362`2<class BaseClass0,class BaseClass0> on type class G3_C1329`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C362`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C362`2<class BaseClass0,class BaseClass0>::ClassMethod1331<object>()
		ldstr "G2_C362::ClassMethod1331.MI.8167<System.Object>()"
		ldstr "class G2_C362`2<class BaseClass0,class BaseClass0> on type class G3_C1329`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C362`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C362`2<class BaseClass0,class BaseClass0>::ClassMethod1330<object>()
		ldstr "G2_C362::ClassMethod1330.MI.8166<System.Object>()"
		ldstr "class G2_C362`2<class BaseClass0,class BaseClass0> on type class G3_C1329`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C362`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C362`2<class BaseClass0,class BaseClass0>::ClassMethod1329()
		ldstr "G2_C362::ClassMethod1329.MI.8165()"
		ldstr "class G2_C362`2<class BaseClass0,class BaseClass0> on type class G3_C1329`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C362`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C362`2<class BaseClass0,class BaseClass0>::ClassMethod1328()
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G2_C362`2<class BaseClass0,class BaseClass0> on type class G3_C1329`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C362`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C362`2<class BaseClass0,class BaseClass0>::Method6<object>()
		ldstr "G1_C7::Method6.4813<System.Object>()"
		ldstr "class G2_C362`2<class BaseClass0,class BaseClass0> on type class G3_C1329`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C362`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C362`2<class BaseClass0,class BaseClass0>::Method5()
		ldstr "G3_C1329::Method5.15579()"
		ldstr "class G2_C362`2<class BaseClass0,class BaseClass0> on type class G3_C1329`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C362`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C362`2<class BaseClass0,class BaseClass0>::Method4()
		ldstr "G1_C7::Method4.4811()"
		ldstr "class G2_C362`2<class BaseClass0,class BaseClass0> on type class G3_C1329`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string IBase0::Method0()
		ldstr "G2_C362::Method0.8157()"
		ldstr "IBase0 on type class G3_C1329`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method1()
		ldstr "G2_C362::Method1.8158()"
		ldstr "IBase0 on type class G3_C1329`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method2<object>()
		ldstr "G2_C362::Method2.MI.8160<System.Object>()"
		ldstr "IBase0 on type class G3_C1329`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method3<object>()
		ldstr "G2_C362::Method3.8161<System.Object>()"
		ldstr "IBase0 on type class G3_C1329`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G3_C1329`1<class BaseClass0>
		callvirt instance string class G3_C1329`1<class BaseClass0>::ClassMethod4042<object>()
		ldstr "G3_C1329::ClassMethod4042.15585<System.Object>()"
		ldstr "class G3_C1329`1<class BaseClass0> on type class G3_C1329`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1329`1<class BaseClass0>
		callvirt instance string class G3_C1329`1<class BaseClass0>::ClassMethod4041<object>()
		ldstr "G3_C1329::ClassMethod4041.15584<System.Object>()"
		ldstr "class G3_C1329`1<class BaseClass0> on type class G3_C1329`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1329`1<class BaseClass0>
		callvirt instance string class G3_C1329`1<class BaseClass0>::ClassMethod4040()
		ldstr "G3_C1329::ClassMethod4040.15583()"
		ldstr "class G3_C1329`1<class BaseClass0> on type class G3_C1329`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1329`1<class BaseClass0>
		callvirt instance string class G3_C1329`1<class BaseClass0>::ClassMethod4039()
		ldstr "G3_C1329::ClassMethod4039.15582()"
		ldstr "class G3_C1329`1<class BaseClass0> on type class G3_C1329`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1329`1<class BaseClass0>
		callvirt instance string class G3_C1329`1<class BaseClass0>::Method6<object>()
		ldstr "G3_C1329::Method6.15581<System.Object>()"
		ldstr "class G3_C1329`1<class BaseClass0> on type class G3_C1329`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1329`1<class BaseClass0>
		callvirt instance string class G3_C1329`1<class BaseClass0>::Method5()
		ldstr "G3_C1329::Method5.15579()"
		ldstr "class G3_C1329`1<class BaseClass0> on type class G3_C1329`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1329`1<class BaseClass0>
		callvirt instance string class G3_C1329`1<class BaseClass0>::Method4()
		ldstr "G3_C1329::Method4.15577()"
		ldstr "class G3_C1329`1<class BaseClass0> on type class G3_C1329`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1329`1<class BaseClass0>
		callvirt instance string class G3_C1329`1<class BaseClass0>::ClassMethod2066<object>()
		ldstr "G3_C1329::ClassMethod2066.MI.15587<System.Object>()"
		ldstr "class G3_C1329`1<class BaseClass0> on type class G3_C1329`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1329`1<class BaseClass0>
		callvirt instance string class G3_C1329`1<class BaseClass0>::ClassMethod2065<object>()
		ldstr "G3_C1329::ClassMethod2065.MI.15586<System.Object>()"
		ldstr "class G3_C1329`1<class BaseClass0> on type class G3_C1329`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1329`1<class BaseClass0>
		callvirt instance string class G3_C1329`1<class BaseClass0>::Method7<object>()
		ldstr "G2_C362::Method7.8162<System.Object>()"
		ldstr "class G3_C1329`1<class BaseClass0> on type class G3_C1329`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1329`1<class BaseClass0>
		callvirt instance string class G3_C1329`1<class BaseClass0>::Method3<object>()
		ldstr "G2_C362::Method3.8161<System.Object>()"
		ldstr "class G3_C1329`1<class BaseClass0> on type class G3_C1329`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1329`1<class BaseClass0>
		callvirt instance string class G3_C1329`1<class BaseClass0>::Method2<object>()
		ldstr "G2_C362::Method2.8159<System.Object>()"
		ldstr "class G3_C1329`1<class BaseClass0> on type class G3_C1329`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1329`1<class BaseClass0>
		callvirt instance string class G3_C1329`1<class BaseClass0>::Method1()
		ldstr "G2_C362::Method1.8158()"
		ldstr "class G3_C1329`1<class BaseClass0> on type class G3_C1329`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1329`1<class BaseClass0>
		callvirt instance string class G3_C1329`1<class BaseClass0>::Method0()
		ldstr "G2_C362::Method0.8157()"
		ldstr "class G3_C1329`1<class BaseClass0> on type class G3_C1329`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1329`1<class BaseClass0>
		callvirt instance string class G3_C1329`1<class BaseClass0>::ClassMethod1331<object>()
		ldstr "G2_C362::ClassMethod1331.MI.8167<System.Object>()"
		ldstr "class G3_C1329`1<class BaseClass0> on type class G3_C1329`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1329`1<class BaseClass0>
		callvirt instance string class G3_C1329`1<class BaseClass0>::ClassMethod1330<object>()
		ldstr "G2_C362::ClassMethod1330.MI.8166<System.Object>()"
		ldstr "class G3_C1329`1<class BaseClass0> on type class G3_C1329`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1329`1<class BaseClass0>
		callvirt instance string class G3_C1329`1<class BaseClass0>::ClassMethod1329()
		ldstr "G2_C362::ClassMethod1329.MI.8165()"
		ldstr "class G3_C1329`1<class BaseClass0> on type class G3_C1329`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1329`1<class BaseClass0>
		callvirt instance string class G3_C1329`1<class BaseClass0>::ClassMethod1328()
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G3_C1329`1<class BaseClass0> on type class G3_C1329`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G3_C1329`1<class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C7`2<class BaseClass1,class BaseClass1>::ClassMethod1331<object>()
		ldstr "G2_C362::ClassMethod1331.MI.8167<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G3_C1329`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C7`2<class BaseClass1,class BaseClass1>::ClassMethod1330<object>()
		ldstr "G2_C362::ClassMethod1330.MI.8166<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G3_C1329`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C7`2<class BaseClass1,class BaseClass1>::ClassMethod1329()
		ldstr "G2_C362::ClassMethod1329.MI.8165()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G3_C1329`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C7`2<class BaseClass1,class BaseClass1>::ClassMethod1328()
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G3_C1329`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C7`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C7::Method7.4815<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G3_C1329`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C7`2<class BaseClass1,class BaseClass1>::Method6<object>()
		ldstr "G1_C7::Method6.4813<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G3_C1329`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C7`2<class BaseClass1,class BaseClass1>::Method5()
		ldstr "G3_C1329::Method5.15579()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G3_C1329`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C7`2<class BaseClass1,class BaseClass1>::Method4()
		ldstr "G1_C7::Method4.4811()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G3_C1329`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method4()
		ldstr "G1_C7::Method4.4811()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1329`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method5()
		ldstr "G3_C1329::Method5.15579()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1329`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method6<object>()
		ldstr "G1_C7::Method6.MI.4814<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1329`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G2_C362::Method7.8162<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G3_C1329`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G3_C1329::Method4.MI.15578()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1329`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G3_C1329::Method5.MI.15580()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1329`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G3_C1329::Method6.15581<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1329`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C362::Method7.8162<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1329`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C362`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C362`2<class BaseClass0,class BaseClass1>::ClassMethod2066<object>()
		ldstr "G3_C1329::ClassMethod2066.MI.15587<System.Object>()"
		ldstr "class G2_C362`2<class BaseClass0,class BaseClass1> on type class G3_C1329`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C362`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C362`2<class BaseClass0,class BaseClass1>::ClassMethod2065<object>()
		ldstr "G3_C1329::ClassMethod2065.MI.15586<System.Object>()"
		ldstr "class G2_C362`2<class BaseClass0,class BaseClass1> on type class G3_C1329`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C362`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C362`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C362::Method7.8162<System.Object>()"
		ldstr "class G2_C362`2<class BaseClass0,class BaseClass1> on type class G3_C1329`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C362`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C362`2<class BaseClass0,class BaseClass1>::Method3<object>()
		ldstr "G2_C362::Method3.8161<System.Object>()"
		ldstr "class G2_C362`2<class BaseClass0,class BaseClass1> on type class G3_C1329`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C362`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C362`2<class BaseClass0,class BaseClass1>::Method2<object>()
		ldstr "G2_C362::Method2.8159<System.Object>()"
		ldstr "class G2_C362`2<class BaseClass0,class BaseClass1> on type class G3_C1329`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C362`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C362`2<class BaseClass0,class BaseClass1>::Method1()
		ldstr "G2_C362::Method1.8158()"
		ldstr "class G2_C362`2<class BaseClass0,class BaseClass1> on type class G3_C1329`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C362`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C362`2<class BaseClass0,class BaseClass1>::Method0()
		ldstr "G2_C362::Method0.8157()"
		ldstr "class G2_C362`2<class BaseClass0,class BaseClass1> on type class G3_C1329`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C362`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C362`2<class BaseClass0,class BaseClass1>::ClassMethod1331<object>()
		ldstr "G2_C362::ClassMethod1331.MI.8167<System.Object>()"
		ldstr "class G2_C362`2<class BaseClass0,class BaseClass1> on type class G3_C1329`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C362`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C362`2<class BaseClass0,class BaseClass1>::ClassMethod1330<object>()
		ldstr "G2_C362::ClassMethod1330.MI.8166<System.Object>()"
		ldstr "class G2_C362`2<class BaseClass0,class BaseClass1> on type class G3_C1329`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C362`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C362`2<class BaseClass0,class BaseClass1>::ClassMethod1329()
		ldstr "G2_C362::ClassMethod1329.MI.8165()"
		ldstr "class G2_C362`2<class BaseClass0,class BaseClass1> on type class G3_C1329`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C362`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C362`2<class BaseClass0,class BaseClass1>::ClassMethod1328()
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G2_C362`2<class BaseClass0,class BaseClass1> on type class G3_C1329`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C362`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C362`2<class BaseClass0,class BaseClass1>::Method6<object>()
		ldstr "G1_C7::Method6.4813<System.Object>()"
		ldstr "class G2_C362`2<class BaseClass0,class BaseClass1> on type class G3_C1329`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C362`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C362`2<class BaseClass0,class BaseClass1>::Method5()
		ldstr "G3_C1329::Method5.15579()"
		ldstr "class G2_C362`2<class BaseClass0,class BaseClass1> on type class G3_C1329`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C362`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C362`2<class BaseClass0,class BaseClass1>::Method4()
		ldstr "G1_C7::Method4.4811()"
		ldstr "class G2_C362`2<class BaseClass0,class BaseClass1> on type class G3_C1329`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string IBase0::Method0()
		ldstr "G2_C362::Method0.8157()"
		ldstr "IBase0 on type class G3_C1329`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method1()
		ldstr "G2_C362::Method1.8158()"
		ldstr "IBase0 on type class G3_C1329`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method2<object>()
		ldstr "G2_C362::Method2.MI.8160<System.Object>()"
		ldstr "IBase0 on type class G3_C1329`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method3<object>()
		ldstr "G2_C362::Method3.8161<System.Object>()"
		ldstr "IBase0 on type class G3_C1329`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G3_C1329`1<class BaseClass1>
		callvirt instance string class G3_C1329`1<class BaseClass1>::ClassMethod4042<object>()
		ldstr "G3_C1329::ClassMethod4042.15585<System.Object>()"
		ldstr "class G3_C1329`1<class BaseClass1> on type class G3_C1329`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1329`1<class BaseClass1>
		callvirt instance string class G3_C1329`1<class BaseClass1>::ClassMethod4041<object>()
		ldstr "G3_C1329::ClassMethod4041.15584<System.Object>()"
		ldstr "class G3_C1329`1<class BaseClass1> on type class G3_C1329`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1329`1<class BaseClass1>
		callvirt instance string class G3_C1329`1<class BaseClass1>::ClassMethod4040()
		ldstr "G3_C1329::ClassMethod4040.15583()"
		ldstr "class G3_C1329`1<class BaseClass1> on type class G3_C1329`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1329`1<class BaseClass1>
		callvirt instance string class G3_C1329`1<class BaseClass1>::ClassMethod4039()
		ldstr "G3_C1329::ClassMethod4039.15582()"
		ldstr "class G3_C1329`1<class BaseClass1> on type class G3_C1329`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1329`1<class BaseClass1>
		callvirt instance string class G3_C1329`1<class BaseClass1>::Method6<object>()
		ldstr "G3_C1329::Method6.15581<System.Object>()"
		ldstr "class G3_C1329`1<class BaseClass1> on type class G3_C1329`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1329`1<class BaseClass1>
		callvirt instance string class G3_C1329`1<class BaseClass1>::Method5()
		ldstr "G3_C1329::Method5.15579()"
		ldstr "class G3_C1329`1<class BaseClass1> on type class G3_C1329`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1329`1<class BaseClass1>
		callvirt instance string class G3_C1329`1<class BaseClass1>::Method4()
		ldstr "G3_C1329::Method4.15577()"
		ldstr "class G3_C1329`1<class BaseClass1> on type class G3_C1329`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1329`1<class BaseClass1>
		callvirt instance string class G3_C1329`1<class BaseClass1>::ClassMethod2066<object>()
		ldstr "G3_C1329::ClassMethod2066.MI.15587<System.Object>()"
		ldstr "class G3_C1329`1<class BaseClass1> on type class G3_C1329`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1329`1<class BaseClass1>
		callvirt instance string class G3_C1329`1<class BaseClass1>::ClassMethod2065<object>()
		ldstr "G3_C1329::ClassMethod2065.MI.15586<System.Object>()"
		ldstr "class G3_C1329`1<class BaseClass1> on type class G3_C1329`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1329`1<class BaseClass1>
		callvirt instance string class G3_C1329`1<class BaseClass1>::Method7<object>()
		ldstr "G2_C362::Method7.8162<System.Object>()"
		ldstr "class G3_C1329`1<class BaseClass1> on type class G3_C1329`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1329`1<class BaseClass1>
		callvirt instance string class G3_C1329`1<class BaseClass1>::Method3<object>()
		ldstr "G2_C362::Method3.8161<System.Object>()"
		ldstr "class G3_C1329`1<class BaseClass1> on type class G3_C1329`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1329`1<class BaseClass1>
		callvirt instance string class G3_C1329`1<class BaseClass1>::Method2<object>()
		ldstr "G2_C362::Method2.8159<System.Object>()"
		ldstr "class G3_C1329`1<class BaseClass1> on type class G3_C1329`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1329`1<class BaseClass1>
		callvirt instance string class G3_C1329`1<class BaseClass1>::Method1()
		ldstr "G2_C362::Method1.8158()"
		ldstr "class G3_C1329`1<class BaseClass1> on type class G3_C1329`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1329`1<class BaseClass1>
		callvirt instance string class G3_C1329`1<class BaseClass1>::Method0()
		ldstr "G2_C362::Method0.8157()"
		ldstr "class G3_C1329`1<class BaseClass1> on type class G3_C1329`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1329`1<class BaseClass1>
		callvirt instance string class G3_C1329`1<class BaseClass1>::ClassMethod1331<object>()
		ldstr "G2_C362::ClassMethod1331.MI.8167<System.Object>()"
		ldstr "class G3_C1329`1<class BaseClass1> on type class G3_C1329`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1329`1<class BaseClass1>
		callvirt instance string class G3_C1329`1<class BaseClass1>::ClassMethod1330<object>()
		ldstr "G2_C362::ClassMethod1330.MI.8166<System.Object>()"
		ldstr "class G3_C1329`1<class BaseClass1> on type class G3_C1329`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1329`1<class BaseClass1>
		callvirt instance string class G3_C1329`1<class BaseClass1>::ClassMethod1329()
		ldstr "G2_C362::ClassMethod1329.MI.8165()"
		ldstr "class G3_C1329`1<class BaseClass1> on type class G3_C1329`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1329`1<class BaseClass1>
		callvirt instance string class G3_C1329`1<class BaseClass1>::ClassMethod1328()
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G3_C1329`1<class BaseClass1> on type class G3_C1329`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G2_C362`2<class BaseClass0,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C7`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C7`2<class BaseClass1,class BaseClass0>::ClassMethod1331<object>()
		ldstr "G2_C362::ClassMethod1331.MI.8167<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass0> on type class G2_C362`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C7`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C7`2<class BaseClass1,class BaseClass0>::ClassMethod1330<object>()
		ldstr "G2_C362::ClassMethod1330.MI.8166<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass0> on type class G2_C362`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C7`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C7`2<class BaseClass1,class BaseClass0>::ClassMethod1329()
		ldstr "G2_C362::ClassMethod1329.MI.8165()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass0> on type class G2_C362`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C7`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C7`2<class BaseClass1,class BaseClass0>::ClassMethod1328()
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass0> on type class G2_C362`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C7`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C7`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G1_C7::Method7.4815<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass0> on type class G2_C362`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C7`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C7`2<class BaseClass1,class BaseClass0>::Method6<object>()
		ldstr "G1_C7::Method6.4813<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass0> on type class G2_C362`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C7`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C7`2<class BaseClass1,class BaseClass0>::Method5()
		ldstr "G1_C7::Method5.4812()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass0> on type class G2_C362`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C7`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C7`2<class BaseClass1,class BaseClass0>::Method4()
		ldstr "G1_C7::Method4.4811()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass0> on type class G2_C362`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method4()
		ldstr "G1_C7::Method4.4811()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C362`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method5()
		ldstr "G1_C7::Method5.4812()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C362`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method6<object>()
		ldstr "G1_C7::Method6.MI.4814<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C362`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G2_C362::Method7.8162<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C362`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G1_C7::Method4.4811()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C362`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G1_C7::Method5.4812()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C362`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G1_C7::Method6.MI.4814<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C362`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C362::Method7.8162<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C362`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C362`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C362`2<class BaseClass0,class BaseClass0>::ClassMethod2066<object>()
		ldstr "G2_C362::ClassMethod2066.8164<System.Object>()"
		ldstr "class G2_C362`2<class BaseClass0,class BaseClass0> on type class G2_C362`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C362`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C362`2<class BaseClass0,class BaseClass0>::ClassMethod2065<object>()
		ldstr "G2_C362::ClassMethod2065.8163<System.Object>()"
		ldstr "class G2_C362`2<class BaseClass0,class BaseClass0> on type class G2_C362`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C362`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C362`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G2_C362::Method7.8162<System.Object>()"
		ldstr "class G2_C362`2<class BaseClass0,class BaseClass0> on type class G2_C362`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C362`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C362`2<class BaseClass0,class BaseClass0>::Method3<object>()
		ldstr "G2_C362::Method3.8161<System.Object>()"
		ldstr "class G2_C362`2<class BaseClass0,class BaseClass0> on type class G2_C362`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C362`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C362`2<class BaseClass0,class BaseClass0>::Method2<object>()
		ldstr "G2_C362::Method2.8159<System.Object>()"
		ldstr "class G2_C362`2<class BaseClass0,class BaseClass0> on type class G2_C362`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C362`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C362`2<class BaseClass0,class BaseClass0>::Method1()
		ldstr "G2_C362::Method1.8158()"
		ldstr "class G2_C362`2<class BaseClass0,class BaseClass0> on type class G2_C362`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C362`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C362`2<class BaseClass0,class BaseClass0>::Method0()
		ldstr "G2_C362::Method0.8157()"
		ldstr "class G2_C362`2<class BaseClass0,class BaseClass0> on type class G2_C362`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C362`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C362`2<class BaseClass0,class BaseClass0>::ClassMethod1331<object>()
		ldstr "G2_C362::ClassMethod1331.MI.8167<System.Object>()"
		ldstr "class G2_C362`2<class BaseClass0,class BaseClass0> on type class G2_C362`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C362`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C362`2<class BaseClass0,class BaseClass0>::ClassMethod1330<object>()
		ldstr "G2_C362::ClassMethod1330.MI.8166<System.Object>()"
		ldstr "class G2_C362`2<class BaseClass0,class BaseClass0> on type class G2_C362`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C362`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C362`2<class BaseClass0,class BaseClass0>::ClassMethod1329()
		ldstr "G2_C362::ClassMethod1329.MI.8165()"
		ldstr "class G2_C362`2<class BaseClass0,class BaseClass0> on type class G2_C362`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C362`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C362`2<class BaseClass0,class BaseClass0>::ClassMethod1328()
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G2_C362`2<class BaseClass0,class BaseClass0> on type class G2_C362`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C362`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C362`2<class BaseClass0,class BaseClass0>::Method6<object>()
		ldstr "G1_C7::Method6.4813<System.Object>()"
		ldstr "class G2_C362`2<class BaseClass0,class BaseClass0> on type class G2_C362`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C362`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C362`2<class BaseClass0,class BaseClass0>::Method5()
		ldstr "G1_C7::Method5.4812()"
		ldstr "class G2_C362`2<class BaseClass0,class BaseClass0> on type class G2_C362`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C362`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C362`2<class BaseClass0,class BaseClass0>::Method4()
		ldstr "G1_C7::Method4.4811()"
		ldstr "class G2_C362`2<class BaseClass0,class BaseClass0> on type class G2_C362`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string IBase0::Method0()
		ldstr "G2_C362::Method0.8157()"
		ldstr "IBase0 on type class G2_C362`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method1()
		ldstr "G2_C362::Method1.8158()"
		ldstr "IBase0 on type class G2_C362`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method2<object>()
		ldstr "G2_C362::Method2.MI.8160<System.Object>()"
		ldstr "IBase0 on type class G2_C362`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method3<object>()
		ldstr "G2_C362::Method3.8161<System.Object>()"
		ldstr "IBase0 on type class G2_C362`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G2_C362`2<class BaseClass0,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C7`2<class BaseClass1,class BaseClass1>::ClassMethod1331<object>()
		ldstr "G2_C362::ClassMethod1331.MI.8167<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G2_C362`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C7`2<class BaseClass1,class BaseClass1>::ClassMethod1330<object>()
		ldstr "G2_C362::ClassMethod1330.MI.8166<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G2_C362`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C7`2<class BaseClass1,class BaseClass1>::ClassMethod1329()
		ldstr "G2_C362::ClassMethod1329.MI.8165()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G2_C362`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C7`2<class BaseClass1,class BaseClass1>::ClassMethod1328()
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G2_C362`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C7`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C7::Method7.4815<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G2_C362`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C7`2<class BaseClass1,class BaseClass1>::Method6<object>()
		ldstr "G1_C7::Method6.4813<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G2_C362`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C7`2<class BaseClass1,class BaseClass1>::Method5()
		ldstr "G1_C7::Method5.4812()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G2_C362`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C7`2<class BaseClass1,class BaseClass1>::Method4()
		ldstr "G1_C7::Method4.4811()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G2_C362`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method4()
		ldstr "G1_C7::Method4.4811()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C362`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method5()
		ldstr "G1_C7::Method5.4812()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C362`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method6<object>()
		ldstr "G1_C7::Method6.MI.4814<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C362`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G2_C362::Method7.8162<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C362`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G1_C7::Method4.4811()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C362`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G1_C7::Method5.4812()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C362`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G1_C7::Method6.MI.4814<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C362`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C362::Method7.8162<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C362`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C362`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C362`2<class BaseClass0,class BaseClass1>::ClassMethod2066<object>()
		ldstr "G2_C362::ClassMethod2066.8164<System.Object>()"
		ldstr "class G2_C362`2<class BaseClass0,class BaseClass1> on type class G2_C362`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C362`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C362`2<class BaseClass0,class BaseClass1>::ClassMethod2065<object>()
		ldstr "G2_C362::ClassMethod2065.8163<System.Object>()"
		ldstr "class G2_C362`2<class BaseClass0,class BaseClass1> on type class G2_C362`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C362`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C362`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C362::Method7.8162<System.Object>()"
		ldstr "class G2_C362`2<class BaseClass0,class BaseClass1> on type class G2_C362`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C362`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C362`2<class BaseClass0,class BaseClass1>::Method3<object>()
		ldstr "G2_C362::Method3.8161<System.Object>()"
		ldstr "class G2_C362`2<class BaseClass0,class BaseClass1> on type class G2_C362`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C362`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C362`2<class BaseClass0,class BaseClass1>::Method2<object>()
		ldstr "G2_C362::Method2.8159<System.Object>()"
		ldstr "class G2_C362`2<class BaseClass0,class BaseClass1> on type class G2_C362`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C362`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C362`2<class BaseClass0,class BaseClass1>::Method1()
		ldstr "G2_C362::Method1.8158()"
		ldstr "class G2_C362`2<class BaseClass0,class BaseClass1> on type class G2_C362`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C362`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C362`2<class BaseClass0,class BaseClass1>::Method0()
		ldstr "G2_C362::Method0.8157()"
		ldstr "class G2_C362`2<class BaseClass0,class BaseClass1> on type class G2_C362`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C362`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C362`2<class BaseClass0,class BaseClass1>::ClassMethod1331<object>()
		ldstr "G2_C362::ClassMethod1331.MI.8167<System.Object>()"
		ldstr "class G2_C362`2<class BaseClass0,class BaseClass1> on type class G2_C362`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C362`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C362`2<class BaseClass0,class BaseClass1>::ClassMethod1330<object>()
		ldstr "G2_C362::ClassMethod1330.MI.8166<System.Object>()"
		ldstr "class G2_C362`2<class BaseClass0,class BaseClass1> on type class G2_C362`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C362`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C362`2<class BaseClass0,class BaseClass1>::ClassMethod1329()
		ldstr "G2_C362::ClassMethod1329.MI.8165()"
		ldstr "class G2_C362`2<class BaseClass0,class BaseClass1> on type class G2_C362`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C362`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C362`2<class BaseClass0,class BaseClass1>::ClassMethod1328()
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G2_C362`2<class BaseClass0,class BaseClass1> on type class G2_C362`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C362`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C362`2<class BaseClass0,class BaseClass1>::Method6<object>()
		ldstr "G1_C7::Method6.4813<System.Object>()"
		ldstr "class G2_C362`2<class BaseClass0,class BaseClass1> on type class G2_C362`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C362`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C362`2<class BaseClass0,class BaseClass1>::Method5()
		ldstr "G1_C7::Method5.4812()"
		ldstr "class G2_C362`2<class BaseClass0,class BaseClass1> on type class G2_C362`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C362`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C362`2<class BaseClass0,class BaseClass1>::Method4()
		ldstr "G1_C7::Method4.4811()"
		ldstr "class G2_C362`2<class BaseClass0,class BaseClass1> on type class G2_C362`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string IBase0::Method0()
		ldstr "G2_C362::Method0.8157()"
		ldstr "IBase0 on type class G2_C362`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method1()
		ldstr "G2_C362::Method1.8158()"
		ldstr "IBase0 on type class G2_C362`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method2<object>()
		ldstr "G2_C362::Method2.MI.8160<System.Object>()"
		ldstr "IBase0 on type class G2_C362`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method3<object>()
		ldstr "G2_C362::Method3.8161<System.Object>()"
		ldstr "IBase0 on type class G2_C362`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G2_C362`2<class BaseClass1,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C7`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C7`2<class BaseClass1,class BaseClass0>::ClassMethod1331<object>()
		ldstr "G2_C362::ClassMethod1331.MI.8167<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass0> on type class G2_C362`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C7`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C7`2<class BaseClass1,class BaseClass0>::ClassMethod1330<object>()
		ldstr "G2_C362::ClassMethod1330.MI.8166<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass0> on type class G2_C362`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C7`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C7`2<class BaseClass1,class BaseClass0>::ClassMethod1329()
		ldstr "G2_C362::ClassMethod1329.MI.8165()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass0> on type class G2_C362`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C7`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C7`2<class BaseClass1,class BaseClass0>::ClassMethod1328()
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass0> on type class G2_C362`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C7`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C7`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G1_C7::Method7.4815<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass0> on type class G2_C362`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C7`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C7`2<class BaseClass1,class BaseClass0>::Method6<object>()
		ldstr "G1_C7::Method6.4813<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass0> on type class G2_C362`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C7`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C7`2<class BaseClass1,class BaseClass0>::Method5()
		ldstr "G1_C7::Method5.4812()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass0> on type class G2_C362`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C7`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C7`2<class BaseClass1,class BaseClass0>::Method4()
		ldstr "G1_C7::Method4.4811()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass0> on type class G2_C362`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method4()
		ldstr "G1_C7::Method4.4811()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C362`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method5()
		ldstr "G1_C7::Method5.4812()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C362`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method6<object>()
		ldstr "G1_C7::Method6.MI.4814<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C362`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G2_C362::Method7.8162<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C362`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G1_C7::Method4.4811()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C362`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G1_C7::Method5.4812()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C362`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G1_C7::Method6.MI.4814<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C362`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C362::Method7.8162<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C362`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C362`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C362`2<class BaseClass1,class BaseClass0>::ClassMethod2066<object>()
		ldstr "G2_C362::ClassMethod2066.8164<System.Object>()"
		ldstr "class G2_C362`2<class BaseClass1,class BaseClass0> on type class G2_C362`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C362`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C362`2<class BaseClass1,class BaseClass0>::ClassMethod2065<object>()
		ldstr "G2_C362::ClassMethod2065.8163<System.Object>()"
		ldstr "class G2_C362`2<class BaseClass1,class BaseClass0> on type class G2_C362`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C362`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C362`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G2_C362::Method7.8162<System.Object>()"
		ldstr "class G2_C362`2<class BaseClass1,class BaseClass0> on type class G2_C362`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C362`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C362`2<class BaseClass1,class BaseClass0>::Method3<object>()
		ldstr "G2_C362::Method3.8161<System.Object>()"
		ldstr "class G2_C362`2<class BaseClass1,class BaseClass0> on type class G2_C362`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C362`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C362`2<class BaseClass1,class BaseClass0>::Method2<object>()
		ldstr "G2_C362::Method2.8159<System.Object>()"
		ldstr "class G2_C362`2<class BaseClass1,class BaseClass0> on type class G2_C362`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C362`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C362`2<class BaseClass1,class BaseClass0>::Method1()
		ldstr "G2_C362::Method1.8158()"
		ldstr "class G2_C362`2<class BaseClass1,class BaseClass0> on type class G2_C362`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C362`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C362`2<class BaseClass1,class BaseClass0>::Method0()
		ldstr "G2_C362::Method0.8157()"
		ldstr "class G2_C362`2<class BaseClass1,class BaseClass0> on type class G2_C362`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C362`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C362`2<class BaseClass1,class BaseClass0>::ClassMethod1331<object>()
		ldstr "G2_C362::ClassMethod1331.MI.8167<System.Object>()"
		ldstr "class G2_C362`2<class BaseClass1,class BaseClass0> on type class G2_C362`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C362`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C362`2<class BaseClass1,class BaseClass0>::ClassMethod1330<object>()
		ldstr "G2_C362::ClassMethod1330.MI.8166<System.Object>()"
		ldstr "class G2_C362`2<class BaseClass1,class BaseClass0> on type class G2_C362`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C362`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C362`2<class BaseClass1,class BaseClass0>::ClassMethod1329()
		ldstr "G2_C362::ClassMethod1329.MI.8165()"
		ldstr "class G2_C362`2<class BaseClass1,class BaseClass0> on type class G2_C362`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C362`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C362`2<class BaseClass1,class BaseClass0>::ClassMethod1328()
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G2_C362`2<class BaseClass1,class BaseClass0> on type class G2_C362`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C362`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C362`2<class BaseClass1,class BaseClass0>::Method6<object>()
		ldstr "G1_C7::Method6.4813<System.Object>()"
		ldstr "class G2_C362`2<class BaseClass1,class BaseClass0> on type class G2_C362`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C362`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C362`2<class BaseClass1,class BaseClass0>::Method5()
		ldstr "G1_C7::Method5.4812()"
		ldstr "class G2_C362`2<class BaseClass1,class BaseClass0> on type class G2_C362`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C362`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C362`2<class BaseClass1,class BaseClass0>::Method4()
		ldstr "G1_C7::Method4.4811()"
		ldstr "class G2_C362`2<class BaseClass1,class BaseClass0> on type class G2_C362`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string IBase0::Method0()
		ldstr "G2_C362::Method0.8157()"
		ldstr "IBase0 on type class G2_C362`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method1()
		ldstr "G2_C362::Method1.8158()"
		ldstr "IBase0 on type class G2_C362`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method2<object>()
		ldstr "G2_C362::Method2.MI.8160<System.Object>()"
		ldstr "IBase0 on type class G2_C362`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method3<object>()
		ldstr "G2_C362::Method3.8161<System.Object>()"
		ldstr "IBase0 on type class G2_C362`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G2_C362`2<class BaseClass1,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C7`2<class BaseClass1,class BaseClass1>::ClassMethod1331<object>()
		ldstr "G2_C362::ClassMethod1331.MI.8167<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G2_C362`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C7`2<class BaseClass1,class BaseClass1>::ClassMethod1330<object>()
		ldstr "G2_C362::ClassMethod1330.MI.8166<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G2_C362`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C7`2<class BaseClass1,class BaseClass1>::ClassMethod1329()
		ldstr "G2_C362::ClassMethod1329.MI.8165()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G2_C362`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C7`2<class BaseClass1,class BaseClass1>::ClassMethod1328()
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G2_C362`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C7`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C7::Method7.4815<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G2_C362`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C7`2<class BaseClass1,class BaseClass1>::Method6<object>()
		ldstr "G1_C7::Method6.4813<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G2_C362`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C7`2<class BaseClass1,class BaseClass1>::Method5()
		ldstr "G1_C7::Method5.4812()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G2_C362`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C7`2<class BaseClass1,class BaseClass1>::Method4()
		ldstr "G1_C7::Method4.4811()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G2_C362`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method4()
		ldstr "G1_C7::Method4.4811()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C362`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method5()
		ldstr "G1_C7::Method5.4812()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C362`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method6<object>()
		ldstr "G1_C7::Method6.MI.4814<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C362`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G2_C362::Method7.8162<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C362`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G1_C7::Method4.4811()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C362`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G1_C7::Method5.4812()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C362`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G1_C7::Method6.MI.4814<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C362`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C362::Method7.8162<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C362`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C362`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C362`2<class BaseClass1,class BaseClass1>::ClassMethod2066<object>()
		ldstr "G2_C362::ClassMethod2066.8164<System.Object>()"
		ldstr "class G2_C362`2<class BaseClass1,class BaseClass1> on type class G2_C362`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C362`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C362`2<class BaseClass1,class BaseClass1>::ClassMethod2065<object>()
		ldstr "G2_C362::ClassMethod2065.8163<System.Object>()"
		ldstr "class G2_C362`2<class BaseClass1,class BaseClass1> on type class G2_C362`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C362`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C362`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G2_C362::Method7.8162<System.Object>()"
		ldstr "class G2_C362`2<class BaseClass1,class BaseClass1> on type class G2_C362`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C362`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C362`2<class BaseClass1,class BaseClass1>::Method3<object>()
		ldstr "G2_C362::Method3.8161<System.Object>()"
		ldstr "class G2_C362`2<class BaseClass1,class BaseClass1> on type class G2_C362`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C362`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C362`2<class BaseClass1,class BaseClass1>::Method2<object>()
		ldstr "G2_C362::Method2.8159<System.Object>()"
		ldstr "class G2_C362`2<class BaseClass1,class BaseClass1> on type class G2_C362`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C362`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C362`2<class BaseClass1,class BaseClass1>::Method1()
		ldstr "G2_C362::Method1.8158()"
		ldstr "class G2_C362`2<class BaseClass1,class BaseClass1> on type class G2_C362`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C362`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C362`2<class BaseClass1,class BaseClass1>::Method0()
		ldstr "G2_C362::Method0.8157()"
		ldstr "class G2_C362`2<class BaseClass1,class BaseClass1> on type class G2_C362`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C362`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C362`2<class BaseClass1,class BaseClass1>::ClassMethod1331<object>()
		ldstr "G2_C362::ClassMethod1331.MI.8167<System.Object>()"
		ldstr "class G2_C362`2<class BaseClass1,class BaseClass1> on type class G2_C362`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C362`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C362`2<class BaseClass1,class BaseClass1>::ClassMethod1330<object>()
		ldstr "G2_C362::ClassMethod1330.MI.8166<System.Object>()"
		ldstr "class G2_C362`2<class BaseClass1,class BaseClass1> on type class G2_C362`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C362`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C362`2<class BaseClass1,class BaseClass1>::ClassMethod1329()
		ldstr "G2_C362::ClassMethod1329.MI.8165()"
		ldstr "class G2_C362`2<class BaseClass1,class BaseClass1> on type class G2_C362`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C362`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C362`2<class BaseClass1,class BaseClass1>::ClassMethod1328()
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G2_C362`2<class BaseClass1,class BaseClass1> on type class G2_C362`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C362`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C362`2<class BaseClass1,class BaseClass1>::Method6<object>()
		ldstr "G1_C7::Method6.4813<System.Object>()"
		ldstr "class G2_C362`2<class BaseClass1,class BaseClass1> on type class G2_C362`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C362`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C362`2<class BaseClass1,class BaseClass1>::Method5()
		ldstr "G1_C7::Method5.4812()"
		ldstr "class G2_C362`2<class BaseClass1,class BaseClass1> on type class G2_C362`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C362`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C362`2<class BaseClass1,class BaseClass1>::Method4()
		ldstr "G1_C7::Method4.4811()"
		ldstr "class G2_C362`2<class BaseClass1,class BaseClass1> on type class G2_C362`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string IBase0::Method0()
		ldstr "G2_C362::Method0.8157()"
		ldstr "IBase0 on type class G2_C362`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method1()
		ldstr "G2_C362::Method1.8158()"
		ldstr "IBase0 on type class G2_C362`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method2<object>()
		ldstr "G2_C362::Method2.MI.8160<System.Object>()"
		ldstr "IBase0 on type class G2_C362`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method3<object>()
		ldstr "G2_C362::Method3.8161<System.Object>()"
		ldstr "IBase0 on type class G2_C362`2<class BaseClass1,class BaseClass1>"
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
		newobj instance void class G3_C1329`1<class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C362::ClassMethod1329.MI.8165()#G2_C362::ClassMethod1330.MI.8166<System.Object>()#G2_C362::ClassMethod1331.MI.8167<System.Object>()#G1_C7::Method4.4811()#G3_C1329::Method5.15579()#G1_C7::Method6.4813<System.Object>()#G1_C7::Method7.4815<System.Object>()#"
		call void Generated857::M.G1_C7.T.T<class BaseClass1,class BaseClass0,class G3_C1329`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C362::ClassMethod1329.MI.8165()#G2_C362::ClassMethod1330.MI.8166<System.Object>()#G2_C362::ClassMethod1331.MI.8167<System.Object>()#G1_C7::Method4.4811()#G3_C1329::Method5.15579()#G1_C7::Method6.4813<System.Object>()#G1_C7::Method7.4815<System.Object>()#"
		call void Generated857::M.G1_C7.B.T<class BaseClass0,class G3_C1329`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C362::ClassMethod1329.MI.8165()#G2_C362::ClassMethod1330.MI.8166<System.Object>()#G2_C362::ClassMethod1331.MI.8167<System.Object>()#G1_C7::Method4.4811()#G3_C1329::Method5.15579()#G1_C7::Method6.4813<System.Object>()#G1_C7::Method7.4815<System.Object>()#"
		call void Generated857::M.G1_C7.B.A<class G3_C1329`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C7::Method4.4811()#G3_C1329::Method5.15579()#G1_C7::Method6.MI.4814<System.Object>()#"
		call void Generated857::M.IBase1.T<class BaseClass1,class G3_C1329`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::Method4.4811()#G3_C1329::Method5.15579()#G1_C7::Method6.MI.4814<System.Object>()#"
		call void Generated857::M.IBase1.B<class G3_C1329`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C362::Method7.8162<System.Object>()#"
		call void Generated857::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G3_C1329`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C362::Method7.8162<System.Object>()#"
		call void Generated857::M.IBase2.B.T<class BaseClass1,class G3_C1329`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C362::Method7.8162<System.Object>()#"
		call void Generated857::M.IBase2.B.B<class G3_C1329`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1329::Method4.MI.15578()#G3_C1329::Method5.MI.15580()#G3_C1329::Method6.15581<System.Object>()#"
		call void Generated857::M.IBase1.T<class BaseClass0,class G3_C1329`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G3_C1329::Method4.MI.15578()#G3_C1329::Method5.MI.15580()#G3_C1329::Method6.15581<System.Object>()#"
		call void Generated857::M.IBase1.A<class G3_C1329`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C362::Method7.8162<System.Object>()#"
		call void Generated857::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G3_C1329`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C362::Method7.8162<System.Object>()#"
		call void Generated857::M.IBase2.A.T<class BaseClass1,class G3_C1329`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C362::Method7.8162<System.Object>()#"
		call void Generated857::M.IBase2.A.B<class G3_C1329`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C362::ClassMethod1329.MI.8165()#G2_C362::ClassMethod1330.MI.8166<System.Object>()#G2_C362::ClassMethod1331.MI.8167<System.Object>()#G3_C1329::ClassMethod2065.MI.15586<System.Object>()#G3_C1329::ClassMethod2066.MI.15587<System.Object>()#G2_C362::Method0.8157()#G2_C362::Method1.8158()#G2_C362::Method2.8159<System.Object>()#G2_C362::Method3.8161<System.Object>()#G1_C7::Method4.4811()#G3_C1329::Method5.15579()#G1_C7::Method6.4813<System.Object>()#G2_C362::Method7.8162<System.Object>()#"
		call void Generated857::M.G2_C362.T.T<class BaseClass0,class BaseClass0,class G3_C1329`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C362::ClassMethod1329.MI.8165()#G2_C362::ClassMethod1330.MI.8166<System.Object>()#G2_C362::ClassMethod1331.MI.8167<System.Object>()#G3_C1329::ClassMethod2065.MI.15586<System.Object>()#G3_C1329::ClassMethod2066.MI.15587<System.Object>()#G2_C362::Method0.8157()#G2_C362::Method1.8158()#G2_C362::Method2.8159<System.Object>()#G2_C362::Method3.8161<System.Object>()#G1_C7::Method4.4811()#G3_C1329::Method5.15579()#G1_C7::Method6.4813<System.Object>()#G2_C362::Method7.8162<System.Object>()#"
		call void Generated857::M.G2_C362.A.T<class BaseClass0,class G3_C1329`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C362::ClassMethod1329.MI.8165()#G2_C362::ClassMethod1330.MI.8166<System.Object>()#G2_C362::ClassMethod1331.MI.8167<System.Object>()#G3_C1329::ClassMethod2065.MI.15586<System.Object>()#G3_C1329::ClassMethod2066.MI.15587<System.Object>()#G2_C362::Method0.8157()#G2_C362::Method1.8158()#G2_C362::Method2.8159<System.Object>()#G2_C362::Method3.8161<System.Object>()#G1_C7::Method4.4811()#G3_C1329::Method5.15579()#G1_C7::Method6.4813<System.Object>()#G2_C362::Method7.8162<System.Object>()#"
		call void Generated857::M.G2_C362.A.A<class G3_C1329`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C362::Method0.8157()#G2_C362::Method1.8158()#G2_C362::Method2.MI.8160<System.Object>()#G2_C362::Method3.8161<System.Object>()#"
		call void Generated857::M.IBase0<class G3_C1329`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C362::ClassMethod1329.MI.8165()#G2_C362::ClassMethod1330.MI.8166<System.Object>()#G2_C362::ClassMethod1331.MI.8167<System.Object>()#G3_C1329::ClassMethod2065.MI.15586<System.Object>()#G3_C1329::ClassMethod2066.MI.15587<System.Object>()#G3_C1329::ClassMethod4039.15582()#G3_C1329::ClassMethod4040.15583()#G3_C1329::ClassMethod4041.15584<System.Object>()#G3_C1329::ClassMethod4042.15585<System.Object>()#G2_C362::Method0.8157()#G2_C362::Method1.8158()#G2_C362::Method2.8159<System.Object>()#G2_C362::Method3.8161<System.Object>()#G3_C1329::Method4.15577()#G3_C1329::Method5.15579()#G3_C1329::Method6.15581<System.Object>()#G2_C362::Method7.8162<System.Object>()#"
		call void Generated857::M.G3_C1329.T<class BaseClass0,class G3_C1329`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C362::ClassMethod1329.MI.8165()#G2_C362::ClassMethod1330.MI.8166<System.Object>()#G2_C362::ClassMethod1331.MI.8167<System.Object>()#G3_C1329::ClassMethod2065.MI.15586<System.Object>()#G3_C1329::ClassMethod2066.MI.15587<System.Object>()#G3_C1329::ClassMethod4039.15582()#G3_C1329::ClassMethod4040.15583()#G3_C1329::ClassMethod4041.15584<System.Object>()#G3_C1329::ClassMethod4042.15585<System.Object>()#G2_C362::Method0.8157()#G2_C362::Method1.8158()#G2_C362::Method2.8159<System.Object>()#G2_C362::Method3.8161<System.Object>()#G3_C1329::Method4.15577()#G3_C1329::Method5.15579()#G3_C1329::Method6.15581<System.Object>()#G2_C362::Method7.8162<System.Object>()#"
		call void Generated857::M.G3_C1329.A<class G3_C1329`1<class BaseClass0>>(!!0,string)
		newobj instance void class G3_C1329`1<class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C362::ClassMethod1329.MI.8165()#G2_C362::ClassMethod1330.MI.8166<System.Object>()#G2_C362::ClassMethod1331.MI.8167<System.Object>()#G1_C7::Method4.4811()#G3_C1329::Method5.15579()#G1_C7::Method6.4813<System.Object>()#G1_C7::Method7.4815<System.Object>()#"
		call void Generated857::M.G1_C7.T.T<class BaseClass1,class BaseClass1,class G3_C1329`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C362::ClassMethod1329.MI.8165()#G2_C362::ClassMethod1330.MI.8166<System.Object>()#G2_C362::ClassMethod1331.MI.8167<System.Object>()#G1_C7::Method4.4811()#G3_C1329::Method5.15579()#G1_C7::Method6.4813<System.Object>()#G1_C7::Method7.4815<System.Object>()#"
		call void Generated857::M.G1_C7.B.T<class BaseClass1,class G3_C1329`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C362::ClassMethod1329.MI.8165()#G2_C362::ClassMethod1330.MI.8166<System.Object>()#G2_C362::ClassMethod1331.MI.8167<System.Object>()#G1_C7::Method4.4811()#G3_C1329::Method5.15579()#G1_C7::Method6.4813<System.Object>()#G1_C7::Method7.4815<System.Object>()#"
		call void Generated857::M.G1_C7.B.B<class G3_C1329`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C7::Method4.4811()#G3_C1329::Method5.15579()#G1_C7::Method6.MI.4814<System.Object>()#"
		call void Generated857::M.IBase1.T<class BaseClass1,class G3_C1329`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::Method4.4811()#G3_C1329::Method5.15579()#G1_C7::Method6.MI.4814<System.Object>()#"
		call void Generated857::M.IBase1.B<class G3_C1329`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C362::Method7.8162<System.Object>()#"
		call void Generated857::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G3_C1329`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C362::Method7.8162<System.Object>()#"
		call void Generated857::M.IBase2.B.T<class BaseClass1,class G3_C1329`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C362::Method7.8162<System.Object>()#"
		call void Generated857::M.IBase2.B.B<class G3_C1329`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1329::Method4.MI.15578()#G3_C1329::Method5.MI.15580()#G3_C1329::Method6.15581<System.Object>()#"
		call void Generated857::M.IBase1.T<class BaseClass0,class G3_C1329`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1329::Method4.MI.15578()#G3_C1329::Method5.MI.15580()#G3_C1329::Method6.15581<System.Object>()#"
		call void Generated857::M.IBase1.A<class G3_C1329`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C362::Method7.8162<System.Object>()#"
		call void Generated857::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G3_C1329`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C362::Method7.8162<System.Object>()#"
		call void Generated857::M.IBase2.A.T<class BaseClass1,class G3_C1329`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C362::Method7.8162<System.Object>()#"
		call void Generated857::M.IBase2.A.B<class G3_C1329`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C362::ClassMethod1329.MI.8165()#G2_C362::ClassMethod1330.MI.8166<System.Object>()#G2_C362::ClassMethod1331.MI.8167<System.Object>()#G3_C1329::ClassMethod2065.MI.15586<System.Object>()#G3_C1329::ClassMethod2066.MI.15587<System.Object>()#G2_C362::Method0.8157()#G2_C362::Method1.8158()#G2_C362::Method2.8159<System.Object>()#G2_C362::Method3.8161<System.Object>()#G1_C7::Method4.4811()#G3_C1329::Method5.15579()#G1_C7::Method6.4813<System.Object>()#G2_C362::Method7.8162<System.Object>()#"
		call void Generated857::M.G2_C362.T.T<class BaseClass0,class BaseClass1,class G3_C1329`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C362::ClassMethod1329.MI.8165()#G2_C362::ClassMethod1330.MI.8166<System.Object>()#G2_C362::ClassMethod1331.MI.8167<System.Object>()#G3_C1329::ClassMethod2065.MI.15586<System.Object>()#G3_C1329::ClassMethod2066.MI.15587<System.Object>()#G2_C362::Method0.8157()#G2_C362::Method1.8158()#G2_C362::Method2.8159<System.Object>()#G2_C362::Method3.8161<System.Object>()#G1_C7::Method4.4811()#G3_C1329::Method5.15579()#G1_C7::Method6.4813<System.Object>()#G2_C362::Method7.8162<System.Object>()#"
		call void Generated857::M.G2_C362.A.T<class BaseClass1,class G3_C1329`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C362::ClassMethod1329.MI.8165()#G2_C362::ClassMethod1330.MI.8166<System.Object>()#G2_C362::ClassMethod1331.MI.8167<System.Object>()#G3_C1329::ClassMethod2065.MI.15586<System.Object>()#G3_C1329::ClassMethod2066.MI.15587<System.Object>()#G2_C362::Method0.8157()#G2_C362::Method1.8158()#G2_C362::Method2.8159<System.Object>()#G2_C362::Method3.8161<System.Object>()#G1_C7::Method4.4811()#G3_C1329::Method5.15579()#G1_C7::Method6.4813<System.Object>()#G2_C362::Method7.8162<System.Object>()#"
		call void Generated857::M.G2_C362.A.B<class G3_C1329`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C362::Method0.8157()#G2_C362::Method1.8158()#G2_C362::Method2.MI.8160<System.Object>()#G2_C362::Method3.8161<System.Object>()#"
		call void Generated857::M.IBase0<class G3_C1329`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C362::ClassMethod1329.MI.8165()#G2_C362::ClassMethod1330.MI.8166<System.Object>()#G2_C362::ClassMethod1331.MI.8167<System.Object>()#G3_C1329::ClassMethod2065.MI.15586<System.Object>()#G3_C1329::ClassMethod2066.MI.15587<System.Object>()#G3_C1329::ClassMethod4039.15582()#G3_C1329::ClassMethod4040.15583()#G3_C1329::ClassMethod4041.15584<System.Object>()#G3_C1329::ClassMethod4042.15585<System.Object>()#G2_C362::Method0.8157()#G2_C362::Method1.8158()#G2_C362::Method2.8159<System.Object>()#G2_C362::Method3.8161<System.Object>()#G3_C1329::Method4.15577()#G3_C1329::Method5.15579()#G3_C1329::Method6.15581<System.Object>()#G2_C362::Method7.8162<System.Object>()#"
		call void Generated857::M.G3_C1329.T<class BaseClass1,class G3_C1329`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C362::ClassMethod1329.MI.8165()#G2_C362::ClassMethod1330.MI.8166<System.Object>()#G2_C362::ClassMethod1331.MI.8167<System.Object>()#G3_C1329::ClassMethod2065.MI.15586<System.Object>()#G3_C1329::ClassMethod2066.MI.15587<System.Object>()#G3_C1329::ClassMethod4039.15582()#G3_C1329::ClassMethod4040.15583()#G3_C1329::ClassMethod4041.15584<System.Object>()#G3_C1329::ClassMethod4042.15585<System.Object>()#G2_C362::Method0.8157()#G2_C362::Method1.8158()#G2_C362::Method2.8159<System.Object>()#G2_C362::Method3.8161<System.Object>()#G3_C1329::Method4.15577()#G3_C1329::Method5.15579()#G3_C1329::Method6.15581<System.Object>()#G2_C362::Method7.8162<System.Object>()#"
		call void Generated857::M.G3_C1329.B<class G3_C1329`1<class BaseClass1>>(!!0,string)
		newobj instance void class G2_C362`2<class BaseClass0,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C362::ClassMethod1329.MI.8165()#G2_C362::ClassMethod1330.MI.8166<System.Object>()#G2_C362::ClassMethod1331.MI.8167<System.Object>()#G1_C7::Method4.4811()#G1_C7::Method5.4812()#G1_C7::Method6.4813<System.Object>()#G1_C7::Method7.4815<System.Object>()#"
		call void Generated857::M.G1_C7.T.T<class BaseClass1,class BaseClass0,class G2_C362`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C362::ClassMethod1329.MI.8165()#G2_C362::ClassMethod1330.MI.8166<System.Object>()#G2_C362::ClassMethod1331.MI.8167<System.Object>()#G1_C7::Method4.4811()#G1_C7::Method5.4812()#G1_C7::Method6.4813<System.Object>()#G1_C7::Method7.4815<System.Object>()#"
		call void Generated857::M.G1_C7.B.T<class BaseClass0,class G2_C362`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C362::ClassMethod1329.MI.8165()#G2_C362::ClassMethod1330.MI.8166<System.Object>()#G2_C362::ClassMethod1331.MI.8167<System.Object>()#G1_C7::Method4.4811()#G1_C7::Method5.4812()#G1_C7::Method6.4813<System.Object>()#G1_C7::Method7.4815<System.Object>()#"
		call void Generated857::M.G1_C7.B.A<class G2_C362`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C7::Method4.4811()#G1_C7::Method5.4812()#G1_C7::Method6.MI.4814<System.Object>()#"
		call void Generated857::M.IBase1.T<class BaseClass1,class G2_C362`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::Method4.4811()#G1_C7::Method5.4812()#G1_C7::Method6.MI.4814<System.Object>()#"
		call void Generated857::M.IBase1.B<class G2_C362`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C362::Method7.8162<System.Object>()#"
		call void Generated857::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G2_C362`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C362::Method7.8162<System.Object>()#"
		call void Generated857::M.IBase2.B.T<class BaseClass1,class G2_C362`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C362::Method7.8162<System.Object>()#"
		call void Generated857::M.IBase2.B.B<class G2_C362`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C7::Method4.4811()#G1_C7::Method5.4812()#G1_C7::Method6.MI.4814<System.Object>()#"
		call void Generated857::M.IBase1.T<class BaseClass0,class G2_C362`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::Method4.4811()#G1_C7::Method5.4812()#G1_C7::Method6.MI.4814<System.Object>()#"
		call void Generated857::M.IBase1.A<class G2_C362`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C362::Method7.8162<System.Object>()#"
		call void Generated857::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G2_C362`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C362::Method7.8162<System.Object>()#"
		call void Generated857::M.IBase2.A.T<class BaseClass1,class G2_C362`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C362::Method7.8162<System.Object>()#"
		call void Generated857::M.IBase2.A.B<class G2_C362`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C362::ClassMethod1329.MI.8165()#G2_C362::ClassMethod1330.MI.8166<System.Object>()#G2_C362::ClassMethod1331.MI.8167<System.Object>()#G2_C362::ClassMethod2065.8163<System.Object>()#G2_C362::ClassMethod2066.8164<System.Object>()#G2_C362::Method0.8157()#G2_C362::Method1.8158()#G2_C362::Method2.8159<System.Object>()#G2_C362::Method3.8161<System.Object>()#G1_C7::Method4.4811()#G1_C7::Method5.4812()#G1_C7::Method6.4813<System.Object>()#G2_C362::Method7.8162<System.Object>()#"
		call void Generated857::M.G2_C362.T.T<class BaseClass0,class BaseClass0,class G2_C362`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C362::ClassMethod1329.MI.8165()#G2_C362::ClassMethod1330.MI.8166<System.Object>()#G2_C362::ClassMethod1331.MI.8167<System.Object>()#G2_C362::ClassMethod2065.8163<System.Object>()#G2_C362::ClassMethod2066.8164<System.Object>()#G2_C362::Method0.8157()#G2_C362::Method1.8158()#G2_C362::Method2.8159<System.Object>()#G2_C362::Method3.8161<System.Object>()#G1_C7::Method4.4811()#G1_C7::Method5.4812()#G1_C7::Method6.4813<System.Object>()#G2_C362::Method7.8162<System.Object>()#"
		call void Generated857::M.G2_C362.A.T<class BaseClass0,class G2_C362`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C362::ClassMethod1329.MI.8165()#G2_C362::ClassMethod1330.MI.8166<System.Object>()#G2_C362::ClassMethod1331.MI.8167<System.Object>()#G2_C362::ClassMethod2065.8163<System.Object>()#G2_C362::ClassMethod2066.8164<System.Object>()#G2_C362::Method0.8157()#G2_C362::Method1.8158()#G2_C362::Method2.8159<System.Object>()#G2_C362::Method3.8161<System.Object>()#G1_C7::Method4.4811()#G1_C7::Method5.4812()#G1_C7::Method6.4813<System.Object>()#G2_C362::Method7.8162<System.Object>()#"
		call void Generated857::M.G2_C362.A.A<class G2_C362`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C362::Method0.8157()#G2_C362::Method1.8158()#G2_C362::Method2.MI.8160<System.Object>()#G2_C362::Method3.8161<System.Object>()#"
		call void Generated857::M.IBase0<class G2_C362`2<class BaseClass0,class BaseClass0>>(!!0,string)
		newobj instance void class G2_C362`2<class BaseClass0,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C362::ClassMethod1329.MI.8165()#G2_C362::ClassMethod1330.MI.8166<System.Object>()#G2_C362::ClassMethod1331.MI.8167<System.Object>()#G1_C7::Method4.4811()#G1_C7::Method5.4812()#G1_C7::Method6.4813<System.Object>()#G1_C7::Method7.4815<System.Object>()#"
		call void Generated857::M.G1_C7.T.T<class BaseClass1,class BaseClass1,class G2_C362`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C362::ClassMethod1329.MI.8165()#G2_C362::ClassMethod1330.MI.8166<System.Object>()#G2_C362::ClassMethod1331.MI.8167<System.Object>()#G1_C7::Method4.4811()#G1_C7::Method5.4812()#G1_C7::Method6.4813<System.Object>()#G1_C7::Method7.4815<System.Object>()#"
		call void Generated857::M.G1_C7.B.T<class BaseClass1,class G2_C362`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C362::ClassMethod1329.MI.8165()#G2_C362::ClassMethod1330.MI.8166<System.Object>()#G2_C362::ClassMethod1331.MI.8167<System.Object>()#G1_C7::Method4.4811()#G1_C7::Method5.4812()#G1_C7::Method6.4813<System.Object>()#G1_C7::Method7.4815<System.Object>()#"
		call void Generated857::M.G1_C7.B.B<class G2_C362`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C7::Method4.4811()#G1_C7::Method5.4812()#G1_C7::Method6.MI.4814<System.Object>()#"
		call void Generated857::M.IBase1.T<class BaseClass1,class G2_C362`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::Method4.4811()#G1_C7::Method5.4812()#G1_C7::Method6.MI.4814<System.Object>()#"
		call void Generated857::M.IBase1.B<class G2_C362`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C362::Method7.8162<System.Object>()#"
		call void Generated857::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G2_C362`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C362::Method7.8162<System.Object>()#"
		call void Generated857::M.IBase2.B.T<class BaseClass1,class G2_C362`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C362::Method7.8162<System.Object>()#"
		call void Generated857::M.IBase2.B.B<class G2_C362`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C7::Method4.4811()#G1_C7::Method5.4812()#G1_C7::Method6.MI.4814<System.Object>()#"
		call void Generated857::M.IBase1.T<class BaseClass0,class G2_C362`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::Method4.4811()#G1_C7::Method5.4812()#G1_C7::Method6.MI.4814<System.Object>()#"
		call void Generated857::M.IBase1.A<class G2_C362`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C362::Method7.8162<System.Object>()#"
		call void Generated857::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G2_C362`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C362::Method7.8162<System.Object>()#"
		call void Generated857::M.IBase2.A.T<class BaseClass1,class G2_C362`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C362::Method7.8162<System.Object>()#"
		call void Generated857::M.IBase2.A.B<class G2_C362`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C362::ClassMethod1329.MI.8165()#G2_C362::ClassMethod1330.MI.8166<System.Object>()#G2_C362::ClassMethod1331.MI.8167<System.Object>()#G2_C362::ClassMethod2065.8163<System.Object>()#G2_C362::ClassMethod2066.8164<System.Object>()#G2_C362::Method0.8157()#G2_C362::Method1.8158()#G2_C362::Method2.8159<System.Object>()#G2_C362::Method3.8161<System.Object>()#G1_C7::Method4.4811()#G1_C7::Method5.4812()#G1_C7::Method6.4813<System.Object>()#G2_C362::Method7.8162<System.Object>()#"
		call void Generated857::M.G2_C362.T.T<class BaseClass0,class BaseClass1,class G2_C362`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C362::ClassMethod1329.MI.8165()#G2_C362::ClassMethod1330.MI.8166<System.Object>()#G2_C362::ClassMethod1331.MI.8167<System.Object>()#G2_C362::ClassMethod2065.8163<System.Object>()#G2_C362::ClassMethod2066.8164<System.Object>()#G2_C362::Method0.8157()#G2_C362::Method1.8158()#G2_C362::Method2.8159<System.Object>()#G2_C362::Method3.8161<System.Object>()#G1_C7::Method4.4811()#G1_C7::Method5.4812()#G1_C7::Method6.4813<System.Object>()#G2_C362::Method7.8162<System.Object>()#"
		call void Generated857::M.G2_C362.A.T<class BaseClass1,class G2_C362`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C362::ClassMethod1329.MI.8165()#G2_C362::ClassMethod1330.MI.8166<System.Object>()#G2_C362::ClassMethod1331.MI.8167<System.Object>()#G2_C362::ClassMethod2065.8163<System.Object>()#G2_C362::ClassMethod2066.8164<System.Object>()#G2_C362::Method0.8157()#G2_C362::Method1.8158()#G2_C362::Method2.8159<System.Object>()#G2_C362::Method3.8161<System.Object>()#G1_C7::Method4.4811()#G1_C7::Method5.4812()#G1_C7::Method6.4813<System.Object>()#G2_C362::Method7.8162<System.Object>()#"
		call void Generated857::M.G2_C362.A.B<class G2_C362`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C362::Method0.8157()#G2_C362::Method1.8158()#G2_C362::Method2.MI.8160<System.Object>()#G2_C362::Method3.8161<System.Object>()#"
		call void Generated857::M.IBase0<class G2_C362`2<class BaseClass0,class BaseClass1>>(!!0,string)
		newobj instance void class G2_C362`2<class BaseClass1,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C362::ClassMethod1329.MI.8165()#G2_C362::ClassMethod1330.MI.8166<System.Object>()#G2_C362::ClassMethod1331.MI.8167<System.Object>()#G1_C7::Method4.4811()#G1_C7::Method5.4812()#G1_C7::Method6.4813<System.Object>()#G1_C7::Method7.4815<System.Object>()#"
		call void Generated857::M.G1_C7.T.T<class BaseClass1,class BaseClass0,class G2_C362`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C362::ClassMethod1329.MI.8165()#G2_C362::ClassMethod1330.MI.8166<System.Object>()#G2_C362::ClassMethod1331.MI.8167<System.Object>()#G1_C7::Method4.4811()#G1_C7::Method5.4812()#G1_C7::Method6.4813<System.Object>()#G1_C7::Method7.4815<System.Object>()#"
		call void Generated857::M.G1_C7.B.T<class BaseClass0,class G2_C362`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C362::ClassMethod1329.MI.8165()#G2_C362::ClassMethod1330.MI.8166<System.Object>()#G2_C362::ClassMethod1331.MI.8167<System.Object>()#G1_C7::Method4.4811()#G1_C7::Method5.4812()#G1_C7::Method6.4813<System.Object>()#G1_C7::Method7.4815<System.Object>()#"
		call void Generated857::M.G1_C7.B.A<class G2_C362`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C7::Method4.4811()#G1_C7::Method5.4812()#G1_C7::Method6.MI.4814<System.Object>()#"
		call void Generated857::M.IBase1.T<class BaseClass1,class G2_C362`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::Method4.4811()#G1_C7::Method5.4812()#G1_C7::Method6.MI.4814<System.Object>()#"
		call void Generated857::M.IBase1.B<class G2_C362`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C362::Method7.8162<System.Object>()#"
		call void Generated857::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G2_C362`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C362::Method7.8162<System.Object>()#"
		call void Generated857::M.IBase2.B.T<class BaseClass1,class G2_C362`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C362::Method7.8162<System.Object>()#"
		call void Generated857::M.IBase2.B.B<class G2_C362`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C7::Method4.4811()#G1_C7::Method5.4812()#G1_C7::Method6.MI.4814<System.Object>()#"
		call void Generated857::M.IBase1.T<class BaseClass0,class G2_C362`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::Method4.4811()#G1_C7::Method5.4812()#G1_C7::Method6.MI.4814<System.Object>()#"
		call void Generated857::M.IBase1.A<class G2_C362`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C362::Method7.8162<System.Object>()#"
		call void Generated857::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G2_C362`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C362::Method7.8162<System.Object>()#"
		call void Generated857::M.IBase2.A.T<class BaseClass1,class G2_C362`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C362::Method7.8162<System.Object>()#"
		call void Generated857::M.IBase2.A.B<class G2_C362`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C362::ClassMethod1329.MI.8165()#G2_C362::ClassMethod1330.MI.8166<System.Object>()#G2_C362::ClassMethod1331.MI.8167<System.Object>()#G2_C362::ClassMethod2065.8163<System.Object>()#G2_C362::ClassMethod2066.8164<System.Object>()#G2_C362::Method0.8157()#G2_C362::Method1.8158()#G2_C362::Method2.8159<System.Object>()#G2_C362::Method3.8161<System.Object>()#G1_C7::Method4.4811()#G1_C7::Method5.4812()#G1_C7::Method6.4813<System.Object>()#G2_C362::Method7.8162<System.Object>()#"
		call void Generated857::M.G2_C362.T.T<class BaseClass1,class BaseClass0,class G2_C362`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C362::ClassMethod1329.MI.8165()#G2_C362::ClassMethod1330.MI.8166<System.Object>()#G2_C362::ClassMethod1331.MI.8167<System.Object>()#G2_C362::ClassMethod2065.8163<System.Object>()#G2_C362::ClassMethod2066.8164<System.Object>()#G2_C362::Method0.8157()#G2_C362::Method1.8158()#G2_C362::Method2.8159<System.Object>()#G2_C362::Method3.8161<System.Object>()#G1_C7::Method4.4811()#G1_C7::Method5.4812()#G1_C7::Method6.4813<System.Object>()#G2_C362::Method7.8162<System.Object>()#"
		call void Generated857::M.G2_C362.B.T<class BaseClass0,class G2_C362`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C362::ClassMethod1329.MI.8165()#G2_C362::ClassMethod1330.MI.8166<System.Object>()#G2_C362::ClassMethod1331.MI.8167<System.Object>()#G2_C362::ClassMethod2065.8163<System.Object>()#G2_C362::ClassMethod2066.8164<System.Object>()#G2_C362::Method0.8157()#G2_C362::Method1.8158()#G2_C362::Method2.8159<System.Object>()#G2_C362::Method3.8161<System.Object>()#G1_C7::Method4.4811()#G1_C7::Method5.4812()#G1_C7::Method6.4813<System.Object>()#G2_C362::Method7.8162<System.Object>()#"
		call void Generated857::M.G2_C362.B.A<class G2_C362`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C362::Method0.8157()#G2_C362::Method1.8158()#G2_C362::Method2.MI.8160<System.Object>()#G2_C362::Method3.8161<System.Object>()#"
		call void Generated857::M.IBase0<class G2_C362`2<class BaseClass1,class BaseClass0>>(!!0,string)
		newobj instance void class G2_C362`2<class BaseClass1,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C362::ClassMethod1329.MI.8165()#G2_C362::ClassMethod1330.MI.8166<System.Object>()#G2_C362::ClassMethod1331.MI.8167<System.Object>()#G1_C7::Method4.4811()#G1_C7::Method5.4812()#G1_C7::Method6.4813<System.Object>()#G1_C7::Method7.4815<System.Object>()#"
		call void Generated857::M.G1_C7.T.T<class BaseClass1,class BaseClass1,class G2_C362`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C362::ClassMethod1329.MI.8165()#G2_C362::ClassMethod1330.MI.8166<System.Object>()#G2_C362::ClassMethod1331.MI.8167<System.Object>()#G1_C7::Method4.4811()#G1_C7::Method5.4812()#G1_C7::Method6.4813<System.Object>()#G1_C7::Method7.4815<System.Object>()#"
		call void Generated857::M.G1_C7.B.T<class BaseClass1,class G2_C362`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C362::ClassMethod1329.MI.8165()#G2_C362::ClassMethod1330.MI.8166<System.Object>()#G2_C362::ClassMethod1331.MI.8167<System.Object>()#G1_C7::Method4.4811()#G1_C7::Method5.4812()#G1_C7::Method6.4813<System.Object>()#G1_C7::Method7.4815<System.Object>()#"
		call void Generated857::M.G1_C7.B.B<class G2_C362`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C7::Method4.4811()#G1_C7::Method5.4812()#G1_C7::Method6.MI.4814<System.Object>()#"
		call void Generated857::M.IBase1.T<class BaseClass1,class G2_C362`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::Method4.4811()#G1_C7::Method5.4812()#G1_C7::Method6.MI.4814<System.Object>()#"
		call void Generated857::M.IBase1.B<class G2_C362`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C362::Method7.8162<System.Object>()#"
		call void Generated857::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G2_C362`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C362::Method7.8162<System.Object>()#"
		call void Generated857::M.IBase2.B.T<class BaseClass1,class G2_C362`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C362::Method7.8162<System.Object>()#"
		call void Generated857::M.IBase2.B.B<class G2_C362`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C7::Method4.4811()#G1_C7::Method5.4812()#G1_C7::Method6.MI.4814<System.Object>()#"
		call void Generated857::M.IBase1.T<class BaseClass0,class G2_C362`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::Method4.4811()#G1_C7::Method5.4812()#G1_C7::Method6.MI.4814<System.Object>()#"
		call void Generated857::M.IBase1.A<class G2_C362`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C362::Method7.8162<System.Object>()#"
		call void Generated857::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G2_C362`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C362::Method7.8162<System.Object>()#"
		call void Generated857::M.IBase2.A.T<class BaseClass1,class G2_C362`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C362::Method7.8162<System.Object>()#"
		call void Generated857::M.IBase2.A.B<class G2_C362`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C362::ClassMethod1329.MI.8165()#G2_C362::ClassMethod1330.MI.8166<System.Object>()#G2_C362::ClassMethod1331.MI.8167<System.Object>()#G2_C362::ClassMethod2065.8163<System.Object>()#G2_C362::ClassMethod2066.8164<System.Object>()#G2_C362::Method0.8157()#G2_C362::Method1.8158()#G2_C362::Method2.8159<System.Object>()#G2_C362::Method3.8161<System.Object>()#G1_C7::Method4.4811()#G1_C7::Method5.4812()#G1_C7::Method6.4813<System.Object>()#G2_C362::Method7.8162<System.Object>()#"
		call void Generated857::M.G2_C362.T.T<class BaseClass1,class BaseClass1,class G2_C362`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C362::ClassMethod1329.MI.8165()#G2_C362::ClassMethod1330.MI.8166<System.Object>()#G2_C362::ClassMethod1331.MI.8167<System.Object>()#G2_C362::ClassMethod2065.8163<System.Object>()#G2_C362::ClassMethod2066.8164<System.Object>()#G2_C362::Method0.8157()#G2_C362::Method1.8158()#G2_C362::Method2.8159<System.Object>()#G2_C362::Method3.8161<System.Object>()#G1_C7::Method4.4811()#G1_C7::Method5.4812()#G1_C7::Method6.4813<System.Object>()#G2_C362::Method7.8162<System.Object>()#"
		call void Generated857::M.G2_C362.B.T<class BaseClass1,class G2_C362`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C362::ClassMethod1329.MI.8165()#G2_C362::ClassMethod1330.MI.8166<System.Object>()#G2_C362::ClassMethod1331.MI.8167<System.Object>()#G2_C362::ClassMethod2065.8163<System.Object>()#G2_C362::ClassMethod2066.8164<System.Object>()#G2_C362::Method0.8157()#G2_C362::Method1.8158()#G2_C362::Method2.8159<System.Object>()#G2_C362::Method3.8161<System.Object>()#G1_C7::Method4.4811()#G1_C7::Method5.4812()#G1_C7::Method6.4813<System.Object>()#G2_C362::Method7.8162<System.Object>()#"
		call void Generated857::M.G2_C362.B.B<class G2_C362`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C362::Method0.8157()#G2_C362::Method1.8158()#G2_C362::Method2.MI.8160<System.Object>()#G2_C362::Method3.8161<System.Object>()#"
		call void Generated857::M.IBase0<class G2_C362`2<class BaseClass1,class BaseClass1>>(!!0,string)
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
		newobj instance void class G3_C1329`1<class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C7`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass1,class BaseClass0>::ClassMethod1331<object>()
		calli default string(class G3_C1329`1<class BaseClass0>)
		ldstr "G2_C362::ClassMethod1331.MI.8167<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass0> on type class G3_C1329`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C7`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass1,class BaseClass0>::ClassMethod1330<object>()
		calli default string(class G3_C1329`1<class BaseClass0>)
		ldstr "G2_C362::ClassMethod1330.MI.8166<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass0> on type class G3_C1329`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C7`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass1,class BaseClass0>::ClassMethod1329()
		calli default string(class G3_C1329`1<class BaseClass0>)
		ldstr "G2_C362::ClassMethod1329.MI.8165()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass0> on type class G3_C1329`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C7`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass1,class BaseClass0>::ClassMethod1328()
		calli default string(class G3_C1329`1<class BaseClass0>)
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass0> on type class G3_C1329`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C7`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1329`1<class BaseClass0>)
		ldstr "G1_C7::Method7.4815<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass0> on type class G3_C1329`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C7`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass1,class BaseClass0>::Method6<object>()
		calli default string(class G3_C1329`1<class BaseClass0>)
		ldstr "G1_C7::Method6.4813<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass0> on type class G3_C1329`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C7`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass1,class BaseClass0>::Method5()
		calli default string(class G3_C1329`1<class BaseClass0>)
		ldstr "G3_C1329::Method5.15579()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass0> on type class G3_C1329`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C7`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass1,class BaseClass0>::Method4()
		calli default string(class G3_C1329`1<class BaseClass0>)
		ldstr "G1_C7::Method4.4811()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass0> on type class G3_C1329`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method4()
		calli default string(class G3_C1329`1<class BaseClass0>)
		ldstr "G1_C7::Method4.4811()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1329`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method5()
		calli default string(class G3_C1329`1<class BaseClass0>)
		ldstr "G3_C1329::Method5.15579()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1329`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method6<object>()
		calli default string(class G3_C1329`1<class BaseClass0>)
		ldstr "G1_C7::Method6.MI.4814<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1329`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1329`1<class BaseClass0>)
		ldstr "G2_C362::Method7.8162<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G3_C1329`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G3_C1329`1<class BaseClass0>)
		ldstr "G3_C1329::Method4.MI.15578()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1329`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G3_C1329`1<class BaseClass0>)
		ldstr "G3_C1329::Method5.MI.15580()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1329`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G3_C1329`1<class BaseClass0>)
		ldstr "G3_C1329::Method6.15581<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1329`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1329`1<class BaseClass0>)
		ldstr "G2_C362::Method7.8162<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1329`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C362`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C362`2<class BaseClass0,class BaseClass0>::ClassMethod2066<object>()
		calli default string(class G3_C1329`1<class BaseClass0>)
		ldstr "G3_C1329::ClassMethod2066.MI.15587<System.Object>()"
		ldstr "class G2_C362`2<class BaseClass0,class BaseClass0> on type class G3_C1329`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C362`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C362`2<class BaseClass0,class BaseClass0>::ClassMethod2065<object>()
		calli default string(class G3_C1329`1<class BaseClass0>)
		ldstr "G3_C1329::ClassMethod2065.MI.15586<System.Object>()"
		ldstr "class G2_C362`2<class BaseClass0,class BaseClass0> on type class G3_C1329`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C362`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C362`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1329`1<class BaseClass0>)
		ldstr "G2_C362::Method7.8162<System.Object>()"
		ldstr "class G2_C362`2<class BaseClass0,class BaseClass0> on type class G3_C1329`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C362`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C362`2<class BaseClass0,class BaseClass0>::Method3<object>()
		calli default string(class G3_C1329`1<class BaseClass0>)
		ldstr "G2_C362::Method3.8161<System.Object>()"
		ldstr "class G2_C362`2<class BaseClass0,class BaseClass0> on type class G3_C1329`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C362`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C362`2<class BaseClass0,class BaseClass0>::Method2<object>()
		calli default string(class G3_C1329`1<class BaseClass0>)
		ldstr "G2_C362::Method2.8159<System.Object>()"
		ldstr "class G2_C362`2<class BaseClass0,class BaseClass0> on type class G3_C1329`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C362`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C362`2<class BaseClass0,class BaseClass0>::Method1()
		calli default string(class G3_C1329`1<class BaseClass0>)
		ldstr "G2_C362::Method1.8158()"
		ldstr "class G2_C362`2<class BaseClass0,class BaseClass0> on type class G3_C1329`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C362`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C362`2<class BaseClass0,class BaseClass0>::Method0()
		calli default string(class G3_C1329`1<class BaseClass0>)
		ldstr "G2_C362::Method0.8157()"
		ldstr "class G2_C362`2<class BaseClass0,class BaseClass0> on type class G3_C1329`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C362`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C362`2<class BaseClass0,class BaseClass0>::ClassMethod1331<object>()
		calli default string(class G3_C1329`1<class BaseClass0>)
		ldstr "G2_C362::ClassMethod1331.MI.8167<System.Object>()"
		ldstr "class G2_C362`2<class BaseClass0,class BaseClass0> on type class G3_C1329`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C362`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C362`2<class BaseClass0,class BaseClass0>::ClassMethod1330<object>()
		calli default string(class G3_C1329`1<class BaseClass0>)
		ldstr "G2_C362::ClassMethod1330.MI.8166<System.Object>()"
		ldstr "class G2_C362`2<class BaseClass0,class BaseClass0> on type class G3_C1329`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C362`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C362`2<class BaseClass0,class BaseClass0>::ClassMethod1329()
		calli default string(class G3_C1329`1<class BaseClass0>)
		ldstr "G2_C362::ClassMethod1329.MI.8165()"
		ldstr "class G2_C362`2<class BaseClass0,class BaseClass0> on type class G3_C1329`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C362`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C362`2<class BaseClass0,class BaseClass0>::ClassMethod1328()
		calli default string(class G3_C1329`1<class BaseClass0>)
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G2_C362`2<class BaseClass0,class BaseClass0> on type class G3_C1329`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C362`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C362`2<class BaseClass0,class BaseClass0>::Method6<object>()
		calli default string(class G3_C1329`1<class BaseClass0>)
		ldstr "G1_C7::Method6.4813<System.Object>()"
		ldstr "class G2_C362`2<class BaseClass0,class BaseClass0> on type class G3_C1329`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C362`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C362`2<class BaseClass0,class BaseClass0>::Method5()
		calli default string(class G3_C1329`1<class BaseClass0>)
		ldstr "G3_C1329::Method5.15579()"
		ldstr "class G2_C362`2<class BaseClass0,class BaseClass0> on type class G3_C1329`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C362`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C362`2<class BaseClass0,class BaseClass0>::Method4()
		calli default string(class G3_C1329`1<class BaseClass0>)
		ldstr "G1_C7::Method4.4811()"
		ldstr "class G2_C362`2<class BaseClass0,class BaseClass0> on type class G3_C1329`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method0()
		calli default string(class G3_C1329`1<class BaseClass0>)
		ldstr "G2_C362::Method0.8157()"
		ldstr "IBase0 on type class G3_C1329`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method1()
		calli default string(class G3_C1329`1<class BaseClass0>)
		ldstr "G2_C362::Method1.8158()"
		ldstr "IBase0 on type class G3_C1329`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method2<object>()
		calli default string(class G3_C1329`1<class BaseClass0>)
		ldstr "G2_C362::Method2.MI.8160<System.Object>()"
		ldstr "IBase0 on type class G3_C1329`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method3<object>()
		calli default string(class G3_C1329`1<class BaseClass0>)
		ldstr "G2_C362::Method3.8161<System.Object>()"
		ldstr "IBase0 on type class G3_C1329`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1329`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1329`1<class BaseClass0>::ClassMethod4042<object>()
		calli default string(class G3_C1329`1<class BaseClass0>)
		ldstr "G3_C1329::ClassMethod4042.15585<System.Object>()"
		ldstr "class G3_C1329`1<class BaseClass0> on type class G3_C1329`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1329`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1329`1<class BaseClass0>::ClassMethod4041<object>()
		calli default string(class G3_C1329`1<class BaseClass0>)
		ldstr "G3_C1329::ClassMethod4041.15584<System.Object>()"
		ldstr "class G3_C1329`1<class BaseClass0> on type class G3_C1329`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1329`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1329`1<class BaseClass0>::ClassMethod4040()
		calli default string(class G3_C1329`1<class BaseClass0>)
		ldstr "G3_C1329::ClassMethod4040.15583()"
		ldstr "class G3_C1329`1<class BaseClass0> on type class G3_C1329`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1329`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1329`1<class BaseClass0>::ClassMethod4039()
		calli default string(class G3_C1329`1<class BaseClass0>)
		ldstr "G3_C1329::ClassMethod4039.15582()"
		ldstr "class G3_C1329`1<class BaseClass0> on type class G3_C1329`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1329`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1329`1<class BaseClass0>::Method6<object>()
		calli default string(class G3_C1329`1<class BaseClass0>)
		ldstr "G3_C1329::Method6.15581<System.Object>()"
		ldstr "class G3_C1329`1<class BaseClass0> on type class G3_C1329`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1329`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1329`1<class BaseClass0>::Method5()
		calli default string(class G3_C1329`1<class BaseClass0>)
		ldstr "G3_C1329::Method5.15579()"
		ldstr "class G3_C1329`1<class BaseClass0> on type class G3_C1329`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1329`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1329`1<class BaseClass0>::Method4()
		calli default string(class G3_C1329`1<class BaseClass0>)
		ldstr "G3_C1329::Method4.15577()"
		ldstr "class G3_C1329`1<class BaseClass0> on type class G3_C1329`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1329`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1329`1<class BaseClass0>::ClassMethod2066<object>()
		calli default string(class G3_C1329`1<class BaseClass0>)
		ldstr "G3_C1329::ClassMethod2066.MI.15587<System.Object>()"
		ldstr "class G3_C1329`1<class BaseClass0> on type class G3_C1329`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1329`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1329`1<class BaseClass0>::ClassMethod2065<object>()
		calli default string(class G3_C1329`1<class BaseClass0>)
		ldstr "G3_C1329::ClassMethod2065.MI.15586<System.Object>()"
		ldstr "class G3_C1329`1<class BaseClass0> on type class G3_C1329`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1329`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1329`1<class BaseClass0>::Method7<object>()
		calli default string(class G3_C1329`1<class BaseClass0>)
		ldstr "G2_C362::Method7.8162<System.Object>()"
		ldstr "class G3_C1329`1<class BaseClass0> on type class G3_C1329`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1329`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1329`1<class BaseClass0>::Method3<object>()
		calli default string(class G3_C1329`1<class BaseClass0>)
		ldstr "G2_C362::Method3.8161<System.Object>()"
		ldstr "class G3_C1329`1<class BaseClass0> on type class G3_C1329`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1329`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1329`1<class BaseClass0>::Method2<object>()
		calli default string(class G3_C1329`1<class BaseClass0>)
		ldstr "G2_C362::Method2.8159<System.Object>()"
		ldstr "class G3_C1329`1<class BaseClass0> on type class G3_C1329`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1329`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1329`1<class BaseClass0>::Method1()
		calli default string(class G3_C1329`1<class BaseClass0>)
		ldstr "G2_C362::Method1.8158()"
		ldstr "class G3_C1329`1<class BaseClass0> on type class G3_C1329`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1329`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1329`1<class BaseClass0>::Method0()
		calli default string(class G3_C1329`1<class BaseClass0>)
		ldstr "G2_C362::Method0.8157()"
		ldstr "class G3_C1329`1<class BaseClass0> on type class G3_C1329`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1329`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1329`1<class BaseClass0>::ClassMethod1331<object>()
		calli default string(class G3_C1329`1<class BaseClass0>)
		ldstr "G2_C362::ClassMethod1331.MI.8167<System.Object>()"
		ldstr "class G3_C1329`1<class BaseClass0> on type class G3_C1329`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1329`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1329`1<class BaseClass0>::ClassMethod1330<object>()
		calli default string(class G3_C1329`1<class BaseClass0>)
		ldstr "G2_C362::ClassMethod1330.MI.8166<System.Object>()"
		ldstr "class G3_C1329`1<class BaseClass0> on type class G3_C1329`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1329`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1329`1<class BaseClass0>::ClassMethod1329()
		calli default string(class G3_C1329`1<class BaseClass0>)
		ldstr "G2_C362::ClassMethod1329.MI.8165()"
		ldstr "class G3_C1329`1<class BaseClass0> on type class G3_C1329`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1329`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1329`1<class BaseClass0>::ClassMethod1328()
		calli default string(class G3_C1329`1<class BaseClass0>)
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G3_C1329`1<class BaseClass0> on type class G3_C1329`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G3_C1329`1<class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass1,class BaseClass1>::ClassMethod1331<object>()
		calli default string(class G3_C1329`1<class BaseClass1>)
		ldstr "G2_C362::ClassMethod1331.MI.8167<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G3_C1329`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass1,class BaseClass1>::ClassMethod1330<object>()
		calli default string(class G3_C1329`1<class BaseClass1>)
		ldstr "G2_C362::ClassMethod1330.MI.8166<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G3_C1329`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass1,class BaseClass1>::ClassMethod1329()
		calli default string(class G3_C1329`1<class BaseClass1>)
		ldstr "G2_C362::ClassMethod1329.MI.8165()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G3_C1329`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass1,class BaseClass1>::ClassMethod1328()
		calli default string(class G3_C1329`1<class BaseClass1>)
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G3_C1329`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1329`1<class BaseClass1>)
		ldstr "G1_C7::Method7.4815<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G3_C1329`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass1,class BaseClass1>::Method6<object>()
		calli default string(class G3_C1329`1<class BaseClass1>)
		ldstr "G1_C7::Method6.4813<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G3_C1329`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass1,class BaseClass1>::Method5()
		calli default string(class G3_C1329`1<class BaseClass1>)
		ldstr "G3_C1329::Method5.15579()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G3_C1329`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass1,class BaseClass1>::Method4()
		calli default string(class G3_C1329`1<class BaseClass1>)
		ldstr "G1_C7::Method4.4811()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G3_C1329`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method4()
		calli default string(class G3_C1329`1<class BaseClass1>)
		ldstr "G1_C7::Method4.4811()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1329`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method5()
		calli default string(class G3_C1329`1<class BaseClass1>)
		ldstr "G3_C1329::Method5.15579()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1329`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method6<object>()
		calli default string(class G3_C1329`1<class BaseClass1>)
		ldstr "G1_C7::Method6.MI.4814<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1329`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1329`1<class BaseClass1>)
		ldstr "G2_C362::Method7.8162<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G3_C1329`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G3_C1329`1<class BaseClass1>)
		ldstr "G3_C1329::Method4.MI.15578()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1329`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G3_C1329`1<class BaseClass1>)
		ldstr "G3_C1329::Method5.MI.15580()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1329`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G3_C1329`1<class BaseClass1>)
		ldstr "G3_C1329::Method6.15581<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1329`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1329`1<class BaseClass1>)
		ldstr "G2_C362::Method7.8162<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1329`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C362`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C362`2<class BaseClass0,class BaseClass1>::ClassMethod2066<object>()
		calli default string(class G3_C1329`1<class BaseClass1>)
		ldstr "G3_C1329::ClassMethod2066.MI.15587<System.Object>()"
		ldstr "class G2_C362`2<class BaseClass0,class BaseClass1> on type class G3_C1329`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C362`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C362`2<class BaseClass0,class BaseClass1>::ClassMethod2065<object>()
		calli default string(class G3_C1329`1<class BaseClass1>)
		ldstr "G3_C1329::ClassMethod2065.MI.15586<System.Object>()"
		ldstr "class G2_C362`2<class BaseClass0,class BaseClass1> on type class G3_C1329`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C362`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C362`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1329`1<class BaseClass1>)
		ldstr "G2_C362::Method7.8162<System.Object>()"
		ldstr "class G2_C362`2<class BaseClass0,class BaseClass1> on type class G3_C1329`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C362`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C362`2<class BaseClass0,class BaseClass1>::Method3<object>()
		calli default string(class G3_C1329`1<class BaseClass1>)
		ldstr "G2_C362::Method3.8161<System.Object>()"
		ldstr "class G2_C362`2<class BaseClass0,class BaseClass1> on type class G3_C1329`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C362`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C362`2<class BaseClass0,class BaseClass1>::Method2<object>()
		calli default string(class G3_C1329`1<class BaseClass1>)
		ldstr "G2_C362::Method2.8159<System.Object>()"
		ldstr "class G2_C362`2<class BaseClass0,class BaseClass1> on type class G3_C1329`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C362`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C362`2<class BaseClass0,class BaseClass1>::Method1()
		calli default string(class G3_C1329`1<class BaseClass1>)
		ldstr "G2_C362::Method1.8158()"
		ldstr "class G2_C362`2<class BaseClass0,class BaseClass1> on type class G3_C1329`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C362`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C362`2<class BaseClass0,class BaseClass1>::Method0()
		calli default string(class G3_C1329`1<class BaseClass1>)
		ldstr "G2_C362::Method0.8157()"
		ldstr "class G2_C362`2<class BaseClass0,class BaseClass1> on type class G3_C1329`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C362`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C362`2<class BaseClass0,class BaseClass1>::ClassMethod1331<object>()
		calli default string(class G3_C1329`1<class BaseClass1>)
		ldstr "G2_C362::ClassMethod1331.MI.8167<System.Object>()"
		ldstr "class G2_C362`2<class BaseClass0,class BaseClass1> on type class G3_C1329`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C362`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C362`2<class BaseClass0,class BaseClass1>::ClassMethod1330<object>()
		calli default string(class G3_C1329`1<class BaseClass1>)
		ldstr "G2_C362::ClassMethod1330.MI.8166<System.Object>()"
		ldstr "class G2_C362`2<class BaseClass0,class BaseClass1> on type class G3_C1329`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C362`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C362`2<class BaseClass0,class BaseClass1>::ClassMethod1329()
		calli default string(class G3_C1329`1<class BaseClass1>)
		ldstr "G2_C362::ClassMethod1329.MI.8165()"
		ldstr "class G2_C362`2<class BaseClass0,class BaseClass1> on type class G3_C1329`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C362`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C362`2<class BaseClass0,class BaseClass1>::ClassMethod1328()
		calli default string(class G3_C1329`1<class BaseClass1>)
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G2_C362`2<class BaseClass0,class BaseClass1> on type class G3_C1329`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C362`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C362`2<class BaseClass0,class BaseClass1>::Method6<object>()
		calli default string(class G3_C1329`1<class BaseClass1>)
		ldstr "G1_C7::Method6.4813<System.Object>()"
		ldstr "class G2_C362`2<class BaseClass0,class BaseClass1> on type class G3_C1329`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C362`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C362`2<class BaseClass0,class BaseClass1>::Method5()
		calli default string(class G3_C1329`1<class BaseClass1>)
		ldstr "G3_C1329::Method5.15579()"
		ldstr "class G2_C362`2<class BaseClass0,class BaseClass1> on type class G3_C1329`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C362`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C362`2<class BaseClass0,class BaseClass1>::Method4()
		calli default string(class G3_C1329`1<class BaseClass1>)
		ldstr "G1_C7::Method4.4811()"
		ldstr "class G2_C362`2<class BaseClass0,class BaseClass1> on type class G3_C1329`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method0()
		calli default string(class G3_C1329`1<class BaseClass1>)
		ldstr "G2_C362::Method0.8157()"
		ldstr "IBase0 on type class G3_C1329`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method1()
		calli default string(class G3_C1329`1<class BaseClass1>)
		ldstr "G2_C362::Method1.8158()"
		ldstr "IBase0 on type class G3_C1329`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method2<object>()
		calli default string(class G3_C1329`1<class BaseClass1>)
		ldstr "G2_C362::Method2.MI.8160<System.Object>()"
		ldstr "IBase0 on type class G3_C1329`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method3<object>()
		calli default string(class G3_C1329`1<class BaseClass1>)
		ldstr "G2_C362::Method3.8161<System.Object>()"
		ldstr "IBase0 on type class G3_C1329`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1329`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1329`1<class BaseClass1>::ClassMethod4042<object>()
		calli default string(class G3_C1329`1<class BaseClass1>)
		ldstr "G3_C1329::ClassMethod4042.15585<System.Object>()"
		ldstr "class G3_C1329`1<class BaseClass1> on type class G3_C1329`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1329`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1329`1<class BaseClass1>::ClassMethod4041<object>()
		calli default string(class G3_C1329`1<class BaseClass1>)
		ldstr "G3_C1329::ClassMethod4041.15584<System.Object>()"
		ldstr "class G3_C1329`1<class BaseClass1> on type class G3_C1329`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1329`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1329`1<class BaseClass1>::ClassMethod4040()
		calli default string(class G3_C1329`1<class BaseClass1>)
		ldstr "G3_C1329::ClassMethod4040.15583()"
		ldstr "class G3_C1329`1<class BaseClass1> on type class G3_C1329`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1329`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1329`1<class BaseClass1>::ClassMethod4039()
		calli default string(class G3_C1329`1<class BaseClass1>)
		ldstr "G3_C1329::ClassMethod4039.15582()"
		ldstr "class G3_C1329`1<class BaseClass1> on type class G3_C1329`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1329`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1329`1<class BaseClass1>::Method6<object>()
		calli default string(class G3_C1329`1<class BaseClass1>)
		ldstr "G3_C1329::Method6.15581<System.Object>()"
		ldstr "class G3_C1329`1<class BaseClass1> on type class G3_C1329`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1329`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1329`1<class BaseClass1>::Method5()
		calli default string(class G3_C1329`1<class BaseClass1>)
		ldstr "G3_C1329::Method5.15579()"
		ldstr "class G3_C1329`1<class BaseClass1> on type class G3_C1329`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1329`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1329`1<class BaseClass1>::Method4()
		calli default string(class G3_C1329`1<class BaseClass1>)
		ldstr "G3_C1329::Method4.15577()"
		ldstr "class G3_C1329`1<class BaseClass1> on type class G3_C1329`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1329`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1329`1<class BaseClass1>::ClassMethod2066<object>()
		calli default string(class G3_C1329`1<class BaseClass1>)
		ldstr "G3_C1329::ClassMethod2066.MI.15587<System.Object>()"
		ldstr "class G3_C1329`1<class BaseClass1> on type class G3_C1329`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1329`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1329`1<class BaseClass1>::ClassMethod2065<object>()
		calli default string(class G3_C1329`1<class BaseClass1>)
		ldstr "G3_C1329::ClassMethod2065.MI.15586<System.Object>()"
		ldstr "class G3_C1329`1<class BaseClass1> on type class G3_C1329`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1329`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1329`1<class BaseClass1>::Method7<object>()
		calli default string(class G3_C1329`1<class BaseClass1>)
		ldstr "G2_C362::Method7.8162<System.Object>()"
		ldstr "class G3_C1329`1<class BaseClass1> on type class G3_C1329`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1329`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1329`1<class BaseClass1>::Method3<object>()
		calli default string(class G3_C1329`1<class BaseClass1>)
		ldstr "G2_C362::Method3.8161<System.Object>()"
		ldstr "class G3_C1329`1<class BaseClass1> on type class G3_C1329`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1329`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1329`1<class BaseClass1>::Method2<object>()
		calli default string(class G3_C1329`1<class BaseClass1>)
		ldstr "G2_C362::Method2.8159<System.Object>()"
		ldstr "class G3_C1329`1<class BaseClass1> on type class G3_C1329`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1329`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1329`1<class BaseClass1>::Method1()
		calli default string(class G3_C1329`1<class BaseClass1>)
		ldstr "G2_C362::Method1.8158()"
		ldstr "class G3_C1329`1<class BaseClass1> on type class G3_C1329`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1329`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1329`1<class BaseClass1>::Method0()
		calli default string(class G3_C1329`1<class BaseClass1>)
		ldstr "G2_C362::Method0.8157()"
		ldstr "class G3_C1329`1<class BaseClass1> on type class G3_C1329`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1329`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1329`1<class BaseClass1>::ClassMethod1331<object>()
		calli default string(class G3_C1329`1<class BaseClass1>)
		ldstr "G2_C362::ClassMethod1331.MI.8167<System.Object>()"
		ldstr "class G3_C1329`1<class BaseClass1> on type class G3_C1329`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1329`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1329`1<class BaseClass1>::ClassMethod1330<object>()
		calli default string(class G3_C1329`1<class BaseClass1>)
		ldstr "G2_C362::ClassMethod1330.MI.8166<System.Object>()"
		ldstr "class G3_C1329`1<class BaseClass1> on type class G3_C1329`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1329`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1329`1<class BaseClass1>::ClassMethod1329()
		calli default string(class G3_C1329`1<class BaseClass1>)
		ldstr "G2_C362::ClassMethod1329.MI.8165()"
		ldstr "class G3_C1329`1<class BaseClass1> on type class G3_C1329`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1329`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1329`1<class BaseClass1>::ClassMethod1328()
		calli default string(class G3_C1329`1<class BaseClass1>)
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G3_C1329`1<class BaseClass1> on type class G3_C1329`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G2_C362`2<class BaseClass0,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C7`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass1,class BaseClass0>::ClassMethod1331<object>()
		calli default string(class G2_C362`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C362::ClassMethod1331.MI.8167<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass0> on type class G2_C362`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C7`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass1,class BaseClass0>::ClassMethod1330<object>()
		calli default string(class G2_C362`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C362::ClassMethod1330.MI.8166<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass0> on type class G2_C362`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C7`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass1,class BaseClass0>::ClassMethod1329()
		calli default string(class G2_C362`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C362::ClassMethod1329.MI.8165()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass0> on type class G2_C362`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C7`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass1,class BaseClass0>::ClassMethod1328()
		calli default string(class G2_C362`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass0> on type class G2_C362`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C7`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G2_C362`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C7::Method7.4815<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass0> on type class G2_C362`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C7`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass1,class BaseClass0>::Method6<object>()
		calli default string(class G2_C362`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C7::Method6.4813<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass0> on type class G2_C362`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C7`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass1,class BaseClass0>::Method5()
		calli default string(class G2_C362`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C7::Method5.4812()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass0> on type class G2_C362`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C7`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass1,class BaseClass0>::Method4()
		calli default string(class G2_C362`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C7::Method4.4811()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass0> on type class G2_C362`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method4()
		calli default string(class G2_C362`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C7::Method4.4811()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C362`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method5()
		calli default string(class G2_C362`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C7::Method5.4812()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C362`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method6<object>()
		calli default string(class G2_C362`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C7::Method6.MI.4814<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C362`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C362`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C362::Method7.8162<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C362`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G2_C362`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C7::Method4.4811()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C362`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G2_C362`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C7::Method5.4812()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C362`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G2_C362`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C7::Method6.MI.4814<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C362`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C362`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C362::Method7.8162<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C362`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C362`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C362`2<class BaseClass0,class BaseClass0>::ClassMethod2066<object>()
		calli default string(class G2_C362`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C362::ClassMethod2066.8164<System.Object>()"
		ldstr "class G2_C362`2<class BaseClass0,class BaseClass0> on type class G2_C362`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C362`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C362`2<class BaseClass0,class BaseClass0>::ClassMethod2065<object>()
		calli default string(class G2_C362`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C362::ClassMethod2065.8163<System.Object>()"
		ldstr "class G2_C362`2<class BaseClass0,class BaseClass0> on type class G2_C362`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C362`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C362`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G2_C362`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C362::Method7.8162<System.Object>()"
		ldstr "class G2_C362`2<class BaseClass0,class BaseClass0> on type class G2_C362`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C362`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C362`2<class BaseClass0,class BaseClass0>::Method3<object>()
		calli default string(class G2_C362`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C362::Method3.8161<System.Object>()"
		ldstr "class G2_C362`2<class BaseClass0,class BaseClass0> on type class G2_C362`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C362`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C362`2<class BaseClass0,class BaseClass0>::Method2<object>()
		calli default string(class G2_C362`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C362::Method2.8159<System.Object>()"
		ldstr "class G2_C362`2<class BaseClass0,class BaseClass0> on type class G2_C362`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C362`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C362`2<class BaseClass0,class BaseClass0>::Method1()
		calli default string(class G2_C362`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C362::Method1.8158()"
		ldstr "class G2_C362`2<class BaseClass0,class BaseClass0> on type class G2_C362`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C362`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C362`2<class BaseClass0,class BaseClass0>::Method0()
		calli default string(class G2_C362`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C362::Method0.8157()"
		ldstr "class G2_C362`2<class BaseClass0,class BaseClass0> on type class G2_C362`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C362`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C362`2<class BaseClass0,class BaseClass0>::ClassMethod1331<object>()
		calli default string(class G2_C362`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C362::ClassMethod1331.MI.8167<System.Object>()"
		ldstr "class G2_C362`2<class BaseClass0,class BaseClass0> on type class G2_C362`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C362`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C362`2<class BaseClass0,class BaseClass0>::ClassMethod1330<object>()
		calli default string(class G2_C362`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C362::ClassMethod1330.MI.8166<System.Object>()"
		ldstr "class G2_C362`2<class BaseClass0,class BaseClass0> on type class G2_C362`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C362`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C362`2<class BaseClass0,class BaseClass0>::ClassMethod1329()
		calli default string(class G2_C362`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C362::ClassMethod1329.MI.8165()"
		ldstr "class G2_C362`2<class BaseClass0,class BaseClass0> on type class G2_C362`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C362`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C362`2<class BaseClass0,class BaseClass0>::ClassMethod1328()
		calli default string(class G2_C362`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G2_C362`2<class BaseClass0,class BaseClass0> on type class G2_C362`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C362`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C362`2<class BaseClass0,class BaseClass0>::Method6<object>()
		calli default string(class G2_C362`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C7::Method6.4813<System.Object>()"
		ldstr "class G2_C362`2<class BaseClass0,class BaseClass0> on type class G2_C362`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C362`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C362`2<class BaseClass0,class BaseClass0>::Method5()
		calli default string(class G2_C362`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C7::Method5.4812()"
		ldstr "class G2_C362`2<class BaseClass0,class BaseClass0> on type class G2_C362`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C362`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C362`2<class BaseClass0,class BaseClass0>::Method4()
		calli default string(class G2_C362`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C7::Method4.4811()"
		ldstr "class G2_C362`2<class BaseClass0,class BaseClass0> on type class G2_C362`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method0()
		calli default string(class G2_C362`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C362::Method0.8157()"
		ldstr "IBase0 on type class G2_C362`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method1()
		calli default string(class G2_C362`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C362::Method1.8158()"
		ldstr "IBase0 on type class G2_C362`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method2<object>()
		calli default string(class G2_C362`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C362::Method2.MI.8160<System.Object>()"
		ldstr "IBase0 on type class G2_C362`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method3<object>()
		calli default string(class G2_C362`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C362::Method3.8161<System.Object>()"
		ldstr "IBase0 on type class G2_C362`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G2_C362`2<class BaseClass0,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass1,class BaseClass1>::ClassMethod1331<object>()
		calli default string(class G2_C362`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C362::ClassMethod1331.MI.8167<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G2_C362`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass1,class BaseClass1>::ClassMethod1330<object>()
		calli default string(class G2_C362`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C362::ClassMethod1330.MI.8166<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G2_C362`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass1,class BaseClass1>::ClassMethod1329()
		calli default string(class G2_C362`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C362::ClassMethod1329.MI.8165()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G2_C362`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass1,class BaseClass1>::ClassMethod1328()
		calli default string(class G2_C362`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G2_C362`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C362`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C7::Method7.4815<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G2_C362`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass1,class BaseClass1>::Method6<object>()
		calli default string(class G2_C362`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C7::Method6.4813<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G2_C362`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass1,class BaseClass1>::Method5()
		calli default string(class G2_C362`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C7::Method5.4812()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G2_C362`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass1,class BaseClass1>::Method4()
		calli default string(class G2_C362`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C7::Method4.4811()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G2_C362`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method4()
		calli default string(class G2_C362`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C7::Method4.4811()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C362`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method5()
		calli default string(class G2_C362`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C7::Method5.4812()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C362`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method6<object>()
		calli default string(class G2_C362`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C7::Method6.MI.4814<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C362`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C362`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C362::Method7.8162<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C362`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G2_C362`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C7::Method4.4811()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C362`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G2_C362`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C7::Method5.4812()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C362`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G2_C362`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C7::Method6.MI.4814<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C362`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C362`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C362::Method7.8162<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C362`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C362`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C362`2<class BaseClass0,class BaseClass1>::ClassMethod2066<object>()
		calli default string(class G2_C362`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C362::ClassMethod2066.8164<System.Object>()"
		ldstr "class G2_C362`2<class BaseClass0,class BaseClass1> on type class G2_C362`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C362`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C362`2<class BaseClass0,class BaseClass1>::ClassMethod2065<object>()
		calli default string(class G2_C362`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C362::ClassMethod2065.8163<System.Object>()"
		ldstr "class G2_C362`2<class BaseClass0,class BaseClass1> on type class G2_C362`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C362`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C362`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C362`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C362::Method7.8162<System.Object>()"
		ldstr "class G2_C362`2<class BaseClass0,class BaseClass1> on type class G2_C362`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C362`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C362`2<class BaseClass0,class BaseClass1>::Method3<object>()
		calli default string(class G2_C362`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C362::Method3.8161<System.Object>()"
		ldstr "class G2_C362`2<class BaseClass0,class BaseClass1> on type class G2_C362`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C362`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C362`2<class BaseClass0,class BaseClass1>::Method2<object>()
		calli default string(class G2_C362`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C362::Method2.8159<System.Object>()"
		ldstr "class G2_C362`2<class BaseClass0,class BaseClass1> on type class G2_C362`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C362`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C362`2<class BaseClass0,class BaseClass1>::Method1()
		calli default string(class G2_C362`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C362::Method1.8158()"
		ldstr "class G2_C362`2<class BaseClass0,class BaseClass1> on type class G2_C362`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C362`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C362`2<class BaseClass0,class BaseClass1>::Method0()
		calli default string(class G2_C362`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C362::Method0.8157()"
		ldstr "class G2_C362`2<class BaseClass0,class BaseClass1> on type class G2_C362`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C362`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C362`2<class BaseClass0,class BaseClass1>::ClassMethod1331<object>()
		calli default string(class G2_C362`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C362::ClassMethod1331.MI.8167<System.Object>()"
		ldstr "class G2_C362`2<class BaseClass0,class BaseClass1> on type class G2_C362`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C362`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C362`2<class BaseClass0,class BaseClass1>::ClassMethod1330<object>()
		calli default string(class G2_C362`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C362::ClassMethod1330.MI.8166<System.Object>()"
		ldstr "class G2_C362`2<class BaseClass0,class BaseClass1> on type class G2_C362`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C362`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C362`2<class BaseClass0,class BaseClass1>::ClassMethod1329()
		calli default string(class G2_C362`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C362::ClassMethod1329.MI.8165()"
		ldstr "class G2_C362`2<class BaseClass0,class BaseClass1> on type class G2_C362`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C362`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C362`2<class BaseClass0,class BaseClass1>::ClassMethod1328()
		calli default string(class G2_C362`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G2_C362`2<class BaseClass0,class BaseClass1> on type class G2_C362`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C362`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C362`2<class BaseClass0,class BaseClass1>::Method6<object>()
		calli default string(class G2_C362`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C7::Method6.4813<System.Object>()"
		ldstr "class G2_C362`2<class BaseClass0,class BaseClass1> on type class G2_C362`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C362`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C362`2<class BaseClass0,class BaseClass1>::Method5()
		calli default string(class G2_C362`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C7::Method5.4812()"
		ldstr "class G2_C362`2<class BaseClass0,class BaseClass1> on type class G2_C362`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C362`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C362`2<class BaseClass0,class BaseClass1>::Method4()
		calli default string(class G2_C362`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C7::Method4.4811()"
		ldstr "class G2_C362`2<class BaseClass0,class BaseClass1> on type class G2_C362`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method0()
		calli default string(class G2_C362`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C362::Method0.8157()"
		ldstr "IBase0 on type class G2_C362`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method1()
		calli default string(class G2_C362`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C362::Method1.8158()"
		ldstr "IBase0 on type class G2_C362`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method2<object>()
		calli default string(class G2_C362`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C362::Method2.MI.8160<System.Object>()"
		ldstr "IBase0 on type class G2_C362`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method3<object>()
		calli default string(class G2_C362`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C362::Method3.8161<System.Object>()"
		ldstr "IBase0 on type class G2_C362`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G2_C362`2<class BaseClass1,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C7`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass1,class BaseClass0>::ClassMethod1331<object>()
		calli default string(class G2_C362`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C362::ClassMethod1331.MI.8167<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass0> on type class G2_C362`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C7`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass1,class BaseClass0>::ClassMethod1330<object>()
		calli default string(class G2_C362`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C362::ClassMethod1330.MI.8166<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass0> on type class G2_C362`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C7`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass1,class BaseClass0>::ClassMethod1329()
		calli default string(class G2_C362`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C362::ClassMethod1329.MI.8165()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass0> on type class G2_C362`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C7`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass1,class BaseClass0>::ClassMethod1328()
		calli default string(class G2_C362`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass0> on type class G2_C362`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C7`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G2_C362`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C7::Method7.4815<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass0> on type class G2_C362`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C7`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass1,class BaseClass0>::Method6<object>()
		calli default string(class G2_C362`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C7::Method6.4813<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass0> on type class G2_C362`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C7`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass1,class BaseClass0>::Method5()
		calli default string(class G2_C362`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C7::Method5.4812()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass0> on type class G2_C362`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C7`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass1,class BaseClass0>::Method4()
		calli default string(class G2_C362`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C7::Method4.4811()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass0> on type class G2_C362`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method4()
		calli default string(class G2_C362`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C7::Method4.4811()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C362`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method5()
		calli default string(class G2_C362`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C7::Method5.4812()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C362`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method6<object>()
		calli default string(class G2_C362`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C7::Method6.MI.4814<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C362`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C362`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C362::Method7.8162<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C362`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G2_C362`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C7::Method4.4811()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C362`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G2_C362`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C7::Method5.4812()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C362`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G2_C362`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C7::Method6.MI.4814<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C362`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C362`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C362::Method7.8162<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C362`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C362`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C362`2<class BaseClass1,class BaseClass0>::ClassMethod2066<object>()
		calli default string(class G2_C362`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C362::ClassMethod2066.8164<System.Object>()"
		ldstr "class G2_C362`2<class BaseClass1,class BaseClass0> on type class G2_C362`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C362`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C362`2<class BaseClass1,class BaseClass0>::ClassMethod2065<object>()
		calli default string(class G2_C362`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C362::ClassMethod2065.8163<System.Object>()"
		ldstr "class G2_C362`2<class BaseClass1,class BaseClass0> on type class G2_C362`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C362`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C362`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G2_C362`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C362::Method7.8162<System.Object>()"
		ldstr "class G2_C362`2<class BaseClass1,class BaseClass0> on type class G2_C362`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C362`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C362`2<class BaseClass1,class BaseClass0>::Method3<object>()
		calli default string(class G2_C362`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C362::Method3.8161<System.Object>()"
		ldstr "class G2_C362`2<class BaseClass1,class BaseClass0> on type class G2_C362`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C362`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C362`2<class BaseClass1,class BaseClass0>::Method2<object>()
		calli default string(class G2_C362`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C362::Method2.8159<System.Object>()"
		ldstr "class G2_C362`2<class BaseClass1,class BaseClass0> on type class G2_C362`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C362`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C362`2<class BaseClass1,class BaseClass0>::Method1()
		calli default string(class G2_C362`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C362::Method1.8158()"
		ldstr "class G2_C362`2<class BaseClass1,class BaseClass0> on type class G2_C362`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C362`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C362`2<class BaseClass1,class BaseClass0>::Method0()
		calli default string(class G2_C362`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C362::Method0.8157()"
		ldstr "class G2_C362`2<class BaseClass1,class BaseClass0> on type class G2_C362`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C362`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C362`2<class BaseClass1,class BaseClass0>::ClassMethod1331<object>()
		calli default string(class G2_C362`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C362::ClassMethod1331.MI.8167<System.Object>()"
		ldstr "class G2_C362`2<class BaseClass1,class BaseClass0> on type class G2_C362`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C362`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C362`2<class BaseClass1,class BaseClass0>::ClassMethod1330<object>()
		calli default string(class G2_C362`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C362::ClassMethod1330.MI.8166<System.Object>()"
		ldstr "class G2_C362`2<class BaseClass1,class BaseClass0> on type class G2_C362`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C362`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C362`2<class BaseClass1,class BaseClass0>::ClassMethod1329()
		calli default string(class G2_C362`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C362::ClassMethod1329.MI.8165()"
		ldstr "class G2_C362`2<class BaseClass1,class BaseClass0> on type class G2_C362`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C362`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C362`2<class BaseClass1,class BaseClass0>::ClassMethod1328()
		calli default string(class G2_C362`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G2_C362`2<class BaseClass1,class BaseClass0> on type class G2_C362`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C362`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C362`2<class BaseClass1,class BaseClass0>::Method6<object>()
		calli default string(class G2_C362`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C7::Method6.4813<System.Object>()"
		ldstr "class G2_C362`2<class BaseClass1,class BaseClass0> on type class G2_C362`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C362`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C362`2<class BaseClass1,class BaseClass0>::Method5()
		calli default string(class G2_C362`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C7::Method5.4812()"
		ldstr "class G2_C362`2<class BaseClass1,class BaseClass0> on type class G2_C362`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C362`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C362`2<class BaseClass1,class BaseClass0>::Method4()
		calli default string(class G2_C362`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C7::Method4.4811()"
		ldstr "class G2_C362`2<class BaseClass1,class BaseClass0> on type class G2_C362`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method0()
		calli default string(class G2_C362`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C362::Method0.8157()"
		ldstr "IBase0 on type class G2_C362`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method1()
		calli default string(class G2_C362`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C362::Method1.8158()"
		ldstr "IBase0 on type class G2_C362`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method2<object>()
		calli default string(class G2_C362`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C362::Method2.MI.8160<System.Object>()"
		ldstr "IBase0 on type class G2_C362`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method3<object>()
		calli default string(class G2_C362`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C362::Method3.8161<System.Object>()"
		ldstr "IBase0 on type class G2_C362`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G2_C362`2<class BaseClass1,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass1,class BaseClass1>::ClassMethod1331<object>()
		calli default string(class G2_C362`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C362::ClassMethod1331.MI.8167<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G2_C362`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass1,class BaseClass1>::ClassMethod1330<object>()
		calli default string(class G2_C362`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C362::ClassMethod1330.MI.8166<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G2_C362`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass1,class BaseClass1>::ClassMethod1329()
		calli default string(class G2_C362`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C362::ClassMethod1329.MI.8165()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G2_C362`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass1,class BaseClass1>::ClassMethod1328()
		calli default string(class G2_C362`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G2_C362`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C362`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C7::Method7.4815<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G2_C362`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass1,class BaseClass1>::Method6<object>()
		calli default string(class G2_C362`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C7::Method6.4813<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G2_C362`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass1,class BaseClass1>::Method5()
		calli default string(class G2_C362`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C7::Method5.4812()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G2_C362`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass1,class BaseClass1>::Method4()
		calli default string(class G2_C362`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C7::Method4.4811()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G2_C362`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method4()
		calli default string(class G2_C362`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C7::Method4.4811()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C362`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method5()
		calli default string(class G2_C362`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C7::Method5.4812()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C362`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method6<object>()
		calli default string(class G2_C362`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C7::Method6.MI.4814<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C362`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C362`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C362::Method7.8162<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C362`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G2_C362`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C7::Method4.4811()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C362`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G2_C362`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C7::Method5.4812()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C362`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G2_C362`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C7::Method6.MI.4814<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C362`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C362`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C362::Method7.8162<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C362`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C362`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C362`2<class BaseClass1,class BaseClass1>::ClassMethod2066<object>()
		calli default string(class G2_C362`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C362::ClassMethod2066.8164<System.Object>()"
		ldstr "class G2_C362`2<class BaseClass1,class BaseClass1> on type class G2_C362`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C362`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C362`2<class BaseClass1,class BaseClass1>::ClassMethod2065<object>()
		calli default string(class G2_C362`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C362::ClassMethod2065.8163<System.Object>()"
		ldstr "class G2_C362`2<class BaseClass1,class BaseClass1> on type class G2_C362`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C362`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C362`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C362`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C362::Method7.8162<System.Object>()"
		ldstr "class G2_C362`2<class BaseClass1,class BaseClass1> on type class G2_C362`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C362`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C362`2<class BaseClass1,class BaseClass1>::Method3<object>()
		calli default string(class G2_C362`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C362::Method3.8161<System.Object>()"
		ldstr "class G2_C362`2<class BaseClass1,class BaseClass1> on type class G2_C362`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C362`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C362`2<class BaseClass1,class BaseClass1>::Method2<object>()
		calli default string(class G2_C362`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C362::Method2.8159<System.Object>()"
		ldstr "class G2_C362`2<class BaseClass1,class BaseClass1> on type class G2_C362`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C362`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C362`2<class BaseClass1,class BaseClass1>::Method1()
		calli default string(class G2_C362`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C362::Method1.8158()"
		ldstr "class G2_C362`2<class BaseClass1,class BaseClass1> on type class G2_C362`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C362`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C362`2<class BaseClass1,class BaseClass1>::Method0()
		calli default string(class G2_C362`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C362::Method0.8157()"
		ldstr "class G2_C362`2<class BaseClass1,class BaseClass1> on type class G2_C362`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C362`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C362`2<class BaseClass1,class BaseClass1>::ClassMethod1331<object>()
		calli default string(class G2_C362`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C362::ClassMethod1331.MI.8167<System.Object>()"
		ldstr "class G2_C362`2<class BaseClass1,class BaseClass1> on type class G2_C362`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C362`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C362`2<class BaseClass1,class BaseClass1>::ClassMethod1330<object>()
		calli default string(class G2_C362`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C362::ClassMethod1330.MI.8166<System.Object>()"
		ldstr "class G2_C362`2<class BaseClass1,class BaseClass1> on type class G2_C362`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C362`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C362`2<class BaseClass1,class BaseClass1>::ClassMethod1329()
		calli default string(class G2_C362`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C362::ClassMethod1329.MI.8165()"
		ldstr "class G2_C362`2<class BaseClass1,class BaseClass1> on type class G2_C362`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C362`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C362`2<class BaseClass1,class BaseClass1>::ClassMethod1328()
		calli default string(class G2_C362`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G2_C362`2<class BaseClass1,class BaseClass1> on type class G2_C362`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C362`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C362`2<class BaseClass1,class BaseClass1>::Method6<object>()
		calli default string(class G2_C362`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C7::Method6.4813<System.Object>()"
		ldstr "class G2_C362`2<class BaseClass1,class BaseClass1> on type class G2_C362`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C362`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C362`2<class BaseClass1,class BaseClass1>::Method5()
		calli default string(class G2_C362`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C7::Method5.4812()"
		ldstr "class G2_C362`2<class BaseClass1,class BaseClass1> on type class G2_C362`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C362`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C362`2<class BaseClass1,class BaseClass1>::Method4()
		calli default string(class G2_C362`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C7::Method4.4811()"
		ldstr "class G2_C362`2<class BaseClass1,class BaseClass1> on type class G2_C362`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method0()
		calli default string(class G2_C362`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C362::Method0.8157()"
		ldstr "IBase0 on type class G2_C362`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method1()
		calli default string(class G2_C362`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C362::Method1.8158()"
		ldstr "IBase0 on type class G2_C362`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method2<object>()
		calli default string(class G2_C362`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C362::Method2.MI.8160<System.Object>()"
		ldstr "IBase0 on type class G2_C362`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method3<object>()
		calli default string(class G2_C362`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C362::Method3.8161<System.Object>()"
		ldstr "IBase0 on type class G2_C362`2<class BaseClass1,class BaseClass1>"
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
		call void Generated857::MethodCallingTest()
		call void Generated857::ConstrainedCallsTest()
		call void Generated857::StructConstrainedInterfaceCallsTest()
		call void Generated857::CalliTest()
		ldc.i4 100
		ret
	}
}
