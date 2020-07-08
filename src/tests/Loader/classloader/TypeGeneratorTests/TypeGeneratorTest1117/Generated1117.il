// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) .ver 4:0:0:0 }
.assembly extern TestFramework { .publickeytoken = ( B0 3F 5F 7F 11 D5 0A 3A )  }

//TYPES IN FORWARDER ASSEMBLIES:

//TEST ASSEMBLY:
.assembly Generated1117 { .hash algorithm 0x00008004 }
.module Generated1117.exe

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
.class public G3_C1589`1<T0> 
		extends class G2_C598`2<!T0,class BaseClass0>
		implements class IBase2`2<!T0,class BaseClass0> 
{
	.method public hidebysig newslot virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G3_C1589::Method7.16950<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod4566() cil managed noinlining { 
		ldstr "G3_C1589::ClassMethod4566.16951()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod4567<M0>() cil managed noinlining { 
		ldstr "G3_C1589::ClassMethod4567.16952<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class G2_C598`2<!T0,class BaseClass0>::.ctor()
		ret
	}
}
.class public G2_C598`2<T0, T1> 
		extends class G1_C11`2<!T0,class BaseClass1>
		implements class IBase1`1<class BaseClass1>, class IBase2`2<!T0,class BaseClass0> 
{
	.method public hidebysig newslot virtual instance string Method4() cil managed noinlining { 
		ldstr "G2_C598::Method4.10508()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<class BaseClass1>.Method4'() cil managed noinlining { 
		.override method instance string class IBase1`1<class BaseClass1>::Method4()
		ldstr "G2_C598::Method4.MI.10509()"
		ret
	}
	.method public hidebysig virtual instance string Method5() cil managed noinlining { 
		ldstr "G2_C598::Method5.10510()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<class BaseClass1>.Method5'() cil managed noinlining { 
		.override method instance string class IBase1`1<class BaseClass1>::Method5()
		ldstr "G2_C598::Method5.MI.10511()"
		ret
	}
	.method public hidebysig newslot virtual instance string Method6<M0>() cil managed noinlining { 
		ldstr "G2_C598::Method6.10512<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G2_C598::Method7.10513<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase2<T0,class BaseClass0>.Method7'<M0>() cil managed noinlining { 
		.override method instance string class IBase2`2<!T0,class BaseClass0>::Method7<[1]>()
		ldstr "G2_C598::Method7.MI.10514<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod2566() cil managed noinlining { 
		ldstr "G2_C598::ClassMethod2566.10515()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'G1_C11<T0,class BaseClass1>.ClassMethod1344'<M0>() cil managed noinlining { 
		.override method instance string class G1_C11`2<!T0,class BaseClass1>::ClassMethod1344<[1]>()
		ldstr "G2_C598::ClassMethod1344.MI.10516<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class G1_C11`2<!T0,class BaseClass1>::.ctor()
		ret
	}
}
.class interface public abstract IBase2`2<+T0, -T1> 
{
	.method public hidebysig newslot abstract virtual instance string Method7<M0>() cil managed { }
}
.class public G1_C11`2<T0, T1> 
		implements class IBase2`2<class BaseClass1,!T1>, class IBase1`1<class BaseClass0> 
{
	.method public hidebysig newslot virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G1_C11::Method7.4854<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase2<class BaseClass1,T1>.Method7'<M0>() cil managed noinlining { 
		.override method instance string class IBase2`2<class BaseClass1,!T1>::Method7<[1]>()
		ldstr "G1_C11::Method7.MI.4855<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig virtual instance string Method4() cil managed noinlining { 
		ldstr "G1_C11::Method4.4856()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<class BaseClass0>.Method4'() cil managed noinlining { 
		.override method instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G1_C11::Method4.MI.4857()"
		ret
	}
	.method public hidebysig virtual instance string Method5() cil managed noinlining { 
		ldstr "G1_C11::Method5.4858()"
		ret
	}
	.method public hidebysig virtual instance string Method6<M0>() cil managed noinlining { 
		ldstr "G1_C11::Method6.4859<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<class BaseClass0>.Method6'<M0>() cil managed noinlining { 
		.override method instance string class IBase1`1<class BaseClass0>::Method6<[1]>()
		ldstr "G1_C11::Method6.MI.4860<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1344<M0>() cil managed noinlining { 
		ldstr "G1_C11::ClassMethod1344.4861<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1345<M0>() cil managed noinlining { 
		ldstr "G1_C11::ClassMethod1345.4862<"
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
.class interface public abstract IBase1`1<+T0> 
{
	.method public hidebysig newslot abstract virtual instance string Method4() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method5() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method6<M0>() cil managed { }
}
.class private auto ansi beforefieldinit Framework {
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
	.method static void M.G3_C1589.T<T0,(class G3_C1589`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 14
		.locals init (string[] actualResults)
		ldc.i4.s    9
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1589.T<T0,(class G3_C1589`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 9
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1589`1<!!T0>::ClassMethod1344<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1589`1<!!T0>::ClassMethod1345<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1589`1<!!T0>::ClassMethod2566()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1589`1<!!T0>::ClassMethod4566()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1589`1<!!T0>::ClassMethod4567<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1589`1<!!T0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1589`1<!!T0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1589`1<!!T0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1589`1<!!T0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1589.A<(class G3_C1589`1<class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 14
		.locals init (string[] actualResults)
		ldc.i4.s    9
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1589.A<(class G3_C1589`1<class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 9
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1589`1<class BaseClass0>::ClassMethod1344<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1589`1<class BaseClass0>::ClassMethod1345<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1589`1<class BaseClass0>::ClassMethod2566()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1589`1<class BaseClass0>::ClassMethod4566()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1589`1<class BaseClass0>::ClassMethod4567<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1589`1<class BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1589`1<class BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1589`1<class BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1589`1<class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1589.B<(class G3_C1589`1<class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 14
		.locals init (string[] actualResults)
		ldc.i4.s    9
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1589.B<(class G3_C1589`1<class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 9
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1589`1<class BaseClass1>::ClassMethod1344<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1589`1<class BaseClass1>::ClassMethod1345<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1589`1<class BaseClass1>::ClassMethod2566()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1589`1<class BaseClass1>::ClassMethod4566()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1589`1<class BaseClass1>::ClassMethod4567<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1589`1<class BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1589`1<class BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1589`1<class BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1589`1<class BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C598.T.T<T0,T1,(class G2_C598`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 12
		.locals init (string[] actualResults)
		ldc.i4.s    7
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C598.T.T<T0,T1,(class G2_C598`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 7
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C598`2<!!T0,!!T1>::ClassMethod1344<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C598`2<!!T0,!!T1>::ClassMethod1345<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C598`2<!!T0,!!T1>::ClassMethod2566()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C598`2<!!T0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C598`2<!!T0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C598`2<!!T0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C598`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C598.A.T<T1,(class G2_C598`2<class BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 12
		.locals init (string[] actualResults)
		ldc.i4.s    7
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C598.A.T<T1,(class G2_C598`2<class BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 7
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C598`2<class BaseClass0,!!T1>::ClassMethod1344<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C598`2<class BaseClass0,!!T1>::ClassMethod1345<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C598`2<class BaseClass0,!!T1>::ClassMethod2566()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C598`2<class BaseClass0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C598`2<class BaseClass0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C598`2<class BaseClass0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C598`2<class BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C598.A.A<(class G2_C598`2<class BaseClass0,class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 12
		.locals init (string[] actualResults)
		ldc.i4.s    7
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C598.A.A<(class G2_C598`2<class BaseClass0,class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 7
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C598`2<class BaseClass0,class BaseClass0>::ClassMethod1344<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C598`2<class BaseClass0,class BaseClass0>::ClassMethod1345<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C598`2<class BaseClass0,class BaseClass0>::ClassMethod2566()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C598`2<class BaseClass0,class BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C598`2<class BaseClass0,class BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C598`2<class BaseClass0,class BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C598`2<class BaseClass0,class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C598.A.B<(class G2_C598`2<class BaseClass0,class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 12
		.locals init (string[] actualResults)
		ldc.i4.s    7
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C598.A.B<(class G2_C598`2<class BaseClass0,class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 7
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C598`2<class BaseClass0,class BaseClass1>::ClassMethod1344<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C598`2<class BaseClass0,class BaseClass1>::ClassMethod1345<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C598`2<class BaseClass0,class BaseClass1>::ClassMethod2566()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C598`2<class BaseClass0,class BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C598`2<class BaseClass0,class BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C598`2<class BaseClass0,class BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C598`2<class BaseClass0,class BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C598.B.T<T1,(class G2_C598`2<class BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 12
		.locals init (string[] actualResults)
		ldc.i4.s    7
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C598.B.T<T1,(class G2_C598`2<class BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 7
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C598`2<class BaseClass1,!!T1>::ClassMethod1344<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C598`2<class BaseClass1,!!T1>::ClassMethod1345<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C598`2<class BaseClass1,!!T1>::ClassMethod2566()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C598`2<class BaseClass1,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C598`2<class BaseClass1,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C598`2<class BaseClass1,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C598`2<class BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C598.B.A<(class G2_C598`2<class BaseClass1,class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 12
		.locals init (string[] actualResults)
		ldc.i4.s    7
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C598.B.A<(class G2_C598`2<class BaseClass1,class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 7
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C598`2<class BaseClass1,class BaseClass0>::ClassMethod1344<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C598`2<class BaseClass1,class BaseClass0>::ClassMethod1345<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C598`2<class BaseClass1,class BaseClass0>::ClassMethod2566()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C598`2<class BaseClass1,class BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C598`2<class BaseClass1,class BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C598`2<class BaseClass1,class BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C598`2<class BaseClass1,class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C598.B.B<(class G2_C598`2<class BaseClass1,class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 12
		.locals init (string[] actualResults)
		ldc.i4.s    7
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C598.B.B<(class G2_C598`2<class BaseClass1,class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 7
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C598`2<class BaseClass1,class BaseClass1>::ClassMethod1344<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C598`2<class BaseClass1,class BaseClass1>::ClassMethod1345<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C598`2<class BaseClass1,class BaseClass1>::ClassMethod2566()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C598`2<class BaseClass1,class BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C598`2<class BaseClass1,class BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C598`2<class BaseClass1,class BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C598`2<class BaseClass1,class BaseClass1>::Method7<object>()
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
	.method static void M.G1_C11.T.T<T0,T1,(class G1_C11`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C11.T.T<T0,T1,(class G1_C11`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C11`2<!!T0,!!T1>::ClassMethod1344<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C11`2<!!T0,!!T1>::ClassMethod1345<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C11`2<!!T0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C11`2<!!T0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C11`2<!!T0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C11`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C11.A.T<T1,(class G1_C11`2<class BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C11.A.T<T1,(class G1_C11`2<class BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C11`2<class BaseClass0,!!T1>::ClassMethod1344<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C11`2<class BaseClass0,!!T1>::ClassMethod1345<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C11`2<class BaseClass0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C11`2<class BaseClass0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C11`2<class BaseClass0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C11`2<class BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C11.A.A<(class G1_C11`2<class BaseClass0,class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C11.A.A<(class G1_C11`2<class BaseClass0,class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C11`2<class BaseClass0,class BaseClass0>::ClassMethod1344<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C11`2<class BaseClass0,class BaseClass0>::ClassMethod1345<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C11`2<class BaseClass0,class BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C11`2<class BaseClass0,class BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C11`2<class BaseClass0,class BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C11`2<class BaseClass0,class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C11.A.B<(class G1_C11`2<class BaseClass0,class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C11.A.B<(class G1_C11`2<class BaseClass0,class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C11`2<class BaseClass0,class BaseClass1>::ClassMethod1344<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C11`2<class BaseClass0,class BaseClass1>::ClassMethod1345<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C11`2<class BaseClass0,class BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C11`2<class BaseClass0,class BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C11`2<class BaseClass0,class BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C11`2<class BaseClass0,class BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C11.B.T<T1,(class G1_C11`2<class BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C11.B.T<T1,(class G1_C11`2<class BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C11`2<class BaseClass1,!!T1>::ClassMethod1344<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C11`2<class BaseClass1,!!T1>::ClassMethod1345<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C11`2<class BaseClass1,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C11`2<class BaseClass1,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C11`2<class BaseClass1,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C11`2<class BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C11.B.A<(class G1_C11`2<class BaseClass1,class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C11.B.A<(class G1_C11`2<class BaseClass1,class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C11`2<class BaseClass1,class BaseClass0>::ClassMethod1344<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C11`2<class BaseClass1,class BaseClass0>::ClassMethod1345<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C11`2<class BaseClass1,class BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C11`2<class BaseClass1,class BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C11`2<class BaseClass1,class BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C11`2<class BaseClass1,class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C11.B.B<(class G1_C11`2<class BaseClass1,class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C11.B.B<(class G1_C11`2<class BaseClass1,class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C11`2<class BaseClass1,class BaseClass1>::ClassMethod1344<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C11`2<class BaseClass1,class BaseClass1>::ClassMethod1345<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C11`2<class BaseClass1,class BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C11`2<class BaseClass1,class BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C11`2<class BaseClass1,class BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C11`2<class BaseClass1,class BaseClass1>::Method7<object>()
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
	.method public hidebysig static void MethodCallingTest() cil managed
	{
		.maxstack  10
		.locals init (object V_0)
		ldstr "========================== Method Calling Test =========================="
		call void [mscorlib]System.Console::WriteLine(string)
		newobj instance void class G3_C1589`1<class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C11`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C11`2<class BaseClass0,class BaseClass1>::ClassMethod1345<object>()
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass1> on type class G3_C1589`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C11`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C11`2<class BaseClass0,class BaseClass1>::ClassMethod1344<object>()
		ldstr "G2_C598::ClassMethod1344.MI.10516<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass1> on type class G3_C1589`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C11`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C11`2<class BaseClass0,class BaseClass1>::Method6<object>()
		ldstr "G1_C11::Method6.4859<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass1> on type class G3_C1589`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C11`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C11`2<class BaseClass0,class BaseClass1>::Method5()
		ldstr "G2_C598::Method5.10510()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass1> on type class G3_C1589`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C11`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C11`2<class BaseClass0,class BaseClass1>::Method4()
		ldstr "G1_C11::Method4.4856()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass1> on type class G3_C1589`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C11`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C11`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C598::Method7.10513<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass1> on type class G3_C1589`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G3_C1589`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G2_C598::Method4.MI.10509()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1589`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G2_C598::Method5.MI.10511()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1589`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G2_C598::Method6.10512<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1589`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G3_C1589::Method7.16950<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1589`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C598`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C598`2<class BaseClass0,class BaseClass0>::ClassMethod2566()
		ldstr "G2_C598::ClassMethod2566.10515()"
		ldstr "class G2_C598`2<class BaseClass0,class BaseClass0> on type class G3_C1589`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C598`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C598`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G2_C598::Method7.10513<System.Object>()"
		ldstr "class G2_C598`2<class BaseClass0,class BaseClass0> on type class G3_C1589`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C598`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C598`2<class BaseClass0,class BaseClass0>::Method6<object>()
		ldstr "G2_C598::Method6.10512<System.Object>()"
		ldstr "class G2_C598`2<class BaseClass0,class BaseClass0> on type class G3_C1589`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C598`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C598`2<class BaseClass0,class BaseClass0>::Method5()
		ldstr "G2_C598::Method5.10510()"
		ldstr "class G2_C598`2<class BaseClass0,class BaseClass0> on type class G3_C1589`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C598`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C598`2<class BaseClass0,class BaseClass0>::Method4()
		ldstr "G2_C598::Method4.10508()"
		ldstr "class G2_C598`2<class BaseClass0,class BaseClass0> on type class G3_C1589`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C598`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C598`2<class BaseClass0,class BaseClass0>::ClassMethod1345<object>()
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G2_C598`2<class BaseClass0,class BaseClass0> on type class G3_C1589`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C598`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C598`2<class BaseClass0,class BaseClass0>::ClassMethod1344<object>()
		ldstr "G2_C598::ClassMethod1344.MI.10516<System.Object>()"
		ldstr "class G2_C598`2<class BaseClass0,class BaseClass0> on type class G3_C1589`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method4()
		ldstr "G2_C598::Method4.MI.10509()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1589`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method5()
		ldstr "G2_C598::Method5.MI.10511()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1589`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method6<object>()
		ldstr "G2_C598::Method6.10512<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1589`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G3_C1589::Method7.16950<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G3_C1589`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G3_C1589`1<class BaseClass0>
		callvirt instance string class G3_C1589`1<class BaseClass0>::ClassMethod4567<object>()
		ldstr "G3_C1589::ClassMethod4567.16952<System.Object>()"
		ldstr "class G3_C1589`1<class BaseClass0> on type class G3_C1589`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1589`1<class BaseClass0>
		callvirt instance string class G3_C1589`1<class BaseClass0>::ClassMethod4566()
		ldstr "G3_C1589::ClassMethod4566.16951()"
		ldstr "class G3_C1589`1<class BaseClass0> on type class G3_C1589`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1589`1<class BaseClass0>
		callvirt instance string class G3_C1589`1<class BaseClass0>::Method7<object>()
		ldstr "G3_C1589::Method7.16950<System.Object>()"
		ldstr "class G3_C1589`1<class BaseClass0> on type class G3_C1589`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1589`1<class BaseClass0>
		callvirt instance string class G3_C1589`1<class BaseClass0>::ClassMethod2566()
		ldstr "G2_C598::ClassMethod2566.10515()"
		ldstr "class G3_C1589`1<class BaseClass0> on type class G3_C1589`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1589`1<class BaseClass0>
		callvirt instance string class G3_C1589`1<class BaseClass0>::Method6<object>()
		ldstr "G2_C598::Method6.10512<System.Object>()"
		ldstr "class G3_C1589`1<class BaseClass0> on type class G3_C1589`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1589`1<class BaseClass0>
		callvirt instance string class G3_C1589`1<class BaseClass0>::Method5()
		ldstr "G2_C598::Method5.10510()"
		ldstr "class G3_C1589`1<class BaseClass0> on type class G3_C1589`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1589`1<class BaseClass0>
		callvirt instance string class G3_C1589`1<class BaseClass0>::Method4()
		ldstr "G2_C598::Method4.10508()"
		ldstr "class G3_C1589`1<class BaseClass0> on type class G3_C1589`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1589`1<class BaseClass0>
		callvirt instance string class G3_C1589`1<class BaseClass0>::ClassMethod1345<object>()
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G3_C1589`1<class BaseClass0> on type class G3_C1589`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1589`1<class BaseClass0>
		callvirt instance string class G3_C1589`1<class BaseClass0>::ClassMethod1344<object>()
		ldstr "G2_C598::ClassMethod1344.MI.10516<System.Object>()"
		ldstr "class G3_C1589`1<class BaseClass0> on type class G3_C1589`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G3_C1589`1<class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C11`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C11`2<class BaseClass1,class BaseClass1>::ClassMethod1345<object>()
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass1> on type class G3_C1589`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C11`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C11`2<class BaseClass1,class BaseClass1>::ClassMethod1344<object>()
		ldstr "G2_C598::ClassMethod1344.MI.10516<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass1> on type class G3_C1589`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C11`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C11`2<class BaseClass1,class BaseClass1>::Method6<object>()
		ldstr "G1_C11::Method6.4859<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass1> on type class G3_C1589`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C11`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C11`2<class BaseClass1,class BaseClass1>::Method5()
		ldstr "G2_C598::Method5.10510()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass1> on type class G3_C1589`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C11`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C11`2<class BaseClass1,class BaseClass1>::Method4()
		ldstr "G1_C11::Method4.4856()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass1> on type class G3_C1589`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C11`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C11`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G2_C598::Method7.10513<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass1> on type class G3_C1589`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G3_C1589::Method7.16950<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G3_C1589`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G2_C598::Method4.MI.10509()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1589`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G2_C598::Method5.MI.10511()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1589`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G2_C598::Method6.10512<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1589`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G3_C1589::Method7.16950<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1589`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C598`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C598`2<class BaseClass1,class BaseClass0>::ClassMethod2566()
		ldstr "G2_C598::ClassMethod2566.10515()"
		ldstr "class G2_C598`2<class BaseClass1,class BaseClass0> on type class G3_C1589`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C598`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C598`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G2_C598::Method7.10513<System.Object>()"
		ldstr "class G2_C598`2<class BaseClass1,class BaseClass0> on type class G3_C1589`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C598`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C598`2<class BaseClass1,class BaseClass0>::Method6<object>()
		ldstr "G2_C598::Method6.10512<System.Object>()"
		ldstr "class G2_C598`2<class BaseClass1,class BaseClass0> on type class G3_C1589`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C598`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C598`2<class BaseClass1,class BaseClass0>::Method5()
		ldstr "G2_C598::Method5.10510()"
		ldstr "class G2_C598`2<class BaseClass1,class BaseClass0> on type class G3_C1589`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C598`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C598`2<class BaseClass1,class BaseClass0>::Method4()
		ldstr "G2_C598::Method4.10508()"
		ldstr "class G2_C598`2<class BaseClass1,class BaseClass0> on type class G3_C1589`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C598`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C598`2<class BaseClass1,class BaseClass0>::ClassMethod1345<object>()
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G2_C598`2<class BaseClass1,class BaseClass0> on type class G3_C1589`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C598`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C598`2<class BaseClass1,class BaseClass0>::ClassMethod1344<object>()
		ldstr "G2_C598::ClassMethod1344.MI.10516<System.Object>()"
		ldstr "class G2_C598`2<class BaseClass1,class BaseClass0> on type class G3_C1589`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method4()
		ldstr "G2_C598::Method4.MI.10509()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1589`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method5()
		ldstr "G2_C598::Method5.MI.10511()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1589`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method6<object>()
		ldstr "G2_C598::Method6.10512<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1589`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G3_C1589::Method7.16950<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G3_C1589`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G3_C1589::Method7.16950<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G3_C1589`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G3_C1589`1<class BaseClass1>
		callvirt instance string class G3_C1589`1<class BaseClass1>::ClassMethod4567<object>()
		ldstr "G3_C1589::ClassMethod4567.16952<System.Object>()"
		ldstr "class G3_C1589`1<class BaseClass1> on type class G3_C1589`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1589`1<class BaseClass1>
		callvirt instance string class G3_C1589`1<class BaseClass1>::ClassMethod4566()
		ldstr "G3_C1589::ClassMethod4566.16951()"
		ldstr "class G3_C1589`1<class BaseClass1> on type class G3_C1589`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1589`1<class BaseClass1>
		callvirt instance string class G3_C1589`1<class BaseClass1>::Method7<object>()
		ldstr "G3_C1589::Method7.16950<System.Object>()"
		ldstr "class G3_C1589`1<class BaseClass1> on type class G3_C1589`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1589`1<class BaseClass1>
		callvirt instance string class G3_C1589`1<class BaseClass1>::ClassMethod2566()
		ldstr "G2_C598::ClassMethod2566.10515()"
		ldstr "class G3_C1589`1<class BaseClass1> on type class G3_C1589`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1589`1<class BaseClass1>
		callvirt instance string class G3_C1589`1<class BaseClass1>::Method6<object>()
		ldstr "G2_C598::Method6.10512<System.Object>()"
		ldstr "class G3_C1589`1<class BaseClass1> on type class G3_C1589`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1589`1<class BaseClass1>
		callvirt instance string class G3_C1589`1<class BaseClass1>::Method5()
		ldstr "G2_C598::Method5.10510()"
		ldstr "class G3_C1589`1<class BaseClass1> on type class G3_C1589`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1589`1<class BaseClass1>
		callvirt instance string class G3_C1589`1<class BaseClass1>::Method4()
		ldstr "G2_C598::Method4.10508()"
		ldstr "class G3_C1589`1<class BaseClass1> on type class G3_C1589`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1589`1<class BaseClass1>
		callvirt instance string class G3_C1589`1<class BaseClass1>::ClassMethod1345<object>()
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G3_C1589`1<class BaseClass1> on type class G3_C1589`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1589`1<class BaseClass1>
		callvirt instance string class G3_C1589`1<class BaseClass1>::ClassMethod1344<object>()
		ldstr "G2_C598::ClassMethod1344.MI.10516<System.Object>()"
		ldstr "class G3_C1589`1<class BaseClass1> on type class G3_C1589`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G2_C598`2<class BaseClass0,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C11`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C11`2<class BaseClass0,class BaseClass1>::ClassMethod1345<object>()
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass1> on type class G2_C598`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C11`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C11`2<class BaseClass0,class BaseClass1>::ClassMethod1344<object>()
		ldstr "G2_C598::ClassMethod1344.MI.10516<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass1> on type class G2_C598`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C11`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C11`2<class BaseClass0,class BaseClass1>::Method6<object>()
		ldstr "G1_C11::Method6.4859<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass1> on type class G2_C598`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C11`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C11`2<class BaseClass0,class BaseClass1>::Method5()
		ldstr "G2_C598::Method5.10510()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass1> on type class G2_C598`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C11`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C11`2<class BaseClass0,class BaseClass1>::Method4()
		ldstr "G1_C11::Method4.4856()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass1> on type class G2_C598`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C11`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C11`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C598::Method7.10513<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass1> on type class G2_C598`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C598`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G2_C598::Method4.MI.10509()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C598`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G2_C598::Method5.MI.10511()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C598`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G2_C598::Method6.10512<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C598`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C598::Method7.MI.10514<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C598`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C598`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C598`2<class BaseClass0,class BaseClass0>::ClassMethod2566()
		ldstr "G2_C598::ClassMethod2566.10515()"
		ldstr "class G2_C598`2<class BaseClass0,class BaseClass0> on type class G2_C598`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C598`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C598`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G2_C598::Method7.10513<System.Object>()"
		ldstr "class G2_C598`2<class BaseClass0,class BaseClass0> on type class G2_C598`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C598`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C598`2<class BaseClass0,class BaseClass0>::Method6<object>()
		ldstr "G2_C598::Method6.10512<System.Object>()"
		ldstr "class G2_C598`2<class BaseClass0,class BaseClass0> on type class G2_C598`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C598`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C598`2<class BaseClass0,class BaseClass0>::Method5()
		ldstr "G2_C598::Method5.10510()"
		ldstr "class G2_C598`2<class BaseClass0,class BaseClass0> on type class G2_C598`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C598`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C598`2<class BaseClass0,class BaseClass0>::Method4()
		ldstr "G2_C598::Method4.10508()"
		ldstr "class G2_C598`2<class BaseClass0,class BaseClass0> on type class G2_C598`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C598`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C598`2<class BaseClass0,class BaseClass0>::ClassMethod1345<object>()
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G2_C598`2<class BaseClass0,class BaseClass0> on type class G2_C598`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C598`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C598`2<class BaseClass0,class BaseClass0>::ClassMethod1344<object>()
		ldstr "G2_C598::ClassMethod1344.MI.10516<System.Object>()"
		ldstr "class G2_C598`2<class BaseClass0,class BaseClass0> on type class G2_C598`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method4()
		ldstr "G2_C598::Method4.MI.10509()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C598`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method5()
		ldstr "G2_C598::Method5.MI.10511()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C598`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method6<object>()
		ldstr "G2_C598::Method6.10512<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C598`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G2_C598::Method7.MI.10514<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G2_C598`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G2_C598`2<class BaseClass0,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C11`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C11`2<class BaseClass0,class BaseClass1>::ClassMethod1345<object>()
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass1> on type class G2_C598`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C11`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C11`2<class BaseClass0,class BaseClass1>::ClassMethod1344<object>()
		ldstr "G2_C598::ClassMethod1344.MI.10516<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass1> on type class G2_C598`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C11`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C11`2<class BaseClass0,class BaseClass1>::Method6<object>()
		ldstr "G1_C11::Method6.4859<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass1> on type class G2_C598`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C11`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C11`2<class BaseClass0,class BaseClass1>::Method5()
		ldstr "G2_C598::Method5.10510()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass1> on type class G2_C598`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C11`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C11`2<class BaseClass0,class BaseClass1>::Method4()
		ldstr "G1_C11::Method4.4856()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass1> on type class G2_C598`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C11`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C11`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C598::Method7.10513<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass1> on type class G2_C598`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C598`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G2_C598::Method4.MI.10509()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C598`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G2_C598::Method5.MI.10511()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C598`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G2_C598::Method6.10512<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C598`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C598::Method7.MI.10514<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C598`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C598`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C598`2<class BaseClass0,class BaseClass1>::ClassMethod2566()
		ldstr "G2_C598::ClassMethod2566.10515()"
		ldstr "class G2_C598`2<class BaseClass0,class BaseClass1> on type class G2_C598`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C598`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C598`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C598::Method7.10513<System.Object>()"
		ldstr "class G2_C598`2<class BaseClass0,class BaseClass1> on type class G2_C598`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C598`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C598`2<class BaseClass0,class BaseClass1>::Method6<object>()
		ldstr "G2_C598::Method6.10512<System.Object>()"
		ldstr "class G2_C598`2<class BaseClass0,class BaseClass1> on type class G2_C598`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C598`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C598`2<class BaseClass0,class BaseClass1>::Method5()
		ldstr "G2_C598::Method5.10510()"
		ldstr "class G2_C598`2<class BaseClass0,class BaseClass1> on type class G2_C598`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C598`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C598`2<class BaseClass0,class BaseClass1>::Method4()
		ldstr "G2_C598::Method4.10508()"
		ldstr "class G2_C598`2<class BaseClass0,class BaseClass1> on type class G2_C598`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C598`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C598`2<class BaseClass0,class BaseClass1>::ClassMethod1345<object>()
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G2_C598`2<class BaseClass0,class BaseClass1> on type class G2_C598`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C598`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C598`2<class BaseClass0,class BaseClass1>::ClassMethod1344<object>()
		ldstr "G2_C598::ClassMethod1344.MI.10516<System.Object>()"
		ldstr "class G2_C598`2<class BaseClass0,class BaseClass1> on type class G2_C598`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method4()
		ldstr "G2_C598::Method4.MI.10509()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C598`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method5()
		ldstr "G2_C598::Method5.MI.10511()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C598`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method6<object>()
		ldstr "G2_C598::Method6.10512<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C598`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G2_C598::Method7.MI.10514<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G2_C598`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G2_C598`2<class BaseClass1,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C11`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C11`2<class BaseClass1,class BaseClass1>::ClassMethod1345<object>()
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass1> on type class G2_C598`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C11`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C11`2<class BaseClass1,class BaseClass1>::ClassMethod1344<object>()
		ldstr "G2_C598::ClassMethod1344.MI.10516<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass1> on type class G2_C598`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C11`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C11`2<class BaseClass1,class BaseClass1>::Method6<object>()
		ldstr "G1_C11::Method6.4859<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass1> on type class G2_C598`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C11`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C11`2<class BaseClass1,class BaseClass1>::Method5()
		ldstr "G2_C598::Method5.10510()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass1> on type class G2_C598`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C11`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C11`2<class BaseClass1,class BaseClass1>::Method4()
		ldstr "G1_C11::Method4.4856()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass1> on type class G2_C598`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C11`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C11`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G2_C598::Method7.10513<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass1> on type class G2_C598`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G2_C598::Method7.MI.10514<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C598`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G2_C598::Method4.MI.10509()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C598`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G2_C598::Method5.MI.10511()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C598`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G2_C598::Method6.10512<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C598`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C598::Method7.MI.10514<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C598`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C598`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C598`2<class BaseClass1,class BaseClass0>::ClassMethod2566()
		ldstr "G2_C598::ClassMethod2566.10515()"
		ldstr "class G2_C598`2<class BaseClass1,class BaseClass0> on type class G2_C598`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C598`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C598`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G2_C598::Method7.10513<System.Object>()"
		ldstr "class G2_C598`2<class BaseClass1,class BaseClass0> on type class G2_C598`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C598`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C598`2<class BaseClass1,class BaseClass0>::Method6<object>()
		ldstr "G2_C598::Method6.10512<System.Object>()"
		ldstr "class G2_C598`2<class BaseClass1,class BaseClass0> on type class G2_C598`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C598`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C598`2<class BaseClass1,class BaseClass0>::Method5()
		ldstr "G2_C598::Method5.10510()"
		ldstr "class G2_C598`2<class BaseClass1,class BaseClass0> on type class G2_C598`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C598`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C598`2<class BaseClass1,class BaseClass0>::Method4()
		ldstr "G2_C598::Method4.10508()"
		ldstr "class G2_C598`2<class BaseClass1,class BaseClass0> on type class G2_C598`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C598`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C598`2<class BaseClass1,class BaseClass0>::ClassMethod1345<object>()
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G2_C598`2<class BaseClass1,class BaseClass0> on type class G2_C598`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C598`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C598`2<class BaseClass1,class BaseClass0>::ClassMethod1344<object>()
		ldstr "G2_C598::ClassMethod1344.MI.10516<System.Object>()"
		ldstr "class G2_C598`2<class BaseClass1,class BaseClass0> on type class G2_C598`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method4()
		ldstr "G2_C598::Method4.MI.10509()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C598`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method5()
		ldstr "G2_C598::Method5.MI.10511()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C598`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method6<object>()
		ldstr "G2_C598::Method6.10512<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C598`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G2_C598::Method7.MI.10514<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G2_C598`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G2_C598::Method7.MI.10514<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G2_C598`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G2_C598`2<class BaseClass1,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C11`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C11`2<class BaseClass1,class BaseClass1>::ClassMethod1345<object>()
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass1> on type class G2_C598`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C11`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C11`2<class BaseClass1,class BaseClass1>::ClassMethod1344<object>()
		ldstr "G2_C598::ClassMethod1344.MI.10516<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass1> on type class G2_C598`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C11`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C11`2<class BaseClass1,class BaseClass1>::Method6<object>()
		ldstr "G1_C11::Method6.4859<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass1> on type class G2_C598`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C11`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C11`2<class BaseClass1,class BaseClass1>::Method5()
		ldstr "G2_C598::Method5.10510()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass1> on type class G2_C598`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C11`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C11`2<class BaseClass1,class BaseClass1>::Method4()
		ldstr "G1_C11::Method4.4856()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass1> on type class G2_C598`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C11`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C11`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G2_C598::Method7.10513<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass1> on type class G2_C598`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G2_C598::Method7.MI.10514<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C598`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G2_C598::Method4.MI.10509()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C598`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G2_C598::Method5.MI.10511()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C598`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G2_C598::Method6.10512<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C598`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C598::Method7.MI.10514<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C598`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C598`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C598`2<class BaseClass1,class BaseClass1>::ClassMethod2566()
		ldstr "G2_C598::ClassMethod2566.10515()"
		ldstr "class G2_C598`2<class BaseClass1,class BaseClass1> on type class G2_C598`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C598`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C598`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G2_C598::Method7.10513<System.Object>()"
		ldstr "class G2_C598`2<class BaseClass1,class BaseClass1> on type class G2_C598`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C598`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C598`2<class BaseClass1,class BaseClass1>::Method6<object>()
		ldstr "G2_C598::Method6.10512<System.Object>()"
		ldstr "class G2_C598`2<class BaseClass1,class BaseClass1> on type class G2_C598`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C598`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C598`2<class BaseClass1,class BaseClass1>::Method5()
		ldstr "G2_C598::Method5.10510()"
		ldstr "class G2_C598`2<class BaseClass1,class BaseClass1> on type class G2_C598`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C598`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C598`2<class BaseClass1,class BaseClass1>::Method4()
		ldstr "G2_C598::Method4.10508()"
		ldstr "class G2_C598`2<class BaseClass1,class BaseClass1> on type class G2_C598`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C598`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C598`2<class BaseClass1,class BaseClass1>::ClassMethod1345<object>()
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G2_C598`2<class BaseClass1,class BaseClass1> on type class G2_C598`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C598`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C598`2<class BaseClass1,class BaseClass1>::ClassMethod1344<object>()
		ldstr "G2_C598::ClassMethod1344.MI.10516<System.Object>()"
		ldstr "class G2_C598`2<class BaseClass1,class BaseClass1> on type class G2_C598`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method4()
		ldstr "G2_C598::Method4.MI.10509()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C598`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method5()
		ldstr "G2_C598::Method5.MI.10511()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C598`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method6<object>()
		ldstr "G2_C598::Method6.10512<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C598`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G2_C598::Method7.MI.10514<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G2_C598`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G2_C598::Method7.MI.10514<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G2_C598`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G1_C11`2<class BaseClass0,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C11`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C11`2<class BaseClass0,class BaseClass0>::ClassMethod1345<object>()
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass0> on type class G1_C11`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C11`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C11`2<class BaseClass0,class BaseClass0>::ClassMethod1344<object>()
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass0> on type class G1_C11`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C11`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C11`2<class BaseClass0,class BaseClass0>::Method6<object>()
		ldstr "G1_C11::Method6.4859<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass0> on type class G1_C11`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C11`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C11`2<class BaseClass0,class BaseClass0>::Method5()
		ldstr "G1_C11::Method5.4858()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass0> on type class G1_C11`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C11`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C11`2<class BaseClass0,class BaseClass0>::Method4()
		ldstr "G1_C11::Method4.4856()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass0> on type class G1_C11`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C11`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C11`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G1_C11::Method7.4854<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass0> on type class G1_C11`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G1_C11`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G1_C11::Method4.MI.4857()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C11`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G1_C11::Method5.4858()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C11`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G1_C11::Method6.MI.4860<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C11`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G1_C11`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G1_C11`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G1_C11`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G1_C11`2<class BaseClass0,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C11`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C11`2<class BaseClass0,class BaseClass1>::ClassMethod1345<object>()
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass1> on type class G1_C11`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C11`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C11`2<class BaseClass0,class BaseClass1>::ClassMethod1344<object>()
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass1> on type class G1_C11`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C11`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C11`2<class BaseClass0,class BaseClass1>::Method6<object>()
		ldstr "G1_C11::Method6.4859<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass1> on type class G1_C11`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C11`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C11`2<class BaseClass0,class BaseClass1>::Method5()
		ldstr "G1_C11::Method5.4858()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass1> on type class G1_C11`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C11`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C11`2<class BaseClass0,class BaseClass1>::Method4()
		ldstr "G1_C11::Method4.4856()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass1> on type class G1_C11`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C11`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C11`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G1_C11::Method7.4854<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass1> on type class G1_C11`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G1_C11`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G1_C11::Method4.MI.4857()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C11`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G1_C11::Method5.4858()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C11`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G1_C11::Method6.MI.4860<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C11`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G1_C11`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G1_C11`2<class BaseClass1,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C11`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C11`2<class BaseClass1,class BaseClass0>::ClassMethod1345<object>()
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass0> on type class G1_C11`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C11`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C11`2<class BaseClass1,class BaseClass0>::ClassMethod1344<object>()
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass0> on type class G1_C11`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C11`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C11`2<class BaseClass1,class BaseClass0>::Method6<object>()
		ldstr "G1_C11::Method6.4859<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass0> on type class G1_C11`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C11`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C11`2<class BaseClass1,class BaseClass0>::Method5()
		ldstr "G1_C11::Method5.4858()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass0> on type class G1_C11`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C11`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C11`2<class BaseClass1,class BaseClass0>::Method4()
		ldstr "G1_C11::Method4.4856()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass0> on type class G1_C11`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C11`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C11`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G1_C11::Method7.4854<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass0> on type class G1_C11`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G1_C11`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G1_C11::Method4.MI.4857()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C11`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G1_C11::Method5.4858()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C11`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G1_C11::Method6.MI.4860<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C11`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G1_C11`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G1_C11`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G1_C11`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G1_C11`2<class BaseClass1,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C11`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C11`2<class BaseClass1,class BaseClass1>::ClassMethod1345<object>()
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass1> on type class G1_C11`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C11`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C11`2<class BaseClass1,class BaseClass1>::ClassMethod1344<object>()
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass1> on type class G1_C11`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C11`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C11`2<class BaseClass1,class BaseClass1>::Method6<object>()
		ldstr "G1_C11::Method6.4859<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass1> on type class G1_C11`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C11`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C11`2<class BaseClass1,class BaseClass1>::Method5()
		ldstr "G1_C11::Method5.4858()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass1> on type class G1_C11`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C11`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C11`2<class BaseClass1,class BaseClass1>::Method4()
		ldstr "G1_C11::Method4.4856()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass1> on type class G1_C11`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C11`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C11`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C11::Method7.4854<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass1> on type class G1_C11`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G1_C11`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G1_C11::Method4.MI.4857()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C11`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G1_C11::Method5.4858()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C11`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G1_C11::Method6.MI.4860<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C11`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G1_C11`2<class BaseClass1,class BaseClass1>"
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
		newobj instance void class G3_C1589`1<class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C598::ClassMethod1344.MI.10516<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G1_C11::Method4.4856()#G2_C598::Method5.10510()#G1_C11::Method6.4859<System.Object>()#G2_C598::Method7.10513<System.Object>()#"
		call void Framework::M.G1_C11.T.T<class BaseClass0,class BaseClass1,class G3_C1589`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C598::ClassMethod1344.MI.10516<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G1_C11::Method4.4856()#G2_C598::Method5.10510()#G1_C11::Method6.4859<System.Object>()#G2_C598::Method7.10513<System.Object>()#"
		call void Framework::M.G1_C11.A.T<class BaseClass1,class G3_C1589`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C598::ClassMethod1344.MI.10516<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G1_C11::Method4.4856()#G2_C598::Method5.10510()#G1_C11::Method6.4859<System.Object>()#G2_C598::Method7.10513<System.Object>()#"
		call void Framework::M.G1_C11.A.B<class G3_C1589`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G3_C1589`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass1,class G3_C1589`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Framework::M.IBase2.B.B<class G3_C1589`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C598::Method4.MI.10509()#G2_C598::Method5.MI.10511()#G2_C598::Method6.10512<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass0,class G3_C1589`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C598::Method4.MI.10509()#G2_C598::Method5.MI.10511()#G2_C598::Method6.10512<System.Object>()#"
		call void Framework::M.IBase1.A<class G3_C1589`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1589::Method7.16950<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G3_C1589`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G3_C1589::Method7.16950<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G3_C1589`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G3_C1589::Method7.16950<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G3_C1589`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C598::ClassMethod1344.MI.10516<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G2_C598::ClassMethod2566.10515()#G2_C598::Method4.10508()#G2_C598::Method5.10510()#G2_C598::Method6.10512<System.Object>()#G2_C598::Method7.10513<System.Object>()#"
		call void Framework::M.G2_C598.T.T<class BaseClass0,class BaseClass0,class G3_C1589`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C598::ClassMethod1344.MI.10516<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G2_C598::ClassMethod2566.10515()#G2_C598::Method4.10508()#G2_C598::Method5.10510()#G2_C598::Method6.10512<System.Object>()#G2_C598::Method7.10513<System.Object>()#"
		call void Framework::M.G2_C598.A.T<class BaseClass0,class G3_C1589`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C598::ClassMethod1344.MI.10516<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G2_C598::ClassMethod2566.10515()#G2_C598::Method4.10508()#G2_C598::Method5.10510()#G2_C598::Method6.10512<System.Object>()#G2_C598::Method7.10513<System.Object>()#"
		call void Framework::M.G2_C598.A.A<class G3_C1589`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C598::Method4.MI.10509()#G2_C598::Method5.MI.10511()#G2_C598::Method6.10512<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass1,class G3_C1589`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C598::Method4.MI.10509()#G2_C598::Method5.MI.10511()#G2_C598::Method6.10512<System.Object>()#"
		call void Framework::M.IBase1.B<class G3_C1589`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1589::Method7.16950<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G3_C1589`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G3_C1589::Method7.16950<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass0,class G3_C1589`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G3_C1589::Method7.16950<System.Object>()#"
		call void Framework::M.IBase2.A.A<class G3_C1589`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C598::ClassMethod1344.MI.10516<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G2_C598::ClassMethod2566.10515()#G3_C1589::ClassMethod4566.16951()#G3_C1589::ClassMethod4567.16952<System.Object>()#G2_C598::Method4.10508()#G2_C598::Method5.10510()#G2_C598::Method6.10512<System.Object>()#G3_C1589::Method7.16950<System.Object>()#"
		call void Framework::M.G3_C1589.T<class BaseClass0,class G3_C1589`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C598::ClassMethod1344.MI.10516<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G2_C598::ClassMethod2566.10515()#G3_C1589::ClassMethod4566.16951()#G3_C1589::ClassMethod4567.16952<System.Object>()#G2_C598::Method4.10508()#G2_C598::Method5.10510()#G2_C598::Method6.10512<System.Object>()#G3_C1589::Method7.16950<System.Object>()#"
		call void Framework::M.G3_C1589.A<class G3_C1589`1<class BaseClass0>>(!!0,string)
		newobj instance void class G3_C1589`1<class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C598::ClassMethod1344.MI.10516<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G1_C11::Method4.4856()#G2_C598::Method5.10510()#G1_C11::Method6.4859<System.Object>()#G2_C598::Method7.10513<System.Object>()#"
		call void Framework::M.G1_C11.T.T<class BaseClass1,class BaseClass1,class G3_C1589`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C598::ClassMethod1344.MI.10516<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G1_C11::Method4.4856()#G2_C598::Method5.10510()#G1_C11::Method6.4859<System.Object>()#G2_C598::Method7.10513<System.Object>()#"
		call void Framework::M.G1_C11.B.T<class BaseClass1,class G3_C1589`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C598::ClassMethod1344.MI.10516<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G1_C11::Method4.4856()#G2_C598::Method5.10510()#G1_C11::Method6.4859<System.Object>()#G2_C598::Method7.10513<System.Object>()#"
		call void Framework::M.G1_C11.B.B<class G3_C1589`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1589::Method7.16950<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G3_C1589`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G3_C1589::Method7.16950<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass1,class G3_C1589`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1589::Method7.16950<System.Object>()#"
		call void Framework::M.IBase2.B.B<class G3_C1589`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C598::Method4.MI.10509()#G2_C598::Method5.MI.10511()#G2_C598::Method6.10512<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass0,class G3_C1589`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C598::Method4.MI.10509()#G2_C598::Method5.MI.10511()#G2_C598::Method6.10512<System.Object>()#"
		call void Framework::M.IBase1.A<class G3_C1589`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1589::Method7.16950<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G3_C1589`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G3_C1589::Method7.16950<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G3_C1589`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1589::Method7.16950<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G3_C1589`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C598::ClassMethod1344.MI.10516<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G2_C598::ClassMethod2566.10515()#G2_C598::Method4.10508()#G2_C598::Method5.10510()#G2_C598::Method6.10512<System.Object>()#G2_C598::Method7.10513<System.Object>()#"
		call void Framework::M.G2_C598.T.T<class BaseClass1,class BaseClass0,class G3_C1589`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C598::ClassMethod1344.MI.10516<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G2_C598::ClassMethod2566.10515()#G2_C598::Method4.10508()#G2_C598::Method5.10510()#G2_C598::Method6.10512<System.Object>()#G2_C598::Method7.10513<System.Object>()#"
		call void Framework::M.G2_C598.B.T<class BaseClass0,class G3_C1589`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C598::ClassMethod1344.MI.10516<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G2_C598::ClassMethod2566.10515()#G2_C598::Method4.10508()#G2_C598::Method5.10510()#G2_C598::Method6.10512<System.Object>()#G2_C598::Method7.10513<System.Object>()#"
		call void Framework::M.G2_C598.B.A<class G3_C1589`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C598::Method4.MI.10509()#G2_C598::Method5.MI.10511()#G2_C598::Method6.10512<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass1,class G3_C1589`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C598::Method4.MI.10509()#G2_C598::Method5.MI.10511()#G2_C598::Method6.10512<System.Object>()#"
		call void Framework::M.IBase1.B<class G3_C1589`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1589::Method7.16950<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass0,class G3_C1589`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G3_C1589::Method7.16950<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass0,class G3_C1589`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1589::Method7.16950<System.Object>()#"
		call void Framework::M.IBase2.B.A<class G3_C1589`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1589::Method7.16950<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G3_C1589`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G3_C1589::Method7.16950<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass0,class G3_C1589`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1589::Method7.16950<System.Object>()#"
		call void Framework::M.IBase2.A.A<class G3_C1589`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C598::ClassMethod1344.MI.10516<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G2_C598::ClassMethod2566.10515()#G3_C1589::ClassMethod4566.16951()#G3_C1589::ClassMethod4567.16952<System.Object>()#G2_C598::Method4.10508()#G2_C598::Method5.10510()#G2_C598::Method6.10512<System.Object>()#G3_C1589::Method7.16950<System.Object>()#"
		call void Framework::M.G3_C1589.T<class BaseClass1,class G3_C1589`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C598::ClassMethod1344.MI.10516<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G2_C598::ClassMethod2566.10515()#G3_C1589::ClassMethod4566.16951()#G3_C1589::ClassMethod4567.16952<System.Object>()#G2_C598::Method4.10508()#G2_C598::Method5.10510()#G2_C598::Method6.10512<System.Object>()#G3_C1589::Method7.16950<System.Object>()#"
		call void Framework::M.G3_C1589.B<class G3_C1589`1<class BaseClass1>>(!!0,string)
		newobj instance void class G2_C598`2<class BaseClass0,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C598::ClassMethod1344.MI.10516<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G1_C11::Method4.4856()#G2_C598::Method5.10510()#G1_C11::Method6.4859<System.Object>()#G2_C598::Method7.10513<System.Object>()#"
		call void Framework::M.G1_C11.T.T<class BaseClass0,class BaseClass1,class G2_C598`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C598::ClassMethod1344.MI.10516<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G1_C11::Method4.4856()#G2_C598::Method5.10510()#G1_C11::Method6.4859<System.Object>()#G2_C598::Method7.10513<System.Object>()#"
		call void Framework::M.G1_C11.A.T<class BaseClass1,class G2_C598`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C598::ClassMethod1344.MI.10516<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G1_C11::Method4.4856()#G2_C598::Method5.10510()#G1_C11::Method6.4859<System.Object>()#G2_C598::Method7.10513<System.Object>()#"
		call void Framework::M.G1_C11.A.B<class G2_C598`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G2_C598`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass1,class G2_C598`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Framework::M.IBase2.B.B<class G2_C598`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C598::Method4.MI.10509()#G2_C598::Method5.MI.10511()#G2_C598::Method6.10512<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass0,class G2_C598`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C598::Method4.MI.10509()#G2_C598::Method5.MI.10511()#G2_C598::Method6.10512<System.Object>()#"
		call void Framework::M.IBase1.A<class G2_C598`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C598::Method7.MI.10514<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G2_C598`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C598::Method7.MI.10514<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G2_C598`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C598::Method7.MI.10514<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G2_C598`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C598::ClassMethod1344.MI.10516<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G2_C598::ClassMethod2566.10515()#G2_C598::Method4.10508()#G2_C598::Method5.10510()#G2_C598::Method6.10512<System.Object>()#G2_C598::Method7.10513<System.Object>()#"
		call void Framework::M.G2_C598.T.T<class BaseClass0,class BaseClass0,class G2_C598`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C598::ClassMethod1344.MI.10516<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G2_C598::ClassMethod2566.10515()#G2_C598::Method4.10508()#G2_C598::Method5.10510()#G2_C598::Method6.10512<System.Object>()#G2_C598::Method7.10513<System.Object>()#"
		call void Framework::M.G2_C598.A.T<class BaseClass0,class G2_C598`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C598::ClassMethod1344.MI.10516<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G2_C598::ClassMethod2566.10515()#G2_C598::Method4.10508()#G2_C598::Method5.10510()#G2_C598::Method6.10512<System.Object>()#G2_C598::Method7.10513<System.Object>()#"
		call void Framework::M.G2_C598.A.A<class G2_C598`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C598::Method4.MI.10509()#G2_C598::Method5.MI.10511()#G2_C598::Method6.10512<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass1,class G2_C598`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C598::Method4.MI.10509()#G2_C598::Method5.MI.10511()#G2_C598::Method6.10512<System.Object>()#"
		call void Framework::M.IBase1.B<class G2_C598`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C598::Method7.MI.10514<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G2_C598`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C598::Method7.MI.10514<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass0,class G2_C598`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C598::Method7.MI.10514<System.Object>()#"
		call void Framework::M.IBase2.A.A<class G2_C598`2<class BaseClass0,class BaseClass0>>(!!0,string)
		newobj instance void class G2_C598`2<class BaseClass0,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C598::ClassMethod1344.MI.10516<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G1_C11::Method4.4856()#G2_C598::Method5.10510()#G1_C11::Method6.4859<System.Object>()#G2_C598::Method7.10513<System.Object>()#"
		call void Framework::M.G1_C11.T.T<class BaseClass0,class BaseClass1,class G2_C598`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C598::ClassMethod1344.MI.10516<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G1_C11::Method4.4856()#G2_C598::Method5.10510()#G1_C11::Method6.4859<System.Object>()#G2_C598::Method7.10513<System.Object>()#"
		call void Framework::M.G1_C11.A.T<class BaseClass1,class G2_C598`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C598::ClassMethod1344.MI.10516<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G1_C11::Method4.4856()#G2_C598::Method5.10510()#G1_C11::Method6.4859<System.Object>()#G2_C598::Method7.10513<System.Object>()#"
		call void Framework::M.G1_C11.A.B<class G2_C598`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G2_C598`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass1,class G2_C598`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Framework::M.IBase2.B.B<class G2_C598`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C598::Method4.MI.10509()#G2_C598::Method5.MI.10511()#G2_C598::Method6.10512<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass0,class G2_C598`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C598::Method4.MI.10509()#G2_C598::Method5.MI.10511()#G2_C598::Method6.10512<System.Object>()#"
		call void Framework::M.IBase1.A<class G2_C598`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C598::Method7.MI.10514<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G2_C598`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C598::Method7.MI.10514<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G2_C598`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C598::Method7.MI.10514<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G2_C598`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C598::ClassMethod1344.MI.10516<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G2_C598::ClassMethod2566.10515()#G2_C598::Method4.10508()#G2_C598::Method5.10510()#G2_C598::Method6.10512<System.Object>()#G2_C598::Method7.10513<System.Object>()#"
		call void Framework::M.G2_C598.T.T<class BaseClass0,class BaseClass1,class G2_C598`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C598::ClassMethod1344.MI.10516<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G2_C598::ClassMethod2566.10515()#G2_C598::Method4.10508()#G2_C598::Method5.10510()#G2_C598::Method6.10512<System.Object>()#G2_C598::Method7.10513<System.Object>()#"
		call void Framework::M.G2_C598.A.T<class BaseClass1,class G2_C598`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C598::ClassMethod1344.MI.10516<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G2_C598::ClassMethod2566.10515()#G2_C598::Method4.10508()#G2_C598::Method5.10510()#G2_C598::Method6.10512<System.Object>()#G2_C598::Method7.10513<System.Object>()#"
		call void Framework::M.G2_C598.A.B<class G2_C598`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C598::Method4.MI.10509()#G2_C598::Method5.MI.10511()#G2_C598::Method6.10512<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass1,class G2_C598`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C598::Method4.MI.10509()#G2_C598::Method5.MI.10511()#G2_C598::Method6.10512<System.Object>()#"
		call void Framework::M.IBase1.B<class G2_C598`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C598::Method7.MI.10514<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G2_C598`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C598::Method7.MI.10514<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass0,class G2_C598`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C598::Method7.MI.10514<System.Object>()#"
		call void Framework::M.IBase2.A.A<class G2_C598`2<class BaseClass0,class BaseClass1>>(!!0,string)
		newobj instance void class G2_C598`2<class BaseClass1,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C598::ClassMethod1344.MI.10516<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G1_C11::Method4.4856()#G2_C598::Method5.10510()#G1_C11::Method6.4859<System.Object>()#G2_C598::Method7.10513<System.Object>()#"
		call void Framework::M.G1_C11.T.T<class BaseClass1,class BaseClass1,class G2_C598`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C598::ClassMethod1344.MI.10516<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G1_C11::Method4.4856()#G2_C598::Method5.10510()#G1_C11::Method6.4859<System.Object>()#G2_C598::Method7.10513<System.Object>()#"
		call void Framework::M.G1_C11.B.T<class BaseClass1,class G2_C598`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C598::ClassMethod1344.MI.10516<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G1_C11::Method4.4856()#G2_C598::Method5.10510()#G1_C11::Method6.4859<System.Object>()#G2_C598::Method7.10513<System.Object>()#"
		call void Framework::M.G1_C11.B.B<class G2_C598`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C598::Method7.MI.10514<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G2_C598`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C598::Method7.MI.10514<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass1,class G2_C598`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C598::Method7.MI.10514<System.Object>()#"
		call void Framework::M.IBase2.B.B<class G2_C598`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C598::Method4.MI.10509()#G2_C598::Method5.MI.10511()#G2_C598::Method6.10512<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass0,class G2_C598`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C598::Method4.MI.10509()#G2_C598::Method5.MI.10511()#G2_C598::Method6.10512<System.Object>()#"
		call void Framework::M.IBase1.A<class G2_C598`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C598::Method7.MI.10514<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G2_C598`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C598::Method7.MI.10514<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G2_C598`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C598::Method7.MI.10514<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G2_C598`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C598::ClassMethod1344.MI.10516<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G2_C598::ClassMethod2566.10515()#G2_C598::Method4.10508()#G2_C598::Method5.10510()#G2_C598::Method6.10512<System.Object>()#G2_C598::Method7.10513<System.Object>()#"
		call void Framework::M.G2_C598.T.T<class BaseClass1,class BaseClass0,class G2_C598`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C598::ClassMethod1344.MI.10516<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G2_C598::ClassMethod2566.10515()#G2_C598::Method4.10508()#G2_C598::Method5.10510()#G2_C598::Method6.10512<System.Object>()#G2_C598::Method7.10513<System.Object>()#"
		call void Framework::M.G2_C598.B.T<class BaseClass0,class G2_C598`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C598::ClassMethod1344.MI.10516<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G2_C598::ClassMethod2566.10515()#G2_C598::Method4.10508()#G2_C598::Method5.10510()#G2_C598::Method6.10512<System.Object>()#G2_C598::Method7.10513<System.Object>()#"
		call void Framework::M.G2_C598.B.A<class G2_C598`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C598::Method4.MI.10509()#G2_C598::Method5.MI.10511()#G2_C598::Method6.10512<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass1,class G2_C598`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C598::Method4.MI.10509()#G2_C598::Method5.MI.10511()#G2_C598::Method6.10512<System.Object>()#"
		call void Framework::M.IBase1.B<class G2_C598`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C598::Method7.MI.10514<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass0,class G2_C598`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C598::Method7.MI.10514<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass0,class G2_C598`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C598::Method7.MI.10514<System.Object>()#"
		call void Framework::M.IBase2.B.A<class G2_C598`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C598::Method7.MI.10514<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G2_C598`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C598::Method7.MI.10514<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass0,class G2_C598`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C598::Method7.MI.10514<System.Object>()#"
		call void Framework::M.IBase2.A.A<class G2_C598`2<class BaseClass1,class BaseClass0>>(!!0,string)
		newobj instance void class G2_C598`2<class BaseClass1,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C598::ClassMethod1344.MI.10516<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G1_C11::Method4.4856()#G2_C598::Method5.10510()#G1_C11::Method6.4859<System.Object>()#G2_C598::Method7.10513<System.Object>()#"
		call void Framework::M.G1_C11.T.T<class BaseClass1,class BaseClass1,class G2_C598`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C598::ClassMethod1344.MI.10516<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G1_C11::Method4.4856()#G2_C598::Method5.10510()#G1_C11::Method6.4859<System.Object>()#G2_C598::Method7.10513<System.Object>()#"
		call void Framework::M.G1_C11.B.T<class BaseClass1,class G2_C598`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C598::ClassMethod1344.MI.10516<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G1_C11::Method4.4856()#G2_C598::Method5.10510()#G1_C11::Method6.4859<System.Object>()#G2_C598::Method7.10513<System.Object>()#"
		call void Framework::M.G1_C11.B.B<class G2_C598`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C598::Method7.MI.10514<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G2_C598`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C598::Method7.MI.10514<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass1,class G2_C598`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C598::Method7.MI.10514<System.Object>()#"
		call void Framework::M.IBase2.B.B<class G2_C598`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C598::Method4.MI.10509()#G2_C598::Method5.MI.10511()#G2_C598::Method6.10512<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass0,class G2_C598`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C598::Method4.MI.10509()#G2_C598::Method5.MI.10511()#G2_C598::Method6.10512<System.Object>()#"
		call void Framework::M.IBase1.A<class G2_C598`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C598::Method7.MI.10514<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G2_C598`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C598::Method7.MI.10514<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G2_C598`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C598::Method7.MI.10514<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G2_C598`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C598::ClassMethod1344.MI.10516<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G2_C598::ClassMethod2566.10515()#G2_C598::Method4.10508()#G2_C598::Method5.10510()#G2_C598::Method6.10512<System.Object>()#G2_C598::Method7.10513<System.Object>()#"
		call void Framework::M.G2_C598.T.T<class BaseClass1,class BaseClass1,class G2_C598`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C598::ClassMethod1344.MI.10516<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G2_C598::ClassMethod2566.10515()#G2_C598::Method4.10508()#G2_C598::Method5.10510()#G2_C598::Method6.10512<System.Object>()#G2_C598::Method7.10513<System.Object>()#"
		call void Framework::M.G2_C598.B.T<class BaseClass1,class G2_C598`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C598::ClassMethod1344.MI.10516<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G2_C598::ClassMethod2566.10515()#G2_C598::Method4.10508()#G2_C598::Method5.10510()#G2_C598::Method6.10512<System.Object>()#G2_C598::Method7.10513<System.Object>()#"
		call void Framework::M.G2_C598.B.B<class G2_C598`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C598::Method4.MI.10509()#G2_C598::Method5.MI.10511()#G2_C598::Method6.10512<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass1,class G2_C598`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C598::Method4.MI.10509()#G2_C598::Method5.MI.10511()#G2_C598::Method6.10512<System.Object>()#"
		call void Framework::M.IBase1.B<class G2_C598`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C598::Method7.MI.10514<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass0,class G2_C598`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C598::Method7.MI.10514<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass0,class G2_C598`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C598::Method7.MI.10514<System.Object>()#"
		call void Framework::M.IBase2.B.A<class G2_C598`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C598::Method7.MI.10514<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G2_C598`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C598::Method7.MI.10514<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass0,class G2_C598`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C598::Method7.MI.10514<System.Object>()#"
		call void Framework::M.IBase2.A.A<class G2_C598`2<class BaseClass1,class BaseClass1>>(!!0,string)
		newobj instance void class G1_C11`2<class BaseClass0,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G1_C11::Method4.4856()#G1_C11::Method5.4858()#G1_C11::Method6.4859<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Framework::M.G1_C11.T.T<class BaseClass0,class BaseClass0,class G1_C11`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G1_C11::Method4.4856()#G1_C11::Method5.4858()#G1_C11::Method6.4859<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Framework::M.G1_C11.A.T<class BaseClass0,class G1_C11`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G1_C11::Method4.4856()#G1_C11::Method5.4858()#G1_C11::Method6.4859<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Framework::M.G1_C11.A.A<class G1_C11`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass0,class G1_C11`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass0,class G1_C11`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Framework::M.IBase2.B.A<class G1_C11`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method4.MI.4857()#G1_C11::Method5.4858()#G1_C11::Method6.MI.4860<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass0,class G1_C11`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method4.MI.4857()#G1_C11::Method5.4858()#G1_C11::Method6.MI.4860<System.Object>()#"
		call void Framework::M.IBase1.A<class G1_C11`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G1_C11`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass0,class G1_C11`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Framework::M.IBase2.A.A<class G1_C11`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G1_C11`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G1_C11`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G1_C11`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G1_C11`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass1,class G1_C11`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Framework::M.IBase2.B.B<class G1_C11`2<class BaseClass0,class BaseClass0>>(!!0,string)
		newobj instance void class G1_C11`2<class BaseClass0,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G1_C11::Method4.4856()#G1_C11::Method5.4858()#G1_C11::Method6.4859<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Framework::M.G1_C11.T.T<class BaseClass0,class BaseClass1,class G1_C11`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G1_C11::Method4.4856()#G1_C11::Method5.4858()#G1_C11::Method6.4859<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Framework::M.G1_C11.A.T<class BaseClass1,class G1_C11`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G1_C11::Method4.4856()#G1_C11::Method5.4858()#G1_C11::Method6.4859<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Framework::M.G1_C11.A.B<class G1_C11`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G1_C11`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass1,class G1_C11`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Framework::M.IBase2.B.B<class G1_C11`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method4.MI.4857()#G1_C11::Method5.4858()#G1_C11::Method6.MI.4860<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass0,class G1_C11`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method4.MI.4857()#G1_C11::Method5.4858()#G1_C11::Method6.MI.4860<System.Object>()#"
		call void Framework::M.IBase1.A<class G1_C11`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G1_C11`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G1_C11`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G1_C11`2<class BaseClass0,class BaseClass1>>(!!0,string)
		newobj instance void class G1_C11`2<class BaseClass1,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G1_C11::Method4.4856()#G1_C11::Method5.4858()#G1_C11::Method6.4859<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Framework::M.G1_C11.T.T<class BaseClass1,class BaseClass0,class G1_C11`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G1_C11::Method4.4856()#G1_C11::Method5.4858()#G1_C11::Method6.4859<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Framework::M.G1_C11.B.T<class BaseClass0,class G1_C11`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G1_C11::Method4.4856()#G1_C11::Method5.4858()#G1_C11::Method6.4859<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Framework::M.G1_C11.B.A<class G1_C11`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass0,class G1_C11`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass0,class G1_C11`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Framework::M.IBase2.B.A<class G1_C11`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method4.MI.4857()#G1_C11::Method5.4858()#G1_C11::Method6.MI.4860<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass0,class G1_C11`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method4.MI.4857()#G1_C11::Method5.4858()#G1_C11::Method6.MI.4860<System.Object>()#"
		call void Framework::M.IBase1.A<class G1_C11`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G1_C11`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass0,class G1_C11`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Framework::M.IBase2.A.A<class G1_C11`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G1_C11`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G1_C11`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G1_C11`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G1_C11`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass1,class G1_C11`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Framework::M.IBase2.B.B<class G1_C11`2<class BaseClass1,class BaseClass0>>(!!0,string)
		newobj instance void class G1_C11`2<class BaseClass1,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G1_C11::Method4.4856()#G1_C11::Method5.4858()#G1_C11::Method6.4859<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Framework::M.G1_C11.T.T<class BaseClass1,class BaseClass1,class G1_C11`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G1_C11::Method4.4856()#G1_C11::Method5.4858()#G1_C11::Method6.4859<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Framework::M.G1_C11.B.T<class BaseClass1,class G1_C11`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G1_C11::Method4.4856()#G1_C11::Method5.4858()#G1_C11::Method6.4859<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Framework::M.G1_C11.B.B<class G1_C11`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G1_C11`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass1,class G1_C11`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Framework::M.IBase2.B.B<class G1_C11`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method4.MI.4857()#G1_C11::Method5.4858()#G1_C11::Method6.MI.4860<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass0,class G1_C11`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method4.MI.4857()#G1_C11::Method5.4858()#G1_C11::Method6.MI.4860<System.Object>()#"
		call void Framework::M.IBase1.A<class G1_C11`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G1_C11`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G1_C11`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G1_C11`2<class BaseClass1,class BaseClass1>>(!!0,string)
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
		newobj instance void class G3_C1589`1<class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C11`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass0,class BaseClass1>::ClassMethod1345<object>()
		calli default string(class G3_C1589`1<class BaseClass0>)
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass1> on type class G3_C1589`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C11`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass0,class BaseClass1>::ClassMethod1344<object>()
		calli default string(class G3_C1589`1<class BaseClass0>)
		ldstr "G2_C598::ClassMethod1344.MI.10516<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass1> on type class G3_C1589`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C11`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass0,class BaseClass1>::Method6<object>()
		calli default string(class G3_C1589`1<class BaseClass0>)
		ldstr "G1_C11::Method6.4859<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass1> on type class G3_C1589`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C11`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass0,class BaseClass1>::Method5()
		calli default string(class G3_C1589`1<class BaseClass0>)
		ldstr "G2_C598::Method5.10510()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass1> on type class G3_C1589`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C11`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass0,class BaseClass1>::Method4()
		calli default string(class G3_C1589`1<class BaseClass0>)
		ldstr "G1_C11::Method4.4856()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass1> on type class G3_C1589`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C11`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1589`1<class BaseClass0>)
		ldstr "G2_C598::Method7.10513<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass1> on type class G3_C1589`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1589`1<class BaseClass0>)
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G3_C1589`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G3_C1589`1<class BaseClass0>)
		ldstr "G2_C598::Method4.MI.10509()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1589`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G3_C1589`1<class BaseClass0>)
		ldstr "G2_C598::Method5.MI.10511()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1589`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G3_C1589`1<class BaseClass0>)
		ldstr "G2_C598::Method6.10512<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1589`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1589`1<class BaseClass0>)
		ldstr "G3_C1589::Method7.16950<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1589`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C598`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C598`2<class BaseClass0,class BaseClass0>::ClassMethod2566()
		calli default string(class G3_C1589`1<class BaseClass0>)
		ldstr "G2_C598::ClassMethod2566.10515()"
		ldstr "class G2_C598`2<class BaseClass0,class BaseClass0> on type class G3_C1589`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C598`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C598`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1589`1<class BaseClass0>)
		ldstr "G2_C598::Method7.10513<System.Object>()"
		ldstr "class G2_C598`2<class BaseClass0,class BaseClass0> on type class G3_C1589`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C598`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C598`2<class BaseClass0,class BaseClass0>::Method6<object>()
		calli default string(class G3_C1589`1<class BaseClass0>)
		ldstr "G2_C598::Method6.10512<System.Object>()"
		ldstr "class G2_C598`2<class BaseClass0,class BaseClass0> on type class G3_C1589`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C598`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C598`2<class BaseClass0,class BaseClass0>::Method5()
		calli default string(class G3_C1589`1<class BaseClass0>)
		ldstr "G2_C598::Method5.10510()"
		ldstr "class G2_C598`2<class BaseClass0,class BaseClass0> on type class G3_C1589`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C598`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C598`2<class BaseClass0,class BaseClass0>::Method4()
		calli default string(class G3_C1589`1<class BaseClass0>)
		ldstr "G2_C598::Method4.10508()"
		ldstr "class G2_C598`2<class BaseClass0,class BaseClass0> on type class G3_C1589`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C598`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C598`2<class BaseClass0,class BaseClass0>::ClassMethod1345<object>()
		calli default string(class G3_C1589`1<class BaseClass0>)
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G2_C598`2<class BaseClass0,class BaseClass0> on type class G3_C1589`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C598`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C598`2<class BaseClass0,class BaseClass0>::ClassMethod1344<object>()
		calli default string(class G3_C1589`1<class BaseClass0>)
		ldstr "G2_C598::ClassMethod1344.MI.10516<System.Object>()"
		ldstr "class G2_C598`2<class BaseClass0,class BaseClass0> on type class G3_C1589`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method4()
		calli default string(class G3_C1589`1<class BaseClass0>)
		ldstr "G2_C598::Method4.MI.10509()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1589`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method5()
		calli default string(class G3_C1589`1<class BaseClass0>)
		ldstr "G2_C598::Method5.MI.10511()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1589`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method6<object>()
		calli default string(class G3_C1589`1<class BaseClass0>)
		ldstr "G2_C598::Method6.10512<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1589`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1589`1<class BaseClass0>)
		ldstr "G3_C1589::Method7.16950<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G3_C1589`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1589`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1589`1<class BaseClass0>::ClassMethod4567<object>()
		calli default string(class G3_C1589`1<class BaseClass0>)
		ldstr "G3_C1589::ClassMethod4567.16952<System.Object>()"
		ldstr "class G3_C1589`1<class BaseClass0> on type class G3_C1589`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1589`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1589`1<class BaseClass0>::ClassMethod4566()
		calli default string(class G3_C1589`1<class BaseClass0>)
		ldstr "G3_C1589::ClassMethod4566.16951()"
		ldstr "class G3_C1589`1<class BaseClass0> on type class G3_C1589`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1589`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1589`1<class BaseClass0>::Method7<object>()
		calli default string(class G3_C1589`1<class BaseClass0>)
		ldstr "G3_C1589::Method7.16950<System.Object>()"
		ldstr "class G3_C1589`1<class BaseClass0> on type class G3_C1589`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1589`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1589`1<class BaseClass0>::ClassMethod2566()
		calli default string(class G3_C1589`1<class BaseClass0>)
		ldstr "G2_C598::ClassMethod2566.10515()"
		ldstr "class G3_C1589`1<class BaseClass0> on type class G3_C1589`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1589`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1589`1<class BaseClass0>::Method6<object>()
		calli default string(class G3_C1589`1<class BaseClass0>)
		ldstr "G2_C598::Method6.10512<System.Object>()"
		ldstr "class G3_C1589`1<class BaseClass0> on type class G3_C1589`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1589`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1589`1<class BaseClass0>::Method5()
		calli default string(class G3_C1589`1<class BaseClass0>)
		ldstr "G2_C598::Method5.10510()"
		ldstr "class G3_C1589`1<class BaseClass0> on type class G3_C1589`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1589`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1589`1<class BaseClass0>::Method4()
		calli default string(class G3_C1589`1<class BaseClass0>)
		ldstr "G2_C598::Method4.10508()"
		ldstr "class G3_C1589`1<class BaseClass0> on type class G3_C1589`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1589`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1589`1<class BaseClass0>::ClassMethod1345<object>()
		calli default string(class G3_C1589`1<class BaseClass0>)
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G3_C1589`1<class BaseClass0> on type class G3_C1589`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1589`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1589`1<class BaseClass0>::ClassMethod1344<object>()
		calli default string(class G3_C1589`1<class BaseClass0>)
		ldstr "G2_C598::ClassMethod1344.MI.10516<System.Object>()"
		ldstr "class G3_C1589`1<class BaseClass0> on type class G3_C1589`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G3_C1589`1<class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C11`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass1,class BaseClass1>::ClassMethod1345<object>()
		calli default string(class G3_C1589`1<class BaseClass1>)
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass1> on type class G3_C1589`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C11`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass1,class BaseClass1>::ClassMethod1344<object>()
		calli default string(class G3_C1589`1<class BaseClass1>)
		ldstr "G2_C598::ClassMethod1344.MI.10516<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass1> on type class G3_C1589`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C11`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass1,class BaseClass1>::Method6<object>()
		calli default string(class G3_C1589`1<class BaseClass1>)
		ldstr "G1_C11::Method6.4859<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass1> on type class G3_C1589`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C11`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass1,class BaseClass1>::Method5()
		calli default string(class G3_C1589`1<class BaseClass1>)
		ldstr "G2_C598::Method5.10510()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass1> on type class G3_C1589`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C11`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass1,class BaseClass1>::Method4()
		calli default string(class G3_C1589`1<class BaseClass1>)
		ldstr "G1_C11::Method4.4856()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass1> on type class G3_C1589`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C11`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1589`1<class BaseClass1>)
		ldstr "G2_C598::Method7.10513<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass1> on type class G3_C1589`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1589`1<class BaseClass1>)
		ldstr "G3_C1589::Method7.16950<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G3_C1589`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G3_C1589`1<class BaseClass1>)
		ldstr "G2_C598::Method4.MI.10509()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1589`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G3_C1589`1<class BaseClass1>)
		ldstr "G2_C598::Method5.MI.10511()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1589`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G3_C1589`1<class BaseClass1>)
		ldstr "G2_C598::Method6.10512<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1589`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1589`1<class BaseClass1>)
		ldstr "G3_C1589::Method7.16950<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1589`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C598`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C598`2<class BaseClass1,class BaseClass0>::ClassMethod2566()
		calli default string(class G3_C1589`1<class BaseClass1>)
		ldstr "G2_C598::ClassMethod2566.10515()"
		ldstr "class G2_C598`2<class BaseClass1,class BaseClass0> on type class G3_C1589`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C598`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C598`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1589`1<class BaseClass1>)
		ldstr "G2_C598::Method7.10513<System.Object>()"
		ldstr "class G2_C598`2<class BaseClass1,class BaseClass0> on type class G3_C1589`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C598`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C598`2<class BaseClass1,class BaseClass0>::Method6<object>()
		calli default string(class G3_C1589`1<class BaseClass1>)
		ldstr "G2_C598::Method6.10512<System.Object>()"
		ldstr "class G2_C598`2<class BaseClass1,class BaseClass0> on type class G3_C1589`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C598`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C598`2<class BaseClass1,class BaseClass0>::Method5()
		calli default string(class G3_C1589`1<class BaseClass1>)
		ldstr "G2_C598::Method5.10510()"
		ldstr "class G2_C598`2<class BaseClass1,class BaseClass0> on type class G3_C1589`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C598`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C598`2<class BaseClass1,class BaseClass0>::Method4()
		calli default string(class G3_C1589`1<class BaseClass1>)
		ldstr "G2_C598::Method4.10508()"
		ldstr "class G2_C598`2<class BaseClass1,class BaseClass0> on type class G3_C1589`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C598`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C598`2<class BaseClass1,class BaseClass0>::ClassMethod1345<object>()
		calli default string(class G3_C1589`1<class BaseClass1>)
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G2_C598`2<class BaseClass1,class BaseClass0> on type class G3_C1589`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C598`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C598`2<class BaseClass1,class BaseClass0>::ClassMethod1344<object>()
		calli default string(class G3_C1589`1<class BaseClass1>)
		ldstr "G2_C598::ClassMethod1344.MI.10516<System.Object>()"
		ldstr "class G2_C598`2<class BaseClass1,class BaseClass0> on type class G3_C1589`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method4()
		calli default string(class G3_C1589`1<class BaseClass1>)
		ldstr "G2_C598::Method4.MI.10509()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1589`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method5()
		calli default string(class G3_C1589`1<class BaseClass1>)
		ldstr "G2_C598::Method5.MI.10511()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1589`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method6<object>()
		calli default string(class G3_C1589`1<class BaseClass1>)
		ldstr "G2_C598::Method6.10512<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1589`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1589`1<class BaseClass1>)
		ldstr "G3_C1589::Method7.16950<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G3_C1589`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1589`1<class BaseClass1>)
		ldstr "G3_C1589::Method7.16950<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G3_C1589`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1589`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1589`1<class BaseClass1>::ClassMethod4567<object>()
		calli default string(class G3_C1589`1<class BaseClass1>)
		ldstr "G3_C1589::ClassMethod4567.16952<System.Object>()"
		ldstr "class G3_C1589`1<class BaseClass1> on type class G3_C1589`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1589`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1589`1<class BaseClass1>::ClassMethod4566()
		calli default string(class G3_C1589`1<class BaseClass1>)
		ldstr "G3_C1589::ClassMethod4566.16951()"
		ldstr "class G3_C1589`1<class BaseClass1> on type class G3_C1589`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1589`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1589`1<class BaseClass1>::Method7<object>()
		calli default string(class G3_C1589`1<class BaseClass1>)
		ldstr "G3_C1589::Method7.16950<System.Object>()"
		ldstr "class G3_C1589`1<class BaseClass1> on type class G3_C1589`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1589`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1589`1<class BaseClass1>::ClassMethod2566()
		calli default string(class G3_C1589`1<class BaseClass1>)
		ldstr "G2_C598::ClassMethod2566.10515()"
		ldstr "class G3_C1589`1<class BaseClass1> on type class G3_C1589`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1589`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1589`1<class BaseClass1>::Method6<object>()
		calli default string(class G3_C1589`1<class BaseClass1>)
		ldstr "G2_C598::Method6.10512<System.Object>()"
		ldstr "class G3_C1589`1<class BaseClass1> on type class G3_C1589`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1589`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1589`1<class BaseClass1>::Method5()
		calli default string(class G3_C1589`1<class BaseClass1>)
		ldstr "G2_C598::Method5.10510()"
		ldstr "class G3_C1589`1<class BaseClass1> on type class G3_C1589`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1589`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1589`1<class BaseClass1>::Method4()
		calli default string(class G3_C1589`1<class BaseClass1>)
		ldstr "G2_C598::Method4.10508()"
		ldstr "class G3_C1589`1<class BaseClass1> on type class G3_C1589`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1589`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1589`1<class BaseClass1>::ClassMethod1345<object>()
		calli default string(class G3_C1589`1<class BaseClass1>)
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G3_C1589`1<class BaseClass1> on type class G3_C1589`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1589`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1589`1<class BaseClass1>::ClassMethod1344<object>()
		calli default string(class G3_C1589`1<class BaseClass1>)
		ldstr "G2_C598::ClassMethod1344.MI.10516<System.Object>()"
		ldstr "class G3_C1589`1<class BaseClass1> on type class G3_C1589`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G2_C598`2<class BaseClass0,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C11`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass0,class BaseClass1>::ClassMethod1345<object>()
		calli default string(class G2_C598`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass1> on type class G2_C598`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C11`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass0,class BaseClass1>::ClassMethod1344<object>()
		calli default string(class G2_C598`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C598::ClassMethod1344.MI.10516<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass1> on type class G2_C598`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C11`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass0,class BaseClass1>::Method6<object>()
		calli default string(class G2_C598`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C11::Method6.4859<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass1> on type class G2_C598`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C11`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass0,class BaseClass1>::Method5()
		calli default string(class G2_C598`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C598::Method5.10510()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass1> on type class G2_C598`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C11`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass0,class BaseClass1>::Method4()
		calli default string(class G2_C598`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C11::Method4.4856()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass1> on type class G2_C598`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C11`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C598`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C598::Method7.10513<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass1> on type class G2_C598`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C598`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C598`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G2_C598`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C598::Method4.MI.10509()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C598`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G2_C598`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C598::Method5.MI.10511()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C598`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G2_C598`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C598::Method6.10512<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C598`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C598`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C598::Method7.MI.10514<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C598`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C598`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C598`2<class BaseClass0,class BaseClass0>::ClassMethod2566()
		calli default string(class G2_C598`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C598::ClassMethod2566.10515()"
		ldstr "class G2_C598`2<class BaseClass0,class BaseClass0> on type class G2_C598`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C598`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C598`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G2_C598`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C598::Method7.10513<System.Object>()"
		ldstr "class G2_C598`2<class BaseClass0,class BaseClass0> on type class G2_C598`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C598`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C598`2<class BaseClass0,class BaseClass0>::Method6<object>()
		calli default string(class G2_C598`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C598::Method6.10512<System.Object>()"
		ldstr "class G2_C598`2<class BaseClass0,class BaseClass0> on type class G2_C598`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C598`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C598`2<class BaseClass0,class BaseClass0>::Method5()
		calli default string(class G2_C598`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C598::Method5.10510()"
		ldstr "class G2_C598`2<class BaseClass0,class BaseClass0> on type class G2_C598`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C598`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C598`2<class BaseClass0,class BaseClass0>::Method4()
		calli default string(class G2_C598`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C598::Method4.10508()"
		ldstr "class G2_C598`2<class BaseClass0,class BaseClass0> on type class G2_C598`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C598`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C598`2<class BaseClass0,class BaseClass0>::ClassMethod1345<object>()
		calli default string(class G2_C598`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G2_C598`2<class BaseClass0,class BaseClass0> on type class G2_C598`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C598`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C598`2<class BaseClass0,class BaseClass0>::ClassMethod1344<object>()
		calli default string(class G2_C598`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C598::ClassMethod1344.MI.10516<System.Object>()"
		ldstr "class G2_C598`2<class BaseClass0,class BaseClass0> on type class G2_C598`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method4()
		calli default string(class G2_C598`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C598::Method4.MI.10509()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C598`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method5()
		calli default string(class G2_C598`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C598::Method5.MI.10511()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C598`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method6<object>()
		calli default string(class G2_C598`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C598::Method6.10512<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C598`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G2_C598`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C598::Method7.MI.10514<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G2_C598`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G2_C598`2<class BaseClass0,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C11`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass0,class BaseClass1>::ClassMethod1345<object>()
		calli default string(class G2_C598`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass1> on type class G2_C598`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C11`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass0,class BaseClass1>::ClassMethod1344<object>()
		calli default string(class G2_C598`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C598::ClassMethod1344.MI.10516<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass1> on type class G2_C598`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C11`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass0,class BaseClass1>::Method6<object>()
		calli default string(class G2_C598`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C11::Method6.4859<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass1> on type class G2_C598`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C11`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass0,class BaseClass1>::Method5()
		calli default string(class G2_C598`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C598::Method5.10510()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass1> on type class G2_C598`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C11`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass0,class BaseClass1>::Method4()
		calli default string(class G2_C598`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C11::Method4.4856()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass1> on type class G2_C598`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C11`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C598`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C598::Method7.10513<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass1> on type class G2_C598`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C598`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C598`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G2_C598`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C598::Method4.MI.10509()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C598`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G2_C598`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C598::Method5.MI.10511()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C598`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G2_C598`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C598::Method6.10512<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C598`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C598`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C598::Method7.MI.10514<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C598`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C598`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C598`2<class BaseClass0,class BaseClass1>::ClassMethod2566()
		calli default string(class G2_C598`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C598::ClassMethod2566.10515()"
		ldstr "class G2_C598`2<class BaseClass0,class BaseClass1> on type class G2_C598`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C598`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C598`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C598`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C598::Method7.10513<System.Object>()"
		ldstr "class G2_C598`2<class BaseClass0,class BaseClass1> on type class G2_C598`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C598`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C598`2<class BaseClass0,class BaseClass1>::Method6<object>()
		calli default string(class G2_C598`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C598::Method6.10512<System.Object>()"
		ldstr "class G2_C598`2<class BaseClass0,class BaseClass1> on type class G2_C598`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C598`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C598`2<class BaseClass0,class BaseClass1>::Method5()
		calli default string(class G2_C598`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C598::Method5.10510()"
		ldstr "class G2_C598`2<class BaseClass0,class BaseClass1> on type class G2_C598`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C598`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C598`2<class BaseClass0,class BaseClass1>::Method4()
		calli default string(class G2_C598`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C598::Method4.10508()"
		ldstr "class G2_C598`2<class BaseClass0,class BaseClass1> on type class G2_C598`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C598`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C598`2<class BaseClass0,class BaseClass1>::ClassMethod1345<object>()
		calli default string(class G2_C598`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G2_C598`2<class BaseClass0,class BaseClass1> on type class G2_C598`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C598`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C598`2<class BaseClass0,class BaseClass1>::ClassMethod1344<object>()
		calli default string(class G2_C598`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C598::ClassMethod1344.MI.10516<System.Object>()"
		ldstr "class G2_C598`2<class BaseClass0,class BaseClass1> on type class G2_C598`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method4()
		calli default string(class G2_C598`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C598::Method4.MI.10509()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C598`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method5()
		calli default string(class G2_C598`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C598::Method5.MI.10511()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C598`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method6<object>()
		calli default string(class G2_C598`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C598::Method6.10512<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C598`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G2_C598`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C598::Method7.MI.10514<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G2_C598`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G2_C598`2<class BaseClass1,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C11`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass1,class BaseClass1>::ClassMethod1345<object>()
		calli default string(class G2_C598`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass1> on type class G2_C598`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C11`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass1,class BaseClass1>::ClassMethod1344<object>()
		calli default string(class G2_C598`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C598::ClassMethod1344.MI.10516<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass1> on type class G2_C598`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C11`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass1,class BaseClass1>::Method6<object>()
		calli default string(class G2_C598`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C11::Method6.4859<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass1> on type class G2_C598`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C11`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass1,class BaseClass1>::Method5()
		calli default string(class G2_C598`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C598::Method5.10510()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass1> on type class G2_C598`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C11`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass1,class BaseClass1>::Method4()
		calli default string(class G2_C598`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C11::Method4.4856()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass1> on type class G2_C598`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C11`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C598`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C598::Method7.10513<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass1> on type class G2_C598`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C598`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C598::Method7.MI.10514<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C598`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G2_C598`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C598::Method4.MI.10509()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C598`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G2_C598`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C598::Method5.MI.10511()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C598`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G2_C598`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C598::Method6.10512<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C598`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C598`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C598::Method7.MI.10514<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C598`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C598`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C598`2<class BaseClass1,class BaseClass0>::ClassMethod2566()
		calli default string(class G2_C598`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C598::ClassMethod2566.10515()"
		ldstr "class G2_C598`2<class BaseClass1,class BaseClass0> on type class G2_C598`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C598`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C598`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G2_C598`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C598::Method7.10513<System.Object>()"
		ldstr "class G2_C598`2<class BaseClass1,class BaseClass0> on type class G2_C598`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C598`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C598`2<class BaseClass1,class BaseClass0>::Method6<object>()
		calli default string(class G2_C598`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C598::Method6.10512<System.Object>()"
		ldstr "class G2_C598`2<class BaseClass1,class BaseClass0> on type class G2_C598`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C598`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C598`2<class BaseClass1,class BaseClass0>::Method5()
		calli default string(class G2_C598`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C598::Method5.10510()"
		ldstr "class G2_C598`2<class BaseClass1,class BaseClass0> on type class G2_C598`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C598`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C598`2<class BaseClass1,class BaseClass0>::Method4()
		calli default string(class G2_C598`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C598::Method4.10508()"
		ldstr "class G2_C598`2<class BaseClass1,class BaseClass0> on type class G2_C598`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C598`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C598`2<class BaseClass1,class BaseClass0>::ClassMethod1345<object>()
		calli default string(class G2_C598`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G2_C598`2<class BaseClass1,class BaseClass0> on type class G2_C598`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C598`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C598`2<class BaseClass1,class BaseClass0>::ClassMethod1344<object>()
		calli default string(class G2_C598`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C598::ClassMethod1344.MI.10516<System.Object>()"
		ldstr "class G2_C598`2<class BaseClass1,class BaseClass0> on type class G2_C598`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method4()
		calli default string(class G2_C598`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C598::Method4.MI.10509()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C598`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method5()
		calli default string(class G2_C598`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C598::Method5.MI.10511()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C598`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method6<object>()
		calli default string(class G2_C598`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C598::Method6.10512<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C598`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G2_C598`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C598::Method7.MI.10514<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G2_C598`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G2_C598`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C598::Method7.MI.10514<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G2_C598`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G2_C598`2<class BaseClass1,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C11`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass1,class BaseClass1>::ClassMethod1345<object>()
		calli default string(class G2_C598`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass1> on type class G2_C598`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C11`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass1,class BaseClass1>::ClassMethod1344<object>()
		calli default string(class G2_C598`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C598::ClassMethod1344.MI.10516<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass1> on type class G2_C598`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C11`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass1,class BaseClass1>::Method6<object>()
		calli default string(class G2_C598`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C11::Method6.4859<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass1> on type class G2_C598`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C11`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass1,class BaseClass1>::Method5()
		calli default string(class G2_C598`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C598::Method5.10510()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass1> on type class G2_C598`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C11`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass1,class BaseClass1>::Method4()
		calli default string(class G2_C598`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C11::Method4.4856()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass1> on type class G2_C598`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C11`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C598`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C598::Method7.10513<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass1> on type class G2_C598`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C598`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C598::Method7.MI.10514<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C598`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G2_C598`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C598::Method4.MI.10509()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C598`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G2_C598`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C598::Method5.MI.10511()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C598`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G2_C598`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C598::Method6.10512<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C598`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C598`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C598::Method7.MI.10514<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C598`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C598`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C598`2<class BaseClass1,class BaseClass1>::ClassMethod2566()
		calli default string(class G2_C598`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C598::ClassMethod2566.10515()"
		ldstr "class G2_C598`2<class BaseClass1,class BaseClass1> on type class G2_C598`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C598`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C598`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C598`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C598::Method7.10513<System.Object>()"
		ldstr "class G2_C598`2<class BaseClass1,class BaseClass1> on type class G2_C598`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C598`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C598`2<class BaseClass1,class BaseClass1>::Method6<object>()
		calli default string(class G2_C598`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C598::Method6.10512<System.Object>()"
		ldstr "class G2_C598`2<class BaseClass1,class BaseClass1> on type class G2_C598`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C598`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C598`2<class BaseClass1,class BaseClass1>::Method5()
		calli default string(class G2_C598`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C598::Method5.10510()"
		ldstr "class G2_C598`2<class BaseClass1,class BaseClass1> on type class G2_C598`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C598`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C598`2<class BaseClass1,class BaseClass1>::Method4()
		calli default string(class G2_C598`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C598::Method4.10508()"
		ldstr "class G2_C598`2<class BaseClass1,class BaseClass1> on type class G2_C598`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C598`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C598`2<class BaseClass1,class BaseClass1>::ClassMethod1345<object>()
		calli default string(class G2_C598`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G2_C598`2<class BaseClass1,class BaseClass1> on type class G2_C598`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C598`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C598`2<class BaseClass1,class BaseClass1>::ClassMethod1344<object>()
		calli default string(class G2_C598`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C598::ClassMethod1344.MI.10516<System.Object>()"
		ldstr "class G2_C598`2<class BaseClass1,class BaseClass1> on type class G2_C598`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method4()
		calli default string(class G2_C598`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C598::Method4.MI.10509()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C598`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method5()
		calli default string(class G2_C598`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C598::Method5.MI.10511()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C598`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method6<object>()
		calli default string(class G2_C598`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C598::Method6.10512<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C598`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G2_C598`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C598::Method7.MI.10514<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G2_C598`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G2_C598`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C598::Method7.MI.10514<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G2_C598`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G1_C11`2<class BaseClass0,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C11`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass0,class BaseClass0>::ClassMethod1345<object>()
		calli default string(class G1_C11`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass0> on type class G1_C11`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C11`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass0,class BaseClass0>::ClassMethod1344<object>()
		calli default string(class G1_C11`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass0> on type class G1_C11`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C11`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass0,class BaseClass0>::Method6<object>()
		calli default string(class G1_C11`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C11::Method6.4859<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass0> on type class G1_C11`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C11`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass0,class BaseClass0>::Method5()
		calli default string(class G1_C11`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C11::Method5.4858()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass0> on type class G1_C11`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C11`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass0,class BaseClass0>::Method4()
		calli default string(class G1_C11`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C11::Method4.4856()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass0> on type class G1_C11`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C11`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G1_C11`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C11::Method7.4854<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass0> on type class G1_C11`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G1_C11`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G1_C11`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G1_C11`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C11::Method4.MI.4857()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C11`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G1_C11`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C11::Method5.4858()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C11`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G1_C11`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C11::Method6.MI.4860<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C11`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G1_C11`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G1_C11`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G1_C11`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G1_C11`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G1_C11`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G1_C11`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G1_C11`2<class BaseClass0,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C11`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass0,class BaseClass1>::ClassMethod1345<object>()
		calli default string(class G1_C11`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass1> on type class G1_C11`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C11`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass0,class BaseClass1>::ClassMethod1344<object>()
		calli default string(class G1_C11`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass1> on type class G1_C11`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C11`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass0,class BaseClass1>::Method6<object>()
		calli default string(class G1_C11`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C11::Method6.4859<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass1> on type class G1_C11`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C11`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass0,class BaseClass1>::Method5()
		calli default string(class G1_C11`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C11::Method5.4858()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass1> on type class G1_C11`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C11`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass0,class BaseClass1>::Method4()
		calli default string(class G1_C11`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C11::Method4.4856()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass1> on type class G1_C11`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C11`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G1_C11`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C11::Method7.4854<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass1> on type class G1_C11`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G1_C11`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G1_C11`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G1_C11`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C11::Method4.MI.4857()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C11`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G1_C11`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C11::Method5.4858()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C11`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G1_C11`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C11::Method6.MI.4860<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C11`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G1_C11`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G1_C11`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G1_C11`2<class BaseClass1,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C11`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass1,class BaseClass0>::ClassMethod1345<object>()
		calli default string(class G1_C11`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass0> on type class G1_C11`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C11`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass1,class BaseClass0>::ClassMethod1344<object>()
		calli default string(class G1_C11`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass0> on type class G1_C11`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C11`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass1,class BaseClass0>::Method6<object>()
		calli default string(class G1_C11`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C11::Method6.4859<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass0> on type class G1_C11`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C11`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass1,class BaseClass0>::Method5()
		calli default string(class G1_C11`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C11::Method5.4858()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass0> on type class G1_C11`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C11`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass1,class BaseClass0>::Method4()
		calli default string(class G1_C11`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C11::Method4.4856()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass0> on type class G1_C11`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C11`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G1_C11`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C11::Method7.4854<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass0> on type class G1_C11`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G1_C11`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G1_C11`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G1_C11`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C11::Method4.MI.4857()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C11`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G1_C11`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C11::Method5.4858()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C11`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G1_C11`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C11::Method6.MI.4860<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C11`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G1_C11`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G1_C11`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G1_C11`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G1_C11`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G1_C11`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G1_C11`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G1_C11`2<class BaseClass1,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C11`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass1,class BaseClass1>::ClassMethod1345<object>()
		calli default string(class G1_C11`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass1> on type class G1_C11`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C11`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass1,class BaseClass1>::ClassMethod1344<object>()
		calli default string(class G1_C11`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass1> on type class G1_C11`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C11`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass1,class BaseClass1>::Method6<object>()
		calli default string(class G1_C11`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C11::Method6.4859<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass1> on type class G1_C11`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C11`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass1,class BaseClass1>::Method5()
		calli default string(class G1_C11`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C11::Method5.4858()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass1> on type class G1_C11`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C11`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass1,class BaseClass1>::Method4()
		calli default string(class G1_C11`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C11::Method4.4856()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass1> on type class G1_C11`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C11`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G1_C11`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C11::Method7.4854<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass1> on type class G1_C11`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G1_C11`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G1_C11`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G1_C11`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C11::Method4.MI.4857()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C11`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G1_C11`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C11::Method5.4858()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C11`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G1_C11`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C11::Method6.MI.4860<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C11`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G1_C11`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G1_C11`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldstr "========================================================================\n\n"
		call void [mscorlib]System.Console::WriteLine(string)
		ret
	}
	.method public hidebysig static int32  Main() cil managed
	{
		.entrypoint
		.maxstack  10
		call void Framework::MethodCallingTest()
		call void Framework::ConstrainedCallsTest()
		call void Framework::StructConstrainedInterfaceCallsTest()
		call void Framework::CalliTest()
		ldc.i4 100
		ret
	}
}
