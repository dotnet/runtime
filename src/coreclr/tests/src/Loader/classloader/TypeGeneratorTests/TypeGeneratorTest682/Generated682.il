// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern mscorlib { .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) .ver 4:0:0:0 }
.assembly extern TestFramework { .publickeytoken = ( B0 3F 5F 7F 11 D5 0A 3A )  }

//TYPES IN FORWARDER ASSEMBLIES:

//TEST ASSEMBLY:
.assembly Generated682 { .hash algorithm 0x00008004 }
.module Generated682.exe

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
.class public G3_C1154`1<T0> 
		extends class G2_C221`2<class BaseClass0,!T0>
		implements class IBase2`2<!T0,class BaseClass0> 
{
	.method public hidebysig newslot virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G3_C1154::Method7.14618<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase2<T0,class BaseClass0>.Method7'<M0>() cil managed noinlining { 
		.override method instance string class IBase2`2<!T0,class BaseClass0>::Method7<[1]>()
		ldstr "G3_C1154::Method7.MI.14619<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod3700() cil managed noinlining { 
		ldstr "G3_C1154::ClassMethod3700.14620()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod3701<M0>() cil managed noinlining { 
		ldstr "G3_C1154::ClassMethod3701.14621<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'G2_C221<class BaseClass0,T0>.ClassMethod1773'<M0>() cil managed noinlining { 
		.override method instance string class G2_C221`2<class BaseClass0,!T0>::ClassMethod1773<[1]>()
		ldstr "G3_C1154::ClassMethod1773.MI.14622<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class G2_C221`2<class BaseClass0,!T0>::.ctor()
		ret
	}
}
.class public G2_C221`2<T0, T1> 
		extends G1_C2
		implements class IBase2`2<!T1,!T1> 
{
	.method public hidebysig virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G2_C221::Method7.6813<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase2<T1,T1>.Method7'<M0>() cil managed noinlining { 
		.override method instance string class IBase2`2<!T1,!T1>::Method7<[1]>()
		ldstr "G2_C221::Method7.MI.6814<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1772() cil managed noinlining { 
		ldstr "G2_C221::ClassMethod1772.6815()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1773<M0>() cil managed noinlining { 
		ldstr "G2_C221::ClassMethod1773.6816<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void G1_C2::.ctor()
		ret
	}
}
.class interface public abstract IBase2`2<+T0, -T1> 
{
	.method public hidebysig newslot abstract virtual instance string Method7<M0>() cil managed { }
}
.class public abstract G1_C2 
		implements class IBase2`2<class BaseClass0,class BaseClass0>, class IBase1`1<class BaseClass1> 
{
	.method public hidebysig newslot virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G1_C2::Method7.4778<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig virtual instance string Method4() cil managed noinlining { 
		ldstr "G1_C2::Method4.4779()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<class BaseClass1>.Method4'() cil managed noinlining { 
		.override method instance string class IBase1`1<class BaseClass1>::Method4()
		ldstr "G1_C2::Method4.MI.4780()"
		ret
	}
	.method public hidebysig newslot virtual instance string Method5() cil managed noinlining { 
		ldstr "G1_C2::Method5.4781()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<class BaseClass1>.Method5'() cil managed noinlining { 
		.override method instance string class IBase1`1<class BaseClass1>::Method5()
		ldstr "G1_C2::Method5.MI.4782()"
		ret
	}
	.method public hidebysig newslot virtual instance string Method6<M0>() cil managed noinlining { 
		ldstr "G1_C2::Method6.4783<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<class BaseClass1>.Method6'<M0>() cil managed noinlining { 
		.override method instance string class IBase1`1<class BaseClass1>::Method6<[1]>()
		ldstr "G1_C2::Method6.MI.4784<"
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
	.method static void M.G3_C1154.T<T0,(class G3_C1154`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1154.T<T0,(class G3_C1154`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1154`1<!!T0>::ClassMethod1772()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1154`1<!!T0>::ClassMethod1773<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1154`1<!!T0>::ClassMethod3700()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1154`1<!!T0>::ClassMethod3701<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1154`1<!!T0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1154`1<!!T0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1154`1<!!T0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1154`1<!!T0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1154.A<(class G3_C1154`1<class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1154.A<(class G3_C1154`1<class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1154`1<class BaseClass0>::ClassMethod1772()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1154`1<class BaseClass0>::ClassMethod1773<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1154`1<class BaseClass0>::ClassMethod3700()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1154`1<class BaseClass0>::ClassMethod3701<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1154`1<class BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1154`1<class BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1154`1<class BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1154`1<class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1154.B<(class G3_C1154`1<class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1154.B<(class G3_C1154`1<class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1154`1<class BaseClass1>::ClassMethod1772()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1154`1<class BaseClass1>::ClassMethod1773<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1154`1<class BaseClass1>::ClassMethod3700()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1154`1<class BaseClass1>::ClassMethod3701<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1154`1<class BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1154`1<class BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1154`1<class BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1154`1<class BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C221.T.T<T0,T1,(class G2_C221`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C221.T.T<T0,T1,(class G2_C221`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C221`2<!!T0,!!T1>::ClassMethod1772()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C221`2<!!T0,!!T1>::ClassMethod1773<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C221`2<!!T0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C221`2<!!T0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C221`2<!!T0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C221`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C221.A.T<T1,(class G2_C221`2<class BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C221.A.T<T1,(class G2_C221`2<class BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C221`2<class BaseClass0,!!T1>::ClassMethod1772()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C221`2<class BaseClass0,!!T1>::ClassMethod1773<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C221`2<class BaseClass0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C221`2<class BaseClass0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C221`2<class BaseClass0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C221`2<class BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C221.A.A<(class G2_C221`2<class BaseClass0,class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C221.A.A<(class G2_C221`2<class BaseClass0,class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C221`2<class BaseClass0,class BaseClass0>::ClassMethod1772()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C221`2<class BaseClass0,class BaseClass0>::ClassMethod1773<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C221`2<class BaseClass0,class BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C221`2<class BaseClass0,class BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C221`2<class BaseClass0,class BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C221`2<class BaseClass0,class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C221.A.B<(class G2_C221`2<class BaseClass0,class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C221.A.B<(class G2_C221`2<class BaseClass0,class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C221`2<class BaseClass0,class BaseClass1>::ClassMethod1772()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C221`2<class BaseClass0,class BaseClass1>::ClassMethod1773<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C221`2<class BaseClass0,class BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C221`2<class BaseClass0,class BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C221`2<class BaseClass0,class BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C221`2<class BaseClass0,class BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C221.B.T<T1,(class G2_C221`2<class BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C221.B.T<T1,(class G2_C221`2<class BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C221`2<class BaseClass1,!!T1>::ClassMethod1772()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C221`2<class BaseClass1,!!T1>::ClassMethod1773<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C221`2<class BaseClass1,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C221`2<class BaseClass1,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C221`2<class BaseClass1,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C221`2<class BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C221.B.A<(class G2_C221`2<class BaseClass1,class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C221.B.A<(class G2_C221`2<class BaseClass1,class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C221`2<class BaseClass1,class BaseClass0>::ClassMethod1772()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C221`2<class BaseClass1,class BaseClass0>::ClassMethod1773<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C221`2<class BaseClass1,class BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C221`2<class BaseClass1,class BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C221`2<class BaseClass1,class BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C221`2<class BaseClass1,class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C221.B.B<(class G2_C221`2<class BaseClass1,class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C221.B.B<(class G2_C221`2<class BaseClass1,class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C221`2<class BaseClass1,class BaseClass1>::ClassMethod1772()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C221`2<class BaseClass1,class BaseClass1>::ClassMethod1773<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C221`2<class BaseClass1,class BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C221`2<class BaseClass1,class BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C221`2<class BaseClass1,class BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C221`2<class BaseClass1,class BaseClass1>::Method7<object>()
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
	.method static void M.G1_C2<(G1_C2)W>(!!W inst, string exp) cil managed {
		.maxstack 9
		.locals init (string[] actualResults)
		ldc.i4.s    4
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C2<(G1_C2)W>(!!W inst, string exp)"
		ldc.i4.s 4
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string G1_C2::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string G1_C2::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string G1_C2::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string G1_C2::Method7<object>()
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
		newobj instance void class G3_C1154`1<class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C2
		callvirt instance string class G1_C2::Method6<object>()
		ldstr "G1_C2::Method6.4783<System.Object>()"
		ldstr "class G1_C2 on type class G3_C1154`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C2
		callvirt instance string class G1_C2::Method5()
		ldstr "G1_C2::Method5.4781()"
		ldstr "class G1_C2 on type class G3_C1154`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C2
		callvirt instance string class G1_C2::Method4()
		ldstr "G1_C2::Method4.4779()"
		ldstr "class G1_C2 on type class G3_C1154`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C2
		callvirt instance string class G1_C2::Method7<object>()
		ldstr "G2_C221::Method7.6813<System.Object>()"
		ldstr "class G1_C2 on type class G3_C1154`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G3_C1154::Method7.MI.14619<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G3_C1154`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method4()
		ldstr "G1_C2::Method4.MI.4780()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1154`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method5()
		ldstr "G1_C2::Method5.MI.4782()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1154`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method6<object>()
		ldstr "G1_C2::Method6.MI.4784<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1154`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G3_C1154::Method7.MI.14619<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1154`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G1_C2::Method4.MI.4780()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1154`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G1_C2::Method5.MI.4782()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1154`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G1_C2::Method6.MI.4784<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1154`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C221`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C221`2<class BaseClass0,class BaseClass0>::ClassMethod1773<object>()
		ldstr "G3_C1154::ClassMethod1773.MI.14622<System.Object>()"
		ldstr "class G2_C221`2<class BaseClass0,class BaseClass0> on type class G3_C1154`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C221`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C221`2<class BaseClass0,class BaseClass0>::ClassMethod1772()
		ldstr "G2_C221::ClassMethod1772.6815()"
		ldstr "class G2_C221`2<class BaseClass0,class BaseClass0> on type class G3_C1154`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C221`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C221`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G2_C221::Method7.6813<System.Object>()"
		ldstr "class G2_C221`2<class BaseClass0,class BaseClass0> on type class G3_C1154`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C221`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C221`2<class BaseClass0,class BaseClass0>::Method6<object>()
		ldstr "G1_C2::Method6.4783<System.Object>()"
		ldstr "class G2_C221`2<class BaseClass0,class BaseClass0> on type class G3_C1154`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C221`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C221`2<class BaseClass0,class BaseClass0>::Method5()
		ldstr "G1_C2::Method5.4781()"
		ldstr "class G2_C221`2<class BaseClass0,class BaseClass0> on type class G3_C1154`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C221`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C221`2<class BaseClass0,class BaseClass0>::Method4()
		ldstr "G1_C2::Method4.4779()"
		ldstr "class G2_C221`2<class BaseClass0,class BaseClass0> on type class G3_C1154`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G3_C1154`1<class BaseClass0>
		callvirt instance string class G3_C1154`1<class BaseClass0>::ClassMethod3701<object>()
		ldstr "G3_C1154::ClassMethod3701.14621<System.Object>()"
		ldstr "class G3_C1154`1<class BaseClass0> on type class G3_C1154`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1154`1<class BaseClass0>
		callvirt instance string class G3_C1154`1<class BaseClass0>::ClassMethod3700()
		ldstr "G3_C1154::ClassMethod3700.14620()"
		ldstr "class G3_C1154`1<class BaseClass0> on type class G3_C1154`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1154`1<class BaseClass0>
		callvirt instance string class G3_C1154`1<class BaseClass0>::Method7<object>()
		ldstr "G3_C1154::Method7.14618<System.Object>()"
		ldstr "class G3_C1154`1<class BaseClass0> on type class G3_C1154`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1154`1<class BaseClass0>
		callvirt instance string class G3_C1154`1<class BaseClass0>::ClassMethod1773<object>()
		ldstr "G3_C1154::ClassMethod1773.MI.14622<System.Object>()"
		ldstr "class G3_C1154`1<class BaseClass0> on type class G3_C1154`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1154`1<class BaseClass0>
		callvirt instance string class G3_C1154`1<class BaseClass0>::ClassMethod1772()
		ldstr "G2_C221::ClassMethod1772.6815()"
		ldstr "class G3_C1154`1<class BaseClass0> on type class G3_C1154`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1154`1<class BaseClass0>
		callvirt instance string class G3_C1154`1<class BaseClass0>::Method6<object>()
		ldstr "G1_C2::Method6.4783<System.Object>()"
		ldstr "class G3_C1154`1<class BaseClass0> on type class G3_C1154`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1154`1<class BaseClass0>
		callvirt instance string class G3_C1154`1<class BaseClass0>::Method5()
		ldstr "G1_C2::Method5.4781()"
		ldstr "class G3_C1154`1<class BaseClass0> on type class G3_C1154`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1154`1<class BaseClass0>
		callvirt instance string class G3_C1154`1<class BaseClass0>::Method4()
		ldstr "G1_C2::Method4.4779()"
		ldstr "class G3_C1154`1<class BaseClass0> on type class G3_C1154`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G3_C1154`1<class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C2
		callvirt instance string class G1_C2::Method6<object>()
		ldstr "G1_C2::Method6.4783<System.Object>()"
		ldstr "class G1_C2 on type class G3_C1154`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C2
		callvirt instance string class G1_C2::Method5()
		ldstr "G1_C2::Method5.4781()"
		ldstr "class G1_C2 on type class G3_C1154`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C2
		callvirt instance string class G1_C2::Method4()
		ldstr "G1_C2::Method4.4779()"
		ldstr "class G1_C2 on type class G3_C1154`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C2
		callvirt instance string class G1_C2::Method7<object>()
		ldstr "G2_C221::Method7.6813<System.Object>()"
		ldstr "class G1_C2 on type class G3_C1154`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G3_C1154::Method7.MI.14619<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G3_C1154`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method4()
		ldstr "G1_C2::Method4.MI.4780()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1154`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method5()
		ldstr "G1_C2::Method5.MI.4782()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1154`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method6<object>()
		ldstr "G1_C2::Method6.MI.4784<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1154`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G3_C1154::Method7.MI.14619<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1154`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G1_C2::Method4.MI.4780()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1154`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G1_C2::Method5.MI.4782()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1154`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G1_C2::Method6.MI.4784<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1154`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C221`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C221`2<class BaseClass0,class BaseClass1>::ClassMethod1773<object>()
		ldstr "G3_C1154::ClassMethod1773.MI.14622<System.Object>()"
		ldstr "class G2_C221`2<class BaseClass0,class BaseClass1> on type class G3_C1154`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C221`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C221`2<class BaseClass0,class BaseClass1>::ClassMethod1772()
		ldstr "G2_C221::ClassMethod1772.6815()"
		ldstr "class G2_C221`2<class BaseClass0,class BaseClass1> on type class G3_C1154`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C221`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C221`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C221::Method7.6813<System.Object>()"
		ldstr "class G2_C221`2<class BaseClass0,class BaseClass1> on type class G3_C1154`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C221`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C221`2<class BaseClass0,class BaseClass1>::Method6<object>()
		ldstr "G1_C2::Method6.4783<System.Object>()"
		ldstr "class G2_C221`2<class BaseClass0,class BaseClass1> on type class G3_C1154`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C221`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C221`2<class BaseClass0,class BaseClass1>::Method5()
		ldstr "G1_C2::Method5.4781()"
		ldstr "class G2_C221`2<class BaseClass0,class BaseClass1> on type class G3_C1154`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C221`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C221`2<class BaseClass0,class BaseClass1>::Method4()
		ldstr "G1_C2::Method4.4779()"
		ldstr "class G2_C221`2<class BaseClass0,class BaseClass1> on type class G3_C1154`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G3_C1154::Method7.MI.14619<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G3_C1154`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G3_C1154`1<class BaseClass1>
		callvirt instance string class G3_C1154`1<class BaseClass1>::ClassMethod3701<object>()
		ldstr "G3_C1154::ClassMethod3701.14621<System.Object>()"
		ldstr "class G3_C1154`1<class BaseClass1> on type class G3_C1154`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1154`1<class BaseClass1>
		callvirt instance string class G3_C1154`1<class BaseClass1>::ClassMethod3700()
		ldstr "G3_C1154::ClassMethod3700.14620()"
		ldstr "class G3_C1154`1<class BaseClass1> on type class G3_C1154`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1154`1<class BaseClass1>
		callvirt instance string class G3_C1154`1<class BaseClass1>::Method7<object>()
		ldstr "G3_C1154::Method7.14618<System.Object>()"
		ldstr "class G3_C1154`1<class BaseClass1> on type class G3_C1154`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1154`1<class BaseClass1>
		callvirt instance string class G3_C1154`1<class BaseClass1>::ClassMethod1773<object>()
		ldstr "G3_C1154::ClassMethod1773.MI.14622<System.Object>()"
		ldstr "class G3_C1154`1<class BaseClass1> on type class G3_C1154`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1154`1<class BaseClass1>
		callvirt instance string class G3_C1154`1<class BaseClass1>::ClassMethod1772()
		ldstr "G2_C221::ClassMethod1772.6815()"
		ldstr "class G3_C1154`1<class BaseClass1> on type class G3_C1154`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1154`1<class BaseClass1>
		callvirt instance string class G3_C1154`1<class BaseClass1>::Method6<object>()
		ldstr "G1_C2::Method6.4783<System.Object>()"
		ldstr "class G3_C1154`1<class BaseClass1> on type class G3_C1154`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1154`1<class BaseClass1>
		callvirt instance string class G3_C1154`1<class BaseClass1>::Method5()
		ldstr "G1_C2::Method5.4781()"
		ldstr "class G3_C1154`1<class BaseClass1> on type class G3_C1154`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1154`1<class BaseClass1>
		callvirt instance string class G3_C1154`1<class BaseClass1>::Method4()
		ldstr "G1_C2::Method4.4779()"
		ldstr "class G3_C1154`1<class BaseClass1> on type class G3_C1154`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G3_C1154::Method7.MI.14619<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G3_C1154`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G2_C221`2<class BaseClass0,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C2
		callvirt instance string class G1_C2::Method6<object>()
		ldstr "G1_C2::Method6.4783<System.Object>()"
		ldstr "class G1_C2 on type class G2_C221`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C2
		callvirt instance string class G1_C2::Method5()
		ldstr "G1_C2::Method5.4781()"
		ldstr "class G1_C2 on type class G2_C221`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C2
		callvirt instance string class G1_C2::Method4()
		ldstr "G1_C2::Method4.4779()"
		ldstr "class G1_C2 on type class G2_C221`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C2
		callvirt instance string class G1_C2::Method7<object>()
		ldstr "G2_C221::Method7.6813<System.Object>()"
		ldstr "class G1_C2 on type class G2_C221`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G2_C221::Method7.MI.6814<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G2_C221`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method4()
		ldstr "G1_C2::Method4.MI.4780()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C221`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method5()
		ldstr "G1_C2::Method5.MI.4782()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C221`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method6<object>()
		ldstr "G1_C2::Method6.MI.4784<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C221`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C221::Method7.MI.6814<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C221`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G1_C2::Method4.MI.4780()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C221`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G1_C2::Method5.MI.4782()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C221`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G1_C2::Method6.MI.4784<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C221`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C221`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C221`2<class BaseClass0,class BaseClass0>::ClassMethod1773<object>()
		ldstr "G2_C221::ClassMethod1773.6816<System.Object>()"
		ldstr "class G2_C221`2<class BaseClass0,class BaseClass0> on type class G2_C221`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C221`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C221`2<class BaseClass0,class BaseClass0>::ClassMethod1772()
		ldstr "G2_C221::ClassMethod1772.6815()"
		ldstr "class G2_C221`2<class BaseClass0,class BaseClass0> on type class G2_C221`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C221`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C221`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G2_C221::Method7.6813<System.Object>()"
		ldstr "class G2_C221`2<class BaseClass0,class BaseClass0> on type class G2_C221`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C221`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C221`2<class BaseClass0,class BaseClass0>::Method6<object>()
		ldstr "G1_C2::Method6.4783<System.Object>()"
		ldstr "class G2_C221`2<class BaseClass0,class BaseClass0> on type class G2_C221`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C221`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C221`2<class BaseClass0,class BaseClass0>::Method5()
		ldstr "G1_C2::Method5.4781()"
		ldstr "class G2_C221`2<class BaseClass0,class BaseClass0> on type class G2_C221`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C221`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C221`2<class BaseClass0,class BaseClass0>::Method4()
		ldstr "G1_C2::Method4.4779()"
		ldstr "class G2_C221`2<class BaseClass0,class BaseClass0> on type class G2_C221`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G2_C221`2<class BaseClass0,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C2
		callvirt instance string class G1_C2::Method6<object>()
		ldstr "G1_C2::Method6.4783<System.Object>()"
		ldstr "class G1_C2 on type class G2_C221`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C2
		callvirt instance string class G1_C2::Method5()
		ldstr "G1_C2::Method5.4781()"
		ldstr "class G1_C2 on type class G2_C221`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C2
		callvirt instance string class G1_C2::Method4()
		ldstr "G1_C2::Method4.4779()"
		ldstr "class G1_C2 on type class G2_C221`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C2
		callvirt instance string class G1_C2::Method7<object>()
		ldstr "G2_C221::Method7.6813<System.Object>()"
		ldstr "class G1_C2 on type class G2_C221`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G2_C221::Method7.6813<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G2_C221`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method4()
		ldstr "G1_C2::Method4.MI.4780()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C221`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method5()
		ldstr "G1_C2::Method5.MI.4782()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C221`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method6<object>()
		ldstr "G1_C2::Method6.MI.4784<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C221`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C221::Method7.MI.6814<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C221`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G1_C2::Method4.MI.4780()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C221`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G1_C2::Method5.MI.4782()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C221`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G1_C2::Method6.MI.4784<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C221`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C221`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C221`2<class BaseClass0,class BaseClass1>::ClassMethod1773<object>()
		ldstr "G2_C221::ClassMethod1773.6816<System.Object>()"
		ldstr "class G2_C221`2<class BaseClass0,class BaseClass1> on type class G2_C221`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C221`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C221`2<class BaseClass0,class BaseClass1>::ClassMethod1772()
		ldstr "G2_C221::ClassMethod1772.6815()"
		ldstr "class G2_C221`2<class BaseClass0,class BaseClass1> on type class G2_C221`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C221`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C221`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C221::Method7.6813<System.Object>()"
		ldstr "class G2_C221`2<class BaseClass0,class BaseClass1> on type class G2_C221`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C221`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C221`2<class BaseClass0,class BaseClass1>::Method6<object>()
		ldstr "G1_C2::Method6.4783<System.Object>()"
		ldstr "class G2_C221`2<class BaseClass0,class BaseClass1> on type class G2_C221`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C221`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C221`2<class BaseClass0,class BaseClass1>::Method5()
		ldstr "G1_C2::Method5.4781()"
		ldstr "class G2_C221`2<class BaseClass0,class BaseClass1> on type class G2_C221`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C221`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C221`2<class BaseClass0,class BaseClass1>::Method4()
		ldstr "G1_C2::Method4.4779()"
		ldstr "class G2_C221`2<class BaseClass0,class BaseClass1> on type class G2_C221`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G2_C221::Method7.MI.6814<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C221`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G2_C221`2<class BaseClass1,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C2
		callvirt instance string class G1_C2::Method6<object>()
		ldstr "G1_C2::Method6.4783<System.Object>()"
		ldstr "class G1_C2 on type class G2_C221`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C2
		callvirt instance string class G1_C2::Method5()
		ldstr "G1_C2::Method5.4781()"
		ldstr "class G1_C2 on type class G2_C221`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C2
		callvirt instance string class G1_C2::Method4()
		ldstr "G1_C2::Method4.4779()"
		ldstr "class G1_C2 on type class G2_C221`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C2
		callvirt instance string class G1_C2::Method7<object>()
		ldstr "G2_C221::Method7.6813<System.Object>()"
		ldstr "class G1_C2 on type class G2_C221`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G2_C221::Method7.MI.6814<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G2_C221`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method4()
		ldstr "G1_C2::Method4.MI.4780()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C221`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method5()
		ldstr "G1_C2::Method5.MI.4782()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C221`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method6<object>()
		ldstr "G1_C2::Method6.MI.4784<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C221`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C221::Method7.MI.6814<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C221`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G1_C2::Method4.MI.4780()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C221`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G1_C2::Method5.MI.4782()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C221`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G1_C2::Method6.MI.4784<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C221`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C221`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C221`2<class BaseClass1,class BaseClass0>::ClassMethod1773<object>()
		ldstr "G2_C221::ClassMethod1773.6816<System.Object>()"
		ldstr "class G2_C221`2<class BaseClass1,class BaseClass0> on type class G2_C221`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C221`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C221`2<class BaseClass1,class BaseClass0>::ClassMethod1772()
		ldstr "G2_C221::ClassMethod1772.6815()"
		ldstr "class G2_C221`2<class BaseClass1,class BaseClass0> on type class G2_C221`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C221`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C221`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G2_C221::Method7.6813<System.Object>()"
		ldstr "class G2_C221`2<class BaseClass1,class BaseClass0> on type class G2_C221`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C221`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C221`2<class BaseClass1,class BaseClass0>::Method6<object>()
		ldstr "G1_C2::Method6.4783<System.Object>()"
		ldstr "class G2_C221`2<class BaseClass1,class BaseClass0> on type class G2_C221`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C221`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C221`2<class BaseClass1,class BaseClass0>::Method5()
		ldstr "G1_C2::Method5.4781()"
		ldstr "class G2_C221`2<class BaseClass1,class BaseClass0> on type class G2_C221`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C221`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C221`2<class BaseClass1,class BaseClass0>::Method4()
		ldstr "G1_C2::Method4.4779()"
		ldstr "class G2_C221`2<class BaseClass1,class BaseClass0> on type class G2_C221`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G2_C221`2<class BaseClass1,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C2
		callvirt instance string class G1_C2::Method6<object>()
		ldstr "G1_C2::Method6.4783<System.Object>()"
		ldstr "class G1_C2 on type class G2_C221`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C2
		callvirt instance string class G1_C2::Method5()
		ldstr "G1_C2::Method5.4781()"
		ldstr "class G1_C2 on type class G2_C221`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C2
		callvirt instance string class G1_C2::Method4()
		ldstr "G1_C2::Method4.4779()"
		ldstr "class G1_C2 on type class G2_C221`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C2
		callvirt instance string class G1_C2::Method7<object>()
		ldstr "G2_C221::Method7.6813<System.Object>()"
		ldstr "class G1_C2 on type class G2_C221`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G2_C221::Method7.6813<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G2_C221`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method4()
		ldstr "G1_C2::Method4.MI.4780()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C221`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method5()
		ldstr "G1_C2::Method5.MI.4782()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C221`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method6<object>()
		ldstr "G1_C2::Method6.MI.4784<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C221`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C221::Method7.MI.6814<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C221`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G1_C2::Method4.MI.4780()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C221`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G1_C2::Method5.MI.4782()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C221`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G1_C2::Method6.MI.4784<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C221`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C221`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C221`2<class BaseClass1,class BaseClass1>::ClassMethod1773<object>()
		ldstr "G2_C221::ClassMethod1773.6816<System.Object>()"
		ldstr "class G2_C221`2<class BaseClass1,class BaseClass1> on type class G2_C221`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C221`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C221`2<class BaseClass1,class BaseClass1>::ClassMethod1772()
		ldstr "G2_C221::ClassMethod1772.6815()"
		ldstr "class G2_C221`2<class BaseClass1,class BaseClass1> on type class G2_C221`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C221`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C221`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G2_C221::Method7.6813<System.Object>()"
		ldstr "class G2_C221`2<class BaseClass1,class BaseClass1> on type class G2_C221`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C221`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C221`2<class BaseClass1,class BaseClass1>::Method6<object>()
		ldstr "G1_C2::Method6.4783<System.Object>()"
		ldstr "class G2_C221`2<class BaseClass1,class BaseClass1> on type class G2_C221`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C221`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C221`2<class BaseClass1,class BaseClass1>::Method5()
		ldstr "G1_C2::Method5.4781()"
		ldstr "class G2_C221`2<class BaseClass1,class BaseClass1> on type class G2_C221`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C221`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C221`2<class BaseClass1,class BaseClass1>::Method4()
		ldstr "G1_C2::Method4.4779()"
		ldstr "class G2_C221`2<class BaseClass1,class BaseClass1> on type class G2_C221`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G2_C221::Method7.MI.6814<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C221`2<class BaseClass1,class BaseClass1>"
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
		newobj instance void class G3_C1154`1<class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C2::Method4.4779()#G1_C2::Method5.4781()#G1_C2::Method6.4783<System.Object>()#G2_C221::Method7.6813<System.Object>()#"
		call void Framework::M.G1_C2<class G3_C1154`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1154::Method7.MI.14619<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G3_C1154`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G3_C1154::Method7.MI.14619<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass0,class G3_C1154`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G3_C1154::Method7.MI.14619<System.Object>()#"
		call void Framework::M.IBase2.A.A<class G3_C1154`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C2::Method4.MI.4780()#G1_C2::Method5.MI.4782()#G1_C2::Method6.MI.4784<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass1,class G3_C1154`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C2::Method4.MI.4780()#G1_C2::Method5.MI.4782()#G1_C2::Method6.MI.4784<System.Object>()#"
		call void Framework::M.IBase1.B<class G3_C1154`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1154::Method7.MI.14619<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G3_C1154`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G3_C1154::Method7.MI.14619<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G3_C1154`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G3_C1154::Method7.MI.14619<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G3_C1154`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C2::Method4.MI.4780()#G1_C2::Method5.MI.4782()#G1_C2::Method6.MI.4784<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass0,class G3_C1154`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C2::Method4.MI.4780()#G1_C2::Method5.MI.4782()#G1_C2::Method6.MI.4784<System.Object>()#"
		call void Framework::M.IBase1.A<class G3_C1154`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C221::ClassMethod1772.6815()#G3_C1154::ClassMethod1773.MI.14622<System.Object>()#G1_C2::Method4.4779()#G1_C2::Method5.4781()#G1_C2::Method6.4783<System.Object>()#G2_C221::Method7.6813<System.Object>()#"
		call void Framework::M.G2_C221.T.T<class BaseClass0,class BaseClass0,class G3_C1154`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C221::ClassMethod1772.6815()#G3_C1154::ClassMethod1773.MI.14622<System.Object>()#G1_C2::Method4.4779()#G1_C2::Method5.4781()#G1_C2::Method6.4783<System.Object>()#G2_C221::Method7.6813<System.Object>()#"
		call void Framework::M.G2_C221.A.T<class BaseClass0,class G3_C1154`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C221::ClassMethod1772.6815()#G3_C1154::ClassMethod1773.MI.14622<System.Object>()#G1_C2::Method4.4779()#G1_C2::Method5.4781()#G1_C2::Method6.4783<System.Object>()#G2_C221::Method7.6813<System.Object>()#"
		call void Framework::M.G2_C221.A.A<class G3_C1154`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C221::ClassMethod1772.6815()#G3_C1154::ClassMethod1773.MI.14622<System.Object>()#G3_C1154::ClassMethod3700.14620()#G3_C1154::ClassMethod3701.14621<System.Object>()#G1_C2::Method4.4779()#G1_C2::Method5.4781()#G1_C2::Method6.4783<System.Object>()#G3_C1154::Method7.14618<System.Object>()#"
		call void Framework::M.G3_C1154.T<class BaseClass0,class G3_C1154`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C221::ClassMethod1772.6815()#G3_C1154::ClassMethod1773.MI.14622<System.Object>()#G3_C1154::ClassMethod3700.14620()#G3_C1154::ClassMethod3701.14621<System.Object>()#G1_C2::Method4.4779()#G1_C2::Method5.4781()#G1_C2::Method6.4783<System.Object>()#G3_C1154::Method7.14618<System.Object>()#"
		call void Framework::M.G3_C1154.A<class G3_C1154`1<class BaseClass0>>(!!0,string)
		newobj instance void class G3_C1154`1<class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C2::Method4.4779()#G1_C2::Method5.4781()#G1_C2::Method6.4783<System.Object>()#G2_C221::Method7.6813<System.Object>()#"
		call void Framework::M.G1_C2<class G3_C1154`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1154::Method7.MI.14619<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G3_C1154`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G3_C1154::Method7.MI.14619<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass0,class G3_C1154`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1154::Method7.MI.14619<System.Object>()#"
		call void Framework::M.IBase2.A.A<class G3_C1154`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C2::Method4.MI.4780()#G1_C2::Method5.MI.4782()#G1_C2::Method6.MI.4784<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass1,class G3_C1154`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C2::Method4.MI.4780()#G1_C2::Method5.MI.4782()#G1_C2::Method6.MI.4784<System.Object>()#"
		call void Framework::M.IBase1.B<class G3_C1154`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1154::Method7.MI.14619<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G3_C1154`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G3_C1154::Method7.MI.14619<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G3_C1154`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1154::Method7.MI.14619<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G3_C1154`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C2::Method4.MI.4780()#G1_C2::Method5.MI.4782()#G1_C2::Method6.MI.4784<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass0,class G3_C1154`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C2::Method4.MI.4780()#G1_C2::Method5.MI.4782()#G1_C2::Method6.MI.4784<System.Object>()#"
		call void Framework::M.IBase1.A<class G3_C1154`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C221::ClassMethod1772.6815()#G3_C1154::ClassMethod1773.MI.14622<System.Object>()#G1_C2::Method4.4779()#G1_C2::Method5.4781()#G1_C2::Method6.4783<System.Object>()#G2_C221::Method7.6813<System.Object>()#"
		call void Framework::M.G2_C221.T.T<class BaseClass0,class BaseClass1,class G3_C1154`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C221::ClassMethod1772.6815()#G3_C1154::ClassMethod1773.MI.14622<System.Object>()#G1_C2::Method4.4779()#G1_C2::Method5.4781()#G1_C2::Method6.4783<System.Object>()#G2_C221::Method7.6813<System.Object>()#"
		call void Framework::M.G2_C221.A.T<class BaseClass1,class G3_C1154`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C221::ClassMethod1772.6815()#G3_C1154::ClassMethod1773.MI.14622<System.Object>()#G1_C2::Method4.4779()#G1_C2::Method5.4781()#G1_C2::Method6.4783<System.Object>()#G2_C221::Method7.6813<System.Object>()#"
		call void Framework::M.G2_C221.A.B<class G3_C1154`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1154::Method7.MI.14619<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G3_C1154`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G3_C1154::Method7.MI.14619<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass1,class G3_C1154`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1154::Method7.MI.14619<System.Object>()#"
		call void Framework::M.IBase2.B.B<class G3_C1154`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C221::ClassMethod1772.6815()#G3_C1154::ClassMethod1773.MI.14622<System.Object>()#G3_C1154::ClassMethod3700.14620()#G3_C1154::ClassMethod3701.14621<System.Object>()#G1_C2::Method4.4779()#G1_C2::Method5.4781()#G1_C2::Method6.4783<System.Object>()#G3_C1154::Method7.14618<System.Object>()#"
		call void Framework::M.G3_C1154.T<class BaseClass1,class G3_C1154`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C221::ClassMethod1772.6815()#G3_C1154::ClassMethod1773.MI.14622<System.Object>()#G3_C1154::ClassMethod3700.14620()#G3_C1154::ClassMethod3701.14621<System.Object>()#G1_C2::Method4.4779()#G1_C2::Method5.4781()#G1_C2::Method6.4783<System.Object>()#G3_C1154::Method7.14618<System.Object>()#"
		call void Framework::M.G3_C1154.B<class G3_C1154`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1154::Method7.MI.14619<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass0,class G3_C1154`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G3_C1154::Method7.MI.14619<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass0,class G3_C1154`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1154::Method7.MI.14619<System.Object>()#"
		call void Framework::M.IBase2.B.A<class G3_C1154`1<class BaseClass1>>(!!0,string)
		newobj instance void class G2_C221`2<class BaseClass0,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C2::Method4.4779()#G1_C2::Method5.4781()#G1_C2::Method6.4783<System.Object>()#G2_C221::Method7.6813<System.Object>()#"
		call void Framework::M.G1_C2<class G2_C221`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C221::Method7.MI.6814<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G2_C221`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C221::Method7.MI.6814<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass0,class G2_C221`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C221::Method7.MI.6814<System.Object>()#"
		call void Framework::M.IBase2.A.A<class G2_C221`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C2::Method4.MI.4780()#G1_C2::Method5.MI.4782()#G1_C2::Method6.MI.4784<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass1,class G2_C221`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C2::Method4.MI.4780()#G1_C2::Method5.MI.4782()#G1_C2::Method6.MI.4784<System.Object>()#"
		call void Framework::M.IBase1.B<class G2_C221`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C221::Method7.MI.6814<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G2_C221`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C221::Method7.MI.6814<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G2_C221`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C221::Method7.MI.6814<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G2_C221`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C2::Method4.MI.4780()#G1_C2::Method5.MI.4782()#G1_C2::Method6.MI.4784<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass0,class G2_C221`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C2::Method4.MI.4780()#G1_C2::Method5.MI.4782()#G1_C2::Method6.MI.4784<System.Object>()#"
		call void Framework::M.IBase1.A<class G2_C221`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C221::ClassMethod1772.6815()#G2_C221::ClassMethod1773.6816<System.Object>()#G1_C2::Method4.4779()#G1_C2::Method5.4781()#G1_C2::Method6.4783<System.Object>()#G2_C221::Method7.6813<System.Object>()#"
		call void Framework::M.G2_C221.T.T<class BaseClass0,class BaseClass0,class G2_C221`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C221::ClassMethod1772.6815()#G2_C221::ClassMethod1773.6816<System.Object>()#G1_C2::Method4.4779()#G1_C2::Method5.4781()#G1_C2::Method6.4783<System.Object>()#G2_C221::Method7.6813<System.Object>()#"
		call void Framework::M.G2_C221.A.T<class BaseClass0,class G2_C221`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C221::ClassMethod1772.6815()#G2_C221::ClassMethod1773.6816<System.Object>()#G1_C2::Method4.4779()#G1_C2::Method5.4781()#G1_C2::Method6.4783<System.Object>()#G2_C221::Method7.6813<System.Object>()#"
		call void Framework::M.G2_C221.A.A<class G2_C221`2<class BaseClass0,class BaseClass0>>(!!0,string)
		newobj instance void class G2_C221`2<class BaseClass0,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C2::Method4.4779()#G1_C2::Method5.4781()#G1_C2::Method6.4783<System.Object>()#G2_C221::Method7.6813<System.Object>()#"
		call void Framework::M.G1_C2<class G2_C221`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C221::Method7.6813<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G2_C221`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C221::Method7.6813<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass0,class G2_C221`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C221::Method7.6813<System.Object>()#"
		call void Framework::M.IBase2.A.A<class G2_C221`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C2::Method4.MI.4780()#G1_C2::Method5.MI.4782()#G1_C2::Method6.MI.4784<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass1,class G2_C221`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C2::Method4.MI.4780()#G1_C2::Method5.MI.4782()#G1_C2::Method6.MI.4784<System.Object>()#"
		call void Framework::M.IBase1.B<class G2_C221`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C221::Method7.MI.6814<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G2_C221`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C221::Method7.MI.6814<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G2_C221`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C221::Method7.MI.6814<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G2_C221`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C2::Method4.MI.4780()#G1_C2::Method5.MI.4782()#G1_C2::Method6.MI.4784<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass0,class G2_C221`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C2::Method4.MI.4780()#G1_C2::Method5.MI.4782()#G1_C2::Method6.MI.4784<System.Object>()#"
		call void Framework::M.IBase1.A<class G2_C221`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C221::ClassMethod1772.6815()#G2_C221::ClassMethod1773.6816<System.Object>()#G1_C2::Method4.4779()#G1_C2::Method5.4781()#G1_C2::Method6.4783<System.Object>()#G2_C221::Method7.6813<System.Object>()#"
		call void Framework::M.G2_C221.T.T<class BaseClass0,class BaseClass1,class G2_C221`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C221::ClassMethod1772.6815()#G2_C221::ClassMethod1773.6816<System.Object>()#G1_C2::Method4.4779()#G1_C2::Method5.4781()#G1_C2::Method6.4783<System.Object>()#G2_C221::Method7.6813<System.Object>()#"
		call void Framework::M.G2_C221.A.T<class BaseClass1,class G2_C221`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C221::ClassMethod1772.6815()#G2_C221::ClassMethod1773.6816<System.Object>()#G1_C2::Method4.4779()#G1_C2::Method5.4781()#G1_C2::Method6.4783<System.Object>()#G2_C221::Method7.6813<System.Object>()#"
		call void Framework::M.G2_C221.A.B<class G2_C221`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C221::Method7.MI.6814<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G2_C221`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C221::Method7.MI.6814<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass1,class G2_C221`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C221::Method7.MI.6814<System.Object>()#"
		call void Framework::M.IBase2.B.B<class G2_C221`2<class BaseClass0,class BaseClass1>>(!!0,string)
		newobj instance void class G2_C221`2<class BaseClass1,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C2::Method4.4779()#G1_C2::Method5.4781()#G1_C2::Method6.4783<System.Object>()#G2_C221::Method7.6813<System.Object>()#"
		call void Framework::M.G1_C2<class G2_C221`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C221::Method7.MI.6814<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G2_C221`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C221::Method7.MI.6814<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass0,class G2_C221`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C221::Method7.MI.6814<System.Object>()#"
		call void Framework::M.IBase2.A.A<class G2_C221`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C2::Method4.MI.4780()#G1_C2::Method5.MI.4782()#G1_C2::Method6.MI.4784<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass1,class G2_C221`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C2::Method4.MI.4780()#G1_C2::Method5.MI.4782()#G1_C2::Method6.MI.4784<System.Object>()#"
		call void Framework::M.IBase1.B<class G2_C221`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C221::Method7.MI.6814<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G2_C221`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C221::Method7.MI.6814<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G2_C221`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C221::Method7.MI.6814<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G2_C221`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C2::Method4.MI.4780()#G1_C2::Method5.MI.4782()#G1_C2::Method6.MI.4784<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass0,class G2_C221`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C2::Method4.MI.4780()#G1_C2::Method5.MI.4782()#G1_C2::Method6.MI.4784<System.Object>()#"
		call void Framework::M.IBase1.A<class G2_C221`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C221::ClassMethod1772.6815()#G2_C221::ClassMethod1773.6816<System.Object>()#G1_C2::Method4.4779()#G1_C2::Method5.4781()#G1_C2::Method6.4783<System.Object>()#G2_C221::Method7.6813<System.Object>()#"
		call void Framework::M.G2_C221.T.T<class BaseClass1,class BaseClass0,class G2_C221`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C221::ClassMethod1772.6815()#G2_C221::ClassMethod1773.6816<System.Object>()#G1_C2::Method4.4779()#G1_C2::Method5.4781()#G1_C2::Method6.4783<System.Object>()#G2_C221::Method7.6813<System.Object>()#"
		call void Framework::M.G2_C221.B.T<class BaseClass0,class G2_C221`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C221::ClassMethod1772.6815()#G2_C221::ClassMethod1773.6816<System.Object>()#G1_C2::Method4.4779()#G1_C2::Method5.4781()#G1_C2::Method6.4783<System.Object>()#G2_C221::Method7.6813<System.Object>()#"
		call void Framework::M.G2_C221.B.A<class G2_C221`2<class BaseClass1,class BaseClass0>>(!!0,string)
		newobj instance void class G2_C221`2<class BaseClass1,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C2::Method4.4779()#G1_C2::Method5.4781()#G1_C2::Method6.4783<System.Object>()#G2_C221::Method7.6813<System.Object>()#"
		call void Framework::M.G1_C2<class G2_C221`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C221::Method7.6813<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G2_C221`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C221::Method7.6813<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass0,class G2_C221`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C221::Method7.6813<System.Object>()#"
		call void Framework::M.IBase2.A.A<class G2_C221`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C2::Method4.MI.4780()#G1_C2::Method5.MI.4782()#G1_C2::Method6.MI.4784<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass1,class G2_C221`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C2::Method4.MI.4780()#G1_C2::Method5.MI.4782()#G1_C2::Method6.MI.4784<System.Object>()#"
		call void Framework::M.IBase1.B<class G2_C221`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C221::Method7.MI.6814<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G2_C221`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C221::Method7.MI.6814<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G2_C221`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C221::Method7.MI.6814<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G2_C221`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C2::Method4.MI.4780()#G1_C2::Method5.MI.4782()#G1_C2::Method6.MI.4784<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass0,class G2_C221`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C2::Method4.MI.4780()#G1_C2::Method5.MI.4782()#G1_C2::Method6.MI.4784<System.Object>()#"
		call void Framework::M.IBase1.A<class G2_C221`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C221::ClassMethod1772.6815()#G2_C221::ClassMethod1773.6816<System.Object>()#G1_C2::Method4.4779()#G1_C2::Method5.4781()#G1_C2::Method6.4783<System.Object>()#G2_C221::Method7.6813<System.Object>()#"
		call void Framework::M.G2_C221.T.T<class BaseClass1,class BaseClass1,class G2_C221`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C221::ClassMethod1772.6815()#G2_C221::ClassMethod1773.6816<System.Object>()#G1_C2::Method4.4779()#G1_C2::Method5.4781()#G1_C2::Method6.4783<System.Object>()#G2_C221::Method7.6813<System.Object>()#"
		call void Framework::M.G2_C221.B.T<class BaseClass1,class G2_C221`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C221::ClassMethod1772.6815()#G2_C221::ClassMethod1773.6816<System.Object>()#G1_C2::Method4.4779()#G1_C2::Method5.4781()#G1_C2::Method6.4783<System.Object>()#G2_C221::Method7.6813<System.Object>()#"
		call void Framework::M.G2_C221.B.B<class G2_C221`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C221::Method7.MI.6814<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G2_C221`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C221::Method7.MI.6814<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass1,class G2_C221`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C221::Method7.MI.6814<System.Object>()#"
		call void Framework::M.IBase2.B.B<class G2_C221`2<class BaseClass1,class BaseClass1>>(!!0,string)
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
		newobj instance void class G3_C1154`1<class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C2
		ldloc.0
		ldvirtftn instance string class G1_C2::Method6<object>()
		calli default string(class G3_C1154`1<class BaseClass0>)
		ldstr "G1_C2::Method6.4783<System.Object>()"
		ldstr "class G1_C2 on type class G3_C1154`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C2
		ldloc.0
		ldvirtftn instance string class G1_C2::Method5()
		calli default string(class G3_C1154`1<class BaseClass0>)
		ldstr "G1_C2::Method5.4781()"
		ldstr "class G1_C2 on type class G3_C1154`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C2
		ldloc.0
		ldvirtftn instance string class G1_C2::Method4()
		calli default string(class G3_C1154`1<class BaseClass0>)
		ldstr "G1_C2::Method4.4779()"
		ldstr "class G1_C2 on type class G3_C1154`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C2
		ldloc.0
		ldvirtftn instance string class G1_C2::Method7<object>()
		calli default string(class G3_C1154`1<class BaseClass0>)
		ldstr "G2_C221::Method7.6813<System.Object>()"
		ldstr "class G1_C2 on type class G3_C1154`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1154`1<class BaseClass0>)
		ldstr "G3_C1154::Method7.MI.14619<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G3_C1154`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method4()
		calli default string(class G3_C1154`1<class BaseClass0>)
		ldstr "G1_C2::Method4.MI.4780()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1154`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method5()
		calli default string(class G3_C1154`1<class BaseClass0>)
		ldstr "G1_C2::Method5.MI.4782()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1154`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method6<object>()
		calli default string(class G3_C1154`1<class BaseClass0>)
		ldstr "G1_C2::Method6.MI.4784<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1154`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1154`1<class BaseClass0>)
		ldstr "G3_C1154::Method7.MI.14619<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1154`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G3_C1154`1<class BaseClass0>)
		ldstr "G1_C2::Method4.MI.4780()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1154`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G3_C1154`1<class BaseClass0>)
		ldstr "G1_C2::Method5.MI.4782()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1154`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G3_C1154`1<class BaseClass0>)
		ldstr "G1_C2::Method6.MI.4784<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1154`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C221`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C221`2<class BaseClass0,class BaseClass0>::ClassMethod1773<object>()
		calli default string(class G3_C1154`1<class BaseClass0>)
		ldstr "G3_C1154::ClassMethod1773.MI.14622<System.Object>()"
		ldstr "class G2_C221`2<class BaseClass0,class BaseClass0> on type class G3_C1154`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C221`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C221`2<class BaseClass0,class BaseClass0>::ClassMethod1772()
		calli default string(class G3_C1154`1<class BaseClass0>)
		ldstr "G2_C221::ClassMethod1772.6815()"
		ldstr "class G2_C221`2<class BaseClass0,class BaseClass0> on type class G3_C1154`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C221`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C221`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1154`1<class BaseClass0>)
		ldstr "G2_C221::Method7.6813<System.Object>()"
		ldstr "class G2_C221`2<class BaseClass0,class BaseClass0> on type class G3_C1154`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C221`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C221`2<class BaseClass0,class BaseClass0>::Method6<object>()
		calli default string(class G3_C1154`1<class BaseClass0>)
		ldstr "G1_C2::Method6.4783<System.Object>()"
		ldstr "class G2_C221`2<class BaseClass0,class BaseClass0> on type class G3_C1154`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C221`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C221`2<class BaseClass0,class BaseClass0>::Method5()
		calli default string(class G3_C1154`1<class BaseClass0>)
		ldstr "G1_C2::Method5.4781()"
		ldstr "class G2_C221`2<class BaseClass0,class BaseClass0> on type class G3_C1154`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C221`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C221`2<class BaseClass0,class BaseClass0>::Method4()
		calli default string(class G3_C1154`1<class BaseClass0>)
		ldstr "G1_C2::Method4.4779()"
		ldstr "class G2_C221`2<class BaseClass0,class BaseClass0> on type class G3_C1154`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1154`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1154`1<class BaseClass0>::ClassMethod3701<object>()
		calli default string(class G3_C1154`1<class BaseClass0>)
		ldstr "G3_C1154::ClassMethod3701.14621<System.Object>()"
		ldstr "class G3_C1154`1<class BaseClass0> on type class G3_C1154`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1154`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1154`1<class BaseClass0>::ClassMethod3700()
		calli default string(class G3_C1154`1<class BaseClass0>)
		ldstr "G3_C1154::ClassMethod3700.14620()"
		ldstr "class G3_C1154`1<class BaseClass0> on type class G3_C1154`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1154`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1154`1<class BaseClass0>::Method7<object>()
		calli default string(class G3_C1154`1<class BaseClass0>)
		ldstr "G3_C1154::Method7.14618<System.Object>()"
		ldstr "class G3_C1154`1<class BaseClass0> on type class G3_C1154`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1154`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1154`1<class BaseClass0>::ClassMethod1773<object>()
		calli default string(class G3_C1154`1<class BaseClass0>)
		ldstr "G3_C1154::ClassMethod1773.MI.14622<System.Object>()"
		ldstr "class G3_C1154`1<class BaseClass0> on type class G3_C1154`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1154`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1154`1<class BaseClass0>::ClassMethod1772()
		calli default string(class G3_C1154`1<class BaseClass0>)
		ldstr "G2_C221::ClassMethod1772.6815()"
		ldstr "class G3_C1154`1<class BaseClass0> on type class G3_C1154`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1154`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1154`1<class BaseClass0>::Method6<object>()
		calli default string(class G3_C1154`1<class BaseClass0>)
		ldstr "G1_C2::Method6.4783<System.Object>()"
		ldstr "class G3_C1154`1<class BaseClass0> on type class G3_C1154`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1154`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1154`1<class BaseClass0>::Method5()
		calli default string(class G3_C1154`1<class BaseClass0>)
		ldstr "G1_C2::Method5.4781()"
		ldstr "class G3_C1154`1<class BaseClass0> on type class G3_C1154`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1154`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1154`1<class BaseClass0>::Method4()
		calli default string(class G3_C1154`1<class BaseClass0>)
		ldstr "G1_C2::Method4.4779()"
		ldstr "class G3_C1154`1<class BaseClass0> on type class G3_C1154`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G3_C1154`1<class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C2
		ldloc.0
		ldvirtftn instance string class G1_C2::Method6<object>()
		calli default string(class G3_C1154`1<class BaseClass1>)
		ldstr "G1_C2::Method6.4783<System.Object>()"
		ldstr "class G1_C2 on type class G3_C1154`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C2
		ldloc.0
		ldvirtftn instance string class G1_C2::Method5()
		calli default string(class G3_C1154`1<class BaseClass1>)
		ldstr "G1_C2::Method5.4781()"
		ldstr "class G1_C2 on type class G3_C1154`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C2
		ldloc.0
		ldvirtftn instance string class G1_C2::Method4()
		calli default string(class G3_C1154`1<class BaseClass1>)
		ldstr "G1_C2::Method4.4779()"
		ldstr "class G1_C2 on type class G3_C1154`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C2
		ldloc.0
		ldvirtftn instance string class G1_C2::Method7<object>()
		calli default string(class G3_C1154`1<class BaseClass1>)
		ldstr "G2_C221::Method7.6813<System.Object>()"
		ldstr "class G1_C2 on type class G3_C1154`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1154`1<class BaseClass1>)
		ldstr "G3_C1154::Method7.MI.14619<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G3_C1154`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method4()
		calli default string(class G3_C1154`1<class BaseClass1>)
		ldstr "G1_C2::Method4.MI.4780()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1154`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method5()
		calli default string(class G3_C1154`1<class BaseClass1>)
		ldstr "G1_C2::Method5.MI.4782()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1154`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method6<object>()
		calli default string(class G3_C1154`1<class BaseClass1>)
		ldstr "G1_C2::Method6.MI.4784<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1154`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1154`1<class BaseClass1>)
		ldstr "G3_C1154::Method7.MI.14619<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1154`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G3_C1154`1<class BaseClass1>)
		ldstr "G1_C2::Method4.MI.4780()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1154`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G3_C1154`1<class BaseClass1>)
		ldstr "G1_C2::Method5.MI.4782()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1154`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G3_C1154`1<class BaseClass1>)
		ldstr "G1_C2::Method6.MI.4784<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1154`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C221`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C221`2<class BaseClass0,class BaseClass1>::ClassMethod1773<object>()
		calli default string(class G3_C1154`1<class BaseClass1>)
		ldstr "G3_C1154::ClassMethod1773.MI.14622<System.Object>()"
		ldstr "class G2_C221`2<class BaseClass0,class BaseClass1> on type class G3_C1154`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C221`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C221`2<class BaseClass0,class BaseClass1>::ClassMethod1772()
		calli default string(class G3_C1154`1<class BaseClass1>)
		ldstr "G2_C221::ClassMethod1772.6815()"
		ldstr "class G2_C221`2<class BaseClass0,class BaseClass1> on type class G3_C1154`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C221`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C221`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1154`1<class BaseClass1>)
		ldstr "G2_C221::Method7.6813<System.Object>()"
		ldstr "class G2_C221`2<class BaseClass0,class BaseClass1> on type class G3_C1154`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C221`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C221`2<class BaseClass0,class BaseClass1>::Method6<object>()
		calli default string(class G3_C1154`1<class BaseClass1>)
		ldstr "G1_C2::Method6.4783<System.Object>()"
		ldstr "class G2_C221`2<class BaseClass0,class BaseClass1> on type class G3_C1154`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C221`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C221`2<class BaseClass0,class BaseClass1>::Method5()
		calli default string(class G3_C1154`1<class BaseClass1>)
		ldstr "G1_C2::Method5.4781()"
		ldstr "class G2_C221`2<class BaseClass0,class BaseClass1> on type class G3_C1154`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C221`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C221`2<class BaseClass0,class BaseClass1>::Method4()
		calli default string(class G3_C1154`1<class BaseClass1>)
		ldstr "G1_C2::Method4.4779()"
		ldstr "class G2_C221`2<class BaseClass0,class BaseClass1> on type class G3_C1154`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1154`1<class BaseClass1>)
		ldstr "G3_C1154::Method7.MI.14619<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G3_C1154`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1154`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1154`1<class BaseClass1>::ClassMethod3701<object>()
		calli default string(class G3_C1154`1<class BaseClass1>)
		ldstr "G3_C1154::ClassMethod3701.14621<System.Object>()"
		ldstr "class G3_C1154`1<class BaseClass1> on type class G3_C1154`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1154`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1154`1<class BaseClass1>::ClassMethod3700()
		calli default string(class G3_C1154`1<class BaseClass1>)
		ldstr "G3_C1154::ClassMethod3700.14620()"
		ldstr "class G3_C1154`1<class BaseClass1> on type class G3_C1154`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1154`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1154`1<class BaseClass1>::Method7<object>()
		calli default string(class G3_C1154`1<class BaseClass1>)
		ldstr "G3_C1154::Method7.14618<System.Object>()"
		ldstr "class G3_C1154`1<class BaseClass1> on type class G3_C1154`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1154`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1154`1<class BaseClass1>::ClassMethod1773<object>()
		calli default string(class G3_C1154`1<class BaseClass1>)
		ldstr "G3_C1154::ClassMethod1773.MI.14622<System.Object>()"
		ldstr "class G3_C1154`1<class BaseClass1> on type class G3_C1154`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1154`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1154`1<class BaseClass1>::ClassMethod1772()
		calli default string(class G3_C1154`1<class BaseClass1>)
		ldstr "G2_C221::ClassMethod1772.6815()"
		ldstr "class G3_C1154`1<class BaseClass1> on type class G3_C1154`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1154`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1154`1<class BaseClass1>::Method6<object>()
		calli default string(class G3_C1154`1<class BaseClass1>)
		ldstr "G1_C2::Method6.4783<System.Object>()"
		ldstr "class G3_C1154`1<class BaseClass1> on type class G3_C1154`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1154`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1154`1<class BaseClass1>::Method5()
		calli default string(class G3_C1154`1<class BaseClass1>)
		ldstr "G1_C2::Method5.4781()"
		ldstr "class G3_C1154`1<class BaseClass1> on type class G3_C1154`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1154`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1154`1<class BaseClass1>::Method4()
		calli default string(class G3_C1154`1<class BaseClass1>)
		ldstr "G1_C2::Method4.4779()"
		ldstr "class G3_C1154`1<class BaseClass1> on type class G3_C1154`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1154`1<class BaseClass1>)
		ldstr "G3_C1154::Method7.MI.14619<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G3_C1154`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G2_C221`2<class BaseClass0,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C2
		ldloc.0
		ldvirtftn instance string class G1_C2::Method6<object>()
		calli default string(class G2_C221`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C2::Method6.4783<System.Object>()"
		ldstr "class G1_C2 on type class G2_C221`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C2
		ldloc.0
		ldvirtftn instance string class G1_C2::Method5()
		calli default string(class G2_C221`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C2::Method5.4781()"
		ldstr "class G1_C2 on type class G2_C221`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C2
		ldloc.0
		ldvirtftn instance string class G1_C2::Method4()
		calli default string(class G2_C221`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C2::Method4.4779()"
		ldstr "class G1_C2 on type class G2_C221`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C2
		ldloc.0
		ldvirtftn instance string class G1_C2::Method7<object>()
		calli default string(class G2_C221`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C221::Method7.6813<System.Object>()"
		ldstr "class G1_C2 on type class G2_C221`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G2_C221`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C221::Method7.MI.6814<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G2_C221`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method4()
		calli default string(class G2_C221`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C2::Method4.MI.4780()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C221`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method5()
		calli default string(class G2_C221`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C2::Method5.MI.4782()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C221`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method6<object>()
		calli default string(class G2_C221`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C2::Method6.MI.4784<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C221`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C221`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C221::Method7.MI.6814<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C221`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G2_C221`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C2::Method4.MI.4780()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C221`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G2_C221`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C2::Method5.MI.4782()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C221`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G2_C221`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C2::Method6.MI.4784<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C221`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C221`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C221`2<class BaseClass0,class BaseClass0>::ClassMethod1773<object>()
		calli default string(class G2_C221`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C221::ClassMethod1773.6816<System.Object>()"
		ldstr "class G2_C221`2<class BaseClass0,class BaseClass0> on type class G2_C221`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C221`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C221`2<class BaseClass0,class BaseClass0>::ClassMethod1772()
		calli default string(class G2_C221`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C221::ClassMethod1772.6815()"
		ldstr "class G2_C221`2<class BaseClass0,class BaseClass0> on type class G2_C221`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C221`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C221`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G2_C221`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C221::Method7.6813<System.Object>()"
		ldstr "class G2_C221`2<class BaseClass0,class BaseClass0> on type class G2_C221`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C221`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C221`2<class BaseClass0,class BaseClass0>::Method6<object>()
		calli default string(class G2_C221`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C2::Method6.4783<System.Object>()"
		ldstr "class G2_C221`2<class BaseClass0,class BaseClass0> on type class G2_C221`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C221`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C221`2<class BaseClass0,class BaseClass0>::Method5()
		calli default string(class G2_C221`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C2::Method5.4781()"
		ldstr "class G2_C221`2<class BaseClass0,class BaseClass0> on type class G2_C221`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C221`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C221`2<class BaseClass0,class BaseClass0>::Method4()
		calli default string(class G2_C221`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C2::Method4.4779()"
		ldstr "class G2_C221`2<class BaseClass0,class BaseClass0> on type class G2_C221`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G2_C221`2<class BaseClass0,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C2
		ldloc.0
		ldvirtftn instance string class G1_C2::Method6<object>()
		calli default string(class G2_C221`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C2::Method6.4783<System.Object>()"
		ldstr "class G1_C2 on type class G2_C221`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C2
		ldloc.0
		ldvirtftn instance string class G1_C2::Method5()
		calli default string(class G2_C221`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C2::Method5.4781()"
		ldstr "class G1_C2 on type class G2_C221`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C2
		ldloc.0
		ldvirtftn instance string class G1_C2::Method4()
		calli default string(class G2_C221`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C2::Method4.4779()"
		ldstr "class G1_C2 on type class G2_C221`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C2
		ldloc.0
		ldvirtftn instance string class G1_C2::Method7<object>()
		calli default string(class G2_C221`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C221::Method7.6813<System.Object>()"
		ldstr "class G1_C2 on type class G2_C221`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G2_C221`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C221::Method7.6813<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G2_C221`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method4()
		calli default string(class G2_C221`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C2::Method4.MI.4780()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C221`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method5()
		calli default string(class G2_C221`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C2::Method5.MI.4782()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C221`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method6<object>()
		calli default string(class G2_C221`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C2::Method6.MI.4784<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C221`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C221`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C221::Method7.MI.6814<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C221`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G2_C221`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C2::Method4.MI.4780()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C221`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G2_C221`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C2::Method5.MI.4782()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C221`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G2_C221`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C2::Method6.MI.4784<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C221`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C221`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C221`2<class BaseClass0,class BaseClass1>::ClassMethod1773<object>()
		calli default string(class G2_C221`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C221::ClassMethod1773.6816<System.Object>()"
		ldstr "class G2_C221`2<class BaseClass0,class BaseClass1> on type class G2_C221`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C221`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C221`2<class BaseClass0,class BaseClass1>::ClassMethod1772()
		calli default string(class G2_C221`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C221::ClassMethod1772.6815()"
		ldstr "class G2_C221`2<class BaseClass0,class BaseClass1> on type class G2_C221`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C221`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C221`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C221`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C221::Method7.6813<System.Object>()"
		ldstr "class G2_C221`2<class BaseClass0,class BaseClass1> on type class G2_C221`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C221`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C221`2<class BaseClass0,class BaseClass1>::Method6<object>()
		calli default string(class G2_C221`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C2::Method6.4783<System.Object>()"
		ldstr "class G2_C221`2<class BaseClass0,class BaseClass1> on type class G2_C221`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C221`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C221`2<class BaseClass0,class BaseClass1>::Method5()
		calli default string(class G2_C221`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C2::Method5.4781()"
		ldstr "class G2_C221`2<class BaseClass0,class BaseClass1> on type class G2_C221`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C221`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C221`2<class BaseClass0,class BaseClass1>::Method4()
		calli default string(class G2_C221`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C2::Method4.4779()"
		ldstr "class G2_C221`2<class BaseClass0,class BaseClass1> on type class G2_C221`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C221`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C221::Method7.MI.6814<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C221`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G2_C221`2<class BaseClass1,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C2
		ldloc.0
		ldvirtftn instance string class G1_C2::Method6<object>()
		calli default string(class G2_C221`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C2::Method6.4783<System.Object>()"
		ldstr "class G1_C2 on type class G2_C221`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C2
		ldloc.0
		ldvirtftn instance string class G1_C2::Method5()
		calli default string(class G2_C221`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C2::Method5.4781()"
		ldstr "class G1_C2 on type class G2_C221`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C2
		ldloc.0
		ldvirtftn instance string class G1_C2::Method4()
		calli default string(class G2_C221`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C2::Method4.4779()"
		ldstr "class G1_C2 on type class G2_C221`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C2
		ldloc.0
		ldvirtftn instance string class G1_C2::Method7<object>()
		calli default string(class G2_C221`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C221::Method7.6813<System.Object>()"
		ldstr "class G1_C2 on type class G2_C221`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G2_C221`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C221::Method7.MI.6814<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G2_C221`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method4()
		calli default string(class G2_C221`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C2::Method4.MI.4780()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C221`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method5()
		calli default string(class G2_C221`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C2::Method5.MI.4782()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C221`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method6<object>()
		calli default string(class G2_C221`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C2::Method6.MI.4784<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C221`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C221`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C221::Method7.MI.6814<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C221`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G2_C221`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C2::Method4.MI.4780()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C221`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G2_C221`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C2::Method5.MI.4782()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C221`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G2_C221`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C2::Method6.MI.4784<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C221`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C221`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C221`2<class BaseClass1,class BaseClass0>::ClassMethod1773<object>()
		calli default string(class G2_C221`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C221::ClassMethod1773.6816<System.Object>()"
		ldstr "class G2_C221`2<class BaseClass1,class BaseClass0> on type class G2_C221`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C221`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C221`2<class BaseClass1,class BaseClass0>::ClassMethod1772()
		calli default string(class G2_C221`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C221::ClassMethod1772.6815()"
		ldstr "class G2_C221`2<class BaseClass1,class BaseClass0> on type class G2_C221`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C221`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C221`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G2_C221`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C221::Method7.6813<System.Object>()"
		ldstr "class G2_C221`2<class BaseClass1,class BaseClass0> on type class G2_C221`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C221`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C221`2<class BaseClass1,class BaseClass0>::Method6<object>()
		calli default string(class G2_C221`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C2::Method6.4783<System.Object>()"
		ldstr "class G2_C221`2<class BaseClass1,class BaseClass0> on type class G2_C221`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C221`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C221`2<class BaseClass1,class BaseClass0>::Method5()
		calli default string(class G2_C221`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C2::Method5.4781()"
		ldstr "class G2_C221`2<class BaseClass1,class BaseClass0> on type class G2_C221`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C221`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C221`2<class BaseClass1,class BaseClass0>::Method4()
		calli default string(class G2_C221`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C2::Method4.4779()"
		ldstr "class G2_C221`2<class BaseClass1,class BaseClass0> on type class G2_C221`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G2_C221`2<class BaseClass1,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C2
		ldloc.0
		ldvirtftn instance string class G1_C2::Method6<object>()
		calli default string(class G2_C221`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C2::Method6.4783<System.Object>()"
		ldstr "class G1_C2 on type class G2_C221`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C2
		ldloc.0
		ldvirtftn instance string class G1_C2::Method5()
		calli default string(class G2_C221`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C2::Method5.4781()"
		ldstr "class G1_C2 on type class G2_C221`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C2
		ldloc.0
		ldvirtftn instance string class G1_C2::Method4()
		calli default string(class G2_C221`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C2::Method4.4779()"
		ldstr "class G1_C2 on type class G2_C221`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C2
		ldloc.0
		ldvirtftn instance string class G1_C2::Method7<object>()
		calli default string(class G2_C221`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C221::Method7.6813<System.Object>()"
		ldstr "class G1_C2 on type class G2_C221`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G2_C221`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C221::Method7.6813<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G2_C221`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method4()
		calli default string(class G2_C221`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C2::Method4.MI.4780()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C221`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method5()
		calli default string(class G2_C221`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C2::Method5.MI.4782()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C221`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method6<object>()
		calli default string(class G2_C221`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C2::Method6.MI.4784<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C221`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C221`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C221::Method7.MI.6814<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C221`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G2_C221`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C2::Method4.MI.4780()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C221`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G2_C221`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C2::Method5.MI.4782()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C221`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G2_C221`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C2::Method6.MI.4784<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C221`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C221`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C221`2<class BaseClass1,class BaseClass1>::ClassMethod1773<object>()
		calli default string(class G2_C221`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C221::ClassMethod1773.6816<System.Object>()"
		ldstr "class G2_C221`2<class BaseClass1,class BaseClass1> on type class G2_C221`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C221`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C221`2<class BaseClass1,class BaseClass1>::ClassMethod1772()
		calli default string(class G2_C221`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C221::ClassMethod1772.6815()"
		ldstr "class G2_C221`2<class BaseClass1,class BaseClass1> on type class G2_C221`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C221`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C221`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C221`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C221::Method7.6813<System.Object>()"
		ldstr "class G2_C221`2<class BaseClass1,class BaseClass1> on type class G2_C221`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C221`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C221`2<class BaseClass1,class BaseClass1>::Method6<object>()
		calli default string(class G2_C221`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C2::Method6.4783<System.Object>()"
		ldstr "class G2_C221`2<class BaseClass1,class BaseClass1> on type class G2_C221`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C221`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C221`2<class BaseClass1,class BaseClass1>::Method5()
		calli default string(class G2_C221`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C2::Method5.4781()"
		ldstr "class G2_C221`2<class BaseClass1,class BaseClass1> on type class G2_C221`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C221`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C221`2<class BaseClass1,class BaseClass1>::Method4()
		calli default string(class G2_C221`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C2::Method4.4779()"
		ldstr "class G2_C221`2<class BaseClass1,class BaseClass1> on type class G2_C221`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C221`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C221::Method7.MI.6814<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C221`2<class BaseClass1,class BaseClass1>"
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
