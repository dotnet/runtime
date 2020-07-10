// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) .ver 4:0:0:0 }
.assembly extern TestFramework { .publickeytoken = ( B0 3F 5F 7F 11 D5 0A 3A )  }

//TYPES IN FORWARDER ASSEMBLIES:

//TEST ASSEMBLY:
.assembly Generated1126 { .hash algorithm 0x00008004 }
.module Generated1126.exe

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
.class public G3_C1598`1<T0> 
		extends class G2_C606`2<class BaseClass1,class BaseClass0>
		implements class IBase2`2<!T0,class BaseClass1> 
{
	.method public hidebysig virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G3_C1598::Method7.17005<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase2<T0,class BaseClass1>.Method7'<M0>() cil managed noinlining { 
		.override method instance string class IBase2`2<!T0,class BaseClass1>::Method7<[1]>()
		ldstr "G3_C1598::Method7.MI.17006<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod4585() cil managed noinlining { 
		ldstr "G3_C1598::ClassMethod4585.17007()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod4586<M0>() cil managed noinlining { 
		ldstr "G3_C1598::ClassMethod4586.17008<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod4587<M0>() cil managed noinlining { 
		ldstr "G3_C1598::ClassMethod4587.17009<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'G2_C606<class BaseClass1,class BaseClass0>.ClassMethod2578'() cil managed noinlining { 
		.override method instance string class G2_C606`2<class BaseClass1,class BaseClass0>::ClassMethod2578()
		ldstr "G3_C1598::ClassMethod2578.MI.17010()"
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class G2_C606`2<class BaseClass1,class BaseClass0>::.ctor()
		ret
	}
}
.class public G2_C606`2<T0, T1> 
		extends class G1_C11`2<!T0,!T0>
		implements class IBase2`2<!T0,class BaseClass0>, class IBase1`1<!T0> 
{
	.method public hidebysig virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G2_C606::Method7.10574<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase2<T0,class BaseClass0>.Method7'<M0>() cil managed noinlining { 
		.override method instance string class IBase2`2<!T0,class BaseClass0>::Method7<[1]>()
		ldstr "G2_C606::Method7.MI.10575<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string Method4() cil managed noinlining { 
		ldstr "G2_C606::Method4.10576()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<T0>.Method4'() cil managed noinlining { 
		.override method instance string class IBase1`1<!T0>::Method4()
		ldstr "G2_C606::Method4.MI.10577()"
		ret
	}
	.method public hidebysig virtual instance string Method5() cil managed noinlining { 
		ldstr "G2_C606::Method5.10578()"
		ret
	}
	.method public hidebysig virtual instance string Method6<M0>() cil managed noinlining { 
		ldstr "G2_C606::Method6.10579<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<T0>.Method6'<M0>() cil managed noinlining { 
		.override method instance string class IBase1`1<!T0>::Method6<[1]>()
		ldstr "G2_C606::Method6.MI.10580<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod2578() cil managed noinlining { 
		ldstr "G2_C606::ClassMethod2578.10581()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'G1_C11<T0,T0>.ClassMethod1344'<M0>() cil managed noinlining { 
		.override method instance string class G1_C11`2<!T0,!T0>::ClassMethod1344<[1]>()
		ldstr "G2_C606::ClassMethod1344.MI.10582<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'G1_C11<T0,T0>.ClassMethod1345'<M0>() cil managed noinlining { 
		.override method instance string class G1_C11`2<!T0,!T0>::ClassMethod1345<[1]>()
		ldstr "G2_C606::ClassMethod1345.MI.10583<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class G1_C11`2<!T0,!T0>::.ctor()
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
	.method static void M.G3_C1598.T<T0,(class G3_C1598`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 15
		.locals init (string[] actualResults)
		ldc.i4.s    10
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1598.T<T0,(class G3_C1598`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 10
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1598`1<!!T0>::ClassMethod1344<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1598`1<!!T0>::ClassMethod1345<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1598`1<!!T0>::ClassMethod2578()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1598`1<!!T0>::ClassMethod4585()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1598`1<!!T0>::ClassMethod4586<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1598`1<!!T0>::ClassMethod4587<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1598`1<!!T0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1598`1<!!T0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1598`1<!!T0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1598`1<!!T0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1598.A<(class G3_C1598`1<class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 15
		.locals init (string[] actualResults)
		ldc.i4.s    10
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1598.A<(class G3_C1598`1<class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 10
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1598`1<class BaseClass0>::ClassMethod1344<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1598`1<class BaseClass0>::ClassMethod1345<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1598`1<class BaseClass0>::ClassMethod2578()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1598`1<class BaseClass0>::ClassMethod4585()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1598`1<class BaseClass0>::ClassMethod4586<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1598`1<class BaseClass0>::ClassMethod4587<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1598`1<class BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1598`1<class BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1598`1<class BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1598`1<class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1598.B<(class G3_C1598`1<class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 15
		.locals init (string[] actualResults)
		ldc.i4.s    10
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1598.B<(class G3_C1598`1<class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 10
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1598`1<class BaseClass1>::ClassMethod1344<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1598`1<class BaseClass1>::ClassMethod1345<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1598`1<class BaseClass1>::ClassMethod2578()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1598`1<class BaseClass1>::ClassMethod4585()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1598`1<class BaseClass1>::ClassMethod4586<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1598`1<class BaseClass1>::ClassMethod4587<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1598`1<class BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1598`1<class BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1598`1<class BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1598`1<class BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C606.T.T<T0,T1,(class G2_C606`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 12
		.locals init (string[] actualResults)
		ldc.i4.s    7
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C606.T.T<T0,T1,(class G2_C606`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 7
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C606`2<!!T0,!!T1>::ClassMethod1344<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C606`2<!!T0,!!T1>::ClassMethod1345<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C606`2<!!T0,!!T1>::ClassMethod2578()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C606`2<!!T0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C606`2<!!T0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C606`2<!!T0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C606`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C606.A.T<T1,(class G2_C606`2<class BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 12
		.locals init (string[] actualResults)
		ldc.i4.s    7
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C606.A.T<T1,(class G2_C606`2<class BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 7
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C606`2<class BaseClass0,!!T1>::ClassMethod1344<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C606`2<class BaseClass0,!!T1>::ClassMethod1345<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C606`2<class BaseClass0,!!T1>::ClassMethod2578()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C606`2<class BaseClass0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C606`2<class BaseClass0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C606`2<class BaseClass0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C606`2<class BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C606.A.A<(class G2_C606`2<class BaseClass0,class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 12
		.locals init (string[] actualResults)
		ldc.i4.s    7
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C606.A.A<(class G2_C606`2<class BaseClass0,class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 7
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C606`2<class BaseClass0,class BaseClass0>::ClassMethod1344<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C606`2<class BaseClass0,class BaseClass0>::ClassMethod1345<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C606`2<class BaseClass0,class BaseClass0>::ClassMethod2578()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C606`2<class BaseClass0,class BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C606`2<class BaseClass0,class BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C606`2<class BaseClass0,class BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C606`2<class BaseClass0,class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C606.A.B<(class G2_C606`2<class BaseClass0,class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 12
		.locals init (string[] actualResults)
		ldc.i4.s    7
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C606.A.B<(class G2_C606`2<class BaseClass0,class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 7
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C606`2<class BaseClass0,class BaseClass1>::ClassMethod1344<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C606`2<class BaseClass0,class BaseClass1>::ClassMethod1345<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C606`2<class BaseClass0,class BaseClass1>::ClassMethod2578()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C606`2<class BaseClass0,class BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C606`2<class BaseClass0,class BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C606`2<class BaseClass0,class BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C606`2<class BaseClass0,class BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C606.B.T<T1,(class G2_C606`2<class BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 12
		.locals init (string[] actualResults)
		ldc.i4.s    7
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C606.B.T<T1,(class G2_C606`2<class BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 7
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C606`2<class BaseClass1,!!T1>::ClassMethod1344<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C606`2<class BaseClass1,!!T1>::ClassMethod1345<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C606`2<class BaseClass1,!!T1>::ClassMethod2578()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C606`2<class BaseClass1,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C606`2<class BaseClass1,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C606`2<class BaseClass1,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C606`2<class BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C606.B.A<(class G2_C606`2<class BaseClass1,class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 12
		.locals init (string[] actualResults)
		ldc.i4.s    7
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C606.B.A<(class G2_C606`2<class BaseClass1,class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 7
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C606`2<class BaseClass1,class BaseClass0>::ClassMethod1344<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C606`2<class BaseClass1,class BaseClass0>::ClassMethod1345<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C606`2<class BaseClass1,class BaseClass0>::ClassMethod2578()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C606`2<class BaseClass1,class BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C606`2<class BaseClass1,class BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C606`2<class BaseClass1,class BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C606`2<class BaseClass1,class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C606.B.B<(class G2_C606`2<class BaseClass1,class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 12
		.locals init (string[] actualResults)
		ldc.i4.s    7
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C606.B.B<(class G2_C606`2<class BaseClass1,class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 7
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C606`2<class BaseClass1,class BaseClass1>::ClassMethod1344<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C606`2<class BaseClass1,class BaseClass1>::ClassMethod1345<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C606`2<class BaseClass1,class BaseClass1>::ClassMethod2578()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C606`2<class BaseClass1,class BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C606`2<class BaseClass1,class BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C606`2<class BaseClass1,class BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C606`2<class BaseClass1,class BaseClass1>::Method7<object>()
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
		newobj instance void class G3_C1598`1<class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C11`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C11`2<class BaseClass1,class BaseClass1>::ClassMethod1345<object>()
		ldstr "G2_C606::ClassMethod1345.MI.10583<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass1> on type class G3_C1598`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C11`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C11`2<class BaseClass1,class BaseClass1>::ClassMethod1344<object>()
		ldstr "G2_C606::ClassMethod1344.MI.10582<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass1> on type class G3_C1598`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C11`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C11`2<class BaseClass1,class BaseClass1>::Method6<object>()
		ldstr "G2_C606::Method6.10579<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass1> on type class G3_C1598`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C11`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C11`2<class BaseClass1,class BaseClass1>::Method5()
		ldstr "G2_C606::Method5.10578()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass1> on type class G3_C1598`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C11`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C11`2<class BaseClass1,class BaseClass1>::Method4()
		ldstr "G1_C11::Method4.4856()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass1> on type class G3_C1598`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C11`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C11`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G3_C1598::Method7.17005<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass1> on type class G3_C1598`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G2_C606::Method7.MI.10575<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G3_C1598`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G2_C606::Method4.MI.10577()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1598`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G2_C606::Method5.10578()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1598`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G2_C606::Method6.MI.10580<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1598`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G3_C1598::Method7.MI.17006<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1598`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C606`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C606`2<class BaseClass1,class BaseClass0>::ClassMethod2578()
		ldstr "G3_C1598::ClassMethod2578.MI.17010()"
		ldstr "class G2_C606`2<class BaseClass1,class BaseClass0> on type class G3_C1598`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C606`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C606`2<class BaseClass1,class BaseClass0>::Method6<object>()
		ldstr "G2_C606::Method6.10579<System.Object>()"
		ldstr "class G2_C606`2<class BaseClass1,class BaseClass0> on type class G3_C1598`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C606`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C606`2<class BaseClass1,class BaseClass0>::Method5()
		ldstr "G2_C606::Method5.10578()"
		ldstr "class G2_C606`2<class BaseClass1,class BaseClass0> on type class G3_C1598`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C606`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C606`2<class BaseClass1,class BaseClass0>::Method4()
		ldstr "G2_C606::Method4.10576()"
		ldstr "class G2_C606`2<class BaseClass1,class BaseClass0> on type class G3_C1598`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C606`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C606`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G3_C1598::Method7.17005<System.Object>()"
		ldstr "class G2_C606`2<class BaseClass1,class BaseClass0> on type class G3_C1598`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C606`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C606`2<class BaseClass1,class BaseClass0>::ClassMethod1345<object>()
		ldstr "G2_C606::ClassMethod1345.MI.10583<System.Object>()"
		ldstr "class G2_C606`2<class BaseClass1,class BaseClass0> on type class G3_C1598`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C606`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C606`2<class BaseClass1,class BaseClass0>::ClassMethod1344<object>()
		ldstr "G2_C606::ClassMethod1344.MI.10582<System.Object>()"
		ldstr "class G2_C606`2<class BaseClass1,class BaseClass0> on type class G3_C1598`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G2_C606::Method7.MI.10575<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G3_C1598`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method4()
		ldstr "G2_C606::Method4.MI.10577()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1598`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method5()
		ldstr "G2_C606::Method5.10578()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1598`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method6<object>()
		ldstr "G2_C606::Method6.MI.10580<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1598`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G2_C606::Method7.MI.10575<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G3_C1598`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G3_C1598`1<class BaseClass0>
		callvirt instance string class G3_C1598`1<class BaseClass0>::ClassMethod4587<object>()
		ldstr "G3_C1598::ClassMethod4587.17009<System.Object>()"
		ldstr "class G3_C1598`1<class BaseClass0> on type class G3_C1598`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1598`1<class BaseClass0>
		callvirt instance string class G3_C1598`1<class BaseClass0>::ClassMethod4586<object>()
		ldstr "G3_C1598::ClassMethod4586.17008<System.Object>()"
		ldstr "class G3_C1598`1<class BaseClass0> on type class G3_C1598`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1598`1<class BaseClass0>
		callvirt instance string class G3_C1598`1<class BaseClass0>::ClassMethod4585()
		ldstr "G3_C1598::ClassMethod4585.17007()"
		ldstr "class G3_C1598`1<class BaseClass0> on type class G3_C1598`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1598`1<class BaseClass0>
		callvirt instance string class G3_C1598`1<class BaseClass0>::Method7<object>()
		ldstr "G3_C1598::Method7.17005<System.Object>()"
		ldstr "class G3_C1598`1<class BaseClass0> on type class G3_C1598`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1598`1<class BaseClass0>
		callvirt instance string class G3_C1598`1<class BaseClass0>::ClassMethod2578()
		ldstr "G3_C1598::ClassMethod2578.MI.17010()"
		ldstr "class G3_C1598`1<class BaseClass0> on type class G3_C1598`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1598`1<class BaseClass0>
		callvirt instance string class G3_C1598`1<class BaseClass0>::Method6<object>()
		ldstr "G2_C606::Method6.10579<System.Object>()"
		ldstr "class G3_C1598`1<class BaseClass0> on type class G3_C1598`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1598`1<class BaseClass0>
		callvirt instance string class G3_C1598`1<class BaseClass0>::Method5()
		ldstr "G2_C606::Method5.10578()"
		ldstr "class G3_C1598`1<class BaseClass0> on type class G3_C1598`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1598`1<class BaseClass0>
		callvirt instance string class G3_C1598`1<class BaseClass0>::Method4()
		ldstr "G2_C606::Method4.10576()"
		ldstr "class G3_C1598`1<class BaseClass0> on type class G3_C1598`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1598`1<class BaseClass0>
		callvirt instance string class G3_C1598`1<class BaseClass0>::ClassMethod1345<object>()
		ldstr "G2_C606::ClassMethod1345.MI.10583<System.Object>()"
		ldstr "class G3_C1598`1<class BaseClass0> on type class G3_C1598`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1598`1<class BaseClass0>
		callvirt instance string class G3_C1598`1<class BaseClass0>::ClassMethod1344<object>()
		ldstr "G2_C606::ClassMethod1344.MI.10582<System.Object>()"
		ldstr "class G3_C1598`1<class BaseClass0> on type class G3_C1598`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G3_C1598`1<class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C11`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C11`2<class BaseClass1,class BaseClass1>::ClassMethod1345<object>()
		ldstr "G2_C606::ClassMethod1345.MI.10583<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass1> on type class G3_C1598`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C11`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C11`2<class BaseClass1,class BaseClass1>::ClassMethod1344<object>()
		ldstr "G2_C606::ClassMethod1344.MI.10582<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass1> on type class G3_C1598`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C11`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C11`2<class BaseClass1,class BaseClass1>::Method6<object>()
		ldstr "G2_C606::Method6.10579<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass1> on type class G3_C1598`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C11`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C11`2<class BaseClass1,class BaseClass1>::Method5()
		ldstr "G2_C606::Method5.10578()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass1> on type class G3_C1598`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C11`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C11`2<class BaseClass1,class BaseClass1>::Method4()
		ldstr "G1_C11::Method4.4856()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass1> on type class G3_C1598`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C11`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C11`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G3_C1598::Method7.17005<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass1> on type class G3_C1598`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G3_C1598::Method7.MI.17006<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G3_C1598`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G2_C606::Method4.MI.10577()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1598`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G2_C606::Method5.10578()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1598`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G2_C606::Method6.MI.10580<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1598`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G3_C1598::Method7.MI.17006<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1598`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C606`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C606`2<class BaseClass1,class BaseClass0>::ClassMethod2578()
		ldstr "G3_C1598::ClassMethod2578.MI.17010()"
		ldstr "class G2_C606`2<class BaseClass1,class BaseClass0> on type class G3_C1598`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C606`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C606`2<class BaseClass1,class BaseClass0>::Method6<object>()
		ldstr "G2_C606::Method6.10579<System.Object>()"
		ldstr "class G2_C606`2<class BaseClass1,class BaseClass0> on type class G3_C1598`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C606`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C606`2<class BaseClass1,class BaseClass0>::Method5()
		ldstr "G2_C606::Method5.10578()"
		ldstr "class G2_C606`2<class BaseClass1,class BaseClass0> on type class G3_C1598`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C606`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C606`2<class BaseClass1,class BaseClass0>::Method4()
		ldstr "G2_C606::Method4.10576()"
		ldstr "class G2_C606`2<class BaseClass1,class BaseClass0> on type class G3_C1598`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C606`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C606`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G3_C1598::Method7.17005<System.Object>()"
		ldstr "class G2_C606`2<class BaseClass1,class BaseClass0> on type class G3_C1598`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C606`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C606`2<class BaseClass1,class BaseClass0>::ClassMethod1345<object>()
		ldstr "G2_C606::ClassMethod1345.MI.10583<System.Object>()"
		ldstr "class G2_C606`2<class BaseClass1,class BaseClass0> on type class G3_C1598`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C606`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C606`2<class BaseClass1,class BaseClass0>::ClassMethod1344<object>()
		ldstr "G2_C606::ClassMethod1344.MI.10582<System.Object>()"
		ldstr "class G2_C606`2<class BaseClass1,class BaseClass0> on type class G3_C1598`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G2_C606::Method7.MI.10575<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G3_C1598`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method4()
		ldstr "G2_C606::Method4.MI.10577()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1598`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method5()
		ldstr "G2_C606::Method5.10578()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1598`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method6<object>()
		ldstr "G2_C606::Method6.MI.10580<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1598`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G2_C606::Method7.MI.10575<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G3_C1598`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G3_C1598`1<class BaseClass1>
		callvirt instance string class G3_C1598`1<class BaseClass1>::ClassMethod4587<object>()
		ldstr "G3_C1598::ClassMethod4587.17009<System.Object>()"
		ldstr "class G3_C1598`1<class BaseClass1> on type class G3_C1598`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1598`1<class BaseClass1>
		callvirt instance string class G3_C1598`1<class BaseClass1>::ClassMethod4586<object>()
		ldstr "G3_C1598::ClassMethod4586.17008<System.Object>()"
		ldstr "class G3_C1598`1<class BaseClass1> on type class G3_C1598`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1598`1<class BaseClass1>
		callvirt instance string class G3_C1598`1<class BaseClass1>::ClassMethod4585()
		ldstr "G3_C1598::ClassMethod4585.17007()"
		ldstr "class G3_C1598`1<class BaseClass1> on type class G3_C1598`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1598`1<class BaseClass1>
		callvirt instance string class G3_C1598`1<class BaseClass1>::Method7<object>()
		ldstr "G3_C1598::Method7.17005<System.Object>()"
		ldstr "class G3_C1598`1<class BaseClass1> on type class G3_C1598`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1598`1<class BaseClass1>
		callvirt instance string class G3_C1598`1<class BaseClass1>::ClassMethod2578()
		ldstr "G3_C1598::ClassMethod2578.MI.17010()"
		ldstr "class G3_C1598`1<class BaseClass1> on type class G3_C1598`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1598`1<class BaseClass1>
		callvirt instance string class G3_C1598`1<class BaseClass1>::Method6<object>()
		ldstr "G2_C606::Method6.10579<System.Object>()"
		ldstr "class G3_C1598`1<class BaseClass1> on type class G3_C1598`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1598`1<class BaseClass1>
		callvirt instance string class G3_C1598`1<class BaseClass1>::Method5()
		ldstr "G2_C606::Method5.10578()"
		ldstr "class G3_C1598`1<class BaseClass1> on type class G3_C1598`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1598`1<class BaseClass1>
		callvirt instance string class G3_C1598`1<class BaseClass1>::Method4()
		ldstr "G2_C606::Method4.10576()"
		ldstr "class G3_C1598`1<class BaseClass1> on type class G3_C1598`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1598`1<class BaseClass1>
		callvirt instance string class G3_C1598`1<class BaseClass1>::ClassMethod1345<object>()
		ldstr "G2_C606::ClassMethod1345.MI.10583<System.Object>()"
		ldstr "class G3_C1598`1<class BaseClass1> on type class G3_C1598`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1598`1<class BaseClass1>
		callvirt instance string class G3_C1598`1<class BaseClass1>::ClassMethod1344<object>()
		ldstr "G2_C606::ClassMethod1344.MI.10582<System.Object>()"
		ldstr "class G3_C1598`1<class BaseClass1> on type class G3_C1598`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G2_C606`2<class BaseClass0,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C11`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C11`2<class BaseClass0,class BaseClass0>::ClassMethod1345<object>()
		ldstr "G2_C606::ClassMethod1345.MI.10583<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass0> on type class G2_C606`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C11`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C11`2<class BaseClass0,class BaseClass0>::ClassMethod1344<object>()
		ldstr "G2_C606::ClassMethod1344.MI.10582<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass0> on type class G2_C606`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C11`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C11`2<class BaseClass0,class BaseClass0>::Method6<object>()
		ldstr "G2_C606::Method6.10579<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass0> on type class G2_C606`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C11`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C11`2<class BaseClass0,class BaseClass0>::Method5()
		ldstr "G2_C606::Method5.10578()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass0> on type class G2_C606`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C11`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C11`2<class BaseClass0,class BaseClass0>::Method4()
		ldstr "G1_C11::Method4.4856()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass0> on type class G2_C606`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C11`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C11`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G2_C606::Method7.10574<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass0> on type class G2_C606`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G2_C606`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G2_C606::Method4.MI.10577()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C606`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G2_C606::Method5.10578()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C606`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G2_C606::Method6.MI.10580<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C606`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G2_C606::Method7.MI.10575<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G2_C606`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C606::Method7.MI.10575<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C606`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C606`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C606`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C606`2<class BaseClass0,class BaseClass0>::ClassMethod2578()
		ldstr "G2_C606::ClassMethod2578.10581()"
		ldstr "class G2_C606`2<class BaseClass0,class BaseClass0> on type class G2_C606`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C606`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C606`2<class BaseClass0,class BaseClass0>::Method6<object>()
		ldstr "G2_C606::Method6.10579<System.Object>()"
		ldstr "class G2_C606`2<class BaseClass0,class BaseClass0> on type class G2_C606`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C606`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C606`2<class BaseClass0,class BaseClass0>::Method5()
		ldstr "G2_C606::Method5.10578()"
		ldstr "class G2_C606`2<class BaseClass0,class BaseClass0> on type class G2_C606`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C606`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C606`2<class BaseClass0,class BaseClass0>::Method4()
		ldstr "G2_C606::Method4.10576()"
		ldstr "class G2_C606`2<class BaseClass0,class BaseClass0> on type class G2_C606`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C606`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C606`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G2_C606::Method7.10574<System.Object>()"
		ldstr "class G2_C606`2<class BaseClass0,class BaseClass0> on type class G2_C606`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C606`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C606`2<class BaseClass0,class BaseClass0>::ClassMethod1345<object>()
		ldstr "G2_C606::ClassMethod1345.MI.10583<System.Object>()"
		ldstr "class G2_C606`2<class BaseClass0,class BaseClass0> on type class G2_C606`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C606`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C606`2<class BaseClass0,class BaseClass0>::ClassMethod1344<object>()
		ldstr "G2_C606::ClassMethod1344.MI.10582<System.Object>()"
		ldstr "class G2_C606`2<class BaseClass0,class BaseClass0> on type class G2_C606`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G2_C606`2<class BaseClass0,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C11`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C11`2<class BaseClass0,class BaseClass0>::ClassMethod1345<object>()
		ldstr "G2_C606::ClassMethod1345.MI.10583<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass0> on type class G2_C606`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C11`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C11`2<class BaseClass0,class BaseClass0>::ClassMethod1344<object>()
		ldstr "G2_C606::ClassMethod1344.MI.10582<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass0> on type class G2_C606`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C11`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C11`2<class BaseClass0,class BaseClass0>::Method6<object>()
		ldstr "G2_C606::Method6.10579<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass0> on type class G2_C606`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C11`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C11`2<class BaseClass0,class BaseClass0>::Method5()
		ldstr "G2_C606::Method5.10578()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass0> on type class G2_C606`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C11`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C11`2<class BaseClass0,class BaseClass0>::Method4()
		ldstr "G1_C11::Method4.4856()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass0> on type class G2_C606`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C11`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C11`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G2_C606::Method7.10574<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass0> on type class G2_C606`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G2_C606`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G2_C606::Method4.MI.10577()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C606`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G2_C606::Method5.10578()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C606`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G2_C606::Method6.MI.10580<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C606`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G2_C606::Method7.MI.10575<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G2_C606`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C606::Method7.MI.10575<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C606`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C606`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C606`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C606`2<class BaseClass0,class BaseClass1>::ClassMethod2578()
		ldstr "G2_C606::ClassMethod2578.10581()"
		ldstr "class G2_C606`2<class BaseClass0,class BaseClass1> on type class G2_C606`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C606`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C606`2<class BaseClass0,class BaseClass1>::Method6<object>()
		ldstr "G2_C606::Method6.10579<System.Object>()"
		ldstr "class G2_C606`2<class BaseClass0,class BaseClass1> on type class G2_C606`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C606`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C606`2<class BaseClass0,class BaseClass1>::Method5()
		ldstr "G2_C606::Method5.10578()"
		ldstr "class G2_C606`2<class BaseClass0,class BaseClass1> on type class G2_C606`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C606`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C606`2<class BaseClass0,class BaseClass1>::Method4()
		ldstr "G2_C606::Method4.10576()"
		ldstr "class G2_C606`2<class BaseClass0,class BaseClass1> on type class G2_C606`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C606`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C606`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C606::Method7.10574<System.Object>()"
		ldstr "class G2_C606`2<class BaseClass0,class BaseClass1> on type class G2_C606`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C606`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C606`2<class BaseClass0,class BaseClass1>::ClassMethod1345<object>()
		ldstr "G2_C606::ClassMethod1345.MI.10583<System.Object>()"
		ldstr "class G2_C606`2<class BaseClass0,class BaseClass1> on type class G2_C606`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C606`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C606`2<class BaseClass0,class BaseClass1>::ClassMethod1344<object>()
		ldstr "G2_C606::ClassMethod1344.MI.10582<System.Object>()"
		ldstr "class G2_C606`2<class BaseClass0,class BaseClass1> on type class G2_C606`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G2_C606`2<class BaseClass1,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C11`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C11`2<class BaseClass1,class BaseClass1>::ClassMethod1345<object>()
		ldstr "G2_C606::ClassMethod1345.MI.10583<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass1> on type class G2_C606`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C11`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C11`2<class BaseClass1,class BaseClass1>::ClassMethod1344<object>()
		ldstr "G2_C606::ClassMethod1344.MI.10582<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass1> on type class G2_C606`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C11`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C11`2<class BaseClass1,class BaseClass1>::Method6<object>()
		ldstr "G2_C606::Method6.10579<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass1> on type class G2_C606`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C11`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C11`2<class BaseClass1,class BaseClass1>::Method5()
		ldstr "G2_C606::Method5.10578()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass1> on type class G2_C606`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C11`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C11`2<class BaseClass1,class BaseClass1>::Method4()
		ldstr "G1_C11::Method4.4856()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass1> on type class G2_C606`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C11`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C11`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G2_C606::Method7.10574<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass1> on type class G2_C606`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G2_C606::Method7.MI.10575<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C606`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G2_C606::Method4.MI.10577()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C606`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G2_C606::Method5.10578()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C606`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G2_C606::Method6.MI.10580<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C606`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C606::Method7.MI.10575<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C606`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C606`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C606`2<class BaseClass1,class BaseClass0>::ClassMethod2578()
		ldstr "G2_C606::ClassMethod2578.10581()"
		ldstr "class G2_C606`2<class BaseClass1,class BaseClass0> on type class G2_C606`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C606`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C606`2<class BaseClass1,class BaseClass0>::Method6<object>()
		ldstr "G2_C606::Method6.10579<System.Object>()"
		ldstr "class G2_C606`2<class BaseClass1,class BaseClass0> on type class G2_C606`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C606`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C606`2<class BaseClass1,class BaseClass0>::Method5()
		ldstr "G2_C606::Method5.10578()"
		ldstr "class G2_C606`2<class BaseClass1,class BaseClass0> on type class G2_C606`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C606`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C606`2<class BaseClass1,class BaseClass0>::Method4()
		ldstr "G2_C606::Method4.10576()"
		ldstr "class G2_C606`2<class BaseClass1,class BaseClass0> on type class G2_C606`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C606`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C606`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G2_C606::Method7.10574<System.Object>()"
		ldstr "class G2_C606`2<class BaseClass1,class BaseClass0> on type class G2_C606`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C606`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C606`2<class BaseClass1,class BaseClass0>::ClassMethod1345<object>()
		ldstr "G2_C606::ClassMethod1345.MI.10583<System.Object>()"
		ldstr "class G2_C606`2<class BaseClass1,class BaseClass0> on type class G2_C606`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C606`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C606`2<class BaseClass1,class BaseClass0>::ClassMethod1344<object>()
		ldstr "G2_C606::ClassMethod1344.MI.10582<System.Object>()"
		ldstr "class G2_C606`2<class BaseClass1,class BaseClass0> on type class G2_C606`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G2_C606::Method7.MI.10575<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G2_C606`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method4()
		ldstr "G2_C606::Method4.MI.10577()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C606`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method5()
		ldstr "G2_C606::Method5.10578()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C606`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method6<object>()
		ldstr "G2_C606::Method6.MI.10580<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C606`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G2_C606::Method7.MI.10575<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G2_C606`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G2_C606`2<class BaseClass1,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C11`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C11`2<class BaseClass1,class BaseClass1>::ClassMethod1345<object>()
		ldstr "G2_C606::ClassMethod1345.MI.10583<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass1> on type class G2_C606`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C11`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C11`2<class BaseClass1,class BaseClass1>::ClassMethod1344<object>()
		ldstr "G2_C606::ClassMethod1344.MI.10582<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass1> on type class G2_C606`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C11`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C11`2<class BaseClass1,class BaseClass1>::Method6<object>()
		ldstr "G2_C606::Method6.10579<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass1> on type class G2_C606`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C11`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C11`2<class BaseClass1,class BaseClass1>::Method5()
		ldstr "G2_C606::Method5.10578()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass1> on type class G2_C606`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C11`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C11`2<class BaseClass1,class BaseClass1>::Method4()
		ldstr "G1_C11::Method4.4856()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass1> on type class G2_C606`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C11`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C11`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G2_C606::Method7.10574<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass1> on type class G2_C606`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G2_C606::Method7.MI.10575<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C606`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G2_C606::Method4.MI.10577()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C606`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G2_C606::Method5.10578()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C606`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G2_C606::Method6.MI.10580<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C606`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C606::Method7.MI.10575<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C606`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C606`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C606`2<class BaseClass1,class BaseClass1>::ClassMethod2578()
		ldstr "G2_C606::ClassMethod2578.10581()"
		ldstr "class G2_C606`2<class BaseClass1,class BaseClass1> on type class G2_C606`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C606`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C606`2<class BaseClass1,class BaseClass1>::Method6<object>()
		ldstr "G2_C606::Method6.10579<System.Object>()"
		ldstr "class G2_C606`2<class BaseClass1,class BaseClass1> on type class G2_C606`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C606`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C606`2<class BaseClass1,class BaseClass1>::Method5()
		ldstr "G2_C606::Method5.10578()"
		ldstr "class G2_C606`2<class BaseClass1,class BaseClass1> on type class G2_C606`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C606`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C606`2<class BaseClass1,class BaseClass1>::Method4()
		ldstr "G2_C606::Method4.10576()"
		ldstr "class G2_C606`2<class BaseClass1,class BaseClass1> on type class G2_C606`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C606`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C606`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G2_C606::Method7.10574<System.Object>()"
		ldstr "class G2_C606`2<class BaseClass1,class BaseClass1> on type class G2_C606`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C606`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C606`2<class BaseClass1,class BaseClass1>::ClassMethod1345<object>()
		ldstr "G2_C606::ClassMethod1345.MI.10583<System.Object>()"
		ldstr "class G2_C606`2<class BaseClass1,class BaseClass1> on type class G2_C606`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C606`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C606`2<class BaseClass1,class BaseClass1>::ClassMethod1344<object>()
		ldstr "G2_C606::ClassMethod1344.MI.10582<System.Object>()"
		ldstr "class G2_C606`2<class BaseClass1,class BaseClass1> on type class G2_C606`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G2_C606::Method7.MI.10575<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G2_C606`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method4()
		ldstr "G2_C606::Method4.MI.10577()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C606`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method5()
		ldstr "G2_C606::Method5.10578()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C606`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method6<object>()
		ldstr "G2_C606::Method6.MI.10580<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C606`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G2_C606::Method7.MI.10575<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G2_C606`2<class BaseClass1,class BaseClass1>"
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
		newobj instance void class G3_C1598`1<class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C606::ClassMethod1344.MI.10582<System.Object>()#G2_C606::ClassMethod1345.MI.10583<System.Object>()#G1_C11::Method4.4856()#G2_C606::Method5.10578()#G2_C606::Method6.10579<System.Object>()#G3_C1598::Method7.17005<System.Object>()#"
		call void Framework::M.G1_C11.T.T<class BaseClass1,class BaseClass1,class G3_C1598`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C606::ClassMethod1344.MI.10582<System.Object>()#G2_C606::ClassMethod1345.MI.10583<System.Object>()#G1_C11::Method4.4856()#G2_C606::Method5.10578()#G2_C606::Method6.10579<System.Object>()#G3_C1598::Method7.17005<System.Object>()#"
		call void Framework::M.G1_C11.B.T<class BaseClass1,class G3_C1598`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C606::ClassMethod1344.MI.10582<System.Object>()#G2_C606::ClassMethod1345.MI.10583<System.Object>()#G1_C11::Method4.4856()#G2_C606::Method5.10578()#G2_C606::Method6.10579<System.Object>()#G3_C1598::Method7.17005<System.Object>()#"
		call void Framework::M.G1_C11.B.B<class G3_C1598`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C606::Method7.MI.10575<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G3_C1598`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C606::Method7.MI.10575<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass1,class G3_C1598`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C606::Method7.MI.10575<System.Object>()#"
		call void Framework::M.IBase2.B.B<class G3_C1598`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C606::Method4.MI.10577()#G2_C606::Method5.10578()#G2_C606::Method6.MI.10580<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass0,class G3_C1598`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C606::Method4.MI.10577()#G2_C606::Method5.10578()#G2_C606::Method6.MI.10580<System.Object>()#"
		call void Framework::M.IBase1.A<class G3_C1598`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1598::Method7.MI.17006<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G3_C1598`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G3_C1598::Method7.MI.17006<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G3_C1598`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G3_C1598::Method7.MI.17006<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G3_C1598`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C606::ClassMethod1344.MI.10582<System.Object>()#G2_C606::ClassMethod1345.MI.10583<System.Object>()#G3_C1598::ClassMethod2578.MI.17010()#G2_C606::Method4.10576()#G2_C606::Method5.10578()#G2_C606::Method6.10579<System.Object>()#G3_C1598::Method7.17005<System.Object>()#"
		call void Framework::M.G2_C606.T.T<class BaseClass1,class BaseClass0,class G3_C1598`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C606::ClassMethod1344.MI.10582<System.Object>()#G2_C606::ClassMethod1345.MI.10583<System.Object>()#G3_C1598::ClassMethod2578.MI.17010()#G2_C606::Method4.10576()#G2_C606::Method5.10578()#G2_C606::Method6.10579<System.Object>()#G3_C1598::Method7.17005<System.Object>()#"
		call void Framework::M.G2_C606.B.T<class BaseClass0,class G3_C1598`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C606::ClassMethod1344.MI.10582<System.Object>()#G2_C606::ClassMethod1345.MI.10583<System.Object>()#G3_C1598::ClassMethod2578.MI.17010()#G2_C606::Method4.10576()#G2_C606::Method5.10578()#G2_C606::Method6.10579<System.Object>()#G3_C1598::Method7.17005<System.Object>()#"
		call void Framework::M.G2_C606.B.A<class G3_C1598`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C606::Method7.MI.10575<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass0,class G3_C1598`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C606::Method7.MI.10575<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass0,class G3_C1598`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C606::Method7.MI.10575<System.Object>()#"
		call void Framework::M.IBase2.B.A<class G3_C1598`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C606::Method4.MI.10577()#G2_C606::Method5.10578()#G2_C606::Method6.MI.10580<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass1,class G3_C1598`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C606::Method4.MI.10577()#G2_C606::Method5.10578()#G2_C606::Method6.MI.10580<System.Object>()#"
		call void Framework::M.IBase1.B<class G3_C1598`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C606::Method7.MI.10575<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G3_C1598`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C606::Method7.MI.10575<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass0,class G3_C1598`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C606::Method7.MI.10575<System.Object>()#"
		call void Framework::M.IBase2.A.A<class G3_C1598`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C606::ClassMethod1344.MI.10582<System.Object>()#G2_C606::ClassMethod1345.MI.10583<System.Object>()#G3_C1598::ClassMethod2578.MI.17010()#G3_C1598::ClassMethod4585.17007()#G3_C1598::ClassMethod4586.17008<System.Object>()#G3_C1598::ClassMethod4587.17009<System.Object>()#G2_C606::Method4.10576()#G2_C606::Method5.10578()#G2_C606::Method6.10579<System.Object>()#G3_C1598::Method7.17005<System.Object>()#"
		call void Framework::M.G3_C1598.T<class BaseClass0,class G3_C1598`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C606::ClassMethod1344.MI.10582<System.Object>()#G2_C606::ClassMethod1345.MI.10583<System.Object>()#G3_C1598::ClassMethod2578.MI.17010()#G3_C1598::ClassMethod4585.17007()#G3_C1598::ClassMethod4586.17008<System.Object>()#G3_C1598::ClassMethod4587.17009<System.Object>()#G2_C606::Method4.10576()#G2_C606::Method5.10578()#G2_C606::Method6.10579<System.Object>()#G3_C1598::Method7.17005<System.Object>()#"
		call void Framework::M.G3_C1598.A<class G3_C1598`1<class BaseClass0>>(!!0,string)
		newobj instance void class G3_C1598`1<class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C606::ClassMethod1344.MI.10582<System.Object>()#G2_C606::ClassMethod1345.MI.10583<System.Object>()#G1_C11::Method4.4856()#G2_C606::Method5.10578()#G2_C606::Method6.10579<System.Object>()#G3_C1598::Method7.17005<System.Object>()#"
		call void Framework::M.G1_C11.T.T<class BaseClass1,class BaseClass1,class G3_C1598`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C606::ClassMethod1344.MI.10582<System.Object>()#G2_C606::ClassMethod1345.MI.10583<System.Object>()#G1_C11::Method4.4856()#G2_C606::Method5.10578()#G2_C606::Method6.10579<System.Object>()#G3_C1598::Method7.17005<System.Object>()#"
		call void Framework::M.G1_C11.B.T<class BaseClass1,class G3_C1598`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C606::ClassMethod1344.MI.10582<System.Object>()#G2_C606::ClassMethod1345.MI.10583<System.Object>()#G1_C11::Method4.4856()#G2_C606::Method5.10578()#G2_C606::Method6.10579<System.Object>()#G3_C1598::Method7.17005<System.Object>()#"
		call void Framework::M.G1_C11.B.B<class G3_C1598`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1598::Method7.MI.17006<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G3_C1598`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G3_C1598::Method7.MI.17006<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass1,class G3_C1598`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1598::Method7.MI.17006<System.Object>()#"
		call void Framework::M.IBase2.B.B<class G3_C1598`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C606::Method4.MI.10577()#G2_C606::Method5.10578()#G2_C606::Method6.MI.10580<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass0,class G3_C1598`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C606::Method4.MI.10577()#G2_C606::Method5.10578()#G2_C606::Method6.MI.10580<System.Object>()#"
		call void Framework::M.IBase1.A<class G3_C1598`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1598::Method7.MI.17006<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G3_C1598`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G3_C1598::Method7.MI.17006<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G3_C1598`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1598::Method7.MI.17006<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G3_C1598`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C606::ClassMethod1344.MI.10582<System.Object>()#G2_C606::ClassMethod1345.MI.10583<System.Object>()#G3_C1598::ClassMethod2578.MI.17010()#G2_C606::Method4.10576()#G2_C606::Method5.10578()#G2_C606::Method6.10579<System.Object>()#G3_C1598::Method7.17005<System.Object>()#"
		call void Framework::M.G2_C606.T.T<class BaseClass1,class BaseClass0,class G3_C1598`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C606::ClassMethod1344.MI.10582<System.Object>()#G2_C606::ClassMethod1345.MI.10583<System.Object>()#G3_C1598::ClassMethod2578.MI.17010()#G2_C606::Method4.10576()#G2_C606::Method5.10578()#G2_C606::Method6.10579<System.Object>()#G3_C1598::Method7.17005<System.Object>()#"
		call void Framework::M.G2_C606.B.T<class BaseClass0,class G3_C1598`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C606::ClassMethod1344.MI.10582<System.Object>()#G2_C606::ClassMethod1345.MI.10583<System.Object>()#G3_C1598::ClassMethod2578.MI.17010()#G2_C606::Method4.10576()#G2_C606::Method5.10578()#G2_C606::Method6.10579<System.Object>()#G3_C1598::Method7.17005<System.Object>()#"
		call void Framework::M.G2_C606.B.A<class G3_C1598`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C606::Method7.MI.10575<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass0,class G3_C1598`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C606::Method7.MI.10575<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass0,class G3_C1598`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C606::Method7.MI.10575<System.Object>()#"
		call void Framework::M.IBase2.B.A<class G3_C1598`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C606::Method4.MI.10577()#G2_C606::Method5.10578()#G2_C606::Method6.MI.10580<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass1,class G3_C1598`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C606::Method4.MI.10577()#G2_C606::Method5.10578()#G2_C606::Method6.MI.10580<System.Object>()#"
		call void Framework::M.IBase1.B<class G3_C1598`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C606::Method7.MI.10575<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G3_C1598`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C606::Method7.MI.10575<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass0,class G3_C1598`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C606::Method7.MI.10575<System.Object>()#"
		call void Framework::M.IBase2.A.A<class G3_C1598`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C606::ClassMethod1344.MI.10582<System.Object>()#G2_C606::ClassMethod1345.MI.10583<System.Object>()#G3_C1598::ClassMethod2578.MI.17010()#G3_C1598::ClassMethod4585.17007()#G3_C1598::ClassMethod4586.17008<System.Object>()#G3_C1598::ClassMethod4587.17009<System.Object>()#G2_C606::Method4.10576()#G2_C606::Method5.10578()#G2_C606::Method6.10579<System.Object>()#G3_C1598::Method7.17005<System.Object>()#"
		call void Framework::M.G3_C1598.T<class BaseClass1,class G3_C1598`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C606::ClassMethod1344.MI.10582<System.Object>()#G2_C606::ClassMethod1345.MI.10583<System.Object>()#G3_C1598::ClassMethod2578.MI.17010()#G3_C1598::ClassMethod4585.17007()#G3_C1598::ClassMethod4586.17008<System.Object>()#G3_C1598::ClassMethod4587.17009<System.Object>()#G2_C606::Method4.10576()#G2_C606::Method5.10578()#G2_C606::Method6.10579<System.Object>()#G3_C1598::Method7.17005<System.Object>()#"
		call void Framework::M.G3_C1598.B<class G3_C1598`1<class BaseClass1>>(!!0,string)
		newobj instance void class G2_C606`2<class BaseClass0,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C606::ClassMethod1344.MI.10582<System.Object>()#G2_C606::ClassMethod1345.MI.10583<System.Object>()#G1_C11::Method4.4856()#G2_C606::Method5.10578()#G2_C606::Method6.10579<System.Object>()#G2_C606::Method7.10574<System.Object>()#"
		call void Framework::M.G1_C11.T.T<class BaseClass0,class BaseClass0,class G2_C606`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C606::ClassMethod1344.MI.10582<System.Object>()#G2_C606::ClassMethod1345.MI.10583<System.Object>()#G1_C11::Method4.4856()#G2_C606::Method5.10578()#G2_C606::Method6.10579<System.Object>()#G2_C606::Method7.10574<System.Object>()#"
		call void Framework::M.G1_C11.A.T<class BaseClass0,class G2_C606`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C606::ClassMethod1344.MI.10582<System.Object>()#G2_C606::ClassMethod1345.MI.10583<System.Object>()#G1_C11::Method4.4856()#G2_C606::Method5.10578()#G2_C606::Method6.10579<System.Object>()#G2_C606::Method7.10574<System.Object>()#"
		call void Framework::M.G1_C11.A.A<class G2_C606`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass0,class G2_C606`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass0,class G2_C606`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Framework::M.IBase2.B.A<class G2_C606`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C606::Method4.MI.10577()#G2_C606::Method5.10578()#G2_C606::Method6.MI.10580<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass0,class G2_C606`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C606::Method4.MI.10577()#G2_C606::Method5.10578()#G2_C606::Method6.MI.10580<System.Object>()#"
		call void Framework::M.IBase1.A<class G2_C606`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C606::Method7.MI.10575<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G2_C606`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C606::Method7.MI.10575<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass0,class G2_C606`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C606::Method7.MI.10575<System.Object>()#"
		call void Framework::M.IBase2.A.A<class G2_C606`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C606::Method7.MI.10575<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G2_C606`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C606::Method7.MI.10575<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G2_C606`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C606::Method7.MI.10575<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G2_C606`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G2_C606`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass1,class G2_C606`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Framework::M.IBase2.B.B<class G2_C606`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C606::ClassMethod1344.MI.10582<System.Object>()#G2_C606::ClassMethod1345.MI.10583<System.Object>()#G2_C606::ClassMethod2578.10581()#G2_C606::Method4.10576()#G2_C606::Method5.10578()#G2_C606::Method6.10579<System.Object>()#G2_C606::Method7.10574<System.Object>()#"
		call void Framework::M.G2_C606.T.T<class BaseClass0,class BaseClass0,class G2_C606`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C606::ClassMethod1344.MI.10582<System.Object>()#G2_C606::ClassMethod1345.MI.10583<System.Object>()#G2_C606::ClassMethod2578.10581()#G2_C606::Method4.10576()#G2_C606::Method5.10578()#G2_C606::Method6.10579<System.Object>()#G2_C606::Method7.10574<System.Object>()#"
		call void Framework::M.G2_C606.A.T<class BaseClass0,class G2_C606`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C606::ClassMethod1344.MI.10582<System.Object>()#G2_C606::ClassMethod1345.MI.10583<System.Object>()#G2_C606::ClassMethod2578.10581()#G2_C606::Method4.10576()#G2_C606::Method5.10578()#G2_C606::Method6.10579<System.Object>()#G2_C606::Method7.10574<System.Object>()#"
		call void Framework::M.G2_C606.A.A<class G2_C606`2<class BaseClass0,class BaseClass0>>(!!0,string)
		newobj instance void class G2_C606`2<class BaseClass0,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C606::ClassMethod1344.MI.10582<System.Object>()#G2_C606::ClassMethod1345.MI.10583<System.Object>()#G1_C11::Method4.4856()#G2_C606::Method5.10578()#G2_C606::Method6.10579<System.Object>()#G2_C606::Method7.10574<System.Object>()#"
		call void Framework::M.G1_C11.T.T<class BaseClass0,class BaseClass0,class G2_C606`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C606::ClassMethod1344.MI.10582<System.Object>()#G2_C606::ClassMethod1345.MI.10583<System.Object>()#G1_C11::Method4.4856()#G2_C606::Method5.10578()#G2_C606::Method6.10579<System.Object>()#G2_C606::Method7.10574<System.Object>()#"
		call void Framework::M.G1_C11.A.T<class BaseClass0,class G2_C606`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C606::ClassMethod1344.MI.10582<System.Object>()#G2_C606::ClassMethod1345.MI.10583<System.Object>()#G1_C11::Method4.4856()#G2_C606::Method5.10578()#G2_C606::Method6.10579<System.Object>()#G2_C606::Method7.10574<System.Object>()#"
		call void Framework::M.G1_C11.A.A<class G2_C606`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass0,class G2_C606`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass0,class G2_C606`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Framework::M.IBase2.B.A<class G2_C606`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C606::Method4.MI.10577()#G2_C606::Method5.10578()#G2_C606::Method6.MI.10580<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass0,class G2_C606`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C606::Method4.MI.10577()#G2_C606::Method5.10578()#G2_C606::Method6.MI.10580<System.Object>()#"
		call void Framework::M.IBase1.A<class G2_C606`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C606::Method7.MI.10575<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G2_C606`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C606::Method7.MI.10575<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass0,class G2_C606`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C606::Method7.MI.10575<System.Object>()#"
		call void Framework::M.IBase2.A.A<class G2_C606`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C606::Method7.MI.10575<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G2_C606`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C606::Method7.MI.10575<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G2_C606`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C606::Method7.MI.10575<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G2_C606`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G2_C606`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass1,class G2_C606`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Framework::M.IBase2.B.B<class G2_C606`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C606::ClassMethod1344.MI.10582<System.Object>()#G2_C606::ClassMethod1345.MI.10583<System.Object>()#G2_C606::ClassMethod2578.10581()#G2_C606::Method4.10576()#G2_C606::Method5.10578()#G2_C606::Method6.10579<System.Object>()#G2_C606::Method7.10574<System.Object>()#"
		call void Framework::M.G2_C606.T.T<class BaseClass0,class BaseClass1,class G2_C606`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C606::ClassMethod1344.MI.10582<System.Object>()#G2_C606::ClassMethod1345.MI.10583<System.Object>()#G2_C606::ClassMethod2578.10581()#G2_C606::Method4.10576()#G2_C606::Method5.10578()#G2_C606::Method6.10579<System.Object>()#G2_C606::Method7.10574<System.Object>()#"
		call void Framework::M.G2_C606.A.T<class BaseClass1,class G2_C606`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C606::ClassMethod1344.MI.10582<System.Object>()#G2_C606::ClassMethod1345.MI.10583<System.Object>()#G2_C606::ClassMethod2578.10581()#G2_C606::Method4.10576()#G2_C606::Method5.10578()#G2_C606::Method6.10579<System.Object>()#G2_C606::Method7.10574<System.Object>()#"
		call void Framework::M.G2_C606.A.B<class G2_C606`2<class BaseClass0,class BaseClass1>>(!!0,string)
		newobj instance void class G2_C606`2<class BaseClass1,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C606::ClassMethod1344.MI.10582<System.Object>()#G2_C606::ClassMethod1345.MI.10583<System.Object>()#G1_C11::Method4.4856()#G2_C606::Method5.10578()#G2_C606::Method6.10579<System.Object>()#G2_C606::Method7.10574<System.Object>()#"
		call void Framework::M.G1_C11.T.T<class BaseClass1,class BaseClass1,class G2_C606`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C606::ClassMethod1344.MI.10582<System.Object>()#G2_C606::ClassMethod1345.MI.10583<System.Object>()#G1_C11::Method4.4856()#G2_C606::Method5.10578()#G2_C606::Method6.10579<System.Object>()#G2_C606::Method7.10574<System.Object>()#"
		call void Framework::M.G1_C11.B.T<class BaseClass1,class G2_C606`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C606::ClassMethod1344.MI.10582<System.Object>()#G2_C606::ClassMethod1345.MI.10583<System.Object>()#G1_C11::Method4.4856()#G2_C606::Method5.10578()#G2_C606::Method6.10579<System.Object>()#G2_C606::Method7.10574<System.Object>()#"
		call void Framework::M.G1_C11.B.B<class G2_C606`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C606::Method7.MI.10575<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G2_C606`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C606::Method7.MI.10575<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass1,class G2_C606`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C606::Method7.MI.10575<System.Object>()#"
		call void Framework::M.IBase2.B.B<class G2_C606`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C606::Method4.MI.10577()#G2_C606::Method5.10578()#G2_C606::Method6.MI.10580<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass0,class G2_C606`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C606::Method4.MI.10577()#G2_C606::Method5.10578()#G2_C606::Method6.MI.10580<System.Object>()#"
		call void Framework::M.IBase1.A<class G2_C606`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C606::Method7.MI.10575<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G2_C606`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C606::Method7.MI.10575<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G2_C606`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C606::Method7.MI.10575<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G2_C606`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C606::ClassMethod1344.MI.10582<System.Object>()#G2_C606::ClassMethod1345.MI.10583<System.Object>()#G2_C606::ClassMethod2578.10581()#G2_C606::Method4.10576()#G2_C606::Method5.10578()#G2_C606::Method6.10579<System.Object>()#G2_C606::Method7.10574<System.Object>()#"
		call void Framework::M.G2_C606.T.T<class BaseClass1,class BaseClass0,class G2_C606`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C606::ClassMethod1344.MI.10582<System.Object>()#G2_C606::ClassMethod1345.MI.10583<System.Object>()#G2_C606::ClassMethod2578.10581()#G2_C606::Method4.10576()#G2_C606::Method5.10578()#G2_C606::Method6.10579<System.Object>()#G2_C606::Method7.10574<System.Object>()#"
		call void Framework::M.G2_C606.B.T<class BaseClass0,class G2_C606`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C606::ClassMethod1344.MI.10582<System.Object>()#G2_C606::ClassMethod1345.MI.10583<System.Object>()#G2_C606::ClassMethod2578.10581()#G2_C606::Method4.10576()#G2_C606::Method5.10578()#G2_C606::Method6.10579<System.Object>()#G2_C606::Method7.10574<System.Object>()#"
		call void Framework::M.G2_C606.B.A<class G2_C606`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C606::Method7.MI.10575<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass0,class G2_C606`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C606::Method7.MI.10575<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass0,class G2_C606`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C606::Method7.MI.10575<System.Object>()#"
		call void Framework::M.IBase2.B.A<class G2_C606`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C606::Method4.MI.10577()#G2_C606::Method5.10578()#G2_C606::Method6.MI.10580<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass1,class G2_C606`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C606::Method4.MI.10577()#G2_C606::Method5.10578()#G2_C606::Method6.MI.10580<System.Object>()#"
		call void Framework::M.IBase1.B<class G2_C606`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C606::Method7.MI.10575<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G2_C606`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C606::Method7.MI.10575<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass0,class G2_C606`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C606::Method7.MI.10575<System.Object>()#"
		call void Framework::M.IBase2.A.A<class G2_C606`2<class BaseClass1,class BaseClass0>>(!!0,string)
		newobj instance void class G2_C606`2<class BaseClass1,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C606::ClassMethod1344.MI.10582<System.Object>()#G2_C606::ClassMethod1345.MI.10583<System.Object>()#G1_C11::Method4.4856()#G2_C606::Method5.10578()#G2_C606::Method6.10579<System.Object>()#G2_C606::Method7.10574<System.Object>()#"
		call void Framework::M.G1_C11.T.T<class BaseClass1,class BaseClass1,class G2_C606`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C606::ClassMethod1344.MI.10582<System.Object>()#G2_C606::ClassMethod1345.MI.10583<System.Object>()#G1_C11::Method4.4856()#G2_C606::Method5.10578()#G2_C606::Method6.10579<System.Object>()#G2_C606::Method7.10574<System.Object>()#"
		call void Framework::M.G1_C11.B.T<class BaseClass1,class G2_C606`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C606::ClassMethod1344.MI.10582<System.Object>()#G2_C606::ClassMethod1345.MI.10583<System.Object>()#G1_C11::Method4.4856()#G2_C606::Method5.10578()#G2_C606::Method6.10579<System.Object>()#G2_C606::Method7.10574<System.Object>()#"
		call void Framework::M.G1_C11.B.B<class G2_C606`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C606::Method7.MI.10575<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G2_C606`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C606::Method7.MI.10575<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass1,class G2_C606`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C606::Method7.MI.10575<System.Object>()#"
		call void Framework::M.IBase2.B.B<class G2_C606`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C606::Method4.MI.10577()#G2_C606::Method5.10578()#G2_C606::Method6.MI.10580<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass0,class G2_C606`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C606::Method4.MI.10577()#G2_C606::Method5.10578()#G2_C606::Method6.MI.10580<System.Object>()#"
		call void Framework::M.IBase1.A<class G2_C606`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C606::Method7.MI.10575<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G2_C606`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C606::Method7.MI.10575<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G2_C606`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C606::Method7.MI.10575<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G2_C606`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C606::ClassMethod1344.MI.10582<System.Object>()#G2_C606::ClassMethod1345.MI.10583<System.Object>()#G2_C606::ClassMethod2578.10581()#G2_C606::Method4.10576()#G2_C606::Method5.10578()#G2_C606::Method6.10579<System.Object>()#G2_C606::Method7.10574<System.Object>()#"
		call void Framework::M.G2_C606.T.T<class BaseClass1,class BaseClass1,class G2_C606`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C606::ClassMethod1344.MI.10582<System.Object>()#G2_C606::ClassMethod1345.MI.10583<System.Object>()#G2_C606::ClassMethod2578.10581()#G2_C606::Method4.10576()#G2_C606::Method5.10578()#G2_C606::Method6.10579<System.Object>()#G2_C606::Method7.10574<System.Object>()#"
		call void Framework::M.G2_C606.B.T<class BaseClass1,class G2_C606`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C606::ClassMethod1344.MI.10582<System.Object>()#G2_C606::ClassMethod1345.MI.10583<System.Object>()#G2_C606::ClassMethod2578.10581()#G2_C606::Method4.10576()#G2_C606::Method5.10578()#G2_C606::Method6.10579<System.Object>()#G2_C606::Method7.10574<System.Object>()#"
		call void Framework::M.G2_C606.B.B<class G2_C606`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C606::Method7.MI.10575<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass0,class G2_C606`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C606::Method7.MI.10575<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass0,class G2_C606`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C606::Method7.MI.10575<System.Object>()#"
		call void Framework::M.IBase2.B.A<class G2_C606`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C606::Method4.MI.10577()#G2_C606::Method5.10578()#G2_C606::Method6.MI.10580<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass1,class G2_C606`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C606::Method4.MI.10577()#G2_C606::Method5.10578()#G2_C606::Method6.MI.10580<System.Object>()#"
		call void Framework::M.IBase1.B<class G2_C606`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C606::Method7.MI.10575<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G2_C606`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C606::Method7.MI.10575<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass0,class G2_C606`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C606::Method7.MI.10575<System.Object>()#"
		call void Framework::M.IBase2.A.A<class G2_C606`2<class BaseClass1,class BaseClass1>>(!!0,string)
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
		newobj instance void class G3_C1598`1<class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C11`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass1,class BaseClass1>::ClassMethod1345<object>()
		calli default string(class G3_C1598`1<class BaseClass0>)
		ldstr "G2_C606::ClassMethod1345.MI.10583<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass1> on type class G3_C1598`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C11`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass1,class BaseClass1>::ClassMethod1344<object>()
		calli default string(class G3_C1598`1<class BaseClass0>)
		ldstr "G2_C606::ClassMethod1344.MI.10582<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass1> on type class G3_C1598`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C11`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass1,class BaseClass1>::Method6<object>()
		calli default string(class G3_C1598`1<class BaseClass0>)
		ldstr "G2_C606::Method6.10579<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass1> on type class G3_C1598`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C11`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass1,class BaseClass1>::Method5()
		calli default string(class G3_C1598`1<class BaseClass0>)
		ldstr "G2_C606::Method5.10578()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass1> on type class G3_C1598`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C11`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass1,class BaseClass1>::Method4()
		calli default string(class G3_C1598`1<class BaseClass0>)
		ldstr "G1_C11::Method4.4856()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass1> on type class G3_C1598`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C11`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1598`1<class BaseClass0>)
		ldstr "G3_C1598::Method7.17005<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass1> on type class G3_C1598`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1598`1<class BaseClass0>)
		ldstr "G2_C606::Method7.MI.10575<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G3_C1598`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G3_C1598`1<class BaseClass0>)
		ldstr "G2_C606::Method4.MI.10577()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1598`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G3_C1598`1<class BaseClass0>)
		ldstr "G2_C606::Method5.10578()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1598`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G3_C1598`1<class BaseClass0>)
		ldstr "G2_C606::Method6.MI.10580<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1598`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1598`1<class BaseClass0>)
		ldstr "G3_C1598::Method7.MI.17006<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1598`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C606`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C606`2<class BaseClass1,class BaseClass0>::ClassMethod2578()
		calli default string(class G3_C1598`1<class BaseClass0>)
		ldstr "G3_C1598::ClassMethod2578.MI.17010()"
		ldstr "class G2_C606`2<class BaseClass1,class BaseClass0> on type class G3_C1598`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C606`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C606`2<class BaseClass1,class BaseClass0>::Method6<object>()
		calli default string(class G3_C1598`1<class BaseClass0>)
		ldstr "G2_C606::Method6.10579<System.Object>()"
		ldstr "class G2_C606`2<class BaseClass1,class BaseClass0> on type class G3_C1598`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C606`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C606`2<class BaseClass1,class BaseClass0>::Method5()
		calli default string(class G3_C1598`1<class BaseClass0>)
		ldstr "G2_C606::Method5.10578()"
		ldstr "class G2_C606`2<class BaseClass1,class BaseClass0> on type class G3_C1598`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C606`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C606`2<class BaseClass1,class BaseClass0>::Method4()
		calli default string(class G3_C1598`1<class BaseClass0>)
		ldstr "G2_C606::Method4.10576()"
		ldstr "class G2_C606`2<class BaseClass1,class BaseClass0> on type class G3_C1598`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C606`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C606`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1598`1<class BaseClass0>)
		ldstr "G3_C1598::Method7.17005<System.Object>()"
		ldstr "class G2_C606`2<class BaseClass1,class BaseClass0> on type class G3_C1598`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C606`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C606`2<class BaseClass1,class BaseClass0>::ClassMethod1345<object>()
		calli default string(class G3_C1598`1<class BaseClass0>)
		ldstr "G2_C606::ClassMethod1345.MI.10583<System.Object>()"
		ldstr "class G2_C606`2<class BaseClass1,class BaseClass0> on type class G3_C1598`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C606`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C606`2<class BaseClass1,class BaseClass0>::ClassMethod1344<object>()
		calli default string(class G3_C1598`1<class BaseClass0>)
		ldstr "G2_C606::ClassMethod1344.MI.10582<System.Object>()"
		ldstr "class G2_C606`2<class BaseClass1,class BaseClass0> on type class G3_C1598`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1598`1<class BaseClass0>)
		ldstr "G2_C606::Method7.MI.10575<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G3_C1598`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method4()
		calli default string(class G3_C1598`1<class BaseClass0>)
		ldstr "G2_C606::Method4.MI.10577()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1598`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method5()
		calli default string(class G3_C1598`1<class BaseClass0>)
		ldstr "G2_C606::Method5.10578()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1598`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method6<object>()
		calli default string(class G3_C1598`1<class BaseClass0>)
		ldstr "G2_C606::Method6.MI.10580<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1598`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1598`1<class BaseClass0>)
		ldstr "G2_C606::Method7.MI.10575<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G3_C1598`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1598`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1598`1<class BaseClass0>::ClassMethod4587<object>()
		calli default string(class G3_C1598`1<class BaseClass0>)
		ldstr "G3_C1598::ClassMethod4587.17009<System.Object>()"
		ldstr "class G3_C1598`1<class BaseClass0> on type class G3_C1598`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1598`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1598`1<class BaseClass0>::ClassMethod4586<object>()
		calli default string(class G3_C1598`1<class BaseClass0>)
		ldstr "G3_C1598::ClassMethod4586.17008<System.Object>()"
		ldstr "class G3_C1598`1<class BaseClass0> on type class G3_C1598`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1598`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1598`1<class BaseClass0>::ClassMethod4585()
		calli default string(class G3_C1598`1<class BaseClass0>)
		ldstr "G3_C1598::ClassMethod4585.17007()"
		ldstr "class G3_C1598`1<class BaseClass0> on type class G3_C1598`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1598`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1598`1<class BaseClass0>::Method7<object>()
		calli default string(class G3_C1598`1<class BaseClass0>)
		ldstr "G3_C1598::Method7.17005<System.Object>()"
		ldstr "class G3_C1598`1<class BaseClass0> on type class G3_C1598`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1598`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1598`1<class BaseClass0>::ClassMethod2578()
		calli default string(class G3_C1598`1<class BaseClass0>)
		ldstr "G3_C1598::ClassMethod2578.MI.17010()"
		ldstr "class G3_C1598`1<class BaseClass0> on type class G3_C1598`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1598`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1598`1<class BaseClass0>::Method6<object>()
		calli default string(class G3_C1598`1<class BaseClass0>)
		ldstr "G2_C606::Method6.10579<System.Object>()"
		ldstr "class G3_C1598`1<class BaseClass0> on type class G3_C1598`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1598`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1598`1<class BaseClass0>::Method5()
		calli default string(class G3_C1598`1<class BaseClass0>)
		ldstr "G2_C606::Method5.10578()"
		ldstr "class G3_C1598`1<class BaseClass0> on type class G3_C1598`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1598`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1598`1<class BaseClass0>::Method4()
		calli default string(class G3_C1598`1<class BaseClass0>)
		ldstr "G2_C606::Method4.10576()"
		ldstr "class G3_C1598`1<class BaseClass0> on type class G3_C1598`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1598`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1598`1<class BaseClass0>::ClassMethod1345<object>()
		calli default string(class G3_C1598`1<class BaseClass0>)
		ldstr "G2_C606::ClassMethod1345.MI.10583<System.Object>()"
		ldstr "class G3_C1598`1<class BaseClass0> on type class G3_C1598`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1598`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1598`1<class BaseClass0>::ClassMethod1344<object>()
		calli default string(class G3_C1598`1<class BaseClass0>)
		ldstr "G2_C606::ClassMethod1344.MI.10582<System.Object>()"
		ldstr "class G3_C1598`1<class BaseClass0> on type class G3_C1598`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G3_C1598`1<class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C11`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass1,class BaseClass1>::ClassMethod1345<object>()
		calli default string(class G3_C1598`1<class BaseClass1>)
		ldstr "G2_C606::ClassMethod1345.MI.10583<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass1> on type class G3_C1598`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C11`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass1,class BaseClass1>::ClassMethod1344<object>()
		calli default string(class G3_C1598`1<class BaseClass1>)
		ldstr "G2_C606::ClassMethod1344.MI.10582<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass1> on type class G3_C1598`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C11`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass1,class BaseClass1>::Method6<object>()
		calli default string(class G3_C1598`1<class BaseClass1>)
		ldstr "G2_C606::Method6.10579<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass1> on type class G3_C1598`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C11`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass1,class BaseClass1>::Method5()
		calli default string(class G3_C1598`1<class BaseClass1>)
		ldstr "G2_C606::Method5.10578()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass1> on type class G3_C1598`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C11`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass1,class BaseClass1>::Method4()
		calli default string(class G3_C1598`1<class BaseClass1>)
		ldstr "G1_C11::Method4.4856()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass1> on type class G3_C1598`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C11`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1598`1<class BaseClass1>)
		ldstr "G3_C1598::Method7.17005<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass1> on type class G3_C1598`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1598`1<class BaseClass1>)
		ldstr "G3_C1598::Method7.MI.17006<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G3_C1598`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G3_C1598`1<class BaseClass1>)
		ldstr "G2_C606::Method4.MI.10577()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1598`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G3_C1598`1<class BaseClass1>)
		ldstr "G2_C606::Method5.10578()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1598`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G3_C1598`1<class BaseClass1>)
		ldstr "G2_C606::Method6.MI.10580<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1598`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1598`1<class BaseClass1>)
		ldstr "G3_C1598::Method7.MI.17006<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1598`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C606`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C606`2<class BaseClass1,class BaseClass0>::ClassMethod2578()
		calli default string(class G3_C1598`1<class BaseClass1>)
		ldstr "G3_C1598::ClassMethod2578.MI.17010()"
		ldstr "class G2_C606`2<class BaseClass1,class BaseClass0> on type class G3_C1598`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C606`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C606`2<class BaseClass1,class BaseClass0>::Method6<object>()
		calli default string(class G3_C1598`1<class BaseClass1>)
		ldstr "G2_C606::Method6.10579<System.Object>()"
		ldstr "class G2_C606`2<class BaseClass1,class BaseClass0> on type class G3_C1598`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C606`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C606`2<class BaseClass1,class BaseClass0>::Method5()
		calli default string(class G3_C1598`1<class BaseClass1>)
		ldstr "G2_C606::Method5.10578()"
		ldstr "class G2_C606`2<class BaseClass1,class BaseClass0> on type class G3_C1598`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C606`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C606`2<class BaseClass1,class BaseClass0>::Method4()
		calli default string(class G3_C1598`1<class BaseClass1>)
		ldstr "G2_C606::Method4.10576()"
		ldstr "class G2_C606`2<class BaseClass1,class BaseClass0> on type class G3_C1598`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C606`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C606`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1598`1<class BaseClass1>)
		ldstr "G3_C1598::Method7.17005<System.Object>()"
		ldstr "class G2_C606`2<class BaseClass1,class BaseClass0> on type class G3_C1598`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C606`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C606`2<class BaseClass1,class BaseClass0>::ClassMethod1345<object>()
		calli default string(class G3_C1598`1<class BaseClass1>)
		ldstr "G2_C606::ClassMethod1345.MI.10583<System.Object>()"
		ldstr "class G2_C606`2<class BaseClass1,class BaseClass0> on type class G3_C1598`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C606`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C606`2<class BaseClass1,class BaseClass0>::ClassMethod1344<object>()
		calli default string(class G3_C1598`1<class BaseClass1>)
		ldstr "G2_C606::ClassMethod1344.MI.10582<System.Object>()"
		ldstr "class G2_C606`2<class BaseClass1,class BaseClass0> on type class G3_C1598`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1598`1<class BaseClass1>)
		ldstr "G2_C606::Method7.MI.10575<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G3_C1598`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method4()
		calli default string(class G3_C1598`1<class BaseClass1>)
		ldstr "G2_C606::Method4.MI.10577()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1598`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method5()
		calli default string(class G3_C1598`1<class BaseClass1>)
		ldstr "G2_C606::Method5.10578()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1598`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method6<object>()
		calli default string(class G3_C1598`1<class BaseClass1>)
		ldstr "G2_C606::Method6.MI.10580<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1598`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1598`1<class BaseClass1>)
		ldstr "G2_C606::Method7.MI.10575<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G3_C1598`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1598`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1598`1<class BaseClass1>::ClassMethod4587<object>()
		calli default string(class G3_C1598`1<class BaseClass1>)
		ldstr "G3_C1598::ClassMethod4587.17009<System.Object>()"
		ldstr "class G3_C1598`1<class BaseClass1> on type class G3_C1598`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1598`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1598`1<class BaseClass1>::ClassMethod4586<object>()
		calli default string(class G3_C1598`1<class BaseClass1>)
		ldstr "G3_C1598::ClassMethod4586.17008<System.Object>()"
		ldstr "class G3_C1598`1<class BaseClass1> on type class G3_C1598`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1598`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1598`1<class BaseClass1>::ClassMethod4585()
		calli default string(class G3_C1598`1<class BaseClass1>)
		ldstr "G3_C1598::ClassMethod4585.17007()"
		ldstr "class G3_C1598`1<class BaseClass1> on type class G3_C1598`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1598`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1598`1<class BaseClass1>::Method7<object>()
		calli default string(class G3_C1598`1<class BaseClass1>)
		ldstr "G3_C1598::Method7.17005<System.Object>()"
		ldstr "class G3_C1598`1<class BaseClass1> on type class G3_C1598`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1598`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1598`1<class BaseClass1>::ClassMethod2578()
		calli default string(class G3_C1598`1<class BaseClass1>)
		ldstr "G3_C1598::ClassMethod2578.MI.17010()"
		ldstr "class G3_C1598`1<class BaseClass1> on type class G3_C1598`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1598`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1598`1<class BaseClass1>::Method6<object>()
		calli default string(class G3_C1598`1<class BaseClass1>)
		ldstr "G2_C606::Method6.10579<System.Object>()"
		ldstr "class G3_C1598`1<class BaseClass1> on type class G3_C1598`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1598`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1598`1<class BaseClass1>::Method5()
		calli default string(class G3_C1598`1<class BaseClass1>)
		ldstr "G2_C606::Method5.10578()"
		ldstr "class G3_C1598`1<class BaseClass1> on type class G3_C1598`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1598`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1598`1<class BaseClass1>::Method4()
		calli default string(class G3_C1598`1<class BaseClass1>)
		ldstr "G2_C606::Method4.10576()"
		ldstr "class G3_C1598`1<class BaseClass1> on type class G3_C1598`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1598`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1598`1<class BaseClass1>::ClassMethod1345<object>()
		calli default string(class G3_C1598`1<class BaseClass1>)
		ldstr "G2_C606::ClassMethod1345.MI.10583<System.Object>()"
		ldstr "class G3_C1598`1<class BaseClass1> on type class G3_C1598`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1598`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1598`1<class BaseClass1>::ClassMethod1344<object>()
		calli default string(class G3_C1598`1<class BaseClass1>)
		ldstr "G2_C606::ClassMethod1344.MI.10582<System.Object>()"
		ldstr "class G3_C1598`1<class BaseClass1> on type class G3_C1598`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G2_C606`2<class BaseClass0,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C11`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass0,class BaseClass0>::ClassMethod1345<object>()
		calli default string(class G2_C606`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C606::ClassMethod1345.MI.10583<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass0> on type class G2_C606`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C11`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass0,class BaseClass0>::ClassMethod1344<object>()
		calli default string(class G2_C606`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C606::ClassMethod1344.MI.10582<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass0> on type class G2_C606`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C11`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass0,class BaseClass0>::Method6<object>()
		calli default string(class G2_C606`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C606::Method6.10579<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass0> on type class G2_C606`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C11`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass0,class BaseClass0>::Method5()
		calli default string(class G2_C606`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C606::Method5.10578()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass0> on type class G2_C606`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C11`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass0,class BaseClass0>::Method4()
		calli default string(class G2_C606`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C11::Method4.4856()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass0> on type class G2_C606`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C11`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G2_C606`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C606::Method7.10574<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass0> on type class G2_C606`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G2_C606`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G2_C606`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G2_C606`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C606::Method4.MI.10577()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C606`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G2_C606`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C606::Method5.10578()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C606`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G2_C606`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C606::Method6.MI.10580<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C606`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G2_C606`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C606::Method7.MI.10575<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G2_C606`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C606`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C606::Method7.MI.10575<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C606`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C606`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C606`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C606`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C606`2<class BaseClass0,class BaseClass0>::ClassMethod2578()
		calli default string(class G2_C606`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C606::ClassMethod2578.10581()"
		ldstr "class G2_C606`2<class BaseClass0,class BaseClass0> on type class G2_C606`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C606`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C606`2<class BaseClass0,class BaseClass0>::Method6<object>()
		calli default string(class G2_C606`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C606::Method6.10579<System.Object>()"
		ldstr "class G2_C606`2<class BaseClass0,class BaseClass0> on type class G2_C606`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C606`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C606`2<class BaseClass0,class BaseClass0>::Method5()
		calli default string(class G2_C606`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C606::Method5.10578()"
		ldstr "class G2_C606`2<class BaseClass0,class BaseClass0> on type class G2_C606`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C606`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C606`2<class BaseClass0,class BaseClass0>::Method4()
		calli default string(class G2_C606`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C606::Method4.10576()"
		ldstr "class G2_C606`2<class BaseClass0,class BaseClass0> on type class G2_C606`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C606`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C606`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G2_C606`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C606::Method7.10574<System.Object>()"
		ldstr "class G2_C606`2<class BaseClass0,class BaseClass0> on type class G2_C606`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C606`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C606`2<class BaseClass0,class BaseClass0>::ClassMethod1345<object>()
		calli default string(class G2_C606`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C606::ClassMethod1345.MI.10583<System.Object>()"
		ldstr "class G2_C606`2<class BaseClass0,class BaseClass0> on type class G2_C606`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C606`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C606`2<class BaseClass0,class BaseClass0>::ClassMethod1344<object>()
		calli default string(class G2_C606`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C606::ClassMethod1344.MI.10582<System.Object>()"
		ldstr "class G2_C606`2<class BaseClass0,class BaseClass0> on type class G2_C606`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G2_C606`2<class BaseClass0,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C11`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass0,class BaseClass0>::ClassMethod1345<object>()
		calli default string(class G2_C606`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C606::ClassMethod1345.MI.10583<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass0> on type class G2_C606`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C11`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass0,class BaseClass0>::ClassMethod1344<object>()
		calli default string(class G2_C606`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C606::ClassMethod1344.MI.10582<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass0> on type class G2_C606`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C11`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass0,class BaseClass0>::Method6<object>()
		calli default string(class G2_C606`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C606::Method6.10579<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass0> on type class G2_C606`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C11`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass0,class BaseClass0>::Method5()
		calli default string(class G2_C606`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C606::Method5.10578()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass0> on type class G2_C606`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C11`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass0,class BaseClass0>::Method4()
		calli default string(class G2_C606`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C11::Method4.4856()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass0> on type class G2_C606`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C11`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G2_C606`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C606::Method7.10574<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass0> on type class G2_C606`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G2_C606`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G2_C606`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G2_C606`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C606::Method4.MI.10577()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C606`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G2_C606`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C606::Method5.10578()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C606`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G2_C606`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C606::Method6.MI.10580<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C606`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G2_C606`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C606::Method7.MI.10575<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G2_C606`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C606`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C606::Method7.MI.10575<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C606`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C606`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C606`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C606`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C606`2<class BaseClass0,class BaseClass1>::ClassMethod2578()
		calli default string(class G2_C606`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C606::ClassMethod2578.10581()"
		ldstr "class G2_C606`2<class BaseClass0,class BaseClass1> on type class G2_C606`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C606`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C606`2<class BaseClass0,class BaseClass1>::Method6<object>()
		calli default string(class G2_C606`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C606::Method6.10579<System.Object>()"
		ldstr "class G2_C606`2<class BaseClass0,class BaseClass1> on type class G2_C606`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C606`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C606`2<class BaseClass0,class BaseClass1>::Method5()
		calli default string(class G2_C606`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C606::Method5.10578()"
		ldstr "class G2_C606`2<class BaseClass0,class BaseClass1> on type class G2_C606`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C606`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C606`2<class BaseClass0,class BaseClass1>::Method4()
		calli default string(class G2_C606`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C606::Method4.10576()"
		ldstr "class G2_C606`2<class BaseClass0,class BaseClass1> on type class G2_C606`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C606`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C606`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C606`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C606::Method7.10574<System.Object>()"
		ldstr "class G2_C606`2<class BaseClass0,class BaseClass1> on type class G2_C606`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C606`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C606`2<class BaseClass0,class BaseClass1>::ClassMethod1345<object>()
		calli default string(class G2_C606`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C606::ClassMethod1345.MI.10583<System.Object>()"
		ldstr "class G2_C606`2<class BaseClass0,class BaseClass1> on type class G2_C606`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C606`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C606`2<class BaseClass0,class BaseClass1>::ClassMethod1344<object>()
		calli default string(class G2_C606`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C606::ClassMethod1344.MI.10582<System.Object>()"
		ldstr "class G2_C606`2<class BaseClass0,class BaseClass1> on type class G2_C606`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G2_C606`2<class BaseClass1,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C11`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass1,class BaseClass1>::ClassMethod1345<object>()
		calli default string(class G2_C606`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C606::ClassMethod1345.MI.10583<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass1> on type class G2_C606`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C11`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass1,class BaseClass1>::ClassMethod1344<object>()
		calli default string(class G2_C606`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C606::ClassMethod1344.MI.10582<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass1> on type class G2_C606`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C11`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass1,class BaseClass1>::Method6<object>()
		calli default string(class G2_C606`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C606::Method6.10579<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass1> on type class G2_C606`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C11`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass1,class BaseClass1>::Method5()
		calli default string(class G2_C606`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C606::Method5.10578()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass1> on type class G2_C606`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C11`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass1,class BaseClass1>::Method4()
		calli default string(class G2_C606`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C11::Method4.4856()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass1> on type class G2_C606`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C11`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C606`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C606::Method7.10574<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass1> on type class G2_C606`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C606`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C606::Method7.MI.10575<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C606`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G2_C606`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C606::Method4.MI.10577()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C606`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G2_C606`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C606::Method5.10578()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C606`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G2_C606`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C606::Method6.MI.10580<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C606`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C606`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C606::Method7.MI.10575<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C606`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C606`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C606`2<class BaseClass1,class BaseClass0>::ClassMethod2578()
		calli default string(class G2_C606`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C606::ClassMethod2578.10581()"
		ldstr "class G2_C606`2<class BaseClass1,class BaseClass0> on type class G2_C606`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C606`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C606`2<class BaseClass1,class BaseClass0>::Method6<object>()
		calli default string(class G2_C606`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C606::Method6.10579<System.Object>()"
		ldstr "class G2_C606`2<class BaseClass1,class BaseClass0> on type class G2_C606`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C606`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C606`2<class BaseClass1,class BaseClass0>::Method5()
		calli default string(class G2_C606`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C606::Method5.10578()"
		ldstr "class G2_C606`2<class BaseClass1,class BaseClass0> on type class G2_C606`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C606`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C606`2<class BaseClass1,class BaseClass0>::Method4()
		calli default string(class G2_C606`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C606::Method4.10576()"
		ldstr "class G2_C606`2<class BaseClass1,class BaseClass0> on type class G2_C606`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C606`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C606`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G2_C606`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C606::Method7.10574<System.Object>()"
		ldstr "class G2_C606`2<class BaseClass1,class BaseClass0> on type class G2_C606`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C606`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C606`2<class BaseClass1,class BaseClass0>::ClassMethod1345<object>()
		calli default string(class G2_C606`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C606::ClassMethod1345.MI.10583<System.Object>()"
		ldstr "class G2_C606`2<class BaseClass1,class BaseClass0> on type class G2_C606`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C606`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C606`2<class BaseClass1,class BaseClass0>::ClassMethod1344<object>()
		calli default string(class G2_C606`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C606::ClassMethod1344.MI.10582<System.Object>()"
		ldstr "class G2_C606`2<class BaseClass1,class BaseClass0> on type class G2_C606`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G2_C606`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C606::Method7.MI.10575<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G2_C606`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method4()
		calli default string(class G2_C606`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C606::Method4.MI.10577()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C606`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method5()
		calli default string(class G2_C606`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C606::Method5.10578()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C606`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method6<object>()
		calli default string(class G2_C606`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C606::Method6.MI.10580<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C606`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G2_C606`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C606::Method7.MI.10575<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G2_C606`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G2_C606`2<class BaseClass1,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C11`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass1,class BaseClass1>::ClassMethod1345<object>()
		calli default string(class G2_C606`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C606::ClassMethod1345.MI.10583<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass1> on type class G2_C606`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C11`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass1,class BaseClass1>::ClassMethod1344<object>()
		calli default string(class G2_C606`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C606::ClassMethod1344.MI.10582<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass1> on type class G2_C606`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C11`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass1,class BaseClass1>::Method6<object>()
		calli default string(class G2_C606`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C606::Method6.10579<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass1> on type class G2_C606`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C11`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass1,class BaseClass1>::Method5()
		calli default string(class G2_C606`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C606::Method5.10578()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass1> on type class G2_C606`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C11`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass1,class BaseClass1>::Method4()
		calli default string(class G2_C606`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C11::Method4.4856()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass1> on type class G2_C606`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C11`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C606`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C606::Method7.10574<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass1> on type class G2_C606`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C606`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C606::Method7.MI.10575<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C606`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G2_C606`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C606::Method4.MI.10577()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C606`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G2_C606`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C606::Method5.10578()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C606`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G2_C606`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C606::Method6.MI.10580<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C606`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C606`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C606::Method7.MI.10575<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C606`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C606`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C606`2<class BaseClass1,class BaseClass1>::ClassMethod2578()
		calli default string(class G2_C606`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C606::ClassMethod2578.10581()"
		ldstr "class G2_C606`2<class BaseClass1,class BaseClass1> on type class G2_C606`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C606`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C606`2<class BaseClass1,class BaseClass1>::Method6<object>()
		calli default string(class G2_C606`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C606::Method6.10579<System.Object>()"
		ldstr "class G2_C606`2<class BaseClass1,class BaseClass1> on type class G2_C606`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C606`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C606`2<class BaseClass1,class BaseClass1>::Method5()
		calli default string(class G2_C606`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C606::Method5.10578()"
		ldstr "class G2_C606`2<class BaseClass1,class BaseClass1> on type class G2_C606`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C606`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C606`2<class BaseClass1,class BaseClass1>::Method4()
		calli default string(class G2_C606`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C606::Method4.10576()"
		ldstr "class G2_C606`2<class BaseClass1,class BaseClass1> on type class G2_C606`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C606`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C606`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C606`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C606::Method7.10574<System.Object>()"
		ldstr "class G2_C606`2<class BaseClass1,class BaseClass1> on type class G2_C606`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C606`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C606`2<class BaseClass1,class BaseClass1>::ClassMethod1345<object>()
		calli default string(class G2_C606`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C606::ClassMethod1345.MI.10583<System.Object>()"
		ldstr "class G2_C606`2<class BaseClass1,class BaseClass1> on type class G2_C606`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C606`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C606`2<class BaseClass1,class BaseClass1>::ClassMethod1344<object>()
		calli default string(class G2_C606`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C606::ClassMethod1344.MI.10582<System.Object>()"
		ldstr "class G2_C606`2<class BaseClass1,class BaseClass1> on type class G2_C606`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G2_C606`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C606::Method7.MI.10575<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G2_C606`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method4()
		calli default string(class G2_C606`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C606::Method4.MI.10577()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C606`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method5()
		calli default string(class G2_C606`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C606::Method5.10578()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C606`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method6<object>()
		calli default string(class G2_C606`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C606::Method6.MI.10580<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C606`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G2_C606`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C606::Method7.MI.10575<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G2_C606`2<class BaseClass1,class BaseClass1>"
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
