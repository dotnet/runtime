// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) .ver 4:0:0:0 }
.assembly extern TestFramework { .publickeytoken = ( B0 3F 5F 7F 11 D5 0A 3A )  }

//TYPES IN FORWARDER ASSEMBLIES:

//TEST ASSEMBLY:
.assembly Generated1175 { .hash algorithm 0x00008004 }
.module Generated1175.exe

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
.class public G3_C1647`1<T0> 
		extends class G2_C649`2<class BaseClass0,class BaseClass1>
{
	.method public hidebysig newslot virtual instance string ClassMethod4684() cil managed noinlining { 
		ldstr "G3_C1647::ClassMethod4684.17240()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod4685() cil managed noinlining { 
		ldstr "G3_C1647::ClassMethod4685.17241()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod4686<M0>() cil managed noinlining { 
		ldstr "G3_C1647::ClassMethod4686.17242<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod4687<M0>() cil managed noinlining { 
		ldstr "G3_C1647::ClassMethod4687.17243<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'G2_C649<class BaseClass0,class BaseClass1>.ClassMethod2656'() cil managed noinlining { 
		.override method instance string class G2_C649`2<class BaseClass0,class BaseClass1>::ClassMethod2656()
		ldstr "G3_C1647::ClassMethod2656.MI.17244()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'G2_C649<class BaseClass0,class BaseClass1>.ClassMethod2657'() cil managed noinlining { 
		.override method instance string class G2_C649`2<class BaseClass0,class BaseClass1>::ClassMethod2657()
		ldstr "G3_C1647::ClassMethod2657.MI.17245()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'G2_C649<class BaseClass0,class BaseClass1>.ClassMethod2658'<M0>() cil managed noinlining { 
		.override method instance string class G2_C649`2<class BaseClass0,class BaseClass1>::ClassMethod2658<[1]>()
		ldstr "G3_C1647::ClassMethod2658.MI.17246<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class G2_C649`2<class BaseClass0,class BaseClass1>::.ctor()
		ret
	}
}
.class public G2_C649`2<T0, T1> 
		extends class G1_C12`2<class BaseClass1,class BaseClass1>
		implements IBase0, class IBase2`2<class BaseClass0,!T0> 
{
	.method public hidebysig virtual instance string Method0() cil managed noinlining { 
		ldstr "G2_C649::Method0.10974()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase0.Method0'() cil managed noinlining { 
		.override method instance string IBase0::Method0()
		ldstr "G2_C649::Method0.MI.10975()"
		ret
	}
	.method public hidebysig newslot virtual instance string Method1() cil managed noinlining { 
		ldstr "G2_C649::Method1.10976()"
		ret
	}
	.method public hidebysig newslot virtual instance string Method2<M0>() cil managed noinlining { 
		ldstr "G2_C649::Method2.10977<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase0.Method2'<M0>() cil managed noinlining { 
		.override method instance string IBase0::Method2<[1]>()
		ldstr "G2_C649::Method2.MI.10978<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig virtual instance string Method3<M0>() cil managed noinlining { 
		ldstr "G2_C649::Method3.10979<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase0.Method3'<M0>() cil managed noinlining { 
		.override method instance string IBase0::Method3<[1]>()
		ldstr "G2_C649::Method3.MI.10980<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G2_C649::Method7.10981<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod2656() cil managed noinlining { 
		ldstr "G2_C649::ClassMethod2656.10982()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod2657() cil managed noinlining { 
		ldstr "G2_C649::ClassMethod2657.10983()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod2658<M0>() cil managed noinlining { 
		ldstr "G2_C649::ClassMethod2658.10984<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'G1_C12<class BaseClass1,class BaseClass1>.ClassMethod1346'<M0>() cil managed noinlining { 
		.override method instance string class G1_C12`2<class BaseClass1,class BaseClass1>::ClassMethod1346<[1]>()
		ldstr "G2_C649::ClassMethod1346.MI.10985<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'G1_C12<class BaseClass1,class BaseClass1>.ClassMethod1347'<M0>() cil managed noinlining { 
		.override method instance string class G1_C12`2<class BaseClass1,class BaseClass1>::ClassMethod1347<[1]>()
		ldstr "G2_C649::ClassMethod1347.MI.10986<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class G1_C12`2<class BaseClass1,class BaseClass1>::.ctor()
		ret
	}
}
.class public G1_C12`2<T0, T1> 
		implements class IBase2`2<!T0,!T1>, class IBase1`1<class BaseClass0> 
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
		.override method instance string class IBase2`2<!T0,!T1>::Method7<[1]>()
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
	.method public hidebysig newslot virtual instance string 'IBase1<class BaseClass0>.Method6'<M0>() cil managed noinlining { 
		.override method instance string class IBase1`1<class BaseClass0>::Method6<[1]>()
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
	.method static void M.G3_C1647.T<T0,(class G3_C1647`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 22
		.locals init (string[] actualResults)
		ldc.i4.s    17
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1647.T<T0,(class G3_C1647`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 17
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1647`1<!!T0>::ClassMethod1346<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1647`1<!!T0>::ClassMethod1347<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1647`1<!!T0>::ClassMethod2656()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1647`1<!!T0>::ClassMethod2657()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1647`1<!!T0>::ClassMethod2658<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1647`1<!!T0>::ClassMethod4684()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1647`1<!!T0>::ClassMethod4685()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1647`1<!!T0>::ClassMethod4686<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1647`1<!!T0>::ClassMethod4687<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1647`1<!!T0>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1647`1<!!T0>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1647`1<!!T0>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1647`1<!!T0>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     13
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1647`1<!!T0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     14
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1647`1<!!T0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     15
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1647`1<!!T0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     16
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1647`1<!!T0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1647.A<(class G3_C1647`1<class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 22
		.locals init (string[] actualResults)
		ldc.i4.s    17
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1647.A<(class G3_C1647`1<class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 17
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1647`1<class BaseClass0>::ClassMethod1346<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1647`1<class BaseClass0>::ClassMethod1347<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1647`1<class BaseClass0>::ClassMethod2656()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1647`1<class BaseClass0>::ClassMethod2657()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1647`1<class BaseClass0>::ClassMethod2658<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1647`1<class BaseClass0>::ClassMethod4684()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1647`1<class BaseClass0>::ClassMethod4685()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1647`1<class BaseClass0>::ClassMethod4686<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1647`1<class BaseClass0>::ClassMethod4687<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1647`1<class BaseClass0>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1647`1<class BaseClass0>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1647`1<class BaseClass0>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1647`1<class BaseClass0>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     13
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1647`1<class BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     14
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1647`1<class BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     15
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1647`1<class BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     16
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1647`1<class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1647.B<(class G3_C1647`1<class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 22
		.locals init (string[] actualResults)
		ldc.i4.s    17
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1647.B<(class G3_C1647`1<class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 17
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1647`1<class BaseClass1>::ClassMethod1346<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1647`1<class BaseClass1>::ClassMethod1347<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1647`1<class BaseClass1>::ClassMethod2656()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1647`1<class BaseClass1>::ClassMethod2657()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1647`1<class BaseClass1>::ClassMethod2658<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1647`1<class BaseClass1>::ClassMethod4684()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1647`1<class BaseClass1>::ClassMethod4685()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1647`1<class BaseClass1>::ClassMethod4686<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1647`1<class BaseClass1>::ClassMethod4687<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1647`1<class BaseClass1>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1647`1<class BaseClass1>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1647`1<class BaseClass1>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1647`1<class BaseClass1>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     13
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1647`1<class BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     14
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1647`1<class BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     15
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1647`1<class BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     16
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1647`1<class BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C649.T.T<T0,T1,(class G2_C649`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 18
		.locals init (string[] actualResults)
		ldc.i4.s    13
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C649.T.T<T0,T1,(class G2_C649`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 13
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C649`2<!!T0,!!T1>::ClassMethod1346<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C649`2<!!T0,!!T1>::ClassMethod1347<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C649`2<!!T0,!!T1>::ClassMethod2656()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C649`2<!!T0,!!T1>::ClassMethod2657()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C649`2<!!T0,!!T1>::ClassMethod2658<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C649`2<!!T0,!!T1>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C649`2<!!T0,!!T1>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C649`2<!!T0,!!T1>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C649`2<!!T0,!!T1>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C649`2<!!T0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C649`2<!!T0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C649`2<!!T0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C649`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C649.A.T<T1,(class G2_C649`2<class BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 18
		.locals init (string[] actualResults)
		ldc.i4.s    13
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C649.A.T<T1,(class G2_C649`2<class BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 13
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C649`2<class BaseClass0,!!T1>::ClassMethod1346<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C649`2<class BaseClass0,!!T1>::ClassMethod1347<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C649`2<class BaseClass0,!!T1>::ClassMethod2656()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C649`2<class BaseClass0,!!T1>::ClassMethod2657()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C649`2<class BaseClass0,!!T1>::ClassMethod2658<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C649`2<class BaseClass0,!!T1>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C649`2<class BaseClass0,!!T1>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C649`2<class BaseClass0,!!T1>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C649`2<class BaseClass0,!!T1>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C649`2<class BaseClass0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C649`2<class BaseClass0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C649`2<class BaseClass0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C649`2<class BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C649.A.A<(class G2_C649`2<class BaseClass0,class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 18
		.locals init (string[] actualResults)
		ldc.i4.s    13
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C649.A.A<(class G2_C649`2<class BaseClass0,class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 13
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C649`2<class BaseClass0,class BaseClass0>::ClassMethod1346<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C649`2<class BaseClass0,class BaseClass0>::ClassMethod1347<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C649`2<class BaseClass0,class BaseClass0>::ClassMethod2656()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C649`2<class BaseClass0,class BaseClass0>::ClassMethod2657()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C649`2<class BaseClass0,class BaseClass0>::ClassMethod2658<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C649`2<class BaseClass0,class BaseClass0>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C649`2<class BaseClass0,class BaseClass0>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C649`2<class BaseClass0,class BaseClass0>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C649`2<class BaseClass0,class BaseClass0>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C649`2<class BaseClass0,class BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C649`2<class BaseClass0,class BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C649`2<class BaseClass0,class BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C649`2<class BaseClass0,class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C649.A.B<(class G2_C649`2<class BaseClass0,class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 18
		.locals init (string[] actualResults)
		ldc.i4.s    13
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C649.A.B<(class G2_C649`2<class BaseClass0,class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 13
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C649`2<class BaseClass0,class BaseClass1>::ClassMethod1346<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C649`2<class BaseClass0,class BaseClass1>::ClassMethod1347<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C649`2<class BaseClass0,class BaseClass1>::ClassMethod2656()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C649`2<class BaseClass0,class BaseClass1>::ClassMethod2657()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C649`2<class BaseClass0,class BaseClass1>::ClassMethod2658<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C649`2<class BaseClass0,class BaseClass1>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C649`2<class BaseClass0,class BaseClass1>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C649`2<class BaseClass0,class BaseClass1>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C649`2<class BaseClass0,class BaseClass1>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C649`2<class BaseClass0,class BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C649`2<class BaseClass0,class BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C649`2<class BaseClass0,class BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C649`2<class BaseClass0,class BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C649.B.T<T1,(class G2_C649`2<class BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 18
		.locals init (string[] actualResults)
		ldc.i4.s    13
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C649.B.T<T1,(class G2_C649`2<class BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 13
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C649`2<class BaseClass1,!!T1>::ClassMethod1346<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C649`2<class BaseClass1,!!T1>::ClassMethod1347<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C649`2<class BaseClass1,!!T1>::ClassMethod2656()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C649`2<class BaseClass1,!!T1>::ClassMethod2657()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C649`2<class BaseClass1,!!T1>::ClassMethod2658<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C649`2<class BaseClass1,!!T1>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C649`2<class BaseClass1,!!T1>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C649`2<class BaseClass1,!!T1>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C649`2<class BaseClass1,!!T1>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C649`2<class BaseClass1,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C649`2<class BaseClass1,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C649`2<class BaseClass1,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C649`2<class BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C649.B.A<(class G2_C649`2<class BaseClass1,class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 18
		.locals init (string[] actualResults)
		ldc.i4.s    13
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C649.B.A<(class G2_C649`2<class BaseClass1,class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 13
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C649`2<class BaseClass1,class BaseClass0>::ClassMethod1346<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C649`2<class BaseClass1,class BaseClass0>::ClassMethod1347<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C649`2<class BaseClass1,class BaseClass0>::ClassMethod2656()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C649`2<class BaseClass1,class BaseClass0>::ClassMethod2657()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C649`2<class BaseClass1,class BaseClass0>::ClassMethod2658<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C649`2<class BaseClass1,class BaseClass0>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C649`2<class BaseClass1,class BaseClass0>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C649`2<class BaseClass1,class BaseClass0>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C649`2<class BaseClass1,class BaseClass0>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C649`2<class BaseClass1,class BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C649`2<class BaseClass1,class BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C649`2<class BaseClass1,class BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C649`2<class BaseClass1,class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C649.B.B<(class G2_C649`2<class BaseClass1,class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 18
		.locals init (string[] actualResults)
		ldc.i4.s    13
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C649.B.B<(class G2_C649`2<class BaseClass1,class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 13
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C649`2<class BaseClass1,class BaseClass1>::ClassMethod1346<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C649`2<class BaseClass1,class BaseClass1>::ClassMethod1347<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C649`2<class BaseClass1,class BaseClass1>::ClassMethod2656()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C649`2<class BaseClass1,class BaseClass1>::ClassMethod2657()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C649`2<class BaseClass1,class BaseClass1>::ClassMethod2658<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C649`2<class BaseClass1,class BaseClass1>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C649`2<class BaseClass1,class BaseClass1>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C649`2<class BaseClass1,class BaseClass1>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C649`2<class BaseClass1,class BaseClass1>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C649`2<class BaseClass1,class BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C649`2<class BaseClass1,class BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C649`2<class BaseClass1,class BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C649`2<class BaseClass1,class BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C12.T.T<T0,T1,(class G1_C12`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C12.T.T<T0,T1,(class G1_C12`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C12`2<!!T0,!!T1>::ClassMethod1346<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C12`2<!!T0,!!T1>::ClassMethod1347<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C12`2<!!T0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C12`2<!!T0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C12`2<!!T0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C12`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C12.A.T<T1,(class G1_C12`2<class BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C12.A.T<T1,(class G1_C12`2<class BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C12`2<class BaseClass0,!!T1>::ClassMethod1346<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C12`2<class BaseClass0,!!T1>::ClassMethod1347<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C12`2<class BaseClass0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C12`2<class BaseClass0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C12`2<class BaseClass0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C12`2<class BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C12.A.A<(class G1_C12`2<class BaseClass0,class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C12.A.A<(class G1_C12`2<class BaseClass0,class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C12`2<class BaseClass0,class BaseClass0>::ClassMethod1346<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C12`2<class BaseClass0,class BaseClass0>::ClassMethod1347<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C12`2<class BaseClass0,class BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C12`2<class BaseClass0,class BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C12`2<class BaseClass0,class BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C12`2<class BaseClass0,class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C12.A.B<(class G1_C12`2<class BaseClass0,class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C12.A.B<(class G1_C12`2<class BaseClass0,class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C12`2<class BaseClass0,class BaseClass1>::ClassMethod1346<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C12`2<class BaseClass0,class BaseClass1>::ClassMethod1347<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C12`2<class BaseClass0,class BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C12`2<class BaseClass0,class BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C12`2<class BaseClass0,class BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C12`2<class BaseClass0,class BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C12.B.T<T1,(class G1_C12`2<class BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C12.B.T<T1,(class G1_C12`2<class BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C12`2<class BaseClass1,!!T1>::ClassMethod1346<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C12`2<class BaseClass1,!!T1>::ClassMethod1347<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C12`2<class BaseClass1,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C12`2<class BaseClass1,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C12`2<class BaseClass1,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C12`2<class BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C12.B.A<(class G1_C12`2<class BaseClass1,class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C12.B.A<(class G1_C12`2<class BaseClass1,class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C12`2<class BaseClass1,class BaseClass0>::ClassMethod1346<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C12`2<class BaseClass1,class BaseClass0>::ClassMethod1347<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C12`2<class BaseClass1,class BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C12`2<class BaseClass1,class BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C12`2<class BaseClass1,class BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C12`2<class BaseClass1,class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C12.B.B<(class G1_C12`2<class BaseClass1,class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C12.B.B<(class G1_C12`2<class BaseClass1,class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C12`2<class BaseClass1,class BaseClass1>::ClassMethod1346<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C12`2<class BaseClass1,class BaseClass1>::ClassMethod1347<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C12`2<class BaseClass1,class BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C12`2<class BaseClass1,class BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C12`2<class BaseClass1,class BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C12`2<class BaseClass1,class BaseClass1>::Method7<object>()
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
		newobj instance void class G3_C1647`1<class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C12`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C12`2<class BaseClass1,class BaseClass1>::ClassMethod1347<object>()
		ldstr "G2_C649::ClassMethod1347.MI.10986<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass1> on type class G3_C1647`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C12`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C12`2<class BaseClass1,class BaseClass1>::ClassMethod1346<object>()
		ldstr "G2_C649::ClassMethod1346.MI.10985<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass1> on type class G3_C1647`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C12`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C12`2<class BaseClass1,class BaseClass1>::Method6<object>()
		ldstr "G1_C12::Method6.4867<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass1> on type class G3_C1647`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C12`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C12`2<class BaseClass1,class BaseClass1>::Method5()
		ldstr "G1_C12::Method5.4866()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass1> on type class G3_C1647`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C12`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C12`2<class BaseClass1,class BaseClass1>::Method4()
		ldstr "G1_C12::Method4.4865()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass1> on type class G3_C1647`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C12`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C12`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C12::Method7.4863<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass1> on type class G3_C1647`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G3_C1647`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G1_C12::Method4.4865()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1647`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G1_C12::Method5.4866()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1647`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G1_C12::Method6.MI.4868<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1647`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C649::Method7.10981<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1647`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C649`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C649`2<class BaseClass0,class BaseClass1>::ClassMethod2658<object>()
		ldstr "G3_C1647::ClassMethod2658.MI.17246<System.Object>()"
		ldstr "class G2_C649`2<class BaseClass0,class BaseClass1> on type class G3_C1647`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C649`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C649`2<class BaseClass0,class BaseClass1>::ClassMethod2657()
		ldstr "G3_C1647::ClassMethod2657.MI.17245()"
		ldstr "class G2_C649`2<class BaseClass0,class BaseClass1> on type class G3_C1647`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C649`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C649`2<class BaseClass0,class BaseClass1>::ClassMethod2656()
		ldstr "G3_C1647::ClassMethod2656.MI.17244()"
		ldstr "class G2_C649`2<class BaseClass0,class BaseClass1> on type class G3_C1647`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C649`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C649`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C649::Method7.10981<System.Object>()"
		ldstr "class G2_C649`2<class BaseClass0,class BaseClass1> on type class G3_C1647`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C649`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C649`2<class BaseClass0,class BaseClass1>::Method3<object>()
		ldstr "G2_C649::Method3.10979<System.Object>()"
		ldstr "class G2_C649`2<class BaseClass0,class BaseClass1> on type class G3_C1647`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C649`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C649`2<class BaseClass0,class BaseClass1>::Method2<object>()
		ldstr "G2_C649::Method2.10977<System.Object>()"
		ldstr "class G2_C649`2<class BaseClass0,class BaseClass1> on type class G3_C1647`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C649`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C649`2<class BaseClass0,class BaseClass1>::Method1()
		ldstr "G2_C649::Method1.10976()"
		ldstr "class G2_C649`2<class BaseClass0,class BaseClass1> on type class G3_C1647`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C649`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C649`2<class BaseClass0,class BaseClass1>::Method0()
		ldstr "G2_C649::Method0.10974()"
		ldstr "class G2_C649`2<class BaseClass0,class BaseClass1> on type class G3_C1647`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C649`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C649`2<class BaseClass0,class BaseClass1>::ClassMethod1347<object>()
		ldstr "G2_C649::ClassMethod1347.MI.10986<System.Object>()"
		ldstr "class G2_C649`2<class BaseClass0,class BaseClass1> on type class G3_C1647`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C649`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C649`2<class BaseClass0,class BaseClass1>::ClassMethod1346<object>()
		ldstr "G2_C649::ClassMethod1346.MI.10985<System.Object>()"
		ldstr "class G2_C649`2<class BaseClass0,class BaseClass1> on type class G3_C1647`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C649`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C649`2<class BaseClass0,class BaseClass1>::Method6<object>()
		ldstr "G1_C12::Method6.4867<System.Object>()"
		ldstr "class G2_C649`2<class BaseClass0,class BaseClass1> on type class G3_C1647`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C649`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C649`2<class BaseClass0,class BaseClass1>::Method5()
		ldstr "G1_C12::Method5.4866()"
		ldstr "class G2_C649`2<class BaseClass0,class BaseClass1> on type class G3_C1647`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C649`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C649`2<class BaseClass0,class BaseClass1>::Method4()
		ldstr "G1_C12::Method4.4865()"
		ldstr "class G2_C649`2<class BaseClass0,class BaseClass1> on type class G3_C1647`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string IBase0::Method0()
		ldstr "G2_C649::Method0.MI.10975()"
		ldstr "IBase0 on type class G3_C1647`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method1()
		ldstr "G2_C649::Method1.10976()"
		ldstr "IBase0 on type class G3_C1647`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method2<object>()
		ldstr "G2_C649::Method2.MI.10978<System.Object>()"
		ldstr "IBase0 on type class G3_C1647`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method3<object>()
		ldstr "G2_C649::Method3.MI.10980<System.Object>()"
		ldstr "IBase0 on type class G3_C1647`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G2_C649::Method7.10981<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G3_C1647`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G3_C1647`1<class BaseClass0>
		callvirt instance string class G3_C1647`1<class BaseClass0>::ClassMethod4687<object>()
		ldstr "G3_C1647::ClassMethod4687.17243<System.Object>()"
		ldstr "class G3_C1647`1<class BaseClass0> on type class G3_C1647`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1647`1<class BaseClass0>
		callvirt instance string class G3_C1647`1<class BaseClass0>::ClassMethod4686<object>()
		ldstr "G3_C1647::ClassMethod4686.17242<System.Object>()"
		ldstr "class G3_C1647`1<class BaseClass0> on type class G3_C1647`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1647`1<class BaseClass0>
		callvirt instance string class G3_C1647`1<class BaseClass0>::ClassMethod4685()
		ldstr "G3_C1647::ClassMethod4685.17241()"
		ldstr "class G3_C1647`1<class BaseClass0> on type class G3_C1647`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1647`1<class BaseClass0>
		callvirt instance string class G3_C1647`1<class BaseClass0>::ClassMethod4684()
		ldstr "G3_C1647::ClassMethod4684.17240()"
		ldstr "class G3_C1647`1<class BaseClass0> on type class G3_C1647`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1647`1<class BaseClass0>
		callvirt instance string class G3_C1647`1<class BaseClass0>::ClassMethod2658<object>()
		ldstr "G3_C1647::ClassMethod2658.MI.17246<System.Object>()"
		ldstr "class G3_C1647`1<class BaseClass0> on type class G3_C1647`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1647`1<class BaseClass0>
		callvirt instance string class G3_C1647`1<class BaseClass0>::ClassMethod2657()
		ldstr "G3_C1647::ClassMethod2657.MI.17245()"
		ldstr "class G3_C1647`1<class BaseClass0> on type class G3_C1647`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1647`1<class BaseClass0>
		callvirt instance string class G3_C1647`1<class BaseClass0>::ClassMethod2656()
		ldstr "G3_C1647::ClassMethod2656.MI.17244()"
		ldstr "class G3_C1647`1<class BaseClass0> on type class G3_C1647`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1647`1<class BaseClass0>
		callvirt instance string class G3_C1647`1<class BaseClass0>::Method7<object>()
		ldstr "G2_C649::Method7.10981<System.Object>()"
		ldstr "class G3_C1647`1<class BaseClass0> on type class G3_C1647`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1647`1<class BaseClass0>
		callvirt instance string class G3_C1647`1<class BaseClass0>::Method3<object>()
		ldstr "G2_C649::Method3.10979<System.Object>()"
		ldstr "class G3_C1647`1<class BaseClass0> on type class G3_C1647`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1647`1<class BaseClass0>
		callvirt instance string class G3_C1647`1<class BaseClass0>::Method2<object>()
		ldstr "G2_C649::Method2.10977<System.Object>()"
		ldstr "class G3_C1647`1<class BaseClass0> on type class G3_C1647`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1647`1<class BaseClass0>
		callvirt instance string class G3_C1647`1<class BaseClass0>::Method1()
		ldstr "G2_C649::Method1.10976()"
		ldstr "class G3_C1647`1<class BaseClass0> on type class G3_C1647`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1647`1<class BaseClass0>
		callvirt instance string class G3_C1647`1<class BaseClass0>::Method0()
		ldstr "G2_C649::Method0.10974()"
		ldstr "class G3_C1647`1<class BaseClass0> on type class G3_C1647`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1647`1<class BaseClass0>
		callvirt instance string class G3_C1647`1<class BaseClass0>::ClassMethod1347<object>()
		ldstr "G2_C649::ClassMethod1347.MI.10986<System.Object>()"
		ldstr "class G3_C1647`1<class BaseClass0> on type class G3_C1647`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1647`1<class BaseClass0>
		callvirt instance string class G3_C1647`1<class BaseClass0>::ClassMethod1346<object>()
		ldstr "G2_C649::ClassMethod1346.MI.10985<System.Object>()"
		ldstr "class G3_C1647`1<class BaseClass0> on type class G3_C1647`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1647`1<class BaseClass0>
		callvirt instance string class G3_C1647`1<class BaseClass0>::Method6<object>()
		ldstr "G1_C12::Method6.4867<System.Object>()"
		ldstr "class G3_C1647`1<class BaseClass0> on type class G3_C1647`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1647`1<class BaseClass0>
		callvirt instance string class G3_C1647`1<class BaseClass0>::Method5()
		ldstr "G1_C12::Method5.4866()"
		ldstr "class G3_C1647`1<class BaseClass0> on type class G3_C1647`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1647`1<class BaseClass0>
		callvirt instance string class G3_C1647`1<class BaseClass0>::Method4()
		ldstr "G1_C12::Method4.4865()"
		ldstr "class G3_C1647`1<class BaseClass0> on type class G3_C1647`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G3_C1647`1<class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C12`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C12`2<class BaseClass1,class BaseClass1>::ClassMethod1347<object>()
		ldstr "G2_C649::ClassMethod1347.MI.10986<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass1> on type class G3_C1647`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C12`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C12`2<class BaseClass1,class BaseClass1>::ClassMethod1346<object>()
		ldstr "G2_C649::ClassMethod1346.MI.10985<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass1> on type class G3_C1647`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C12`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C12`2<class BaseClass1,class BaseClass1>::Method6<object>()
		ldstr "G1_C12::Method6.4867<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass1> on type class G3_C1647`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C12`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C12`2<class BaseClass1,class BaseClass1>::Method5()
		ldstr "G1_C12::Method5.4866()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass1> on type class G3_C1647`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C12`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C12`2<class BaseClass1,class BaseClass1>::Method4()
		ldstr "G1_C12::Method4.4865()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass1> on type class G3_C1647`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C12`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C12`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C12::Method7.4863<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass1> on type class G3_C1647`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G3_C1647`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G1_C12::Method4.4865()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1647`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G1_C12::Method5.4866()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1647`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G1_C12::Method6.MI.4868<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1647`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C649::Method7.10981<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1647`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C649`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C649`2<class BaseClass0,class BaseClass1>::ClassMethod2658<object>()
		ldstr "G3_C1647::ClassMethod2658.MI.17246<System.Object>()"
		ldstr "class G2_C649`2<class BaseClass0,class BaseClass1> on type class G3_C1647`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C649`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C649`2<class BaseClass0,class BaseClass1>::ClassMethod2657()
		ldstr "G3_C1647::ClassMethod2657.MI.17245()"
		ldstr "class G2_C649`2<class BaseClass0,class BaseClass1> on type class G3_C1647`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C649`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C649`2<class BaseClass0,class BaseClass1>::ClassMethod2656()
		ldstr "G3_C1647::ClassMethod2656.MI.17244()"
		ldstr "class G2_C649`2<class BaseClass0,class BaseClass1> on type class G3_C1647`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C649`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C649`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C649::Method7.10981<System.Object>()"
		ldstr "class G2_C649`2<class BaseClass0,class BaseClass1> on type class G3_C1647`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C649`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C649`2<class BaseClass0,class BaseClass1>::Method3<object>()
		ldstr "G2_C649::Method3.10979<System.Object>()"
		ldstr "class G2_C649`2<class BaseClass0,class BaseClass1> on type class G3_C1647`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C649`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C649`2<class BaseClass0,class BaseClass1>::Method2<object>()
		ldstr "G2_C649::Method2.10977<System.Object>()"
		ldstr "class G2_C649`2<class BaseClass0,class BaseClass1> on type class G3_C1647`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C649`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C649`2<class BaseClass0,class BaseClass1>::Method1()
		ldstr "G2_C649::Method1.10976()"
		ldstr "class G2_C649`2<class BaseClass0,class BaseClass1> on type class G3_C1647`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C649`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C649`2<class BaseClass0,class BaseClass1>::Method0()
		ldstr "G2_C649::Method0.10974()"
		ldstr "class G2_C649`2<class BaseClass0,class BaseClass1> on type class G3_C1647`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C649`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C649`2<class BaseClass0,class BaseClass1>::ClassMethod1347<object>()
		ldstr "G2_C649::ClassMethod1347.MI.10986<System.Object>()"
		ldstr "class G2_C649`2<class BaseClass0,class BaseClass1> on type class G3_C1647`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C649`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C649`2<class BaseClass0,class BaseClass1>::ClassMethod1346<object>()
		ldstr "G2_C649::ClassMethod1346.MI.10985<System.Object>()"
		ldstr "class G2_C649`2<class BaseClass0,class BaseClass1> on type class G3_C1647`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C649`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C649`2<class BaseClass0,class BaseClass1>::Method6<object>()
		ldstr "G1_C12::Method6.4867<System.Object>()"
		ldstr "class G2_C649`2<class BaseClass0,class BaseClass1> on type class G3_C1647`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C649`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C649`2<class BaseClass0,class BaseClass1>::Method5()
		ldstr "G1_C12::Method5.4866()"
		ldstr "class G2_C649`2<class BaseClass0,class BaseClass1> on type class G3_C1647`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C649`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C649`2<class BaseClass0,class BaseClass1>::Method4()
		ldstr "G1_C12::Method4.4865()"
		ldstr "class G2_C649`2<class BaseClass0,class BaseClass1> on type class G3_C1647`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string IBase0::Method0()
		ldstr "G2_C649::Method0.MI.10975()"
		ldstr "IBase0 on type class G3_C1647`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method1()
		ldstr "G2_C649::Method1.10976()"
		ldstr "IBase0 on type class G3_C1647`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method2<object>()
		ldstr "G2_C649::Method2.MI.10978<System.Object>()"
		ldstr "IBase0 on type class G3_C1647`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method3<object>()
		ldstr "G2_C649::Method3.MI.10980<System.Object>()"
		ldstr "IBase0 on type class G3_C1647`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G2_C649::Method7.10981<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G3_C1647`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G3_C1647`1<class BaseClass1>
		callvirt instance string class G3_C1647`1<class BaseClass1>::ClassMethod4687<object>()
		ldstr "G3_C1647::ClassMethod4687.17243<System.Object>()"
		ldstr "class G3_C1647`1<class BaseClass1> on type class G3_C1647`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1647`1<class BaseClass1>
		callvirt instance string class G3_C1647`1<class BaseClass1>::ClassMethod4686<object>()
		ldstr "G3_C1647::ClassMethod4686.17242<System.Object>()"
		ldstr "class G3_C1647`1<class BaseClass1> on type class G3_C1647`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1647`1<class BaseClass1>
		callvirt instance string class G3_C1647`1<class BaseClass1>::ClassMethod4685()
		ldstr "G3_C1647::ClassMethod4685.17241()"
		ldstr "class G3_C1647`1<class BaseClass1> on type class G3_C1647`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1647`1<class BaseClass1>
		callvirt instance string class G3_C1647`1<class BaseClass1>::ClassMethod4684()
		ldstr "G3_C1647::ClassMethod4684.17240()"
		ldstr "class G3_C1647`1<class BaseClass1> on type class G3_C1647`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1647`1<class BaseClass1>
		callvirt instance string class G3_C1647`1<class BaseClass1>::ClassMethod2658<object>()
		ldstr "G3_C1647::ClassMethod2658.MI.17246<System.Object>()"
		ldstr "class G3_C1647`1<class BaseClass1> on type class G3_C1647`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1647`1<class BaseClass1>
		callvirt instance string class G3_C1647`1<class BaseClass1>::ClassMethod2657()
		ldstr "G3_C1647::ClassMethod2657.MI.17245()"
		ldstr "class G3_C1647`1<class BaseClass1> on type class G3_C1647`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1647`1<class BaseClass1>
		callvirt instance string class G3_C1647`1<class BaseClass1>::ClassMethod2656()
		ldstr "G3_C1647::ClassMethod2656.MI.17244()"
		ldstr "class G3_C1647`1<class BaseClass1> on type class G3_C1647`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1647`1<class BaseClass1>
		callvirt instance string class G3_C1647`1<class BaseClass1>::Method7<object>()
		ldstr "G2_C649::Method7.10981<System.Object>()"
		ldstr "class G3_C1647`1<class BaseClass1> on type class G3_C1647`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1647`1<class BaseClass1>
		callvirt instance string class G3_C1647`1<class BaseClass1>::Method3<object>()
		ldstr "G2_C649::Method3.10979<System.Object>()"
		ldstr "class G3_C1647`1<class BaseClass1> on type class G3_C1647`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1647`1<class BaseClass1>
		callvirt instance string class G3_C1647`1<class BaseClass1>::Method2<object>()
		ldstr "G2_C649::Method2.10977<System.Object>()"
		ldstr "class G3_C1647`1<class BaseClass1> on type class G3_C1647`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1647`1<class BaseClass1>
		callvirt instance string class G3_C1647`1<class BaseClass1>::Method1()
		ldstr "G2_C649::Method1.10976()"
		ldstr "class G3_C1647`1<class BaseClass1> on type class G3_C1647`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1647`1<class BaseClass1>
		callvirt instance string class G3_C1647`1<class BaseClass1>::Method0()
		ldstr "G2_C649::Method0.10974()"
		ldstr "class G3_C1647`1<class BaseClass1> on type class G3_C1647`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1647`1<class BaseClass1>
		callvirt instance string class G3_C1647`1<class BaseClass1>::ClassMethod1347<object>()
		ldstr "G2_C649::ClassMethod1347.MI.10986<System.Object>()"
		ldstr "class G3_C1647`1<class BaseClass1> on type class G3_C1647`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1647`1<class BaseClass1>
		callvirt instance string class G3_C1647`1<class BaseClass1>::ClassMethod1346<object>()
		ldstr "G2_C649::ClassMethod1346.MI.10985<System.Object>()"
		ldstr "class G3_C1647`1<class BaseClass1> on type class G3_C1647`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1647`1<class BaseClass1>
		callvirt instance string class G3_C1647`1<class BaseClass1>::Method6<object>()
		ldstr "G1_C12::Method6.4867<System.Object>()"
		ldstr "class G3_C1647`1<class BaseClass1> on type class G3_C1647`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1647`1<class BaseClass1>
		callvirt instance string class G3_C1647`1<class BaseClass1>::Method5()
		ldstr "G1_C12::Method5.4866()"
		ldstr "class G3_C1647`1<class BaseClass1> on type class G3_C1647`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1647`1<class BaseClass1>
		callvirt instance string class G3_C1647`1<class BaseClass1>::Method4()
		ldstr "G1_C12::Method4.4865()"
		ldstr "class G3_C1647`1<class BaseClass1> on type class G3_C1647`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G2_C649`2<class BaseClass0,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C12`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C12`2<class BaseClass1,class BaseClass1>::ClassMethod1347<object>()
		ldstr "G2_C649::ClassMethod1347.MI.10986<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass1> on type class G2_C649`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C12`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C12`2<class BaseClass1,class BaseClass1>::ClassMethod1346<object>()
		ldstr "G2_C649::ClassMethod1346.MI.10985<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass1> on type class G2_C649`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C12`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C12`2<class BaseClass1,class BaseClass1>::Method6<object>()
		ldstr "G1_C12::Method6.4867<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass1> on type class G2_C649`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C12`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C12`2<class BaseClass1,class BaseClass1>::Method5()
		ldstr "G1_C12::Method5.4866()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass1> on type class G2_C649`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C12`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C12`2<class BaseClass1,class BaseClass1>::Method4()
		ldstr "G1_C12::Method4.4865()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass1> on type class G2_C649`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C12`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C12`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C12::Method7.4863<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass1> on type class G2_C649`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C649`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G1_C12::Method4.4865()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C649`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G1_C12::Method5.4866()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C649`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G1_C12::Method6.MI.4868<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C649`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C649::Method7.10981<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C649`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C649`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C649`2<class BaseClass0,class BaseClass0>::ClassMethod2658<object>()
		ldstr "G2_C649::ClassMethod2658.10984<System.Object>()"
		ldstr "class G2_C649`2<class BaseClass0,class BaseClass0> on type class G2_C649`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C649`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C649`2<class BaseClass0,class BaseClass0>::ClassMethod2657()
		ldstr "G2_C649::ClassMethod2657.10983()"
		ldstr "class G2_C649`2<class BaseClass0,class BaseClass0> on type class G2_C649`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C649`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C649`2<class BaseClass0,class BaseClass0>::ClassMethod2656()
		ldstr "G2_C649::ClassMethod2656.10982()"
		ldstr "class G2_C649`2<class BaseClass0,class BaseClass0> on type class G2_C649`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C649`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C649`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G2_C649::Method7.10981<System.Object>()"
		ldstr "class G2_C649`2<class BaseClass0,class BaseClass0> on type class G2_C649`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C649`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C649`2<class BaseClass0,class BaseClass0>::Method3<object>()
		ldstr "G2_C649::Method3.10979<System.Object>()"
		ldstr "class G2_C649`2<class BaseClass0,class BaseClass0> on type class G2_C649`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C649`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C649`2<class BaseClass0,class BaseClass0>::Method2<object>()
		ldstr "G2_C649::Method2.10977<System.Object>()"
		ldstr "class G2_C649`2<class BaseClass0,class BaseClass0> on type class G2_C649`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C649`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C649`2<class BaseClass0,class BaseClass0>::Method1()
		ldstr "G2_C649::Method1.10976()"
		ldstr "class G2_C649`2<class BaseClass0,class BaseClass0> on type class G2_C649`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C649`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C649`2<class BaseClass0,class BaseClass0>::Method0()
		ldstr "G2_C649::Method0.10974()"
		ldstr "class G2_C649`2<class BaseClass0,class BaseClass0> on type class G2_C649`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C649`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C649`2<class BaseClass0,class BaseClass0>::ClassMethod1347<object>()
		ldstr "G2_C649::ClassMethod1347.MI.10986<System.Object>()"
		ldstr "class G2_C649`2<class BaseClass0,class BaseClass0> on type class G2_C649`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C649`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C649`2<class BaseClass0,class BaseClass0>::ClassMethod1346<object>()
		ldstr "G2_C649::ClassMethod1346.MI.10985<System.Object>()"
		ldstr "class G2_C649`2<class BaseClass0,class BaseClass0> on type class G2_C649`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C649`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C649`2<class BaseClass0,class BaseClass0>::Method6<object>()
		ldstr "G1_C12::Method6.4867<System.Object>()"
		ldstr "class G2_C649`2<class BaseClass0,class BaseClass0> on type class G2_C649`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C649`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C649`2<class BaseClass0,class BaseClass0>::Method5()
		ldstr "G1_C12::Method5.4866()"
		ldstr "class G2_C649`2<class BaseClass0,class BaseClass0> on type class G2_C649`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C649`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C649`2<class BaseClass0,class BaseClass0>::Method4()
		ldstr "G1_C12::Method4.4865()"
		ldstr "class G2_C649`2<class BaseClass0,class BaseClass0> on type class G2_C649`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string IBase0::Method0()
		ldstr "G2_C649::Method0.MI.10975()"
		ldstr "IBase0 on type class G2_C649`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method1()
		ldstr "G2_C649::Method1.10976()"
		ldstr "IBase0 on type class G2_C649`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method2<object>()
		ldstr "G2_C649::Method2.MI.10978<System.Object>()"
		ldstr "IBase0 on type class G2_C649`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method3<object>()
		ldstr "G2_C649::Method3.MI.10980<System.Object>()"
		ldstr "IBase0 on type class G2_C649`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G2_C649::Method7.10981<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G2_C649`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G2_C649`2<class BaseClass0,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C12`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C12`2<class BaseClass1,class BaseClass1>::ClassMethod1347<object>()
		ldstr "G2_C649::ClassMethod1347.MI.10986<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass1> on type class G2_C649`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C12`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C12`2<class BaseClass1,class BaseClass1>::ClassMethod1346<object>()
		ldstr "G2_C649::ClassMethod1346.MI.10985<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass1> on type class G2_C649`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C12`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C12`2<class BaseClass1,class BaseClass1>::Method6<object>()
		ldstr "G1_C12::Method6.4867<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass1> on type class G2_C649`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C12`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C12`2<class BaseClass1,class BaseClass1>::Method5()
		ldstr "G1_C12::Method5.4866()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass1> on type class G2_C649`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C12`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C12`2<class BaseClass1,class BaseClass1>::Method4()
		ldstr "G1_C12::Method4.4865()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass1> on type class G2_C649`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C12`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C12`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C12::Method7.4863<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass1> on type class G2_C649`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C649`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G1_C12::Method4.4865()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C649`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G1_C12::Method5.4866()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C649`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G1_C12::Method6.MI.4868<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C649`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C649::Method7.10981<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C649`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C649`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C649`2<class BaseClass0,class BaseClass1>::ClassMethod2658<object>()
		ldstr "G2_C649::ClassMethod2658.10984<System.Object>()"
		ldstr "class G2_C649`2<class BaseClass0,class BaseClass1> on type class G2_C649`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C649`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C649`2<class BaseClass0,class BaseClass1>::ClassMethod2657()
		ldstr "G2_C649::ClassMethod2657.10983()"
		ldstr "class G2_C649`2<class BaseClass0,class BaseClass1> on type class G2_C649`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C649`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C649`2<class BaseClass0,class BaseClass1>::ClassMethod2656()
		ldstr "G2_C649::ClassMethod2656.10982()"
		ldstr "class G2_C649`2<class BaseClass0,class BaseClass1> on type class G2_C649`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C649`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C649`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C649::Method7.10981<System.Object>()"
		ldstr "class G2_C649`2<class BaseClass0,class BaseClass1> on type class G2_C649`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C649`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C649`2<class BaseClass0,class BaseClass1>::Method3<object>()
		ldstr "G2_C649::Method3.10979<System.Object>()"
		ldstr "class G2_C649`2<class BaseClass0,class BaseClass1> on type class G2_C649`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C649`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C649`2<class BaseClass0,class BaseClass1>::Method2<object>()
		ldstr "G2_C649::Method2.10977<System.Object>()"
		ldstr "class G2_C649`2<class BaseClass0,class BaseClass1> on type class G2_C649`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C649`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C649`2<class BaseClass0,class BaseClass1>::Method1()
		ldstr "G2_C649::Method1.10976()"
		ldstr "class G2_C649`2<class BaseClass0,class BaseClass1> on type class G2_C649`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C649`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C649`2<class BaseClass0,class BaseClass1>::Method0()
		ldstr "G2_C649::Method0.10974()"
		ldstr "class G2_C649`2<class BaseClass0,class BaseClass1> on type class G2_C649`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C649`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C649`2<class BaseClass0,class BaseClass1>::ClassMethod1347<object>()
		ldstr "G2_C649::ClassMethod1347.MI.10986<System.Object>()"
		ldstr "class G2_C649`2<class BaseClass0,class BaseClass1> on type class G2_C649`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C649`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C649`2<class BaseClass0,class BaseClass1>::ClassMethod1346<object>()
		ldstr "G2_C649::ClassMethod1346.MI.10985<System.Object>()"
		ldstr "class G2_C649`2<class BaseClass0,class BaseClass1> on type class G2_C649`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C649`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C649`2<class BaseClass0,class BaseClass1>::Method6<object>()
		ldstr "G1_C12::Method6.4867<System.Object>()"
		ldstr "class G2_C649`2<class BaseClass0,class BaseClass1> on type class G2_C649`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C649`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C649`2<class BaseClass0,class BaseClass1>::Method5()
		ldstr "G1_C12::Method5.4866()"
		ldstr "class G2_C649`2<class BaseClass0,class BaseClass1> on type class G2_C649`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C649`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C649`2<class BaseClass0,class BaseClass1>::Method4()
		ldstr "G1_C12::Method4.4865()"
		ldstr "class G2_C649`2<class BaseClass0,class BaseClass1> on type class G2_C649`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string IBase0::Method0()
		ldstr "G2_C649::Method0.MI.10975()"
		ldstr "IBase0 on type class G2_C649`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method1()
		ldstr "G2_C649::Method1.10976()"
		ldstr "IBase0 on type class G2_C649`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method2<object>()
		ldstr "G2_C649::Method2.MI.10978<System.Object>()"
		ldstr "IBase0 on type class G2_C649`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method3<object>()
		ldstr "G2_C649::Method3.MI.10980<System.Object>()"
		ldstr "IBase0 on type class G2_C649`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G2_C649::Method7.10981<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G2_C649`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G2_C649`2<class BaseClass1,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C12`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C12`2<class BaseClass1,class BaseClass1>::ClassMethod1347<object>()
		ldstr "G2_C649::ClassMethod1347.MI.10986<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass1> on type class G2_C649`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C12`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C12`2<class BaseClass1,class BaseClass1>::ClassMethod1346<object>()
		ldstr "G2_C649::ClassMethod1346.MI.10985<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass1> on type class G2_C649`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C12`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C12`2<class BaseClass1,class BaseClass1>::Method6<object>()
		ldstr "G1_C12::Method6.4867<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass1> on type class G2_C649`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C12`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C12`2<class BaseClass1,class BaseClass1>::Method5()
		ldstr "G1_C12::Method5.4866()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass1> on type class G2_C649`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C12`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C12`2<class BaseClass1,class BaseClass1>::Method4()
		ldstr "G1_C12::Method4.4865()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass1> on type class G2_C649`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C12`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C12`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C12::Method7.4863<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass1> on type class G2_C649`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C649`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G1_C12::Method4.4865()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C649`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G1_C12::Method5.4866()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C649`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G1_C12::Method6.MI.4868<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C649`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C649::Method7.10981<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C649`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C649`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C649`2<class BaseClass1,class BaseClass0>::ClassMethod2658<object>()
		ldstr "G2_C649::ClassMethod2658.10984<System.Object>()"
		ldstr "class G2_C649`2<class BaseClass1,class BaseClass0> on type class G2_C649`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C649`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C649`2<class BaseClass1,class BaseClass0>::ClassMethod2657()
		ldstr "G2_C649::ClassMethod2657.10983()"
		ldstr "class G2_C649`2<class BaseClass1,class BaseClass0> on type class G2_C649`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C649`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C649`2<class BaseClass1,class BaseClass0>::ClassMethod2656()
		ldstr "G2_C649::ClassMethod2656.10982()"
		ldstr "class G2_C649`2<class BaseClass1,class BaseClass0> on type class G2_C649`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C649`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C649`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G2_C649::Method7.10981<System.Object>()"
		ldstr "class G2_C649`2<class BaseClass1,class BaseClass0> on type class G2_C649`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C649`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C649`2<class BaseClass1,class BaseClass0>::Method3<object>()
		ldstr "G2_C649::Method3.10979<System.Object>()"
		ldstr "class G2_C649`2<class BaseClass1,class BaseClass0> on type class G2_C649`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C649`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C649`2<class BaseClass1,class BaseClass0>::Method2<object>()
		ldstr "G2_C649::Method2.10977<System.Object>()"
		ldstr "class G2_C649`2<class BaseClass1,class BaseClass0> on type class G2_C649`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C649`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C649`2<class BaseClass1,class BaseClass0>::Method1()
		ldstr "G2_C649::Method1.10976()"
		ldstr "class G2_C649`2<class BaseClass1,class BaseClass0> on type class G2_C649`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C649`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C649`2<class BaseClass1,class BaseClass0>::Method0()
		ldstr "G2_C649::Method0.10974()"
		ldstr "class G2_C649`2<class BaseClass1,class BaseClass0> on type class G2_C649`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C649`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C649`2<class BaseClass1,class BaseClass0>::ClassMethod1347<object>()
		ldstr "G2_C649::ClassMethod1347.MI.10986<System.Object>()"
		ldstr "class G2_C649`2<class BaseClass1,class BaseClass0> on type class G2_C649`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C649`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C649`2<class BaseClass1,class BaseClass0>::ClassMethod1346<object>()
		ldstr "G2_C649::ClassMethod1346.MI.10985<System.Object>()"
		ldstr "class G2_C649`2<class BaseClass1,class BaseClass0> on type class G2_C649`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C649`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C649`2<class BaseClass1,class BaseClass0>::Method6<object>()
		ldstr "G1_C12::Method6.4867<System.Object>()"
		ldstr "class G2_C649`2<class BaseClass1,class BaseClass0> on type class G2_C649`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C649`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C649`2<class BaseClass1,class BaseClass0>::Method5()
		ldstr "G1_C12::Method5.4866()"
		ldstr "class G2_C649`2<class BaseClass1,class BaseClass0> on type class G2_C649`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C649`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C649`2<class BaseClass1,class BaseClass0>::Method4()
		ldstr "G1_C12::Method4.4865()"
		ldstr "class G2_C649`2<class BaseClass1,class BaseClass0> on type class G2_C649`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string IBase0::Method0()
		ldstr "G2_C649::Method0.MI.10975()"
		ldstr "IBase0 on type class G2_C649`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method1()
		ldstr "G2_C649::Method1.10976()"
		ldstr "IBase0 on type class G2_C649`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method2<object>()
		ldstr "G2_C649::Method2.MI.10978<System.Object>()"
		ldstr "IBase0 on type class G2_C649`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method3<object>()
		ldstr "G2_C649::Method3.MI.10980<System.Object>()"
		ldstr "IBase0 on type class G2_C649`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G2_C649`2<class BaseClass1,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C12`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C12`2<class BaseClass1,class BaseClass1>::ClassMethod1347<object>()
		ldstr "G2_C649::ClassMethod1347.MI.10986<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass1> on type class G2_C649`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C12`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C12`2<class BaseClass1,class BaseClass1>::ClassMethod1346<object>()
		ldstr "G2_C649::ClassMethod1346.MI.10985<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass1> on type class G2_C649`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C12`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C12`2<class BaseClass1,class BaseClass1>::Method6<object>()
		ldstr "G1_C12::Method6.4867<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass1> on type class G2_C649`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C12`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C12`2<class BaseClass1,class BaseClass1>::Method5()
		ldstr "G1_C12::Method5.4866()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass1> on type class G2_C649`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C12`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C12`2<class BaseClass1,class BaseClass1>::Method4()
		ldstr "G1_C12::Method4.4865()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass1> on type class G2_C649`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C12`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C12`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C12::Method7.4863<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass1> on type class G2_C649`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C649`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G1_C12::Method4.4865()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C649`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G1_C12::Method5.4866()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C649`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G1_C12::Method6.MI.4868<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C649`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C649::Method7.10981<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C649`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C649`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C649`2<class BaseClass1,class BaseClass1>::ClassMethod2658<object>()
		ldstr "G2_C649::ClassMethod2658.10984<System.Object>()"
		ldstr "class G2_C649`2<class BaseClass1,class BaseClass1> on type class G2_C649`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C649`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C649`2<class BaseClass1,class BaseClass1>::ClassMethod2657()
		ldstr "G2_C649::ClassMethod2657.10983()"
		ldstr "class G2_C649`2<class BaseClass1,class BaseClass1> on type class G2_C649`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C649`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C649`2<class BaseClass1,class BaseClass1>::ClassMethod2656()
		ldstr "G2_C649::ClassMethod2656.10982()"
		ldstr "class G2_C649`2<class BaseClass1,class BaseClass1> on type class G2_C649`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C649`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C649`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G2_C649::Method7.10981<System.Object>()"
		ldstr "class G2_C649`2<class BaseClass1,class BaseClass1> on type class G2_C649`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C649`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C649`2<class BaseClass1,class BaseClass1>::Method3<object>()
		ldstr "G2_C649::Method3.10979<System.Object>()"
		ldstr "class G2_C649`2<class BaseClass1,class BaseClass1> on type class G2_C649`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C649`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C649`2<class BaseClass1,class BaseClass1>::Method2<object>()
		ldstr "G2_C649::Method2.10977<System.Object>()"
		ldstr "class G2_C649`2<class BaseClass1,class BaseClass1> on type class G2_C649`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C649`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C649`2<class BaseClass1,class BaseClass1>::Method1()
		ldstr "G2_C649::Method1.10976()"
		ldstr "class G2_C649`2<class BaseClass1,class BaseClass1> on type class G2_C649`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C649`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C649`2<class BaseClass1,class BaseClass1>::Method0()
		ldstr "G2_C649::Method0.10974()"
		ldstr "class G2_C649`2<class BaseClass1,class BaseClass1> on type class G2_C649`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C649`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C649`2<class BaseClass1,class BaseClass1>::ClassMethod1347<object>()
		ldstr "G2_C649::ClassMethod1347.MI.10986<System.Object>()"
		ldstr "class G2_C649`2<class BaseClass1,class BaseClass1> on type class G2_C649`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C649`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C649`2<class BaseClass1,class BaseClass1>::ClassMethod1346<object>()
		ldstr "G2_C649::ClassMethod1346.MI.10985<System.Object>()"
		ldstr "class G2_C649`2<class BaseClass1,class BaseClass1> on type class G2_C649`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C649`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C649`2<class BaseClass1,class BaseClass1>::Method6<object>()
		ldstr "G1_C12::Method6.4867<System.Object>()"
		ldstr "class G2_C649`2<class BaseClass1,class BaseClass1> on type class G2_C649`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C649`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C649`2<class BaseClass1,class BaseClass1>::Method5()
		ldstr "G1_C12::Method5.4866()"
		ldstr "class G2_C649`2<class BaseClass1,class BaseClass1> on type class G2_C649`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C649`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C649`2<class BaseClass1,class BaseClass1>::Method4()
		ldstr "G1_C12::Method4.4865()"
		ldstr "class G2_C649`2<class BaseClass1,class BaseClass1> on type class G2_C649`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string IBase0::Method0()
		ldstr "G2_C649::Method0.MI.10975()"
		ldstr "IBase0 on type class G2_C649`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method1()
		ldstr "G2_C649::Method1.10976()"
		ldstr "IBase0 on type class G2_C649`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method2<object>()
		ldstr "G2_C649::Method2.MI.10978<System.Object>()"
		ldstr "IBase0 on type class G2_C649`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method3<object>()
		ldstr "G2_C649::Method3.MI.10980<System.Object>()"
		ldstr "IBase0 on type class G2_C649`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G1_C12`2<class BaseClass0,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C12`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C12`2<class BaseClass0,class BaseClass0>::ClassMethod1347<object>()
		ldstr "G1_C12::ClassMethod1347.4870<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass0,class BaseClass0> on type class G1_C12`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C12`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C12`2<class BaseClass0,class BaseClass0>::ClassMethod1346<object>()
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass0,class BaseClass0> on type class G1_C12`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C12`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C12`2<class BaseClass0,class BaseClass0>::Method6<object>()
		ldstr "G1_C12::Method6.4867<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass0,class BaseClass0> on type class G1_C12`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C12`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C12`2<class BaseClass0,class BaseClass0>::Method5()
		ldstr "G1_C12::Method5.4866()"
		ldstr "class G1_C12`2<class BaseClass0,class BaseClass0> on type class G1_C12`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C12`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C12`2<class BaseClass0,class BaseClass0>::Method4()
		ldstr "G1_C12::Method4.4865()"
		ldstr "class G1_C12`2<class BaseClass0,class BaseClass0> on type class G1_C12`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C12`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C12`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G1_C12::Method7.4863<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass0,class BaseClass0> on type class G1_C12`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G1_C12`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G1_C12::Method4.4865()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C12`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G1_C12::Method5.4866()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C12`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G1_C12::Method6.MI.4868<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C12`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G1_C12`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G1_C12`2<class BaseClass0,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C12`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C12`2<class BaseClass0,class BaseClass1>::ClassMethod1347<object>()
		ldstr "G1_C12::ClassMethod1347.4870<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass0,class BaseClass1> on type class G1_C12`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C12`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C12`2<class BaseClass0,class BaseClass1>::ClassMethod1346<object>()
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass0,class BaseClass1> on type class G1_C12`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C12`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C12`2<class BaseClass0,class BaseClass1>::Method6<object>()
		ldstr "G1_C12::Method6.4867<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass0,class BaseClass1> on type class G1_C12`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C12`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C12`2<class BaseClass0,class BaseClass1>::Method5()
		ldstr "G1_C12::Method5.4866()"
		ldstr "class G1_C12`2<class BaseClass0,class BaseClass1> on type class G1_C12`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C12`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C12`2<class BaseClass0,class BaseClass1>::Method4()
		ldstr "G1_C12::Method4.4865()"
		ldstr "class G1_C12`2<class BaseClass0,class BaseClass1> on type class G1_C12`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C12`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C12`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G1_C12::Method7.4863<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass0,class BaseClass1> on type class G1_C12`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G1_C12`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G1_C12::Method4.4865()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C12`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G1_C12::Method5.4866()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C12`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G1_C12::Method6.MI.4868<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C12`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G1_C12`2<class BaseClass1,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C12`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C12`2<class BaseClass1,class BaseClass0>::ClassMethod1347<object>()
		ldstr "G1_C12::ClassMethod1347.4870<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass0> on type class G1_C12`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C12`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C12`2<class BaseClass1,class BaseClass0>::ClassMethod1346<object>()
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass0> on type class G1_C12`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C12`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C12`2<class BaseClass1,class BaseClass0>::Method6<object>()
		ldstr "G1_C12::Method6.4867<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass0> on type class G1_C12`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C12`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C12`2<class BaseClass1,class BaseClass0>::Method5()
		ldstr "G1_C12::Method5.4866()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass0> on type class G1_C12`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C12`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C12`2<class BaseClass1,class BaseClass0>::Method4()
		ldstr "G1_C12::Method4.4865()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass0> on type class G1_C12`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C12`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C12`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G1_C12::Method7.4863<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass0> on type class G1_C12`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G1_C12`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G1_C12::Method4.4865()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C12`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G1_C12::Method5.4866()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C12`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G1_C12::Method6.MI.4868<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C12`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G1_C12`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G1_C12`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G1_C12`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G1_C12`2<class BaseClass1,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C12`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C12`2<class BaseClass1,class BaseClass1>::ClassMethod1347<object>()
		ldstr "G1_C12::ClassMethod1347.4870<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass1> on type class G1_C12`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C12`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C12`2<class BaseClass1,class BaseClass1>::ClassMethod1346<object>()
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass1> on type class G1_C12`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C12`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C12`2<class BaseClass1,class BaseClass1>::Method6<object>()
		ldstr "G1_C12::Method6.4867<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass1> on type class G1_C12`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C12`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C12`2<class BaseClass1,class BaseClass1>::Method5()
		ldstr "G1_C12::Method5.4866()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass1> on type class G1_C12`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C12`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C12`2<class BaseClass1,class BaseClass1>::Method4()
		ldstr "G1_C12::Method4.4865()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass1> on type class G1_C12`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C12`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C12`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C12::Method7.4863<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass1> on type class G1_C12`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G1_C12`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G1_C12::Method4.4865()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C12`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G1_C12::Method5.4866()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C12`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G1_C12::Method6.MI.4868<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C12`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G1_C12`2<class BaseClass1,class BaseClass1>"
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
		newobj instance void class G3_C1647`1<class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C649::ClassMethod1346.MI.10985<System.Object>()#G2_C649::ClassMethod1347.MI.10986<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Framework::M.G1_C12.T.T<class BaseClass1,class BaseClass1,class G3_C1647`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C649::ClassMethod1346.MI.10985<System.Object>()#G2_C649::ClassMethod1347.MI.10986<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Framework::M.G1_C12.B.T<class BaseClass1,class G3_C1647`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C649::ClassMethod1346.MI.10985<System.Object>()#G2_C649::ClassMethod1347.MI.10986<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Framework::M.G1_C12.B.B<class G3_C1647`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G3_C1647`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass1,class G3_C1647`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Framework::M.IBase2.B.B<class G3_C1647`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.MI.4868<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass0,class G3_C1647`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.MI.4868<System.Object>()#"
		call void Framework::M.IBase1.A<class G3_C1647`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C649::Method7.10981<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G3_C1647`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C649::Method7.10981<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G3_C1647`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C649::Method7.10981<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G3_C1647`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C649::ClassMethod1346.MI.10985<System.Object>()#G2_C649::ClassMethod1347.MI.10986<System.Object>()#G3_C1647::ClassMethod2656.MI.17244()#G3_C1647::ClassMethod2657.MI.17245()#G3_C1647::ClassMethod2658.MI.17246<System.Object>()#G2_C649::Method0.10974()#G2_C649::Method1.10976()#G2_C649::Method2.10977<System.Object>()#G2_C649::Method3.10979<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G2_C649::Method7.10981<System.Object>()#"
		call void Framework::M.G2_C649.T.T<class BaseClass0,class BaseClass1,class G3_C1647`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C649::ClassMethod1346.MI.10985<System.Object>()#G2_C649::ClassMethod1347.MI.10986<System.Object>()#G3_C1647::ClassMethod2656.MI.17244()#G3_C1647::ClassMethod2657.MI.17245()#G3_C1647::ClassMethod2658.MI.17246<System.Object>()#G2_C649::Method0.10974()#G2_C649::Method1.10976()#G2_C649::Method2.10977<System.Object>()#G2_C649::Method3.10979<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G2_C649::Method7.10981<System.Object>()#"
		call void Framework::M.G2_C649.A.T<class BaseClass1,class G3_C1647`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C649::ClassMethod1346.MI.10985<System.Object>()#G2_C649::ClassMethod1347.MI.10986<System.Object>()#G3_C1647::ClassMethod2656.MI.17244()#G3_C1647::ClassMethod2657.MI.17245()#G3_C1647::ClassMethod2658.MI.17246<System.Object>()#G2_C649::Method0.10974()#G2_C649::Method1.10976()#G2_C649::Method2.10977<System.Object>()#G2_C649::Method3.10979<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G2_C649::Method7.10981<System.Object>()#"
		call void Framework::M.G2_C649.A.B<class G3_C1647`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C649::Method0.MI.10975()#G2_C649::Method1.10976()#G2_C649::Method2.MI.10978<System.Object>()#G2_C649::Method3.MI.10980<System.Object>()#"
		call void Framework::M.IBase0<class G3_C1647`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C649::Method7.10981<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G3_C1647`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C649::Method7.10981<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass0,class G3_C1647`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C649::Method7.10981<System.Object>()#"
		call void Framework::M.IBase2.A.A<class G3_C1647`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C649::ClassMethod1346.MI.10985<System.Object>()#G2_C649::ClassMethod1347.MI.10986<System.Object>()#G3_C1647::ClassMethod2656.MI.17244()#G3_C1647::ClassMethod2657.MI.17245()#G3_C1647::ClassMethod2658.MI.17246<System.Object>()#G3_C1647::ClassMethod4684.17240()#G3_C1647::ClassMethod4685.17241()#G3_C1647::ClassMethod4686.17242<System.Object>()#G3_C1647::ClassMethod4687.17243<System.Object>()#G2_C649::Method0.10974()#G2_C649::Method1.10976()#G2_C649::Method2.10977<System.Object>()#G2_C649::Method3.10979<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G2_C649::Method7.10981<System.Object>()#"
		call void Framework::M.G3_C1647.T<class BaseClass0,class G3_C1647`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C649::ClassMethod1346.MI.10985<System.Object>()#G2_C649::ClassMethod1347.MI.10986<System.Object>()#G3_C1647::ClassMethod2656.MI.17244()#G3_C1647::ClassMethod2657.MI.17245()#G3_C1647::ClassMethod2658.MI.17246<System.Object>()#G3_C1647::ClassMethod4684.17240()#G3_C1647::ClassMethod4685.17241()#G3_C1647::ClassMethod4686.17242<System.Object>()#G3_C1647::ClassMethod4687.17243<System.Object>()#G2_C649::Method0.10974()#G2_C649::Method1.10976()#G2_C649::Method2.10977<System.Object>()#G2_C649::Method3.10979<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G2_C649::Method7.10981<System.Object>()#"
		call void Framework::M.G3_C1647.A<class G3_C1647`1<class BaseClass0>>(!!0,string)
		newobj instance void class G3_C1647`1<class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C649::ClassMethod1346.MI.10985<System.Object>()#G2_C649::ClassMethod1347.MI.10986<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Framework::M.G1_C12.T.T<class BaseClass1,class BaseClass1,class G3_C1647`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C649::ClassMethod1346.MI.10985<System.Object>()#G2_C649::ClassMethod1347.MI.10986<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Framework::M.G1_C12.B.T<class BaseClass1,class G3_C1647`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C649::ClassMethod1346.MI.10985<System.Object>()#G2_C649::ClassMethod1347.MI.10986<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Framework::M.G1_C12.B.B<class G3_C1647`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G3_C1647`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass1,class G3_C1647`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Framework::M.IBase2.B.B<class G3_C1647`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.MI.4868<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass0,class G3_C1647`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.MI.4868<System.Object>()#"
		call void Framework::M.IBase1.A<class G3_C1647`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C649::Method7.10981<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G3_C1647`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C649::Method7.10981<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G3_C1647`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C649::Method7.10981<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G3_C1647`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C649::ClassMethod1346.MI.10985<System.Object>()#G2_C649::ClassMethod1347.MI.10986<System.Object>()#G3_C1647::ClassMethod2656.MI.17244()#G3_C1647::ClassMethod2657.MI.17245()#G3_C1647::ClassMethod2658.MI.17246<System.Object>()#G2_C649::Method0.10974()#G2_C649::Method1.10976()#G2_C649::Method2.10977<System.Object>()#G2_C649::Method3.10979<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G2_C649::Method7.10981<System.Object>()#"
		call void Framework::M.G2_C649.T.T<class BaseClass0,class BaseClass1,class G3_C1647`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C649::ClassMethod1346.MI.10985<System.Object>()#G2_C649::ClassMethod1347.MI.10986<System.Object>()#G3_C1647::ClassMethod2656.MI.17244()#G3_C1647::ClassMethod2657.MI.17245()#G3_C1647::ClassMethod2658.MI.17246<System.Object>()#G2_C649::Method0.10974()#G2_C649::Method1.10976()#G2_C649::Method2.10977<System.Object>()#G2_C649::Method3.10979<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G2_C649::Method7.10981<System.Object>()#"
		call void Framework::M.G2_C649.A.T<class BaseClass1,class G3_C1647`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C649::ClassMethod1346.MI.10985<System.Object>()#G2_C649::ClassMethod1347.MI.10986<System.Object>()#G3_C1647::ClassMethod2656.MI.17244()#G3_C1647::ClassMethod2657.MI.17245()#G3_C1647::ClassMethod2658.MI.17246<System.Object>()#G2_C649::Method0.10974()#G2_C649::Method1.10976()#G2_C649::Method2.10977<System.Object>()#G2_C649::Method3.10979<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G2_C649::Method7.10981<System.Object>()#"
		call void Framework::M.G2_C649.A.B<class G3_C1647`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C649::Method0.MI.10975()#G2_C649::Method1.10976()#G2_C649::Method2.MI.10978<System.Object>()#G2_C649::Method3.MI.10980<System.Object>()#"
		call void Framework::M.IBase0<class G3_C1647`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C649::Method7.10981<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G3_C1647`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C649::Method7.10981<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass0,class G3_C1647`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C649::Method7.10981<System.Object>()#"
		call void Framework::M.IBase2.A.A<class G3_C1647`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C649::ClassMethod1346.MI.10985<System.Object>()#G2_C649::ClassMethod1347.MI.10986<System.Object>()#G3_C1647::ClassMethod2656.MI.17244()#G3_C1647::ClassMethod2657.MI.17245()#G3_C1647::ClassMethod2658.MI.17246<System.Object>()#G3_C1647::ClassMethod4684.17240()#G3_C1647::ClassMethod4685.17241()#G3_C1647::ClassMethod4686.17242<System.Object>()#G3_C1647::ClassMethod4687.17243<System.Object>()#G2_C649::Method0.10974()#G2_C649::Method1.10976()#G2_C649::Method2.10977<System.Object>()#G2_C649::Method3.10979<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G2_C649::Method7.10981<System.Object>()#"
		call void Framework::M.G3_C1647.T<class BaseClass1,class G3_C1647`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C649::ClassMethod1346.MI.10985<System.Object>()#G2_C649::ClassMethod1347.MI.10986<System.Object>()#G3_C1647::ClassMethod2656.MI.17244()#G3_C1647::ClassMethod2657.MI.17245()#G3_C1647::ClassMethod2658.MI.17246<System.Object>()#G3_C1647::ClassMethod4684.17240()#G3_C1647::ClassMethod4685.17241()#G3_C1647::ClassMethod4686.17242<System.Object>()#G3_C1647::ClassMethod4687.17243<System.Object>()#G2_C649::Method0.10974()#G2_C649::Method1.10976()#G2_C649::Method2.10977<System.Object>()#G2_C649::Method3.10979<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G2_C649::Method7.10981<System.Object>()#"
		call void Framework::M.G3_C1647.B<class G3_C1647`1<class BaseClass1>>(!!0,string)
		newobj instance void class G2_C649`2<class BaseClass0,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C649::ClassMethod1346.MI.10985<System.Object>()#G2_C649::ClassMethod1347.MI.10986<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Framework::M.G1_C12.T.T<class BaseClass1,class BaseClass1,class G2_C649`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C649::ClassMethod1346.MI.10985<System.Object>()#G2_C649::ClassMethod1347.MI.10986<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Framework::M.G1_C12.B.T<class BaseClass1,class G2_C649`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C649::ClassMethod1346.MI.10985<System.Object>()#G2_C649::ClassMethod1347.MI.10986<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Framework::M.G1_C12.B.B<class G2_C649`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G2_C649`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass1,class G2_C649`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Framework::M.IBase2.B.B<class G2_C649`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.MI.4868<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass0,class G2_C649`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.MI.4868<System.Object>()#"
		call void Framework::M.IBase1.A<class G2_C649`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C649::Method7.10981<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G2_C649`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C649::Method7.10981<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G2_C649`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C649::Method7.10981<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G2_C649`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C649::ClassMethod1346.MI.10985<System.Object>()#G2_C649::ClassMethod1347.MI.10986<System.Object>()#G2_C649::ClassMethod2656.10982()#G2_C649::ClassMethod2657.10983()#G2_C649::ClassMethod2658.10984<System.Object>()#G2_C649::Method0.10974()#G2_C649::Method1.10976()#G2_C649::Method2.10977<System.Object>()#G2_C649::Method3.10979<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G2_C649::Method7.10981<System.Object>()#"
		call void Framework::M.G2_C649.T.T<class BaseClass0,class BaseClass0,class G2_C649`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C649::ClassMethod1346.MI.10985<System.Object>()#G2_C649::ClassMethod1347.MI.10986<System.Object>()#G2_C649::ClassMethod2656.10982()#G2_C649::ClassMethod2657.10983()#G2_C649::ClassMethod2658.10984<System.Object>()#G2_C649::Method0.10974()#G2_C649::Method1.10976()#G2_C649::Method2.10977<System.Object>()#G2_C649::Method3.10979<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G2_C649::Method7.10981<System.Object>()#"
		call void Framework::M.G2_C649.A.T<class BaseClass0,class G2_C649`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C649::ClassMethod1346.MI.10985<System.Object>()#G2_C649::ClassMethod1347.MI.10986<System.Object>()#G2_C649::ClassMethod2656.10982()#G2_C649::ClassMethod2657.10983()#G2_C649::ClassMethod2658.10984<System.Object>()#G2_C649::Method0.10974()#G2_C649::Method1.10976()#G2_C649::Method2.10977<System.Object>()#G2_C649::Method3.10979<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G2_C649::Method7.10981<System.Object>()#"
		call void Framework::M.G2_C649.A.A<class G2_C649`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C649::Method0.MI.10975()#G2_C649::Method1.10976()#G2_C649::Method2.MI.10978<System.Object>()#G2_C649::Method3.MI.10980<System.Object>()#"
		call void Framework::M.IBase0<class G2_C649`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C649::Method7.10981<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G2_C649`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C649::Method7.10981<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass0,class G2_C649`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C649::Method7.10981<System.Object>()#"
		call void Framework::M.IBase2.A.A<class G2_C649`2<class BaseClass0,class BaseClass0>>(!!0,string)
		newobj instance void class G2_C649`2<class BaseClass0,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C649::ClassMethod1346.MI.10985<System.Object>()#G2_C649::ClassMethod1347.MI.10986<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Framework::M.G1_C12.T.T<class BaseClass1,class BaseClass1,class G2_C649`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C649::ClassMethod1346.MI.10985<System.Object>()#G2_C649::ClassMethod1347.MI.10986<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Framework::M.G1_C12.B.T<class BaseClass1,class G2_C649`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C649::ClassMethod1346.MI.10985<System.Object>()#G2_C649::ClassMethod1347.MI.10986<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Framework::M.G1_C12.B.B<class G2_C649`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G2_C649`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass1,class G2_C649`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Framework::M.IBase2.B.B<class G2_C649`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.MI.4868<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass0,class G2_C649`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.MI.4868<System.Object>()#"
		call void Framework::M.IBase1.A<class G2_C649`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C649::Method7.10981<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G2_C649`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C649::Method7.10981<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G2_C649`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C649::Method7.10981<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G2_C649`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C649::ClassMethod1346.MI.10985<System.Object>()#G2_C649::ClassMethod1347.MI.10986<System.Object>()#G2_C649::ClassMethod2656.10982()#G2_C649::ClassMethod2657.10983()#G2_C649::ClassMethod2658.10984<System.Object>()#G2_C649::Method0.10974()#G2_C649::Method1.10976()#G2_C649::Method2.10977<System.Object>()#G2_C649::Method3.10979<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G2_C649::Method7.10981<System.Object>()#"
		call void Framework::M.G2_C649.T.T<class BaseClass0,class BaseClass1,class G2_C649`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C649::ClassMethod1346.MI.10985<System.Object>()#G2_C649::ClassMethod1347.MI.10986<System.Object>()#G2_C649::ClassMethod2656.10982()#G2_C649::ClassMethod2657.10983()#G2_C649::ClassMethod2658.10984<System.Object>()#G2_C649::Method0.10974()#G2_C649::Method1.10976()#G2_C649::Method2.10977<System.Object>()#G2_C649::Method3.10979<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G2_C649::Method7.10981<System.Object>()#"
		call void Framework::M.G2_C649.A.T<class BaseClass1,class G2_C649`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C649::ClassMethod1346.MI.10985<System.Object>()#G2_C649::ClassMethod1347.MI.10986<System.Object>()#G2_C649::ClassMethod2656.10982()#G2_C649::ClassMethod2657.10983()#G2_C649::ClassMethod2658.10984<System.Object>()#G2_C649::Method0.10974()#G2_C649::Method1.10976()#G2_C649::Method2.10977<System.Object>()#G2_C649::Method3.10979<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G2_C649::Method7.10981<System.Object>()#"
		call void Framework::M.G2_C649.A.B<class G2_C649`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C649::Method0.MI.10975()#G2_C649::Method1.10976()#G2_C649::Method2.MI.10978<System.Object>()#G2_C649::Method3.MI.10980<System.Object>()#"
		call void Framework::M.IBase0<class G2_C649`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C649::Method7.10981<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G2_C649`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C649::Method7.10981<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass0,class G2_C649`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C649::Method7.10981<System.Object>()#"
		call void Framework::M.IBase2.A.A<class G2_C649`2<class BaseClass0,class BaseClass1>>(!!0,string)
		newobj instance void class G2_C649`2<class BaseClass1,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C649::ClassMethod1346.MI.10985<System.Object>()#G2_C649::ClassMethod1347.MI.10986<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Framework::M.G1_C12.T.T<class BaseClass1,class BaseClass1,class G2_C649`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C649::ClassMethod1346.MI.10985<System.Object>()#G2_C649::ClassMethod1347.MI.10986<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Framework::M.G1_C12.B.T<class BaseClass1,class G2_C649`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C649::ClassMethod1346.MI.10985<System.Object>()#G2_C649::ClassMethod1347.MI.10986<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Framework::M.G1_C12.B.B<class G2_C649`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G2_C649`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass1,class G2_C649`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Framework::M.IBase2.B.B<class G2_C649`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.MI.4868<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass0,class G2_C649`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.MI.4868<System.Object>()#"
		call void Framework::M.IBase1.A<class G2_C649`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C649::Method7.10981<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G2_C649`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C649::Method7.10981<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G2_C649`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C649::Method7.10981<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G2_C649`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C649::ClassMethod1346.MI.10985<System.Object>()#G2_C649::ClassMethod1347.MI.10986<System.Object>()#G2_C649::ClassMethod2656.10982()#G2_C649::ClassMethod2657.10983()#G2_C649::ClassMethod2658.10984<System.Object>()#G2_C649::Method0.10974()#G2_C649::Method1.10976()#G2_C649::Method2.10977<System.Object>()#G2_C649::Method3.10979<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G2_C649::Method7.10981<System.Object>()#"
		call void Framework::M.G2_C649.T.T<class BaseClass1,class BaseClass0,class G2_C649`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C649::ClassMethod1346.MI.10985<System.Object>()#G2_C649::ClassMethod1347.MI.10986<System.Object>()#G2_C649::ClassMethod2656.10982()#G2_C649::ClassMethod2657.10983()#G2_C649::ClassMethod2658.10984<System.Object>()#G2_C649::Method0.10974()#G2_C649::Method1.10976()#G2_C649::Method2.10977<System.Object>()#G2_C649::Method3.10979<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G2_C649::Method7.10981<System.Object>()#"
		call void Framework::M.G2_C649.B.T<class BaseClass0,class G2_C649`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C649::ClassMethod1346.MI.10985<System.Object>()#G2_C649::ClassMethod1347.MI.10986<System.Object>()#G2_C649::ClassMethod2656.10982()#G2_C649::ClassMethod2657.10983()#G2_C649::ClassMethod2658.10984<System.Object>()#G2_C649::Method0.10974()#G2_C649::Method1.10976()#G2_C649::Method2.10977<System.Object>()#G2_C649::Method3.10979<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G2_C649::Method7.10981<System.Object>()#"
		call void Framework::M.G2_C649.B.A<class G2_C649`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C649::Method0.MI.10975()#G2_C649::Method1.10976()#G2_C649::Method2.MI.10978<System.Object>()#G2_C649::Method3.MI.10980<System.Object>()#"
		call void Framework::M.IBase0<class G2_C649`2<class BaseClass1,class BaseClass0>>(!!0,string)
		newobj instance void class G2_C649`2<class BaseClass1,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C649::ClassMethod1346.MI.10985<System.Object>()#G2_C649::ClassMethod1347.MI.10986<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Framework::M.G1_C12.T.T<class BaseClass1,class BaseClass1,class G2_C649`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C649::ClassMethod1346.MI.10985<System.Object>()#G2_C649::ClassMethod1347.MI.10986<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Framework::M.G1_C12.B.T<class BaseClass1,class G2_C649`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C649::ClassMethod1346.MI.10985<System.Object>()#G2_C649::ClassMethod1347.MI.10986<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Framework::M.G1_C12.B.B<class G2_C649`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G2_C649`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass1,class G2_C649`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Framework::M.IBase2.B.B<class G2_C649`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.MI.4868<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass0,class G2_C649`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.MI.4868<System.Object>()#"
		call void Framework::M.IBase1.A<class G2_C649`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C649::Method7.10981<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G2_C649`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C649::Method7.10981<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G2_C649`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C649::Method7.10981<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G2_C649`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C649::ClassMethod1346.MI.10985<System.Object>()#G2_C649::ClassMethod1347.MI.10986<System.Object>()#G2_C649::ClassMethod2656.10982()#G2_C649::ClassMethod2657.10983()#G2_C649::ClassMethod2658.10984<System.Object>()#G2_C649::Method0.10974()#G2_C649::Method1.10976()#G2_C649::Method2.10977<System.Object>()#G2_C649::Method3.10979<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G2_C649::Method7.10981<System.Object>()#"
		call void Framework::M.G2_C649.T.T<class BaseClass1,class BaseClass1,class G2_C649`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C649::ClassMethod1346.MI.10985<System.Object>()#G2_C649::ClassMethod1347.MI.10986<System.Object>()#G2_C649::ClassMethod2656.10982()#G2_C649::ClassMethod2657.10983()#G2_C649::ClassMethod2658.10984<System.Object>()#G2_C649::Method0.10974()#G2_C649::Method1.10976()#G2_C649::Method2.10977<System.Object>()#G2_C649::Method3.10979<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G2_C649::Method7.10981<System.Object>()#"
		call void Framework::M.G2_C649.B.T<class BaseClass1,class G2_C649`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C649::ClassMethod1346.MI.10985<System.Object>()#G2_C649::ClassMethod1347.MI.10986<System.Object>()#G2_C649::ClassMethod2656.10982()#G2_C649::ClassMethod2657.10983()#G2_C649::ClassMethod2658.10984<System.Object>()#G2_C649::Method0.10974()#G2_C649::Method1.10976()#G2_C649::Method2.10977<System.Object>()#G2_C649::Method3.10979<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G2_C649::Method7.10981<System.Object>()#"
		call void Framework::M.G2_C649.B.B<class G2_C649`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C649::Method0.MI.10975()#G2_C649::Method1.10976()#G2_C649::Method2.MI.10978<System.Object>()#G2_C649::Method3.MI.10980<System.Object>()#"
		call void Framework::M.IBase0<class G2_C649`2<class BaseClass1,class BaseClass1>>(!!0,string)
		newobj instance void class G1_C12`2<class BaseClass0,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()#G1_C12::ClassMethod1347.4870<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Framework::M.G1_C12.T.T<class BaseClass0,class BaseClass0,class G1_C12`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()#G1_C12::ClassMethod1347.4870<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Framework::M.G1_C12.A.T<class BaseClass0,class G1_C12`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()#G1_C12::ClassMethod1347.4870<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Framework::M.G1_C12.A.A<class G1_C12`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G1_C12`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass0,class G1_C12`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Framework::M.IBase2.A.A<class G1_C12`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.MI.4868<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass0,class G1_C12`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.MI.4868<System.Object>()#"
		call void Framework::M.IBase1.A<class G1_C12`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G1_C12`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G1_C12`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G1_C12`2<class BaseClass0,class BaseClass0>>(!!0,string)
		newobj instance void class G1_C12`2<class BaseClass0,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()#G1_C12::ClassMethod1347.4870<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Framework::M.G1_C12.T.T<class BaseClass0,class BaseClass1,class G1_C12`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()#G1_C12::ClassMethod1347.4870<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Framework::M.G1_C12.A.T<class BaseClass1,class G1_C12`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()#G1_C12::ClassMethod1347.4870<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Framework::M.G1_C12.A.B<class G1_C12`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G1_C12`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G1_C12`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G1_C12`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.MI.4868<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass0,class G1_C12`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.MI.4868<System.Object>()#"
		call void Framework::M.IBase1.A<class G1_C12`2<class BaseClass0,class BaseClass1>>(!!0,string)
		newobj instance void class G1_C12`2<class BaseClass1,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()#G1_C12::ClassMethod1347.4870<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Framework::M.G1_C12.T.T<class BaseClass1,class BaseClass0,class G1_C12`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()#G1_C12::ClassMethod1347.4870<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Framework::M.G1_C12.B.T<class BaseClass0,class G1_C12`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()#G1_C12::ClassMethod1347.4870<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Framework::M.G1_C12.B.A<class G1_C12`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass0,class G1_C12`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass0,class G1_C12`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Framework::M.IBase2.B.A<class G1_C12`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.MI.4868<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass0,class G1_C12`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.MI.4868<System.Object>()#"
		call void Framework::M.IBase1.A<class G1_C12`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G1_C12`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass0,class G1_C12`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Framework::M.IBase2.A.A<class G1_C12`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G1_C12`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G1_C12`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G1_C12`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G1_C12`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass1,class G1_C12`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Framework::M.IBase2.B.B<class G1_C12`2<class BaseClass1,class BaseClass0>>(!!0,string)
		newobj instance void class G1_C12`2<class BaseClass1,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()#G1_C12::ClassMethod1347.4870<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Framework::M.G1_C12.T.T<class BaseClass1,class BaseClass1,class G1_C12`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()#G1_C12::ClassMethod1347.4870<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Framework::M.G1_C12.B.T<class BaseClass1,class G1_C12`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()#G1_C12::ClassMethod1347.4870<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Framework::M.G1_C12.B.B<class G1_C12`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G1_C12`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass1,class G1_C12`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Framework::M.IBase2.B.B<class G1_C12`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.MI.4868<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass0,class G1_C12`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.MI.4868<System.Object>()#"
		call void Framework::M.IBase1.A<class G1_C12`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G1_C12`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G1_C12`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G1_C12`2<class BaseClass1,class BaseClass1>>(!!0,string)
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
		newobj instance void class G3_C1647`1<class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C12`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C12`2<class BaseClass1,class BaseClass1>::ClassMethod1347<object>()
		calli default string(class G3_C1647`1<class BaseClass0>)
		ldstr "G2_C649::ClassMethod1347.MI.10986<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass1> on type class G3_C1647`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C12`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C12`2<class BaseClass1,class BaseClass1>::ClassMethod1346<object>()
		calli default string(class G3_C1647`1<class BaseClass0>)
		ldstr "G2_C649::ClassMethod1346.MI.10985<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass1> on type class G3_C1647`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C12`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C12`2<class BaseClass1,class BaseClass1>::Method6<object>()
		calli default string(class G3_C1647`1<class BaseClass0>)
		ldstr "G1_C12::Method6.4867<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass1> on type class G3_C1647`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C12`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C12`2<class BaseClass1,class BaseClass1>::Method5()
		calli default string(class G3_C1647`1<class BaseClass0>)
		ldstr "G1_C12::Method5.4866()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass1> on type class G3_C1647`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C12`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C12`2<class BaseClass1,class BaseClass1>::Method4()
		calli default string(class G3_C1647`1<class BaseClass0>)
		ldstr "G1_C12::Method4.4865()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass1> on type class G3_C1647`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C12`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C12`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1647`1<class BaseClass0>)
		ldstr "G1_C12::Method7.4863<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass1> on type class G3_C1647`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1647`1<class BaseClass0>)
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G3_C1647`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G3_C1647`1<class BaseClass0>)
		ldstr "G1_C12::Method4.4865()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1647`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G3_C1647`1<class BaseClass0>)
		ldstr "G1_C12::Method5.4866()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1647`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G3_C1647`1<class BaseClass0>)
		ldstr "G1_C12::Method6.MI.4868<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1647`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1647`1<class BaseClass0>)
		ldstr "G2_C649::Method7.10981<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1647`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C649`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C649`2<class BaseClass0,class BaseClass1>::ClassMethod2658<object>()
		calli default string(class G3_C1647`1<class BaseClass0>)
		ldstr "G3_C1647::ClassMethod2658.MI.17246<System.Object>()"
		ldstr "class G2_C649`2<class BaseClass0,class BaseClass1> on type class G3_C1647`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C649`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C649`2<class BaseClass0,class BaseClass1>::ClassMethod2657()
		calli default string(class G3_C1647`1<class BaseClass0>)
		ldstr "G3_C1647::ClassMethod2657.MI.17245()"
		ldstr "class G2_C649`2<class BaseClass0,class BaseClass1> on type class G3_C1647`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C649`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C649`2<class BaseClass0,class BaseClass1>::ClassMethod2656()
		calli default string(class G3_C1647`1<class BaseClass0>)
		ldstr "G3_C1647::ClassMethod2656.MI.17244()"
		ldstr "class G2_C649`2<class BaseClass0,class BaseClass1> on type class G3_C1647`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C649`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C649`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1647`1<class BaseClass0>)
		ldstr "G2_C649::Method7.10981<System.Object>()"
		ldstr "class G2_C649`2<class BaseClass0,class BaseClass1> on type class G3_C1647`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C649`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C649`2<class BaseClass0,class BaseClass1>::Method3<object>()
		calli default string(class G3_C1647`1<class BaseClass0>)
		ldstr "G2_C649::Method3.10979<System.Object>()"
		ldstr "class G2_C649`2<class BaseClass0,class BaseClass1> on type class G3_C1647`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C649`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C649`2<class BaseClass0,class BaseClass1>::Method2<object>()
		calli default string(class G3_C1647`1<class BaseClass0>)
		ldstr "G2_C649::Method2.10977<System.Object>()"
		ldstr "class G2_C649`2<class BaseClass0,class BaseClass1> on type class G3_C1647`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C649`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C649`2<class BaseClass0,class BaseClass1>::Method1()
		calli default string(class G3_C1647`1<class BaseClass0>)
		ldstr "G2_C649::Method1.10976()"
		ldstr "class G2_C649`2<class BaseClass0,class BaseClass1> on type class G3_C1647`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C649`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C649`2<class BaseClass0,class BaseClass1>::Method0()
		calli default string(class G3_C1647`1<class BaseClass0>)
		ldstr "G2_C649::Method0.10974()"
		ldstr "class G2_C649`2<class BaseClass0,class BaseClass1> on type class G3_C1647`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C649`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C649`2<class BaseClass0,class BaseClass1>::ClassMethod1347<object>()
		calli default string(class G3_C1647`1<class BaseClass0>)
		ldstr "G2_C649::ClassMethod1347.MI.10986<System.Object>()"
		ldstr "class G2_C649`2<class BaseClass0,class BaseClass1> on type class G3_C1647`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C649`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C649`2<class BaseClass0,class BaseClass1>::ClassMethod1346<object>()
		calli default string(class G3_C1647`1<class BaseClass0>)
		ldstr "G2_C649::ClassMethod1346.MI.10985<System.Object>()"
		ldstr "class G2_C649`2<class BaseClass0,class BaseClass1> on type class G3_C1647`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C649`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C649`2<class BaseClass0,class BaseClass1>::Method6<object>()
		calli default string(class G3_C1647`1<class BaseClass0>)
		ldstr "G1_C12::Method6.4867<System.Object>()"
		ldstr "class G2_C649`2<class BaseClass0,class BaseClass1> on type class G3_C1647`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C649`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C649`2<class BaseClass0,class BaseClass1>::Method5()
		calli default string(class G3_C1647`1<class BaseClass0>)
		ldstr "G1_C12::Method5.4866()"
		ldstr "class G2_C649`2<class BaseClass0,class BaseClass1> on type class G3_C1647`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C649`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C649`2<class BaseClass0,class BaseClass1>::Method4()
		calli default string(class G3_C1647`1<class BaseClass0>)
		ldstr "G1_C12::Method4.4865()"
		ldstr "class G2_C649`2<class BaseClass0,class BaseClass1> on type class G3_C1647`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method0()
		calli default string(class G3_C1647`1<class BaseClass0>)
		ldstr "G2_C649::Method0.MI.10975()"
		ldstr "IBase0 on type class G3_C1647`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method1()
		calli default string(class G3_C1647`1<class BaseClass0>)
		ldstr "G2_C649::Method1.10976()"
		ldstr "IBase0 on type class G3_C1647`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method2<object>()
		calli default string(class G3_C1647`1<class BaseClass0>)
		ldstr "G2_C649::Method2.MI.10978<System.Object>()"
		ldstr "IBase0 on type class G3_C1647`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method3<object>()
		calli default string(class G3_C1647`1<class BaseClass0>)
		ldstr "G2_C649::Method3.MI.10980<System.Object>()"
		ldstr "IBase0 on type class G3_C1647`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1647`1<class BaseClass0>)
		ldstr "G2_C649::Method7.10981<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G3_C1647`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1647`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1647`1<class BaseClass0>::ClassMethod4687<object>()
		calli default string(class G3_C1647`1<class BaseClass0>)
		ldstr "G3_C1647::ClassMethod4687.17243<System.Object>()"
		ldstr "class G3_C1647`1<class BaseClass0> on type class G3_C1647`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1647`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1647`1<class BaseClass0>::ClassMethod4686<object>()
		calli default string(class G3_C1647`1<class BaseClass0>)
		ldstr "G3_C1647::ClassMethod4686.17242<System.Object>()"
		ldstr "class G3_C1647`1<class BaseClass0> on type class G3_C1647`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1647`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1647`1<class BaseClass0>::ClassMethod4685()
		calli default string(class G3_C1647`1<class BaseClass0>)
		ldstr "G3_C1647::ClassMethod4685.17241()"
		ldstr "class G3_C1647`1<class BaseClass0> on type class G3_C1647`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1647`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1647`1<class BaseClass0>::ClassMethod4684()
		calli default string(class G3_C1647`1<class BaseClass0>)
		ldstr "G3_C1647::ClassMethod4684.17240()"
		ldstr "class G3_C1647`1<class BaseClass0> on type class G3_C1647`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1647`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1647`1<class BaseClass0>::ClassMethod2658<object>()
		calli default string(class G3_C1647`1<class BaseClass0>)
		ldstr "G3_C1647::ClassMethod2658.MI.17246<System.Object>()"
		ldstr "class G3_C1647`1<class BaseClass0> on type class G3_C1647`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1647`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1647`1<class BaseClass0>::ClassMethod2657()
		calli default string(class G3_C1647`1<class BaseClass0>)
		ldstr "G3_C1647::ClassMethod2657.MI.17245()"
		ldstr "class G3_C1647`1<class BaseClass0> on type class G3_C1647`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1647`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1647`1<class BaseClass0>::ClassMethod2656()
		calli default string(class G3_C1647`1<class BaseClass0>)
		ldstr "G3_C1647::ClassMethod2656.MI.17244()"
		ldstr "class G3_C1647`1<class BaseClass0> on type class G3_C1647`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1647`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1647`1<class BaseClass0>::Method7<object>()
		calli default string(class G3_C1647`1<class BaseClass0>)
		ldstr "G2_C649::Method7.10981<System.Object>()"
		ldstr "class G3_C1647`1<class BaseClass0> on type class G3_C1647`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1647`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1647`1<class BaseClass0>::Method3<object>()
		calli default string(class G3_C1647`1<class BaseClass0>)
		ldstr "G2_C649::Method3.10979<System.Object>()"
		ldstr "class G3_C1647`1<class BaseClass0> on type class G3_C1647`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1647`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1647`1<class BaseClass0>::Method2<object>()
		calli default string(class G3_C1647`1<class BaseClass0>)
		ldstr "G2_C649::Method2.10977<System.Object>()"
		ldstr "class G3_C1647`1<class BaseClass0> on type class G3_C1647`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1647`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1647`1<class BaseClass0>::Method1()
		calli default string(class G3_C1647`1<class BaseClass0>)
		ldstr "G2_C649::Method1.10976()"
		ldstr "class G3_C1647`1<class BaseClass0> on type class G3_C1647`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1647`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1647`1<class BaseClass0>::Method0()
		calli default string(class G3_C1647`1<class BaseClass0>)
		ldstr "G2_C649::Method0.10974()"
		ldstr "class G3_C1647`1<class BaseClass0> on type class G3_C1647`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1647`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1647`1<class BaseClass0>::ClassMethod1347<object>()
		calli default string(class G3_C1647`1<class BaseClass0>)
		ldstr "G2_C649::ClassMethod1347.MI.10986<System.Object>()"
		ldstr "class G3_C1647`1<class BaseClass0> on type class G3_C1647`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1647`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1647`1<class BaseClass0>::ClassMethod1346<object>()
		calli default string(class G3_C1647`1<class BaseClass0>)
		ldstr "G2_C649::ClassMethod1346.MI.10985<System.Object>()"
		ldstr "class G3_C1647`1<class BaseClass0> on type class G3_C1647`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1647`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1647`1<class BaseClass0>::Method6<object>()
		calli default string(class G3_C1647`1<class BaseClass0>)
		ldstr "G1_C12::Method6.4867<System.Object>()"
		ldstr "class G3_C1647`1<class BaseClass0> on type class G3_C1647`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1647`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1647`1<class BaseClass0>::Method5()
		calli default string(class G3_C1647`1<class BaseClass0>)
		ldstr "G1_C12::Method5.4866()"
		ldstr "class G3_C1647`1<class BaseClass0> on type class G3_C1647`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1647`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1647`1<class BaseClass0>::Method4()
		calli default string(class G3_C1647`1<class BaseClass0>)
		ldstr "G1_C12::Method4.4865()"
		ldstr "class G3_C1647`1<class BaseClass0> on type class G3_C1647`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G3_C1647`1<class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C12`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C12`2<class BaseClass1,class BaseClass1>::ClassMethod1347<object>()
		calli default string(class G3_C1647`1<class BaseClass1>)
		ldstr "G2_C649::ClassMethod1347.MI.10986<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass1> on type class G3_C1647`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C12`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C12`2<class BaseClass1,class BaseClass1>::ClassMethod1346<object>()
		calli default string(class G3_C1647`1<class BaseClass1>)
		ldstr "G2_C649::ClassMethod1346.MI.10985<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass1> on type class G3_C1647`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C12`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C12`2<class BaseClass1,class BaseClass1>::Method6<object>()
		calli default string(class G3_C1647`1<class BaseClass1>)
		ldstr "G1_C12::Method6.4867<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass1> on type class G3_C1647`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C12`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C12`2<class BaseClass1,class BaseClass1>::Method5()
		calli default string(class G3_C1647`1<class BaseClass1>)
		ldstr "G1_C12::Method5.4866()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass1> on type class G3_C1647`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C12`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C12`2<class BaseClass1,class BaseClass1>::Method4()
		calli default string(class G3_C1647`1<class BaseClass1>)
		ldstr "G1_C12::Method4.4865()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass1> on type class G3_C1647`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C12`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C12`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1647`1<class BaseClass1>)
		ldstr "G1_C12::Method7.4863<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass1> on type class G3_C1647`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1647`1<class BaseClass1>)
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G3_C1647`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G3_C1647`1<class BaseClass1>)
		ldstr "G1_C12::Method4.4865()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1647`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G3_C1647`1<class BaseClass1>)
		ldstr "G1_C12::Method5.4866()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1647`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G3_C1647`1<class BaseClass1>)
		ldstr "G1_C12::Method6.MI.4868<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1647`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1647`1<class BaseClass1>)
		ldstr "G2_C649::Method7.10981<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1647`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C649`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C649`2<class BaseClass0,class BaseClass1>::ClassMethod2658<object>()
		calli default string(class G3_C1647`1<class BaseClass1>)
		ldstr "G3_C1647::ClassMethod2658.MI.17246<System.Object>()"
		ldstr "class G2_C649`2<class BaseClass0,class BaseClass1> on type class G3_C1647`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C649`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C649`2<class BaseClass0,class BaseClass1>::ClassMethod2657()
		calli default string(class G3_C1647`1<class BaseClass1>)
		ldstr "G3_C1647::ClassMethod2657.MI.17245()"
		ldstr "class G2_C649`2<class BaseClass0,class BaseClass1> on type class G3_C1647`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C649`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C649`2<class BaseClass0,class BaseClass1>::ClassMethod2656()
		calli default string(class G3_C1647`1<class BaseClass1>)
		ldstr "G3_C1647::ClassMethod2656.MI.17244()"
		ldstr "class G2_C649`2<class BaseClass0,class BaseClass1> on type class G3_C1647`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C649`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C649`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1647`1<class BaseClass1>)
		ldstr "G2_C649::Method7.10981<System.Object>()"
		ldstr "class G2_C649`2<class BaseClass0,class BaseClass1> on type class G3_C1647`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C649`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C649`2<class BaseClass0,class BaseClass1>::Method3<object>()
		calli default string(class G3_C1647`1<class BaseClass1>)
		ldstr "G2_C649::Method3.10979<System.Object>()"
		ldstr "class G2_C649`2<class BaseClass0,class BaseClass1> on type class G3_C1647`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C649`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C649`2<class BaseClass0,class BaseClass1>::Method2<object>()
		calli default string(class G3_C1647`1<class BaseClass1>)
		ldstr "G2_C649::Method2.10977<System.Object>()"
		ldstr "class G2_C649`2<class BaseClass0,class BaseClass1> on type class G3_C1647`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C649`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C649`2<class BaseClass0,class BaseClass1>::Method1()
		calli default string(class G3_C1647`1<class BaseClass1>)
		ldstr "G2_C649::Method1.10976()"
		ldstr "class G2_C649`2<class BaseClass0,class BaseClass1> on type class G3_C1647`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C649`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C649`2<class BaseClass0,class BaseClass1>::Method0()
		calli default string(class G3_C1647`1<class BaseClass1>)
		ldstr "G2_C649::Method0.10974()"
		ldstr "class G2_C649`2<class BaseClass0,class BaseClass1> on type class G3_C1647`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C649`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C649`2<class BaseClass0,class BaseClass1>::ClassMethod1347<object>()
		calli default string(class G3_C1647`1<class BaseClass1>)
		ldstr "G2_C649::ClassMethod1347.MI.10986<System.Object>()"
		ldstr "class G2_C649`2<class BaseClass0,class BaseClass1> on type class G3_C1647`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C649`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C649`2<class BaseClass0,class BaseClass1>::ClassMethod1346<object>()
		calli default string(class G3_C1647`1<class BaseClass1>)
		ldstr "G2_C649::ClassMethod1346.MI.10985<System.Object>()"
		ldstr "class G2_C649`2<class BaseClass0,class BaseClass1> on type class G3_C1647`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C649`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C649`2<class BaseClass0,class BaseClass1>::Method6<object>()
		calli default string(class G3_C1647`1<class BaseClass1>)
		ldstr "G1_C12::Method6.4867<System.Object>()"
		ldstr "class G2_C649`2<class BaseClass0,class BaseClass1> on type class G3_C1647`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C649`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C649`2<class BaseClass0,class BaseClass1>::Method5()
		calli default string(class G3_C1647`1<class BaseClass1>)
		ldstr "G1_C12::Method5.4866()"
		ldstr "class G2_C649`2<class BaseClass0,class BaseClass1> on type class G3_C1647`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C649`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C649`2<class BaseClass0,class BaseClass1>::Method4()
		calli default string(class G3_C1647`1<class BaseClass1>)
		ldstr "G1_C12::Method4.4865()"
		ldstr "class G2_C649`2<class BaseClass0,class BaseClass1> on type class G3_C1647`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method0()
		calli default string(class G3_C1647`1<class BaseClass1>)
		ldstr "G2_C649::Method0.MI.10975()"
		ldstr "IBase0 on type class G3_C1647`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method1()
		calli default string(class G3_C1647`1<class BaseClass1>)
		ldstr "G2_C649::Method1.10976()"
		ldstr "IBase0 on type class G3_C1647`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method2<object>()
		calli default string(class G3_C1647`1<class BaseClass1>)
		ldstr "G2_C649::Method2.MI.10978<System.Object>()"
		ldstr "IBase0 on type class G3_C1647`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method3<object>()
		calli default string(class G3_C1647`1<class BaseClass1>)
		ldstr "G2_C649::Method3.MI.10980<System.Object>()"
		ldstr "IBase0 on type class G3_C1647`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1647`1<class BaseClass1>)
		ldstr "G2_C649::Method7.10981<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G3_C1647`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1647`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1647`1<class BaseClass1>::ClassMethod4687<object>()
		calli default string(class G3_C1647`1<class BaseClass1>)
		ldstr "G3_C1647::ClassMethod4687.17243<System.Object>()"
		ldstr "class G3_C1647`1<class BaseClass1> on type class G3_C1647`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1647`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1647`1<class BaseClass1>::ClassMethod4686<object>()
		calli default string(class G3_C1647`1<class BaseClass1>)
		ldstr "G3_C1647::ClassMethod4686.17242<System.Object>()"
		ldstr "class G3_C1647`1<class BaseClass1> on type class G3_C1647`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1647`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1647`1<class BaseClass1>::ClassMethod4685()
		calli default string(class G3_C1647`1<class BaseClass1>)
		ldstr "G3_C1647::ClassMethod4685.17241()"
		ldstr "class G3_C1647`1<class BaseClass1> on type class G3_C1647`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1647`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1647`1<class BaseClass1>::ClassMethod4684()
		calli default string(class G3_C1647`1<class BaseClass1>)
		ldstr "G3_C1647::ClassMethod4684.17240()"
		ldstr "class G3_C1647`1<class BaseClass1> on type class G3_C1647`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1647`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1647`1<class BaseClass1>::ClassMethod2658<object>()
		calli default string(class G3_C1647`1<class BaseClass1>)
		ldstr "G3_C1647::ClassMethod2658.MI.17246<System.Object>()"
		ldstr "class G3_C1647`1<class BaseClass1> on type class G3_C1647`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1647`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1647`1<class BaseClass1>::ClassMethod2657()
		calli default string(class G3_C1647`1<class BaseClass1>)
		ldstr "G3_C1647::ClassMethod2657.MI.17245()"
		ldstr "class G3_C1647`1<class BaseClass1> on type class G3_C1647`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1647`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1647`1<class BaseClass1>::ClassMethod2656()
		calli default string(class G3_C1647`1<class BaseClass1>)
		ldstr "G3_C1647::ClassMethod2656.MI.17244()"
		ldstr "class G3_C1647`1<class BaseClass1> on type class G3_C1647`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1647`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1647`1<class BaseClass1>::Method7<object>()
		calli default string(class G3_C1647`1<class BaseClass1>)
		ldstr "G2_C649::Method7.10981<System.Object>()"
		ldstr "class G3_C1647`1<class BaseClass1> on type class G3_C1647`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1647`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1647`1<class BaseClass1>::Method3<object>()
		calli default string(class G3_C1647`1<class BaseClass1>)
		ldstr "G2_C649::Method3.10979<System.Object>()"
		ldstr "class G3_C1647`1<class BaseClass1> on type class G3_C1647`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1647`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1647`1<class BaseClass1>::Method2<object>()
		calli default string(class G3_C1647`1<class BaseClass1>)
		ldstr "G2_C649::Method2.10977<System.Object>()"
		ldstr "class G3_C1647`1<class BaseClass1> on type class G3_C1647`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1647`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1647`1<class BaseClass1>::Method1()
		calli default string(class G3_C1647`1<class BaseClass1>)
		ldstr "G2_C649::Method1.10976()"
		ldstr "class G3_C1647`1<class BaseClass1> on type class G3_C1647`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1647`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1647`1<class BaseClass1>::Method0()
		calli default string(class G3_C1647`1<class BaseClass1>)
		ldstr "G2_C649::Method0.10974()"
		ldstr "class G3_C1647`1<class BaseClass1> on type class G3_C1647`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1647`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1647`1<class BaseClass1>::ClassMethod1347<object>()
		calli default string(class G3_C1647`1<class BaseClass1>)
		ldstr "G2_C649::ClassMethod1347.MI.10986<System.Object>()"
		ldstr "class G3_C1647`1<class BaseClass1> on type class G3_C1647`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1647`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1647`1<class BaseClass1>::ClassMethod1346<object>()
		calli default string(class G3_C1647`1<class BaseClass1>)
		ldstr "G2_C649::ClassMethod1346.MI.10985<System.Object>()"
		ldstr "class G3_C1647`1<class BaseClass1> on type class G3_C1647`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1647`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1647`1<class BaseClass1>::Method6<object>()
		calli default string(class G3_C1647`1<class BaseClass1>)
		ldstr "G1_C12::Method6.4867<System.Object>()"
		ldstr "class G3_C1647`1<class BaseClass1> on type class G3_C1647`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1647`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1647`1<class BaseClass1>::Method5()
		calli default string(class G3_C1647`1<class BaseClass1>)
		ldstr "G1_C12::Method5.4866()"
		ldstr "class G3_C1647`1<class BaseClass1> on type class G3_C1647`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1647`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1647`1<class BaseClass1>::Method4()
		calli default string(class G3_C1647`1<class BaseClass1>)
		ldstr "G1_C12::Method4.4865()"
		ldstr "class G3_C1647`1<class BaseClass1> on type class G3_C1647`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G2_C649`2<class BaseClass0,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C12`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C12`2<class BaseClass1,class BaseClass1>::ClassMethod1347<object>()
		calli default string(class G2_C649`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C649::ClassMethod1347.MI.10986<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass1> on type class G2_C649`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C12`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C12`2<class BaseClass1,class BaseClass1>::ClassMethod1346<object>()
		calli default string(class G2_C649`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C649::ClassMethod1346.MI.10985<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass1> on type class G2_C649`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C12`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C12`2<class BaseClass1,class BaseClass1>::Method6<object>()
		calli default string(class G2_C649`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C12::Method6.4867<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass1> on type class G2_C649`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C12`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C12`2<class BaseClass1,class BaseClass1>::Method5()
		calli default string(class G2_C649`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C12::Method5.4866()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass1> on type class G2_C649`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C12`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C12`2<class BaseClass1,class BaseClass1>::Method4()
		calli default string(class G2_C649`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C12::Method4.4865()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass1> on type class G2_C649`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C12`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C12`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C649`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C12::Method7.4863<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass1> on type class G2_C649`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C649`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C649`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G2_C649`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C12::Method4.4865()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C649`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G2_C649`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C12::Method5.4866()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C649`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G2_C649`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C12::Method6.MI.4868<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C649`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C649`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C649::Method7.10981<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C649`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C649`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C649`2<class BaseClass0,class BaseClass0>::ClassMethod2658<object>()
		calli default string(class G2_C649`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C649::ClassMethod2658.10984<System.Object>()"
		ldstr "class G2_C649`2<class BaseClass0,class BaseClass0> on type class G2_C649`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C649`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C649`2<class BaseClass0,class BaseClass0>::ClassMethod2657()
		calli default string(class G2_C649`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C649::ClassMethod2657.10983()"
		ldstr "class G2_C649`2<class BaseClass0,class BaseClass0> on type class G2_C649`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C649`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C649`2<class BaseClass0,class BaseClass0>::ClassMethod2656()
		calli default string(class G2_C649`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C649::ClassMethod2656.10982()"
		ldstr "class G2_C649`2<class BaseClass0,class BaseClass0> on type class G2_C649`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C649`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C649`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G2_C649`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C649::Method7.10981<System.Object>()"
		ldstr "class G2_C649`2<class BaseClass0,class BaseClass0> on type class G2_C649`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C649`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C649`2<class BaseClass0,class BaseClass0>::Method3<object>()
		calli default string(class G2_C649`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C649::Method3.10979<System.Object>()"
		ldstr "class G2_C649`2<class BaseClass0,class BaseClass0> on type class G2_C649`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C649`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C649`2<class BaseClass0,class BaseClass0>::Method2<object>()
		calli default string(class G2_C649`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C649::Method2.10977<System.Object>()"
		ldstr "class G2_C649`2<class BaseClass0,class BaseClass0> on type class G2_C649`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C649`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C649`2<class BaseClass0,class BaseClass0>::Method1()
		calli default string(class G2_C649`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C649::Method1.10976()"
		ldstr "class G2_C649`2<class BaseClass0,class BaseClass0> on type class G2_C649`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C649`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C649`2<class BaseClass0,class BaseClass0>::Method0()
		calli default string(class G2_C649`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C649::Method0.10974()"
		ldstr "class G2_C649`2<class BaseClass0,class BaseClass0> on type class G2_C649`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C649`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C649`2<class BaseClass0,class BaseClass0>::ClassMethod1347<object>()
		calli default string(class G2_C649`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C649::ClassMethod1347.MI.10986<System.Object>()"
		ldstr "class G2_C649`2<class BaseClass0,class BaseClass0> on type class G2_C649`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C649`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C649`2<class BaseClass0,class BaseClass0>::ClassMethod1346<object>()
		calli default string(class G2_C649`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C649::ClassMethod1346.MI.10985<System.Object>()"
		ldstr "class G2_C649`2<class BaseClass0,class BaseClass0> on type class G2_C649`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C649`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C649`2<class BaseClass0,class BaseClass0>::Method6<object>()
		calli default string(class G2_C649`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C12::Method6.4867<System.Object>()"
		ldstr "class G2_C649`2<class BaseClass0,class BaseClass0> on type class G2_C649`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C649`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C649`2<class BaseClass0,class BaseClass0>::Method5()
		calli default string(class G2_C649`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C12::Method5.4866()"
		ldstr "class G2_C649`2<class BaseClass0,class BaseClass0> on type class G2_C649`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C649`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C649`2<class BaseClass0,class BaseClass0>::Method4()
		calli default string(class G2_C649`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C12::Method4.4865()"
		ldstr "class G2_C649`2<class BaseClass0,class BaseClass0> on type class G2_C649`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method0()
		calli default string(class G2_C649`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C649::Method0.MI.10975()"
		ldstr "IBase0 on type class G2_C649`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method1()
		calli default string(class G2_C649`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C649::Method1.10976()"
		ldstr "IBase0 on type class G2_C649`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method2<object>()
		calli default string(class G2_C649`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C649::Method2.MI.10978<System.Object>()"
		ldstr "IBase0 on type class G2_C649`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method3<object>()
		calli default string(class G2_C649`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C649::Method3.MI.10980<System.Object>()"
		ldstr "IBase0 on type class G2_C649`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G2_C649`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C649::Method7.10981<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G2_C649`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G2_C649`2<class BaseClass0,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C12`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C12`2<class BaseClass1,class BaseClass1>::ClassMethod1347<object>()
		calli default string(class G2_C649`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C649::ClassMethod1347.MI.10986<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass1> on type class G2_C649`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C12`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C12`2<class BaseClass1,class BaseClass1>::ClassMethod1346<object>()
		calli default string(class G2_C649`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C649::ClassMethod1346.MI.10985<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass1> on type class G2_C649`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C12`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C12`2<class BaseClass1,class BaseClass1>::Method6<object>()
		calli default string(class G2_C649`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C12::Method6.4867<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass1> on type class G2_C649`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C12`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C12`2<class BaseClass1,class BaseClass1>::Method5()
		calli default string(class G2_C649`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C12::Method5.4866()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass1> on type class G2_C649`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C12`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C12`2<class BaseClass1,class BaseClass1>::Method4()
		calli default string(class G2_C649`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C12::Method4.4865()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass1> on type class G2_C649`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C12`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C12`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C649`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C12::Method7.4863<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass1> on type class G2_C649`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C649`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C649`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G2_C649`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C12::Method4.4865()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C649`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G2_C649`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C12::Method5.4866()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C649`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G2_C649`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C12::Method6.MI.4868<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C649`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C649`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C649::Method7.10981<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C649`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C649`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C649`2<class BaseClass0,class BaseClass1>::ClassMethod2658<object>()
		calli default string(class G2_C649`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C649::ClassMethod2658.10984<System.Object>()"
		ldstr "class G2_C649`2<class BaseClass0,class BaseClass1> on type class G2_C649`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C649`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C649`2<class BaseClass0,class BaseClass1>::ClassMethod2657()
		calli default string(class G2_C649`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C649::ClassMethod2657.10983()"
		ldstr "class G2_C649`2<class BaseClass0,class BaseClass1> on type class G2_C649`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C649`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C649`2<class BaseClass0,class BaseClass1>::ClassMethod2656()
		calli default string(class G2_C649`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C649::ClassMethod2656.10982()"
		ldstr "class G2_C649`2<class BaseClass0,class BaseClass1> on type class G2_C649`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C649`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C649`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C649`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C649::Method7.10981<System.Object>()"
		ldstr "class G2_C649`2<class BaseClass0,class BaseClass1> on type class G2_C649`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C649`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C649`2<class BaseClass0,class BaseClass1>::Method3<object>()
		calli default string(class G2_C649`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C649::Method3.10979<System.Object>()"
		ldstr "class G2_C649`2<class BaseClass0,class BaseClass1> on type class G2_C649`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C649`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C649`2<class BaseClass0,class BaseClass1>::Method2<object>()
		calli default string(class G2_C649`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C649::Method2.10977<System.Object>()"
		ldstr "class G2_C649`2<class BaseClass0,class BaseClass1> on type class G2_C649`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C649`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C649`2<class BaseClass0,class BaseClass1>::Method1()
		calli default string(class G2_C649`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C649::Method1.10976()"
		ldstr "class G2_C649`2<class BaseClass0,class BaseClass1> on type class G2_C649`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C649`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C649`2<class BaseClass0,class BaseClass1>::Method0()
		calli default string(class G2_C649`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C649::Method0.10974()"
		ldstr "class G2_C649`2<class BaseClass0,class BaseClass1> on type class G2_C649`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C649`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C649`2<class BaseClass0,class BaseClass1>::ClassMethod1347<object>()
		calli default string(class G2_C649`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C649::ClassMethod1347.MI.10986<System.Object>()"
		ldstr "class G2_C649`2<class BaseClass0,class BaseClass1> on type class G2_C649`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C649`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C649`2<class BaseClass0,class BaseClass1>::ClassMethod1346<object>()
		calli default string(class G2_C649`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C649::ClassMethod1346.MI.10985<System.Object>()"
		ldstr "class G2_C649`2<class BaseClass0,class BaseClass1> on type class G2_C649`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C649`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C649`2<class BaseClass0,class BaseClass1>::Method6<object>()
		calli default string(class G2_C649`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C12::Method6.4867<System.Object>()"
		ldstr "class G2_C649`2<class BaseClass0,class BaseClass1> on type class G2_C649`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C649`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C649`2<class BaseClass0,class BaseClass1>::Method5()
		calli default string(class G2_C649`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C12::Method5.4866()"
		ldstr "class G2_C649`2<class BaseClass0,class BaseClass1> on type class G2_C649`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C649`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C649`2<class BaseClass0,class BaseClass1>::Method4()
		calli default string(class G2_C649`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C12::Method4.4865()"
		ldstr "class G2_C649`2<class BaseClass0,class BaseClass1> on type class G2_C649`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method0()
		calli default string(class G2_C649`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C649::Method0.MI.10975()"
		ldstr "IBase0 on type class G2_C649`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method1()
		calli default string(class G2_C649`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C649::Method1.10976()"
		ldstr "IBase0 on type class G2_C649`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method2<object>()
		calli default string(class G2_C649`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C649::Method2.MI.10978<System.Object>()"
		ldstr "IBase0 on type class G2_C649`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method3<object>()
		calli default string(class G2_C649`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C649::Method3.MI.10980<System.Object>()"
		ldstr "IBase0 on type class G2_C649`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G2_C649`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C649::Method7.10981<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G2_C649`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G2_C649`2<class BaseClass1,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C12`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C12`2<class BaseClass1,class BaseClass1>::ClassMethod1347<object>()
		calli default string(class G2_C649`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C649::ClassMethod1347.MI.10986<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass1> on type class G2_C649`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C12`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C12`2<class BaseClass1,class BaseClass1>::ClassMethod1346<object>()
		calli default string(class G2_C649`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C649::ClassMethod1346.MI.10985<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass1> on type class G2_C649`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C12`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C12`2<class BaseClass1,class BaseClass1>::Method6<object>()
		calli default string(class G2_C649`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C12::Method6.4867<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass1> on type class G2_C649`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C12`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C12`2<class BaseClass1,class BaseClass1>::Method5()
		calli default string(class G2_C649`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C12::Method5.4866()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass1> on type class G2_C649`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C12`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C12`2<class BaseClass1,class BaseClass1>::Method4()
		calli default string(class G2_C649`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C12::Method4.4865()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass1> on type class G2_C649`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C12`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C12`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C649`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C12::Method7.4863<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass1> on type class G2_C649`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C649`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C649`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G2_C649`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C12::Method4.4865()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C649`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G2_C649`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C12::Method5.4866()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C649`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G2_C649`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C12::Method6.MI.4868<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C649`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C649`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C649::Method7.10981<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C649`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C649`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C649`2<class BaseClass1,class BaseClass0>::ClassMethod2658<object>()
		calli default string(class G2_C649`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C649::ClassMethod2658.10984<System.Object>()"
		ldstr "class G2_C649`2<class BaseClass1,class BaseClass0> on type class G2_C649`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C649`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C649`2<class BaseClass1,class BaseClass0>::ClassMethod2657()
		calli default string(class G2_C649`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C649::ClassMethod2657.10983()"
		ldstr "class G2_C649`2<class BaseClass1,class BaseClass0> on type class G2_C649`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C649`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C649`2<class BaseClass1,class BaseClass0>::ClassMethod2656()
		calli default string(class G2_C649`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C649::ClassMethod2656.10982()"
		ldstr "class G2_C649`2<class BaseClass1,class BaseClass0> on type class G2_C649`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C649`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C649`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G2_C649`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C649::Method7.10981<System.Object>()"
		ldstr "class G2_C649`2<class BaseClass1,class BaseClass0> on type class G2_C649`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C649`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C649`2<class BaseClass1,class BaseClass0>::Method3<object>()
		calli default string(class G2_C649`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C649::Method3.10979<System.Object>()"
		ldstr "class G2_C649`2<class BaseClass1,class BaseClass0> on type class G2_C649`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C649`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C649`2<class BaseClass1,class BaseClass0>::Method2<object>()
		calli default string(class G2_C649`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C649::Method2.10977<System.Object>()"
		ldstr "class G2_C649`2<class BaseClass1,class BaseClass0> on type class G2_C649`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C649`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C649`2<class BaseClass1,class BaseClass0>::Method1()
		calli default string(class G2_C649`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C649::Method1.10976()"
		ldstr "class G2_C649`2<class BaseClass1,class BaseClass0> on type class G2_C649`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C649`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C649`2<class BaseClass1,class BaseClass0>::Method0()
		calli default string(class G2_C649`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C649::Method0.10974()"
		ldstr "class G2_C649`2<class BaseClass1,class BaseClass0> on type class G2_C649`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C649`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C649`2<class BaseClass1,class BaseClass0>::ClassMethod1347<object>()
		calli default string(class G2_C649`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C649::ClassMethod1347.MI.10986<System.Object>()"
		ldstr "class G2_C649`2<class BaseClass1,class BaseClass0> on type class G2_C649`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C649`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C649`2<class BaseClass1,class BaseClass0>::ClassMethod1346<object>()
		calli default string(class G2_C649`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C649::ClassMethod1346.MI.10985<System.Object>()"
		ldstr "class G2_C649`2<class BaseClass1,class BaseClass0> on type class G2_C649`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C649`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C649`2<class BaseClass1,class BaseClass0>::Method6<object>()
		calli default string(class G2_C649`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C12::Method6.4867<System.Object>()"
		ldstr "class G2_C649`2<class BaseClass1,class BaseClass0> on type class G2_C649`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C649`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C649`2<class BaseClass1,class BaseClass0>::Method5()
		calli default string(class G2_C649`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C12::Method5.4866()"
		ldstr "class G2_C649`2<class BaseClass1,class BaseClass0> on type class G2_C649`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C649`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C649`2<class BaseClass1,class BaseClass0>::Method4()
		calli default string(class G2_C649`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C12::Method4.4865()"
		ldstr "class G2_C649`2<class BaseClass1,class BaseClass0> on type class G2_C649`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method0()
		calli default string(class G2_C649`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C649::Method0.MI.10975()"
		ldstr "IBase0 on type class G2_C649`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method1()
		calli default string(class G2_C649`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C649::Method1.10976()"
		ldstr "IBase0 on type class G2_C649`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method2<object>()
		calli default string(class G2_C649`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C649::Method2.MI.10978<System.Object>()"
		ldstr "IBase0 on type class G2_C649`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method3<object>()
		calli default string(class G2_C649`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C649::Method3.MI.10980<System.Object>()"
		ldstr "IBase0 on type class G2_C649`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G2_C649`2<class BaseClass1,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C12`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C12`2<class BaseClass1,class BaseClass1>::ClassMethod1347<object>()
		calli default string(class G2_C649`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C649::ClassMethod1347.MI.10986<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass1> on type class G2_C649`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C12`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C12`2<class BaseClass1,class BaseClass1>::ClassMethod1346<object>()
		calli default string(class G2_C649`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C649::ClassMethod1346.MI.10985<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass1> on type class G2_C649`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C12`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C12`2<class BaseClass1,class BaseClass1>::Method6<object>()
		calli default string(class G2_C649`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C12::Method6.4867<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass1> on type class G2_C649`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C12`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C12`2<class BaseClass1,class BaseClass1>::Method5()
		calli default string(class G2_C649`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C12::Method5.4866()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass1> on type class G2_C649`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C12`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C12`2<class BaseClass1,class BaseClass1>::Method4()
		calli default string(class G2_C649`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C12::Method4.4865()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass1> on type class G2_C649`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C12`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C12`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C649`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C12::Method7.4863<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass1> on type class G2_C649`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C649`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C649`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G2_C649`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C12::Method4.4865()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C649`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G2_C649`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C12::Method5.4866()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C649`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G2_C649`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C12::Method6.MI.4868<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C649`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C649`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C649::Method7.10981<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C649`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C649`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C649`2<class BaseClass1,class BaseClass1>::ClassMethod2658<object>()
		calli default string(class G2_C649`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C649::ClassMethod2658.10984<System.Object>()"
		ldstr "class G2_C649`2<class BaseClass1,class BaseClass1> on type class G2_C649`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C649`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C649`2<class BaseClass1,class BaseClass1>::ClassMethod2657()
		calli default string(class G2_C649`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C649::ClassMethod2657.10983()"
		ldstr "class G2_C649`2<class BaseClass1,class BaseClass1> on type class G2_C649`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C649`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C649`2<class BaseClass1,class BaseClass1>::ClassMethod2656()
		calli default string(class G2_C649`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C649::ClassMethod2656.10982()"
		ldstr "class G2_C649`2<class BaseClass1,class BaseClass1> on type class G2_C649`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C649`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C649`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C649`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C649::Method7.10981<System.Object>()"
		ldstr "class G2_C649`2<class BaseClass1,class BaseClass1> on type class G2_C649`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C649`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C649`2<class BaseClass1,class BaseClass1>::Method3<object>()
		calli default string(class G2_C649`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C649::Method3.10979<System.Object>()"
		ldstr "class G2_C649`2<class BaseClass1,class BaseClass1> on type class G2_C649`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C649`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C649`2<class BaseClass1,class BaseClass1>::Method2<object>()
		calli default string(class G2_C649`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C649::Method2.10977<System.Object>()"
		ldstr "class G2_C649`2<class BaseClass1,class BaseClass1> on type class G2_C649`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C649`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C649`2<class BaseClass1,class BaseClass1>::Method1()
		calli default string(class G2_C649`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C649::Method1.10976()"
		ldstr "class G2_C649`2<class BaseClass1,class BaseClass1> on type class G2_C649`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C649`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C649`2<class BaseClass1,class BaseClass1>::Method0()
		calli default string(class G2_C649`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C649::Method0.10974()"
		ldstr "class G2_C649`2<class BaseClass1,class BaseClass1> on type class G2_C649`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C649`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C649`2<class BaseClass1,class BaseClass1>::ClassMethod1347<object>()
		calli default string(class G2_C649`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C649::ClassMethod1347.MI.10986<System.Object>()"
		ldstr "class G2_C649`2<class BaseClass1,class BaseClass1> on type class G2_C649`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C649`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C649`2<class BaseClass1,class BaseClass1>::ClassMethod1346<object>()
		calli default string(class G2_C649`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C649::ClassMethod1346.MI.10985<System.Object>()"
		ldstr "class G2_C649`2<class BaseClass1,class BaseClass1> on type class G2_C649`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C649`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C649`2<class BaseClass1,class BaseClass1>::Method6<object>()
		calli default string(class G2_C649`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C12::Method6.4867<System.Object>()"
		ldstr "class G2_C649`2<class BaseClass1,class BaseClass1> on type class G2_C649`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C649`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C649`2<class BaseClass1,class BaseClass1>::Method5()
		calli default string(class G2_C649`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C12::Method5.4866()"
		ldstr "class G2_C649`2<class BaseClass1,class BaseClass1> on type class G2_C649`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C649`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C649`2<class BaseClass1,class BaseClass1>::Method4()
		calli default string(class G2_C649`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C12::Method4.4865()"
		ldstr "class G2_C649`2<class BaseClass1,class BaseClass1> on type class G2_C649`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method0()
		calli default string(class G2_C649`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C649::Method0.MI.10975()"
		ldstr "IBase0 on type class G2_C649`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method1()
		calli default string(class G2_C649`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C649::Method1.10976()"
		ldstr "IBase0 on type class G2_C649`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method2<object>()
		calli default string(class G2_C649`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C649::Method2.MI.10978<System.Object>()"
		ldstr "IBase0 on type class G2_C649`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method3<object>()
		calli default string(class G2_C649`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C649::Method3.MI.10980<System.Object>()"
		ldstr "IBase0 on type class G2_C649`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G1_C12`2<class BaseClass0,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C12`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C12`2<class BaseClass0,class BaseClass0>::ClassMethod1347<object>()
		calli default string(class G1_C12`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C12::ClassMethod1347.4870<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass0,class BaseClass0> on type class G1_C12`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C12`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C12`2<class BaseClass0,class BaseClass0>::ClassMethod1346<object>()
		calli default string(class G1_C12`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass0,class BaseClass0> on type class G1_C12`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C12`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C12`2<class BaseClass0,class BaseClass0>::Method6<object>()
		calli default string(class G1_C12`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C12::Method6.4867<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass0,class BaseClass0> on type class G1_C12`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C12`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C12`2<class BaseClass0,class BaseClass0>::Method5()
		calli default string(class G1_C12`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C12::Method5.4866()"
		ldstr "class G1_C12`2<class BaseClass0,class BaseClass0> on type class G1_C12`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C12`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C12`2<class BaseClass0,class BaseClass0>::Method4()
		calli default string(class G1_C12`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C12::Method4.4865()"
		ldstr "class G1_C12`2<class BaseClass0,class BaseClass0> on type class G1_C12`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C12`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C12`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G1_C12`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C12::Method7.4863<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass0,class BaseClass0> on type class G1_C12`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G1_C12`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G1_C12`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G1_C12`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C12::Method4.4865()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C12`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G1_C12`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C12::Method5.4866()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C12`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G1_C12`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C12::Method6.MI.4868<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C12`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G1_C12`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G1_C12`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G1_C12`2<class BaseClass0,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C12`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C12`2<class BaseClass0,class BaseClass1>::ClassMethod1347<object>()
		calli default string(class G1_C12`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C12::ClassMethod1347.4870<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass0,class BaseClass1> on type class G1_C12`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C12`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C12`2<class BaseClass0,class BaseClass1>::ClassMethod1346<object>()
		calli default string(class G1_C12`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass0,class BaseClass1> on type class G1_C12`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C12`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C12`2<class BaseClass0,class BaseClass1>::Method6<object>()
		calli default string(class G1_C12`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C12::Method6.4867<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass0,class BaseClass1> on type class G1_C12`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C12`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C12`2<class BaseClass0,class BaseClass1>::Method5()
		calli default string(class G1_C12`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C12::Method5.4866()"
		ldstr "class G1_C12`2<class BaseClass0,class BaseClass1> on type class G1_C12`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C12`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C12`2<class BaseClass0,class BaseClass1>::Method4()
		calli default string(class G1_C12`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C12::Method4.4865()"
		ldstr "class G1_C12`2<class BaseClass0,class BaseClass1> on type class G1_C12`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C12`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C12`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G1_C12`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C12::Method7.4863<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass0,class BaseClass1> on type class G1_C12`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G1_C12`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G1_C12`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G1_C12`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C12::Method4.4865()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C12`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G1_C12`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C12::Method5.4866()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C12`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G1_C12`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C12::Method6.MI.4868<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C12`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G1_C12`2<class BaseClass1,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C12`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C12`2<class BaseClass1,class BaseClass0>::ClassMethod1347<object>()
		calli default string(class G1_C12`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C12::ClassMethod1347.4870<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass0> on type class G1_C12`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C12`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C12`2<class BaseClass1,class BaseClass0>::ClassMethod1346<object>()
		calli default string(class G1_C12`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass0> on type class G1_C12`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C12`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C12`2<class BaseClass1,class BaseClass0>::Method6<object>()
		calli default string(class G1_C12`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C12::Method6.4867<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass0> on type class G1_C12`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C12`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C12`2<class BaseClass1,class BaseClass0>::Method5()
		calli default string(class G1_C12`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C12::Method5.4866()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass0> on type class G1_C12`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C12`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C12`2<class BaseClass1,class BaseClass0>::Method4()
		calli default string(class G1_C12`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C12::Method4.4865()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass0> on type class G1_C12`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C12`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C12`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G1_C12`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C12::Method7.4863<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass0> on type class G1_C12`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G1_C12`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G1_C12`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G1_C12`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C12::Method4.4865()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C12`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G1_C12`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C12::Method5.4866()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C12`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G1_C12`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C12::Method6.MI.4868<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C12`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G1_C12`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G1_C12`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G1_C12`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G1_C12`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G1_C12`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G1_C12`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G1_C12`2<class BaseClass1,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C12`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C12`2<class BaseClass1,class BaseClass1>::ClassMethod1347<object>()
		calli default string(class G1_C12`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C12::ClassMethod1347.4870<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass1> on type class G1_C12`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C12`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C12`2<class BaseClass1,class BaseClass1>::ClassMethod1346<object>()
		calli default string(class G1_C12`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass1> on type class G1_C12`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C12`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C12`2<class BaseClass1,class BaseClass1>::Method6<object>()
		calli default string(class G1_C12`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C12::Method6.4867<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass1> on type class G1_C12`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C12`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C12`2<class BaseClass1,class BaseClass1>::Method5()
		calli default string(class G1_C12`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C12::Method5.4866()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass1> on type class G1_C12`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C12`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C12`2<class BaseClass1,class BaseClass1>::Method4()
		calli default string(class G1_C12`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C12::Method4.4865()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass1> on type class G1_C12`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C12`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C12`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G1_C12`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C12::Method7.4863<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass1> on type class G1_C12`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G1_C12`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G1_C12`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G1_C12`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C12::Method4.4865()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C12`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G1_C12`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C12::Method5.4866()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C12`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G1_C12`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C12::Method6.MI.4868<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C12`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G1_C12`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G1_C12`2<class BaseClass1,class BaseClass1>"
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
