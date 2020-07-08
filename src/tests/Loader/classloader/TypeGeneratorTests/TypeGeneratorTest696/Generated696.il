// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern mscorlib { .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) .ver 4:0:0:0 }
.assembly extern TestFramework { .publickeytoken = ( B0 3F 5F 7F 11 D5 0A 3A )  }

//TYPES IN FORWARDER ASSEMBLIES:

//TEST ASSEMBLY:
.assembly Generated696 { .hash algorithm 0x00008004 }
.module Generated696.exe

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
.class public G3_C1168`1<T0> 
		extends class G2_C235`2<class BaseClass0,class BaseClass0>
		implements class IBase2`2<!T0,class BaseClass0> 
{
	.method public hidebysig newslot virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G3_C1168::Method7.14704<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'G2_C235<class BaseClass0,class BaseClass0>.ClassMethod1811'() cil managed noinlining { 
		.override method instance string class G2_C235`2<class BaseClass0,class BaseClass0>::ClassMethod1811()
		ldstr "G3_C1168::ClassMethod1811.MI.14705()"
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class G2_C235`2<class BaseClass0,class BaseClass0>::.ctor()
		ret
	}
}
.class public abstract G2_C235`2<T0, T1> 
		extends class G1_C3`1<!T1>
		implements class IBase1`1<class BaseClass1>, class IBase2`2<!T1,!T0> 
{
	.method public hidebysig virtual instance string Method4() cil managed noinlining { 
		ldstr "G2_C235::Method4.6935()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<class BaseClass1>.Method4'() cil managed noinlining { 
		.override method instance string class IBase1`1<class BaseClass1>::Method4()
		ldstr "G2_C235::Method4.MI.6936()"
		ret
	}
	.method public hidebysig virtual instance string Method5() cil managed noinlining { 
		ldstr "G2_C235::Method5.6937()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<class BaseClass1>.Method5'() cil managed noinlining { 
		.override method instance string class IBase1`1<class BaseClass1>::Method5()
		ldstr "G2_C235::Method5.MI.6938()"
		ret
	}
	.method public hidebysig newslot virtual instance string Method6<M0>() cil managed noinlining { 
		ldstr "G2_C235::Method6.6939<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<class BaseClass1>.Method6'<M0>() cil managed noinlining { 
		.override method instance string class IBase1`1<class BaseClass1>::Method6<[1]>()
		ldstr "G2_C235::Method6.MI.6940<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G2_C235::Method7.6941<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase2<T1,T0>.Method7'<M0>() cil managed noinlining { 
		.override method instance string class IBase2`2<!T1,!T0>::Method7<[1]>()
		ldstr "G2_C235::Method7.MI.6942<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1810() cil managed noinlining { 
		ldstr "G2_C235::ClassMethod1810.6943()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1811() cil managed noinlining { 
		ldstr "G2_C235::ClassMethod1811.6944()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'G1_C3<T1>.ClassMethod1318'() cil managed noinlining { 
		.override method instance string class G1_C3`1<!T1>::ClassMethod1318()
		ldstr "G2_C235::ClassMethod1318.MI.6945()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'G1_C3<T1>.ClassMethod1319'<M0>() cil managed noinlining { 
		.override method instance string class G1_C3`1<!T1>::ClassMethod1319<[1]>()
		ldstr "G2_C235::ClassMethod1319.MI.6946<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class G1_C3`1<!T1>::.ctor()
		ret
	}
}
.class interface public abstract IBase2`2<+T0, -T1> 
{
	.method public hidebysig newslot abstract virtual instance string Method7<M0>() cil managed { }
}
.class public G1_C3`1<T0> 
		implements class IBase2`2<class BaseClass0,class BaseClass1> 
{
	.method public hidebysig newslot virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G1_C3::Method7.4785<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase2<class BaseClass0,class BaseClass1>.Method7'<M0>() cil managed noinlining { 
		.override method instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<[1]>()
		ldstr "G1_C3::Method7.MI.4786<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1318() cil managed noinlining { 
		ldstr "G1_C3::ClassMethod1318.4787()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1319<M0>() cil managed noinlining { 
		ldstr "G1_C3::ClassMethod1319.4788<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1320<M0>() cil managed noinlining { 
		ldstr "G1_C3::ClassMethod1320.4789<"
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
	.method static void M.G3_C1168.T<T0,(class G3_C1168`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 14
		.locals init (string[] actualResults)
		ldc.i4.s    9
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1168.T<T0,(class G3_C1168`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 9
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1168`1<!!T0>::ClassMethod1318()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1168`1<!!T0>::ClassMethod1319<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1168`1<!!T0>::ClassMethod1320<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1168`1<!!T0>::ClassMethod1810()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1168`1<!!T0>::ClassMethod1811()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1168`1<!!T0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1168`1<!!T0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1168`1<!!T0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1168`1<!!T0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1168.A<(class G3_C1168`1<class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 14
		.locals init (string[] actualResults)
		ldc.i4.s    9
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1168.A<(class G3_C1168`1<class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 9
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1168`1<class BaseClass0>::ClassMethod1318()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1168`1<class BaseClass0>::ClassMethod1319<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1168`1<class BaseClass0>::ClassMethod1320<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1168`1<class BaseClass0>::ClassMethod1810()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1168`1<class BaseClass0>::ClassMethod1811()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1168`1<class BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1168`1<class BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1168`1<class BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1168`1<class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1168.B<(class G3_C1168`1<class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 14
		.locals init (string[] actualResults)
		ldc.i4.s    9
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1168.B<(class G3_C1168`1<class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 9
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1168`1<class BaseClass1>::ClassMethod1318()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1168`1<class BaseClass1>::ClassMethod1319<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1168`1<class BaseClass1>::ClassMethod1320<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1168`1<class BaseClass1>::ClassMethod1810()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1168`1<class BaseClass1>::ClassMethod1811()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1168`1<class BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1168`1<class BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1168`1<class BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1168`1<class BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C235.T.T<T0,T1,(class G2_C235`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 14
		.locals init (string[] actualResults)
		ldc.i4.s    9
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C235.T.T<T0,T1,(class G2_C235`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 9
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C235`2<!!T0,!!T1>::ClassMethod1318()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C235`2<!!T0,!!T1>::ClassMethod1319<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C235`2<!!T0,!!T1>::ClassMethod1320<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C235`2<!!T0,!!T1>::ClassMethod1810()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C235`2<!!T0,!!T1>::ClassMethod1811()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C235`2<!!T0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C235`2<!!T0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C235`2<!!T0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C235`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C235.A.T<T1,(class G2_C235`2<class BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 14
		.locals init (string[] actualResults)
		ldc.i4.s    9
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C235.A.T<T1,(class G2_C235`2<class BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 9
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C235`2<class BaseClass0,!!T1>::ClassMethod1318()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C235`2<class BaseClass0,!!T1>::ClassMethod1319<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C235`2<class BaseClass0,!!T1>::ClassMethod1320<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C235`2<class BaseClass0,!!T1>::ClassMethod1810()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C235`2<class BaseClass0,!!T1>::ClassMethod1811()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C235`2<class BaseClass0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C235`2<class BaseClass0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C235`2<class BaseClass0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C235`2<class BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C235.A.A<(class G2_C235`2<class BaseClass0,class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 14
		.locals init (string[] actualResults)
		ldc.i4.s    9
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C235.A.A<(class G2_C235`2<class BaseClass0,class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 9
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C235`2<class BaseClass0,class BaseClass0>::ClassMethod1318()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C235`2<class BaseClass0,class BaseClass0>::ClassMethod1319<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C235`2<class BaseClass0,class BaseClass0>::ClassMethod1320<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C235`2<class BaseClass0,class BaseClass0>::ClassMethod1810()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C235`2<class BaseClass0,class BaseClass0>::ClassMethod1811()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C235`2<class BaseClass0,class BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C235`2<class BaseClass0,class BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C235`2<class BaseClass0,class BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C235`2<class BaseClass0,class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C235.A.B<(class G2_C235`2<class BaseClass0,class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 14
		.locals init (string[] actualResults)
		ldc.i4.s    9
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C235.A.B<(class G2_C235`2<class BaseClass0,class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 9
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C235`2<class BaseClass0,class BaseClass1>::ClassMethod1318()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C235`2<class BaseClass0,class BaseClass1>::ClassMethod1319<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C235`2<class BaseClass0,class BaseClass1>::ClassMethod1320<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C235`2<class BaseClass0,class BaseClass1>::ClassMethod1810()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C235`2<class BaseClass0,class BaseClass1>::ClassMethod1811()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C235`2<class BaseClass0,class BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C235`2<class BaseClass0,class BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C235`2<class BaseClass0,class BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C235`2<class BaseClass0,class BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C235.B.T<T1,(class G2_C235`2<class BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 14
		.locals init (string[] actualResults)
		ldc.i4.s    9
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C235.B.T<T1,(class G2_C235`2<class BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 9
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C235`2<class BaseClass1,!!T1>::ClassMethod1318()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C235`2<class BaseClass1,!!T1>::ClassMethod1319<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C235`2<class BaseClass1,!!T1>::ClassMethod1320<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C235`2<class BaseClass1,!!T1>::ClassMethod1810()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C235`2<class BaseClass1,!!T1>::ClassMethod1811()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C235`2<class BaseClass1,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C235`2<class BaseClass1,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C235`2<class BaseClass1,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C235`2<class BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C235.B.A<(class G2_C235`2<class BaseClass1,class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 14
		.locals init (string[] actualResults)
		ldc.i4.s    9
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C235.B.A<(class G2_C235`2<class BaseClass1,class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 9
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C235`2<class BaseClass1,class BaseClass0>::ClassMethod1318()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C235`2<class BaseClass1,class BaseClass0>::ClassMethod1319<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C235`2<class BaseClass1,class BaseClass0>::ClassMethod1320<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C235`2<class BaseClass1,class BaseClass0>::ClassMethod1810()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C235`2<class BaseClass1,class BaseClass0>::ClassMethod1811()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C235`2<class BaseClass1,class BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C235`2<class BaseClass1,class BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C235`2<class BaseClass1,class BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C235`2<class BaseClass1,class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C235.B.B<(class G2_C235`2<class BaseClass1,class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 14
		.locals init (string[] actualResults)
		ldc.i4.s    9
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C235.B.B<(class G2_C235`2<class BaseClass1,class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 9
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C235`2<class BaseClass1,class BaseClass1>::ClassMethod1318()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C235`2<class BaseClass1,class BaseClass1>::ClassMethod1319<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C235`2<class BaseClass1,class BaseClass1>::ClassMethod1320<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C235`2<class BaseClass1,class BaseClass1>::ClassMethod1810()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C235`2<class BaseClass1,class BaseClass1>::ClassMethod1811()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C235`2<class BaseClass1,class BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C235`2<class BaseClass1,class BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C235`2<class BaseClass1,class BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C235`2<class BaseClass1,class BaseClass1>::Method7<object>()
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
	.method static void M.G1_C3.T<T0,(class G1_C3`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 9
		.locals init (string[] actualResults)
		ldc.i4.s    4
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C3.T<T0,(class G1_C3`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 4
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C3`1<!!T0>::ClassMethod1318()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C3`1<!!T0>::ClassMethod1319<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C3`1<!!T0>::ClassMethod1320<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C3`1<!!T0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C3.A<(class G1_C3`1<class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 9
		.locals init (string[] actualResults)
		ldc.i4.s    4
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C3.A<(class G1_C3`1<class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 4
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C3`1<class BaseClass0>::ClassMethod1318()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C3`1<class BaseClass0>::ClassMethod1319<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C3`1<class BaseClass0>::ClassMethod1320<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C3`1<class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C3.B<(class G1_C3`1<class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 9
		.locals init (string[] actualResults)
		ldc.i4.s    4
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C3.B<(class G1_C3`1<class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 4
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C3`1<class BaseClass1>::ClassMethod1318()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C3`1<class BaseClass1>::ClassMethod1319<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C3`1<class BaseClass1>::ClassMethod1320<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C3`1<class BaseClass1>::Method7<object>()
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
		newobj instance void class G3_C1168`1<class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C3`1<class BaseClass0>
		callvirt instance string class G1_C3`1<class BaseClass0>::ClassMethod1320<object>()
		ldstr "G1_C3::ClassMethod1320.4789<System.Object>()"
		ldstr "class G1_C3`1<class BaseClass0> on type class G3_C1168`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C3`1<class BaseClass0>
		callvirt instance string class G1_C3`1<class BaseClass0>::ClassMethod1319<object>()
		ldstr "G2_C235::ClassMethod1319.MI.6946<System.Object>()"
		ldstr "class G1_C3`1<class BaseClass0> on type class G3_C1168`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C3`1<class BaseClass0>
		callvirt instance string class G1_C3`1<class BaseClass0>::ClassMethod1318()
		ldstr "G2_C235::ClassMethod1318.MI.6945()"
		ldstr "class G1_C3`1<class BaseClass0> on type class G3_C1168`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C3`1<class BaseClass0>
		callvirt instance string class G1_C3`1<class BaseClass0>::Method7<object>()
		ldstr "G1_C3::Method7.4785<System.Object>()"
		ldstr "class G1_C3`1<class BaseClass0> on type class G3_C1168`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G3_C1168::Method7.14704<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1168`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C235`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C235`2<class BaseClass0,class BaseClass0>::ClassMethod1811()
		ldstr "G3_C1168::ClassMethod1811.MI.14705()"
		ldstr "class G2_C235`2<class BaseClass0,class BaseClass0> on type class G3_C1168`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C235`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C235`2<class BaseClass0,class BaseClass0>::ClassMethod1810()
		ldstr "G2_C235::ClassMethod1810.6943()"
		ldstr "class G2_C235`2<class BaseClass0,class BaseClass0> on type class G3_C1168`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C235`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C235`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G2_C235::Method7.6941<System.Object>()"
		ldstr "class G2_C235`2<class BaseClass0,class BaseClass0> on type class G3_C1168`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C235`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C235`2<class BaseClass0,class BaseClass0>::Method6<object>()
		ldstr "G2_C235::Method6.6939<System.Object>()"
		ldstr "class G2_C235`2<class BaseClass0,class BaseClass0> on type class G3_C1168`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C235`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C235`2<class BaseClass0,class BaseClass0>::Method5()
		ldstr "G2_C235::Method5.6937()"
		ldstr "class G2_C235`2<class BaseClass0,class BaseClass0> on type class G3_C1168`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C235`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C235`2<class BaseClass0,class BaseClass0>::Method4()
		ldstr "G2_C235::Method4.6935()"
		ldstr "class G2_C235`2<class BaseClass0,class BaseClass0> on type class G3_C1168`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C235`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C235`2<class BaseClass0,class BaseClass0>::ClassMethod1320<object>()
		ldstr "G1_C3::ClassMethod1320.4789<System.Object>()"
		ldstr "class G2_C235`2<class BaseClass0,class BaseClass0> on type class G3_C1168`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C235`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C235`2<class BaseClass0,class BaseClass0>::ClassMethod1319<object>()
		ldstr "G2_C235::ClassMethod1319.MI.6946<System.Object>()"
		ldstr "class G2_C235`2<class BaseClass0,class BaseClass0> on type class G3_C1168`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C235`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C235`2<class BaseClass0,class BaseClass0>::ClassMethod1318()
		ldstr "G2_C235::ClassMethod1318.MI.6945()"
		ldstr "class G2_C235`2<class BaseClass0,class BaseClass0> on type class G3_C1168`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method4()
		ldstr "G2_C235::Method4.MI.6936()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1168`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method5()
		ldstr "G2_C235::Method5.MI.6938()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1168`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method6<object>()
		ldstr "G2_C235::Method6.MI.6940<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1168`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G3_C1168::Method7.14704<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G3_C1168`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G2_C235::Method4.MI.6936()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1168`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G2_C235::Method5.MI.6938()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1168`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G2_C235::Method6.MI.6940<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1168`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G3_C1168`1<class BaseClass0>
		callvirt instance string class G3_C1168`1<class BaseClass0>::Method7<object>()
		ldstr "G3_C1168::Method7.14704<System.Object>()"
		ldstr "class G3_C1168`1<class BaseClass0> on type class G3_C1168`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1168`1<class BaseClass0>
		callvirt instance string class G3_C1168`1<class BaseClass0>::ClassMethod1811()
		ldstr "G3_C1168::ClassMethod1811.MI.14705()"
		ldstr "class G3_C1168`1<class BaseClass0> on type class G3_C1168`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1168`1<class BaseClass0>
		callvirt instance string class G3_C1168`1<class BaseClass0>::ClassMethod1810()
		ldstr "G2_C235::ClassMethod1810.6943()"
		ldstr "class G3_C1168`1<class BaseClass0> on type class G3_C1168`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1168`1<class BaseClass0>
		callvirt instance string class G3_C1168`1<class BaseClass0>::Method6<object>()
		ldstr "G2_C235::Method6.6939<System.Object>()"
		ldstr "class G3_C1168`1<class BaseClass0> on type class G3_C1168`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1168`1<class BaseClass0>
		callvirt instance string class G3_C1168`1<class BaseClass0>::Method5()
		ldstr "G2_C235::Method5.6937()"
		ldstr "class G3_C1168`1<class BaseClass0> on type class G3_C1168`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1168`1<class BaseClass0>
		callvirt instance string class G3_C1168`1<class BaseClass0>::Method4()
		ldstr "G2_C235::Method4.6935()"
		ldstr "class G3_C1168`1<class BaseClass0> on type class G3_C1168`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1168`1<class BaseClass0>
		callvirt instance string class G3_C1168`1<class BaseClass0>::ClassMethod1320<object>()
		ldstr "G1_C3::ClassMethod1320.4789<System.Object>()"
		ldstr "class G3_C1168`1<class BaseClass0> on type class G3_C1168`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1168`1<class BaseClass0>
		callvirt instance string class G3_C1168`1<class BaseClass0>::ClassMethod1319<object>()
		ldstr "G2_C235::ClassMethod1319.MI.6946<System.Object>()"
		ldstr "class G3_C1168`1<class BaseClass0> on type class G3_C1168`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1168`1<class BaseClass0>
		callvirt instance string class G3_C1168`1<class BaseClass0>::ClassMethod1318()
		ldstr "G2_C235::ClassMethod1318.MI.6945()"
		ldstr "class G3_C1168`1<class BaseClass0> on type class G3_C1168`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G3_C1168`1<class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C3`1<class BaseClass0>
		callvirt instance string class G1_C3`1<class BaseClass0>::ClassMethod1320<object>()
		ldstr "G1_C3::ClassMethod1320.4789<System.Object>()"
		ldstr "class G1_C3`1<class BaseClass0> on type class G3_C1168`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C3`1<class BaseClass0>
		callvirt instance string class G1_C3`1<class BaseClass0>::ClassMethod1319<object>()
		ldstr "G2_C235::ClassMethod1319.MI.6946<System.Object>()"
		ldstr "class G1_C3`1<class BaseClass0> on type class G3_C1168`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C3`1<class BaseClass0>
		callvirt instance string class G1_C3`1<class BaseClass0>::ClassMethod1318()
		ldstr "G2_C235::ClassMethod1318.MI.6945()"
		ldstr "class G1_C3`1<class BaseClass0> on type class G3_C1168`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C3`1<class BaseClass0>
		callvirt instance string class G1_C3`1<class BaseClass0>::Method7<object>()
		ldstr "G1_C3::Method7.4785<System.Object>()"
		ldstr "class G1_C3`1<class BaseClass0> on type class G3_C1168`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G3_C1168::Method7.14704<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1168`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C235`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C235`2<class BaseClass0,class BaseClass0>::ClassMethod1811()
		ldstr "G3_C1168::ClassMethod1811.MI.14705()"
		ldstr "class G2_C235`2<class BaseClass0,class BaseClass0> on type class G3_C1168`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C235`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C235`2<class BaseClass0,class BaseClass0>::ClassMethod1810()
		ldstr "G2_C235::ClassMethod1810.6943()"
		ldstr "class G2_C235`2<class BaseClass0,class BaseClass0> on type class G3_C1168`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C235`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C235`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G2_C235::Method7.6941<System.Object>()"
		ldstr "class G2_C235`2<class BaseClass0,class BaseClass0> on type class G3_C1168`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C235`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C235`2<class BaseClass0,class BaseClass0>::Method6<object>()
		ldstr "G2_C235::Method6.6939<System.Object>()"
		ldstr "class G2_C235`2<class BaseClass0,class BaseClass0> on type class G3_C1168`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C235`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C235`2<class BaseClass0,class BaseClass0>::Method5()
		ldstr "G2_C235::Method5.6937()"
		ldstr "class G2_C235`2<class BaseClass0,class BaseClass0> on type class G3_C1168`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C235`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C235`2<class BaseClass0,class BaseClass0>::Method4()
		ldstr "G2_C235::Method4.6935()"
		ldstr "class G2_C235`2<class BaseClass0,class BaseClass0> on type class G3_C1168`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C235`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C235`2<class BaseClass0,class BaseClass0>::ClassMethod1320<object>()
		ldstr "G1_C3::ClassMethod1320.4789<System.Object>()"
		ldstr "class G2_C235`2<class BaseClass0,class BaseClass0> on type class G3_C1168`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C235`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C235`2<class BaseClass0,class BaseClass0>::ClassMethod1319<object>()
		ldstr "G2_C235::ClassMethod1319.MI.6946<System.Object>()"
		ldstr "class G2_C235`2<class BaseClass0,class BaseClass0> on type class G3_C1168`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C235`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C235`2<class BaseClass0,class BaseClass0>::ClassMethod1318()
		ldstr "G2_C235::ClassMethod1318.MI.6945()"
		ldstr "class G2_C235`2<class BaseClass0,class BaseClass0> on type class G3_C1168`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method4()
		ldstr "G2_C235::Method4.MI.6936()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1168`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method5()
		ldstr "G2_C235::Method5.MI.6938()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1168`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method6<object>()
		ldstr "G2_C235::Method6.MI.6940<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1168`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G3_C1168::Method7.14704<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G3_C1168`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G2_C235::Method4.MI.6936()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1168`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G2_C235::Method5.MI.6938()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1168`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G2_C235::Method6.MI.6940<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1168`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G3_C1168`1<class BaseClass1>
		callvirt instance string class G3_C1168`1<class BaseClass1>::Method7<object>()
		ldstr "G3_C1168::Method7.14704<System.Object>()"
		ldstr "class G3_C1168`1<class BaseClass1> on type class G3_C1168`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1168`1<class BaseClass1>
		callvirt instance string class G3_C1168`1<class BaseClass1>::ClassMethod1811()
		ldstr "G3_C1168::ClassMethod1811.MI.14705()"
		ldstr "class G3_C1168`1<class BaseClass1> on type class G3_C1168`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1168`1<class BaseClass1>
		callvirt instance string class G3_C1168`1<class BaseClass1>::ClassMethod1810()
		ldstr "G2_C235::ClassMethod1810.6943()"
		ldstr "class G3_C1168`1<class BaseClass1> on type class G3_C1168`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1168`1<class BaseClass1>
		callvirt instance string class G3_C1168`1<class BaseClass1>::Method6<object>()
		ldstr "G2_C235::Method6.6939<System.Object>()"
		ldstr "class G3_C1168`1<class BaseClass1> on type class G3_C1168`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1168`1<class BaseClass1>
		callvirt instance string class G3_C1168`1<class BaseClass1>::Method5()
		ldstr "G2_C235::Method5.6937()"
		ldstr "class G3_C1168`1<class BaseClass1> on type class G3_C1168`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1168`1<class BaseClass1>
		callvirt instance string class G3_C1168`1<class BaseClass1>::Method4()
		ldstr "G2_C235::Method4.6935()"
		ldstr "class G3_C1168`1<class BaseClass1> on type class G3_C1168`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1168`1<class BaseClass1>
		callvirt instance string class G3_C1168`1<class BaseClass1>::ClassMethod1320<object>()
		ldstr "G1_C3::ClassMethod1320.4789<System.Object>()"
		ldstr "class G3_C1168`1<class BaseClass1> on type class G3_C1168`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1168`1<class BaseClass1>
		callvirt instance string class G3_C1168`1<class BaseClass1>::ClassMethod1319<object>()
		ldstr "G2_C235::ClassMethod1319.MI.6946<System.Object>()"
		ldstr "class G3_C1168`1<class BaseClass1> on type class G3_C1168`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1168`1<class BaseClass1>
		callvirt instance string class G3_C1168`1<class BaseClass1>::ClassMethod1318()
		ldstr "G2_C235::ClassMethod1318.MI.6945()"
		ldstr "class G3_C1168`1<class BaseClass1> on type class G3_C1168`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G3_C1168::Method7.14704<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G3_C1168`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G3_C1168::Method7.14704<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G3_C1168`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G1_C3`1<class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C3`1<class BaseClass0>
		callvirt instance string class G1_C3`1<class BaseClass0>::ClassMethod1320<object>()
		ldstr "G1_C3::ClassMethod1320.4789<System.Object>()"
		ldstr "class G1_C3`1<class BaseClass0> on type class G1_C3`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C3`1<class BaseClass0>
		callvirt instance string class G1_C3`1<class BaseClass0>::ClassMethod1319<object>()
		ldstr "G1_C3::ClassMethod1319.4788<System.Object>()"
		ldstr "class G1_C3`1<class BaseClass0> on type class G1_C3`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C3`1<class BaseClass0>
		callvirt instance string class G1_C3`1<class BaseClass0>::ClassMethod1318()
		ldstr "G1_C3::ClassMethod1318.4787()"
		ldstr "class G1_C3`1<class BaseClass0> on type class G1_C3`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C3`1<class BaseClass0>
		callvirt instance string class G1_C3`1<class BaseClass0>::Method7<object>()
		ldstr "G1_C3::Method7.4785<System.Object>()"
		ldstr "class G1_C3`1<class BaseClass0> on type class G1_C3`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G1_C3::Method7.MI.4786<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G1_C3`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G1_C3`1<class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C3`1<class BaseClass1>
		callvirt instance string class G1_C3`1<class BaseClass1>::ClassMethod1320<object>()
		ldstr "G1_C3::ClassMethod1320.4789<System.Object>()"
		ldstr "class G1_C3`1<class BaseClass1> on type class G1_C3`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C3`1<class BaseClass1>
		callvirt instance string class G1_C3`1<class BaseClass1>::ClassMethod1319<object>()
		ldstr "G1_C3::ClassMethod1319.4788<System.Object>()"
		ldstr "class G1_C3`1<class BaseClass1> on type class G1_C3`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C3`1<class BaseClass1>
		callvirt instance string class G1_C3`1<class BaseClass1>::ClassMethod1318()
		ldstr "G1_C3::ClassMethod1318.4787()"
		ldstr "class G1_C3`1<class BaseClass1> on type class G1_C3`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C3`1<class BaseClass1>
		callvirt instance string class G1_C3`1<class BaseClass1>::Method7<object>()
		ldstr "G1_C3::Method7.4785<System.Object>()"
		ldstr "class G1_C3`1<class BaseClass1> on type class G1_C3`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G1_C3::Method7.MI.4786<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G1_C3`1<class BaseClass1>"
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
		newobj instance void class G3_C1168`1<class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C235::ClassMethod1318.MI.6945()#G2_C235::ClassMethod1319.MI.6946<System.Object>()#G1_C3::ClassMethod1320.4789<System.Object>()#G1_C3::Method7.4785<System.Object>()#"
		call void Framework::M.G1_C3.T<class BaseClass0,class G3_C1168`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C235::ClassMethod1318.MI.6945()#G2_C235::ClassMethod1319.MI.6946<System.Object>()#G1_C3::ClassMethod1320.4789<System.Object>()#G1_C3::Method7.4785<System.Object>()#"
		call void Framework::M.G1_C3.A<class G3_C1168`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1168::Method7.14704<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G3_C1168`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G3_C1168::Method7.14704<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G3_C1168`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G3_C1168::Method7.14704<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G3_C1168`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C235::ClassMethod1318.MI.6945()#G2_C235::ClassMethod1319.MI.6946<System.Object>()#G1_C3::ClassMethod1320.4789<System.Object>()#G2_C235::ClassMethod1810.6943()#G3_C1168::ClassMethod1811.MI.14705()#G2_C235::Method4.6935()#G2_C235::Method5.6937()#G2_C235::Method6.6939<System.Object>()#G2_C235::Method7.6941<System.Object>()#"
		call void Framework::M.G2_C235.T.T<class BaseClass0,class BaseClass0,class G3_C1168`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C235::ClassMethod1318.MI.6945()#G2_C235::ClassMethod1319.MI.6946<System.Object>()#G1_C3::ClassMethod1320.4789<System.Object>()#G2_C235::ClassMethod1810.6943()#G3_C1168::ClassMethod1811.MI.14705()#G2_C235::Method4.6935()#G2_C235::Method5.6937()#G2_C235::Method6.6939<System.Object>()#G2_C235::Method7.6941<System.Object>()#"
		call void Framework::M.G2_C235.A.T<class BaseClass0,class G3_C1168`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C235::ClassMethod1318.MI.6945()#G2_C235::ClassMethod1319.MI.6946<System.Object>()#G1_C3::ClassMethod1320.4789<System.Object>()#G2_C235::ClassMethod1810.6943()#G3_C1168::ClassMethod1811.MI.14705()#G2_C235::Method4.6935()#G2_C235::Method5.6937()#G2_C235::Method6.6939<System.Object>()#G2_C235::Method7.6941<System.Object>()#"
		call void Framework::M.G2_C235.A.A<class G3_C1168`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C235::Method4.MI.6936()#G2_C235::Method5.MI.6938()#G2_C235::Method6.MI.6940<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass1,class G3_C1168`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C235::Method4.MI.6936()#G2_C235::Method5.MI.6938()#G2_C235::Method6.MI.6940<System.Object>()#"
		call void Framework::M.IBase1.B<class G3_C1168`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1168::Method7.14704<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G3_C1168`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G3_C1168::Method7.14704<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass0,class G3_C1168`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G3_C1168::Method7.14704<System.Object>()#"
		call void Framework::M.IBase2.A.A<class G3_C1168`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C235::Method4.MI.6936()#G2_C235::Method5.MI.6938()#G2_C235::Method6.MI.6940<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass0,class G3_C1168`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C235::Method4.MI.6936()#G2_C235::Method5.MI.6938()#G2_C235::Method6.MI.6940<System.Object>()#"
		call void Framework::M.IBase1.A<class G3_C1168`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C235::ClassMethod1318.MI.6945()#G2_C235::ClassMethod1319.MI.6946<System.Object>()#G1_C3::ClassMethod1320.4789<System.Object>()#G2_C235::ClassMethod1810.6943()#G3_C1168::ClassMethod1811.MI.14705()#G2_C235::Method4.6935()#G2_C235::Method5.6937()#G2_C235::Method6.6939<System.Object>()#G3_C1168::Method7.14704<System.Object>()#"
		call void Framework::M.G3_C1168.T<class BaseClass0,class G3_C1168`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C235::ClassMethod1318.MI.6945()#G2_C235::ClassMethod1319.MI.6946<System.Object>()#G1_C3::ClassMethod1320.4789<System.Object>()#G2_C235::ClassMethod1810.6943()#G3_C1168::ClassMethod1811.MI.14705()#G2_C235::Method4.6935()#G2_C235::Method5.6937()#G2_C235::Method6.6939<System.Object>()#G3_C1168::Method7.14704<System.Object>()#"
		call void Framework::M.G3_C1168.A<class G3_C1168`1<class BaseClass0>>(!!0,string)
		newobj instance void class G3_C1168`1<class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C235::ClassMethod1318.MI.6945()#G2_C235::ClassMethod1319.MI.6946<System.Object>()#G1_C3::ClassMethod1320.4789<System.Object>()#G1_C3::Method7.4785<System.Object>()#"
		call void Framework::M.G1_C3.T<class BaseClass0,class G3_C1168`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C235::ClassMethod1318.MI.6945()#G2_C235::ClassMethod1319.MI.6946<System.Object>()#G1_C3::ClassMethod1320.4789<System.Object>()#G1_C3::Method7.4785<System.Object>()#"
		call void Framework::M.G1_C3.A<class G3_C1168`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1168::Method7.14704<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G3_C1168`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G3_C1168::Method7.14704<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G3_C1168`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1168::Method7.14704<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G3_C1168`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C235::ClassMethod1318.MI.6945()#G2_C235::ClassMethod1319.MI.6946<System.Object>()#G1_C3::ClassMethod1320.4789<System.Object>()#G2_C235::ClassMethod1810.6943()#G3_C1168::ClassMethod1811.MI.14705()#G2_C235::Method4.6935()#G2_C235::Method5.6937()#G2_C235::Method6.6939<System.Object>()#G2_C235::Method7.6941<System.Object>()#"
		call void Framework::M.G2_C235.T.T<class BaseClass0,class BaseClass0,class G3_C1168`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C235::ClassMethod1318.MI.6945()#G2_C235::ClassMethod1319.MI.6946<System.Object>()#G1_C3::ClassMethod1320.4789<System.Object>()#G2_C235::ClassMethod1810.6943()#G3_C1168::ClassMethod1811.MI.14705()#G2_C235::Method4.6935()#G2_C235::Method5.6937()#G2_C235::Method6.6939<System.Object>()#G2_C235::Method7.6941<System.Object>()#"
		call void Framework::M.G2_C235.A.T<class BaseClass0,class G3_C1168`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C235::ClassMethod1318.MI.6945()#G2_C235::ClassMethod1319.MI.6946<System.Object>()#G1_C3::ClassMethod1320.4789<System.Object>()#G2_C235::ClassMethod1810.6943()#G3_C1168::ClassMethod1811.MI.14705()#G2_C235::Method4.6935()#G2_C235::Method5.6937()#G2_C235::Method6.6939<System.Object>()#G2_C235::Method7.6941<System.Object>()#"
		call void Framework::M.G2_C235.A.A<class G3_C1168`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C235::Method4.MI.6936()#G2_C235::Method5.MI.6938()#G2_C235::Method6.MI.6940<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass1,class G3_C1168`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C235::Method4.MI.6936()#G2_C235::Method5.MI.6938()#G2_C235::Method6.MI.6940<System.Object>()#"
		call void Framework::M.IBase1.B<class G3_C1168`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1168::Method7.14704<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G3_C1168`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G3_C1168::Method7.14704<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass0,class G3_C1168`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1168::Method7.14704<System.Object>()#"
		call void Framework::M.IBase2.A.A<class G3_C1168`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C235::Method4.MI.6936()#G2_C235::Method5.MI.6938()#G2_C235::Method6.MI.6940<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass0,class G3_C1168`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C235::Method4.MI.6936()#G2_C235::Method5.MI.6938()#G2_C235::Method6.MI.6940<System.Object>()#"
		call void Framework::M.IBase1.A<class G3_C1168`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C235::ClassMethod1318.MI.6945()#G2_C235::ClassMethod1319.MI.6946<System.Object>()#G1_C3::ClassMethod1320.4789<System.Object>()#G2_C235::ClassMethod1810.6943()#G3_C1168::ClassMethod1811.MI.14705()#G2_C235::Method4.6935()#G2_C235::Method5.6937()#G2_C235::Method6.6939<System.Object>()#G3_C1168::Method7.14704<System.Object>()#"
		call void Framework::M.G3_C1168.T<class BaseClass1,class G3_C1168`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C235::ClassMethod1318.MI.6945()#G2_C235::ClassMethod1319.MI.6946<System.Object>()#G1_C3::ClassMethod1320.4789<System.Object>()#G2_C235::ClassMethod1810.6943()#G3_C1168::ClassMethod1811.MI.14705()#G2_C235::Method4.6935()#G2_C235::Method5.6937()#G2_C235::Method6.6939<System.Object>()#G3_C1168::Method7.14704<System.Object>()#"
		call void Framework::M.G3_C1168.B<class G3_C1168`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1168::Method7.14704<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass0,class G3_C1168`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G3_C1168::Method7.14704<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass0,class G3_C1168`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1168::Method7.14704<System.Object>()#"
		call void Framework::M.IBase2.B.A<class G3_C1168`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1168::Method7.14704<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G3_C1168`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G3_C1168::Method7.14704<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass1,class G3_C1168`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1168::Method7.14704<System.Object>()#"
		call void Framework::M.IBase2.B.B<class G3_C1168`1<class BaseClass1>>(!!0,string)
		newobj instance void class G1_C3`1<class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C3::ClassMethod1318.4787()#G1_C3::ClassMethod1319.4788<System.Object>()#G1_C3::ClassMethod1320.4789<System.Object>()#G1_C3::Method7.4785<System.Object>()#"
		call void Framework::M.G1_C3.T<class BaseClass0,class G1_C3`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C3::ClassMethod1318.4787()#G1_C3::ClassMethod1319.4788<System.Object>()#G1_C3::ClassMethod1320.4789<System.Object>()#G1_C3::Method7.4785<System.Object>()#"
		call void Framework::M.G1_C3.A<class G1_C3`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C3::Method7.MI.4786<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G1_C3`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C3::Method7.MI.4786<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G1_C3`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C3::Method7.MI.4786<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G1_C3`1<class BaseClass0>>(!!0,string)
		newobj instance void class G1_C3`1<class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C3::ClassMethod1318.4787()#G1_C3::ClassMethod1319.4788<System.Object>()#G1_C3::ClassMethod1320.4789<System.Object>()#G1_C3::Method7.4785<System.Object>()#"
		call void Framework::M.G1_C3.T<class BaseClass1,class G1_C3`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C3::ClassMethod1318.4787()#G1_C3::ClassMethod1319.4788<System.Object>()#G1_C3::ClassMethod1320.4789<System.Object>()#G1_C3::Method7.4785<System.Object>()#"
		call void Framework::M.G1_C3.B<class G1_C3`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C3::Method7.MI.4786<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G1_C3`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C3::Method7.MI.4786<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G1_C3`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C3::Method7.MI.4786<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G1_C3`1<class BaseClass1>>(!!0,string)
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
		newobj instance void class G3_C1168`1<class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C3`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C3`1<class BaseClass0>::ClassMethod1320<object>()
		calli default string(class G3_C1168`1<class BaseClass0>)
		ldstr "G1_C3::ClassMethod1320.4789<System.Object>()"
		ldstr "class G1_C3`1<class BaseClass0> on type class G3_C1168`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C3`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C3`1<class BaseClass0>::ClassMethod1319<object>()
		calli default string(class G3_C1168`1<class BaseClass0>)
		ldstr "G2_C235::ClassMethod1319.MI.6946<System.Object>()"
		ldstr "class G1_C3`1<class BaseClass0> on type class G3_C1168`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C3`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C3`1<class BaseClass0>::ClassMethod1318()
		calli default string(class G3_C1168`1<class BaseClass0>)
		ldstr "G2_C235::ClassMethod1318.MI.6945()"
		ldstr "class G1_C3`1<class BaseClass0> on type class G3_C1168`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C3`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C3`1<class BaseClass0>::Method7<object>()
		calli default string(class G3_C1168`1<class BaseClass0>)
		ldstr "G1_C3::Method7.4785<System.Object>()"
		ldstr "class G1_C3`1<class BaseClass0> on type class G3_C1168`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1168`1<class BaseClass0>)
		ldstr "G3_C1168::Method7.14704<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1168`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C235`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C235`2<class BaseClass0,class BaseClass0>::ClassMethod1811()
		calli default string(class G3_C1168`1<class BaseClass0>)
		ldstr "G3_C1168::ClassMethod1811.MI.14705()"
		ldstr "class G2_C235`2<class BaseClass0,class BaseClass0> on type class G3_C1168`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C235`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C235`2<class BaseClass0,class BaseClass0>::ClassMethod1810()
		calli default string(class G3_C1168`1<class BaseClass0>)
		ldstr "G2_C235::ClassMethod1810.6943()"
		ldstr "class G2_C235`2<class BaseClass0,class BaseClass0> on type class G3_C1168`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C235`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C235`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1168`1<class BaseClass0>)
		ldstr "G2_C235::Method7.6941<System.Object>()"
		ldstr "class G2_C235`2<class BaseClass0,class BaseClass0> on type class G3_C1168`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C235`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C235`2<class BaseClass0,class BaseClass0>::Method6<object>()
		calli default string(class G3_C1168`1<class BaseClass0>)
		ldstr "G2_C235::Method6.6939<System.Object>()"
		ldstr "class G2_C235`2<class BaseClass0,class BaseClass0> on type class G3_C1168`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C235`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C235`2<class BaseClass0,class BaseClass0>::Method5()
		calli default string(class G3_C1168`1<class BaseClass0>)
		ldstr "G2_C235::Method5.6937()"
		ldstr "class G2_C235`2<class BaseClass0,class BaseClass0> on type class G3_C1168`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C235`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C235`2<class BaseClass0,class BaseClass0>::Method4()
		calli default string(class G3_C1168`1<class BaseClass0>)
		ldstr "G2_C235::Method4.6935()"
		ldstr "class G2_C235`2<class BaseClass0,class BaseClass0> on type class G3_C1168`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C235`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C235`2<class BaseClass0,class BaseClass0>::ClassMethod1320<object>()
		calli default string(class G3_C1168`1<class BaseClass0>)
		ldstr "G1_C3::ClassMethod1320.4789<System.Object>()"
		ldstr "class G2_C235`2<class BaseClass0,class BaseClass0> on type class G3_C1168`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C235`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C235`2<class BaseClass0,class BaseClass0>::ClassMethod1319<object>()
		calli default string(class G3_C1168`1<class BaseClass0>)
		ldstr "G2_C235::ClassMethod1319.MI.6946<System.Object>()"
		ldstr "class G2_C235`2<class BaseClass0,class BaseClass0> on type class G3_C1168`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C235`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C235`2<class BaseClass0,class BaseClass0>::ClassMethod1318()
		calli default string(class G3_C1168`1<class BaseClass0>)
		ldstr "G2_C235::ClassMethod1318.MI.6945()"
		ldstr "class G2_C235`2<class BaseClass0,class BaseClass0> on type class G3_C1168`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method4()
		calli default string(class G3_C1168`1<class BaseClass0>)
		ldstr "G2_C235::Method4.MI.6936()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1168`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method5()
		calli default string(class G3_C1168`1<class BaseClass0>)
		ldstr "G2_C235::Method5.MI.6938()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1168`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method6<object>()
		calli default string(class G3_C1168`1<class BaseClass0>)
		ldstr "G2_C235::Method6.MI.6940<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1168`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1168`1<class BaseClass0>)
		ldstr "G3_C1168::Method7.14704<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G3_C1168`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G3_C1168`1<class BaseClass0>)
		ldstr "G2_C235::Method4.MI.6936()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1168`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G3_C1168`1<class BaseClass0>)
		ldstr "G2_C235::Method5.MI.6938()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1168`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G3_C1168`1<class BaseClass0>)
		ldstr "G2_C235::Method6.MI.6940<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1168`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1168`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1168`1<class BaseClass0>::Method7<object>()
		calli default string(class G3_C1168`1<class BaseClass0>)
		ldstr "G3_C1168::Method7.14704<System.Object>()"
		ldstr "class G3_C1168`1<class BaseClass0> on type class G3_C1168`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1168`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1168`1<class BaseClass0>::ClassMethod1811()
		calli default string(class G3_C1168`1<class BaseClass0>)
		ldstr "G3_C1168::ClassMethod1811.MI.14705()"
		ldstr "class G3_C1168`1<class BaseClass0> on type class G3_C1168`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1168`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1168`1<class BaseClass0>::ClassMethod1810()
		calli default string(class G3_C1168`1<class BaseClass0>)
		ldstr "G2_C235::ClassMethod1810.6943()"
		ldstr "class G3_C1168`1<class BaseClass0> on type class G3_C1168`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1168`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1168`1<class BaseClass0>::Method6<object>()
		calli default string(class G3_C1168`1<class BaseClass0>)
		ldstr "G2_C235::Method6.6939<System.Object>()"
		ldstr "class G3_C1168`1<class BaseClass0> on type class G3_C1168`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1168`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1168`1<class BaseClass0>::Method5()
		calli default string(class G3_C1168`1<class BaseClass0>)
		ldstr "G2_C235::Method5.6937()"
		ldstr "class G3_C1168`1<class BaseClass0> on type class G3_C1168`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1168`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1168`1<class BaseClass0>::Method4()
		calli default string(class G3_C1168`1<class BaseClass0>)
		ldstr "G2_C235::Method4.6935()"
		ldstr "class G3_C1168`1<class BaseClass0> on type class G3_C1168`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1168`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1168`1<class BaseClass0>::ClassMethod1320<object>()
		calli default string(class G3_C1168`1<class BaseClass0>)
		ldstr "G1_C3::ClassMethod1320.4789<System.Object>()"
		ldstr "class G3_C1168`1<class BaseClass0> on type class G3_C1168`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1168`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1168`1<class BaseClass0>::ClassMethod1319<object>()
		calli default string(class G3_C1168`1<class BaseClass0>)
		ldstr "G2_C235::ClassMethod1319.MI.6946<System.Object>()"
		ldstr "class G3_C1168`1<class BaseClass0> on type class G3_C1168`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1168`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1168`1<class BaseClass0>::ClassMethod1318()
		calli default string(class G3_C1168`1<class BaseClass0>)
		ldstr "G2_C235::ClassMethod1318.MI.6945()"
		ldstr "class G3_C1168`1<class BaseClass0> on type class G3_C1168`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G3_C1168`1<class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C3`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C3`1<class BaseClass0>::ClassMethod1320<object>()
		calli default string(class G3_C1168`1<class BaseClass1>)
		ldstr "G1_C3::ClassMethod1320.4789<System.Object>()"
		ldstr "class G1_C3`1<class BaseClass0> on type class G3_C1168`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C3`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C3`1<class BaseClass0>::ClassMethod1319<object>()
		calli default string(class G3_C1168`1<class BaseClass1>)
		ldstr "G2_C235::ClassMethod1319.MI.6946<System.Object>()"
		ldstr "class G1_C3`1<class BaseClass0> on type class G3_C1168`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C3`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C3`1<class BaseClass0>::ClassMethod1318()
		calli default string(class G3_C1168`1<class BaseClass1>)
		ldstr "G2_C235::ClassMethod1318.MI.6945()"
		ldstr "class G1_C3`1<class BaseClass0> on type class G3_C1168`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C3`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C3`1<class BaseClass0>::Method7<object>()
		calli default string(class G3_C1168`1<class BaseClass1>)
		ldstr "G1_C3::Method7.4785<System.Object>()"
		ldstr "class G1_C3`1<class BaseClass0> on type class G3_C1168`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1168`1<class BaseClass1>)
		ldstr "G3_C1168::Method7.14704<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1168`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C235`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C235`2<class BaseClass0,class BaseClass0>::ClassMethod1811()
		calli default string(class G3_C1168`1<class BaseClass1>)
		ldstr "G3_C1168::ClassMethod1811.MI.14705()"
		ldstr "class G2_C235`2<class BaseClass0,class BaseClass0> on type class G3_C1168`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C235`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C235`2<class BaseClass0,class BaseClass0>::ClassMethod1810()
		calli default string(class G3_C1168`1<class BaseClass1>)
		ldstr "G2_C235::ClassMethod1810.6943()"
		ldstr "class G2_C235`2<class BaseClass0,class BaseClass0> on type class G3_C1168`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C235`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C235`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1168`1<class BaseClass1>)
		ldstr "G2_C235::Method7.6941<System.Object>()"
		ldstr "class G2_C235`2<class BaseClass0,class BaseClass0> on type class G3_C1168`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C235`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C235`2<class BaseClass0,class BaseClass0>::Method6<object>()
		calli default string(class G3_C1168`1<class BaseClass1>)
		ldstr "G2_C235::Method6.6939<System.Object>()"
		ldstr "class G2_C235`2<class BaseClass0,class BaseClass0> on type class G3_C1168`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C235`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C235`2<class BaseClass0,class BaseClass0>::Method5()
		calli default string(class G3_C1168`1<class BaseClass1>)
		ldstr "G2_C235::Method5.6937()"
		ldstr "class G2_C235`2<class BaseClass0,class BaseClass0> on type class G3_C1168`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C235`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C235`2<class BaseClass0,class BaseClass0>::Method4()
		calli default string(class G3_C1168`1<class BaseClass1>)
		ldstr "G2_C235::Method4.6935()"
		ldstr "class G2_C235`2<class BaseClass0,class BaseClass0> on type class G3_C1168`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C235`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C235`2<class BaseClass0,class BaseClass0>::ClassMethod1320<object>()
		calli default string(class G3_C1168`1<class BaseClass1>)
		ldstr "G1_C3::ClassMethod1320.4789<System.Object>()"
		ldstr "class G2_C235`2<class BaseClass0,class BaseClass0> on type class G3_C1168`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C235`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C235`2<class BaseClass0,class BaseClass0>::ClassMethod1319<object>()
		calli default string(class G3_C1168`1<class BaseClass1>)
		ldstr "G2_C235::ClassMethod1319.MI.6946<System.Object>()"
		ldstr "class G2_C235`2<class BaseClass0,class BaseClass0> on type class G3_C1168`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C235`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C235`2<class BaseClass0,class BaseClass0>::ClassMethod1318()
		calli default string(class G3_C1168`1<class BaseClass1>)
		ldstr "G2_C235::ClassMethod1318.MI.6945()"
		ldstr "class G2_C235`2<class BaseClass0,class BaseClass0> on type class G3_C1168`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method4()
		calli default string(class G3_C1168`1<class BaseClass1>)
		ldstr "G2_C235::Method4.MI.6936()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1168`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method5()
		calli default string(class G3_C1168`1<class BaseClass1>)
		ldstr "G2_C235::Method5.MI.6938()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1168`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method6<object>()
		calli default string(class G3_C1168`1<class BaseClass1>)
		ldstr "G2_C235::Method6.MI.6940<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1168`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1168`1<class BaseClass1>)
		ldstr "G3_C1168::Method7.14704<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G3_C1168`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G3_C1168`1<class BaseClass1>)
		ldstr "G2_C235::Method4.MI.6936()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1168`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G3_C1168`1<class BaseClass1>)
		ldstr "G2_C235::Method5.MI.6938()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1168`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G3_C1168`1<class BaseClass1>)
		ldstr "G2_C235::Method6.MI.6940<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1168`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1168`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1168`1<class BaseClass1>::Method7<object>()
		calli default string(class G3_C1168`1<class BaseClass1>)
		ldstr "G3_C1168::Method7.14704<System.Object>()"
		ldstr "class G3_C1168`1<class BaseClass1> on type class G3_C1168`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1168`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1168`1<class BaseClass1>::ClassMethod1811()
		calli default string(class G3_C1168`1<class BaseClass1>)
		ldstr "G3_C1168::ClassMethod1811.MI.14705()"
		ldstr "class G3_C1168`1<class BaseClass1> on type class G3_C1168`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1168`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1168`1<class BaseClass1>::ClassMethod1810()
		calli default string(class G3_C1168`1<class BaseClass1>)
		ldstr "G2_C235::ClassMethod1810.6943()"
		ldstr "class G3_C1168`1<class BaseClass1> on type class G3_C1168`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1168`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1168`1<class BaseClass1>::Method6<object>()
		calli default string(class G3_C1168`1<class BaseClass1>)
		ldstr "G2_C235::Method6.6939<System.Object>()"
		ldstr "class G3_C1168`1<class BaseClass1> on type class G3_C1168`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1168`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1168`1<class BaseClass1>::Method5()
		calli default string(class G3_C1168`1<class BaseClass1>)
		ldstr "G2_C235::Method5.6937()"
		ldstr "class G3_C1168`1<class BaseClass1> on type class G3_C1168`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1168`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1168`1<class BaseClass1>::Method4()
		calli default string(class G3_C1168`1<class BaseClass1>)
		ldstr "G2_C235::Method4.6935()"
		ldstr "class G3_C1168`1<class BaseClass1> on type class G3_C1168`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1168`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1168`1<class BaseClass1>::ClassMethod1320<object>()
		calli default string(class G3_C1168`1<class BaseClass1>)
		ldstr "G1_C3::ClassMethod1320.4789<System.Object>()"
		ldstr "class G3_C1168`1<class BaseClass1> on type class G3_C1168`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1168`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1168`1<class BaseClass1>::ClassMethod1319<object>()
		calli default string(class G3_C1168`1<class BaseClass1>)
		ldstr "G2_C235::ClassMethod1319.MI.6946<System.Object>()"
		ldstr "class G3_C1168`1<class BaseClass1> on type class G3_C1168`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1168`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1168`1<class BaseClass1>::ClassMethod1318()
		calli default string(class G3_C1168`1<class BaseClass1>)
		ldstr "G2_C235::ClassMethod1318.MI.6945()"
		ldstr "class G3_C1168`1<class BaseClass1> on type class G3_C1168`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1168`1<class BaseClass1>)
		ldstr "G3_C1168::Method7.14704<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G3_C1168`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1168`1<class BaseClass1>)
		ldstr "G3_C1168::Method7.14704<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G3_C1168`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G1_C3`1<class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C3`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C3`1<class BaseClass0>::ClassMethod1320<object>()
		calli default string(class G1_C3`1<class BaseClass0>)
		ldstr "G1_C3::ClassMethod1320.4789<System.Object>()"
		ldstr "class G1_C3`1<class BaseClass0> on type class G1_C3`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C3`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C3`1<class BaseClass0>::ClassMethod1319<object>()
		calli default string(class G1_C3`1<class BaseClass0>)
		ldstr "G1_C3::ClassMethod1319.4788<System.Object>()"
		ldstr "class G1_C3`1<class BaseClass0> on type class G1_C3`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C3`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C3`1<class BaseClass0>::ClassMethod1318()
		calli default string(class G1_C3`1<class BaseClass0>)
		ldstr "G1_C3::ClassMethod1318.4787()"
		ldstr "class G1_C3`1<class BaseClass0> on type class G1_C3`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C3`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C3`1<class BaseClass0>::Method7<object>()
		calli default string(class G1_C3`1<class BaseClass0>)
		ldstr "G1_C3::Method7.4785<System.Object>()"
		ldstr "class G1_C3`1<class BaseClass0> on type class G1_C3`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G1_C3`1<class BaseClass0>)
		ldstr "G1_C3::Method7.MI.4786<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G1_C3`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G1_C3`1<class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C3`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C3`1<class BaseClass1>::ClassMethod1320<object>()
		calli default string(class G1_C3`1<class BaseClass1>)
		ldstr "G1_C3::ClassMethod1320.4789<System.Object>()"
		ldstr "class G1_C3`1<class BaseClass1> on type class G1_C3`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C3`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C3`1<class BaseClass1>::ClassMethod1319<object>()
		calli default string(class G1_C3`1<class BaseClass1>)
		ldstr "G1_C3::ClassMethod1319.4788<System.Object>()"
		ldstr "class G1_C3`1<class BaseClass1> on type class G1_C3`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C3`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C3`1<class BaseClass1>::ClassMethod1318()
		calli default string(class G1_C3`1<class BaseClass1>)
		ldstr "G1_C3::ClassMethod1318.4787()"
		ldstr "class G1_C3`1<class BaseClass1> on type class G1_C3`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C3`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C3`1<class BaseClass1>::Method7<object>()
		calli default string(class G1_C3`1<class BaseClass1>)
		ldstr "G1_C3::Method7.4785<System.Object>()"
		ldstr "class G1_C3`1<class BaseClass1> on type class G1_C3`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G1_C3`1<class BaseClass1>)
		ldstr "G1_C3::Method7.MI.4786<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G1_C3`1<class BaseClass1>"
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
