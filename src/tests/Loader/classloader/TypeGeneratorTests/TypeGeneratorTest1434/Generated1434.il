// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) .ver 4:0:0:0 }
.assembly extern TestFramework { .publickeytoken = ( B0 3F 5F 7F 11 D5 0A 3A )  }

//TYPES IN FORWARDER ASSEMBLIES:

//TEST ASSEMBLY:
.assembly Generated1434 { .hash algorithm 0x00008004 }
.module Generated1434.exe

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
.class public G3_C1906`1<T0> 
		extends class G2_C829`2<!T0,class BaseClass0>
		implements IBase0 
{
	.method public hidebysig virtual instance string Method0() cil managed noinlining { 
		ldstr "G3_C1906::Method0.18640()"
		ret
	}
	.method public hidebysig virtual instance string Method1() cil managed noinlining { 
		ldstr "G3_C1906::Method1.18641()"
		ret
	}
	.method public hidebysig virtual instance string Method2<M0>() cil managed noinlining { 
		ldstr "G3_C1906::Method2.18642<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase0.Method2'<M0>() cil managed noinlining { 
		.override method instance string IBase0::Method2<[1]>()
		ldstr "G3_C1906::Method2.MI.18643<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig virtual instance string Method3<M0>() cil managed noinlining { 
		ldstr "G3_C1906::Method3.18644<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod5207() cil managed noinlining { 
		ldstr "G3_C1906::ClassMethod5207.18645()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod5208() cil managed noinlining { 
		ldstr "G3_C1906::ClassMethod5208.18646()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod5209<M0>() cil managed noinlining { 
		ldstr "G3_C1906::ClassMethod5209.18647<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod5210<M0>() cil managed noinlining { 
		ldstr "G3_C1906::ClassMethod5210.18648<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'G2_C829<T0,class BaseClass0>.ClassMethod3038'<M0>() cil managed noinlining { 
		.override method instance string class G2_C829`2<!T0,class BaseClass0>::ClassMethod3038<[1]>()
		ldstr "G3_C1906::ClassMethod3038.MI.18649<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class G2_C829`2<!T0,class BaseClass0>::.ctor()
		ret
	}
}
.class public abstract G2_C829`2<T0, T1> 
		extends class G1_C15`2<class BaseClass1,class BaseClass1>
		implements class IBase1`1<class BaseClass1> 
{
	.method public hidebysig newslot virtual instance string Method4() cil managed noinlining { 
		ldstr "G2_C829::Method4.12633()"
		ret
	}
	.method public hidebysig virtual instance string Method5() cil managed noinlining { 
		ldstr "G2_C829::Method5.12634()"
		ret
	}
	.method public hidebysig newslot virtual instance string Method6<M0>() cil managed noinlining { 
		ldstr "G2_C829::Method6.12635<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod3037() cil managed noinlining { 
		ldstr "G2_C829::ClassMethod3037.12636()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod3038<M0>() cil managed noinlining { 
		ldstr "G2_C829::ClassMethod3038.12637<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class G1_C15`2<class BaseClass1,class BaseClass1>::.ctor()
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
.class public abstract G1_C15`2<T0, T1> 
		implements class IBase2`2<!T1,!T1>, class IBase1`1<class BaseClass0> 
{
	.method public hidebysig newslot virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G1_C15::Method7.4885<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase2<T1,T1>.Method7'<M0>() cil managed noinlining { 
		.override method instance string class IBase2`2<!T1,!T1>::Method7<[1]>()
		ldstr "G1_C15::Method7.MI.4886<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig virtual instance string Method4() cil managed noinlining { 
		ldstr "G1_C15::Method4.4887()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<class BaseClass0>.Method4'() cil managed noinlining { 
		.override method instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G1_C15::Method4.MI.4888()"
		ret
	}
	.method public hidebysig virtual instance string Method5() cil managed noinlining { 
		ldstr "G1_C15::Method5.4889()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<class BaseClass0>.Method5'() cil managed noinlining { 
		.override method instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G1_C15::Method5.MI.4890()"
		ret
	}
	.method public hidebysig virtual instance string Method6<M0>() cil managed noinlining { 
		ldstr "G1_C15::Method6.4891<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<class BaseClass0>.Method6'<M0>() cil managed noinlining { 
		.override method instance string class IBase1`1<class BaseClass0>::Method6<[1]>()
		ldstr "G1_C15::Method6.MI.4892<"
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
.class interface public abstract IBase2`2<+T0, -T1> 
{
	.method public hidebysig newslot abstract virtual instance string Method7<M0>() cil managed { }
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
	.method static void M.G3_C1906.T<T0,(class G3_C1906`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 19
		.locals init (string[] actualResults)
		ldc.i4.s    14
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1906.T<T0,(class G3_C1906`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 14
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1906`1<!!T0>::ClassMethod3037()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1906`1<!!T0>::ClassMethod3038<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1906`1<!!T0>::ClassMethod5207()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1906`1<!!T0>::ClassMethod5208()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1906`1<!!T0>::ClassMethod5209<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1906`1<!!T0>::ClassMethod5210<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1906`1<!!T0>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1906`1<!!T0>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1906`1<!!T0>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1906`1<!!T0>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1906`1<!!T0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1906`1<!!T0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1906`1<!!T0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     13
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1906`1<!!T0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1906.A<(class G3_C1906`1<class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 19
		.locals init (string[] actualResults)
		ldc.i4.s    14
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1906.A<(class G3_C1906`1<class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 14
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1906`1<class BaseClass0>::ClassMethod3037()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1906`1<class BaseClass0>::ClassMethod3038<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1906`1<class BaseClass0>::ClassMethod5207()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1906`1<class BaseClass0>::ClassMethod5208()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1906`1<class BaseClass0>::ClassMethod5209<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1906`1<class BaseClass0>::ClassMethod5210<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1906`1<class BaseClass0>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1906`1<class BaseClass0>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1906`1<class BaseClass0>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1906`1<class BaseClass0>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1906`1<class BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1906`1<class BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1906`1<class BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     13
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1906`1<class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1906.B<(class G3_C1906`1<class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 19
		.locals init (string[] actualResults)
		ldc.i4.s    14
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1906.B<(class G3_C1906`1<class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 14
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1906`1<class BaseClass1>::ClassMethod3037()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1906`1<class BaseClass1>::ClassMethod3038<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1906`1<class BaseClass1>::ClassMethod5207()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1906`1<class BaseClass1>::ClassMethod5208()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1906`1<class BaseClass1>::ClassMethod5209<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1906`1<class BaseClass1>::ClassMethod5210<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1906`1<class BaseClass1>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1906`1<class BaseClass1>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1906`1<class BaseClass1>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1906`1<class BaseClass1>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1906`1<class BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1906`1<class BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1906`1<class BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     13
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1906`1<class BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C829.T.T<T0,T1,(class G2_C829`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C829.T.T<T0,T1,(class G2_C829`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C829`2<!!T0,!!T1>::ClassMethod3037()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C829`2<!!T0,!!T1>::ClassMethod3038<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C829`2<!!T0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C829`2<!!T0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C829`2<!!T0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C829`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C829.A.T<T1,(class G2_C829`2<class BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C829.A.T<T1,(class G2_C829`2<class BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C829`2<class BaseClass0,!!T1>::ClassMethod3037()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C829`2<class BaseClass0,!!T1>::ClassMethod3038<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C829`2<class BaseClass0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C829`2<class BaseClass0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C829`2<class BaseClass0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C829`2<class BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C829.A.A<(class G2_C829`2<class BaseClass0,class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C829.A.A<(class G2_C829`2<class BaseClass0,class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C829`2<class BaseClass0,class BaseClass0>::ClassMethod3037()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C829`2<class BaseClass0,class BaseClass0>::ClassMethod3038<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C829`2<class BaseClass0,class BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C829`2<class BaseClass0,class BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C829`2<class BaseClass0,class BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C829`2<class BaseClass0,class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C829.A.B<(class G2_C829`2<class BaseClass0,class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C829.A.B<(class G2_C829`2<class BaseClass0,class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C829`2<class BaseClass0,class BaseClass1>::ClassMethod3037()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C829`2<class BaseClass0,class BaseClass1>::ClassMethod3038<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C829`2<class BaseClass0,class BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C829`2<class BaseClass0,class BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C829`2<class BaseClass0,class BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C829`2<class BaseClass0,class BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C829.B.T<T1,(class G2_C829`2<class BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C829.B.T<T1,(class G2_C829`2<class BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C829`2<class BaseClass1,!!T1>::ClassMethod3037()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C829`2<class BaseClass1,!!T1>::ClassMethod3038<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C829`2<class BaseClass1,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C829`2<class BaseClass1,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C829`2<class BaseClass1,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C829`2<class BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C829.B.A<(class G2_C829`2<class BaseClass1,class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C829.B.A<(class G2_C829`2<class BaseClass1,class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C829`2<class BaseClass1,class BaseClass0>::ClassMethod3037()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C829`2<class BaseClass1,class BaseClass0>::ClassMethod3038<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C829`2<class BaseClass1,class BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C829`2<class BaseClass1,class BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C829`2<class BaseClass1,class BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C829`2<class BaseClass1,class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C829.B.B<(class G2_C829`2<class BaseClass1,class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C829.B.B<(class G2_C829`2<class BaseClass1,class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C829`2<class BaseClass1,class BaseClass1>::ClassMethod3037()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C829`2<class BaseClass1,class BaseClass1>::ClassMethod3038<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C829`2<class BaseClass1,class BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C829`2<class BaseClass1,class BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C829`2<class BaseClass1,class BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C829`2<class BaseClass1,class BaseClass1>::Method7<object>()
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
	.method static void M.G1_C15.T.T<T0,T1,(class G1_C15`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 9
		.locals init (string[] actualResults)
		ldc.i4.s    4
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C15.T.T<T0,T1,(class G1_C15`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 4
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C15`2<!!T0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C15`2<!!T0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C15`2<!!T0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C15`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C15.A.T<T1,(class G1_C15`2<class BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 9
		.locals init (string[] actualResults)
		ldc.i4.s    4
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C15.A.T<T1,(class G1_C15`2<class BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 4
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C15`2<class BaseClass0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C15`2<class BaseClass0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C15`2<class BaseClass0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C15`2<class BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C15.A.A<(class G1_C15`2<class BaseClass0,class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 9
		.locals init (string[] actualResults)
		ldc.i4.s    4
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C15.A.A<(class G1_C15`2<class BaseClass0,class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 4
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C15`2<class BaseClass0,class BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C15`2<class BaseClass0,class BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C15`2<class BaseClass0,class BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C15`2<class BaseClass0,class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C15.A.B<(class G1_C15`2<class BaseClass0,class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 9
		.locals init (string[] actualResults)
		ldc.i4.s    4
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C15.A.B<(class G1_C15`2<class BaseClass0,class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 4
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C15`2<class BaseClass0,class BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C15`2<class BaseClass0,class BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C15`2<class BaseClass0,class BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C15`2<class BaseClass0,class BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C15.B.T<T1,(class G1_C15`2<class BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 9
		.locals init (string[] actualResults)
		ldc.i4.s    4
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C15.B.T<T1,(class G1_C15`2<class BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 4
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C15`2<class BaseClass1,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C15`2<class BaseClass1,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C15`2<class BaseClass1,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C15`2<class BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C15.B.A<(class G1_C15`2<class BaseClass1,class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 9
		.locals init (string[] actualResults)
		ldc.i4.s    4
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C15.B.A<(class G1_C15`2<class BaseClass1,class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 4
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C15`2<class BaseClass1,class BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C15`2<class BaseClass1,class BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C15`2<class BaseClass1,class BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C15`2<class BaseClass1,class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C15.B.B<(class G1_C15`2<class BaseClass1,class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 9
		.locals init (string[] actualResults)
		ldc.i4.s    4
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C15.B.B<(class G1_C15`2<class BaseClass1,class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 4
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C15`2<class BaseClass1,class BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C15`2<class BaseClass1,class BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C15`2<class BaseClass1,class BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C15`2<class BaseClass1,class BaseClass1>::Method7<object>()
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
		newobj instance void class G3_C1906`1<class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C15`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C15`2<class BaseClass1,class BaseClass1>::Method6<object>()
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G1_C15`2<class BaseClass1,class BaseClass1> on type class G3_C1906`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C15`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C15`2<class BaseClass1,class BaseClass1>::Method5()
		ldstr "G2_C829::Method5.12634()"
		ldstr "class G1_C15`2<class BaseClass1,class BaseClass1> on type class G3_C1906`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C15`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C15`2<class BaseClass1,class BaseClass1>::Method4()
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G1_C15`2<class BaseClass1,class BaseClass1> on type class G3_C1906`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C15`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C15`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C15::Method7.4885<System.Object>()"
		ldstr "class G1_C15`2<class BaseClass1,class BaseClass1> on type class G3_C1906`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C15::Method7.MI.4886<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G3_C1906`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G2_C829::Method4.12633()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1906`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G2_C829::Method5.12634()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1906`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G2_C829::Method6.12635<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1906`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G1_C15::Method7.MI.4886<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1906`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C829`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C829`2<class BaseClass0,class BaseClass0>::ClassMethod3038<object>()
		ldstr "G3_C1906::ClassMethod3038.MI.18649<System.Object>()"
		ldstr "class G2_C829`2<class BaseClass0,class BaseClass0> on type class G3_C1906`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C829`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C829`2<class BaseClass0,class BaseClass0>::ClassMethod3037()
		ldstr "G2_C829::ClassMethod3037.12636()"
		ldstr "class G2_C829`2<class BaseClass0,class BaseClass0> on type class G3_C1906`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C829`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C829`2<class BaseClass0,class BaseClass0>::Method6<object>()
		ldstr "G2_C829::Method6.12635<System.Object>()"
		ldstr "class G2_C829`2<class BaseClass0,class BaseClass0> on type class G3_C1906`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C829`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C829`2<class BaseClass0,class BaseClass0>::Method5()
		ldstr "G2_C829::Method5.12634()"
		ldstr "class G2_C829`2<class BaseClass0,class BaseClass0> on type class G3_C1906`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C829`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C829`2<class BaseClass0,class BaseClass0>::Method4()
		ldstr "G2_C829::Method4.12633()"
		ldstr "class G2_C829`2<class BaseClass0,class BaseClass0> on type class G3_C1906`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C829`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C829`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G1_C15::Method7.4885<System.Object>()"
		ldstr "class G2_C829`2<class BaseClass0,class BaseClass0> on type class G3_C1906`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method4()
		ldstr "G2_C829::Method4.12633()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1906`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method5()
		ldstr "G2_C829::Method5.12634()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1906`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method6<object>()
		ldstr "G2_C829::Method6.12635<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1906`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G3_C1906`1<class BaseClass0>
		callvirt instance string class G3_C1906`1<class BaseClass0>::ClassMethod5210<object>()
		ldstr "G3_C1906::ClassMethod5210.18648<System.Object>()"
		ldstr "class G3_C1906`1<class BaseClass0> on type class G3_C1906`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1906`1<class BaseClass0>
		callvirt instance string class G3_C1906`1<class BaseClass0>::ClassMethod5209<object>()
		ldstr "G3_C1906::ClassMethod5209.18647<System.Object>()"
		ldstr "class G3_C1906`1<class BaseClass0> on type class G3_C1906`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1906`1<class BaseClass0>
		callvirt instance string class G3_C1906`1<class BaseClass0>::ClassMethod5208()
		ldstr "G3_C1906::ClassMethod5208.18646()"
		ldstr "class G3_C1906`1<class BaseClass0> on type class G3_C1906`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1906`1<class BaseClass0>
		callvirt instance string class G3_C1906`1<class BaseClass0>::ClassMethod5207()
		ldstr "G3_C1906::ClassMethod5207.18645()"
		ldstr "class G3_C1906`1<class BaseClass0> on type class G3_C1906`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1906`1<class BaseClass0>
		callvirt instance string class G3_C1906`1<class BaseClass0>::Method3<object>()
		ldstr "G3_C1906::Method3.18644<System.Object>()"
		ldstr "class G3_C1906`1<class BaseClass0> on type class G3_C1906`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1906`1<class BaseClass0>
		callvirt instance string class G3_C1906`1<class BaseClass0>::Method2<object>()
		ldstr "G3_C1906::Method2.18642<System.Object>()"
		ldstr "class G3_C1906`1<class BaseClass0> on type class G3_C1906`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1906`1<class BaseClass0>
		callvirt instance string class G3_C1906`1<class BaseClass0>::Method1()
		ldstr "G3_C1906::Method1.18641()"
		ldstr "class G3_C1906`1<class BaseClass0> on type class G3_C1906`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1906`1<class BaseClass0>
		callvirt instance string class G3_C1906`1<class BaseClass0>::Method0()
		ldstr "G3_C1906::Method0.18640()"
		ldstr "class G3_C1906`1<class BaseClass0> on type class G3_C1906`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1906`1<class BaseClass0>
		callvirt instance string class G3_C1906`1<class BaseClass0>::ClassMethod3038<object>()
		ldstr "G3_C1906::ClassMethod3038.MI.18649<System.Object>()"
		ldstr "class G3_C1906`1<class BaseClass0> on type class G3_C1906`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1906`1<class BaseClass0>
		callvirt instance string class G3_C1906`1<class BaseClass0>::ClassMethod3037()
		ldstr "G2_C829::ClassMethod3037.12636()"
		ldstr "class G3_C1906`1<class BaseClass0> on type class G3_C1906`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1906`1<class BaseClass0>
		callvirt instance string class G3_C1906`1<class BaseClass0>::Method6<object>()
		ldstr "G2_C829::Method6.12635<System.Object>()"
		ldstr "class G3_C1906`1<class BaseClass0> on type class G3_C1906`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1906`1<class BaseClass0>
		callvirt instance string class G3_C1906`1<class BaseClass0>::Method5()
		ldstr "G2_C829::Method5.12634()"
		ldstr "class G3_C1906`1<class BaseClass0> on type class G3_C1906`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1906`1<class BaseClass0>
		callvirt instance string class G3_C1906`1<class BaseClass0>::Method4()
		ldstr "G2_C829::Method4.12633()"
		ldstr "class G3_C1906`1<class BaseClass0> on type class G3_C1906`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1906`1<class BaseClass0>
		callvirt instance string class G3_C1906`1<class BaseClass0>::Method7<object>()
		ldstr "G1_C15::Method7.4885<System.Object>()"
		ldstr "class G3_C1906`1<class BaseClass0> on type class G3_C1906`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string IBase0::Method0()
		ldstr "G3_C1906::Method0.18640()"
		ldstr "IBase0 on type class G3_C1906`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method1()
		ldstr "G3_C1906::Method1.18641()"
		ldstr "IBase0 on type class G3_C1906`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method2<object>()
		ldstr "G3_C1906::Method2.MI.18643<System.Object>()"
		ldstr "IBase0 on type class G3_C1906`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method3<object>()
		ldstr "G3_C1906::Method3.18644<System.Object>()"
		ldstr "IBase0 on type class G3_C1906`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G3_C1906`1<class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C15`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C15`2<class BaseClass1,class BaseClass1>::Method6<object>()
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G1_C15`2<class BaseClass1,class BaseClass1> on type class G3_C1906`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C15`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C15`2<class BaseClass1,class BaseClass1>::Method5()
		ldstr "G2_C829::Method5.12634()"
		ldstr "class G1_C15`2<class BaseClass1,class BaseClass1> on type class G3_C1906`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C15`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C15`2<class BaseClass1,class BaseClass1>::Method4()
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G1_C15`2<class BaseClass1,class BaseClass1> on type class G3_C1906`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C15`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C15`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C15::Method7.4885<System.Object>()"
		ldstr "class G1_C15`2<class BaseClass1,class BaseClass1> on type class G3_C1906`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C15::Method7.MI.4886<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G3_C1906`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G2_C829::Method4.12633()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1906`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G2_C829::Method5.12634()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1906`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G2_C829::Method6.12635<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1906`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G1_C15::Method7.MI.4886<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1906`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C829`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C829`2<class BaseClass1,class BaseClass0>::ClassMethod3038<object>()
		ldstr "G3_C1906::ClassMethod3038.MI.18649<System.Object>()"
		ldstr "class G2_C829`2<class BaseClass1,class BaseClass0> on type class G3_C1906`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C829`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C829`2<class BaseClass1,class BaseClass0>::ClassMethod3037()
		ldstr "G2_C829::ClassMethod3037.12636()"
		ldstr "class G2_C829`2<class BaseClass1,class BaseClass0> on type class G3_C1906`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C829`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C829`2<class BaseClass1,class BaseClass0>::Method6<object>()
		ldstr "G2_C829::Method6.12635<System.Object>()"
		ldstr "class G2_C829`2<class BaseClass1,class BaseClass0> on type class G3_C1906`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C829`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C829`2<class BaseClass1,class BaseClass0>::Method5()
		ldstr "G2_C829::Method5.12634()"
		ldstr "class G2_C829`2<class BaseClass1,class BaseClass0> on type class G3_C1906`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C829`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C829`2<class BaseClass1,class BaseClass0>::Method4()
		ldstr "G2_C829::Method4.12633()"
		ldstr "class G2_C829`2<class BaseClass1,class BaseClass0> on type class G3_C1906`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C829`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C829`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G1_C15::Method7.4885<System.Object>()"
		ldstr "class G2_C829`2<class BaseClass1,class BaseClass0> on type class G3_C1906`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method4()
		ldstr "G2_C829::Method4.12633()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1906`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method5()
		ldstr "G2_C829::Method5.12634()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1906`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method6<object>()
		ldstr "G2_C829::Method6.12635<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1906`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G3_C1906`1<class BaseClass1>
		callvirt instance string class G3_C1906`1<class BaseClass1>::ClassMethod5210<object>()
		ldstr "G3_C1906::ClassMethod5210.18648<System.Object>()"
		ldstr "class G3_C1906`1<class BaseClass1> on type class G3_C1906`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1906`1<class BaseClass1>
		callvirt instance string class G3_C1906`1<class BaseClass1>::ClassMethod5209<object>()
		ldstr "G3_C1906::ClassMethod5209.18647<System.Object>()"
		ldstr "class G3_C1906`1<class BaseClass1> on type class G3_C1906`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1906`1<class BaseClass1>
		callvirt instance string class G3_C1906`1<class BaseClass1>::ClassMethod5208()
		ldstr "G3_C1906::ClassMethod5208.18646()"
		ldstr "class G3_C1906`1<class BaseClass1> on type class G3_C1906`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1906`1<class BaseClass1>
		callvirt instance string class G3_C1906`1<class BaseClass1>::ClassMethod5207()
		ldstr "G3_C1906::ClassMethod5207.18645()"
		ldstr "class G3_C1906`1<class BaseClass1> on type class G3_C1906`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1906`1<class BaseClass1>
		callvirt instance string class G3_C1906`1<class BaseClass1>::Method3<object>()
		ldstr "G3_C1906::Method3.18644<System.Object>()"
		ldstr "class G3_C1906`1<class BaseClass1> on type class G3_C1906`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1906`1<class BaseClass1>
		callvirt instance string class G3_C1906`1<class BaseClass1>::Method2<object>()
		ldstr "G3_C1906::Method2.18642<System.Object>()"
		ldstr "class G3_C1906`1<class BaseClass1> on type class G3_C1906`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1906`1<class BaseClass1>
		callvirt instance string class G3_C1906`1<class BaseClass1>::Method1()
		ldstr "G3_C1906::Method1.18641()"
		ldstr "class G3_C1906`1<class BaseClass1> on type class G3_C1906`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1906`1<class BaseClass1>
		callvirt instance string class G3_C1906`1<class BaseClass1>::Method0()
		ldstr "G3_C1906::Method0.18640()"
		ldstr "class G3_C1906`1<class BaseClass1> on type class G3_C1906`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1906`1<class BaseClass1>
		callvirt instance string class G3_C1906`1<class BaseClass1>::ClassMethod3038<object>()
		ldstr "G3_C1906::ClassMethod3038.MI.18649<System.Object>()"
		ldstr "class G3_C1906`1<class BaseClass1> on type class G3_C1906`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1906`1<class BaseClass1>
		callvirt instance string class G3_C1906`1<class BaseClass1>::ClassMethod3037()
		ldstr "G2_C829::ClassMethod3037.12636()"
		ldstr "class G3_C1906`1<class BaseClass1> on type class G3_C1906`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1906`1<class BaseClass1>
		callvirt instance string class G3_C1906`1<class BaseClass1>::Method6<object>()
		ldstr "G2_C829::Method6.12635<System.Object>()"
		ldstr "class G3_C1906`1<class BaseClass1> on type class G3_C1906`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1906`1<class BaseClass1>
		callvirt instance string class G3_C1906`1<class BaseClass1>::Method5()
		ldstr "G2_C829::Method5.12634()"
		ldstr "class G3_C1906`1<class BaseClass1> on type class G3_C1906`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1906`1<class BaseClass1>
		callvirt instance string class G3_C1906`1<class BaseClass1>::Method4()
		ldstr "G2_C829::Method4.12633()"
		ldstr "class G3_C1906`1<class BaseClass1> on type class G3_C1906`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1906`1<class BaseClass1>
		callvirt instance string class G3_C1906`1<class BaseClass1>::Method7<object>()
		ldstr "G1_C15::Method7.4885<System.Object>()"
		ldstr "class G3_C1906`1<class BaseClass1> on type class G3_C1906`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string IBase0::Method0()
		ldstr "G3_C1906::Method0.18640()"
		ldstr "IBase0 on type class G3_C1906`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method1()
		ldstr "G3_C1906::Method1.18641()"
		ldstr "IBase0 on type class G3_C1906`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method2<object>()
		ldstr "G3_C1906::Method2.MI.18643<System.Object>()"
		ldstr "IBase0 on type class G3_C1906`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method3<object>()
		ldstr "G3_C1906::Method3.18644<System.Object>()"
		ldstr "IBase0 on type class G3_C1906`1<class BaseClass1>"
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
		newobj instance void class G3_C1906`1<class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C15::Method4.4887()#G2_C829::Method5.12634()#G1_C15::Method6.4891<System.Object>()#G1_C15::Method7.4885<System.Object>()#"
		call void Framework::M.G1_C15.T.T<class BaseClass1,class BaseClass1,class G3_C1906`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C15::Method4.4887()#G2_C829::Method5.12634()#G1_C15::Method6.4891<System.Object>()#G1_C15::Method7.4885<System.Object>()#"
		call void Framework::M.G1_C15.B.T<class BaseClass1,class G3_C1906`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C15::Method4.4887()#G2_C829::Method5.12634()#G1_C15::Method6.4891<System.Object>()#G1_C15::Method7.4885<System.Object>()#"
		call void Framework::M.G1_C15.B.B<class G3_C1906`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C15::Method7.MI.4886<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G3_C1906`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C15::Method7.MI.4886<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass1,class G3_C1906`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C15::Method7.MI.4886<System.Object>()#"
		call void Framework::M.IBase2.B.B<class G3_C1906`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C829::Method4.12633()#G2_C829::Method5.12634()#G2_C829::Method6.12635<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass0,class G3_C1906`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C829::Method4.12633()#G2_C829::Method5.12634()#G2_C829::Method6.12635<System.Object>()#"
		call void Framework::M.IBase1.A<class G3_C1906`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C15::Method7.MI.4886<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G3_C1906`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C15::Method7.MI.4886<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G3_C1906`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C15::Method7.MI.4886<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G3_C1906`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C829::ClassMethod3037.12636()#G3_C1906::ClassMethod3038.MI.18649<System.Object>()#G2_C829::Method4.12633()#G2_C829::Method5.12634()#G2_C829::Method6.12635<System.Object>()#G1_C15::Method7.4885<System.Object>()#"
		call void Framework::M.G2_C829.T.T<class BaseClass0,class BaseClass0,class G3_C1906`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C829::ClassMethod3037.12636()#G3_C1906::ClassMethod3038.MI.18649<System.Object>()#G2_C829::Method4.12633()#G2_C829::Method5.12634()#G2_C829::Method6.12635<System.Object>()#G1_C15::Method7.4885<System.Object>()#"
		call void Framework::M.G2_C829.A.T<class BaseClass0,class G3_C1906`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C829::ClassMethod3037.12636()#G3_C1906::ClassMethod3038.MI.18649<System.Object>()#G2_C829::Method4.12633()#G2_C829::Method5.12634()#G2_C829::Method6.12635<System.Object>()#G1_C15::Method7.4885<System.Object>()#"
		call void Framework::M.G2_C829.A.A<class G3_C1906`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C829::Method4.12633()#G2_C829::Method5.12634()#G2_C829::Method6.12635<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass1,class G3_C1906`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C829::Method4.12633()#G2_C829::Method5.12634()#G2_C829::Method6.12635<System.Object>()#"
		call void Framework::M.IBase1.B<class G3_C1906`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C829::ClassMethod3037.12636()#G3_C1906::ClassMethod3038.MI.18649<System.Object>()#G3_C1906::ClassMethod5207.18645()#G3_C1906::ClassMethod5208.18646()#G3_C1906::ClassMethod5209.18647<System.Object>()#G3_C1906::ClassMethod5210.18648<System.Object>()#G3_C1906::Method0.18640()#G3_C1906::Method1.18641()#G3_C1906::Method2.18642<System.Object>()#G3_C1906::Method3.18644<System.Object>()#G2_C829::Method4.12633()#G2_C829::Method5.12634()#G2_C829::Method6.12635<System.Object>()#G1_C15::Method7.4885<System.Object>()#"
		call void Framework::M.G3_C1906.T<class BaseClass0,class G3_C1906`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C829::ClassMethod3037.12636()#G3_C1906::ClassMethod3038.MI.18649<System.Object>()#G3_C1906::ClassMethod5207.18645()#G3_C1906::ClassMethod5208.18646()#G3_C1906::ClassMethod5209.18647<System.Object>()#G3_C1906::ClassMethod5210.18648<System.Object>()#G3_C1906::Method0.18640()#G3_C1906::Method1.18641()#G3_C1906::Method2.18642<System.Object>()#G3_C1906::Method3.18644<System.Object>()#G2_C829::Method4.12633()#G2_C829::Method5.12634()#G2_C829::Method6.12635<System.Object>()#G1_C15::Method7.4885<System.Object>()#"
		call void Framework::M.G3_C1906.A<class G3_C1906`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1906::Method0.18640()#G3_C1906::Method1.18641()#G3_C1906::Method2.MI.18643<System.Object>()#G3_C1906::Method3.18644<System.Object>()#"
		call void Framework::M.IBase0<class G3_C1906`1<class BaseClass0>>(!!0,string)
		newobj instance void class G3_C1906`1<class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C15::Method4.4887()#G2_C829::Method5.12634()#G1_C15::Method6.4891<System.Object>()#G1_C15::Method7.4885<System.Object>()#"
		call void Framework::M.G1_C15.T.T<class BaseClass1,class BaseClass1,class G3_C1906`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C15::Method4.4887()#G2_C829::Method5.12634()#G1_C15::Method6.4891<System.Object>()#G1_C15::Method7.4885<System.Object>()#"
		call void Framework::M.G1_C15.B.T<class BaseClass1,class G3_C1906`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C15::Method4.4887()#G2_C829::Method5.12634()#G1_C15::Method6.4891<System.Object>()#G1_C15::Method7.4885<System.Object>()#"
		call void Framework::M.G1_C15.B.B<class G3_C1906`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C15::Method7.MI.4886<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G3_C1906`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C15::Method7.MI.4886<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass1,class G3_C1906`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C15::Method7.MI.4886<System.Object>()#"
		call void Framework::M.IBase2.B.B<class G3_C1906`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C829::Method4.12633()#G2_C829::Method5.12634()#G2_C829::Method6.12635<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass0,class G3_C1906`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C829::Method4.12633()#G2_C829::Method5.12634()#G2_C829::Method6.12635<System.Object>()#"
		call void Framework::M.IBase1.A<class G3_C1906`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C15::Method7.MI.4886<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G3_C1906`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C15::Method7.MI.4886<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G3_C1906`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C15::Method7.MI.4886<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G3_C1906`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C829::ClassMethod3037.12636()#G3_C1906::ClassMethod3038.MI.18649<System.Object>()#G2_C829::Method4.12633()#G2_C829::Method5.12634()#G2_C829::Method6.12635<System.Object>()#G1_C15::Method7.4885<System.Object>()#"
		call void Framework::M.G2_C829.T.T<class BaseClass1,class BaseClass0,class G3_C1906`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C829::ClassMethod3037.12636()#G3_C1906::ClassMethod3038.MI.18649<System.Object>()#G2_C829::Method4.12633()#G2_C829::Method5.12634()#G2_C829::Method6.12635<System.Object>()#G1_C15::Method7.4885<System.Object>()#"
		call void Framework::M.G2_C829.B.T<class BaseClass0,class G3_C1906`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C829::ClassMethod3037.12636()#G3_C1906::ClassMethod3038.MI.18649<System.Object>()#G2_C829::Method4.12633()#G2_C829::Method5.12634()#G2_C829::Method6.12635<System.Object>()#G1_C15::Method7.4885<System.Object>()#"
		call void Framework::M.G2_C829.B.A<class G3_C1906`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C829::Method4.12633()#G2_C829::Method5.12634()#G2_C829::Method6.12635<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass1,class G3_C1906`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C829::Method4.12633()#G2_C829::Method5.12634()#G2_C829::Method6.12635<System.Object>()#"
		call void Framework::M.IBase1.B<class G3_C1906`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C829::ClassMethod3037.12636()#G3_C1906::ClassMethod3038.MI.18649<System.Object>()#G3_C1906::ClassMethod5207.18645()#G3_C1906::ClassMethod5208.18646()#G3_C1906::ClassMethod5209.18647<System.Object>()#G3_C1906::ClassMethod5210.18648<System.Object>()#G3_C1906::Method0.18640()#G3_C1906::Method1.18641()#G3_C1906::Method2.18642<System.Object>()#G3_C1906::Method3.18644<System.Object>()#G2_C829::Method4.12633()#G2_C829::Method5.12634()#G2_C829::Method6.12635<System.Object>()#G1_C15::Method7.4885<System.Object>()#"
		call void Framework::M.G3_C1906.T<class BaseClass1,class G3_C1906`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C829::ClassMethod3037.12636()#G3_C1906::ClassMethod3038.MI.18649<System.Object>()#G3_C1906::ClassMethod5207.18645()#G3_C1906::ClassMethod5208.18646()#G3_C1906::ClassMethod5209.18647<System.Object>()#G3_C1906::ClassMethod5210.18648<System.Object>()#G3_C1906::Method0.18640()#G3_C1906::Method1.18641()#G3_C1906::Method2.18642<System.Object>()#G3_C1906::Method3.18644<System.Object>()#G2_C829::Method4.12633()#G2_C829::Method5.12634()#G2_C829::Method6.12635<System.Object>()#G1_C15::Method7.4885<System.Object>()#"
		call void Framework::M.G3_C1906.B<class G3_C1906`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1906::Method0.18640()#G3_C1906::Method1.18641()#G3_C1906::Method2.MI.18643<System.Object>()#G3_C1906::Method3.18644<System.Object>()#"
		call void Framework::M.IBase0<class G3_C1906`1<class BaseClass1>>(!!0,string)
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
		newobj instance void class G3_C1906`1<class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C15`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C15`2<class BaseClass1,class BaseClass1>::Method6<object>()
		calli default string(class G3_C1906`1<class BaseClass0>)
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G1_C15`2<class BaseClass1,class BaseClass1> on type class G3_C1906`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C15`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C15`2<class BaseClass1,class BaseClass1>::Method5()
		calli default string(class G3_C1906`1<class BaseClass0>)
		ldstr "G2_C829::Method5.12634()"
		ldstr "class G1_C15`2<class BaseClass1,class BaseClass1> on type class G3_C1906`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C15`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C15`2<class BaseClass1,class BaseClass1>::Method4()
		calli default string(class G3_C1906`1<class BaseClass0>)
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G1_C15`2<class BaseClass1,class BaseClass1> on type class G3_C1906`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C15`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C15`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1906`1<class BaseClass0>)
		ldstr "G1_C15::Method7.4885<System.Object>()"
		ldstr "class G1_C15`2<class BaseClass1,class BaseClass1> on type class G3_C1906`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1906`1<class BaseClass0>)
		ldstr "G1_C15::Method7.MI.4886<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G3_C1906`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G3_C1906`1<class BaseClass0>)
		ldstr "G2_C829::Method4.12633()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1906`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G3_C1906`1<class BaseClass0>)
		ldstr "G2_C829::Method5.12634()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1906`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G3_C1906`1<class BaseClass0>)
		ldstr "G2_C829::Method6.12635<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1906`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1906`1<class BaseClass0>)
		ldstr "G1_C15::Method7.MI.4886<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1906`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C829`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C829`2<class BaseClass0,class BaseClass0>::ClassMethod3038<object>()
		calli default string(class G3_C1906`1<class BaseClass0>)
		ldstr "G3_C1906::ClassMethod3038.MI.18649<System.Object>()"
		ldstr "class G2_C829`2<class BaseClass0,class BaseClass0> on type class G3_C1906`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C829`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C829`2<class BaseClass0,class BaseClass0>::ClassMethod3037()
		calli default string(class G3_C1906`1<class BaseClass0>)
		ldstr "G2_C829::ClassMethod3037.12636()"
		ldstr "class G2_C829`2<class BaseClass0,class BaseClass0> on type class G3_C1906`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C829`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C829`2<class BaseClass0,class BaseClass0>::Method6<object>()
		calli default string(class G3_C1906`1<class BaseClass0>)
		ldstr "G2_C829::Method6.12635<System.Object>()"
		ldstr "class G2_C829`2<class BaseClass0,class BaseClass0> on type class G3_C1906`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C829`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C829`2<class BaseClass0,class BaseClass0>::Method5()
		calli default string(class G3_C1906`1<class BaseClass0>)
		ldstr "G2_C829::Method5.12634()"
		ldstr "class G2_C829`2<class BaseClass0,class BaseClass0> on type class G3_C1906`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C829`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C829`2<class BaseClass0,class BaseClass0>::Method4()
		calli default string(class G3_C1906`1<class BaseClass0>)
		ldstr "G2_C829::Method4.12633()"
		ldstr "class G2_C829`2<class BaseClass0,class BaseClass0> on type class G3_C1906`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C829`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C829`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1906`1<class BaseClass0>)
		ldstr "G1_C15::Method7.4885<System.Object>()"
		ldstr "class G2_C829`2<class BaseClass0,class BaseClass0> on type class G3_C1906`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method4()
		calli default string(class G3_C1906`1<class BaseClass0>)
		ldstr "G2_C829::Method4.12633()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1906`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method5()
		calli default string(class G3_C1906`1<class BaseClass0>)
		ldstr "G2_C829::Method5.12634()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1906`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method6<object>()
		calli default string(class G3_C1906`1<class BaseClass0>)
		ldstr "G2_C829::Method6.12635<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1906`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1906`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1906`1<class BaseClass0>::ClassMethod5210<object>()
		calli default string(class G3_C1906`1<class BaseClass0>)
		ldstr "G3_C1906::ClassMethod5210.18648<System.Object>()"
		ldstr "class G3_C1906`1<class BaseClass0> on type class G3_C1906`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1906`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1906`1<class BaseClass0>::ClassMethod5209<object>()
		calli default string(class G3_C1906`1<class BaseClass0>)
		ldstr "G3_C1906::ClassMethod5209.18647<System.Object>()"
		ldstr "class G3_C1906`1<class BaseClass0> on type class G3_C1906`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1906`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1906`1<class BaseClass0>::ClassMethod5208()
		calli default string(class G3_C1906`1<class BaseClass0>)
		ldstr "G3_C1906::ClassMethod5208.18646()"
		ldstr "class G3_C1906`1<class BaseClass0> on type class G3_C1906`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1906`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1906`1<class BaseClass0>::ClassMethod5207()
		calli default string(class G3_C1906`1<class BaseClass0>)
		ldstr "G3_C1906::ClassMethod5207.18645()"
		ldstr "class G3_C1906`1<class BaseClass0> on type class G3_C1906`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1906`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1906`1<class BaseClass0>::Method3<object>()
		calli default string(class G3_C1906`1<class BaseClass0>)
		ldstr "G3_C1906::Method3.18644<System.Object>()"
		ldstr "class G3_C1906`1<class BaseClass0> on type class G3_C1906`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1906`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1906`1<class BaseClass0>::Method2<object>()
		calli default string(class G3_C1906`1<class BaseClass0>)
		ldstr "G3_C1906::Method2.18642<System.Object>()"
		ldstr "class G3_C1906`1<class BaseClass0> on type class G3_C1906`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1906`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1906`1<class BaseClass0>::Method1()
		calli default string(class G3_C1906`1<class BaseClass0>)
		ldstr "G3_C1906::Method1.18641()"
		ldstr "class G3_C1906`1<class BaseClass0> on type class G3_C1906`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1906`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1906`1<class BaseClass0>::Method0()
		calli default string(class G3_C1906`1<class BaseClass0>)
		ldstr "G3_C1906::Method0.18640()"
		ldstr "class G3_C1906`1<class BaseClass0> on type class G3_C1906`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1906`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1906`1<class BaseClass0>::ClassMethod3038<object>()
		calli default string(class G3_C1906`1<class BaseClass0>)
		ldstr "G3_C1906::ClassMethod3038.MI.18649<System.Object>()"
		ldstr "class G3_C1906`1<class BaseClass0> on type class G3_C1906`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1906`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1906`1<class BaseClass0>::ClassMethod3037()
		calli default string(class G3_C1906`1<class BaseClass0>)
		ldstr "G2_C829::ClassMethod3037.12636()"
		ldstr "class G3_C1906`1<class BaseClass0> on type class G3_C1906`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1906`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1906`1<class BaseClass0>::Method6<object>()
		calli default string(class G3_C1906`1<class BaseClass0>)
		ldstr "G2_C829::Method6.12635<System.Object>()"
		ldstr "class G3_C1906`1<class BaseClass0> on type class G3_C1906`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1906`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1906`1<class BaseClass0>::Method5()
		calli default string(class G3_C1906`1<class BaseClass0>)
		ldstr "G2_C829::Method5.12634()"
		ldstr "class G3_C1906`1<class BaseClass0> on type class G3_C1906`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1906`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1906`1<class BaseClass0>::Method4()
		calli default string(class G3_C1906`1<class BaseClass0>)
		ldstr "G2_C829::Method4.12633()"
		ldstr "class G3_C1906`1<class BaseClass0> on type class G3_C1906`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1906`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1906`1<class BaseClass0>::Method7<object>()
		calli default string(class G3_C1906`1<class BaseClass0>)
		ldstr "G1_C15::Method7.4885<System.Object>()"
		ldstr "class G3_C1906`1<class BaseClass0> on type class G3_C1906`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method0()
		calli default string(class G3_C1906`1<class BaseClass0>)
		ldstr "G3_C1906::Method0.18640()"
		ldstr "IBase0 on type class G3_C1906`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method1()
		calli default string(class G3_C1906`1<class BaseClass0>)
		ldstr "G3_C1906::Method1.18641()"
		ldstr "IBase0 on type class G3_C1906`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method2<object>()
		calli default string(class G3_C1906`1<class BaseClass0>)
		ldstr "G3_C1906::Method2.MI.18643<System.Object>()"
		ldstr "IBase0 on type class G3_C1906`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method3<object>()
		calli default string(class G3_C1906`1<class BaseClass0>)
		ldstr "G3_C1906::Method3.18644<System.Object>()"
		ldstr "IBase0 on type class G3_C1906`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G3_C1906`1<class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C15`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C15`2<class BaseClass1,class BaseClass1>::Method6<object>()
		calli default string(class G3_C1906`1<class BaseClass1>)
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G1_C15`2<class BaseClass1,class BaseClass1> on type class G3_C1906`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C15`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C15`2<class BaseClass1,class BaseClass1>::Method5()
		calli default string(class G3_C1906`1<class BaseClass1>)
		ldstr "G2_C829::Method5.12634()"
		ldstr "class G1_C15`2<class BaseClass1,class BaseClass1> on type class G3_C1906`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C15`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C15`2<class BaseClass1,class BaseClass1>::Method4()
		calli default string(class G3_C1906`1<class BaseClass1>)
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G1_C15`2<class BaseClass1,class BaseClass1> on type class G3_C1906`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C15`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C15`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1906`1<class BaseClass1>)
		ldstr "G1_C15::Method7.4885<System.Object>()"
		ldstr "class G1_C15`2<class BaseClass1,class BaseClass1> on type class G3_C1906`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1906`1<class BaseClass1>)
		ldstr "G1_C15::Method7.MI.4886<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G3_C1906`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G3_C1906`1<class BaseClass1>)
		ldstr "G2_C829::Method4.12633()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1906`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G3_C1906`1<class BaseClass1>)
		ldstr "G2_C829::Method5.12634()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1906`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G3_C1906`1<class BaseClass1>)
		ldstr "G2_C829::Method6.12635<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1906`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1906`1<class BaseClass1>)
		ldstr "G1_C15::Method7.MI.4886<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1906`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C829`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C829`2<class BaseClass1,class BaseClass0>::ClassMethod3038<object>()
		calli default string(class G3_C1906`1<class BaseClass1>)
		ldstr "G3_C1906::ClassMethod3038.MI.18649<System.Object>()"
		ldstr "class G2_C829`2<class BaseClass1,class BaseClass0> on type class G3_C1906`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C829`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C829`2<class BaseClass1,class BaseClass0>::ClassMethod3037()
		calli default string(class G3_C1906`1<class BaseClass1>)
		ldstr "G2_C829::ClassMethod3037.12636()"
		ldstr "class G2_C829`2<class BaseClass1,class BaseClass0> on type class G3_C1906`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C829`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C829`2<class BaseClass1,class BaseClass0>::Method6<object>()
		calli default string(class G3_C1906`1<class BaseClass1>)
		ldstr "G2_C829::Method6.12635<System.Object>()"
		ldstr "class G2_C829`2<class BaseClass1,class BaseClass0> on type class G3_C1906`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C829`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C829`2<class BaseClass1,class BaseClass0>::Method5()
		calli default string(class G3_C1906`1<class BaseClass1>)
		ldstr "G2_C829::Method5.12634()"
		ldstr "class G2_C829`2<class BaseClass1,class BaseClass0> on type class G3_C1906`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C829`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C829`2<class BaseClass1,class BaseClass0>::Method4()
		calli default string(class G3_C1906`1<class BaseClass1>)
		ldstr "G2_C829::Method4.12633()"
		ldstr "class G2_C829`2<class BaseClass1,class BaseClass0> on type class G3_C1906`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C829`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C829`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1906`1<class BaseClass1>)
		ldstr "G1_C15::Method7.4885<System.Object>()"
		ldstr "class G2_C829`2<class BaseClass1,class BaseClass0> on type class G3_C1906`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method4()
		calli default string(class G3_C1906`1<class BaseClass1>)
		ldstr "G2_C829::Method4.12633()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1906`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method5()
		calli default string(class G3_C1906`1<class BaseClass1>)
		ldstr "G2_C829::Method5.12634()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1906`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method6<object>()
		calli default string(class G3_C1906`1<class BaseClass1>)
		ldstr "G2_C829::Method6.12635<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1906`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1906`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1906`1<class BaseClass1>::ClassMethod5210<object>()
		calli default string(class G3_C1906`1<class BaseClass1>)
		ldstr "G3_C1906::ClassMethod5210.18648<System.Object>()"
		ldstr "class G3_C1906`1<class BaseClass1> on type class G3_C1906`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1906`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1906`1<class BaseClass1>::ClassMethod5209<object>()
		calli default string(class G3_C1906`1<class BaseClass1>)
		ldstr "G3_C1906::ClassMethod5209.18647<System.Object>()"
		ldstr "class G3_C1906`1<class BaseClass1> on type class G3_C1906`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1906`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1906`1<class BaseClass1>::ClassMethod5208()
		calli default string(class G3_C1906`1<class BaseClass1>)
		ldstr "G3_C1906::ClassMethod5208.18646()"
		ldstr "class G3_C1906`1<class BaseClass1> on type class G3_C1906`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1906`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1906`1<class BaseClass1>::ClassMethod5207()
		calli default string(class G3_C1906`1<class BaseClass1>)
		ldstr "G3_C1906::ClassMethod5207.18645()"
		ldstr "class G3_C1906`1<class BaseClass1> on type class G3_C1906`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1906`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1906`1<class BaseClass1>::Method3<object>()
		calli default string(class G3_C1906`1<class BaseClass1>)
		ldstr "G3_C1906::Method3.18644<System.Object>()"
		ldstr "class G3_C1906`1<class BaseClass1> on type class G3_C1906`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1906`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1906`1<class BaseClass1>::Method2<object>()
		calli default string(class G3_C1906`1<class BaseClass1>)
		ldstr "G3_C1906::Method2.18642<System.Object>()"
		ldstr "class G3_C1906`1<class BaseClass1> on type class G3_C1906`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1906`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1906`1<class BaseClass1>::Method1()
		calli default string(class G3_C1906`1<class BaseClass1>)
		ldstr "G3_C1906::Method1.18641()"
		ldstr "class G3_C1906`1<class BaseClass1> on type class G3_C1906`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1906`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1906`1<class BaseClass1>::Method0()
		calli default string(class G3_C1906`1<class BaseClass1>)
		ldstr "G3_C1906::Method0.18640()"
		ldstr "class G3_C1906`1<class BaseClass1> on type class G3_C1906`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1906`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1906`1<class BaseClass1>::ClassMethod3038<object>()
		calli default string(class G3_C1906`1<class BaseClass1>)
		ldstr "G3_C1906::ClassMethod3038.MI.18649<System.Object>()"
		ldstr "class G3_C1906`1<class BaseClass1> on type class G3_C1906`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1906`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1906`1<class BaseClass1>::ClassMethod3037()
		calli default string(class G3_C1906`1<class BaseClass1>)
		ldstr "G2_C829::ClassMethod3037.12636()"
		ldstr "class G3_C1906`1<class BaseClass1> on type class G3_C1906`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1906`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1906`1<class BaseClass1>::Method6<object>()
		calli default string(class G3_C1906`1<class BaseClass1>)
		ldstr "G2_C829::Method6.12635<System.Object>()"
		ldstr "class G3_C1906`1<class BaseClass1> on type class G3_C1906`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1906`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1906`1<class BaseClass1>::Method5()
		calli default string(class G3_C1906`1<class BaseClass1>)
		ldstr "G2_C829::Method5.12634()"
		ldstr "class G3_C1906`1<class BaseClass1> on type class G3_C1906`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1906`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1906`1<class BaseClass1>::Method4()
		calli default string(class G3_C1906`1<class BaseClass1>)
		ldstr "G2_C829::Method4.12633()"
		ldstr "class G3_C1906`1<class BaseClass1> on type class G3_C1906`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1906`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1906`1<class BaseClass1>::Method7<object>()
		calli default string(class G3_C1906`1<class BaseClass1>)
		ldstr "G1_C15::Method7.4885<System.Object>()"
		ldstr "class G3_C1906`1<class BaseClass1> on type class G3_C1906`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method0()
		calli default string(class G3_C1906`1<class BaseClass1>)
		ldstr "G3_C1906::Method0.18640()"
		ldstr "IBase0 on type class G3_C1906`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method1()
		calli default string(class G3_C1906`1<class BaseClass1>)
		ldstr "G3_C1906::Method1.18641()"
		ldstr "IBase0 on type class G3_C1906`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method2<object>()
		calli default string(class G3_C1906`1<class BaseClass1>)
		ldstr "G3_C1906::Method2.MI.18643<System.Object>()"
		ldstr "IBase0 on type class G3_C1906`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method3<object>()
		calli default string(class G3_C1906`1<class BaseClass1>)
		ldstr "G3_C1906::Method3.18644<System.Object>()"
		ldstr "IBase0 on type class G3_C1906`1<class BaseClass1>"
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
