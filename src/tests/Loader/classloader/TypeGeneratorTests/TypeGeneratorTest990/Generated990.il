// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) .ver 4:0:0:0 }
.assembly extern TestFramework { .publickeytoken = ( B0 3F 5F 7F 11 D5 0A 3A )  }

//TYPES IN FORWARDER ASSEMBLIES:

//TEST ASSEMBLY:
.assembly Generated990 { .hash algorithm 0x00008004 }
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
.class public G3_C1462`1<T0> 
		extends class G2_C488`2<!T0,class BaseClass1>
		implements class IBase2`2<!T0,!T0> 
{
	.method public hidebysig virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G3_C1462::Method7.16292<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase2<T0,T0>.Method7'<M0>() cil managed noinlining { 
		.override method instance string class IBase2`2<!T0,!T0>::Method7<[1]>()
		ldstr "G3_C1462::Method7.MI.16293<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod4312() cil managed noinlining { 
		ldstr "G3_C1462::ClassMethod4312.16294()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod4313<M0>() cil managed noinlining { 
		ldstr "G3_C1462::ClassMethod4313.16295<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod4314<M0>() cil managed noinlining { 
		ldstr "G3_C1462::ClassMethod4314.16296<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'G2_C488<T0,class BaseClass1>.ClassMethod2329'() cil managed noinlining { 
		.override method instance string class G2_C488`2<!T0,class BaseClass1>::ClassMethod2329()
		ldstr "G3_C1462::ClassMethod2329.MI.16297()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'G2_C488<T0,class BaseClass1>.ClassMethod2330'() cil managed noinlining { 
		.override method instance string class G2_C488`2<!T0,class BaseClass1>::ClassMethod2330()
		ldstr "G3_C1462::ClassMethod2330.MI.16298()"
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class G2_C488`2<!T0,class BaseClass1>::.ctor()
		ret
	}
}
.class public abstract G2_C488`2<T0, T1> 
		extends class G1_C9`2<class BaseClass1,!T1>
		implements class IBase2`2<class BaseClass0,!T0>, IBase0 
{
	.method public hidebysig virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G2_C488::Method7.9435<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase2<class BaseClass0,T0>.Method7'<M0>() cil managed noinlining { 
		.override method instance string class IBase2`2<class BaseClass0,!T0>::Method7<[1]>()
		ldstr "G2_C488::Method7.MI.9436<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string Method0() cil managed noinlining { 
		ldstr "G2_C488::Method0.9437()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase0.Method0'() cil managed noinlining { 
		.override method instance string IBase0::Method0()
		ldstr "G2_C488::Method0.MI.9438()"
		ret
	}
	.method public hidebysig virtual instance string Method1() cil managed noinlining { 
		ldstr "G2_C488::Method1.9439()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase0.Method1'() cil managed noinlining { 
		.override method instance string IBase0::Method1()
		ldstr "G2_C488::Method1.MI.9440()"
		ret
	}
	.method public hidebysig virtual instance string Method2<M0>() cil managed noinlining { 
		ldstr "G2_C488::Method2.9441<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase0.Method2'<M0>() cil managed noinlining { 
		.override method instance string IBase0::Method2<[1]>()
		ldstr "G2_C488::Method2.MI.9442<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig virtual instance string Method3<M0>() cil managed noinlining { 
		ldstr "G2_C488::Method3.9443<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase0.Method3'<M0>() cil managed noinlining { 
		.override method instance string IBase0::Method3<[1]>()
		ldstr "G2_C488::Method3.MI.9444<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod2329() cil managed noinlining { 
		ldstr "G2_C488::ClassMethod2329.9445()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod2330() cil managed noinlining { 
		ldstr "G2_C488::ClassMethod2330.9446()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod2331<M0>() cil managed noinlining { 
		ldstr "G2_C488::ClassMethod2331.9447<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'G1_C9<class BaseClass1,T1>.ClassMethod1336'() cil managed noinlining { 
		.override method instance string class G1_C9`2<class BaseClass1,!T1>::ClassMethod1336()
		ldstr "G2_C488::ClassMethod1336.MI.9448()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'G1_C9<class BaseClass1,T1>.ClassMethod1337'() cil managed noinlining { 
		.override method instance string class G1_C9`2<class BaseClass1,!T1>::ClassMethod1337()
		ldstr "G2_C488::ClassMethod1337.MI.9449()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'G1_C9<class BaseClass1,T1>.ClassMethod1338'<M0>() cil managed noinlining { 
		.override method instance string class G1_C9`2<class BaseClass1,!T1>::ClassMethod1338<[1]>()
		ldstr "G2_C488::ClassMethod1338.MI.9450<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class G1_C9`2<class BaseClass1,!T1>::.ctor()
		ret
	}
}
.class interface public abstract IBase2`2<+T0, -T1> 
{
	.method public hidebysig newslot abstract virtual instance string Method7<M0>() cil managed { }
}
.class public abstract G1_C9`2<T0, T1> 
		implements class IBase2`2<class BaseClass0,!T0>, class IBase1`1<class BaseClass0> 
{
	.method public hidebysig virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G1_C9::Method7.4833<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig virtual instance string Method4() cil managed noinlining { 
		ldstr "G1_C9::Method4.4834()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<class BaseClass0>.Method4'() cil managed noinlining { 
		.override method instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G1_C9::Method4.MI.4835()"
		ret
	}
	.method public hidebysig newslot virtual instance string Method5() cil managed noinlining { 
		ldstr "G1_C9::Method5.4836()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<class BaseClass0>.Method5'() cil managed noinlining { 
		.override method instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G1_C9::Method5.MI.4837()"
		ret
	}
	.method public hidebysig virtual instance string Method6<M0>() cil managed noinlining { 
		ldstr "G1_C9::Method6.4838<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<class BaseClass0>.Method6'<M0>() cil managed noinlining { 
		.override method instance string class IBase1`1<class BaseClass0>::Method6<[1]>()
		ldstr "G1_C9::Method6.MI.4839<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1336() cil managed noinlining { 
		ldstr "G1_C9::ClassMethod1336.4840()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1337() cil managed noinlining { 
		ldstr "G1_C9::ClassMethod1337.4841()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1338<M0>() cil managed noinlining { 
		ldstr "G1_C9::ClassMethod1338.4842<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1339<M0>() cil managed noinlining { 
		ldstr "G1_C9::ClassMethod1339.4843<"
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
.class interface public abstract IBase1`1<+T0> 
{
	.method public hidebysig newslot abstract virtual instance string Method4() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method5() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method6<M0>() cil managed { }
}
.class public auto ansi beforefieldinit Generated990 {
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
	.method static void M.G3_C1462.T<T0,(class G3_C1462`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 23
		.locals init (string[] actualResults)
		ldc.i4.s    18
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1462.T<T0,(class G3_C1462`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 18
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1462`1<!!T0>::ClassMethod1336()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1462`1<!!T0>::ClassMethod1337()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1462`1<!!T0>::ClassMethod1338<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1462`1<!!T0>::ClassMethod1339<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1462`1<!!T0>::ClassMethod2329()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1462`1<!!T0>::ClassMethod2330()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1462`1<!!T0>::ClassMethod2331<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1462`1<!!T0>::ClassMethod4312()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1462`1<!!T0>::ClassMethod4313<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1462`1<!!T0>::ClassMethod4314<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1462`1<!!T0>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1462`1<!!T0>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1462`1<!!T0>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     13
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1462`1<!!T0>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     14
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1462`1<!!T0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     15
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1462`1<!!T0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     16
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1462`1<!!T0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     17
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1462`1<!!T0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1462.A<(class G3_C1462`1<class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 23
		.locals init (string[] actualResults)
		ldc.i4.s    18
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1462.A<(class G3_C1462`1<class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 18
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1462`1<class BaseClass0>::ClassMethod1336()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1462`1<class BaseClass0>::ClassMethod1337()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1462`1<class BaseClass0>::ClassMethod1338<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1462`1<class BaseClass0>::ClassMethod1339<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1462`1<class BaseClass0>::ClassMethod2329()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1462`1<class BaseClass0>::ClassMethod2330()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1462`1<class BaseClass0>::ClassMethod2331<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1462`1<class BaseClass0>::ClassMethod4312()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1462`1<class BaseClass0>::ClassMethod4313<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1462`1<class BaseClass0>::ClassMethod4314<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1462`1<class BaseClass0>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1462`1<class BaseClass0>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1462`1<class BaseClass0>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     13
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1462`1<class BaseClass0>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     14
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1462`1<class BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     15
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1462`1<class BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     16
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1462`1<class BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     17
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1462`1<class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1462.B<(class G3_C1462`1<class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 23
		.locals init (string[] actualResults)
		ldc.i4.s    18
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1462.B<(class G3_C1462`1<class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 18
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1462`1<class BaseClass1>::ClassMethod1336()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1462`1<class BaseClass1>::ClassMethod1337()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1462`1<class BaseClass1>::ClassMethod1338<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1462`1<class BaseClass1>::ClassMethod1339<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1462`1<class BaseClass1>::ClassMethod2329()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1462`1<class BaseClass1>::ClassMethod2330()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1462`1<class BaseClass1>::ClassMethod2331<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1462`1<class BaseClass1>::ClassMethod4312()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1462`1<class BaseClass1>::ClassMethod4313<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1462`1<class BaseClass1>::ClassMethod4314<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1462`1<class BaseClass1>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1462`1<class BaseClass1>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1462`1<class BaseClass1>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     13
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1462`1<class BaseClass1>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     14
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1462`1<class BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     15
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1462`1<class BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     16
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1462`1<class BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     17
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1462`1<class BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C488.T.T<T0,T1,(class G2_C488`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 20
		.locals init (string[] actualResults)
		ldc.i4.s    15
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C488.T.T<T0,T1,(class G2_C488`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 15
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C488`2<!!T0,!!T1>::ClassMethod1336()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C488`2<!!T0,!!T1>::ClassMethod1337()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C488`2<!!T0,!!T1>::ClassMethod1338<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C488`2<!!T0,!!T1>::ClassMethod1339<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C488`2<!!T0,!!T1>::ClassMethod2329()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C488`2<!!T0,!!T1>::ClassMethod2330()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C488`2<!!T0,!!T1>::ClassMethod2331<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C488`2<!!T0,!!T1>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C488`2<!!T0,!!T1>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C488`2<!!T0,!!T1>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C488`2<!!T0,!!T1>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C488`2<!!T0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C488`2<!!T0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     13
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C488`2<!!T0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     14
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C488`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C488.A.T<T1,(class G2_C488`2<class BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 20
		.locals init (string[] actualResults)
		ldc.i4.s    15
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C488.A.T<T1,(class G2_C488`2<class BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 15
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C488`2<class BaseClass0,!!T1>::ClassMethod1336()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C488`2<class BaseClass0,!!T1>::ClassMethod1337()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C488`2<class BaseClass0,!!T1>::ClassMethod1338<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C488`2<class BaseClass0,!!T1>::ClassMethod1339<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C488`2<class BaseClass0,!!T1>::ClassMethod2329()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C488`2<class BaseClass0,!!T1>::ClassMethod2330()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C488`2<class BaseClass0,!!T1>::ClassMethod2331<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C488`2<class BaseClass0,!!T1>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C488`2<class BaseClass0,!!T1>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C488`2<class BaseClass0,!!T1>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C488`2<class BaseClass0,!!T1>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C488`2<class BaseClass0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C488`2<class BaseClass0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     13
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C488`2<class BaseClass0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     14
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C488`2<class BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C488.A.A<(class G2_C488`2<class BaseClass0,class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 20
		.locals init (string[] actualResults)
		ldc.i4.s    15
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C488.A.A<(class G2_C488`2<class BaseClass0,class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 15
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C488`2<class BaseClass0,class BaseClass0>::ClassMethod1336()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C488`2<class BaseClass0,class BaseClass0>::ClassMethod1337()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C488`2<class BaseClass0,class BaseClass0>::ClassMethod1338<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C488`2<class BaseClass0,class BaseClass0>::ClassMethod1339<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C488`2<class BaseClass0,class BaseClass0>::ClassMethod2329()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C488`2<class BaseClass0,class BaseClass0>::ClassMethod2330()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C488`2<class BaseClass0,class BaseClass0>::ClassMethod2331<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C488`2<class BaseClass0,class BaseClass0>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C488`2<class BaseClass0,class BaseClass0>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C488`2<class BaseClass0,class BaseClass0>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C488`2<class BaseClass0,class BaseClass0>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C488`2<class BaseClass0,class BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C488`2<class BaseClass0,class BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     13
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C488`2<class BaseClass0,class BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     14
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C488`2<class BaseClass0,class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C488.A.B<(class G2_C488`2<class BaseClass0,class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 20
		.locals init (string[] actualResults)
		ldc.i4.s    15
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C488.A.B<(class G2_C488`2<class BaseClass0,class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 15
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C488`2<class BaseClass0,class BaseClass1>::ClassMethod1336()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C488`2<class BaseClass0,class BaseClass1>::ClassMethod1337()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C488`2<class BaseClass0,class BaseClass1>::ClassMethod1338<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C488`2<class BaseClass0,class BaseClass1>::ClassMethod1339<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C488`2<class BaseClass0,class BaseClass1>::ClassMethod2329()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C488`2<class BaseClass0,class BaseClass1>::ClassMethod2330()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C488`2<class BaseClass0,class BaseClass1>::ClassMethod2331<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C488`2<class BaseClass0,class BaseClass1>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C488`2<class BaseClass0,class BaseClass1>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C488`2<class BaseClass0,class BaseClass1>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C488`2<class BaseClass0,class BaseClass1>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C488`2<class BaseClass0,class BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C488`2<class BaseClass0,class BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     13
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C488`2<class BaseClass0,class BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     14
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C488`2<class BaseClass0,class BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C488.B.T<T1,(class G2_C488`2<class BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 20
		.locals init (string[] actualResults)
		ldc.i4.s    15
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C488.B.T<T1,(class G2_C488`2<class BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 15
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C488`2<class BaseClass1,!!T1>::ClassMethod1336()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C488`2<class BaseClass1,!!T1>::ClassMethod1337()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C488`2<class BaseClass1,!!T1>::ClassMethod1338<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C488`2<class BaseClass1,!!T1>::ClassMethod1339<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C488`2<class BaseClass1,!!T1>::ClassMethod2329()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C488`2<class BaseClass1,!!T1>::ClassMethod2330()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C488`2<class BaseClass1,!!T1>::ClassMethod2331<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C488`2<class BaseClass1,!!T1>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C488`2<class BaseClass1,!!T1>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C488`2<class BaseClass1,!!T1>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C488`2<class BaseClass1,!!T1>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C488`2<class BaseClass1,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C488`2<class BaseClass1,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     13
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C488`2<class BaseClass1,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     14
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C488`2<class BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C488.B.A<(class G2_C488`2<class BaseClass1,class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 20
		.locals init (string[] actualResults)
		ldc.i4.s    15
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C488.B.A<(class G2_C488`2<class BaseClass1,class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 15
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C488`2<class BaseClass1,class BaseClass0>::ClassMethod1336()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C488`2<class BaseClass1,class BaseClass0>::ClassMethod1337()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C488`2<class BaseClass1,class BaseClass0>::ClassMethod1338<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C488`2<class BaseClass1,class BaseClass0>::ClassMethod1339<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C488`2<class BaseClass1,class BaseClass0>::ClassMethod2329()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C488`2<class BaseClass1,class BaseClass0>::ClassMethod2330()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C488`2<class BaseClass1,class BaseClass0>::ClassMethod2331<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C488`2<class BaseClass1,class BaseClass0>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C488`2<class BaseClass1,class BaseClass0>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C488`2<class BaseClass1,class BaseClass0>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C488`2<class BaseClass1,class BaseClass0>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C488`2<class BaseClass1,class BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C488`2<class BaseClass1,class BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     13
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C488`2<class BaseClass1,class BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     14
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C488`2<class BaseClass1,class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C488.B.B<(class G2_C488`2<class BaseClass1,class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 20
		.locals init (string[] actualResults)
		ldc.i4.s    15
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C488.B.B<(class G2_C488`2<class BaseClass1,class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 15
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C488`2<class BaseClass1,class BaseClass1>::ClassMethod1336()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C488`2<class BaseClass1,class BaseClass1>::ClassMethod1337()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C488`2<class BaseClass1,class BaseClass1>::ClassMethod1338<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C488`2<class BaseClass1,class BaseClass1>::ClassMethod1339<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C488`2<class BaseClass1,class BaseClass1>::ClassMethod2329()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C488`2<class BaseClass1,class BaseClass1>::ClassMethod2330()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C488`2<class BaseClass1,class BaseClass1>::ClassMethod2331<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C488`2<class BaseClass1,class BaseClass1>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C488`2<class BaseClass1,class BaseClass1>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C488`2<class BaseClass1,class BaseClass1>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C488`2<class BaseClass1,class BaseClass1>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C488`2<class BaseClass1,class BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C488`2<class BaseClass1,class BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     13
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C488`2<class BaseClass1,class BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     14
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C488`2<class BaseClass1,class BaseClass1>::Method7<object>()
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
	.method static void M.G1_C9.T.T<T0,T1,(class G1_C9`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C9.T.T<T0,T1,(class G1_C9`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C9`2<!!T0,!!T1>::ClassMethod1336()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C9`2<!!T0,!!T1>::ClassMethod1337()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C9`2<!!T0,!!T1>::ClassMethod1338<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C9`2<!!T0,!!T1>::ClassMethod1339<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C9`2<!!T0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C9`2<!!T0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C9`2<!!T0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C9`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C9.A.T<T1,(class G1_C9`2<class BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C9.A.T<T1,(class G1_C9`2<class BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C9`2<class BaseClass0,!!T1>::ClassMethod1336()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C9`2<class BaseClass0,!!T1>::ClassMethod1337()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C9`2<class BaseClass0,!!T1>::ClassMethod1338<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C9`2<class BaseClass0,!!T1>::ClassMethod1339<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C9`2<class BaseClass0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C9`2<class BaseClass0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C9`2<class BaseClass0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C9`2<class BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C9.A.A<(class G1_C9`2<class BaseClass0,class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C9.A.A<(class G1_C9`2<class BaseClass0,class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C9`2<class BaseClass0,class BaseClass0>::ClassMethod1336()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C9`2<class BaseClass0,class BaseClass0>::ClassMethod1337()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C9`2<class BaseClass0,class BaseClass0>::ClassMethod1338<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C9`2<class BaseClass0,class BaseClass0>::ClassMethod1339<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C9`2<class BaseClass0,class BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C9`2<class BaseClass0,class BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C9`2<class BaseClass0,class BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C9`2<class BaseClass0,class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C9.A.B<(class G1_C9`2<class BaseClass0,class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C9.A.B<(class G1_C9`2<class BaseClass0,class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C9`2<class BaseClass0,class BaseClass1>::ClassMethod1336()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C9`2<class BaseClass0,class BaseClass1>::ClassMethod1337()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C9`2<class BaseClass0,class BaseClass1>::ClassMethod1338<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C9`2<class BaseClass0,class BaseClass1>::ClassMethod1339<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C9`2<class BaseClass0,class BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C9`2<class BaseClass0,class BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C9`2<class BaseClass0,class BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C9`2<class BaseClass0,class BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C9.B.T<T1,(class G1_C9`2<class BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C9.B.T<T1,(class G1_C9`2<class BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C9`2<class BaseClass1,!!T1>::ClassMethod1336()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C9`2<class BaseClass1,!!T1>::ClassMethod1337()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C9`2<class BaseClass1,!!T1>::ClassMethod1338<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C9`2<class BaseClass1,!!T1>::ClassMethod1339<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C9`2<class BaseClass1,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C9`2<class BaseClass1,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C9`2<class BaseClass1,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C9`2<class BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C9.B.A<(class G1_C9`2<class BaseClass1,class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C9.B.A<(class G1_C9`2<class BaseClass1,class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C9`2<class BaseClass1,class BaseClass0>::ClassMethod1336()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C9`2<class BaseClass1,class BaseClass0>::ClassMethod1337()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C9`2<class BaseClass1,class BaseClass0>::ClassMethod1338<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C9`2<class BaseClass1,class BaseClass0>::ClassMethod1339<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C9`2<class BaseClass1,class BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C9`2<class BaseClass1,class BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C9`2<class BaseClass1,class BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C9`2<class BaseClass1,class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C9.B.B<(class G1_C9`2<class BaseClass1,class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C9.B.B<(class G1_C9`2<class BaseClass1,class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C9`2<class BaseClass1,class BaseClass1>::ClassMethod1336()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C9`2<class BaseClass1,class BaseClass1>::ClassMethod1337()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C9`2<class BaseClass1,class BaseClass1>::ClassMethod1338<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C9`2<class BaseClass1,class BaseClass1>::ClassMethod1339<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C9`2<class BaseClass1,class BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C9`2<class BaseClass1,class BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C9`2<class BaseClass1,class BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C9`2<class BaseClass1,class BaseClass1>::Method7<object>()
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
		newobj instance void class G3_C1462`1<class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C9`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C9`2<class BaseClass1,class BaseClass1>::ClassMethod1339<object>()
		ldstr "G1_C9::ClassMethod1339.4843<System.Object>()"
		ldstr "class G1_C9`2<class BaseClass1,class BaseClass1> on type class G3_C1462`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C9`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C9`2<class BaseClass1,class BaseClass1>::ClassMethod1338<object>()
		ldstr "G2_C488::ClassMethod1338.MI.9450<System.Object>()"
		ldstr "class G1_C9`2<class BaseClass1,class BaseClass1> on type class G3_C1462`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C9`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C9`2<class BaseClass1,class BaseClass1>::ClassMethod1337()
		ldstr "G2_C488::ClassMethod1337.MI.9449()"
		ldstr "class G1_C9`2<class BaseClass1,class BaseClass1> on type class G3_C1462`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C9`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C9`2<class BaseClass1,class BaseClass1>::ClassMethod1336()
		ldstr "G2_C488::ClassMethod1336.MI.9448()"
		ldstr "class G1_C9`2<class BaseClass1,class BaseClass1> on type class G3_C1462`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C9`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C9`2<class BaseClass1,class BaseClass1>::Method6<object>()
		ldstr "G1_C9::Method6.4838<System.Object>()"
		ldstr "class G1_C9`2<class BaseClass1,class BaseClass1> on type class G3_C1462`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C9`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C9`2<class BaseClass1,class BaseClass1>::Method5()
		ldstr "G1_C9::Method5.4836()"
		ldstr "class G1_C9`2<class BaseClass1,class BaseClass1> on type class G3_C1462`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C9`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C9`2<class BaseClass1,class BaseClass1>::Method4()
		ldstr "G1_C9::Method4.4834()"
		ldstr "class G1_C9`2<class BaseClass1,class BaseClass1> on type class G3_C1462`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C9`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C9`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G3_C1462::Method7.16292<System.Object>()"
		ldstr "class G1_C9`2<class BaseClass1,class BaseClass1> on type class G3_C1462`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G3_C1462::Method7.MI.16293<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1462`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G1_C9::Method4.MI.4835()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1462`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G1_C9::Method5.MI.4837()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1462`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G1_C9::Method6.MI.4839<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1462`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C488`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C488`2<class BaseClass0,class BaseClass1>::ClassMethod2331<object>()
		ldstr "G2_C488::ClassMethod2331.9447<System.Object>()"
		ldstr "class G2_C488`2<class BaseClass0,class BaseClass1> on type class G3_C1462`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C488`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C488`2<class BaseClass0,class BaseClass1>::ClassMethod2330()
		ldstr "G3_C1462::ClassMethod2330.MI.16298()"
		ldstr "class G2_C488`2<class BaseClass0,class BaseClass1> on type class G3_C1462`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C488`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C488`2<class BaseClass0,class BaseClass1>::ClassMethod2329()
		ldstr "G3_C1462::ClassMethod2329.MI.16297()"
		ldstr "class G2_C488`2<class BaseClass0,class BaseClass1> on type class G3_C1462`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C488`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C488`2<class BaseClass0,class BaseClass1>::Method3<object>()
		ldstr "G2_C488::Method3.9443<System.Object>()"
		ldstr "class G2_C488`2<class BaseClass0,class BaseClass1> on type class G3_C1462`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C488`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C488`2<class BaseClass0,class BaseClass1>::Method2<object>()
		ldstr "G2_C488::Method2.9441<System.Object>()"
		ldstr "class G2_C488`2<class BaseClass0,class BaseClass1> on type class G3_C1462`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C488`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C488`2<class BaseClass0,class BaseClass1>::Method1()
		ldstr "G2_C488::Method1.9439()"
		ldstr "class G2_C488`2<class BaseClass0,class BaseClass1> on type class G3_C1462`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C488`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C488`2<class BaseClass0,class BaseClass1>::Method0()
		ldstr "G2_C488::Method0.9437()"
		ldstr "class G2_C488`2<class BaseClass0,class BaseClass1> on type class G3_C1462`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C488`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C488`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G3_C1462::Method7.16292<System.Object>()"
		ldstr "class G2_C488`2<class BaseClass0,class BaseClass1> on type class G3_C1462`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C488`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C488`2<class BaseClass0,class BaseClass1>::ClassMethod1339<object>()
		ldstr "G1_C9::ClassMethod1339.4843<System.Object>()"
		ldstr "class G2_C488`2<class BaseClass0,class BaseClass1> on type class G3_C1462`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C488`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C488`2<class BaseClass0,class BaseClass1>::ClassMethod1338<object>()
		ldstr "G2_C488::ClassMethod1338.MI.9450<System.Object>()"
		ldstr "class G2_C488`2<class BaseClass0,class BaseClass1> on type class G3_C1462`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C488`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C488`2<class BaseClass0,class BaseClass1>::ClassMethod1337()
		ldstr "G2_C488::ClassMethod1337.MI.9449()"
		ldstr "class G2_C488`2<class BaseClass0,class BaseClass1> on type class G3_C1462`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C488`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C488`2<class BaseClass0,class BaseClass1>::ClassMethod1336()
		ldstr "G2_C488::ClassMethod1336.MI.9448()"
		ldstr "class G2_C488`2<class BaseClass0,class BaseClass1> on type class G3_C1462`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C488`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C488`2<class BaseClass0,class BaseClass1>::Method6<object>()
		ldstr "G1_C9::Method6.4838<System.Object>()"
		ldstr "class G2_C488`2<class BaseClass0,class BaseClass1> on type class G3_C1462`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C488`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C488`2<class BaseClass0,class BaseClass1>::Method5()
		ldstr "G1_C9::Method5.4836()"
		ldstr "class G2_C488`2<class BaseClass0,class BaseClass1> on type class G3_C1462`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C488`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C488`2<class BaseClass0,class BaseClass1>::Method4()
		ldstr "G1_C9::Method4.4834()"
		ldstr "class G2_C488`2<class BaseClass0,class BaseClass1> on type class G3_C1462`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G3_C1462::Method7.MI.16293<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G3_C1462`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string IBase0::Method0()
		ldstr "G2_C488::Method0.MI.9438()"
		ldstr "IBase0 on type class G3_C1462`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method1()
		ldstr "G2_C488::Method1.MI.9440()"
		ldstr "IBase0 on type class G3_C1462`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method2<object>()
		ldstr "G2_C488::Method2.MI.9442<System.Object>()"
		ldstr "IBase0 on type class G3_C1462`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method3<object>()
		ldstr "G2_C488::Method3.MI.9444<System.Object>()"
		ldstr "IBase0 on type class G3_C1462`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G3_C1462`1<class BaseClass0>
		callvirt instance string class G3_C1462`1<class BaseClass0>::ClassMethod4314<object>()
		ldstr "G3_C1462::ClassMethod4314.16296<System.Object>()"
		ldstr "class G3_C1462`1<class BaseClass0> on type class G3_C1462`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1462`1<class BaseClass0>
		callvirt instance string class G3_C1462`1<class BaseClass0>::ClassMethod4313<object>()
		ldstr "G3_C1462::ClassMethod4313.16295<System.Object>()"
		ldstr "class G3_C1462`1<class BaseClass0> on type class G3_C1462`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1462`1<class BaseClass0>
		callvirt instance string class G3_C1462`1<class BaseClass0>::ClassMethod4312()
		ldstr "G3_C1462::ClassMethod4312.16294()"
		ldstr "class G3_C1462`1<class BaseClass0> on type class G3_C1462`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1462`1<class BaseClass0>
		callvirt instance string class G3_C1462`1<class BaseClass0>::Method7<object>()
		ldstr "G3_C1462::Method7.16292<System.Object>()"
		ldstr "class G3_C1462`1<class BaseClass0> on type class G3_C1462`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1462`1<class BaseClass0>
		callvirt instance string class G3_C1462`1<class BaseClass0>::ClassMethod2331<object>()
		ldstr "G2_C488::ClassMethod2331.9447<System.Object>()"
		ldstr "class G3_C1462`1<class BaseClass0> on type class G3_C1462`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1462`1<class BaseClass0>
		callvirt instance string class G3_C1462`1<class BaseClass0>::ClassMethod2330()
		ldstr "G3_C1462::ClassMethod2330.MI.16298()"
		ldstr "class G3_C1462`1<class BaseClass0> on type class G3_C1462`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1462`1<class BaseClass0>
		callvirt instance string class G3_C1462`1<class BaseClass0>::ClassMethod2329()
		ldstr "G3_C1462::ClassMethod2329.MI.16297()"
		ldstr "class G3_C1462`1<class BaseClass0> on type class G3_C1462`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1462`1<class BaseClass0>
		callvirt instance string class G3_C1462`1<class BaseClass0>::Method3<object>()
		ldstr "G2_C488::Method3.9443<System.Object>()"
		ldstr "class G3_C1462`1<class BaseClass0> on type class G3_C1462`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1462`1<class BaseClass0>
		callvirt instance string class G3_C1462`1<class BaseClass0>::Method2<object>()
		ldstr "G2_C488::Method2.9441<System.Object>()"
		ldstr "class G3_C1462`1<class BaseClass0> on type class G3_C1462`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1462`1<class BaseClass0>
		callvirt instance string class G3_C1462`1<class BaseClass0>::Method1()
		ldstr "G2_C488::Method1.9439()"
		ldstr "class G3_C1462`1<class BaseClass0> on type class G3_C1462`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1462`1<class BaseClass0>
		callvirt instance string class G3_C1462`1<class BaseClass0>::Method0()
		ldstr "G2_C488::Method0.9437()"
		ldstr "class G3_C1462`1<class BaseClass0> on type class G3_C1462`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1462`1<class BaseClass0>
		callvirt instance string class G3_C1462`1<class BaseClass0>::ClassMethod1339<object>()
		ldstr "G1_C9::ClassMethod1339.4843<System.Object>()"
		ldstr "class G3_C1462`1<class BaseClass0> on type class G3_C1462`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1462`1<class BaseClass0>
		callvirt instance string class G3_C1462`1<class BaseClass0>::ClassMethod1338<object>()
		ldstr "G2_C488::ClassMethod1338.MI.9450<System.Object>()"
		ldstr "class G3_C1462`1<class BaseClass0> on type class G3_C1462`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1462`1<class BaseClass0>
		callvirt instance string class G3_C1462`1<class BaseClass0>::ClassMethod1337()
		ldstr "G2_C488::ClassMethod1337.MI.9449()"
		ldstr "class G3_C1462`1<class BaseClass0> on type class G3_C1462`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1462`1<class BaseClass0>
		callvirt instance string class G3_C1462`1<class BaseClass0>::ClassMethod1336()
		ldstr "G2_C488::ClassMethod1336.MI.9448()"
		ldstr "class G3_C1462`1<class BaseClass0> on type class G3_C1462`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1462`1<class BaseClass0>
		callvirt instance string class G3_C1462`1<class BaseClass0>::Method6<object>()
		ldstr "G1_C9::Method6.4838<System.Object>()"
		ldstr "class G3_C1462`1<class BaseClass0> on type class G3_C1462`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1462`1<class BaseClass0>
		callvirt instance string class G3_C1462`1<class BaseClass0>::Method5()
		ldstr "G1_C9::Method5.4836()"
		ldstr "class G3_C1462`1<class BaseClass0> on type class G3_C1462`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1462`1<class BaseClass0>
		callvirt instance string class G3_C1462`1<class BaseClass0>::Method4()
		ldstr "G1_C9::Method4.4834()"
		ldstr "class G3_C1462`1<class BaseClass0> on type class G3_C1462`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G3_C1462`1<class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C9`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C9`2<class BaseClass1,class BaseClass1>::ClassMethod1339<object>()
		ldstr "G1_C9::ClassMethod1339.4843<System.Object>()"
		ldstr "class G1_C9`2<class BaseClass1,class BaseClass1> on type class G3_C1462`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C9`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C9`2<class BaseClass1,class BaseClass1>::ClassMethod1338<object>()
		ldstr "G2_C488::ClassMethod1338.MI.9450<System.Object>()"
		ldstr "class G1_C9`2<class BaseClass1,class BaseClass1> on type class G3_C1462`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C9`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C9`2<class BaseClass1,class BaseClass1>::ClassMethod1337()
		ldstr "G2_C488::ClassMethod1337.MI.9449()"
		ldstr "class G1_C9`2<class BaseClass1,class BaseClass1> on type class G3_C1462`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C9`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C9`2<class BaseClass1,class BaseClass1>::ClassMethod1336()
		ldstr "G2_C488::ClassMethod1336.MI.9448()"
		ldstr "class G1_C9`2<class BaseClass1,class BaseClass1> on type class G3_C1462`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C9`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C9`2<class BaseClass1,class BaseClass1>::Method6<object>()
		ldstr "G1_C9::Method6.4838<System.Object>()"
		ldstr "class G1_C9`2<class BaseClass1,class BaseClass1> on type class G3_C1462`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C9`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C9`2<class BaseClass1,class BaseClass1>::Method5()
		ldstr "G1_C9::Method5.4836()"
		ldstr "class G1_C9`2<class BaseClass1,class BaseClass1> on type class G3_C1462`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C9`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C9`2<class BaseClass1,class BaseClass1>::Method4()
		ldstr "G1_C9::Method4.4834()"
		ldstr "class G1_C9`2<class BaseClass1,class BaseClass1> on type class G3_C1462`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C9`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C9`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G3_C1462::Method7.16292<System.Object>()"
		ldstr "class G1_C9`2<class BaseClass1,class BaseClass1> on type class G3_C1462`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G3_C1462::Method7.MI.16293<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1462`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G1_C9::Method4.MI.4835()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1462`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G1_C9::Method5.MI.4837()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1462`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G1_C9::Method6.MI.4839<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1462`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C488`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C488`2<class BaseClass1,class BaseClass1>::ClassMethod2331<object>()
		ldstr "G2_C488::ClassMethod2331.9447<System.Object>()"
		ldstr "class G2_C488`2<class BaseClass1,class BaseClass1> on type class G3_C1462`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C488`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C488`2<class BaseClass1,class BaseClass1>::ClassMethod2330()
		ldstr "G3_C1462::ClassMethod2330.MI.16298()"
		ldstr "class G2_C488`2<class BaseClass1,class BaseClass1> on type class G3_C1462`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C488`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C488`2<class BaseClass1,class BaseClass1>::ClassMethod2329()
		ldstr "G3_C1462::ClassMethod2329.MI.16297()"
		ldstr "class G2_C488`2<class BaseClass1,class BaseClass1> on type class G3_C1462`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C488`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C488`2<class BaseClass1,class BaseClass1>::Method3<object>()
		ldstr "G2_C488::Method3.9443<System.Object>()"
		ldstr "class G2_C488`2<class BaseClass1,class BaseClass1> on type class G3_C1462`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C488`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C488`2<class BaseClass1,class BaseClass1>::Method2<object>()
		ldstr "G2_C488::Method2.9441<System.Object>()"
		ldstr "class G2_C488`2<class BaseClass1,class BaseClass1> on type class G3_C1462`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C488`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C488`2<class BaseClass1,class BaseClass1>::Method1()
		ldstr "G2_C488::Method1.9439()"
		ldstr "class G2_C488`2<class BaseClass1,class BaseClass1> on type class G3_C1462`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C488`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C488`2<class BaseClass1,class BaseClass1>::Method0()
		ldstr "G2_C488::Method0.9437()"
		ldstr "class G2_C488`2<class BaseClass1,class BaseClass1> on type class G3_C1462`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C488`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C488`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G3_C1462::Method7.16292<System.Object>()"
		ldstr "class G2_C488`2<class BaseClass1,class BaseClass1> on type class G3_C1462`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C488`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C488`2<class BaseClass1,class BaseClass1>::ClassMethod1339<object>()
		ldstr "G1_C9::ClassMethod1339.4843<System.Object>()"
		ldstr "class G2_C488`2<class BaseClass1,class BaseClass1> on type class G3_C1462`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C488`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C488`2<class BaseClass1,class BaseClass1>::ClassMethod1338<object>()
		ldstr "G2_C488::ClassMethod1338.MI.9450<System.Object>()"
		ldstr "class G2_C488`2<class BaseClass1,class BaseClass1> on type class G3_C1462`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C488`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C488`2<class BaseClass1,class BaseClass1>::ClassMethod1337()
		ldstr "G2_C488::ClassMethod1337.MI.9449()"
		ldstr "class G2_C488`2<class BaseClass1,class BaseClass1> on type class G3_C1462`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C488`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C488`2<class BaseClass1,class BaseClass1>::ClassMethod1336()
		ldstr "G2_C488::ClassMethod1336.MI.9448()"
		ldstr "class G2_C488`2<class BaseClass1,class BaseClass1> on type class G3_C1462`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C488`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C488`2<class BaseClass1,class BaseClass1>::Method6<object>()
		ldstr "G1_C9::Method6.4838<System.Object>()"
		ldstr "class G2_C488`2<class BaseClass1,class BaseClass1> on type class G3_C1462`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C488`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C488`2<class BaseClass1,class BaseClass1>::Method5()
		ldstr "G1_C9::Method5.4836()"
		ldstr "class G2_C488`2<class BaseClass1,class BaseClass1> on type class G3_C1462`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C488`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C488`2<class BaseClass1,class BaseClass1>::Method4()
		ldstr "G1_C9::Method4.4834()"
		ldstr "class G2_C488`2<class BaseClass1,class BaseClass1> on type class G3_C1462`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string IBase0::Method0()
		ldstr "G2_C488::Method0.MI.9438()"
		ldstr "IBase0 on type class G3_C1462`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method1()
		ldstr "G2_C488::Method1.MI.9440()"
		ldstr "IBase0 on type class G3_C1462`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method2<object>()
		ldstr "G2_C488::Method2.MI.9442<System.Object>()"
		ldstr "IBase0 on type class G3_C1462`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method3<object>()
		ldstr "G2_C488::Method3.MI.9444<System.Object>()"
		ldstr "IBase0 on type class G3_C1462`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G3_C1462`1<class BaseClass1>
		callvirt instance string class G3_C1462`1<class BaseClass1>::ClassMethod4314<object>()
		ldstr "G3_C1462::ClassMethod4314.16296<System.Object>()"
		ldstr "class G3_C1462`1<class BaseClass1> on type class G3_C1462`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1462`1<class BaseClass1>
		callvirt instance string class G3_C1462`1<class BaseClass1>::ClassMethod4313<object>()
		ldstr "G3_C1462::ClassMethod4313.16295<System.Object>()"
		ldstr "class G3_C1462`1<class BaseClass1> on type class G3_C1462`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1462`1<class BaseClass1>
		callvirt instance string class G3_C1462`1<class BaseClass1>::ClassMethod4312()
		ldstr "G3_C1462::ClassMethod4312.16294()"
		ldstr "class G3_C1462`1<class BaseClass1> on type class G3_C1462`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1462`1<class BaseClass1>
		callvirt instance string class G3_C1462`1<class BaseClass1>::Method7<object>()
		ldstr "G3_C1462::Method7.16292<System.Object>()"
		ldstr "class G3_C1462`1<class BaseClass1> on type class G3_C1462`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1462`1<class BaseClass1>
		callvirt instance string class G3_C1462`1<class BaseClass1>::ClassMethod2331<object>()
		ldstr "G2_C488::ClassMethod2331.9447<System.Object>()"
		ldstr "class G3_C1462`1<class BaseClass1> on type class G3_C1462`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1462`1<class BaseClass1>
		callvirt instance string class G3_C1462`1<class BaseClass1>::ClassMethod2330()
		ldstr "G3_C1462::ClassMethod2330.MI.16298()"
		ldstr "class G3_C1462`1<class BaseClass1> on type class G3_C1462`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1462`1<class BaseClass1>
		callvirt instance string class G3_C1462`1<class BaseClass1>::ClassMethod2329()
		ldstr "G3_C1462::ClassMethod2329.MI.16297()"
		ldstr "class G3_C1462`1<class BaseClass1> on type class G3_C1462`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1462`1<class BaseClass1>
		callvirt instance string class G3_C1462`1<class BaseClass1>::Method3<object>()
		ldstr "G2_C488::Method3.9443<System.Object>()"
		ldstr "class G3_C1462`1<class BaseClass1> on type class G3_C1462`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1462`1<class BaseClass1>
		callvirt instance string class G3_C1462`1<class BaseClass1>::Method2<object>()
		ldstr "G2_C488::Method2.9441<System.Object>()"
		ldstr "class G3_C1462`1<class BaseClass1> on type class G3_C1462`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1462`1<class BaseClass1>
		callvirt instance string class G3_C1462`1<class BaseClass1>::Method1()
		ldstr "G2_C488::Method1.9439()"
		ldstr "class G3_C1462`1<class BaseClass1> on type class G3_C1462`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1462`1<class BaseClass1>
		callvirt instance string class G3_C1462`1<class BaseClass1>::Method0()
		ldstr "G2_C488::Method0.9437()"
		ldstr "class G3_C1462`1<class BaseClass1> on type class G3_C1462`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1462`1<class BaseClass1>
		callvirt instance string class G3_C1462`1<class BaseClass1>::ClassMethod1339<object>()
		ldstr "G1_C9::ClassMethod1339.4843<System.Object>()"
		ldstr "class G3_C1462`1<class BaseClass1> on type class G3_C1462`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1462`1<class BaseClass1>
		callvirt instance string class G3_C1462`1<class BaseClass1>::ClassMethod1338<object>()
		ldstr "G2_C488::ClassMethod1338.MI.9450<System.Object>()"
		ldstr "class G3_C1462`1<class BaseClass1> on type class G3_C1462`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1462`1<class BaseClass1>
		callvirt instance string class G3_C1462`1<class BaseClass1>::ClassMethod1337()
		ldstr "G2_C488::ClassMethod1337.MI.9449()"
		ldstr "class G3_C1462`1<class BaseClass1> on type class G3_C1462`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1462`1<class BaseClass1>
		callvirt instance string class G3_C1462`1<class BaseClass1>::ClassMethod1336()
		ldstr "G2_C488::ClassMethod1336.MI.9448()"
		ldstr "class G3_C1462`1<class BaseClass1> on type class G3_C1462`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1462`1<class BaseClass1>
		callvirt instance string class G3_C1462`1<class BaseClass1>::Method6<object>()
		ldstr "G1_C9::Method6.4838<System.Object>()"
		ldstr "class G3_C1462`1<class BaseClass1> on type class G3_C1462`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1462`1<class BaseClass1>
		callvirt instance string class G3_C1462`1<class BaseClass1>::Method5()
		ldstr "G1_C9::Method5.4836()"
		ldstr "class G3_C1462`1<class BaseClass1> on type class G3_C1462`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1462`1<class BaseClass1>
		callvirt instance string class G3_C1462`1<class BaseClass1>::Method4()
		ldstr "G1_C9::Method4.4834()"
		ldstr "class G3_C1462`1<class BaseClass1> on type class G3_C1462`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G3_C1462::Method7.MI.16293<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G3_C1462`1<class BaseClass1>"
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
		newobj instance void class G3_C1462`1<class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C488::ClassMethod1336.MI.9448()#G2_C488::ClassMethod1337.MI.9449()#G2_C488::ClassMethod1338.MI.9450<System.Object>()#G1_C9::ClassMethod1339.4843<System.Object>()#G1_C9::Method4.4834()#G1_C9::Method5.4836()#G1_C9::Method6.4838<System.Object>()#G3_C1462::Method7.16292<System.Object>()#"
		call void Generated990::M.G1_C9.T.T<class BaseClass1,class BaseClass1,class G3_C1462`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C488::ClassMethod1336.MI.9448()#G2_C488::ClassMethod1337.MI.9449()#G2_C488::ClassMethod1338.MI.9450<System.Object>()#G1_C9::ClassMethod1339.4843<System.Object>()#G1_C9::Method4.4834()#G1_C9::Method5.4836()#G1_C9::Method6.4838<System.Object>()#G3_C1462::Method7.16292<System.Object>()#"
		call void Generated990::M.G1_C9.B.T<class BaseClass1,class G3_C1462`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C488::ClassMethod1336.MI.9448()#G2_C488::ClassMethod1337.MI.9449()#G2_C488::ClassMethod1338.MI.9450<System.Object>()#G1_C9::ClassMethod1339.4843<System.Object>()#G1_C9::Method4.4834()#G1_C9::Method5.4836()#G1_C9::Method6.4838<System.Object>()#G3_C1462::Method7.16292<System.Object>()#"
		call void Generated990::M.G1_C9.B.B<class G3_C1462`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1462::Method7.MI.16293<System.Object>()#"
		call void Generated990::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G3_C1462`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G3_C1462::Method7.MI.16293<System.Object>()#"
		call void Generated990::M.IBase2.A.T<class BaseClass1,class G3_C1462`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G3_C1462::Method7.MI.16293<System.Object>()#"
		call void Generated990::M.IBase2.A.B<class G3_C1462`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C9::Method4.MI.4835()#G1_C9::Method5.MI.4837()#G1_C9::Method6.MI.4839<System.Object>()#"
		call void Generated990::M.IBase1.T<class BaseClass0,class G3_C1462`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C9::Method4.MI.4835()#G1_C9::Method5.MI.4837()#G1_C9::Method6.MI.4839<System.Object>()#"
		call void Generated990::M.IBase1.A<class G3_C1462`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C488::ClassMethod1336.MI.9448()#G2_C488::ClassMethod1337.MI.9449()#G2_C488::ClassMethod1338.MI.9450<System.Object>()#G1_C9::ClassMethod1339.4843<System.Object>()#G3_C1462::ClassMethod2329.MI.16297()#G3_C1462::ClassMethod2330.MI.16298()#G2_C488::ClassMethod2331.9447<System.Object>()#G2_C488::Method0.9437()#G2_C488::Method1.9439()#G2_C488::Method2.9441<System.Object>()#G2_C488::Method3.9443<System.Object>()#G1_C9::Method4.4834()#G1_C9::Method5.4836()#G1_C9::Method6.4838<System.Object>()#G3_C1462::Method7.16292<System.Object>()#"
		call void Generated990::M.G2_C488.T.T<class BaseClass0,class BaseClass1,class G3_C1462`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C488::ClassMethod1336.MI.9448()#G2_C488::ClassMethod1337.MI.9449()#G2_C488::ClassMethod1338.MI.9450<System.Object>()#G1_C9::ClassMethod1339.4843<System.Object>()#G3_C1462::ClassMethod2329.MI.16297()#G3_C1462::ClassMethod2330.MI.16298()#G2_C488::ClassMethod2331.9447<System.Object>()#G2_C488::Method0.9437()#G2_C488::Method1.9439()#G2_C488::Method2.9441<System.Object>()#G2_C488::Method3.9443<System.Object>()#G1_C9::Method4.4834()#G1_C9::Method5.4836()#G1_C9::Method6.4838<System.Object>()#G3_C1462::Method7.16292<System.Object>()#"
		call void Generated990::M.G2_C488.A.T<class BaseClass1,class G3_C1462`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C488::ClassMethod1336.MI.9448()#G2_C488::ClassMethod1337.MI.9449()#G2_C488::ClassMethod1338.MI.9450<System.Object>()#G1_C9::ClassMethod1339.4843<System.Object>()#G3_C1462::ClassMethod2329.MI.16297()#G3_C1462::ClassMethod2330.MI.16298()#G2_C488::ClassMethod2331.9447<System.Object>()#G2_C488::Method0.9437()#G2_C488::Method1.9439()#G2_C488::Method2.9441<System.Object>()#G2_C488::Method3.9443<System.Object>()#G1_C9::Method4.4834()#G1_C9::Method5.4836()#G1_C9::Method6.4838<System.Object>()#G3_C1462::Method7.16292<System.Object>()#"
		call void Generated990::M.G2_C488.A.B<class G3_C1462`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1462::Method7.MI.16293<System.Object>()#"
		call void Generated990::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G3_C1462`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G3_C1462::Method7.MI.16293<System.Object>()#"
		call void Generated990::M.IBase2.A.T<class BaseClass0,class G3_C1462`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G3_C1462::Method7.MI.16293<System.Object>()#"
		call void Generated990::M.IBase2.A.A<class G3_C1462`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C488::Method0.MI.9438()#G2_C488::Method1.MI.9440()#G2_C488::Method2.MI.9442<System.Object>()#G2_C488::Method3.MI.9444<System.Object>()#"
		call void Generated990::M.IBase0<class G3_C1462`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C488::ClassMethod1336.MI.9448()#G2_C488::ClassMethod1337.MI.9449()#G2_C488::ClassMethod1338.MI.9450<System.Object>()#G1_C9::ClassMethod1339.4843<System.Object>()#G3_C1462::ClassMethod2329.MI.16297()#G3_C1462::ClassMethod2330.MI.16298()#G2_C488::ClassMethod2331.9447<System.Object>()#G3_C1462::ClassMethod4312.16294()#G3_C1462::ClassMethod4313.16295<System.Object>()#G3_C1462::ClassMethod4314.16296<System.Object>()#G2_C488::Method0.9437()#G2_C488::Method1.9439()#G2_C488::Method2.9441<System.Object>()#G2_C488::Method3.9443<System.Object>()#G1_C9::Method4.4834()#G1_C9::Method5.4836()#G1_C9::Method6.4838<System.Object>()#G3_C1462::Method7.16292<System.Object>()#"
		call void Generated990::M.G3_C1462.T<class BaseClass0,class G3_C1462`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C488::ClassMethod1336.MI.9448()#G2_C488::ClassMethod1337.MI.9449()#G2_C488::ClassMethod1338.MI.9450<System.Object>()#G1_C9::ClassMethod1339.4843<System.Object>()#G3_C1462::ClassMethod2329.MI.16297()#G3_C1462::ClassMethod2330.MI.16298()#G2_C488::ClassMethod2331.9447<System.Object>()#G3_C1462::ClassMethod4312.16294()#G3_C1462::ClassMethod4313.16295<System.Object>()#G3_C1462::ClassMethod4314.16296<System.Object>()#G2_C488::Method0.9437()#G2_C488::Method1.9439()#G2_C488::Method2.9441<System.Object>()#G2_C488::Method3.9443<System.Object>()#G1_C9::Method4.4834()#G1_C9::Method5.4836()#G1_C9::Method6.4838<System.Object>()#G3_C1462::Method7.16292<System.Object>()#"
		call void Generated990::M.G3_C1462.A<class G3_C1462`1<class BaseClass0>>(!!0,string)
		newobj instance void class G3_C1462`1<class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C488::ClassMethod1336.MI.9448()#G2_C488::ClassMethod1337.MI.9449()#G2_C488::ClassMethod1338.MI.9450<System.Object>()#G1_C9::ClassMethod1339.4843<System.Object>()#G1_C9::Method4.4834()#G1_C9::Method5.4836()#G1_C9::Method6.4838<System.Object>()#G3_C1462::Method7.16292<System.Object>()#"
		call void Generated990::M.G1_C9.T.T<class BaseClass1,class BaseClass1,class G3_C1462`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C488::ClassMethod1336.MI.9448()#G2_C488::ClassMethod1337.MI.9449()#G2_C488::ClassMethod1338.MI.9450<System.Object>()#G1_C9::ClassMethod1339.4843<System.Object>()#G1_C9::Method4.4834()#G1_C9::Method5.4836()#G1_C9::Method6.4838<System.Object>()#G3_C1462::Method7.16292<System.Object>()#"
		call void Generated990::M.G1_C9.B.T<class BaseClass1,class G3_C1462`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C488::ClassMethod1336.MI.9448()#G2_C488::ClassMethod1337.MI.9449()#G2_C488::ClassMethod1338.MI.9450<System.Object>()#G1_C9::ClassMethod1339.4843<System.Object>()#G1_C9::Method4.4834()#G1_C9::Method5.4836()#G1_C9::Method6.4838<System.Object>()#G3_C1462::Method7.16292<System.Object>()#"
		call void Generated990::M.G1_C9.B.B<class G3_C1462`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1462::Method7.MI.16293<System.Object>()#"
		call void Generated990::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G3_C1462`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G3_C1462::Method7.MI.16293<System.Object>()#"
		call void Generated990::M.IBase2.A.T<class BaseClass1,class G3_C1462`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1462::Method7.MI.16293<System.Object>()#"
		call void Generated990::M.IBase2.A.B<class G3_C1462`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C9::Method4.MI.4835()#G1_C9::Method5.MI.4837()#G1_C9::Method6.MI.4839<System.Object>()#"
		call void Generated990::M.IBase1.T<class BaseClass0,class G3_C1462`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C9::Method4.MI.4835()#G1_C9::Method5.MI.4837()#G1_C9::Method6.MI.4839<System.Object>()#"
		call void Generated990::M.IBase1.A<class G3_C1462`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C488::ClassMethod1336.MI.9448()#G2_C488::ClassMethod1337.MI.9449()#G2_C488::ClassMethod1338.MI.9450<System.Object>()#G1_C9::ClassMethod1339.4843<System.Object>()#G3_C1462::ClassMethod2329.MI.16297()#G3_C1462::ClassMethod2330.MI.16298()#G2_C488::ClassMethod2331.9447<System.Object>()#G2_C488::Method0.9437()#G2_C488::Method1.9439()#G2_C488::Method2.9441<System.Object>()#G2_C488::Method3.9443<System.Object>()#G1_C9::Method4.4834()#G1_C9::Method5.4836()#G1_C9::Method6.4838<System.Object>()#G3_C1462::Method7.16292<System.Object>()#"
		call void Generated990::M.G2_C488.T.T<class BaseClass1,class BaseClass1,class G3_C1462`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C488::ClassMethod1336.MI.9448()#G2_C488::ClassMethod1337.MI.9449()#G2_C488::ClassMethod1338.MI.9450<System.Object>()#G1_C9::ClassMethod1339.4843<System.Object>()#G3_C1462::ClassMethod2329.MI.16297()#G3_C1462::ClassMethod2330.MI.16298()#G2_C488::ClassMethod2331.9447<System.Object>()#G2_C488::Method0.9437()#G2_C488::Method1.9439()#G2_C488::Method2.9441<System.Object>()#G2_C488::Method3.9443<System.Object>()#G1_C9::Method4.4834()#G1_C9::Method5.4836()#G1_C9::Method6.4838<System.Object>()#G3_C1462::Method7.16292<System.Object>()#"
		call void Generated990::M.G2_C488.B.T<class BaseClass1,class G3_C1462`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C488::ClassMethod1336.MI.9448()#G2_C488::ClassMethod1337.MI.9449()#G2_C488::ClassMethod1338.MI.9450<System.Object>()#G1_C9::ClassMethod1339.4843<System.Object>()#G3_C1462::ClassMethod2329.MI.16297()#G3_C1462::ClassMethod2330.MI.16298()#G2_C488::ClassMethod2331.9447<System.Object>()#G2_C488::Method0.9437()#G2_C488::Method1.9439()#G2_C488::Method2.9441<System.Object>()#G2_C488::Method3.9443<System.Object>()#G1_C9::Method4.4834()#G1_C9::Method5.4836()#G1_C9::Method6.4838<System.Object>()#G3_C1462::Method7.16292<System.Object>()#"
		call void Generated990::M.G2_C488.B.B<class G3_C1462`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C488::Method0.MI.9438()#G2_C488::Method1.MI.9440()#G2_C488::Method2.MI.9442<System.Object>()#G2_C488::Method3.MI.9444<System.Object>()#"
		call void Generated990::M.IBase0<class G3_C1462`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C488::ClassMethod1336.MI.9448()#G2_C488::ClassMethod1337.MI.9449()#G2_C488::ClassMethod1338.MI.9450<System.Object>()#G1_C9::ClassMethod1339.4843<System.Object>()#G3_C1462::ClassMethod2329.MI.16297()#G3_C1462::ClassMethod2330.MI.16298()#G2_C488::ClassMethod2331.9447<System.Object>()#G3_C1462::ClassMethod4312.16294()#G3_C1462::ClassMethod4313.16295<System.Object>()#G3_C1462::ClassMethod4314.16296<System.Object>()#G2_C488::Method0.9437()#G2_C488::Method1.9439()#G2_C488::Method2.9441<System.Object>()#G2_C488::Method3.9443<System.Object>()#G1_C9::Method4.4834()#G1_C9::Method5.4836()#G1_C9::Method6.4838<System.Object>()#G3_C1462::Method7.16292<System.Object>()#"
		call void Generated990::M.G3_C1462.T<class BaseClass1,class G3_C1462`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C488::ClassMethod1336.MI.9448()#G2_C488::ClassMethod1337.MI.9449()#G2_C488::ClassMethod1338.MI.9450<System.Object>()#G1_C9::ClassMethod1339.4843<System.Object>()#G3_C1462::ClassMethod2329.MI.16297()#G3_C1462::ClassMethod2330.MI.16298()#G2_C488::ClassMethod2331.9447<System.Object>()#G3_C1462::ClassMethod4312.16294()#G3_C1462::ClassMethod4313.16295<System.Object>()#G3_C1462::ClassMethod4314.16296<System.Object>()#G2_C488::Method0.9437()#G2_C488::Method1.9439()#G2_C488::Method2.9441<System.Object>()#G2_C488::Method3.9443<System.Object>()#G1_C9::Method4.4834()#G1_C9::Method5.4836()#G1_C9::Method6.4838<System.Object>()#G3_C1462::Method7.16292<System.Object>()#"
		call void Generated990::M.G3_C1462.B<class G3_C1462`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1462::Method7.MI.16293<System.Object>()#"
		call void Generated990::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G3_C1462`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G3_C1462::Method7.MI.16293<System.Object>()#"
		call void Generated990::M.IBase2.B.T<class BaseClass1,class G3_C1462`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1462::Method7.MI.16293<System.Object>()#"
		call void Generated990::M.IBase2.B.B<class G3_C1462`1<class BaseClass1>>(!!0,string)
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
		newobj instance void class G3_C1462`1<class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C9`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C9`2<class BaseClass1,class BaseClass1>::ClassMethod1339<object>()
		calli default string(class G3_C1462`1<class BaseClass0>)
		ldstr "G1_C9::ClassMethod1339.4843<System.Object>()"
		ldstr "class G1_C9`2<class BaseClass1,class BaseClass1> on type class G3_C1462`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C9`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C9`2<class BaseClass1,class BaseClass1>::ClassMethod1338<object>()
		calli default string(class G3_C1462`1<class BaseClass0>)
		ldstr "G2_C488::ClassMethod1338.MI.9450<System.Object>()"
		ldstr "class G1_C9`2<class BaseClass1,class BaseClass1> on type class G3_C1462`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C9`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C9`2<class BaseClass1,class BaseClass1>::ClassMethod1337()
		calli default string(class G3_C1462`1<class BaseClass0>)
		ldstr "G2_C488::ClassMethod1337.MI.9449()"
		ldstr "class G1_C9`2<class BaseClass1,class BaseClass1> on type class G3_C1462`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C9`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C9`2<class BaseClass1,class BaseClass1>::ClassMethod1336()
		calli default string(class G3_C1462`1<class BaseClass0>)
		ldstr "G2_C488::ClassMethod1336.MI.9448()"
		ldstr "class G1_C9`2<class BaseClass1,class BaseClass1> on type class G3_C1462`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C9`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C9`2<class BaseClass1,class BaseClass1>::Method6<object>()
		calli default string(class G3_C1462`1<class BaseClass0>)
		ldstr "G1_C9::Method6.4838<System.Object>()"
		ldstr "class G1_C9`2<class BaseClass1,class BaseClass1> on type class G3_C1462`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C9`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C9`2<class BaseClass1,class BaseClass1>::Method5()
		calli default string(class G3_C1462`1<class BaseClass0>)
		ldstr "G1_C9::Method5.4836()"
		ldstr "class G1_C9`2<class BaseClass1,class BaseClass1> on type class G3_C1462`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C9`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C9`2<class BaseClass1,class BaseClass1>::Method4()
		calli default string(class G3_C1462`1<class BaseClass0>)
		ldstr "G1_C9::Method4.4834()"
		ldstr "class G1_C9`2<class BaseClass1,class BaseClass1> on type class G3_C1462`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C9`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C9`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1462`1<class BaseClass0>)
		ldstr "G3_C1462::Method7.16292<System.Object>()"
		ldstr "class G1_C9`2<class BaseClass1,class BaseClass1> on type class G3_C1462`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1462`1<class BaseClass0>)
		ldstr "G3_C1462::Method7.MI.16293<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1462`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G3_C1462`1<class BaseClass0>)
		ldstr "G1_C9::Method4.MI.4835()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1462`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G3_C1462`1<class BaseClass0>)
		ldstr "G1_C9::Method5.MI.4837()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1462`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G3_C1462`1<class BaseClass0>)
		ldstr "G1_C9::Method6.MI.4839<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1462`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C488`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C488`2<class BaseClass0,class BaseClass1>::ClassMethod2331<object>()
		calli default string(class G3_C1462`1<class BaseClass0>)
		ldstr "G2_C488::ClassMethod2331.9447<System.Object>()"
		ldstr "class G2_C488`2<class BaseClass0,class BaseClass1> on type class G3_C1462`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C488`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C488`2<class BaseClass0,class BaseClass1>::ClassMethod2330()
		calli default string(class G3_C1462`1<class BaseClass0>)
		ldstr "G3_C1462::ClassMethod2330.MI.16298()"
		ldstr "class G2_C488`2<class BaseClass0,class BaseClass1> on type class G3_C1462`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C488`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C488`2<class BaseClass0,class BaseClass1>::ClassMethod2329()
		calli default string(class G3_C1462`1<class BaseClass0>)
		ldstr "G3_C1462::ClassMethod2329.MI.16297()"
		ldstr "class G2_C488`2<class BaseClass0,class BaseClass1> on type class G3_C1462`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C488`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C488`2<class BaseClass0,class BaseClass1>::Method3<object>()
		calli default string(class G3_C1462`1<class BaseClass0>)
		ldstr "G2_C488::Method3.9443<System.Object>()"
		ldstr "class G2_C488`2<class BaseClass0,class BaseClass1> on type class G3_C1462`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C488`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C488`2<class BaseClass0,class BaseClass1>::Method2<object>()
		calli default string(class G3_C1462`1<class BaseClass0>)
		ldstr "G2_C488::Method2.9441<System.Object>()"
		ldstr "class G2_C488`2<class BaseClass0,class BaseClass1> on type class G3_C1462`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C488`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C488`2<class BaseClass0,class BaseClass1>::Method1()
		calli default string(class G3_C1462`1<class BaseClass0>)
		ldstr "G2_C488::Method1.9439()"
		ldstr "class G2_C488`2<class BaseClass0,class BaseClass1> on type class G3_C1462`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C488`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C488`2<class BaseClass0,class BaseClass1>::Method0()
		calli default string(class G3_C1462`1<class BaseClass0>)
		ldstr "G2_C488::Method0.9437()"
		ldstr "class G2_C488`2<class BaseClass0,class BaseClass1> on type class G3_C1462`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C488`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C488`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1462`1<class BaseClass0>)
		ldstr "G3_C1462::Method7.16292<System.Object>()"
		ldstr "class G2_C488`2<class BaseClass0,class BaseClass1> on type class G3_C1462`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C488`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C488`2<class BaseClass0,class BaseClass1>::ClassMethod1339<object>()
		calli default string(class G3_C1462`1<class BaseClass0>)
		ldstr "G1_C9::ClassMethod1339.4843<System.Object>()"
		ldstr "class G2_C488`2<class BaseClass0,class BaseClass1> on type class G3_C1462`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C488`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C488`2<class BaseClass0,class BaseClass1>::ClassMethod1338<object>()
		calli default string(class G3_C1462`1<class BaseClass0>)
		ldstr "G2_C488::ClassMethod1338.MI.9450<System.Object>()"
		ldstr "class G2_C488`2<class BaseClass0,class BaseClass1> on type class G3_C1462`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C488`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C488`2<class BaseClass0,class BaseClass1>::ClassMethod1337()
		calli default string(class G3_C1462`1<class BaseClass0>)
		ldstr "G2_C488::ClassMethod1337.MI.9449()"
		ldstr "class G2_C488`2<class BaseClass0,class BaseClass1> on type class G3_C1462`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C488`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C488`2<class BaseClass0,class BaseClass1>::ClassMethod1336()
		calli default string(class G3_C1462`1<class BaseClass0>)
		ldstr "G2_C488::ClassMethod1336.MI.9448()"
		ldstr "class G2_C488`2<class BaseClass0,class BaseClass1> on type class G3_C1462`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C488`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C488`2<class BaseClass0,class BaseClass1>::Method6<object>()
		calli default string(class G3_C1462`1<class BaseClass0>)
		ldstr "G1_C9::Method6.4838<System.Object>()"
		ldstr "class G2_C488`2<class BaseClass0,class BaseClass1> on type class G3_C1462`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C488`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C488`2<class BaseClass0,class BaseClass1>::Method5()
		calli default string(class G3_C1462`1<class BaseClass0>)
		ldstr "G1_C9::Method5.4836()"
		ldstr "class G2_C488`2<class BaseClass0,class BaseClass1> on type class G3_C1462`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C488`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C488`2<class BaseClass0,class BaseClass1>::Method4()
		calli default string(class G3_C1462`1<class BaseClass0>)
		ldstr "G1_C9::Method4.4834()"
		ldstr "class G2_C488`2<class BaseClass0,class BaseClass1> on type class G3_C1462`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1462`1<class BaseClass0>)
		ldstr "G3_C1462::Method7.MI.16293<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G3_C1462`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method0()
		calli default string(class G3_C1462`1<class BaseClass0>)
		ldstr "G2_C488::Method0.MI.9438()"
		ldstr "IBase0 on type class G3_C1462`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method1()
		calli default string(class G3_C1462`1<class BaseClass0>)
		ldstr "G2_C488::Method1.MI.9440()"
		ldstr "IBase0 on type class G3_C1462`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method2<object>()
		calli default string(class G3_C1462`1<class BaseClass0>)
		ldstr "G2_C488::Method2.MI.9442<System.Object>()"
		ldstr "IBase0 on type class G3_C1462`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method3<object>()
		calli default string(class G3_C1462`1<class BaseClass0>)
		ldstr "G2_C488::Method3.MI.9444<System.Object>()"
		ldstr "IBase0 on type class G3_C1462`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1462`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1462`1<class BaseClass0>::ClassMethod4314<object>()
		calli default string(class G3_C1462`1<class BaseClass0>)
		ldstr "G3_C1462::ClassMethod4314.16296<System.Object>()"
		ldstr "class G3_C1462`1<class BaseClass0> on type class G3_C1462`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1462`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1462`1<class BaseClass0>::ClassMethod4313<object>()
		calli default string(class G3_C1462`1<class BaseClass0>)
		ldstr "G3_C1462::ClassMethod4313.16295<System.Object>()"
		ldstr "class G3_C1462`1<class BaseClass0> on type class G3_C1462`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1462`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1462`1<class BaseClass0>::ClassMethod4312()
		calli default string(class G3_C1462`1<class BaseClass0>)
		ldstr "G3_C1462::ClassMethod4312.16294()"
		ldstr "class G3_C1462`1<class BaseClass0> on type class G3_C1462`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1462`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1462`1<class BaseClass0>::Method7<object>()
		calli default string(class G3_C1462`1<class BaseClass0>)
		ldstr "G3_C1462::Method7.16292<System.Object>()"
		ldstr "class G3_C1462`1<class BaseClass0> on type class G3_C1462`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1462`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1462`1<class BaseClass0>::ClassMethod2331<object>()
		calli default string(class G3_C1462`1<class BaseClass0>)
		ldstr "G2_C488::ClassMethod2331.9447<System.Object>()"
		ldstr "class G3_C1462`1<class BaseClass0> on type class G3_C1462`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1462`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1462`1<class BaseClass0>::ClassMethod2330()
		calli default string(class G3_C1462`1<class BaseClass0>)
		ldstr "G3_C1462::ClassMethod2330.MI.16298()"
		ldstr "class G3_C1462`1<class BaseClass0> on type class G3_C1462`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1462`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1462`1<class BaseClass0>::ClassMethod2329()
		calli default string(class G3_C1462`1<class BaseClass0>)
		ldstr "G3_C1462::ClassMethod2329.MI.16297()"
		ldstr "class G3_C1462`1<class BaseClass0> on type class G3_C1462`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1462`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1462`1<class BaseClass0>::Method3<object>()
		calli default string(class G3_C1462`1<class BaseClass0>)
		ldstr "G2_C488::Method3.9443<System.Object>()"
		ldstr "class G3_C1462`1<class BaseClass0> on type class G3_C1462`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1462`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1462`1<class BaseClass0>::Method2<object>()
		calli default string(class G3_C1462`1<class BaseClass0>)
		ldstr "G2_C488::Method2.9441<System.Object>()"
		ldstr "class G3_C1462`1<class BaseClass0> on type class G3_C1462`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1462`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1462`1<class BaseClass0>::Method1()
		calli default string(class G3_C1462`1<class BaseClass0>)
		ldstr "G2_C488::Method1.9439()"
		ldstr "class G3_C1462`1<class BaseClass0> on type class G3_C1462`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1462`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1462`1<class BaseClass0>::Method0()
		calli default string(class G3_C1462`1<class BaseClass0>)
		ldstr "G2_C488::Method0.9437()"
		ldstr "class G3_C1462`1<class BaseClass0> on type class G3_C1462`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1462`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1462`1<class BaseClass0>::ClassMethod1339<object>()
		calli default string(class G3_C1462`1<class BaseClass0>)
		ldstr "G1_C9::ClassMethod1339.4843<System.Object>()"
		ldstr "class G3_C1462`1<class BaseClass0> on type class G3_C1462`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1462`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1462`1<class BaseClass0>::ClassMethod1338<object>()
		calli default string(class G3_C1462`1<class BaseClass0>)
		ldstr "G2_C488::ClassMethod1338.MI.9450<System.Object>()"
		ldstr "class G3_C1462`1<class BaseClass0> on type class G3_C1462`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1462`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1462`1<class BaseClass0>::ClassMethod1337()
		calli default string(class G3_C1462`1<class BaseClass0>)
		ldstr "G2_C488::ClassMethod1337.MI.9449()"
		ldstr "class G3_C1462`1<class BaseClass0> on type class G3_C1462`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1462`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1462`1<class BaseClass0>::ClassMethod1336()
		calli default string(class G3_C1462`1<class BaseClass0>)
		ldstr "G2_C488::ClassMethod1336.MI.9448()"
		ldstr "class G3_C1462`1<class BaseClass0> on type class G3_C1462`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1462`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1462`1<class BaseClass0>::Method6<object>()
		calli default string(class G3_C1462`1<class BaseClass0>)
		ldstr "G1_C9::Method6.4838<System.Object>()"
		ldstr "class G3_C1462`1<class BaseClass0> on type class G3_C1462`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1462`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1462`1<class BaseClass0>::Method5()
		calli default string(class G3_C1462`1<class BaseClass0>)
		ldstr "G1_C9::Method5.4836()"
		ldstr "class G3_C1462`1<class BaseClass0> on type class G3_C1462`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1462`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1462`1<class BaseClass0>::Method4()
		calli default string(class G3_C1462`1<class BaseClass0>)
		ldstr "G1_C9::Method4.4834()"
		ldstr "class G3_C1462`1<class BaseClass0> on type class G3_C1462`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G3_C1462`1<class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C9`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C9`2<class BaseClass1,class BaseClass1>::ClassMethod1339<object>()
		calli default string(class G3_C1462`1<class BaseClass1>)
		ldstr "G1_C9::ClassMethod1339.4843<System.Object>()"
		ldstr "class G1_C9`2<class BaseClass1,class BaseClass1> on type class G3_C1462`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C9`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C9`2<class BaseClass1,class BaseClass1>::ClassMethod1338<object>()
		calli default string(class G3_C1462`1<class BaseClass1>)
		ldstr "G2_C488::ClassMethod1338.MI.9450<System.Object>()"
		ldstr "class G1_C9`2<class BaseClass1,class BaseClass1> on type class G3_C1462`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C9`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C9`2<class BaseClass1,class BaseClass1>::ClassMethod1337()
		calli default string(class G3_C1462`1<class BaseClass1>)
		ldstr "G2_C488::ClassMethod1337.MI.9449()"
		ldstr "class G1_C9`2<class BaseClass1,class BaseClass1> on type class G3_C1462`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C9`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C9`2<class BaseClass1,class BaseClass1>::ClassMethod1336()
		calli default string(class G3_C1462`1<class BaseClass1>)
		ldstr "G2_C488::ClassMethod1336.MI.9448()"
		ldstr "class G1_C9`2<class BaseClass1,class BaseClass1> on type class G3_C1462`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C9`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C9`2<class BaseClass1,class BaseClass1>::Method6<object>()
		calli default string(class G3_C1462`1<class BaseClass1>)
		ldstr "G1_C9::Method6.4838<System.Object>()"
		ldstr "class G1_C9`2<class BaseClass1,class BaseClass1> on type class G3_C1462`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C9`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C9`2<class BaseClass1,class BaseClass1>::Method5()
		calli default string(class G3_C1462`1<class BaseClass1>)
		ldstr "G1_C9::Method5.4836()"
		ldstr "class G1_C9`2<class BaseClass1,class BaseClass1> on type class G3_C1462`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C9`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C9`2<class BaseClass1,class BaseClass1>::Method4()
		calli default string(class G3_C1462`1<class BaseClass1>)
		ldstr "G1_C9::Method4.4834()"
		ldstr "class G1_C9`2<class BaseClass1,class BaseClass1> on type class G3_C1462`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C9`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C9`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1462`1<class BaseClass1>)
		ldstr "G3_C1462::Method7.16292<System.Object>()"
		ldstr "class G1_C9`2<class BaseClass1,class BaseClass1> on type class G3_C1462`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1462`1<class BaseClass1>)
		ldstr "G3_C1462::Method7.MI.16293<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1462`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G3_C1462`1<class BaseClass1>)
		ldstr "G1_C9::Method4.MI.4835()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1462`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G3_C1462`1<class BaseClass1>)
		ldstr "G1_C9::Method5.MI.4837()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1462`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G3_C1462`1<class BaseClass1>)
		ldstr "G1_C9::Method6.MI.4839<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1462`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C488`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C488`2<class BaseClass1,class BaseClass1>::ClassMethod2331<object>()
		calli default string(class G3_C1462`1<class BaseClass1>)
		ldstr "G2_C488::ClassMethod2331.9447<System.Object>()"
		ldstr "class G2_C488`2<class BaseClass1,class BaseClass1> on type class G3_C1462`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C488`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C488`2<class BaseClass1,class BaseClass1>::ClassMethod2330()
		calli default string(class G3_C1462`1<class BaseClass1>)
		ldstr "G3_C1462::ClassMethod2330.MI.16298()"
		ldstr "class G2_C488`2<class BaseClass1,class BaseClass1> on type class G3_C1462`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C488`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C488`2<class BaseClass1,class BaseClass1>::ClassMethod2329()
		calli default string(class G3_C1462`1<class BaseClass1>)
		ldstr "G3_C1462::ClassMethod2329.MI.16297()"
		ldstr "class G2_C488`2<class BaseClass1,class BaseClass1> on type class G3_C1462`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C488`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C488`2<class BaseClass1,class BaseClass1>::Method3<object>()
		calli default string(class G3_C1462`1<class BaseClass1>)
		ldstr "G2_C488::Method3.9443<System.Object>()"
		ldstr "class G2_C488`2<class BaseClass1,class BaseClass1> on type class G3_C1462`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C488`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C488`2<class BaseClass1,class BaseClass1>::Method2<object>()
		calli default string(class G3_C1462`1<class BaseClass1>)
		ldstr "G2_C488::Method2.9441<System.Object>()"
		ldstr "class G2_C488`2<class BaseClass1,class BaseClass1> on type class G3_C1462`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C488`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C488`2<class BaseClass1,class BaseClass1>::Method1()
		calli default string(class G3_C1462`1<class BaseClass1>)
		ldstr "G2_C488::Method1.9439()"
		ldstr "class G2_C488`2<class BaseClass1,class BaseClass1> on type class G3_C1462`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C488`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C488`2<class BaseClass1,class BaseClass1>::Method0()
		calli default string(class G3_C1462`1<class BaseClass1>)
		ldstr "G2_C488::Method0.9437()"
		ldstr "class G2_C488`2<class BaseClass1,class BaseClass1> on type class G3_C1462`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C488`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C488`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1462`1<class BaseClass1>)
		ldstr "G3_C1462::Method7.16292<System.Object>()"
		ldstr "class G2_C488`2<class BaseClass1,class BaseClass1> on type class G3_C1462`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C488`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C488`2<class BaseClass1,class BaseClass1>::ClassMethod1339<object>()
		calli default string(class G3_C1462`1<class BaseClass1>)
		ldstr "G1_C9::ClassMethod1339.4843<System.Object>()"
		ldstr "class G2_C488`2<class BaseClass1,class BaseClass1> on type class G3_C1462`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C488`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C488`2<class BaseClass1,class BaseClass1>::ClassMethod1338<object>()
		calli default string(class G3_C1462`1<class BaseClass1>)
		ldstr "G2_C488::ClassMethod1338.MI.9450<System.Object>()"
		ldstr "class G2_C488`2<class BaseClass1,class BaseClass1> on type class G3_C1462`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C488`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C488`2<class BaseClass1,class BaseClass1>::ClassMethod1337()
		calli default string(class G3_C1462`1<class BaseClass1>)
		ldstr "G2_C488::ClassMethod1337.MI.9449()"
		ldstr "class G2_C488`2<class BaseClass1,class BaseClass1> on type class G3_C1462`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C488`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C488`2<class BaseClass1,class BaseClass1>::ClassMethod1336()
		calli default string(class G3_C1462`1<class BaseClass1>)
		ldstr "G2_C488::ClassMethod1336.MI.9448()"
		ldstr "class G2_C488`2<class BaseClass1,class BaseClass1> on type class G3_C1462`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C488`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C488`2<class BaseClass1,class BaseClass1>::Method6<object>()
		calli default string(class G3_C1462`1<class BaseClass1>)
		ldstr "G1_C9::Method6.4838<System.Object>()"
		ldstr "class G2_C488`2<class BaseClass1,class BaseClass1> on type class G3_C1462`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C488`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C488`2<class BaseClass1,class BaseClass1>::Method5()
		calli default string(class G3_C1462`1<class BaseClass1>)
		ldstr "G1_C9::Method5.4836()"
		ldstr "class G2_C488`2<class BaseClass1,class BaseClass1> on type class G3_C1462`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C488`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C488`2<class BaseClass1,class BaseClass1>::Method4()
		calli default string(class G3_C1462`1<class BaseClass1>)
		ldstr "G1_C9::Method4.4834()"
		ldstr "class G2_C488`2<class BaseClass1,class BaseClass1> on type class G3_C1462`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method0()
		calli default string(class G3_C1462`1<class BaseClass1>)
		ldstr "G2_C488::Method0.MI.9438()"
		ldstr "IBase0 on type class G3_C1462`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method1()
		calli default string(class G3_C1462`1<class BaseClass1>)
		ldstr "G2_C488::Method1.MI.9440()"
		ldstr "IBase0 on type class G3_C1462`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method2<object>()
		calli default string(class G3_C1462`1<class BaseClass1>)
		ldstr "G2_C488::Method2.MI.9442<System.Object>()"
		ldstr "IBase0 on type class G3_C1462`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method3<object>()
		calli default string(class G3_C1462`1<class BaseClass1>)
		ldstr "G2_C488::Method3.MI.9444<System.Object>()"
		ldstr "IBase0 on type class G3_C1462`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1462`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1462`1<class BaseClass1>::ClassMethod4314<object>()
		calli default string(class G3_C1462`1<class BaseClass1>)
		ldstr "G3_C1462::ClassMethod4314.16296<System.Object>()"
		ldstr "class G3_C1462`1<class BaseClass1> on type class G3_C1462`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1462`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1462`1<class BaseClass1>::ClassMethod4313<object>()
		calli default string(class G3_C1462`1<class BaseClass1>)
		ldstr "G3_C1462::ClassMethod4313.16295<System.Object>()"
		ldstr "class G3_C1462`1<class BaseClass1> on type class G3_C1462`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1462`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1462`1<class BaseClass1>::ClassMethod4312()
		calli default string(class G3_C1462`1<class BaseClass1>)
		ldstr "G3_C1462::ClassMethod4312.16294()"
		ldstr "class G3_C1462`1<class BaseClass1> on type class G3_C1462`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1462`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1462`1<class BaseClass1>::Method7<object>()
		calli default string(class G3_C1462`1<class BaseClass1>)
		ldstr "G3_C1462::Method7.16292<System.Object>()"
		ldstr "class G3_C1462`1<class BaseClass1> on type class G3_C1462`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1462`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1462`1<class BaseClass1>::ClassMethod2331<object>()
		calli default string(class G3_C1462`1<class BaseClass1>)
		ldstr "G2_C488::ClassMethod2331.9447<System.Object>()"
		ldstr "class G3_C1462`1<class BaseClass1> on type class G3_C1462`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1462`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1462`1<class BaseClass1>::ClassMethod2330()
		calli default string(class G3_C1462`1<class BaseClass1>)
		ldstr "G3_C1462::ClassMethod2330.MI.16298()"
		ldstr "class G3_C1462`1<class BaseClass1> on type class G3_C1462`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1462`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1462`1<class BaseClass1>::ClassMethod2329()
		calli default string(class G3_C1462`1<class BaseClass1>)
		ldstr "G3_C1462::ClassMethod2329.MI.16297()"
		ldstr "class G3_C1462`1<class BaseClass1> on type class G3_C1462`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1462`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1462`1<class BaseClass1>::Method3<object>()
		calli default string(class G3_C1462`1<class BaseClass1>)
		ldstr "G2_C488::Method3.9443<System.Object>()"
		ldstr "class G3_C1462`1<class BaseClass1> on type class G3_C1462`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1462`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1462`1<class BaseClass1>::Method2<object>()
		calli default string(class G3_C1462`1<class BaseClass1>)
		ldstr "G2_C488::Method2.9441<System.Object>()"
		ldstr "class G3_C1462`1<class BaseClass1> on type class G3_C1462`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1462`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1462`1<class BaseClass1>::Method1()
		calli default string(class G3_C1462`1<class BaseClass1>)
		ldstr "G2_C488::Method1.9439()"
		ldstr "class G3_C1462`1<class BaseClass1> on type class G3_C1462`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1462`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1462`1<class BaseClass1>::Method0()
		calli default string(class G3_C1462`1<class BaseClass1>)
		ldstr "G2_C488::Method0.9437()"
		ldstr "class G3_C1462`1<class BaseClass1> on type class G3_C1462`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1462`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1462`1<class BaseClass1>::ClassMethod1339<object>()
		calli default string(class G3_C1462`1<class BaseClass1>)
		ldstr "G1_C9::ClassMethod1339.4843<System.Object>()"
		ldstr "class G3_C1462`1<class BaseClass1> on type class G3_C1462`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1462`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1462`1<class BaseClass1>::ClassMethod1338<object>()
		calli default string(class G3_C1462`1<class BaseClass1>)
		ldstr "G2_C488::ClassMethod1338.MI.9450<System.Object>()"
		ldstr "class G3_C1462`1<class BaseClass1> on type class G3_C1462`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1462`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1462`1<class BaseClass1>::ClassMethod1337()
		calli default string(class G3_C1462`1<class BaseClass1>)
		ldstr "G2_C488::ClassMethod1337.MI.9449()"
		ldstr "class G3_C1462`1<class BaseClass1> on type class G3_C1462`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1462`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1462`1<class BaseClass1>::ClassMethod1336()
		calli default string(class G3_C1462`1<class BaseClass1>)
		ldstr "G2_C488::ClassMethod1336.MI.9448()"
		ldstr "class G3_C1462`1<class BaseClass1> on type class G3_C1462`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1462`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1462`1<class BaseClass1>::Method6<object>()
		calli default string(class G3_C1462`1<class BaseClass1>)
		ldstr "G1_C9::Method6.4838<System.Object>()"
		ldstr "class G3_C1462`1<class BaseClass1> on type class G3_C1462`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1462`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1462`1<class BaseClass1>::Method5()
		calli default string(class G3_C1462`1<class BaseClass1>)
		ldstr "G1_C9::Method5.4836()"
		ldstr "class G3_C1462`1<class BaseClass1> on type class G3_C1462`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1462`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1462`1<class BaseClass1>::Method4()
		calli default string(class G3_C1462`1<class BaseClass1>)
		ldstr "G1_C9::Method4.4834()"
		ldstr "class G3_C1462`1<class BaseClass1> on type class G3_C1462`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1462`1<class BaseClass1>)
		ldstr "G3_C1462::Method7.MI.16293<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G3_C1462`1<class BaseClass1>"
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
		call void Generated990::MethodCallingTest()
		call void Generated990::ConstrainedCallsTest()
		call void Generated990::StructConstrainedInterfaceCallsTest()
		call void Generated990::CalliTest()
		ldc.i4 100
		ret
	}
}
